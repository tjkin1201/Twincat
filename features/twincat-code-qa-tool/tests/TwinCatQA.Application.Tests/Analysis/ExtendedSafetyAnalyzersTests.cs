using TwinCatQA.Domain.Models;
using TwinCatQA.Infrastructure.Analysis;
using Xunit;

namespace TwinCatQA.Application.Tests.Analysis;

/// <summary>
/// 심화 안전성 분석기 테스트
/// </summary>
public class ExtendedSafetyAnalyzersTests
{
    #region 동시성 분석 테스트

    [Fact]
    public void ConcurrencyAnalyzer_DetectsRaceCondition()
    {
        // Arrange
        var analyzer = new ConcurrencyAnalyzer();
        var session = new ValidationSession
        {
            ScannedFiles = new List<CodeFile>
            {
                new CodeFile
                {
                    FilePath = "GVL.TcGVL",
                    Content = @"
VAR_GLOBAL
    gSharedCounter : INT;
END_VAR"
                },
                new CodeFile
                {
                    FilePath = "Task1.TcPOU",
                    Content = "gSharedCounter := gSharedCounter + 1;"  // 쓰기
                },
                new CodeFile
                {
                    FilePath = "Task2.TcPOU",
                    Content = "gSharedCounter := 0;"  // 쓰기
                }
            }
        };

        // Act
        var results = analyzer.Analyze(session);

        // Assert
        Assert.Contains(results, r => r.Type == ConcurrencyIssueType.RaceCondition &&
                                       r.VariableName == "gSharedCounter");
    }

    #endregion

    #region 상태머신 분석 테스트

    [Fact]
    public void StateMachineAnalyzer_DetectsMissingElse()
    {
        // Arrange
        var analyzer = new StateMachineAnalyzer();
        var session = CreateSession(@"
CASE nState OF
    0: nState := 1;
    1: nState := 2;
    2: nState := 0;
END_CASE
");

        // Act
        var results = analyzer.Analyze(session);

        // Assert
        Assert.Contains(results, r => r.Type == StateMachineIssueType.MissingElseBranch);
    }

    [Fact]
    public void StateMachineAnalyzer_DetectsDeadEndState()
    {
        // Arrange
        var analyzer = new StateMachineAnalyzer();
        var session = CreateSession(@"
CASE nState OF
    0: nState := 1;
    1: nState := 2;
    2: x := 1;  // 전이 없음 - DeadEnd
END_CASE
");

        // Act
        var results = analyzer.Analyze(session);

        // Assert
        Assert.Contains(results, r => r.Type == StateMachineIssueType.DeadEndState);
    }

    #endregion

    #region 통신 분석 테스트

    [Fact]
    public void CommunicationAnalyzer_DetectsMissingTimeout()
    {
        // Arrange
        var analyzer = new CommunicationAnalyzer();
        var session = CreateSession(@"
VAR
    fbAdsRead : ADSREAD;
END_VAR

fbAdsRead(NETID := '', PORT := 851);
// 타임아웃 처리 없음
");

        // Act
        var results = analyzer.Analyze(session);

        // Assert
        Assert.Contains(results, r => r.Type == CommunicationIssueType.MissingTimeout);
    }

    [Fact]
    public void CommunicationAnalyzer_DetectsMissingErrorHandling()
    {
        // Arrange
        var analyzer = new CommunicationAnalyzer();
        var session = CreateSession(@"
VAR
    fbModbus : FB_MBReadRegs;
END_VAR

fbModbus(sIPAddr := '192.168.1.1');
// 에러 처리 없음
");

        // Act
        var results = analyzer.Analyze(session);

        // Assert
        Assert.Contains(results, r => r.Type == CommunicationIssueType.MissingErrorHandling);
    }

    [Fact]
    public void CommunicationAnalyzer_DetectsUnvalidatedData()
    {
        // Arrange
        var analyzer = new CommunicationAnalyzer();
        var session = CreateSession(@"
VAR
    fbAdsRead : ADSREAD;
    nData : INT;
END_VAR

fbAdsRead(NETID := '');
nData := fbAdsRead.DATA;  // 검증 없이 사용
");

        // Act
        var results = analyzer.Analyze(session);

        // Assert
        Assert.Contains(results, r => r.Type == CommunicationIssueType.UnvalidatedData);
    }

    #endregion

    #region 리소스 분석 테스트

    [Fact]
    public void ResourceAnalyzer_DetectsStringOverflow()
    {
        // Arrange
        var analyzer = new ResourceAnalyzer();
        var session = CreateSession(@"
VAR
    sResult : STRING(20);
    sA : STRING := 'Hello';
    sB : STRING := 'World';
END_VAR

sResult := CONCAT(sA, sB);  // 길이 체크 없음
");

        // Act
        var results = analyzer.Analyze(session);

        // Assert
        Assert.Contains(results, r => r.Type == ResourceIssueType.StringBufferOverflow);
    }

    [Fact]
    public void ResourceAnalyzer_DetectsUncheckedPointer()
    {
        // Arrange
        var analyzer = new ResourceAnalyzer();
        var session = CreateSession(@"
VAR
    pData : POINTER TO INT;
    nValue : INT;
END_VAR

nValue := pData^;  // 널 체크 없이 역참조
");

        // Act
        var results = analyzer.Analyze(session);

        // Assert
        Assert.Contains(results, r => r.Type == ResourceIssueType.UncheckedPointer);
    }

    [Fact]
    public void ResourceAnalyzer_PassesWithNullCheck()
    {
        // Arrange
        var analyzer = new ResourceAnalyzer();
        var session = CreateSession(@"
VAR
    pData : POINTER TO INT;
    nValue : INT;
END_VAR

IF pData <> 0 THEN
    nValue := pData^;
END_IF
");

        // Act
        var results = analyzer.Analyze(session);

        // Assert
        Assert.DoesNotContain(results, r => r.Type == ResourceIssueType.UncheckedPointer);
    }

    #endregion

    #region 센서 분석 테스트

    [Fact]
    public void SensorAnalyzer_DetectsMissingRangeCheck()
    {
        // Arrange
        var analyzer = new SensorAnalyzer();
        var session = CreateSession(@"
VAR_GLOBAL
    nPressure AT %IW100 : INT;  // 아날로그 입력
END_VAR

// 범위 체크 없이 사용
rCalculated := nPressure * 0.1;
");

        // Act
        var results = analyzer.Analyze(session);

        // Assert
        Assert.Contains(results, r => r.Type == SensorIssueType.MissingRangeCheck);
    }

    [Fact]
    public void SensorAnalyzer_DetectsMissingFaultDetection()
    {
        // Arrange
        var analyzer = new SensorAnalyzer();
        var session = CreateSession(@"
VAR_GLOBAL
    nTemperature AT %IW200 : INT;
END_VAR

// 고장 감지 없이 사용
");

        // Act
        var results = analyzer.Analyze(session);

        // Assert
        Assert.Contains(results, r => r.Type == SensorIssueType.MissingFaultDetection);
    }

    [Fact]
    public void SensorAnalyzer_DetectsMissingChatteringFilter()
    {
        // Arrange
        var analyzer = new SensorAnalyzer();
        var session = CreateSession(@"
VAR_GLOBAL
    bProximitySensor AT %IX0.0 : BOOL;
END_VAR

// 채터링 필터 없이 직접 사용
IF bProximitySensor THEN
    bOutput := TRUE;
END_IF
");

        // Act
        var results = analyzer.Analyze(session);

        // Assert
        Assert.Contains(results, r => r.Type == SensorIssueType.MissingChatteringFilter);
    }

    [Fact]
    public void SensorAnalyzer_PassesWithFilter()
    {
        // Arrange
        var analyzer = new SensorAnalyzer();
        var session = CreateSession(@"
VAR_GLOBAL
    bProximitySensor AT %IX0.0 : BOOL;
END_VAR
VAR
    tonFilter : TON;
END_VAR

tonFilter(IN := bProximitySensor, PT := T#50ms);
IF tonFilter.Q THEN
    bOutput := TRUE;
END_IF
");

        // Act
        var results = analyzer.Analyze(session);

        // Assert
        Assert.DoesNotContain(results, r => r.Type == SensorIssueType.MissingChatteringFilter &&
                                            r.SensorName == "bProximitySensor");
    }

    #endregion

    #region 에러 복구 분석 테스트

    [Fact]
    public void ErrorRecoveryAnalyzer_DetectsMissingErrorReset()
    {
        // Arrange
        var analyzer = new ErrorRecoveryAnalyzer();
        var session = CreateSession(@"
VAR
    bError : BOOL;
END_VAR

IF bCondition THEN
    bError := TRUE;  // 에러 설정만 있고 리셋 없음
END_IF
");

        // Act
        var results = analyzer.Analyze(session);

        // Assert
        Assert.Contains(results, r => r.Type == ErrorRecoveryIssueType.MissingErrorReset);
    }

    [Fact]
    public void ErrorRecoveryAnalyzer_PassesWithErrorReset()
    {
        // Arrange
        var analyzer = new ErrorRecoveryAnalyzer();
        var session = CreateSession(@"
VAR
    bError : BOOL;
END_VAR

IF bCondition THEN
    bError := TRUE;
END_IF

IF bErrorReset THEN
    bError := FALSE;
END_IF
");

        // Act
        var results = analyzer.Analyze(session);

        // Assert
        Assert.DoesNotContain(results, r => r.Type == ErrorRecoveryIssueType.MissingErrorReset &&
                                            r.RelatedElement == "bError");
    }

    [Fact]
    public void ErrorRecoveryAnalyzer_DetectsMissingWatchdog()
    {
        // Arrange
        var analyzer = new ErrorRecoveryAnalyzer();
        var session = CreateSession(@"
PROGRAM MAIN
VAR
    nCounter : INT;
END_VAR

nCounter := nCounter + 1;
// 워치독 없음
");

        // Act
        var results = analyzer.Analyze(session);

        // Assert
        Assert.Contains(results, r => r.Type == ErrorRecoveryIssueType.MissingWatchdog);
    }

    [Fact]
    public void ErrorRecoveryAnalyzer_DetectsMissingSafeState()
    {
        // Arrange
        var analyzer = new ErrorRecoveryAnalyzer();
        var session = CreateSession(@"
IF bMachineError THEN
    // 에러 발생했지만 안전 조치 없음
    nErrorCode := 100;
END_IF
");

        // Act
        var results = analyzer.Analyze(session);

        // Assert
        Assert.Contains(results, r => r.Type == ErrorRecoveryIssueType.MissingSafeState);
    }

    #endregion

    #region 로깅 분석 테스트

    [Fact]
    public void LoggingAnalyzer_DetectsMissingErrorLogging()
    {
        // Arrange
        var analyzer = new LoggingAnalyzer();
        var session = CreateSession(@"
IF bCondition THEN
    bMachineError := TRUE;  // 에러 설정만 있고 로깅 없음
END_IF
");

        // Act
        var results = analyzer.Analyze(session);

        // Assert
        Assert.Contains(results, r => r.Type == LoggingIssueType.MissingErrorLogging);
    }

    [Fact]
    public void LoggingAnalyzer_PassesWithLogging()
    {
        // Arrange
        var analyzer = new LoggingAnalyzer();
        var session = CreateSession(@"
IF bCondition THEN
    bMachineError := TRUE;
    ADSLOGSTR(ADSLOG_MSGTYPE_ERROR, 'Machine error occurred');
END_IF
");

        // Act
        var results = analyzer.Analyze(session);

        // Assert
        Assert.DoesNotContain(results, r => r.Type == LoggingIssueType.MissingErrorLogging &&
                                            r.RelatedElement == "bMachineError");
    }

    [Fact]
    public void LoggingAnalyzer_DetectsMissingStateChangeLogging()
    {
        // Arrange
        var analyzer = new LoggingAnalyzer();
        var session = CreateSession(@"
CASE nState OF
    0: nState := 1;
    1: nState := 2;
    2: nState := 3;
    3: nState := 0;
END_CASE
");

        // Act
        var results = analyzer.Analyze(session);

        // Assert
        Assert.Contains(results, r => r.Type == LoggingIssueType.MissingStateChangeLogging);
    }

    #endregion

    #region 센서 분석 테스트 (압력/진공/차압)

    [Fact]
    public void SensorAnalyzer_DetectsPressureSensorWithoutRangeCheck()
    {
        // Arrange
        var analyzer = new SensorAnalyzer();
        var session = CreateSession(@"
VAR_GLOBAL
    nPressureGauge AT %IW100 : INT;
END_VAR

// 범위 체크 없이 사용
rCalculated := nPressureGauge * 0.1;
");

        // Act
        var results = analyzer.Analyze(session);

        // Assert
        Assert.Contains(results, r => r.Type == SensorIssueType.MissingRangeCheck &&
                                       r.SensorType.Contains("Pressure"));
    }

    [Fact]
    public void SensorAnalyzer_DetectsVacuumSensorWithoutFaultDetection()
    {
        // Arrange
        var analyzer = new SensorAnalyzer();
        var session = CreateSession(@"
VAR_GLOBAL
    nVacuumLevel AT %IW200 : INT;
END_VAR
");

        // Act
        var results = analyzer.Analyze(session);

        // Assert
        Assert.Contains(results, r => r.Type == SensorIssueType.MissingFaultDetection &&
                                       r.SensorType.Contains("Vacuum"));
    }

    [Fact]
    public void SensorAnalyzer_DetectsDiffPressureSensor()
    {
        // Arrange
        var analyzer = new SensorAnalyzer();
        var session = CreateSession(@"
VAR_GLOBAL
    nDiffPressure AT %IW300 : INT;  // 차압 센서
END_VAR
");

        // Act
        var results = analyzer.Analyze(session);

        // Assert
        Assert.Contains(results, r => r.SensorType.Contains("DiffPressure"));
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
