using System.Text;
using TwinCatQA.Domain.Models.QA;

namespace TwinCatQA.Infrastructure.Reports;

/// <summary>
/// Markdown 형식의 QA 리포트 생성기
/// GitHub PR/Issue에 첨부하기 적합한 형식
/// </summary>
public class MarkdownReportGenerator
{
    /// <summary>
    /// Markdown 리포트 생성
    /// </summary>
    /// <param name="report">QA 보고서</param>
    /// <returns>Markdown 형식의 문자열</returns>
    public string Generate(QAReport report)
    {
        if (report == null)
            throw new ArgumentNullException(nameof(report), "QA 보고서가 null입니다.");

        var sb = new StringBuilder();

        // 헤더
        AppendHeader(sb, report);

        // 요약 섹션
        AppendSummary(sb, report);

        // Critical 이슈
        if (report.CriticalCount > 0)
        {
            AppendIssuesByPriority(sb, report, Severity.Critical, "🔴 Critical Issues");
        }

        // Warning 이슈
        if (report.WarningCount > 0)
        {
            AppendIssuesByPriority(sb, report, Severity.Warning, "🟡 Warning Issues");
        }

        // Info 이슈
        if (report.InfoCount > 0)
        {
            AppendIssuesByPriority(sb, report, Severity.Info, "🔵 Info Issues");
        }

        // 통계
        AppendStatistics(sb, report);

        // 파일별 상세 분석
        AppendFileAnalysis(sb, report);

        // 푸터
        AppendFooter(sb, report);

        return sb.ToString();
    }

    /// <summary>
    /// Markdown 리포트를 파일로 저장
    /// </summary>
    /// <param name="report">QA 보고서</param>
    /// <param name="outputPath">출력 경로 (null인 경우 자동 생성)</param>
    /// <returns>저장된 파일의 절대 경로</returns>
    public string GenerateToFile(QAReport report, string? outputPath = null)
    {
        if (report == null)
            throw new ArgumentNullException(nameof(report), "QA 보고서가 null입니다.");

        // 기본 출력 경로 설정
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            var reportsDir = Path.Combine(Directory.GetCurrentDirectory(), "reports");
            Directory.CreateDirectory(reportsDir);
            outputPath = Path.Combine(reportsDir, $"qa_report_{report.GeneratedAt:yyyyMMdd_HHmmss}.md");
        }

        var content = Generate(report);

        // 파일 저장
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(outputPath, content, Encoding.UTF8);

        return Path.GetFullPath(outputPath);
    }

    #region 내부 메서드

    /// <summary>
    /// 헤더 추가
    /// </summary>
    private void AppendHeader(StringBuilder sb, QAReport report)
    {
        sb.AppendLine("# 🔍 TwinCAT Code QA Report");
        sb.AppendLine();
        sb.AppendLine($"**생성 시각**: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"**원본 폴더**: `{EscapeMarkdown(report.SourceFolder)}`");
        sb.AppendLine($"**대상 폴더**: `{EscapeMarkdown(report.TargetFolder)}`");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
    }

    /// <summary>
    /// 요약 섹션 추가
    /// </summary>
    private void AppendSummary(StringBuilder sb, QAReport report)
    {
        sb.AppendLine("## 📊 Summary");
        sb.AppendLine();

        // 전체 상태
        var status = report.HasCriticalIssues
            ? "⚠️ **심각한 이슈가 발견되었습니다!**"
            : "✅ **심각한 이슈가 없습니다.**";
        sb.AppendLine(status);
        sb.AppendLine();

        // 요약 테이블
        sb.AppendLine("| 항목 | 개수 |");
        sb.AppendLine("|------|------|");
        sb.AppendLine($"| 총 변경 사항 | {report.TotalChanges} |");
        sb.AppendLine($"| 총 이슈 | {report.TotalIssues} |");
        sb.AppendLine($"| 🔴 Critical | {report.CriticalCount} |");
        sb.AppendLine($"| 🟡 Warning | {report.WarningCount} |");
        sb.AppendLine($"| 🔵 Info | {report.InfoCount} |");
        sb.AppendLine();
    }

    /// <summary>
    /// 심각도별 이슈 추가
    /// </summary>
    private void AppendIssuesByPriority(StringBuilder sb, QAReport report, Severity severity, string title)
    {
        var issues = report.IssuesBySeverity.GetValueOrDefault(severity, new List<QAIssue>());

        if (issues.Count == 0)
            return;

        sb.AppendLine($"## {title}");
        sb.AppendLine();
        sb.AppendLine($"총 **{issues.Count}개**의 {severity} 이슈가 발견되었습니다.");
        sb.AppendLine();

        foreach (var issue in issues)
        {
            AppendIssueDetail(sb, issue);
        }

        sb.AppendLine();
    }

    /// <summary>
    /// 개별 이슈 상세 정보 추가
    /// </summary>
    private void AppendIssueDetail(StringBuilder sb, QAIssue issue)
    {
        // 이슈 헤더
        var severityIcon = issue.Severity switch
        {
            Severity.Critical => "🔴",
            Severity.Warning => "🟡",
            Severity.Info => "🔵",
            _ => "⚪"
        };

        sb.AppendLine($"### {severityIcon} [{issue.RuleId}] {EscapeMarkdown(issue.Title)}");
        sb.AppendLine();

        // 위치 정보
        sb.AppendLine($"**파일**: `{EscapeMarkdown(issue.FilePath)}`");
        sb.AppendLine($"**위치**: 라인 {issue.Line}");
        sb.AppendLine($"**카테고리**: {EscapeMarkdown(issue.Category)}");
        sb.AppendLine();

        // 설명
        if (!string.IsNullOrWhiteSpace(issue.Description))
        {
            sb.AppendLine($"**설명**: {EscapeMarkdown(issue.Description)}");
            sb.AppendLine();
        }

        // 위험성 설명
        if (!string.IsNullOrWhiteSpace(issue.WhyDangerous))
        {
            sb.AppendLine($"**⚠️ 왜 위험한가요?**");
            sb.AppendLine();
            sb.AppendLine($"> {EscapeMarkdown(issue.WhyDangerous)}");
            sb.AppendLine();
        }

        // 변경 전 코드
        if (!string.IsNullOrWhiteSpace(issue.OldCodeSnippet))
        {
            sb.AppendLine("**변경 전 코드**:");
            sb.AppendLine("```iecst");
            sb.AppendLine(issue.OldCodeSnippet);
            sb.AppendLine("```");
            sb.AppendLine();
        }

        // 변경 후 코드
        if (!string.IsNullOrWhiteSpace(issue.NewCodeSnippet))
        {
            sb.AppendLine("**변경 후 코드**:");
            sb.AppendLine("```iecst");
            sb.AppendLine(issue.NewCodeSnippet);
            sb.AppendLine("```");
            sb.AppendLine();
        }

        // 권장 사항
        if (!string.IsNullOrWhiteSpace(issue.Recommendation))
        {
            sb.AppendLine($"**✅ 권장 해결 방법**:");
            sb.AppendLine();
            sb.AppendLine($"{EscapeMarkdown(issue.Recommendation)}");
            sb.AppendLine();
        }

        // 예시 코드
        if (issue.Examples != null && issue.Examples.Any())
        {
            sb.AppendLine("**📝 예시**:");
            sb.AppendLine();
            foreach (var example in issue.Examples)
            {
                sb.AppendLine("```iecst");
                sb.AppendLine(example);
                sb.AppendLine("```");
            }
            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine();
    }

    /// <summary>
    /// 통계 정보 추가
    /// </summary>
    private void AppendStatistics(StringBuilder sb, QAReport report)
    {
        sb.AppendLine("## 📈 Statistics");
        sb.AppendLine();

        // 카테고리별 통계
        if (report.IssuesByCategory.Any())
        {
            sb.AppendLine("### 카테고리별 이슈 분포");
            sb.AppendLine();
            sb.AppendLine("| 카테고리 | 개수 | 비율 |");
            sb.AppendLine("|---------|------|------|");

            foreach (var category in report.IssuesByCategory.OrderByDescending(x => x.Value.Count))
            {
                var percentage = (double)category.Value.Count / report.TotalIssues * 100;
                sb.AppendLine($"| {EscapeMarkdown(category.Key)} | {category.Value.Count} | {percentage:F1}% |");
            }
            sb.AppendLine();
        }

        // 규칙별 통계
        if (report.IssuesByRule.Any())
        {
            sb.AppendLine("### 위반 규칙 TOP 5");
            sb.AppendLine();
            sb.AppendLine("| 규칙 ID | 개수 |");
            sb.AppendLine("|---------|------|");

            var topRules = report.IssuesByRule
                .OrderByDescending(x => x.Value.Count)
                .Take(5);

            foreach (var rule in topRules)
            {
                sb.AppendLine($"| {EscapeMarkdown(rule.Key)} | {rule.Value.Count} |");
            }
            sb.AppendLine();
        }
    }

    /// <summary>
    /// 파일별 분석 추가
    /// </summary>
    private void AppendFileAnalysis(StringBuilder sb, QAReport report)
    {
        if (!report.IssuesByFile.Any())
            return;

        sb.AppendLine("## 📁 File Analysis");
        sb.AppendLine();

        sb.AppendLine("### 이슈가 가장 많은 파일 TOP 10");
        sb.AppendLine();
        sb.AppendLine("| 파일 | Critical | Warning | Info | 합계 |");
        sb.AppendLine("|------|----------|---------|------|------|");

        var topFiles = report.IssuesByFile
            .OrderByDescending(x => x.Value.Count)
            .Take(10);

        foreach (var file in topFiles)
        {
            var criticalCount = file.Value.Count(i => i.Severity == Severity.Critical);
            var warningCount = file.Value.Count(i => i.Severity == Severity.Warning);
            var infoCount = file.Value.Count(i => i.Severity == Severity.Info);
            var total = file.Value.Count;

            var fileName = Path.GetFileName(file.Key);
            sb.AppendLine($"| {EscapeMarkdown(fileName)} | {criticalCount} | {warningCount} | {infoCount} | {total} |");
        }
        sb.AppendLine();
    }

    /// <summary>
    /// 푸터 추가
    /// </summary>
    private void AppendFooter(StringBuilder sb, QAReport report)
    {
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("*Generated by TwinCAT Code QA Tool - MarkdownReportGenerator*");
        sb.AppendLine($"*Report generated at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}*");
    }

    /// <summary>
    /// Markdown 특수 문자 이스케이프
    /// </summary>
    private string EscapeMarkdown(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        // Markdown 특수 문자 이스케이프
        return text
            .Replace("\\", "\\\\")
            .Replace("`", "\\`")
            .Replace("*", "\\*")
            .Replace("_", "\\_")
            .Replace("{", "\\{")
            .Replace("}", "\\}")
            .Replace("[", "\\[")
            .Replace("]", "\\]")
            .Replace("(", "\\(")
            .Replace(")", "\\)")
            .Replace("#", "\\#")
            .Replace("+", "\\+")
            .Replace("-", "\\-")
            .Replace(".", "\\.")
            .Replace("!", "\\!");
    }

    #endregion
}
