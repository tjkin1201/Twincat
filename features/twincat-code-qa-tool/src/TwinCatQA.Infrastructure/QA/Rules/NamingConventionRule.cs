using System.Text.RegularExpressions;
using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Models.QA;
using TwinCatQA.Domain.Services;

namespace TwinCatQA.Infrastructure.QA.Rules;

/// <summary>
/// 명명 규칙 검사 규칙 (Warning)
/// TwinCAT/PLC 명명 규칙 위반 감지 (FB_, I_, O_, g 등)
/// </summary>
public class NamingConventionRule : IQARuleChecker
{
    public string RuleId => "QA012";
    public string RuleName => "명명 규칙 검사";
    public string Description => "TwinCAT/PLC 명명 규칙 위반을 감지하여 코드 일관성 향상";
    public Severity Severity => Severity.Warning;

    public List<QAIssue> CheckVariableChange(VariableChange change)
    {
        var issues = new List<QAIssue>();

        // 추가되거나 수정된 변수만 검사
        if (change.ChangeType != ChangeType.Added && change.ChangeType != ChangeType.Modified)
        {
            return issues;
        }

        var violations = CheckNamingConvention(change.VariableName, change.Scope, change.NewDataType ?? string.Empty);

        foreach (var violation in violations)
        {
            issues.Add(new QAIssue
            {
                Severity = Severity.Warning,
                Category = "명명 규칙",
                Title = "명명 규칙 위반",
                Description = $"{change.VariableName}: {violation}",
                Location = $"{change.FilePath}:{change.Line}",
                FilePath = change.FilePath,
                Line = change.Line,
                WhyDangerous = @"
명명 규칙 위반은 다음 문제를 야기합니다:
- 코드 가독성 저하
- 변수 용도 파악 어려움
- 팀 협업 시 혼란
- 유지보수 비용 증가
- 일관성 없는 코드베이스
",
                Recommendation = $@"
✅ 권장 명명 규칙:

1. Function Block
   FB_ConveyorControl  // FB_ 접두사

2. 전역 변수
   gSystemStatus       // g 접두사
   GVL.temperature     // GVL (Global Variable List)

3. 입력 변수
   iSensorValue        // i 접두사 (또는 in)

4. 출력 변수
   oMotorSpeed         // o 접두사 (또는 out)

5. 로컬 변수
   currentSpeed        // 소문자 카멜 케이스
   isRunning           // is/has 접두사 (BOOL)

6. 상수
   MAX_SPEED           // 대문자 스네이크 케이스
   PI_VALUE

7. 데이터 타입
   TYPE T_SensorData   // T_ 접두사
   TYPE E_ErrorCode    // E_ 접두사 (열거형)
   TYPE ST_Config      // ST_ 접두사 (구조체)

올바른 이름: {GetSuggestedName(change.VariableName, change.Scope, change.NewDataType ?? string.Empty)}
",
                Examples = new List<string>
                {
                    "❌ 잘못된 예:",
                    "   sensor : INT;  // 범위 불명확",
                    "   x : BOOL;      // 의미 불명확",
                    "   MyFB : ...     // FB_ 접두사 없음",
                    "",
                    "✅ 올바른 예:",
                    "   VAR_GLOBAL",
                    "       gSystemReady : BOOL;  // 전역",
                    "   END_VAR",
                    "   ",
                    "   VAR_INPUT",
                    "       iTemperature : REAL;  // 입력",
                    "   END_VAR",
                    "   ",
                    "   FUNCTION_BLOCK FB_MotorControl  // FB"
                },
                RuleId = RuleId,
                OldCodeSnippet = change.OldDataType ?? string.Empty,
                NewCodeSnippet = change.VariableName
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
        var issues = new List<QAIssue>();

        // 추가된 데이터 타입만 검사
        if (change.ChangeType != ChangeType.Added)
        {
            return issues;
        }

        var violations = CheckDataTypeNaming(change.TypeName, change.Kind);

        foreach (var violation in violations)
        {
            issues.Add(new QAIssue
            {
                Severity = Severity.Warning,
                Category = "명명 규칙",
                Title = "데이터 타입 명명 규칙 위반",
                Description = $"{change.TypeName}: {violation}",
                Location = $"{change.FilePath}:{change.Line}",
                FilePath = change.FilePath,
                Line = change.Line,
                WhyDangerous = "데이터 타입 명명 규칙 위반은 타입 식별을 어렵게 만듭니다.",
                Recommendation = $@"
권장 접두사:
- 구조체: ST_ (예: ST_SensorData)
- 열거형: E_ (예: E_MotorState)
- 함수 블록: FB_ (예: FB_Controller)
- 일반 타입: T_ (예: T_CustomArray)
",
                Examples = new List<string>(),
                RuleId = RuleId,
                OldCodeSnippet = string.Empty,
                NewCodeSnippet = change.TypeName
            });
        }

        return issues;
    }

    /// <summary>
    /// 명명 규칙 검사
    /// </summary>
    private List<string> CheckNamingConvention(string name, string scope, string dataType)
    {
        var violations = new List<string>();

        // 전역 변수 검사
        if (scope.ToUpper().Contains("GLOBAL"))
        {
            if (!name.StartsWith("g") && !name.StartsWith("G"))
            {
                violations.Add("전역 변수는 'g' 접두사 사용 권장 (예: gSystemStatus)");
            }
        }

        // 입력 변수 검사
        if (scope.ToUpper().Contains("INPUT"))
        {
            if (!name.StartsWith("i") && !name.StartsWith("in"))
            {
                violations.Add("입력 변수는 'i' 또는 'in' 접두사 사용 권장 (예: iTemperature)");
            }
        }

        // 출력 변수 검사
        if (scope.ToUpper().Contains("OUTPUT"))
        {
            if (!name.StartsWith("o") && !name.StartsWith("out"))
            {
                violations.Add("출력 변수는 'o' 또는 'out' 접두사 사용 권장 (예: oSpeed)");
            }
        }

        // BOOL 변수는 is/has 접두사 권장
        if (dataType.ToUpper() == "BOOL" && scope.ToUpper() == "VAR")
        {
            if (!Regex.IsMatch(name, @"^(is|has|can|should)", RegexOptions.IgnoreCase))
            {
                violations.Add("BOOL 변수는 'is', 'has' 등의 접두사 사용 권장 (예: isRunning)");
            }
        }

        // 한 글자 변수명 금지 (i, j, k 제외)
        if (name.Length == 1 && !new[] { "i", "j", "k" }.Contains(name.ToLower()))
        {
            violations.Add("한 글자 변수명은 피하세요 (의미 있는 이름 사용)");
        }

        // 너무 긴 변수명 (50자 초과)
        if (name.Length > 50)
        {
            violations.Add("변수명이 너무 길어 가독성이 떨어집니다 (50자 이하 권장)");
        }

        return violations;
    }

    /// <summary>
    /// 데이터 타입 명명 규칙 검사
    /// </summary>
    private List<string> CheckDataTypeNaming(string typeName, TwinCatQA.Domain.Models.DataTypeKind kind)
    {
        var violations = new List<string>();

        switch (kind)
        {
            case DataTypeKind.Struct:
                if (!typeName.StartsWith("ST_") && !typeName.StartsWith("T_"))
                {
                    violations.Add("구조체는 'ST_' 또는 'T_' 접두사 사용 권장 (예: ST_SensorData)");
                }
                break;

            case DataTypeKind.Enum:
                if (!typeName.StartsWith("E_"))
                {
                    violations.Add("열거형은 'E_' 접두사 사용 권장 (예: E_MotorState)");
                }
                break;
        }

        return violations;
    }

    /// <summary>
    /// 제안 이름 생성
    /// </summary>
    private string GetSuggestedName(string name, string scope, string dataType)
    {
        if (scope.ToUpper().Contains("GLOBAL"))
            return "g" + char.ToUpper(name[0]) + name.Substring(1);

        if (scope.ToUpper().Contains("INPUT"))
            return "i" + char.ToUpper(name[0]) + name.Substring(1);

        if (scope.ToUpper().Contains("OUTPUT"))
            return "o" + char.ToUpper(name[0]) + name.Substring(1);

        if (dataType.ToUpper() == "BOOL")
            return "is" + char.ToUpper(name[0]) + name.Substring(1);

        return name;
    }
}
