namespace TwinCatQA.Domain.Models.AST;

/// <summary>
/// 구문 노드의 기본 추상 클래스
/// </summary>
public abstract class StatementNode : ASTNode
{
}

/// <summary>
/// 할당문 노드
/// variable := expression;
/// </summary>
public class AssignmentStatementNode : StatementNode
{
    /// <summary>
    /// 좌변 (변수)
    /// </summary>
    public VariableReferenceNode Left { get; init; } = null!;

    /// <summary>
    /// 우변 (표현식)
    /// </summary>
    public ExpressionNode Right { get; init; } = null!;

    public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(IASTVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// IF 문 노드
/// IF condition THEN ... ELSIF ... ELSE ... END_IF
/// </summary>
public class IfStatementNode : StatementNode
{
    /// <summary>
    /// IF 조건
    /// </summary>
    public ExpressionNode Condition { get; init; } = null!;

    /// <summary>
    /// THEN 블록
    /// </summary>
    public List<StatementNode> ThenBlock { get; init; } = new();

    /// <summary>
    /// ELSIF 블록 목록
    /// </summary>
    public List<ElsifClause> ElsifClauses { get; init; } = new();

    /// <summary>
    /// ELSE 블록 (선택적)
    /// </summary>
    public List<StatementNode>? ElseBlock { get; init; }

    public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(IASTVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// ELSIF 절
/// </summary>
public class ElsifClause
{
    /// <summary>
    /// ELSIF 조건
    /// </summary>
    public ExpressionNode Condition { get; init; } = null!;

    /// <summary>
    /// ELSIF 블록
    /// </summary>
    public List<StatementNode> Statements { get; init; } = new();

    /// <summary>
    /// 위치 정보
    /// </summary>
    public int StartLine { get; init; }
    public int EndLine { get; init; }
}

/// <summary>
/// CASE 문 노드
/// CASE expression OF ... END_CASE
/// </summary>
public class CaseStatementNode : StatementNode
{
    /// <summary>
    /// CASE 표현식
    /// </summary>
    public ExpressionNode Expression { get; init; } = null!;

    /// <summary>
    /// CASE 분기 목록
    /// </summary>
    public List<CaseElement> CaseElements { get; init; } = new();

    /// <summary>
    /// ELSE 블록 (선택적)
    /// </summary>
    public List<StatementNode>? ElseBlock { get; init; }

    public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(IASTVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// CASE 분기
/// </summary>
public class CaseElement
{
    /// <summary>
    /// 상수 값 목록 (예: 1, 2, 3:)
    /// </summary>
    public List<ExpressionNode> Values { get; init; } = new();

    /// <summary>
    /// 실행할 구문 목록
    /// </summary>
    public List<StatementNode> Statements { get; init; } = new();

    /// <summary>
    /// 위치 정보
    /// </summary>
    public int StartLine { get; init; }
    public int EndLine { get; init; }
}

/// <summary>
/// FOR 루프 노드
/// FOR i := 1 TO 10 BY 1 DO ... END_FOR
/// </summary>
public class ForStatementNode : StatementNode
{
    /// <summary>
    /// 루프 변수명
    /// </summary>
    public string LoopVariable { get; init; } = string.Empty;

    /// <summary>
    /// 시작 값
    /// </summary>
    public ExpressionNode StartValue { get; init; } = null!;

    /// <summary>
    /// 종료 값
    /// </summary>
    public ExpressionNode EndValue { get; init; } = null!;

    /// <summary>
    /// 증가값 (선택적, 기본값 1)
    /// </summary>
    public ExpressionNode? StepValue { get; init; }

    /// <summary>
    /// 루프 본문
    /// </summary>
    public List<StatementNode> Body { get; init; } = new();

    public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(IASTVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// WHILE 루프 노드
/// WHILE condition DO ... END_WHILE
/// </summary>
public class WhileStatementNode : StatementNode
{
    /// <summary>
    /// 조건식
    /// </summary>
    public ExpressionNode Condition { get; init; } = null!;

    /// <summary>
    /// 루프 본문
    /// </summary>
    public List<StatementNode> Body { get; init; } = new();

    public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(IASTVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// REPEAT 루프 노드
/// REPEAT ... UNTIL condition END_REPEAT
/// </summary>
public class RepeatStatementNode : StatementNode
{
    /// <summary>
    /// 루프 본문
    /// </summary>
    public List<StatementNode> Body { get; init; } = new();

    /// <summary>
    /// 종료 조건 (UNTIL)
    /// </summary>
    public ExpressionNode UntilCondition { get; init; } = null!;

    public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(IASTVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// EXIT 문 노드
/// </summary>
public class ExitStatementNode : StatementNode
{
    public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(IASTVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// RETURN 문 노드
/// </summary>
public class ReturnStatementNode : StatementNode
{
    public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(IASTVisitor<T> visitor) => visitor.Visit(this);
}
