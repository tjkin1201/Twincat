using System.Text.RegularExpressions;
using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Services;

namespace TwinCatQA.Infrastructure.Analysis;

/// <summary>
/// 매직 넘버 검출기 구현
/// 하드코딩된 숫자 상수 검출
/// </summary>
public class MagicNumberDetector : IMagicNumberDetector
{
    // 숫자 리터럴 패턴
    private static readonly Regex NumberPattern = new(
        @"(?<!\w)(-?\d+\.?\d*)\b",
        RegexOptions.Compiled);

    // 허용되는 숫자 (0, 1, -1, 100 등 일반적인 값)
    private static readonly HashSet<string> AllowedNumbers = new()
    {
        "0", "1", "-1", "2", "10", "100", "1000",
        "0.0", "1.0", "0.5", "2.0"
    };

    // 제외 패턴 (배열 인덱스, 상수 선언 등)
    private static readonly Regex ExcludePatterns = new(
        @"(?:ARRAY\s*\[|:=\s*\d+\s*;|CONSTANT|#\d+|T#|TIME#)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // 상수 선언 패턴
    private static readonly Regex ConstantDeclPattern = new(
        @"^\s*(\w+)\s*:\s*\w+\s*:=\s*(-?\d+\.?\d*)\s*;",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public List<MagicNumberResult> Detect(ValidationSession session)
    {
        var results = new List<MagicNumberResult>();

        foreach (var file in session.Files)
        {
            var lines = file.Content.Split('\n');

            // 상수 선언 수집
            var declaredConstants = CollectConstants(lines);

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // 주석 제거
                line = RemoveComments(line);

                // 제외 패턴 검사
                if (ExcludePatterns.IsMatch(line))
                    continue;

                // 상수 선언 라인 건너뛰기
                if (ConstantDeclPattern.IsMatch(line))
                    continue;

                // VAR CONSTANT 블록 내부 건너뛰기
                if (IsInConstantBlock(lines, i))
                    continue;

                // 숫자 검출
                var matches = NumberPattern.Matches(line);
                foreach (Match match in matches)
                {
                    var value = match.Value;

                    // 허용된 숫자 건너뛰기
                    if (AllowedNumbers.Contains(value))
                        continue;

                    // 이미 선언된 상수와 동일한 값은 낮은 심각도
                    var severity = declaredConstants.Contains(value)
                        ? IssueSeverity.Info
                        : IssueSeverity.Warning;

                    // 큰 숫자나 특정 패턴은 더 높은 심각도
                    if (double.TryParse(value, out double numValue))
                    {
                        if (Math.Abs(numValue) > 1000 || (numValue != 0 && Math.Abs(numValue) < 0.001))
                        {
                            severity = IssueSeverity.Error;
                        }
                    }

                    var context = GetContext(line, match.Index);
                    var suggestedName = SuggestConstantName(value, context);

                    results.Add(new MagicNumberResult
                    {
                        Value = value,
                        FilePath = file.FilePath,
                        Line = i + 1,
                        Context = context,
                        SuggestedConstantName = suggestedName,
                        Severity = severity,
                        Message = $"매직 넘버 '{value}' 발견 - 상수로 정의 권장"
                    });
                }
            }
        }

        // 중복 제거 및 정렬
        return results
            .GroupBy(r => new { r.FilePath, r.Line, r.Value })
            .Select(g => g.First())
            .OrderBy(r => r.FilePath)
            .ThenBy(r => r.Line)
            .ToList();
    }

    /// <summary>
    /// 상수 선언 값 수집
    /// </summary>
    private HashSet<string> CollectConstants(string[] lines)
    {
        var constants = new HashSet<string>();
        bool inConstantBlock = false;

        foreach (var line in lines)
        {
            if (Regex.IsMatch(line, @"\bVAR\s+CONSTANT\b", RegexOptions.IgnoreCase))
                inConstantBlock = true;

            if (Regex.IsMatch(line, @"\bEND_VAR\b", RegexOptions.IgnoreCase))
                inConstantBlock = false;

            if (inConstantBlock)
            {
                var match = Regex.Match(line, @":=\s*(-?\d+\.?\d*)\s*;");
                if (match.Success)
                {
                    constants.Add(match.Groups[1].Value);
                }
            }
        }

        return constants;
    }

    /// <summary>
    /// VAR CONSTANT 블록 내부인지 확인
    /// </summary>
    private bool IsInConstantBlock(string[] lines, int currentLine)
    {
        bool inConstant = false;

        for (int i = 0; i <= currentLine; i++)
        {
            if (Regex.IsMatch(lines[i], @"\bVAR\s+CONSTANT\b", RegexOptions.IgnoreCase))
                inConstant = true;

            if (Regex.IsMatch(lines[i], @"\bEND_VAR\b", RegexOptions.IgnoreCase))
                inConstant = false;
        }

        return inConstant;
    }

    /// <summary>
    /// 주석 제거
    /// </summary>
    private string RemoveComments(string line)
    {
        // 라인 주석 제거
        var idx = line.IndexOf("//");
        if (idx >= 0)
            line = line.Substring(0, idx);

        // 블록 주석 제거
        line = Regex.Replace(line, @"\(\*.*?\*\)", "");

        return line;
    }

    /// <summary>
    /// 컨텍스트 추출
    /// </summary>
    private string GetContext(string line, int position)
    {
        int start = Math.Max(0, position - 20);
        int end = Math.Min(line.Length, position + 20);
        return line.Substring(start, end - start).Trim();
    }

    /// <summary>
    /// 상수명 제안
    /// </summary>
    private string SuggestConstantName(string value, string context)
    {
        // 컨텍스트 기반 이름 생성
        var contextLower = context.ToLower();

        if (contextLower.Contains("speed"))
            return $"MAX_SPEED";
        if (contextLower.Contains("time") || contextLower.Contains("delay"))
            return $"DELAY_TIME_MS";
        if (contextLower.Contains("count") || contextLower.Contains("max"))
            return $"MAX_COUNT";
        if (contextLower.Contains("min"))
            return $"MIN_VALUE";
        if (contextLower.Contains("size") || contextLower.Contains("length"))
            return $"BUFFER_SIZE";
        if (contextLower.Contains("position") || contextLower.Contains("pos"))
            return $"TARGET_POSITION";
        if (contextLower.Contains("temp"))
            return $"TEMP_THRESHOLD";
        if (contextLower.Contains("pressure"))
            return $"PRESSURE_LIMIT";

        // 기본 이름
        if (double.TryParse(value, out double numValue))
        {
            if (numValue > 0)
                return $"VALUE_{value.Replace(".", "_").Replace("-", "NEG_")}";
        }

        return $"CONST_{value.Replace(".", "_").Replace("-", "NEG_")}";
    }
}
