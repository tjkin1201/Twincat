using System.Text.RegularExpressions;
using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Models.QA;

namespace TwinCatQA.Infrastructure.QA.Rules.TE1200;

#region SA0161-SA0170 고급 분석 규칙

/// <summary>
/// SA0161: 순환 의존성 감지
/// </summary>
public class SA0161_CircularDependency : SARuleBase
{
    public override string RuleId => "SA0161";
    public override string RuleName => "순환 의존성";
    public override string Description => "POU 간의 순환 의존성을 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.ObjectOriented;
    public override bool SupportsPrecompile => false;

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        // 파일명에서 POU 이름 추출
        var fileName = Path.GetFileNameWithoutExtension(change.FilePath);
        var code = change.NewCode;

        // 자기 자신 참조 확인
        if (Regex.IsMatch(code, $@"\b{Regex.Escape(fileName)}\s*\(", RegexOptions.IgnoreCase))
        {
            issues.Add(CreateIssue(
                "잠재적 순환 의존성",
                $"POU '{fileName}'이(가) 자기 자신을 호출할 수 있습니다.",
                change.FilePath,
                change.StartLine,
                "순환 의존성은 스택 오버플로우를 유발할 수 있습니다.",
                "의존성 구조를 검토하고 필요시 인터페이스를 사용하세요."));
        }

        return issues;
    }
}

/// <summary>
/// SA0162: 모듈 크기 초과
/// </summary>
public class SA0162_ModuleSizeExceeded : SARuleBase
{
    public override string RuleId => "SA0162";
    public override string RuleName => "모듈 크기 초과";
    public override string Description => "권장 크기를 초과하는 모듈(POU)을 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Metrics;
    public override bool SupportsPrecompile => true;

    private const int MaxModuleLines = 300;

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var lines = change.NewCode.Split('\n').Length;

        if (lines > MaxModuleLines)
        {
            issues.Add(CreateIssue(
                "모듈 크기 초과",
                $"모듈이 {lines}줄로, 권장 크기({MaxModuleLines}줄)를 초과합니다.",
                change.FilePath,
                change.StartLine,
                "큰 모듈은 이해하기 어렵고 테스트하기 어렵습니다.",
                "기능별로 작은 모듈로 분리하세요."));
        }

        return issues;
    }
}

/// <summary>
/// SA0163: 조건부 컴파일 과다
/// </summary>
public class SA0163_ConditionalCompilation : SARuleBase
{
    public override string RuleId => "SA0163";
    public override string RuleName => "조건부 컴파일";
    public override string Description => "과도한 조건부 컴파일 사용을 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.Declarations;
    public override bool SupportsPrecompile => true;

    private const int MaxConditionalBlocks = 5;

    private static readonly Regex ConditionalPattern = new(
        @"\{IF\s+\w+\}|\{IFDEF\s+\w+\}",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var matches = ConditionalPattern.Matches(code);

        if (matches.Count > MaxConditionalBlocks)
        {
            issues.Add(CreateIssue(
                "과다 조건부 컴파일",
                $"조건부 컴파일 블록이 {matches.Count}개 있습니다.",
                change.FilePath,
                change.StartLine,
                "과도한 조건부 컴파일은 코드 가독성을 저해합니다.",
                "별도 모듈로 분리하거나 설정 패턴을 사용하세요."));
        }

        return issues;
    }
}

/// <summary>
/// SA0164: 상수 중복 정의
/// </summary>
public class SA0164_DuplicateConstants : SARuleBase
{
    public override string RuleId => "SA0164";
    public override string RuleName => "상수 중복 정의";
    public override string Description => "동일한 값을 가진 상수가 여러 곳에 정의된 경우를 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.VariablesAndConstants;
    public override bool SupportsPrecompile => true;

    // VAR_GLOBAL CONSTANT 또는 VAR CONSTANT 섹션 내 상수
    private static readonly Regex ConstantPattern = new(
        @"(\w+)\s*:\s*\w+\s*:=\s*([+-]?\d+(?:\.\d+)?|16#[0-9A-F]+|2#[01]+|T#\w+|'[^']*')\s*;",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;

        // CONSTANT 섹션만 검사
        if (!code.Contains("CONSTANT", StringComparison.OrdinalIgnoreCase))
            return issues;

        var matches = ConstantPattern.Matches(code);

        // 값별로 상수명 그룹화
        var valueToNames = new Dictionary<string, List<(string name, int line)>>();

        foreach (Match match in matches)
        {
            var name = match.Groups[1].Value;
            var value = match.Groups[2].Value.Trim();
            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;

            // 0, 1 같은 기본값은 제외
            if (value == "0" || value == "1" || value == "FALSE" || value == "TRUE")
                continue;

            if (!valueToNames.ContainsKey(value))
                valueToNames[value] = new List<(string, int)>();

            valueToNames[value].Add((name, lineNumber));
        }

        // 중복된 값 보고
        foreach (var kvp in valueToNames.Where(v => v.Value.Count > 1))
        {
            var value = kvp.Key;
            var names = kvp.Value;

            var nameList = string.Join(", ", names.Select(n => n.name));
            var firstLine = names.First().line;

            issues.Add(CreateIssue(
                "중복 상수 값",
                $"값 '{value}'이(가) {names.Count}개의 상수에서 중복 정의됨: {nameList}",
                change.FilePath,
                change.StartLine + firstLine,
                @"
중복 상수 값의 문제점:
- 의미가 다른 상수가 우연히 같은 값을 가질 수 있음
- 값 변경 시 일관성 유지 어려움
- 매직 넘버 중복

합법적 중복: 서로 다른 의미를 가진 상수 (예: MAX_SPEED := 100, MAX_TEMP := 100)
문제적 중복: 같은 의미의 상수 (예: TIMEOUT_MS := 5000, DELAY_MS := 5000)
",
                "같은 의미라면 하나의 상수로 통합하세요. 다른 의미라면 주석으로 명확히 하세요."));
        }

        return issues;
    }
}

/// <summary>
/// SA0165: 불완전한 초기화
/// </summary>
public class SA0165_IncompleteInitialization : SARuleBase
{
    public override string RuleId => "SA0165";
    public override string RuleName => "불완전한 초기화";
    public override string Description => "구조체나 배열의 불완전한 초기화를 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Initialization;
    public override bool SupportsPrecompile => true;

    // 구조체 부분 초기화 패턴: ST_Data := (field1 := 10); // field2가 누락
    private static readonly Regex PartialStructInitPattern = new(
        @"(\w+)\s*:=\s*\([^)]*\)",
        RegexOptions.Compiled);

    // 배열 부분 초기화: ARRAY[0..9] := [1, 2, 3]; // 나머지 0
    private static readonly Regex PartialArrayInitPattern = new(
        @"ARRAY\s*\[\s*(\d+)\s*\.\.\s*(\d+)\s*\].*?:=\s*\[([^\]]+)\]",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;

        // 배열 부분 초기화 검사
        var arrayMatches = PartialArrayInitPattern.Matches(code);
        foreach (Match match in arrayMatches)
        {
            if (int.TryParse(match.Groups[1].Value, out int start) &&
                int.TryParse(match.Groups[2].Value, out int end))
            {
                var values = match.Groups[3].Value.Split(',').Length;
                var expectedSize = end - start + 1;

                if (values < expectedSize)
                {
                    var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
                    issues.Add(CreateIssue(
                        "불완전한 배열 초기화",
                        $"배열 크기 {expectedSize}에 대해 {values}개 값만 초기화되었습니다. 나머지는 0/FALSE로 초기화됩니다.",
                        change.FilePath,
                        change.StartLine + lineNumber,
                        @"
불완전한 배열 초기화:
- 명시되지 않은 요소는 기본값(0, FALSE)으로 초기화
- 의도된 동작인지 불분명
- 버그 가능성

예: arr : ARRAY[0..9] OF INT := [1, 2, 3]; // arr[3]..arr[9]는 0
",
                        "모든 요소를 명시적으로 초기화하거나 주석으로 의도를 명확히 하세요."));
                }
            }
        }

        // 구조체 초기화에서 일부 필드만 초기화하는 경우
        // 이는 STRUCT 정의를 알아야 정확히 검사 가능하므로 간단한 휴리스틱 사용
        var structInitMatches = PartialStructInitPattern.Matches(code);
        foreach (Match match in structInitMatches)
        {
            var initContent = match.Value;
            // := ( ) 형태의 구조체 초기화 중 필드가 1-2개만 있는 경우
            var fieldCount = initContent.Split(new[] { ":=" }, StringSplitOptions.None).Length - 1;

            if (fieldCount > 0 && fieldCount <= 2)
            {
                var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
                issues.Add(CreateIssue(
                    "구조체 부분 초기화",
                    $"구조체가 {fieldCount}개 필드만 초기화되었습니다.",
                    change.FilePath,
                    change.StartLine + lineNumber,
                    @"
구조체 부분 초기화 주의사항:
- 초기화되지 않은 필드는 기본값 사용
- 모든 필드를 의도적으로 초기화하는 것이 안전
- 특히 안전 관련 구조체는 명시적 초기화 필요

예:
  stData := (nValue := 10); // 다른 필드는?
  stData := (nValue := 10, bFlag := TRUE, ...); // 명시적
",
                    "모든 필드를 명시적으로 초기화하거나 주석으로 의도를 설명하세요."));
            }
        }

        return issues;
    }
}

/// <summary>
/// SA0166: 메모리 정렬 주의
/// </summary>
public class SA0166_MemoryAlignment : SARuleBase
{
    public override string RuleId => "SA0166";
    public override string RuleName => "메모리 정렬";
    public override string Description => "메모리 정렬 문제가 발생할 수 있는 구조체를 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.MemoryLayout;
    public override bool SupportsPrecompile => true;

    // 크기가 다른 타입들의 혼합
    private static readonly Regex MixedSizePattern = new(
        @"BOOL\s*;.*?REAL|REAL\s*;.*?BOOL",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    public override List<QAIssue> CheckDataTypeChange(DataTypeChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewDefinition))
            return issues;

        var code = change.NewDefinition;

        if (code.Contains("STRUCT", StringComparison.OrdinalIgnoreCase) &&
            MixedSizePattern.IsMatch(code))
        {
            issues.Add(CreateIssue(
                "메모리 정렬 주의",
                "구조체에 다양한 크기의 멤버가 혼합되어 있습니다.",
                change.FilePath,
                change.Line,
                "멤버 순서에 따라 패딩이 추가되어 크기가 달라질 수 있습니다.",
                "{attribute 'pack_mode' := '0'} 또는 크기순 정렬을 고려하세요."));
        }

        return issues;
    }
}

/// <summary>
/// SA0167: 복잡한 상속 구조
/// </summary>
public class SA0167_ComplexInheritance : SARuleBase
{
    public override string RuleId => "SA0167";
    public override string RuleName => "복잡한 상속";
    public override string Description => "깊은 상속 계층을 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.ObjectOriented;
    public override bool SupportsPrecompile => true;

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;

        // EXTENDS 키워드 확인
        if (code.Contains("EXTENDS", StringComparison.OrdinalIgnoreCase))
        {
            // SUPER^ 호출 깊이 확인
            var superCalls = Regex.Matches(code, @"SUPER\^", RegexOptions.IgnoreCase).Count;
            if (superCalls > 2)
            {
                issues.Add(CreateIssue(
                    "복잡한 상속 구조",
                    $"SUPER^ 호출이 {superCalls}번 있습니다.",
                    change.FilePath,
                    change.StartLine,
                    "깊은 상속 계층은 코드 이해를 어렵게 합니다.",
                    "상속보다 컴포지션을 고려하세요."));
            }
        }

        return issues;
    }
}

/// <summary>
/// SA0168: 하드코딩된 타이밍
/// </summary>
public class SA0168_HardcodedTiming : SARuleBase
{
    public override string RuleId => "SA0168";
    public override string RuleName => "하드코딩된 타이밍";
    public override string Description => "하드코딩된 시간 값을 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.VariablesAndConstants;
    public override bool SupportsPrecompile => true;

    // T#10s, T#500ms, TIME#1m 등
    private static readonly Regex TimeLiteralPattern = new(
        @"(?:T|TIME)#\d+(?:d|h|m|s|ms|us|ns)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var lines = code.Split('\n');
        var matches = TimeLiteralPattern.Matches(code);

        // 상수 선언이 아닌 곳에서 사용되는 시간 리터럴
        foreach (Match match in matches)
        {
            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
            var line = lines[lineNumber - 1].Trim();

            // 상수 선언인지 확인
            bool isConstantDeclaration = Regex.IsMatch(line, @"^\w+\s*:\s*TIME\s*:=", RegexOptions.IgnoreCase);
            bool isInConstantSection = code.Substring(0, match.Index).Contains("VAR CONSTANT", StringComparison.OrdinalIgnoreCase) ||
                                       code.Substring(0, match.Index).Contains("VAR_GLOBAL CONSTANT", StringComparison.OrdinalIgnoreCase);

            // 상수가 아닌 곳에서 직접 사용
            if (!isConstantDeclaration && !isInConstantSection)
            {
                issues.Add(CreateIssue(
                    "하드코딩된 시간 값",
                    $"시간 리터럴 '{match.Value}'이(가) 코드에 직접 사용되었습니다.",
                    change.FilePath,
                    change.StartLine + lineNumber,
                    @"
하드코딩된 타이밍의 문제점:
- 튜닝/조정 어려움 (여러 곳 수정 필요)
- 테스트 시 변경 불가
- 파라미터화 불가
- 재사용성 저하

나쁜 예:
  tonDelay(IN := bStart, PT := T#5s);
  IF tmrTimer.ET > T#10s THEN ...

좋은 예:
  VAR CONSTANT
    STARTUP_DELAY : TIME := T#5s;
  END_VAR
  tonDelay(IN := bStart, PT := STARTUP_DELAY);
",
                    "상수로 정의하거나 VAR_INPUT 파라미터로 만드세요."));
            }
        }

        return issues;
    }
}

/// <summary>
/// SA0169: 미완성 구현 표시
/// </summary>
public class SA0169_IncompleteImplementation : SARuleBase
{
    public override string RuleId => "SA0169";
    public override string RuleName => "미완성 구현";
    public override string Description => "미완성으로 표시된 코드를 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Comments;
    public override bool SupportsPrecompile => true;

    private static readonly Regex IncompletePattern = new(
        @"(NOT\s*IMPLEMENTED|STUB|PLACEHOLDER|TBD|TO\s*BE\s*DONE)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var matches = IncompletePattern.Matches(code);

        foreach (Match match in matches)
        {
            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
            issues.Add(CreateIssue(
                "미완성 구현",
                $"'{match.Groups[1].Value}' 표시가 발견되었습니다.",
                change.FilePath,
                change.StartLine + lineNumber,
                "미완성 코드는 프로덕션에 배포하기 전에 완성해야 합니다.",
                "구현을 완료하거나 예외를 발생시키세요."));
        }

        return issues;
    }
}

/// <summary>
/// SA0170: 사용하지 않는 USING 문
/// </summary>
public class SA0170_UnusedUsing : SARuleBase
{
    public override string RuleId => "SA0170";
    public override string RuleName => "미사용 USING";
    public override string Description => "사용되지 않는 USING 문을 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.UnreachableUnusedCode;
    public override bool SupportsPrecompile => true;

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

            // USING 이후 코드에서 네임스페이스 사용 확인
            var afterUsing = code.Substring(match.Index + match.Length);
            if (!afterUsing.Contains($"{namespaceName}.", StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(CreateIssue(
                    "미사용 USING",
                    $"USING '{namespaceName}'이(가) 사용되지 않습니다.",
                    change.FilePath,
                    change.StartLine + lineNumber,
                    "미사용 USING 문은 코드를 정리할 때 제거하세요.",
                    "USING 문을 제거하세요."));
            }
        }

        return issues;
    }
}

#endregion

#region SA0171-SA0180 안전 및 성능 규칙

/// <summary>
/// SA0171: 안전 관련 변수 보호
/// </summary>
public class SA0171_SafetyVariableProtection : SARuleBase
{
    public override string RuleId => "SA0171";
    public override string RuleName => "안전 변수 보호";
    public override string Description => "안전 관련 변수가 적절히 보호되는지 확인합니다.";
    public override Severity Severity => Severity.Critical;
    public override SARuleCategory Category => SARuleCategory.Safety;
    public override bool SupportsPrecompile => true;

    private static readonly Regex SafetyVarPattern = new(
        @"(Emergency|Safety|EStop|Alarm|Fault|Interlock)\w*\s*:\s*BOOL",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckVariableChange(VariableChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed)
            return issues;

        var fullDecl = $"{change.VariableName}: {change.NewDataType}";

        if (SafetyVarPattern.IsMatch(fullDecl))
        {
            issues.Add(CreateIssue(
                "안전 변수",
                $"'{change.VariableName}'은 안전 관련 변수로 보입니다.",
                change.FilePath,
                change.Line,
                @"
안전 변수는 특별한 취급이 필요합니다:
- 무단 수정 방지
- 이중화 고려
- 테스트 충분성
- 문서화
",
                "안전 관련 표준(IEC 62443 등)을 확인하세요."));
        }

        return issues;
    }
}

/// <summary>
/// SA0172: 위험한 연산 순서
/// </summary>
public class SA0172_DangerousOperationOrder : SARuleBase
{
    public override string RuleId => "SA0172";
    public override string RuleName => "위험한 연산 순서";
    public override string Description => "안전하지 않은 연산 순서를 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Safety;
    public override bool SupportsPrecompile => true;

    // 나누기 전 0 체크 없음
    private static readonly Regex DivisionWithoutCheckPattern = new(
        @"(\w+)\s*/\s*(\w+)(?!.*IF\s+\2\s*<>\s*0)",
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
            if (line.Contains("/") && !line.Contains("//"))
            {
                var match = Regex.Match(line, @"(\w+)\s*/\s*(\w+)");
                if (match.Success && !int.TryParse(match.Groups[2].Value, out _))
                {
                    var divisor = match.Groups[2].Value;
                    // 이전 5줄에서 0 체크 확인
                    bool hasCheck = false;
                    for (int j = Math.Max(0, i - 5); j < i; j++)
                    {
                        if (Regex.IsMatch(lines[j], $@"\b{Regex.Escape(divisor)}\s*<>\s*0"))
                        {
                            hasCheck = true;
                            break;
                        }
                    }

                    if (!hasCheck)
                    {
                        issues.Add(CreateIssue(
                            "0 나누기 체크 누락",
                            $"'{divisor}'로 나누기 전 0 체크가 없습니다.",
                            change.FilePath,
                            change.StartLine + i,
                            "0으로 나누면 런타임 오류가 발생합니다.",
                            $"IF {divisor} <> 0 THEN ... END_IF로 보호하세요."));
                    }
                }
            }
        }

        return issues;
    }
}

/// <summary>
/// SA0173: 무한 재시도 패턴
/// </summary>
public class SA0173_InfiniteRetry : SARuleBase
{
    public override string RuleId => "SA0173";
    public override string RuleName => "무한 재시도";
    public override string Description => "제한 없는 재시도 패턴을 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Operations;
    public override bool SupportsPrecompile => true;

    private static readonly Regex RetryPattern = new(
        @"(retry|repeat|again)\s*:=\s*TRUE",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;

        if (RetryPattern.IsMatch(code))
        {
            // 재시도 횟수 카운터 확인
            if (!Regex.IsMatch(code, @"(RetryCount|nRetry|iRetry)\s*[<>]=?\s*\d+", RegexOptions.IgnoreCase))
            {
                issues.Add(CreateIssue(
                    "재시도 제한 없음",
                    "재시도 로직에 횟수 제한이 없어 보입니다.",
                    change.FilePath,
                    change.StartLine,
                    "무한 재시도는 시스템을 불안정하게 만들 수 있습니다.",
                    "최대 재시도 횟수를 설정하고 초과 시 에러 처리하세요."));
            }
        }

        return issues;
    }
}

/// <summary>
/// SA0174: 비용이 높은 연산
/// </summary>
public class SA0174_ExpensiveOperation : SARuleBase
{
    public override string RuleId => "SA0174";
    public override string RuleName => "비용 높은 연산";
    public override string Description => "계산 비용이 높은 연산을 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.Operations;
    public override bool SupportsPrecompile => true;

    // 비용이 높은 함수들
    private static readonly string[] ExpensiveFunctions =
    {
        "SIN", "COS", "TAN", "ASIN", "ACOS", "ATAN", "ATAN2",
        "SQRT", "EXP", "LN", "LOG", "EXPT", "POW",
        "CONCAT", "INSERT", "DELETE", "REPLACE", "FIND" // 문자열 함수
    };

    // 루프 패턴
    private static readonly Regex LoopPattern = new(
        @"(FOR|WHILE|REPEAT)[\s\S]*?END_\1",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;

        // 루프 내에서 비용 높은 연산 확인
        var loopMatches = LoopPattern.Matches(code);

        foreach (Match loopMatch in loopMatches)
        {
            var loopBody = loopMatch.Value;
            var loopType = loopMatch.Groups[1].Value;
            var loopStartLine = code.Take(loopMatch.Index).Count(c => c == '\n') + 1;

            // 루프 내에서 각 비용 높은 함수 검사
            foreach (var func in ExpensiveFunctions)
            {
                var funcPattern = new Regex($@"\b{func}\s*\(", RegexOptions.IgnoreCase);
                if (funcPattern.IsMatch(loopBody))
                {
                    var funcCategory = func switch
                    {
                        "CONCAT" or "INSERT" or "DELETE" or "REPLACE" or "FIND" => "문자열 연산",
                        "SIN" or "COS" or "TAN" or "ASIN" or "ACOS" or "ATAN" or "ATAN2" => "삼각함수",
                        "SQRT" or "EXP" or "LN" or "LOG" or "EXPT" or "POW" => "수학 함수",
                        _ => "함수"
                    };

                    issues.Add(CreateIssue(
                        "루프 내 비용 높은 연산",
                        $"{loopType} 루프 내에서 {funcCategory} '{func}'이(가) 호출됩니다.",
                        change.FilePath,
                        change.StartLine + loopStartLine,
                        @"
비용 높은 연산의 문제점:
- PLC 사이클 타임 증가
- 실시간 성능 저하
- Watchdog 타임아웃 위험

최적화 방법:
1. 루프 외부에서 미리 계산
2. 룩업 테이블 사용 (삼각함수)
3. 문자열 연산은 StringBuilder 패턴 고려
4. 결과 캐싱

예:
  나쁨: FOR i := 1 TO 100 DO rSin := SIN(rAngle); END_FOR
  좋음: rSin := SIN(rAngle); FOR i := 1 TO 100 DO ... END_FOR
",
                        "가능하면 루프 외부로 이동하거나 최적화하세요."));
                    break; // 루프당 한 번만 보고
                }
            }
        }

        return issues;
    }
}

/// <summary>
/// SA0175: 캐시 비효율적 접근
/// </summary>
public class SA0175_CacheInefficientAccess : SARuleBase
{
    public override string RuleId => "SA0175";
    public override string RuleName => "캐시 비효율 접근";
    public override string Description => "캐시 비효율적인 메모리 접근 패턴을 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.Operations;
    public override bool SupportsPrecompile => true;

    // 2차원 배열의 열 우선 접근
    private static readonly Regex ColumnFirstAccessPattern = new(
        @"FOR\s+(\w+)\s*:=.*?TO.*?DO.*?FOR\s+(\w+)\s*:=.*?TO.*?DO.*?\[\s*\2\s*,\s*\1\s*\]",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;

        if (ColumnFirstAccessPattern.IsMatch(code))
        {
            issues.Add(CreateIssue(
                "캐시 비효율 접근",
                "2차원 배열이 열 우선으로 접근되고 있습니다.",
                change.FilePath,
                change.StartLine,
                "열 우선 접근은 캐시 효율이 낮습니다.",
                "행 우선 접근(array[i, j])으로 변경하세요."));
        }

        return issues;
    }
}

/// <summary>
/// SA0176: 문자열 연산 최적화
/// </summary>
public class SA0176_StringOperationOptimization : SARuleBase
{
    public override string RuleId => "SA0176";
    public override string RuleName => "문자열 연산 최적화";
    public override string Description => "최적화 가능한 문자열 연산을 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.Operations;
    public override bool SupportsPrecompile => true;

    // 연속된 CONCAT
    private static readonly Regex ChainedConcatPattern = new(
        @"CONCAT\s*\(\s*CONCAT\s*\(",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;

        if (ChainedConcatPattern.IsMatch(code))
        {
            issues.Add(CreateIssue(
                "연속 CONCAT",
                "중첩된 CONCAT 호출이 있습니다.",
                change.FilePath,
                change.StartLine,
                "중첩 CONCAT은 비효율적입니다.",
                "단일 CONCAT 또는 FORMAT 함수를 사용하세요."));
        }

        return issues;
    }
}

/// <summary>
/// SA0177: 비트 연산 최적화
/// </summary>
public class SA0177_BitOperationOptimization : SARuleBase
{
    public override string RuleId => "SA0177";
    public override string RuleName => "비트 연산 최적화";
    public override string Description => "비효율적인 비트 연산을 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.Operations;
    public override bool SupportsPrecompile => true;

    // MOD 2로 홀짝 판단
    private static readonly Regex ModTwoPattern = new(
        @"\w+\s+MOD\s+2\s*[=<>]",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // *2, /2 패턴
    private static readonly Regex MultiplyDivideTwoPattern = new(
        @"\w+\s*[*/]\s*2(?!\d)",
        RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;

        if (ModTwoPattern.IsMatch(code))
        {
            issues.Add(CreateIssue(
                "MOD 2 최적화 가능",
                "MOD 2는 AND 1로 대체할 수 있습니다.",
                change.FilePath,
                change.StartLine,
                "비트 연산이 산술 연산보다 빠릅니다.",
                "value MOD 2 대신 value AND 1을 사용하세요."));
        }

        // 정수 *2, /2 는 시프트로 대체 가능하지만 가독성 문제로 Info만
        if (MultiplyDivideTwoPattern.IsMatch(code))
        {
            issues.Add(CreateIssue(
                "2의 배수 연산",
                "*2 또는 /2 연산이 있습니다.",
                change.FilePath,
                change.StartLine,
                "SHL/SHR로 대체 가능하지만 가독성을 고려하세요.",
                "성능이 중요하면 SHL(n,1), SHR(n,1)을 고려하세요."));
        }

        return issues;
    }
}

/// <summary>
/// SA0178: 리소스 누수 가능성
/// </summary>
public class SA0178_ResourceLeak : SARuleBase
{
    public override string RuleId => "SA0178";
    public override string RuleName => "리소스 누수";
    public override string Description => "리소스 누수 가능성이 있는 코드를 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Operations;
    public override bool SupportsPrecompile => true;

    // 리소스 할당/해제 함수 쌍
    private static readonly Dictionary<string, string> ResourcePairs = new()
    {
        { "FB_FileOpen", "FB_FileClose" },
        { "FB_FileRead", "FB_FileClose" },
        { "FB_FileWrite", "FB_FileClose" },
        { "FB_SocketConnect", "FB_SocketClose" },
        { "FB_ClientServerConnection", "FB_ConnectionClose" },
        { "__NEW", "__DELETE" },
        { "OPEN", "CLOSE" }
    };

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;

        // 각 리소스 할당 함수에 대해 해제 함수가 있는지 확인
        foreach (var pair in ResourcePairs)
        {
            var allocFunc = pair.Key;
            var releaseFunc = pair.Value;

            // 할당 함수 사용 횟수
            var allocPattern = new Regex($@"\b{Regex.Escape(allocFunc)}\b", RegexOptions.IgnoreCase);
            var allocMatches = allocPattern.Matches(code);

            if (allocMatches.Count > 0)
            {
                // 해제 함수 사용 횟수
                var releasePattern = new Regex($@"\b{Regex.Escape(releaseFunc)}\b", RegexOptions.IgnoreCase);
                var releaseMatches = releasePattern.Matches(code);

                // 해제 함수가 없거나 횟수가 다르면 경고
                if (releaseMatches.Count == 0)
                {
                    var lineNumber = code.Take(allocMatches[0].Index).Count(c => c == '\n') + 1;
                    issues.Add(CreateIssue(
                        "리소스 해제 누락",
                        $"'{allocFunc}' 사용 후 '{releaseFunc}' 호출이 없습니다.",
                        change.FilePath,
                        change.StartLine + lineNumber,
                        @"
리소스 누수의 위험성:
- 파일 핸들 고갈
- 메모리 누수
- 네트워크 소켓 고갈
- 시스템 불안정

예:
  fbFileOpen(bExecute := TRUE, ...);
  // ... 파일 읽기/쓰기 ...
  // fbFileClose(bExecute := TRUE, ...); // 누락!
",
                        $"모든 경로에서 {releaseFunc}를 호출하세요. 특히 에러 처리에서도 해제가 필요합니다."));
                }
                else if (releaseMatches.Count < allocMatches.Count)
                {
                    issues.Add(CreateIssue(
                        "불균형한 리소스 관리",
                        $"'{allocFunc}' {allocMatches.Count}회, '{releaseFunc}' {releaseMatches.Count}회 호출됨",
                        change.FilePath,
                        change.StartLine,
                        "할당과 해제 횟수가 다르면 누수 또는 이중 해제 가능성이 있습니다.",
                        "모든 할당에 대응하는 해제가 있는지 확인하세요."));
                }
            }
        }

        return issues;
    }
}

/// <summary>
/// SA0179: 상태 머신 완전성
/// </summary>
public class SA0179_StateMachineCompleteness : SARuleBase
{
    public override string RuleId => "SA0179";
    public override string RuleName => "상태 머신 완전성";
    public override string Description => "상태 머신의 완전성을 확인합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Operations;
    public override bool SupportsPrecompile => true;

    private static readonly Regex StateMachinePattern = new(
        @"CASE\s+(e?State|n?State|iState)\s+OF",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var match = StateMachinePattern.Match(code);

        if (match.Success)
        {
            // ELSE 케이스 확인
            var caseBlock = code.Substring(match.Index);
            var endCase = caseBlock.IndexOf("END_CASE", StringComparison.OrdinalIgnoreCase);
            if (endCase > 0)
            {
                caseBlock = caseBlock.Substring(0, endCase);

                if (!caseBlock.Contains("ELSE", StringComparison.OrdinalIgnoreCase))
                {
                    var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
                    issues.Add(CreateIssue(
                        "상태 머신 ELSE 누락",
                        "상태 머신에 기본 ELSE 케이스가 없습니다.",
                        change.FilePath,
                        change.StartLine + lineNumber,
                        "예상치 못한 상태에 대한 처리가 필요합니다.",
                        "ELSE 케이스를 추가하여 잘못된 상태를 처리하세요."));
                }
            }
        }

        return issues;
    }
}

/// <summary>
/// SA0180: 코드 문서화 수준
/// </summary>
public class SA0180_DocumentationLevel : SARuleBase
{
    public override string RuleId => "SA0180";
    public override string RuleName => "문서화 수준";
    public override string Description => "코드 문서화 수준을 분석합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.Comments;
    public override bool SupportsPrecompile => true;

    private const double MinDocRatio = 0.10; // 10% (너무 엄격하면 false positive)
    private const int MinCodeLines = 50; // 최소 코드 라인

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var lines = code.Split('\n');

        int codeLines = 0;
        int commentLines = 0;
        bool inBlockComment = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // 빈 줄 제외
            if (string.IsNullOrWhiteSpace(trimmed))
                continue;

            // 블록 주석 상태 추적
            if (trimmed.StartsWith("(*"))
                inBlockComment = true;

            if (inBlockComment)
            {
                commentLines++;
                if (trimmed.EndsWith("*)"))
                    inBlockComment = false;
                continue;
            }

            // 라인 주석
            if (trimmed.StartsWith("//"))
            {
                commentLines++;
                continue;
            }

            // 코드 라인 (주석이 포함되어 있어도 코드로 간주)
            codeLines++;

            // 인라인 주석도 카운트
            if (trimmed.Contains("//") || trimmed.Contains("(*"))
                commentLines++;
        }

        // 최소 라인 수 이상만 검사
        if (codeLines >= MinCodeLines)
        {
            double ratio = codeLines > 0 ? (double)commentLines / (codeLines + commentLines) : 0;

            if (ratio < MinDocRatio)
            {
                issues.Add(CreateIssue(
                    "문서화 부족",
                    $"주석 비율이 {ratio:P1}입니다. (권장: {MinDocRatio:P0} 이상, 코드 {codeLines}줄, 주석 {commentLines}줄)",
                    change.FilePath,
                    change.StartLine,
                    @"
문서화의 중요성:
- 코드 의도 명확화
- 유지보수성 향상
- 인수인계 용이
- 버그 예방

다음을 문서화하세요:
1. POU 헤더
   - 목적: 이 FB/Function이 무엇을 하는가?
   - 입력/출력 설명
   - 사용 예시

2. 복잡한 알고리즘
   - 계산 공식의 의미
   - 참고 문서/표준

3. 비즈니스 로직
   - 왜 이렇게 구현했는가?
   - 특수 케이스 처리

4. 매직 넘버
   - 상수의 의미와 출처

예:
  (*
    FB_MotorControl
    목적: 3상 모터의 속도 및 방향 제어
    입력: rTargetSpeed (목표 속도, rpm)
    출력: bRunning (동작 상태)
  *)
",
                    "함수/FB 헤더와 복잡한 로직에 주석을 추가하세요."));
            }
        }

        return issues;
    }
}

#endregion
