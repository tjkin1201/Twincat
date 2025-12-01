namespace TwinCatQA.Infrastructure.Git;

/// <summary>
/// Diff 비교 대상 열거형
/// </summary>
public enum DiffTarget
{
    /// <summary>
    /// 스테이징 영역 vs HEAD 커밋
    /// </summary>
    Index,

    /// <summary>
    /// 워킹 디렉토리 vs 스테이징 영역
    /// </summary>
    WorkingDirectory,

    /// <summary>
    /// 워킹 디렉토리 vs HEAD 커밋 (모든 변경사항)
    /// </summary>
    All
}
