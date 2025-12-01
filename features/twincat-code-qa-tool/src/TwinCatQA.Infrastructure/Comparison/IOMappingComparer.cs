using System.Text.RegularExpressions;
using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Services;

namespace TwinCatQA.Infrastructure.Comparison;

/// <summary>
/// I/O 매핑 비교 분석기 구현
/// </summary>
public class IOMappingComparer : IIOMappingComparer
{
    // I/O 매핑 패턴: varName AT %IX0.0 : BOOL;
    private static readonly Regex IOMappingPattern = new(
        @"(\w+)\s+AT\s+(%[IQM][XBWDL]?\d+(?:\.\d+)?)\s*:\s*(\w+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public List<IOMappingChange> Compare(List<CodeFile> oldFiles, List<CodeFile> newFiles)
    {
        var changes = new List<IOMappingChange>();

        // 모든 I/O 매핑 수집
        var oldMappings = CollectIOMappings(oldFiles);
        var newMappings = CollectIOMappings(newFiles);

        // 추가된 매핑 찾기
        foreach (var (key, newMapping) in newMappings)
        {
            if (!oldMappings.ContainsKey(key))
            {
                changes.Add(new IOMappingChange
                {
                    ChangeType = ChangeType.Added,
                    VariableName = newMapping.Name,
                    FilePath = newMapping.FilePath,
                    Line = newMapping.Line,
                    NewAddress = newMapping.Address,
                    DataType = newMapping.DataType
                });
            }
        }

        // 삭제된 매핑 찾기
        foreach (var (key, oldMapping) in oldMappings)
        {
            if (!newMappings.ContainsKey(key))
            {
                changes.Add(new IOMappingChange
                {
                    ChangeType = ChangeType.Removed,
                    VariableName = oldMapping.Name,
                    FilePath = oldMapping.FilePath,
                    Line = oldMapping.Line,
                    OldAddress = oldMapping.Address,
                    DataType = oldMapping.DataType
                });
            }
        }

        // 변경된 매핑 찾기 (같은 변수명, 다른 주소)
        foreach (var (key, oldMapping) in oldMappings)
        {
            if (newMappings.TryGetValue(key, out var newMapping))
            {
                if (!string.Equals(oldMapping.Address, newMapping.Address, StringComparison.OrdinalIgnoreCase))
                {
                    changes.Add(new IOMappingChange
                    {
                        ChangeType = ChangeType.Modified,
                        VariableName = newMapping.Name,
                        FilePath = newMapping.FilePath,
                        Line = newMapping.Line,
                        OldAddress = oldMapping.Address,
                        NewAddress = newMapping.Address,
                        DataType = newMapping.DataType
                    });
                }
            }
        }

        // 주소 충돌 검사 (같은 주소에 다른 변수가 할당된 경우)
        var oldByAddress = oldMappings.GroupBy(m => m.Value.Address.ToUpper())
            .ToDictionary(g => g.Key, g => g.First().Value.Name);
        var newByAddress = newMappings.GroupBy(m => m.Value.Address.ToUpper())
            .ToDictionary(g => g.Key, g => g.First().Value.Name);

        foreach (var (address, newVarName) in newByAddress)
        {
            if (oldByAddress.TryGetValue(address, out var oldVarName))
            {
                if (!string.Equals(oldVarName, newVarName, StringComparison.OrdinalIgnoreCase))
                {
                    // 같은 주소에 다른 변수 이름이 매핑됨
                    var mapping = newMappings.Values.First(m => m.Address.Equals(address, StringComparison.OrdinalIgnoreCase));
                    changes.Add(new IOMappingChange
                    {
                        ChangeType = ChangeType.Moved,
                        VariableName = $"{oldVarName} → {newVarName}",
                        FilePath = mapping.FilePath,
                        Line = mapping.Line,
                        OldAddress = address,
                        NewAddress = address,
                        DataType = mapping.DataType
                    });
                }
            }
        }

        return changes;
    }

    private Dictionary<string, IOMappingInfo> CollectIOMappings(List<CodeFile> files)
    {
        var mappings = new Dictionary<string, IOMappingInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            if (string.IsNullOrEmpty(file.Content)) continue;

            var lines = file.Content.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var match = IOMappingPattern.Match(lines[i]);
                if (match.Success)
                {
                    var varName = match.Groups[1].Value;
                    var address = match.Groups[2].Value;
                    var dataType = match.Groups[3].Value;

                    // 키: 변수명
                    mappings[varName] = new IOMappingInfo
                    {
                        Name = varName,
                        Address = address,
                        DataType = dataType,
                        FilePath = file.FilePath,
                        Line = i + 1
                    };
                }
            }
        }

        return mappings;
    }

    private class IOMappingInfo
    {
        public string Name { get; init; } = string.Empty;
        public string Address { get; init; } = string.Empty;
        public string DataType { get; init; } = string.Empty;
        public string FilePath { get; init; } = string.Empty;
        public int Line { get; init; }
    }
}
