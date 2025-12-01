namespace TwinCatQA.Domain.Models;

#region 동시성 분석

/// <summary>
/// 동시성 이슈 타입
/// </summary>
public enum ConcurrencyIssueType
{
    /// <summary>공유 변수 경쟁 조건</summary>
    RaceCondition,

    /// <summary>태스크 우선순위 역전 가능성</summary>
    PriorityInversion,

    /// <summary>데드락 가능성</summary>
    PotentialDeadlock
}

/// <summary>
/// 동시성 이슈 결과
/// </summary>
public class ConcurrencyIssue
{
    public ConcurrencyIssueType Type { get; init; }
    public string FilePath { get; init; } = string.Empty;
    public int Line { get; init; }
    public string VariableName { get; init; } = string.Empty;
    public List<string> AccessingTasks { get; init; } = new();
    public IssueSeverity Severity { get; init; }
    public string Message { get; init; } = string.Empty;
    public string Recommendation { get; init; } = string.Empty;
}

#endregion

#region 상태머신 분석

/// <summary>
/// 상태머신 이슈 타입
/// </summary>
public enum StateMachineIssueType
{
    /// <summary>도달 불가능한 상태</summary>
    UnreachableState,

    /// <summary>탈출 불가능한 상태</summary>
    DeadEndState,

    /// <summary>기본/초기 상태 미정의</summary>
    MissingDefaultState,

    /// <summary>에러 복구 상태 없음</summary>
    MissingErrorRecovery,

    /// <summary>ELSE 분기 없음</summary>
    MissingElseBranch
}

/// <summary>
/// 상태머신 분석 결과
/// </summary>
public class StateMachineIssue
{
    public StateMachineIssueType Type { get; init; }
    public string FilePath { get; init; } = string.Empty;
    public int Line { get; init; }
    public string StateMachineName { get; init; } = string.Empty;
    public List<string> DefinedStates { get; init; } = new();
    public List<string> ProblematicStates { get; init; } = new();
    public IssueSeverity Severity { get; init; }
    public string Message { get; init; } = string.Empty;
    public string Recommendation { get; init; } = string.Empty;
}

#endregion

#region 통신 분석

/// <summary>
/// 통신 이슈 타입
/// </summary>
public enum CommunicationIssueType
{
    /// <summary>통신 타임아웃 미처리</summary>
    MissingTimeout,

    /// <summary>수신 데이터 검증 없음</summary>
    UnvalidatedData,

    /// <summary>통신 에러 핸들링 없음</summary>
    MissingErrorHandling,

    /// <summary>재시도 로직 없음</summary>
    MissingRetryLogic
}

/// <summary>
/// 통신 분석 결과
/// </summary>
public class CommunicationIssue
{
    public CommunicationIssueType Type { get; init; }
    public string FilePath { get; init; } = string.Empty;
    public int Line { get; init; }
    public string CommunicationType { get; init; } = string.Empty;  // ADS, Modbus, TCP 등
    public string FunctionBlockName { get; init; } = string.Empty;
    public IssueSeverity Severity { get; init; }
    public string Message { get; init; } = string.Empty;
    public string Recommendation { get; init; } = string.Empty;
}

#endregion

#region 리소스 분석

/// <summary>
/// 리소스 이슈 타입
/// </summary>
public enum ResourceIssueType
{
    /// <summary>문자열 버퍼 오버플로우 가능성</summary>
    StringBufferOverflow,

    /// <summary>포인터 널 체크 없음</summary>
    UncheckedPointer,

    /// <summary>동적 메모리 해제 없음</summary>
    MemoryLeak,

    /// <summary>배열 동적 할당 미검증</summary>
    UncheckedDynamicArray
}

/// <summary>
/// 리소스 분석 결과
/// </summary>
public class ResourceIssue
{
    public ResourceIssueType Type { get; init; }
    public string FilePath { get; init; } = string.Empty;
    public int Line { get; init; }
    public string VariableName { get; init; } = string.Empty;
    public string DataType { get; init; } = string.Empty;
    public IssueSeverity Severity { get; init; }
    public string Message { get; init; } = string.Empty;
    public string Recommendation { get; init; } = string.Empty;
}

#endregion

#region 센서 분석

/// <summary>
/// 센서 이슈 타입
/// </summary>
public enum SensorIssueType
{
    /// <summary>아날로그 센서 값 범위 체크 없음</summary>
    MissingRangeCheck,

    /// <summary>센서 고장 감지 로직 없음</summary>
    MissingFaultDetection,

    /// <summary>채터링 필터 없음</summary>
    MissingChatteringFilter,

    /// <summary>센서 응답 타임아웃 없음</summary>
    MissingSensorTimeout
}

/// <summary>
/// 센서 분석 결과
/// </summary>
public class SensorIssue
{
    public SensorIssueType Type { get; init; }
    public string FilePath { get; init; } = string.Empty;
    public int Line { get; init; }
    public string SensorName { get; init; } = string.Empty;
    public string SensorType { get; init; } = string.Empty;  // Analog, Digital
    public string IOAddress { get; init; } = string.Empty;
    public IssueSeverity Severity { get; init; }
    public string Message { get; init; } = string.Empty;
    public string Recommendation { get; init; } = string.Empty;
}

#endregion

#region 에러 복구 분석

/// <summary>
/// 에러 복구 이슈 타입
/// </summary>
public enum ErrorRecoveryIssueType
{
    /// <summary>에러 리셋 로직 없음</summary>
    MissingErrorReset,

    /// <summary>워치독 타이머 미사용</summary>
    MissingWatchdog,

    /// <summary>에러 발생 후 안전 상태 전환 없음</summary>
    MissingSafeState,

    /// <summary>재시작 시퀀스 없음</summary>
    MissingRestartSequence
}

/// <summary>
/// 에러 복구 분석 결과
/// </summary>
public class ErrorRecoveryIssue
{
    public ErrorRecoveryIssueType Type { get; init; }
    public string FilePath { get; init; } = string.Empty;
    public int Line { get; init; }
    public string RelatedElement { get; init; } = string.Empty;
    public IssueSeverity Severity { get; init; }
    public string Message { get; init; } = string.Empty;
    public string Recommendation { get; init; } = string.Empty;
}

#endregion

#region 로깅 분석

/// <summary>
/// 로깅 이슈 타입
/// </summary>
public enum LoggingIssueType
{
    /// <summary>에러/알람 로깅 없음</summary>
    MissingErrorLogging,

    /// <summary>상태 변경 로깅 없음</summary>
    MissingStateChangeLogging,

    /// <summary>중요 동작 로깅 없음</summary>
    MissingOperationLogging,

    /// <summary>타임스탬프 없는 로깅</summary>
    MissingTimestamp
}

/// <summary>
/// 로깅 분석 결과
/// </summary>
public class LoggingIssue
{
    public LoggingIssueType Type { get; init; }
    public string FilePath { get; init; } = string.Empty;
    public int Line { get; init; }
    public string RelatedElement { get; init; } = string.Empty;
    public IssueSeverity Severity { get; init; }
    public string Message { get; init; } = string.Empty;
    public string Recommendation { get; init; } = string.Empty;
}

#endregion

#region 종합 심화 분석 결과

/// <summary>
/// 종합 심화 분석 결과
/// </summary>
public class ExtendedSafetyAnalysis
{
    public List<ConcurrencyIssue> ConcurrencyIssues { get; init; } = new();
    public List<StateMachineIssue> StateMachineIssues { get; init; } = new();
    public List<CommunicationIssue> CommunicationIssues { get; init; } = new();
    public List<ResourceIssue> ResourceIssues { get; init; } = new();
    public List<SensorIssue> SensorIssues { get; init; } = new();
    public List<ErrorRecoveryIssue> ErrorRecoveryIssues { get; init; } = new();
    public List<LoggingIssue> LoggingIssues { get; init; } = new();

    public int TotalIssues =>
        ConcurrencyIssues.Count +
        StateMachineIssues.Count +
        CommunicationIssues.Count +
        ResourceIssues.Count +
        SensorIssues.Count +
        ErrorRecoveryIssues.Count +
        LoggingIssues.Count;

    public int CriticalCount =>
        ConcurrencyIssues.Count(i => i.Severity == IssueSeverity.Critical) +
        StateMachineIssues.Count(i => i.Severity == IssueSeverity.Critical) +
        CommunicationIssues.Count(i => i.Severity == IssueSeverity.Critical) +
        ResourceIssues.Count(i => i.Severity == IssueSeverity.Critical) +
        SensorIssues.Count(i => i.Severity == IssueSeverity.Critical) +
        ErrorRecoveryIssues.Count(i => i.Severity == IssueSeverity.Critical) +
        LoggingIssues.Count(i => i.Severity == IssueSeverity.Critical);
}

#endregion
