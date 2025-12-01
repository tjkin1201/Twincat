using System;
using System.Linq;
using Xunit;
using FluentAssertions;
using static TwinCatQA.Infrastructure.Tests.Parsers.ParserTestHelper;

namespace TwinCatQA.Infrastructure.Tests.Parsers
{
    /// <summary>
    /// 표현식 파싱 테스트
    /// </summary>
    public class ExpressionParsingTests
    {
        #region 산술 연산자 테스트

        [Theory]
        [InlineData("a + b", "+")]
        [InlineData("a - b", "-")]
        [InlineData("a * b", "*")]
        [InlineData("a / b", "/")]
        public void Parse_이진산술연산자_성공(string exprCode, string expectedOp)
        {
            // Arrange & Act
            var expr = ParseExpression(exprCode);

            // Assert
            expr.Should().NotBeNull();
            expr.ChildCount.Should().BeGreaterThan(1);

            var op = GetBinaryOperator(expr);
            op.Should().Be(expectedOp);
        }

        [Fact]
        public void Parse_MOD연산자_성공()
        {
            // Arrange
            var exprCode = "value MOD 10";

            // Act
            var expr = ParseExpression(exprCode);

            // Assert
            var op = GetBinaryOperator(expr);
            op.Should().Be("MOD");
        }

        [Fact]
        public void Parse_단항마이너스_성공()
        {
            // Arrange
            var exprCode = "-value";

            // Act
            var expr = ParseExpression(exprCode);

            // Assert
            expr.Should().NotBeNull();
        }

        [Fact]
        public void Parse_괄호표현식_성공()
        {
            // Arrange
            var exprCode = "(a + b) * c";

            // Act
            var expr = ParseExpression(exprCode);

            // Assert
            expr.Should().NotBeNull();
        }

        #endregion

        #region 비교 연산자 테스트

        [Theory]
        [InlineData("a = b", "=")]
        [InlineData("a <> b", "<>")]
        [InlineData("a < b", "<")]
        [InlineData("a <= b", "<=")]
        [InlineData("a > b", ">")]
        [InlineData("a >= b", ">=")]
        public void Parse_비교연산자_성공(string exprCode, string expectedOp)
        {
            // Arrange & Act
            var expr = ParseExpression(exprCode);

            // Assert
            expr.Should().NotBeNull();
            var op = GetBinaryOperator(expr);
            op.Should().Be(expectedOp);
        }

        #endregion

        #region 논리 연산자 테스트

        [Theory]
        [InlineData("a AND b", "AND")]
        [InlineData("a OR b", "OR")]
        [InlineData("a XOR b", "XOR")]
        public void Parse_논리연산자_성공(string exprCode, string expectedOp)
        {
            // Arrange & Act
            var expr = ParseExpression(exprCode);

            // Assert
            expr.Should().NotBeNull();
            var op = GetBinaryOperator(expr);
            op.Should().Be(expectedOp);
        }

        [Fact]
        public void Parse_NOT연산자_성공()
        {
            // Arrange
            var exprCode = "NOT flag";

            // Act
            var expr = ParseExpression(exprCode);

            // Assert
            expr.Should().NotBeNull();
        }

        #endregion

        #region 복잡한 표현식 테스트

        [Fact]
        public void Parse_연산자우선순위_곱셈먼저_성공()
        {
            // Arrange
            var exprCode = "a + b * c";

            // Act
            var expr = ParseExpression(exprCode);

            // Assert
            expr.Should().NotBeNull();
            // b * c가 먼저 계산되어야 함 (우선순위)
            GetBinaryOperator(expr).Should().Be("+");
        }

        [Fact]
        public void Parse_연산자우선순위_괄호우선_성공()
        {
            // Arrange
            var exprCode = "(a + b) * c";

            // Act
            var expr = ParseExpression(exprCode);

            // Assert
            expr.Should().NotBeNull();
            // 괄호가 먼저 계산되어야 함
            GetBinaryOperator(expr).Should().Be("*");
        }

        [Fact]
        public void Parse_복합논리표현식_성공()
        {
            // Arrange
            var exprCode = "(temp > 20.0) AND (pressure < 100.0) OR alarm";

            // Act
            var expr = ParseExpression(exprCode);

            // Assert
            expr.Should().NotBeNull();
        }

        [Fact]
        public void Parse_중첩괄호_성공()
        {
            // Arrange
            var exprCode = "((a + b) * (c - d)) / e";

            // Act
            var expr = ParseExpression(exprCode);

            // Assert
            expr.Should().NotBeNull();
            GetBinaryOperator(expr).Should().Be("/");
        }

        #endregion

        #region 리터럴 테스트

        [Fact]
        public void Parse_정수리터럴_성공()
        {
            // Arrange
            var exprCode = "42";

            // Act
            var expr = ParseExpression(exprCode);

            // Assert
            expr.Should().NotBeNull();
            var value = GetLiteralValue(expr);
            value.Should().Be(42);
        }

        [Fact]
        public void Parse_실수리터럴_성공()
        {
            // Arrange
            var exprCode = "3.14";

            // Act
            var expr = ParseExpression(exprCode);

            // Assert
            expr.Should().NotBeNull();
            var value = GetLiteralValue(expr);
            value.Should().Be(3.14);
        }

        [Fact]
        public void Parse_불리언리터럴_TRUE_성공()
        {
            // Arrange
            var exprCode = "TRUE";

            // Act
            var expr = ParseExpression(exprCode);

            // Assert
            expr.Should().NotBeNull();
            var value = GetLiteralValue(expr);
            value.Should().Be(true);
        }

        [Fact]
        public void Parse_불리언리터럴_FALSE_성공()
        {
            // Arrange
            var exprCode = "FALSE";

            // Act
            var expr = ParseExpression(exprCode);

            // Assert
            expr.Should().NotBeNull();
            var value = GetLiteralValue(expr);
            value.Should().Be(false);
        }

        [Fact]
        public void Parse_문자열리터럴_성공()
        {
            // Arrange
            var exprCode = "'Hello World'";

            // Act
            var expr = ParseExpression(exprCode);

            // Assert
            expr.Should().NotBeNull();
            var value = GetLiteralValue(expr);
            value.Should().Be("Hello World");
        }

        #endregion

        #region 변수 및 배열 접근 테스트

        [Fact]
        public void Parse_단순변수_성공()
        {
            // Arrange
            var exprCode = "counter";

            // Act
            var expr = ParseExpression(exprCode);

            // Assert
            expr.Should().NotBeNull();
            expr.variable().Should().NotBeNull();
        }

        [Fact]
        public void Parse_배열인덱스_성공()
        {
            // Arrange
            var exprCode = "array[5]";

            // Act
            var expr = ParseExpression(exprCode);

            // Assert
            expr.Should().NotBeNull();
        }

        [Fact]
        public void Parse_다차원배열_성공()
        {
            // Arrange
            var exprCode = "matrix[i, j]";

            // Act
            var expr = ParseExpression(exprCode);

            // Assert
            expr.Should().NotBeNull();
        }

        [Fact]
        public void Parse_구조체멤버접근_성공()
        {
            // Arrange
            var exprCode = "motor.speed";

            // Act
            var expr = ParseExpression(exprCode);

            // Assert
            expr.Should().NotBeNull();
        }

        [Fact]
        public void Parse_중첩구조체접근_성공()
        {
            // Arrange
            var exprCode = "machine.motor.speed";

            // Act
            var expr = ParseExpression(exprCode);

            // Assert
            expr.Should().NotBeNull();
        }

        #endregion

        #region 함수 호출 테스트

        [Fact]
        public void Parse_함수호출_인자없음_성공()
        {
            // Arrange
            var exprCode = "GetValue()";

            // Act
            var expr = ParseExpression(exprCode);

            // Assert
            expr.Should().NotBeNull();
            expr.functionCall().Should().NotBeNull();
        }

        [Fact]
        public void Parse_함수호출_단일인자_성공()
        {
            // Arrange
            var exprCode = "Calculate(value)";

            // Act
            var expr = ParseExpression(exprCode);

            // Assert
            expr.Should().NotBeNull();
            expr.functionCall().Should().NotBeNull();
        }

        [Fact]
        public void Parse_함수호출_복수인자_성공()
        {
            // Arrange
            var exprCode = "Add(a, b, c)";

            // Act
            var expr = ParseExpression(exprCode);

            // Assert
            expr.Should().NotBeNull();
            var funcCall = expr.functionCall();
            funcCall.Should().NotBeNull();
            funcCall!.argumentList().Should().NotBeNull();
        }

        [Fact]
        public void Parse_중첩함수호출_성공()
        {
            // Arrange
            var exprCode = "Calculate(GetValue(index))";

            // Act
            var expr = ParseExpression(exprCode);

            // Assert
            expr.Should().NotBeNull();
        }

        #endregion

        #region 엣지 케이스

        [Fact]
        public void Parse_매우긴표현식_성공()
        {
            // Arrange
            var exprCode = "a + b + c + d + e + f + g + h + i + j";

            // Act
            var expr = ParseExpression(exprCode);

            // Assert
            expr.Should().NotBeNull();
        }

        [Fact]
        public void Parse_복합표현식_모든연산자_성공()
        {
            // Arrange
            var exprCode = "(a + b * c) >= (d - e / f) AND NOT flag";

            // Act
            var expr = ParseExpression(exprCode);

            // Assert
            expr.Should().NotBeNull();
        }

        [Fact]
        public void Parse_음수리터럴_성공()
        {
            // Arrange
            var exprCode = "-42";

            // Act
            var expr = ParseExpression(exprCode);

            // Assert
            expr.Should().NotBeNull();
        }

        [Fact]
        public void Parse_과학표기법_성공()
        {
            // Arrange
            var exprCode = "1.5E-3";

            // Act
            var expr = ParseExpression(exprCode);

            // Assert
            expr.Should().NotBeNull();
        }

        #endregion
    }
}
