namespace TwinCatQA.Domain.Models.QA;

/// <summary>
/// QA 분석 보고서
/// </summary>
public class QAReport
{
    /// <summary>보고서 생성 시각</summary>
    public DateTime GeneratedAt { get; init; }

    /// <summary>원본 폴더 경로</summary>
    public string SourceFolder { get; init; } = string.Empty;

    /// <summary>대상 폴더 경로</summary>
    public string TargetFolder { get; init; } = string.Empty;

    /// <summary>전체 변경사항 개수</summary>
    public int TotalChanges { get; init; }

    /// <summary>검출된 모든 이슈</summary>
    public List<QAIssue> Issues { get; init; } = new();

    /// <summary>심각 이슈 개수</summary>
    public int CriticalCount { get; init; }

    /// <summary>경고 이슈 개수</summary>
    public int WarningCount { get; init; }

    /// <summary>정보 이슈 개수</summary>
    public int InfoCount { get; init; }

    /// <summary>심각 이슈가 있는지 여부</summary>
    public bool HasCriticalIssues => CriticalCount > 0;

    /// <summary>전체 이슈 개수</summary>
    public int TotalIssues => Issues.Count;

    /// <summary>
    /// 파일별 이슈 그룹화
    /// </summary>
    public Dictionary<string, List<QAIssue>> IssuesByFile =>
        Issues.GroupBy(i => i.FilePath)
              .ToDictionary(g => g.Key, g => g.ToList());

    /// <summary>
    /// 규칙별 이슈 그룹화
    /// </summary>
    public Dictionary<string, List<QAIssue>> IssuesByRule =>
        Issues.GroupBy(i => i.RuleId)
              .ToDictionary(g => g.Key, g => g.ToList());

    /// <summary>
    /// 심각도별 이슈 그룹화
    /// </summary>
    public Dictionary<Severity, List<QAIssue>> IssuesBySeverity =>
        Issues.GroupBy(i => i.Severity)
              .ToDictionary(g => g.Key, g => g.ToList());

    /// <summary>
    /// 카테고리별 이슈 그룹화
    /// </summary>
    public Dictionary<string, List<QAIssue>> IssuesByCategory =>
        Issues.GroupBy(i => i.Category)
              .ToDictionary(g => g.Key, g => g.ToList());

    /// <summary>
    /// 요약 정보 생성
    /// </summary>
    public string GetSummary()
    {
        return $@"QA 분석 보고서
================
생성 시각: {GeneratedAt:yyyy-MM-dd HH:mm:ss}
원본 폴더: {SourceFolder}
대상 폴더: {TargetFolder}

변경 사항: {TotalChanges}개
검출 이슈: {TotalIssues}개
  - 심각 (Critical): {CriticalCount}개
  - 경고 (Warning): {WarningCount}개
  - 정보 (Info): {InfoCount}개

상태: {(HasCriticalIssues ? "⚠️ 심각한 이슈가 발견되었습니다!" : "✅ 심각한 이슈가 없습니다.")}";
    }
}
