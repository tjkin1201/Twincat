using System.Text.RegularExpressions;
using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Models.QA;

namespace TwinCatQA.Infrastructure.QA.Rules.TE1200;

/// <summary>
/// SA0002: 빈 객체 감지
/// 본문이 없는 Function Block, Program, Function 등
/// </summary>
public class SA0002_EmptyObjects : SARuleBase
{
    public override string RuleId => "SA0002";
    public override string RuleName => "빈 객체";
    public override string Description => "본문이 없는 Function Block, Program, Function을 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.UnreachableUnusedCode;
    public override bool SupportsPrecompile => true;

    // 빈 POU 패턴들 (변수 선언부는 허용하지만 실행 코드가 없는 경우)
    private static readonly Regex EmptyProgramPattern = new(
        @"PROGRAM\s+(\w+)(?:\s+VAR.*?END_VAR)?(?:\s*//[^\n]*)?\s*END_PROGRAM",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex EmptyFunctionBlockPattern = new(
        @"FUNCTION_BLOCK\s+(\w+)(?:\s+(?:EXTENDS|IMPLEMENTS)\s+\w+)?(?:\s+VAR.*?END_VAR)?(?:\s*//[^\n]*)?\s*END_FUNCTION_BLOCK",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex EmptyFunctionPattern = new(
        @"FUNCTION\s+(\w+)\s*:\s*\w+(?:\s+VAR.*?END_VAR)?(?:\s*//[^\n]*)?\s*END_FUNCTION",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex EmptyMethodPattern = new(
        @"METHOD\s+(?:PUBLIC|PRIVATE|PROTECTED|INTERNAL)?\s*(\w+)\s*(?::\s*\w+)?(?:\s+VAR.*?END_VAR)?(?:\s*//[^\n]*)?\s*END_METHOD",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex EmptyPropertyGetPattern = new(
        @"PROPERTY\s+(?:PUBLIC|PRIVATE|PROTECTED)?\s*(\w+)\s*:\s*\w+(?:\s*//[^\n]*)?\s*END_PROPERTY",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex EmptyActionPattern = new(
        @"ACTION\s+(\w+)(?:\s*//[^\n]*)?\s*END_ACTION",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;

        // 빈 Program 검사
        CheckEmptyPattern(code, EmptyProgramPattern, "PROGRAM", change, issues);

        // 빈 Function Block 검사
        CheckEmptyPattern(code, EmptyFunctionBlockPattern, "FUNCTION_BLOCK", change, issues);

        // 빈 Function 검사
        CheckEmptyPattern(code, EmptyFunctionPattern, "FUNCTION", change, issues);

        // 빈 Method 검사
        CheckEmptyPattern(code, EmptyMethodPattern, "METHOD", change, issues);

        // 빈 Property 검사
        CheckEmptyPattern(code, EmptyPropertyGetPattern, "PROPERTY", change, issues);

        // 빈 Action 검사
        CheckEmptyPattern(code, EmptyActionPattern, "ACTION", change, issues);

        return issues;
    }

    private void CheckEmptyPattern(string code, Regex pattern, string objectType, LogicChange change, List<QAIssue> issues)
    {
        var matches = pattern.Matches(code);
        foreach (Match match in matches)
        {
            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;

            // 객체 이름 추출 (그룹 1에서)
            var objectName = match.Groups.Count > 1 && match.Groups[1].Success
                ? match.Groups[1].Value
                : "Unknown";

            // 변수 선언부와 주석을 제외하고 실행 코드가 있는지 확인
            var content = match.Value;
            var hasExecutableCode = false;

            // VAR...END_VAR 블록 제거
            var withoutVars = Regex.Replace(content, @"VAR.*?END_VAR", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);

            // 헤더와 종료 구문 제거
            var headerPattern = $@"{objectType}\s+.*?(?:\n|$)";
            var footerPattern = $@"END_{objectType}";
            withoutVars = Regex.Replace(withoutVars, headerPattern, "", RegexOptions.IgnoreCase);
            withoutVars = Regex.Replace(withoutVars, footerPattern, "", RegexOptions.IgnoreCase);

            // 주석 제거
            withoutVars = Regex.Replace(withoutVars, @"//[^\n]*", "");
            withoutVars = Regex.Replace(withoutVars, @"\(\*.*?\*\)", "", RegexOptions.Singleline);

            // 남은 내용이 공백만 아니면 실행 코드가 있음
            hasExecutableCode = !string.IsNullOrWhiteSpace(withoutVars);

            // 실행 코드가 없으면 이슈 생성
            if (!hasExecutableCode)
            {
                issues.Add(CreateIssue(
                    $"빈 {objectType} 감지",
                    $"{objectType} '{objectName}'에 실행 코드가 없습니다.",
                    change.FilePath,
                    change.StartLine + lineNumber,
                    $@"
빈 {objectType}은 다음 문제를 야기합니다:
- 코드 가독성 저하 (왜 존재하는지 불분명)
- 미완성 구현의 징후
- 불필요한 메모리 사용
- 유지보수 혼란
",
                    $@"
✅ 권장 해결 방법:
1. 구현이 필요 없으면 삭제
2. 향후 구현 예정이면 TODO 주석 추가
3. 의도적 빈 객체면 주석으로 이유 명시

예시:
{objectType} {objectName}
    // TODO: 추후 구현 예정 - 센서 연동
END_{objectType}
",
                    match.Value,
                    $"{objectType} {objectName}\n    // 구현 필요\nEND_{objectType}",
                    new List<string>
                    {
                        "❌ 나쁜 예:",
                        $"   {objectType} Empty{objectType}",
                        $"   END_{objectType}",
                        "",
                        "✅ 좋은 예:",
                        $"   {objectType} {objectName}",
                        "       // TODO: Phase 2에서 구현 예정",
                        "       // 현재는 인터페이스 정의만",
                        $"   END_{objectType}"
                    }));
            }
        }
    }
}
