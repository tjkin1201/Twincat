using System.Windows;

namespace TwinCatQA.UI.Views;

/// <summary>
/// 메인 윈도우 - 분석 모드 선택 화면
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 단일 프로젝트 분석 모드 선택
    /// </summary>
    private void SingleProjectAnalysis_Click(object sender, RoutedEventArgs e)
    {
        var window = new SingleProjectAnalysisWindow();
        window.Owner = this;
        window.ShowDialog();
    }

    /// <summary>
    /// Source & Target 비교 모드 선택
    /// </summary>
    private void CompareProjects_Click(object sender, RoutedEventArgs e)
    {
        var window = new FolderComparisonWindow();
        window.Owner = this;
        window.ShowDialog();
    }
}
