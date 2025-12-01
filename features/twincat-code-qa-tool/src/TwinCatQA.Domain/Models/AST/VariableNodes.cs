namespace TwinCatQA.Domain.Models.AST;

/// <summary>
/// 변수 선언 목록 노드
/// VAR ... END_VAR, VAR_INPUT ... END_VAR 등
/// </summary>
public class VariableDeclarationListNode : ASTNode
{
    /// <summary>
    /// 변수 스코프 (Input, Output, Local 등)
    /// </summary>
    public VariableScope Scope { get; init; }

    /// <summary>
    /// 상수 여부 (VAR CONSTANT)
    /// </summary>
    public bool IsConstant { get; init; }

    /// <summary>
    /// 변수 선언 목록
    /// </summary>
    public List<VariableDeclarationNode> Variables { get; init; } = new();

    public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(IASTVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// 개별 변수 선언 노드
/// counter : INT := 0;
/// </summary>
public class VariableDeclarationNode : ASTNode
{
    /// <summary>
    /// 변수명
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 데이터 타입
    /// </summary>
    public string DataType { get; init; } = string.Empty;

    /// <summary>
    /// 초기값 표현식 (선택적)
    /// </summary>
    public ExpressionNode? InitialValue { get; init; }

    /// <summary>
    /// 배열 여부
    /// </summary>
    public bool IsArray { get; init; }

    /// <summary>
    /// 배열 범위 (예: [0..9])
    /// </summary>
    public List<ArrayRange>? ArrayRanges { get; init; }

    /// <summary>
    /// 포인터 여부
    /// </summary>
    public bool IsPointer { get; init; }

    /// <summary>
    /// 레퍼런스 여부
    /// </summary>
    public bool IsReference { get; init; }

    public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(IASTVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// 배열 범위
/// </summary>
public record ArrayRange(int LowerBound, int UpperBound);
