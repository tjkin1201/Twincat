using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Models.QA;
using TwinCatQA.Domain.Services;

namespace TwinCatQA.Infrastructure.QA.Rules;

/// <summary>
/// 파라미터 과다 감지 규칙 (Info)
/// Function/Method 파라미터가 5개 이상인 경우 구조체 사용 권장
/// </summary>
public class TooManyParametersRule : IQARuleChecker
{
    public string RuleId => "QA018";
    public string RuleName => "파라미터 과다 감지";
    public string Description => "함수 시그니처 단순화를 위한 구조체 사용 권장 (파라미터 5개 이상)";
    public Severity Severity => Severity.Info;

    private const int MaxParametersThreshold = 5;

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

        // VAR_INPUT 섹션에서 파라미터 개수 계산
        var parameterCount = CountParameters(codeToCheck);

        if (parameterCount >= MaxParametersThreshold)
        {
            issues.Add(new QAIssue
            {
                Severity = Severity.Info,
                Category = "코드 품질",
                Title = "파라미터 과다로 인한 가독성 및 유지보수성 저하",
                Description = $"{change.ElementName}: 파라미터 {parameterCount}개 (권장: {MaxParametersThreshold}개 미만)",
                Location = $"{change.FilePath}:{change.StartLine}",
                FilePath = change.FilePath,
                Line = change.StartLine,
                WhyDangerous = $@"
파라미터가 {parameterCount}개로 많아 다음 문제가 발생합니다:

1. 가독성 저하: 함수 호출 시 인자 순서 파악이 어려움
2. 오류 가능성: 파라미터 순서 착오로 인한 버그 발생
3. 유지보수 어려움: 파라미터 추가/제거 시 모든 호출부 수정 필요
4. 테스트 복잡도: 조합 경우의 수가 기하급수적으로 증가

일반적으로 3-4개 이하의 파라미터가 이상적이며,
5개 이상이면 구조체로 그룹화하는 것이 좋습니다.
",
                Recommendation = $@"
✅ 권장 해결 방법:

1. 구조체로 파라미터 그룹화
   TYPE ST_MotorConfig :
   STRUCT
       speed : REAL;           // 속도 [rpm]
       acceleration : REAL;    // 가속도 [rpm/s]
       maxCurrent : REAL;      // 최대 전류 [A]
       direction : BOOL;       // 방향 (0:CW, 1:CCW)
       enableBrake : BOOL;     // 브레이크 활성화
   END_STRUCT
   END_TYPE

   // 변경 전: 파라미터 5개
   METHOD ConfigureMotor(speed, accel, current, dir, brake)

   // 변경 후: 구조체 1개
   METHOD ConfigureMotor(config : ST_MotorConfig)

2. 관련 파라미터를 의미 단위로 그룹화
   TYPE ST_InputSignals :
   STRUCT
       sensor1 : BOOL;
       sensor2 : BOOL;
       sensor3 : BOOL;
   END_STRUCT
   END_TYPE

3. Builder 패턴 고려 (복잡한 설정의 경우)
   config.SetSpeed(1000.0)
         .SetAcceleration(500.0)
         .SetDirection(TRUE)
         .Build();
",
                Examples = new List<string>
                {
                    "// ❌ 파라미터가 너무 많음",
                    "FUNCTION_BLOCK FB_Motor",
                    "VAR_INPUT",
                    "    targetSpeed : REAL;",
                    "    acceleration : REAL;",
                    "    maxCurrent : REAL;",
                    "    direction : BOOL;",
                    "    enableBrake : BOOL;",
                    "    enableMonitoring : BOOL;",
                    "END_VAR",
                    "",
                    "// ✅ 구조체로 개선",
                    "TYPE ST_MotorParams :",
                    "STRUCT",
                    "    targetSpeed : REAL;",
                    "    acceleration : REAL;",
                    "    maxCurrent : REAL;",
                    "    direction : BOOL;",
                    "    enableBrake : BOOL;",
                    "    enableMonitoring : BOOL;",
                    "END_STRUCT",
                    "END_TYPE",
                    "",
                    "FUNCTION_BLOCK FB_Motor",
                    "VAR_INPUT",
                    "    params : ST_MotorParams;  // 명확하고 확장 가능",
                    "END_VAR"
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
    /// VAR_INPUT 섹션의 파라미터 개수 계산
    /// </summary>
    private int CountParameters(string code)
    {
        var lines = code.Split('\n');
        bool inVarInput = false;
        int paramCount = 0;

        foreach (var line in lines)
        {
            var trimmed = line.Trim().ToUpper();

            // VAR_INPUT 섹션 시작
            if (trimmed.StartsWith("VAR_INPUT"))
            {
                inVarInput = true;
                continue;
            }

            // VAR_INPUT 섹션 종료
            if (trimmed.StartsWith("END_VAR") && inVarInput)
            {
                inVarInput = false;
                continue;
            }

            // VAR_INPUT 섹션 내부의 변수 선언
            if (inVarInput &&
                !string.IsNullOrWhiteSpace(trimmed) &&
                !trimmed.StartsWith("//") &&
                !trimmed.StartsWith("(*") &&
                trimmed.Contains(":"))
            {
                paramCount++;
            }
        }

        return paramCount;
    }
}
