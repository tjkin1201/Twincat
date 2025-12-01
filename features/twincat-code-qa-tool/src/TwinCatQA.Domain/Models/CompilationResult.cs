namespace TwinCatQA.Domain.Models;

/// <summary>
/// TwinCAT 프로젝트 컴파일 결과
/// </summary>
public class CompilationResult
{
    /// <summary>
    /// 컴파일 성공 여부
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// 컴파일 오류 목록
    /// </summary>
    public List<CompilationError> Errors { get; init; } = new();

    /// <summary>
    /// 컴파일 경고 목록
    /// </summary>
    public List<CompilationWarning> Warnings { get; init; } = new();

    /// <summary>
    /// 컴파일 시작 시간
    /// </summary>
    public DateTime StartTime { get; init; }

    /// <summary>
    /// 컴파일 종료 시간
    /// </summary>
    public DateTime EndTime { get; init; }

    /// <summary>
    /// 컴파일 소요 시간
    /// </summary>
    public TimeSpan Duration => EndTime - StartTime;

    /// <summary>
    /// 컴파일된 프로젝트 경로
    /// </summary>
    public string ProjectPath { get; init; } = string.Empty;

    /// <summary>
    /// 빌드 구성 (Debug, Release 등)
    /// </summary>
    public string BuildConfiguration { get; init; } = "Debug";

    /// <summary>
    /// 총 오류 개수
    /// </summary>
    public int ErrorCount => Errors.Count;

    /// <summary>
    /// 총 경고 개수
    /// </summary>
    public int WarningCount => Warnings.Count;
}

/// <summary>
/// 컴파일 오류 정보
/// </summary>
public class CompilationError
{
    /// <summary>
    /// 오류 코드 (예: C0001)
    /// </summary>
    public string ErrorCode { get; init; } = string.Empty;

    /// <summary>
    /// 오류 메시지
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// 오류가 발생한 파일 경로
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// 오류가 발생한 라인 번호
    /// </summary>
    public int LineNumber { get; init; }

    /// <summary>
    /// 오류가 발생한 컬럼 번호
    /// </summary>
    public int ColumnNumber { get; init; }

    /// <summary>
    /// 오류 심각도 (Error, Critical 등)
    /// </summary>
    public ErrorSeverity Severity { get; init; } = ErrorSeverity.Error;

    /// <summary>
    /// POU 이름 (해당되는 경우)
    /// </summary>
    public string? PouName { get; init; }
}

/// <summary>
/// 컴파일 경고 정보
/// </summary>
public class CompilationWarning
{
    /// <summary>
    /// 경고 코드 (예: W0001)
    /// </summary>
    public string WarningCode { get; init; } = string.Empty;

    /// <summary>
    /// 경고 메시지
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// 경고가 발생한 파일 경로
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// 경고가 발생한 라인 번호
    /// </summary>
    public int LineNumber { get; init; }

    /// <summary>
    /// 경고가 발생한 컬럼 번호
    /// </summary>
    public int ColumnNumber { get; init; }

    /// <summary>
    /// 경고 카테고리 (사용되지 않는 변수, 타입 불일치 등)
    /// </summary>
    public WarningCategory Category { get; init; } = WarningCategory.General;

    /// <summary>
    /// POU 이름 (해당되는 경우)
    /// </summary>
    public string? PouName { get; init; }
}

/// <summary>
/// 오류 심각도
/// </summary>
public enum ErrorSeverity
{
    /// <summary>일반 오류</summary>
    Error,

    /// <summary>치명적 오류</summary>
    Critical,

    /// <summary>구문 오류</summary>
    SyntaxError,

    /// <summary>타입 오류</summary>
    TypeError,

    /// <summary>링커 오류</summary>
    LinkerError
}

/// <summary>
/// 경고 카테고리
/// </summary>
public enum WarningCategory
{
    /// <summary>일반 경고</summary>
    General,

    /// <summary>사용되지 않는 변수</summary>
    UnusedVariable,

    /// <summary>초기화되지 않은 변수</summary>
    UninitializedVariable,

    /// <summary>타입 변환 경고</summary>
    TypeConversion,

    /// <summary>사용되지 않는 함수</summary>
    UnusedFunction,

    /// <summary>Dead Code 경고</summary>
    DeadCode,

    /// <summary>성능 경고</summary>
    Performance
}
