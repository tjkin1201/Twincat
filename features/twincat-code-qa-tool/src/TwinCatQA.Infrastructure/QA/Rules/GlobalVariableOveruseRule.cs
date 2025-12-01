using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Models.QA;
using TwinCatQA.Domain.Services;

namespace TwinCatQA.Infrastructure.QA.Rules;

/// <summary>
/// 전역 변수 과다 사용 감지 규칙 (Warning)
/// 전역 변수가 과도하게 사용되는 경우 감지
/// </summary>
public class GlobalVariableOveruseRule : IQARuleChecker
{
    public string RuleId => "QA013";
    public string RuleName => "전역 변수 과다 사용 감지";
    public string Description => "전역 변수의 과다 사용을 감지하여 모듈화 및 캡슐화 권장";
    public Severity Severity => Severity.Warning;

    private const int MAX_GLOBAL_VARIABLES_PER_FILE = 20;
    private const int WARNING_THRESHOLD = 10;

    // 파일별 전역 변수 카운트
    private static readonly Dictionary<string, int> _globalVariableCount = new();

    public List<QAIssue> CheckVariableChange(VariableChange change)
    {
        var issues = new List<QAIssue>();

        // 전역 변수 추가만 검사
        if (change.ChangeType != ChangeType.Added)
        {
            return issues;
        }

        if (!IsGlobalVariable(change.Scope))
        {
            return issues;
        }

        // 파일별 전역 변수 카운트 증가
        if (!_globalVariableCount.ContainsKey(change.FilePath))
        {
            _globalVariableCount[change.FilePath] = 0;
        }
        _globalVariableCount[change.FilePath]++;

        var count = _globalVariableCount[change.FilePath];

        // 경고 임계값 초과
        if (count >= WARNING_THRESHOLD)
        {
            var severity = count >= MAX_GLOBAL_VARIABLES_PER_FILE ? Severity.Critical : Severity.Warning;

            issues.Add(new QAIssue
            {
                Severity = severity,
                Category = "아키텍처",
                Title = "전역 변수 과다 사용",
                Description = $"{change.FilePath}: 전역 변수 {count}개 (권장: {WARNING_THRESHOLD}개 이하)",
                Location = $"{change.FilePath}:{change.Line}",
                FilePath = change.FilePath,
                Line = change.Line,
                WhyDangerous = @"
전역 변수 과다 사용은 다음 문제를 야기합니다:
- 결합도(Coupling) 증가
- 코드 추적 및 디버깅 어려움
- 의도치 않은 값 변경 위험
- 재사용성 저하
- 테스트 어려움 (모든 전역 상태 고려)
- 동시성 문제 (여러 FB에서 접근)
",
                Recommendation = $@"
✅ 권장 해결 방법:

1. Function Block 내부 변수로 캡슐화
   FUNCTION_BLOCK FB_Controller
       VAR
           internalState : INT;  // 전역 변수 대신
           sensorValue : REAL;
       END_VAR

       VAR_INPUT
           enable : BOOL;  // 외부 입력
       END_VAR

       VAR_OUTPUT
           status : INT;   // 외부 출력
       END_VAR
   END_FUNCTION_BLOCK

2. 구조체로 관련 변수 그룹화
   TYPE ST_SystemConfig :
   STRUCT
       maxSpeed : REAL;
       timeout : TIME;
       retryCount : INT;
   END_STRUCT
   END_TYPE

   VAR_GLOBAL
       gConfig : ST_SystemConfig;  // 여러 변수 → 하나의 구조체
   END_VAR

3. Property 사용 (TwinCAT 3)
   FUNCTION_BLOCK FB_Data
       VAR PRIVATE
           _value : INT;
       END_VAR

       PROPERTY Value : INT
           GET
               Value := _value;
           SET
               _value := Value;
   END_FUNCTION_BLOCK

4. 필요한 경우에만 전역 변수 사용
   - 시스템 전체 설정
   - HMI 통신 변수
   - 안전 관련 상태

현재 파일의 전역 변수 수: {count}개
권장: Function Block으로 리팩토링
",
                Examples = new List<string>
                {
                    "❌ 나쁜 예 (전역 변수 과다):",
                    "   VAR_GLOBAL",
                    "       gMotorSpeed : REAL;",
                    "       gMotorCurrent : REAL;",
                    "       gMotorTemp : REAL;",
                    "       gMotorStatus : INT;",
                    "       gMotorError : BOOL;",
                    "       // ... 20개 이상",
                    "   END_VAR",
                    "",
                    "✅ 좋은 예 (캡슐화):",
                    "   FUNCTION_BLOCK FB_Motor",
                    "       VAR",
                    "           speed : REAL;",
                    "           current : REAL;",
                    "           temperature : REAL;",
                    "       END_VAR",
                    "   END_FUNCTION_BLOCK",
                    "   ",
                    "   VAR_GLOBAL",
                    "       gMotor : FB_Motor;  // 하나의 인스턴스",
                    "   END_VAR"
                },
                RuleId = RuleId,
                OldCodeSnippet = string.Empty,
                NewCodeSnippet = $"전역 변수: {count}개"
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
    /// 전역 변수 여부 확인
    /// </summary>
    private bool IsGlobalVariable(string scope)
    {
        return scope.ToUpper().Contains("GLOBAL") || scope.ToUpper() == "VAR_GLOBAL";
    }

    /// <summary>
    /// 전역 변수 카운트 초기화 (테스트용)
    /// </summary>
    public static void ResetCount()
    {
        _globalVariableCount.Clear();
    }
}
