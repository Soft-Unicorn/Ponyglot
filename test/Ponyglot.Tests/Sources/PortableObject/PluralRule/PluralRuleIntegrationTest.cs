using AwesomeAssertions;
using Ponyglot.Sources.PortableObject.PluralRule;
using Xunit;

namespace Ponyglot.Tests.Sources.PortableObject.PluralRule;

// See: https://www.gnu.org/software/gettext/manual/html_node/Plural-forms.html
public class PluralRuleIntegrationTest
{
    [Theory]
    [InlineData(0, 1)] // 0 -> n!=1 -> plural (form 1)
    [InlineData(1, 0)] // 1 -> n==1 -> singular (form 0)
    [InlineData(2, 1)] // 2 -> n!=1 -> plural (form 1)
    public void Evaluation_TwoForms_SimpleSingularPlural_ReturnsTheExpectedValue(int n, int expected)
    {
        // Arrange
        var expression = Parse("n != 1");

        // Act
        var result = expression.Evaluate(n);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, 0)] // 0 -> always 0
    [InlineData(1, 0)] // 1 -> always 0
    [InlineData(2, 0)] // 2 -> always 0
    [InlineData(101, 0)] // 101 -> always 0
    public void Evaluation_OneForm_ConstantZero_ReturnsTheExpectedValue(int n, int expected)
    {
        // Arrange
        var expression = Parse("0");

        // Act
        var result = expression.Evaluate(n);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, 0)] // 0 -> n>1 false -> singular (form 0)
    [InlineData(1, 0)] // 1 -> n>1 false -> singular (form 0)
    [InlineData(2, 1)] // 2 -> n>1 true  -> plural (form 1)
    public void Evaluation_TwoForms_FrenchStyle_ReturnsTheExpectedValue(int n, int expected)
    {
        // Arrange
        var expression = Parse("n > 1");

        // Act
        var result = expression.Evaluate(n);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, 1)] // 0 -> n%10!=1 true  -> plural (form 1)
    [InlineData(1, 0)] // 1 -> n%10==1 and n%100!=11 -> singular (form 0)
    [InlineData(2, 1)] // 2 -> n%10!=1 true  -> plural (form 1)
    [InlineData(11, 1)] // 11 -> n%100==11 special -> plural (form 1)
    [InlineData(21, 0)] // 21 -> ends with 1 and not 11 -> singular (form 0)
    [InlineData(101, 0)] // 101 -> ends with 1 and not 11 -> singular (form 0)
    [InlineData(111, 1)] // 111 -> n%100==11 special -> plural (form 1)
    public void Evaluation_TwoForms_IcelandicStyle_ReturnsTheExpectedValue(int n, int expected)
    {
        // Arrange
        var expression = Parse("n%10!=1 || n%100==11");

        // Act
        var result = expression.Evaluate(n);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, 1)] // 0 -> n==1 false -> else branch -> form 1
    [InlineData(1, 0)] // 1 -> n==1 true  -> then branch -> form 0
    [InlineData(2, 1)] // 2 -> n==1 false -> else branch -> form 1
    public void Evaluation_TwoForms_TernaryVariant_ReturnsTheExpectedValue(int n, int expected)
    {
        // Arrange
        var expression = Parse("n==1 ? 0 : 1");

        // Act
        var result = expression.Evaluate(n);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(1, 0)] // 1 -> first branch n==1 -> form 0
    [InlineData(2, 1)] // 2 -> second branch (2..4) -> form 1
    [InlineData(4, 1)] // 4 -> second branch (2..4) -> form 1
    [InlineData(0, 2)] // 0 -> else branch -> form 2
    [InlineData(5, 2)] // 5 -> else branch -> form 2
    public void Evaluation_ThreeForms_CzechSlovak_ReturnsTheExpectedValue(int n, int expected)
    {
        // Arrange
        var expression = Parse("n==1 ? 0 : n>=2 && n<=4 ? 1 : 2");

        // Act
        var result = expression.Evaluate(n);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(1, 0)] // 1 -> %10==1 and %100!=11 -> form 0
    [InlineData(21, 0)] // 21 -> %10==1 and %100!=11 -> form 0
    [InlineData(101, 0)] // 101 -> %10==1 and %100!=11 -> form 0
    [InlineData(2, 1)] // 2 -> %10 in 2..4 and %100<10 -> form 1
    [InlineData(4, 1)] // 4 -> %10 in 2..4 and %100<10 -> form 1
    [InlineData(22, 1)] // 22 -> %10 in 2..4 and %100>=20 -> form 1
    [InlineData(24, 1)] // 24 -> %10 in 2..4 and %100>=20 -> form 1
    [InlineData(0, 2)] // 0 -> default -> form 2
    [InlineData(5, 2)] // 5 -> default -> form 2
    [InlineData(11, 2)] // 11 -> blocked from form 0 by %100==11 -> default -> form 2
    [InlineData(12, 2)] // 12 -> blocked from form 1 by %100 in 10..19 -> default -> form 2
    [InlineData(14, 2)] // 14 -> blocked from form 1 by %100 in 10..19 -> default -> form 2
    [InlineData(20, 2)] // 20 -> %100==20 -> default -> form 2
    [InlineData(25, 2)] // 25 -> %10==5 -> default -> form 2
    public void Evaluation_ThreeForms_SlavicSimple_ReturnsTheExpectedValue(int n, int expected)
    {
        // Arrange
        var expression = Parse("n%10==1 && n%100!=11 ? 0 : n%10>=2 && n%10<=4 && (n%100<10 || n%100>=20) ? 1 : 2");

        // Act
        var result = expression.Evaluate(n);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(1, 0)] // 1 -> n==1 -> form 0
    [InlineData(2, 1)] // 2 -> %10 in 2..4 and %100<12 -> form 1
    [InlineData(4, 1)] // 4 -> %10 in 2..4 and %100<12 -> form 1
    [InlineData(22, 1)] // 22 -> %10 in 2..4 and %100>14 -> form 1
    [InlineData(24, 1)] // 24 -> %10 in 2..4 and %100>14 -> form 1
    [InlineData(0, 2)] // 0 -> default -> form 2
    [InlineData(5, 2)] // 5 -> default -> form 2
    [InlineData(12, 2)] // 12 -> excluded by %100 in 12..14 -> default -> form 2
    [InlineData(13, 2)] // 13 -> excluded by %100 in 12..14 -> default -> form 2
    [InlineData(14, 2)] // 14 -> excluded by %100 in 12..14 -> default -> form 2
    [InlineData(112, 2)] // 112 -> %100==12 -> excluded -> default -> form 2
    public void Evaluation_ThreeForms_Polish_ReturnsTheExpectedValue(int n, int expected)
    {
        // Arrange
        var expression = Parse("n==1 ? 0 : n%10>=2 && n%10<=4 && (n%100<12 || n%100>14) ? 1 : 2");

        // Act
        var result = expression.Evaluate(n);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(1, 0)] // 1 -> %10==1 and %100!=11 -> form 0
    [InlineData(21, 0)] // 21 -> %10==1 and %100!=11 -> form 0
    [InlineData(101, 0)] // 101 -> %10==1 and %100!=11 -> form 0
    [InlineData(2, 1)] // 2 -> %10>=2 and %100<10 -> form 1
    [InlineData(9, 1)] // 9 -> %10>=2 and %100<10 -> form 1
    [InlineData(22, 1)] // 22 -> %10>=2 and %100>=20 -> form 1
    [InlineData(29, 1)] // 29 -> %10>=2 and %100>=20 -> form 1
    [InlineData(0, 2)] // 0 -> default -> form 2
    [InlineData(10, 2)] // 10 -> %10==0 -> default -> form 2
    [InlineData(11, 2)] // 11 -> excluded from form 0 by %100==11 -> default -> form 2
    [InlineData(12, 2)] // 12 -> %100 in 10..19 -> default -> form 2
    [InlineData(19, 2)] // 19 -> %100 in 10..19 -> default -> form 2
    public void Evaluation_ThreeForms_LithuanianBalticStyle_ReturnsTheExpectedValue(int n, int expected)
    {
        // Arrange
        var expression = Parse("n%10==1 && n%100!=11 ? 0 : n%10>=2 && (n%100<10 || n%100>=20) ? 1 : 2");

        // Act
        var result = expression.Evaluate(n);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(1, 0)] // 1 -> n==1 -> form 0
    [InlineData(0, 1)] // 0 -> n==0 -> form 1
    [InlineData(2, 1)] // 2 -> %100 in 1..19 -> form 1
    [InlineData(19, 1)] // 19 -> %100 in 1..19 -> form 1
    [InlineData(101, 1)] // 101 -> %100==1 -> form 1 (note: not form 0 because n!=1)
    [InlineData(119, 1)] // 119 -> %100==19 -> form 1
    [InlineData(20, 2)] // 20 -> %100==20 -> else -> form 2
    [InlineData(120, 2)] // 120 -> %100==20 -> else -> form 2
    public void Evaluation_ThreeForms_Romanian_ReturnsTheExpectedValue(int n, int expected)
    {
        // Arrange
        var expression = Parse("n==1 ? 0 : n==0 || (n%100>0 && n%100<20) ? 1 : 2");

        // Act
        var result = expression.Evaluate(n);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(1, 0)] // 1 -> %10==1 -> form 0
    [InlineData(11, 0)] // 11 -> %10==1 -> form 0
    [InlineData(21, 0)] // 21 -> %10==1 -> form 0
    [InlineData(2, 1)] // 2 -> %10==2 -> form 1
    [InlineData(12, 1)] // 12 -> %10==2 -> form 1
    [InlineData(22, 1)] // 22 -> %10==2 -> form 1
    [InlineData(0, 2)] // 0 -> %10==0 -> else -> form 2
    [InlineData(3, 2)] // 3 -> %10==3 -> else -> form 2
    [InlineData(10, 2)] // 10 -> %10==0 -> else -> form 2
    public void Evaluation_ThreeForms_Macedonian_ReturnsTheExpectedValue(int n, int expected)
    {
        // Arrange
        var expression = Parse("n%10==1 ? 0 : n%10==2 ? 1 : 2");

        // Act
        var result = expression.Evaluate(n);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(1, 0)] // 1 -> %100==1 -> form 0
    [InlineData(101, 0)] // 101 -> %100==1 -> form 0
    [InlineData(2, 1)] // 2 -> %100==2 -> form 1
    [InlineData(102, 1)] // 102 -> %100==2 -> form 1
    [InlineData(3, 2)] // 3 -> %100==3 -> form 2
    [InlineData(4, 2)] // 4 -> %100==4 -> form 2
    [InlineData(103, 2)] // 103 -> %100==3 -> form 2
    [InlineData(104, 2)] // 104 -> %100==4 -> form 2
    [InlineData(0, 3)] // 0 -> %100==0 -> other -> form 3
    [InlineData(5, 3)] // 5 -> %100==5 -> other -> form 3
    [InlineData(11, 3)] // 11 -> %100==11 -> other -> form 3
    [InlineData(100, 3)] // 100 -> %100==0 -> other -> form 3
    public void Evaluation_FourForms_Slovenian_ReturnsTheExpectedValue(int n, int expected)
    {
        // Arrange
        var expression = Parse("n%100==1 ? 0 : n%100==2 ? 1 : n%100==3 || n%100==4 ? 2 : 3");

        // Act
        var result = expression.Evaluate(n);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(1, 0)] // 1 -> first branch n==1 -> form 0
    [InlineData(2, 1)] // 2 -> second branch n==2 -> form 1
    [InlineData(8, 2)] // 8 -> third branch (8 or 11) -> form 2
    [InlineData(11, 2)] // 11 -> third branch (8 or 11) -> form 2
    [InlineData(0, 3)] // 0 -> else -> form 3
    [InlineData(3, 3)] // 3 -> else -> form 3
    [InlineData(12, 3)] // 12 -> else -> form 3
    public void Evaluation_FourForms_Welsh_ReturnsTheExpectedValue(int n, int expected)
    {
        // Arrange
        var expression = Parse("n==1 ? 0 : n==2 ? 1 : n==8 || n==11 ? 2 : 3");

        // Act
        var result = expression.Evaluate(n);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(1, 0)] // 1 -> first branch n==1 -> form 0
    [InlineData(2, 1)] // 2 -> second branch n==2 -> form 1
    [InlineData(3, 2)] // 3 -> third branch n<7 -> form 2
    [InlineData(6, 2)] // 6 -> third branch n<7 -> form 2 (upper edge)
    [InlineData(7, 3)] // 7 -> fourth branch n<11 -> form 3 (lower edge)
    [InlineData(10, 3)] // 10 -> fourth branch n<11 -> form 3 (upper edge)
    [InlineData(11, 4)] // 11 -> else -> form 4 (boundary)
    [InlineData(25, 4)] // 25 -> else -> form 4
    public void Evaluation_FiveForms_Irish_ReturnsTheExpectedValue(int n, int expected)
    {
        // Arrange
        var expression = Parse("n==1 ? 0 : n==2 ? 1 : n<7 ? 2 : n<11 ? 3 : 4");

        // Act
        var result = expression.Evaluate(n);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, 0)] // 0 -> n==0 -> form 0
    [InlineData(1, 1)] // 1 -> n==1 -> form 1
    [InlineData(2, 2)] // 2 -> n==2 -> form 2
    [InlineData(3, 3)] // 3 -> %100 in 3..10 -> form 3
    [InlineData(10, 3)] // 10 -> %100 in 3..10 -> form 3
    [InlineData(103, 3)] // 103 -> %100==3 -> form 3
    [InlineData(11, 4)] // 11 -> %100 in 11..99 -> form 4
    [InlineData(99, 4)] // 99 -> %100 in 11..99 -> form 4
    [InlineData(111, 4)] // 111 -> %100==11 -> form 4
    [InlineData(100, 5)] // 100 -> %100==0 -> else -> form 5
    [InlineData(101, 5)] // 101 -> %100==1 -> else -> form 5
    [InlineData(102, 5)] // 102 -> %100==2 -> else -> form 5
    [InlineData(200, 5)] // 200 -> %100==0 -> else -> form 5
    public void Evaluation_SixForms_Arabic_ReturnsTheExpectedValue(int n, int expected)
    {
        // Arrange
        var expression = Parse("n==0 ? 0 : n==1 ? 1 : n==2 ? 2 : n%100>=3 && n%100<=10 ? 3 : n%100>=11 && n%100<=99 ? 4 : 5");

        // Act
        var result = expression.Evaluate(n);

        // Assert
        result.Should().Be(expected);
    }

    #region Helpers

    private PluralRuleExpression Parse(string expression) => new PluralRuleParser(new PluralRuleLexer(expression)).Parse();

    #endregion
}