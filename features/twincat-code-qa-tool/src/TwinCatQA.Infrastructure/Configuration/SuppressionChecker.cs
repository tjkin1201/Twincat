using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace TwinCatQA.Infrastructure.Configuration;

/// <summary>
/// 코드 내 인라인 억제(suppression) 주석을 처리하는 클래스
/// // qa-ignore: SA0049 또는 (* qa-ignore-start: SA0033 *) 같은 패턴 인식
/// </summary>
public class SuppressionChecker
{
    private readonly ILogger<SuppressionChecker> _logger;
    private readonly InlineSuppressionConfig _config;
    private readonly Dictionary<string, List<Regex>> _compiledPatterns;

    public SuppressionChecker(
        ILogger<SuppressionChecker> logger,
        InlineSuppressionConfig config)
    {
        _logger = logger;
        _config = config;
        _compiledPatterns = new Dictionary<string, List<Regex>>();

        // 패턴들을 미리 컴파일
        CompilePatterns();
    }

    /// <summary>
    /// 모든 억제 패턴을 정규식으로 컴파일
    /// </summary>
    private void CompilePatterns()
    {
        _compiledPatterns["comment"] = CompilePatternList(_config.CommentPatterns);
        _compiledPatterns["blockStart"] = CompilePatternList(_config.BlockStartPatterns);
        _compiledPatterns["blockEnd"] = CompilePatternList(_config.BlockEndPatterns);
        _compiledPatterns["file"] = CompilePatternList(_config.FileSuppressionPatterns);
    }

    /// <summary>
    /// 패턴 목록을 정규식 목록으로 컴파일
    /// </summary>
    private List<Regex> CompilePatternList(List<string> patterns)
    {
        var regexList = new List<Regex>();

        foreach (var pattern in patterns)
        {
            try
            {
                // {ruleId}를 캡처 그룹으로 변환
                // 예: "// qa-ignore: {ruleId}" -> "// qa-ignore:\s*(\w+)"
                var regexPattern = pattern
                    .Replace("{ruleId}", @"(?<ruleId>[\w\-]+)")
                    .Replace("(", @"\(")
                    .Replace(")", @"\)")
                    .Replace("*", @"\*")
                    .Replace("//", @"//\s*");

                var regex = new Regex(regexPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                regexList.Add(regex);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "억제 패턴 컴파일 실패: {Pattern}", pattern);
            }
        }

        return regexList;
    }

    /// <summary>
    /// 파일 전체에 대한 억제 규칙 확인
    /// </summary>
    /// <param name="fileContent">파일 내용</param>
    /// <returns>억제된 규칙 ID 목록</returns>
    public HashSet<string> GetFileLevelSuppressions(string fileContent)
    {
        if (!_config.Enabled)
        {
            return new HashSet<string>();
        }

        var suppressions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lines = fileContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        // 파일 상단 10줄만 검사 (일반적으로 파일 수준 억제는 파일 상단에 위치)
        var linesToCheck = Math.Min(10, lines.Length);

        for (int i = 0; i < linesToCheck; i++)
        {
            var line = lines[i].Trim();

            foreach (var regex in _compiledPatterns["file"])
            {
                var match = regex.Match(line);
                if (match.Success && match.Groups["ruleId"].Success)
                {
                    var ruleId = match.Groups["ruleId"].Value;
                    suppressions.Add(ruleId);
                    _logger.LogDebug("파일 수준 억제 발견: {RuleId} (줄 {LineNumber})", ruleId, i + 1);
                }
            }
        }

        return suppressions;
    }

    /// <summary>
    /// 특정 줄에 대한 억제 규칙 확인
    /// </summary>
    /// <param name="lineNumber">확인할 줄 번호 (1부터 시작)</param>
    /// <param name="fileContent">파일 전체 내용</param>
    /// <returns>억제된 규칙 ID 목록</returns>
    public HashSet<string> GetLineSuppressions(int lineNumber, string fileContent)
    {
        if (!_config.Enabled)
        {
            return new HashSet<string>();
        }

        var suppressions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lines = fileContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        if (lineNumber < 1 || lineNumber > lines.Length)
        {
            return suppressions;
        }

        var lineIndex = lineNumber - 1;

        // 1. 해당 줄의 주석 확인
        var currentLine = lines[lineIndex].Trim();
        CheckCommentSuppressions(currentLine, suppressions);

        // 2. 이전 줄의 주석 확인 (바로 위 줄에 억제 주석이 있는 경우)
        if (lineIndex > 0)
        {
            var previousLine = lines[lineIndex - 1].Trim();
            CheckCommentSuppressions(previousLine, suppressions);
        }

        // 3. 블록 억제 확인
        var blockSuppressions = GetBlockSuppressions(lineNumber, lines);
        suppressions.UnionWith(blockSuppressions);

        return suppressions;
    }

    /// <summary>
    /// 주석에서 억제 규칙 추출
    /// </summary>
    private void CheckCommentSuppressions(string line, HashSet<string> suppressions)
    {
        foreach (var regex in _compiledPatterns["comment"])
        {
            var match = regex.Match(line);
            if (match.Success && match.Groups["ruleId"].Success)
            {
                var ruleId = match.Groups["ruleId"].Value;
                suppressions.Add(ruleId);
                _logger.LogTrace("인라인 억제 발견: {RuleId}", ruleId);
            }
        }
    }

    /// <summary>
    /// 블록 억제 범위 내에 있는지 확인
    /// </summary>
    private HashSet<string> GetBlockSuppressions(int lineNumber, string[] lines)
    {
        var suppressions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var activeBlocks = new Dictionary<string, int>(); // ruleId -> start line

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            var currentLineNumber = i + 1;

            // 블록 시작 확인
            foreach (var regex in _compiledPatterns["blockStart"])
            {
                var match = regex.Match(line);
                if (match.Success && match.Groups["ruleId"].Success)
                {
                    var ruleId = match.Groups["ruleId"].Value;
                    activeBlocks[ruleId] = currentLineNumber;
                    _logger.LogTrace("블록 억제 시작: {RuleId} (줄 {LineNumber})", ruleId, currentLineNumber);
                }
            }

            // 현재 확인 중인 줄이면 활성 블록 억제 추가
            if (currentLineNumber == lineNumber)
            {
                foreach (var ruleId in activeBlocks.Keys)
                {
                    suppressions.Add(ruleId);
                }
            }

            // 블록 종료 확인
            foreach (var regex in _compiledPatterns["blockEnd"])
            {
                var match = regex.Match(line);
                if (match.Success)
                {
                    // 모든 활성 블록 종료
                    if (currentLineNumber < lineNumber)
                    {
                        activeBlocks.Clear();
                    }
                    _logger.LogTrace("블록 억제 종료 (줄 {LineNumber})", currentLineNumber);
                    break;
                }
            }
        }

        return suppressions;
    }

    /// <summary>
    /// 특정 규칙이 특정 줄에서 억제되었는지 확인
    /// </summary>
    /// <param name="ruleId">확인할 규칙 ID</param>
    /// <param name="lineNumber">줄 번호</param>
    /// <param name="fileContent">파일 내용</param>
    /// <returns>억제되었으면 true</returns>
    public bool IsSuppressed(string ruleId, int lineNumber, string fileContent)
    {
        if (!_config.Enabled)
        {
            return false;
        }

        // 파일 수준 억제 확인
        var fileSuppressions = GetFileLevelSuppressions(fileContent);
        if (fileSuppressions.Contains(ruleId))
        {
            _logger.LogDebug("규칙 {RuleId}가 파일 수준에서 억제되었습니다", ruleId);
            return true;
        }

        // 줄 수준 억제 확인
        var lineSuppressions = GetLineSuppressions(lineNumber, fileContent);
        if (lineSuppressions.Contains(ruleId))
        {
            _logger.LogDebug("규칙 {RuleId}가 줄 {LineNumber}에서 억제되었습니다", ruleId, lineNumber);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 파일 내의 모든 억제 정보를 추출
    /// </summary>
    /// <param name="fileContent">파일 내용</param>
    /// <returns>줄 번호와 억제된 규칙 ID 매핑</returns>
    public Dictionary<int, HashSet<string>> GetAllSuppressions(string fileContent)
    {
        if (!_config.Enabled)
        {
            return new Dictionary<int, HashSet<string>>();
        }

        var result = new Dictionary<int, HashSet<string>>();
        var lines = fileContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        // 파일 수준 억제
        var fileSuppressions = GetFileLevelSuppressions(fileContent);

        // 각 줄에 대해 억제 정보 수집
        for (int i = 0; i < lines.Length; i++)
        {
            var lineNumber = i + 1;
            var lineSuppressions = GetLineSuppressions(lineNumber, fileContent);

            // 파일 수준 억제 추가
            lineSuppressions.UnionWith(fileSuppressions);

            if (lineSuppressions.Count > 0)
            {
                result[lineNumber] = lineSuppressions;
            }
        }

        return result;
    }

    /// <summary>
    /// 사용되지 않는 억제 주석 찾기
    /// </summary>
    /// <param name="fileContent">파일 내용</param>
    /// <param name="actualIssueRuleIds">실제로 발견된 이슈의 규칙 ID 목록</param>
    /// <returns>사용되지 않는 억제 정보 (줄 번호, 규칙 ID)</returns>
    public List<(int LineNumber, string RuleId)> FindUnusedSuppressions(
        string fileContent,
        HashSet<string> actualIssueRuleIds)
    {
        if (!_config.Enabled || !_config.WarnOnUnusedSuppressions)
        {
            return new List<(int, string)>();
        }

        var unusedSuppressions = new List<(int, string)>();
        var allSuppressions = GetAllSuppressions(fileContent);

        foreach (var (lineNumber, ruleIds) in allSuppressions)
        {
            foreach (var ruleId in ruleIds)
            {
                // 실제 이슈에 없는 억제된 규칙 찾기
                if (!actualIssueRuleIds.Contains(ruleId))
                {
                    unusedSuppressions.Add((lineNumber, ruleId));
                    _logger.LogDebug("사용되지 않는 억제 발견: {RuleId} (줄 {LineNumber})",
                        ruleId, lineNumber);
                }
            }
        }

        return unusedSuppressions;
    }
}
