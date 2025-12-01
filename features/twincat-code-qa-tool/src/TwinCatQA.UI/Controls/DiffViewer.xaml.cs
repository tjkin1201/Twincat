using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

namespace TwinCatQA.UI.Controls;

/// <summary>
/// 코드 Diff 뷰어 컨트롤
/// DiffPlex 라이브러리를 사용하여 두 코드의 차이점을 시각화
/// </summary>
public partial class DiffViewer : UserControl
{
    public static readonly DependencyProperty OldTextProperty =
        DependencyProperty.Register(nameof(OldText), typeof(string), typeof(DiffViewer),
            new PropertyMetadata(string.Empty, OnTextChanged));

    public static readonly DependencyProperty NewTextProperty =
        DependencyProperty.Register(nameof(NewText), typeof(string), typeof(DiffViewer),
            new PropertyMetadata(string.Empty, OnTextChanged));

    /// <summary>이전 코드</summary>
    public string OldText
    {
        get => (string)GetValue(OldTextProperty);
        set => SetValue(OldTextProperty, value);
    }

    /// <summary>새 코드</summary>
    public string NewText
    {
        get => (string)GetValue(NewTextProperty);
        set => SetValue(NewTextProperty, value);
    }

    public ObservableCollection<DiffLineViewModel> DiffLines { get; } = new();

    public DiffViewer()
    {
        InitializeComponent();
        DiffContent.ItemsSource = DiffLines;
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DiffViewer viewer)
        {
            viewer.UpdateDiff();
        }
    }

    private void UpdateDiff()
    {
        DiffLines.Clear();

        var oldText = OldText ?? string.Empty;
        var newText = NewText ?? string.Empty;

        // 둘 다 비어있으면 표시할 것 없음
        if (string.IsNullOrEmpty(oldText) && string.IsNullOrEmpty(newText))
        {
            DiffLines.Add(new DiffLineViewModel
            {
                LineNumber = "",
                Prefix = "",
                Text = "(선택된 항목이 없습니다)",
                Background = Brushes.White,
                Foreground = Brushes.Gray
            });
            return;
        }

        // DiffPlex를 사용한 인라인 diff
        var diffBuilder = new InlineDiffBuilder(new Differ());
        var diff = diffBuilder.BuildDiffModel(oldText, newText);

        int lineNumber = 1;
        foreach (var line in diff.Lines)
        {
            var viewModel = new DiffLineViewModel
            {
                LineNumber = line.Type == ChangeType.Imaginary ? "" : lineNumber.ToString(),
                Text = line.Text ?? string.Empty
            };

            switch (line.Type)
            {
                case ChangeType.Inserted:
                    viewModel.Prefix = "+";
                    viewModel.Background = new SolidColorBrush(Color.FromRgb(204, 255, 204)); // #CCFFCC
                    viewModel.Foreground = new SolidColorBrush(Color.FromRgb(0, 102, 0)); // #006600
                    viewModel.PrefixBackground = new SolidColorBrush(Color.FromRgb(144, 238, 144)); // 더 진한 녹색
                    viewModel.PrefixForeground = new SolidColorBrush(Color.FromRgb(0, 100, 0));
                    break;

                case ChangeType.Deleted:
                    viewModel.Prefix = "-";
                    viewModel.Background = new SolidColorBrush(Color.FromRgb(255, 204, 204)); // #FFCCCC
                    viewModel.Foreground = new SolidColorBrush(Color.FromRgb(102, 0, 0)); // #660000
                    viewModel.PrefixBackground = new SolidColorBrush(Color.FromRgb(255, 160, 160)); // 더 진한 빨간색
                    viewModel.PrefixForeground = new SolidColorBrush(Color.FromRgb(139, 0, 0));
                    break;

                case ChangeType.Modified:
                    viewModel.Prefix = "~";
                    viewModel.Background = new SolidColorBrush(Color.FromRgb(255, 255, 204)); // #FFFFCC
                    viewModel.Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 0)); // #666600
                    viewModel.PrefixBackground = new SolidColorBrush(Color.FromRgb(255, 255, 150));
                    viewModel.PrefixForeground = new SolidColorBrush(Color.FromRgb(102, 102, 0));
                    break;

                case ChangeType.Imaginary:
                    viewModel.Prefix = "";
                    viewModel.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));
                    viewModel.Foreground = Brushes.LightGray;
                    viewModel.PrefixBackground = Brushes.Transparent;
                    viewModel.PrefixForeground = Brushes.Transparent;
                    break;

                default: // Unchanged
                    viewModel.Prefix = " ";
                    viewModel.Background = Brushes.White;
                    viewModel.Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51)); // #333333
                    viewModel.PrefixBackground = Brushes.White;
                    viewModel.PrefixForeground = Brushes.LightGray;
                    break;
            }

            DiffLines.Add(viewModel);

            if (line.Type != ChangeType.Imaginary)
            {
                lineNumber++;
            }
        }
    }
}

/// <summary>
/// Diff 라인 뷰 모델
/// </summary>
public class DiffLineViewModel
{
    public string LineNumber { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public Brush Background { get; set; } = Brushes.White;
    public Brush Foreground { get; set; } = Brushes.Black;
    public Brush PrefixBackground { get; set; } = Brushes.White;
    public Brush PrefixForeground { get; set; } = Brushes.Black;
}
