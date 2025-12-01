namespace TwinCatQA.Domain.Models;

/// <summary>
/// 변수 사용 분석 결과
/// </summary>
public class VariableUsageAnalysis
{
    /// <summary>
    /// 사용되지 않는 변수 목록
    /// </summary>
    public List<UnusedVariable> UnusedVariables { get; init; } = new();

    /// <summary>
    /// 초기화되지 않은 변수 목록
    /// </summary>
    public List<UninitializedVariable> UninitializedVariables { get; init; } = new();

    /// <summary>
    /// Dead Code 목록
    /// </summary>
    public List<DeadCode> DeadCodeBlocks { get; init; } = new();

    /// <summary>
    /// 분석된 프로젝트 경로
    /// </summary>
    public string ProjectPath { get; init; } = string.Empty;

    /// <summary>
    /// 분석 시간
    /// </summary>
    public DateTime AnalysisTime { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// 총 문제 개수
    /// </summary>
    public int TotalIssues => UnusedVariables.Count + UninitializedVariables.Count + DeadCodeBlocks.Count;
}

/// <summary>
/// 사용되지 않는 변수
/// </summary>
public class UnusedVariable
{
    /// <summary>
    /// 변수명
    /// </summary>
    public string VariableName { get; init; } = string.Empty;

    /// <summary>
    /// 변수 타입
    /// </summary>
    public string VariableType { get; init; } = string.Empty;

    /// <summary>
    /// 선언된 파일 경로
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// 선언된 라인 번호
    /// </summary>
    public int LineNumber { get; init; }

    /// <summary>
    /// POU 이름
    /// </summary>
    public string PouName { get; init; } = string.Empty;

    /// <summary>
    /// 변수 스코프 (VAR, VAR_INPUT, VAR_OUTPUT 등)
    /// </summary>
    public VariableScope Scope { get; init; } = VariableScope.Local;

    /// <summary>
    /// 심각도
    /// </summary>
    public IssueSeverity Severity { get; init; } = IssueSeverity.Warning;
}

/// <summary>
/// 초기화되지 않은 변수
/// </summary>
public class UninitializedVariable
{
    /// <summary>
    /// 변수명
    /// </summary>
    public string VariableName { get; init; } = string.Empty;

    /// <summary>
    /// 변수 타입
    /// </summary>
    public string VariableType { get; init; } = string.Empty;

    /// <summary>
    /// 사용된 파일 경로
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// 사용된 라인 번호
    /// </summary>
    public int LineNumber { get; init; }

    /// <summary>
    /// POU 이름
    /// </summary>
    public string PouName { get; init; } = string.Empty;

    /// <summary>
    /// 사용 컨텍스트 (어떤 표현식에서 사용되었는지)
    /// </summary>
    public string UsageContext { get; init; } = string.Empty;

    /// <summary>
    /// 심각도
    /// </summary>
    public IssueSeverity Severity { get; init; } = IssueSeverity.Error;
}

/// <summary>
/// Dead Code (실행되지 않는 코드)
/// </summary>
public class DeadCode
{
    /// <summary>
    /// Dead Code가 발견된 파일 경로
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// 시작 라인 번호
    /// </summary>
    public int StartLine { get; init; }

    /// <summary>
    /// 종료 라인 번호
    /// </summary>
    public int EndLine { get; init; }

    /// <summary>
    /// POU 이름
    /// </summary>
    public string PouName { get; init; } = string.Empty;

    /// <summary>
    /// Dead Code 타입
    /// </summary>
    public DeadCodeType Type { get; init; } = DeadCodeType.UnreachableCode;

    /// <summary>
    /// 설명
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// 심각도
    /// </summary>
    public IssueSeverity Severity { get; init; } = IssueSeverity.Warning;
}

