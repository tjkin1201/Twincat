namespace TwinCatQA.Domain.Models.AST;

/// <summary>
/// 표현식 노드의 기본 추상 클래스
/// </summary>
public abstract class ExpressionNode : ASTNode
{
    /// <summary>
    /// 추론된 타입 (의미 분석 후 채워짐)
    /// </summary>
    public string? InferredType { get; set; }
}

/// <summary>
/// 이항 연산 노드
/// left operator right
/// </summary>
public class BinaryExpressionNode : ExpressionNode
{
    /// <summary>
    /// 좌측 피연산자
    /// </summary>
    public ExpressionNode Left { get; init; } = null!;

    /// <summary>
    /// 우측 피연산자
    /// </summary>
    public ExpressionNode Right { get; init; } = null!;

    /// <summary>
    /// 연산자
    /// </summary>
    public BinaryOperator Operator { get; init; }

    public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(IASTVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// 단항 연산 노드
/// operator operand
/// </summary>
public class UnaryExpressionNode : ExpressionNode
{
    /// <summary>
    /// 피연산자
    /// </summary>
    public ExpressionNode Operand { get; init; } = null!;

    /// <summary>
    /// 연산자
    /// </summary>
    public UnaryOperator Operator { get; init; }

    public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(IASTVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// 리터럴 노드
/// 정수, 실수, 문자열, 불리언 등
/// </summary>
public class LiteralNode : ExpressionNode
{
    /// <summary>
    /// 리터럴 타입
    /// </summary>
    public LiteralType Type { get; init; }

    /// <summary>
    /// 리터럴 값 (문자열로 저장)
    /// </summary>
    public string Value { get; init; } = string.Empty;

    public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(IASTVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// 변수 참조 노드
/// myVar, myStruct.field, myArray[0]
/// </summary>
public class VariableReferenceNode : ExpressionNode
{
    /// <summary>
    /// 변수명
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 구조체 필드 접근 (예: myStruct.field.subfield)
    /// </summary>
    public List<string>? FieldAccess { get; init; }

    /// <summary>
    /// 배열 인덱스 (예: myArray[i, j])
    /// </summary>
    public List<ExpressionNode>? ArrayIndices { get; init; }

    public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(IASTVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// 함수 호출 노드
/// MyFunction(arg1 := 10, arg2 := 20)
/// </summary>
public class FunctionCallNode : ExpressionNode
{
    /// <summary>
    /// 함수명
    /// </summary>
    public string FunctionName { get; init; } = string.Empty;

    /// <summary>
    /// 인자 목록
    /// </summary>
    public List<FunctionArgument> Arguments { get; init; } = new();

    public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(IASTVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// 함수 인자
/// </summary>
public class FunctionArgument
{
    /// <summary>
    /// 인자명 (Named parameter의 경우)
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// 인자 값
    /// </summary>
    public ExpressionNode Value { get; init; } = null!;
}

/// <summary>
/// 이항 연산자
/// </summary>
public enum BinaryOperator
{
    // 산술 연산자
    Add,        // +
    Subtract,   // -
    Multiply,   // *
    Divide,     // /
    Modulo,     // MOD

    // 비교 연산자
    Equal,              // =
    NotEqual,           // <>
    LessThan,           // <
    LessThanOrEqual,    // <=
    GreaterThan,        // >
    GreaterThanOrEqual, // >=

    // 논리 연산자
    And,        // AND, &
    Or,         // OR
    Xor         // XOR
}

/// <summary>
/// 단항 연산자
/// </summary>
public enum UnaryOperator
{
    Not,        // NOT
    Minus       // - (부호 반전)
}

/// <summary>
/// 리터럴 타입
/// </summary>
public enum LiteralType
{
    Integer,    // 123, 16#FF
    Real,       // 3.14, 1.5e-3
    String,     // 'Hello'
    WString,    // "World"
    Boolean,    // TRUE, FALSE
    Time        // T#10s, TIME#1h30m
}
