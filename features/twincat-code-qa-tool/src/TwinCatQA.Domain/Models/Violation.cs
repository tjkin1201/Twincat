namespace TwinCatQA.Domain.Models;

/// <summary>
/// 검증 규칙 위반 사항
/// </summary>
public class Violation
{
    /// <summary>
    /// 고유 식별자
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    #region 규칙 정보

    /// <summary>
    /// 규칙 ID (예: "FR-1-COMPLEXITY")
    /// </summary>
    public string RuleId { get; init; } = string.Empty;

    /// <summary>
    /// 규칙 이름 (예: "사이클로매틱 복잡도 초과")
    /// </summary>
    public string RuleName { get; init; } = string.Empty;

    /// <summary>
    /// 관련된 헌장 원칙
    /// </summary>
    public ConstitutionPrinciple RelatedPrinciple { get; init; }

    /// <summary>
    /// 위반 심각도
    /// </summary>
    public ViolationSeverity Severity { get; init; }

    #endregion

    #region 위치 정보

    /// <summary>
    /// 파일 경로
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// 라인 번호
    /// </summary>
    public int Line { get; init; }

    /// <summary>
    /// 컬럼 번호
    /// </summary>
    public int Column { get; init; }

    /// <summary>
    /// 위반 코드 스니펫 (컨텍스트 5줄)
    /// </summary>
    public string CodeSnippet { get; init; } = string.Empty;

    #endregion

    #region 설명 및 제안

    /// <summary>
    /// 한글 설명 메시지
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// 수정 제안 (선택적)
    /// </summary>
    public string? Suggestion { get; init; }

    /// <summary>
    /// 헌장 또는 문서 링크
    /// </summary>
    public string? DocumentationUrl { get; init; }

    #endregion

    #region 메타데이터

    /// <summary>
    /// 위반 감지 시각
    /// </summary>
    public DateTime DetectedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// 사용자가 억제한 위반 여부
    /// </summary>
    public bool IsSuppressed { get; set; }

    #endregion
}
