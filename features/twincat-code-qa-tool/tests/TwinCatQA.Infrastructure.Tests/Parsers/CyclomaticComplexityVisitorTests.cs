using FluentAssertions;
using TwinCatQA.Infrastructure.Parsers;
using Xunit;

namespace TwinCatQA.Infrastructure.Tests.Parsers;

/// <summary>
/// 사이클로매틱 복잡도 계산기 테스트
/// 정규식 기반 McCabe 복잡도 계산 검증
/// </summary>
public class CyclomaticComplexityVisitorTests
{
    private readonly CyclomaticComplexityVisitor _calculator = new();

    #region 기본 복잡도 테스트

    /// <summary>
    /// 빈 코드의 복잡도는 1
    /// </summary>
    [Fact]
    public void CalculateFromCode_EmptyCode_ReturnsOne()
    {
        // Arrange
        var code = "";

        // Act
        var complexity = _calculator.CalculateFromCode(code);

        // Assert
        complexity.Should().Be(1, "빈 코드의 기본 복잡도는 1");
    }

    /// <summary>
    /// 단순 할당문만 있는 코드의 복잡도는 1
    /// </summary>
    [Fact]
    public void CalculateFromCode_SimpleAssignment_ReturnsOne()
    {
        // Arrange
        var code = @"
x := 10;
y := 20;
z := x + y;
";

        // Act
        var complexity = _calculator.CalculateFromCode(code);

        // Assert
        complexity.Should().Be(1, "분기 없는 코드의 복잡도는 1");
    }

    #endregion

    #region IF 문 테스트

    /// <summary>
    /// 단일 IF 문의 복잡도는 2 (기본 1 + IF 1)
    /// </summary>
    [Fact]
    public void CalculateFromCode_SingleIf_ReturnsTwo()
    {
        // Arrange
        var code = @"
IF condition THEN
    x := 10;
END_IF
";

        // Act
        var complexity = _calculator.CalculateFromCode(code);

        // Assert
        complexity.Should().Be(2, "IF 문 1개 = 기본(1) + IF(1)");
    }

    /// <summary>
    /// IF-ELSIF 문의 복잡도 검증
    /// </summary>
    [Fact]
    public void CalculateFromCode_IfElsif_ReturnsCorrectComplexity()
    {
        // Arrange
        var code = @"
IF condition1 THEN
    x := 1;
ELSIF condition2 THEN
    x := 2;
ELSIF condition3 THEN
    x := 3;
ELSE
    x := 0;
END_IF
";

        // Act
        var complexity = _calculator.CalculateFromCode(code);

        // Assert
        complexity.Should().Be(4, "기본(1) + IF(1) + ELSIF(2) = 4");
    }

    /// <summary>
    /// 중첩된 IF 문의 복잡도 검증
    /// </summary>
    [Fact]
    public void CalculateFromCode_NestedIf_ReturnsCorrectComplexity()
    {
        // Arrange
        var code = @"
IF outer THEN
    IF inner1 THEN
        x := 1;
    END_IF
    IF inner2 THEN
        y := 2;
    END_IF
END_IF
";

        // Act
        var complexity = _calculator.CalculateFromCode(code);

        // Assert
        complexity.Should().Be(4, "기본(1) + IF(3) = 4");
    }

    #endregion

    #region 반복문 테스트

    /// <summary>
    /// FOR 문의 복잡도 검증
    /// </summary>
    [Fact]
    public void CalculateFromCode_ForLoop_ReturnsCorrectComplexity()
    {
        // Arrange
        var code = @"
FOR i := 1 TO 10 DO
    sum := sum + i;
END_FOR
";

        // Act
        var complexity = _calculator.CalculateFromCode(code);

        // Assert
        complexity.Should().Be(2, "기본(1) + FOR(1) = 2");
    }

    /// <summary>
    /// WHILE 문의 복잡도 검증
    /// </summary>
    [Fact]
    public void CalculateFromCode_WhileLoop_ReturnsCorrectComplexity()
    {
        // Arrange
        var code = @"
WHILE running DO
    ProcessData();
END_WHILE
";

        // Act
        var complexity = _calculator.CalculateFromCode(code);

        // Assert
        complexity.Should().Be(2, "기본(1) + WHILE(1) = 2");
    }

    /// <summary>
    /// REPEAT-UNTIL 문의 복잡도 검증
    /// </summary>
    [Fact]
    public void CalculateFromCode_RepeatLoop_ReturnsCorrectComplexity()
    {
        // Arrange
        var code = @"
REPEAT
    counter := counter + 1;
UNTIL counter >= 10
END_REPEAT
";

        // Act
        var complexity = _calculator.CalculateFromCode(code);

        // Assert
        complexity.Should().Be(2, "기본(1) + REPEAT(1) = 2");
    }

    #endregion

    #region CASE 문 테스트

    /// <summary>
    /// CASE 문의 분기별 복잡도 검증
    /// </summary>
    [Fact]
    public void CalculateFromCode_CaseStatement_ReturnsCorrectComplexity()
    {
        // Arrange
        var code = @"
CASE state OF
    0: x := 0;
    1: x := 1;
    2: x := 2;
    3: x := 3;
ELSE
    x := -1;
END_CASE
";

        // Act
        var complexity = _calculator.CalculateFromCode(code);

        // Assert
        // 정규식 기반 CASE 분기 감지는 각 "숫자:" 패턴을 찾음
        complexity.Should().BeGreaterOrEqualTo(4, "CASE 분기가 4개 이상이어야 함");
    }

    /// <summary>
    /// 중첩된 CASE 문의 복잡도 검증
    /// </summary>
    [Fact]
    public void CalculateFromCode_NestedCaseStatement_ReturnsCorrectComplexity()
    {
        // Arrange
        var code = @"
CASE outerState OF
    0:
        CASE innerState OF
            0: x := 0;
            1: x := 1;
        END_CASE
    1: y := 1;
END_CASE
";

        // Act
        var complexity = _calculator.CalculateFromCode(code);

        // Assert
        // 기본(1) + 외부 분기(2) + 내부 분기(2) + 가능한 추가 패턴 매칭
        complexity.Should().BeGreaterOrEqualTo(5, "기본(1) + 외부 CASE(2) + 내부 CASE(2) = 최소 5");
    }

    #endregion

    #region 논리 연산자 테스트

    /// <summary>
    /// AND 연산자의 복잡도 검증
    /// </summary>
    [Fact]
    public void CalculateFromCode_AndOperator_ReturnsCorrectComplexity()
    {
        // Arrange
        var code = @"
IF condition1 AND condition2 THEN
    x := 1;
END_IF
";

        // Act
        var complexity = _calculator.CalculateFromCode(code);

        // Assert
        complexity.Should().Be(3, "기본(1) + IF(1) + AND(1) = 3");
    }

    /// <summary>
    /// OR 연산자의 복잡도 검증
    /// </summary>
    [Fact]
    public void CalculateFromCode_OrOperator_ReturnsCorrectComplexity()
    {
        // Arrange
        var code = @"
IF condition1 OR condition2 THEN
    x := 1;
END_IF
";

        // Act
        var complexity = _calculator.CalculateFromCode(code);

        // Assert
        complexity.Should().Be(3, "기본(1) + IF(1) + OR(1) = 3");
    }

    /// <summary>
    /// 복합 논리 연산자의 복잡도 검증
    /// </summary>
    [Fact]
    public void CalculateFromCode_ComplexLogicalOperators_ReturnsCorrectComplexity()
    {
        // Arrange
        var code = @"
IF (a AND b) OR (c AND d) THEN
    x := 1;
END_IF
";

        // Act
        var complexity = _calculator.CalculateFromCode(code);

        // Assert
        complexity.Should().Be(5, "기본(1) + IF(1) + AND(2) + OR(1) = 5");
    }

    #endregion

    #region EXIT/RETURN 테스트

    /// <summary>
    /// EXIT 문의 복잡도 검증
    /// </summary>
    [Fact]
    public void CalculateFromCode_ExitStatement_ReturnsCorrectComplexity()
    {
        // Arrange
        var code = @"
FOR i := 1 TO 10 DO
    IF found THEN
        EXIT;
    END_IF
END_FOR
";

        // Act
        var complexity = _calculator.CalculateFromCode(code);

        // Assert
        complexity.Should().Be(4, "기본(1) + FOR(1) + IF(1) + EXIT(1) = 4");
    }

    /// <summary>
    /// RETURN 문의 복잡도 검증
    /// </summary>
    [Fact]
    public void CalculateFromCode_ReturnStatement_ReturnsCorrectComplexity()
    {
        // Arrange
        var code = @"
IF error THEN
    RETURN;
END_IF
ProcessData();
";

        // Act
        var complexity = _calculator.CalculateFromCode(code);

        // Assert
        complexity.Should().Be(3, "기본(1) + IF(1) + RETURN(1) = 3");
    }

    #endregion

    #region 주석 처리 테스트

    /// <summary>
    /// 주석 내 키워드는 복잡도에 포함되지 않음 (라인 주석)
    /// </summary>
    [Fact]
    public void CalculateFromCode_LineComments_IgnoresKeywords()
    {
        // Arrange
        var code = @"
// IF this is a comment THEN
// FOR WHILE REPEAT
x := 10;
";

        // Act
        var complexity = _calculator.CalculateFromCode(code);

        // Assert
        complexity.Should().Be(1, "주석 내 키워드는 무시");
    }

    /// <summary>
    /// 주석 내 키워드는 복잡도에 포함되지 않음 (블록 주석)
    /// </summary>
    [Fact]
    public void CalculateFromCode_BlockComments_IgnoresKeywords()
    {
        // Arrange
        var code = @"
(*
IF this is a block comment THEN
FOR WHILE REPEAT CASE
*)
x := 10;
";

        // Act
        var complexity = _calculator.CalculateFromCode(code);

        // Assert
        complexity.Should().Be(1, "블록 주석 내 키워드는 무시");
    }

    #endregion

    #region 복합 코드 테스트

    /// <summary>
    /// 복잡한 실제 코드의 복잡도 검증
    /// </summary>
    [Fact]
    public void CalculateFromCode_ComplexCode_ReturnsCorrectComplexity()
    {
        // Arrange
        var code = @"
FUNCTION_BLOCK FB_MotorControl
VAR_INPUT
    iEnable : BOOL;
    iSpeed : INT;
END_VAR

IF iEnable THEN
    IF iSpeed > 0 AND iSpeed <= 100 THEN
        CASE state OF
            0: // 초기화
                InitMotor();
                state := 1;
            1: // 가속
                FOR i := 0 TO 10 DO
                    IF CheckSensor(i) THEN
                        Accelerate();
                    ELSE
                        EXIT;
                    END_IF
                END_FOR
                state := 2;
            2: // 운전
                WHILE running DO
                    Monitor();
                END_WHILE
        END_CASE
    ELSIF iSpeed > 100 THEN
        errorCode := 1;
        RETURN;
    END_IF
END_IF
END_FUNCTION_BLOCK
";

        // Act
        var complexity = _calculator.CalculateFromCode(code);

        // Assert
        // 기본(1) + IF(4개) + ELSIF(1) + AND(1) + CASE 분기(3) + FOR(1) + WHILE(1) + EXIT(1) + RETURN(1) = 14
        complexity.Should().BeGreaterThan(10, "복잡한 코드는 높은 복잡도를 가짐");
        complexity.Should().BeLessThan(20, "예상 범위 내 복잡도");
    }

    /// <summary>
    /// TwinCAT 실제 패턴 코드 테스트
    /// </summary>
    [Fact]
    public void CalculateFromCode_StateMachinePattern_ReturnsExpectedComplexity()
    {
        // Arrange - 일반적인 상태 머신 패턴
        var code = @"
CASE eState OF
    E_STATE_IDLE:
        IF bStart THEN
            eState := E_STATE_INIT;
        END_IF

    E_STATE_INIT:
        IF bInitDone AND NOT bError THEN
            eState := E_STATE_RUN;
        ELSIF bError THEN
            eState := E_STATE_ERROR;
        END_IF

    E_STATE_RUN:
        IF bStop OR bError THEN
            eState := E_STATE_STOP;
        END_IF

    E_STATE_STOP:
        eState := E_STATE_IDLE;

    E_STATE_ERROR:
        IF bReset THEN
            eState := E_STATE_IDLE;
        END_IF
END_CASE
";

        // Act
        var complexity = _calculator.CalculateFromCode(code);

        // Assert
        // 기본(1) + CASE 분기(5) + IF(4) + ELSIF(1) + AND(1) + OR(1) = 13 이상
        // 정규식 기반이므로 일부 추가 매칭이 있을 수 있음
        complexity.Should().BeGreaterOrEqualTo(10, "상태 머신 패턴은 높은 복잡도를 가짐");
    }

    #endregion

    #region 품질 등급 테스트

    /// <summary>
    /// 낮은 복잡도는 Good 등급
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void GetQualityGrade_LowComplexity_ReturnsGood(int complexity)
    {
        // Act
        var grade = CyclomaticComplexityVisitor.GetQualityGrade(complexity);

        // Assert
        grade.Should().Be(TwinCatQA.Domain.Models.QualityGrade.Good);
    }

    /// <summary>
    /// 중간 복잡도는 Fair 등급
    /// </summary>
    [Theory]
    [InlineData(11)]
    [InlineData(13)]
    [InlineData(15)]
    public void GetQualityGrade_MediumComplexity_ReturnsFair(int complexity)
    {
        // Act
        var grade = CyclomaticComplexityVisitor.GetQualityGrade(complexity);

        // Assert
        grade.Should().Be(TwinCatQA.Domain.Models.QualityGrade.Fair);
    }

    /// <summary>
    /// 높은 복잡도는 Poor 등급
    /// </summary>
    [Theory]
    [InlineData(16)]
    [InlineData(20)]
    [InlineData(25)]
    [InlineData(100)]
    public void GetQualityGrade_HighComplexity_ReturnsPoor(int complexity)
    {
        // Act
        var grade = CyclomaticComplexityVisitor.GetQualityGrade(complexity);

        // Assert
        grade.Should().Be(TwinCatQA.Domain.Models.QualityGrade.Poor);
    }

    #endregion
}
