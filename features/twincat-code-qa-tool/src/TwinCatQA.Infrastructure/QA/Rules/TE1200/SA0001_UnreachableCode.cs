using System.Text.RegularExpressions;
using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Models.QA;

namespace TwinCatQA.Infrastructure.QA.Rules.TE1200;

/// <summary>
/// SA0001: 도달 불가능한 코드 감지
/// RETURN/EXIT 이후 코드, 항상 false인 조건문 등
/// </summary>
public class SA0001_UnreachableCode : SARuleBase
{
    public override string RuleId => "SA0001";
    public override string RuleName => "도달 불가능한 코드";
    public override string Description => "RETURN, EXIT 문 이후나 항상 false인 조건문 내부의 코드를 감지합니다.";
    public override Severity Severity => Severity.Warning;
    public override SARuleCategory Category => SARuleCategory.UnreachableUnusedCode;
    public override bool SupportsPrecompile => true;

    // 제어 흐름 종료 패턴
    private static readonly string[] FlowTerminators = { "RETURN", "EXIT", "CONTINUE" };

    // 항상 false인 조건 패턴
    private static readonly Regex AlwaysFalsePattern = new(
        @"IF\s+(FALSE|0\s*=\s*1|1\s*=\s*0)\s+THEN",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // 항상 true인 조건 후 ELSE 패턴
    private static readonly Regex AlwaysTrueElsePattern = new(
        @"IF\s+(TRUE|1\s*=\s*1|0\s*=\s*0)\s+THEN.*?ELSE",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    public override List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        if (change.ChangeType == ChangeType.Removed || string.IsNullOrWhiteSpace(change.NewCode))
            return issues;

        var code = change.NewCode;
        var lines = code.Split('\n');

        // 1. RETURN/EXIT 이후 코드 감지
        CheckCodeAfterTerminator(lines, change, issues);

        // 2. 항상 false인 조건문 감지
        CheckAlwaysFalseConditions(code, change, issues);

        // 3. 항상 true인 조건의 ELSE 블록 감지
        CheckUnreachableElse(code, change, issues);

        return issues;
    }

    private void CheckCodeAfterTerminator(string[] lines, LogicChange change, List<QAIssue> issues)
    {
        bool foundTerminator = false;
        int terminatorLine = 0;
        int nestingLevel = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            var lineUpper = line.ToUpperInvariant();

            // 주석과 빈 줄 건너뛰기
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//") || line.StartsWith("(*"))
                continue;

            // 중첩 레벨 추적 (더 정확하게)
            if (lineUpper.StartsWith("IF ") || lineUpper.StartsWith("CASE ") ||
                lineUpper.StartsWith("FOR ") || lineUpper.StartsWith("WHILE ") ||
                lineUpper.StartsWith("REPEAT"))
                nestingLevel++;

            if (lineUpper.StartsWith("END_IF") || lineUpper.StartsWith("END_CASE") ||
                lineUpper.StartsWith("END_FOR") || lineUpper.StartsWith("END_WHILE") ||
                lineUpper.StartsWith("END_REPEAT"))
            {
                nestingLevel = Math.Max(0, nestingLevel - 1);
                foundTerminator = false; // 블록 종료 시 리셋
                continue;
            }

            // ELSE, ELSIF는 종료자 이후가 아님
            if (lineUpper.StartsWith("ELSE") || lineUpper.StartsWith("ELSIF"))
            {
                foundTerminator = false;
                continue;
            }

            // 종료자 감지 (라인 시작이나 세미콜론 직후)
            bool isTerminator = FlowTerminators.Any(t =>
                lineUpper.StartsWith(t + ";") ||
                lineUpper.Equals(t) ||
                (lineUpper.Contains(";") && lineUpper.Split(';')[^1].Trim().Equals(t)));

            if (isTerminator)
            {
                foundTerminator = true;
                terminatorLine = i;
                continue;
            }

            // 종료자 이후 실행 코드 감지
            if (foundTerminator && nestingLevel == 0 && !lineUpper.StartsWith("END_"))
            {
                var terminatorType = FlowTerminators.First(t =>
                    lines[terminatorLine].ToUpperInvariant().Contains(t));

                issues.Add(CreateIssue(
                    "RETURN/EXIT 이후 도달 불가능한 코드",
                    $"라인 {terminatorLine + 1}의 '{terminatorType}' 이후 코드는 실행되지 않습니다.",
                    change.FilePath,
                    change.StartLine + i,
                    @"
RETURN, EXIT 문 이후의 코드는 절대 실행되지 않습니다:
- 코드 검토 시 혼란 야기
- 의도하지 않은 로직 누락 가능성
- 컴파일러 최적화 시 제거됨
",
                    @"
✅ 권장 해결 방법:
1. 불필요한 코드 삭제
2. 조건문으로 분기 처리
3. RETURN/EXIT 위치 재검토
",
                    lines[i],
                    "// 삭제 권장",
                    new List<string>
                    {
                        "❌ 나쁜 예:",
                        "   RETURN;",
                        "   nValue := 10;  // 실행 안됨!",
                        "",
                        "✅ 좋은 예:",
                        "   IF bCondition THEN",
                        "       RETURN;",
                        "   END_IF",
                        "   nValue := 10;  // 조건에 따라 실행"
                    }));

                foundTerminator = false; // 하나만 보고
            }
        }
    }

    private void CheckAlwaysFalseConditions(string code, LogicChange change, List<QAIssue> issues)
    {
        var matches = AlwaysFalsePattern.Matches(code);
        foreach (Match match in matches)
        {
            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;

            issues.Add(CreateIssue(
                "항상 FALSE인 조건문",
                $"조건 '{match.Value}'은(는) 항상 FALSE입니다. 내부 코드는 실행되지 않습니다.",
                change.FilePath,
                change.StartLine + lineNumber,
                @"
항상 FALSE인 조건문 내부 코드는 Dead Code입니다:
- 테스트 코드가 남아있을 가능성
- 잘못된 조건식 작성
- 코드 리뷰 시 혼란
",
                @"
✅ 권장 해결 방법:
1. 조건문 전체 삭제 (Dead Code인 경우)
2. 올바른 조건으로 수정
3. 디버그용이면 주석 처리
",
                match.Value,
                "// 조건 재검토 필요"));
        }
    }

    private void CheckUnreachableElse(string code, LogicChange change, List<QAIssue> issues)
    {
        var matches = AlwaysTrueElsePattern.Matches(code);
        foreach (Match match in matches)
        {
            var lineNumber = code.Take(match.Index).Count(c => c == '\n') + 1;

            issues.Add(CreateIssue(
                "항상 TRUE인 조건의 ELSE 블록",
                "조건이 항상 TRUE이므로 ELSE 블록은 실행되지 않습니다.",
                change.FilePath,
                change.StartLine + lineNumber,
                @"
항상 TRUE인 조건의 ELSE 블록은 Dead Code입니다:
- ELSE 내부 코드 절대 실행 안됨
- 유지보수 시 혼란
",
                @"
✅ 권장 해결 방법:
1. IF-ELSE 전체를 IF 블록 내용으로 대체
2. 조건이 올바른지 재검토
"));
        }
    }
}
