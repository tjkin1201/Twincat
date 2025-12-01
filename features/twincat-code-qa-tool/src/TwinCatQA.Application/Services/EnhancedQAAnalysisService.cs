using Microsoft.Extensions.Logging;
using TwinCatQA.Domain.Models.QA;
using TwinCatQA.Infrastructure.Analysis.Confidence;
using TwinCatQA.Infrastructure.Configuration;

namespace TwinCatQA.Application.Services;

/// <summary>
/// Level 2 AST 기반 QA 분석 서비스
/// 기존 QAIssue를 EnhancedQAIssue로 변환하고 신뢰도를 계산합니다.
/// </summary>
public class EnhancedQAAnalysisService : IEnhancedQAAnalysisService
{
    private readonly ILogger<EnhancedQAAnalysisService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ConfidenceCalculator _confidenceCalculator;
    private readonly QAConfigurationLoader _configurationLoader;
    private QAConfiguration? _configuration;
    private SuppressionChecker? _suppressionChecker;

    public EnhancedQAAnalysisService(
        ILogger<EnhancedQAAnalysisService> logger,
        ILoggerFactory loggerFactory,
        ConfidenceCalculator confidenceCalculator,
        QAConfigurationLoader configurationLoader)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _confidenceCalculator = confidenceCalculator ?? throw new ArgumentNullException(nameof(confidenceCalculator));
        _configurationLoader = configurationLoader ?? throw new ArgumentNullException(nameof(configurationLoader));
    }

    /// <summary>
    /// 설정 파일을 로드하고 억제 체커를 초기화합니다.
    /// </summary>
    /// <param name="projectPath">프로젝트 루트 경로</param>
    public async Task LoadConfigurationAsync(string projectPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
        {
            _logger.LogWarning("프로젝트 경로가 지정되지 않아 기본 설정을 사용합니다");
            _configuration = QAConfigurationLoader.GetDefaultConfiguration();
        }
        else
        {
            _logger.LogInformation("설정 파일을 로드합니다: {ProjectPath}", projectPath);
            _configuration = await _configurationLoader.LoadConfigurationAsync(projectPath);
        }

        // 억제 체커 초기화
        _suppressionChecker = new SuppressionChecker(
            _loggerFactory.CreateLogger<SuppressionChecker>(),
            _configuration.InlineSuppressions);

        _logger.LogInformation("설정 로드 완료 - 프로젝트: {ProjectName}",
            _configuration?.ProjectName ?? "Unknown");
    }

    /// <summary>
    /// 기존 QAIssue 목록을 EnhancedQAIssue로 변환하고 신뢰도를 계산합니다.
    /// </summary>
    /// <param name="issues">기존 QA 이슈 목록</param>
    /// <param name="sourceCode">분석 대상 소스 코드</param>
    /// <param name="filePath">파일 경로</param>
    /// <returns>향상된 QA 이슈 목록</returns>
    public List<EnhancedQAIssue> EnhanceIssues(List<QAIssue> issues, string sourceCode, string filePath)
    {
        if (issues == null || issues.Count == 0)
        {
            _logger.LogDebug("향상시킬 이슈가 없습니다");
            return new List<EnhancedQAIssue>();
        }

        _logger.LogInformation("이슈 향상 시작: {Count}개 이슈 처리 중 (파일: {FilePath})",
            issues.Count, filePath);

        var enhancedIssues = new List<EnhancedQAIssue>();

        foreach (var issue in issues)
        {
            try
            {
                var enhanced = EnhanceSingleIssue(issue, sourceCode, filePath);
                enhancedIssues.Add(enhanced);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "이슈 향상 중 오류 발생: {RuleId} at {Location}",
                    issue.RuleId, issue.Location);

                // 오류가 발생해도 기본 EnhancedQAIssue로 변환하여 추가
                var fallbackIssue = EnhancedQAIssue.FromQAIssue(issue);
                fallbackIssue.ConfidenceScore = 50;
                fallbackIssue.Confidence = ConfidenceLevel.Low;
                fallbackIssue.ConfidenceReasons.Add($"분석 중 오류 발생: {ex.Message}");
                enhancedIssues.Add(fallbackIssue);
            }
        }

        _logger.LogInformation("이슈 향상 완료: {Count}개 이슈 처리됨", enhancedIssues.Count);

        return enhancedIssues;
    }

    /// <summary>
    /// 단일 이슈를 향상시킵니다.
    /// </summary>
    private EnhancedQAIssue EnhanceSingleIssue(QAIssue issue, string sourceCode, string filePath)
    {
        // 기본 EnhancedQAIssue 생성
        var enhanced = EnhancedQAIssue.FromQAIssue(issue);

        // AST 분석 결과 생성 (규칙 타입에 따라 기본값 사용)
        var astResult = _confidenceCalculator.CreateDefaultASTResult(issue.RuleId, issue);

        // 신뢰도 계산
        var confidenceResult = _confidenceCalculator.Calculate(astResult, issue);

        // 신뢰도 정보 설정
        enhanced.ConfidenceScore = confidenceResult.Score;
        enhanced.Confidence = confidenceResult.Level;
        enhanced.ConfidenceReasons = confidenceResult.Reasons;

        // 분석 레벨 설정 (Level 2: AST 분석)
        enhanced.AnalysisLevel = 2;

        // AST 컨텍스트 설정
        enhanced.ASTContext = CreateASTAnalysisContext(astResult, issue);

        // 억제 여부 확인
        if (_suppressionChecker != null && !string.IsNullOrWhiteSpace(sourceCode))
        {
            CheckSuppression(enhanced, sourceCode);
        }

        _logger.LogDebug("이슈 향상 완료: {RuleId} - 신뢰도 {Score}% ({Level})",
            issue.RuleId, enhanced.ConfidenceScore, enhanced.Confidence);

        return enhanced;
    }

    /// <summary>
    /// AST 분석 결과를 기반으로 ASTAnalysisContext를 생성합니다.
    /// </summary>
    private ASTAnalysisContext CreateASTAnalysisContext(ASTAnalysisResult astResult, QAIssue issue)
    {
        return new ASTAnalysisContext
        {
            IsConfirmedByAST = astResult.IsConfirmedByAST,
            DataFlowConfirmed = astResult.DataFlowConfirmed,
            SimilarOccurrences = astResult.SimilarOccurrences,
            AmbiguousContext = astResult.AmbiguousContext,
            PossibleExternalReference = astResult.PossibleExternalReference,
            IsIOVariable = astResult.IsInputOutputVariable,
            IsGlobalVariable = astResult.IsGlobalVariable,
            ASTNodeType = astResult.AdditionalContext.ContainsKey("NodeType")
                ? astResult.AdditionalContext["NodeType"]?.ToString()
                : null,
            ParentNodeType = astResult.AdditionalContext.ContainsKey("ParentNodeType")
                ? astResult.AdditionalContext["ParentNodeType"]?.ToString()
                : null,
            VariableScope = astResult.AdditionalContext.ContainsKey("VariableScope")
                ? astResult.AdditionalContext["VariableScope"]?.ToString()
                : null,
            AdditionalData = astResult.AdditionalContext.Count > 0
                ? new Dictionary<string, object>(astResult.AdditionalContext)
                : null
        };
    }

    /// <summary>
    /// 억제(Suppression) 여부를 확인합니다.
    /// </summary>
    private void CheckSuppression(EnhancedQAIssue issue, string sourceCode)
    {
        if (_suppressionChecker == null)
        {
            return;
        }

        try
        {
            // 파일 수준 억제 확인
            var fileSuppressions = _suppressionChecker.GetFileLevelSuppressions(sourceCode);
            if (fileSuppressions.Contains(issue.RuleId))
            {
                issue.IsSuppressed = true;
                issue.SuppressionReason = "파일 수준에서 억제됨";
                issue.SuppressionSource = "file";
                _logger.LogDebug("파일 수준 억제 적용: {RuleId} in {FilePath}",
                    issue.RuleId, issue.FilePath);
                return;
            }

            // 줄 수준 억제 확인
            if (issue.Line > 0)
            {
                var lineSuppressions = _suppressionChecker.GetLineSuppressions(issue.Line, sourceCode);
                if (lineSuppressions.Contains(issue.RuleId))
                {
                    issue.IsSuppressed = true;
                    issue.SuppressionReason = $"줄 {issue.Line}에서 억제됨";
                    issue.SuppressionSource = "inline";
                    _logger.LogDebug("인라인 억제 적용: {RuleId} at line {Line}",
                        issue.RuleId, issue.Line);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "억제 확인 중 오류 발생: {RuleId} at {FilePath}:{Line}",
                issue.RuleId, issue.FilePath, issue.Line);
        }
    }

    /// <summary>
    /// 신뢰도 레벨에 따라 이슈를 필터링합니다.
    /// </summary>
    /// <param name="issues">향상된 QA 이슈 목록</param>
    /// <param name="minLevel">최소 신뢰도 레벨</param>
    /// <returns>필터링된 이슈 목록</returns>
    public List<EnhancedQAIssue> FilterByConfidence(List<EnhancedQAIssue> issues, ConfidenceLevel minLevel)
    {
        if (issues == null || issues.Count == 0)
        {
            return new List<EnhancedQAIssue>();
        }

        var filtered = issues.Where(i => i.Confidence >= minLevel).ToList();

        _logger.LogInformation("신뢰도 필터링 완료: {Original}개 -> {Filtered}개 (최소 레벨: {MinLevel})",
            issues.Count, filtered.Count, minLevel);

        return filtered;
    }

    /// <summary>
    /// 억제된 이슈를 제외합니다.
    /// </summary>
    /// <param name="issues">향상된 QA 이슈 목록</param>
    /// <returns>억제되지 않은 이슈 목록</returns>
    public List<EnhancedQAIssue> ExcludeSuppressed(List<EnhancedQAIssue> issues)
    {
        if (issues == null || issues.Count == 0)
        {
            return new List<EnhancedQAIssue>();
        }

        var nonSuppressed = issues.Where(i => !i.IsSuppressed).ToList();
        var suppressedCount = issues.Count - nonSuppressed.Count;

        if (suppressedCount > 0)
        {
            _logger.LogInformation("억제된 이슈 제외: {Suppressed}개 억제됨, {Remaining}개 남음",
                suppressedCount, nonSuppressed.Count);
        }

        return nonSuppressed;
    }

    /// <summary>
    /// 설정 파일에 정의된 규칙 오버라이드를 적용합니다.
    /// </summary>
    public List<EnhancedQAIssue> ApplyRuleOverrides(List<EnhancedQAIssue> issues)
    {
        if (issues == null || issues.Count == 0 || _configuration == null)
        {
            return issues;
        }

        var result = new List<EnhancedQAIssue>();

        foreach (var issue in issues)
        {
            // 전역 규칙 제외 확인
            if (_configuration.GlobalExclusions.Rules.Contains(issue.RuleId))
            {
                _logger.LogDebug("전역 제외 규칙으로 이슈 제외: {RuleId}", issue.RuleId);
                continue;
            }

            // 규칙별 오버라이드 적용
            if (_configuration.RuleOverrides.TryGetValue(issue.RuleId, out var ruleOverride))
            {
                if (!ruleOverride.Enabled)
                {
                    _logger.LogDebug("비활성화된 규칙으로 이슈 제외: {RuleId}", issue.RuleId);
                    continue;
                }

                // 심각도 오버라이드 적용
                if (!string.IsNullOrWhiteSpace(ruleOverride.Severity))
                {
                    if (Enum.TryParse<Severity>(ruleOverride.Severity, true, out var newSeverity))
                    {
                        if (issue.Severity != newSeverity)
                        {
                            _logger.LogDebug("심각도 오버라이드 적용: {RuleId} {OldSeverity} -> {NewSeverity}",
                                issue.RuleId, issue.Severity, newSeverity);

                            // 새 인스턴스 생성 (init 속성이므로)
                            var modifiedIssue = CloneWithModifiedSeverity(issue, newSeverity);
                            result.Add(modifiedIssue);
                            continue;
                        }
                    }
                }
            }

            result.Add(issue);
        }

        var excludedCount = issues.Count - result.Count;
        if (excludedCount > 0)
        {
            _logger.LogInformation("규칙 오버라이드 적용 완료: {Excluded}개 제외됨, {Remaining}개 남음",
                excludedCount, result.Count);
        }

        return result;
    }

    /// <summary>
    /// 심각도가 수정된 이슈의 복제본을 생성합니다.
    /// </summary>
    private EnhancedQAIssue CloneWithModifiedSeverity(EnhancedQAIssue original, Severity newSeverity)
    {
        // 기본 QAIssue 속성은 init이므로 새로 생성해야 함
        var baseIssue = new QAIssue
        {
            Severity = newSeverity, // 수정된 심각도
            Category = original.Category,
            Title = original.Title,
            Description = original.Description,
            Location = original.Location,
            FilePath = original.FilePath,
            Line = original.Line,
            WhyDangerous = original.WhyDangerous,
            Recommendation = original.Recommendation,
            Examples = original.Examples,
            RuleId = original.RuleId,
            OldCodeSnippet = original.OldCodeSnippet,
            NewCodeSnippet = original.NewCodeSnippet
        };

        var modified = EnhancedQAIssue.FromQAIssue(baseIssue);

        // EnhancedQAIssue 속성 복사
        modified.Confidence = original.Confidence;
        modified.ConfidenceScore = original.ConfidenceScore;
        modified.ConfidenceReasons = new List<string>(original.ConfidenceReasons);
        modified.AnalysisLevel = original.AnalysisLevel;
        modified.ASTContext = original.ASTContext;
        modified.AIContext = original.AIContext;
        modified.IsSuppressed = original.IsSuppressed;
        modified.SuppressionReason = original.SuppressionReason;
        modified.SuppressionSource = original.SuppressionSource;
        modified.RelatedIssueIds = original.RelatedIssueIds != null
            ? new List<string>(original.RelatedIssueIds)
            : null;
        modified.IssueId = original.IssueId;
        modified.Feedback = original.Feedback;
        modified.RiskScore = original.RiskScore;

        return modified;
    }

    /// <summary>
    /// 향상된 이슈 목록의 통계를 생성합니다.
    /// </summary>
    public EnhancedIssueStatistics GetStatistics(List<EnhancedQAIssue> issues)
    {
        if (issues == null || issues.Count == 0)
        {
            return new EnhancedIssueStatistics();
        }

        return new EnhancedIssueStatistics
        {
            TotalIssues = issues.Count,
            HighConfidenceCount = issues.Count(i => i.Confidence == ConfidenceLevel.High),
            MediumConfidenceCount = issues.Count(i => i.Confidence == ConfidenceLevel.Medium),
            LowConfidenceCount = issues.Count(i => i.Confidence == ConfidenceLevel.Low),
            SuppressedCount = issues.Count(i => i.IsSuppressed),
            AverageConfidenceScore = issues.Any() ? (int)issues.Average(i => i.ConfidenceScore) : 0,
            Level2AnalyzedCount = issues.Count(i => i.AnalysisLevel >= 2),
            Level3AnalyzedCount = issues.Count(i => i.AnalysisLevel >= 3)
        };
    }
}

/// <summary>
/// EnhancedQAAnalysisService 인터페이스
/// </summary>
public interface IEnhancedQAAnalysisService
{
    /// <summary>
    /// 기존 QAIssue 목록을 EnhancedQAIssue로 변환합니다.
    /// 참고: 실제 AST 파싱이 아닌 휴리스틱 기반 신뢰도 계산을 수행합니다.
    /// </summary>
    List<EnhancedQAIssue> EnhanceIssues(List<QAIssue> issues, string sourceCode, string filePath);

    /// <summary>
    /// 신뢰도 레벨에 따라 이슈를 필터링합니다.
    /// </summary>
    List<EnhancedQAIssue> FilterByConfidence(List<EnhancedQAIssue> issues, ConfidenceLevel minLevel);

    /// <summary>
    /// 억제된 이슈를 제외합니다.
    /// </summary>
    List<EnhancedQAIssue> ExcludeSuppressed(List<EnhancedQAIssue> issues);

    /// <summary>
    /// 설정 파일을 로드합니다.
    /// </summary>
    Task LoadConfigurationAsync(string projectPath);

    /// <summary>
    /// 향상된 이슈 목록의 통계를 생성합니다.
    /// </summary>
    EnhancedIssueStatistics GetStatistics(List<EnhancedQAIssue> issues);
}

/// <summary>
/// 향상된 이슈 통계 정보
/// </summary>
public class EnhancedIssueStatistics
{
    /// <summary>
    /// 전체 이슈 수
    /// </summary>
    public int TotalIssues { get; set; }

    /// <summary>
    /// 높은 신뢰도 이슈 수
    /// </summary>
    public int HighConfidenceCount { get; set; }

    /// <summary>
    /// 중간 신뢰도 이슈 수
    /// </summary>
    public int MediumConfidenceCount { get; set; }

    /// <summary>
    /// 낮은 신뢰도 이슈 수
    /// </summary>
    public int LowConfidenceCount { get; set; }

    /// <summary>
    /// 억제된 이슈 수
    /// </summary>
    public int SuppressedCount { get; set; }

    /// <summary>
    /// 평균 신뢰도 점수
    /// </summary>
    public int AverageConfidenceScore { get; set; }

    /// <summary>
    /// Level 2 분석된 이슈 수
    /// </summary>
    public int Level2AnalyzedCount { get; set; }

    /// <summary>
    /// Level 3 분석된 이슈 수
    /// </summary>
    public int Level3AnalyzedCount { get; set; }

    /// <summary>
    /// 높은 신뢰도 비율
    /// </summary>
    public double HighConfidencePercentage => TotalIssues > 0
        ? (double)HighConfidenceCount / TotalIssues * 100
        : 0;

    /// <summary>
    /// 중간 이상 신뢰도 비율
    /// </summary>
    public double MediumOrHighPercentage => TotalIssues > 0
        ? (double)(HighConfidenceCount + MediumConfidenceCount) / TotalIssues * 100
        : 0;

    /// <summary>
    /// 억제 비율
    /// </summary>
    public double SuppressedPercentage => TotalIssues > 0
        ? (double)SuppressedCount / TotalIssues * 100
        : 0;

    public override string ToString()
    {
        return $"총 {TotalIssues}개 이슈 - High: {HighConfidenceCount}, " +
               $"Medium: {MediumConfidenceCount}, Low: {LowConfidenceCount}, " +
               $"억제됨: {SuppressedCount} (평균 신뢰도: {AverageConfidenceScore}점)";
    }
}
