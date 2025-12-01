using System.Text.RegularExpressions;
using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Models.QA;

namespace TwinCatQA.Infrastructure.QA.Rules.TE1200;

#region SA0101 - 미사용 라이브러리 참조

/// <summary>
/// SA0101: 미사용 라이브러리 참조 감지
/// </summary>
public class SA0101_UnusedLibraryReference : SARuleBase
{
    public override string RuleId => "SA0101";
    public override string RuleName => "미사용 라이브러리";
    public override string Description => "참조되었지만 사용되지 않는 라이브러리를 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.UnreachableUnusedCode;
    public override bool SupportsPrecompile => false;

    private static readonly Regex UsingPattern = new(
        @"USING\s+(\w+)\s*;",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var matches = UsingPattern.Matches(code);

        foreach (Match match in matches)
        {
            var namespaceName = match.Groups[1].Value;
            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;

            // USING 문 이후 네임스페이스 사용 확인
            var codeAfterUsing = code.Substring(match.Index + match.Length);
            if (!Regex.IsMatch(codeAfterUsing, $@"\b{Regex.Escape(namespaceName)}\b"))
            {
                issues.Add(CreateIssue(
                    "미사용 USING 문",
                    $"USING '{namespaceName}'이(가) 사용되지 않습니다.",
                    change.FilePath,
                    change.StartLine + lineNumber,
                    "미사용 USING 문은 코드 정리를 위해 제거하세요.",
                    "USING 문을 제거하세요."));
            }
        }

        return issues;
    }
}

#endregion

#region SA0102 - 비효율적 배열 초기화

/// <summary>
/// SA0102: 비효율적 배열 초기화 감지
/// </summary>
public class SA0102_InefficientArrayInit : SARuleBase
{
    public override string RuleId => "SA0102";
    public override string RuleName => "비효율적 배열 초기화";
    public override string Description => "루프를 사용한 비효율적인 배열 초기화를 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.Operations;
    public override bool SupportsPrecompile => true;

    private static readonly Regex ForLoopArrayInitPattern = new(
        @"FOR\s+\w+\s*:=.*?DO\s*\w+\s*\[\s*\w+\s*\]\s*:=\s*0\s*;",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;

        if (ForLoopArrayInitPattern.IsMatch(code))
        {
            issues.Add(CreateIssue(
                "루프 배열 초기화",
                "FOR 루프를 사용하여 배열을 0으로 초기화하고 있습니다.",
                change.FilePath,
                change.StartLine,
                "루프 초기화는 MEMSET보다 비효율적입니다.",
                "MEMSET 함수 또는 배열 초기화 구문을 사용하세요."));
        }

        return issues;
    }
}

#endregion

#region SA0103 - 과도한 변수 범위

/// <summary>
/// SA0103: 과도한 변수 범위 감지
/// </summary>
public class SA0103_ExcessiveVariableScope : SARuleBase
{
    public override string RuleId => "SA0103";
    public override string RuleName => "과도한 변수 범위";
    public override string Description => "필요 이상으로 넓은 범위를 가진 변수를 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.VariablesAndConstants;
    public override bool SupportsPrecompile => true;

    public override List<QAIssue> CheckVariableChange(VariableChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed)
            return issues;

        // 전역 변수인데 한 곳에서만 사용되는 경우
        if (change.VariableName.StartsWith("g", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(CreateIssue(
                "전역 변수 범위 검토",
                $"전역 변수 '{change.VariableName}'의 사용 범위를 검토하세요.",
                change.FilePath,
                change.Line,
                "전역 변수는 모든 POU에서 접근 가능하여 부작용이 발생할 수 있습니다.",
                "가능하면 지역 변수로 범위를 제한하세요."));
        }

        return issues;
    }
}

#endregion

#region SA0104 - 타입 안전하지 않은 MEMCPY

/// <summary>
/// SA0104: MEMCPY/MEMMOVE 사용 감지
/// </summary>
public class SA0104_UnsafeMemcpy : SARuleBase
{
    public override string RuleId => "SA0104";
    public override string RuleName => "MEMCPY/MEMMOVE 사용";
    public override string Description => "타입 안전하지 않은 MEMCPY/MEMMOVE 사용을 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Operations;
    public override bool SupportsPrecompile => true;

    private static readonly Regex MemcpyPattern = new(
        @"\b(MEMCPY|MEMMOVE)\s*\(",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var matches = MemcpyPattern.Matches(code);

        foreach (Match match in matches)
        {
            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
            issues.Add(CreateIssue(
                "MEMCPY/MEMMOVE 사용",
                $"'{match.Groups[1].Value}' 함수가 사용되었습니다.",
                change.FilePath,
                change.StartLine + lineNumber,
                @"
MEMCPY/MEMMOVE의 위험성:
- 타입 안전성 없음
- 버퍼 오버플로우 가능
- 정렬 문제 가능
",
                "가능하면 직접 대입이나 타입 안전한 복사 함수를 사용하세요."));
        }

        return issues;
    }
}

#endregion

#region SA0105 - 재귀 함수 호출

/// <summary>
/// SA0105: 재귀 함수 호출 감지
/// </summary>
public class SA0105_RecursiveCall : SARuleBase
{
    public override string RuleId => "SA0105";
    public override string RuleName => "재귀 호출";
    public override string Description => "함수의 재귀 호출을 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Operations;
    public override bool SupportsPrecompile => true;

    private static readonly Regex FunctionPattern = new(
        @"(FUNCTION|METHOD)\s+(\w+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex FunctionEndPattern = new(
        @"END_(FUNCTION|METHOD)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;

        // FUNCTION 또는 METHOD 찾기
        var funcMatches = FunctionPattern.Matches(code);

        foreach (Match funcMatch in funcMatches)
        {
            var funcType = funcMatch.Groups[1].Value;
            var funcName = funcMatch.Groups[2].Value;

            // 함수 본문 추출 (시작부터 END까지)
            int startIndex = funcMatch.Index;
            var endMatch = FunctionEndPattern.Match(code, startIndex);

            if (endMatch.Success)
            {
                var funcBody = code.Substring(startIndex, endMatch.Index - startIndex);

                // 자기 자신 호출 확인 (재귀)
                var recursivePattern = new Regex($@"\b{Regex.Escape(funcName)}\s*\(", RegexOptions.IgnoreCase);
                var callMatches = recursivePattern.Matches(funcBody);

                // 첫 번째 매치는 선언 자체이므로 제외
                if (callMatches.Count > 1)
                {
                    var lineNumber = code.Take(startIndex).Count(c => c == '\n') + 1;
                    issues.Add(CreateIssue(
                        "재귀 호출",
                        $"{funcType} '{funcName}'이(가) 자기 자신을 호출합니다 (재귀).",
                        change.FilePath,
                        change.StartLine + lineNumber,
                        @"
PLC에서 재귀의 위험성:
- 제한된 스택 공간으로 스택 오버플로우 가능
- 결정론적 실행 시간 보장 불가
- 실시간 시스템에서 예측 불가능한 동작
",
                        "반복문(FOR, WHILE)으로 변환하거나 꼬리 재귀 최적화를 검토하세요."));
                }
            }
        }

        return issues;
    }
}

#endregion

#region SA0106 - 동적 메모리 할당

/// <summary>
/// SA0106: 동적 메모리 할당 감지
/// </summary>
public class SA0106_DynamicMemory : SARuleBase
{
    public override string RuleId => "SA0106";
    public override string RuleName => "동적 메모리 할당";
    public override string Description => "__NEW, __DELETE 등 동적 메모리 할당 사용을 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Operations;
    public override bool SupportsPrecompile => true;

    private static readonly Regex DynamicMemoryPattern = new(
        @"\b(__NEW|__DELETE|__ISVALIDREF)\s*\(",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var matches = DynamicMemoryPattern.Matches(code);

        foreach (Match match in matches)
        {
            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
            issues.Add(CreateIssue(
                "동적 메모리 사용",
                $"'{match.Groups[1].Value}' 함수가 사용되었습니다.",
                change.FilePath,
                change.StartLine + lineNumber,
                @"
동적 메모리 할당의 위험성:
- 메모리 누수 가능
- 프래그멘테이션
- 실시간 동작 불예측성
- 메모리 부족 오류
",
                "가능하면 정적 메모리 할당을 사용하세요."));
        }

        return issues;
    }
}

#endregion

#region SA0107 - FB_init에서 출력 변수 초기화

/// <summary>
/// SA0107: FB_init에서 출력 변수 초기화 감지
/// </summary>
public class SA0107_OutputInitInFbInit : SARuleBase
{
    public override string RuleId => "SA0107";
    public override string RuleName => "FB_init 출력 초기화";
    public override string Description => "FB_init에서 VAR_OUTPUT 변수를 초기화하는 것을 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Initialization;
    public override bool SupportsPrecompile => true;

    private static readonly Regex FbInitPattern = new(
        @"METHOD\s+FB_init.*?END_METHOD",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;

        // VAR_OUTPUT 변수 수집
        var outputMatch = Regex.Match(code, @"VAR_OUTPUT\s*(.*?)END_VAR",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        if (outputMatch.Success)
        {
            var outputVars = Regex.Matches(outputMatch.Groups[1].Value, @"(\w+)\s*:")
                .Cast<Match>()
                .Select(m => m.Groups[1].Value)
                .ToList();

            // FB_init에서 출력 변수 초기화 확인
            var fbInitMatch = FbInitPattern.Match(code);
            if (fbInitMatch.Success)
            {
                foreach (var outputVar in outputVars)
                {
                    if (Regex.IsMatch(fbInitMatch.Value, $@"\b{Regex.Escape(outputVar)}\s*:="))
                    {
                        var lineNumber = code.Take(fbInitMatch.Index).Count(c => c == '\n') + 1;
                        issues.Add(CreateIssue(
                            "FB_init에서 출력 변수 초기화",
                            $"FB_init에서 출력 변수 '{outputVar}'를 초기화하고 있습니다.",
                            change.FilePath,
                            change.StartLine + lineNumber,
                            "FB_init에서 출력 변수를 초기화하면 예상치 못한 동작이 발생할 수 있습니다.",
                            "출력 변수는 일반 FB 호출에서 설정하세요."));
                    }
                }
            }
        }

        return issues;
    }
}

#endregion

#region SA0108 - SUPER 호출 누락

/// <summary>
/// SA0108: SUPER 호출 누락 감지
/// </summary>
public class SA0108_MissingSuperCall : SARuleBase
{
    public override string RuleId => "SA0108";
    public override string RuleName => "SUPER 호출 누락";
    public override string Description => "오버라이드된 메서드에서 SUPER 호출이 누락된 경우를 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.ObjectOriented;
    public override bool SupportsPrecompile => true;

    private static readonly Regex OverrideMethodPattern = new(
        @"METHOD\s+(?:PUBLIC\s+)?OVERRIDE\s+(\w+).*?END_METHOD",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var matches = OverrideMethodPattern.Matches(code);

        foreach (Match match in matches)
        {
            var methodName = match.Groups[1].Value;
            var methodBody = match.Value;

            if (!methodBody.Contains("SUPER^", StringComparison.OrdinalIgnoreCase))
            {
                var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
                issues.Add(CreateIssue(
                    "SUPER 호출 누락",
                    $"오버라이드된 메서드 '{methodName}'에서 SUPER^.{methodName}() 호출이 없습니다.",
                    change.FilePath,
                    change.StartLine + lineNumber,
                    "부모 클래스의 동작을 유지하려면 SUPER를 호출해야 합니다.",
                    $"필요한 경우 SUPER^.{methodName}()를 호출하세요."));
            }
        }

        return issues;
    }
}

#endregion

#region SA0109 - THIS 포인터 저장

/// <summary>
/// SA0109: THIS 포인터 저장 감지
/// </summary>
public class SA0109_ThisPointerStorage : SARuleBase
{
    public override string RuleId => "SA0109";
    public override string RuleName => "THIS 포인터 저장";
    public override string Description => "THIS 포인터를 변수에 저장하는 것을 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.ObjectOriented;
    public override bool SupportsPrecompile => true;

    private static readonly Regex ThisStoragePattern = new(
        @"(\w+)\s*:=\s*THIS\s*;",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var matches = ThisStoragePattern.Matches(code);

        foreach (Match match in matches)
        {
            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
            issues.Add(CreateIssue(
                "THIS 포인터 저장",
                $"THIS 포인터가 변수 '{match.Groups[1].Value}'에 저장되고 있습니다.",
                change.FilePath,
                change.StartLine + lineNumber,
                @"
THIS 포인터 저장의 위험성:
- 객체 수명 관리 어려움
- 댕글링 포인터 가능성
- 메모리 관리 복잡성
",
                "THIS 저장을 피하고, 필요한 경우 참조나 인터페이스를 사용하세요."));
        }

        return issues;
    }
}

#endregion

#region SA0110 - 비가상 메서드 오버라이드

/// <summary>
/// SA0110: 비가상 메서드 오버라이드 시도 감지
/// </summary>
public class SA0110_NonVirtualOverride : SARuleBase
{
    public override string RuleId => "SA0110";
    public override string RuleName => "비가상 오버라이드";
    public override string Description => "VIRTUAL이 아닌 메서드를 오버라이드하려는 시도를 감지합니다.";
    public override Severity Severity => Severity.Critical;
    public override SARuleCategory Category => SARuleCategory.ObjectOriented;
    public override bool SupportsPrecompile => false;

    // 일반 메서드 정의 (VIRTUAL 없음)
    private static readonly Regex NonVirtualMethodPattern = new(
        @"METHOD\s+(?!.*VIRTUAL)(\w+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;

        // EXTENDS가 있고 OVERRIDE가 있지만 부모에 VIRTUAL이 없는 경우
        if (code.Contains("EXTENDS", StringComparison.OrdinalIgnoreCase) &&
            code.Contains("OVERRIDE", StringComparison.OrdinalIgnoreCase))
        {
            var lineNumber = code.IndexOf("OVERRIDE", StringComparison.OrdinalIgnoreCase);
            lineNumber = code.Take(lineNumber).Count(c => c == '\n') + 1;

            issues.Add(CreateIssue(
                "오버라이드 검토 필요",
                "OVERRIDE 키워드가 사용되었습니다. 부모 메서드가 VIRTUAL인지 확인하세요.",
                change.FilePath,
                change.StartLine + lineNumber,
                "비가상 메서드는 오버라이드할 수 없습니다.",
                "부모 클래스의 메서드에 VIRTUAL 키워드를 추가하세요."));
        }

        return issues;
    }
}

#endregion

#region SA0111 - 인터페이스 분리 원칙 위반

/// <summary>
/// SA0111: 인터페이스 분리 원칙 위반 감지
/// </summary>
public class SA0111_InterfaceSegregation : SARuleBase
{
    public override string RuleId => "SA0111";
    public override string RuleName => "큰 인터페이스";
    public override string Description => "메서드가 많은 큰 인터페이스를 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.ObjectOriented;
    public override bool SupportsPrecompile => true;

    private const int MaxInterfaceMethods = 5;

    private static readonly Regex InterfacePattern = new(
        @"INTERFACE\s+\w+.*?END_INTERFACE",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    public override List<QAIssue> CheckDataTypeChange(DataTypeChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewDefinition))
            return issues;

        var code = change.NewDefinition;
        var match = InterfacePattern.Match(code);

        if (match.Success)
        {
            var methodCount = Regex.Matches(match.Value, @"\bMETHOD\b", RegexOptions.IgnoreCase).Count;

            if (methodCount > MaxInterfaceMethods)
            {
                var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
                issues.Add(CreateIssue(
                    "큰 인터페이스",
                    $"인터페이스에 {methodCount}개의 메서드가 있습니다. (권장: {MaxInterfaceMethods}개 이하)",
                    change.FilePath,
                    change.Line + lineNumber,
                    "큰 인터페이스는 인터페이스 분리 원칙(ISP)을 위반합니다.",
                    "관련 메서드별로 작은 인터페이스로 분리하세요."));
            }
        }

        return issues;
    }
}

#endregion

#region SA0112 - 단일 책임 원칙 위반

/// <summary>
/// SA0112: 단일 책임 원칙 위반 감지 (FB 크기)
/// </summary>
public class SA0112_SingleResponsibility : SARuleBase
{
    public override string RuleId => "SA0112";
    public override string RuleName => "큰 FB";
    public override string Description => "메서드가 많은 큰 Function Block을 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.ObjectOriented;
    public override bool SupportsPrecompile => true;

    private const int MaxFBMethods = 10;
    private const int MaxFBVariables = 20;

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;

        if (code.Contains("FUNCTION_BLOCK", StringComparison.OrdinalIgnoreCase))
        {
            var methodCount = Regex.Matches(code, @"\bMETHOD\b", RegexOptions.IgnoreCase).Count;
            var varMatches = Regex.Matches(code, @"^\s*\w+\s*:", RegexOptions.Multiline);

            if (methodCount > MaxFBMethods)
            {
                issues.Add(CreateIssue(
                    "많은 메서드의 FB",
                    $"Function Block에 {methodCount}개의 메서드가 있습니다. (권장: {MaxFBMethods}개 이하)",
                    change.FilePath,
                    change.StartLine,
                    "메서드가 많은 FB는 단일 책임 원칙(SRP)을 위반할 수 있습니다.",
                    "관련 기능별로 FB를 분리하세요."));
            }

            if (varMatches.Count > MaxFBVariables)
            {
                issues.Add(CreateIssue(
                    "많은 변수의 FB",
                    $"Function Block에 {varMatches.Count}개 이상의 변수가 있습니다. (권장: {MaxFBVariables}개 이하)",
                    change.FilePath,
                    change.StartLine,
                    "변수가 많은 FB는 복잡성이 높고 유지보수가 어렵습니다.",
                    "관련 변수를 구조체로 그룹화하거나 FB를 분리하세요."));
            }
        }

        return issues;
    }
}

#endregion

#region SA0113 - 결합도 높은 코드

/// <summary>
/// SA0113: 높은 결합도 감지
/// </summary>
public class SA0113_HighCoupling : SARuleBase
{
    public override string RuleId => "SA0113";
    public override string RuleName => "높은 결합도";
    public override string Description => "다른 FB에 대한 높은 결합도를 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.ObjectOriented;
    public override bool SupportsPrecompile => true;

    private const int MaxFBDependencies = 7;

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;

        // FB 인스턴스 수 계산
        var fbInstances = Regex.Matches(code, @":\s*FB_\w+", RegexOptions.IgnoreCase);

        if (fbInstances.Count > MaxFBDependencies)
        {
            issues.Add(CreateIssue(
                "높은 결합도",
                $"FB가 {fbInstances.Count}개의 다른 FB에 의존합니다. (권장: {MaxFBDependencies}개 이하)",
                change.FilePath,
                change.StartLine,
                "높은 결합도는 변경의 영향이 크고 테스트가 어렵습니다.",
                "의존성 주입이나 인터페이스를 통해 결합도를 낮추세요."));
        }

        return issues;
    }
}

#endregion

#region SA0114 - 낮은 응집도

/// <summary>
/// SA0114: 낮은 응집도 감지
/// </summary>
public class SA0114_LowCohesion : SARuleBase
{
    public override string RuleId => "SA0114";
    public override string RuleName => "낮은 응집도";
    public override string Description => "멤버 변수를 사용하지 않는 메서드를 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.ObjectOriented;
    public override bool SupportsPrecompile => true;

    private static readonly Regex MethodPattern = new(
        @"METHOD\s+\w+.*?END_METHOD",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;

        // THIS 키워드 사용 여부로 응집도 판단
        var methodMatches = MethodPattern.Matches(code);
        foreach (Match match in methodMatches)
        {
            var methodBody = match.Value;
            if (!methodBody.Contains("THIS.", StringComparison.OrdinalIgnoreCase) &&
                !methodBody.Contains("THIS^", StringComparison.OrdinalIgnoreCase))
            {
                // METHOD 이름 추출
                var nameMatch = Regex.Match(methodBody, @"METHOD\s+(\w+)", RegexOptions.IgnoreCase);
                var methodName = nameMatch.Success ? nameMatch.Groups[1].Value : "Unknown";

                if (!methodName.Equals("FB_init", StringComparison.OrdinalIgnoreCase) &&
                    !methodName.Equals("FB_exit", StringComparison.OrdinalIgnoreCase))
                {
                    var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
                    issues.Add(CreateIssue(
                        "낮은 응집도",
                        $"메서드 '{methodName}'이(가) 인스턴스 변수를 사용하지 않습니다.",
                        change.FilePath,
                        change.StartLine + lineNumber,
                        "인스턴스 변수를 사용하지 않는 메서드는 FUNCTION으로 분리하는 것이 좋습니다.",
                        "정적 함수(FUNCTION)로 변환하거나 메서드 위치를 검토하세요."));
                }
            }
        }

        return issues;
    }
}

#endregion

#region SA0115-SA0130 추가 규칙

/// <summary>
/// SA0115: 하드코딩된 IP 주소 감지
/// </summary>
public class SA0115_HardcodedIP : SARuleBase
{
    public override string RuleId => "SA0115";
    public override string RuleName => "하드코딩된 IP";
    public override string Description => "코드에 하드코딩된 IP 주소를 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Safety;
    public override bool SupportsPrecompile => true;

    private static readonly Regex IPPattern = new(
        @"'(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})'|""(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})""",
        RegexOptions.Compiled);

    // 루프백, 로컬, 예약 IP는 제외
    private static readonly HashSet<string> AllowedIPs = new(StringComparer.OrdinalIgnoreCase)
    {
        "127.0.0.1", "0.0.0.0", "255.255.255.255"
    };

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var matches = IPPattern.Matches(code);

        foreach (Match match in matches)
        {
            var ipAddress = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;

            // 허용 목록 확인
            if (AllowedIPs.Contains(ipAddress))
                continue;

            // VAR_CONSTANT 블록 내부인지 확인 (상수는 허용)
            int varConstStart = code.LastIndexOf("VAR_CONSTANT", match.Index, StringComparison.OrdinalIgnoreCase);
            int varConstEnd = varConstStart >= 0 ? code.IndexOf("END_VAR", varConstStart, StringComparison.OrdinalIgnoreCase) : -1;

            if (varConstStart >= 0 && varConstEnd > match.Index)
                continue; // 상수 블록 내부이면 허용

            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
            issues.Add(CreateIssue(
                "하드코딩된 IP 주소",
                $"IP 주소 '{ipAddress}'이(가) 하드코딩되어 있습니다.",
                change.FilePath,
                change.StartLine + lineNumber,
                "하드코딩된 IP는 환경 변경 시 문제가 되고 유지보수를 어렵게 합니다.",
                "VAR_CONSTANT나 파라미터로 정의하여 중앙 관리하세요."));
        }

        return issues;
    }
}

/// <summary>
/// SA0116: 하드코딩된 경로 감지
/// </summary>
public class SA0116_HardcodedPath : SARuleBase
{
    public override string RuleId => "SA0116";
    public override string RuleName => "하드코딩된 경로";
    public override string Description => "코드에 하드코딩된 파일 경로를 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Safety;
    public override bool SupportsPrecompile => true;

    // Windows 경로, Unix 경로, UNC 경로
    private static readonly Regex PathPattern = new(
        @"'([A-Za-z]:\\[^']{3,})'|""([A-Za-z]:\\[^""]{3,})""|'(/[^']{4,})'|""(/[^""]{4,})""|'(\\\\[^']{5,})'|""(\\\\[^""]{5,})""",
        RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var matches = PathPattern.Matches(code);

        foreach (Match match in matches)
        {
            // 실제 매치된 경로 추출
            string path = "";
            for (int i = 1; i <= 6; i++)
            {
                if (match.Groups[i].Success)
                {
                    path = match.Groups[i].Value;
                    break;
                }
            }

            if (string.IsNullOrEmpty(path))
                continue;

            // VAR_CONSTANT 블록 내부인지 확인 (상수는 경고만)
            int varConstStart = code.LastIndexOf("VAR_CONSTANT", match.Index, StringComparison.OrdinalIgnoreCase);
            int varConstEnd = varConstStart >= 0 ? code.IndexOf("END_VAR", varConstStart, StringComparison.OrdinalIgnoreCase) : -1;

            bool isConstant = varConstStart >= 0 && varConstEnd > match.Index;

            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
            issues.Add(CreateIssue(
                isConstant ? "상수 파일 경로" : "하드코딩된 파일 경로",
                $"파일 경로 '{path}'가 {(isConstant ? "상수로" : "")} 하드코딩되어 있습니다.",
                change.FilePath,
                change.StartLine + lineNumber,
                "하드코딩된 경로는 시스템 변경 시 문제가 되고, 다른 환경(개발/운영)에서 오류를 유발합니다.",
                isConstant ?
                    "상수 경로도 설정 파일이나 파라미터로 외부화하는 것을 권장합니다." :
                    "VAR_CONSTANT, 파라미터, 또는 설정 파일로 정의하세요."));
        }

        return issues;
    }
}

/// <summary>
/// SA0117: 비트 연산 우선순위 감지
/// </summary>
public class SA0117_BitOperationPrecedence : SARuleBase
{
    public override string RuleId => "SA0117";
    public override string RuleName => "비트 연산 우선순위";
    public override string Description => "비트 연산과 비교 연산의 혼합 사용을 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Operations;
    public override bool SupportsPrecompile => true;

    private static readonly Regex MixedBitComparePattern = new(
        @"\b(AND|OR|XOR)\b.*?[<>=]|[<>=].*?\b(AND|OR|XOR)\b",
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
            if (MixedBitComparePattern.IsMatch(line) && !line.Contains("("))
            {
                issues.Add(CreateIssue(
                    "비트 연산 우선순위",
                    "비트 연산과 비교 연산이 괄호 없이 혼합되어 있습니다.",
                    change.FilePath,
                    change.StartLine + i,
                    "연산자 우선순위로 인해 예상치 못한 결과가 발생할 수 있습니다.",
                    "괄호를 사용하여 우선순위를 명확히 하세요."));
            }
        }

        return issues;
    }
}

/// <summary>
/// SA0118: 정수 오버플로우 가능성 감지
/// </summary>
public class SA0118_IntegerOverflow : SARuleBase
{
    public override string RuleId => "SA0118";
    public override string RuleName => "정수 오버플로우";
    public override string Description => "정수 오버플로우가 발생할 수 있는 연산을 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Operations;
    public override bool SupportsPrecompile => true;

    private static readonly Regex IntVarPattern = new(
        @"(\w+)\s*:\s*(U?S?INT)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex MultiplyPattern = new(
        @"(\w+)\s*:=\s*(\w+)\s*\*\s*(\w+)",
        RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;

        // INT 타입 변수 수집
        var intVars = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in IntVarPattern.Matches(code))
        {
            intVars.Add(match.Groups[1].Value);
        }

        // 곱셈 연산 확인
        var multiplyMatches = MultiplyPattern.Matches(code);
        foreach (Match match in multiplyMatches)
        {
            var targetVar = match.Groups[1].Value;
            var operand1 = match.Groups[2].Value;
            var operand2 = match.Groups[3].Value;

            // INT * INT -> INT 패턴
            if (intVars.Contains(targetVar) &&
                (intVars.Contains(operand1) || intVars.Contains(operand2)))
            {
                var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
                issues.Add(CreateIssue(
                    "정수 오버플로우 위험",
                    $"INT 변수 '{targetVar}'에 곱셈 결과를 저장합니다.",
                    change.FilePath,
                    change.StartLine + lineNumber,
                    @"
INT * INT 연산의 위험성:
- 결과가 -32768 ~ 32767 범위를 초과하면 오버플로우
- 예측 불가능한 값 발생
- 계산 오류로 인한 시스템 오작동
",
                    "중간 결과를 DINT에 저장하거나, 연산 전 DINT로 변환하세요: nResult := TO_DINT(nValue1) * TO_DINT(nValue2)"));
            }
        }

        // 큰 상수 곱셈 (100 이상)
        var largeConstMultiply = new Regex(@"\*\s*([1-9]\d{2,})\b", RegexOptions.Compiled);
        foreach (Match match in largeConstMultiply.Matches(code))
        {
            var constant = match.Groups[1].Value;
            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;

            issues.Add(CreateIssue(
                "큰 상수 곱셈",
                $"큰 상수 {constant}와의 곱셈이 있습니다.",
                change.FilePath,
                change.StartLine + lineNumber,
                "큰 상수와의 곱셈은 쉽게 오버플로우를 유발할 수 있습니다.",
                "결과 타입이 충분히 큰지 확인하세요."));
        }

        return issues;
    }
}

/// <summary>
/// SA0119: 시간 연산 주의 감지
/// </summary>
public class SA0119_TimeOperation : SARuleBase
{
    public override string RuleId => "SA0119";
    public override string RuleName => "시간 연산 주의";
    public override string Description => "TIME 타입 연산 시 주의가 필요한 경우를 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.Operations;
    public override bool SupportsPrecompile => true;

    private static readonly Regex TimeSubtractPattern = new(
        @"TIME\s*-\s*TIME|\w+\s*-\s*\w+.*?TIME",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;

        if (TimeSubtractPattern.IsMatch(code))
        {
            issues.Add(CreateIssue(
                "TIME 뺄셈 연산",
                "TIME 타입의 뺄셈 연산이 있습니다.",
                change.FilePath,
                change.StartLine,
                "TIME 뺄셈에서 음수가 발생할 수 있습니다.",
                "결과가 음수가 되지 않도록 조건을 확인하세요."));
        }

        return issues;
    }
}

/// <summary>
/// SA0120: WSTRING/STRING 혼용 감지
/// </summary>
public class SA0120_StringWstringMix : SARuleBase
{
    public override string RuleId => "SA0120";
    public override string RuleName => "STRING/WSTRING 혼용";
    public override string Description => "STRING과 WSTRING의 부적절한 혼용을 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Conversions;
    public override bool SupportsPrecompile => true;

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;

        // STRING과 WSTRING이 모두 존재
        bool hasString = Regex.IsMatch(code, @"\bSTRING\b(?!\s*TO)", RegexOptions.IgnoreCase);
        bool hasWstring = Regex.IsMatch(code, @"\bWSTRING\b(?!\s*TO)", RegexOptions.IgnoreCase);

        if (hasString && hasWstring)
        {
            issues.Add(CreateIssue(
                "STRING/WSTRING 혼용",
                "STRING과 WSTRING 타입이 혼용되고 있습니다.",
                change.FilePath,
                change.StartLine,
                "STRING과 WSTRING 혼용은 변환 오버헤드와 데이터 손실을 유발합니다.",
                "가능하면 하나의 문자열 타입으로 통일하세요."));
        }

        return issues;
    }
}

/// <summary>
/// SA0121: 열거형 범위 초과 감지
/// </summary>
public class SA0121_EnumRangeOverflow : SARuleBase
{
    public override string RuleId => "SA0121";
    public override string RuleName => "열거형 범위";
    public override string Description => "열거형의 기본 타입 범위를 초과할 수 있는 값을 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Declarations;
    public override bool SupportsPrecompile => true;

    private static readonly Regex EnumWithValuePattern = new(
        @"(\w+)\s*:=\s*(\d+)",
        RegexOptions.Compiled);

    public override List<QAIssue> CheckDataTypeChange(DataTypeChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewDefinition))
            return issues;

        var code = change.NewDefinition;
        var matches = EnumWithValuePattern.Matches(code);

        foreach (Match match in matches)
        {
            if (int.TryParse(match.Groups[2].Value, out int value))
            {
                // INT 범위 확인 (기본 열거형 타입)
                if (value > 32767)
                {
                    var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
                    issues.Add(CreateIssue(
                        "열거형 값 범위",
                        $"열거형 값 {value}이(가) INT 범위를 초과합니다.",
                        change.FilePath,
                        change.Line + lineNumber,
                        "기본 열거형은 INT 타입이므로 32767을 초과할 수 없습니다.",
                        "DINT 기반 열거형을 사용하거나 값을 조정하세요."));
                }
            }
        }

        return issues;
    }
}

/// <summary>
/// SA0122: 중첩 구조체 깊이 감지
/// </summary>
public class SA0122_NestedStructDepth : SARuleBase
{
    public override string RuleId => "SA0122";
    public override string RuleName => "중첩 구조체 깊이";
    public override string Description => "과도하게 중첩된 구조체를 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.Declarations;
    public override bool SupportsPrecompile => true;

    public override List<QAIssue> CheckVariableChange(VariableChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed)
            return issues;

        // 변수 접근 경로 깊이 확인
        if (change.VariableName.Count(c => c == '.') >= 3)
        {
            issues.Add(CreateIssue(
                "깊은 구조체 접근",
                $"'{change.VariableName}'의 접근 깊이가 깊습니다.",
                change.FilePath,
                change.Line,
                "깊은 중첩은 코드 가독성을 저해합니다.",
                "중간 변수를 사용하거나 구조를 단순화하세요."));
        }

        return issues;
    }
}

/// <summary>
/// SA0123: 타입 캐스팅 안전성 감지
/// </summary>
public class SA0123_UnsafeCast : SARuleBase
{
    public override string RuleId => "SA0123";
    public override string RuleName => "안전하지 않은 캐스트";
    public override string Description => "안전하지 않은 타입 캐스팅을 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Conversions;
    public override bool SupportsPrecompile => true;

    private static readonly Regex DirectCastPattern = new(
        @"\b(ANY_TO_|TO_)\w+\s*\(",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;

        if (code.Contains("ANY_TO_", StringComparison.OrdinalIgnoreCase))
        {
            var matches = DirectCastPattern.Matches(code);
            foreach (Match match in matches)
            {
                var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
                issues.Add(CreateIssue(
                    "ANY 타입 캐스트",
                    "ANY_TO_ 캐스트 함수가 사용되었습니다.",
                    change.FilePath,
                    change.StartLine + lineNumber,
                    "ANY 타입 캐스트는 타입 안전성을 보장하지 않습니다.",
                    "가능하면 명시적 타입 변환 함수를 사용하세요."));
            }
        }

        return issues;
    }
}

/// <summary>
/// SA0124: 다중 상속 감지
/// </summary>
public class SA0124_MultipleInheritance : SARuleBase
{
    public override string RuleId => "SA0124";
    public override string RuleName => "다중 인터페이스 구현";
    public override string Description => "많은 인터페이스를 구현하는 FB를 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.ObjectOriented;
    public override bool SupportsPrecompile => true;

    private const int MaxInterfaces = 3;

    private static readonly Regex ImplementsPattern = new(
        @"IMPLEMENTS\s+([\w,\s]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var match = ImplementsPattern.Match(code);

        if (match.Success)
        {
            var interfaces = match.Groups[1].Value.Split(',').Length;
            if (interfaces > MaxInterfaces)
            {
                var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
                issues.Add(CreateIssue(
                    "많은 인터페이스 구현",
                    $"FB가 {interfaces}개의 인터페이스를 구현합니다. (권장: {MaxInterfaces}개 이하)",
                    change.FilePath,
                    change.StartLine + lineNumber,
                    "많은 인터페이스 구현은 FB의 복잡성을 높입니다.",
                    "FB를 분리하거나 인터페이스 설계를 검토하세요."));
            }
        }

        return issues;
    }
}

/// <summary>
/// SA0125: PROPERTY 남용 감지
/// </summary>
public class SA0125_PropertyMisuse : SARuleBase
{
    public override string RuleId => "SA0125";
    public override string RuleName => "PROPERTY 남용";
    public override string Description => "복잡한 로직이 포함된 PROPERTY를 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.ObjectOriented;
    public override bool SupportsPrecompile => true;

    private static readonly Regex PropertyPattern = new(
        @"PROPERTY\s+\w+.*?END_PROPERTY",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var matches = PropertyPattern.Matches(code);

        foreach (Match match in matches)
        {
            var propertyBody = match.Value;
            var lineCount = propertyBody.Split('\n').Length;

            // IF, FOR 등 제어문 포함 여부
            bool hasControlFlow = Regex.IsMatch(propertyBody, @"\b(IF|FOR|WHILE|CASE)\b", RegexOptions.IgnoreCase);

            if (lineCount > 10 || hasControlFlow)
            {
                var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
                issues.Add(CreateIssue(
                    "복잡한 PROPERTY",
                    "PROPERTY에 복잡한 로직이 포함되어 있습니다.",
                    change.FilePath,
                    change.StartLine + lineNumber,
                    "PROPERTY는 단순한 값 접근에만 사용해야 합니다.",
                    "복잡한 로직은 METHOD로 이동하세요."));
            }
        }

        return issues;
    }
}

/// <summary>
/// SA0126: 문자열 버퍼 크기 감지
/// </summary>
public class SA0126_StringBufferSize : SARuleBase
{
    public override string RuleId => "SA0126";
    public override string RuleName => "문자열 버퍼 크기";
    public override string Description => "작은 크기의 STRING 버퍼를 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.Declarations;
    public override bool SupportsPrecompile => true;

    private static readonly Regex SmallStringPattern = new(
        @":\s*STRING\s*\(\s*([1-9]|1[0-9])\s*\)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckVariableChange(VariableChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewDataType))
            return issues;

        var match = SmallStringPattern.Match(change.NewDataType);
        if (match.Success)
        {
            var size = int.Parse(match.Groups[1].Value);
            issues.Add(CreateIssue(
                "작은 STRING 버퍼",
                $"STRING({size})은 작은 버퍼 크기입니다.",
                change.FilePath,
                change.Line,
                "작은 문자열 버퍼는 잘림이 자주 발생할 수 있습니다.",
                "필요한 최대 길이를 고려하여 적절한 크기를 설정하세요."));
        }

        return issues;
    }
}

/// <summary>
/// SA0127: 배열 크기 불일치 감지
/// </summary>
public class SA0127_ArraySizeMismatch : SARuleBase
{
    public override string RuleId => "SA0127";
    public override string RuleName => "배열 크기 주의";
    public override string Description => "배열 크기와 관련된 잠재적 문제를 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Operations;
    public override bool SupportsPrecompile => true;

    private static readonly Regex ForToArrayPattern = new(
        @"FOR\s+\w+\s*:=\s*(\d+)\s+TO\s+(\d+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var forMatches = ForToArrayPattern.Matches(code);

        foreach (Match match in forMatches)
        {
            if (int.TryParse(match.Groups[1].Value, out int start) &&
                int.TryParse(match.Groups[2].Value, out int end))
            {
                // 시작이 1이 아닌 경우
                if (start != 0 && start != 1)
                {
                    var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
                    issues.Add(CreateIssue(
                        "비표준 FOR 시작 인덱스",
                        $"FOR 루프가 {start}부터 시작합니다.",
                        change.FilePath,
                        change.StartLine + lineNumber,
                        "비표준 시작 인덱스는 배열 접근 오류를 유발할 수 있습니다.",
                        "배열 선언과 일치하는지 확인하세요."));
                }
            }
        }

        return issues;
    }
}

/// <summary>
/// SA0128: ACTION 남용 감지
/// </summary>
public class SA0128_ActionMisuse : SARuleBase
{
    public override string RuleId => "SA0128";
    public override string RuleName => "ACTION 사용";
    public override string Description => "ACTION 사용을 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.Operations;
    public override bool SupportsPrecompile => true;

    private static readonly Regex ActionPattern = new(
        @"ACTION\s+\w+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var matches = ActionPattern.Matches(code);

        if (matches.Count > 0)
        {
            var lineNumber = code.Take(matches[0].Index).Count(c => c == '\n') + 1;
            issues.Add(CreateIssue(
                "ACTION 사용",
                "ACTION이 사용되고 있습니다.",
                change.FilePath,
                change.StartLine + lineNumber,
                "ACTION은 레거시 기능입니다. METHOD 사용을 권장합니다.",
                "가능하면 METHOD로 리팩토링하세요."));
        }

        return issues;
    }
}

/// <summary>
/// SA0129: FB_reinit 사용 감지
/// </summary>
public class SA0129_FbReinitUsage : SARuleBase
{
    public override string RuleId => "SA0129";
    public override string RuleName => "FB_reinit 사용";
    public override string Description => "FB_reinit 함수 사용을 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Operations;
    public override bool SupportsPrecompile => true;

    private static readonly Regex ReinitPattern = new(
        @"\bFB_reinit\s*\(",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var matches = ReinitPattern.Matches(code);

        foreach (Match match in matches)
        {
            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
            issues.Add(CreateIssue(
                "FB_reinit 사용",
                "FB_reinit 함수가 호출되었습니다.",
                change.FilePath,
                change.StartLine + lineNumber,
                @"
FB_reinit의 위험성:
- 모든 멤버 변수가 초기화됨
- 예상치 못한 상태 손실
- 연결된 객체 참조 문제
",
                "필요한 경우에만 사용하고, 부작용을 문서화하세요."));
        }

        return issues;
    }
}

/// <summary>
/// SA0130: 직접 I/O 접근 감지
/// </summary>
public class SA0130_DirectIOAccess : SARuleBase
{
    public override string RuleId => "SA0130";
    public override string RuleName => "직접 I/O 접근";
    public override string Description => "코드에서 직접 I/O 주소 접근을 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.MemoryLayout;
    public override bool SupportsPrecompile => true;

    // %IX0.0, %QX0.1, %IB0, %QW1, %MD100 등
    private static readonly Regex DirectIOPattern = new(
        @"%[IQMX][XBWDL]?[\d\.]+",
        RegexOptions.Compiled);

    private static readonly Regex AtDirectivePattern = new(
        @"AT\s+%[IQMX]",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;

        // 1. 코드 내에서 직접 I/O 주소 사용
        var matches = DirectIOPattern.Matches(code);
        var reportedAddresses = new HashSet<string>();

        foreach (Match match in matches)
        {
            var ioAddress = match.Value;

            // AT 선언부는 제외 (이건 SA0087에서 처리)
            int atIndex = code.LastIndexOf("AT", match.Index, StringComparison.OrdinalIgnoreCase);
            if (atIndex >= 0 && match.Index - atIndex < 10)
                continue;

            // 중복 보고 방지
            if (reportedAddresses.Contains(ioAddress))
                continue;

            reportedAddresses.Add(ioAddress);

            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
            issues.Add(CreateIssue(
                "직접 I/O 접근",
                $"직접 I/O 주소 '{ioAddress}'가 코드에서 사용되었습니다.",
                change.FilePath,
                change.StartLine + lineNumber,
                @"
직접 I/O 접근의 문제점:
- 하드웨어 종속성 (I/O 위치 변경 시 코드 수정 필요)
- 매핑 변경 시 수동 수정 필요
- 코드 이식성 저하
- 가독성 저하 (의미를 알 수 없음)
",
                @"
권장 방법:
1. 변수를 선언: bEmergencyStop AT %IX0.0 : BOOL;
2. 코드에서 변수명 사용: IF bEmergencyStop THEN ...
3. 또는 I/O 매핑을 통해 심볼릭 링크 사용
"));
        }

        return issues;
    }
}

#endregion
