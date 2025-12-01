using OfficeOpenXml;
using OfficeOpenXml.Style;
using OfficeOpenXml.Table;
using System.Drawing;
using TwinCatQA.Domain.Models.QA;

namespace TwinCatQA.Infrastructure.Reports;

/// <summary>
/// QA 분석 결과를 Excel 파일로 생성하는 리포트 생성기
/// 스타일이 적용된 표와 필터 기능을 포함합니다.
/// </summary>
public class ExcelReportGenerator
{
    public ExcelReportGenerator()
    {
        // EPPlus 라이선스 설정 (비상업적 사용)
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    /// <summary>
    /// QA 리포트를 Excel 파일로 생성합니다.
    /// </summary>
    public void GenerateReport(QAReport report, string outputPath)
    {
        using var package = new ExcelPackage();

        // 1. 요약 시트
        CreateSummarySheet(package, report);

        // 2. 전체 이슈 목록 시트
        CreateIssuesSheet(package, report);

        // 3. 파일별 이슈 시트
        CreateIssuesByFileSheet(package, report);

        // 4. 규칙별 통계 시트
        CreateRuleStatisticsSheet(package, report);

        // 5. 심각도별 이슈 시트
        CreateIssuesBySeveritySheet(package, report);

        // 파일 저장
        var fileInfo = new FileInfo(outputPath);
        package.SaveAs(fileInfo);
    }

    /// <summary>
    /// 요약 시트 생성
    /// </summary>
    private void CreateSummarySheet(ExcelPackage package, QAReport report)
    {
        var ws = package.Workbook.Worksheets.Add("요약");

        // 제목
        ws.Cells["A1"].Value = "TwinCAT 코드 QA 분석 리포트";
        ws.Cells["A1:F1"].Merge = true;
        ws.Cells["A1"].Style.Font.Size = 18;
        ws.Cells["A1"].Style.Font.Bold = true;
        ws.Cells["A1"].Style.Font.Color.SetColor(Color.White);
        ws.Cells["A1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        ws.Cells["A1"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(0, 112, 192));
        ws.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        ws.Row(1).Height = 30;

        // 분석 정보
        var infoRow = 3;
        ws.Cells[infoRow, 1].Value = "분석 일시:";
        ws.Cells[infoRow, 2].Value = report.GeneratedAt.ToString("yyyy-MM-dd HH:mm:ss");
        ws.Cells[infoRow, 1].Style.Font.Bold = true;

        infoRow++;
        ws.Cells[infoRow, 1].Value = "소스 폴더:";
        ws.Cells[infoRow, 2].Value = report.SourceFolder;
        ws.Cells[infoRow, 1].Style.Font.Bold = true;

        infoRow++;
        ws.Cells[infoRow, 1].Value = "대상 폴더:";
        ws.Cells[infoRow, 2].Value = report.TargetFolder;
        ws.Cells[infoRow, 1].Style.Font.Bold = true;

        infoRow++;
        ws.Cells[infoRow, 1].Value = "총 변경 사항:";
        ws.Cells[infoRow, 2].Value = report.TotalChanges;
        ws.Cells[infoRow, 1].Style.Font.Bold = true;

        // 요약 통계 테이블
        var summaryRow = infoRow + 3;
        ws.Cells[summaryRow, 1].Value = "심각도";
        ws.Cells[summaryRow, 2].Value = "개수";
        ws.Cells[summaryRow, 3].Value = "비율";

        // 헤더 스타일
        var headerRange = ws.Cells[summaryRow, 1, summaryRow, 3];
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
        headerRange.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(68, 114, 196));
        headerRange.Style.Font.Color.SetColor(Color.White);
        headerRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

        // Critical
        summaryRow++;
        ws.Cells[summaryRow, 1].Value = "🔴 Critical";
        ws.Cells[summaryRow, 2].Value = report.CriticalCount;
        ws.Cells[summaryRow, 3].Value = report.TotalIssues > 0 ? (double)report.CriticalCount / report.TotalIssues : 0;
        ws.Cells[summaryRow, 3].Style.Numberformat.Format = "0.0%";
        ws.Cells[summaryRow, 1, summaryRow, 3].Style.Fill.PatternType = ExcelFillStyle.Solid;
        ws.Cells[summaryRow, 1, summaryRow, 3].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 199, 206));

        // Warning
        summaryRow++;
        ws.Cells[summaryRow, 1].Value = "🟡 Warning";
        ws.Cells[summaryRow, 2].Value = report.WarningCount;
        ws.Cells[summaryRow, 3].Value = report.TotalIssues > 0 ? (double)report.WarningCount / report.TotalIssues : 0;
        ws.Cells[summaryRow, 3].Style.Numberformat.Format = "0.0%";
        ws.Cells[summaryRow, 1, summaryRow, 3].Style.Fill.PatternType = ExcelFillStyle.Solid;
        ws.Cells[summaryRow, 1, summaryRow, 3].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 235, 156));

        // Info
        summaryRow++;
        ws.Cells[summaryRow, 1].Value = "🔵 Info";
        ws.Cells[summaryRow, 2].Value = report.InfoCount;
        ws.Cells[summaryRow, 3].Value = report.TotalIssues > 0 ? (double)report.InfoCount / report.TotalIssues : 0;
        ws.Cells[summaryRow, 3].Style.Numberformat.Format = "0.0%";
        ws.Cells[summaryRow, 1, summaryRow, 3].Style.Fill.PatternType = ExcelFillStyle.Solid;
        ws.Cells[summaryRow, 1, summaryRow, 3].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(198, 224, 180));

        // 합계
        summaryRow++;
        ws.Cells[summaryRow, 1].Value = "합계";
        ws.Cells[summaryRow, 2].Value = report.TotalIssues;
        ws.Cells[summaryRow, 3].Value = 1;
        ws.Cells[summaryRow, 3].Style.Numberformat.Format = "0.0%";
        ws.Cells[summaryRow, 1, summaryRow, 3].Style.Font.Bold = true;
        ws.Cells[summaryRow, 1, summaryRow, 3].Style.Fill.PatternType = ExcelFillStyle.Solid;
        ws.Cells[summaryRow, 1, summaryRow, 3].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(217, 217, 217));

        // 테두리 적용
        var tableRange = ws.Cells[infoRow + 3, 1, summaryRow, 3];
        tableRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
        tableRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
        tableRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
        tableRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

        // 열 너비 조정
        ws.Column(1).Width = 20;
        ws.Column(2).Width = 50;
        ws.Column(3).Width = 15;
    }

    /// <summary>
    /// 전체 이슈 목록 시트 생성 (필터 포함)
    /// </summary>
    private void CreateIssuesSheet(ExcelPackage package, QAReport report)
    {
        var ws = package.Workbook.Worksheets.Add("전체 이슈 목록");

        // 헤더
        var headers = new[] { "번호", "심각도", "규칙 ID", "제목", "파일명", "라인", "설명", "권장 조치", "카테고리" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cells[1, i + 1].Value = headers[i];
        }

        // 헤더 스타일
        var headerRange = ws.Cells[1, 1, 1, headers.Length];
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
        headerRange.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(68, 114, 196));
        headerRange.Style.Font.Color.SetColor(Color.White);
        headerRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        ws.Row(1).Height = 25;

        // 데이터 입력
        var row = 2;
        var issueNumber = 1;
        foreach (var issue in report.Issues)
        {
            ws.Cells[row, 1].Value = issueNumber++;
            ws.Cells[row, 2].Value = GetSeverityText(issue.Severity);
            ws.Cells[row, 3].Value = issue.RuleId;
            ws.Cells[row, 4].Value = issue.Title;
            ws.Cells[row, 5].Value = Path.GetFileName(issue.FilePath);
            ws.Cells[row, 6].Value = issue.Line;
            ws.Cells[row, 7].Value = TruncateText(issue.Description, 200);
            ws.Cells[row, 8].Value = TruncateText(issue.Recommendation ?? "", 200);
            ws.Cells[row, 9].Value = issue.Category;

            // 심각도별 행 색상
            ApplySeverityRowStyle(ws, row, issue.Severity, headers.Length);

            row++;
        }

        // 테이블로 변환 (필터 자동 적용)
        if (report.Issues.Any())
        {
            var tableRange = ws.Cells[1, 1, row - 1, headers.Length];
            var table = ws.Tables.Add(tableRange, "IssuesTable");
            table.TableStyle = TableStyles.Medium2;
            table.ShowFilter = true;
        }

        // 열 너비 조정
        ws.Column(1).Width = 8;   // 번호
        ws.Column(2).Width = 12;  // 심각도
        ws.Column(3).Width = 12;  // 규칙 ID
        ws.Column(4).Width = 35;  // 제목
        ws.Column(5).Width = 40;  // 파일명
        ws.Column(6).Width = 8;   // 라인
        ws.Column(7).Width = 60;  // 설명
        ws.Column(8).Width = 50;  // 권장 조치
        ws.Column(9).Width = 20;  // 카테고리

        // 텍스트 줄바꿈 설정
        ws.Cells[2, 7, row - 1, 8].Style.WrapText = true;

        // 행 높이 자동 조절을 위한 최소 높이 설정
        for (int r = 2; r < row; r++)
        {
            ws.Row(r).Height = 30;
        }
    }

    /// <summary>
    /// 파일별 이슈 시트 생성
    /// </summary>
    private void CreateIssuesByFileSheet(ExcelPackage package, QAReport report)
    {
        var ws = package.Workbook.Worksheets.Add("파일별 이슈");

        // 헤더
        var headers = new[] { "번호", "파일명", "전체 경로", "Critical", "Warning", "Info", "총 이슈" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cells[1, i + 1].Value = headers[i];
        }

        // 헤더 스타일
        var headerRange = ws.Cells[1, 1, 1, headers.Length];
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
        headerRange.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(112, 48, 160));
        headerRange.Style.Font.Color.SetColor(Color.White);
        headerRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        ws.Row(1).Height = 25;

        // 파일별 그룹화
        var issuesByFile = report.Issues
            .GroupBy(i => i.FilePath)
            .Select(g => new
            {
                FilePath = g.Key,
                FileName = Path.GetFileName(g.Key),
                CriticalCount = g.Count(i => i.Severity == Severity.Critical),
                WarningCount = g.Count(i => i.Severity == Severity.Warning),
                InfoCount = g.Count(i => i.Severity == Severity.Info),
                TotalCount = g.Count()
            })
            .OrderByDescending(f => f.TotalCount)
            .ToList();

        // 데이터 입력
        var row = 2;
        var fileNumber = 1;
        foreach (var file in issuesByFile)
        {
            ws.Cells[row, 1].Value = fileNumber++;
            ws.Cells[row, 2].Value = file.FileName;
            ws.Cells[row, 3].Value = file.FilePath;
            ws.Cells[row, 4].Value = file.CriticalCount;
            ws.Cells[row, 5].Value = file.WarningCount;
            ws.Cells[row, 6].Value = file.InfoCount;
            ws.Cells[row, 7].Value = file.TotalCount;

            // Critical 개수가 있으면 빨간 배경
            if (file.CriticalCount > 0)
            {
                ws.Cells[row, 4].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[row, 4].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 199, 206));
            }

            row++;
        }

        // 테이블로 변환
        if (issuesByFile.Any())
        {
            var tableRange = ws.Cells[1, 1, row - 1, headers.Length];
            var table = ws.Tables.Add(tableRange, "FileIssuesTable");
            table.TableStyle = TableStyles.Medium4;
            table.ShowFilter = true;
        }

        // 열 너비 조정
        ws.Column(1).Width = 8;
        ws.Column(2).Width = 40;
        ws.Column(3).Width = 80;
        ws.Column(4).Width = 12;
        ws.Column(5).Width = 12;
        ws.Column(6).Width = 12;
        ws.Column(7).Width = 12;
    }

    /// <summary>
    /// 규칙별 통계 시트 생성
    /// </summary>
    private void CreateRuleStatisticsSheet(ExcelPackage package, QAReport report)
    {
        var ws = package.Workbook.Worksheets.Add("규칙별 통계");

        // 헤더
        var headers = new[] { "번호", "규칙 ID", "규칙명", "심각도", "검출 횟수", "카테고리", "설명" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cells[1, i + 1].Value = headers[i];
        }

        // 헤더 스타일
        var headerRange = ws.Cells[1, 1, 1, headers.Length];
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
        headerRange.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(0, 176, 80));
        headerRange.Style.Font.Color.SetColor(Color.White);
        headerRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        ws.Row(1).Height = 25;

        // 규칙별 그룹화
        var ruleStats = report.Issues
            .GroupBy(i => new { i.RuleId, i.Title, i.Severity, i.Category })
            .Select(g => new
            {
                g.Key.RuleId,
                g.Key.Title,
                g.Key.Severity,
                g.Key.Category,
                Count = g.Count(),
                Description = g.First().Description
            })
            .OrderByDescending(r => r.Count)
            .ToList();

        // 데이터 입력
        var row = 2;
        var ruleNumber = 1;
        foreach (var rule in ruleStats)
        {
            ws.Cells[row, 1].Value = ruleNumber++;
            ws.Cells[row, 2].Value = rule.RuleId;
            ws.Cells[row, 3].Value = rule.Title;
            ws.Cells[row, 4].Value = GetSeverityText(rule.Severity);
            ws.Cells[row, 5].Value = rule.Count;
            ws.Cells[row, 6].Value = rule.Category;
            ws.Cells[row, 7].Value = TruncateText(rule.Description, 150);

            // 심각도별 색상
            ApplySeverityCellStyle(ws.Cells[row, 4], rule.Severity);

            row++;
        }

        // 테이블로 변환
        if (ruleStats.Any())
        {
            var tableRange = ws.Cells[1, 1, row - 1, headers.Length];
            var table = ws.Tables.Add(tableRange, "RuleStatsTable");
            table.TableStyle = TableStyles.Medium3;
            table.ShowFilter = true;
        }

        // 열 너비 조정
        ws.Column(1).Width = 8;
        ws.Column(2).Width = 12;
        ws.Column(3).Width = 35;
        ws.Column(4).Width = 12;
        ws.Column(5).Width = 12;
        ws.Column(6).Width = 20;
        ws.Column(7).Width = 60;
    }

    /// <summary>
    /// 심각도별 이슈 시트 생성
    /// </summary>
    private void CreateIssuesBySeveritySheet(ExcelPackage package, QAReport report)
    {
        // Critical 이슈 시트
        CreateSeveritySheet(package, report, Severity.Critical, "Critical 이슈", Color.FromArgb(192, 0, 0));

        // Warning 이슈 시트
        CreateSeveritySheet(package, report, Severity.Warning, "Warning 이슈", Color.FromArgb(255, 192, 0));

        // Info 이슈 시트
        CreateSeveritySheet(package, report, Severity.Info, "Info 이슈", Color.FromArgb(0, 112, 192));
    }

    private void CreateSeveritySheet(ExcelPackage package, QAReport report, Severity severity, string sheetName, Color headerColor)
    {
        var filteredIssues = report.Issues.Where(i => i.Severity == severity).ToList();
        if (!filteredIssues.Any()) return;

        var ws = package.Workbook.Worksheets.Add(sheetName);

        // 헤더
        var headers = new[] { "번호", "규칙 ID", "제목", "파일명", "라인", "설명", "권장 조치" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cells[1, i + 1].Value = headers[i];
        }

        // 헤더 스타일
        var headerRange = ws.Cells[1, 1, 1, headers.Length];
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
        headerRange.Style.Fill.BackgroundColor.SetColor(headerColor);
        headerRange.Style.Font.Color.SetColor(Color.White);
        headerRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        ws.Row(1).Height = 25;

        // 데이터 입력
        var row = 2;
        var issueNumber = 1;
        foreach (var issue in filteredIssues)
        {
            ws.Cells[row, 1].Value = issueNumber++;
            ws.Cells[row, 2].Value = issue.RuleId;
            ws.Cells[row, 3].Value = issue.Title;
            ws.Cells[row, 4].Value = Path.GetFileName(issue.FilePath);
            ws.Cells[row, 5].Value = issue.Line;
            ws.Cells[row, 6].Value = TruncateText(issue.Description, 200);
            ws.Cells[row, 7].Value = TruncateText(issue.Recommendation ?? "", 200);

            row++;
        }

        // 테이블로 변환
        var tableRange = ws.Cells[1, 1, row - 1, headers.Length];
        var tableName = sheetName.Replace(" ", "") + "Table";
        var table = ws.Tables.Add(tableRange, tableName);
        table.TableStyle = TableStyles.Medium2;
        table.ShowFilter = true;

        // 열 너비 조정
        ws.Column(1).Width = 8;
        ws.Column(2).Width = 12;
        ws.Column(3).Width = 35;
        ws.Column(4).Width = 40;
        ws.Column(5).Width = 8;
        ws.Column(6).Width = 60;
        ws.Column(7).Width = 50;
    }

    #region 헬퍼 메서드

    private string GetSeverityText(Severity severity)
    {
        return severity switch
        {
            Severity.Critical => "Critical",
            Severity.Warning => "Warning",
            Severity.Info => "Info",
            _ => severity.ToString()
        };
    }

    private void ApplySeverityRowStyle(ExcelWorksheet ws, int row, Severity severity, int columnCount)
    {
        var range = ws.Cells[row, 1, row, columnCount];
        range.Style.Fill.PatternType = ExcelFillStyle.Solid;

        switch (severity)
        {
            case Severity.Critical:
                range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 235, 238));
                break;
            case Severity.Warning:
                range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 249, 196));
                break;
            case Severity.Info:
                range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(232, 245, 233));
                break;
        }
    }

    private void ApplySeverityCellStyle(ExcelRange cell, Severity severity)
    {
        cell.Style.Fill.PatternType = ExcelFillStyle.Solid;

        switch (severity)
        {
            case Severity.Critical:
                cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 199, 206));
                cell.Style.Font.Color.SetColor(Color.FromArgb(156, 0, 6));
                break;
            case Severity.Warning:
                cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 235, 156));
                cell.Style.Font.Color.SetColor(Color.FromArgb(156, 87, 0));
                break;
            case Severity.Info:
                cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(198, 224, 180));
                cell.Style.Font.Color.SetColor(Color.FromArgb(0, 97, 0));
                break;
        }
    }

    private string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return "";

        // 줄바꿈 제거하고 공백으로 대체
        text = text.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");

        // 연속 공백 제거
        while (text.Contains("  "))
        {
            text = text.Replace("  ", " ");
        }

        if (text.Length <= maxLength) return text;
        return text.Substring(0, maxLength - 3) + "...";
    }

    #endregion
}
