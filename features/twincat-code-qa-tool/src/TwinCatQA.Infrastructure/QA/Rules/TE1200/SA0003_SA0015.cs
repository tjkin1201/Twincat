using System.Text.RegularExpressions;
using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Models.QA;

namespace TwinCatQA.Infrastructure.QA.Rules.TE1200;

/// <summary>
/// SA0003: 빈 문장 감지
/// 단독 세미콜론, 빈 IF/CASE 블록 등
/// </summary>
public class SA0003_EmptyStatements : SARuleBase
{
    public override string RuleId => "SA0003";
    public override string RuleName => "빈 문장";
    public override string Description => "빈 문장(단독 세미콜론)이나 빈 블록을 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.UnreachableUnusedCode;

    private static readonly Regex EmptyStatementPattern = new(@"^\s*;\s*$", RegexOptions.Multiline | RegexOptions.Compiled);
    private static readonly Regex EmptyIfPattern = new(@"IF\s+.+?\s+THEN\s*(?://[^\n]*)?\s*END_IF", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex EmptyCasePattern = new(@"CASE\s+.+?\s+OF\s*(?://[^\n]*)?\s*END_CASE", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex EmptyWhilePattern = new(@"WHILE\s+.+?\s+DO\s*(?://[^\n]*)?\s*END_WHILE", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex EmptyForPattern = new(@"FOR\s+.+?\s+DO\s*(?://[^\n]*)?\s*END_FOR", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();
        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;

        // 빈 문장 검사
        foreach (Match match in EmptyStatementPattern.Matches(code))
        {
            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
            issues.Add(CreateIssue("빈 문장", "단독 세미콜론(;)이 발견되었습니다.", change.FilePath, change.StartLine + lineNumber,
                "빈 문장은 의도하지 않은 것이거나 코드 삭제 후 남은 흔적일 수 있습니다.", "빈 문장을 삭제하거나 의도적이면 주석으로 명시하세요."));
        }

        // 빈 IF 블록
        foreach (Match match in EmptyIfPattern.Matches(code))
        {
            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
            issues.Add(CreateIssue("빈 IF 블록", "IF 블록 내부가 비어 있습니다.", change.FilePath, change.StartLine + lineNumber,
                "빈 IF 블록은 불필요하거나 미완성 코드입니다. 조건만 있고 실행 내용이 없습니다.", "블록을 삭제하거나 구현을 추가하세요."));
        }

        // 빈 CASE 블록
        foreach (Match match in EmptyCasePattern.Matches(code))
        {
            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
            issues.Add(CreateIssue("빈 CASE 블록", "CASE 블록 내부가 비어 있습니다.", change.FilePath, change.StartLine + lineNumber,
                "빈 CASE 블록은 불필요하거나 미완성 코드입니다.", "블록을 삭제하거나 CASE 항목들을 추가하세요."));
        }

        // 빈 WHILE 블록
        foreach (Match match in EmptyWhilePattern.Matches(code))
        {
            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
            issues.Add(CreateIssue("빈 WHILE 블록", "WHILE 블록 내부가 비어 있습니다.", change.FilePath, change.StartLine + lineNumber,
                "빈 WHILE 블록은 무한 루프를 유발하거나 불필요한 코드입니다.", "블록을 삭제하거나 루프 본문을 추가하세요."));
        }

        // 빈 FOR 블록
        foreach (Match match in EmptyForPattern.Matches(code))
        {
            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
            issues.Add(CreateIssue("빈 FOR 블록", "FOR 블록 내부가 비어 있습니다.", change.FilePath, change.StartLine + lineNumber,
                "빈 FOR 블록은 단순히 카운터만 증가시키는 의미 없는 루프입니다.", "블록을 삭제하거나 루프 본문을 추가하세요."));
        }

        return issues;
    }
}

/// <summary>
/// SA0004: 출력에 대한 다중 쓰기 접근
/// 동일 출력 변수에 여러 번 쓰기
/// </summary>
public class SA0004_MultipleWriteOnOutput : SARuleBase
{
    public override string RuleId => "SA0004";
    public override string RuleName => "출력에 대한 다중 쓰기";
    public override string Description => "동일 출력 변수에 여러 번 쓰기 접근을 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Concurrency;

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();
        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var lines = code.Split('\n');

        // 변수 할당 추적
        var assignments = new Dictionary<string, List<(int lineNum, string fullLine)>>();
        var assignmentPattern = new Regex(@"^\s*(\w+)(?:\[.*?\])?\s*:=", RegexOptions.Compiled);

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            // 주석이나 빈 줄 건너뛰기
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//") || line.StartsWith("(*"))
                continue;

            var match = assignmentPattern.Match(line);
            if (match.Success)
            {
                var varName = match.Groups[1].Value;

                // 시스템 변수나 FB 호출은 제외
                if (varName.StartsWith("fb", StringComparison.OrdinalIgnoreCase) ||
                    varName.Equals("THIS", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!assignments.ContainsKey(varName))
                    assignments[varName] = new List<(int, string)>();

                assignments[varName].Add((i + 1, line));
            }
        }

        // 동일 변수에 3회 이상 할당된 경우 (2회는 일반적으로 허용)
        foreach (var kvp in assignments.Where(x => x.Value.Count >= 3))
        {
            var lines_str = string.Join(", ", kvp.Value.Select(x => x.lineNum));
            issues.Add(CreateIssue(
                "변수에 대한 다중 쓰기",
                $"변수 '{kvp.Key}'에 {kvp.Value.Count}회 할당이 발견되었습니다. (라인: {lines_str})",
                change.FilePath,
                change.StartLine + kvp.Value.First().lineNum - 1,
                @"
동일 변수에 여러 번 쓰기는 다음 문제를 야기합니다:
- 의도하지 않은 값 덮어쓰기
- 코드 흐름 파악 어려움
- 중간 값이 사용되지 않는 경우 불필요한 연산
",
                @"
✅ 권장 해결 방법:
1. 각 할당이 필요한지 검토
2. 중간 계산용 임시 변수 사용
3. 조건문으로 분기하여 한 번만 할당
"));
        }

        return issues;
    }
}

/// <summary>
/// SA0006: 여러 태스크에서 쓰기 접근
/// 멀티태스크 환경에서 동시 쓰기 충돌 감지
/// </summary>
public class SA0006_MultiTaskWriteAccess : SARuleBase
{
    public override string RuleId => "SA0006";
    public override string RuleName => "여러 태스크에서 쓰기 접근";
    public override string Description => "여러 태스크에서 동일 변수에 쓰기 접근하는 것을 감지합니다.";
    public override Severity Severity => Severity.Critical;
    public override SARuleCategory Category => SARuleCategory.Concurrency;

    public override List<QAIssue> CheckVariableChange(VariableChange change)
    {
        var issues = new List<QAIssue>();

        // 전역 변수이고 VAR_GLOBAL인 경우 경고
        if (change.ChangeType == ChangeType.Added &&
            (change.Scope?.Contains("GLOBAL") == true || change.FilePath?.Contains("GVL") == true))
        {
            issues.Add(CreateIssue(
                "전역 변수 추가 - 멀티태스크 주의",
                $"전역 변수 '{change.VariableName}'이(가) 추가되었습니다. 여러 태스크에서 접근 시 동기화 필요.",
                change.FilePath,
                change.Line,
                @"
여러 태스크에서 동일 전역 변수에 쓰기 접근 시:
- 레이스 컨디션 발생 가능
- 데이터 불일치
- 예측 불가능한 동작
",
                @"
✅ 권장 해결 방법:
1. 세마포어/뮤텍스로 동기화
2. 태스크별 로컬 복사본 사용
3. 단일 태스크에서만 쓰기, 나머지는 읽기만
"));
        }

        return issues;
    }
}

/// <summary>
/// SA0007: 상수에 대한 주소 연산자
/// 상수에 ADR 연산자 사용 감지
/// </summary>
public class SA0007_AddressOfConstant : SARuleBase
{
    public override string RuleId => "SA0007";
    public override string RuleName => "상수에 대한 주소 연산자";
    public override string Description => "상수에 ADR 연산자를 사용하는 것을 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Operations;

    // 상수 패턴: 대문자와 언더스코어, 또는 숫자 리터럴
    private static readonly Regex AdrOnConstantPattern = new(@"ADR\s*\(\s*(?:[A-Z_][A-Z0-9_]*|[0-9]+)\s*\)", RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();
        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var matches = AdrOnConstantPattern.Matches(change.NewCode);
        foreach (Match match in matches)
        {
            var lineNumber = change.NewCode.Take(match.Index).Count(c => c == '\n') + 1;

            // ADR() 안의 식별자 추출
            var adrContent = Regex.Match(match.Value, @"ADR\s*\(\s*([^)]+)\s*\)");
            var target = adrContent.Success ? adrContent.Groups[1].Value.Trim() : "Unknown";

            issues.Add(CreateIssue(
                "상수에 ADR 연산자 사용",
                $"상수나 리터럴 '{target}'에 ADR 연산자가 사용되었습니다.",
                change.FilePath,
                change.StartLine + lineNumber,
                @"
상수나 리터럴의 주소를 얻는 것은 문제를 일으킵니다:
- 컴파일 오류 또는 런타임 오류 발생
- 상수는 메모리 주소가 고정되지 않을 수 있음
- 리터럴은 주소를 가질 수 없음
",
                @"
✅ 권장 해결 방법:
1. 상수 대신 VAR 변수 사용
2. VAR_STAT (정적 변수) 사용
3. 설계 재검토: 포인터가 정말 필요한지 확인
",
                match.Value,
                $"VAR temp{target} : <type> := {target}; END_VAR\n// ADR(temp{target})"));
        }

        return issues;
    }
}

/// <summary>
/// SA0008: 서브레인지 타입 검사
/// SUBRANGE 타입의 범위 위반 가능성 감지
/// </summary>
public class SA0008_SubrangeTypeCheck : SARuleBase
{
    public override string RuleId => "SA0008";
    public override string RuleName => "서브레인지 타입 검사";
    public override string Description => "SUBRANGE 타입 변수의 범위 위반 가능성을 검사합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Conversions;

    public override List<QAIssue> CheckVariableChange(VariableChange change)
    {
        var issues = new List<QAIssue>();

        // SUBRANGE 타입 감지 (예: INT(0..100))
        if (change.NewDataType?.Contains("..") == true)
        {
            issues.Add(CreateIssue(
                "서브레인지 타입 사용",
                $"변수 '{change.VariableName}'이(가) 서브레인지 타입 '{change.NewDataType}'을(를) 사용합니다.",
                change.FilePath,
                change.Line,
                "서브레인지 타입은 런타임에 범위 검사를 수행하며, 범위 위반 시 예외가 발생합니다.",
                "할당 전 범위 검사를 수행하거나 적절한 에러 처리를 구현하세요."));
        }

        return issues;
    }
}

/// <summary>
/// SA0009: 사용하지 않는 반환값
/// 함수 호출 결과를 무시하는 경우 감지
/// </summary>
public class SA0009_UnusedReturnValue : SARuleBase
{
    public override string RuleId => "SA0009";
    public override string RuleName => "사용하지 않는 반환값";
    public override string Description => "함수 호출의 반환값을 사용하지 않는 경우를 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.UnreachableUnusedCode;

    // 함수 호출 패턴 (할당 없이 호출)
    private static readonly Regex StandaloneFunctionCallPattern = new(
        @"^\s*(\w+)\s*\(\s*[^)]*\s*\)\s*;",
        RegexOptions.Multiline | RegexOptions.Compiled);

    // 할당과 함께 호출되는 패턴 (정상)
    private static readonly Regex AssignedFunctionCallPattern = new(
        @"\w+\s*:=\s*\w+\s*\(",
        RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();
        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var lines = change.NewCode.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            // 할당 없는 함수 호출 감지
            if (StandaloneFunctionCallPattern.IsMatch(line) && !AssignedFunctionCallPattern.IsMatch(line))
            {
                var match = StandaloneFunctionCallPattern.Match(line);
                var funcName = match.Groups[1].Value;

                // FB 호출이나 프로시저는 제외
                if (!funcName.StartsWith("fb", StringComparison.OrdinalIgnoreCase) &&
                    !funcName.Equals("RETURN", StringComparison.OrdinalIgnoreCase))
                {
                    issues.Add(CreateIssue(
                        "반환값 무시",
                        $"함수 '{funcName}'의 반환값이 사용되지 않습니다.",
                        change.FilePath,
                        change.StartLine + i,
                        @"
함수의 반환값을 무시하면:
- 에러 상태를 놓칠 수 있음
- 중요한 결과값 손실
- 의도하지 않은 동작 가능
",
                        @"
✅ 권장 해결 방법:
1. 반환값을 변수에 저장: result := Function();
2. 반환값이 필요 없으면 주석으로 명시
3. 에러 코드 반환 시 반드시 검사
"));
                }
            }
        }

        return issues;
    }
}

/// <summary>
/// SA0010: 단일 요소 배열
/// 요소가 하나뿐인 배열 감지
/// </summary>
public class SA0010_SingleElementArray : SARuleBase
{
    public override string RuleId => "SA0010";
    public override string RuleName => "단일 요소 배열";
    public override string Description => "요소가 하나뿐인 배열을 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.Declarations;

    private static readonly Regex SingleElementArrayPattern = new(
        @"ARRAY\s*\[\s*0\s*\.\.\s*0\s*\]|ARRAY\s*\[\s*1\s*\.\.\s*1\s*\]",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckVariableChange(VariableChange change)
    {
        var issues = new List<QAIssue>();

        if (change.NewDataType != null && SingleElementArrayPattern.IsMatch(change.NewDataType))
        {
            issues.Add(CreateIssue(
                "단일 요소 배열",
                $"변수 '{change.VariableName}'이(가) 단일 요소 배열입니다.",
                change.FilePath,
                change.Line,
                "단일 요소 배열은 일반 변수로 대체할 수 있습니다.",
                "배열 대신 일반 변수 사용을 고려하세요."));
        }

        return issues;
    }
}

/// <summary>
/// SA0011: 단일 요소 열거형
/// 멤버가 하나뿐인 열거형 감지
/// </summary>
public class SA0011_SingleMemberEnum : SARuleBase
{
    public override string RuleId => "SA0011";
    public override string RuleName => "단일 요소 열거형";
    public override string Description => "멤버가 하나뿐인 열거형을 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.Declarations;
    public override bool SupportsPrecompile => true;

    public override List<QAIssue> CheckDataTypeChange(DataTypeChange change)
    {
        var issues = new List<QAIssue>();

        // 열거형이고 멤버가 1개인 경우
        if (change.NewDefinition?.Contains("(") == true &&
            change.NewDefinition?.Split(',').Length == 1)
        {
            issues.Add(CreateIssue(
                "단일 멤버 열거형",
                $"열거형 '{change.TypeName}'이(가) 멤버가 하나뿐입니다.",
                change.FilePath,
                change.Line,
                "단일 멤버 열거형은 의미가 제한적입니다.",
                "상수 또는 BOOL 타입 사용을 고려하세요."));
        }

        return issues;
    }
}

/// <summary>
/// SA0012: 상수로 선언 가능한 변수
/// 값이 변경되지 않는 변수 감지
/// </summary>
public class SA0012_VariableCouldBeConstant : SARuleBase
{
    public override string RuleId => "SA0012";
    public override string RuleName => "상수로 선언 가능한 변수";
    public override string Description => "값이 변경되지 않아 상수로 선언할 수 있는 변수를 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.VariablesAndConstants;

    public override List<QAIssue> CheckVariableChange(VariableChange change)
    {
        var issues = new List<QAIssue>();

        // 초기값이 있고 이름이 상수 패턴인 경우 힌트
        if (change.ChangeType == ChangeType.Added &&
            !string.IsNullOrEmpty(change.NewInitialValue) &&
            IsConstantLikeName(change.VariableName))
        {
            issues.Add(CreateIssue(
                "상수로 선언 가능",
                $"변수 '{change.VariableName}'은(는) CONSTANT로 선언할 수 있습니다.",
                change.FilePath,
                change.Line,
                "변경되지 않는 값은 상수로 선언하면 컴파일러 최적화와 안전성이 향상됩니다.",
                "VAR CONSTANT로 선언을 변경하세요."));
        }

        return issues;
    }

    private bool IsConstantLikeName(string name)
    {
        // 대문자와 언더스코어로만 구성된 이름
        return name.ToUpperInvariant() == name && name.Contains("_");
    }
}

/// <summary>
/// SA0013: 동일한 변수 이름 선언
/// 다른 스코프에서 같은 이름의 변수 선언 감지
/// </summary>
public class SA0013_SameVariableName : SARuleBase
{
    public override string RuleId => "SA0013";
    public override string RuleName => "동일한 변수 이름 선언";
    public override string Description => "다른 스코프에서 동일한 이름의 변수 선언을 감지합니다 (섀도잉).";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Declarations;

    public override List<QAIssue> CheckVariableChange(VariableChange change)
    {
        var issues = new List<QAIssue>();

        // 전역 변수와 같은 이름의 로컬 변수 추가 시 경고
        // 실제 구현에서는 심볼 테이블 조회 필요
        if (change.ChangeType == ChangeType.Added &&
            change.Scope?.Contains("LOCAL") == true)
        {
            // 일반적인 전역 변수 이름 패턴과 충돌 검사
            if (change.VariableName.StartsWith("g", StringComparison.OrdinalIgnoreCase) ||
                change.VariableName.StartsWith("G_"))
            {
                issues.Add(CreateIssue(
                    "전역 변수 이름 패턴 사용",
                    $"로컬 변수 '{change.VariableName}'이(가) 전역 변수 명명 패턴을 사용합니다.",
                    change.FilePath,
                    change.Line,
                    "전역 변수와 유사한 이름의 로컬 변수는 혼란을 야기합니다.",
                    "명확한 로컬 변수 이름을 사용하세요."));
            }
        }

        return issues;
    }
}

/// <summary>
/// SA0014: 인스턴스 할당
/// Function Block 인스턴스 간 직접 할당 감지
/// </summary>
public class SA0014_InstanceAssignment : SARuleBase
{
    public override string RuleId => "SA0014";
    public override string RuleName => "인스턴스 할당";
    public override string Description => "Function Block 인스턴스 간 직접 할당을 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.ObjectOriented;

    private static readonly Regex FBAssignmentPattern = new(
        @"fb\w+\s*:=\s*fb\w+\s*;",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();
        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var matches = FBAssignmentPattern.Matches(change.NewCode);
        foreach (Match match in matches)
        {
            var lineNumber = change.NewCode.Take(match.Index).Count(c => c == '\n') + 1;
            issues.Add(CreateIssue(
                "FB 인스턴스 직접 할당",
                $"Function Block 인스턴스 간 직접 할당: '{match.Value}'",
                change.FilePath,
                change.StartLine + lineNumber,
                "FB 인스턴스 간 직접 할당은 내부 상태를 예기치 않게 복사합니다.",
                "필요한 속성만 개별적으로 복사하세요."));
        }

        return issues;
    }
}

/// <summary>
/// SA0015: FB_init을 통한 전역 데이터 접근
/// FB_init에서 전역 변수 접근 감지
/// </summary>
public class SA0015_GlobalAccessInFBInit : SARuleBase
{
    public override string RuleId => "SA0015";
    public override string RuleName => "FB_init에서 전역 데이터 접근";
    public override string Description => "FB_init 메서드에서 전역 변수에 접근하는 것을 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Initialization;

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();
        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        // FB_init 메서드인지 확인
        if (change.ElementName?.Equals("FB_init", StringComparison.OrdinalIgnoreCase) == true ||
            change.NewCode.Contains("METHOD FB_init", StringComparison.OrdinalIgnoreCase))
        {
            // 전역 변수 접근 패턴 (GVL., g 접두사 등)
            if (Regex.IsMatch(change.NewCode, @"GVL\.\w+|g[A-Z]\w+", RegexOptions.IgnoreCase))
            {
                issues.Add(CreateIssue(
                    "FB_init에서 전역 데이터 접근",
                    "FB_init 메서드에서 전역 변수에 접근하고 있습니다.",
                    change.FilePath,
                    change.StartLine,
                    "FB_init 실행 시점에 전역 변수가 초기화되지 않았을 수 있습니다.",
                    "FB_init에서는 파라미터만 사용하고, 전역 데이터는 별도 초기화 메서드에서 접근하세요."));
            }
        }

        return issues;
    }
}
