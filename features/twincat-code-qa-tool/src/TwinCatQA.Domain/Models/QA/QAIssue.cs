namespace TwinCatQA.Domain.Models.QA;

/// <summary>
/// QA 검증 이슈
/// </summary>
public class QAIssue
{
    /// <summary>
    /// 심각도
    /// </summary>
    public Severity Severity { get; init; }

    /// <summary>
    /// 카테고리 (타입 안전성, 초기화 누락 등)
    /// </summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>
    /// 이슈 제목
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// 상세 설명
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// 위치 (파일경로:라인번호)
    /// </summary>
    public string Location { get; init; } = string.Empty;

    /// <summary>
    /// 파일 경로
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// 라인 번호
    /// </summary>
    public int Line { get; init; }

    /// <summary>
    /// 왜 위험한지 설명
    /// </summary>
    public string WhyDangerous { get; init; } = string.Empty;

    /// <summary>
    /// 권장 해결 방법
    /// </summary>
    public string Recommendation { get; init; } = string.Empty;

    /// <summary>
    /// 예시 코드
    /// </summary>
    public List<string> Examples { get; init; } = new();

    /// <summary>
    /// 규칙 ID
    /// </summary>
    public string RuleId { get; init; } = string.Empty;

    /// <summary>
    /// 변경 전 코드 스니펫
    /// </summary>
    public string OldCodeSnippet { get; init; } = string.Empty;

    /// <summary>
    /// 변경 후 코드 스니펫
    /// </summary>
    public string NewCodeSnippet { get; init; } = string.Empty;
}

/// <summary>
/// QA 이슈 심각도
/// </summary>
public enum Severity
{
    /// <summary>
    /// 정보성 (개선 권장)
    /// </summary>
    Info,

    /// <summary>
    /// 경고 (잠재적 버그)
    /// </summary>
    Warning,

    /// <summary>
    /// 심각 (즉시 수정 필요)
    /// </summary>
    Critical
}
