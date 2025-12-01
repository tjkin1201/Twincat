namespace TwinCatQA.Domain.Models.AST;

/// <summary>
/// AST 노드의 기본 추상 클래스
/// 모든 AST 노드는 이 클래스를 상속받습니다.
/// </summary>
public abstract class ASTNode
{
    /// <summary>
    /// 노드가 속한 파일 경로
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// 시작 라인 번호
    /// </summary>
    public int StartLine { get; init; }

    /// <summary>
    /// 종료 라인 번호
    /// </summary>
    public int EndLine { get; init; }

    /// <summary>
    /// 시작 컬럼 번호
    /// </summary>
    public int StartColumn { get; init; }

    /// <summary>
    /// 종료 컬럼 번호
    /// </summary>
    public int EndColumn { get; init; }

    /// <summary>
    /// 부모 노드 (루트 노드의 경우 null)
    /// </summary>
    public ASTNode? Parent { get; set; }

    /// <summary>
    /// Visitor 패턴 구현
    /// </summary>
    public abstract void Accept(IASTVisitor visitor);

    /// <summary>
    /// Generic Visitor 패턴 구현
    /// </summary>
    public abstract T Accept<T>(IASTVisitor<T> visitor);
}
