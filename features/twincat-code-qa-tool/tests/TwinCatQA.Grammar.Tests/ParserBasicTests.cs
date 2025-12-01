using Antlr4.Runtime;
using Xunit;

namespace TwinCatQA.Grammar.Tests;

/// <summary>
/// STParser의 기본 파싱 기능을 테스트합니다.
/// </summary>
public class ParserBasicTests
{
    /// <summary>
    /// 간단한 PROGRAM 선언이 올바르게 파싱되는지 테스트
    /// </summary>
    [Fact]
    public void ParseSimpleProgram_성공()
    {
        // Arrange: 간단한 TwinCAT ST 코드
        var code = @"
PROGRAM Main
VAR
    counter : INT;
END_VAR

counter := counter + 1;

END_PROGRAM
";

        // Act: Lexer와 Parser 실행
        var inputStream = new AntlrInputStream(code);
        var lexer = new STLexer(inputStream);
        var tokenStream = new CommonTokenStream(lexer);
        var parser = new STParser(tokenStream);

        // Parse 시작
        var context = parser.compilationUnit();

        // Assert: 파싱 오류가 없어야 함
        Assert.NotNull(context);
        Assert.Equal(0, parser.NumberOfSyntaxErrors);
    }

    /// <summary>
    /// FUNCTION 선언이 올바르게 파싱되는지 테스트
    /// </summary>
    [Fact]
    public void ParseFunction_성공()
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
        var inputStream = new AntlrInputStream(code);
        var lexer = new STLexer(inputStream);
        var tokenStream = new CommonTokenStream(lexer);
        var parser = new STParser(tokenStream);

        var context = parser.compilationUnit();

        // Assert
        Assert.NotNull(context);
        Assert.Equal(0, parser.NumberOfSyntaxErrors);
        Assert.Single(context.declaration()); // 하나의 선언 (FUNCTION)
    }

    /// <summary>
    /// IF-THEN-ELSE 제어문이 올바르게 파싱되는지 테스트
    /// </summary>
    [Fact]
    public void ParseIfStatement_성공()
    {
        // Arrange
        var code = @"
PROGRAM Test
VAR
    value : INT;
    result : BOOL;
END_VAR

IF value > 10 THEN
    result := TRUE;
ELSIF value > 5 THEN
    result := FALSE;
ELSE
    result := FALSE;
END_IF;

END_PROGRAM
";

        // Act
        var inputStream = new AntlrInputStream(code);
        var lexer = new STLexer(inputStream);
        var tokenStream = new CommonTokenStream(lexer);
        var parser = new STParser(tokenStream);

        var context = parser.compilationUnit();

        // Assert
        Assert.NotNull(context);
        Assert.Equal(0, parser.NumberOfSyntaxErrors);
    }

    /// <summary>
    /// FOR 루프가 올바르게 파싱되는지 테스트
    /// </summary>
    [Fact]
    public void ParseForLoop_성공()
    {
        // Arrange
        var code = @"
PROGRAM LoopTest
VAR
    i : INT;
    sum : INT;
END_VAR

sum := 0;

FOR i := 1 TO 10 BY 1 DO
    sum := sum + i;
END_FOR;

END_PROGRAM
";

        // Act
        var inputStream = new AntlrInputStream(code);
        var lexer = new STLexer(inputStream);
        var tokenStream = new CommonTokenStream(lexer);
        var parser = new STParser(tokenStream);

        var context = parser.compilationUnit();

        // Assert
        Assert.NotNull(context);
        Assert.Equal(0, parser.NumberOfSyntaxErrors);
    }

    /// <summary>
    /// FUNCTION_BLOCK 선언이 올바르게 파싱되는지 테스트
    /// </summary>
    [Fact]
    public void ParseFunctionBlock_성공()
    {
        // Arrange
        var code = @"
FUNCTION_BLOCK FB_Counter
VAR_INPUT
    reset : BOOL;
END_VAR
VAR_OUTPUT
    count : INT;
END_VAR

IF reset THEN
    count := 0;
ELSE
    count := count + 1;
END_IF;

END_FUNCTION_BLOCK
";

        // Act
        var inputStream = new AntlrInputStream(code);
        var lexer = new STLexer(inputStream);
        var tokenStream = new CommonTokenStream(lexer);
        var parser = new STParser(tokenStream);

        var context = parser.compilationUnit();

        // Assert
        Assert.NotNull(context);
        Assert.Equal(0, parser.NumberOfSyntaxErrors);
    }

    /// <summary>
    /// TYPE 선언 (구조체)이 올바르게 파싱되는지 테스트
    /// </summary>
    [Fact]
    public void ParseStructType_성공()
    {
        // Arrange
        var code = @"
TYPE ST_Motor : STRUCT
    speed : REAL;
    position : LREAL;
    isRunning : BOOL;
END_STRUCT
END_TYPE
";

        // Act
        var inputStream = new AntlrInputStream(code);
        var lexer = new STLexer(inputStream);
        var tokenStream = new CommonTokenStream(lexer);
        var parser = new STParser(tokenStream);

        var context = parser.compilationUnit();

        // Assert
        Assert.NotNull(context);
        Assert.Equal(0, parser.NumberOfSyntaxErrors);
    }

    /// <summary>
    /// 배열 선언이 올바르게 파싱되는지 테스트
    /// </summary>
    [Fact]
    public void ParseArrayDeclaration_성공()
    {
        // Arrange
        var code = @"
PROGRAM ArrayTest
VAR
    values : ARRAY[0..9] OF INT;
    matrix : ARRAY[0..2, 0..3] OF REAL;
END_VAR

values[0] := 100;
matrix[1, 2] := 3.14;

END_PROGRAM
";

        // Act
        var inputStream = new AntlrInputStream(code);
        var lexer = new STLexer(inputStream);
        var tokenStream = new CommonTokenStream(lexer);
        var parser = new STParser(tokenStream);

        var context = parser.compilationUnit();

        // Assert
        Assert.NotNull(context);
        Assert.Equal(0, parser.NumberOfSyntaxErrors);
    }
}
