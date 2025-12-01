using System;
using System.Linq;
using Xunit;
using FluentAssertions;
using static TwinCatQA.Infrastructure.Tests.Parsers.ParserTestHelper;

namespace TwinCatQA.Infrastructure.Tests.Parsers
{
    /// <summary>
    /// 데이터 타입 파싱 테스트
    /// </summary>
    public class DataTypeParsingTests
    {
        #region 기본 데이터 타입 테스트

        [Theory]
        [InlineData("BOOL")]
        [InlineData("BYTE")]
        [InlineData("WORD")]
        [InlineData("DWORD")]
        [InlineData("LWORD")]
        public void Parse_비트타입_성공(string typeName)
        {
            // Arrange
            var code = $@"
VAR
    value : {typeName};
END_VAR
";

            // Act
            var varDecl = ParseVarDeclaration(code);

            // Assert
            varDecl.Should().NotBeNull();
        }

        [Theory]
        [InlineData("SINT")]
        [InlineData("USINT")]
        [InlineData("INT")]
        [InlineData("UINT")]
        [InlineData("DINT")]
        [InlineData("UDINT")]
        [InlineData("LINT")]
        [InlineData("ULINT")]
        public void Parse_정수타입_성공(string typeName)
        {
            // Arrange
            var code = $@"
VAR
    value : {typeName};
END_VAR
";

            // Act
            var varDecl = ParseVarDeclaration(code);

            // Assert
            varDecl.Should().NotBeNull();
        }

        [Theory]
        [InlineData("REAL")]
        [InlineData("LREAL")]
        public void Parse_실수타입_성공(string typeName)
        {
            // Arrange
            var code = $@"
VAR
    value : {typeName};
END_VAR
";

            // Act
            var varDecl = ParseVarDeclaration(code);

            // Assert
            varDecl.Should().NotBeNull();
        }

        [Theory]
        [InlineData("STRING")]
        [InlineData("WSTRING")]
        public void Parse_문자열타입_성공(string typeName)
        {
            // Arrange
            var code = $@"
VAR
    text : {typeName};
END_VAR
";

            // Act
            var varDecl = ParseVarDeclaration(code);

            // Assert
            varDecl.Should().NotBeNull();
        }

        [Theory]
        [InlineData("TIME")]
        [InlineData("DATE")]
        [InlineData("TIME_OF_DAY")]
        [InlineData("TOD")]
        [InlineData("DATE_AND_TIME")]
        [InlineData("DT")]
        public void Parse_시간타입_성공(string typeName)
        {
            // Arrange
            var code = $@"
VAR
    timestamp : {typeName};
END_VAR
";

            // Act
            var varDecl = ParseVarDeclaration(code);

            // Assert
            varDecl.Should().NotBeNull();
        }

        #endregion

        #region 배열 타입 테스트

        [Fact]
        public void Parse_1차원배열_성공()
        {
            // Arrange
            var code = @"
VAR
    values : ARRAY[0..9] OF INT;
END_VAR
";

            // Act
            var varDecl = ParseVarDeclaration(code);

            // Assert
            varDecl.Should().NotBeNull();
        }

        [Fact]
        public void Parse_2차원배열_성공()
        {
            // Arrange
            var code = @"
VAR
    matrix : ARRAY[0..9, 0..9] OF REAL;
END_VAR
";

            // Act
            var varDecl = ParseVarDeclaration(code);

            // Assert
            varDecl.Should().NotBeNull();
        }

        [Fact]
        public void Parse_3차원배열_성공()
        {
            // Arrange
            var code = @"
VAR
    cube : ARRAY[0..4, 0..4, 0..4] OF INT;
END_VAR
";

            // Act
            var varDecl = ParseVarDeclaration(code);

            // Assert
            varDecl.Should().NotBeNull();
        }

        [Fact]
        public void Parse_배열_음수인덱스_성공()
        {
            // Arrange
            var code = @"
VAR
    values : ARRAY[-10..10] OF INT;
END_VAR
";

            // Act
            var varDecl = ParseVarDeclaration(code);

            // Assert
            varDecl.Should().NotBeNull();
        }

        [Fact]
        public void Parse_배열_큰범위_성공()
        {
            // Arrange
            var code = @"
VAR
    buffer : ARRAY[0..999] OF BYTE;
END_VAR
";

            // Act
            var varDecl = ParseVarDeclaration(code);

            // Assert
            varDecl.Should().NotBeNull();
        }

        #endregion

        #region 구조체 타입 테스트

        [Fact]
        public void Parse_STRUCT선언_성공()
        {
            // Arrange
            var code = @"
TYPE T_Point :
STRUCT
    x : REAL;
    y : REAL;
END_STRUCT
END_TYPE
";

            // Act
            var tree = ParseCode(code);

            // Assert
            tree.Should().NotBeNull();
            var program = tree as StructuredTextParser.ProgramContext;
            program.Should().NotBeNull();
        }

        [Fact]
        public void Parse_중첩STRUCT_성공()
        {
            // Arrange
            var code = @"
TYPE T_Rectangle :
STRUCT
    topLeft : T_Point;
    bottomRight : T_Point;
    width : REAL;
    height : REAL;
END_STRUCT
END_TYPE
";

            // Act
            var tree = ParseCode(code);

            // Assert
            tree.Should().NotBeNull();
        }

        [Fact]
        public void Parse_STRUCT변수선언_성공()
        {
            // Arrange
            var code = @"
VAR
    point : T_Point;
    rect : T_Rectangle;
END_VAR
";

            // Act
            var varDecl = ParseVarDeclaration(code);

            // Assert
            varDecl.Should().NotBeNull();
        }

        #endregion

        #region 포인터 및 참조 타입 테스트

        [Fact]
        public void Parse_POINTER타입_성공()
        {
            // Arrange
            var code = @"
VAR
    pValue : POINTER TO INT;
END_VAR
";

            // Act
            var varDecl = ParseVarDeclaration(code);

            // Assert
            varDecl.Should().NotBeNull();
        }

        [Fact]
        public void Parse_REFERENCE타입_성공()
        {
            // Arrange
            var code = @"
VAR_IN_OUT
    refValue : REFERENCE TO REAL;
END_VAR
";

            // Act
            var varDecl = ParseVarDeclaration(code);

            // Assert
            varDecl.Should().NotBeNull();
        }

        [Fact]
        public void Parse_POINTER_TO_ARRAY_성공()
        {
            // Arrange
            var code = @"
VAR
    pArray : POINTER TO ARRAY[0..9] OF INT;
END_VAR
";

            // Act
            var varDecl = ParseVarDeclaration(code);

            // Assert
            varDecl.Should().NotBeNull();
        }

        #endregion

        #region 사용자 정의 타입 테스트

        [Fact]
        public void Parse_TYPE_ENUM_성공()
        {
            // Arrange
            var code = @"
TYPE E_Color :
(
    RED := 0,
    GREEN := 1,
    BLUE := 2
);
END_TYPE
";

            // Act
            var tree = ParseCode(code);

            // Assert
            tree.Should().NotBeNull();
        }

        [Fact]
        public void Parse_TYPE_별칭_성공()
        {
            // Arrange
            var code = @"
TYPE T_Speed : REAL;
END_TYPE
";

            // Act
            var tree = ParseCode(code);

            // Assert
            tree.Should().NotBeNull();
        }

        #endregion

        #region STRING 길이 지정 테스트

        [Fact]
        public void Parse_STRING_길이지정_성공()
        {
            // Arrange
            var code = @"
VAR
    name : STRING[80];
END_VAR
";

            // Act
            var varDecl = ParseVarDeclaration(code);

            // Assert
            varDecl.Should().NotBeNull();
        }

        [Fact]
        public void Parse_WSTRING_길이지정_성공()
        {
            // Arrange
            var code = @"
VAR
    wideText : WSTRING[100];
END_VAR
";

            // Act
            var varDecl = ParseVarDeclaration(code);

            // Assert
            varDecl.Should().NotBeNull();
        }

        #endregion

        #region 복합 타입 테스트

        [Fact]
        public void Parse_구조체배열_성공()
        {
            // Arrange
            var code = @"
VAR
    points : ARRAY[0..9] OF T_Point;
END_VAR
";

            // Act
            var varDecl = ParseVarDeclaration(code);

            // Assert
            varDecl.Should().NotBeNull();
        }

        [Fact]
        public void Parse_포인터배열_성공()
        {
            // Arrange
            var code = @"
VAR
    pointers : ARRAY[0..4] OF POINTER TO INT;
END_VAR
";

            // Act
            var varDecl = ParseVarDeclaration(code);

            // Assert
            varDecl.Should().NotBeNull();
        }

        #endregion

        #region 에러 케이스

        [Fact]
        public void Parse_존재하지않는타입_파싱성공_의미분석실패예상()
        {
            // Arrange
            var code = @"
VAR
    value : INVALID_TYPE;
END_VAR
";

            // Act
            // 파싱은 성공하지만 의미 분석에서 실패해야 함
            var varDecl = ParseVarDeclaration(code);

            // Assert
            varDecl.Should().NotBeNull();
        }

        [Fact]
        public void Parse_배열_범위누락_실패()
        {
            // Arrange
            var code = @"
PROGRAM Test
VAR
    values : ARRAY OF INT;
END_VAR
END_PROGRAM
";

            // Act & Assert
            HasParsingError(code).Should().BeTrue();
        }

        [Fact]
        public void Parse_POINTER_TO누락_실패()
        {
            // Arrange
            var code = @"
PROGRAM Test
VAR
    ptr : POINTER INT;
END_VAR
END_PROGRAM
";

            // Act & Assert
            HasParsingError(code).Should().BeTrue();
        }

        #endregion
    }
}
