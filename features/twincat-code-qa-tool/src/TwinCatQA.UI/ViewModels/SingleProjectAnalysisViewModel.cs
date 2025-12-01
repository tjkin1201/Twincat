using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Ookii.Dialogs.Wpf;
using TwinCatQA.Domain.Models.QA;
using TwinCatQA.Application.Services;

namespace TwinCatQA.UI.ViewModels;

/// <summary>
/// 단일 프로젝트 QA 분석 ViewModel
/// </summary>
public partial class SingleProjectAnalysisViewModel : ObservableObject
{
    private readonly ILogger<SingleProjectAnalysisViewModel> _logger;

    [ObservableProperty]
    private string _projectFolderPath = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "프로젝트 폴더를 선택하고 분석을 시작하세요.";

    [ObservableProperty]
    private bool _isAnalyzing = false;

    [ObservableProperty]
    private bool _hasResults = false;

    [ObservableProperty]
    private int _totalFiles = 0;

    [ObservableProperty]
    private int _totalLines = 0;

    [ObservableProperty]
    private int _totalIssues = 0;

    [ObservableProperty]
    private int _criticalCount = 0;

    [ObservableProperty]
    private int _warningCount = 0;

    [ObservableProperty]
    private int _infoCount = 0;

    // 분석 결과 저장
    private SingleProjectAnalysisResult? _analysisResult;

    public ObservableCollection<QAIssueDisplay> CriticalIssues { get; } = new();
    public ObservableCollection<QAIssueDisplay> WarningIssues { get; } = new();
    public ObservableCollection<FileStatDisplay> FileStats { get; } = new();
    public ObservableCollection<RuleStatDisplay> RuleStatistics { get; } = new();
    public ObservableCollection<FileStatDisplay> HighComplexityFiles { get; } = new();

    public SingleProjectAnalysisViewModel()
    {
        _logger = NullLogger<SingleProjectAnalysisViewModel>.Instance;
    }

    public SingleProjectAnalysisViewModel(ILogger<SingleProjectAnalysisViewModel> logger)
    {
        _logger = logger;
    }

    [RelayCommand]
    private void BrowseFolder()
    {
        var dialog = new VistaFolderBrowserDialog
        {
            Description = "TwinCAT 프로젝트 폴더를 선택하세요",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };

        if (!string.IsNullOrEmpty(ProjectFolderPath) && Directory.Exists(ProjectFolderPath))
        {
            dialog.SelectedPath = ProjectFolderPath;
        }

        if (dialog.ShowDialog() == true)
        {
            ProjectFolderPath = dialog.SelectedPath;
            StatusMessage = $"프로젝트 폴더 선택됨: {ProjectFolderPath}";
            _logger.LogInformation("프로젝트 폴더 선택: {Path}", ProjectFolderPath);
        }
    }

    [RelayCommand(CanExecute = nameof(CanAnalyze))]
    private async Task AnalyzeAsync()
    {
        if (!ValidateFolderPath())
        {
            return;
        }

        IsAnalyzing = true;
        HasResults = false;
        StatusMessage = "프로젝트를 분석하는 중입니다...";
        ClearResults();

        try
        {
            _logger.LogInformation("단일 프로젝트 분석 시작: {Path}", ProjectFolderPath);

            // 분석 서비스 실행
            var analyzer = new SingleProjectAnalyzer();
            _analysisResult = await Task.Run(() => analyzer.Analyze(ProjectFolderPath));

            // 결과를 UI에 바인딩
            PopulateResults(_analysisResult);

            HasResults = true;
            StatusMessage = $"분석 완료: {TotalIssues}개의 이슈 발견 (Critical: {CriticalCount}, Warning: {WarningCount}, Info: {InfoCount})";
            _logger.LogInformation("분석 완료: {TotalIssues}개 이슈", TotalIssues);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "분석 중 오류 발생");
            StatusMessage = $"오류: {ex.Message}";

            MessageBox.Show(
                $"분석 중 오류가 발생했습니다:\n\n{ex.Message}",
                "오류",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            IsAnalyzing = false;
        }
    }

    private bool CanAnalyze()
    {
        return !string.IsNullOrWhiteSpace(ProjectFolderPath) && !IsAnalyzing;
    }

    private bool ValidateFolderPath()
    {
        if (string.IsNullOrWhiteSpace(ProjectFolderPath))
        {
            MessageBox.Show("프로젝트 폴더를 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (!Directory.Exists(ProjectFolderPath))
        {
            MessageBox.Show($"폴더가 존재하지 않습니다:\n{ProjectFolderPath}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        return true;
    }

    private void ClearResults()
    {
        CriticalIssues.Clear();
        WarningIssues.Clear();
        FileStats.Clear();
        RuleStatistics.Clear();
        HighComplexityFiles.Clear();

        TotalFiles = 0;
        TotalLines = 0;
        TotalIssues = 0;
        CriticalCount = 0;
        WarningCount = 0;
        InfoCount = 0;
    }

    private void PopulateResults(SingleProjectAnalysisResult result)
    {
        // 요약 통계
        TotalFiles = result.TotalFiles;
        TotalLines = result.TotalLines;
        TotalIssues = result.Issues.Count;
        CriticalCount = result.Issues.Count(i => i.Severity == "Critical");
        WarningCount = result.Issues.Count(i => i.Severity == "Warning");
        InfoCount = result.Issues.Count(i => i.Severity == "Info");

        // Critical 이슈
        foreach (var issue in result.Issues.Where(i => i.Severity == "Critical").Take(500))
        {
            CriticalIssues.Add(new QAIssueDisplay
            {
                FileName = issue.FileName,
                LineNumber = issue.LineNumber,
                RuleId = issue.RuleId,
                Message = issue.Message,
                CodeSnippet = issue.CodeSnippet?.Length > 100 ? issue.CodeSnippet.Substring(0, 100) + "..." : issue.CodeSnippet ?? ""
            });
        }

        // Warning 이슈
        foreach (var issue in result.Issues.Where(i => i.Severity == "Warning").Take(500))
        {
            WarningIssues.Add(new QAIssueDisplay
            {
                FileName = issue.FileName,
                LineNumber = issue.LineNumber,
                RuleId = issue.RuleId,
                Message = issue.Message,
                CodeSnippet = issue.CodeSnippet?.Length > 100 ? issue.CodeSnippet.Substring(0, 100) + "..." : issue.CodeSnippet ?? ""
            });
        }

        // 파일 통계
        foreach (var file in result.Files.OrderByDescending(f => f.IssueCount).Take(200))
        {
            FileStats.Add(new FileStatDisplay
            {
                FileName = file.FileName,
                FileType = file.FileType,
                PouType = file.PouType,
                LineCount = file.LineCount,
                Complexity = file.Complexity,
                IssueCount = file.IssueCount
            });
        }

        // 규칙별 통계
        var ruleGroups = result.Issues.GroupBy(i => i.RuleId)
            .Select(g => new
            {
                RuleId = g.Key,
                Count = g.Count(),
                Severity = g.First().Severity,
                Category = g.First().Category,
                Description = GetRuleDescription(g.Key)
            })
            .OrderByDescending(r => r.Count);

        foreach (var rule in ruleGroups)
        {
            RuleStatistics.Add(new RuleStatDisplay
            {
                RuleId = rule.RuleId,
                Severity = rule.Severity,
                Category = rule.Category,
                Count = rule.Count,
                Description = rule.Description
            });
        }

        // 복잡도 높은 파일
        foreach (var file in result.Files.OrderByDescending(f => f.Complexity).Take(20))
        {
            HighComplexityFiles.Add(new FileStatDisplay
            {
                FileName = file.FileName,
                FileType = file.FileType,
                PouType = file.PouType,
                LineCount = file.LineCount,
                Complexity = file.Complexity,
                IssueCount = file.IssueCount
            });
        }
    }

    private string GetRuleDescription(string ruleId)
    {
        return ruleId switch
        {
            "QA001" => "초기화되지 않은 중요 변수 (REAL/LREAL/포인터)",
            "QA002" => "위험한 타입 변환 (정밀도 손실 가능)",
            "QA003" => "큰 배열/구조체 반복 복사",
            "QA004" => "배열 인덱스 범위 초과 가능성",
            "QA005" => "실수형 직접 등호 비교",
            "QA006" => "0으로 나누기 가능성",
            "QA007" => "매직 넘버 사용",
            "QA008" => "CASE 문에 ELSE 없음",
            "QA009" => "깊은 중첩 (depth > 4)",
            "QA010" => "긴 함수 (100줄 초과)",
            "QA011" => "사용되지 않는 변수",
            "QA012" => "하드코딩된 I/O 주소",
            "QA013" => "주석 부족",
            "QA014" => "높은 순환 복잡도",
            "QA015" => "중복 코드",
            "QA016" => "명명 규칙 위반",
            "QA017" => "너무 많은 매개변수",
            "QA018" => "NULL 체크 누락",
            "QA019" => "무한 루프 위험",
            "QA020" => "전역 변수 과다 사용",
            _ => "알 수 없는 규칙"
        };
    }

    [RelayCommand]
    private void ExportHtml()
    {
        if (_analysisResult == null) return;

        var dialog = new VistaSaveFileDialog
        {
            Filter = "HTML Files (*.html)|*.html",
            DefaultExt = "html",
            FileName = $"qa_report_{DateTime.Now:yyyyMMdd_HHmmss}.html"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var html = GenerateHtmlReport(_analysisResult);
                File.WriteAllText(dialog.FileName, html, System.Text.Encoding.UTF8);
                StatusMessage = $"HTML 리포트 저장 완료: {dialog.FileName}";

                // 브라우저에서 열기
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = dialog.FileName,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"HTML 저장 중 오류:\n{ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    [RelayCommand]
    private void ExportJson()
    {
        if (_analysisResult == null) return;

        var dialog = new VistaSaveFileDialog
        {
            Filter = "JSON Files (*.json)|*.json",
            DefaultExt = "json",
            FileName = $"qa_report_{DateTime.Now:yyyyMMdd_HHmmss}.json"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var json = JsonSerializer.Serialize(_analysisResult, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
                File.WriteAllText(dialog.FileName, json, System.Text.Encoding.UTF8);
                StatusMessage = $"JSON 저장 완료: {dialog.FileName}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"JSON 저장 중 오류:\n{ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private string GenerateHtmlReport(SingleProjectAnalysisResult result)
    {
        return $@"<!DOCTYPE html>
<html lang=""ko"">
<head>
    <meta charset=""UTF-8"">
    <title>TwinCAT QA 분석 리포트</title>
    <style>
        :root {{
            --critical-color: #dc3545;
            --warning-color: #ffc107;
            --info-color: #17a2b8;
            --bg-dark: #1a1a2e;
            --bg-card: #16213e;
        }}
        * {{ box-sizing: border-box; margin: 0; padding: 0; }}
        body {{
            font-family: 'Segoe UI', sans-serif;
            background: var(--bg-dark);
            color: #eee;
            line-height: 1.6;
        }}
        .container {{ max-width: 1400px; margin: 0 auto; padding: 20px; }}
        header {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            padding: 30px;
            border-radius: 10px;
            margin-bottom: 30px;
        }}
        header h1 {{ font-size: 2em; margin-bottom: 10px; }}
        .summary-grid {{
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
            gap: 15px;
            margin-bottom: 30px;
        }}
        .summary-card {{
            background: var(--bg-card);
            border-radius: 10px;
            padding: 20px;
            text-align: center;
            border: 1px solid #0f3460;
        }}
        .summary-card .number {{ font-size: 2.5em; font-weight: bold; margin-bottom: 5px; }}
        .summary-card.critical .number {{ color: var(--critical-color); }}
        .summary-card.warning .number {{ color: var(--warning-color); }}
        .summary-card.info .number {{ color: var(--info-color); }}
        .section {{
            background: var(--bg-card);
            border-radius: 10px;
            padding: 20px;
            margin-bottom: 20px;
        }}
        .section h2 {{ color: #667eea; margin-bottom: 15px; border-bottom: 1px solid #0f3460; padding-bottom: 10px; }}
        table {{ width: 100%; border-collapse: collapse; }}
        th, td {{ padding: 10px; text-align: left; border-bottom: 1px solid #0f3460; }}
        th {{ background: rgba(0,0,0,0.3); }}
        .badge {{ padding: 3px 10px; border-radius: 12px; font-size: 0.8em; }}
        .badge.critical {{ background: var(--critical-color); }}
        .badge.warning {{ background: var(--warning-color); color: #000; }}
        .badge.info {{ background: var(--info-color); }}
    </style>
</head>
<body>
<div class=""container"">
    <header>
        <h1>TwinCAT QA 분석 리포트</h1>
        <p>프로젝트: {result.ProjectPath}</p>
        <p>분석 일시: {result.Timestamp:yyyy-MM-dd HH:mm:ss}</p>
    </header>

    <div class=""summary-grid"">
        <div class=""summary-card"">
            <div class=""number"">{result.TotalFiles}</div>
            <div class=""label"">총 파일</div>
        </div>
        <div class=""summary-card"">
            <div class=""number"">{result.TotalLines:N0}</div>
            <div class=""label"">코드 라인</div>
        </div>
        <div class=""summary-card"">
            <div class=""number"">{result.Issues.Count}</div>
            <div class=""label"">총 이슈</div>
        </div>
        <div class=""summary-card critical"">
            <div class=""number"">{CriticalCount}</div>
            <div class=""label"">Critical</div>
        </div>
        <div class=""summary-card warning"">
            <div class=""number"">{WarningCount}</div>
            <div class=""label"">Warning</div>
        </div>
        <div class=""summary-card info"">
            <div class=""number"">{InfoCount}</div>
            <div class=""label"">Info</div>
        </div>
    </div>

    <div class=""section"">
        <h2>Critical Issues ({CriticalCount})</h2>
        <table>
            <tr><th>파일</th><th>라인</th><th>규칙</th><th>메시지</th></tr>
            {string.Join("\n", result.Issues.Where(i => i.Severity == "Critical").Take(100).Select(i => $"<tr><td>{i.FileName}</td><td>{i.LineNumber}</td><td>{i.RuleId}</td><td>{i.Message}</td></tr>"))}
        </table>
    </div>

    <div class=""section"">
        <h2>Warning Issues ({WarningCount})</h2>
        <table>
            <tr><th>파일</th><th>라인</th><th>규칙</th><th>메시지</th></tr>
            {string.Join("\n", result.Issues.Where(i => i.Severity == "Warning").Take(100).Select(i => $"<tr><td>{i.FileName}</td><td>{i.LineNumber}</td><td>{i.RuleId}</td><td>{i.Message}</td></tr>"))}
        </table>
    </div>

    <div class=""section"">
        <h2>복잡도 높은 파일 (Top 20)</h2>
        <table>
            <tr><th>파일</th><th>타입</th><th>라인수</th><th>복잡도</th><th>이슈수</th></tr>
            {string.Join("\n", result.Files.OrderByDescending(f => f.Complexity).Take(20).Select(f => $"<tr><td>{f.FileName}</td><td>{f.PouType}</td><td>{f.LineCount}</td><td>{f.Complexity}</td><td>{f.IssueCount}</td></tr>"))}
        </table>
    </div>
</div>
</body>
</html>";
    }

    partial void OnProjectFolderPathChanged(string value)
    {
        AnalyzeCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsAnalyzingChanged(bool value)
    {
        AnalyzeCommand.NotifyCanExecuteChanged();
    }
}

#region Display Models

/// <summary>
/// QA 이슈 표시용 모델
/// </summary>
public class QAIssueDisplay
{
    public string FileName { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public string RuleId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string CodeSnippet { get; set; } = string.Empty;
}

/// <summary>
/// 파일 통계 표시용 모델
/// </summary>
public class FileStatDisplay
{
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public string PouType { get; set; } = string.Empty;
    public int LineCount { get; set; }
    public int Complexity { get; set; }
    public int IssueCount { get; set; }
}

/// <summary>
/// 규칙 통계 표시용 모델
/// </summary>
public class RuleStatDisplay
{
    public string RuleId { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Count { get; set; }
    public string Description { get; set; } = string.Empty;
}

#endregion
