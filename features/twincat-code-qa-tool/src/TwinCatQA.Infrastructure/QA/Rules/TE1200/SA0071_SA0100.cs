using System.Text.RegularExpressions;
using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Models.QA;

namespace TwinCatQA.Infrastructure.QA.Rules.TE1200;

#region SA0071 - ELSE 없는 IF-ELSIF 체인

/// <summary>
/// SA0071: ELSE 없는 IF-ELSIF 체인 감지
/// 모든 경우를 처리하지 않는 조건문
/// </summary>
public class SA0071_MissingElse : SARuleBase
{
    public override string RuleId => "SA0071";
    public override string RuleName => "ELSE 누락";
    public override string Description => "IF-ELSIF 체인에서 ELSE가 누락된 경우를 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.Operations;
    public override bool SupportsPrecompile => true;

    private static readonly Regex IfBlockPattern = new(
        @"IF\s+.*?END_IF",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var matches = IfBlockPattern.Matches(code);

        foreach (Match match in matches)
        {
            var ifBlock = match.Value;

            // ELSIF가 있는지 확인
            bool hasElsif = ifBlock.Contains("ELSIF", StringComparison.OrdinalIgnoreCase);

            if (!hasElsif)
                continue; // ELSIF가 없으면 검사 안 함

            // ELSE가 있는지 확인 (ELSIF 이후에)
            int lastElsifIndex = ifBlock.LastIndexOf("ELSIF", StringComparison.OrdinalIgnoreCase);
            string afterLastElsif = ifBlock.Substring(lastElsifIndex);

            // ELSIF 이후에 ELSE가 없으면 (ELSIF만 있고 ELSE 없음)
            if (!afterLastElsif.Contains("ELSE", StringComparison.OrdinalIgnoreCase) ||
                afterLastElsif.IndexOf("ELSE", StringComparison.OrdinalIgnoreCase) == 0) // ELSIF의 ELSE 부분
            {
                var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
                issues.Add(CreateIssue(
                    "ELSE 누락",
                    "IF-ELSIF 체인에 기본 ELSE 절이 없습니다.",
                    change.FilePath,
                    change.StartLine + lineNumber,
                    "ELSE 없는 조건문은 예상치 못한 경우를 처리하지 않습니다.",
                    "기본 ELSE 절을 추가하여 모든 경우를 처리하세요."));
            }
        }

        return issues;
    }
}

#endregion

#region SA0072 - DEFAULT 없는 CASE

/// <summary>
/// SA0072: DEFAULT 없는 CASE 감지
/// ELSE 분기가 없는 CASE 문
/// </summary>
public class SA0072_CaseMissingDefault : SARuleBase
{
    public override string RuleId => "SA0072";
    public override string RuleName => "CASE DEFAULT 누락";
    public override string Description => "CASE 문에서 ELSE(DEFAULT) 분기가 누락된 경우를 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Operations;
    public override bool SupportsPrecompile => true;

    private static readonly Regex CaseBlockPattern = new(
        @"CASE\s+[\w\.\[\]]+\s+OF\s*(.*?)END_CASE",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var matches = CaseBlockPattern.Matches(code);

        foreach (Match match in matches)
        {
            var caseBody = match.Groups[1].Value;

            // 주석 제거 (간단한 방법)
            var cleanBody = Regex.Replace(caseBody, @"//.*?$|(?s)/\*.*?\*/|\(\*.*?\*\)", "", RegexOptions.Multiline);

            // ELSE 또는 DEFAULT 확인
            if (!Regex.IsMatch(cleanBody, @"\bELSE\b", RegexOptions.IgnoreCase))
            {
                var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
                issues.Add(CreateIssue(
                    "CASE ELSE 누락",
                    "CASE 문에 기본 ELSE 분기가 없습니다.",
                    change.FilePath,
                    change.StartLine + lineNumber,
                    @"
ELSE 없는 CASE 문은 다음 문제를 야기합니다:
- 예상치 못한 값에 대한 처리 누락
- 디버깅 어려움
- 상태 머신의 불완전한 전이
",
                    @"
✅ 권장 해결 방법:
CASE nState OF
    0: ...
    1: ...
    ELSE
        // 예외 처리 또는 에러 로깅
END_CASE
"));
            }
        }

        return issues;
    }
}

#endregion

#region SA0073 - 네이밍 규칙 위반 (변수)

/// <summary>
/// SA0073: 변수 네이밍 규칙 위반 감지
/// 표준 명명 규칙 미준수
/// </summary>
public class SA0073_VariableNamingViolation : SARuleBase
{
    public override string RuleId => "SA0073";
    public override string RuleName => "변수 명명 규칙 위반";
    public override string Description => "표준 변수 명명 규칙을 위반하는 경우를 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.NamingConventions;
    public override bool SupportsPrecompile => true;

    // 헝가리안 표기법 접두사
    private static readonly Dictionary<string, string[]> TypePrefixes = new()
    {
        { "BOOL", new[] { "b", "x" } },
        { "INT", new[] { "n", "i" } },
        { "DINT", new[] { "n", "di", "l" } },
        { "LINT", new[] { "n", "l" } },
        { "REAL", new[] { "r", "f" } },
        { "LREAL", new[] { "lr", "lf" } },
        { "STRING", new[] { "s", "str" } },
        { "TIME", new[] { "t", "tim" } },
        { "WORD", new[] { "w" } },
        { "DWORD", new[] { "dw" } },
        { "BYTE", new[] { "by" } },
        { "ARRAY", new[] { "a", "arr" } },
        { "POINTER", new[] { "p", "ptr" } }
    };

    public override List<QAIssue> CheckVariableChange(VariableChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed)
            return issues;

        var varName = change.VariableName;
        var varType = change.NewDataType?.ToUpperInvariant() ?? "";

        // 전역 변수, 상수, FB 인스턴스는 제외
        if (varName.StartsWith("g", StringComparison.OrdinalIgnoreCase) ||
            varName.StartsWith("G_") ||
            varName.ToUpperInvariant() == varName || // 상수
            varName.StartsWith("fb", StringComparison.OrdinalIgnoreCase))
            return issues;

        // 타입별 접두사 확인
        foreach (var kvp in TypePrefixes)
        {
            if (varType.StartsWith(kvp.Key) || varType.Contains($"ARRAY OF {kvp.Key}"))
            {
                bool hasCorrectPrefix = kvp.Value.Any(prefix =>
                    varName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

                if (!hasCorrectPrefix && varName.Length > 2 && !char.IsUpper(varName[0]))
                {
                    issues.Add(CreateIssue(
                        "변수 명명 규칙 위반",
                        $"변수 '{varName}'이(가) {kvp.Key} 타입에 대한 권장 접두사({string.Join(", ", kvp.Value)})를 사용하지 않습니다.",
                        change.FilePath,
                        change.Line,
                        "일관된 명명 규칙은 코드 가독성을 높입니다.",
                        $"'{kvp.Value[0]}{char.ToUpper(varName[0])}{varName.Substring(1)}'와 같은 형식을 권장합니다."));
                }
                break;
            }
        }

        return issues;
    }
}

#endregion

#region SA0074 - 네이밍 규칙 위반 (FB)

/// <summary>
/// SA0074: Function Block 네이밍 규칙 위반 감지
/// FB_ 접두사 미사용
/// </summary>
public class SA0074_FBNamingViolation : SARuleBase
{
    public override string RuleId => "SA0074";
    public override string RuleName => "FB 명명 규칙 위반";
    public override string Description => "Function Block 명명 규칙을 위반하는 경우를 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.NamingConventions;
    public override bool SupportsPrecompile => true;

    private static readonly Regex FBDeclarationPattern = new(
        @"FUNCTION_BLOCK\s+(\w+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var match = FBDeclarationPattern.Match(code);

        if (match.Success)
        {
            var fbName = match.Groups[1].Value;
            if (!fbName.StartsWith("FB_", StringComparison.OrdinalIgnoreCase))
            {
                var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
                issues.Add(CreateIssue(
                    "FB 명명 규칙 위반",
                    $"Function Block '{fbName}'이(가) 'FB_' 접두사를 사용하지 않습니다.",
                    change.FilePath,
                    change.StartLine + lineNumber,
                    "FB_ 접두사는 Function Block임을 명확히 합니다.",
                    $"'FB_{fbName}'로 이름을 변경하는 것을 권장합니다."));
            }
        }

        return issues;
    }
}

#endregion

#region SA0075 - 네이밍 규칙 위반 (인터페이스)

/// <summary>
/// SA0075: 인터페이스 네이밍 규칙 위반 감지
/// I_ 접두사 미사용
/// </summary>
public class SA0075_InterfaceNamingViolation : SARuleBase
{
    public override string RuleId => "SA0075";
    public override string RuleName => "인터페이스 명명 규칙 위반";
    public override string Description => "INTERFACE 명명 규칙을 위반하는 경우를 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.NamingConventions;
    public override bool SupportsPrecompile => true;

    private static readonly Regex InterfacePattern = new(
        @"INTERFACE\s+(\w+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckDataTypeChange(DataTypeChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewDefinition))
            return issues;

        var code = change.NewDefinition;
        var match = InterfacePattern.Match(code);

        if (match.Success)
        {
            var interfaceName = match.Groups[1].Value;
            if (!interfaceName.StartsWith("I_", StringComparison.OrdinalIgnoreCase) &&
                !interfaceName.StartsWith("I", StringComparison.Ordinal) ||
                (interfaceName.Length > 1 && char.IsLower(interfaceName[1])))
            {
                var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
                issues.Add(CreateIssue(
                    "인터페이스 명명 규칙 위반",
                    $"INTERFACE '{interfaceName}'이(가) 'I_' 또는 'I' 접두사를 사용하지 않습니다.",
                    change.FilePath,
                    change.Line + lineNumber,
                    "인터페이스 접두사는 타입을 명확히 구분합니다.",
                    $"'I_{interfaceName}' 또는 'I{interfaceName}'로 이름 변경을 권장합니다."));
            }
        }

        return issues;
    }
}

#endregion

#region SA0076 - 네이밍 규칙 위반 (열거형)

/// <summary>
/// SA0076: 열거형 네이밍 규칙 위반 감지
/// E_ 접두사 미사용
/// </summary>
public class SA0076_EnumNamingViolation : SARuleBase
{
    public override string RuleId => "SA0076";
    public override string RuleName => "열거형 명명 규칙 위반";
    public override string Description => "열거형(ENUM) 명명 규칙을 위반하는 경우를 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.NamingConventions;
    public override bool SupportsPrecompile => true;

    private static readonly Regex EnumPattern = new(
        @"TYPE\s+(\w+)\s*:\s*\(",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckDataTypeChange(DataTypeChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewDefinition))
            return issues;

        var code = change.NewDefinition;
        var match = EnumPattern.Match(code);

        if (match.Success)
        {
            var enumName = match.Groups[1].Value;
            if (!enumName.StartsWith("E_", StringComparison.OrdinalIgnoreCase))
            {
                var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
                issues.Add(CreateIssue(
                    "열거형 명명 규칙 위반",
                    $"열거형 '{enumName}'이(가) 'E_' 접두사를 사용하지 않습니다.",
                    change.FilePath,
                    change.Line + lineNumber,
                    "E_ 접두사는 열거형임을 명확히 합니다.",
                    $"'E_{enumName}'로 이름 변경을 권장합니다."));
            }
        }

        return issues;
    }
}

#endregion

#region SA0077 - 네이밍 규칙 위반 (구조체)

/// <summary>
/// SA0077: 구조체 네이밍 규칙 위반 감지
/// ST_ 접두사 미사용
/// </summary>
public class SA0077_StructNamingViolation : SARuleBase
{
    public override string RuleId => "SA0077";
    public override string RuleName => "구조체 명명 규칙 위반";
    public override string Description => "구조체(STRUCT) 명명 규칙을 위반하는 경우를 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.NamingConventions;
    public override bool SupportsPrecompile => true;

    private static readonly Regex StructPattern = new(
        @"TYPE\s+(\w+)\s*:\s*STRUCT",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckDataTypeChange(DataTypeChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewDefinition))
            return issues;

        var code = change.NewDefinition;
        var match = StructPattern.Match(code);

        if (match.Success)
        {
            var structName = match.Groups[1].Value;
            if (!structName.StartsWith("ST_", StringComparison.OrdinalIgnoreCase) &&
                !structName.StartsWith("T_", StringComparison.OrdinalIgnoreCase))
            {
                var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
                issues.Add(CreateIssue(
                    "구조체 명명 규칙 위반",
                    $"구조체 '{structName}'이(가) 'ST_' 또는 'T_' 접두사를 사용하지 않습니다.",
                    change.FilePath,
                    change.Line + lineNumber,
                    "ST_ 또는 T_ 접두사는 구조체임을 명확히 합니다.",
                    $"'ST_{structName}' 또는 'T_{structName}'로 이름 변경을 권장합니다."));
            }
        }

        return issues;
    }
}

#endregion

#region SA0078 - 네이밍 규칙 위반 (상수)

/// <summary>
/// SA0078: 상수 네이밍 규칙 위반 감지
/// 대문자와 밑줄 미사용
/// </summary>
public class SA0078_ConstantNamingViolation : SARuleBase
{
    public override string RuleId => "SA0078";
    public override string RuleName => "상수 명명 규칙 위반";
    public override string Description => "상수 명명 규칙(대문자, 밑줄)을 위반하는 경우를 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.NamingConventions;
    public override bool SupportsPrecompile => true;

    private static readonly Regex ConstantDeclPattern = new(
        @"VAR\s+CONSTANT\s*(.*?)END_VAR",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex VarNamePattern = new(
        @"(\w+)\s*:\s*\w+",
        RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var constMatch = ConstantDeclPattern.Match(code);

        if (constMatch.Success)
        {
            var constBlock = constMatch.Groups[1].Value;
            var varMatches = VarNamePattern.Matches(constBlock);

            foreach (Match varMatch in varMatches)
            {
                var constName = varMatch.Groups[1].Value;
                // 상수는 대문자와 밑줄만 사용해야 함
                if (constName != constName.ToUpperInvariant() || !Regex.IsMatch(constName, @"^[A-Z][A-Z0-9_]*$"))
                {
                    var lineNumber = code.Take(constMatch.Index).Count(c => c == '\n') + 1;
                    issues.Add(CreateIssue(
                        "상수 명명 규칙 위반",
                        $"상수 '{constName}'이(가) 대문자와 밑줄 규칙을 따르지 않습니다.",
                        change.FilePath,
                        change.StartLine + lineNumber,
                        "상수는 대문자와 밑줄만 사용하는 것이 관례입니다.",
                        $"'{constName.ToUpperInvariant()}'로 이름 변경을 권장합니다."));
                }
            }
        }

        return issues;
    }
}

#endregion

#region SA0079 - 네이밍 규칙 위반 (전역 변수)

/// <summary>
/// SA0079: 전역 변수 네이밍 규칙 위반 감지
/// g 접두사 미사용
/// </summary>
public class SA0079_GlobalVarNamingViolation : SARuleBase
{
    public override string RuleId => "SA0079";
    public override string RuleName => "전역 변수 명명 규칙 위반";
    public override string Description => "전역 변수가 'g' 접두사를 사용하지 않는 경우를 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.NamingConventions;
    public override bool SupportsPrecompile => true;

    private static readonly Regex GVLBlockPattern = new(
        @"VAR_GLOBAL\s*(.*?)END_VAR",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex VarNamePattern = new(
        @"^\s*(\w+)\s*:",
        RegexOptions.Multiline | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var gvlMatch = GVLBlockPattern.Match(code);

        if (gvlMatch.Success)
        {
            var gvlBlock = gvlMatch.Groups[1].Value;
            var varMatches = VarNamePattern.Matches(gvlBlock);

            foreach (Match varMatch in varMatches)
            {
                var varName = varMatch.Groups[1].Value;
                if (!varName.StartsWith("g", StringComparison.OrdinalIgnoreCase) &&
                    !varName.StartsWith("G_", StringComparison.OrdinalIgnoreCase))
                {
                    var lineNumber = code.Take(gvlMatch.Index).Count(c => c == '\n') + 1;
                    issues.Add(CreateIssue(
                        "전역 변수 명명 규칙 위반",
                        $"전역 변수 '{varName}'이(가) 'g' 또는 'G_' 접두사를 사용하지 않습니다.",
                        change.FilePath,
                        change.StartLine + lineNumber,
                        "전역 변수 접두사는 스코프를 명확히 합니다.",
                        $"'g{char.ToUpper(varName[0])}{varName.Substring(1)}' 또는 'G_{varName}'로 이름 변경을 권장합니다."));
                }
            }
        }

        return issues;
    }
}

#endregion

#region SA0080 - 암시적 타입 변환

/// <summary>
/// SA0080: 암시적 타입 변환 감지
/// 명시적 변환 함수 미사용
/// </summary>
public class SA0080_ImplicitConversion : SARuleBase
{
    public override string RuleId => "SA0080";
    public override string RuleName => "암시적 타입 변환";
    public override string Description => "암시적 타입 변환을 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Conversions;
    public override bool SupportsPrecompile => true;

    // 변수 선언과 대입을 추적하기 위한 패턴
    private static readonly Regex VarDeclPattern = new(
        @"(\w+)\s*:\s*(REAL|LREAL)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex IntAssignPattern = new(
        @"(\w+)\s*:=\s*([\w\.]+)\s*;",
        RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var lines = code.Split('\n');

        // REAL 타입 변수 목록 수집
        var realVars = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var match in VarDeclPattern.Matches(code).Cast<Match>())
        {
            realVars.Add(match.Groups[1].Value);
        }

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            // 1. REAL 리터럴을 INT에 대입
            if (Regex.IsMatch(line, @":\s*(?:U?S?D?L?INT)\s*:=\s*\d+\.\d+"))
            {
                issues.Add(CreateIssue(
                    "암시적 REAL->INT 변환",
                    "실수 리터럴이 정수 타입에 직접 대입되고 있습니다.",
                    change.FilePath,
                    change.StartLine + i,
                    "암시적 변환은 데이터 손실을 야기합니다.",
                    "REAL_TO_INT 또는 TRUNC 함수를 사용하세요."));
            }

            // 2. REAL 변수를 INT 변수에 대입 (변환 함수 없이)
            var assignMatch = IntAssignPattern.Match(line);
            if (assignMatch.Success)
            {
                var targetVar = assignMatch.Groups[1].Value;
                var sourceVar = assignMatch.Groups[2].Value;

                // 소스가 REAL 변수이고, 변환 함수가 없으면
                if (realVars.Contains(sourceVar) &&
                    !line.Contains("TO_INT", StringComparison.OrdinalIgnoreCase) &&
                    !line.Contains("TRUNC", StringComparison.OrdinalIgnoreCase) &&
                    !line.Contains("ROUND", StringComparison.OrdinalIgnoreCase))
                {
                    // INT 타입 변수인지 확인 (간단한 휴리스틱)
                    if (targetVar.StartsWith("n", StringComparison.OrdinalIgnoreCase) ||
                        targetVar.StartsWith("i", StringComparison.OrdinalIgnoreCase))
                    {
                        issues.Add(CreateIssue(
                            "암시적 REAL->INT 변환",
                            $"REAL 변수 '{sourceVar}'가 정수 변수 '{targetVar}'에 직접 대입되고 있습니다.",
                            change.FilePath,
                            change.StartLine + i,
                            "암시적 변환은 데이터 손실을 야기합니다.",
                            "REAL_TO_INT, TRUNC, 또는 ROUND 함수를 사용하세요."));
                    }
                }
            }
        }

        return issues;
    }
}

#endregion

#region SA0081 - 위험한 타입 변환

/// <summary>
/// SA0081: 위험한 타입 변환 감지
/// 데이터 손실 가능한 변환
/// </summary>
public class SA0081_DangerousConversion : SARuleBase
{
    public override string RuleId => "SA0081";
    public override string RuleName => "위험한 타입 변환";
    public override string Description => "데이터 손실이 발생할 수 있는 타입 변환을 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Conversions;
    public override bool SupportsPrecompile => true;

    // 위험한 변환 패턴
    private static readonly Regex DintToIntPattern = new(
        @"INT_TO_SINT|DINT_TO_INT|LINT_TO_DINT|LREAL_TO_REAL",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var matches = DintToIntPattern.Matches(code);

        foreach (Match match in matches)
        {
            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
            issues.Add(CreateIssue(
                "데이터 손실 가능 변환",
                $"'{match.Value}' 변환은 데이터 손실을 야기할 수 있습니다.",
                change.FilePath,
                change.StartLine + lineNumber,
                @"
큰 타입에서 작은 타입으로 변환 시:
- 값 범위 초과 시 오버플로우
- 정밀도 손실
- 예측 불가능한 결과
",
                "변환 전 값 범위를 확인하거나 적절한 타입을 사용하세요."));
        }

        return issues;
    }
}

#endregion

#region SA0082 - 부호 변환 위험

/// <summary>
/// SA0082: 부호 있음/없음 변환 감지
/// UINT와 INT 간 변환
/// </summary>
public class SA0082_SignedUnsignedConversion : SARuleBase
{
    public override string RuleId => "SA0082";
    public override string RuleName => "부호 변환 위험";
    public override string Description => "부호 있는 타입과 없는 타입 간의 변환을 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Conversions;
    public override bool SupportsPrecompile => true;

    private static readonly Regex SignConversionPattern = new(
        @"(?:U?D?L?INT)_TO_(?:U?D?L?INT)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var matches = SignConversionPattern.Matches(code);

        foreach (Match match in matches)
        {
            var conversion = match.Value.ToUpperInvariant();
            bool fromSigned = !conversion.StartsWith("U");
            bool toUnsigned = conversion.Contains("_TO_U");

            // 부호 있음 -> 부호 없음 변환
            if (fromSigned && toUnsigned)
            {
                var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
                issues.Add(CreateIssue(
                    "부호 변환 위험",
                    $"'{match.Value}'은 음수 값에서 예상치 못한 결과를 초래할 수 있습니다.",
                    change.FilePath,
                    change.StartLine + lineNumber,
                    "음수 값이 부호 없는 타입으로 변환되면 큰 양수가 됩니다.",
                    "변환 전 값이 0 이상인지 확인하세요."));
            }
        }

        return issues;
    }
}

#endregion

#region SA0083 - 문자열 길이 초과

/// <summary>
/// SA0083: 문자열 길이 초과 가능성 감지
/// STRING(n)에 긴 리터럴 대입
/// </summary>
public class SA0083_StringLengthOverflow : SARuleBase
{
    public override string RuleId => "SA0083";
    public override string RuleName => "문자열 길이 초과";
    public override string Description => "문자열 변수의 최대 길이를 초과할 수 있는 대입을 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Operations;
    public override bool SupportsPrecompile => true;

    private static readonly Regex StringDeclPattern = new(
        @"(\w+)\s*:\s*STRING\s*\(\s*(\d+)\s*\)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex StringAssignPattern = new(
        @"(\w+)\s*:=\s*'([^']*)'",
        RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;

        // 문자열 변수와 길이 수집
        var stringVars = new Dictionary<string, int>();
        var declMatches = StringDeclPattern.Matches(code);
        foreach (Match match in declMatches)
        {
            stringVars[match.Groups[1].Value.ToLowerInvariant()] = int.Parse(match.Groups[2].Value);
        }

        // 대입 검사
        var assignMatches = StringAssignPattern.Matches(code);
        foreach (Match match in assignMatches)
        {
            var varName = match.Groups[1].Value.ToLowerInvariant();
            var stringValue = match.Groups[2].Value;

            if (stringVars.TryGetValue(varName, out int maxLen) && stringValue.Length > maxLen)
            {
                var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
                issues.Add(CreateIssue(
                    "문자열 길이 초과",
                    $"'{varName}'에 대입되는 문자열({stringValue.Length}자)이 최대 길이({maxLen})를 초과합니다.",
                    change.FilePath,
                    change.StartLine + lineNumber,
                    "문자열 길이 초과 시 잘림이 발생합니다.",
                    $"문자열 길이를 {maxLen}자 이하로 줄이거나 변수 크기를 늘리세요."));
            }
        }

        return issues;
    }
}

#endregion

#region SA0084 - 타이머/카운터 초기화 누락

/// <summary>
/// SA0084: 타이머/카운터 초기화 누락 감지
/// TON, CTU 등의 리셋 누락
/// </summary>
public class SA0084_TimerCounterNotReset : SARuleBase
{
    public override string RuleId => "SA0084";
    public override string RuleName => "타이머/카운터 리셋 누락";
    public override string Description => "타이머나 카운터가 리셋되지 않는 경우를 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Initialization;
    public override bool SupportsPrecompile => true;

    // 타이머/카운터 인스턴스 패턴
    private static readonly Regex TimerCounterDeclPattern = new(
        @"(\w+)\s*:\s*(TON|TOF|TP|CTU|CTD|CTUD)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex TimerCallPattern = new(
        @"(\w+)\s*\(",
        RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;

        // 타이머/카운터 인스턴스 수집
        var timerCounters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in TimerCounterDeclPattern.Matches(code))
        {
            timerCounters[match.Groups[1].Value] = match.Groups[2].Value.ToUpperInvariant();
        }

        // 각 타이머/카운터 인스턴스에 대해 리셋 확인
        foreach (var tc in timerCounters)
        {
            var instanceName = tc.Key;
            var typeName = tc.Value;

            // 리셋 패턴 확인 (더 일반적인 패턴)
            bool hasReset = typeName switch
            {
                "TON" or "TOF" or "TP" =>
                    Regex.IsMatch(code, $@"{Regex.Escape(instanceName)}\s*\(\s*IN\s*:=\s*FALSE", RegexOptions.IgnoreCase) ||
                    Regex.IsMatch(code, $@"{Regex.Escape(instanceName)}\.IN\s*:=\s*FALSE", RegexOptions.IgnoreCase) ||
                    Regex.IsMatch(code, $@"NOT\s+\w+.*{Regex.Escape(instanceName)}", RegexOptions.IgnoreCase),

                "CTU" or "CTD" or "CTUD" =>
                    Regex.IsMatch(code, $@"{Regex.Escape(instanceName)}\s*\(\s*RESET\s*:=\s*TRUE", RegexOptions.IgnoreCase) ||
                    Regex.IsMatch(code, $@"{Regex.Escape(instanceName)}\.RESET\s*:=\s*TRUE", RegexOptions.IgnoreCase) ||
                    Regex.IsMatch(code, $@"{Regex.Escape(instanceName)}\s*\(\s*RESET\s*:=\s*\w+", RegexOptions.IgnoreCase),

                _ => true
            };

            if (!hasReset)
            {
                // 인스턴스가 실제로 호출되는지 확인
                bool isCalled = Regex.IsMatch(code, $@"{Regex.Escape(instanceName)}\s*\(");

                if (isCalled)
                {
                    var declMatch = TimerCounterDeclPattern.Match(code);
                    var lineNumber = code.Take(declMatch.Index).Count(c => c == '\n') + 1;
                    issues.Add(CreateIssue(
                        $"{typeName} 리셋 누락",
                        $"'{instanceName}'({typeName})에 대한 리셋 로직이 없습니다.",
                        change.FilePath,
                        change.StartLine + lineNumber,
                        $"{typeName}을 리셋하지 않으면 예상치 못한 동작이 발생할 수 있습니다.",
                        "적절한 조건에서 타이머/카운터를 리셋하세요."));
                }
            }
        }

        return issues;
    }
}

#endregion

#region SA0085 - PERSISTENT 변수 초기화

/// <summary>
/// SA0085: PERSISTENT 변수 초기화 주의 감지
/// 영속 변수의 초기화가 의도적인지 확인
/// </summary>
public class SA0085_PersistentInitialization : SARuleBase
{
    public override string RuleId => "SA0085";
    public override string RuleName => "PERSISTENT 초기화";
    public override string Description => "PERSISTENT 변수가 초기화되는 경우를 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.Initialization;
    public override bool SupportsPrecompile => true;

    private static readonly Regex PersistentInitPattern = new(
        @"VAR\s+PERSISTENT\s*(.*?)END_VAR",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex InitializationPattern = new(
        @"(\w+)\s*:\s*\w+\s*:=",
        RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var persistMatch = PersistentInitPattern.Match(code);

        if (persistMatch.Success)
        {
            var persistBlock = persistMatch.Groups[1].Value;
            var initMatches = InitializationPattern.Matches(persistBlock);

            foreach (Match initMatch in initMatches)
            {
                var varName = initMatch.Groups[1].Value;
                var lineNumber = code.Take(persistMatch.Index).Count(c => c == '\n') + 1;
                issues.Add(CreateIssue(
                    "PERSISTENT 변수 초기화",
                    $"PERSISTENT 변수 '{varName}'이(가) 초기화되어 있습니다.",
                    change.FilePath,
                    change.StartLine + lineNumber,
                    @"
PERSISTENT 변수 초기화 시 주의:
- 다운로드마다 초기화될 수 있음
- 의도치 않은 데이터 손실
- 저장된 값 덮어쓰기
",
                    "초기화가 의도적인지 확인하고, 그렇지 않으면 제거하세요."));
            }
        }

        return issues;
    }
}

#endregion

#region SA0086 - RETAIN 변수 주의

/// <summary>
/// SA0086: RETAIN 변수 사용 주의 감지
/// 보존 변수의 적절한 사용
/// </summary>
public class SA0086_RetainVariableWarning : SARuleBase
{
    public override string RuleId => "SA0086";
    public override string RuleName => "RETAIN 변수 주의";
    public override string Description => "RETAIN 변수 사용 시 주의가 필요한 경우를 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.VariablesAndConstants;
    public override bool SupportsPrecompile => true;

    private static readonly Regex RetainBlockPattern = new(
        @"VAR\s+RETAIN\s*(.*?)END_VAR",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex LargeTypePattern = new(
        @"ARRAY\s*\[.*?\]|STRING\s*\(\s*\d{3,}\s*\)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var retainMatch = RetainBlockPattern.Match(code);

        if (retainMatch.Success)
        {
            var retainBlock = retainMatch.Groups[1].Value;

            // 큰 배열이나 문자열 확인
            if (LargeTypePattern.IsMatch(retainBlock))
            {
                var lineNumber = code.Take(retainMatch.Index).Count(c => c == '\n') + 1;
                issues.Add(CreateIssue(
                    "큰 RETAIN 변수",
                    "RETAIN 영역에 큰 배열이나 문자열이 있습니다.",
                    change.FilePath,
                    change.StartLine + lineNumber,
                    @"
큰 RETAIN 변수는:
- NOVRAM 용량 소비
- 시작 시간 증가
- 저장/복원 시간 영향
",
                    "필요한 데이터만 RETAIN으로 지정하세요."));
            }
        }

        return issues;
    }
}

#endregion

#region SA0087 - AT 지정자 주의

/// <summary>
/// SA0087: AT 지정자 사용 감지
/// 절대 주소 사용
/// </summary>
public class SA0087_AtDirectiveWarning : SARuleBase
{
    public override string RuleId => "SA0087";
    public override string RuleName => "AT 지정자 사용";
    public override string Description => "AT 지정자를 사용한 절대 주소 지정을 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.MemoryLayout;
    public override bool SupportsPrecompile => true;

    private static readonly Regex AtDirectivePattern = new(
        @"(\w+)\s+AT\s+%[IQMX]\w*",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckVariableChange(VariableChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed)
            return issues;

        // AT 지정자가 있는 변수 감지
        if (change.NewDataType?.Contains(" AT ") == true ||
            change.NewDataType?.Contains("%I") == true ||
            change.NewDataType?.Contains("%Q") == true ||
            change.NewDataType?.Contains("%M") == true)
        {
            issues.Add(CreateIssue(
                "AT 지정자 사용",
                $"변수 '{change.VariableName}'이(가) 절대 주소를 사용합니다.",
                change.FilePath,
                change.Line,
                @"
AT 지정자(절대 주소)의 문제점:
- 하드웨어 종속성
- 이식성 저하
- I/O 매핑 변경 시 수동 수정 필요
",
                "가능하면 심볼릭 I/O 링크를 사용하세요."));
        }

        return issues;
    }

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var matches = AtDirectivePattern.Matches(code);

        foreach (Match match in matches)
        {
            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
            issues.Add(CreateIssue(
                "AT 지정자 사용",
                $"'{match.Value}'에서 절대 주소를 사용합니다.",
                change.FilePath,
                change.StartLine + lineNumber,
                "절대 주소는 하드웨어 변경 시 문제가 될 수 있습니다.",
                "심볼릭 I/O 링크나 매핑을 사용하는 것을 권장합니다."));
        }

        return issues;
    }
}

#endregion

#region SA0088 - VAR_ACCESS 사용

/// <summary>
/// SA0088: VAR_ACCESS 사용 감지
/// 외부 접근 가능 변수
/// </summary>
public class SA0088_VarAccessUsage : SARuleBase
{
    public override string RuleId => "SA0088";
    public override string RuleName => "VAR_ACCESS 사용";
    public override string Description => "VAR_ACCESS를 사용한 외부 접근 가능 변수를 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.VariablesAndConstants;
    public override bool SupportsPrecompile => true;

    private static readonly Regex VarAccessPattern = new(
        @"VAR_ACCESS\s*(.*?)END_VAR",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var match = VarAccessPattern.Match(code);

        if (match.Success)
        {
            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
            issues.Add(CreateIssue(
                "VAR_ACCESS 사용",
                "VAR_ACCESS를 통해 외부에서 접근 가능한 변수가 정의되어 있습니다.",
                change.FilePath,
                change.StartLine + lineNumber,
                @"
VAR_ACCESS 사용 시 주의:
- 외부 시스템에서 값 변경 가능
- 보안 고려 필요
- 문서화 권장
",
                "VAR_ACCESS 변수는 명확히 문서화하고, 접근 권한을 검토하세요."));
        }

        return issues;
    }
}

#endregion

#region SA0089 - 속성(Attribute) 사용 확인

/// <summary>
/// SA0089: 속성 사용 확인
/// TwinCAT 속성의 올바른 사용
/// </summary>
public class SA0089_AttributeUsage : SARuleBase
{
    public override string RuleId => "SA0089";
    public override string RuleName => "속성 사용 확인";
    public override string Description => "TwinCAT 속성(Attribute)의 올바른 사용을 확인합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.Declarations;
    public override bool SupportsPrecompile => true;

    private static readonly Regex AttributePattern = new(
        @"\{attribute\s+'([^']+)'\}",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // 알려진 속성 목록
    private static readonly HashSet<string> KnownAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        "call_after_init", "call_after_online_change", "call_on_type_change",
        "pack_mode", "qualified_only", "strict", "no_virtual_actions",
        "init_on_onlchange", "enable_dynamic_creation", "monitoring",
        "parameterstringid", "symbol"
    };

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var matches = AttributePattern.Matches(code);

        foreach (Match match in matches)
        {
            var attrName = match.Groups[1].Value.Split(' ')[0];
            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;

            if (!KnownAttributes.Contains(attrName))
            {
                issues.Add(CreateIssue(
                    "알 수 없는 속성",
                    $"속성 '{attrName}'이(가) 표준 TwinCAT 속성 목록에 없습니다.",
                    change.FilePath,
                    change.StartLine + lineNumber,
                    "알 수 없는 속성은 무시되거나 오류를 발생시킬 수 있습니다.",
                    "속성 이름 철자를 확인하고, TwinCAT 문서를 참조하세요."));
            }
        }

        return issues;
    }
}

#endregion

#region SA0090 - 프라그마 사용

/// <summary>
/// SA0090: 프라그마 사용 감지
/// 컴파일러 지시문 사용
/// </summary>
public class SA0090_PragmaUsage : SARuleBase
{
    public override string RuleId => "SA0090";
    public override string RuleName => "프라그마 사용";
    public override string Description => "컴파일러 프라그마 지시문 사용을 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.Declarations;
    public override bool SupportsPrecompile => true;

    private static readonly Regex PragmaPattern = new(
        @"\{(\w+)\s*:=.*?\}",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var matches = PragmaPattern.Matches(code);

        foreach (Match match in matches)
        {
            var pragmaName = match.Groups[1].Value;
            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;

            // 경고 비활성화 프라그마
            if (pragmaName.Equals("warning", StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(CreateIssue(
                    "경고 비활성화 프라그마",
                    "컴파일러 경고를 비활성화하는 프라그마가 사용되었습니다.",
                    change.FilePath,
                    change.StartLine + lineNumber,
                    "경고 비활성화는 잠재적 문제를 숨길 수 있습니다.",
                    "가능하면 경고의 원인을 수정하는 것이 좋습니다."));
            }
        }

        return issues;
    }
}

#endregion

#region SA0091 - 중복 타입 정의

/// <summary>
/// SA0091: 중복 타입 정의 감지
/// 동일한 이름의 타입 정의
/// </summary>
public class SA0091_DuplicateTypeDefinition : SARuleBase
{
    public override string RuleId => "SA0091";
    public override string RuleName => "중복 타입 정의";
    public override string Description => "중복된 이름의 타입 정의를 감지합니다.";
    public override Severity Severity => Severity.Critical;
    public override SARuleCategory Category => SARuleCategory.Declarations;
    public override bool SupportsPrecompile => false;

    private static readonly Regex TypeDefinitionPattern = new(
        @"TYPE\s+(\w+)\s*:",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckDataTypeChange(DataTypeChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewDefinition))
            return issues;

        var code = change.NewDefinition;
        var matches = TypeDefinitionPattern.Matches(code);
        var typeNames = new Dictionary<string, int>();

        foreach (Match match in matches)
        {
            var typeName = match.Groups[1].Value.ToLowerInvariant();
            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;

            if (typeNames.ContainsKey(typeName))
            {
                issues.Add(CreateIssue(
                    "중복 타입 정의",
                    $"타입 '{match.Groups[1].Value}'이(가) 라인 {typeNames[typeName]}에서 이미 정의되었습니다.",
                    change.FilePath,
                    change.Line + lineNumber,
                    "중복 타입 정의는 컴파일 오류를 발생시킵니다.",
                    "중복된 정의 중 하나를 제거하거나 이름을 변경하세요."));
            }
            else
            {
                typeNames[typeName] = change.Line + lineNumber;
            }
        }

        return issues;
    }
}

#endregion

#region SA0092 - 순환 타입 의존성

/// <summary>
/// SA0092: 순환 타입 의존성 감지
/// 타입 간 상호 참조
/// </summary>
public class SA0092_CircularTypeDependency : SARuleBase
{
    public override string RuleId => "SA0092";
    public override string RuleName => "순환 타입 의존성";
    public override string Description => "타입 간의 순환 의존성을 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Declarations;
    public override bool SupportsPrecompile => false;

    private static readonly Regex StructTypePattern = new(
        @"TYPE\s+(\w+)\s*:\s*STRUCT\s*(.*?)END_STRUCT",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    public override List<QAIssue> CheckDataTypeChange(DataTypeChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewDefinition))
            return issues;

        var code = change.NewDefinition;
        var match = StructTypePattern.Match(code);

        if (match.Success)
        {
            var structName = match.Groups[1].Value;
            var structBody = match.Groups[2].Value;

            // 자기 자신 참조 확인
            if (Regex.IsMatch(structBody, $@"\b{structName}\b", RegexOptions.IgnoreCase))
            {
                var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
                issues.Add(CreateIssue(
                    "자기 참조 구조체",
                    $"구조체 '{structName}'이(가) 자기 자신을 직접 참조합니다.",
                    change.FilePath,
                    change.Line + lineNumber,
                    "직접 자기 참조는 무한 크기 문제를 야기합니다.",
                    "POINTER TO 타입을 사용하여 간접 참조로 변경하세요."));
            }
        }

        return issues;
    }
}

#endregion

#region SA0093-SA0100 추가 규칙

/// <summary>
/// SA0093: 비표준 데이터 타입 사용 감지
/// </summary>
public class SA0093_NonStandardDataType : SARuleBase
{
    public override string RuleId => "SA0093";
    public override string RuleName => "비표준 데이터 타입";
    public override string Description => "IEC 61131-3 표준이 아닌 데이터 타입 사용을 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.StrictIEC;
    public override bool SupportsPrecompile => true;

    private static readonly HashSet<string> StandardTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "BOOL", "BYTE", "WORD", "DWORD", "LWORD",
        "SINT", "INT", "DINT", "LINT",
        "USINT", "UINT", "UDINT", "ULINT",
        "REAL", "LREAL", "STRING", "WSTRING",
        "TIME", "DATE", "TIME_OF_DAY", "TOD", "DATE_AND_TIME", "DT"
    };

    public override List<QAIssue> CheckVariableChange(VariableChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewDataType))
            return issues;

        var baseType = change.NewDataType.Split('[')[0].Split('(')[0].Trim();

        // 포인터, 참조, 배열 등 복합 타입 처리
        baseType = Regex.Replace(baseType, @"^(POINTER TO|REFERENCE TO|ARRAY OF)\s*", "", RegexOptions.IgnoreCase).Trim();

        if (!StandardTypes.Contains(baseType) && !baseType.StartsWith("FB_") &&
            !baseType.StartsWith("ST_") && !baseType.StartsWith("E_") &&
            !baseType.StartsWith("I_") && !baseType.StartsWith("T_"))
        {
            // 사용자 정의 타입이 아닌 비표준 타입
            if (Regex.IsMatch(baseType, @"^[A-Z]+$"))
            {
                issues.Add(CreateIssue(
                    "비표준 데이터 타입",
                    $"'{baseType}'은(는) IEC 61131-3 표준 타입이 아닙니다.",
                    change.FilePath,
                    change.Line,
                    "비표준 타입은 다른 PLC 시스템과 호환되지 않을 수 있습니다.",
                    "가능하면 표준 타입을 사용하세요."));
            }
        }

        return issues;
    }
}

/// <summary>
/// SA0094: EXIT 문 사용 감지
/// </summary>
public class SA0094_ExitStatement : SARuleBase
{
    public override string RuleId => "SA0094";
    public override string RuleName => "EXIT 문 사용";
    public override string Description => "루프에서 EXIT 문 사용을 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.Operations;
    public override bool SupportsPrecompile => true;

    private static readonly Regex ExitPattern = new(
        @"\bEXIT\s*;",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var matches = ExitPattern.Matches(code);

        foreach (Match match in matches)
        {
            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
            issues.Add(CreateIssue(
                "EXIT 문 사용",
                "루프에서 EXIT 문이 사용되었습니다.",
                change.FilePath,
                change.StartLine + lineNumber,
                "EXIT 문은 제어 흐름을 복잡하게 만들 수 있습니다.",
                "가능하면 루프 조건을 조정하여 EXIT 없이 종료하도록 설계하세요."));
        }

        return issues;
    }
}

/// <summary>
/// SA0095: CONTINUE 문 사용 감지
/// </summary>
public class SA0095_ContinueStatement : SARuleBase
{
    public override string RuleId => "SA0095";
    public override string RuleName => "CONTINUE 문 사용";
    public override string Description => "루프에서 CONTINUE 문 사용을 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.Operations;
    public override bool SupportsPrecompile => true;

    private static readonly Regex ContinuePattern = new(
        @"\bCONTINUE\s*;",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var matches = ContinuePattern.Matches(code);

        foreach (Match match in matches)
        {
            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
            issues.Add(CreateIssue(
                "CONTINUE 문 사용",
                "루프에서 CONTINUE 문이 사용되었습니다.",
                change.FilePath,
                change.StartLine + lineNumber,
                "CONTINUE 문은 제어 흐름을 이해하기 어렵게 만들 수 있습니다.",
                "가능하면 조건문을 재구성하여 CONTINUE 없이 구현하세요."));
        }

        return issues;
    }
}

/// <summary>
/// SA0096: JMP 문 사용 감지
/// </summary>
public class SA0096_JmpStatement : SARuleBase
{
    public override string RuleId => "SA0096";
    public override string RuleName => "JMP 문 사용";
    public override string Description => "JMP(GOTO) 문 사용을 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Operations;
    public override bool SupportsPrecompile => true;

    private static readonly Regex JmpPattern = new(
        @"\bJMP\s+\w+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var matches = JmpPattern.Matches(code);

        foreach (Match match in matches)
        {
            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
            issues.Add(CreateIssue(
                "JMP 문 사용",
                "JMP(GOTO) 문이 사용되었습니다.",
                change.FilePath,
                change.StartLine + lineNumber,
                @"
JMP 문은 스파게티 코드를 유발합니다:
- 제어 흐름 추적 어려움
- 유지보수 복잡성 증가
- 버그 발생 가능성 증가
",
                "구조적 프로그래밍 구문(IF, FOR, WHILE 등)으로 대체하세요."));
        }

        return issues;
    }
}

/// <summary>
/// SA0097: 빈 루프 감지
/// </summary>
public class SA0097_EmptyLoop : SARuleBase
{
    public override string RuleId => "SA0097";
    public override string RuleName => "빈 루프";
    public override string Description => "본문이 없는 빈 루프를 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.UnreachableUnusedCode;
    public override bool SupportsPrecompile => true;

    private static readonly Regex EmptyForPattern = new(
        @"FOR\s+.*?\s+DO\s*;\s*END_FOR",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex EmptyWhilePattern = new(
        @"WHILE\s+.*?\s+DO\s*;\s*END_WHILE",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;

        var forMatches = EmptyForPattern.Matches(code);
        foreach (Match match in forMatches)
        {
            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
            issues.Add(CreateIssue(
                "빈 FOR 루프",
                "FOR 루프의 본문이 비어 있습니다.",
                change.FilePath,
                change.StartLine + lineNumber,
                "빈 루프는 무한 대기나 의도치 않은 동작을 유발할 수 있습니다.",
                "루프 본문을 구현하거나 루프를 제거하세요."));
        }

        var whileMatches = EmptyWhilePattern.Matches(code);
        foreach (Match match in whileMatches)
        {
            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
            issues.Add(CreateIssue(
                "빈 WHILE 루프",
                "WHILE 루프의 본문이 비어 있습니다.",
                change.FilePath,
                change.StartLine + lineNumber,
                "빈 WHILE 루프는 무한 루프의 위험이 있습니다.",
                "루프 본문을 구현하거나 루프를 제거하세요."));
        }

        return issues;
    }
}

/// <summary>
/// SA0098: 무한 루프 가능성 감지
/// </summary>
public class SA0098_PotentialInfiniteLoop : SARuleBase
{
    public override string RuleId => "SA0098";
    public override string RuleName => "무한 루프 가능성";
    public override string Description => "무한 루프가 발생할 수 있는 코드를 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Operations;
    public override bool SupportsPrecompile => true;

    private static readonly Regex WhilePattern = new(
        @"WHILE\s+(TRUE|1)\s+DO(.*?)END_WHILE",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex RepeatUntilPattern = new(
        @"REPEAT(.*?)UNTIL\s+(FALSE|0)",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex ForPattern = new(
        @"FOR\s+\w+\s*:=\s*(\d+)\s+TO\s+(\d+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;

        // 1. WHILE TRUE 패턴
        var whileMatches = WhilePattern.Matches(code);
        foreach (Match match in whileMatches)
        {
            var loopBody = match.Groups[2].Value;

            // EXIT 문이 있는지 확인
            bool hasExit = loopBody.Contains("EXIT", StringComparison.OrdinalIgnoreCase) ||
                          loopBody.Contains("RETURN", StringComparison.OrdinalIgnoreCase);

            if (!hasExit)
            {
                var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
                issues.Add(CreateIssue(
                    "무한 WHILE 루프",
                    "WHILE TRUE 루프에 EXIT 조건이 없습니다.",
                    change.FilePath,
                    change.StartLine + lineNumber,
                    "무한 루프는 PLC 태스크를 블로킹하여 시스템 정지를 유발할 수 있습니다.",
                    "EXIT 조건을 추가하거나 루프 조건을 변수로 변경하세요."));
            }
        }

        // 2. REPEAT UNTIL FALSE 패턴
        var repeatMatches = RepeatUntilPattern.Matches(code);
        foreach (Match match in repeatMatches)
        {
            var loopBody = match.Groups[1].Value;

            // EXIT 문이 있는지 확인
            bool hasExit = loopBody.Contains("EXIT", StringComparison.OrdinalIgnoreCase) ||
                          loopBody.Contains("RETURN", StringComparison.OrdinalIgnoreCase);

            if (!hasExit)
            {
                var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
                issues.Add(CreateIssue(
                    "무한 REPEAT 루프",
                    "REPEAT UNTIL FALSE 루프에 EXIT 조건이 없습니다.",
                    change.FilePath,
                    change.StartLine + lineNumber,
                    "무한 루프는 PLC 태스크를 블로킹할 수 있습니다.",
                    "종료 조건을 추가하거나 UNTIL 조건을 변수로 변경하세요."));
            }
        }

        // 3. FOR 루프에서 시작 > 종료 (역방향 루프 없이)
        var forMatches = ForPattern.Matches(code);
        foreach (Match match in forMatches)
        {
            if (int.TryParse(match.Groups[1].Value, out int start) &&
                int.TryParse(match.Groups[2].Value, out int end))
            {
                // BY -1 없이 start > end
                if (start > end && !code.Substring(match.Index, Math.Min(100, code.Length - match.Index)).Contains("BY", StringComparison.OrdinalIgnoreCase))
                {
                    var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
                    issues.Add(CreateIssue(
                        "무한 FOR 루프",
                        $"FOR 루프가 {start}에서 {end}로 진행하지만 BY 절이 없습니다.",
                        change.FilePath,
                        change.StartLine + lineNumber,
                        "시작 값이 종료 값보다 크면 BY -1이 필요합니다.",
                        "FOR 루프에 'BY -1'을 추가하거나 범위를 수정하세요."));
                }
            }
        }

        return issues;
    }
}

/// <summary>
/// SA0099: 부적절한 FOR 루프 변수 수정
/// </summary>
public class SA0099_ForLoopVariableModification : SARuleBase
{
    public override string RuleId => "SA0099";
    public override string RuleName => "FOR 루프 변수 수정";
    public override string Description => "FOR 루프 내에서 루프 변수를 수정하는 것을 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Operations;
    public override bool SupportsPrecompile => true;

    private static readonly Regex ForLoopPattern = new(
        @"FOR\s+(\w+)\s*:=.*?DO\s*(.*?)END_FOR",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var matches = ForLoopPattern.Matches(code);

        foreach (Match match in matches)
        {
            var loopVar = match.Groups[1].Value;
            var loopBody = match.Groups[2].Value;

            // 루프 본문에서 루프 변수 수정 확인
            if (Regex.IsMatch(loopBody, $@"\b{Regex.Escape(loopVar)}\s*:="))
            {
                var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
                issues.Add(CreateIssue(
                    "FOR 루프 변수 수정",
                    $"FOR 루프 내에서 루프 변수 '{loopVar}'를 수정하고 있습니다.",
                    change.FilePath,
                    change.StartLine + lineNumber,
                    @"
FOR 루프 변수 수정의 문제점:
- 예측 불가능한 반복 횟수
- 무한 루프 가능성
- 코드 가독성 저하
",
                    "루프 변수는 FOR 문에 의해서만 수정되어야 합니다."));
            }
        }

        return issues;
    }
}

/// <summary>
/// SA0100: 부적절한 SIZEOF 사용
/// </summary>
public class SA0100_ImproperSizeOf : SARuleBase
{
    public override string RuleId => "SA0100";
    public override string RuleName => "부적절한 SIZEOF";
    public override string Description => "SIZEOF 함수의 부적절한 사용을 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Operations;
    public override bool SupportsPrecompile => true;

    // SIZEOF를 포인터에 사용
    private static readonly Regex SizeOfPointerPattern = new(
        @"SIZEOF\s*\(\s*p\w+\s*\)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var matches = SizeOfPointerPattern.Matches(code);

        foreach (Match match in matches)
        {
            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
            issues.Add(CreateIssue(
                "포인터에 SIZEOF 사용",
                "SIZEOF를 포인터 변수에 사용하고 있습니다.",
                change.FilePath,
                change.StartLine + lineNumber,
                @"
포인터에 SIZEOF 사용 시:
- 포인터 크기(4 또는 8바이트)가 반환됨
- 가리키는 데이터 크기가 아님
",
                "포인터가 가리키는 타입에 SIZEOF를 사용하세요: SIZEOF(타입)"));
        }

        return issues;
    }
}

#endregion
