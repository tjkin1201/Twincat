using System.Text.Json;
using System.Text.Json;
using System.Text;
using TwinCatQA.Domain.Models.QA;

namespace TwinCatQA.Infrastructure.Reports;

/// <summary>
/// JSON 형식의 QA 리포트 생성기
/// CI/CD 파이프라인 연동 및 자동화 처리에 적합
/// </summary>
public class JsonReportGenerator
{
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// JsonReportGenerator 생성자
    /// </summary>
    /// <param name="prettyPrint">들여쓰기 적용 여부 (기본값: true)</param>
    public JsonReportGenerator(bool prettyPrint = true)
    {
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = prettyPrint,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <summary>
    /// JSON 리포트 생성
    /// </summary>
    /// <param name="report">QA 보고서</param>
    /// <returns>JSON 형식의 문자열</returns>
    public string Generate(QAReport report)
    {
        if (report == null)
            throw new ArgumentNullException(nameof(report), "QA 보고서가 null입니다.");

        var jsonReport = CreateJsonReport(report);
        return JsonSerializer.Serialize(jsonReport, _jsonOptions);
    }

    /// <summary>
    /// JSON 리포트를 파일로 저장
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
            outputPath = Path.Combine(reportsDir, $"qa_report_{report.GeneratedAt:yyyyMMdd_HHmmss}.json");
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

    /// <summary>
    /// JSON 리포트 객체를 직렬화하여 스트림에 저장
    /// </summary>
    /// <param name="report">QA 보고서</param>
    /// <param name="stream">출력 스트림</param>
    public async Task GenerateToStreamAsync(QAReport report, Stream stream)
    {
        if (report == null)
            throw new ArgumentNullException(nameof(report), "QA 보고서가 null입니다.");

        if (stream == null)
            throw new ArgumentNullException(nameof(stream), "출력 스트림이 null입니다.");

        var jsonReport = CreateJsonReport(report);
        await JsonSerializer.SerializeAsync(stream, jsonReport, _jsonOptions);
    }

    /// <summary>
    /// 요약 정보만 포함하는 간단한 JSON 생성
    /// </summary>
    /// <param name="report">QA 보고서</param>
    /// <returns>요약 정보만 포함하는 JSON 문자열</returns>
    public string GenerateSummary(QAReport report)
    {
        if (report == null)
            throw new ArgumentNullException(nameof(report), "QA 보고서가 null입니다.");

        var summary = new
        {
            generatedAt = report.GeneratedAt,
            sourceFolder = report.SourceFolder,
            targetFolder = report.TargetFolder,
            totalChanges = report.TotalChanges,
            totalIssues = report.TotalIssues,
            hasCriticalIssues = report.HasCriticalIssues,
            severityCounts = new
            {
                critical = report.CriticalCount,
                warning = report.WarningCount,
                info = report.InfoCount
            },
            topCategories = report.IssuesByCategory
                .OrderByDescending(x => x.Value.Count)
                .Take(5)
                .Select(x => new { category = x.Key, count = x.Value.Count })
                .ToList()
        };

        return JsonSerializer.Serialize(summary, _jsonOptions);
    }

    #region 내부 메서드

    /// <summary>
    /// JSON 리포트 객체 생성
    /// </summary>
    private object CreateJsonReport(QAReport report)
    {
        return new
        {
            metadata = new
            {
                generatedAt = report.GeneratedAt,
                generator = "TwinCAT Code QA Tool - JsonReportGenerator",
                version = "1.0.0"
            },
            project = new
            {
                sourceFolder = report.SourceFolder,
                targetFolder = report.TargetFolder,
                totalChanges = report.TotalChanges
            },
            summary = new
            {
                totalIssues = report.TotalIssues,
                hasCriticalIssues = report.HasCriticalIssues,
                severityCounts = new
                {
                    critical = report.CriticalCount,
                    warning = report.WarningCount,
                    info = report.InfoCount
                }
            },
            statistics = new
            {
                byCategory = report.IssuesByCategory
                    .Select(x => new
                    {
                        category = x.Key,
                        count = x.Value.Count,
                        percentage = Math.Round((double)x.Value.Count / report.TotalIssues * 100, 2)
                    })
                    .OrderByDescending(x => x.count)
                    .ToList(),
                byRule = report.IssuesByRule
                    .Select(x => new
                    {
                        ruleId = x.Key,
                        count = x.Value.Count
                    })
                    .OrderByDescending(x => x.count)
                    .ToList(),
                byFile = report.IssuesByFile
                    .Select(x => new
                    {
                        filePath = x.Key,
                        fileName = Path.GetFileName(x.Key),
                        totalIssues = x.Value.Count,
                        criticalCount = x.Value.Count(i => i.Severity == Severity.Critical),
                        warningCount = x.Value.Count(i => i.Severity == Severity.Warning),
                        infoCount = x.Value.Count(i => i.Severity == Severity.Info)
                    })
                    .OrderByDescending(x => x.totalIssues)
                    .ToList()
            },
            issues = report.Issues.Select(issue => new
            {
                ruleId = issue.RuleId,
                severity = issue.Severity.ToString(),
                category = issue.Category,
                title = issue.Title,
                description = issue.Description,
                location = new
                {
                    filePath = issue.FilePath,
                    fileName = Path.GetFileName(issue.FilePath),
                    line = issue.Line,
                    locationString = issue.Location
                },
                details = new
                {
                    whyDangerous = issue.WhyDangerous,
                    recommendation = issue.Recommendation,
                    oldCodeSnippet = issue.OldCodeSnippet,
                    newCodeSnippet = issue.NewCodeSnippet,
                    examples = issue.Examples
                }
            }).ToList()
        };
    }

    #endregion
}

/// <summary>
/// GitHub Actions 등 CI/CD 도구용 포맷터
/// </summary>
public static class CICDFormatter
{
    /// <summary>
    /// GitHub Actions 어노테이션 형식으로 변환
    /// </summary>
    /// <param name="report">QA 보고서</param>
    /// <returns>GitHub Actions 어노테이션 문자열</returns>
    public static string ToGitHubActionsAnnotations(QAReport report)
    {
        var sb = new StringBuilder();

        foreach (var issue in report.Issues)
        {
            var level = issue.Severity switch
            {
                Severity.Critical => "error",
                Severity.Warning => "warning",
                Severity.Info => "notice",
                _ => "notice"
            };

            // GitHub Actions 어노테이션 형식
            // ::error file={name},line={line},col={col}::{message}
            sb.AppendLine($"::{level} file={issue.FilePath},line={issue.Line}::[{issue.RuleId}] {issue.Title}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Azure DevOps 로그 형식으로 변환
    /// </summary>
    /// <param name="report">QA 보고서</param>
    /// <returns>Azure DevOps 로그 문자열</returns>
    public static string ToAzureDevOpsLog(QAReport report)
    {
        var sb = new StringBuilder();

        foreach (var issue in report.Issues)
        {
            var level = issue.Severity switch
            {
                Severity.Critical => "error",
                Severity.Warning => "warning",
                Severity.Info => "info",
                _ => "info"
            };

            // Azure DevOps 로그 형식
            // ##vso[task.logissue type=error;sourcepath={path};linenumber={line}]{message}
            sb.AppendLine($"##vso[task.logissue type={level};sourcepath={issue.FilePath};linenumber={issue.Line}][{issue.RuleId}] {issue.Title}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// JUnit XML 형식으로 변환 (Jenkins 등에서 사용)
    /// </summary>
    /// <param name="report">QA 보고서</param>
    /// <returns>JUnit XML 문자열</returns>
    public static string ToJUnitXml(QAReport report)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine($"<testsuite name=\"TwinCAT QA Analysis\" tests=\"{report.TotalIssues}\" failures=\"{report.CriticalCount + report.WarningCount}\" errors=\"0\" skipped=\"{report.InfoCount}\" timestamp=\"{report.GeneratedAt:O}\">");

        foreach (var file in report.IssuesByFile)
        {
            foreach (var issue in file.Value)
            {
                var className = Path.GetFileNameWithoutExtension(file.Key);
                var testName = $"[{issue.RuleId}] {issue.Title}";

                sb.AppendLine($"  <testcase classname=\"{className}\" name=\"{EscapeXml(testName)}\" time=\"0\">");

                if (issue.Severity == Severity.Critical || issue.Severity == Severity.Warning)
                {
                    sb.AppendLine($"    <failure message=\"{EscapeXml(issue.Title)}\" type=\"{issue.Severity}\">");
                    sb.AppendLine($"      <![CDATA[");
                    sb.AppendLine($"File: {issue.FilePath}");
                    sb.AppendLine($"Line: {issue.Line}");
                    sb.AppendLine($"Category: {issue.Category}");
                    sb.AppendLine($"Description: {issue.Description}");
                    if (!string.IsNullOrWhiteSpace(issue.WhyDangerous))
                        sb.AppendLine($"Why Dangerous: {issue.WhyDangerous}");
                    if (!string.IsNullOrWhiteSpace(issue.Recommendation))
                        sb.AppendLine($"Recommendation: {issue.Recommendation}");
                    sb.AppendLine($"      ]]>");
                    sb.AppendLine($"    </failure>");
                }
                else
                {
                    sb.AppendLine($"    <skipped message=\"{EscapeXml(issue.Title)}\" />");
                }

                sb.AppendLine($"  </testcase>");
            }
        }

        sb.AppendLine("</testsuite>");

        return sb.ToString();
    }

    /// <summary>
    /// XML 특수 문자 이스케이프
    /// </summary>
    private static string EscapeXml(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }
}
