using System;
using System.Linq;
using Xunit;
using FluentAssertions;
using static TwinCatQA.Infrastructure.Tests.Parsers.ParserTestHelper;

namespace TwinCatQA.Infrastructure.Tests.Parsers
{
    /// <summary>
    /// ST 파서 기본 파싱 테스트
    /// </summary>
    public class STParserTests
    {
        [Fact]
        public void Parse_간단한프로그램_성공()
        {
            // Arrange
            var code = @"
PROGRAM Main
VAR
    counter : INT := 0;
END_VAR

counter := counter + 1;
END_PROGRAM
";

            // Act
            var tree = ParseCode(code);

            // Assert
            tree.Should().NotBeNull();
            var program = tree as StructuredTextParser.ProgramContext;
            program.Should().NotBeNull();
            program!.programUnit().Should().HaveCount(1);

            var programDecl = program.programUnit(0).programDeclaration();
            programDecl.Should().NotBeNull();
            programDecl!.IDENTIFIER().GetText().Should().Be("Main");
        }

        [Fact]
        public void Parse_FunctionBlock선언_성공()
        {
            // Arrange
            var code = @"
FUNCTION_BLOCK FB_Motor
VAR_INPUT
    enable : BOOL;
END_VAR
VAR_OUTPUT
    running : BOOL;
END_VAR
END_FUNCTION_BLOCK
";

            // Act
            var tree = ParseCode(code);

            // Assert
            var program = tree as StructuredTextParser.ProgramContext;
            program.Should().NotBeNull();

            var fbDecl = program!.programUnit(0).functionBlockDeclaration();
            fbDecl.Should().NotBeNull();
            fbDecl!.IDENTIFIER().GetText().Should().Be("FB_Motor");
        }

        [Fact]
        public void Parse_Function선언_성공()
        {
            // Arrange
            var code = @"
FUNCTION Add : INT
VAR_INPUT
    a : INT;
    b : INT;
END_VAR

Add := a + b;
END_FUNCTION
";

            // Act
            var tree = ParseCode(code);

            // Assert
            var program = tree as StructuredTextParser.ProgramContext;
            program.Should().NotBeNull();

            var funcDecl = program!.programUnit(0).functionDeclaration();
            funcDecl.Should().NotBeNull();
            funcDecl!.IDENTIFIER().GetText().Should().Be("Add");
        }

        [Fact]
        public void Parse_변수선언_VAR_성공()
        {
            // Arrange
            var code = @"
VAR
    counter : INT;
    temperature : REAL;
    isRunning : BOOL;
END_VAR
";

            // Act
            var varDecl = ParseVarDeclaration(code);

            // Assert
            varDecl.Should().NotBeNull();
            varDecl.VAR().Should().NotBeNull();
            varDecl.varDeclList().varDecl().Should().HaveCount(3);
        }

        [Fact]
        public void Parse_변수선언_VAR_INPUT_성공()
        {
            // Arrange
            var code = @"
VAR_INPUT
    enable : BOOL;
    setpoint : REAL;
END_VAR
";

            // Act
            var varDecl = ParseVarDeclaration(code);

            // Assert
            varDecl.Should().NotBeNull();
            varDecl.VAR_INPUT().Should().NotBeNull();
            varDecl.varDeclList().varDecl().Should().HaveCount(2);
        }

        [Fact]
        public void Parse_변수선언_VAR_OUTPUT_성공()
        {
            // Arrange
            var code = @"
VAR_OUTPUT
    status : INT;
    fault : BOOL;
END_VAR
";

            // Act
            var varDecl = ParseVarDeclaration(code);

            // Assert
            varDecl.Should().NotBeNull();
            varDecl.VAR_OUTPUT().Should().NotBeNull();
            varDecl.varDeclList().varDecl().Should().HaveCount(2);
        }

        [Fact]
        public void Parse_변수선언_VAR_IN_OUT_성공()
        {
            // Arrange
            var code = @"
VAR_IN_OUT
    buffer : ARRAY[0..99] OF INT;
END_VAR
";

            // Act
            var varDecl = ParseVarDeclaration(code);

            // Assert
            varDecl.Should().NotBeNull();
            varDecl.VAR_IN_OUT().Should().NotBeNull();
        }

        [Fact]
        public void Parse_변수초기값_성공()
        {
            // Arrange
            var code = @"
VAR
    counter : INT := 100;
    speed : REAL := 25.5;
    name : STRING := 'Test';
    enabled : BOOL := TRUE;
END_VAR
";

            // Act
            var varDecl = ParseVarDeclaration(code);

            // Assert
            varDecl.Should().NotBeNull();
            varDecl.varDeclList().varDecl().Should().HaveCount(4);
        }

        [Fact]
        public void Parse_할당문_성공()
        {
            // Arrange
            var code = "result := 100;";

            // Act
            var statement = ParseStatement(code);

            // Assert
            statement.Should().NotBeNull();
            var assignStmt = statement.assignmentStatement();
            assignStmt.Should().NotBeNull();
            assignStmt!.variable().IDENTIFIER()[0].GetText().Should().Be("result");
        }

        [Fact]
        public void Parse_복수프로그램단위_성공()
        {
            // Arrange
            var code = @"
FUNCTION_BLOCK FB_Test1
END_FUNCTION_BLOCK

FUNCTION_BLOCK FB_Test2
END_FUNCTION_BLOCK

PROGRAM Main
END_PROGRAM
";

            // Act
            var tree = ParseCode(code);

            // Assert
            var program = tree as StructuredTextParser.ProgramContext;
            program.Should().NotBeNull();
            program!.programUnit().Should().HaveCount(3);
        }

        [Fact]
        public void Parse_빈프로그램_성공()
        {
            // Arrange
            var code = @"
PROGRAM Empty
END_PROGRAM
";

            // Act
            var tree = ParseCode(code);

            // Assert
            tree.Should().NotBeNull();
            var program = tree as StructuredTextParser.ProgramContext;
            program.Should().NotBeNull();
        }

        [Fact]
        public void Parse_주석포함코드_성공()
        {
            // Arrange
            var code = @"
PROGRAM Test
VAR
    // 라인 주석
    counter : INT;
    (* 블록 주석 *)
    value : REAL;
END_VAR

// 할당문 주석
counter := 0;
END_PROGRAM
";

            // Act
            var tree = ParseCode(code);

            // Assert
            tree.Should().NotBeNull();
        }

        [Fact]
        public void Parse_세미콜론누락_실패()
        {
            // Arrange
            var code = @"
PROGRAM Test
VAR
    counter : INT
END_VAR
END_PROGRAM
";

            // Act & Assert
            HasParsingError(code).Should().BeTrue();
        }

        [Fact]
        public void Parse_잘못된키워드_실패()
        {
            // Arrange
            var code = @"
INVALID_KEYWORD Test
END_PROGRAM
";

            // Act & Assert
            HasParsingError(code).Should().BeTrue();
        }

        [Fact]
        public void Parse_END키워드누락_실패()
        {
            // Arrange
            var code = @"
PROGRAM Test
VAR
    counter : INT;
END_VAR
";

            // Act & Assert
            HasParsingError(code).Should().BeTrue();
        }
    }
}
