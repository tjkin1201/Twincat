using System.Text.RegularExpressions;
using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Models.QA;

namespace TwinCatQA.Infrastructure.QA.Rules.TE1200;

/// <summary>
/// SA0016: 구조체의 갭
/// 구조체 멤버 정렬로 인한 메모리 갭 감지
/// </summary>
public class SA0016_GapsInStructures : SARuleBase
{
    public override string RuleId => "SA0016";
    public override string RuleName => "구조체의 갭";
    public override string Description => "구조체 멤버 정렬로 인한 메모리 갭을 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.MemoryLayout;
    public override bool EnabledByDefault => false; // Beckhoff 기본 비활성화

    public override List<QAIssue> CheckDataTypeChange(DataTypeChange change)
    {
        var issues = new List<QAIssue>();

        if (change.NewDefinition?.Contains("STRUCT") == true)
        {
            // 구조체 멤버 타입 크기 분석
            var members = ParseStructMembers(change.NewDefinition);
            if (members.Count >= 3 && HasSuboptimalLayout(members))
            {
                issues.Add(CreateIssue(
                    "구조체 메모리 갭 가능성",
                    $"구조체 '{change.TypeName}'의 멤버 순서가 최적이 아닙니다. 메모리 갭이 발생할 수 있습니다.",
                    change.FilePath,
                    change.Line,
                    @"
잘못된 멤버 순서는 메모리 정렬 규칙에 의해 패딩을 유발합니다:
- LREAL(8바이트) 뒤 BOOL(1바이트)은 7바이트 낭비
- 메모리 사용량 증가
- 성능 저하 가능
",
                    @"
✅ 권장 해결 방법:
멤버를 크기 순으로 정렬하세요:
1. LREAL, LINT (8바이트)
2. REAL, DINT, TIME (4바이트)
3. INT, WORD (2바이트)
4. BYTE, SINT, BOOL (1바이트)
"));
            }
        }

        return issues;
    }

    private List<(string name, int size)> ParseStructMembers(string structDef)
    {
        var members = new List<(string, int)>();
        var memberPattern = new Regex(@"(\w+)\s*:\s*(LREAL|REAL|LINT|DINT|INT|WORD|BYTE|SINT|BOOL)", RegexOptions.IgnoreCase);

        foreach (Match match in memberPattern.Matches(structDef))
        {
            var type = match.Groups[2].Value.ToUpperInvariant();
            var size = type switch
            {
                "LREAL" or "LINT" => 8,
                "REAL" or "DINT" or "TIME" => 4,
                "INT" or "WORD" => 2,
                _ => 1
            };
            members.Add((match.Groups[1].Value, size));
        }

        return members;
    }

    private bool HasSuboptimalLayout(List<(string name, int size)> members)
    {
        // 큰 타입 뒤 작은 타입이 오는 경우 감지
        for (int i = 0; i < members.Count - 1; i++)
        {
            if (members[i].size < members[i + 1].size)
                return true; // 작은 타입 뒤 큰 타입: 비최적
        }
        return false;
    }
}

/// <summary>
/// SA0017: 포인터 변수에 비정상 할당
/// ADR 연산자나 0이 아닌 값을 포인터에 할당 감지
/// </summary>
public class SA0017_IrregularPointerAssignment : SARuleBase
{
    public override string RuleId => "SA0017";
    public override string RuleName => "포인터 변수에 비정상 할당";
    public override string Description => "ADR 연산자나 상수 0이 아닌 값을 포인터에 할당하는 것을 감지합니다.";
    public override Severity Severity => Severity.Critical;
    public override SARuleCategory Category => SARuleCategory.Operations;

    // 포인터 변수에 숫자 리터럴 직접 할당 (ADR, 0, NULL, 다른 포인터 제외)
    private static readonly Regex InvalidPointerAssignment = new(
        @"(?:p[A-Z]\w*|POINTER\s+TO\s+\w+)\s*:=\s*(?!ADR\s*\(|NULL|0(?:\s|;)|[a-zA-Z_]\w*(?:\s|;))[0-9]+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();
        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var matches = InvalidPointerAssignment.Matches(change.NewCode);
        foreach (Match match in matches)
        {
            var lineNumber = change.NewCode.Take(match.Index).Count(c => c == '\n') + 1;

            // 할당된 숫자 값 추출
            var assignedValue = Regex.Match(match.Value, @":=\s*([0-9]+)");
            var value = assignedValue.Success ? assignedValue.Groups[1].Value : "unknown";

            issues.Add(CreateIssue(
                "포인터에 비정상 값 할당",
                $"포인터에 숫자 리터럴 {value}이(가) 직접 할당되었습니다.",
                change.FilePath,
                change.StartLine + lineNumber,
                @"
유효하지 않은 포인터 값 할당은 심각한 오류를 유발합니다:
- Access Violation Exception (잘못된 메모리 접근)
- 시스템 크래시
- 메모리 손상
- 예측 불가능한 동작
",
                @"
✅ 포인터 할당 시 허용되는 방법:
1. ADR(variable) - 변수의 주소
2. 다른 포인터 변수 할당
3. 초기화 시 0 또는 NULL만 허용
4. 함수에서 반환된 포인터

❌ 절대 금지:
- 임의의 숫자 리터럴 할당
",
                match.Value,
                "pData := ADR(sourceVariable);"));
        }

        return issues;
    }
}

/// <summary>
/// SA0018: 비정상적인 비트 접근
/// 부호 있는 변수에 대한 비트 접근 감지
/// </summary>
public class SA0018_UnusualBitAccess : SARuleBase
{
    public override string RuleId => "SA0018";
    public override string RuleName => "비정상적인 비트 접근";
    public override string Description => "부호 있는 변수에 대한 비트 접근을 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Operations;

    // 비트 접근 패턴: variable.bitNumber (단, 속성 접근 .Property는 제외)
    private static readonly Regex BitAccessPattern = new(
        @"\b(\w+)\.(\d+)\b",
        RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();
        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var matches = BitAccessPattern.Matches(change.NewCode);
        foreach (Match match in matches)
        {
            var varName = match.Groups[1].Value;
            var bitNum = match.Groups[2].Value;

            // 명백한 부호 있는 타입 명명 패턴
            bool isSignedType =
                varName.StartsWith("n", StringComparison.OrdinalIgnoreCase) &&
                !varName.StartsWith("nU", StringComparison.OrdinalIgnoreCase) &&
                !varName.StartsWith("nW", StringComparison.OrdinalIgnoreCase);

            // 또는 리터럴 INT/DINT 값에 비트 접근
            bool isSignedLiteral = Regex.IsMatch(change.NewCode,
                $@"(INT|DINT|SINT)#\d+\.{bitNum}\b",
                RegexOptions.IgnoreCase);

            if (isSignedType || isSignedLiteral)
            {
                var lineNumber = change.NewCode.Take(match.Index).Count(c => c == '\n') + 1;
                issues.Add(CreateIssue(
                    "부호 있는 변수에 비트 접근",
                    $"부호 있는 타입 변수 '{varName}'에 비트 접근(.{bitNum})이 사용되었습니다.",
                    change.FilePath,
                    change.StartLine + lineNumber,
                    @"
부호 있는 타입에 비트 접근은 문제를 야기합니다:
- IEC 61131-3 표준 위반
- MSB는 부호 비트이므로 의미가 다름
- 음수 처리 시 예상치 못한 동작
",
                    @"
✅ 권장 해결 방법:
1. BYTE, WORD, DWORD 등 부호 없는 타입 사용
2. 비트 연산이 필요하면 타입 변환 후 처리
3. 비트마스킹 연산 사용 (AND, OR)

예시:
IF (nStatus AND 16#0001) <> 0 THEN  // 올바른 비트 검사
",
                    match.Value,
                    $"// WORD 타입 변환 또는 비트 마스킹 사용"));
            }
        }

        return issues;
    }
}

/// <summary>
/// SA0019: 암시적 포인터 변환
/// 포인터 타입 간 암시적 변환 감지
/// </summary>
public class SA0019_ImplicitPointerConversion : SARuleBase
{
    public override string RuleId => "SA0019";
    public override string RuleName => "암시적 포인터 변환";
    public override string Description => "포인터 타입 간 암시적 변환을 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Conversions;

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();
        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        // POINTER TO 간 할당 패턴
        if (Regex.IsMatch(change.NewCode, @"POINTER\s+TO\s+\w+.*:=.*POINTER\s+TO", RegexOptions.IgnoreCase))
        {
            issues.Add(CreateIssue(
                "포인터 타입 간 변환",
                "서로 다른 포인터 타입 간 변환이 감지되었습니다.",
                change.FilePath,
                change.StartLine,
                "암시적 포인터 변환은 타입 안전성을 해칩니다.",
                "명시적 타입 캐스팅을 사용하거나 동일한 포인터 타입을 사용하세요."));
        }

        return issues;
    }
}

/// <summary>
/// SA0020: REAL 변수에 잘린 값 할당 가능성
/// 정수를 REAL로 변환 시 정밀도 손실 감지
/// </summary>
public class SA0020_TruncatedRealAssignment : SARuleBase
{
    public override string RuleId => "SA0020";
    public override string RuleName => "REAL 변수에 잘린 값 할당";
    public override string Description => "큰 정수를 REAL로 변환 시 정밀도 손실 가능성을 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Conversions;
    public override bool SupportsPrecompile => true;

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();
        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        // DINT/LINT를 REAL에 할당하는 패턴
        if (Regex.IsMatch(change.NewCode, @"r\w+\s*:=\s*(n|l)(D|L)INT", RegexOptions.IgnoreCase) ||
            Regex.IsMatch(change.NewCode, @"REAL\s*#?\s*\(\s*(DINT|LINT)", RegexOptions.IgnoreCase))
        {
            issues.Add(CreateIssue(
                "REAL로의 정밀도 손실 가능성",
                "큰 정수(DINT/LINT)를 REAL로 변환하면 정밀도가 손실될 수 있습니다.",
                change.FilePath,
                change.StartLine,
                "REAL은 약 7자리 유효숫자만 표현할 수 있어 큰 정수 변환 시 정밀도 손실이 발생합니다.",
                "LREAL 사용을 고려하거나 정밀도 손실이 허용되는지 확인하세요."));
        }

        return issues;
    }
}

/// <summary>
/// SA0021: 임시 변수의 주소 전달
/// 스택 변수(로컬)의 주소를 외부로 전달 감지
/// </summary>
public class SA0021_AddressOfTemporary : SARuleBase
{
    public override string RuleId => "SA0021";
    public override string RuleName => "임시 변수의 주소 전달";
    public override string Description => "로컬(스택) 변수의 주소를 외부로 전달하는 것을 감지합니다.";
    public override Severity Severity => Severity.Critical;
    public override SARuleCategory Category => SARuleCategory.Safety;

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();
        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        // ADR(로컬변수)를 포인터 파라미터나 전역에 할당하는 패턴
        var adrLocalPattern = new Regex(@"(p\w+|POINTER)\s*:=\s*ADR\s*\(\s*(\w+)\s*\)", RegexOptions.IgnoreCase);
        var matches = adrLocalPattern.Matches(change.NewCode);

        foreach (Match match in matches)
        {
            var lineNumber = change.NewCode.Take(match.Index).Count(c => c == '\n') + 1;
            issues.Add(CreateIssue(
                "임시 변수 주소 전달",
                $"로컬 변수 '{match.Groups[2].Value}'의 주소가 포인터에 할당되었습니다.",
                change.FilePath,
                change.StartLine + lineNumber,
                @"
로컬 변수는 함수/메서드 실행 중에만 존재합니다:
- 함수 종료 후 포인터는 무효한 메모리를 가리킴
- 다른 함수 호출 시 해당 메모리가 덮어쓰여짐
- Access Violation 또는 데이터 손상 발생
",
                @"
✅ 해결 방법:
1. VAR_STAT (정적 로컬 변수) 사용
2. 전역 변수 사용
3. 호출자가 버퍼를 제공하도록 설계 변경
"));
        }

        return issues;
    }
}

/// <summary>
/// SA0022: 반환값 미거부
/// 반환값이 있는 함수에서 모든 경로에서 반환하지 않는 경우 감지
/// </summary>
public class SA0022_NonRejectedReturnValue : SARuleBase
{
    public override string RuleId => "SA0022";
    public override string RuleName => "반환값 미거부";
    public override string Description => "반환값이 있는 함수에서 반환값을 설정하지 않는 실행 경로를 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.UnreachableUnusedCode;
    public override bool SupportsPrecompile => true;

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();
        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        // FUNCTION 정의에서 반환값 할당 검사
        if (change.NewCode.Contains("FUNCTION", StringComparison.OrdinalIgnoreCase) &&
            change.NewCode.Contains(":", StringComparison.OrdinalIgnoreCase))
        {
            // 함수명 := 패턴이 없으면 경고
            var funcNameMatch = Regex.Match(change.NewCode, @"FUNCTION\s+(\w+)\s*:", RegexOptions.IgnoreCase);
            if (funcNameMatch.Success)
            {
                var funcName = funcNameMatch.Groups[1].Value;
                if (!Regex.IsMatch(change.NewCode, $@"{funcName}\s*:=", RegexOptions.IgnoreCase))
                {
                    issues.Add(CreateIssue(
                        "함수 반환값 미설정",
                        $"함수 '{funcName}'에서 반환값을 설정하지 않는 실행 경로가 있을 수 있습니다.",
                        change.FilePath,
                        change.StartLine,
                        "반환값을 설정하지 않으면 호출자가 불확정 값을 받게 됩니다.",
                        "모든 실행 경로에서 반환값을 설정하세요."));
                }
            }
        }

        return issues;
    }
}

/// <summary>
/// SA0023: 복잡한 반환값
/// 구조체/배열을 반환하는 함수 감지
/// </summary>
public class SA0023_ComplexReturnValue : SARuleBase
{
    public override string RuleId => "SA0023";
    public override string RuleName => "복잡한 반환값";
    public override string Description => "구조체나 배열을 반환하는 함수를 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.Declarations;

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();
        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        // FUNCTION : STRUCT 또는 ARRAY 패턴
        if (Regex.IsMatch(change.NewCode, @"FUNCTION\s+\w+\s*:\s*(ARRAY|STRUCT|ST_)", RegexOptions.IgnoreCase))
        {
            issues.Add(CreateIssue(
                "복잡한 타입 반환",
                "함수가 구조체나 배열을 반환합니다.",
                change.FilePath,
                change.StartLine,
                "복잡한 타입 반환은 메모리 복사 오버헤드가 발생합니다.",
                "VAR_IN_OUT 파라미터를 사용하거나 포인터 반환을 고려하세요."));
        }

        return issues;
    }
}

/// <summary>
/// SA0024: 타입 없는 리터럴
/// 타입 접두사 없는 리터럴 사용 감지
/// </summary>
public class SA0024_UntypedLiterals : SARuleBase
{
    public override string RuleId => "SA0024";
    public override string RuleName => "타입 없는 리터럴";
    public override string Description => "타입 접두사 없는 리터럴 사용을 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.Conversions;
    public override bool EnabledByDefault => false; // Beckhoff 기본 비활성화

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();
        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        // 타입 접두사 없는 숫자 리터럴 (예: 34 대신 INT#34)
        var untypedPattern = new Regex(@"(?<!\w#)\b\d+\b(?!\s*\.\.)", RegexOptions.Compiled);
        var matches = untypedPattern.Matches(change.NewCode);

        if (matches.Count > 5) // 많은 경우만 경고
        {
            issues.Add(CreateIssue(
                "타입 없는 리터럴 다수 사용",
                $"타입 접두사 없는 리터럴이 {matches.Count}개 발견되었습니다.",
                change.FilePath,
                change.StartLine,
                "타입 없는 리터럴은 암시적 타입 변환을 유발할 수 있습니다.",
                "INT#34, REAL#3.14 등 타입 접두사를 사용하세요."));
        }

        return issues;
    }
}

/// <summary>
/// SA0025: 비한정 열거형 상수
/// 열거형 타입명 없이 사용된 열거형 상수 감지
/// </summary>
public class SA0025_UnqualifiedEnumConstants : SARuleBase
{
    public override string RuleId => "SA0025";
    public override string RuleName => "비한정 열거형 상수";
    public override string Description => "열거형 타입명 없이 사용된 열거형 상수를 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.NamingConventions;

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();
        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        // e로 시작하는 열거형 상수가 타입명 없이 사용된 경우 (E_xxx.eValue 권장)
        if (Regex.IsMatch(change.NewCode, @"(?<!E_\w+\.)\be[A-Z]\w+\b", RegexOptions.Compiled))
        {
            issues.Add(CreateIssue(
                "비한정 열거형 상수",
                "열거형 상수가 타입명 없이 사용되었습니다.",
                change.FilePath,
                change.StartLine,
                "비한정 열거형 상수는 코드 가독성을 저하시킵니다.",
                "E_EnumType.eValue 형태로 한정된 이름을 사용하세요."));
        }

        return issues;
    }
}

/// <summary>
/// SA0026-SA0030: 추가 규칙들
/// </summary>
public class SA0026_UseOfDirectAddresses : SARuleBase
{
    public override string RuleId => "SA0026";
    public override string RuleName => "직접 주소 사용";
    public override string Description => "%I, %Q, %M 등 직접 주소 사용을 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Safety;

    private static readonly Regex DirectAddressPattern = new(@"%[IQM][XBWD]?\d+(\.\d+)?", RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();
        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var matches = DirectAddressPattern.Matches(change.NewCode);
        foreach (Match match in matches)
        {
            var lineNumber = change.NewCode.Take(match.Index).Count(c => c == '\n') + 1;
            issues.Add(CreateIssue(
                "직접 주소 사용",
                $"직접 주소 '{match.Value}'가 사용되었습니다.",
                change.FilePath,
                change.StartLine + lineNumber,
                "직접 주소 사용은 하드웨어 의존성을 만들고 이식성을 저하시킵니다.",
                "심볼릭 변수와 I/O 매핑을 사용하세요."));
        }

        return issues;
    }
}

public class SA0027_UnsafeTypeConversion : SARuleBase
{
    public override string RuleId => "SA0027";
    public override string RuleName => "안전하지 않은 타입 변환";
    public override string Description => "데이터 손실 가능성이 있는 타입 변환을 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Conversions;

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();
        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        // 큰 타입에서 작은 타입으로 변환
        var dangerousConversions = new[] {
            (@"TO_INT\s*\(\s*\w*DINT", "DINT → INT"),
            (@"TO_SINT\s*\(\s*\w*INT", "INT → SINT"),
            (@"TO_REAL\s*\(\s*\w*LREAL", "LREAL → REAL")
        };

        foreach (var (pattern, conversion) in dangerousConversions)
        {
            if (Regex.IsMatch(change.NewCode, pattern, RegexOptions.IgnoreCase))
            {
                issues.Add(CreateIssue(
                    "안전하지 않은 타입 변환",
                    $"데이터 손실 가능성이 있는 변환: {conversion}",
                    change.FilePath,
                    change.StartLine,
                    "큰 타입에서 작은 타입으로 변환 시 데이터가 잘릴 수 있습니다.",
                    "변환 전 범위를 확인하거나 적절한 타입을 사용하세요."));
            }
        }

        return issues;
    }
}

public class SA0028_NestedComments : SARuleBase
{
    public override string RuleId => "SA0028";
    public override string RuleName => "중첩 주석";
    public override string Description => "(* 주석 내 (* 중첩 주석 *)을 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Comments;

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();
        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        int depth = 0;
        bool hasNested = false;
        for (int i = 0; i < change.NewCode.Length - 1; i++)
        {
            if (change.NewCode[i] == '(' && change.NewCode[i + 1] == '*')
            {
                depth++;
                if (depth > 1) hasNested = true;
            }
            else if (change.NewCode[i] == '*' && change.NewCode[i + 1] == ')')
            {
                depth = Math.Max(0, depth - 1);
            }
        }

        if (hasNested)
        {
            issues.Add(CreateIssue(
                "중첩 주석 감지",
                "주석 내에 중첩된 주석이 있습니다.",
                change.FilePath,
                change.StartLine,
                "중첩 주석은 코드 가독성을 저하시키고 의도하지 않은 코드 제외를 유발할 수 있습니다.",
                "중첩 주석을 피하고 // 스타일 주석을 사용하세요."));
        }

        return issues;
    }
}

public class SA0029_TODO_Comments : SARuleBase
{
    public override string RuleId => "SA0029";
    public override string RuleName => "TODO 주석";
    public override string Description => "TODO, FIXME, HACK 등의 작업 표시 주석을 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.Comments;

    private static readonly Regex TodoPattern = new(@"//\s*(TODO|FIXME|HACK|XXX|BUG):", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();
        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var matches = TodoPattern.Matches(change.NewCode);
        foreach (Match match in matches)
        {
            var lineNumber = change.NewCode.Take(match.Index).Count(c => c == '\n') + 1;
            issues.Add(CreateIssue(
                $"{match.Groups[1].Value} 주석",
                $"작업 표시 주석이 발견되었습니다: {match.Value}",
                change.FilePath,
                change.StartLine + lineNumber,
                "작업 표시 주석은 미완료 작업을 나타냅니다.",
                "릴리스 전 모든 TODO/FIXME를 해결하세요."));
        }

        return issues;
    }
}

public class SA0030_MissingErrorHandling : SARuleBase
{
    public override string RuleId => "SA0030";
    public override string RuleName => "에러 처리 누락";
    public override string Description => "예외 상황에 대한 에러 처리가 누락된 코드를 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Safety;

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();
        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var lines = code.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            // 나눗셈 연산 감지
            var divisionMatch = Regex.Match(line, @"(\w+)\s*/\s*(\w+)", RegexOptions.Compiled);
            if (divisionMatch.Success)
            {
                var divisor = divisionMatch.Groups[2].Value;

                // 숫자 리터럴이면 0 검사 불필요
                if (int.TryParse(divisor, out int value) && value != 0)
                    continue;

                // 주변 코드에서 0 검사가 있는지 확인 (앞뒤 5줄)
                bool hasZeroCheck = false;
                int startCheck = Math.Max(0, i - 5);
                int endCheck = Math.Min(lines.Length - 1, i + 5);

                for (int j = startCheck; j <= endCheck; j++)
                {
                    var checkLine = lines[j].ToUpperInvariant();
                    if (checkLine.Contains($"{divisor.ToUpperInvariant()} <> 0") ||
                        checkLine.Contains($"{divisor.ToUpperInvariant()} > 0") ||
                        checkLine.Contains($"0 < {divisor.ToUpperInvariant()}") ||
                        checkLine.Contains($"{divisor.ToUpperInvariant()} = 0"))
                    {
                        hasZeroCheck = true;
                        break;
                    }
                }

                if (!hasZeroCheck)
                {
                    issues.Add(CreateIssue(
                        "0으로 나누기 검사 누락",
                        $"변수 '{divisor}'(으)로 나누기 전 0 검사가 없습니다.",
                        change.FilePath,
                        change.StartLine + i,
                        @"
0으로 나누기는 심각한 런타임 오류를 발생시킵니다:
- Exception 발생
- PLC 프로그램 중단
- 시스템 불안정
",
                        @"
✅ 권장 해결 방법:
IF divisor <> 0 THEN
    result := numerator / divisor;
ELSE
    result := 0;  // 또는 에러 처리
END_IF
",
                        line,
                        $"IF {divisor} <> 0 THEN\n    {line}\nEND_IF"));
                }
            }

            // 배열 인덱스 범위 검사 누락
            var arrayAccessMatch = Regex.Match(line, @"(\w+)\[(\w+)\]", RegexOptions.Compiled);
            if (arrayAccessMatch.Success)
            {
                var arrayVar = arrayAccessMatch.Groups[1].Value;
                var indexVar = arrayAccessMatch.Groups[2].Value;

                // 인덱스가 숫자 리터럴이 아니면 범위 검사 권장
                if (!int.TryParse(indexVar, out _))
                {
                    issues.Add(CreateIssue(
                        "배열 인덱스 범위 검사 권장",
                        $"배열 '{arrayVar}'의 인덱스 '{indexVar}' 범위 검사를 권장합니다.",
                        change.FilePath,
                        change.StartLine + i,
                        @"
배열 인덱스 범위를 벗어나면:
- 예상치 못한 메모리 접근
- 데이터 손상
- 시스템 오류
",
                        $"IF {indexVar} >= 0 AND {indexVar} <= UPPER_BOUND({arrayVar}, 1) THEN"));
                }
            }
        }

        return issues;
    }
}
