using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Models.QA;
using TwinCatQA.Domain.Services;
using System.Text.RegularExpressions;

namespace TwinCatQA.Infrastructure.QA.Rules;

/// <summary>
/// NULL 포인터 검사 누락 감지 규칙 (Critical)
/// POINTER, REFERENCE 타입 역참조 전 NULL 체크 누락 감지
/// </summary>
public class NullCheckRule : IQARuleChecker
{
    public string RuleId => "QA004";
    public string RuleName => "NULL 포인터 검사 누락";
    public string Description => "POINTER/REFERENCE 타입 사용 전 NULL 검사 누락으로 인한 런타임 오류 가능성 검사";
    public Severity Severity => Severity.Critical;

    // 포인터/레퍼런스 타입
    private static readonly HashSet<string> PointerTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "POINTER", "REFERENCE", "REF_TO", "POINTER TO", "REFERENCE TO"
    };

    // 포인터 역참조 패턴: ptr^, ref.member, ptr^.member
    private static readonly Regex PointerDereferencePattern = new Regex(
        @"(\w+)\^|(\w+)\.(\w+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    // NULL 체크 패턴: IF ptr <> NULL, IF ptr <> 0, IF ptr <> 16#0
    private static readonly Regex NullCheckPattern = new Regex(
        @"IF\s+(\w+)\s*(<>|<\s*>)\s*(NULL|0|16#0)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    public List<QAIssue> CheckVariableChange(VariableChange change)
    {
        // 변수 변경에서는 검사하지 않음
        return new List<QAIssue>();
    }

    public List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        // 새로 추가되거나 수정된 로직만 검사
        if (change.ChangeType != ChangeType.Added &&
            change.ChangeType != ChangeType.Modified)
        {
            return issues;
        }

        var codeToCheck = change.ChangeType == ChangeType.Added
            ? change.NewCode
            : change.NewCode;

        // 포인터 역참조 패턴 찾기
        var dereferences = PointerDereferencePattern.Matches(codeToCheck);

        foreach (Match match in dereferences)
        {
            // ptr^ 형태 또는 ref.member 형태
            var pointerName = !string.IsNullOrEmpty(match.Groups[1].Value)
                ? match.Groups[1].Value
                : match.Groups[2].Value;

            // NULL 체크가 있는지 확인
            if (!HasNullCheck(codeToCheck, pointerName))
            {
                issues.Add(new QAIssue
                {
                    Severity = Severity.Critical,
                    Category = "메모리 안전성",
                    Title = "NULL 체크 없이 포인터 사용",
                    Description = $"{pointerName}: NULL 검사 없이 포인터/레퍼런스를 역참조하여 런타임 오류 가능",
                    Location = $"{change.FilePath}:{change.StartLine}",
                    FilePath = change.FilePath,
                    Line = change.StartLine,
                    WhyDangerous = $@"
NULL 포인터를 역참조하면 다음과 같은 심각한 문제가 발생합니다:
1. PLC 런타임 크래시 (심각한 경우)
2. 예측 불가능한 메모리 접근
3. 시스템 전체 중단 가능성
4. 안전 시스템 기능 상실

NULL 포인터는 유효한 메모리 주소를 가리키지 않으므로,
역참조 시도는 항상 오류를 발생시킵니다.
",
                    Recommendation = $@"
✅ 권장 해결 방법:

1. 명시적 NULL 체크 (가장 권장)
   IF {pointerName} <> NULL THEN
       value := {pointerName}^;
       // 또는 {pointerName}.member
   ELSE
       // 에러 처리
       errorFlag := TRUE;
       LogError('Null pointer detected: {pointerName}');
   END_IF

2. __ISVALIDREF 내장 함수 사용 (REFERENCE 타입)
   IF __ISVALIDREF({pointerName}) THEN
       value := {pointerName}.member;
   ELSE
       errorFlag := TRUE;
   END_IF

3. 초기화 시 NULL 검증
   {pointerName} := ADR(targetVariable);
   IF {pointerName} = NULL THEN
       LogError('Failed to get address');
       RETURN;
   END_IF

4. 방어적 프로그래밍
   // 함수 시작 시 검증
   IF {pointerName} = NULL THEN
       RETURN;
   END_IF
",
                    Examples = new List<string>
                    {
                        $"❌ 위험: value := {pointerName}^; // NULL 체크 없음!",
                        $"         value := {pointerName}.member; // NULL 체크 없음!",
                        "",
                        $"✅ 안전: IF {pointerName} <> NULL THEN",
                        $"            value := {pointerName}^;",
                        "         ELSE",
                        "            errorFlag := TRUE;",
                        "         END_IF",
                        "",
                        "// REFERENCE 타입의 경우",
                        $"IF __ISVALIDREF({pointerName}) THEN",
                        $"    value := {pointerName}.member;",
                        "END_IF"
                    },
                    RuleId = RuleId,
                    OldCodeSnippet = change.ChangeType == ChangeType.Modified ? change.OldCode : "",
                    NewCodeSnippet = match.Value
                });
            }
        }

        return issues;
    }

    public List<QAIssue> CheckDataTypeChange(DataTypeChange change)
    {
        var issues = new List<QAIssue>();

        // DataTypeChange에서는 구조체 필드 변경 시 포인터 타입 검사
        if (change.ChangeType == ChangeType.Modified)
        {
            foreach (var fieldChange in change.FieldChanges)
            {
                // 포인터/레퍼런스 타입으로 변경된 경우
                if (!IsPointerType(fieldChange.OldDataType ?? string.Empty) &&
                    IsPointerType(fieldChange.NewDataType ?? string.Empty))
                {
                    issues.Add(new QAIssue
                    {
                        Severity = Severity.Warning,
                        Category = "타입 안전성",
                        Title = "포인터/레퍼런스 타입으로 변경됨",
                        Description = $"{change.TypeName}.{fieldChange.FieldName}: 포인터/레퍼런스 타입으로 변경되어 NULL 체크 필요",
                        Location = $"{change.FilePath}:{change.Line}",
                        FilePath = change.FilePath,
                        Line = change.Line,
                        WhyDangerous = "포인터/레퍼런스 타입은 NULL일 수 있으므로 사용 전 반드시 검증이 필요합니다.",
                        Recommendation = $@"
변경된 필드를 사용하는 모든 코드에서 NULL 체크를 추가하세요:

IF instance.{fieldChange.FieldName} <> NULL THEN
    // 사용
END_IF
",
                        Examples = new List<string>
                        {
                            "// 변경 전 타입 사용 코드 검토 필요",
                            "// NULL 체크 추가 필수"
                        },
                        RuleId = RuleId,
                        OldCodeSnippet = $"{fieldChange.FieldName} : {fieldChange.OldDataType}",
                        NewCodeSnippet = $"{fieldChange.FieldName} : {fieldChange.NewDataType}"
                    });
                }
            }
        }

        return issues;
    }

    /// <summary>
    /// NULL 체크 존재 여부 확인
    /// </summary>
    private bool HasNullCheck(string code, string pointerName)
    {
        var match = NullCheckPattern.Match(code);
        if (match.Success)
        {
            var checkedVar = match.Groups[1].Value;
            return checkedVar.Equals(pointerName, StringComparison.OrdinalIgnoreCase);
        }

        // __ISVALIDREF 함수 사용 여부 확인
        if (code.Contains("__ISVALIDREF", StringComparison.OrdinalIgnoreCase) &&
            code.Contains(pointerName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 포인터/레퍼런스 타입 여부 확인
    /// </summary>
    private bool IsPointerType(string typeDefinition)
    {
        if (string.IsNullOrEmpty(typeDefinition))
        {
            return false;
        }

        return PointerTypes.Any(pt =>
            typeDefinition.Contains(pt, StringComparison.OrdinalIgnoreCase));
    }
}
