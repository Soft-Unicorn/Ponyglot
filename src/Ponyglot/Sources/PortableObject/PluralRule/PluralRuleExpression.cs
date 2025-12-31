using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Ponyglot.Sources.PortableObject.PluralRule;

/// <summary>
/// Represents a node in the abstract syntax tree of a plural rule.
/// </summary>
[DebuggerDisplay("{GetDebugView(null)}")]
internal class PluralRuleExpression
{
    private readonly string _symbol;
    private readonly Func<long, PluralRuleExpression[], long> _evaluator;
    private readonly PluralRuleExpression[] _children;

    /// <summary>
    /// Initialize a new instance of the <see cref="PluralRuleExpression"/> class.
    /// </summary>
    /// <param name="symbol">The symbol to use for the string representation of the node.</param>
    /// <param name="children">The list of child nodes</param>
    /// <param name="evaluator">The function that evaluates the node.</param>
    private PluralRuleExpression(string symbol, PluralRuleExpression[] children, Func<long, PluralRuleExpression[], long> evaluator)
    {
        _children = children;
        _evaluator = evaluator;
        _symbol = symbol;
    }

    /// <summary>
    /// Evaluates the node value.
    /// </summary>
    /// <param name="n">The value of the 'n' variable.</param>
    /// <returns>The evaluated value.</returns>
    public long Evaluate(long n) => _evaluator(n, _children);

    /// <inheritdoc/>
    public override string ToString()
    {
        return _children.Length switch
        {
            0 => _symbol,
            1 => $"{_symbol}({_children[0]})",
            _ => $"{_symbol}({string.Join(", ", (IEnumerable<PluralRuleExpression>)_children)})",
        };
    }

    /// <summary>
    /// Returns a debug string representation of the node tree.
    /// </summary>
    /// <param name="n">The optional <c>n</c> value to evaluate if evaluation results are to be included in the debug view.</param>
    /// <returns>A multi-line <see cref="string"/> that contains a tree-like representation of the expression.</returns>
    public string GetDebugView(long? n = null)
    {
        var sb = new StringBuilder();

        if (n.HasValue)
        {
            sb.Append("f(").Append(n).Append(") = ").Append(Evaluate(n.Value)).AppendLine();
        }
        else
        {
            sb.Append("f(n)").AppendLine();
        }

        AppendDebugView(sb, prefix: "", isLast: true, n);
        return sb.ToString();
    }

    /// <summary>
    /// Appends the node tree to the specified string builder.
    /// </summary>
    /// <param name="sb">The <see cref="StringBuilder"/> to append the node tree to.</param>
    /// <param name="prefix">The prefix for the node.</param>
    /// <param name="isLast"><c>true</c> if this is the last child node; otherwise, <c>false</c>.</param>
    /// <param name="n">The optional <c>n</c> value to evaluate.</param>
    private void AppendDebugView(StringBuilder sb, string prefix, bool isLast, long? n)
    {
        sb.Append(prefix).Append(isLast ? "└─ " : "├─ ").Append(_symbol);

        if (n.HasValue)
        {
            sb.Append(" (=").Append(Evaluate(n.Value)).Append(')');
        }

        sb.AppendLine();

        var childPrefix = isLast ? $"{prefix}   " : $"{prefix}│  ";

        for (var i = 0; i < _children.Length; i++)
        {
            var childIsLast = i == _children.Length - 1;
            _children[i].AppendDebugView(sb, childPrefix, childIsLast, n);
        }
    }

    /// <summary>
    /// A constant numeric value node.
    /// </summary>
    /// <param name="value">The constant number value.</param>
    public static PluralRuleExpression Number(long value)
    {
        return new PluralRuleExpression(value.ToString(CultureInfo.InvariantCulture), [], Evaluate);

        long Evaluate(long n, PluralRuleExpression[] children)
        {
            return value;
        }
    }

    /// <summary>
    /// A node representing the 'n' variable.
    /// </summary>
    public static PluralRuleExpression NVariable()
    {
        return new PluralRuleExpression("n", [], Evaluate);

        long Evaluate(long n, PluralRuleExpression[] children)
        {
            return n;
        }
    }

    /// <summary>
    /// A node representing the logical NOT operation.
    /// </summary>
    /// <param name="operand">The operator operand.</param>
    public static PluralRuleExpression Not(PluralRuleExpression operand)
    {
        return new PluralRuleExpression("!", [operand], Evaluate);

        long Evaluate(long n, PluralRuleExpression[] children)
        {
            var operandValue = children[0].Evaluate(n);
            var result = operandValue == 0 ? 1 : 0;
            return result;
        }
    }

    /// <summary>
    /// A node representing the unary negative operation.
    /// </summary>
    /// <param name="operand">The operator operand.</param>
    public static PluralRuleExpression Minus(PluralRuleExpression operand)
    {
        return new PluralRuleExpression("-", [operand], Evaluate);

        long Evaluate(long n, PluralRuleExpression[] children)
        {
            var operandValue = children[0].Evaluate(n);
            var result = -operandValue;
            return result;
        }
    }

    /// <summary>
    /// A node representing the unary positive operation.
    /// </summary>
    /// <param name="operand">The operator operand.</param>
    public static PluralRuleExpression Plus(PluralRuleExpression operand)
    {
        return new PluralRuleExpression("+", [operand], Evaluate);

        long Evaluate(long n, PluralRuleExpression[] children)
        {
            var operandValue = children[0].Evaluate(n);
            var result = operandValue;
            return result;
        }
    }

    /// <summary>
    /// A node representing an addition operation.
    /// </summary>
    /// <param name="leftOperand">The left operator operand.</param>
    /// <param name="rightOperand">The right operator operand.</param>
    public static PluralRuleExpression Add(PluralRuleExpression leftOperand, PluralRuleExpression rightOperand)
    {
        return new PluralRuleExpression("+", [leftOperand, rightOperand], Evaluate);

        long Evaluate(long n, PluralRuleExpression[] children)
        {
            var leftOperandValue = children[0].Evaluate(n);
            var rightOperandValue = children[1].Evaluate(n);
            var result = leftOperandValue + rightOperandValue;
            return result;
        }
    }

    /// <summary>
    /// A node representing a subtraction operation.
    /// </summary>
    /// <param name="leftOperand">The left operator operand.</param>
    /// <param name="rightOperand">The right operator operand.</param>
    public static PluralRuleExpression Subtract(PluralRuleExpression leftOperand, PluralRuleExpression rightOperand)
    {
        return new PluralRuleExpression("-", [leftOperand, rightOperand], Evaluate);

        long Evaluate(long n, PluralRuleExpression[] children)
        {
            var leftOperandValue = children[0].Evaluate(n);
            var rightOperandValue = children[1].Evaluate(n);
            var result = leftOperandValue - rightOperandValue;
            return result;
        }
    }

    /// <summary>
    /// A node representing a multiplication operation.
    /// </summary>
    /// <param name="leftOperand">The left operator operand.</param>
    /// <param name="rightOperand">The right operator operand.</param>
    public static PluralRuleExpression Multiply(PluralRuleExpression leftOperand, PluralRuleExpression rightOperand)
    {
        return new PluralRuleExpression("*", [leftOperand, rightOperand], Evaluate);

        long Evaluate(long n, PluralRuleExpression[] children)
        {
            var leftOperandValue = children[0].Evaluate(n);
            var rightOperandValue = children[1].Evaluate(n);
            var result = leftOperandValue * rightOperandValue;
            return result;
        }
    }

    /// <summary>
    /// A node representing a division operation.
    /// </summary>
    /// <param name="leftOperand">The left operator operand.</param>
    /// <param name="rightOperand">The right operator operand.</param>
    public static PluralRuleExpression Divide(PluralRuleExpression leftOperand, PluralRuleExpression rightOperand)
    {
        return new PluralRuleExpression("/", [leftOperand, rightOperand], Evaluate);

        long Evaluate(long n, PluralRuleExpression[] children)
        {
            var leftOperandValue = children[0].Evaluate(n);
            var rightOperandValue = children[1].Evaluate(n);
            var result = leftOperandValue / rightOperandValue;
            return result;
        }
    }

    /// <summary>
    /// A node representing a modulo operation.
    /// </summary>
    /// <param name="leftOperand">The left operator operand.</param>
    /// <param name="rightOperand">The right operator operand.</param>
    public static PluralRuleExpression Modulo(PluralRuleExpression leftOperand, PluralRuleExpression rightOperand)
    {
        return new PluralRuleExpression("%", [leftOperand, rightOperand], Evaluate);

        long Evaluate(long n, PluralRuleExpression[] children)
        {
            var leftOperandValue = children[0].Evaluate(n);
            var rightOperandValue = children[1].Evaluate(n);
            var result = leftOperandValue % rightOperandValue;
            return result;
        }
    }

    /// <summary>
    /// A node representing an equality comparison operation.
    /// </summary>
    /// <param name="leftOperand">The left operator operand.</param>
    /// <param name="rightOperand">The right operator operand.</param>
    public static PluralRuleExpression Equal(PluralRuleExpression leftOperand, PluralRuleExpression rightOperand)
    {
        return new PluralRuleExpression("==", [leftOperand, rightOperand], Evaluate);

        long Evaluate(long n, PluralRuleExpression[] children)
        {
            var leftOperandValue = children[0].Evaluate(n);
            var rightOperandValue = children[1].Evaluate(n);
            var result = leftOperandValue == rightOperandValue ? 1L : 0L;
            return result;
        }
    }

    /// <summary>
    /// A node representing a non-equality comparison operation.
    /// </summary>
    /// <param name="leftOperand">The left operator operand.</param>
    /// <param name="rightOperand">The right operator operand.</param>
    public static PluralRuleExpression NotEqual(PluralRuleExpression leftOperand, PluralRuleExpression rightOperand)
    {
        return new PluralRuleExpression("!=", [leftOperand, rightOperand], Evaluate);

        long Evaluate(long n, PluralRuleExpression[] children)
        {
            var leftOperandValue = children[0].Evaluate(n);
            var rightOperandValue = children[1].Evaluate(n);
            var result = leftOperandValue != rightOperandValue ? 1L : 0L;
            return result;
        }
    }

    /// <summary>
    /// A node representing a strictly less than comparison operation.
    /// </summary>
    /// <param name="leftOperand">The left operator operand.</param>
    /// <param name="rightOperand">The right operator operand.</param>
    public static PluralRuleExpression LessThan(PluralRuleExpression leftOperand, PluralRuleExpression rightOperand)
    {
        return new PluralRuleExpression("<", [leftOperand, rightOperand], Evaluate);

        long Evaluate(long n, PluralRuleExpression[] children)
        {
            var leftOperandValue = children[0].Evaluate(n);
            var rightOperandValue = children[1].Evaluate(n);
            var result = leftOperandValue < rightOperandValue ? 1L : 0L;
            return result;
        }
    }

    /// <summary>
    /// A node representing a less than or equal comparison operation.
    /// </summary>
    /// <param name="leftOperand">The left operator operand.</param>
    /// <param name="rightOperand">The right operator operand.</param>
    public static PluralRuleExpression LessThanOrEqual(PluralRuleExpression leftOperand, PluralRuleExpression rightOperand)
    {
        return new PluralRuleExpression("≤", [leftOperand, rightOperand], Evaluate);

        long Evaluate(long n, PluralRuleExpression[] children)
        {
            var leftOperandValue = children[0].Evaluate(n);
            var rightOperandValue = children[1].Evaluate(n);
            var result = leftOperandValue <= rightOperandValue ? 1L : 0L;
            return result;
        }
    }

    /// <summary>
    /// A node representing a strictly greater than comparison operation.
    /// </summary>
    /// <param name="leftOperand">The left operator operand.</param>
    /// <param name="rightOperand">The right operator operand.</param>
    public static PluralRuleExpression GreaterThan(PluralRuleExpression leftOperand, PluralRuleExpression rightOperand)
    {
        return new PluralRuleExpression(">", [leftOperand, rightOperand], Evaluate);

        long Evaluate(long n, PluralRuleExpression[] children)
        {
            var leftOperandValue = children[0].Evaluate(n);
            var rightOperandValue = children[1].Evaluate(n);
            var result = leftOperandValue > rightOperandValue ? 1L : 0L;
            return result;
        }
    }

    /// <summary>
    /// A node representing a greater than or equal comparison operation.
    /// </summary>
    /// <param name="leftOperand">The left operator operand.</param>
    /// <param name="rightOperand">The right operator operand.</param>
    public static PluralRuleExpression GreaterThanOrEqual(PluralRuleExpression leftOperand, PluralRuleExpression rightOperand)
    {
        return new PluralRuleExpression("≥", [leftOperand, rightOperand], Evaluate);

        long Evaluate(long n, PluralRuleExpression[] children)
        {
            var leftOperandValue = children[0].Evaluate(n);
            var rightOperandValue = children[1].Evaluate(n);
            var result = leftOperandValue >= rightOperandValue ? 1L : 0L;
            return result;
        }
    }

    /// <summary>
    /// A node representing a logical AND operation.
    /// </summary>
    /// <param name="leftOperand">The left operator operand.</param>
    /// <param name="rightOperand">The right operator operand.</param>
    public static PluralRuleExpression And(PluralRuleExpression leftOperand, PluralRuleExpression rightOperand)
    {
        return new PluralRuleExpression("&&", [leftOperand, rightOperand], Evaluate);

        long Evaluate(long n, PluralRuleExpression[] children)
        {
            var leftOperandValue = children[0].Evaluate(n);
            if (leftOperandValue == 0L)
            {
                return 0L;
            }

            var rightOperandValue = children[1].Evaluate(n);
            return rightOperandValue == 0L ? 0L : 1L;
        }
    }

    /// <summary>
    /// A node representing a logical OR operation.
    /// </summary>
    /// <param name="leftOperand">The left operator operand.</param>
    /// <param name="rightOperand">The right operator operand.</param>
    public static PluralRuleExpression Or(PluralRuleExpression leftOperand, PluralRuleExpression rightOperand)
    {
        return new PluralRuleExpression("||", [leftOperand, rightOperand], Evaluate);

        long Evaluate(long n, PluralRuleExpression[] children)
        {
            var leftOperandValue = children[0].Evaluate(n);
            if (leftOperandValue != 0L)
            {
                return 1L;
            }

            var rightOperandValue = children[1].Evaluate(n);
            return rightOperandValue != 0L ? 1L : 0L;
        }
    }

    /// <summary>
    /// A node representing a ternary conditional operation.
    /// </summary>
    /// <param name="condition">The condition operand.</param>
    /// <param name="trueExpression">The true expression operand.</param>
    /// <param name="falseExpression">The false expression operand.</param>
    public static PluralRuleExpression TernaryCondition(PluralRuleExpression condition, PluralRuleExpression trueExpression, PluralRuleExpression falseExpression)
    {
        return new PluralRuleExpression("?:", [condition, trueExpression, falseExpression], Evaluate);

        long Evaluate(long n, PluralRuleExpression[] children)
        {
            var isTrue = condition.Evaluate(n) != 0L;
            var result = isTrue ? trueExpression.Evaluate(n) : falseExpression.Evaluate(n);
            return result;
        }
    }
}