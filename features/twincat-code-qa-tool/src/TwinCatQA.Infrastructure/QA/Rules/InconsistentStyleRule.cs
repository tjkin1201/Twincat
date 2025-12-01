using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Models.QA;
using TwinCatQA.Domain.Services;

namespace TwinCatQA.Infrastructure.QA.Rules;

/// <summary>
/// 스타일 불일치 감지 규칙 (Info)
/// 들여쓰기 스타일 혼용(TAB/SPACE) 감지
/// </summary>
public class InconsistentStyleRule : IQARuleChecker
{
    public string RuleId => "QA020";
    public string RuleName => "들여쓰기 스타일 불일치 감지";
    public string Description => "일관된 코딩 스타일 유지 권장 (TAB/SPACE 혼용 방지)";
    public Severity Severity => Severity.Info;

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

        // 들여쓰기 스타일 검사
        var styleAnalysis = AnalyzeIndentationStyle(codeToCheck);

        if (styleAnalysis.HasMixedStyle)
        {
            issues.Add(new QAIssue
            {
                Severity = Severity.Info,
                Category = "코드 스타일",
                Title = "들여쓰기 스타일 혼용으로 인한 일관성 저하",
                Description = $"{change.ElementName}: TAB {styleAnalysis.TabLines}줄, SPACE {styleAnalysis.SpaceLines}줄 혼용",
                Location = $"{change.FilePath}:{change.StartLine}",
                FilePath = change.FilePath,
                Line = change.StartLine,
                WhyDangerous = $@"
들여쓰기 스타일이 혼용되어 다음 문제가 발생합니다:

1. 가독성 저하: 편집기마다 다르게 표시됨
2. 버전 관리 충돌: 불필요한 diff 발생
3. 코드 정렬 혼란: 탭과 스페이스 너비 차이로 정렬 깨짐
4. 협업 어려움: 팀원마다 다른 설정으로 혼란

통계:
- TAB 들여쓰기: {styleAnalysis.TabLines}줄
- SPACE 들여쓰기: {styleAnalysis.SpaceLines}줄
- 혼용 비율: {styleAnalysis.MixedPercentage:F1}%

일관된 스타일을 선택하고 프로젝트 전체에 적용해야 합니다.
",
                Recommendation = $@"
✅ 권장 해결 방법:

1. 프로젝트 스타일 가이드 정의
   // .editorconfig 파일 생성
   [*.TcPOU]
   indent_style = tab
   indent_size = 4

   또는

   [*.TcPOU]
   indent_style = space
   indent_size = 4

2. Visual Studio/TwinCAT 설정 통일
   도구 → 옵션 → 텍스트 편집기 → 모든 언어
   - 탭 사용 또는 스페이스 사용 선택
   - 팀 전체가 동일 설정 사용

3. 자동 포맷팅 도구 사용
   - 코드 정리 기능 활용 (Ctrl+K, Ctrl+D)
   - 저장 시 자동 포맷팅 설정

4. 기존 코드 일괄 변환
   // TAB → SPACE 변환
   편집 → 고급 → 공백을 탭으로 변환

   // SPACE → TAB 변환
   편집 → 고급 → 탭을 공백으로 변환

5. Git hooks 활용
   // pre-commit hook으로 스타일 검사
   #!/bin/sh
   # 혼용 감지 스크립트 실행

권장 스타일:
- IEC 61131-3 표준: TAB (4칸)
- 대부분의 PLC 환경: TAB 사용이 일반적
",
                Examples = new List<string>
                {
                    "// ❌ 스타일 혼용 (보이지 않음)",
                    "FUNCTION_BLOCK FB_Example",
                    "VAR",
                    "\tvar1 : INT;        // TAB으로 들여쓰기",
                    "    var2 : INT;    // SPACE로 들여쓰기 (4칸)",
                    "\t\tvar3 : INT;    // TAB 2개",
                    "        var4 : INT;// SPACE 8칸",
                    "END_VAR",
                    "",
                    "// ✅ 일관된 스타일 (TAB)",
                    "FUNCTION_BLOCK FB_Example",
                    "VAR",
                    "\tvar1 : INT;",
                    "\tvar2 : INT;",
                    "\tvar3 : INT;",
                    "\tvar4 : INT;",
                    "END_VAR",
                    "",
                    "// ✅ 일관된 스타일 (SPACE 4칸)",
                    "FUNCTION_BLOCK FB_Example",
                    "VAR",
                    "    var1 : INT;",
                    "    var2 : INT;",
                    "    var3 : INT;",
                    "    var4 : INT;",
                    "END_VAR"
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
    /// 들여쓰기 스타일 분석
    /// </summary>
    private IndentationStyleAnalysis AnalyzeIndentationStyle(string code)
    {
        var lines = code.Split('\n');
        int tabLines = 0;
        int spaceLines = 0;

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // 라인 시작 부분의 공백 분석
            if (line.Length > 0 && char.IsWhiteSpace(line[0]))
            {
                if (line[0] == '\t')
                {
                    tabLines++;
                }
                else if (line[0] == ' ')
                {
                    // 연속된 스페이스가 4개 이상이면 들여쓰기로 간주
                    if (line.Length >= 4 &&
                        line[0] == ' ' &&
                        line[1] == ' ' &&
                        line[2] == ' ' &&
                        line[3] == ' ')
                    {
                        spaceLines++;
                    }
                }
            }
        }

        var totalIndentedLines = tabLines + spaceLines;
        var hasMixedStyle = tabLines > 0 && spaceLines > 0;
        var mixedPercentage = totalIndentedLines > 0
            ? (Math.Min(tabLines, spaceLines) * 100.0 / totalIndentedLines)
            : 0.0;

        return new IndentationStyleAnalysis
        {
            TabLines = tabLines,
            SpaceLines = spaceLines,
            HasMixedStyle = hasMixedStyle,
            MixedPercentage = mixedPercentage
        };
    }

    /// <summary>
    /// 들여쓰기 스타일 분석 결과
    /// </summary>
    private class IndentationStyleAnalysis
    {
        public int TabLines { get; init; }
        public int SpaceLines { get; init; }
        public bool HasMixedStyle { get; init; }
        public double MixedPercentage { get; init; }
    }
}
