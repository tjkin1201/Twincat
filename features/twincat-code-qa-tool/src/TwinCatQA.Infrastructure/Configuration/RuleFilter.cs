using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Logging;
using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Models.QA;

namespace TwinCatQA.Infrastructure.Configuration;

/// <summary>
/// QA 설정을 기반으로 규칙과 이슈를 필터링하는 클래스
/// </summary>
public class RuleFilter
{
    private readonly ILogger<RuleFilter> _logger;
    private readonly QAConfiguration _configuration;
    private readonly SuppressionChecker _suppressionChecker;
    private readonly Dictionary<string, Matcher> _compiledMatchers;

    public RuleFilter(
        ILogger<RuleFilter> logger,
        QAConfiguration configuration,
        SuppressionChecker suppressionChecker)
    {
        _logger = logger;
        _configuration = configuration;
        _suppressionChecker = suppressionChecker;
        _compiledMatchers = new Dictionary<string, Matcher>();

        // Glob 패턴 미리 컴파일
        CompileGlobPatterns();
    }

    /// <summary>
    /// 모든 Glob 패턴을 미리 컴파일
    /// </summary>
    private void CompileGlobPatterns()
    {
        var allPatterns = new HashSet<string>();

        // 전역 제외 파일 패턴
        allPatterns.UnionWith(_configuration.GlobalExclusions.Files);
        allPatterns.UnionWith(_configuration.GlobalExclusions.Directories);

        // 파일 오버라이드 패턴
        allPatterns.UnionWith(_configuration.FileOverrides.Keys);

        // 규칙 오버라이드의 파일 패턴
        foreach (var ruleOverride in _configuration.RuleOverrides.Values)
        {
            if (ruleOverride.FilePatterns != null)
            {
                allPatterns.UnionWith(ruleOverride.FilePatterns);
            }
            if (ruleOverride.ExcludePatterns != null)
            {
                allPatterns.UnionWith(ruleOverride.ExcludePatterns);
            }
        }

        // 패턴 컴파일
        foreach (var pattern in allPatterns)
        {
            try
            {
                var matcher = new Matcher();
                matcher.AddInclude(pattern);
                _compiledMatchers[pattern] = matcher;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Glob 패턴 컴파일 실패: {Pattern}", pattern);
            }
        }

        _logger.LogDebug("{Count}개의 Glob 패턴을 컴파일했습니다", _compiledMatchers.Count);
    }

    /// <summary>
    /// 파일이 전역 제외 대상인지 확인
    /// </summary>
    /// <param name="filePath">확인할 파일 경로</param>
    /// <returns>제외 대상이면 true</returns>
    public bool IsFileGloballyExcluded(string filePath)
    {
        var normalizedPath = NormalizePath(filePath);

        // 파일 패턴 확인
        foreach (var pattern in _configuration.GlobalExclusions.Files)
        {
            if (_compiledMatchers.TryGetValue(pattern, out var matcher) &&
                matcher.Match(normalizedPath).HasMatches)
            {
                _logger.LogDebug("파일 {FilePath}가 전역 제외 패턴 {Pattern}과 일치합니다",
                    filePath, pattern);
                return true;
            }
        }

        // 디렉토리 패턴 확인
        foreach (var pattern in _configuration.GlobalExclusions.Directories)
        {
            if (_compiledMatchers.TryGetValue(pattern, out var matcher) &&
                matcher.Match(normalizedPath).HasMatches)
            {
                _logger.LogDebug("파일 {FilePath}가 전역 제외 디렉토리 패턴 {Pattern}과 일치합니다",
                    filePath, pattern);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 규칙이 전역적으로 비활성화되었는지 확인
    /// </summary>
    /// <param name="ruleId">확인할 규칙 ID</param>
    /// <returns>비활성화되었으면 true</returns>
    public bool IsRuleGloballyDisabled(string ruleId)
    {
        var isDisabled = _configuration.GlobalExclusions.Rules
            .Contains(ruleId, StringComparer.OrdinalIgnoreCase);

        if (isDisabled)
        {
            _logger.LogDebug("규칙 {RuleId}가 전역적으로 비활성화되었습니다", ruleId);
        }

        return isDisabled;
    }

    /// <summary>
    /// 특정 파일에 대한 규칙 활성화 여부 확인
    /// </summary>
    /// <param name="ruleId">규칙 ID</param>
    /// <param name="filePath">파일 경로</param>
    /// <returns>활성화되어 있으면 true</returns>
    public bool IsRuleEnabledForFile(string ruleId, string filePath)
    {
        // 전역 비활성화 확인
        if (IsRuleGloballyDisabled(ruleId))
        {
            return false;
        }

        var normalizedPath = NormalizePath(filePath);

        // 규칙 오버라이드 확인
        if (_configuration.RuleOverrides.TryGetValue(ruleId, out var ruleOverride))
        {
            // 명시적으로 비활성화된 경우
            if (!ruleOverride.Enabled)
            {
                _logger.LogDebug("규칙 {RuleId}가 오버라이드로 비활성화되었습니다", ruleId);
                return false;
            }

            // 제외 패턴 확인
            if (ruleOverride.ExcludePatterns != null)
            {
                foreach (var pattern in ruleOverride.ExcludePatterns)
                {
                    if (_compiledMatchers.TryGetValue(pattern, out var matcher) &&
                        matcher.Match(normalizedPath).HasMatches)
                    {
                        _logger.LogDebug("파일 {FilePath}가 규칙 {RuleId}의 제외 패턴과 일치합니다",
                            filePath, ruleId);
                        return false;
                    }
                }
            }

            // 파일 패턴이 지정된 경우, 일치하는 파일만 활성화
            if (ruleOverride.FilePatterns != null && ruleOverride.FilePatterns.Count > 0)
            {
                var matchesPattern = ruleOverride.FilePatterns.Any(pattern =>
                    _compiledMatchers.TryGetValue(pattern, out var matcher) &&
                    matcher.Match(normalizedPath).HasMatches);

                if (!matchesPattern)
                {
                    _logger.LogDebug("파일 {FilePath}가 규칙 {RuleId}의 파일 패턴과 일치하지 않습니다",
                        filePath, ruleId);
                    return false;
                }
            }
        }

        // 파일 오버라이드 확인
        var fileOverride = GetFileOverride(normalizedPath);
        if (fileOverride != null)
        {
            // 명시적으로 비활성화된 규칙
            if (fileOverride.DisabledRules?.Contains(ruleId, StringComparer.OrdinalIgnoreCase) == true)
            {
                _logger.LogDebug("규칙 {RuleId}가 파일 {FilePath}에 대해 비활성화되었습니다",
                    ruleId, filePath);
                return false;
            }

            // 활성화된 규칙만 지정된 경우
            if (fileOverride.EnabledRules != null && fileOverride.EnabledRules.Count > 0)
            {
                if (!fileOverride.EnabledRules.Contains(ruleId, StringComparer.OrdinalIgnoreCase))
                {
                    _logger.LogDebug("규칙 {RuleId}가 파일 {FilePath}의 활성 규칙 목록에 없습니다",
                        ruleId, filePath);
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// 이슈가 필터링되어야 하는지 확인
    /// </summary>
    /// <param name="issue">확인할 이슈</param>
    /// <param name="fileContent">파일 내용 (억제 확인용)</param>
    /// <returns>필터링되어야 하면 true (이슈를 표시하지 않음)</returns>
    public bool ShouldFilterIssue(QAIssue issue, string? fileContent = null)
    {
        // 1. 파일이 전역 제외 대상인지 확인
        if (IsFileGloballyExcluded(issue.FilePath))
        {
            _logger.LogTrace("이슈가 전역 제외 파일에 있어 필터링됩니다: {FilePath}", issue.FilePath);
            return true;
        }

        // 2. 규칙이 해당 파일에 대해 활성화되어 있는지 확인
        if (!IsRuleEnabledForFile(issue.RuleId, issue.FilePath))
        {
            _logger.LogTrace("규칙 {RuleId}가 파일 {FilePath}에 대해 비활성화되어 필터링됩니다",
                issue.RuleId, issue.FilePath);
            return true;
        }

        // 3. 인라인 억제 확인
        if (fileContent != null && issue.Line > 0)
        {
            if (_suppressionChecker.IsSuppressed(issue.RuleId, issue.Line, fileContent))
            {
                _logger.LogTrace("이슈가 인라인 억제로 필터링됩니다: {RuleId} (줄 {Line})",
                    issue.RuleId, issue.Line);
                return true;
            }
        }

        // 4. 심각도 기반 필터링
        var fileOverride = GetFileOverride(issue.FilePath);
        if (fileOverride?.MinSeverity != null)
        {
            var minSeverity = ParseSeverity(fileOverride.MinSeverity);
            if (issue.Severity < minSeverity)
            {
                _logger.LogTrace("이슈 심각도({IssueSeverity})가 최소 심각도({MinSeverity})보다 낮아 필터링됩니다",
                    issue.Severity, minSeverity);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 이슈 목록을 필터링
    /// </summary>
    /// <param name="issues">원본 이슈 목록</param>
    /// <param name="fileContents">파일 경로와 내용 매핑 (옵션)</param>
    /// <returns>필터링된 이슈 목록</returns>
    public List<QAIssue> FilterIssues(
        IEnumerable<QAIssue> issues,
        Dictionary<string, string>? fileContents = null)
    {
        var filtered = new List<QAIssue>();

        foreach (var issue in issues)
        {
            string? fileContent = null;
            fileContents?.TryGetValue(issue.FilePath, out fileContent);

            if (!ShouldFilterIssue(issue, fileContent))
            {
                // 규칙 오버라이드에 따른 심각도 조정
                var adjustedIssue = AdjustIssueSeverity(issue);
                filtered.Add(adjustedIssue);
            }
        }

        _logger.LogInformation("총 {Total}개 이슈 중 {Filtered}개가 필터링되어 {Remaining}개가 남았습니다",
            issues.Count(), issues.Count() - filtered.Count, filtered.Count);

        return filtered;
    }

    /// <summary>
    /// 파일 경로에 해당하는 파일 오버라이드 찾기
    /// </summary>
    private FileOverride? GetFileOverride(string filePath)
    {
        var normalizedPath = NormalizePath(filePath);

        foreach (var (pattern, fileOverride) in _configuration.FileOverrides)
        {
            if (_compiledMatchers.TryGetValue(pattern, out var matcher) &&
                matcher.Match(normalizedPath).HasMatches)
            {
                _logger.LogTrace("파일 {FilePath}에 오버라이드 적용: {Pattern}", filePath, pattern);
                return fileOverride;
            }
        }

        return null;
    }

    /// <summary>
    /// 규칙 오버라이드에 따라 이슈 심각도 조정
    /// </summary>
    private QAIssue AdjustIssueSeverity(QAIssue issue)
    {
        if (!_configuration.RuleOverrides.TryGetValue(issue.RuleId, out var ruleOverride))
        {
            return issue;
        }

        if (ruleOverride.Severity == null)
        {
            return issue;
        }

        var newSeverity = ParseSeverity(ruleOverride.Severity);
        if (newSeverity != issue.Severity)
        {
            _logger.LogDebug("이슈 {RuleId}의 심각도를 {OldSeverity}에서 {NewSeverity}로 조정합니다",
                issue.RuleId, issue.Severity, newSeverity);

            return new QAIssue
            {
                RuleId = issue.RuleId,
                Severity = newSeverity,
                Title = issue.Title,
                Description = issue.Description,
                FilePath = issue.FilePath,
                Line = issue.Line,
                Category = issue.Category,
                Location = issue.Location,
                WhyDangerous = issue.WhyDangerous,
                Recommendation = issue.Recommendation,
                Examples = issue.Examples,
                OldCodeSnippet = issue.OldCodeSnippet,
                NewCodeSnippet = issue.NewCodeSnippet
            };
        }

        return issue;
    }

    /// <summary>
    /// 문자열을 Severity 열거형으로 변환
    /// </summary>
    private Severity ParseSeverity(string severity)
    {
        return severity.ToLowerInvariant() switch
        {
            "info" => Severity.Info,
            "warning" => Severity.Warning,
            "critical" => Severity.Critical,
            "error" => Severity.Critical, // Error를 Critical로 매핑
            _ => Severity.Warning
        };
    }

    /// <summary>
    /// 파일 경로를 정규화 (일관된 비교를 위해)
    /// </summary>
    private string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }

    /// <summary>
    /// 규칙에 대한 커스텀 파라미터 가져오기
    /// </summary>
    /// <param name="ruleId">규칙 ID</param>
    /// <returns>커스텀 파라미터 딕셔너리 (없으면 빈 딕셔너리)</returns>
    public Dictionary<string, object> GetRuleParameters(string ruleId)
    {
        if (_configuration.RuleOverrides.TryGetValue(ruleId, out var ruleOverride) &&
            ruleOverride.Parameters != null)
        {
            return ruleOverride.Parameters;
        }

        return new Dictionary<string, object>();
    }

    /// <summary>
    /// 파일에 대한 엄격 모드 여부 확인
    /// </summary>
    /// <param name="filePath">파일 경로</param>
    /// <returns>엄격 모드이면 true</returns>
    public bool IsStrictModeEnabled(string filePath)
    {
        var fileOverride = GetFileOverride(filePath);
        return fileOverride?.StrictMode ?? false;
    }
}
