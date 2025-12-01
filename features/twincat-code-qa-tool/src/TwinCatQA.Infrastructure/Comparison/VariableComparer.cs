using System.Text.RegularExpressions;
using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Services;

namespace TwinCatQA.Infrastructure.Comparison;

/// <summary>
/// 변수 비교 분석기 구현
/// </summary>
public class VariableComparer : IVariableComparer
{
    // 변수 선언 패턴: varName : DataType := InitValue;
    private static readonly Regex VariablePattern = new(
        @"^\s*(\w+)\s*:\s*(\w+(?:\s*\(\s*\d+(?:\s*\.\.\s*\d+)?\s*\))?(?:\s+OF\s+\w+)?)\s*(?::=\s*([^;]+))?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // VAR 블록 패턴
    private static readonly Regex VarBlockPattern = new(
        @"(VAR(?:_GLOBAL|_INPUT|_OUTPUT|_IN_OUT|_TEMP|_STAT|_CONSTANT)?)\s*(.*?)END_VAR",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    public List<VariableChange> Compare(List<CodeFile> oldFiles, List<CodeFile> newFiles)
    {
        var changes = new List<VariableChange>();

        // 모든 변수 수집
        var oldVariables = CollectVariables(oldFiles);
        var newVariables = CollectVariables(newFiles);

        // 추가된 변수 찾기
        foreach (var (key, newVar) in newVariables)
        {
            if (!oldVariables.ContainsKey(key))
            {
                changes.Add(new VariableChange
                {
                    ChangeType = ChangeType.Added,
                    VariableName = newVar.Name,
                    FilePath = newVar.FilePath,
                    Line = newVar.Line,
                    NewDataType = newVar.DataType,
                    NewInitialValue = newVar.InitialValue,
                    Scope = newVar.Scope
                });
            }
        }

        // 삭제된 변수 찾기
        foreach (var (key, oldVar) in oldVariables)
        {
            if (!newVariables.ContainsKey(key))
            {
                changes.Add(new VariableChange
                {
                    ChangeType = ChangeType.Removed,
                    VariableName = oldVar.Name,
                    FilePath = oldVar.FilePath,
                    Line = oldVar.Line,
                    OldDataType = oldVar.DataType,
                    OldInitialValue = oldVar.InitialValue,
                    Scope = oldVar.Scope
                });
            }
        }

        // 변경된 변수 찾기
        foreach (var (key, oldVar) in oldVariables)
        {
            if (newVariables.TryGetValue(key, out var newVar))
            {
                bool typeChanged = !string.Equals(oldVar.DataType, newVar.DataType, StringComparison.OrdinalIgnoreCase);
                bool valueChanged = !string.Equals(oldVar.InitialValue?.Trim(), newVar.InitialValue?.Trim(), StringComparison.OrdinalIgnoreCase);

                if (typeChanged || valueChanged)
                {
                    changes.Add(new VariableChange
                    {
                        ChangeType = ChangeType.Modified,
                        VariableName = newVar.Name,
                        FilePath = newVar.FilePath,
                        Line = newVar.Line,
                        OldDataType = oldVar.DataType,
                        NewDataType = newVar.DataType,
                        OldInitialValue = oldVar.InitialValue,
                        NewInitialValue = newVar.InitialValue,
                        Scope = newVar.Scope
                    });
                }
            }
        }

        return changes;
    }

    private Dictionary<string, VariableInfo> CollectVariables(List<CodeFile> files)
    {
        var variables = new Dictionary<string, VariableInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            if (string.IsNullOrEmpty(file.Content)) continue;

            var varBlocks = VarBlockPattern.Matches(file.Content);
            foreach (Match blockMatch in varBlocks)
            {
                var scope = blockMatch.Groups[1].Value;
                var blockContent = blockMatch.Groups[2].Value;
                var blockStartIndex = blockMatch.Index;

                // 블록 시작 라인 계산
                var blockStartLine = file.Content[..blockStartIndex].Count(c => c == '\n') + 1;

                var lines = blockContent.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line) || line.StartsWith("//") || line.StartsWith("(*"))
                        continue;

                    var match = VariablePattern.Match(line);
                    if (match.Success)
                    {
                        var varName = match.Groups[1].Value;
                        var dataType = match.Groups[2].Value;
                        var initialValue = match.Groups[3].Success ? match.Groups[3].Value.Trim() : null;

                        // 키: 파일명 + 범위 + 변수명 (전역 변수는 파일명 제외)
                        var key = scope.Contains("GLOBAL", StringComparison.OrdinalIgnoreCase)
                            ? $"GLOBAL.{varName}"
                            : $"{Path.GetFileNameWithoutExtension(file.FilePath)}.{scope}.{varName}";

                        variables[key] = new VariableInfo
                        {
                            Name = varName,
                            DataType = dataType,
                            InitialValue = initialValue,
                            Scope = scope,
                            FilePath = file.FilePath,
                            Line = blockStartLine + i
                        };
                    }
                }
            }
        }

        return variables;
    }

    private class VariableInfo
    {
        public string Name { get; init; } = string.Empty;
        public string DataType { get; init; } = string.Empty;
        public string? InitialValue { get; init; }
        public string Scope { get; init; } = string.Empty;
        public string FilePath { get; init; } = string.Empty;
        public int Line { get; init; }
    }
}
