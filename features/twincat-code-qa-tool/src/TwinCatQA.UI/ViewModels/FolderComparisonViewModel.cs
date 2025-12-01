using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Services;
using TwinCatQA.Infrastructure.Comparison;
using System.IO;
using Ookii.Dialogs.Wpf;

namespace TwinCatQA.UI.ViewModels;

/// <summary>
/// 폴더 비교 창 ViewModel
/// </summary>
public partial class FolderComparisonViewModel : ObservableObject
{
    private readonly IFolderComparer _folderComparer;
    private readonly ILogger<FolderComparisonViewModel> _logger;

    [ObservableProperty]
    private string _sourceFolderPath = string.Empty;

    [ObservableProperty]
    private string _targetFolderPath = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "폴더를 선택하고 비교 시작 버튼을 클릭하세요.";

    [ObservableProperty]
    private bool _isComparing = false;

    [ObservableProperty]
    private bool _includeVariables = true;

    [ObservableProperty]
    private bool _includeIOMapping = true;

    [ObservableProperty]
    private bool _includeLogic = true;

    [ObservableProperty]
    private bool _includeDataTypes = true;

    [ObservableProperty]
    private int _totalChanges = 0;

    [ObservableProperty]
    private int _addedCount = 0;

    [ObservableProperty]
    private int _removedCount = 0;

    [ObservableProperty]
    private int _modifiedCount = 0;

    [ObservableProperty]
    private string _summaryMessage = "비교를 시작하면 결과가 여기에 표시됩니다.";

    [ObservableProperty]
    private LogicChange? _selectedLogicChange;

    [ObservableProperty]
    private VariableChange? _selectedVariableChange;

    [ObservableProperty]
    private DataTypeChange? _selectedDataTypeChange;

    // ChangeType별 그룹화된 변수 목록
    public ObservableCollection<VariableChange> AddedVariables { get; } = new();
    public ObservableCollection<VariableChange> RemovedVariables { get; } = new();
    public ObservableCollection<VariableChange> ModifiedVariables { get; } = new();

    public ObservableCollection<VariableChange> VariableChanges { get; } = new();
    public ObservableCollection<IOMappingChange> IOMappingChanges { get; } = new();
    public ObservableCollection<LogicChange> LogicChanges { get; } = new();
    public ObservableCollection<DataTypeChange> DataTypeChanges { get; } = new();

    public FolderComparisonViewModel()
    {
        _logger = NullLogger<FolderComparisonViewModel>.Instance;
        _folderComparer = new FolderComparer();
    }

    public FolderComparisonViewModel(IFolderComparer folderComparer, ILogger<FolderComparisonViewModel> logger)
    {
        _folderComparer = folderComparer;
        _logger = logger;
    }

    [RelayCommand]
    private void BrowseSourceFolder()
    {
        var dialog = new VistaFolderBrowserDialog
        {
            Description = "Source 폴더를 선택하세요 (이전 버전)",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };

        if (!string.IsNullOrEmpty(SourceFolderPath) && Directory.Exists(SourceFolderPath))
        {
            dialog.SelectedPath = SourceFolderPath;
        }

        if (dialog.ShowDialog() == true)
        {
            SourceFolderPath = dialog.SelectedPath;
            StatusMessage = $"Source 폴더 선택됨: {SourceFolderPath}";
            _logger.LogInformation("Source 폴더 선택: {Path}", SourceFolderPath);
        }
    }

    [RelayCommand]
    private void BrowseTargetFolder()
    {
        var dialog = new VistaFolderBrowserDialog
        {
            Description = "Target 폴더를 선택하세요 (새 버전)",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };

        if (!string.IsNullOrEmpty(TargetFolderPath) && Directory.Exists(TargetFolderPath))
        {
            dialog.SelectedPath = TargetFolderPath;
        }

        if (dialog.ShowDialog() == true)
        {
            TargetFolderPath = dialog.SelectedPath;
            StatusMessage = $"Target 폴더 선택됨: {TargetFolderPath}";
            _logger.LogInformation("Target 폴더 선택: {Path}", TargetFolderPath);
        }
    }

    [RelayCommand(CanExecute = nameof(CanCompare))]
    private async Task CompareAsync()
    {
        if (!ValidateFolderPaths())
        {
            return;
        }

        IsComparing = true;
        StatusMessage = "폴더를 비교하는 중입니다...";
        ClearResults();

        try
        {
            _logger.LogInformation("폴더 비교 시작: {Source} vs {Target}", SourceFolderPath, TargetFolderPath);

            var options = new CompareOptions
            {
                IncludeVariables = IncludeVariables,
                IncludeIOMapping = IncludeIOMapping,
                IncludeLogic = IncludeLogic,
                IncludeDataTypes = IncludeDataTypes
            };

            var result = await _folderComparer.CompareAsync(SourceFolderPath, TargetFolderPath, options);

            // 결과를 ObservableCollection에 추가
            foreach (var change in result.VariableChanges)
            {
                VariableChanges.Add(change);

                // ChangeType별 분류
                switch (change.ChangeType)
                {
                    case ChangeType.Added:
                        AddedVariables.Add(change);
                        break;
                    case ChangeType.Removed:
                        RemovedVariables.Add(change);
                        break;
                    case ChangeType.Modified:
                        ModifiedVariables.Add(change);
                        break;
                }
            }

            foreach (var change in result.IOMappingChanges)
            {
                IOMappingChanges.Add(change);
            }

            foreach (var change in result.LogicChanges)
            {
                LogicChanges.Add(change);
            }

            foreach (var change in result.DataTypeChanges)
            {
                DataTypeChanges.Add(change);
            }

            // 통계 업데이트
            TotalChanges = result.TotalChanges;
            AddedCount = result.AddedCount;
            RemovedCount = result.RemovedCount;
            ModifiedCount = result.ModifiedCount;

            // 요약 메시지 생성
            if (result.HasChanges)
            {
                SummaryMessage = $"비교 완료: {TotalChanges}개의 변경사항이 발견되었습니다.\n\n";
                SummaryMessage += $"[+] 추가: {AddedCount}개\n";
                SummaryMessage += $"[-] 삭제: {RemovedCount}개\n";
                SummaryMessage += $"[~] 수정: {ModifiedCount}개\n\n";
                SummaryMessage += $"비교 시각: {result.ComparedAt:yyyy-MM-dd HH:mm:ss}";

                StatusMessage = $"비교 완료: {TotalChanges}개의 변경사항 발견";
            }
            else
            {
                SummaryMessage = "두 폴더의 내용이 동일합니다. 변경사항이 없습니다.";
                StatusMessage = "비교 완료: 변경사항 없음";
            }

            _logger.LogInformation("폴더 비교 완료: {TotalChanges}개 변경", TotalChanges);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "폴더 비교 중 오류 발생");
            StatusMessage = $"오류: {ex.Message}";
            SummaryMessage = $"비교 중 오류가 발생했습니다:\n{ex.Message}";

            MessageBox.Show(
                $"폴더 비교 중 오류가 발생했습니다:\n\n{ex.Message}",
                "오류",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            IsComparing = false;
        }
    }

    private bool CanCompare()
    {
        return !string.IsNullOrWhiteSpace(SourceFolderPath) &&
               !string.IsNullOrWhiteSpace(TargetFolderPath) &&
               !IsComparing;
    }

    private bool ValidateFolderPaths()
    {
        if (string.IsNullOrWhiteSpace(SourceFolderPath))
        {
            MessageBox.Show("Source 폴더를 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (string.IsNullOrWhiteSpace(TargetFolderPath))
        {
            MessageBox.Show("Target 폴더를 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (!Directory.Exists(SourceFolderPath))
        {
            MessageBox.Show($"Source 폴더가 존재하지 않습니다:\n{SourceFolderPath}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        if (!Directory.Exists(TargetFolderPath))
        {
            MessageBox.Show($"Target 폴더가 존재하지 않습니다:\n{TargetFolderPath}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        if (string.Equals(SourceFolderPath, TargetFolderPath, StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("Source와 Target 폴더가 동일합니다. 서로 다른 폴더를 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        return true;
    }

    private void ClearResults()
    {
        VariableChanges.Clear();
        AddedVariables.Clear();
        RemovedVariables.Clear();
        ModifiedVariables.Clear();
        IOMappingChanges.Clear();
        LogicChanges.Clear();
        DataTypeChanges.Clear();

        TotalChanges = 0;
        AddedCount = 0;
        RemovedCount = 0;
        ModifiedCount = 0;
        SummaryMessage = "비교 중...";
    }

    partial void OnSourceFolderPathChanged(string value)
    {
        CompareCommand.NotifyCanExecuteChanged();
    }

    partial void OnTargetFolderPathChanged(string value)
    {
        CompareCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsComparingChanged(bool value)
    {
        CompareCommand.NotifyCanExecuteChanged();
    }
}
