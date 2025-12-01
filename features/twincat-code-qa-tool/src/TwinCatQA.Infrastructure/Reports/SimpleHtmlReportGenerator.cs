using System.Text;
using TwinCatQA.Domain.Contracts;
using TwinCatQA.Domain.Models;

namespace TwinCatQA.Infrastructure.Reports;

/// <summary>
/// 간단한 HTML 형식의 QA 보고서 생성기
/// GitHub PR 스타일의 인라인 코멘트 UI 제공
/// Razor 템플릿을 사용하지 않는 경량 버전
/// </summary>
public class SimpleHtmlReportGenerator : IReportGenerator
{
    /// <summary>
    /// HTML 리포트 생성
    /// </summary>
    public string GenerateHtmlReport(ValidationSession session, string? outputPath = null)
    {
        if (session == null)
            throw new ArgumentNullException(nameof(session), "검증 세션이 null입니다.");

        // 기본 출력 경로 설정
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            var reportsDir = Path.Combine(Directory.GetCurrentDirectory(), "reports");
            Directory.CreateDirectory(reportsDir);
            outputPath = Path.Combine(reportsDir, $"qa_report_{session.SessionId}_{DateTime.Now:yyyyMMdd_HHmmss}.html");
        }

        var html = BuildHtmlContent(session);

        // 파일 저장
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(outputPath, html);

        return Path.GetFullPath(outputPath);
    }

    /// <summary>
    /// PDF 리포트 생성 (미구현 - NotImplementedException)
    /// </summary>
    public string GeneratePdfReport(ValidationSession session, string? outputPath = null)
    {
        throw new NotImplementedException(
            "PDF 변환 기능은 상업용 라이선스가 필요합니다. " +
            "iText7 또는 다른 PDF 라이브러리를 통합하세요.");
    }

    /// <summary>
    /// 품질 추세 차트 생성
    /// </summary>
    public ChartData CreateQualityTrendChart(List<ValidationSession> sessions)
    {
        if (sessions == null || sessions.Count < 2)
        {
            return new ChartData
            {
                Title = "품질 추세",
                Type = "line",
                Labels = new List<string> { "데이터 부족" },
                Datasets = new List<ChartDataset>()
            };
        }

        var sortedSessions = sessions.OrderBy(s => s.StartTime).ToList();

        return new ChartData
        {
            Title = "품질 점수 추세",
            Type = "line",
            Labels = sortedSessions.Select(s => s.StartTime.ToString("MM/dd HH:mm")).ToList(),
            Datasets = new List<ChartDataset>
            {
                new ChartDataset
                {
                    Label = "품질 점수",
                    Data = sortedSessions.Select(s => s.OverallQualityScore).ToList(),
                    BackgroundColor = "rgba(54, 162, 235, 0.2)",
                    BorderColor = "rgba(54, 162, 235, 1)",
                    BorderWidth = 2
                }
            }
        };
    }

    /// <summary>
    /// 헌장 준수율 차트 생성
    /// </summary>
    public ChartData CreateConstitutionComplianceChart(ValidationSession session)
    {
        if (session == null)
            throw new ArgumentNullException(nameof(session));

        var principles = session.ConstitutionCompliance
            .OrderBy(kv => kv.Key)
            .ToList();

        return new ChartData
        {
            Title = "헌장 준수율",
            Type = "radar",
            Labels = principles.Select(p => p.Key.ToString()).ToList(),
            Datasets = new List<ChartDataset>
            {
                new ChartDataset
                {
                    Label = "준수율 (%)",
                    Data = principles.Select(p => p.Value * 100).ToList(),
                    BackgroundColor = "rgba(255, 99, 132, 0.2)",
                    BorderColor = "rgba(255, 99, 132, 1)",
                    BorderWidth = 2
                }
            }
        };
    }

    /// <summary>
    /// 위반 분포 차트 생성
    /// </summary>
    public ChartData CreateViolationDistributionChart(ValidationSession session)
    {
        if (session == null)
            throw new ArgumentNullException(nameof(session));

        var distribution = session.ViolationsBySeverity;

        return new ChartData
        {
            Title = "위반 사항 분포",
            Type = "pie",
            Labels = distribution.Keys.Select(k => k.ToString()).ToList(),
            Datasets = new List<ChartDataset>
            {
                new ChartDataset
                {
                    Label = "위반 건수",
                    Data = distribution.Values.Select(v => (double)v).ToList(),
                    BackgroundColor = "rgba(255, 159, 64, 0.6)",
                    BorderColor = "rgba(255, 159, 64, 1)",
                    BorderWidth = 1
                }
            }
        };
    }

    /// <summary>
    /// 코드 하이라이팅 (간단한 HTML 이스케이프만 적용)
    /// </summary>
    public string HighlightCode(string code, ProgrammingLanguage language)
    {
        if (string.IsNullOrWhiteSpace(code))
            return string.Empty;

        // 간단한 HTML 이스케이프만 적용
        var escaped = System.Security.SecurityElement.Escape(code);
        return $"<pre><code class=\"language-{language.ToString().ToLower()}\">{escaped}</code></pre>";
    }

    /// <summary>
    /// 커스텀 템플릿 설정 (미구현 - 간단한 버전이므로 템플릿 지원 안 함)
    /// </summary>
    public void SetCustomTemplate(string templateName, string customTemplatePath)
    {
        throw new NotImplementedException(
            "SimpleHtmlReportGenerator는 템플릿 커스터마이징을 지원하지 않습니다. " +
            "RazorReportGenerator를 사용하세요.");
    }

    #region HTML 생성 메서드

    private string BuildHtmlContent(ValidationSession session)
    {
        var html = new StringBuilder();

        AppendHtmlHeader(html);
        AppendHeaderSection(html, session);
        AppendSummaryCards(html, session);
        AppendIssuesByFile(html, session);
        AppendHtmlFooter(html);

        return html.ToString();
    }

    private void AppendHtmlHeader(StringBuilder html)
    {
        html.AppendLine(@"<!DOCTYPE html>
<html lang=""ko"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>TwinCAT 코드 QA 보고서</title>
    <style>
        :root {
            --critical-color: #d73a49;
            --high-color: #e36209;
            --medium-color: #dbab09;
            --low-color: #0366d6;
            --bg-color: #f6f8fa;
            --border-color: #e1e4e8;
            --text-primary: #24292e;
            --text-secondary: #586069;
        }

        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }

        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Noto Sans', Helvetica, Arial, sans-serif;
            line-height: 1.6;
            color: var(--text-primary);
            background: #ffffff;
            padding: 20px;
        }

        .container {
            max-width: 1200px;
            margin: 0 auto;
        }

        .header {
            border-bottom: 3px solid var(--border-color);
            padding-bottom: 20px;
            margin-bottom: 32px;
        }

        .header h1 {
            font-size: 32px;
            font-weight: 600;
            margin-bottom: 12px;
            color: var(--text-primary);
        }

        .header-info {
            display: flex;
            flex-wrap: wrap;
            gap: 16px;
            color: var(--text-secondary);
            font-size: 14px;
        }

        .header-info-item {
            display: flex;
            align-items: center;
            gap: 4px;
        }

        .summary-cards {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 16px;
            margin-bottom: 32px;
        }

        .card {
            background: white;
            padding: 20px;
            border-radius: 6px;
            border: 1px solid var(--border-color);
            box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);
            transition: transform 0.2s, box-shadow 0.2s;
        }

        .card:hover {
            transform: translateY(-2px);
            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
        }

        .card.critical { border-left: 4px solid var(--critical-color); }
        .card.high { border-left: 4px solid var(--high-color); }
        .card.medium { border-left: 4px solid var(--medium-color); }
        .card.low { border-left: 4px solid var(--low-color); }

        .card-label {
            font-size: 13px;
            font-weight: 500;
            color: var(--text-secondary);
            text-transform: uppercase;
            letter-spacing: 0.5px;
            margin-bottom: 8px;
        }

        .card-value {
            font-size: 36px;
            font-weight: 600;
            color: var(--text-primary);
        }

        .card-footer {
            margin-top: 8px;
            font-size: 12px;
            color: var(--text-secondary);
        }

        .file-section {
            margin-bottom: 32px;
        }

        .file-header {
            background: var(--bg-color);
            padding: 14px 18px;
            border: 1px solid var(--border-color);
            border-radius: 6px 6px 0 0;
            font-family: 'SFMono-Regular', Consolas, 'Liberation Mono', Menlo, monospace;
            font-size: 14px;
            font-weight: 600;
            display: flex;
            justify-content: space-between;
            align-items: center;
            cursor: pointer;
            user-select: none;
        }

        .file-header:hover {
            background: #e8ecef;
        }

        .file-header:after {
            content: '▼';
            font-size: 11px;
            transition: transform 0.2s;
        }

        .file-header.collapsed:after {
            transform: rotate(-90deg);
        }

        .file-content {
            border: 1px solid var(--border-color);
            border-top: none;
            border-radius: 0 0 6px 6px;
            overflow: hidden;
        }

        .file-content.hidden {
            display: none;
        }

        .issue {
            padding: 20px;
            border-bottom: 1px solid var(--border-color);
            background: white;
        }

        .issue:last-child {
            border-bottom: none;
        }

        .issue.critical { border-left: 4px solid var(--critical-color); }
        .issue.high { border-left: 4px solid var(--high-color); }
        .issue.medium { border-left: 4px solid var(--medium-color); }
        .issue.low { border-left: 4px solid var(--low-color); }

        .issue-header {
            display: flex;
            justify-content: space-between;
            align-items: flex-start;
            margin-bottom: 12px;
            gap: 16px;
        }

        .issue-title {
            font-size: 16px;
            font-weight: 600;
            color: var(--text-primary);
            flex: 1;
        }

        .severity-badge {
            padding: 4px 10px;
            border-radius: 12px;
            font-size: 12px;
            font-weight: 600;
            color: white;
            white-space: nowrap;
        }

        .severity-badge.critical { background: var(--critical-color); }
        .severity-badge.high { background: var(--high-color); }
        .severity-badge.medium { background: var(--medium-color); }
        .severity-badge.low { background: var(--low-color); }

        .issue-location {
            font-family: 'SFMono-Regular', Consolas, monospace;
            font-size: 13px;
            color: var(--text-secondary);
            margin-bottom: 12px;
        }

        .issue-message {
            font-size: 14px;
            line-height: 1.6;
            color: var(--text-primary);
            margin-bottom: 16px;
        }

        .code-snippet {
            background: #1b1f23;
            color: #e1e4e8;
            padding: 16px;
            border-radius: 6px;
            font-family: 'SFMono-Regular', Consolas, monospace;
            font-size: 13px;
            overflow-x: auto;
            margin-bottom: 16px;
            line-height: 1.5;
        }

        .recommendation {
            background: #ddf4ff;
            border: 1px solid #54aeff;
            padding: 16px;
            border-radius: 6px;
            margin-top: 16px;
        }

        .recommendation-title {
            font-weight: 600;
            margin-bottom: 8px;
            color: #0969da;
        }

        .recommendation-content {
            font-size: 14px;
            line-height: 1.6;
            color: var(--text-primary);
        }

        .footer {
            margin-top: 48px;
            padding-top: 24px;
            border-top: 1px solid var(--border-color);
            text-align: center;
            color: var(--text-secondary);
            font-size: 13px;
        }

        @media (max-width: 768px) {
            .summary-cards {
                grid-template-columns: 1fr;
            }

            .header-info {
                flex-direction: column;
            }

            .issue-header {
                flex-direction: column;
                align-items: flex-start;
            }
        }
    </style>
</head>
<body>
    <div class=""container"">");
    }

    private void AppendHeaderSection(StringBuilder html, ValidationSession session)
    {
        html.AppendLine($@"
        <div class=""header"">
            <h1>🔍 TwinCAT 코드 QA 보고서</h1>
            <div class=""header-info"">
                <div class=""header-info-item"">
                    <span>📅</span>
                    <span>{session.StartTime:yyyy-MM-dd HH:mm:ss}</span>
                </div>
                <div class=""header-info-item"">
                    <span>📦</span>
                    <span>{Escape(session.ProjectName)}</span>
                </div>
                <div class=""header-info-item"">
                    <span>📁</span>
                    <span>{Escape(session.ProjectPath)}</span>
                </div>
                <div class=""header-info-item"">
                    <span>👤</span>
                    <span>{Escape(session.ExecutedBy)}</span>
                </div>
                <div class=""header-info-item"">
                    <span>⏱️</span>
                    <span>{session.Duration.TotalSeconds:F1}초</span>
                </div>
            </div>
        </div>");
    }

    private void AppendSummaryCards(StringBuilder html, ValidationSession session)
    {
        var criticalCount = session.ViolationsBySeverity.GetValueOrDefault(ViolationSeverity.Critical, 0);
        var highCount = session.ViolationsBySeverity.GetValueOrDefault(ViolationSeverity.High, 0);
        var mediumCount = session.ViolationsBySeverity.GetValueOrDefault(ViolationSeverity.Medium, 0);
        var lowCount = session.ViolationsBySeverity.GetValueOrDefault(ViolationSeverity.Low, 0);

        html.AppendLine($@"
        <div class=""summary-cards"">
            <div class=""card critical"">
                <div class=""card-label"">Critical</div>
                <div class=""card-value"">{criticalCount}</div>
                <div class=""card-footer"">치명적 문제</div>
            </div>
            <div class=""card high"">
                <div class=""card-label"">High</div>
                <div class=""card-value"">{highCount}</div>
                <div class=""card-footer"">높은 우선순위</div>
            </div>
            <div class=""card medium"">
                <div class=""card-label"">Medium</div>
                <div class=""card-value"">{mediumCount}</div>
                <div class=""card-footer"">중간 우선순위</div>
            </div>
            <div class=""card low"">
                <div class=""card-label"">Low</div>
                <div class=""card-value"">{lowCount}</div>
                <div class=""card-footer"">낮은 우선순위</div>
            </div>
            <div class=""card"">
                <div class=""card-label"">품질 점수</div>
                <div class=""card-value"">{session.OverallQualityScore:F0}</div>
                <div class=""card-footer"">100점 만점</div>
            </div>
            <div class=""card"">
                <div class=""card-label"">총 위반</div>
                <div class=""card-value"">{session.ViolationsCount}</div>
                <div class=""card-footer"">{session.ScannedFilesCount}개 파일 스캔</div>
            </div>
        </div>");
    }

    private void AppendIssuesByFile(StringBuilder html, ValidationSession session)
    {
        var violationsByFile = session.Violations
            .GroupBy(v => v.FilePath)
            .OrderByDescending(g => g.Count());

        foreach (var fileGroup in violationsByFile)
        {
            var fileName = Path.GetFileName(fileGroup.Key);
            var issueCount = fileGroup.Count();

            html.AppendLine($@"
        <div class=""file-section"">
            <div class=""file-header"" onclick=""toggleContent(this)"">
                <span>📄 {Escape(fileName)} ({issueCount}개 이슈)</span>
            </div>
            <div class=""file-content"">");

            foreach (var violation in fileGroup.OrderByDescending(v => v.Severity))
            {
                var severityClass = violation.Severity.ToString().ToLower();

                html.AppendLine($@"
                <div class=""issue {severityClass}"">
                    <div class=""issue-header"">
                        <div class=""issue-title"">[{Escape(violation.RuleId)}] {Escape(violation.RuleName)}</div>
                        <span class=""severity-badge {severityClass}"">{violation.Severity}</span>
                    </div>
                    <div class=""issue-location"">📍 라인 {violation.Line}, 컬럼 {violation.Column}</div>
                    <div class=""issue-message"">{Escape(violation.Message)}</div>");

                if (!string.IsNullOrWhiteSpace(violation.CodeSnippet))
                {
                    html.AppendLine($@"
                    <div class=""code-snippet"">{Escape(violation.CodeSnippet)}</div>");
                }

                if (!string.IsNullOrWhiteSpace(violation.Suggestion))
                {
                    html.AppendLine($@"
                    <div class=""recommendation"">
                        <div class=""recommendation-title"">✅ 권장 해결 방법</div>
                        <div class=""recommendation-content"">{Escape(violation.Suggestion)}</div>
                    </div>");
                }

                html.AppendLine(@"
                </div>");
            }

            html.AppendLine(@"
            </div>
        </div>");
        }
    }

    private void AppendHtmlFooter(StringBuilder html)
    {
        html.AppendLine($@"
        <div class=""footer"">
            <p>TwinCAT Code QA Tool - Generated by SimpleHtmlReportGenerator</p>
            <p>Generated at {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
        </div>
    </div>

    <script>
        function toggleContent(element) {{
            element.classList.toggle('collapsed');
            element.nextElementSibling.classList.toggle('hidden');
        }}

        // 초기 로드 시 모든 파일 섹션 펼치기
        document.addEventListener('DOMContentLoaded', function() {{
            console.log('TwinCAT QA Report loaded successfully');
        }});
    </script>
</body>
</html>");
    }

    private static string Escape(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return System.Security.SecurityElement.Escape(text);
    }

    #endregion
}
