using System.Text.RegularExpressions;
using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Models.QA;
using TwinCatQA.Domain.Services;

namespace TwinCatQA.Infrastructure.QA.Rules;

/// <summary>
/// 하드코딩된 I/O 주소 감지 규칙 (Warning)
/// 코드에 I/O 주소가 직접 하드코딩된 경우 감지 (AT %IX1.0, AT %QX2.5 등)
/// </summary>
public class HardcodedIOAddressRule : IQARuleChecker
{
    public string RuleId => "QA014";
    public string RuleName => "하드코딩된 I/O 주소 감지";
    public string Description => "코드에 직접 작성된 I/O 주소를 감지하여 유지보수성 향상 권장";
    public Severity Severity => Severity.Warning;

    // I/O 주소 패턴 (AT %IX1.0, AT %QW2.3 등)
    private static readonly Regex IoAddressPattern = new(
        @"AT\s+%[IQM][XBWDL]?\d+\.\d+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public List<QAIssue> CheckVariableChange(VariableChange change)
    {
        var issues = new List<QAIssue>();

        // I/O 매핑 변경은 별도 체크 (IOMappingChange)
        // 여기서는 검사하지 않음
        return issues;
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
        var hardcodedAddresses = FindHardcodedIOAddresses(code);

        if (hardcodedAddresses.Any())
        {
            var addressesText = string.Join(", ", hardcodedAddresses.Distinct());

            issues.Add(new QAIssue
            {
                Severity = Severity.Warning,
                Category = "유지보수성",
                Title = "하드코딩된 I/O 주소 사용",
                Description = $"{change.ElementName}: I/O 주소가 코드에 직접 작성됨 ({addressesText})",
                Location = $"{change.FilePath}:{change.StartLine}",
                FilePath = change.FilePath,
                Line = change.StartLine,
                WhyDangerous = @"
하드코딩된 I/O 주소는 다음 문제를 야기합니다:
- 하드웨어 변경 시 코드 수정 필요
- I/O 매핑 추적 어려움
- 주소 충돌 위험
- 재사용 불가능 (다른 장비 적용 어려움)
- 문서화 및 유지보수 어려움
",
                Recommendation = $@"
✅ 권장 해결 방법:

1. GVL (Global Variable List) 사용
   VAR_GLOBAL
       // 입력 I/O
       iStartButton AT %IX0.0 : BOOL;
       iStopButton AT %IX0.1 : BOOL;
       iSensor1 AT %IX1.0 : BOOL;

       // 출력 I/O
       oMotor AT %QX0.0 : BOOL;
       oLamp AT %QX0.1 : BOOL;
   END_VAR

   // 로직에서 사용
   IF iStartButton THEN
       oMotor := TRUE;
   END_IF

2. Function Block 입력으로 전달
   FUNCTION_BLOCK FB_Controller
       VAR_INPUT
           startButton : BOOL;  // I/O 주소 분리
           sensor : BOOL;
       END_VAR
   END_FUNCTION_BLOCK

   // 호출
   controller(
       startButton := iStartButton,  // GVL의 I/O
       sensor := iSensor1
   );

3. I/O Configuration에서 심볼릭 이름 사용
   - TwinCAT I/O 구성에서 변수 연결
   - 코드에는 심볼릭 이름만 사용

검출된 하드코딩 주소:
{addressesText}

→ GVL로 이동하여 심볼릭 이름 부여 권장
",
                Examples = new List<string>
                {
                    "❌ 나쁜 예 (하드코딩):",
                    "   PROGRAM MAIN",
                    "   VAR",
                    "       sensor AT %IX1.0 : BOOL;  // 하드코딩",
                    "   END_VAR",
                    "   ",
                    "   IF sensor THEN",
                    "       ...",
                    "   END_IF",
                    "",
                    "✅ 좋은 예 (GVL 사용):",
                    "   // GVL_IO.TcGVL",
                    "   VAR_GLOBAL",
                    "       iSensorDoorOpen AT %IX1.0 : BOOL;",
                    "   END_VAR",
                    "   ",
                    "   // MAIN.TcPOU",
                    "   PROGRAM MAIN",
                    "   IF GVL_IO.iSensorDoorOpen THEN",
                    "       // 심볼릭 이름 사용",
                    "   END_IF"
                },
                RuleId = RuleId,
                OldCodeSnippet = change.OldCode ?? string.Empty,
                NewCodeSnippet = $"하드코딩 주소: {addressesText}"
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
    /// 코드에서 하드코딩된 I/O 주소 찾기
    /// </summary>
    private List<string> FindHardcodedIOAddresses(string code)
    {
        var addresses = new List<string>();
        var matches = IoAddressPattern.Matches(code);

        foreach (Match match in matches)
        {
            addresses.Add(match.Value.Trim());
        }

        return addresses;
    }
}
