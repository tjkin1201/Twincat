namespace TwinCatQA.Domain.Models.AST;

/// <summary>
/// 데이터 타입 선언 노드
/// TYPE T_MyStruct : STRUCT ... END_STRUCT END_TYPE
/// </summary>
public class DataTypeDeclarationNode : ASTNode
{
    /// <summary>
    /// 타입 이름
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 타입 종류
    /// </summary>
    public DataTypeKind Kind { get; init; }

    /// <summary>
    /// 타입 정의 (Struct, Enum, Union 또는 Alias)
    /// </summary>
    public ASTNode TypeDefinition { get; init; } = null!;

    public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(IASTVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// 구조체 타입 노드
/// STRUCT ... END_STRUCT
/// </summary>
public class StructTypeNode : ASTNode
{
    /// <summary>
    /// 필드 목록
    /// </summary>
    public List<VariableDeclarationNode> Fields { get; init; } = new();

    public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(IASTVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// 열거형 타입 노드
/// (Idle := 0, Running := 1, Error := 99)
/// </summary>
public class EnumTypeNode : ASTNode
{
    /// <summary>
    /// 열거 값 목록
    /// </summary>
    public List<EnumValue> Values { get; init; } = new();

    public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(IASTVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// 열거형 값
/// </summary>
public class EnumValue
{
    /// <summary>
    /// 열거 값 이름
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 명시적 값 (선택적)
    /// </summary>
    public int? ExplicitValue { get; init; }

    /// <summary>
    /// 위치 정보
    /// </summary>
    public int StartLine { get; init; }
}
