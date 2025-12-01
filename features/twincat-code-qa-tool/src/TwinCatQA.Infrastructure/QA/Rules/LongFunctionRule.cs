using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Models.QA;
using TwinCatQA.Domain.Services;

namespace TwinCatQA.Infrastructure.QA.Rules;

/// <summary>
/// 긴 함수 감지 규칙 (Warning)
/// 50줄 이상의 긴 함수를 감지하여 리팩토링 권장
/// </summary>
public class LongFunctionRule : IQARuleChecker
{
    public string RuleId => "QA008";
    public string RuleName => "긴 함수 감지";
    public string Description => "50줄 이상의 긴 함수를 감지하여 모듈화 및 가독성 향상 권장";
    public Severity Severity => Severity.Warning;

    private const int MAX_LINES = 50;
    private const int CRITICAL_LINES = 100;

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

        var lineCount = change.EndLine - change.StartLine + 1;

        if (lineCount > MAX_LINES)
        {
            var severity = lineCount > CRITICAL_LINES ? Severity.Critical : Severity.Warning;

            issues.Add(new QAIssue
            {
                Severity = severity,
                Category = "코드 품질",
                Title = "함수가 너무 김",
                Description = $"{change.ElementName}: {lineCount}줄 (권장: {MAX_LINES}줄 이하)",
                Location = $"{change.FilePath}:{change.StartLine}",
                FilePath = change.FilePath,
                Line = change.StartLine,
                WhyDangerous = @"
긴 함수는 다음 문제를 야기합니다:
- 코드 이해 어려움 (한눈에 파악 불가)
- 재사용성 저하
- 테스트 어려움 (많은 시나리오)
- 버그 발생 가능성 증가
- 유지보수 비용 증가
- PLC 사이클 타임 증가 위험
",
                Recommendation = $@"
✅ 권장 해결 방법:

1. 기능별로 Function Block 분리
   FUNCTION_BLOCK FB_MainLogic
       VAR
           sensor : FB_SensorProcessing;  // 센서 처리 분리
           motor : FB_MotorControl;       // 모터 제어 분리
       END_VAR

       sensor();  // 센서 처리
       motor();   // 모터 제어
   END_FUNCTION_BLOCK

2. 반복 코드는 별도 함수로 추출
   FUNCTION F_CalculateAverage : REAL
       VAR_INPUT
           values : ARRAY[1..10] OF REAL;
       END_VAR
       // 평균 계산 로직
   END_FUNCTION

3. 상태 머신 사용 (순차 로직)
   CASE step OF
       0: InitializeSystem();
       1: ProcessData();
       2: CheckResults();
   END_CASE

4. 각 블록은 50줄 이하로 유지
   - 단일 책임 원칙 준수
   - 함수명으로 의도 명확화
",
                Examples = new List<string>
                {
                    $"❌ 현재: {change.ElementName} = {lineCount}줄",
                    "",
                    "✅ 리팩토링 예:",
                    "   FUNCTION_BLOCK FB_Main",
                    "       // 초기화 (10줄)",
                    "       InitializeSystem();",
                    "       ",
                    "       // 센서 읽기 (15줄)",
                    "       ReadSensors();",
                    "       ",
                    "       // 제어 로직 (20줄)",
                    "       ControlLogic();",
                    "   END_FUNCTION_BLOCK"
                },
                RuleId = RuleId,
                OldCodeSnippet = $"Lines {change.StartLine}-{change.EndLine}",
                NewCodeSnippet = $"{lineCount} lines total"
            });
        }

        return issues;
    }

    public List<QAIssue> CheckDataTypeChange(DataTypeChange change)
    {
        // 데이터 타입 변경에서는 검사하지 않음
        return new List<QAIssue>();
    }
}
