namespace TwinCatQA.Domain.Models.AST;

/// <summary>
/// 구문 트리 (Syntax Tree)
/// 파일 단위의 AST를 나타냅니다.
/// </summary>
public class SyntaxTree
{
    /// <summary>
    /// 파일 경로
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// 소스 코드
    /// </summary>
    public string SourceCode { get; set; } = string.Empty;

    /// <summary>
    /// 루트 노드 (프로그램 단위 노드들)
    /// </summary>
    public List<ASTNode> RootNodes { get; set; } = new();

    /// <summary>
    /// 파싱 오류 목록
    /// </summary>
    public List<ParsingError> Errors { get; set; } = new();

    /// <summary>
    /// 파싱 성공 여부
    /// </summary>
    public bool IsValid => Errors.Count == 0;
}

/// <summary>
/// 파싱 오류
/// </summary>
public class ParsingError
{
    /// <summary>
    /// 오류 라인
    /// </summary>
    public int Line { get; init; }

    /// <summary>
    /// 오류 컬럼
    /// </summary>
    public int Column { get; init; }

    /// <summary>
    /// 오류 메시지
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// 문제가 된 토큰
    /// </summary>
    public string OffendingSymbol { get; init; } = string.Empty;

    public override string ToString()
        => $"라인 {Line}:{Column} - {Message} (토큰: '{OffendingSymbol}')";
}
