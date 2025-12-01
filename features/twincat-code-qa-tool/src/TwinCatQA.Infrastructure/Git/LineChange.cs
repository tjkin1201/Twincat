namespace TwinCatQA.Infrastructure.Git;

/// <summary>
/// Git Diff에서 변경된 라인 정보
/// </summary>
public class LineChange
{
    /// <summary>
    /// 파일 경로 (저장소 루트 기준 상대 경로)
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 라인 번호 (1부터 시작)
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// 변경 유형 (Added/Modified/Deleted)
    /// </summary>
    public LineChangeType ChangeType { get; set; }

    /// <summary>
    /// 라인 내용
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 이전 라인 번호 (수정/삭제의 경우)
    /// </summary>
    public int? OldLineNumber { get; set; }
}
