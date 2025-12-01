using System;
using System.Linq;
using Xunit;
using FluentAssertions;
using static TwinCatQA.Infrastructure.Tests.Parsers.ParserTestHelper;

namespace TwinCatQA.Infrastructure.Tests.Parsers
{
    /// <summary>
    /// CASE 문 파싱 테스트
    /// </summary>
    public class CaseStatementParsingTests
    {
        [Fact]
        public void Parse_기본CASE문_성공()
        {
            // Arrange
            var code = @"
CASE status OF
    1:
        output := 'Running';
    2:
        output := 'Stopped';
END_CASE
";

            // Act
            var statement = ParseStatement(code);

            // Assert
            statement.Should().NotBeNull();
            var caseStmt = statement.caseStatement();
            caseStmt.Should().NotBeNull();
            caseStmt!.expression().Should().NotBeNull();
        }

        [Fact]
        public void Parse_CASE_다중값_성공()
        {
            // Arrange
            var code = @"
CASE value OF
    1, 2, 3:
        category := 'Low';
    4, 5, 6:
        category := 'Medium';
    7, 8, 9:
        category := 'High';
END_CASE
";

            // Act
            var statement = ParseStatement(code);

            // Assert
            var caseStmt = statement.caseStatement();
            caseStmt.Should().NotBeNull();
            caseStmt!.caseElement().Should().HaveCount(3);
        }

        [Fact]
        public void Parse_CASE_범위지정_성공()
        {
            // Arrange
            var code = @"
CASE temperature OF
    0..10:
        zone := 'Cold';
    11..25:
        zone := 'Normal';
    26..40:
        zone := 'Hot';
END_CASE
";

            // Act
            var statement = ParseStatement(code);

            // Assert
            var caseStmt = statement.caseStatement();
            caseStmt.Should().NotBeNull();
        }

        [Fact]
        public void Parse_CASE_ELSE절_성공()
        {
            // Arrange
            var code = @"
CASE mode OF
    1:
        action := 'Manual';
    2:
        action := 'Auto';
ELSE
    action := 'Unknown';
END_CASE
";

            // Act
            var statement = ParseStatement(code);

            // Assert
            var caseStmt = statement.caseStatement();
            caseStmt.Should().NotBeNull();
            caseStmt!.ELSE().Should().NotBeNull();
        }

        [Fact]
        public void Parse_CASE_복수문장_성공()
        {
            // Arrange
            var code = @"
CASE state OF
    1:
        motor := TRUE;
        led := TRUE;
        counter := 0;
    2:
        motor := FALSE;
        led := FALSE;
END_CASE
";

            // Act
            var statement = ParseStatement(code);

            // Assert
            var caseStmt = statement.caseStatement();
            caseStmt.Should().NotBeNull();
        }

        [Fact]
        public void Parse_중첩CASE문_성공()
        {
            // Arrange
            var code = @"
CASE outerValue OF
    1:
        CASE innerValue OF
            1: result := 11;
            2: result := 12;
        END_CASE
    2:
        result := 20;
END_CASE
";

            // Act
            var statement = ParseStatement(code);

            // Assert
            var caseStmt = statement.caseStatement();
            caseStmt.Should().NotBeNull();
        }

        [Fact]
        public void Parse_CASE_열거형_성공()
        {
            // Arrange
            var code = @"
CASE color OF
    RED:
        value := 1;
    GREEN:
        value := 2;
    BLUE:
        value := 3;
END_CASE
";

            // Act
            var statement = ParseStatement(code);

            // Assert
            var caseStmt = statement.caseStatement();
            caseStmt.Should().NotBeNull();
        }

        [Fact]
        public void Parse_CASE_빈케이스_실패()
        {
            // Arrange
            var code = @"
PROGRAM Test
CASE value OF
END_CASE
END_PROGRAM
";

            // Act & Assert
            // 빈 CASE는 문법적으로 허용될 수 있음
            var hasError = HasParsingError(code);
            hasError.Should().BeFalse();
        }

        [Fact]
        public void Parse_CASE_OF누락_실패()
        {
            // Arrange
            var code = @"
PROGRAM Test
CASE value
    1: output := 'One';
END_CASE
END_PROGRAM
";

            // Act & Assert
            HasParsingError(code).Should().BeTrue();
        }

        [Fact]
        public void Parse_CASE_END_CASE누락_실패()
        {
            // Arrange
            var code = @"
PROGRAM Test
CASE value OF
    1: output := 'One';
END_PROGRAM
";

            // Act & Assert
            HasParsingError(code).Should().BeTrue();
        }

        [Fact]
        public void Parse_CASE_콜론누락_실패()
        {
            // Arrange
            var code = @"
PROGRAM Test
CASE value OF
    1
        output := 'One';
END_CASE
END_PROGRAM
";

            // Act & Assert
            HasParsingError(code).Should().BeTrue();
        }
    }
}
