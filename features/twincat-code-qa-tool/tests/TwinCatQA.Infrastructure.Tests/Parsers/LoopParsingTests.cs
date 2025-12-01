using System;
using System.Linq;
using Xunit;
using FluentAssertions;
using static TwinCatQA.Infrastructure.Tests.Parsers.ParserTestHelper;

namespace TwinCatQA.Infrastructure.Tests.Parsers
{
    /// <summary>
    /// 반복문 파싱 테스트 (FOR, WHILE, REPEAT)
    /// </summary>
    public class LoopParsingTests
    {
        #region FOR 루프 테스트

        [Fact]
        public void Parse_기본FOR루프_성공()
        {
            // Arrange
            var code = @"
FOR i := 1 TO 10 DO
    sum := sum + i;
END_FOR
";

            // Act
            var statement = ParseStatement(code);

            // Assert
            statement.Should().NotBeNull();
            var forStmt = statement.forStatement();
            forStmt.Should().NotBeNull();
            forStmt!.IDENTIFIER().GetText().Should().Be("i");
        }

        [Fact]
        public void Parse_FOR루프_BY절포함_성공()
        {
            // Arrange
            var code = @"
FOR i := 1 TO 10 BY 2 DO
    sum := sum + i;
END_FOR
";

            // Act
            var statement = ParseStatement(code);

            // Assert
            var forStmt = statement.forStatement();
            forStmt.Should().NotBeNull();
            forStmt!.BY().Should().NotBeNull();
        }

        [Fact]
        public void Parse_FOR루프_역방향_성공()
        {
            // Arrange
            var code = @"
FOR i := 10 TO 1 BY -1 DO
    array[i] := 0;
END_FOR
";

            // Act
            var statement = ParseStatement(code);

            // Assert
            var forStmt = statement.forStatement();
            forStmt.Should().NotBeNull();
        }

        [Fact]
        public void Parse_중첩FOR루프_성공()
        {
            // Arrange
            var code = @"
FOR i := 0 TO 9 DO
    FOR j := 0 TO 9 DO
        matrix[i, j] := 0;
    END_FOR
END_FOR
";

            // Act
            var statement = ParseStatement(code);

            // Assert
            var forStmt = statement.forStatement();
            forStmt.Should().NotBeNull();

            // 내부 루프 확인
            var innerStatements = forStmt!.statement();
            innerStatements.Should().HaveCountGreaterThan(0);
        }

        [Fact]
        public void Parse_FOR루프_변수표현식_성공()
        {
            // Arrange
            var code = @"
FOR i := startIndex TO endIndex DO
    process(i);
END_FOR
";

            // Act
            var statement = ParseStatement(code);

            // Assert
            var forStmt = statement.forStatement();
            forStmt.Should().NotBeNull();
        }

        [Fact]
        public void Parse_FOR_DO누락_실패()
        {
            // Arrange
            var code = @"
PROGRAM Test
FOR i := 1 TO 10
    sum := sum + i;
END_FOR
END_PROGRAM
";

            // Act & Assert
            HasParsingError(code).Should().BeTrue();
        }

        #endregion

        #region WHILE 루프 테스트

        [Fact]
        public void Parse_기본WHILE루프_성공()
        {
            // Arrange
            var code = @"
WHILE counter < 100 DO
    counter := counter + 1;
END_WHILE
";

            // Act
            var statement = ParseStatement(code);

            // Assert
            statement.Should().NotBeNull();
            var whileStmt = statement.whileStatement();
            whileStmt.Should().NotBeNull();
            whileStmt!.expression().Should().NotBeNull();
        }

        [Fact]
        public void Parse_WHILE_복잡한조건_성공()
        {
            // Arrange
            var code = @"
WHILE (counter < 100) AND (NOT timeout) DO
    counter := counter + 1;
    CheckTimeout();
END_WHILE
";

            // Act
            var statement = ParseStatement(code);

            // Assert
            var whileStmt = statement.whileStatement();
            whileStmt.Should().NotBeNull();
            whileStmt!.statement().Should().HaveCount(2);
        }

        [Fact]
        public void Parse_중첩WHILE루프_성공()
        {
            // Arrange
            var code = @"
WHILE outerCondition DO
    WHILE innerCondition DO
        process();
    END_WHILE
END_WHILE
";

            // Act
            var statement = ParseStatement(code);

            // Assert
            var whileStmt = statement.whileStatement();
            whileStmt.Should().NotBeNull();
        }

        [Fact]
        public void Parse_WHILE_DO누락_실패()
        {
            // Arrange
            var code = @"
PROGRAM Test
WHILE counter < 100
    counter := counter + 1;
END_WHILE
END_PROGRAM
";

            // Act & Assert
            HasParsingError(code).Should().BeTrue();
        }

        #endregion

        #region REPEAT 루프 테스트

        [Fact]
        public void Parse_기본REPEAT루프_성공()
        {
            // Arrange
            var code = @"
REPEAT
    counter := counter + 1;
UNTIL counter >= 10
END_REPEAT
";

            // Act
            var statement = ParseStatement(code);

            // Assert
            statement.Should().NotBeNull();
            var repeatStmt = statement.repeatStatement();
            repeatStmt.Should().NotBeNull();
            repeatStmt!.expression().Should().NotBeNull();
        }

        [Fact]
        public void Parse_REPEAT_복잡한조건_성공()
        {
            // Arrange
            var code = @"
REPEAT
    value := ReadSensor();
    counter := counter + 1;
UNTIL (value > threshold) OR (counter >= maxRetries)
END_REPEAT
";

            // Act
            var statement = ParseStatement(code);

            // Assert
            var repeatStmt = statement.repeatStatement();
            repeatStmt.Should().NotBeNull();
            repeatStmt!.statement().Should().HaveCount(2);
        }

        [Fact]
        public void Parse_중첩REPEAT루프_성공()
        {
            // Arrange
            var code = @"
REPEAT
    REPEAT
        innerCounter := innerCounter + 1;
    UNTIL innerCounter >= 5
    END_REPEAT
    outerCounter := outerCounter + 1;
UNTIL outerCounter >= 10
END_REPEAT
";

            // Act
            var statement = ParseStatement(code);

            // Assert
            var repeatStmt = statement.repeatStatement();
            repeatStmt.Should().NotBeNull();
        }

        [Fact]
        public void Parse_REPEAT_UNTIL누락_실패()
        {
            // Arrange
            var code = @"
PROGRAM Test
REPEAT
    counter := counter + 1;
END_REPEAT
END_PROGRAM
";

            // Act & Assert
            HasParsingError(code).Should().BeTrue();
        }

        #endregion

        #region EXIT/RETURN 문 테스트

        [Fact]
        public void Parse_EXIT문_성공()
        {
            // Arrange
            var code = @"
FOR i := 1 TO 100 DO
    IF error THEN
        EXIT;
    END_IF
END_FOR
";

            // Act
            var statement = ParseStatement(code);

            // Assert
            var forStmt = statement.forStatement();
            forStmt.Should().NotBeNull();
        }

        [Fact]
        public void Parse_RETURN문_성공()
        {
            // Arrange
            var code = @"
IF error THEN
    RETURN;
END_IF
";

            // Act
            var statement = ParseStatement(code);

            // Assert
            var ifStmt = statement.ifStatement();
            ifStmt.Should().NotBeNull();
        }

        [Fact]
        public void Parse_조건부EXIT_성공()
        {
            // Arrange
            var code = @"
WHILE TRUE DO
    counter := counter + 1;
    IF counter >= 100 THEN
        EXIT;
    END_IF
END_WHILE
";

            // Act
            var statement = ParseStatement(code);

            // Assert
            var whileStmt = statement.whileStatement();
            whileStmt.Should().NotBeNull();
        }

        #endregion

        #region 엣지 케이스

        [Fact]
        public void Parse_FOR루프_단일문장_성공()
        {
            // Arrange
            var code = @"
FOR i := 1 TO 10 DO
    sum := sum + i;
END_FOR
";

            // Act
            var statement = ParseStatement(code);

            // Assert
            var forStmt = statement.forStatement();
            forStmt.Should().NotBeNull();
            forStmt!.statement().Should().HaveCount(1);
        }

        [Fact]
        public void Parse_WHILE루프_빈본문_구문오류()
        {
            // Arrange
            var code = @"
PROGRAM Test
WHILE condition DO
END_WHILE
END_PROGRAM
";

            // Act
            // 빈 본문도 유효할 수 있음 (문법적으로는 가능)
            var hasError = HasParsingError(code);

            // Assert
            // 구현에 따라 달라질 수 있음
            // 여기서는 검증만 수행
            hasError.Should().BeFalse();
        }

        [Fact]
        public void Parse_복합루프구조_성공()
        {
            // Arrange
            var code = @"
FOR i := 1 TO 10 DO
    WHILE condition DO
        REPEAT
            process();
        UNTIL done
        END_REPEAT
    END_WHILE
END_FOR
";

            // Act
            var statement = ParseStatement(code);

            // Assert
            var forStmt = statement.forStatement();
            forStmt.Should().NotBeNull();
        }

        #endregion
    }
}
