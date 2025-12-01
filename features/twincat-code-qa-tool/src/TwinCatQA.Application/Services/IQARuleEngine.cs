using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Models.QA;
using TwinCatQA.Domain.Services;

namespace TwinCatQA.Application.Services;

/// <summary>
/// QA 규칙 엔진 인터페이스
/// 모든 QA 규칙을 통합 관리하고 실행
/// </summary>
public interface IQARuleEngine
{
    /// <summary>
    /// 등록된 모든 규칙 목록
    /// </summary>
    IReadOnlyList<IQARuleChecker> Rules { get; }

    /// <summary>
    /// 변수 변경 분석
    /// </summary>
    List<QAIssue> AnalyzeVariableChanges(IEnumerable<VariableChange> changes);

    /// <summary>
    /// 로직 변경 분석
    /// </summary>
    List<QAIssue> AnalyzeLogicChanges(IEnumerable<LogicChange> changes);

    /// <summary>
    /// 데이터 타입 변경 분석
    /// </summary>
    List<QAIssue> AnalyzeDataTypeChanges(IEnumerable<DataTypeChange> changes);

    /// <summary>
    /// 전체 비교 결과 분석
    /// </summary>
    QAReport AnalyzeComparisonResult(FolderComparisonResult comparison);

    /// <summary>
    /// 특정 심각도 이상의 이슈만 필터링
    /// </summary>
    List<QAIssue> FilterBySeverity(List<QAIssue> issues, Severity minSeverity);

    /// <summary>
    /// 규칙 활성화/비활성화
    /// </summary>
    void SetRuleEnabled(string ruleId, bool enabled);

    /// <summary>
    /// 규칙 활성화 상태 확인
    /// </summary>
    bool IsRuleEnabled(string ruleId);

    /// <summary>
    /// 모든 규칙 정보 조회
    /// </summary>
    List<RuleInfo> GetAllRulesInfo();
}

/// <summary>
/// 규칙 정보
/// </summary>
public class RuleInfo
{
    public string RuleId { get; init; } = string.Empty;
    public string RuleName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Severity Severity { get; init; }
    public bool IsEnabled { get; init; }
}
