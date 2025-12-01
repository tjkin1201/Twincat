using System.Collections.Concurrent;
using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Models.QA;
using TwinCatQA.Domain.Services;
using TwinCatQA.Infrastructure.QA.Rules;
using TwinCatQA.Infrastructure.QA.Rules.TE1200;

namespace TwinCatQA.Application.Services;

/// <summary>
/// QA 규칙 엔진 구현체
/// 모든 QA 규칙을 통합 관리하고 병렬 처리로 분석 수행
/// </summary>
public class QARuleEngine : IQARuleEngine
{
    private readonly List<IQARuleChecker> _rules = new();
    private readonly HashSet<string> _disabledRules = new();
    private readonly object _lockObject = new();

    public IReadOnlyList<IQARuleChecker> Rules => _rules.AsReadOnly();

    public QARuleEngine()
    {
        // 모든 규칙 등록
        RegisterAllRules();
    }

    /// <summary>
    /// 모든 QA 규칙 등록 (기존 20개 + TE1200 SA 180개 = 200개)
    /// </summary>
    private void RegisterAllRules()
    {
        // 기존 Critical 규칙 (QA001-QA005)
        _rules.Add(new TypeNarrowingRule());
        _rules.Add(new UninitializedVariableRule());
        _rules.Add(new ArrayBoundsRule());
        _rules.Add(new NullCheckRule());
        _rules.Add(new FloatingPointComparisonRule());

        // 기존 Warning 규칙 (QA006-QA015)
        _rules.Add(new UnusedVariableRule());
        _rules.Add(new MagicNumberRule());
        _rules.Add(new LongFunctionRule());
        _rules.Add(new DeepNestingRule());
        _rules.Add(new DuplicateCodeRule());
        _rules.Add(new MissingCaseElseRule());
        _rules.Add(new NamingConventionRule());
        _rules.Add(new GlobalVariableOveruseRule());
        _rules.Add(new HardcodedIOAddressRule());
        _rules.Add(new InfiniteLoopRiskRule());

        // 기존 Info 규칙 (QA016-QA020)
        _rules.Add(new InsufficientCommentsRule());
        _rules.Add(new HighComplexityRule());
        _rules.Add(new TooManyParametersRule());
        _rules.Add(new ExcessivelyLongNameRule());
        _rules.Add(new InconsistentStyleRule());

        // TE1200 Static Analysis 규칙 (SA0001-SA0180)
        // Beckhoff TE1200 호환 180개 SA 규칙 등록
        TE1200RuleRegistration.RegisterAllSARules(_rules);
    }

    /// <summary>
    /// 변수 변경 분석 (병렬 처리)
    /// </summary>
    public List<QAIssue> AnalyzeVariableChanges(IEnumerable<VariableChange> changes)
    {
        var changeList = changes.ToList();
        if (!changeList.Any()) return new List<QAIssue>();

        var issues = new ConcurrentBag<QAIssue>();
        var enabledRules = GetEnabledRules();

        // 병렬 처리로 모든 변경사항 분석
        Parallel.ForEach(changeList, change =>
        {
            foreach (var rule in enabledRules)
            {
                try
                {
                    var ruleIssues = rule.CheckVariableChange(change);
                    foreach (var issue in ruleIssues)
                    {
                        issues.Add(issue);
                    }
                }
                catch (Exception ex)
                {
                    // 개별 규칙 실패 시 로깅 (실제 환경에서는 로거 사용)
                    Console.WriteLine($"규칙 {rule.RuleId} 실행 중 오류: {ex.Message}");
                }
            }
        });

        return issues.OrderBy(i => i.Severity)
                     .ThenBy(i => i.FilePath)
                     .ThenBy(i => i.Line)
                     .ToList();
    }

    /// <summary>
    /// 로직 변경 분석 (병렬 처리)
    /// </summary>
    public List<QAIssue> AnalyzeLogicChanges(IEnumerable<LogicChange> changes)
    {
        var changeList = changes.ToList();
        if (!changeList.Any()) return new List<QAIssue>();

        var issues = new ConcurrentBag<QAIssue>();
        var enabledRules = GetEnabledRules();

        Parallel.ForEach(changeList, change =>
        {
            foreach (var rule in enabledRules)
            {
                try
                {
                    var ruleIssues = rule.CheckLogicChange(change);
                    foreach (var issue in ruleIssues)
                    {
                        issues.Add(issue);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"규칙 {rule.RuleId} 실행 중 오류: {ex.Message}");
                }
            }
        });

        return issues.OrderBy(i => i.Severity)
                     .ThenBy(i => i.FilePath)
                     .ThenBy(i => i.Line)
                     .ToList();
    }

    /// <summary>
    /// 데이터 타입 변경 분석 (병렬 처리)
    /// </summary>
    public List<QAIssue> AnalyzeDataTypeChanges(IEnumerable<DataTypeChange> changes)
    {
        var changeList = changes.ToList();
        if (!changeList.Any()) return new List<QAIssue>();

        var issues = new ConcurrentBag<QAIssue>();
        var enabledRules = GetEnabledRules();

        Parallel.ForEach(changeList, change =>
        {
            foreach (var rule in enabledRules)
            {
                try
                {
                    var ruleIssues = rule.CheckDataTypeChange(change);
                    foreach (var issue in ruleIssues)
                    {
                        issues.Add(issue);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"규칙 {rule.RuleId} 실행 중 오류: {ex.Message}");
                }
            }
        });

        return issues.OrderBy(i => i.Severity)
                     .ThenBy(i => i.FilePath)
                     .ThenBy(i => i.Line)
                     .ToList();
    }

    /// <summary>
    /// 전체 비교 결과 분석
    /// </summary>
    public QAReport AnalyzeComparisonResult(FolderComparisonResult comparison)
    {
        var allIssues = new List<QAIssue>();

        // 병렬로 모든 변경 타입 분석
        var tasks = new List<Task<List<QAIssue>>>
        {
            Task.Run(() => AnalyzeVariableChanges(comparison.VariableChanges)),
            Task.Run(() => AnalyzeLogicChanges(comparison.LogicChanges)),
            Task.Run(() => AnalyzeDataTypeChanges(comparison.DataTypeChanges))
        };

        Task.WaitAll(tasks.ToArray());

        foreach (var task in tasks)
        {
            allIssues.AddRange(task.Result);
        }

        // 중복 제거 (동일한 위치, 규칙의 이슈)
        var uniqueIssues = allIssues
            .GroupBy(i => new { i.FilePath, i.Line, i.RuleId })
            .Select(g => g.First())
            .ToList();

        return new QAReport
        {
            GeneratedAt = DateTime.UtcNow,
            SourceFolder = comparison.OldFolderPath,
            TargetFolder = comparison.NewFolderPath,
            TotalChanges = comparison.TotalChanges,
            Issues = uniqueIssues.OrderBy(i => i.Severity)
                                 .ThenBy(i => i.FilePath)
                                 .ThenBy(i => i.Line)
                                 .ToList(),
            CriticalCount = uniqueIssues.Count(i => i.Severity == Severity.Critical),
            WarningCount = uniqueIssues.Count(i => i.Severity == Severity.Warning),
            InfoCount = uniqueIssues.Count(i => i.Severity == Severity.Info)
        };
    }

    /// <summary>
    /// 특정 심각도 이상의 이슈만 필터링
    /// </summary>
    public List<QAIssue> FilterBySeverity(List<QAIssue> issues, Severity minSeverity)
    {
        return minSeverity switch
        {
            Severity.Critical => issues.Where(i => i.Severity == Severity.Critical).ToList(),
            Severity.Warning => issues.Where(i => i.Severity >= Severity.Warning).ToList(),
            Severity.Info => issues.ToList(), // 모든 이슈
            _ => issues.ToList()
        };
    }

    /// <summary>
    /// 규칙 활성화/비활성화
    /// </summary>
    public void SetRuleEnabled(string ruleId, bool enabled)
    {
        lock (_lockObject)
        {
            if (enabled)
            {
                _disabledRules.Remove(ruleId);
            }
            else
            {
                _disabledRules.Add(ruleId);
            }
        }
    }

    /// <summary>
    /// 규칙 활성화 상태 확인
    /// </summary>
    public bool IsRuleEnabled(string ruleId)
    {
        lock (_lockObject)
        {
            return !_disabledRules.Contains(ruleId);
        }
    }

    /// <summary>
    /// 모든 규칙 정보 조회
    /// </summary>
    public List<RuleInfo> GetAllRulesInfo()
    {
        lock (_lockObject)
        {
            return _rules.Select(rule => new RuleInfo
            {
                RuleId = rule.RuleId,
                RuleName = rule.RuleName,
                Description = rule.Description,
                Severity = rule.Severity,
                IsEnabled = !_disabledRules.Contains(rule.RuleId)
            }).OrderBy(r => r.RuleId)
              .ToList();
        }
    }

    /// <summary>
    /// 활성화된 규칙만 반환
    /// </summary>
    private List<IQARuleChecker> GetEnabledRules()
    {
        lock (_lockObject)
        {
            return _rules.Where(r => !_disabledRules.Contains(r.RuleId)).ToList();
        }
    }
}
