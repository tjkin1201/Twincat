using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TwinCatQA.Core.Analysis;
using TwinCatQA.Core.Models;
using TwinCatQA.Core.Services;

namespace TwinCatQA.VsExtension.ToolWindows
{
    /// <summary>
    /// QA Results Tool Window Control의 상호작용 논리
    /// </summary>
    public partial class QAResultsToolWindowControl : UserControl
    {
        private readonly CodeAnalysisService _analysisService;
        private List<Violation> _allViolations;
        private ObservableCollection<ViolationViewModel> _currentViewModels;
        private DateTime _lastAnalysisTime;
        private GroupByMode _currentGroupByMode = GroupByMode.ByFile;

        public QAResultsToolWindowControl()
        {
            InitializeComponent();
            _analysisService = new CodeAnalysisService();
            _allViolations = new List<Violation>();
            _currentViewModels = new ObservableCollection<ViolationViewModel>();
            ViolationsTreeView.ItemsSource = _currentViewModels;
        }

        #region 툴바 이벤트 핸들러

        /// <summary>
        /// 새로고침 버튼 클릭
        /// </summary>
        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await AnalyzeCurrentSolutionAsync();
        }

        /// <summary>
        /// 모두 펼치기 버튼 클릭
        /// </summary>
        private void ExpandAllButton_Click(object sender, RoutedEventArgs e)
        {
            ExpandAll(_currentViewModels, true);
        }

        /// <summary>
        /// 모두 접기 버튼 클릭
        /// </summary>
        private void CollapseAllButton_Click(object sender, RoutedEventArgs e)
        {
            ExpandAll(_currentViewModels, false);
        }

        /// <summary>
        /// 내보내기 버튼 클릭
        /// </summary>
        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: 파일 저장 대화상자 구현
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "HTML 파일 (*.html)|*.html|JSON 파일 (*.json)|*.json|텍스트 파일 (*.txt)|*.txt",
                DefaultExt = ".html",
                FileName = $"QA_Results_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    ExportResults(saveDialog.FileName);
                    MessageBox.Show($"결과가 성공적으로 저장되었습니다.\n{saveDialog.FileName}",
                        "내보내기 완료", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"결과 저장 중 오류가 발생했습니다.\n{ex.Message}",
                        "내보내기 오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// 설정 버튼 클릭
        /// </summary>
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: 설정 대화상자 구현
            MessageBox.Show("설정 기능은 준비 중입니다.", "설정", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region 필터 및 검색

        /// <summary>
        /// 필터 체크박스 변경
        /// </summary>
        private void FilterCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            ApplyFiltersAndSearch();
        }

        /// <summary>
        /// 검색 텍스트 변경
        /// </summary>
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFiltersAndSearch();
        }

        /// <summary>
        /// 검색 버튼 클릭
        /// </summary>
        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyFiltersAndSearch();
        }

        /// <summary>
        /// 그룹화 방식 변경
        /// </summary>
        private void GroupByComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GroupByComboBox.SelectedIndex < 0) return;

            switch (GroupByComboBox.SelectedIndex)
            {
                case 0:
                    _currentGroupByMode = GroupByMode.ByFile;
                    break;
                case 1:
                    _currentGroupByMode = GroupByMode.ByRule;
                    break;
                case 2:
                    _currentGroupByMode = GroupByMode.BySeverity;
                    break;
            }

            RebuildTreeView();
        }

        #endregion

        #region 트리뷰 이벤트

        /// <summary>
        /// 트리뷰 아이템 더블클릭 - 코드 위치로 이동
        /// </summary>
        private void ViolationsTreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ViolationsTreeView.SelectedItem is ViolationViewModel viewModel &&
                viewModel.NodeType == ViolationNodeType.Violation &&
                viewModel.Violation != null)
            {
                NavigateToCode(viewModel.Violation);
            }
        }

        #endregion

        #region 분석 로직

        /// <summary>
        /// 현재 솔루션 분석
        /// </summary>
        private async System.Threading.Tasks.Task AnalyzeCurrentSolutionAsync()
        {
            try
            {
                ShowLoading(true, "프로젝트 파일 검색 중...");

                // TODO: Visual Studio DTE를 사용하여 실제 솔루션 파일 가져오기
                // 현재는 테스트를 위해 더미 데이터 사용
                var projectFiles = GetProjectFiles();

                if (!projectFiles.Any())
                {
                    ShowNoResults();
                    return;
                }

                ShowLoading(true, $"{projectFiles.Count}개 파일 분석 중...");

                var startTime = DateTime.Now;
                _allViolations.Clear();

                // 각 파일 분석
                foreach (var file in projectFiles)
                {
                    if (File.Exists(file))
                    {
                        var violations = await _analysisService.AnalyzeFileAsync(file);
                        _allViolations.AddRange(violations);
                    }
                }

                _lastAnalysisTime = DateTime.Now;
                var analysisTime = _lastAnalysisTime - startTime;

                // 결과 표시
                RebuildTreeView();
                UpdateStatusBar(analysisTime);
                ShowLoading(false);

                if (_allViolations.Count == 0)
                {
                    ShowNoResults();
                }
            }
            catch (Exception ex)
            {
                ShowLoading(false);
                MessageBox.Show($"분석 중 오류가 발생했습니다.\n{ex.Message}",
                    "분석 오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 프로젝트 파일 목록 가져오기
        /// </summary>
        private List<string> GetProjectFiles()
        {
            // TODO: Visual Studio DTE를 사용하여 실제 프로젝트 파일 가져오기
            // 현재는 테스트를 위해 현재 디렉토리에서 .TcPOU 파일 검색
            var projectFiles = new List<string>();

            try
            {
                var currentDir = Directory.GetCurrentDirectory();
                projectFiles = Directory.GetFiles(currentDir, "*.TcPOU", SearchOption.AllDirectories).ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"파일 검색 오류: {ex.Message}");
            }

            return projectFiles;
        }

        #endregion

        #region UI 업데이트

        /// <summary>
        /// 트리뷰 재구성
        /// </summary>
        private void RebuildTreeView()
        {
            _currentViewModels.Clear();

            var filteredViolations = GetFilteredViolations();

            switch (_currentGroupByMode)
            {
                case GroupByMode.ByFile:
                    BuildTreeByFile(filteredViolations);
                    break;
                case GroupByMode.ByRule:
                    BuildTreeByRule(filteredViolations);
                    break;
                case GroupByMode.BySeverity:
                    BuildTreeBySeverity(filteredViolations);
                    break;
            }
        }

        /// <summary>
        /// 파일별 트리 구성
        /// </summary>
        private void BuildTreeByFile(List<Violation> violations)
        {
            var groupedByFile = violations.GroupBy(v => v.FilePath);

            foreach (var fileGroup in groupedByFile)
            {
                var fileNode = new ViolationViewModel
                {
                    DisplayName = Path.GetFileName(fileGroup.Key),
                    NodeType = ViolationNodeType.File,
                    Violation = fileGroup.First(),
                    ViolationCount = fileGroup.Count(),
                    IsExpanded = false
                };

                var groupedByRule = fileGroup.GroupBy(v => v.RuleId);
                foreach (var ruleGroup in groupedByRule)
                {
                    var ruleNode = new ViolationViewModel
                    {
                        DisplayName = $"{ruleGroup.Key}: {ruleGroup.First().Message}",
                        NodeType = ViolationNodeType.Rule,
                        Violation = ruleGroup.First(),
                        ViolationCount = ruleGroup.Count(),
                        IsExpanded = false
                    };

                    foreach (var violation in ruleGroup)
                    {
                        ruleNode.Children.Add(CreateViolationNode(violation));
                    }

                    fileNode.Children.Add(ruleNode);
                }

                _currentViewModels.Add(fileNode);
            }
        }

        /// <summary>
        /// 규칙별 트리 구성
        /// </summary>
        private void BuildTreeByRule(List<Violation> violations)
        {
            var groupedByRule = violations.GroupBy(v => v.RuleId);

            foreach (var ruleGroup in groupedByRule)
            {
                var ruleNode = new ViolationViewModel
                {
                    DisplayName = $"{ruleGroup.Key}: {ruleGroup.First().Message}",
                    NodeType = ViolationNodeType.Rule,
                    Violation = ruleGroup.First(),
                    ViolationCount = ruleGroup.Count(),
                    IsExpanded = false
                };

                var groupedByFile = ruleGroup.GroupBy(v => v.FilePath);
                foreach (var fileGroup in groupedByFile)
                {
                    var fileNode = new ViolationViewModel
                    {
                        DisplayName = Path.GetFileName(fileGroup.Key),
                        NodeType = ViolationNodeType.File,
                        Violation = fileGroup.First(),
                        ViolationCount = fileGroup.Count(),
                        IsExpanded = false
                    };

                    foreach (var violation in fileGroup)
                    {
                        fileNode.Children.Add(CreateViolationNode(violation));
                    }

                    ruleNode.Children.Add(fileNode);
                }

                _currentViewModels.Add(ruleNode);
            }
        }

        /// <summary>
        /// 심각도별 트리 구성
        /// </summary>
        private void BuildTreeBySeverity(List<Violation> violations)
        {
            var groupedBySeverity = violations.GroupBy(v => v.Severity)
                .OrderBy(g => g.Key); // Error, Warning, Info 순서

            foreach (var severityGroup in groupedBySeverity)
            {
                var severityNode = new ViolationViewModel
                {
                    DisplayName = GetSeverityDisplayName(severityGroup.Key),
                    NodeType = ViolationNodeType.File, // 아이콘을 위해 임시로 File 사용
                    ViolationCount = severityGroup.Count(),
                    IsExpanded = true
                };

                var groupedByFile = severityGroup.GroupBy(v => v.FilePath);
                foreach (var fileGroup in groupedByFile)
                {
                    var fileNode = new ViolationViewModel
                    {
                        DisplayName = Path.GetFileName(fileGroup.Key),
                        NodeType = ViolationNodeType.File,
                        Violation = fileGroup.First(),
                        ViolationCount = fileGroup.Count(),
                        IsExpanded = false
                    };

                    foreach (var violation in fileGroup)
                    {
                        fileNode.Children.Add(CreateViolationNode(violation));
                    }

                    severityNode.Children.Add(fileNode);
                }

                _currentViewModels.Add(severityNode);
            }
        }

        /// <summary>
        /// 위반 사항 노드 생성
        /// </summary>
        private ViolationViewModel CreateViolationNode(Violation violation)
        {
            return new ViolationViewModel
            {
                DisplayName = $"라인 {violation.LineNumber}: {violation.Message}",
                NodeType = ViolationNodeType.Violation,
                Violation = violation
            };
        }

        /// <summary>
        /// 필터링 및 검색 적용
        /// </summary>
        private void ApplyFiltersAndSearch()
        {
            RebuildTreeView();
        }

        /// <summary>
        /// 필터링된 위반 사항 가져오기
        /// </summary>
        private List<Violation> GetFilteredViolations()
        {
            var filtered = _allViolations.AsEnumerable();

            // 심각도 필터
            var severities = new List<ViolationSeverity>();
            if (ErrorCheckBox.IsChecked == true) severities.Add(ViolationSeverity.Error);
            if (WarningCheckBox.IsChecked == true) severities.Add(ViolationSeverity.Warning);
            if (InfoCheckBox.IsChecked == true) severities.Add(ViolationSeverity.Info);

            if (severities.Any())
            {
                filtered = filtered.Where(v => severities.Contains(v.Severity));
            }

            // 검색 필터
            var searchText = SearchTextBox.Text?.Trim();
            if (!string.IsNullOrEmpty(searchText))
            {
                filtered = filtered.Where(v =>
                    v.Message.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    v.RuleId.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    v.FilePath.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    v.CodeSnippet.Contains(searchText, StringComparison.OrdinalIgnoreCase));
            }

            return filtered.ToList();
        }

        /// <summary>
        /// 상태바 업데이트
        /// </summary>
        private void UpdateStatusBar(TimeSpan analysisTime)
        {
            var errorCount = _allViolations.Count(v => v.Severity == ViolationSeverity.Error);
            var warningCount = _allViolations.Count(v => v.Severity == ViolationSeverity.Warning);
            var infoCount = _allViolations.Count(v => v.Severity == ViolationSeverity.Info);

            TotalViolationsText.Text = _allViolations.Count.ToString();
            ErrorCountText.Text = errorCount.ToString();
            WarningCountText.Text = warningCount.ToString();
            InfoCountText.Text = infoCount.ToString();
            AnalysisTimeText.Text = $"마지막 분석: {_lastAnalysisTime:HH:mm:ss} ({analysisTime.TotalSeconds:F2}초 소요)";
        }

        /// <summary>
        /// 로딩 표시
        /// </summary>
        private void ShowLoading(bool show, string message = "분석 중...")
        {
            LoadingPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            LoadingText.Text = message;
            NoResultsText.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 결과 없음 표시
        /// </summary>
        private void ShowNoResults()
        {
            NoResultsText.Visibility = _currentViewModels.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// 모두 펼치기/접기
        /// </summary>
        private void ExpandAll(ObservableCollection<ViolationViewModel> items, bool expand)
        {
            foreach (var item in items)
            {
                item.IsExpanded = expand;
                if (item.Children.Any())
                {
                    ExpandAll(item.Children, expand);
                }
            }
        }

        #endregion

        #region 네비게이션

        /// <summary>
        /// 코드 위치로 이동
        /// </summary>
        private void NavigateToCode(Violation violation)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                // TODO: Visual Studio DTE를 사용하여 파일 열기 및 라인으로 이동
                // var dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
                // dte.ItemOperations.OpenFile(violation.FilePath);
                // var textSelection = dte.ActiveDocument.Selection as TextSelection;
                // textSelection.GotoLine(violation.LineNumber, true);

                Debug.WriteLine($"네비게이션: {violation.FilePath} - Line {violation.LineNumber}");
                MessageBox.Show(
                    $"파일: {violation.FilePath}\n라인: {violation.LineNumber}\n\n(실제 네비게이션 기능은 VS 확장에서 구현됩니다)",
                    "코드 위치", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"코드 위치로 이동 중 오류가 발생했습니다.\n{ex.Message}",
                    "네비게이션 오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region 내보내기

        /// <summary>
        /// 결과 내보내기
        /// </summary>
        private void ExportResults(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLower();

            switch (extension)
            {
                case ".html":
                    ExportToHtml(filePath);
                    break;
                case ".json":
                    ExportToJson(filePath);
                    break;
                case ".txt":
                    ExportToText(filePath);
                    break;
                default:
                    throw new NotSupportedException($"지원하지 않는 파일 형식입니다: {extension}");
            }
        }

        /// <summary>
        /// HTML 형식으로 내보내기
        /// </summary>
        private void ExportToHtml(string filePath)
        {
            // TODO: HTML 리포트 생성 구현
            var html = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>TwinCAT QA 분석 결과</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        h1 {{ color: #333; }}
        .summary {{ background: #f0f0f0; padding: 10px; margin: 10px 0; }}
        .violation {{ border-left: 3px solid #ccc; padding: 10px; margin: 5px 0; }}
        .error {{ border-color: red; }}
        .warning {{ border-color: orange; }}
        .info {{ border-color: blue; }}
    </style>
</head>
<body>
    <h1>TwinCAT QA 분석 결과</h1>
    <div class='summary'>
        <p>분석 시간: {_lastAnalysisTime:yyyy-MM-dd HH:mm:ss}</p>
        <p>총 위반: {_allViolations.Count}</p>
    </div>
    <h2>위반 사항 목록</h2>
    {string.Join("\n", _allViolations.Select(v => $@"
    <div class='violation {v.Severity.ToString().ToLower()}'>
        <strong>[{v.Severity}] {v.RuleId}</strong><br>
        파일: {v.FilePath}<br>
        라인: {v.LineNumber}<br>
        메시지: {v.Message}
    </div>"))}
</body>
</html>";

            File.WriteAllText(filePath, html);
        }

        /// <summary>
        /// JSON 형식으로 내보내기
        /// </summary>
        private void ExportToJson(string filePath)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(_allViolations, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// 텍스트 형식으로 내보내기
        /// </summary>
        private void ExportToText(string filePath)
        {
            var text = $@"TwinCAT QA 분석 결과
분석 시간: {_lastAnalysisTime:yyyy-MM-dd HH:mm:ss}
총 위반: {_allViolations.Count}

위반 사항 목록:
{string.Join("\n\n", _allViolations.Select(v => $@"[{v.Severity}] {v.RuleId}
파일: {v.FilePath}
라인: {v.LineNumber}
메시지: {v.Message}"))}";

            File.WriteAllText(filePath, text);
        }

        #endregion

        #region 헬퍼 메서드

        /// <summary>
        /// 심각도 표시 이름 가져오기
        /// </summary>
        private string GetSeverityDisplayName(ViolationSeverity severity)
        {
            return severity switch
            {
                ViolationSeverity.Error => "오류",
                ViolationSeverity.Warning => "경고",
                ViolationSeverity.Info => "정보",
                _ => severity.ToString()
            };
        }

        #endregion
    }

    /// <summary>
    /// 그룹화 방식
    /// </summary>
    public enum GroupByMode
    {
        /// <summary>
        /// 파일별 그룹화
        /// </summary>
        ByFile,

        /// <summary>
        /// 규칙별 그룹화
        /// </summary>
        ByRule,

        /// <summary>
        /// 심각도별 그룹화
        /// </summary>
        BySeverity
    }
}
