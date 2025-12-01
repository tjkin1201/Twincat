namespace TwinCatQA.Domain.Models;

/// <summary>
/// 안전성 이슈 타입
/// </summary>
public enum SafetyIssueType
{
    /// <summary>비상정지 로직 미구현</summary>
    MissingEmergencyStop,

    /// <summary>인터락 조건 누락</summary>
    MissingInterlock,

    /// <summary>나눗셈 0 체크 누락</summary>
    DivisionByZeroRisk,

    /// <summary>무한 루프 가능성</summary>
    PotentialInfiniteLoop,

    /// <summary>타이머 오버플로우</summary>
    TimerOverflow,

    /// <summary>정수 오버플로우 가능성</summary>
    IntegerOverflowRisk,

    /// <summary>형변환 데이터 손실</summary>
    TypeConversionLoss,

    /// <summary>FB 인스턴스 미초기화</summary>
    UninitializedFunctionBlock,

    /// <summary>출력 변수 미할당</summary>
    UnassignedOutput,

    /// <summary>입력을 출력으로 사용</summary>
    InputUsedAsOutput,

    /// <summary>과도한 사이클 연산</summary>
    ExcessiveCycleLoad
}

/// <summary>
/// 안전성 검사 결과
/// </summary>
public class SafetyIssue
{
    /// <summary>이슈 타입</summary>
    public SafetyIssueType Type { get; init; }

    /// <summary>파일 경로</summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>라인 번호</summary>
    public int Line { get; init; }

    /// <summary>관련 코드</summary>
    public string CodeSnippet { get; init; } = string.Empty;

    /// <summary>심각도</summary>
    public IssueSeverity Severity { get; init; }

    /// <summary>설명 메시지</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>권장 수정 방법</summary>
    public string Recommendation { get; init; } = string.Empty;

    /// <summary>관련 변수/FB 이름</summary>
    public string? RelatedElement { get; init; }
}

/// <summary>
/// 나눗셈 검사 결과
/// </summary>
public class DivisionCheckResult
{
    /// <summary>파일 경로</summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>라인 번호</summary>
    public int Line { get; init; }

    /// <summary>나눗셈 표현식</summary>
    public string Expression { get; init; } = string.Empty;

    /// <summary>제수(나누는 수)</summary>
    public string Divisor { get; init; } = string.Empty;

    /// <summary>0 체크 존재 여부</summary>
    public bool HasZeroCheck { get; init; }

    /// <summary>심각도</summary>
    public IssueSeverity Severity { get; init; }
}

/// <summary>
/// 타이머 분석 결과
/// </summary>
public class TimerAnalysisResult
{
    /// <summary>타이머 변수명</summary>
    public string TimerName { get; init; } = string.Empty;

    /// <summary>타이머 타입 (TON, TOF, TP)</summary>
    public string TimerType { get; init; } = string.Empty;

    /// <summary>파일 경로</summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>라인 번호</summary>
    public int Line { get; init; }

    /// <summary>설정 시간 (ms)</summary>
    public long? PresetTimeMs { get; init; }

    /// <summary>이슈 목록</summary>
    public List<string> Issues { get; init; } = new();

    /// <summary>심각도</summary>
    public IssueSeverity Severity { get; init; }
}

/// <summary>
/// 형변환 검사 결과
/// </summary>
public class TypeConversionResult
{
    /// <summary>파일 경로</summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>라인 번호</summary>
    public int Line { get; init; }

    /// <summary>소스 타입</summary>
    public string SourceType { get; init; } = string.Empty;

    /// <summary>대상 타입</summary>
    public string TargetType { get; init; } = string.Empty;

    /// <summary>데이터 손실 가능성</summary>
    public bool PotentialDataLoss { get; init; }

    /// <summary>코드 스니펫</summary>
    public string CodeSnippet { get; init; } = string.Empty;

    /// <summary>심각도</summary>
    public IssueSeverity Severity { get; init; }

    /// <summary>설명</summary>
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// FB 초기화 검사 결과
/// </summary>
public class FBInitializationResult
{
    /// <summary>FB 인스턴스명</summary>
    public string InstanceName { get; init; } = string.Empty;

    /// <summary>FB 타입</summary>
    public string FBType { get; init; } = string.Empty;

    /// <summary>파일 경로</summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>선언 라인</summary>
    public int DeclarationLine { get; init; }

    /// <summary>초기화 여부</summary>
    public bool IsInitialized { get; init; }

    /// <summary>호출 여부</summary>
    public bool IsCalled { get; init; }

    /// <summary>심각도</summary>
    public IssueSeverity Severity { get; init; }

    /// <summary>설명</summary>
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// 출력 변수 검사 결과
/// </summary>
public class OutputAssignmentResult
{
    /// <summary>변수명</summary>
    public string VariableName { get; init; } = string.Empty;

    /// <summary>파일 경로</summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>라인 번호</summary>
    public int Line { get; init; }

    /// <summary>할당 여부</summary>
    public bool IsAssigned { get; init; }

    /// <summary>모든 경로에서 할당 여부</summary>
    public bool IsAssignedInAllPaths { get; init; }

    /// <summary>심각도</summary>
    public IssueSeverity Severity { get; init; }

    /// <summary>설명</summary>
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// 종합 안전성 분석 결과
/// </summary>
public class ComprehensiveSafetyAnalysis
{
    /// <summary>안전성 이슈 목록</summary>
    public List<SafetyIssue> SafetyIssues { get; init; } = new();

    /// <summary>나눗셈 검사 결과</summary>
    public List<DivisionCheckResult> DivisionChecks { get; init; } = new();

    /// <summary>타이머 분석 결과</summary>
    public List<TimerAnalysisResult> TimerAnalysis { get; init; } = new();

    /// <summary>형변환 검사 결과</summary>
    public List<TypeConversionResult> TypeConversions { get; init; } = new();

    /// <summary>FB 초기화 검사 결과</summary>
    public List<FBInitializationResult> FBInitializations { get; init; } = new();

    /// <summary>출력 할당 검사 결과</summary>
    public List<OutputAssignmentResult> OutputAssignments { get; init; } = new();

    /// <summary>총 Critical 이슈 수</summary>
    public int CriticalCount => SafetyIssues.Count(i => i.Severity == IssueSeverity.Critical) +
                                 DivisionChecks.Count(d => d.Severity == IssueSeverity.Critical);

    /// <summary>총 이슈 수</summary>
    public int TotalIssues => SafetyIssues.Count + DivisionChecks.Count(d => !d.HasZeroCheck) +
                               TimerAnalysis.Count(t => t.Issues.Any()) +
                               TypeConversions.Count(t => t.PotentialDataLoss) +
                               FBInitializations.Count(f => !f.IsInitialized) +
                               OutputAssignments.Count(o => !o.IsAssigned);
}
