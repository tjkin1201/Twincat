namespace TwinCatQA.Infrastructure.Git;

/// <summary>
/// 변경된 라인의 코드 컨텍스트 정보
/// </summary>
public class CodeContext
{
    /// <summary>
    /// 컨텍스트 시작 라인 번호
    /// </summary>
    public int StartLine { get; set; }

    /// <summary>
    /// 컨텍스트 종료 라인 번호
    /// </summary>
    public int EndLine { get; set; }

    /// <summary>
    /// 컨텍스트 유형 (FunctionBlock, ControlStructure, Surrounding)
    /// </summary>
    public string ContextType { get; set; } = "Surrounding";

    /// <summary>
    /// 컨텍스트 이름 (FB 이름, 블록 유형 등)
    /// </summary>
    public string? ContextName { get; set; }
}
