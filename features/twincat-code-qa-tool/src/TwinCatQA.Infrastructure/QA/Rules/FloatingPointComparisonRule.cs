using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Models.QA;
using TwinCatQA.Domain.Services;
using System.Text.RegularExpressions;

namespace TwinCatQA.Infrastructure.QA.Rules;

/// <summary>
/// 부동소수점 직접 비교 감지 규칙 (Critical)
/// REAL/LREAL 타입의 등호(=) 비교 감지
/// </summary>
public class FloatingPointComparisonRule : IQARuleChecker
{
    public string RuleId => "QA005";
    public string RuleName => "부동소수점 직접 비교";
    public string Description => "REAL/LREAL 타입을 등호(=)로 직접 비교하여 오작동 가능성 검사";
    public Severity Severity => Severity.Critical;

    // 부동소수점 타입
    private static readonly HashSet<string> FloatTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "REAL", "LREAL"
    };

    // 부동소수점 비교 패턴: floatVar = value, floatVar <> value
    // 주의: := (할당)은 제외, = 와 <> (비교)만 검사
    private static readonly Regex FloatComparisonPattern = new Regex(
        @"(\w+)\s*(=|<>)\s*([\w\.\-\+]+)(?!\s*;)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    // 할당문 제외 패턴: :=
    private static readonly Regex AssignmentPattern = new Regex(
        @":=",
        RegexOptions.Compiled
    );

    // 권장 비교 함수 패턴
    private static readonly Regex SafeComparisonPattern = new Regex(
        @"ABS\s*\(\s*(\w+)\s*-\s*(\w+)\s*\)\s*<\s*[\d\.]+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    // 기본 epsilon 값
    private const string DEFAULT_EPSILON = "1.0E-6";

    public List<QAIssue> CheckVariableChange(VariableChange change)
    {
        // 변수 변경에서는 검사하지 않음
        return new List<QAIssue>();
    }

    public List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        // 새로 추가되거나 수정된 로직만 검사
        if (change.ChangeType != ChangeType.Added &&
            change.ChangeType != ChangeType.Modified)
        {
            return issues;
        }

        var codeToCheck = change.ChangeType == ChangeType.Added
            ? change.NewCode
            : change.NewCode;

        // IF, WHILE, ELSIF 등 조건문에서 비교 찾기
        if (!IsConditionalStatement(codeToCheck))
        {
            return issues;
        }

        // 부동소수점 비교 패턴 찾기
        var comparisons = FloatComparisonPattern.Matches(codeToCheck);

        foreach (Match match in comparisons)
        {
            var leftOperand = match.Groups[1].Value;
            var @operator = match.Groups[2].Value;
            var rightOperand = match.Groups[3].Value;

            // 할당문(:=)은 제외
            var fullMatch = match.Value;
            if (AssignmentPattern.IsMatch(codeToCheck.Substring(
                Math.Max(0, match.Index - 1),
                Math.Min(2, match.Index))))
            {
                continue;
            }

            // 안전한 비교 패턴이 이미 사용되었는지 확인
            if (SafeComparisonPattern.IsMatch(codeToCheck))
            {
                continue;
            }

            // 부동소수점 변수인지 추정 (실제로는 타입 정보 필요)
            // 현재는 변수명이나 값으로 추정
            if (LooksLikeFloat(leftOperand) || LooksLikeFloat(rightOperand))
            {
                issues.Add(new QAIssue
                {
                    Severity = Severity.Critical,
                    Category = "부동소수점 연산",
                    Title = "부동소수점 직접 비교 사용",
                    Description = $"{leftOperand} {@operator} {rightOperand}: REAL/LREAL 타입을 직접 비교하여 오작동 가능",
                    Location = $"{change.FilePath}:{change.StartLine}",
                    FilePath = change.FilePath,
                    Line = change.StartLine,
                    WhyDangerous = $@"
부동소수점 연산의 특성상 정확히 같은 값을 가지기 어렵습니다:
1. 반올림 오차 누적 (0.1 + 0.2 ≠ 0.3)
2. 서로 다른 연산 순서에 따른 미세한 차이
3. 컴파일러 최적화에 따른 정밀도 변화
4. 하드웨어 부동소수점 유닛(FPU)의 구현 차이

예시:
  result := 0.1 + 0.2;
  IF result = 0.3 THEN  // FALSE가 될 수 있음!
      // 이 코드는 실행되지 않을 수 있음
  END_IF

실제 값: 0.30000000000000004 (부동소수점 표현의 한계)
",
                    Recommendation = $@"
✅ 권장 해결 방법:

1. Epsilon 비교 사용 (가장 권장)
   VAR CONSTANT
       EPSILON : LREAL := {DEFAULT_EPSILON}; // 허용 오차
   END_VAR

   IF ABS({leftOperand} - {rightOperand}) < EPSILON THEN
       // 값이 ""거의 같음""
   END_IF

2. 범위 비교 사용
   IF {leftOperand} >= ({rightOperand} - {DEFAULT_EPSILON}) AND
      {leftOperand} <= ({rightOperand} + {DEFAULT_EPSILON}) THEN
       // 허용 범위 내에 있음
   END_IF

3. 부등호 비교로 변경 (가능한 경우)
   // = 대신 >= 또는 <= 사용
   IF {leftOperand} >= {rightOperand} THEN
       // ...
   END_IF

4. 정수 연산으로 변환 (가능한 경우)
   // 0.1 단위 비교 → 1 단위 비교
   intValue := REAL_TO_DINT({leftOperand} * 10.0);
   intTarget := REAL_TO_DINT({rightOperand} * 10.0);
   IF intValue = intTarget THEN
       // 정확한 비교 가능
   END_IF
",
                    Examples = new List<string>
                    {
                        $"❌ 위험: IF {leftOperand} = {rightOperand} THEN",
                        "             // 부동소수점 직접 비교!",
                        "         END_IF",
                        "",
                        $"✅ 안전: CONST EPSILON : LREAL := {DEFAULT_EPSILON}; END_CONST",
                        $"         IF ABS({leftOperand} - {rightOperand}) < EPSILON THEN",
                        "             // 안전한 비교",
                        "         END_IF",
                        "",
                        "// 실제 사례",
                        "temperature : REAL := 25.5;",
                        "setpoint : REAL := 25.5;",
                        "",
                        "// 잘못된 방법",
                        "IF temperature = setpoint THEN  // 위험!",
                        "",
                        "// 올바른 방법",
                        "IF ABS(temperature - setpoint) < 0.1 THEN  // 안전!"
                    },
                    RuleId = RuleId,
                    OldCodeSnippet = change.ChangeType == ChangeType.Modified ? change.OldCode : "",
                    NewCodeSnippet = match.Value
                });
            }
        }

        return issues;
    }

    public List<QAIssue> CheckDataTypeChange(DataTypeChange change)
    {
        var issues = new List<QAIssue>();

        // DataTypeChange에서는 구조체 필드 변경 시 부동소수점 타입 검사
        if (change.ChangeType == ChangeType.Modified)
        {
            foreach (var fieldChange in change.FieldChanges)
            {
                // 정수형에서 부동소수점으로 변경된 경우
                if (!IsFloatType(fieldChange.OldDataType ?? string.Empty) &&
                    IsFloatType(fieldChange.NewDataType ?? string.Empty))
                {
                    issues.Add(new QAIssue
                    {
                        Severity = Severity.Warning,
                        Category = "타입 안전성",
                        Title = "부동소수점 타입으로 변경됨",
                        Description = $"{change.TypeName}.{fieldChange.FieldName}: 부동소수점 타입으로 변경되어 등호 비교 주의 필요",
                        Location = $"{change.FilePath}:{change.Line}",
                        FilePath = change.FilePath,
                        Line = change.Line,
                        WhyDangerous = "부동소수점 타입은 직접 등호(=) 비교 시 예상치 못한 결과가 발생할 수 있습니다.",
                        Recommendation = $@"
이 필드를 사용하는 모든 비교문을 검토하세요:
1. = 비교를 epsilon 비교로 변경
2. 범위 체크로 변경
3. 필요시 정수 연산으로 변환

IF ABS(instance.{fieldChange.FieldName} - target) < {DEFAULT_EPSILON} THEN
    // ...
END_IF
",
                        Examples = new List<string>
                        {
                            "// 기존 정수형 비교 코드 검토 필요",
                            "// 부동소수점 안전 비교로 변경 필요"
                        },
                        RuleId = RuleId,
                        OldCodeSnippet = $"{fieldChange.FieldName} : {fieldChange.OldDataType}",
                        NewCodeSnippet = $"{fieldChange.FieldName} : {fieldChange.NewDataType}"
                    });
                }
            }
        }

        return issues;
    }

    /// <summary>
    /// 조건문 여부 확인
    /// </summary>
    private bool IsConditionalStatement(string code)
    {
        return code.Contains("IF", StringComparison.OrdinalIgnoreCase) ||
               code.Contains("WHILE", StringComparison.OrdinalIgnoreCase) ||
               code.Contains("ELSIF", StringComparison.OrdinalIgnoreCase) ||
               code.Contains("UNTIL", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 부동소수점 타입인지 추정
    /// </summary>
    private bool LooksLikeFloat(string operand)
    {
        // 소수점이 포함되어 있으면 부동소수점으로 추정
        if (operand.Contains("."))
        {
            return true;
        }

        // 과학적 표기법 (1.23E-4)
        if (operand.Contains("E", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // 일반적인 부동소수점 변수명 패턴
        var floatVarPatterns = new[]
        {
            "temp", "speed", "position", "voltage", "current",
            "pressure", "flow", "rate", "ratio", "factor",
            "real", "float", "double"
        };

        return floatVarPatterns.Any(pattern =>
            operand.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 부동소수점 타입 여부 확인
    /// </summary>
    private bool IsFloatType(string typeDefinition)
    {
        if (string.IsNullOrEmpty(typeDefinition))
        {
            return false;
        }

        return FloatTypes.Any(ft =>
            typeDefinition.Contains(ft, StringComparison.OrdinalIgnoreCase));
    }
}
