using TwinCatQA.Domain.Models;
using TwinCatQA.Infrastructure.Analysis;
using Xunit;

namespace TwinCatQA.Application.Tests.Analysis;

/// <summary>
/// 안전성 분석기 테스트
/// </summary>
public class SafetyAnalyzersTests
{
    #region 나눗셈 안전성 테스트

    [Fact]
    public void DivisionChecker_DetectsUncheckedDivision()
    {
        // Arrange
        var checker = new DivisionSafetyChecker();
        var session = CreateSession(@"
VAR
    a, b, result : INT;
END_VAR

result := a / b;  // b가 0인지 체크 안함
");

        // Act
        var results = checker.Check(session);

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.Divisor == "b" && !r.HasZeroCheck);
    }

    [Fact]
    public void DivisionChecker_PassesWithZeroCheck()
    {
        // Arrange
        var checker = new DivisionSafetyChecker();
        var session = CreateSession(@"
VAR
    a, b, result : INT;
END_VAR

IF b <> 0 THEN
    result := a / b;
END_IF
");

        // Act
        var results = checker.Check(session);

        // Assert
        Assert.Empty(results);  // 0 체크가 있으므로 이슈 없음
    }

    #endregion

    #region 루프 안전성 테스트

    [Fact]
    public void LoopAnalyzer_DetectsWhileTrue()
    {
        // Arrange
        var analyzer = new LoopSafetyAnalyzer();
        var session = CreateSession(@"
WHILE TRUE DO
    x := x + 1;
END_WHILE
");

        // Act
        var results = analyzer.Analyze(session);

        // Assert
        Assert.Contains(results, r => r.Type == SafetyIssueType.PotentialInfiniteLoop);
    }

    [Fact]
    public void LoopAnalyzer_DetectsUnmodifiedCondition()
    {
        // Arrange
        var analyzer = new LoopSafetyAnalyzer();
        var session = CreateSession(@"
VAR
    running : BOOL := TRUE;
END_VAR

WHILE running DO
    x := x + 1;  // running이 변경되지 않음
END_WHILE
");

        // Act
        var results = analyzer.Analyze(session);

        // Assert
        Assert.Contains(results, r => r.Type == SafetyIssueType.PotentialInfiniteLoop);
    }

    [Fact]
    public void LoopAnalyzer_PassesWithExit()
    {
        // Arrange
        var analyzer = new LoopSafetyAnalyzer();
        var session = CreateSession(@"
WHILE TRUE DO
    x := x + 1;
    IF x > 100 THEN
        EXIT;
    END_IF
END_WHILE
");

        // Act
        var results = analyzer.Analyze(session);

        // Assert
        Assert.Empty(results);  // EXIT가 있으므로 OK
    }

    #endregion

    #region 형변환 안전성 테스트

    [Fact]
    public void TypeConversionChecker_DetectsDataLoss()
    {
        // Arrange
        var checker = new TypeConversionChecker();
        var session = CreateSession(@"
VAR
    bigValue : DINT;
    smallValue : INT;
END_VAR

smallValue := DINT_TO_INT(bigValue);  // 32비트 -> 16비트 손실
");

        // Act
        var results = checker.Check(session);

        // Assert
        Assert.Contains(results, r => r.PotentialDataLoss && r.SourceType == "DINT" && r.TargetType == "INT");
    }

    [Fact]
    public void TypeConversionChecker_DetectsSignLoss()
    {
        // Arrange
        var checker = new TypeConversionChecker();
        var session = CreateSession(@"
VAR
    signed : INT;
    unsigned : UINT;
END_VAR

unsigned := INT_TO_UINT(signed);  // 부호 손실 가능
");

        // Act
        var results = checker.Check(session);

        // Assert
        Assert.Contains(results, r => r.PotentialDataLoss);
    }

    #endregion

    #region FB 초기화 테스트

    [Fact]
    public void FBInitChecker_DetectsUncalledFB()
    {
        // Arrange
        var checker = new FBInitializationChecker();
        var session = CreateSession(@"
VAR
    fbMotor : FB_MotorControl;  // 선언만 하고 호출 안함
    timer1 : TON;
END_VAR

// fbMotor는 호출되지 않음
timer1(IN := TRUE, PT := T#1s);
");

        // Act
        var results = checker.Check(session);

        // Assert
        Assert.Contains(results, r => r.InstanceName == "fbMotor" && !r.IsCalled);
    }

    #endregion

    #region 출력 할당 테스트

    [Fact]
    public void OutputChecker_DetectsUnassignedOutput()
    {
        // Arrange
        var checker = new OutputAssignmentChecker();
        var session = CreateSession(@"
FUNCTION_BLOCK FB_Test
VAR_OUTPUT
    result : INT;
    status : BOOL;  // 할당 안됨
END_VAR

result := 100;
// status는 할당되지 않음
END_FUNCTION_BLOCK
");

        // Act
        var results = checker.Check(session);

        // Assert
        Assert.Contains(results, r => r.VariableName == "status" && !r.IsAssigned);
    }

    #endregion

    #region I/O 방향성 테스트

    [Fact]
    public void IODirectionChecker_DetectsInputWrite()
    {
        // Arrange
        var checker = new IODirectionChecker();
        var session = CreateSession(@"
VAR_GLOBAL
    sensor AT %IX0.0 : BOOL;  // 입력
END_VAR

sensor := TRUE;  // 입력에 쓰기 시도!
");

        // Act
        var results = checker.Check(session);

        // Assert
        Assert.Contains(results, r => r.Type == SafetyIssueType.InputUsedAsOutput);
    }

    #endregion

    #region 타이머 안전성 테스트

    [Fact]
    public void TimerAnalyzer_DetectsTooShortTimer()
    {
        // Arrange
        var analyzer = new TimerSafetyAnalyzer();
        var session = CreateSession(@"
VAR
    quickTimer : TON;
END_VAR

quickTimer(IN := TRUE, PT := T#1ms);  // 너무 짧음
");

        // Act
        var results = analyzer.Analyze(session);

        // Assert
        Assert.Contains(results, r => r.PresetTimeMs < 10);
    }

    #endregion

    #region Helper Methods

    private ValidationSession CreateSession(string content)
    {
        return new ValidationSession
        {
            ScannedFiles = new List<CodeFile>
            {
                new CodeFile
                {
                    FilePath = "test.TcPOU",
                    Content = content,
                    FunctionBlocks = new List<FunctionBlock>()
                }
            }
        };
    }

    #endregion
}
