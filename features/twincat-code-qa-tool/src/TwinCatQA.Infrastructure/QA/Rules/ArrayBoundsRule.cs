using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Models.QA;
using TwinCatQA.Domain.Services;
using System.Text.RegularExpressions;

namespace TwinCatQA.Infrastructure.QA.Rules;

/// <summary>
/// 배열 범위 검사 누락 감지 규칙 (Critical)
/// 배열 접근 시 인덱스 범위 검사 없이 사용하는 경우 감지
/// </summary>
public class ArrayBoundsRule : IQARuleChecker
{
    public string RuleId => "QA003";
    public string RuleName => "배열 범위 검사 누락";
    public string Description => "배열 인덱스 범위 검사 없이 접근하여 메모리 오류 발생 가능성 검사";
    public Severity Severity => Severity.Critical;

    // 배열 접근 패턴: array[index]
    private static readonly Regex ArrayAccessPattern = new Regex(
        @"(\w+)\[([^\]]+)\]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    // 범위 검사 패턴: IF index >= lower AND index <= upper
    private static readonly Regex BoundsCheckPattern = new Regex(
        @"IF\s+(\w+)\s*(>=|>)\s*\d+\s*AND\s*\1\s*(<=|<)\s*\d+",
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

        // 배열 접근 패턴 찾기
        var arrayAccesses = ArrayAccessPattern.Matches(codeToCheck);

        foreach (Match match in arrayAccesses)
        {
            var arrayName = match.Groups[1].Value;
            var indexExpression = match.Groups[2].Value;

            // 인덱스가 상수인 경우는 제외 (컴파일 타임에 검증 가능)
            if (int.TryParse(indexExpression.Trim(), out _))
            {
                continue;
            }

            // 범위 검사가 있는지 확인
            if (!HasBoundsCheck(codeToCheck, indexExpression))
            {
                issues.Add(new QAIssue
                {
                    Severity = Severity.Critical,
                    Category = "메모리 안전성",
                    Title = "배열 범위 검사 없이 접근",
                    Description = $"{arrayName}[{indexExpression}]: 인덱스 범위 검사 없이 배열에 접근하여 메모리 오류 가능",
                    Location = $"{change.FilePath}:{change.StartLine}",
                    FilePath = change.FilePath,
                    Line = change.StartLine,
                    WhyDangerous = $@"
배열 범위를 벗어난 인덱스로 접근하면 다음과 같은 위험이 발생합니다:
1. 잘못된 메모리 영역 읽기/쓰기
2. 다른 변수의 값이 의도치 않게 변경됨
3. PLC 런타임 오류 또는 예측 불가능한 동작
4. 시스템 크래시 가능성

예: ARRAY[1..10] 에 index=15 로 접근하면 메모리 침범 발생
",
                    Recommendation = $@"
✅ 권장 해결 방법:

1. 명시적 범위 검사 추가 (가장 권장)
   IF {indexExpression} >= {GetArrayLowerBound(arrayName)} AND
      {indexExpression} <= {GetArrayUpperBound(arrayName)} THEN
       value := {arrayName}[{indexExpression}];
   ELSE
       // 에러 처리
       errorFlag := TRUE;
       value := 0; // 기본값
   END_IF

2. LIMIT 함수 사용
   safeIndex := LIMIT({GetArrayLowerBound(arrayName)}, {indexExpression}, {GetArrayUpperBound(arrayName)});
   value := {arrayName}[safeIndex];

3. 배열 크기 상수 정의 후 검증
   VAR CONSTANT
       ARRAY_MIN : INT := {GetArrayLowerBound(arrayName)};
       ARRAY_MAX : INT := {GetArrayUpperBound(arrayName)};
   END_VAR

   IF {indexExpression} >= ARRAY_MIN AND {indexExpression} <= ARRAY_MAX THEN
       value := {arrayName}[{indexExpression}];
   END_IF
",
                    Examples = new List<string>
                    {
                        $"❌ 위험: value := {arrayName}[{indexExpression}]; // 범위 검사 없음!",
                        "",
                        $"✅ 안전: IF {indexExpression} >= 1 AND {indexExpression} <= 10 THEN",
                        $"            value := {arrayName}[{indexExpression}];",
                        "         ELSE",
                        "            errorFlag := TRUE;",
                        "         END_IF"
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
        // 데이터 타입 변경에서는 검사하지 않음
        return new List<QAIssue>();
    }

    /// <summary>
    /// 범위 검사 존재 여부 확인
    /// </summary>
    private bool HasBoundsCheck(string code, string indexExpression)
    {
        // 인덱스 변수명 추출 (간단한 경우만)
        var indexVar = indexExpression.Trim();

        // 복잡한 표현식은 경고 발생 (안전을 위해)
        if (indexVar.Contains("+") || indexVar.Contains("-") ||
            indexVar.Contains("*") || indexVar.Contains("/"))
        {
            return false;
        }

        // 범위 검사 패턴이 있는지 확인
        var match = BoundsCheckPattern.Match(code);
        if (match.Success)
        {
            var checkedVar = match.Groups[1].Value;
            return checkedVar.Equals(indexVar, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    /// <summary>
    /// 배열 하한 추정 (기본값)
    /// </summary>
    private string GetArrayLowerBound(string arrayName)
    {
        // 실제로는 변수 선언 정보에서 가져와야 함
        // 현재는 TwinCAT 기본값 1 반환
        return "1";
    }

    /// <summary>
    /// 배열 상한 추정 (기본값)
    /// </summary>
    private string GetArrayUpperBound(string arrayName)
    {
        // 실제로는 변수 선언 정보에서 가져와야 함
        // 현재는 일반적인 크기 10 반환
        return "10";
    }
}
