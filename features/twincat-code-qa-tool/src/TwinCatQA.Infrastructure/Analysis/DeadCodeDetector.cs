using System.Text.RegularExpressions;
using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Services;

namespace TwinCatQA.Infrastructure.Analysis;

/// <summary>
/// 데드 코드 검출기 구현
/// 도달 불가능한 코드, 항상 참/거짓 조건, 주석 처리된 코드 검출
/// </summary>
public class DeadCodeDetector : IDeadCodeDetector
{
    // 항상 참인 조건 패턴
    private static readonly Regex AlwaysTruePattern = new(
        @"\bIF\s+(TRUE|1\s*=\s*1|0\s*<\s*1)\s+THEN",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // 항상 거짓인 조건 패턴
    private static readonly Regex AlwaysFalsePattern = new(
        @"\bIF\s+(FALSE|1\s*=\s*0|0\s*>\s*1)\s+THEN",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // RETURN 후 코드 패턴
    private static readonly Regex ReturnPattern = new(
        @"^\s*RETURN\s*;?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // 주석 처리된 코드 패턴 (ST 문법 포함)
    private static readonly Regex CommentedCodePattern = new(
        @"//\s*(?:IF|FOR|WHILE|CASE|VAR|END_|:=)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // 블록 주석 내 코드
    private static readonly Regex BlockCommentCodePattern = new(
        @"\(\*.*?(?:IF|FOR|WHILE|CASE|VAR|:=).*?\*\)",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    public List<DeadCodeResult> Detect(ValidationSession session)
    {
        var results = new List<DeadCodeResult>();

        foreach (var file in session.Files)
        {
            var lines = file.Content.Split('\n');

            // 항상 참/거짓 조건 검출
            results.AddRange(DetectAlwaysTrueFalse(file.FilePath, lines));

            // RETURN 후 도달 불가 코드 검출
            results.AddRange(DetectUnreachableAfterReturn(file.FilePath, lines));

            // 주석 처리된 코드 검출
            results.AddRange(DetectCommentedCode(file.FilePath, lines, file.Content));

            // 사용되지 않는 CASE 분기 검출
            results.AddRange(DetectUnusedCaseBranches(file.FilePath, lines));
        }

        return results;
    }

    /// <summary>
    /// 항상 참/거짓인 조건문 검출
    /// </summary>
    private List<DeadCodeResult> DetectAlwaysTrueFalse(string filePath, string[] lines)
    {
        var results = new List<DeadCodeResult>();

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            // 항상 참
            var trueMatch = AlwaysTruePattern.Match(line);
            if (trueMatch.Success)
            {
                results.Add(new DeadCodeResult
                {
                    Type = DeadCodeType.AlwaysTrueCondition,
                    FilePath = filePath,
                    StartLine = i + 1,
                    EndLine = i + 1,
                    CodeSnippet = line.Trim(),
                    Condition = trueMatch.Groups[1].Value,
                    Severity = IssueSeverity.Warning,
                    Message = $"항상 참인 조건문: '{trueMatch.Groups[1].Value}'"
                });
            }

            // 항상 거짓
            var falseMatch = AlwaysFalsePattern.Match(line);
            if (falseMatch.Success)
            {
                results.Add(new DeadCodeResult
                {
                    Type = DeadCodeType.AlwaysFalseCondition,
                    FilePath = filePath,
                    StartLine = i + 1,
                    EndLine = i + 1,
                    CodeSnippet = line.Trim(),
                    Condition = falseMatch.Groups[1].Value,
                    Severity = IssueSeverity.Error,
                    Message = $"항상 거짓인 조건문 (데드 코드): '{falseMatch.Groups[1].Value}'"
                });
            }
        }

        return results;
    }

    /// <summary>
    /// RETURN 후 도달 불가능한 코드 검출
    /// </summary>
    private List<DeadCodeResult> DetectUnreachableAfterReturn(string filePath, string[] lines)
    {
        var results = new List<DeadCodeResult>();
        bool afterReturn = false;
        int returnLine = 0;
        int nestingLevel = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            // 블록 시작/끝 추적
            if (Regex.IsMatch(line, @"\b(IF|FOR|WHILE|CASE)\b", RegexOptions.IgnoreCase))
                nestingLevel++;
            if (Regex.IsMatch(line, @"\bEND_(IF|FOR|WHILE|CASE)\b", RegexOptions.IgnoreCase))
            {
                nestingLevel = Math.Max(0, nestingLevel - 1);
                afterReturn = false;
            }

            // RETURN 검출
            if (ReturnPattern.IsMatch(line) && nestingLevel == 0)
            {
                afterReturn = true;
                returnLine = i + 1;
                continue;
            }

            // RETURN 후 코드 검출
            if (afterReturn && !string.IsNullOrWhiteSpace(line) &&
                !line.StartsWith("//") && !line.StartsWith("(*") &&
                !Regex.IsMatch(line, @"\bEND_", RegexOptions.IgnoreCase))
            {
                results.Add(new DeadCodeResult
                {
                    Type = DeadCodeType.UnreachableCode,
                    FilePath = filePath,
                    StartLine = i + 1,
                    EndLine = i + 1,
                    CodeSnippet = line,
                    Severity = IssueSeverity.Error,
                    Message = $"RETURN 문({returnLine}번째 줄) 후 도달 불가능한 코드"
                });
            }
        }

        return results;
    }

    /// <summary>
    /// 주석 처리된 코드 검출
    /// </summary>
    private List<DeadCodeResult> DetectCommentedCode(string filePath, string[] lines, string content)
    {
        var results = new List<DeadCodeResult>();

        // 라인 주석 검출
        for (int i = 0; i < lines.Length; i++)
        {
            if (CommentedCodePattern.IsMatch(lines[i]))
            {
                results.Add(new DeadCodeResult
                {
                    Type = DeadCodeType.CommentedOutCode,
                    FilePath = filePath,
                    StartLine = i + 1,
                    EndLine = i + 1,
                    CodeSnippet = lines[i].Trim(),
                    Severity = IssueSeverity.Info,
                    Message = "주석 처리된 코드 발견 - 정리 필요"
                });
            }
        }

        // 블록 주석 내 코드 검출
        var blockMatches = BlockCommentCodePattern.Matches(content);
        foreach (Match match in blockMatches)
        {
            var lineNumber = content.Substring(0, match.Index).Count(c => c == '\n') + 1;
            results.Add(new DeadCodeResult
            {
                Type = DeadCodeType.CommentedOutCode,
                FilePath = filePath,
                StartLine = lineNumber,
                EndLine = lineNumber,
                CodeSnippet = match.Value.Length > 100 ? match.Value.Substring(0, 100) + "..." : match.Value,
                Severity = IssueSeverity.Info,
                Message = "블록 주석 내 코드 발견 - 정리 필요"
            });
        }

        return results;
    }

    /// <summary>
    /// 사용되지 않는 CASE 분기 검출
    /// </summary>
    private List<DeadCodeResult> DetectUnusedCaseBranches(string filePath, string[] lines)
    {
        var results = new List<DeadCodeResult>();
        bool inCase = false;
        int caseStart = 0;
        var usedValues = new HashSet<string>();
        var definedValues = new HashSet<string>();

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            // CASE 시작
            if (Regex.IsMatch(line, @"\bCASE\b", RegexOptions.IgnoreCase))
            {
                inCase = true;
                caseStart = i + 1;
                usedValues.Clear();
                definedValues.Clear();
            }

            // CASE 분기 값
            if (inCase && Regex.IsMatch(line, @"^\d+\s*:", RegexOptions.IgnoreCase))
            {
                var valueMatch = Regex.Match(line, @"^(\d+)\s*:");
                if (valueMatch.Success)
                {
                    definedValues.Add(valueMatch.Groups[1].Value);
                }
            }

            // ELSE 분기 없는 CASE 종료
            if (inCase && Regex.IsMatch(line, @"\bEND_CASE\b", RegexOptions.IgnoreCase))
            {
                // ELSE가 없으면 경고
                bool hasElse = false;
                for (int j = caseStart; j < i; j++)
                {
                    if (Regex.IsMatch(lines[j], @"^\s*ELSE\s*:", RegexOptions.IgnoreCase))
                    {
                        hasElse = true;
                        break;
                    }
                }

                if (!hasElse && definedValues.Count > 0)
                {
                    results.Add(new DeadCodeResult
                    {
                        Type = DeadCodeType.UnreachableCode,
                        FilePath = filePath,
                        StartLine = caseStart,
                        EndLine = i + 1,
                        CodeSnippet = $"CASE 문에 ELSE 분기 없음 (정의된 값: {string.Join(", ", definedValues)})",
                        Severity = IssueSeverity.Warning,
                        Message = "CASE 문에 ELSE 분기가 없어 예상치 못한 값 처리 불가"
                    });
                }

                inCase = false;
            }
        }

        return results;
    }
}
