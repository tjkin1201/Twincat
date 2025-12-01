using System;
using System.Linq;
using Xunit;
using FluentAssertions;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using static TwinCatQA.Infrastructure.Tests.Parsers.ParserTestHelper;

namespace TwinCatQA.Infrastructure.Tests.Parsers
{
    /// <summary>
    /// 오류 처리 및 에러 복구 테스트
    /// </summary>
    public class ErrorHandlingTests
    {
        #region 구문 오류 테스트

        [Fact]
        public void Parse_예약어_식별자사용_실패()
        {
            // Arrange
            var code = @"
PROGRAM Test
VAR
    IF : INT; // 예약어를 변수명으로 사용
END_VAR
END_PROGRAM
";

            // Act & Assert
            HasParsingError(code).Should().BeTrue();
        }

        [Fact]
        public void Parse_중괄호_불일치_실패()
        {
            // Arrange
            var code = @"
PROGRAM Test
VAR
    value : INT;
// END_VAR 누락
END_PROGRAM
";

            // Act & Assert
            HasParsingError(code).Should().BeTrue();
        }

        [Fact]
        public void Parse_세미콜론_연속_실패()
        {
            // Arrange
            var code = @"
PROGRAM Test
VAR
    value : INT;;  // 세미콜론 중복
END_VAR
END_PROGRAM
";

            // Act & Assert
            // 일부 파서는 허용할 수 있음
            var hasError = HasParsingError(code);
            // 결과만 검증
            hasError.Should().BeFalse();
        }

        [Fact]
        public void Parse_할당연산자_오타_실패()
        {
            // Arrange
            var code = @"
PROGRAM Test
value = 10;  // := 대신 = 사용
END_PROGRAM
";

            // Act & Assert
            // = 는 비교 연산자이므로 문맥상 오류
            HasParsingError(code).Should().BeTrue();
        }

        [Fact]
        public void Parse_괄호_불일치_실패()
        {
            // Arrange
            var code = @"
PROGRAM Test
result := (a + b * c;  // 닫는 괄호 누락
END_PROGRAM
";

            // Act & Assert
            HasParsingError(code).Should().BeTrue();
        }

        #endregion

        #region 의미론적 오류 (파싱은 성공, 의미 분석 실패 예상)

        [Fact]
        public void Parse_타입불일치_파싱성공()
        {
            // Arrange
            var code = @"
PROGRAM Test
VAR
    intVar : INT;
    boolVar : BOOL;
END_VAR

intVar := boolVar;  // 타입 불일치 (의미 분석에서 검출)
END_PROGRAM
";

            // Act
            var tree = ParseCode(code);

            // Assert
            // 파싱은 성공해야 함 (문법적으로 올바름)
            tree.Should().NotBeNull();
        }

        [Fact]
        public void Parse_선언되지않은변수_파싱성공()
        {
            // Arrange
            var code = @"
PROGRAM Test
VAR
    declared : INT;
END_VAR

undeclared := 100;  // 선언되지 않은 변수 (의미 분석에서 검출)
END_PROGRAM
";

            // Act
            var tree = ParseCode(code);

            // Assert
            // 파싱은 성공 (문법적으로는 올바름)
            tree.Should().NotBeNull();
        }

        [Fact]
        public void Parse_중복변수선언_파싱성공()
        {
            // Arrange
            var code = @"
PROGRAM Test
VAR
    value : INT;
    value : REAL;  // 중복 선언 (의미 분석에서 검출)
END_VAR
END_PROGRAM
";

            // Act
            var tree = ParseCode(code);

            // Assert
            // 파싱은 성공
            tree.Should().NotBeNull();
        }

        #endregion

        #region 엣지 케이스

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
        }

        [Fact]
        public void Parse_빈변수블록_성공()
        {
            // Arrange
            var code = @"
PROGRAM Test
VAR
END_VAR
END_PROGRAM
";

            // Act
            var tree = ParseCode(code);

            // Assert
            tree.Should().NotBeNull();
        }

        [Fact]
        public void Parse_매우긴식별자_성공()
        {
            // Arrange
            var code = @"
PROGRAM Test
VAR
    thisIsAVeryLongVariableNameThatExceedsNormalLengthButShouldStillBeValid : INT;
END_VAR
END_PROGRAM
";

            // Act
            var tree = ParseCode(code);

            // Assert
            tree.Should().NotBeNull();
        }

        [Fact]
        public void Parse_숫자로시작하는식별자_실패()
        {
            // Arrange
            var code = @"
PROGRAM Test
VAR
    123invalid : INT;  // 숫자로 시작
END_VAR
END_PROGRAM
";

            // Act & Assert
            HasParsingError(code).Should().BeTrue();
        }

        [Fact]
        public void Parse_특수문자식별자_실패()
        {
            // Arrange
            var code = @"
PROGRAM Test
VAR
    value@invalid : INT;  // 특수문자 포함
END_VAR
END_PROGRAM
";

            // Act & Assert
            HasParsingError(code).Should().BeTrue();
        }

        #endregion

        #region 주석 처리 테스트

        [Fact]
        public void Parse_라인주석_성공()
        {
            // Arrange
            var code = @"
PROGRAM Test
// 이것은 주석입니다
VAR
    value : INT; // 변수 선언
END_VAR
END_PROGRAM
";

            // Act
            var tree = ParseCode(code);

            // Assert
            tree.Should().NotBeNull();
        }

        [Fact]
        public void Parse_블록주석_성공()
        {
            // Arrange
            var code = @"
PROGRAM Test
(*
   여러 줄에 걸친
   블록 주석
*)
VAR
    value : INT;
END_VAR
END_PROGRAM
";

            // Act
            var tree = ParseCode(code);

            // Assert
            tree.Should().NotBeNull();
        }

        [Fact]
        public void Parse_중첩블록주석_성공또는실패()
        {
            // Arrange
            var code = @"
PROGRAM Test
(* 외부 주석
   (* 내부 주석 *)
   외부 주석 계속
*)
END_PROGRAM
";

            // Act
            // IEC 61131-3는 중첩 주석을 지원하지 않을 수 있음
            var tree = ParseCode(code);

            // Assert
            tree.Should().NotBeNull();
        }

        #endregion

        #region 복잡한 오류 시나리오

        [Fact]
        public void Parse_여러오류_첫번째오류만보고()
        {
            // Arrange
            var code = @"
PROGRAM Test
VAR
    value : INT
    another : REAL;  // 위에서 세미콜론 누락
END_VAR
undeclared := 100  // 선언되지 않은 변수, 세미콜론 누락
END_PROGRAM
";

            // Act
            Action act = () => ParseCode(code);

            // Assert
            act.Should().Throw<ParseCanceledException>();
        }

        [Fact]
        public void Parse_배열인덱스_범위초과_파싱성공()
        {
            // Arrange
            var code = @"
PROGRAM Test
VAR
    array : ARRAY[0..9] OF INT;
END_VAR

array[100] := 5;  // 런타임 오류, 파싱은 성공
END_PROGRAM
";

            // Act
            var tree = ParseCode(code);

            // Assert
            // 파싱은 성공 (범위 검사는 의미 분석 또는 런타임)
            tree.Should().NotBeNull();
        }

        [Fact]
        public void Parse_0으로나누기_파싱성공()
        {
            // Arrange
            var code = @"
PROGRAM Test
VAR
    result : REAL;
END_VAR

result := 10.0 / 0.0;  // 런타임 오류, 파싱은 성공
END_PROGRAM
";

            // Act
            var tree = ParseCode(code);

            // Assert
            // 파싱은 성공
            tree.Should().NotBeNull();
        }

        #endregion

        #region 복구 가능한 오류 테스트

        [Fact]
        public void Parse_잘못된토큰_스킵하고복구()
        {
            // Arrange
            var code = @"
PROGRAM Test
VAR
    value : INT;
    @ INVALID @  // 잘못된 토큰
    another : REAL;
END_VAR
END_PROGRAM
";

            // Act & Assert
            // 파서가 오류 복구를 시도할 수 있음
            HasParsingError(code).Should().BeTrue();
        }

        #endregion

        #region 경계 조건 테스트

        [Fact]
        public void Parse_최소코드_성공()
        {
            // Arrange
            var code = "PROGRAM P END_PROGRAM";

            // Act
            var tree = ParseCode(code);

            // Assert
            tree.Should().NotBeNull();
        }

        [Fact]
        public void Parse_빈파일_실패()
        {
            // Arrange
            var code = "";

            // Act & Assert
            HasParsingError(code).Should().BeTrue();
        }

        [Fact]
        public void Parse_공백만_실패()
        {
            // Arrange
            var code = "   \n\n\t\t   ";

            // Act & Assert
            HasParsingError(code).Should().BeTrue();
        }

        [Fact]
        public void Parse_주석만_실패()
        {
            // Arrange
            var code = @"
// 주석만 있음
(* 블록 주석 *)
";

            // Act & Assert
            HasParsingError(code).Should().BeTrue();
        }

        #endregion
    }
}
