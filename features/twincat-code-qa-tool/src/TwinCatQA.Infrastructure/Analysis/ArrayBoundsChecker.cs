using System.Text.RegularExpressions;
using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Services;

namespace TwinCatQA.Infrastructure.Analysis;

/// <summary>
/// 배열 경계 검사기 구현
/// 배열 인덱스 범위 초과 검출
/// </summary>
public class ArrayBoundsChecker : IArrayBoundsChecker
{
    // 배열 선언 패턴: arr : ARRAY[0..10] OF INT
    private static readonly Regex ArrayDeclPattern = new(
        @"(\w+)\s*:\s*ARRAY\s*\[\s*(-?\d+)\s*\.\.\s*(-?\d+)\s*\]\s*OF\s+(\w+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // 배열 접근 패턴: arr[index]
    private static readonly Regex ArrayAccessPattern = new(
        @"(\w+)\s*\[\s*(.+?)\s*\]",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // FOR 루프 패턴: FOR i := 0 TO 10 DO
    private static readonly Regex ForLoopPattern = new(
        @"\bFOR\s+(\w+)\s*:=\s*(-?\d+)\s+TO\s+(-?\d+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public List<ArrayBoundsResult> Check(ValidationSession session)
    {
        var results = new List<ArrayBoundsResult>();

        foreach (var file in session.Files)
        {
            var lines = file.Content.Split('\n');

            // 1단계: 배열 선언 수집
            var arrays = CollectArrayDeclarations(file.FilePath, lines);

            // 2단계: 배열 접근 검사
            results.AddRange(CheckArrayAccesses(file.FilePath, lines, arrays));

            // 3단계: FOR 루프 인덱스 검사
            results.AddRange(CheckForLoopIndices(file.FilePath, lines, arrays));
        }

        return results;
    }

    /// <summary>
    /// 배열 선언 정보 수집
    /// </summary>
    private Dictionary<string, ArrayInfo> CollectArrayDeclarations(string filePath, string[] lines)
    {
        var arrays = new Dictionary<string, ArrayInfo>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < lines.Length; i++)
        {
            var matches = ArrayDeclPattern.Matches(lines[i]);
            foreach (Match match in matches)
            {
                var name = match.Groups[1].Value;
                var lower = int.Parse(match.Groups[2].Value);
                var upper = int.Parse(match.Groups[3].Value);
                var dataType = match.Groups[4].Value;

                arrays[name] = new ArrayInfo
                {
                    Name = name,
                    LowerBound = lower,
                    UpperBound = upper,
                    DataType = dataType,
                    DeclarationLine = i + 1
                };
            }
        }

        return arrays;
    }

    /// <summary>
    /// 배열 접근 검사
    /// </summary>
    private List<ArrayBoundsResult> CheckArrayAccesses(string filePath, string[] lines, Dictionary<string, ArrayInfo> arrays)
    {
        var results = new List<ArrayBoundsResult>();

        for (int i = 0; i < lines.Length; i++)
        {
            var matches = ArrayAccessPattern.Matches(lines[i]);
            foreach (Match match in matches)
            {
                var arrayName = match.Groups[1].Value;
                var indexExpr = match.Groups[2].Value.Trim();

                // 알려진 배열인지 확인
                if (!arrays.TryGetValue(arrayName, out var arrayInfo))
                    continue;

                // 상수 인덱스인 경우
                if (int.TryParse(indexExpr, out int constIndex))
                {
                    if (constIndex < arrayInfo.LowerBound)
                    {
                        results.Add(new ArrayBoundsResult
                        {
                            ArrayName = arrayName,
                            FilePath = filePath,
                            Line = i + 1,
                            DeclaredSize = arrayInfo.UpperBound - arrayInfo.LowerBound + 1,
                            LowerBound = arrayInfo.LowerBound,
                            UpperBound = arrayInfo.UpperBound,
                            AccessIndex = constIndex,
                            ViolationType = constIndex < 0 ? ArrayBoundsViolationType.NegativeIndex : ArrayBoundsViolationType.ConstantOutOfBounds,
                            Severity = IssueSeverity.Critical,
                            Message = $"배열 '{arrayName}' 인덱스 {constIndex}이(가) 하한({arrayInfo.LowerBound})보다 작음"
                        });
                    }
                    else if (constIndex > arrayInfo.UpperBound)
                    {
                        results.Add(new ArrayBoundsResult
                        {
                            ArrayName = arrayName,
                            FilePath = filePath,
                            Line = i + 1,
                            DeclaredSize = arrayInfo.UpperBound - arrayInfo.LowerBound + 1,
                            LowerBound = arrayInfo.LowerBound,
                            UpperBound = arrayInfo.UpperBound,
                            AccessIndex = constIndex,
                            ViolationType = ArrayBoundsViolationType.ConstantOutOfBounds,
                            Severity = IssueSeverity.Critical,
                            Message = $"배열 '{arrayName}' 인덱스 {constIndex}이(가) 상한({arrayInfo.UpperBound})보다 큼"
                        });
                    }
                }
                // 변수 인덱스인 경우 - 경계 검사 코드 존재 여부 확인
                else if (!IsIndexValidated(lines, i, indexExpr, arrayInfo))
                {
                    results.Add(new ArrayBoundsResult
                    {
                        ArrayName = arrayName,
                        FilePath = filePath,
                        Line = i + 1,
                        DeclaredSize = arrayInfo.UpperBound - arrayInfo.LowerBound + 1,
                        LowerBound = arrayInfo.LowerBound,
                        UpperBound = arrayInfo.UpperBound,
                        AccessExpression = indexExpr,
                        ViolationType = ArrayBoundsViolationType.UncheckedDynamicIndex,
                        Severity = IssueSeverity.Warning,
                        Message = $"배열 '{arrayName}'의 동적 인덱스 '{indexExpr}'에 대한 경계 검사 없음"
                    });
                }
            }
        }

        return results;
    }

    /// <summary>
    /// 인덱스 검증 코드 존재 여부 확인
    /// </summary>
    private bool IsIndexValidated(string[] lines, int currentLine, string indexExpr, ArrayInfo arrayInfo)
    {
        // 현재 라인 주변 5줄 검사
        int start = Math.Max(0, currentLine - 5);
        int end = Math.Min(lines.Length - 1, currentLine + 2);

        for (int i = start; i <= end; i++)
        {
            var line = lines[i];

            // 경계 검사 패턴: IF index >= 0 AND index <= 10 THEN
            if (Regex.IsMatch(line, $@"\b{Regex.Escape(indexExpr)}\s*>=\s*{arrayInfo.LowerBound}", RegexOptions.IgnoreCase) ||
                Regex.IsMatch(line, $@"\b{Regex.Escape(indexExpr)}\s*<=\s*{arrayInfo.UpperBound}", RegexOptions.IgnoreCase) ||
                Regex.IsMatch(line, $@"LIMIT\s*\(\s*{Regex.Escape(indexExpr)}", RegexOptions.IgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// FOR 루프 인덱스 범위 검사
    /// </summary>
    private List<ArrayBoundsResult> CheckForLoopIndices(string filePath, string[] lines, Dictionary<string, ArrayInfo> arrays)
    {
        var results = new List<ArrayBoundsResult>();
        var loopVars = new Dictionary<string, (int Start, int End, int Line)>();

        // FOR 루프 수집
        for (int i = 0; i < lines.Length; i++)
        {
            var forMatch = ForLoopPattern.Match(lines[i]);
            if (forMatch.Success)
            {
                var varName = forMatch.Groups[1].Value;
                var start = int.Parse(forMatch.Groups[2].Value);
                var end = int.Parse(forMatch.Groups[3].Value);
                loopVars[varName] = (start, end, i + 1);
            }
        }

        // 루프 변수로 배열 접근 검사
        for (int i = 0; i < lines.Length; i++)
        {
            var accessMatches = ArrayAccessPattern.Matches(lines[i]);
            foreach (Match match in accessMatches)
            {
                var arrayName = match.Groups[1].Value;
                var indexExpr = match.Groups[2].Value.Trim();

                if (!arrays.TryGetValue(arrayName, out var arrayInfo))
                    continue;

                // 루프 변수로 접근하는 경우
                if (loopVars.TryGetValue(indexExpr, out var loopInfo))
                {
                    if (loopInfo.Start < arrayInfo.LowerBound || loopInfo.End > arrayInfo.UpperBound)
                    {
                        results.Add(new ArrayBoundsResult
                        {
                            ArrayName = arrayName,
                            FilePath = filePath,
                            Line = i + 1,
                            DeclaredSize = arrayInfo.UpperBound - arrayInfo.LowerBound + 1,
                            LowerBound = arrayInfo.LowerBound,
                            UpperBound = arrayInfo.UpperBound,
                            AccessExpression = $"FOR {indexExpr} := {loopInfo.Start} TO {loopInfo.End}",
                            ViolationType = ArrayBoundsViolationType.LoopIndexOutOfBounds,
                            Severity = IssueSeverity.Critical,
                            Message = $"FOR 루프({loopInfo.Line}번째 줄)의 범위({loopInfo.Start}..{loopInfo.End})가 배열 '{arrayName}'의 범위({arrayInfo.LowerBound}..{arrayInfo.UpperBound})를 초과"
                        });
                    }
                }
            }
        }

        return results;
    }

    /// <summary>
    /// 배열 정보 내부 클래스
    /// </summary>
    private class ArrayInfo
    {
        public string Name { get; init; } = string.Empty;
        public int LowerBound { get; init; }
        public int UpperBound { get; init; }
        public string DataType { get; init; } = string.Empty;
        public int DeclarationLine { get; init; }
    }
}
