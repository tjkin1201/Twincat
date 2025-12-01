using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Models.QA;
using TwinCatQA.Domain.Services;

namespace TwinCatQA.Infrastructure.QA.Rules;

/// <summary>
/// 초기화되지 않은 변수 감지 규칙 (Critical)
/// 선언은 되었으나 초기값이 없는 변수 사용 감지
/// </summary>
public class UninitializedVariableRule : IQARuleChecker
{
    public string RuleId => "QA002";
    public string RuleName => "초기화되지 않은 변수 감지";
    public string Description => "선언만 되고 초기값이 없는 변수의 사용으로 인한 예측 불가능한 동작 검사";
    public Severity Severity => Severity.Critical;

    // 초기화가 필수적인 타입들
    private static readonly HashSet<string> CriticalTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "BOOL", "INT", "DINT", "LINT", "UINT", "UDINT", "ULINT",
        "REAL", "LREAL", "BYTE", "WORD", "DWORD", "LWORD",
        "POINTER", "REFERENCE"
    };

    public List<QAIssue> CheckVariableChange(VariableChange change)
    {
        var issues = new List<QAIssue>();

        // 새로 추가된 변수만 검사
        if (change.ChangeType != ChangeType.Added)
        {
            return issues;
        }

        // 초기값이 없고, Critical 타입인 경우
        if (string.IsNullOrEmpty(change.NewInitialValue) &&
            IsCriticalType(change.NewDataType ?? string.Empty))
        {
            issues.Add(new QAIssue
            {
                Severity = Severity.Critical,
                Category = "초기화 누락",
                Title = "변수가 초기화되지 않음",
                Description = $"{change.VariableName} ({change.NewDataType}): 초기값 없이 선언되어 예측 불가능한 값을 가질 수 있음",
                Location = $"{change.FilePath}:{change.Line}",
                FilePath = change.FilePath,
                Line = change.Line,
                WhyDangerous = $@"
초기화되지 않은 변수는 메모리에 남아있던 임의의 값을 가집니다.
PLC 재시작 시마다 다른 값을 가질 수 있어 예측 불가능한 동작을 유발합니다.
특히 BOOL 타입의 경우 안전 로직에서 치명적인 오류를 발생시킬 수 있습니다.
",
                Recommendation = $@"
✅ 권장 해결 방법:

1. 명시적 초기화 (가장 권장)
   {change.VariableName} : {change.NewDataType} := {GetDefaultValue(change.NewDataType)};

2. VAR_STAT 사용 시 RETAIN으로 값 유지
   VAR_STAT
       {change.VariableName} : {change.NewDataType} := {GetDefaultValue(change.NewDataType)};
   END_VAR

3. 초기화 로직 추가
   IF NOT initialized THEN
       {change.VariableName} := {GetDefaultValue(change.NewDataType)};
       initialized := TRUE;
   END_IF
",
                Examples = new List<string>
                {
                    $"❌ 위험: {change.VariableName} : {change.NewDataType}; // 초기값 없음!",
                    "",
                    $"✅ 안전: {change.VariableName} : {change.NewDataType} := {GetDefaultValue(change.NewDataType)};",
                    "",
                    "// 특히 BOOL 타입은 반드시 초기화",
                    "isEnabled : BOOL := FALSE;",
                    "emergencyStop : BOOL := FALSE;"
                },
                RuleId = RuleId,
                OldCodeSnippet = "",
                NewCodeSnippet = $"{change.VariableName} : {change.NewDataType};"
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
    /// Critical 타입 여부 확인
    /// </summary>
    private bool IsCriticalType(string dataType)
    {
        var baseType = ExtractBaseType(dataType);
        return CriticalTypes.Contains(baseType);
    }

    /// <summary>
    /// 배열 타입에서 기본 타입 추출
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
    /// 타입별 권장 기본값 반환
    /// </summary>
    private string GetDefaultValue(string type)
    {
        var baseType = ExtractBaseType(type).ToUpper();
        return baseType switch
        {
            "BOOL" => "FALSE",
            "SINT" or "INT" or "DINT" or "LINT" => "0",
            "USINT" or "UINT" or "UDINT" or "ULINT" => "0",
            "BYTE" or "WORD" or "DWORD" or "LWORD" => "0",
            "REAL" or "LREAL" => "0.0",
            "STRING" or "WSTRING" => "''",
            "TIME" => "T#0s",
            "POINTER" or "REFERENCE" => "NULL",
            _ => "0"
        };
    }
}
