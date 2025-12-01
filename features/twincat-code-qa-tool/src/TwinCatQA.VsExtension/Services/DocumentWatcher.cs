using System.Collections.Concurrent;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace TwinCatQA.VsExtension.Services;

/// <summary>
/// 문서 변경 감지 및 자동 분석 트리거
/// Visual Studio 텍스트 에디터 이벤트 구독
/// </summary>
public class DocumentWatcher : IDisposable
{
    private readonly IAnalysisService _analysisService;
    private readonly ConcurrentDictionary<string, DocumentState> _documentStates;
    private readonly Timer _debounceTimer;
    private readonly object _lockObject = new();
    private readonly TimeSpan _debounceDelay;
    private bool _disposed;

    #region 이벤트

    /// <summary>
    /// 분석 완료 이벤트
    /// </summary>
    public event EventHandler<AnalysisCompletedEventArgs>? AnalysisCompleted;

    /// <summary>
    /// 분석 시작 이벤트
    /// </summary>
    public event EventHandler<AnalysisStartedEventArgs>? AnalysisStarted;

    #endregion

    /// <summary>
    /// 문서 감시자 생성자
    /// </summary>
    /// <param name="analysisService">분석 서비스</param>
    /// <param name="debounceDelayMs">디바운스 지연 시간 (밀리초, 기본 500ms)</param>
    public DocumentWatcher(
        IAnalysisService analysisService,
        int debounceDelayMs = 500)
    {
        _analysisService = analysisService ?? throw new ArgumentNullException(nameof(analysisService));
        _documentStates = new ConcurrentDictionary<string, DocumentState>();
        _debounceDelay = TimeSpan.FromMilliseconds(debounceDelayMs);
        _debounceTimer = new Timer(OnDebounceTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);
    }

    #region 문서 추적

    /// <summary>
    /// 텍스트 뷰 추적 시작
    /// </summary>
    /// <param name="textView">Visual Studio 텍스트 뷰</param>
    public void WatchTextView(IWpfTextView textView)
    {
        if (textView == null)
            throw new ArgumentNullException(nameof(textView));

        var filePath = GetFilePathFromTextView(textView);
        if (string.IsNullOrEmpty(filePath) || !IsTcPouFile(filePath))
            return;

        // 문서 상태 초기화
        var state = _documentStates.GetOrAdd(filePath, _ => new DocumentState
        {
            FilePath = filePath,
            LastModified = DateTime.UtcNow,
            TextView = textView
        });

        // 텍스트 변경 이벤트 구독
        textView.TextBuffer.Changed += OnTextBufferChanged;
        textView.Closed += OnTextViewClosed;
    }

    /// <summary>
    /// 텍스트 뷰 추적 중지
    /// </summary>
    public void UnwatchTextView(IWpfTextView textView)
    {
        if (textView == null)
            return;

        var filePath = GetFilePathFromTextView(textView);
        if (string.IsNullOrEmpty(filePath))
            return;

        // 이벤트 구독 해제
        textView.TextBuffer.Changed -= OnTextBufferChanged;
        textView.Closed -= OnTextViewClosed;

        // 상태 제거
        _documentStates.TryRemove(filePath, out _);

        // 캐시 무효화
        _analysisService.InvalidateCache(filePath);
    }

    #endregion

    #region 이벤트 핸들러

    /// <summary>
    /// 텍스트 버퍼 변경 핸들러
    /// </summary>
    private void OnTextBufferChanged(object? sender, TextContentChangedEventArgs e)
    {
        if (sender is not ITextBuffer textBuffer)
            return;

        var filePath = GetFilePathFromTextBuffer(textBuffer);
        if (string.IsNullOrEmpty(filePath))
            return;

        if (!_documentStates.TryGetValue(filePath, out var state))
            return;

        lock (_lockObject)
        {
            // 변경된 라인 추적
            foreach (var change in e.Changes)
            {
                var startLine = e.Before.GetLineFromPosition(change.OldPosition).LineNumber;
                var endLine = e.After.GetLineFromPosition(change.NewPosition).LineNumber;

                for (int line = startLine; line <= endLine; line++)
                {
                    state.ChangedLines.Add(line + 1); // 1-based 인덱스
                }
            }

            state.LastModified = DateTime.UtcNow;
            state.IsDirty = true;

            // 캐시 무효화
            _analysisService.InvalidateCache(filePath);

            // 디바운스 타이머 리셋
            _debounceTimer.Change(_debounceDelay, Timeout.InfiniteTimeSpan);
        }
    }

    /// <summary>
    /// 텍스트 뷰 닫힘 핸들러
    /// </summary>
    private void OnTextViewClosed(object? sender, EventArgs e)
    {
        if (sender is IWpfTextView textView)
        {
            UnwatchTextView(textView);
        }
    }

    /// <summary>
    /// 디바운스 타이머 만료 핸들러
    /// </summary>
    private async void OnDebounceTimerElapsed(object? state)
    {
        // 변경된 모든 문서 분석
        var dirtyDocuments = _documentStates.Values
            .Where(d => d.IsDirty)
            .ToList();

        foreach (var doc in dirtyDocuments)
        {
            await AnalyzeDocumentAsync(doc);
        }
    }

    #endregion

    #region 분석 실행

    /// <summary>
    /// 문서 분석 실행
    /// </summary>
    private async Task AnalyzeDocumentAsync(DocumentState state)
    {
        if (state.IsAnalyzing)
            return;

        state.IsAnalyzing = true;
        state.IsDirty = false;

        try
        {
            // 분석 시작 이벤트 발생
            AnalysisStarted?.Invoke(this, new AnalysisStartedEventArgs
            {
                FilePath = state.FilePath
            });

            // 증분 분석 (변경된 라인만)
            var violations = await _analysisService.AnalyzeIncrementalAsync(
                state.FilePath,
                state.ChangedLines);

            // 분석 완료 이벤트 발생
            AnalysisCompleted?.Invoke(this, new AnalysisCompletedEventArgs
            {
                FilePath = state.FilePath,
                Violations = violations,
                AnalysisType = AnalysisType.Incremental
            });

            // 변경된 라인 목록 초기화
            state.ChangedLines.Clear();
        }
        catch (Exception ex)
        {
            // 분석 오류 이벤트 발생
            AnalysisCompleted?.Invoke(this, new AnalysisCompletedEventArgs
            {
                FilePath = state.FilePath,
                Violations = new List<Domain.Models.Violation>(),
                Error = ex,
                AnalysisType = AnalysisType.Incremental
            });
        }
        finally
        {
            state.IsAnalyzing = false;
        }
    }

    /// <summary>
    /// 수동 분석 트리거 (전체 분석)
    /// </summary>
    public async Task TriggerAnalysisAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        AnalysisStarted?.Invoke(this, new AnalysisStartedEventArgs
        {
            FilePath = filePath
        });

        try
        {
            var violations = await _analysisService.AnalyzeDocumentAsync(filePath);

            AnalysisCompleted?.Invoke(this, new AnalysisCompletedEventArgs
            {
                FilePath = filePath,
                Violations = violations,
                AnalysisType = AnalysisType.Full
            });
        }
        catch (Exception ex)
        {
            AnalysisCompleted?.Invoke(this, new AnalysisCompletedEventArgs
            {
                FilePath = filePath,
                Violations = new List<Domain.Models.Violation>(),
                Error = ex,
                AnalysisType = AnalysisType.Full
            });
        }
    }

    #endregion

    #region 유틸리티

    /// <summary>
    /// 텍스트 뷰에서 파일 경로 추출
    /// </summary>
    private string GetFilePathFromTextView(ITextView textView)
    {
        return textView.TextBuffer.Properties.TryGetProperty<ITextDocument>(
            typeof(ITextDocument),
            out var document)
            ? document.FilePath
            : string.Empty;
    }

    /// <summary>
    /// 텍스트 버퍼에서 파일 경로 추출
    /// </summary>
    private string GetFilePathFromTextBuffer(ITextBuffer textBuffer)
    {
        return textBuffer.Properties.TryGetProperty<ITextDocument>(
            typeof(ITextDocument),
            out var document)
            ? document.FilePath
            : string.Empty;
    }

    /// <summary>
    /// TcPOU 파일 여부 확인
    /// </summary>
    private bool IsTcPouFile(string filePath)
    {
        return filePath.EndsWith(".TcPOU", StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed)
            return;

        _debounceTimer?.Dispose();

        foreach (var state in _documentStates.Values)
        {
            if (state.TextView != null)
            {
                state.TextView.TextBuffer.Changed -= OnTextBufferChanged;
                state.TextView.Closed -= OnTextViewClosed;
            }
        }

        _documentStates.Clear();
        _disposed = true;
    }

    #endregion
}

#region 내부 클래스

/// <summary>
/// 문서 상태 추적
/// </summary>
internal class DocumentState
{
    public string FilePath { get; init; } = string.Empty;
    public DateTime LastModified { get; set; }
    public HashSet<int> ChangedLines { get; } = new();
    public bool IsDirty { get; set; }
    public bool IsAnalyzing { get; set; }
    public IWpfTextView? TextView { get; init; }
}

/// <summary>
/// 분석 시작 이벤트 인자
/// </summary>
public class AnalysisStartedEventArgs : EventArgs
{
    public string FilePath { get; init; } = string.Empty;
}

/// <summary>
/// 분석 완료 이벤트 인자
/// </summary>
public class AnalysisCompletedEventArgs : EventArgs
{
    public string FilePath { get; init; } = string.Empty;
    public List<Domain.Models.Violation> Violations { get; init; } = new();
    public Exception? Error { get; init; }
    public AnalysisType AnalysisType { get; init; }
}

/// <summary>
/// 분석 타입
/// </summary>
public enum AnalysisType
{
    /// <summary>전체 분석</summary>
    Full,
    /// <summary>증분 분석</summary>
    Incremental
}

#endregion
