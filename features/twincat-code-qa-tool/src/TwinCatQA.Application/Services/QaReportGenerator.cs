using System.Text;
using System.Text.Json;
using TwinCatQA.Domain.Models.QA;
using TwinCatQA.Infrastructure.Reports;

namespace TwinCatQA.Application.Services;

/// <summary>
/// QA 분석 보고서 생성기
/// HTML, Markdown, JSON 형식 지원
/// </summary>
public class QaReportGenerator
{
    /// <summary>
    /// 보고서 생성
    /// </summary>
    public async Task<List<string>> GenerateReportsAsync(
        QaAnalysisResult result,
        string outputPath,
        ReportFormat format)
    {
        var generatedFiles = new List<string>();
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        Directory.CreateDirectory(outputPath);

        if (format == ReportFormat.All || format == ReportFormat.Html)
        {
            var htmlFile = Path.Combine(outputPath, $"qa_report_{timestamp}.html");
            await File.WriteAllTextAsync(htmlFile, GenerateHtmlReport(result));
            generatedFiles.Add(htmlFile);
        }

        if (format == ReportFormat.All || format == ReportFormat.Markdown)
        {
            var mdFile = Path.Combine(outputPath, $"qa_report_{timestamp}.md");
            await File.WriteAllTextAsync(mdFile, GenerateMarkdownReport(result));
            generatedFiles.Add(mdFile);
        }

        if (format == ReportFormat.All || format == ReportFormat.Json)
        {
            var jsonFile = Path.Combine(outputPath, $"qa_report_{timestamp}.json");
            await File.WriteAllTextAsync(jsonFile, GenerateJsonReport(result));
            generatedFiles.Add(jsonFile);
        }

        if (format == ReportFormat.All || format == ReportFormat.Excel)
        {
            var excelFile = Path.Combine(outputPath, $"qa_report_{timestamp}.xlsx");
            GenerateExcelReport(result, excelFile);
            generatedFiles.Add(excelFile);
        }

        return generatedFiles;
    }

    /// <summary>
    /// HTML 보고서 생성
    /// </summary>
    private string GenerateHtmlReport(QaAnalysisResult result)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"ko\">");
        sb.AppendLine("<head>");
        sb.AppendLine("    <meta charset=\"UTF-8\">");
        sb.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine("    <title>TwinCAT 코드 QA 분석 보고서</title>");
        sb.AppendLine("    <style>");
        sb.AppendLine("        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 20px; background: #f5f5f5; }");
        sb.AppendLine("        .container { max-width: 1200px; margin: 0 auto; background: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }");
        sb.AppendLine("        h1 { color: #2c3e50; border-bottom: 3px solid #3498db; padding-bottom: 10px; }");
        sb.AppendLine("        h2 { color: #34495e; margin-top: 30px; }");
        sb.AppendLine("        .summary { display: flex; gap: 20px; margin: 20px 0; }");
        sb.AppendLine("        .summary-box { flex: 1; padding: 20px; border-radius: 5px; text-align: center; }");
        sb.AppendLine("        .critical { background: #fee; border-left: 4px solid #e74c3c; }");
        sb.AppendLine("        .warning { background: #ffeaa7; border-left: 4px solid #f39c12; }");
        sb.AppendLine("        .info { background: #e3f2fd; border-left: 4px solid #3498db; }");
        sb.AppendLine("        .issue { border: 1px solid #ddd; margin: 15px 0; padding: 15px; border-radius: 5px; }");
        sb.AppendLine("        .issue-header { font-weight: bold; margin-bottom: 10px; }");
        sb.AppendLine("        .badge { display: inline-block; padding: 5px 10px; border-radius: 3px; font-size: 0.9em; margin-right: 10px; }");
        sb.AppendLine("        .badge-critical { background: #e74c3c; color: white; }");
        sb.AppendLine("        .badge-warning { background: #f39c12; color: white; }");
        sb.AppendLine("        .badge-info { background: #3498db; color: white; }");
        sb.AppendLine("        .code { background: #f8f9fa; border: 1px solid #dee2e6; padding: 10px; border-radius: 4px; overflow-x: auto; margin: 10px 0; font-family: 'Courier New', monospace; font-size: 0.9em; }");
        sb.AppendLine("        .location { color: #7f8c8d; font-size: 0.9em; }");
        sb.AppendLine("        .recommendation { background: #d5f4e6; border-left: 4px solid #27ae60; padding: 10px; margin: 10px 0; }");
        sb.AppendLine("        table { width: 100%; border-collapse: collapse; margin: 20px 0; }");
        sb.AppendLine("        th, td { border: 1px solid #ddd; padding: 12px; text-align: left; }");
        sb.AppendLine("        th { background: #34495e; color: white; }");
        sb.AppendLine("        tr:nth-child(even) { background: #f8f9fa; }");
        sb.AppendLine("    </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("    <div class=\"container\">");
        sb.AppendLine($"        <h1>🔍 TwinCAT 코드 QA 분석 보고서</h1>");
        sb.AppendLine($"        <p><strong>생성 일시:</strong> {result.EndTime:yyyy-MM-dd HH:mm:ss}</p>");
        sb.AppendLine($"        <p><strong>분석 시간:</strong> {result.Duration.TotalSeconds:F2}초</p>");
        sb.AppendLine($"        <p><strong>이전 버전:</strong> {result.OldFolderPath}</p>");
        sb.AppendLine($"        <p><strong>신규 버전:</strong> {result.NewFolderPath}</p>");

        // 요약
        sb.AppendLine("        <h2>📊 이슈 요약</h2>");
        sb.AppendLine("        <div class=\"summary\">");
        sb.AppendLine($"            <div class=\"summary-box critical\">");
        sb.AppendLine($"                <h3>🔴 Critical</h3>");
        sb.AppendLine($"                <h1>{result.CriticalCount}</h1>");
        sb.AppendLine($"            </div>");
        sb.AppendLine($"            <div class=\"summary-box warning\">");
        sb.AppendLine($"                <h3>🟡 Warning</h3>");
        sb.AppendLine($"                <h1>{result.WarningCount}</h1>");
        sb.AppendLine($"            </div>");
        sb.AppendLine($"            <div class=\"summary-box info\">");
        sb.AppendLine($"                <h3>🔵 Info</h3>");
        sb.AppendLine($"                <h1>{result.InfoCount}</h1>");
        sb.AppendLine($"            </div>");
        sb.AppendLine("        </div>");

        // 변경 사항 통계
        sb.AppendLine("        <h2>📈 변경 사항 통계</h2>");
        sb.AppendLine("        <table>");
        sb.AppendLine("            <tr><th>항목</th><th>개수</th></tr>");
        sb.AppendLine($"            <tr><td>변수 변경</td><td>{result.ComparisonResult.VariableChanges.Count}</td></tr>");
        sb.AppendLine($"            <tr><td>로직 변경</td><td>{result.ComparisonResult.LogicChanges.Count}</td></tr>");
        sb.AppendLine($"            <tr><td>데이터 타입 변경</td><td>{result.ComparisonResult.DataTypeChanges.Count}</td></tr>");
        sb.AppendLine("        </table>");

        // 이슈 상세
        if (result.Issues.Any())
        {
            sb.AppendLine("        <h2>🚨 이슈 상세</h2>");

            foreach (var issue in result.Issues.OrderByDescending(i => i.Severity))
            {
                var severityClass = issue.Severity.ToString().ToLower();
                var severityBadge = issue.Severity switch
                {
                    Severity.Critical => "<span class=\"badge badge-critical\">🔴 Critical</span>",
                    Severity.Warning => "<span class=\"badge badge-warning\">🟡 Warning</span>",
                    _ => "<span class=\"badge badge-info\">🔵 Info</span>"
                };

                sb.AppendLine($"        <div class=\"issue {severityClass}\">");
                sb.AppendLine($"            <div class=\"issue-header\">");
                sb.AppendLine($"                {severityBadge}");
                sb.AppendLine($"                [{issue.RuleId}] {issue.Title}");
                sb.AppendLine($"            </div>");
                sb.AppendLine($"            <p><strong>카테고리:</strong> {issue.Category}</p>");
                sb.AppendLine($"            <p class=\"location\">📍 {issue.Location}</p>");
                sb.AppendLine($"            <p><strong>설명:</strong> {issue.Description}</p>");

                if (!string.IsNullOrEmpty(issue.WhyDangerous))
                {
                    sb.AppendLine($"            <p><strong>⚠️ 위험성:</strong></p>");
                    sb.AppendLine($"            <div class=\"code\">{System.Net.WebUtility.HtmlEncode(issue.WhyDangerous.Trim())}</div>");
                }

                if (!string.IsNullOrEmpty(issue.Recommendation))
                {
                    sb.AppendLine($"            <div class=\"recommendation\">");
                    sb.AppendLine($"                <strong>💡 권장 해결 방법:</strong>");
                    sb.AppendLine($"                <pre>{System.Net.WebUtility.HtmlEncode(issue.Recommendation.Trim())}</pre>");
                    sb.AppendLine($"            </div>");
                }

                if (issue.Examples?.Any() == true)
                {
                    sb.AppendLine($"            <p><strong>예시:</strong></p>");
                    sb.AppendLine($"            <div class=\"code\">");
                    foreach (var example in issue.Examples)
                    {
                        sb.AppendLine($"                {System.Net.WebUtility.HtmlEncode(example)}<br>");
                    }
                    sb.AppendLine($"            </div>");
                }

                sb.AppendLine($"        </div>");
            }
        }
        else
        {
            sb.AppendLine("        <p>✅ 발견된 이슈가 없습니다!</p>");
        }

        sb.AppendLine("    </div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    /// <summary>
    /// Markdown 보고서 생성
    /// </summary>
    private string GenerateMarkdownReport(QaAnalysisResult result)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# 🔍 TwinCAT 코드 QA 분석 보고서");
        sb.AppendLine();
        sb.AppendLine($"**생성 일시:** {result.EndTime:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"**분석 시간:** {result.Duration.TotalSeconds:F2}초");
        sb.AppendLine($"**이전 버전:** `{result.OldFolderPath}`");
        sb.AppendLine($"**신규 버전:** `{result.NewFolderPath}`");
        sb.AppendLine();

        // 요약
        sb.AppendLine("## 📊 이슈 요약");
        sb.AppendLine();
        sb.AppendLine("| 심각도 | 개수 |");
        sb.AppendLine("|--------|------|");
        sb.AppendLine($"| 🔴 Critical | {result.CriticalCount} |");
        sb.AppendLine($"| 🟡 Warning | {result.WarningCount} |");
        sb.AppendLine($"| 🔵 Info | {result.InfoCount} |");
        sb.AppendLine();

        // 변경 사항
        sb.AppendLine("## 📈 변경 사항 통계");
        sb.AppendLine();
        sb.AppendLine("| 항목 | 개수 |");
        sb.AppendLine("|------|------|");
        sb.AppendLine($"| 변수 변경 | {result.ComparisonResult.VariableChanges.Count} |");
        sb.AppendLine($"| 로직 변경 | {result.ComparisonResult.LogicChanges.Count} |");
        sb.AppendLine($"| 데이터 타입 변경 | {result.ComparisonResult.DataTypeChanges.Count} |");
        sb.AppendLine();

        // 이슈 상세
        if (result.Issues.Any())
        {
            sb.AppendLine("## 🚨 이슈 상세");
            sb.AppendLine();

            foreach (var issue in result.Issues.OrderByDescending(i => i.Severity))
            {
                var severityIcon = issue.Severity switch
                {
                    Severity.Critical => "🔴",
                    Severity.Warning => "🟡",
                    _ => "🔵"
                };

                sb.AppendLine($"### {severityIcon} [{issue.RuleId}] {issue.Title}");
                sb.AppendLine();
                sb.AppendLine($"**심각도:** {issue.Severity} | **카테고리:** {issue.Category}");
                sb.AppendLine($"**위치:** `{issue.Location}`");
                sb.AppendLine();
                sb.AppendLine($"**설명:** {issue.Description}");
                sb.AppendLine();

                if (!string.IsNullOrEmpty(issue.Recommendation))
                {
                    sb.AppendLine("**💡 권장 해결 방법:**");
                    sb.AppendLine("```");
                    sb.AppendLine(issue.Recommendation.Trim());
                    sb.AppendLine("```");
                    sb.AppendLine();
                }

                if (issue.Examples?.Any() == true)
                {
                    sb.AppendLine("**예시:**");
                    sb.AppendLine("```iecst");
                    foreach (var example in issue.Examples)
                    {
                        sb.AppendLine(example);
                    }
                    sb.AppendLine("```");
                    sb.AppendLine();
                }

                sb.AppendLine("---");
                sb.AppendLine();
            }
        }
        else
        {
            sb.AppendLine("✅ 발견된 이슈가 없습니다!");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Excel 보고서 생성 (EPPlus 사용)
    /// </summary>
    private void GenerateExcelReport(QaAnalysisResult result, string outputPath)
    {
        // QaAnalysisResult를 QAReport로 변환
        var qaReport = new QAReport
        {
            GeneratedAt = result.EndTime,
            SourceFolder = result.OldFolderPath,
            TargetFolder = result.NewFolderPath,
            TotalChanges = result.ComparisonResult.VariableChanges.Count +
                          result.ComparisonResult.LogicChanges.Count +
                          result.ComparisonResult.DataTypeChanges.Count
        };

        // 이슈 복사
        foreach (var issue in result.Issues)
        {
            qaReport.Issues.Add(issue);
        }

        // Excel 리포트 생성
        var excelGenerator = new ExcelReportGenerator();
        excelGenerator.GenerateReport(qaReport, outputPath);
    }

    /// <summary>
    /// JSON 보고서 생성
    /// </summary>
    private string GenerateJsonReport(QaAnalysisResult result)
    {
        var reportData = new
        {
            generatedAt = result.EndTime,
            analysisTime = result.Duration.TotalSeconds,
            oldFolder = result.OldFolderPath,
            newFolder = result.NewFolderPath,
            summary = new
            {
                total = result.Issues.Count,
                critical = result.CriticalCount,
                warning = result.WarningCount,
                info = result.InfoCount
            },
            changes = new
            {
                variables = result.ComparisonResult.VariableChanges.Count,
                logic = result.ComparisonResult.LogicChanges.Count,
                dataTypes = result.ComparisonResult.DataTypeChanges.Count
            },
            issues = result.Issues.OrderByDescending(i => i.Severity).Select(i => new
            {
                ruleId = i.RuleId,
                severity = i.Severity.ToString(),
                category = i.Category,
                title = i.Title,
                description = i.Description,
                location = i.Location,
                filePath = i.FilePath,
                line = i.Line,
                whyDangerous = i.WhyDangerous,
                recommendation = i.Recommendation,
                examples = i.Examples
            })
        };

        return JsonSerializer.Serialize(reportData, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
    }
}

/// <summary>
/// 보고서 형식
/// </summary>
public enum ReportFormat
{
    Html,
    Markdown,
    Json,
    Excel,
    All
}
