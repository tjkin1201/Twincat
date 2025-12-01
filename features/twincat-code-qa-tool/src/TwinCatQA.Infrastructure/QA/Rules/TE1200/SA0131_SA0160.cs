using System.Text.RegularExpressions;
using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Models.QA;

namespace TwinCatQA.Infrastructure.QA.Rules.TE1200;

#region SA0131-SA0140 안전 및 품질 관련 규칙

/// <summary>
/// SA0131: 위험한 형변환 없이 포인터 역참조
/// </summary>
public class SA0131_UnsafePointerDereference : SARuleBase
{
    public override string RuleId => "SA0131";
    public override string RuleName => "안전하지 않은 포인터 역참조";
    public override string Description => "NULL 체크 없는 포인터 역참조를 감지합니다.";
    public override Severity Severity => Severity.Critical;
    public override SARuleCategory Category => SARuleCategory.Safety;
    public override bool SupportsPrecompile => true;

    private static readonly Regex PointerDereferencePattern = new(
        @"(\w+)\s*\^",
        RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var matches = PointerDereferencePattern.Matches(code);

        foreach (Match match in matches)
        {
            var ptrName = match.Groups[1].Value;

            // NULL 체크 패턴 확인
            var nullCheckPattern = new Regex(
                $@"IF\s+{Regex.Escape(ptrName)}\s*<>\s*(0|NULL|NIL).*?{Regex.Escape(ptrName)}\s*\^",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (!nullCheckPattern.IsMatch(code))
            {
                var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
                issues.Add(CreateIssue(
                    "안전하지 않은 포인터 역참조",
                    $"포인터 '{ptrName}'이(가) NULL 체크 없이 역참조되고 있습니다.",
                    change.FilePath,
                    change.StartLine + lineNumber,
                    "NULL 포인터 역참조는 런타임 오류를 발생시킵니다.",
                    $"IF {ptrName} <> 0 THEN {ptrName}^... END_IF 형태로 수정하세요."));
            }
        }

        return issues;
    }
}

/// <summary>
/// SA0132: 배열 인덱스 검증 누락
/// </summary>
public class SA0132_ArrayIndexValidation : SARuleBase
{
    public override string RuleId => "SA0132";
    public override string RuleName => "배열 인덱스 검증 누락";
    public override string Description => "변수 인덱스로 배열 접근 시 범위 검증 누락을 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Safety;
    public override bool SupportsPrecompile => true;

    private static readonly Regex DynamicArrayAccessPattern = new(
        @"(\w+)\s*\[\s*(\w+)\s*\]",
        RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var matches = DynamicArrayAccessPattern.Matches(code);

        foreach (Match match in matches)
        {
            var arrayName = match.Groups[1].Value;
            var indexVar = match.Groups[2].Value;

            // 상수 인덱스는 건너뛰기
            if (int.TryParse(indexVar, out _))
                continue;

            // 범위 검증 패턴 확인
            var rangeCheckPattern = new Regex(
                $@"IF\s+{Regex.Escape(indexVar)}\s*>=?\s*\d+\s+AND\s+{Regex.Escape(indexVar)}\s*<=?\s*\d+",
                RegexOptions.IgnoreCase);

            if (!rangeCheckPattern.IsMatch(code))
            {
                var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
                issues.Add(CreateIssue(
                    "배열 인덱스 검증 누락",
                    $"배열 '{arrayName}'에 대한 인덱스 '{indexVar}'의 범위 검증이 없습니다.",
                    change.FilePath,
                    change.StartLine + lineNumber,
                    "범위 밖 인덱스 접근은 메모리 오류를 발생시킵니다.",
                    $"IF {indexVar} >= 0 AND {indexVar} <= MAX_INDEX THEN ... 형태로 검증하세요."));
            }
        }

        return issues;
    }
}

/// <summary>
/// SA0133: 부동소수점 루프 카운터
/// </summary>
public class SA0133_FloatLoopCounter : SARuleBase
{
    public override string RuleId => "SA0133";
    public override string RuleName => "부동소수점 루프 카운터";
    public override string Description => "FOR 루프에서 부동소수점 타입을 카운터로 사용하는 것을 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Operations;
    public override bool SupportsPrecompile => true;

    private static readonly Regex ForFloatPattern = new(
        @"FOR\s+(\w+)\s*:\s*(L?REAL)\s*:=",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var matches = ForFloatPattern.Matches(code);

        foreach (Match match in matches)
        {
            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
            var varName = match.Groups[1].Value;
            var typeName = match.Groups[2].Value;

            issues.Add(CreateIssue(
                "부동소수점 루프 카운터",
                $"FOR 루프 카운터 '{varName}'이(가) {typeName} 타입으로 선언되었습니다.",
                change.FilePath,
                change.StartLine + lineNumber,
                @"
부동소수점 루프 카운터의 문제점:
- 정밀도 오차로 인한 반복 횟수 불일치
- 예측 불가능한 종료 조건
- 무한 루프 가능성
예: FOR r: REAL := 0.0 TO 1.0 BY 0.1 DO // 실제로 10회가 아닐 수 있음
",
                "정수 타입(INT, DINT)을 루프 카운터로 사용하세요."));
        }

        return issues;
    }
}

/// <summary>
/// SA0134: 단위 테스트 누락 표시
/// </summary>
public class SA0134_MissingUnitTest : SARuleBase
{
    public override string RuleId => "SA0134";
    public override string RuleName => "테스트 누락 표시";
    public override string Description => "테스트가 필요한 것으로 표시된 코드를 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.Comments;
    public override bool SupportsPrecompile => true;

    private static readonly Regex NeedsTestPattern = new(
        @"(NEEDS?\s*TEST|TODO:\s*TEST|TEST\s*REQUIRED)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var matches = NeedsTestPattern.Matches(code);

        foreach (Match match in matches)
        {
            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
            issues.Add(CreateIssue(
                "테스트 필요",
                "테스트가 필요한 것으로 표시된 코드가 있습니다.",
                change.FilePath,
                change.StartLine + lineNumber,
                "테스트되지 않은 코드는 품질을 보장할 수 없습니다.",
                "해당 코드에 대한 단위 테스트를 작성하세요."));
        }

        return issues;
    }
}

/// <summary>
/// SA0135: FIXME 주석 감지
/// </summary>
public class SA0135_FixmeComment : SARuleBase
{
    public override string RuleId => "SA0135";
    public override string RuleName => "FIXME 주석";
    public override string Description => "FIXME 주석을 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Comments;
    public override bool SupportsPrecompile => true;

    private static readonly Regex FixmePattern = new(
        @"//\s*(FIXME|BUG|HACK|XXX)[\s:]+(.*)$|\(\*\s*(FIXME|BUG|HACK|XXX)[\s:]+(.*)$",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var matches = FixmePattern.Matches(code);

        foreach (Match match in matches)
        {
            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;

            // // 주석인지 (* 주석인지 확인
            var tag = !string.IsNullOrEmpty(match.Groups[1].Value)
                ? match.Groups[1].Value.ToUpperInvariant()
                : match.Groups[3].Value.ToUpperInvariant();

            var description = !string.IsNullOrEmpty(match.Groups[2].Value)
                ? match.Groups[2].Value.Trim()
                : match.Groups[4].Value.Trim();

            var severity = tag switch
            {
                "BUG" => Severity.Critical,
                "FIXME" => Severity.Warning,
                "HACK" => Severity.Warning,
                "XXX" => Severity.Info,
                _ => Severity.Info
            };

            issues.Add(CreateIssue(
                $"{tag} 주석",
                string.IsNullOrEmpty(description) ? $"{tag} 표시가 발견되었습니다." : $"{tag}: {description}",
                change.FilePath,
                change.StartLine + lineNumber,
                $"{tag} 주석은 해결되지 않은 문제를 나타냅니다. 프로덕션 배포 전에 해결이 필요합니다.",
                "문제를 해결하고 주석을 제거하세요."));
        }

        return issues;
    }
}

/// <summary>
/// SA0136: 위험한 형변환 감지
/// </summary>
public class SA0136_DangerousCast : SARuleBase
{
    public override string RuleId => "SA0136";
    public override string RuleName => "위험한 형변환";
    public override string Description => "위험할 수 있는 형변환을 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Conversions;
    public override bool SupportsPrecompile => true;

    private static readonly Regex PointerCastPattern = new(
        @":\s*POINTER\s+TO\s+.*?:=\s*ADR\s*\(",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var matches = PointerCastPattern.Matches(code);

        foreach (Match match in matches)
        {
            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
            issues.Add(CreateIssue(
                "포인터 타입 변환",
                "ADR을 사용한 포인터 타입 변환이 감지되었습니다.",
                change.FilePath,
                change.StartLine + lineNumber,
                "잘못된 포인터 타입 변환은 메모리 손상을 유발합니다.",
                "타입 안전성을 확인하고, 가능하면 직접 참조를 사용하세요."));
        }

        return issues;
    }
}

/// <summary>
/// SA0137: 중복 조건 검사 감지
/// </summary>
public class SA0137_RedundantConditionCheck : SARuleBase
{
    public override string RuleId => "SA0137";
    public override string RuleName => "중복 조건 검사";
    public override string Description => "불필요하게 중복된 조건 검사를 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.Operations;
    public override bool SupportsPrecompile => true;

    private static readonly Regex DoubleCheckPattern = new(
        @"IF\s+(\w+)\s+THEN\s+IF\s+\1\s+THEN",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var matches = DoubleCheckPattern.Matches(code);

        foreach (Match match in matches)
        {
            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
            issues.Add(CreateIssue(
                "중복 조건 검사",
                $"조건 '{match.Groups[1].Value}'이(가) 연속으로 두 번 검사되고 있습니다.",
                change.FilePath,
                change.StartLine + lineNumber,
                "중복 조건 검사는 불필요한 오버헤드입니다.",
                "중복된 검사를 제거하세요."));
        }

        return issues;
    }
}

/// <summary>
/// SA0138: 부울 리터럴 반환 감지
/// </summary>
public class SA0138_BooleanLiteralReturn : SARuleBase
{
    public override string RuleId => "SA0138";
    public override string RuleName => "부울 리터럴 반환";
    public override string Description => "IF 조건에서 직접 TRUE/FALSE를 반환하는 패턴을 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.Operations;
    public override bool SupportsPrecompile => true;

    private static readonly Regex BoolReturnPattern = new(
        @"IF\s+(.+?)\s+THEN\s+(\w+)\s*:=\s*TRUE\s*;\s*ELSE\s+\2\s*:=\s*FALSE",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var matches = BoolReturnPattern.Matches(code);

        foreach (Match match in matches)
        {
            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
            var condition = match.Groups[1].Value.Trim();
            var varName = match.Groups[2].Value;

            issues.Add(CreateIssue(
                "부울 리터럴 반환",
                "IF-ELSE로 TRUE/FALSE를 직접 할당하고 있습니다.",
                change.FilePath,
                change.StartLine + lineNumber,
                "이 패턴은 직접 조건을 할당하는 것으로 단순화할 수 있습니다.",
                $"'{varName} := {condition};'로 단순화하세요."));
        }

        return issues;
    }
}

/// <summary>
/// SA0139: 빈 예외 처리 감지
/// </summary>
public class SA0139_EmptyExceptionHandler : SARuleBase
{
    public override string RuleId => "SA0139";
    public override string RuleName => "빈 예외 처리";
    public override string Description => "__TRY/__CATCH 블록에서 빈 예외 처리를 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Operations;
    public override bool SupportsPrecompile => true;

    private static readonly Regex EmptyCatchPattern = new(
        @"__CATCH\s*\(\s*\w+\s*\)\s*;?\s*__ENDTRY",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var matches = EmptyCatchPattern.Matches(code);

        foreach (Match match in matches)
        {
            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
            issues.Add(CreateIssue(
                "빈 예외 처리",
                "__CATCH 블록이 비어 있습니다.",
                change.FilePath,
                change.StartLine + lineNumber,
                "빈 예외 처리는 오류를 숨기고 디버깅을 어렵게 만듭니다.",
                "최소한 로깅이나 상태 설정을 추가하세요."));
        }

        return issues;
    }
}

/// <summary>
/// SA0140: 너무 많은 RETURN 문 감지
/// </summary>
public class SA0140_TooManyReturns : SARuleBase
{
    public override string RuleId => "SA0140";
    public override string RuleName => "과다 RETURN 문";
    public override string Description => "함수/메서드에 너무 많은 RETURN 문이 있는 경우를 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.Metrics;
    public override bool SupportsPrecompile => true;

    private const int MaxReturnStatements = 4;

    private static readonly Regex ReturnPattern = new(
        @"(?:^|\s)RETURN\s*;",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex FunctionPattern = new(
        @"(FUNCTION|METHOD)\s+(\w+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;

        // 함수나 메서드인지 확인
        var funcMatch = FunctionPattern.Match(code);
        if (!funcMatch.Success)
            return issues;

        var returnCount = ReturnPattern.Matches(code).Count;

        if (returnCount > MaxReturnStatements)
        {
            var funcType = funcMatch.Groups[1].Value;
            var funcName = funcMatch.Groups[2].Value;

            issues.Add(CreateIssue(
                "과다 RETURN 문",
                $"{funcType} '{funcName}'에 {returnCount}개의 RETURN 문이 있습니다. (권장: {MaxReturnStatements}개 이하)",
                change.FilePath,
                change.StartLine,
                @"
많은 RETURN 문의 문제점:
- 함수 흐름 이해 어려움
- 디버깅 복잡도 증가
- 리팩토링 어려움
",
                "Guard clause 패턴을 사용하거나 함수를 더 작은 단위로 분리하세요."));
        }

        return issues;
    }
}

#endregion

#region SA0141-SA0150 병렬/멀티태스크 관련 규칙

/// <summary>
/// SA0141: 태스크 간 공유 변수 감지
/// </summary>
public class SA0141_SharedVariable : SARuleBase
{
    public override string RuleId => "SA0141";
    public override string RuleName => "태스크 공유 변수";
    public override string Description => "멀티태스크 환경에서 보호되지 않은 공유 변수를 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Concurrency;
    public override bool SupportsPrecompile => true;

    private static readonly Regex VarGlobalPattern = new(
        @"VAR_GLOBAL",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckVariableChange(VariableChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed)
            return issues;

        var type = change.NewDataType?.ToUpperInvariant() ?? "";

        // VAR_GLOBAL 섹션 내부이거나 g로 시작하는 전역 변수 패턴
        bool isGlobalVar = change.VariableName.StartsWith("g", StringComparison.OrdinalIgnoreCase) ||
                          change.VariableName.StartsWith("GVL_", StringComparison.OrdinalIgnoreCase);

        if (isGlobalVar)
        {
            // 원자적이지 않은 타입 (32비트 초과 또는 복합 타입)
            if (type.Contains("LWORD") || type.Contains("LINT") || type.Contains("ULINT") ||
                type.Contains("LREAL") || type.Contains("STRING") ||
                type.Contains("STRUCT") || type.Contains("ARRAY") ||
                type.Contains("WSTRING"))
            {
                issues.Add(CreateIssue(
                    "비원자적 전역 변수",
                    $"전역 변수 '{change.VariableName}'의 타입 '{change.NewDataType}'은(는) 원자적 접근이 보장되지 않습니다.",
                    change.FilePath,
                    change.Line,
                    @"
멀티태스크 환경에서 비원자적 타입의 문제점:
- 읽기/쓰기 중 태스크 전환 시 데이터 손상 (Tearing)
- 경쟁 조건(Race Condition) 발생
- 예측 불가능한 동작

안전한 타입: BOOL, BYTE, WORD, DWORD, INT, DINT, UDINT, REAL (32비트)
위험한 타입: LWORD, LINT, LREAL, STRING, STRUCT, ARRAY
",
                    "보호 방법: FB_IecCriticalSection, MUTEX를 사용하거나 원자적 타입(32비트 이하)으로 분할하세요."));
            }
        }

        return issues;
    }
}

/// <summary>
/// SA0142: 세마포어/뮤텍스 사용 확인
/// </summary>
public class SA0142_SemaphoreUsage : SARuleBase
{
    public override string RuleId => "SA0142";
    public override string RuleName => "동기화 객체 사용";
    public override string Description => "세마포어나 뮤텍스 사용을 감지하고 검토합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.Concurrency;
    public override bool SupportsPrecompile => true;

    private static readonly Regex SyncObjectPattern = new(
        @"\b(FB_IecCriticalSection|FB_Mutex|FB_Semaphore)\b",
        RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var matches = SyncObjectPattern.Matches(code);

        if (matches.Count > 0)
        {
            issues.Add(CreateIssue(
                "동기화 객체 사용",
                "동기화 객체(Mutex, Semaphore 등)가 사용되고 있습니다.",
                change.FilePath,
                change.StartLine,
                "동기화 객체 사용 시 데드락에 주의해야 합니다.",
                "Enter/Leave가 항상 쌍으로 호출되는지 확인하세요."));
        }

        return issues;
    }
}

/// <summary>
/// SA0143: 태스크 우선순위 주의
/// </summary>
public class SA0143_TaskPriority : SARuleBase
{
    public override string RuleId => "SA0143";
    public override string RuleName => "태스크 우선순위";
    public override string Description => "태스크 우선순위 관련 코드를 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.Concurrency;
    public override bool SupportsPrecompile => true;

    private static readonly Regex TaskPriorityPattern = new(
        @"\bPriority\s*:=\s*(\d+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var matches = TaskPriorityPattern.Matches(code);

        foreach (Match match in matches)
        {
            var priority = int.Parse(match.Groups[1].Value);
            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;

            if (priority < 10)
            {
                issues.Add(CreateIssue(
                    "높은 태스크 우선순위",
                    $"태스크 우선순위가 {priority}로 매우 높게 설정되어 있습니다.",
                    change.FilePath,
                    change.StartLine + lineNumber,
                    "높은 우선순위는 다른 태스크를 기아 상태로 만들 수 있습니다.",
                    "시스템 전체의 태스크 우선순위 설계를 검토하세요."));
            }
        }

        return issues;
    }
}

/// <summary>
/// SA0144: 블로킹 호출 감지
/// </summary>
public class SA0144_BlockingCall : SARuleBase
{
    public override string RuleId => "SA0144";
    public override string RuleName => "블로킹 호출";
    public override string Description => "잠재적으로 블로킹될 수 있는 함수 호출을 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Concurrency;
    public override bool SupportsPrecompile => true;

    private static readonly string[] BlockingFunctions =
    {
        "FB_FileOpen", "FB_FileWrite", "FB_FileRead", "FB_FileCopy",
        "FB_TCPConnection", "FB_UDPSocket", "SLEEP", "DELAY"
    };

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;

        foreach (var func in BlockingFunctions)
        {
            if (code.Contains(func, StringComparison.OrdinalIgnoreCase))
            {
                var index = code.IndexOf(func, StringComparison.OrdinalIgnoreCase);
                var lineNumber = code.Take(index).Count(c => c == '\n') + 1;

                issues.Add(CreateIssue(
                    "블로킹 함수 호출",
                    $"잠재적 블로킹 함수 '{func}'가 호출되었습니다.",
                    change.FilePath,
                    change.StartLine + lineNumber,
                    "블로킹 호출은 PLC 태스크 실행을 지연시킬 수 있습니다.",
                    "비동기 버전을 사용하거나 별도 태스크에서 실행하세요."));
            }
        }

        return issues;
    }
}

/// <summary>
/// SA0145: 스핀락 패턴 감지
/// </summary>
public class SA0145_SpinLockPattern : SARuleBase
{
    public override string RuleId => "SA0145";
    public override string RuleName => "스핀락 패턴";
    public override string Description => "WHILE 루프를 사용한 스핀락 패턴을 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Concurrency;
    public override bool SupportsPrecompile => true;

    private static readonly Regex SpinLockPattern = new(
        @"WHILE\s+(NOT\s+)?(\w+)\s+DO\s*;\s*END_WHILE",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var matches = SpinLockPattern.Matches(code);

        foreach (Match match in matches)
        {
            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
            issues.Add(CreateIssue(
                "스핀락 패턴",
                "조건 대기를 위한 빈 WHILE 루프가 감지되었습니다.",
                change.FilePath,
                change.StartLine + lineNumber,
                @"
스핀락 패턴의 문제점:
- CPU 시간 낭비
- 다른 태스크 실행 방해
- Watchdog 타임아웃 위험
",
                "이벤트 기반 동기화나 상태 머신을 사용하세요."));
        }

        return issues;
    }
}

/// <summary>
/// SA0146: 원자적 연산 필요
/// </summary>
public class SA0146_AtomicOperationNeeded : SARuleBase
{
    public override string RuleId => "SA0146";
    public override string RuleName => "원자적 연산 필요";
    public override string Description => "원자적으로 수행되어야 하는 연산을 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Concurrency;
    public override bool SupportsPrecompile => true;

    private static readonly Regex IncrementDecrementPattern = new(
        @"(g\w+)\s*:=\s*\1\s*[+\-]\s*1",
        RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var matches = IncrementDecrementPattern.Matches(code);

        foreach (Match match in matches)
        {
            var varName = match.Groups[1].Value;
            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;

            issues.Add(CreateIssue(
                "비원자적 증감 연산",
                $"전역 변수 '{varName}'의 증감 연산이 원자적이지 않습니다.",
                change.FilePath,
                change.StartLine + lineNumber,
                "전역 변수의 증감은 읽기-수정-쓰기 연산으로 인터럽트 가능합니다.",
                "INTERLOCK 또는 원자적 연산 함수를 사용하세요."));
        }

        return issues;
    }
}

/// <summary>
/// SA0147: 사이클 타임 초과 위험
/// </summary>
public class SA0147_CycleTimeRisk : SARuleBase
{
    public override string RuleId => "SA0147";
    public override string RuleName => "사이클 타임 위험";
    public override string Description => "태스크 사이클 타임을 초과할 수 있는 코드를 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Concurrency;
    public override bool SupportsPrecompile => true;

    private const int MaxLoopIterations = 1000;

    private static readonly Regex LargeLoopPattern = new(
        @"FOR\s+\w+\s*:=\s*\d+\s+TO\s+(\d+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var matches = LargeLoopPattern.Matches(code);

        foreach (Match match in matches)
        {
            if (int.TryParse(match.Groups[1].Value, out int iterations) &&
                iterations > MaxLoopIterations)
            {
                var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
                issues.Add(CreateIssue(
                    "대형 FOR 루프",
                    $"FOR 루프가 {iterations}회 반복됩니다.",
                    change.FilePath,
                    change.StartLine + lineNumber,
                    $"{MaxLoopIterations}회 이상의 반복은 사이클 타임을 초과할 수 있습니다.",
                    "루프를 여러 사이클에 분산하거나 최적화하세요."));
            }
        }

        return issues;
    }
}

/// <summary>
/// SA0148: Watchdog 고려
/// </summary>
public class SA0148_WatchdogConsideration : SARuleBase
{
    public override string RuleId => "SA0148";
    public override string RuleName => "Watchdog 고려";
    public override string Description => "장시간 실행될 수 있는 코드에서 Watchdog 리셋을 확인합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.Concurrency;
    public override bool SupportsPrecompile => true;

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;

        // 중첩 루프 감지
        int forCount = Regex.Matches(code, @"\bFOR\b", RegexOptions.IgnoreCase).Count;
        int whileCount = Regex.Matches(code, @"\bWHILE\b", RegexOptions.IgnoreCase).Count;

        if (forCount + whileCount >= 2)
        {
            issues.Add(CreateIssue(
                "중첩 루프 - Watchdog 주의",
                "중첩 루프가 감지되었습니다. 실행 시간을 확인하세요.",
                change.FilePath,
                change.StartLine,
                "중첩 루프는 예상보다 오래 실행되어 Watchdog 타임아웃을 유발할 수 있습니다.",
                "필요시 실행 시간을 측정하고 루프를 분산하세요."));
        }

        return issues;
    }
}

/// <summary>
/// SA0149: 데드락 위험 패턴
/// </summary>
public class SA0149_DeadlockRisk : SARuleBase
{
    public override string RuleId => "SA0149";
    public override string RuleName => "데드락 위험";
    public override string Description => "데드락을 유발할 수 있는 패턴을 감지합니다.";
    public override Severity Severity => Severity.Critical;
    public override SARuleCategory Category => SARuleCategory.Concurrency;
    public override bool SupportsPrecompile => true;

    // 중첩 잠금 패턴
    private static readonly Regex NestedLockPattern = new(
        @"Enter\s*\(\).*?Enter\s*\(\)",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;

        if (NestedLockPattern.IsMatch(code))
        {
            issues.Add(CreateIssue(
                "중첩 잠금 - 데드락 위험",
                "중첩된 잠금(Enter) 호출이 감지되었습니다.",
                change.FilePath,
                change.StartLine,
                @"
중첩 잠금의 위험성:
- 데드락 발생 가능
- 잠금 순서 불일치로 인한 교착 상태
- 디버깅 어려움
",
                "잠금 순서를 일관되게 유지하거나 단일 잠금으로 통합하세요."));
        }

        return issues;
    }
}

/// <summary>
/// SA0150: 인터럽트 비활성화 감지
/// </summary>
public class SA0150_InterruptDisable : SARuleBase
{
    public override string RuleId => "SA0150";
    public override string RuleName => "인터럽트 비활성화";
    public override string Description => "인터럽트 비활성화 코드를 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Concurrency;
    public override bool SupportsPrecompile => true;

    private static readonly Regex InterruptDisablePattern = new(
        @"\b(DisableInterrupts?|__disable_irq|TcInterruptDisable)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var matches = InterruptDisablePattern.Matches(code);

        foreach (Match match in matches)
        {
            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
            issues.Add(CreateIssue(
                "인터럽트 비활성화",
                "인터럽트 비활성화 코드가 감지되었습니다.",
                change.FilePath,
                change.StartLine + lineNumber,
                @"
인터럽트 비활성화의 위험성:
- 시스템 응답성 저하
- 다른 태스크 지연
- 하드웨어 이벤트 누락
",
                "최소한의 시간 동안만 비활성화하고, 반드시 다시 활성화하세요."));
        }

        return issues;
    }
}

#endregion

#region SA0151-SA0160 PLCopen/IEC 준수 규칙

/// <summary>
/// SA0151: PLCopen 함수 블록 규칙
/// </summary>
public class SA0151_PLCopenFBRule : SARuleBase
{
    public override string RuleId => "SA0151";
    public override string RuleName => "PLCopen FB 규칙";
    public override string Description => "PLCopen 함수 블록 설계 규칙 준수를 확인합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.StrictIEC;
    public override bool IsPLCopenRelated => true;
    public override bool SupportsPrecompile => true;

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;

        // PLCopen FB는 Execute/Done/Error/ErrorID 패턴을 따라야 함
        if (code.Contains("FUNCTION_BLOCK", StringComparison.OrdinalIgnoreCase))
        {
            bool hasExecute = Regex.IsMatch(code, @"\bExecute\s*:", RegexOptions.IgnoreCase);
            bool hasDone = Regex.IsMatch(code, @"\bDone\s*:", RegexOptions.IgnoreCase);
            bool hasError = Regex.IsMatch(code, @"\bError\s*:", RegexOptions.IgnoreCase);

            if (hasExecute && (!hasDone || !hasError))
            {
                issues.Add(CreateIssue(
                    "PLCopen 패턴 불완전",
                    "Execute 입력이 있지만 Done/Error 출력이 누락되었습니다.",
                    change.FilePath,
                    change.StartLine,
                    "PLCopen 함수 블록은 Execute/Done/Error 패턴을 따라야 합니다.",
                    "Done, Error, ErrorID 출력을 추가하세요."));
            }
        }

        return issues;
    }
}

/// <summary>
/// SA0152: IEC 타입 크기 주의
/// </summary>
public class SA0152_IECTypeSize : SARuleBase
{
    public override string RuleId => "SA0152";
    public override string RuleName => "IEC 타입 크기";
    public override string Description => "플랫폼 의존적 타입 크기 사용을 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.StrictIEC;
    public override bool SupportsPrecompile => true;

    public override List<QAIssue> CheckVariableChange(VariableChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewDataType))
            return issues;

        var type = change.NewDataType.ToUpperInvariant();

        // LWORD, ULINT 등은 64비트 플랫폼 의존적
        if (type.Contains("LWORD") || type.Contains("LINT") || type.Contains("ULINT"))
        {
            issues.Add(CreateIssue(
                "64비트 타입 사용",
                $"'{type}'은 64비트 타입으로 플랫폼에 따라 지원이 다를 수 있습니다.",
                change.FilePath,
                change.Line,
                "64비트 타입은 일부 PLC에서 지원되지 않거나 성능 차이가 있습니다.",
                "호환성이 중요한 경우 32비트 타입 사용을 고려하세요."));
        }

        return issues;
    }
}

/// <summary>
/// SA0153: 직접 주소 표기법
/// </summary>
public class SA0153_DirectAddressNotation : SARuleBase
{
    public override string RuleId => "SA0153";
    public override string RuleName => "직접 주소 표기법";
    public override string Description => "IEC 61131-3 직접 주소 표기법 사용을 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.StrictIEC;
    public override bool SupportsPrecompile => true;

    // %IX1.0, %QW5, %MB100 등 직접 주소 패턴
    private static readonly Regex DirectAddressPattern = new(
        @"%[IQMX][XBWDL]?\d+(?:\.\d+)?",
        RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var matches = DirectAddressPattern.Matches(code);

        if (matches.Count > 0)
        {
            // 각 직접 주소를 개별로 보고
            var reportedAddresses = new HashSet<string>();

            foreach (Match match in matches)
            {
                var address = match.Value;
                if (reportedAddresses.Contains(address))
                    continue;

                reportedAddresses.Add(address);
                var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;

                issues.Add(CreateIssue(
                    "직접 주소 사용",
                    $"직접 주소 '{address}'이(가) 사용되었습니다.",
                    change.FilePath,
                    change.StartLine + lineNumber,
                    @"
직접 주소 표기법의 문제점:
- 하드웨어 변경 시 코드 수정 필요
- 의미 파악 어려움 (%IX1.0 보다 bStartButton이 명확)
- 재사용성 저하
- 유지보수 어려움

예:
  나쁨: IF %IX1.0 THEN ...
  좋음: IF bStartButton THEN ...  // AT %IX1.0로 매핑
",
                    "변수를 선언하고 AT 지시어로 주소를 매핑하세요. (예: bStartButton AT %IX1.0 : BOOL;)"));
            }

            // 과도한 사용 추가 경고
            if (matches.Count > 5)
            {
                issues.Add(CreateIssue(
                    "과도한 직접 주소 사용",
                    $"파일에 총 {matches.Count}개의 직접 주소가 사용되었습니다. (권장: 5개 이하)",
                    change.FilePath,
                    change.StartLine,
                    "많은 직접 주소는 코드 가독성과 유지보수성을 크게 저하시킵니다.",
                    "I/O 매핑을 사용하여 심볼릭 변수로 변경하세요."));
            }
        }

        return issues;
    }
}

/// <summary>
/// SA0154: 언어 호환성
/// </summary>
public class SA0154_LanguageCompatibility : SARuleBase
{
    public override string RuleId => "SA0154";
    public override string RuleName => "언어 호환성";
    public override string Description => "특정 벤더 확장 기능 사용을 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.StrictIEC;
    public override bool SupportsPrecompile => true;

    // TwinCAT 특정 확장
    private static readonly string[] VendorExtensions =
    {
        "__NEW", "__DELETE", "__TRY", "__CATCH", "__FINALLY",
        "SUPER^", "THIS^", "REFERENCE TO", "{attribute"
    };

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;

        foreach (var ext in VendorExtensions)
        {
            if (code.Contains(ext, StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(CreateIssue(
                    "벤더 확장 사용",
                    $"TwinCAT 전용 확장 '{ext}'이(가) 사용되었습니다.",
                    change.FilePath,
                    change.StartLine,
                    "벤더 확장은 다른 PLC 시스템과 호환되지 않습니다.",
                    "이식성이 필요하면 표준 IEC 기능을 사용하세요."));
                break; // 하나만 보고
            }
        }

        return issues;
    }
}

/// <summary>
/// SA0155: VAR_CONFIG 사용
/// </summary>
public class SA0155_VarConfigUsage : SARuleBase
{
    public override string RuleId => "SA0155";
    public override string RuleName => "VAR_CONFIG 사용";
    public override string Description => "VAR_CONFIG 사용을 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.StrictIEC;
    public override bool SupportsPrecompile => true;

    private static readonly Regex VarConfigPattern = new(
        @"VAR_CONFIG\s*(.*?)END_VAR",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var match = VarConfigPattern.Match(code);

        if (match.Success)
        {
            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
            issues.Add(CreateIssue(
                "VAR_CONFIG 사용",
                "VAR_CONFIG가 사용되었습니다.",
                change.FilePath,
                change.StartLine + lineNumber,
                "VAR_CONFIG는 IEC 61131-3의 고급 기능으로 이해가 필요합니다.",
                "VAR_CONFIG 사용 시 I/O 매핑 구조를 문서화하세요."));
        }

        return issues;
    }
}

/// <summary>
/// SA0156: 표준 라이브러리 함수 사용 권장
/// </summary>
public class SA0156_UseStandardLibrary : SARuleBase
{
    public override string RuleId => "SA0156";
    public override string RuleName => "표준 라이브러리 권장";
    public override string Description => "표준 라이브러리 함수 대신 직접 구현한 경우를 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.StrictIEC;
    public override bool SupportsPrecompile => true;

    // 직접 구현하기 쉬운 표준 함수들
    private static readonly Dictionary<string, string> CustomPatterns = new()
    {
        { @"IF\s+\w+\s*>\s*\w+\s+THEN\s+\w+\s*:=\s*\w+\s*;\s*ELSE\s+\w+\s*:=\s*\w+\s*;", "MAX 함수 사용 권장" },
        { @"IF\s+\w+\s*<\s*\w+\s+THEN\s+\w+\s*:=\s*\w+\s*;\s*ELSE\s+\w+\s*:=\s*\w+\s*;", "MIN 함수 사용 권장" }
    };

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;

        foreach (var pattern in CustomPatterns)
        {
            if (Regex.IsMatch(code, pattern.Key, RegexOptions.IgnoreCase))
            {
                issues.Add(CreateIssue(
                    "표준 함수 사용 권장",
                    pattern.Value,
                    change.FilePath,
                    change.StartLine,
                    "표준 라이브러리 함수는 검증되어 있고 최적화되어 있습니다.",
                    "해당 표준 함수 사용을 고려하세요."));
            }
        }

        return issues;
    }
}

/// <summary>
/// SA0157: 비트 접근 표기법
/// </summary>
public class SA0157_BitAccessNotation : SARuleBase
{
    public override string RuleId => "SA0157";
    public override string RuleName => "비트 접근 표기법";
    public override string Description => "비표준 비트 접근 표기법 사용을 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.StrictIEC;
    public override bool SupportsPrecompile => true;

    private static readonly Regex NonStandardBitAccess = new(
        @"\w+\s*\.\s*\d+",
        RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var matches = NonStandardBitAccess.Matches(code);

        foreach (Match match in matches)
        {
            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
            issues.Add(CreateIssue(
                "비트 접근 표기법",
                $"'{match.Value}' 형식의 비트 접근이 사용되었습니다.",
                change.FilePath,
                change.StartLine + lineNumber,
                "일부 PLC에서는 다른 비트 접근 문법을 사용합니다.",
                "변수.비트번호 대신 변수.%X번호 형식을 고려하세요."));
        }

        return issues;
    }
}

/// <summary>
/// SA0158: 데이터 타입 범위 문서화
/// </summary>
public class SA0158_DataTypeRangeDoc : SARuleBase
{
    public override string RuleId => "SA0158";
    public override string RuleName => "데이터 범위 문서화";
    public override string Description => "물리적 범위가 있는 변수의 문서화를 확인합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.Comments;
    public override bool SupportsPrecompile => true;

    // 범위가 필요한 변수 패턴
    private static readonly Regex RangeVarPattern = new(
        @"(Temperature|Pressure|Speed|Position|Velocity|Current|Voltage)\w*\s*:\s*(L?REAL)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckVariableChange(VariableChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed)
            return issues;

        var fullDecl = $"{change.VariableName}: {change.NewDataType}";

        if (RangeVarPattern.IsMatch(fullDecl))
        {
            issues.Add(CreateIssue(
                "물리적 범위 문서화 권장",
                $"변수 '{change.VariableName}'에 물리적 범위를 주석으로 명시하세요.",
                change.FilePath,
                change.Line,
                "물리적 의미가 있는 변수는 유효 범위를 문서화해야 합니다.",
                "예: rTemperature: REAL; // 범위: -40.0 ~ 120.0 [°C]"));
        }

        return issues;
    }
}

/// <summary>
/// SA0159: 단위 일관성
/// </summary>
public class SA0159_UnitConsistency : SARuleBase
{
    public override string RuleId => "SA0159";
    public override string RuleName => "단위 일관성";
    public override string Description => "변수 이름에서 단위 표기의 일관성을 확인합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.NamingConventions;
    public override bool SupportsPrecompile => true;

    // 단위가 포함된 변수명 패턴
    private static readonly Regex UnitInNamePattern = new(
        @"(\w+)(Sec|Ms|Us|Ns|Mm|Cm|M|Km|Kg|Hz|Rpm|Deg|Rad)",
        RegexOptions.Compiled);

    public override List<QAIssue> CheckVariableChange(VariableChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed)
            return issues;

        var match = UnitInNamePattern.Match(change.VariableName);
        if (match.Success)
        {
            var unit = match.Groups[2].Value;

            // 단위 표기 일관성 확인 (첫 글자 대문자 권장)
            if (unit.Length > 2 && unit != unit[0] + unit.Substring(1).ToLowerInvariant())
            {
                issues.Add(CreateIssue(
                    "단위 표기 일관성",
                    $"변수명의 단위 '{unit}'가 표준 표기법과 다릅니다.",
                    change.FilePath,
                    change.Line,
                    "일관된 단위 표기법은 코드 가독성을 높입니다.",
                    "SI 단위 표기법을 따르세요: Ms, Mm, Kg 등"));
            }
        }

        return issues;
    }
}

/// <summary>
/// SA0160: 프로그램 구조 복잡도
/// </summary>
public class SA0160_ProgramStructureComplexity : SARuleBase
{
    public override string RuleId => "SA0160";
    public override string RuleName => "프로그램 구조 복잡도";
    public override string Description => "PROGRAM의 전체적인 복잡도를 분석합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.Metrics;
    public override bool SupportsPrecompile => true;

    private const int MaxProgramLines = 500;
    private const int MaxProgramVars = 50;

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;

        if (code.Contains("PROGRAM", StringComparison.OrdinalIgnoreCase))
        {
            var lines = code.Split('\n').Length;
            var varCount = Regex.Matches(code, @"^\s*\w+\s*:", RegexOptions.Multiline).Count;

            if (lines > MaxProgramLines)
            {
                issues.Add(CreateIssue(
                    "대형 PROGRAM",
                    $"PROGRAM이 {lines}줄입니다. (권장: {MaxProgramLines}줄 이하)",
                    change.FilePath,
                    change.StartLine,
                    "대형 프로그램은 유지보수가 어렵습니다.",
                    "기능별로 FB로 분리하세요."));
            }

            if (varCount > MaxProgramVars)
            {
                issues.Add(CreateIssue(
                    "많은 변수의 PROGRAM",
                    $"PROGRAM에 {varCount}개의 변수가 있습니다. (권장: {MaxProgramVars}개 이하)",
                    change.FilePath,
                    change.StartLine,
                    "변수가 많으면 프로그램 이해가 어렵습니다.",
                    "관련 변수를 구조체로 그룹화하거나 FB로 캡슐화하세요."));
            }
        }

        return issues;
    }
}

#endregion
