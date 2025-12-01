using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Models.QA;
using TwinCatQA.Domain.Services;

namespace TwinCatQA.Infrastructure.QA.Rules;

/// <summary>
/// 타입 축소 감지 규칙 (Critical)
/// DINT → INT, LINT → DINT 등의 위험한 타입 변경 감지
/// </summary>
public class TypeNarrowingRule : IQARuleChecker
{
    public string RuleId => "QA001";
    public string RuleName => "타입 축소 감지";
    public string Description => "값 범위가 줄어드는 타입 변경으로 인한 데이터 손실 가능성 검사";
    public Severity Severity => Severity.Critical;

    // 타입 비트 크기 매핑
    private static readonly Dictionary<string, int> TypeSizes = new()
    {
        // 정수형
        { "SINT", 8 },
        { "USINT", 8 },
        { "INT", 16 },
        { "UINT", 16 },
        { "DINT", 32 },
        { "UDINT", 32 },
        { "LINT", 64 },
        { "ULINT", 64 },
        // 부동소수점
        { "REAL", 32 },
        { "LREAL", 64 },
        // 비트
        { "BYTE", 8 },
        { "WORD", 16 },
        { "DWORD", 32 },
        { "LWORD", 64 }
    };

    public List<QAIssue> CheckVariableChange(VariableChange change)
    {
        var issues = new List<QAIssue>();

        // 타입 변경이 있는 경우만 검사
        if (change.ChangeType != ChangeType.Modified ||
            string.IsNullOrEmpty(change.OldDataType) ||
            string.IsNullOrEmpty(change.NewDataType))
        {
            return issues;
        }

        // 배열 타입에서 기본 타입 추출
        var oldType = ExtractBaseType(change.OldDataType);
        var newType = ExtractBaseType(change.NewDataType);

        if (IsTypeNarrowing(oldType, newType))
        {
            issues.Add(new QAIssue
            {
                Severity = Severity.Critical,
                Category = "타입 안전성",
                Title = "타입 축소로 인한 데이터 손실 가능",
                Description = $"{change.VariableName}: {oldType} → {newType} 변경으로 값 범위 초과 가능",
                Location = $"{change.FilePath}:{change.Line}",
                FilePath = change.FilePath,
                Line = change.Line,
                WhyDangerous = $@"
{oldType} 타입의 값 범위가 {newType}보다 크기 때문에,
기존 값이 새 타입의 범위를 초과하면 예측 불가능한 값으로 변환됩니다.
예: DINT(-2147483648~2147483647) → INT(-32768~32767)
",
                Recommendation = $@"
✅ 권장 해결 방법:

1. 타입 변경 전 값 범위 확인
   IF {change.VariableName} > {GetMaxValue(newType)} OR
      {change.VariableName} < {GetMinValue(newType)} THEN
       // 에러 처리
       errorFlag := TRUE;
   END_IF

2. 또는 원래 타입({oldType}) 유지

3. 정말 필요하면 명시적 변환 + 범위 체크
   {change.VariableName} := LIMIT({GetMinValue(newType)}, value, {GetMaxValue(newType)});
",
                Examples = new List<string>
                {
                    $"❌ 위험: {change.VariableName} : {oldType} := 50000;",
                    $"         {change.VariableName} : {newType} := 50000; // 오버플로우!",
                    "",
                    $"✅ 안전: IF value >= {GetMinValue(newType)} AND value <= {GetMaxValue(newType)} THEN",
                    $"             {change.VariableName} := {newType}_TO_{oldType}(value);",
                    "         ELSE",
                    "             errorFlag := TRUE;",
                    "         END_IF"
                },
                RuleId = RuleId,
                OldCodeSnippet = $"{change.VariableName} : {oldType}",
                NewCodeSnippet = $"{change.VariableName} : {newType}"
            });
        }

        return issues;
    }

    public List<QAIssue> CheckLogicChange(LogicChange change)
    {
        // 로직 변경에서는 검사하지 않음
        return new List<QAIssue>();
    }

    public List<QAIssue> CheckDataTypeChange(DataTypeChange change)
    {
        // 데이터 타입 변경에서는 검사하지 않음
        return new List<QAIssue>();
    }

    /// <summary>
    /// 타입 축소 여부 확인
    /// </summary>
    private bool IsTypeNarrowing(string oldType, string newType)
    {
        if (!TypeSizes.TryGetValue(oldType.ToUpper(), out var oldSize) ||
            !TypeSizes.TryGetValue(newType.ToUpper(), out var newSize))
        {
            return false; // 알 수 없는 타입은 검사하지 않음
        }

        return newSize < oldSize;
    }

    /// <summary>
    /// 배열 타입에서 기본 타입 추출
    /// 예: "ARRAY[1..10] OF INT" → "INT"
    /// </summary>
    private string ExtractBaseType(string dataType)
    {
        if (dataType.Contains(" OF "))
        {
            var parts = dataType.Split(new[] { " OF " }, StringSplitOptions.None);
            return parts.Length > 1 ? parts[1].Trim() : dataType;
        }
        return dataType.Trim();
    }

    /// <summary>
    /// 타입의 최대값 반환
    /// </summary>
    private string GetMaxValue(string type)
    {
        return type.ToUpper() switch
        {
            "SINT" => "127",
            "USINT" => "255",
            "INT" => "32767",
            "UINT" => "65535",
            "DINT" => "2147483647",
            "UDINT" => "4294967295",
            "LINT" => "9223372036854775807",
            "ULINT" => "18446744073709551615",
            _ => "?"
        };
    }

    /// <summary>
    /// 타입의 최소값 반환
    /// </summary>
    private string GetMinValue(string type)
    {
        return type.ToUpper() switch
        {
            "SINT" => "-128",
            "USINT" => "0",
            "INT" => "-32768",
            "UINT" => "0",
            "DINT" => "-2147483648",
            "UDINT" => "0",
            "LINT" => "-9223372036854775808",
            "ULINT" => "0",
            _ => "?"
        };
    }
}
