using System.Text.RegularExpressions;
using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Models.QA;

namespace TwinCatQA.Infrastructure.QA.Rules.TE1200;

#region SA0031 - 미사용 시그니처

/// <summary>
/// SA0031: 미사용 시그니처 감지
/// 호출되지 않는 메서드, 함수의 시그니처
/// </summary>
public class SA0031_UnusedSignatures : SARuleBase
{
    public override string RuleId => "SA0031";
    public override string RuleName => "미사용 시그니처";
    public override string Description => "호출되지 않는 메서드나 함수의 시그니처를 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.UnreachableUnusedCode;
    public override bool SupportsPrecompile => true;

    private static readonly Regex MethodSignaturePattern = new(
        @"METHOD\s+(?:PUBLIC|PRIVATE|PROTECTED|INTERNAL)?\s*(\w+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex FunctionSignaturePattern = new(
        @"FUNCTION\s+(\w+)\s*:",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var declaredMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 모든 METHOD/FUNCTION 선언 수집
        var methodMatches = MethodSignaturePattern.Matches(code);
        foreach (Match match in methodMatches)
        {
            declaredMethods.Add(match.Groups[1].Value);
        }

        var functionMatches = FunctionSignaturePattern.Matches(code);
        foreach (Match match in functionMatches)
        {
            declaredMethods.Add(match.Groups[1].Value);
        }

        // 각 선언된 메서드/함수가 호출되는지 확인
        foreach (var methodName in declaredMethods)
        {
            // 메서드 호출 패턴: methodName( 또는 methodName() 또는 .methodName
            var callPattern = new Regex($@"(?<!METHOD\s+)(?<!FUNCTION\s+)\b{Regex.Escape(methodName)}\s*\(",
                RegexOptions.IgnoreCase);

            var calls = callPattern.Matches(code);

            // 선언 자체를 제외하고 실제 호출이 없으면 미사용
            if (calls.Count == 0)
            {
                var lineNumber = code.IndexOf(methodName, StringComparison.OrdinalIgnoreCase);
                if (lineNumber >= 0)
                {
                    lineNumber = code.Take(lineNumber).Count(c => c == '\n') + 1;

                    issues.Add(CreateIssue(
                        "미사용 메서드",
                        $"메서드 '{methodName}'이(가) 코드 내에서 호출되지 않습니다.",
                        change.FilePath,
                        change.StartLine + lineNumber,
                        "미사용 메서드는 코드 복잡성을 증가시키고 유지보수 비용을 높입니다.",
                        "사용되지 않는 메서드는 삭제하거나 향후 사용 계획을 주석으로 명시하세요.",
                        $"METHOD {methodName}",
                        "// 삭제 또는 사용처 확인"));
                }
            }
        }

        return issues;
    }
}

#endregion

#region SA0032 - 미사용 열거형 상수

/// <summary>
/// SA0032: 미사용 열거형 상수 감지
/// 정의되었지만 사용되지 않는 ENUM 멤버
/// </summary>
public class SA0032_UnusedEnumConstants : SARuleBase
{
    public override string RuleId => "SA0032";
    public override string RuleName => "미사용 열거형 상수";
    public override string Description => "정의되었지만 사용되지 않는 열거형 상수를 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.UnreachableUnusedCode;
    public override bool SupportsPrecompile => true;

    private static readonly Regex EnumDefinitionPattern = new(
        @"TYPE\s+(\w+)\s*:\s*\(\s*([\w\s,:=]+)\s*\)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckDataTypeChange(DataTypeChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewDefinition))
            return issues;

        var code = change.NewDefinition;
        var matches = EnumDefinitionPattern.Matches(code);

        foreach (Match match in matches)
        {
            var enumName = match.Groups[1].Value;
            var members = match.Groups[2].Value.Split(',');
            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;

            // 미사용 상수 패턴 감지 (예: _Reserved, _Unused)
            foreach (var member in members)
            {
                var memberName = member.Split(':')[0].Split('=')[0].Trim();
                if (memberName.Contains("Reserved") || memberName.Contains("Unused") ||
                    memberName.StartsWith("_"))
                {
                    issues.Add(CreateIssue(
                        "잠재적 미사용 열거형 상수",
                        $"열거형 '{enumName}'의 상수 '{memberName}'이(가) 미사용 패턴입니다.",
                        change.FilePath,
                        change.Line + lineNumber,
                        "미사용 열거형 상수는 코드 명확성을 저해합니다.",
                        "실제 사용되지 않는 상수는 제거하거나 주석으로 설명을 추가하세요."));
                }
            }
        }

        return issues;
    }
}

#endregion

#region SA0033 - 미사용 변수

/// <summary>
/// SA0033: 미사용 변수 감지
/// 선언되었지만 사용되지 않는 변수
/// </summary>
public class SA0033_UnusedVariables : SARuleBase
{
    public override string RuleId => "SA0033";
    public override string RuleName => "미사용 변수";
    public override string Description => "선언되었지만 사용되지 않는 변수를 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.UnreachableUnusedCode;
    public override bool SupportsPrecompile => true;

    private static readonly Regex VarDeclarationPattern = new(
        @"^\s*(\w+)\s*:\s*(\w+)",
        RegexOptions.Multiline | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;

        // VAR 블록에서 선언된 모든 변수 수집
        var varBlockPattern = new Regex(@"VAR\s*(.*?)END_VAR",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        var varMatch = varBlockPattern.Match(code);

        if (!varMatch.Success)
            return issues;

        var varBlock = varMatch.Groups[1].Value;
        var varDeclPattern = new Regex(@"^\s*(\w+)\s*:\s*\w+", RegexOptions.Multiline);
        var variables = varDeclPattern.Matches(varBlock);

        // VAR 블록 이후의 코드
        var codeAfterVar = code.Substring(varMatch.Index + varMatch.Length);

        foreach (Match varDecl in variables)
        {
            var varName = varDecl.Groups[1].Value;

            // 코드에서 변수 사용 확인 (선언이 아닌 실제 사용)
            var usagePattern = new Regex($@"\b{Regex.Escape(varName)}\b(?!\s*:)", RegexOptions.IgnoreCase);
            var usages = usagePattern.Matches(codeAfterVar);

            if (usages.Count == 0)
            {
                var lineNumber = code.Take(varMatch.Index).Count(c => c == '\n') +
                                varBlock.Substring(0, varDecl.Index).Count(c => c == '\n') + 1;

                issues.Add(CreateIssue(
                    "미사용 변수",
                    $"변수 '{varName}'이(가) 선언되었지만 사용되지 않습니다.",
                    change.FilePath,
                    change.StartLine + lineNumber,
                    @"
미사용 변수는 다음 문제를 야기합니다:
- 메모리 낭비
- 코드 가독성 저하
- 유지보수 시 혼란
",
                    @"
✅ 권장 해결 방법:
1. 실제 미사용이면 삭제
2. 향후 사용 예정이면 주석으로 설명
3. 디버그용이면 조건부 컴파일 사용
",
                    varDecl.Value,
                    "// 삭제 또는 사용 확인"));
            }
        }

        return issues;
    }
}

#endregion

#region SA0034 - 미사용 입력 변수

/// <summary>
/// SA0034: 미사용 입력 변수 감지
/// VAR_INPUT으로 선언되었지만 사용되지 않는 변수
/// </summary>
public class SA0034_UnusedInputVariables : SARuleBase
{
    public override string RuleId => "SA0034";
    public override string RuleName => "미사용 입력 변수";
    public override string Description => "VAR_INPUT으로 선언되었지만 사용되지 않는 변수를 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.UnreachableUnusedCode;
    public override bool SupportsPrecompile => true;

    private static readonly Regex VarInputBlockPattern = new(
        @"VAR_INPUT\s*(.*?)END_VAR",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var inputMatch = VarInputBlockPattern.Match(code);

        if (inputMatch.Success)
        {
            var inputBlock = inputMatch.Groups[1].Value;
            var varPattern = new Regex(@"(\w+)\s*:\s*\w+", RegexOptions.Compiled);
            var variables = varPattern.Matches(inputBlock);

            foreach (Match varMatch in variables)
            {
                var varName = varMatch.Groups[1].Value;
                // VAR_INPUT 블록 이후의 코드에서 변수 사용 확인
                var codeAfterInput = code.Substring(inputMatch.Index + inputMatch.Length);

                if (!Regex.IsMatch(codeAfterInput, $@"\b{Regex.Escape(varName)}\b"))
                {
                    var lineNumber = code.Take(inputMatch.Index).Count(c => c == '\n') + 1;
                    issues.Add(CreateIssue(
                        "미사용 입력 변수",
                        $"VAR_INPUT 변수 '{varName}'이(가) 로직에서 사용되지 않습니다.",
                        change.FilePath,
                        change.StartLine + lineNumber,
                        "미사용 입력 변수는 인터페이스를 복잡하게 만듭니다.",
                        "사용되지 않는 입력 변수는 제거하거나 향후 사용 계획을 주석으로 명시하세요."));
                }
            }
        }

        return issues;
    }
}

#endregion

#region SA0035 - 미사용 출력 변수

/// <summary>
/// SA0035: 미사용 출력 변수 감지
/// VAR_OUTPUT으로 선언되었지만 값이 할당되지 않는 변수
/// </summary>
public class SA0035_UnusedOutputVariables : SARuleBase
{
    public override string RuleId => "SA0035";
    public override string RuleName => "미사용 출력 변수";
    public override string Description => "VAR_OUTPUT으로 선언되었지만 값이 할당되지 않는 변수를 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.UnreachableUnusedCode;
    public override bool SupportsPrecompile => true;

    private static readonly Regex VarOutputBlockPattern = new(
        @"VAR_OUTPUT\s*(.*?)END_VAR",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var outputMatch = VarOutputBlockPattern.Match(code);

        if (outputMatch.Success)
        {
            var outputBlock = outputMatch.Groups[1].Value;
            var varPattern = new Regex(@"(\w+)\s*:\s*\w+", RegexOptions.Compiled);
            var variables = varPattern.Matches(outputBlock);

            foreach (Match varMatch in variables)
            {
                var varName = varMatch.Groups[1].Value;
                // 출력 변수에 값 할당 확인
                var assignmentPattern = new Regex($@"\b{Regex.Escape(varName)}\s*:=", RegexOptions.Compiled);

                if (!assignmentPattern.IsMatch(code))
                {
                    var lineNumber = code.Take(outputMatch.Index).Count(c => c == '\n') + 1;
                    issues.Add(CreateIssue(
                        "미사용 출력 변수",
                        $"VAR_OUTPUT 변수 '{varName}'에 값이 할당되지 않습니다.",
                        change.FilePath,
                        change.StartLine + lineNumber,
                        "출력 변수에 값이 할당되지 않으면 호출자에게 잘못된 데이터가 전달됩니다.",
                        "출력 변수는 반드시 적절한 값을 할당하거나 불필요시 제거하세요."));
                }
            }
        }

        return issues;
    }
}

#endregion

#region SA0036 - 미사용 VAR_IN_OUT 변수

/// <summary>
/// SA0036: 미사용 VAR_IN_OUT 변수 감지
/// VAR_IN_OUT으로 선언되었지만 사용되지 않는 변수
/// </summary>
public class SA0036_UnusedInOutVariables : SARuleBase
{
    public override string RuleId => "SA0036";
    public override string RuleName => "미사용 VAR_IN_OUT 변수";
    public override string Description => "VAR_IN_OUT으로 선언되었지만 사용되지 않는 변수를 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.UnreachableUnusedCode;
    public override bool SupportsPrecompile => true;

    private static readonly Regex VarInOutBlockPattern = new(
        @"VAR_IN_OUT\s*(.*?)END_VAR",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var inOutMatch = VarInOutBlockPattern.Match(code);

        if (inOutMatch.Success)
        {
            var inOutBlock = inOutMatch.Groups[1].Value;
            var varPattern = new Regex(@"(\w+)\s*:\s*\w+", RegexOptions.Compiled);
            var variables = varPattern.Matches(inOutBlock);

            foreach (Match varMatch in variables)
            {
                var varName = varMatch.Groups[1].Value;
                var codeAfterDecl = code.Substring(inOutMatch.Index + inOutMatch.Length);

                if (!Regex.IsMatch(codeAfterDecl, $@"\b{Regex.Escape(varName)}\b"))
                {
                    var lineNumber = code.Take(inOutMatch.Index).Count(c => c == '\n') + 1;
                    issues.Add(CreateIssue(
                        "미사용 VAR_IN_OUT 변수",
                        $"VAR_IN_OUT 변수 '{varName}'이(가) 로직에서 사용되지 않습니다.",
                        change.FilePath,
                        change.StartLine + lineNumber,
                        "VAR_IN_OUT은 참조 전달이므로 미사용 시 불필요한 오버헤드가 발생합니다.",
                        "사용되지 않는 VAR_IN_OUT 변수는 제거하세요."));
                }
            }
        }

        return issues;
    }
}

#endregion

#region SA0037 - 미사용 임시 변수

/// <summary>
/// SA0037: 미사용 임시 변수 감지
/// VAR_TEMP로 선언되었지만 사용되지 않는 변수
/// </summary>
public class SA0037_UnusedTempVariables : SARuleBase
{
    public override string RuleId => "SA0037";
    public override string RuleName => "미사용 임시 변수";
    public override string Description => "VAR_TEMP로 선언되었지만 사용되지 않는 변수를 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.UnreachableUnusedCode;
    public override bool SupportsPrecompile => true;

    private static readonly Regex VarTempBlockPattern = new(
        @"VAR_TEMP\s*(.*?)END_VAR",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var tempMatch = VarTempBlockPattern.Match(code);

        if (tempMatch.Success)
        {
            var tempBlock = tempMatch.Groups[1].Value;
            var varPattern = new Regex(@"(\w+)\s*:\s*\w+", RegexOptions.Compiled);
            var variables = varPattern.Matches(tempBlock);

            foreach (Match varMatch in variables)
            {
                var varName = varMatch.Groups[1].Value;
                var codeAfterDecl = code.Substring(tempMatch.Index + tempMatch.Length);

                if (!Regex.IsMatch(codeAfterDecl, $@"\b{Regex.Escape(varName)}\b"))
                {
                    var lineNumber = code.Take(tempMatch.Index).Count(c => c == '\n') + 1;
                    issues.Add(CreateIssue(
                        "미사용 임시 변수",
                        $"VAR_TEMP 변수 '{varName}'이(가) 사용되지 않습니다.",
                        change.FilePath,
                        change.StartLine + lineNumber,
                        "미사용 임시 변수는 스택 공간을 낭비합니다.",
                        "사용되지 않는 임시 변수는 제거하세요."));
                }
            }
        }

        return issues;
    }
}

#endregion

#region SA0038 - 쓰기 전용 변수

/// <summary>
/// SA0038: 쓰기 전용 변수 감지
/// 값이 할당되지만 읽히지 않는 변수
/// </summary>
public class SA0038_WriteOnlyVariables : SARuleBase
{
    public override string RuleId => "SA0038";
    public override string RuleName => "쓰기 전용 변수";
    public override string Description => "값이 할당되지만 읽히지 않는 변수를 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.UnreachableUnusedCode;
    public override bool SupportsPrecompile => true;

    public override List<QAIssue> CheckVariableChange(VariableChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed)
            return issues;

        // 쓰기 전용 패턴 (dummy_, unused_ 접두사)
        if (change.VariableName.StartsWith("dummy", StringComparison.OrdinalIgnoreCase) ||
            change.VariableName.StartsWith("unused", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(CreateIssue(
                "쓰기 전용 변수 패턴",
                $"변수 '{change.VariableName}'이(가) 쓰기 전용 패턴의 이름입니다.",
                change.FilePath,
                change.Line,
                "쓰기 전용 변수는 의미 없는 연산을 수행합니다.",
                "변수가 정말 필요한지 검토하고, 불필요하면 제거하세요."));
        }

        return issues;
    }
}

#endregion

#region SA0039 - 읽기 전용 변수

/// <summary>
/// SA0039: 읽기 전용 변수가 VAR로 선언됨
/// CONSTANT로 선언해야 할 변수가 VAR로 선언된 경우
/// </summary>
public class SA0039_ReadOnlyAsVariable : SARuleBase
{
    public override string RuleId => "SA0039";
    public override string RuleName => "상수로 선언 가능한 변수";
    public override string Description => "값이 변경되지 않아 CONSTANT로 선언 가능한 변수를 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.VariablesAndConstants;
    public override bool SupportsPrecompile => true;

    public override List<QAIssue> CheckVariableChange(VariableChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed)
            return issues;

        // 상수 패턴 이름 (MAX_, MIN_, DEFAULT_, _CONST)
        var varName = change.VariableName.ToUpperInvariant();
        if (varName.StartsWith("MAX_") || varName.StartsWith("MIN_") ||
            varName.StartsWith("DEFAULT_") || varName.EndsWith("_CONST") ||
            varName.StartsWith("C_"))
        {
            issues.Add(CreateIssue(
                "상수로 선언 권장",
                $"변수 '{change.VariableName}'이(가) 상수 패턴의 이름을 가지고 있습니다. CONSTANT로 선언을 고려하세요.",
                change.FilePath,
                change.Line,
                "상수 값을 VAR로 선언하면 의도치 않은 수정이 가능합니다.",
                "VAR CONSTANT를 사용하여 값 변경을 방지하세요."));
        }

        return issues;
    }
}

#endregion

#region SA0040 - 0으로 나누기

/// <summary>
/// SA0040: 0으로 나누기 가능성 감지
/// Division by zero 취약점
/// </summary>
public class SA0040_DivisionByZero : SARuleBase
{
    public override string RuleId => "SA0040";
    public override string RuleName => "0으로 나누기";
    public override string Description => "0으로 나누기 가능성이 있는 코드를 감지합니다.";
    public override Severity Severity => Severity.Critical;
    public override SARuleCategory Category => SARuleCategory.Operations;
    public override bool SupportsPrecompile => true;

    // 나누기 연산 패턴 (리터럴 0 또는 변수)
    private static readonly Regex DivisionPattern = new(
        @"(\w+|\([^)]+\))\s*/\s*(\w+|\d+)",
        RegexOptions.Compiled);

    // MOD 연산 패턴
    private static readonly Regex ModPattern = new(
        @"(\w+)\s+MOD\s+(\w+|\d+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var lines = code.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            // 리터럴 0으로 나누기 확인
            var divMatches = DivisionPattern.Matches(line);
            foreach (Match match in divMatches)
            {
                var divisor = match.Groups[2].Value.Trim();

                // 리터럴 0 체크
                if (divisor == "0")
                {
                    issues.Add(CreateIssue(
                        "0으로 나누기",
                        "리터럴 0으로 나누기가 감지되었습니다.",
                        change.FilePath,
                        change.StartLine + i,
                        @"
0으로 나누기는 런타임 오류를 발생시킵니다:
- 프로그램 중단
- 예측 불가능한 동작
- 장비 손상 가능성
",
                        @"
✅ 권장 해결 방법:
IF divisor <> 0 THEN
    result := dividend / divisor;
ELSE
    result := 0; // 또는 에러 처리
END_IF
",
                        match.Value,
                        "IF divisor <> 0 THEN result := value / divisor; END_IF"));
                }
                // 변수로 나누기인 경우 - 체크 없이 나눔
                else if (Regex.IsMatch(divisor, @"^[a-zA-Z_]\w*$"))
                {
                    // 이전 줄에서 divisor != 0 체크가 있는지 확인
                    var contextLines = string.Join("\n", lines.Take(i + 1).TakeLast(Math.Min(5, i + 1)));
                    var hasCheck = Regex.IsMatch(contextLines,
                        $@"IF\s+.*{Regex.Escape(divisor)}\s*(<>|>|<)\s*0",
                        RegexOptions.IgnoreCase);

                    if (!hasCheck)
                    {
                        issues.Add(CreateIssue(
                            "0 체크 없이 나누기",
                            $"변수 '{divisor}'로 나누기 전에 0 체크가 없습니다.",
                            change.FilePath,
                            change.StartLine + i,
                            "0 체크 없는 나누기는 런타임 오류를 발생시킬 수 있습니다.",
                            $"IF {divisor} <> 0 THEN ... END_IF로 보호하세요.",
                            match.Value,
                            $"IF {divisor} <> 0 THEN {match.Value}; END_IF"));
                    }
                }
            }

            // MOD 연산 확인
            var modMatches = ModPattern.Matches(line);
            foreach (Match match in modMatches)
            {
                var divisor = match.Groups[2].Value.Trim();

                if (divisor == "0")
                {
                    issues.Add(CreateIssue(
                        "0으로 MOD 연산",
                        "리터럴 0으로 MOD 연산이 감지되었습니다.",
                        change.FilePath,
                        change.StartLine + i,
                        "0으로 MOD 연산은 런타임 오류를 발생시킵니다.",
                        "제수가 0이 아닌지 확인하세요.",
                        match.Value,
                        $"IF {divisor} <> 0 THEN {match.Value}; END_IF"));
                }
            }
        }

        return issues;
    }
}

#endregion

#region SA0041 - 루프 불변 코드

/// <summary>
/// SA0041: 루프 불변 코드 감지
/// 루프 내부에서 변경되지 않는 연산
/// </summary>
public class SA0041_LoopInvariantCode : SARuleBase
{
    public override string RuleId => "SA0041";
    public override string RuleName => "루프 불변 코드";
    public override string Description => "루프 내부에서 변경되지 않는 연산을 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.Operations;
    public override bool SupportsPrecompile => true;

    // 상수 연산 패턴
    private static readonly Regex ConstantOperationPattern = new(
        @"(\w+)\s*:=\s*(\d+\s*[+\-*/]\s*\d+)\s*;",
        RegexOptions.Compiled);

    private static readonly Regex ForLoopPattern = new(
        @"FOR\s+.*?\s+DO\s*(.*?)END_FOR",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var forMatches = ForLoopPattern.Matches(code);

        foreach (Match forMatch in forMatches)
        {
            var loopBody = forMatch.Groups[1].Value;
            var constOps = ConstantOperationPattern.Matches(loopBody);

            foreach (Match constOp in constOps)
            {
                var lineNumber = code.Take(forMatch.Index).Count(c => c == '\n') + 1;
                issues.Add(CreateIssue(
                    "루프 불변 코드",
                    $"상수 연산 '{constOp.Value}'이(가) 루프 내부에 있습니다.",
                    change.FilePath,
                    change.StartLine + lineNumber,
                    "루프 불변 코드는 불필요한 CPU 사이클을 소비합니다.",
                    "상수 연산은 루프 외부로 이동하세요.",
                    constOp.Value,
                    "// 루프 외부로 이동"));
            }
        }

        return issues;
    }
}

#endregion

#region SA0042 - 일관성 없는 네임스페이스 접근

/// <summary>
/// SA0042: 일관성 없는 네임스페이스 접근 감지
/// 동일 네임스페이스에 대한 다른 접근 방식
/// </summary>
public class SA0042_InconsistentNamespaceAccess : SARuleBase
{
    public override string RuleId => "SA0042";
    public override string RuleName => "일관성 없는 네임스페이스 접근";
    public override string Description => "동일 네임스페이스에 대한 일관성 없는 접근 방식을 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.NamingConventions;
    public override bool SupportsPrecompile => true;

    private static readonly Regex NamespaceAccessPattern = new(
        @"(\w+)\.(\w+)",
        RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var matches = NamespaceAccessPattern.Matches(code);
        var namespaceUsage = new Dictionary<string, HashSet<string>>();

        foreach (Match match in matches)
        {
            var ns = match.Groups[1].Value;
            var member = match.Groups[2].Value;

            if (!namespaceUsage.ContainsKey(ns))
                namespaceUsage[ns] = new HashSet<string>();

            namespaceUsage[ns].Add(member);
        }

        // 동일 네임스페이스에서 많은 멤버 사용 시 USING 권장
        foreach (var kvp in namespaceUsage)
        {
            if (kvp.Value.Count >= 3)
            {
                issues.Add(CreateIssue(
                    "USING 문 사용 권장",
                    $"네임스페이스 '{kvp.Key}'의 멤버가 {kvp.Value.Count}개 이상 사용되었습니다.",
                    change.FilePath,
                    change.StartLine,
                    "반복적인 네임스페이스 접근은 코드 가독성을 저해합니다.",
                    $"USING {kvp.Key}; 선언을 고려하세요."));
            }
        }

        return issues;
    }
}

#endregion

#region SA0043 - 의심스러운 세미콜론

/// <summary>
/// SA0043: 의심스러운 세미콜론 감지
/// 제어문 바로 뒤의 세미콜론
/// </summary>
public class SA0043_SuspiciousSemicolon : SARuleBase
{
    public override string RuleId => "SA0043";
    public override string RuleName => "의심스러운 세미콜론";
    public override string Description => "제어문 바로 뒤의 의심스러운 세미콜론을 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Operations;
    public override bool SupportsPrecompile => true;

    private static readonly Regex SuspiciousSemicolonPattern = new(
        @"(IF\s+.*?\s+THEN)\s*;\s*$|(FOR\s+.*?\s+DO)\s*;\s*$|(WHILE\s+.*?\s+DO)\s*;\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var matches = SuspiciousSemicolonPattern.Matches(code);

        foreach (Match match in matches)
        {
            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
            issues.Add(CreateIssue(
                "의심스러운 세미콜론",
                "제어문 뒤에 빈 세미콜론이 있습니다. 의도된 동작인지 확인하세요.",
                change.FilePath,
                change.StartLine + lineNumber,
                @"
제어문 뒤의 세미콜론은 빈 본문을 의미합니다:
IF condition THEN;  // 아무것도 실행하지 않음
    DoSomething();  // 항상 실행됨
",
                "세미콜론을 제거하거나 의도적인 경우 주석으로 명시하세요.",
                match.Value,
                match.Value.Replace(";", "")));
        }

        return issues;
    }
}

#endregion

#region SA0044 - 괄호 불일치

/// <summary>
/// SA0044: 괄호 불일치 감지
/// 열린 괄호와 닫힌 괄호 수가 다름
/// </summary>
public class SA0044_ParenthesisMismatch : SARuleBase
{
    public override string RuleId => "SA0044";
    public override string RuleName => "괄호 불일치";
    public override string Description => "열린 괄호와 닫힌 괄호의 불일치를 감지합니다.";
    public override Severity Severity => Severity.Critical;
    public override SARuleCategory Category => SARuleCategory.Operations;
    public override bool SupportsPrecompile => true;

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var lines = code.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            // 문자열과 주석 제거
            var cleanLine = Regex.Replace(line, @"'[^']*'", "");
            cleanLine = Regex.Replace(cleanLine, @"//.*$", "");
            cleanLine = Regex.Replace(cleanLine, @"\(\*.*?\*\)", "");

            int openParen = cleanLine.Count(c => c == '(');
            int closeParen = cleanLine.Count(c => c == ')');

            if (openParen != closeParen)
            {
                issues.Add(CreateIssue(
                    "괄호 불일치",
                    $"열린 괄호({openParen}개)와 닫힌 괄호({closeParen}개)가 일치하지 않습니다.",
                    change.FilePath,
                    change.StartLine + i,
                    "괄호 불일치는 컴파일 오류나 논리 오류를 발생시킵니다.",
                    "괄호 개수를 확인하고 수정하세요.",
                    line.Trim(),
                    "// 괄호 확인 필요"));
            }
        }

        return issues;
    }
}

#endregion

#region SA0045 - 비교 대입 혼동

/// <summary>
/// SA0045: 비교와 대입 혼동 감지
/// IF 문에서 := 사용
/// </summary>
public class SA0045_AssignmentInCondition : SARuleBase
{
    public override string RuleId => "SA0045";
    public override string RuleName => "조건문 내 대입";
    public override string Description => "조건문 내에서 대입 연산자 사용을 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Operations;
    public override bool SupportsPrecompile => true;

    private static readonly Regex IfConditionPattern = new(
        @"IF\s+(.*?)\s+THEN",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var matches = IfConditionPattern.Matches(code);

        foreach (Match match in matches)
        {
            var condition = match.Groups[1].Value;

            // 조건문 내에 := 연산자가 있는지 확인
            if (condition.Contains(":="))
            {
                var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
                issues.Add(CreateIssue(
                    "조건문 내 대입 연산",
                    "IF 조건문 내에서 대입 연산자(:=)가 사용되었습니다. 비교 연산자(=)를 의도한 것인지 확인하세요.",
                    change.FilePath,
                    change.StartLine + lineNumber,
                    @"
조건문 내 대입은 혼란을 야기합니다:
- IF a := b THEN  // a에 b 대입 후 a 값으로 조건 판단 (의도하지 않은 동작)
- IF a = b THEN   // a와 b 비교 (일반적인 의도)
",
                    "비교를 의도했다면 = 연산자를 사용하세요.",
                    match.Value,
                    match.Value.Replace(":=", "=")));
            }

            // WHILE, ELSIF 등도 체크
        }

        // WHILE 조건도 체크
        var whilePattern = new Regex(@"WHILE\s+(.*?)\s+DO", RegexOptions.IgnoreCase);
        var whileMatches = whilePattern.Matches(code);

        foreach (Match match in whileMatches)
        {
            var condition = match.Groups[1].Value;

            if (condition.Contains(":="))
            {
                var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
                issues.Add(CreateIssue(
                    "WHILE 조건문 내 대입 연산",
                    "WHILE 조건문 내에서 대입 연산자(:=)가 사용되었습니다.",
                    change.FilePath,
                    change.StartLine + lineNumber,
                    "조건문 내 대입은 의도하지 않은 동작을 유발할 수 있습니다.",
                    "비교 연산자(=)를 사용하거나 대입을 조건문 외부로 이동하세요.",
                    match.Value,
                    match.Value.Replace(":=", "=")));
            }
        }

        return issues;
    }
}

#endregion

#region SA0046 - 불필요한 비교

/// <summary>
/// SA0046: 불필요한 비교 감지
/// BOOL 변수와 TRUE/FALSE 비교
/// </summary>
public class SA0046_UnnecessaryComparison : SARuleBase
{
    public override string RuleId => "SA0046";
    public override string RuleName => "불필요한 비교";
    public override string Description => "BOOL 변수와 TRUE/FALSE의 불필요한 비교를 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.Operations;
    public override bool SupportsPrecompile => true;

    private static readonly Regex UnnecessaryTrueComparePattern = new(
        @"(\w+)\s*=\s*TRUE\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex UnnecessaryFalseComparePattern = new(
        @"(\w+)\s*=\s*FALSE\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;

        var trueMatches = UnnecessaryTrueComparePattern.Matches(code);
        foreach (Match match in trueMatches)
        {
            var varName = match.Groups[1].Value;
            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;

            issues.Add(CreateIssue(
                "불필요한 TRUE 비교",
                $"'{match.Value}'는 '{varName}'로 단순화할 수 있습니다.",
                change.FilePath,
                change.StartLine + lineNumber,
                "BOOL 변수와 TRUE 비교는 불필요합니다.",
                $"IF {varName} THEN 형태로 단순화하세요.",
                match.Value,
                varName));
        }

        var falseMatches = UnnecessaryFalseComparePattern.Matches(code);
        foreach (Match match in falseMatches)
        {
            var varName = match.Groups[1].Value;
            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;

            issues.Add(CreateIssue(
                "불필요한 FALSE 비교",
                $"'{match.Value}'는 'NOT {varName}'로 단순화할 수 있습니다.",
                change.FilePath,
                change.StartLine + lineNumber,
                "BOOL 변수와 FALSE 비교는 불필요합니다.",
                $"IF NOT {varName} THEN 형태로 단순화하세요.",
                match.Value,
                $"NOT {varName}"));
        }

        return issues;
    }
}

#endregion

#region SA0047 - 중복 조건

/// <summary>
/// SA0047: 중복 조건 감지
/// IF-ELSIF 체인에서 동일한 조건
/// </summary>
public class SA0047_DuplicateCondition : SARuleBase
{
    public override string RuleId => "SA0047";
    public override string RuleName => "중복 조건";
    public override string Description => "IF-ELSIF 체인에서 중복된 조건을 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Operations;
    public override bool SupportsPrecompile => true;

    private static readonly Regex ConditionPattern = new(
        @"(?:IF|ELSIF)\s+(.*?)\s+THEN",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var matches = ConditionPattern.Matches(code);
        var conditions = new Dictionary<string, int>();

        foreach (Match match in matches)
        {
            var condition = match.Groups[1].Value.Trim();
            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;

            if (conditions.ContainsKey(condition))
            {
                issues.Add(CreateIssue(
                    "중복 조건",
                    $"조건 '{condition}'이(가) 라인 {conditions[condition]}에서 이미 사용되었습니다.",
                    change.FilePath,
                    change.StartLine + lineNumber,
                    "중복 조건은 두 번째 블록이 절대 실행되지 않음을 의미합니다.",
                    "조건을 수정하거나 중복된 ELSIF 블록을 제거하세요."));
            }
            else
            {
                conditions[condition] = change.StartLine + lineNumber;
            }
        }

        return issues;
    }
}

#endregion

#region SA0048 - 비효율적 문자열 연결

/// <summary>
/// SA0048: 비효율적 문자열 연결 감지
/// 루프 내 반복적 CONCAT
/// </summary>
public class SA0048_InefficientStringConcat : SARuleBase
{
    public override string RuleId => "SA0048";
    public override string RuleName => "비효율적 문자열 연결";
    public override string Description => "루프 내 반복적인 문자열 연결을 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.Operations;
    public override bool SupportsPrecompile => true;

    private static readonly Regex LoopPattern = new(
        @"(?:FOR|WHILE|REPEAT)\s*.*?(?:DO|UNTIL)\s*(.*?)END_(?:FOR|WHILE|REPEAT)",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex ConcatPattern = new(
        @"CONCAT\s*\(",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var loopMatches = LoopPattern.Matches(code);

        foreach (Match loopMatch in loopMatches)
        {
            var loopBody = loopMatch.Groups[1].Value;
            if (ConcatPattern.IsMatch(loopBody))
            {
                var lineNumber = code.Take(loopMatch.Index).Count(c => c == '\n') + 1;
                issues.Add(CreateIssue(
                    "루프 내 문자열 연결",
                    "루프 내에서 CONCAT 함수가 사용되었습니다.",
                    change.FilePath,
                    change.StartLine + lineNumber,
                    "루프 내 반복적인 문자열 연결은 성능을 저하시킵니다.",
                    "가능하면 루프 외부에서 문자열을 구성하거나 배열을 사용하세요."));
            }
        }

        return issues;
    }
}

#endregion

#region SA0049 - 매직 넘버

/// <summary>
/// SA0049: 매직 넘버 감지
/// 의미 없는 상수 리터럴 사용
/// </summary>
public class SA0049_MagicNumbers : SARuleBase
{
    public override string RuleId => "SA0049";
    public override string RuleName => "매직 넘버";
    public override string Description => "코드에서 의미 없는 상수 리터럴 사용을 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.VariablesAndConstants;
    public override bool SupportsPrecompile => true;

    // 허용되는 일반적인 숫자 (단순한 값들)
    private static readonly HashSet<string> AllowedNumbers = new()
    {
        "0", "1", "-1", "2"
    };

    private static readonly Regex MagicNumberPattern = new(
        @"(?<![\w.])(-?\d+\.?\d*)(?![\w.])",
        RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var lines = code.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmedLine = line.TrimStart();

            // 주석이나 선언부는 건너뛰기
            if (trimmedLine.StartsWith("//") ||
                trimmedLine.StartsWith("(*") ||
                trimmedLine.StartsWith("VAR") ||
                line.Contains("CONSTANT"))
                continue;

            // 실제 로직 코드에서만 검사 (대입, 연산, 비교 등)
            if (!line.Contains(":=") && !line.Contains("=") &&
                !line.Contains(">") && !line.Contains("<") &&
                !line.Contains("+") && !line.Contains("-") &&
                !line.Contains("*") && !line.Contains("/"))
                continue;

            var matches = MagicNumberPattern.Matches(line);
            foreach (Match match in matches)
            {
                var number = match.Groups[1].Value;

                // 허용 숫자가 아니고, 파싱 가능하며, 절댓값이 2보다 크면 매직넘버
                if (!AllowedNumbers.Contains(number) &&
                    double.TryParse(number, out double val) &&
                    Math.Abs(val) > 2)
                {
                    issues.Add(CreateIssue(
                        "매직 넘버",
                        $"의미가 불명확한 숫자 '{number}'가 사용되었습니다.",
                        change.FilePath,
                        change.StartLine + i,
                        @"
매직 넘버는 다음 문제를 야기합니다:
- 코드 가독성 저하 (숫자의 의미 불명확)
- 유지보수 어려움 (변경 시 모든 곳을 찾아야 함)
- 실수 가능성 증가
",
                        $@"
✅ 권장 해결 방법:
VAR CONSTANT
    MAX_SPEED : REAL := {number};  // 의미 있는 이름 부여
END_VAR
",
                        line.Trim(),
                        $"// {number}를 상수로 정의 (예: MAX_VALUE, TIMEOUT_MS 등)"));
                }
            }
        }

        return issues;
    }
}

#endregion

#region SA0050 - 복잡한 표현식

/// <summary>
/// SA0050: 복잡한 표현식 감지
/// 과도하게 중첩된 연산자
/// </summary>
public class SA0050_ComplexExpression : SARuleBase
{
    public override string RuleId => "SA0050";
    public override string RuleName => "복잡한 표현식";
    public override string Description => "과도하게 복잡한 표현식을 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Metrics;
    public override bool SupportsPrecompile => true;

    private const int MaxOperatorCount = 5;
    private const int MaxNestingDepth = 4;

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var lines = code.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            // 연산자 수 계산
            int operatorCount = Regex.Matches(line, @"[\+\-\*/]|AND|OR|XOR|NOT|MOD",
                RegexOptions.IgnoreCase).Count;

            // 괄호 중첩 깊이 계산
            int maxDepth = 0;
            int currentDepth = 0;
            foreach (char c in line)
            {
                if (c == '(') currentDepth++;
                else if (c == ')') currentDepth--;
                maxDepth = Math.Max(maxDepth, currentDepth);
            }

            if (operatorCount > MaxOperatorCount)
            {
                issues.Add(CreateIssue(
                    "과다 연산자",
                    $"한 줄에 {operatorCount}개의 연산자가 있습니다. (권장: {MaxOperatorCount}개 이하)",
                    change.FilePath,
                    change.StartLine + i,
                    "복잡한 표현식은 가독성을 저해하고 오류 가능성을 높입니다.",
                    "중간 변수를 사용하여 표현식을 분리하세요."));
            }

            if (maxDepth > MaxNestingDepth)
            {
                issues.Add(CreateIssue(
                    "과도한 괄호 중첩",
                    $"괄호 중첩 깊이가 {maxDepth}입니다. (권장: {MaxNestingDepth} 이하)",
                    change.FilePath,
                    change.StartLine + i,
                    "깊은 괄호 중첩은 코드 이해를 어렵게 합니다.",
                    "중간 변수나 함수를 사용하여 표현식을 단순화하세요."));
            }
        }

        return issues;
    }
}

#endregion
