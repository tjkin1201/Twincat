using System.Text.RegularExpressions;
using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Models.QA;
using TwinCatQA.Domain.Services;

namespace TwinCatQA.Infrastructure.QA.Rules;

/// <summary>
/// 매직 넘버 감지 규칙 (Warning)
/// 의미 없는 숫자 상수를 직접 코드에 사용하는 것을 감지
/// </summary>
public class MagicNumberRule : IQARuleChecker
{
    public string RuleId => "QA007";
    public string RuleName => "매직 넘버 감지";
    public string Description => "코드에 직접 사용된 의미 불명확한 숫자 상수를 감지하여 가독성 향상";
    public Severity Severity => Severity.Warning;

    // 허용되는 숫자 (0, 1, -1, 100 등 일반적인 값)
    private static readonly HashSet<string> AllowedNumbers = new()
    {
        "0", "1", "-1", "2", "10", "100", "1000"
    };

    public List<QAIssue> CheckVariableChange(VariableChange change)
    {
        var issues = new List<QAIssue>();

        // 초기값에 매직 넘버가 있는지 검사
        if (change.ChangeType == ChangeType.Added || change.ChangeType == ChangeType.Modified)
        {
            var initialValue = change.NewInitialValue ?? string.Empty;
            if (IsMagicNumber(initialValue))
            {
                issues.Add(new QAIssue
                {
                    Severity = Severity.Warning,
                    Category = "코드 품질",
                    Title = "매직 넘버 사용",
                    Description = $"{change.VariableName}: 초기값 '{initialValue}'의 의미가 불분명함",
                    Location = $"{change.FilePath}:{change.Line}",
                    FilePath = change.FilePath,
                    Line = change.Line,
                    WhyDangerous = @"
매직 넘버(의미 불명확한 숫자 상수)는 다음 문제를 야기합니다:
- 코드 의도 파악 어려움 (3은 무엇을 의미?)
- 유지보수 시 숫자 변경 누락 위험
- 같은 값을 여러 곳에서 다르게 수정 가능
- 코드 리뷰 시 검증 어려움
",
                    Recommendation = $@"
✅ 권장 해결 방법:

1. 상수로 정의하여 의미 명확화
   VAR CONSTANT
       MAX_RETRY_COUNT : INT := {initialValue};  // {change.VariableName}의 의미
   END_VAR

   {change.VariableName} := MAX_RETRY_COUNT;

2. 열거형 사용 (의미 있는 값들)
   TYPE E_Status :
   (
       IDLE := 0,
       RUNNING := 1,
       ERROR := {initialValue}
   );
   END_TYPE

3. 주석으로 의미 설명
   {change.VariableName} : {change.NewDataType} := {initialValue};  // 최대 재시도 횟수
",
                    Examples = new List<string>
                    {
                        "❌ 나쁜 예:",
                        "   timeout : INT := 500;  // 500이 무엇?",
                        "   speed := 75;  // 75는 무슨 단위?",
                        "",
                        "✅ 좋은 예:",
                        "   VAR CONSTANT",
                        "       MOTOR_TIMEOUT_MS : INT := 500;  // 모터 타임아웃 500ms",
                        "       NORMAL_SPEED_PERCENT : INT := 75;  // 정상 속도 75%",
                        "   END_VAR"
                    },
                    RuleId = RuleId,
                    OldCodeSnippet = change.OldInitialValue ?? string.Empty,
                    NewCodeSnippet = $"{change.VariableName} := {initialValue}"
                });
            }
        }

        return issues;
    }

    public List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        // 로직 코드에서 매직 넘버 검사
        var code = change.NewCode ?? string.Empty;
        var magicNumbers = FindMagicNumbersInCode(code);

        if (magicNumbers.Any())
        {
            var numbersText = string.Join(", ", magicNumbers.Distinct());
            issues.Add(new QAIssue
            {
                Severity = Severity.Warning,
                Category = "코드 품질",
                Title = "로직 내 매직 넘버 사용",
                Description = $"{change.ElementName}: 코드에 의미 불명확한 숫자 사용 ({numbersText})",
                Location = $"{change.FilePath}:{change.StartLine}",
                FilePath = change.FilePath,
                Line = change.StartLine,
                WhyDangerous = @"
로직 내 매직 넘버는 유지보수를 어렵게 만듭니다:
- 값 변경 시 모든 위치를 찾아 수정해야 함
- 같은 숫자라도 다른 의미일 수 있음
- 코드 리뷰 시 의도 파악 어려움
",
                Recommendation = $@"
✅ 권장 해결 방법:

1. 상수 정의
   VAR CONSTANT
       MAX_TEMPERATURE : REAL := 80.0;
       MIN_PRESSURE : REAL := 2.5;
   END_VAR

2. 로직에서 상수 사용
   IF temperature > MAX_TEMPERATURE THEN
       // 처리
   END_IF

3. 주석 추가 (임시 해결책)
   IF temperature > 80.0 THEN  // 최대 허용 온도 80도
",
                Examples = new List<string>
                {
                    "매직 넘버 발견: " + numbersText
                },
                RuleId = RuleId,
                OldCodeSnippet = change.OldCode ?? string.Empty,
                NewCodeSnippet = code
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
    /// 매직 넘버 여부 확인
    /// </summary>
    private bool IsMagicNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        // 숫자가 아니면 false
        if (!Regex.IsMatch(value, @"^-?\d+(\.\d+)?$"))
            return false;

        // 허용된 숫자면 false
        if (AllowedNumbers.Contains(value))
            return false;

        return true;
    }

    /// <summary>
    /// 코드에서 매직 넘버 찾기
    /// </summary>
    private List<string> FindMagicNumbersInCode(string code)
    {
        var magicNumbers = new List<string>();

        // 숫자 패턴 찾기 (정수, 실수)
        var numberPattern = @"\b(\d+\.\d+|\d+)\b";
        var matches = Regex.Matches(code, numberPattern);

        foreach (Match match in matches)
        {
            var number = match.Value;
            if (IsMagicNumber(number))
            {
                magicNumbers.Add(number);
            }
        }

        return magicNumbers;
    }
}
