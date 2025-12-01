using System;
using System.Linq;
using Xunit;
using FluentAssertions;
using static TwinCatQA.Infrastructure.Tests.Parsers.ParserTestHelper;

namespace TwinCatQA.Infrastructure.Tests.Parsers
{
    /// <summary>
    /// IF 문 파싱 테스트
    /// </summary>
    public class IfStatementParsingTests
    {
        [Fact]
        public void Parse_단순IF_THEN_END_IF_성공()
        {
            // Arrange
            var code = @"
IF temperature > 25.0 THEN
    heaterOn := FALSE;
END_IF
";

            // Act
            var statement = ParseStatement(code);

            // Assert
            statement.Should().NotBeNull();
            var ifStmt = statement.ifStatement();
            ifStmt.Should().NotBeNull();
            ifStmt!.expression().Should().NotBeNull();
            ifStmt.statement().Should().HaveCountGreaterThan(0);
        }

        [Fact]
        public void Parse_IF_ELSE_성공()
        {
            // Arrange
            var code = @"
IF enable THEN
    motor := TRUE;
ELSE
    motor := FALSE;
END_IF
";

            // Act
            var statement = ParseStatement(code);

            // Assert
            var ifStmt = statement.ifStatement();
            ifStmt.Should().NotBeNull();
            ifStmt!.ELSE().Should().NotBeNull();
        }

        [Fact]
        public void Parse_IF_ELSIF_ELSE_성공()
        {
            // Arrange
            var code = @"
IF value < 0 THEN
    result := -1;
ELSIF value = 0 THEN
    result := 0;
ELSE
    result := 1;
END_IF
";

            // Act
            var statement = ParseStatement(code);

            // Assert
            var ifStmt = statement.ifStatement();
            ifStmt.Should().NotBeNull();
            ifStmt!.ELSIF().Should().HaveCount(1);
            ifStmt.ELSE().Should().NotBeNull();
        }

        [Fact]
        public void Parse_다중ELSIF_성공()
        {
            // Arrange
            var code = @"
IF speed < 10 THEN
    status := 1;
ELSIF speed < 50 THEN
    status := 2;
ELSIF speed < 100 THEN
    status := 3;
ELSIF speed < 200 THEN
    status := 4;
ELSE
    status := 5;
END_IF
";

            // Act
            var statement = ParseStatement(code);

            // Assert
            var ifStmt = statement.ifStatement();
            ifStmt.Should().NotBeNull();
            ifStmt!.ELSIF().Should().HaveCount(4);
        }

        [Fact]
        public void Parse_중첩IF문_성공()
        {
            // Arrange
            var code = @"
IF enable THEN
    IF temperature > 30 THEN
        heater := FALSE;
    ELSE
        heater := TRUE;
    END_IF
END_IF
";

            // Act
            var statement = ParseStatement(code);

            // Assert
            var ifStmt = statement.ifStatement();
            ifStmt.Should().NotBeNull();

            // 내부에 중첩된 IF문이 있어야 함
            var innerStatements = ifStmt!.statement();
            innerStatements.Should().HaveCountGreaterThan(0);
        }

        [Fact]
        public void Parse_복잡한조건식_성공()
        {
            // Arrange
            var code = @"
IF (temp > 20.0) AND (pressure < 100.0) OR alarm THEN
    shutdown := TRUE;
END_IF
";

            // Act
            var statement = ParseStatement(code);

            // Assert
            var ifStmt = statement.ifStatement();
            ifStmt.Should().NotBeNull();
            ifStmt!.expression().Should().NotBeNull();
        }

        [Fact]
        public void Parse_IF문에여러문장_성공()
        {
            // Arrange
            var code = @"
IF start THEN
    motor := TRUE;
    counter := 0;
    timer := T#0s;
    status := 1;
END_IF
";

            // Act
            var statement = ParseStatement(code);

            // Assert
            var ifStmt = statement.ifStatement();
            ifStmt.Should().NotBeNull();
            ifStmt!.statement().Should().HaveCount(4);
        }

        [Fact]
        public void Parse_THEN키워드누락_실패()
        {
            // Arrange
            var code = @"
PROGRAM Test
IF enable
    motor := TRUE;
END_IF
END_PROGRAM
";

            // Act & Assert
            HasParsingError(code).Should().BeTrue();
        }

        [Fact]
        public void Parse_END_IF누락_실패()
        {
            // Arrange
            var code = @"
PROGRAM Test
IF enable THEN
    motor := TRUE;
END_PROGRAM
";

            // Act & Assert
            HasParsingError(code).Should().BeTrue();
        }

        [Fact]
        public void Parse_조건식없음_실패()
        {
            // Arrange
            var code = @"
PROGRAM Test
IF THEN
    motor := TRUE;
END_IF
END_PROGRAM
";

            // Act & Assert
            HasParsingError(code).Should().BeTrue();
        }

        [Fact]
        public void Parse_ELSIF조건누락_실패()
        {
            // Arrange
            var code = @"
PROGRAM Test
IF value < 0 THEN
    result := -1;
ELSIF THEN
    result := 0;
END_IF
END_PROGRAM
";

            // Act & Assert
            HasParsingError(code).Should().BeTrue();
        }
    }
}
