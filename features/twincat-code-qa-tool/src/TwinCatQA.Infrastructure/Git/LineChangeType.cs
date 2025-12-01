namespace TwinCatQA.Infrastructure.Git;

/// <summary>
/// 라인 변경 유형
/// </summary>
public enum LineChangeType
{
    /// <summary>
    /// 추가된 라인
    /// </summary>
    Added,

    /// <summary>
    /// 수정된 라인
    /// </summary>
    Modified,

    /// <summary>
    /// 삭제된 라인
    /// </summary>
    Deleted
}
