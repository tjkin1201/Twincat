using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Models.QA;
using TwinCatQA.Domain.Services;

namespace TwinCatQA.Infrastructure.QA.Rules;

/// <summary>
/// 순환 복잡도 감지 규칙 (Info)
/// McCabe 순환 복잡도가 15 이상인 경우 리팩토링 권장
/// </summary>
public class HighComplexityRule : IQARuleChecker
{
    public string RuleId => "QA017";
    public string RuleName => "높은 순환 복잡도 감지";
    public string Description => "코드 복잡도 감소를 위한 리팩토링 권장 (McCabe 복잡도 15 이상)";
    public Severity Severity => Severity.Info;

    private const int ComplexityThreshold = 15;

    public List<QAIssue> CheckVariableChange(VariableChange change)
    {
        // 변수 변경에서는 검사하지 않음
        return new List<QAIssue>();
    }

    public List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        // 추가되거나 수정된 로직만 검사
        if (change.ChangeType != ChangeType.Added &&
            change.ChangeType != ChangeType.Modified)
        {
            return issues;
        }

        var codeToCheck = change.NewCode ?? string.Empty;
        if (string.IsNullOrWhiteSpace(codeToCheck))
        {
            return issues;
        }

        // McCabe 순환 복잡도 계산
        var complexity = CalculateCyclomaticComplexity(codeToCheck);

        if (complexity >= ComplexityThreshold)
        {
            issues.Add(new QAIssue
            {
                Severity = Severity.Info,
                Category = "코드 품질",
                Title = "높은 순환 복잡도로 인한 유지보수 어려움",
                Description = $"{change.ElementName}: 순환 복잡도 {complexity} (권장: {ComplexityThreshold} 미만)",
                Location = $"{change.FilePath}:{change.StartLine}",
                FilePath = change.FilePath,
                Line = change.StartLine,
                WhyDangerous = $@"
순환 복잡도가 {complexity}로 매우 높아 다음 문제가 발생할 수 있습니다:

1. 테스트 어려움: {complexity}개 이상의 경로를 테스트해야 함
2. 버그 발생률 증가: 복잡도가 높을수록 버그 가능성 증가
3. 이해 난이도 상승: 코드 흐름 파악이 어려움
4. 변경 위험: 수정 시 예상치 못한 부작용 발생 가능

권장 복잡도는 15 미만이며, 10 이하가 이상적입니다.
",
                Recommendation = $@"
✅ 권장 해결 방법:

1. 함수 분할
   // 복잡한 함수를 여러 개의 작은 함수로 분리
   FUNCTION_BLOCK FB_ProcessControl
       METHOD Process
           ValidateInputs();      // 입력 검증
           CalculateSetpoint();   // 설정값 계산
           ExecuteControl();      // 제어 실행
           UpdateOutputs();       // 출력 갱신
       END_METHOD
   END_FUNCTION_BLOCK

2. 조건문 단순화
   // CASE 문 사용
   CASE state OF
       0: InitializeSystem();
       1: RunNormalOperation();
       2: HandleError();
   END_CASE

3. 복잡한 조건을 Boolean 변수로 추출
   isReadyToStart := (sensor1Active AND sensor2Active AND NOT emergencyStop);
   IF isReadyToStart THEN
       StartOperation();
   END_IF

4. 상태 머신 패턴 적용
   // 복잡한 로직을 명확한 상태로 구조화
",
                Examples = new List<string>
                {
                    "// ❌ 복잡도가 높은 코드",
                    "IF cond1 THEN",
                    "    IF cond2 THEN",
                    "        FOR i := 1 TO 10 DO",
                    "            IF cond3 OR cond4 THEN",
                    "                WHILE cond5 DO",
                    "                    // 많은 중첩...",
                    "                END_WHILE",
                    "            END_IF",
                    "        END_FOR",
                    "    END_IF",
                    "END_IF",
                    "",
                    "// ✅ 리팩토링된 코드",
                    "IF NOT CanProcess() THEN",
                    "    RETURN;",
                    "END_IF",
                    "",
                    "ProcessItems();  // 별도 메소드로 분리",
                    "",
                    "METHOD ProcessItems",
                    "    FOR i := 1 TO 10 DO",
                    "        ProcessSingleItem(i);",
                    "    END_FOR",
                    "END_METHOD"
                },
                RuleId = RuleId,
                OldCodeSnippet = change.OldCode ?? string.Empty,
                NewCodeSnippet = change.NewCode ?? string.Empty
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
    /// McCabe 순환 복잡도 계산
    /// 복잡도 = 결정 포인트 수 + 1
    /// </summary>
    private int CalculateCyclomaticComplexity(string code)
    {
        int complexity = 1; // 기본 복잡도
        var codeUpper = code.ToUpper();

        // 조건문: IF, ELSIF
        complexity += CountOccurrences(codeUpper, "IF ");
        complexity += CountOccurrences(codeUpper, "ELSIF ");

        // 반복문: FOR, WHILE, REPEAT
        complexity += CountOccurrences(codeUpper, "FOR ");
        complexity += CountOccurrences(codeUpper, "WHILE ");
        complexity += CountOccurrences(codeUpper, "REPEAT");

        // CASE 문의 각 분기
        complexity += CountOccurrences(codeUpper, "OF\n") + CountOccurrences(codeUpper, "OF\r\n");

        // 논리 연산자: AND, OR
        complexity += CountOccurrences(codeUpper, " AND ");
        complexity += CountOccurrences(codeUpper, " OR ");

        // 예외 처리
        complexity += CountOccurrences(codeUpper, "__CATCH");

        return complexity;
    }

    /// <summary>
    /// 문자열에서 특정 패턴의 출현 횟수 계산
    /// </summary>
    private int CountOccurrences(string text, string pattern)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(pattern))
            return 0;

        int count = 0;
        int index = 0;

        while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += pattern.Length;
        }

        return count;
    }
}
