using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Models.QA;
using TwinCatQA.Domain.Services;
using TwinCatQA.Infrastructure.Comparison;

namespace TwinCatQA.Application.Services;

/// <summary>
/// QA 분석 오케스트레이션 서비스
/// 폴더 비교 + QA 규칙 검사를 통합 수행
/// </summary>
public class QaAnalysisService
{
    private readonly IEnumerable<IQARuleChecker> _ruleCheckers;

    public QaAnalysisService(IEnumerable<IQARuleChecker> ruleCheckers)
    {
        _ruleCheckers = ruleCheckers;
    }

    /// <summary>
    /// 전체 QA 분석 수행
    /// </summary>
    public async Task<QaAnalysisResult> AnalyzeAsync(
        string oldFolderPath,
        string newFolderPath,
        QaAnalysisOptions options)
    {
        var result = new QaAnalysisResult
        {
            OldFolderPath = oldFolderPath,
            NewFolderPath = newFolderPath,
            StartTime = DateTime.Now
        };

        try
        {
            // 1단계: 폴더 비교
            var comparer = new FolderComparer();
            var compareOptions = new CompareOptions
            {
                IncludeVariables = true,
                IncludeIOMapping = true,
                IncludeLogic = true,
                IncludeDataTypes = true
            };

            result.ComparisonResult = await comparer.CompareAsync(
                oldFolderPath,
                newFolderPath,
                compareOptions);

            // 2단계: 규칙 필터링
            var activeRules = GetActiveRules(options);

            // 3단계: 변수 변경 QA 검사
            foreach (var change in result.ComparisonResult.VariableChanges)
            {
                foreach (var rule in activeRules)
                {
                    var issues = rule.CheckVariableChange(change);
                    result.Issues.AddRange(issues.Where(i => i.Severity >= options.MinSeverity));
                }
            }

            // 4단계: 로직 변경 QA 검사
            foreach (var change in result.ComparisonResult.LogicChanges)
            {
                foreach (var rule in activeRules)
                {
                    var issues = rule.CheckLogicChange(change);
                    result.Issues.AddRange(issues.Where(i => i.Severity >= options.MinSeverity));
                }
            }

            // 5단계: 데이터 타입 변경 QA 검사
            foreach (var change in result.ComparisonResult.DataTypeChanges)
            {
                foreach (var rule in activeRules)
                {
                    var issues = rule.CheckDataTypeChange(change);
                    result.Issues.AddRange(issues.Where(i => i.Severity >= options.MinSeverity));
                }
            }

            result.EndTime = DateTime.Now;
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.EndTime = DateTime.Now;
        }

        return result;
    }

    /// <summary>
    /// 활성화할 규칙 필터링
    /// </summary>
    private IEnumerable<IQARuleChecker> GetActiveRules(QaAnalysisOptions options)
    {
        var rules = _ruleCheckers.ToList();

        // 특정 규칙만 포함
        if (options.IncludeRules?.Any() == true)
        {
            rules = rules.Where(r => options.IncludeRules.Contains(r.RuleId)).ToList();
        }

        // 특정 규칙 제외
        if (options.ExcludeRules?.Any() == true)
        {
            rules = rules.Where(r => !options.ExcludeRules.Contains(r.RuleId)).ToList();
        }

        return rules;
    }
}

/// <summary>
/// QA 분석 옵션
/// </summary>
public class QaAnalysisOptions
{
    /// <summary>
    /// 최소 심각도 (이 수준 이상만 보고)
    /// </summary>
    public Severity MinSeverity { get; set; } = Severity.Info;

    /// <summary>
    /// 포함할 규칙 ID 목록 (null이면 전체)
    /// </summary>
    public List<string>? IncludeRules { get; set; }

    /// <summary>
    /// 제외할 규칙 ID 목록
    /// </summary>
    public List<string>? ExcludeRules { get; set; }

    /// <summary>
    /// 상세 출력 여부
    /// </summary>
    public bool Verbose { get; set; }
}

/// <summary>
/// QA 분석 결과
/// </summary>
public class QaAnalysisResult
{
    /// <summary>
    /// 이전 폴더 경로
    /// </summary>
    public string OldFolderPath { get; set; } = string.Empty;

    /// <summary>
    /// 신규 폴더 경로
    /// </summary>
    public string NewFolderPath { get; set; } = string.Empty;

    /// <summary>
    /// 폴더 비교 결과
    /// </summary>
    public FolderComparisonResult ComparisonResult { get; set; } = new();

    /// <summary>
    /// QA 이슈 목록
    /// </summary>
    public List<QAIssue> Issues { get; set; } = new();

    /// <summary>
    /// 분석 시작 시간
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 분석 종료 시간
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// 소요 시간
    /// </summary>
    public TimeSpan Duration => EndTime - StartTime;

    /// <summary>
    /// 성공 여부
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 에러 메시지 (실패 시)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Critical 이슈 개수
    /// </summary>
    public int CriticalCount => Issues.Count(i => i.Severity == Severity.Critical);

    /// <summary>
    /// Warning 이슈 개수
    /// </summary>
    public int WarningCount => Issues.Count(i => i.Severity == Severity.Warning);

    /// <summary>
    /// Info 이슈 개수
    /// </summary>
    public int InfoCount => Issues.Count(i => i.Severity == Severity.Info);
}
