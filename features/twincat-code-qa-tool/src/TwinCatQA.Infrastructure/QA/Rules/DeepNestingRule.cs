using System.Text.RegularExpressions;
using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Models.QA;
using TwinCatQA.Domain.Services;

namespace TwinCatQA.Infrastructure.QA.Rules;

/// <summary>
/// 깊은 중첩 감지 규칙 (Warning)
/// IF/FOR/WHILE 등이 3단계 이상 중첩된 경우 감지
/// </summary>
public class DeepNestingRule : IQARuleChecker
{
    public string RuleId => "QA009";
    public string RuleName => "깊은 중첩 구조 감지";
    public string Description => "3단계 이상 중첩된 제어문을 감지하여 복잡도 개선 권장";
    public Severity Severity => Severity.Warning;

    private const int MAX_NESTING_LEVEL = 3;

    public List<QAIssue> CheckVariableChange(VariableChange change)
    {
        // 변수 변경에서는 검사하지 않음
        return new List<QAIssue>();
    }

    public List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        // 추가되거나 수정된 로직만 검사
        if (change.ChangeType != ChangeType.Added && change.ChangeType != ChangeType.Modified)
        {
            return issues;
        }

        var code = change.NewCode ?? string.Empty;
        var maxNesting = CalculateMaxNesting(code);

        if (maxNesting > MAX_NESTING_LEVEL)
        {
            issues.Add(new QAIssue
            {
                Severity = Severity.Warning,
                Category = "코드 복잡도",
                Title = "중첩 구조가 너무 깊음",
                Description = $"{change.ElementName}: 최대 {maxNesting}단계 중첩 (권장: {MAX_NESTING_LEVEL}단계 이하)",
                Location = $"{change.FilePath}:{change.StartLine}",
                FilePath = change.FilePath,
                Line = change.StartLine,
                WhyDangerous = @"
깊은 중첩 구조는 다음 문제를 야기합니다:
- 코드 이해 및 추적 어려움
- 순환 복잡도(Cyclomatic Complexity) 증가
- 버그 발생 가능성 증가
- 테스트 케이스 작성 어려움
- 유지보수 비용 증가
",
                Recommendation = $@"
✅ 권장 해결 방법:

1. Early Return 패턴 사용
   ❌ IF condition1 THEN
          IF condition2 THEN
              IF condition3 THEN
                  // 작업
              END_IF
          END_IF
       END_IF

   ✅ IF NOT condition1 THEN RETURN; END_IF
      IF NOT condition2 THEN RETURN; END_IF
      IF NOT condition3 THEN RETURN; END_IF
      // 작업

2. 조건 결합
   ❌ IF a THEN
          IF b THEN
              // 작업
          END_IF
       END_IF

   ✅ IF a AND b THEN
          // 작업
       END_IF

3. 별도 함수로 추출
   IF ComplexCondition() THEN
       ProcessData();
   END_IF

   FUNCTION ComplexCondition : BOOL
       ComplexCondition := condition1 AND condition2;
   END_FUNCTION

4. 상태 머신으로 변환
   CASE state OF
       STATE_INIT: ...
       STATE_PROCESS: ...
       STATE_COMPLETE: ...
   END_CASE
",
                Examples = new List<string>
                {
                    $"현재 중첩 레벨: {maxNesting}단계",
                    "",
                    "리팩토링 예시:",
                    "// Early Return 사용",
                    "IF NOT sensorOK THEN",
                    "    errorFlag := TRUE;",
                    "    RETURN;",
                    "END_IF",
                    "",
                    "IF NOT motorReady THEN",
                    "    RETURN;",
                    "END_IF",
                    "",
                    "// 정상 처리",
                    "ProcessData();"
                },
                RuleId = RuleId,
                OldCodeSnippet = change.OldCode ?? string.Empty,
                NewCodeSnippet = $"Max nesting: {maxNesting} levels"
            });
        }

        return issues;
    }

    public List<QAIssue> CheckDataTypeChange(DataTypeChange change)
    {
        // 데이터 타입 변경에서는 검사하지 않음
        return new List<QAIssue>();
    }

    /// <summary>
    /// 코드의 최대 중첩 레벨 계산
    /// </summary>
    private int CalculateMaxNesting(string code)
    {
        int currentLevel = 0;
        int maxLevel = 0;

        var lines = code.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.Trim().ToUpper();

            // 중첩 시작 키워드
            if (Regex.IsMatch(trimmed, @"^(IF|FOR|WHILE|CASE|REPEAT)\b"))
            {
                currentLevel++;
                maxLevel = Math.Max(maxLevel, currentLevel);
            }

            // 중첩 종료 키워드
            if (Regex.IsMatch(trimmed, @"^(END_IF|END_FOR|END_WHILE|END_CASE|UNTIL)\b"))
            {
                currentLevel = Math.Max(0, currentLevel - 1);
            }
        }

        return maxLevel;
    }
}
