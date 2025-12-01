using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Models.QA;
using TwinCatQA.Domain.Services;
using TwinCatQA.Infrastructure.Parsers;

namespace TwinCatQA.Infrastructure.QA.Rules;

/// <summary>
/// 순환 복잡도 감지 규칙
/// McCabe 순환 복잡도 기반 경고
/// - 10 이하: Good (검사 통과)
/// - 11-15: Info (권장 개선)
/// - 16-20: Warning (개선 필요)
/// - 21 이상: Critical (즉시 리팩토링 필수)
/// </summary>
public class HighComplexityRule : IQARuleChecker
{
    public string RuleId => "QA017";
    public string RuleName => "높은 순환 복잡도 감지";
    public string Description => "코드 복잡도 감소를 위한 리팩토링 권장 (McCabe 복잡도 기반)";
    public Severity Severity => Severity.Warning; // 기본 심각도 (실제로는 복잡도에 따라 동적 결정)

    // 복잡도 임계값
    private const int InfoThreshold = 10;      // 11 이상: Info
    private const int WarningThreshold = 15;   // 16 이상: Warning
    private const int CriticalThreshold = 20;  // 21 이상: Critical

    // 정규식 기반 복잡도 계산기
    private readonly CyclomaticComplexityVisitor _complexityCalculator = new();

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

        // McCabe 순환 복잡도 계산 (정규식 기반)
        var complexity = _complexityCalculator.CalculateFromCode(codeToCheck);

        // 임계값에 따른 심각도 결정
        if (complexity <= InfoThreshold)
        {
            return issues; // 10 이하는 문제 없음
        }

        var severity = GetSeverityForComplexity(complexity);
        var severityText = GetSeverityText(complexity);

        issues.Add(new QAIssue
        {
            Severity = severity,
            Category = "코드 품질",
            Title = $"높은 순환 복잡도 ({severityText})",
            Description = $"{change.ElementName}: 순환 복잡도 {complexity} (권장: {InfoThreshold} 이하)",
            Location = $"{change.FilePath}:{change.StartLine}",
            FilePath = change.FilePath,
            Line = change.StartLine,
            WhyDangerous = $@"
순환 복잡도가 {complexity}로 {severityText} 수준입니다.

복잡도 등급:
- 1-10: Good (이상적)
- 11-15: Info (권장 개선)
- 16-20: Warning (개선 필요)
- 21+: Critical (즉시 리팩토링 필수)

현재 문제:
1. 테스트 어려움: 최소 {complexity}개 경로 테스트 필요
2. 버그 발생률: 복잡도 {complexity}는 버그 발생 가능성 {GetBugProbability(complexity)}
3. 유지보수: 코드 이해 및 수정 어려움
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

        return issues;
    }

    public List<QAIssue> CheckDataTypeChange(DataTypeChange change)
    {
        // 데이터 타입 변경에서는 검사하지 않음
        return new List<QAIssue>();
    }

    /// <summary>
    /// 복잡도에 따른 심각도 반환
    /// </summary>
    private Severity GetSeverityForComplexity(int complexity)
    {
        if (complexity > CriticalThreshold)
            return Severity.Critical;
        if (complexity > WarningThreshold)
            return Severity.Warning;
        return Severity.Info;
    }

    /// <summary>
    /// 복잡도에 따른 심각도 텍스트 반환
    /// </summary>
    private string GetSeverityText(int complexity)
    {
        if (complexity > CriticalThreshold)
            return "매우 높음 - 즉시 리팩토링 필수";
        if (complexity > WarningThreshold)
            return "높음 - 개선 필요";
        return "보통 - 권장 개선";
    }

    /// <summary>
    /// 복잡도에 따른 버그 발생 확률 추정
    /// </summary>
    private string GetBugProbability(int complexity)
    {
        // NASA 및 Carnegie Mellon 연구 기반 추정치
        if (complexity > 50)
            return "매우 높음 (>70%)";
        if (complexity > CriticalThreshold)
            return "높음 (40-70%)";
        if (complexity > WarningThreshold)
            return "중간 (20-40%)";
        return "낮음 (<20%)";
    }
}
