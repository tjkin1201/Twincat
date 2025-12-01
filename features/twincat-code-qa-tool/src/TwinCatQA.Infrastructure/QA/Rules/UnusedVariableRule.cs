using System.Text.RegularExpressions;
using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Models.QA;
using TwinCatQA.Domain.Services;

namespace TwinCatQA.Infrastructure.QA.Rules;

/// <summary>
/// 사용하지 않는 변수 감지 규칙 (Warning)
/// 선언되었지만 코드에서 사용되지 않는 변수 감지
/// </summary>
public class UnusedVariableRule : IQARuleChecker
{
    public string RuleId => "QA006";
    public string RuleName => "사용하지 않는 변수 감지";
    public string Description => "선언되었지만 코드에서 사용되지 않는 변수를 검사하여 코드 품질 향상";
    public Severity Severity => Severity.Warning;

    public List<QAIssue> CheckVariableChange(VariableChange change)
    {
        var issues = new List<QAIssue>();

        // 추가된 변수만 검사 (삭제/수정은 검사 안 함)
        if (change.ChangeType != ChangeType.Added)
        {
            return issues;
        }

        // 변수 이름이 매우 일반적이거나 임시 변수로 보이는 경우 경고
        if (IsSuspiciousVariableName(change.VariableName))
        {
            issues.Add(new QAIssue
            {
                Severity = Severity.Warning,
                Category = "코드 품질",
                Title = "사용되지 않을 가능성이 있는 변수",
                Description = $"{change.VariableName}: 변수 이름이 임시/테스트 목적으로 보임",
                Location = $"{change.FilePath}:{change.Line}",
                FilePath = change.FilePath,
                Line = change.Line,
                WhyDangerous = @"
사용되지 않는 변수는 다음 문제를 야기합니다:
- 메모리 낭비 (PLC 메모리는 제한적)
- 코드 가독성 저하
- 유지보수 혼란 (실제 사용 여부 불분명)
- 디버깅 시 불필요한 변수 추적
",
                Recommendation = $@"
✅ 권장 해결 방법:

1. 변수가 실제로 사용되는지 확인
   - 로직 블록에서 {change.VariableName} 사용 확인
   - 사용되지 않으면 삭제

2. 임시 변수는 명확한 이름 사용
   ❌ temp, tmp, test, dummy
   ✅ calculatedSpeed, tempPressure, testCounter

3. 주석으로 사용 목적 명시
   // 향후 센서 연결 예정
   {change.VariableName} : {change.NewDataType};
",
                Examples = new List<string>
                {
                    "❌ 나쁜 예:",
                    "   temp : INT;  // 어디에 사용?",
                    "   test : BOOL; // 테스트용인가, 운영용인가?",
                    "",
                    "✅ 좋은 예:",
                    "   calculatedSpeed : INT;  // 계산된 속도",
                    "   isMotorRunning : BOOL;  // 모터 동작 상태"
                },
                RuleId = RuleId,
                OldCodeSnippet = string.Empty,
                NewCodeSnippet = $"{change.VariableName} : {change.NewDataType}"
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
    /// 의심스러운 변수 이름 검사
    /// </summary>
    private bool IsSuspiciousVariableName(string name)
    {
        var suspiciousPatterns = new[]
        {
            @"^temp$", @"^tmp$", @"^test$", @"^dummy$",
            @"^temp\d+$", @"^tmp\d+$", @"^test\d+$",
            @"^var\d+$", @"^x$", @"^y$", @"^z$",
            @"^foo$", @"^bar$", @"^baz$"
        };

        return suspiciousPatterns.Any(pattern =>
            Regex.IsMatch(name, pattern, RegexOptions.IgnoreCase));
    }
}
