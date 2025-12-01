using System.Text.RegularExpressions;
using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Services;

namespace TwinCatQA.Infrastructure.Comparison;

/// <summary>
/// 데이터 타입 비교 분석기 구현
/// </summary>
public class DataTypeComparer : IDataTypeComparer
{
    // 구조체 패턴
    private static readonly Regex StructPattern = new(
        @"TYPE\s+(\w+)\s*:\s*STRUCT(.*?)END_STRUCT",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    // 열거형 패턴
    private static readonly Regex EnumPattern = new(
        @"TYPE\s+(\w+)\s*:\s*\((.*?)\)",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    // 구조체 필드 패턴
    private static readonly Regex FieldPattern = new(
        @"(\w+)\s*:\s*(\w+(?:\s*\(.*?\))?)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // 열거형 값 패턴
    private static readonly Regex EnumValuePattern = new(
        @"(\w+)(?:\s*:=\s*(\d+))?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public List<DataTypeChange> Compare(List<CodeFile> oldFiles, List<CodeFile> newFiles)
    {
        var changes = new List<DataTypeChange>();

        // 구조체 비교
        var oldStructs = CollectStructs(oldFiles);
        var newStructs = CollectStructs(newFiles);
        changes.AddRange(CompareDataTypes(oldStructs, newStructs, DataTypeKind.Struct));

        // 열거형 비교
        var oldEnums = CollectEnums(oldFiles);
        var newEnums = CollectEnums(newFiles);
        changes.AddRange(CompareDataTypes(oldEnums, newEnums, DataTypeKind.Enum));

        return changes;
    }

    private List<DataTypeChange> CompareDataTypes(
        Dictionary<string, DataTypeInfo> oldTypes,
        Dictionary<string, DataTypeInfo> newTypes,
        DataTypeKind kind)
    {
        var changes = new List<DataTypeChange>();

        // 추가된 타입
        foreach (var (key, newType) in newTypes)
        {
            if (!oldTypes.ContainsKey(key))
            {
                changes.Add(new DataTypeChange
                {
                    ChangeType = ChangeType.Added,
                    TypeName = newType.Name,
                    Kind = kind,
                    FilePath = newType.FilePath,
                    Line = newType.Line,
                    OldDefinition = "(없음 - 새로 추가됨)",
                    NewDefinition = newType.RawContent,
                    FieldChanges = newType.Fields.Select(f => new FieldChange
                    {
                        ChangeType = ChangeType.Added,
                        FieldName = f.Name,
                        NewDataType = f.DataType
                    }).ToList(),
                    EnumChanges = newType.EnumValues.Select(e => new EnumValueChange
                    {
                        ChangeType = ChangeType.Added,
                        ValueName = e.Name,
                        NewValue = e.Value
                    }).ToList()
                });
            }
        }

        // 삭제된 타입
        foreach (var (key, oldType) in oldTypes)
        {
            if (!newTypes.ContainsKey(key))
            {
                changes.Add(new DataTypeChange
                {
                    ChangeType = ChangeType.Removed,
                    TypeName = oldType.Name,
                    Kind = kind,
                    FilePath = oldType.FilePath,
                    Line = oldType.Line,
                    OldDefinition = oldType.RawContent,
                    NewDefinition = "(삭제됨)",
                    FieldChanges = oldType.Fields.Select(f => new FieldChange
                    {
                        ChangeType = ChangeType.Removed,
                        FieldName = f.Name,
                        OldDataType = f.DataType
                    }).ToList(),
                    EnumChanges = oldType.EnumValues.Select(e => new EnumValueChange
                    {
                        ChangeType = ChangeType.Removed,
                        ValueName = e.Name,
                        OldValue = e.Value
                    }).ToList()
                });
            }
        }

        // 변경된 타입
        foreach (var (key, oldType) in oldTypes)
        {
            if (newTypes.TryGetValue(key, out var newType))
            {
                var fieldChanges = CompareFields(oldType.Fields, newType.Fields);
                var enumChanges = CompareEnumValues(oldType.EnumValues, newType.EnumValues);

                if (fieldChanges.Any() || enumChanges.Any())
                {
                    changes.Add(new DataTypeChange
                    {
                        ChangeType = ChangeType.Modified,
                        TypeName = newType.Name,
                        Kind = kind,
                        FilePath = newType.FilePath,
                        Line = newType.Line,
                        OldDefinition = oldType.RawContent,
                        NewDefinition = newType.RawContent,
                        FieldChanges = fieldChanges,
                        EnumChanges = enumChanges
                    });
                }
            }
        }

        return changes;
    }

    private List<FieldChange> CompareFields(List<FieldInfo> oldFields, List<FieldInfo> newFields)
    {
        var changes = new List<FieldChange>();
        var oldDict = oldFields.ToDictionary(f => f.Name, StringComparer.OrdinalIgnoreCase);
        var newDict = newFields.ToDictionary(f => f.Name, StringComparer.OrdinalIgnoreCase);

        // 추가된 필드
        foreach (var (name, field) in newDict)
        {
            if (!oldDict.ContainsKey(name))
            {
                changes.Add(new FieldChange
                {
                    ChangeType = ChangeType.Added,
                    FieldName = name,
                    NewDataType = field.DataType
                });
            }
        }

        // 삭제된 필드
        foreach (var (name, field) in oldDict)
        {
            if (!newDict.ContainsKey(name))
            {
                changes.Add(new FieldChange
                {
                    ChangeType = ChangeType.Removed,
                    FieldName = name,
                    OldDataType = field.DataType
                });
            }
        }

        // 변경된 필드
        foreach (var (name, oldField) in oldDict)
        {
            if (newDict.TryGetValue(name, out var newField))
            {
                if (!string.Equals(oldField.DataType, newField.DataType, StringComparison.OrdinalIgnoreCase))
                {
                    changes.Add(new FieldChange
                    {
                        ChangeType = ChangeType.Modified,
                        FieldName = name,
                        OldDataType = oldField.DataType,
                        NewDataType = newField.DataType
                    });
                }
            }
        }

        return changes;
    }

    private List<EnumValueChange> CompareEnumValues(List<EnumValueInfo> oldValues, List<EnumValueInfo> newValues)
    {
        var changes = new List<EnumValueChange>();
        var oldDict = oldValues.ToDictionary(e => e.Name, StringComparer.OrdinalIgnoreCase);
        var newDict = newValues.ToDictionary(e => e.Name, StringComparer.OrdinalIgnoreCase);

        // 추가된 값
        foreach (var (name, value) in newDict)
        {
            if (!oldDict.ContainsKey(name))
            {
                changes.Add(new EnumValueChange
                {
                    ChangeType = ChangeType.Added,
                    ValueName = name,
                    NewValue = value.Value
                });
            }
        }

        // 삭제된 값
        foreach (var (name, value) in oldDict)
        {
            if (!newDict.ContainsKey(name))
            {
                changes.Add(new EnumValueChange
                {
                    ChangeType = ChangeType.Removed,
                    ValueName = name,
                    OldValue = value.Value
                });
            }
        }

        // 변경된 값
        foreach (var (name, oldValue) in oldDict)
        {
            if (newDict.TryGetValue(name, out var newValue))
            {
                if (oldValue.Value != newValue.Value)
                {
                    changes.Add(new EnumValueChange
                    {
                        ChangeType = ChangeType.Modified,
                        ValueName = name,
                        OldValue = oldValue.Value,
                        NewValue = newValue.Value
                    });
                }
            }
        }

        return changes;
    }

    private Dictionary<string, DataTypeInfo> CollectStructs(List<CodeFile> files)
    {
        var structs = new Dictionary<string, DataTypeInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            if (string.IsNullOrEmpty(file.Content)) continue;

            var matches = StructPattern.Matches(file.Content);
            foreach (Match match in matches)
            {
                var name = match.Groups[1].Value;
                var body = match.Groups[2].Value;
                var line = file.Content[..match.Index].Count(c => c == '\n') + 1;

                var fields = new List<FieldInfo>();

                // 주석 제거 및 라인별 처리
                var bodyLines = body.Split('\n');
                foreach (var bodyLine in bodyLines)
                {
                    var trimmedLine = bodyLine.Trim();

                    // 주석 라인 필터링
                    if (string.IsNullOrEmpty(trimmedLine) ||
                        trimmedLine.StartsWith("//") ||
                        trimmedLine.StartsWith("(*"))
                        continue;

                    var fieldMatch = FieldPattern.Match(trimmedLine);
                    if (fieldMatch.Success)
                    {
                        fields.Add(new FieldInfo
                        {
                            Name = fieldMatch.Groups[1].Value,
                            DataType = fieldMatch.Groups[2].Value
                        });
                    }
                }

                structs[name] = new DataTypeInfo
                {
                    Name = name,
                    FilePath = file.FilePath,
                    Line = line,
                    Fields = fields,
                    RawContent = match.Value.Trim()
                };
            }
        }

        return structs;
    }

    private Dictionary<string, DataTypeInfo> CollectEnums(List<CodeFile> files)
    {
        var enums = new Dictionary<string, DataTypeInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            if (string.IsNullOrEmpty(file.Content)) continue;

            var matches = EnumPattern.Matches(file.Content);
            foreach (Match match in matches)
            {
                var name = match.Groups[1].Value;
                var body = match.Groups[2].Value;
                var line = file.Content[..match.Index].Count(c => c == '\n') + 1;

                var enumValues = new List<EnumValueInfo>();

                // 주석 제거 및 라인별 처리
                var bodyLines = body.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var bodyLine in bodyLines)
                {
                    var trimmedLine = bodyLine.Trim();

                    // 주석 라인 필터링
                    if (string.IsNullOrEmpty(trimmedLine) ||
                        trimmedLine.StartsWith("//") ||
                        trimmedLine.StartsWith("(*"))
                        continue;

                    var valueMatch = EnumValuePattern.Match(trimmedLine);
                    if (valueMatch.Success)
                    {
                        var valueName = valueMatch.Groups[1].Value;
                        int? value = valueMatch.Groups[2].Success ? int.Parse(valueMatch.Groups[2].Value) : null;

                        enumValues.Add(new EnumValueInfo
                        {
                            Name = valueName,
                            Value = value
                        });
                    }
                }

                enums[name] = new DataTypeInfo
                {
                    Name = name,
                    FilePath = file.FilePath,
                    Line = line,
                    EnumValues = enumValues,
                    RawContent = match.Value.Trim()
                };
            }
        }

        return enums;
    }

    private class DataTypeInfo
    {
        public string Name { get; init; } = string.Empty;
        public string FilePath { get; init; } = string.Empty;
        public int Line { get; init; }
        public List<FieldInfo> Fields { get; init; } = new();
        public List<EnumValueInfo> EnumValues { get; init; } = new();

        /// <summary>전체 원본 정의 코드</summary>
        public string RawContent { get; init; } = string.Empty;
    }

    private class FieldInfo
    {
        public string Name { get; init; } = string.Empty;
        public string DataType { get; init; } = string.Empty;
    }

    private class EnumValueInfo
    {
        public string Name { get; init; } = string.Empty;
        public int? Value { get; init; }
    }
}
