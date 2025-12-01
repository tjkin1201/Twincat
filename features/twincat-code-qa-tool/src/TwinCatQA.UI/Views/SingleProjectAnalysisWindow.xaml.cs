using System.Windows;
using TwinCatQA.UI.ViewModels;

namespace TwinCatQA.UI.Views;

/// <summary>
/// 단일 프로젝트 QA 분석 윈도우
/// </summary>
public partial class SingleProjectAnalysisWindow : Window
{
    public SingleProjectAnalysisWindow()
    {
        InitializeComponent();
        DataContext = new SingleProjectAnalysisViewModel();
    }
}
