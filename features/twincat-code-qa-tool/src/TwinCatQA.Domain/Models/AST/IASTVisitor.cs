namespace TwinCatQA.Domain.Models.AST;

/// <summary>
/// AST Visitor 인터페이스 (Visitor 패턴)
/// </summary>
public interface IASTVisitor
{
    // 프로그램 구조
    void Visit(ProgramNode node);
    void Visit(FunctionBlockNode node);
    void Visit(FunctionNode node);

    // 변수 선언
    void Visit(VariableDeclarationNode node);
    void Visit(VariableDeclarationListNode node);

    // 구문 (Statements)
    void Visit(AssignmentStatementNode node);
    void Visit(IfStatementNode node);
    void Visit(CaseStatementNode node);
    void Visit(ForStatementNode node);
    void Visit(WhileStatementNode node);
    void Visit(RepeatStatementNode node);
    void Visit(ExitStatementNode node);
    void Visit(ReturnStatementNode node);

    // 표현식 (Expressions)
    void Visit(BinaryExpressionNode node);
    void Visit(UnaryExpressionNode node);
    void Visit(LiteralNode node);
    void Visit(VariableReferenceNode node);
    void Visit(FunctionCallNode node);

    // 데이터 타입
    void Visit(DataTypeDeclarationNode node);
    void Visit(StructTypeNode node);
    void Visit(EnumTypeNode node);
}

/// <summary>
/// Generic AST Visitor 인터페이스 (반환 값 있음)
/// </summary>
public interface IASTVisitor<T>
{
    // 프로그램 구조
    T Visit(ProgramNode node);
    T Visit(FunctionBlockNode node);
    T Visit(FunctionNode node);

    // 변수 선언
    T Visit(VariableDeclarationNode node);
    T Visit(VariableDeclarationListNode node);

    // 구문 (Statements)
    T Visit(AssignmentStatementNode node);
    T Visit(IfStatementNode node);
    T Visit(CaseStatementNode node);
    T Visit(ForStatementNode node);
    T Visit(WhileStatementNode node);
    T Visit(RepeatStatementNode node);
    T Visit(ExitStatementNode node);
    T Visit(ReturnStatementNode node);

    // 표현식 (Expressions)
    T Visit(BinaryExpressionNode node);
    T Visit(UnaryExpressionNode node);
    T Visit(LiteralNode node);
    T Visit(VariableReferenceNode node);
    T Visit(FunctionCallNode node);

    // 데이터 타입
    T Visit(DataTypeDeclarationNode node);
    T Visit(StructTypeNode node);
    T Visit(EnumTypeNode node);
}
