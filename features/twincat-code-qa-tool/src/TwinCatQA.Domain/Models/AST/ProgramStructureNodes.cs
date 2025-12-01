namespace TwinCatQA.Domain.Models.AST;

/// <summary>
/// PROGRAM 선언 노드
/// PROGRAM PLC_Main ... END_PROGRAM
/// </summary>
public class ProgramNode : ASTNode
{
    /// <summary>
    /// 프로그램 이름
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 변수 선언 목록
    /// </summary>
    public List<VariableDeclarationListNode> VariableDeclarations { get; init; } = new();

    /// <summary>
    /// 구문 목록
    /// </summary>
    public List<StatementNode> Statements { get; init; } = new();

    public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(IASTVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// FUNCTION_BLOCK 선언 노드
/// FUNCTION_BLOCK FB_Example ... END_FUNCTION_BLOCK
/// </summary>
public class FunctionBlockNode : ASTNode
{
    /// <summary>
    /// Function Block 이름
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 변수 선언 목록
    /// </summary>
    public List<VariableDeclarationListNode> VariableDeclarations { get; init; } = new();

    /// <summary>
    /// 구문 목록
    /// </summary>
    public List<StatementNode> Statements { get; init; } = new();

    public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(IASTVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// FUNCTION 선언 노드
/// FUNCTION Add : INT ... END_FUNCTION
/// </summary>
public class FunctionNode : ASTNode
{
    /// <summary>
    /// 함수 이름
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 반환 타입
    /// </summary>
    public string ReturnType { get; init; } = string.Empty;

    /// <summary>
    /// 변수 선언 목록
    /// </summary>
    public List<VariableDeclarationListNode> VariableDeclarations { get; init; } = new();

    /// <summary>
    /// 구문 목록
    /// </summary>
    public List<StatementNode> Statements { get; init; } = new();

    public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(IASTVisitor<T> visitor) => visitor.Visit(this);
}
