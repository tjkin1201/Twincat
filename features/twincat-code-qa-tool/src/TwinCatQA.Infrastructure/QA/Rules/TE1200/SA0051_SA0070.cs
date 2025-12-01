using System.Text.RegularExpressions;
using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Models.QA;

namespace TwinCatQA.Infrastructure.QA.Rules.TE1200;

#region SA0051 - 함수 길이 초과

/// <summary>
/// SA0051: 함수 길이 초과 감지
/// 권장 라인 수를 초과하는 함수/메서드
/// </summary>
public class SA0051_FunctionTooLong : SARuleBase
{
    public override string RuleId => "SA0051";
    public override string RuleName => "함수 길이 초과";
    public override string Description => "권장 라인 수를 초과하는 함수나 메서드를 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Metrics;
    public override bool SupportsPrecompile => true;

    private const int MaxFunctionLines = 100;

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;

        // FUNCTION, METHOD, PROGRAM, FUNCTION_BLOCK 등의 시작과 끝 찾기
        var blockPatterns = new[]
        {
            (@"(FUNCTION_BLOCK)\s+(\w+)", @"END_FUNCTION_BLOCK"),
            (@"(FUNCTION)\s+(\w+)", @"END_FUNCTION"),
            (@"(METHOD)\s+(?:PUBLIC|PRIVATE|PROTECTED|INTERNAL)?\s*(\w+)", @"END_METHOD"),
            (@"(PROGRAM)\s+(\w+)", @"END_PROGRAM"),
            (@"(ACTION)\s+(\w+)", @"END_ACTION")
        };

        foreach (var (startPattern, endKeyword) in blockPatterns)
        {
            var startRegex = new Regex(startPattern, RegexOptions.IgnoreCase);
            var startMatches = startRegex.Matches(code);

            foreach (Match startMatch in startMatches)
            {
                var blockType = startMatch.Groups[1].Value.ToUpperInvariant();
                var blockName = startMatch.Groups[2].Value;
                var startIndex = startMatch.Index;

                // END_ 키워드 찾기
                var endIndex = code.IndexOf(endKeyword, startIndex, StringComparison.OrdinalIgnoreCase);
                if (endIndex > startIndex)
                {
                    var blockCode = code.Substring(startIndex, endIndex - startIndex + endKeyword.Length);
                    var lines = blockCode.Split('\n');

                    // 빈 줄과 주석만 있는 줄 제외한 실제 코드 줄 수 계산
                    var codeLines = lines.Count(line =>
                    {
                        var trimmed = line.Trim();
                        return !string.IsNullOrWhiteSpace(trimmed) &&
                               !trimmed.StartsWith("//") &&
                               !trimmed.StartsWith("(*");
                    });

                    if (codeLines > MaxFunctionLines)
                    {
                        var lineNumber = code.Take(startIndex).Count(c => c == '\n') + 1;

                        issues.Add(CreateIssue(
                            $"{blockType} 길이 초과",
                            $"{blockType} '{blockName}'의 실제 코드가 {codeLines}줄입니다. (권장: {MaxFunctionLines}줄 이하)",
                            change.FilePath,
                            change.StartLine + lineNumber,
                            @"
긴 함수는 다음 문제를 야기합니다:
- 코드 이해도 저하
- 테스트 어려움
- 유지보수 복잡성 증가
- 버그 발생 가능성 증가
",
                            @"
✅ 권장 해결 방법:
1. 논리적 단위로 서브 함수 분리
2. 반복 코드를 별도 메서드로 추출
3. 복잡한 조건문 단순화
4. 단일 책임 원칙(SRP) 적용
"));
                    }
                }
            }
        }

        return issues;
    }
}

#endregion

#region SA0052 - 파라미터 수 초과

/// <summary>
/// SA0052: 파라미터 수 초과 감지
/// 권장 개수를 초과하는 함수 파라미터
/// </summary>
public class SA0052_TooManyParameters : SARuleBase
{
    public override string RuleId => "SA0052";
    public override string RuleName => "파라미터 수 초과";
    public override string Description => "권장 개수를 초과하는 함수 파라미터를 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Metrics;
    public override bool SupportsPrecompile => true;

    private const int MaxParameters = 7;

    private static readonly Regex VarInputBlockPattern = new(
        @"VAR_INPUT\s*(.*?)END_VAR",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var matches = VarInputBlockPattern.Matches(code);

        foreach (Match match in matches)
        {
            var inputBlock = match.Groups[1].Value;

            // 파라미터 선언 패턴: 변수명 : 타입
            var paramPattern = new Regex(@"^\s*(\w+)\s*:\s*(\w+)", RegexOptions.Multiline);
            var parameters = paramPattern.Matches(inputBlock);
            var parameterCount = parameters.Count;

            if (parameterCount > MaxParameters)
            {
                var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;

                // 파라미터 목록 생성
                var paramList = new List<string>();
                foreach (Match param in parameters)
                {
                    paramList.Add(param.Groups[1].Value);
                }

                issues.Add(CreateIssue(
                    "파라미터 수 초과",
                    $"VAR_INPUT에 {parameterCount}개의 파라미터가 있습니다. (권장: {MaxParameters}개 이하)",
                    change.FilePath,
                    change.StartLine + lineNumber,
                    @"
과다한 파라미터는 다음 문제를 야기합니다:
- 함수 호출 복잡성 증가
- 파라미터 순서 혼동 가능성
- 코드 가독성 저하
- 테스트 어려움
",
                    @"
✅ 권장 해결 방법:
1. 관련 파라미터를 구조체(STRUCT)로 그룹화
2. 함수를 더 작은 단위로 분리
3. 선택적 파라미터는 구성 객체로 통합
",
                    $"파라미터: {string.Join(", ", paramList)}",
                    "TYPE InputParams : STRUCT ... END_STRUCT END_TYPE"));
            }
        }

        return issues;
    }
}

#endregion

#region SA0053 - 중첩 깊이 초과

/// <summary>
/// SA0053: 중첩 깊이 초과 감지
/// 과도하게 중첩된 제어 구조
/// </summary>
public class SA0053_NestingTooDeep : SARuleBase
{
    public override string RuleId => "SA0053";
    public override string RuleName => "중첩 깊이 초과";
    public override string Description => "과도하게 중첩된 제어 구조를 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Metrics;
    public override bool SupportsPrecompile => true;

    private const int MaxNestingDepth = 4;

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var lines = code.Split('\n');
        int currentDepth = 0;
        int maxDepthFound = 0;
        int maxDepthLine = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmedUpper = line.Trim().ToUpperInvariant();

            // 주석 건너뛰기
            if (trimmedUpper.StartsWith("//") || trimmedUpper.StartsWith("(*"))
                continue;

            // 중첩 증가: IF, FOR, WHILE, CASE, REPEAT
            if (Regex.IsMatch(trimmedUpper, @"^\s*(IF|FOR|WHILE|CASE|REPEAT)\b"))
            {
                currentDepth++;
                if (currentDepth > maxDepthFound)
                {
                    maxDepthFound = currentDepth;
                    maxDepthLine = i;
                }
            }

            // 중첩 감소: END_IF, END_FOR, END_WHILE, END_CASE, END_REPEAT
            if (Regex.IsMatch(trimmedUpper, @"^\s*END_(IF|FOR|WHILE|CASE|REPEAT)\b"))
            {
                currentDepth = Math.Max(0, currentDepth - 1);
            }
        }

        if (maxDepthFound > MaxNestingDepth)
        {
            issues.Add(CreateIssue(
                "중첩 깊이 초과",
                $"제어 구조 중첩 깊이가 {maxDepthFound}단계입니다. (권장: {MaxNestingDepth}단계 이하)",
                change.FilePath,
                change.StartLine + maxDepthLine,
                @"
과도한 중첩은 다음 문제를 야기합니다:
- 코드 가독성 저하
- 복잡한 논리 흐름
- 테스트 어려움
- 인지 부하 증가
",
                @"
✅ 권장 해결 방법:
1. Early return 패턴 사용:
   IF NOT validCondition THEN
       RETURN;
   END_IF

2. Guard clause로 조건 분리
3. 중첩 로직을 별도 메서드로 추출
4. 상태 머신 패턴 고려
",
                lines[maxDepthLine].Trim(),
                "// 함수로 추출하거나 조건문 단순화"));
        }

        return issues;
    }
}

#endregion

#region SA0054 - 순환 복잡도 초과

/// <summary>
/// SA0054: 순환 복잡도 초과 감지
/// McCabe 복잡도가 높은 함수
/// </summary>
public class SA0054_CyclomaticComplexity : SARuleBase
{
    public override string RuleId => "SA0054";
    public override string RuleName => "순환 복잡도 초과";
    public override string Description => "순환 복잡도(McCabe Complexity)가 높은 함수를 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Metrics;
    public override bool SupportsPrecompile => true;

    private const int MaxComplexity = 10;

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var lines = code.Split('\n');

        // 기본 복잡도 1로 시작
        int complexity = 1;

        foreach (var line in lines)
        {
            var trimmed = line.Trim().ToUpperInvariant();

            // 주석 제외
            if (trimmed.StartsWith("//") || trimmed.StartsWith("(*"))
                continue;

            // 분기문 카운트: IF, ELSIF, FOR, WHILE, REPEAT, CASE의 각 분기
            if (Regex.IsMatch(trimmed, @"\b(IF|ELSIF|FOR|WHILE|REPEAT)\b"))
                complexity++;

            // CASE 문의 각 분기 (숫자: 또는 상수:)
            var caseMatches = Regex.Matches(trimmed, @"^\s*\d+\s*:|^\s*\w+\s*:");
            complexity += caseMatches.Count;

            // 논리 연산자 (AND, OR, XOR) - 각각 추가 경로 생성
            complexity += Regex.Matches(trimmed, @"\b(AND|OR|XOR)\b").Count;

            // RETURN 문 (함수 내 조기 반환)
            if (trimmed.Contains("RETURN") && !trimmed.StartsWith("FUNCTION"))
                complexity++;
        }

        if (complexity > MaxComplexity)
        {
            issues.Add(CreateIssue(
                "순환 복잡도 초과",
                $"코드의 순환 복잡도가 {complexity}입니다. (권장: {MaxComplexity} 이하)",
                change.FilePath,
                change.StartLine,
                @"
높은 순환 복잡도는:
- 테스트 케이스 폭발적 증가 (복잡도 N = 최소 N개 테스트 필요)
- 버그 발생 확률 증가
- 코드 이해도 저하
- 유지보수 비용 증가
",
                @"
✅ 권장 해결 방법:
1. 함수를 작은 단위로 분리 (단일 책임 원칙)
2. 복잡한 조건을 boolean 변수로 추출
3. CASE 문 대신 상태 머신 패턴 고려
4. 룩업 테이블 또는 함수 포인터 배열 사용
",
                $"복잡도: {complexity}",
                "// 함수 분리 권장"));
        }

        return issues;
    }
}

#endregion

#region SA0055 - 인지 복잡도 초과

/// <summary>
/// SA0055: 인지 복잡도 초과 감지
/// 코드 이해가 어려운 구조
/// </summary>
public class SA0055_CognitiveComplexity : SARuleBase
{
    public override string RuleId => "SA0055";
    public override string RuleName => "인지 복잡도 초과";
    public override string Description => "코드의 인지 복잡도가 높은 구조를 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.Metrics;
    public override bool SupportsPrecompile => true;

    private const int MaxCognitiveComplexity = 15;

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        int complexity = CalculateCognitiveComplexity(code);

        if (complexity > MaxCognitiveComplexity)
        {
            issues.Add(CreateIssue(
                "인지 복잡도 초과",
                $"코드의 인지 복잡도가 {complexity}입니다. (권장: {MaxCognitiveComplexity} 이하)",
                change.FilePath,
                change.StartLine,
                "높은 인지 복잡도는 코드 이해를 어렵게 만듭니다.",
                "중첩을 줄이고, 복잡한 로직을 별도 함수로 분리하세요."));
        }

        return issues;
    }

    private int CalculateCognitiveComplexity(string code)
    {
        int complexity = 0;
        int nestingLevel = 0;
        var lines = code.Split('\n');

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim().ToUpperInvariant();

            // 중첩 증가
            if (trimmedLine.StartsWith("IF ") || trimmedLine.StartsWith("FOR ") ||
                trimmedLine.StartsWith("WHILE ") || trimmedLine.StartsWith("CASE "))
            {
                complexity += 1 + nestingLevel; // 기본 +1, 중첩당 추가 +1
                nestingLevel++;
            }
            else if (trimmedLine.StartsWith("ELSIF"))
            {
                complexity += 1;
            }
            else if (trimmedLine.StartsWith("ELSE"))
            {
                complexity += 1;
            }
            else if (trimmedLine.StartsWith("END_"))
            {
                nestingLevel = Math.Max(0, nestingLevel - 1);
            }

            // 논리 연산자
            complexity += Regex.Matches(trimmedLine, @"\b(AND|OR)\b").Count;
        }

        return complexity;
    }
}

#endregion

#region SA0056 - 주석 비율 부족

/// <summary>
/// SA0056: 주석 비율 부족 감지
/// 코드 대비 주석이 부족한 경우
/// </summary>
public class SA0056_InsufficientComments : SARuleBase
{
    public override string RuleId => "SA0056";
    public override string RuleName => "주석 비율 부족";
    public override string Description => "코드 대비 주석이 부족한 경우를 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.Comments;
    public override bool SupportsPrecompile => true;

    private const double MinCommentRatio = 0.1; // 10%

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var lines = code.Split('\n');

        int codeLines = 0;
        int commentLines = 0;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            if (trimmed.StartsWith("//") || trimmed.StartsWith("(*"))
                commentLines++;
            else
                codeLines++;
        }

        if (codeLines > 20) // 최소 20줄 이상일 때만 검사
        {
            double ratio = (double)commentLines / codeLines;
            if (ratio < MinCommentRatio)
            {
                issues.Add(CreateIssue(
                    "주석 비율 부족",
                    $"주석 비율이 {ratio:P1}입니다. (권장: {MinCommentRatio:P0} 이상)",
                    change.FilePath,
                    change.StartLine,
                    "주석이 부족하면 코드 이해가 어려워집니다.",
                    "주요 로직과 복잡한 부분에 주석을 추가하세요."));
            }
        }

        return issues;
    }
}

#endregion

#region SA0057 - 헤더 주석 누락

/// <summary>
/// SA0057: 헤더 주석 누락 감지
/// 함수/FB에 설명 주석이 없는 경우
/// </summary>
public class SA0057_MissingHeaderComment : SARuleBase
{
    public override string RuleId => "SA0057";
    public override string RuleName => "헤더 주석 누락";
    public override string Description => "함수나 Function Block에 설명 주석이 없는 경우를 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.Comments;
    public override bool SupportsPrecompile => true;

    private static readonly Regex BlockStartPattern = new(
        @"^(FUNCTION_BLOCK|FUNCTION|PROGRAM|METHOD)\s+\w+",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var lines = code.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var match = BlockStartPattern.Match(lines[i]);
            if (match.Success)
            {
                // 이전 줄이 주석인지 확인
                bool hasComment = false;
                if (i > 0)
                {
                    var prevLine = lines[i - 1].Trim();
                    hasComment = prevLine.StartsWith("//") ||
                                 prevLine.EndsWith("*)") ||
                                 prevLine.StartsWith("(*");
                }

                if (!hasComment)
                {
                    var blockType = match.Groups[1].Value;
                    issues.Add(CreateIssue(
                        "헤더 주석 누락",
                        $"{blockType} 선언 전에 설명 주석이 없습니다.",
                        change.FilePath,
                        change.StartLine + i,
                        "헤더 주석이 없으면 코드 목적을 이해하기 어렵습니다.",
                        $@"
다음과 같은 헤더 주석을 추가하세요:
(*
    {blockType}: [이름]
    목적: [설명]
    입력: [입력 변수 설명]
    출력: [출력 변수 설명]
*)
"));
                }
            }
        }

        return issues;
    }
}

#endregion

#region SA0058 - 오래된 주석

/// <summary>
/// SA0058: 오래된 주석 감지
/// 코드와 일치하지 않는 주석
/// </summary>
public class SA0058_OutdatedComments : SARuleBase
{
    public override string RuleId => "SA0058";
    public override string RuleName => "오래된 주석";
    public override string Description => "코드와 일치하지 않을 수 있는 오래된 주석을 감지합니다.";
    public override Severity Severity => Severity.Info;
    public override SARuleCategory Category => SARuleCategory.Comments;
    public override bool SupportsPrecompile => true;

    // 오래된 주석 패턴
    private static readonly Regex OutdatedPattern = new(
        @"(OLD|OBSOLETE|DEPRECATED|이전|기존|예전|삭제 예정)",
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
            if ((line.Contains("//") || line.Contains("(*")) && OutdatedPattern.IsMatch(line))
            {
                issues.Add(CreateIssue(
                    "오래된 주석",
                    "오래된 것으로 보이는 주석이 발견되었습니다.",
                    change.FilePath,
                    change.StartLine + i,
                    "오래된 주석은 혼란을 야기할 수 있습니다.",
                    "주석을 업데이트하거나 관련 코드와 함께 삭제하세요.",
                    line.Trim(),
                    "// 주석 업데이트 필요"));
            }
        }

        return issues;
    }
}

#endregion

#region SA0059 - 주석 처리된 코드

/// <summary>
/// SA0059: 주석 처리된 코드 감지
/// 실행 코드가 주석 처리된 경우
/// </summary>
public class SA0059_CommentedOutCode : SARuleBase
{
    public override string RuleId => "SA0059";
    public override string RuleName => "주석 처리된 코드";
    public override string Description => "실행 코드가 주석 처리된 경우를 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Comments;
    public override bool SupportsPrecompile => true;

    // 코드처럼 보이는 주석 패턴
    private static readonly Regex CommentedCodePattern = new(
        @"//\s*(\w+\s*:=|IF\s+|FOR\s+|WHILE\s+|END_)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex BlockCommentedCodePattern = new(
        @"\(\*\s*(\w+\s*:=|IF\s+|FOR\s+|WHILE\s+)",
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

            if (CommentedCodePattern.IsMatch(line) || BlockCommentedCodePattern.IsMatch(line))
            {
                issues.Add(CreateIssue(
                    "주석 처리된 코드",
                    "실행 코드가 주석 처리된 것으로 보입니다.",
                    change.FilePath,
                    change.StartLine + i,
                    @"
주석 처리된 코드는:
- 코드 가독성 저하
- 버전 관리 시스템으로 대체 가능
- 유지보수 부담 증가
",
                    "불필요한 코드는 삭제하세요. 버전 관리 시스템에서 복구 가능합니다.",
                    line.Trim(),
                    "// 삭제 권장"));
            }
        }

        return issues;
    }
}

#endregion

#region SA0060 - 비효과적 연산

/// <summary>
/// SA0060: 비효과적 연산 감지
/// 결과가 없는 연산 (예: x := x)
/// </summary>
public class SA0060_IneffectiveOperation : SARuleBase
{
    public override string RuleId => "SA0060";
    public override string RuleName => "비효과적 연산";
    public override string Description => "결과가 없는 비효과적인 연산을 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Operations;
    public override bool SupportsPrecompile => true;

    // 자기 자신에게 대입
    private static readonly Regex SelfAssignmentPattern = new(
        @"(\w+)\s*:=\s*\1\s*;",
        RegexOptions.Compiled);

    // 0 더하기/빼기
    private static readonly Regex AddZeroPattern = new(
        @"(\w+)\s*:=\s*\1\s*[+\-]\s*0\s*;",
        RegexOptions.Compiled);

    // 1 곱하기/나누기
    private static readonly Regex MultiplyOnePattern = new(
        @"(\w+)\s*:=\s*\1\s*[*/]\s*1\s*;",
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

            if (SelfAssignmentPattern.IsMatch(line))
            {
                issues.Add(CreateIssue(
                    "자기 자신에게 대입",
                    "변수가 자기 자신에게 대입되고 있습니다.",
                    change.FilePath,
                    change.StartLine + i,
                    "자기 자신에게 대입은 의미 없는 연산입니다.",
                    "이 라인이 필요한지 확인하고 제거하세요.",
                    line.Trim(),
                    "// 삭제 권장"));
            }

            if (AddZeroPattern.IsMatch(line))
            {
                issues.Add(CreateIssue(
                    "0 더하기/빼기",
                    "0을 더하거나 빼는 연산은 효과가 없습니다.",
                    change.FilePath,
                    change.StartLine + i,
                    "x := x + 0 또는 x := x - 0은 불필요합니다.",
                    "이 연산을 제거하세요."));
            }

            if (MultiplyOnePattern.IsMatch(line))
            {
                issues.Add(CreateIssue(
                    "1 곱하기/나누기",
                    "1을 곱하거나 나누는 연산은 효과가 없습니다.",
                    change.FilePath,
                    change.StartLine + i,
                    "x := x * 1 또는 x := x / 1은 불필요합니다.",
                    "이 연산을 제거하세요."));
            }
        }

        return issues;
    }
}

#endregion

#region SA0061 - 의심스러운 포인터 연산

/// <summary>
/// SA0061: 의심스러운 포인터 연산 감지
/// 포인터 관련 잠재적 위험 코드
/// </summary>
public class SA0061_SuspiciousPointerOperation : SARuleBase
{
    public override string RuleId => "SA0061";
    public override string RuleName => "의심스러운 포인터 연산";
    public override string Description => "포인터 관련 잠재적 위험 코드를 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Operations;
    public override bool SupportsPrecompile => true;

    // 포인터 역참조 후 NULL 체크
    private static readonly Regex DereferenceBeforeCheckPattern = new(
        @"(\w+)\^.*?IF\s+\1\s*[<>=]+\s*(0|NULL)",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    // 포인터 산술
    private static readonly Regex PointerArithmeticPattern = new(
        @"POINTER\s+.*?[+\-]\s*\d+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var lines = code.Split('\n');

        // 포인터 산술 감지
        for (int i = 0; i < lines.Length; i++)
        {
            if (PointerArithmeticPattern.IsMatch(lines[i]))
            {
                issues.Add(CreateIssue(
                    "포인터 산술 연산",
                    "포인터에 대한 산술 연산이 감지되었습니다.",
                    change.FilePath,
                    change.StartLine + i,
                    "포인터 산술은 메모리 접근 오류를 유발할 수 있습니다.",
                    "배열 인덱스나 ADR 함수 사용을 고려하세요."));
            }
        }

        // 역참조 후 NULL 체크
        if (DereferenceBeforeCheckPattern.IsMatch(code))
        {
            issues.Add(CreateIssue(
                "역참조 후 NULL 체크",
                "포인터 역참조 후에 NULL 체크를 하고 있습니다.",
                change.FilePath,
                change.StartLine,
                "포인터는 역참조 전에 NULL 체크를 해야 합니다.",
                "NULL 체크를 역참조 이전으로 이동하세요."));
        }

        return issues;
    }
}

#endregion

#region SA0062 - 상수 조건

/// <summary>
/// SA0062: 상수 조건 감지
/// 항상 TRUE 또는 FALSE인 조건
/// </summary>
public class SA0062_ConstantCondition : SARuleBase
{
    public override string RuleId => "SA0062";
    public override string RuleName => "상수 조건";
    public override string Description => "항상 TRUE 또는 FALSE인 상수 조건을 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Operations;
    public override bool SupportsPrecompile => true;

    private static readonly Regex ConstantTruePattern = new(
        @"IF\s+(TRUE|1\s*=\s*1|0\s*=\s*0)\s+THEN",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ConstantFalsePattern = new(
        @"IF\s+(FALSE|0\s*=\s*1|1\s*=\s*0)\s+THEN",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ImpossibleRangePattern = new(
        @"IF\s+(\w+)\s*>\s*(\d+)\s+AND\s+\1\s*<\s*(\d+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;

        var trueMatches = ConstantTruePattern.Matches(code);
        foreach (Match match in trueMatches)
        {
            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
            issues.Add(CreateIssue(
                "항상 TRUE인 조건",
                $"조건 '{match.Groups[1].Value}'은(는) 항상 TRUE입니다.",
                change.FilePath,
                change.StartLine + lineNumber,
                "항상 TRUE인 조건은 IF 문이 불필요함을 의미합니다.",
                "IF 문을 제거하고 본문만 남기세요."));
        }

        var falseMatches = ConstantFalsePattern.Matches(code);
        foreach (Match match in falseMatches)
        {
            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
            issues.Add(CreateIssue(
                "항상 FALSE인 조건",
                $"조건 '{match.Groups[1].Value}'은(는) 항상 FALSE입니다.",
                change.FilePath,
                change.StartLine + lineNumber,
                "항상 FALSE인 조건의 본문은 Dead Code입니다.",
                "IF 블록 전체를 삭제하세요."));
        }

        // 불가능한 범위 검사
        var rangeMatches = ImpossibleRangePattern.Matches(code);
        foreach (Match match in rangeMatches)
        {
            if (int.TryParse(match.Groups[2].Value, out int lower) &&
                int.TryParse(match.Groups[3].Value, out int upper) &&
                lower >= upper)
            {
                var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
                issues.Add(CreateIssue(
                    "불가능한 범위 조건",
                    $"'{match.Groups[1].Value} > {lower} AND {match.Groups[1].Value} < {upper}'는 불가능한 조건입니다.",
                    change.FilePath,
                    change.StartLine + lineNumber,
                    "이 조건은 절대 참이 될 수 없습니다.",
                    "범위 값을 수정하거나 조건을 제거하세요."));
            }
        }

        return issues;
    }
}

#endregion

#region SA0063 - 부동소수점 비교

/// <summary>
/// SA0063: 부동소수점 직접 비교 감지
/// REAL/LREAL의 = 비교
/// </summary>
public class SA0063_FloatEquality : SARuleBase
{
    public override string RuleId => "SA0063";
    public override string RuleName => "부동소수점 직접 비교";
    public override string Description => "REAL/LREAL 타입의 직접 등호 비교를 감지합니다.";
    public override Severity Severity => Severity.Warning;
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
            var trimmed = line.Trim();

            // 주석 제외
            if (trimmed.StartsWith("//") || trimmed.StartsWith("(*"))
                continue;

            // REAL/LREAL 변수 선언 확인
            var realVarPattern = new Regex(@"(\w+)\s*:\s*(REAL|LREAL)", RegexOptions.IgnoreCase);
            var realVars = realVarPattern.Matches(code);
            var realVarNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (Match varMatch in realVars)
            {
                realVarNames.Add(varMatch.Groups[1].Value);
            }

            // 부동소수점 리터럴 비교 (소수점 있는 숫자)
            var floatLiteralPattern = new Regex(@"(\w+)\s*=\s*(\d+\.\d+)", RegexOptions.IgnoreCase);
            var floatMatches = floatLiteralPattern.Matches(line);

            foreach (Match match in floatMatches)
            {
                var varName = match.Groups[1].Value;
                var value = match.Groups[2].Value;

                issues.Add(CreateIssue(
                    "부동소수점 직접 비교",
                    $"'{varName} = {value}'는 부동소수점 정밀도 문제를 야기할 수 있습니다.",
                    change.FilePath,
                    change.StartLine + i,
                    @"
부동소수점 직접 비교는 위험합니다:
- 이진 표현의 정밀도 한계 (0.1은 정확히 표현 불가)
- 계산 오차 누적
- 예상치 못한 FALSE 결과
- 플랫폼 간 차이
",
                    $@"
✅ 권장 해결 방법:
VAR CONSTANT
    EPSILON : REAL := 0.0001;  // 허용 오차
END_VAR

IF ABS({varName} - {value}) < EPSILON THEN
    // 같다고 판단
END_IF
",
                    match.Value,
                    $"ABS({varName} - {value}) < EPSILON"));
            }

            // 두 REAL 변수 간 비교 (변수 대 변수)
            var varComparePattern = new Regex(@"(\w+)\s*=\s*(\w+)(?!\s*:)", RegexOptions.IgnoreCase);
            var varMatches = varComparePattern.Matches(line);

            foreach (Match match in varMatches)
            {
                var var1 = match.Groups[1].Value;
                var var2 = match.Groups[2].Value;

                // 둘 다 REAL/LREAL 변수인지 확인
                if (realVarNames.Contains(var1) && realVarNames.Contains(var2))
                {
                    issues.Add(CreateIssue(
                        "REAL 변수 간 직접 비교",
                        $"REAL 변수 '{var1}'과 '{var2}'를 직접 비교하고 있습니다.",
                        change.FilePath,
                        change.StartLine + i,
                        "부동소수점 변수 간 직접 비교는 미세한 오차로 인해 실패할 수 있습니다.",
                        $"IF ABS({var1} - {var2}) < EPSILON THEN ... END_IF 형태로 비교하세요.",
                        match.Value,
                        $"ABS({var1} - {var2}) < EPSILON"));
                }
            }
        }

        return issues;
    }
}

#endregion

#region SA0064 - 의심스러운 포인터 산술

/// <summary>
/// SA0064: 의심스러운 포인터 산술 감지
/// 위험한 포인터 연산
/// </summary>
public class SA0064_SuspiciousPointerArithmetic : SARuleBase
{
    public override string RuleId => "SA0064";
    public override string RuleName => "의심스러운 포인터 산술";
    public override string Description => "위험한 포인터 산술 연산을 감지합니다.";
    public override Severity Severity => Severity.Critical;
    public override SARuleCategory Category => SARuleCategory.Operations;
    public override bool SupportsPrecompile => true;

    // ADR 결과에 대한 산술
    private static readonly Regex AdrArithmeticPattern = new(
        @"ADR\s*\([^)]+\)\s*[+\-]\s*\d+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // 포인터 변수에 대한 산술
    private static readonly Regex PtrVarArithmeticPattern = new(
        @"p\w+\s*[+\-]\s*\d+",
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

            if (AdrArithmeticPattern.IsMatch(line))
            {
                issues.Add(CreateIssue(
                    "ADR 결과에 대한 산술",
                    "ADR() 결과에 산술 연산을 수행하고 있습니다.",
                    change.FilePath,
                    change.StartLine + i,
                    @"
ADR에 대한 산술은 매우 위험합니다:
- 메모리 경계 벗어남
- 데이터 손상
- 시스템 불안정
",
                    "배열 인덱싱이나 구조체 멤버 접근을 사용하세요."));
            }

            // p로 시작하는 변수명 (포인터 관례)에 산술
            if (PtrVarArithmeticPattern.IsMatch(line))
            {
                issues.Add(CreateIssue(
                    "포인터 변수 산술",
                    "포인터 변수에 산술 연산을 수행하고 있습니다.",
                    change.FilePath,
                    change.StartLine + i,
                    "포인터 산술은 메모리 안전성을 해칠 수 있습니다.",
                    "배열이나 구조체를 사용하여 안전하게 접근하세요."));
            }
        }

        return issues;
    }
}

#endregion

#region SA0065 - 초기화되지 않은 변수 사용

/// <summary>
/// SA0065: 초기화되지 않은 변수 사용 감지
/// 값 할당 전 사용
/// </summary>
public class SA0065_UninitializedVariable : SARuleBase
{
    public override string RuleId => "SA0065";
    public override string RuleName => "초기화되지 않은 변수";
    public override string Description => "초기화되지 않은 변수의 사용을 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Initialization;
    public override bool SupportsPrecompile => true;

    private static readonly Regex VarBlockPattern = new(
        @"VAR\s*(.*?)END_VAR",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var varMatches = VarBlockPattern.Matches(code);

        foreach (Match varMatch in varMatches)
        {
            var varBlock = varMatch.Groups[1].Value;

            // 초기화 없는 변수 선언 패턴: 변수명 : 타입;
            var varWithoutInitPattern = new Regex(@"^\s*(\w+)\s*:\s*(\w+)\s*;", RegexOptions.Multiline);
            var uninitVars = varWithoutInitPattern.Matches(varBlock);

            // VAR 블록 이후의 코드
            var codeAfterVar = code.Substring(varMatch.Index + varMatch.Length);

            foreach (Match uninitVar in uninitVars)
            {
                var varName = uninitVar.Groups[1].Value;
                var varType = uninitVar.Groups[2].Value;

                // VAR_INPUT, VAR_OUTPUT은 초기화 불필요
                if (varBlock.Contains("VAR_INPUT") || varBlock.Contains("VAR_OUTPUT"))
                    continue;

                // 변수 사용 전에 할당이 있는지 확인
                var assignmentPattern = new Regex($@"\b{Regex.Escape(varName)}\s*:=", RegexOptions.IgnoreCase);
                var usagePattern = new Regex($@"\b{Regex.Escape(varName)}\b(?!\s*:=|\s*:)", RegexOptions.IgnoreCase);

                var firstAssignment = assignmentPattern.Match(codeAfterVar);
                var firstUsage = usagePattern.Match(codeAfterVar);

                // 할당 전에 사용되는 경우
                if (firstUsage.Success && (!firstAssignment.Success || firstUsage.Index < firstAssignment.Index))
                {
                    var lineNumber = code.Take(varMatch.Index + varMatch.Length + firstUsage.Index).Count(c => c == '\n') + 1;

                    issues.Add(CreateIssue(
                        "초기화되지 않은 변수 사용",
                        $"변수 '{varName}'이(가) 값을 할당받기 전에 사용되었습니다.",
                        change.FilePath,
                        change.StartLine + lineNumber,
                        @"
초기화되지 않은 변수 사용의 위험:
- 예측 불가능한 초기값 (0일 수도, 쓰레기값일 수도)
- 플랫폼 의존적 동작
- 디버깅 어려움
- 간헐적 버그 발생
",
                        $@"
✅ 권장 해결 방법:
1. 선언 시 초기화:
   {varName} : {varType} := 0;  // 적절한 초기값

2. 사용 전 할당:
   {varName} := initialValue;
   // 이후 사용
",
                        $"{varName} 사용",
                        $"{varName} := 0; // 먼저 초기화"));
                }
            }
        }

        return issues;
    }
}

#endregion

#region SA0066 - 배열 경계 초과

/// <summary>
/// SA0066: 배열 경계 초과 가능성 감지
/// 상수 인덱스가 배열 범위를 벗어남
/// </summary>
public class SA0066_ArrayOutOfBounds : SARuleBase
{
    public override string RuleId => "SA0066";
    public override string RuleName => "배열 경계 초과";
    public override string Description => "배열 경계를 초과할 수 있는 접근을 감지합니다.";
    public override Severity Severity => Severity.Critical;
    public override SARuleCategory Category => SARuleCategory.Operations;
    public override bool SupportsPrecompile => true;

    // 배열 선언 패턴 ARRAY[0..9]
    private static readonly Regex ArrayDeclPattern = new(
        @"(\w+)\s*:\s*ARRAY\s*\[\s*(\d+)\s*\.\.\s*(\d+)\s*\]",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // 배열 접근 패턴
    private static readonly Regex ArrayAccessPattern = new(
        @"(\w+)\s*\[\s*(\d+)\s*\]",
        RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;

        // 배열 범위 수집
        var arrays = new Dictionary<string, (int lower, int upper)>();
        var declMatches = ArrayDeclPattern.Matches(code);
        foreach (Match match in declMatches)
        {
            var name = match.Groups[1].Value;
            var lower = int.Parse(match.Groups[2].Value);
            var upper = int.Parse(match.Groups[3].Value);
            arrays[name.ToLowerInvariant()] = (lower, upper);
        }

        // 배열 접근 검사
        var accessMatches = ArrayAccessPattern.Matches(code);
        foreach (Match match in accessMatches)
        {
            var name = match.Groups[1].Value.ToLowerInvariant();
            if (int.TryParse(match.Groups[2].Value, out int index) && arrays.ContainsKey(name))
            {
                var (lower, upper) = arrays[name];
                if (index < lower || index > upper)
                {
                    var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
                    issues.Add(CreateIssue(
                        "배열 경계 초과",
                        $"'{match.Value}'에서 인덱스 {index}가 범위 [{lower}..{upper}]를 벗어납니다.",
                        change.FilePath,
                        change.StartLine + lineNumber,
                        "배열 경계 초과는 메모리 손상을 유발합니다.",
                        $"인덱스를 {lower}에서 {upper} 사이의 값으로 수정하세요."));
                }
            }
        }

        return issues;
    }
}

#endregion

#region SA0067 - 함수 내 전역 변수 사용

/// <summary>
/// SA0067: 함수 내 전역 변수 사용 감지
/// FUNCTION 내에서 전역 변수 접근
/// </summary>
public class SA0067_GlobalInFunction : SARuleBase
{
    public override string RuleId => "SA0067";
    public override string RuleName => "함수 내 전역 변수";
    public override string Description => "FUNCTION 내에서 전역 변수를 사용하는 경우를 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.VariablesAndConstants;
    public override bool SupportsPrecompile => true;

    private static readonly Regex FunctionBlockPattern = new(
        @"FUNCTION\s+\w+.*?END_FUNCTION",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    // 전역 변수 패턴 (g 또는 GVL. 접두사)
    private static readonly Regex GlobalVarPattern = new(
        @"\b(g\w+|GVL\.\w+)\b",
        RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var funcMatches = FunctionBlockPattern.Matches(code);

        foreach (Match funcMatch in funcMatches)
        {
            var funcBody = funcMatch.Value;
            var globalMatches = GlobalVarPattern.Matches(funcBody);

            foreach (Match globalMatch in globalMatches)
            {
                var lineNumber = code.Take(funcMatch.Index).Count(c => c == '\n') + 1;
                issues.Add(CreateIssue(
                    "함수 내 전역 변수 사용",
                    $"FUNCTION 내에서 전역 변수 '{globalMatch.Value}'를 사용하고 있습니다.",
                    change.FilePath,
                    change.StartLine + lineNumber,
                    @"
FUNCTION 내 전역 변수 사용은:
- 함수의 재사용성 저하
- 사이드 이펙트 발생
- 테스트 어려움
- 멀티태스크 환경에서 경쟁 조건
",
                    "전역 변수를 VAR_INPUT 파라미터로 전달하세요."));
            }
        }

        return issues;
    }
}

#endregion

#region SA0068 - 순환 참조

/// <summary>
/// SA0068: 순환 참조 감지
/// FB 간 상호 참조
/// </summary>
public class SA0068_CircularReference : SARuleBase
{
    public override string RuleId => "SA0068";
    public override string RuleName => "순환 참조";
    public override string Description => "Function Block 간의 순환 참조를 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.ObjectOriented;
    public override bool SupportsPrecompile => true;

    private static readonly Regex FBInstancePattern = new(
        @"(\w+)\s*:\s*FB_(\w+)",
        RegexOptions.Compiled);

    public override List<QAIssue> CheckVariableChange(VariableChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed)
            return issues;

        // FB 인스턴스가 같은 FB 타입을 참조하는 경우 (자기 참조)
        var match = FBInstancePattern.Match($"{change.VariableName}: {change.NewDataType}");
        if (match.Success)
        {
            var fbType = match.Groups[2].Value;
            // 자기 참조 패턴 감지
            if (change.FilePath.Contains(fbType, StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(CreateIssue(
                    "잠재적 순환 참조",
                    $"FB가 자기 자신의 타입을 인스턴스로 가질 수 있습니다.",
                    change.FilePath,
                    change.Line,
                    "순환 참조는 무한 루프나 스택 오버플로를 유발할 수 있습니다.",
                    "설계를 검토하여 순환 의존성을 제거하세요."));
            }
        }

        return issues;
    }
}

#endregion

#region SA0069 - INTERFACE 미구현 메서드

/// <summary>
/// SA0069: INTERFACE 미구현 메서드 감지
/// IMPLEMENTS 선언 후 메서드 누락
/// </summary>
public class SA0069_UnimplementedInterface : SARuleBase
{
    public override string RuleId => "SA0069";
    public override string RuleName => "인터페이스 미구현";
    public override string Description => "INTERFACE를 선언했지만 메서드를 구현하지 않은 경우를 감지합니다.";
    public override Severity Severity => Severity.Critical;
    public override SARuleCategory Category => SARuleCategory.ObjectOriented;
    public override bool SupportsPrecompile => false;

    private static readonly Regex ImplementsPattern = new(
        @"IMPLEMENTS\s+(\w+)",
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
            var interfaceName = match.Groups[1].Value;

            // METHOD 키워드가 없으면 미구현으로 간주
            if (!code.Contains("METHOD", StringComparison.OrdinalIgnoreCase))
            {
                var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;
                issues.Add(CreateIssue(
                    "인터페이스 메서드 미구현",
                    $"'{interfaceName}' 인터페이스를 선언했지만 메서드가 구현되지 않았습니다.",
                    change.FilePath,
                    change.StartLine + lineNumber,
                    "인터페이스의 모든 메서드는 반드시 구현되어야 합니다.",
                    $"'{interfaceName}' 인터페이스에 정의된 모든 메서드를 구현하세요."));
            }
        }

        return issues;
    }
}

#endregion

#region SA0070 - 빈 CASE 분기

/// <summary>
/// SA0070: 빈 CASE 분기 감지
/// 내용이 없는 CASE 항목
/// </summary>
public class SA0070_EmptyCaseBranch : SARuleBase
{
    public override string RuleId => "SA0070";
    public override string RuleName => "빈 CASE 분기";
    public override string Description => "내용이 없는 빈 CASE 분기를 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.Operations;
    public override bool SupportsPrecompile => true;

    private static readonly Regex EmptyCaseBranchPattern = new(
        @"(\d+)\s*:\s*;",
        RegexOptions.Compiled);

    private static readonly Regex CaseBlockPattern = new(
        @"CASE\s+\w+\s+OF\s*(.*?)END_CASE",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var caseMatches = CaseBlockPattern.Matches(code);

        foreach (Match caseMatch in caseMatches)
        {
            var caseBody = caseMatch.Groups[1].Value;
            var emptyBranches = EmptyCaseBranchPattern.Matches(caseBody);

            foreach (Match emptyBranch in emptyBranches)
            {
                var lineNumber = code.Take(caseMatch.Index).Count(c => c == '\n') + 1;
                issues.Add(CreateIssue(
                    "빈 CASE 분기",
                    $"CASE {emptyBranch.Groups[1].Value}: 분기가 비어 있습니다.",
                    change.FilePath,
                    change.StartLine + lineNumber,
                    "빈 CASE 분기는 의도된 것인지 누락인지 불분명합니다.",
                    "의도적인 빈 분기면 주석을 추가하고, 그렇지 않으면 로직을 구현하세요.",
                    emptyBranch.Value,
                    $"{emptyBranch.Groups[1].Value}: // 의도적 빈 분기"));
            }
        }

        return issues;
    }
}

#endregion
