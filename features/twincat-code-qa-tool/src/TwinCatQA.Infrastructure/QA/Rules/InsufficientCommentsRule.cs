using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Models.QA;
using TwinCatQA.Domain.Services;

namespace TwinCatQA.Infrastructure.QA.Rules;

/// <summary>
/// 주석 부족 감지 규칙 (Info)
/// 20줄당 주석이 1개 미만인 경우 가독성 저하 가능성 검사
/// </summary>
public class InsufficientCommentsRule : IQARuleChecker
{
    public string RuleId => "QA016";
    public string RuleName => "주석 부족 감지";
    public string Description => "코드 가독성 향상을 위한 적절한 주석 작성 권장 (20줄당 최소 1개)";
    public Severity Severity => Severity.Info;

    private const int LinesPerCommentThreshold = 20;

    public List<QAIssue> CheckVariableChange(VariableChange change)
    {
        // 변수 변경에서는 검사하지 않음
        return new List<QAIssue>();
    }

    public List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        // 추가되거나 수정된 로직만 검사
        if (change.ChangeType != ChangeType.Added &&
            change.ChangeType != ChangeType.Modified)
        {
            return issues;
        }

        var codeToCheck = change.NewCode ?? string.Empty;
        if (string.IsNullOrWhiteSpace(codeToCheck))
        {
            return issues;
        }

        // 코드 라인 수와 주석 수 계산
        var lines = codeToCheck.Split('\n');
        var totalLines = lines.Length;
        var commentCount = CountComments(lines);

        // 20줄당 최소 1개의 주석이 있어야 함
        var expectedComments = (totalLines + LinesPerCommentThreshold - 1) / LinesPerCommentThreshold;

        if (commentCount < expectedComments && totalLines >= LinesPerCommentThreshold)
        {
            issues.Add(new QAIssue
            {
                Severity = Severity.Info,
                Category = "코드 품질",
                Title = "주석 부족으로 인한 가독성 저하 가능",
                Description = $"{change.ElementName}: {totalLines}줄 중 주석 {commentCount}개 (권장: {expectedComments}개 이상)",
                Location = $"{change.FilePath}:{change.StartLine}",
                FilePath = change.FilePath,
                Line = change.StartLine,
                WhyDangerous = $@"
주석이 부족하면 코드 이해와 유지보수가 어려워집니다.
현재 {totalLines}줄의 코드에 {commentCount}개의 주석만 있어,
{LinesPerCommentThreshold}줄당 1개 기준({expectedComments}개)에 미달합니다.
특히 복잡한 로직이나 알고리즘의 경우 주석이 필수적입니다.
",
                Recommendation = $@"
✅ 권장 해결 방법:

1. 복잡한 로직 설명 추가
   // 센서 값을 읽어 이동평균 필터 적용
   filteredValue := (value1 + value2 + value3) / 3;

2. 중요한 변수 의미 설명
   // 최대 속도 제한값 [mm/s]
   maxSpeed := 1000.0;

3. 알고리즘 의도 명시
   // PID 제어: 목표값과 현재값의 오차를 줄임
   error := setpoint - processValue;

4. 주석 작성 가이드라인
   - 함수/FB 상단: 목적과 동작 설명
   - 복잡한 수식: 물리적 의미 설명
   - 매직 넘버: 값의 근거 설명
   - 상태 전환: 전환 조건 설명
",
                Examples = new List<string>
                {
                    "// ❌ 주석 부족",
                    "FOR i := 1 TO 10 DO",
                    "    sum := sum + arr[i] * 2;",
                    "    IF sum > 100 THEN",
                    "        flag := TRUE;",
                    "    END_IF",
                    "END_FOR",
                    "",
                    "// ✅ 적절한 주석",
                    "// 배열 값을 2배로 증폭하여 합산",
                    "FOR i := 1 TO 10 DO",
                    "    sum := sum + arr[i] * 2;  // 신호 증폭 계수 2.0",
                    "    // 합계가 임계값 초과 시 플래그 설정",
                    "    IF sum > 100 THEN",
                    "        flag := TRUE;  // 알람 트리거",
                    "    END_IF",
                    "END_FOR"
                },
                RuleId = RuleId,
                OldCodeSnippet = change.OldCode ?? string.Empty,
                NewCodeSnippet = change.NewCode ?? string.Empty
            });
        }

        return issues;
    }

    public List<QAIssue> CheckDataTypeChange(DataTypeChange change)
    {
        // 데이터 타입 변경에서는 검사하지 않음
        return new List<QAIssue>();
    }

    /// <summary>
    /// 주석 개수 계산
    /// </summary>
    private int CountComments(string[] lines)
    {
        int count = 0;
        bool inBlockComment = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // 블록 주석 시작 감지
            if (trimmed.Contains("(*"))
            {
                inBlockComment = true;
                count++;
            }

            // 블록 주석 종료 감지
            if (trimmed.Contains("*)"))
            {
                inBlockComment = false;
                continue;
            }

            // 블록 주석 내부
            if (inBlockComment)
            {
                continue;
            }

            // 라인 주석 감지
            if (trimmed.StartsWith("//"))
            {
                count++;
            }
        }

        return count;
    }
}
