using System.Windows;
using TwinCatQA.UI.ViewModels;

namespace TwinCatQA.UI.Views;

/// <summary>
/// 폴더 비교 창
/// </summary>
public partial class FolderComparisonWindow : Window
{
    public FolderComparisonWindow()
    {
        InitializeComponent();
        DataContext = new FolderComparisonViewModel();
    }
}
