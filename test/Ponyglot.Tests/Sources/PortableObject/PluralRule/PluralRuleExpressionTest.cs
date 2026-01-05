using System;
using AwesomeAssertions;
using NSubstitute;
using Ponyglot.Sources.PortableObject.PluralRule;
using Ponyglot.Tests._TestUtils;
using Xunit;

namespace Ponyglot.Tests.Sources.PortableObject.PluralRule;

public class PluralRuleExpressionTest
{
    [Fact]
    public void Evaluate_NoState_CallsTheEvaluator()
    {
        // Arrange
        PluralRuleExpression[] children =
        [
            PluralRuleExpression.New("", [], (_, _) => 0),
            PluralRuleExpression.New("", [], (_, _) => 0),
        ];

        var evaluator = Substitute.For<Func<long, PluralRuleExpression[], long>>();

        var sut = PluralRuleExpression.New("", children, evaluator);

        // Act
        sut.Evaluate(123);

        // Assert
        evaluator.Received(1).Invoke(123, children);
    }

    [Fact]
    public void Evaluate_NoState_ReturnsTheResultOfTheEvaluator()
    {
        // Arrange
        PluralRuleExpression[] children =
        [
            PluralRuleExpression.New("", [], (_, _) => 0),
            PluralRuleExpression.New("", [], (_, _) => 0),
        ];

        var sut = PluralRuleExpression.New("", children, (_, _) => 456);

        // Act
        var result = sut.Evaluate(123);

        // Assert
        result.Should().Be(456);
    }

    [Fact]
    public void ToString_NoChildren_ReturnsExpectedValue()
    {
        // Arrange
        var sut = PluralRuleExpression.New("my-expression", [], (_, _) => 0);

        // Act
        var asString = sut.ToString();

        // Assert
        asString.Should().Be("my-expression");
    }

    [Fact]
    public void ToString_OneChild_ReturnsExpectedValue()
    {
        // Arrange
        var sut = PluralRuleExpression.New(
            "my-expression",
            [
                PluralRuleExpression.New("my-child", [], (_, _) => 0),
            ],
            (_, _) => 0);

        // Act
        var asString = sut.ToString();

        // Assert
        asString.Should().Be("my-expression(my-child)");
    }

    [Fact]
    public void ToString_TwoChild_ReturnsExpectedValue()
    {
        // Arrange
        var sut = PluralRuleExpression.New(
            "my-expression",
            [
                PluralRuleExpression.New("my-child-1", [], (_, _) => 0),
                PluralRuleExpression.New("my-child-2", [], (_, _) => 0),
            ],
            (_, _) => 0);

        // Act
        var asString = sut.ToString();

        // Assert
        asString.Should().Be("my-expression(my-child-1, my-child-2)");
    }

    [Fact]
    public void ToString_TwoChildWithChildren_ReturnsExpectedValue()
    {
        // Arrange
        var sut = PluralRuleExpression.New(
            "my-expression",
            [
                PluralRuleExpression.New(
                    "my-child-1",
                    [
                        PluralRuleExpression.New("my-grandchild-1", [], (_, _) => 0),
                    ],
                    (_, _) => 0),
                PluralRuleExpression.New(
                    "my-child-2",
                    [
                        PluralRuleExpression.New("my-grandchild-2", [], (_, _) => 0),
                    ],
                    (_, _) => 0),
            ],
            (_, _) => 0);

        // Act
        var asString = sut.ToString();

        // Assert
        asString.Should().Be("my-expression(my-child-1(my-grandchild-1), my-child-2(my-grandchild-2))");
    }

    [Fact]
    public void GetDebugView_ThreeLevelsWithoutValue_ReturnsExpectedValue()
    {
        // Arrange
        var sut = PluralRuleExpression.New(
            "my-expression",
            [
                PluralRuleExpression.New(
                    "my-child-1",
                    [
                        PluralRuleExpression.New("my-grandchild-1", [], (_, _) => 0),
                    ],
                    (_, _) => 0),
                PluralRuleExpression.New(
                    "my-child-2",
                    [
                        PluralRuleExpression.New("my-grandchild-2", [], (_, _) => 0),
                    ],
                    (_, _) => 0),
            ],
            (_, _) => 0);

        // Act
        var debugView = sut.GetDebugView();

        // Assert
        var debugViewLines = debugView.SplitLines();
        debugViewLines.Should().BeEquivalentTo(
            "f(n)",
            "└─ my-expression",
            "   ├─ my-child-1",
            "   │  └─ my-grandchild-1",
            "   └─ my-child-2",
            "      └─ my-grandchild-2");
    }

    [Fact]
    public void GetDebugView_ThreeLevelsWithValue_ReturnsExpectedValue()
    {
        // Arrange
        var sut = PluralRuleExpression.New(
            "my-expression",
            [
                PluralRuleExpression.New(
                    "my-child-1",
                    [
                        PluralRuleExpression.New("my-grandchild-1", [], (_, _) => 11),
                    ],
                    (_, _) => 111),
                PluralRuleExpression.New(
                    "my-child-2",
                    [
                        PluralRuleExpression.New("my-grandchild-2", [], (_, _) => 22),
                    ],
                    (_, _) => 222),
            ],
            (_, _) => 3333);

        // Act
        var debugView = sut.GetDebugView(99);

        // Assert
        var debugViewLines = debugView.SplitLines();

        debugViewLines.Should().BeEquivalentTo(
            "f(99) = 3333",
            "└─ my-expression (=3333)",
            "   ├─ my-child-1 (=111)",
            "   │  └─ my-grandchild-1 (=11)",
            "   └─ my-child-2 (=222)",
            "      └─ my-grandchild-2 (=22)");
    }

    [Fact]
    public void Number_Evaluation_ReturnsCorrectValue()
    {
        // Arrange
        var sut = PluralRuleExpression.Number(123);

        // Act
        var result = sut.Evaluate(999);

        // Assert
        result.Should().Be(123);
    }

    [Fact]
    public void NVariable_Evaluation_ReturnsCorrectValue()
    {
        // Arrange
        var sut = PluralRuleExpression.NVariable();

        // Act
        var result = sut.Evaluate(123);

        // Assert
        result.Should().Be(123);
    }

    [Theory]
    [InlineData(-2, 0)]
    [InlineData(-1, 0)]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(2, 0)]
    public void Not_Evaluation_ReturnsCorrectValue(long operandValue, long expected)
    {
        // Arrange
        var operand = PluralRuleExpression.New("", [], (_, _) => operandValue);

        var sut = PluralRuleExpression.Not(operand);

        // Act
        var result = sut.Evaluate(999);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(-2, 2)]
    [InlineData(-1, 1)]
    [InlineData(0, 0)]
    [InlineData(1, -1)]
    [InlineData(2, -2)]
    public void Minus_Evaluation_ReturnsCorrectValue(long operandValue, long expected)
    {
        // Arrange
        var operand = PluralRuleExpression.New("", [], (_, _) => operandValue);

        var sut = PluralRuleExpression.Minus(operand);

        // Act
        var result = sut.Evaluate(999);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(-2, -2)]
    [InlineData(-1, -1)]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    public void Plus_Evaluation_ReturnsCorrectValue(long operandValue, long expected)
    {
        // Arrange
        var operand = PluralRuleExpression.New("", [], (_, _) => operandValue);

        var sut = PluralRuleExpression.Plus(operand);

        // Act
        var result = sut.Evaluate(999);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(1, 1, 2)] // Simple positive
    [InlineData(10, 0, 10)] // Identity
    [InlineData(0, 0, 0)] // Zero
    [InlineData(-5, -5, -10)] // Both negative
    [InlineData(10, -3, 7)] // Positive + Negative
    [InlineData(-10, 3, -7)] // Negative + Positive
    [InlineData(1000000, 2000000, 3000000)] // Larger values
    public void Add_Evaluation_ReturnsCorrectValue(long leftValue, long rightValue, long expected)
    {
        // Arrange
        var left = PluralRuleExpression.New("", [], (_, _) => leftValue);
        var right = PluralRuleExpression.New("", [], (_, _) => rightValue);

        var sut = PluralRuleExpression.Add(left, right);

        // Act
        var result = sut.Evaluate(999);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(10, 3, 7)] // Positive result
    [InlineData(3, 10, -7)] // Negative result
    [InlineData(5, 5, 0)] // Zero result
    [InlineData(5, 0, 5)] // Identity
    [InlineData(-5, -5, 0)] // Subtracting negative from negative
    [InlineData(-5, 5, -10)] // Negative - Positive
    public void Subtract_Evaluation_ReturnsCorrectValue(long leftValue, long rightValue, long expected)
    {
        // Arrange
        var left = PluralRuleExpression.New("", [], (_, _) => leftValue);
        var right = PluralRuleExpression.New("", [], (_, _) => rightValue);

        var sut = PluralRuleExpression.Subtract(left, right);

        // Act
        var result = sut.Evaluate(999);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(3, 4, 12)] // Positive
    [InlineData(-3, 4, -12)] // Negative result
    [InlineData(-3, -4, 12)] // Double negative
    [InlineData(10, 0, 0)] // Multiply by zero
    [InlineData(10, 1, 10)] // Identity
    public void Multiply_Evaluation_ReturnsCorrectValue(long leftValue, long rightValue, long expected)
    {
        // Arrange
        var left = PluralRuleExpression.New("", [], (_, _) => leftValue);
        var right = PluralRuleExpression.New("", [], (_, _) => rightValue);

        var sut = PluralRuleExpression.Multiply(left, right);

        // Act
        var result = sut.Evaluate(999);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(10, 2, 5)] // Even division
    [InlineData(10, 3, 3)] // Integer truncation
    [InlineData(-10, 2, -5)] // Negative dividend
    [InlineData(10, -2, -5)] // Negative divisor
    [InlineData(0, 10, 0)] // Zero dividend
    public void Divide_Evaluation_ReturnsCorrectValue(long leftValue, long rightValue, long expected)
    {
        // Arrange
        var left = PluralRuleExpression.New("", [], (_, _) => leftValue);
        var right = PluralRuleExpression.New("", [], (_, _) => rightValue);

        var sut = PluralRuleExpression.Divide(left, right);

        // Act
        var result = sut.Evaluate(999);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(10, 3, 1)] // Remainder
    [InlineData(10, 5, 0)] // No remainder
    [InlineData(-10, 3, -1)] // Negative dividend
    [InlineData(10, -3, 1)] // Negative divisor
    [InlineData(0, 5, 0)] // Zero dividend
    public void Modulo_Evaluation_ReturnsCorrectValue(long leftValue, long rightValue, long expected)
    {
        // Arrange
        var left = PluralRuleExpression.New("", [], (_, _) => leftValue);
        var right = PluralRuleExpression.New("", [], (_, _) => rightValue);

        var sut = PluralRuleExpression.Modulo(left, right);

        // Act
        var result = sut.Evaluate(999);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(1, 1, 1)] // Identical
    [InlineData(1, 0, 0)] // Different
    [InlineData(-1, -1, 1)] // Negative identical
    [InlineData(0, 0, 1)] // Zero identical
    public void Equal_Evaluation_ReturnsCorrectValue(long leftValue, long rightValue, long expected)
    {
        // Arrange
        var left = PluralRuleExpression.New("", [], (_, _) => leftValue);
        var right = PluralRuleExpression.New("", [], (_, _) => rightValue);

        var sut = PluralRuleExpression.Equal(left, right);

        // Act
        var result = sut.Evaluate(999);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(1, 0, 1)] // Different
    [InlineData(1, 1, 0)] // Identical
    [InlineData(-1, 1, 1)] // Different signs
    [InlineData(0, 0, 0)] // Zero identical
    public void NotEqual_Evaluation_ReturnsCorrectValue(long leftValue, long rightValue, long expected)
    {
        // Arrange
        var left = PluralRuleExpression.New("", [], (_, _) => leftValue);
        var right = PluralRuleExpression.New("", [], (_, _) => rightValue);

        var sut = PluralRuleExpression.NotEqual(left, right);

        // Act
        var result = sut.Evaluate(999);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(4, 5, 1)] // Left (positive) < Right (positive)
    [InlineData(-2, -1, 1)] // Left (negative) < Right (positive)
    [InlineData(-2, 3, 1)] // Left (negative) < Right (positive)
    [InlineData(5, 5, 0)] // Left (positive) == Right (positive)
    [InlineData(-5, -5, 0)] // Left (negative) == Right (negative)
    [InlineData(5, 4, 0)] // Left (positive) > Right (positive)
    [InlineData(-1, -2, 0)] // Left (negative) > Right (negative)
    public void LessThan_Evaluation_ReturnsCorrectValue(long leftValue, long rightValue, long expected)
    {
        // Arrange
        var left = PluralRuleExpression.New("", [], (_, _) => leftValue);
        var right = PluralRuleExpression.New("", [], (_, _) => rightValue);

        var sut = PluralRuleExpression.LessThan(left, right);

        // Act
        var result = sut.Evaluate(999);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(4, 5, 1)] // Left (positive) < Right (positive)
    [InlineData(-2, -1, 1)] // Left (negative) < Right (positive)
    [InlineData(-2, 3, 1)] // Left (negative) < Right (positive)
    [InlineData(5, 5, 1)] // Left (positive) == Right (positive)
    [InlineData(-5, -5, 1)] // Left (negative) == Right (negative)
    [InlineData(5, 4, 0)] // Left (positive) > Right (positive)
    [InlineData(-1, -2, 0)] // Left (negative) > Right (negative)
    public void LessThanOrEqual_Evaluation_ReturnsCorrectValue(long leftValue, long rightValue, long expected)
    {
        // Arrange
        var left = PluralRuleExpression.New("", [], (_, _) => leftValue);
        var right = PluralRuleExpression.New("", [], (_, _) => rightValue);

        var sut = PluralRuleExpression.LessThanOrEqual(left, right);

        // Act
        var result = sut.Evaluate(999);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(4, 5, 0)] // Left (positive) < Right (positive)
    [InlineData(-2, -1, 0)] // Left (negative) < Right (positive)
    [InlineData(-2, 3, 0)] // Left (negative) < Right (positive)
    [InlineData(5, 5, 0)] // Left (positive) == Right (positive)
    [InlineData(-5, -5, 0)] // Left (negative) == Right (negative)
    [InlineData(5, 4, 1)] // Left (positive) > Right (positive)
    [InlineData(-1, -2, 1)] // Left (negative) > Right (negative)
    public void GreaterThan_Evaluation_ReturnsCorrectValue(long leftValue, long rightValue, long expected)
    {
        // Arrange
        var left = PluralRuleExpression.New("", [], (_, _) => leftValue);
        var right = PluralRuleExpression.New("", [], (_, _) => rightValue);

        var sut = PluralRuleExpression.GreaterThan(left, right);

        // Act
        var result = sut.Evaluate(999);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(4, 5, 0)] // Left (positive) < Right (positive)
    [InlineData(-2, -1, 0)] // Left (negative) < Right (positive)
    [InlineData(-2, 3, 0)] // Left (negative) < Right (positive)
    [InlineData(5, 5, 1)] // Left (positive) == Right (positive)
    [InlineData(-5, -5, 1)] // Left (negative) == Right (negative)
    [InlineData(5, 4, 1)] // Left (positive) > Right (positive)
    [InlineData(-1, -2, 1)] // Left (negative) > Right (negative)
    public void GreaterThanOrEqual_Evaluation_ReturnsCorrectValue(long leftValue, long rightValue, long expected)
    {
        // Arrange
        var left = PluralRuleExpression.New("", [], (_, _) => leftValue);
        var right = PluralRuleExpression.New("", [], (_, _) => rightValue);

        var sut = PluralRuleExpression.GreaterThanOrEqual(left, right);

        // Act
        var result = sut.Evaluate(999);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(1, 1, 1)] // Both true (1)
    [InlineData(3, 2, 1)] // Both true (non-1)
    [InlineData(1, 0, 0)] // Left true, Right false
    [InlineData(0, 1, 0)] // Left false, Right true
    [InlineData(0, 0, 0)] // Both false
    public void And_Evaluation_ReturnsCorrectValue(long leftValue, long rightValue, long expected)
    {
        // Arrange
        var left = PluralRuleExpression.New("", [], (_, _) => leftValue);
        var right = PluralRuleExpression.New("", [], (_, _) => rightValue);

        var sut = PluralRuleExpression.And(left, right);

        // Act
        var result = sut.Evaluate(999);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void And_Evaluation_ShortCircuitIfFirstExpressionFalse()
    {
        // Arrange
        var rightEvaluator = Substitute.For<Func<long, PluralRuleExpression[], long>>();

        var left = PluralRuleExpression.New("", [], (_, _) => 0);
        var right = PluralRuleExpression.New("", [], rightEvaluator);

        var sut = PluralRuleExpression.And(left, right);

        // Act
        _ = sut.Evaluate(999);

        // Assert
        rightEvaluator.DidNotReceiveWithAnyArgs().Invoke(default, default!);
    }

    [Theory]
    [InlineData(1, 1, 1)] // Both true (1)
    [InlineData(3, 2, 1)] // Both true (non-1)
    [InlineData(1, 0, 1)] // Left true, Right false
    [InlineData(0, 1, 1)] // Left false, Right true
    [InlineData(0, 0, 0)] // Both false
    public void Or_Evaluation_ReturnsCorrectValue(long leftValue, long rightValue, long expected)
    {
        // Arrange
        var left = PluralRuleExpression.New("", [], (_, _) => leftValue);
        var right = PluralRuleExpression.New("", [], (_, _) => rightValue);

        var sut = PluralRuleExpression.Or(left, right);

        // Act
        var result = sut.Evaluate(999);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Or_Evaluation_ShortCircuitIfFirstExpressionTrue()
    {
        // Arrange
        var rightEvaluator = Substitute.For<Func<long, PluralRuleExpression[], long>>();

        var left = PluralRuleExpression.New("", [], (_, _) => 1);
        var right = PluralRuleExpression.New("", [], rightEvaluator);

        var sut = PluralRuleExpression.Or(left, right);

        // Act
        _ = sut.Evaluate(999);

        // Assert
        rightEvaluator.DidNotReceiveWithAnyArgs().Invoke(default, default!);
    }

    [Theory]
    [InlineData(1, 100, 200, 100)] // Condition true (1)
    [InlineData(5, 100, 200, 100)] // Condition true (non-zero)
    [InlineData(0, 100, 200, 200)] // Condition false (zero)
    [InlineData(-1, 100, 200, 100)] // Condition true (negative is non-zero)
    public void TernaryCondition_Evaluation_ReturnsCorrectValue(long conditionValue, long trueValue, long falseValue, long expected)
    {
        // Arrange
        var condition = PluralRuleExpression.New("", [], (_, _) => conditionValue);
        var trueExpr = PluralRuleExpression.New("", [], (_, _) => trueValue);
        var falseExpr = PluralRuleExpression.New("", [], (_, _) => falseValue);

        var sut = PluralRuleExpression.TernaryCondition(condition, trueExpr, falseExpr);

        // Act
        var result = sut.Evaluate(999);

        // Assert
        result.Should().Be(expected);
    }
}