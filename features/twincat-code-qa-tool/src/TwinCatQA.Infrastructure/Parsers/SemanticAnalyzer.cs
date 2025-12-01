using System;
using System.Collections.Generic;
using System.Linq;
using TwinCatQA.Domain.Models.AST;

namespace TwinCatQA.Infrastructure.Parsers;

/// <summary>
/// 의미 분석 오류
/// </summary>
public class SemanticError
{
    /// <summary>
    /// 오류 메시지
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// 오류 위치 (라인 번호)
    /// </summary>
    public int Line { get; init; }

    /// <summary>
    /// 오류 위치 (컬럼 번호)
    /// </summary>
    public int Column { get; init; }

    /// <summary>
    /// 오류 타입
    /// </summary>
    public SemanticErrorType Type { get; init; }

    public override string ToString()
        => $"[{Type}] 라인 {Line}:{Column} - {Message}";
}

/// <summary>
/// 의미 오류 타입
/// </summary>
public enum SemanticErrorType
{
    /// <summary>미선언 변수 참조</summary>
    UndeclaredVariable,

    /// <summary>중복 선언</summary>
    DuplicateDeclaration,

    /// <summary>타입 불일치</summary>
    TypeMismatch,

    /// <summary>초기화되지 않은 변수 사용</summary>
    UninitializedVariable,

    /// <summary>미사용 변수</summary>
    UnusedVariable,

    /// <summary>잘못된 함수 호출</summary>
    InvalidFunctionCall,

    /// <summary>기타</summary>
    Other
}

/// <summary>
/// AST 의미 분석기
/// 타입 체크, 스코프 분석, 변수 사용 분석 등을 수행합니다.
/// </summary>
public class SemanticAnalyzer : IASTVisitor
{
    private readonly SymbolTable _symbolTable = new();
    private readonly List<SemanticError> _errors = new();

    /// <summary>
    /// 분석 결과 오류 목록
    /// </summary>
    public IReadOnlyList<SemanticError> Errors => _errors;

    /// <summary>
    /// 심볼 테이블
    /// </summary>
    public SymbolTable SymbolTable => _symbolTable;

    /// <summary>
    /// AST 분석 시작
    /// </summary>
    public void Analyze(SyntaxTree syntaxTree)
    {
        _errors.Clear();
        _symbolTable.Clear();

        foreach (var node in syntaxTree.RootNodes)
        {
            node.Accept(this);
        }

        // 분석 후 미사용 변수 검사
        CheckUnusedVariables();
    }

    // ========================================================================
    // 프로그램 구조
    // ========================================================================

    public void Visit(ProgramNode node)
    {
        _symbolTable.EnterScope(node.Name);

        // 변수 선언 분석
        foreach (var varDeclList in node.VariableDeclarations)
        {
            Visit(varDeclList);
        }

        // 구문 분석
        foreach (var statement in node.Statements)
        {
            statement.Accept(this);
        }

        _symbolTable.ExitScope();
    }

    public void Visit(FunctionBlockNode node)
    {
        _symbolTable.EnterScope(node.Name);

        // 변수 선언 분석
        foreach (var varDeclList in node.VariableDeclarations)
        {
            Visit(varDeclList);
        }

        // 구문 분석
        foreach (var statement in node.Statements)
        {
            statement.Accept(this);
        }

        _symbolTable.ExitScope();
    }

    public void Visit(FunctionNode node)
    {
        _symbolTable.EnterScope(node.Name);

        // 반환 변수 자동 선언 (함수명과 동일)
        _symbolTable.Declare(node.Name, node.ReturnType, node.StartLine);

        // 변수 선언 분석
        foreach (var varDeclList in node.VariableDeclarations)
        {
            Visit(varDeclList);
        }

        // 구문 분석
        foreach (var statement in node.Statements)
        {
            statement.Accept(this);
        }

        _symbolTable.ExitScope();
    }

    // ========================================================================
    // 변수 선언
    // ========================================================================

    public void Visit(VariableDeclarationListNode node)
    {
        foreach (var variable in node.Variables)
        {
            Visit(variable);
        }
    }

    public void Visit(VariableDeclarationNode node)
    {
        // 중복 선언 체크
        if (_symbolTable.IsDeclaredInCurrentScope(node.Name))
        {
            _errors.Add(new SemanticError
            {
                Message = $"변수 '{node.Name}'이(가) 이미 선언되었습니다.",
                Line = node.StartLine,
                Column = node.StartColumn,
                Type = SemanticErrorType.DuplicateDeclaration
            });
        }
        else
        {
            _symbolTable.Declare(node.Name, node.DataType, node.StartLine);
        }

        // 초기값이 있으면 초기화 표시
        if (node.InitialValue != null)
        {
            _symbolTable.MarkAsInitialized(node.Name);

            // 초기값 타입 체크
            node.InitialValue.Accept(this);
            var initType = node.InitialValue.InferredType;

            if (initType != null && !AreTypesCompatible(node.DataType, initType))
            {
                _errors.Add(new SemanticError
                {
                    Message = $"초기값 타입 불일치: {node.DataType} ← {initType}",
                    Line = node.StartLine,
                    Column = node.StartColumn,
                    Type = SemanticErrorType.TypeMismatch
                });
            }
        }
    }

    // ========================================================================
    // 구문 (Statements)
    // ========================================================================

    public void Visit(AssignmentStatementNode node)
    {
        // 좌변 변수 체크
        Visit(node.Left);
        var leftType = node.Left.InferredType;

        // 우변 표현식 체크
        node.Right.Accept(this);
        var rightType = node.Right.InferredType;

        // 타입 호환성 검사
        if (leftType != null && rightType != null && !AreTypesCompatible(leftType, rightType))
        {
            _errors.Add(new SemanticError
            {
                Message = $"타입 불일치: {leftType} ← {rightType}",
                Line = node.StartLine,
                Column = node.StartColumn,
                Type = SemanticErrorType.TypeMismatch
            });
        }

        // 좌변 변수를 초기화 표시
        _symbolTable.MarkAsInitialized(node.Left.Name);
    }

    public void Visit(IfStatementNode node)
    {
        // 조건식 분석
        node.Condition.Accept(this);

        // 조건식이 BOOL 타입인지 체크
        if (node.Condition.InferredType != null && node.Condition.InferredType != "BOOL")
        {
            _errors.Add(new SemanticError
            {
                Message = $"IF 조건식은 BOOL 타입이어야 합니다. (현재: {node.Condition.InferredType})",
                Line = node.StartLine,
                Column = node.StartColumn,
                Type = SemanticErrorType.TypeMismatch
            });
        }

        // THEN 블록 분석
        foreach (var statement in node.ThenBlock)
        {
            statement.Accept(this);
        }

        // ELSIF 블록 분석
        foreach (var elsif in node.ElsifClauses)
        {
            elsif.Condition.Accept(this);
            foreach (var statement in elsif.Statements)
            {
                statement.Accept(this);
            }
        }

        // ELSE 블록 분석
        if (node.ElseBlock != null)
        {
            foreach (var statement in node.ElseBlock)
            {
                statement.Accept(this);
            }
        }
    }

    public void Visit(CaseStatementNode node)
    {
        // CASE 표현식 분석
        node.Expression.Accept(this);

        // 각 분기 분석
        foreach (var caseElement in node.CaseElements)
        {
            foreach (var value in caseElement.Values)
            {
                value.Accept(this);
            }

            foreach (var statement in caseElement.Statements)
            {
                statement.Accept(this);
            }
        }

        // ELSE 블록 분석
        if (node.ElseBlock != null)
        {
            foreach (var statement in node.ElseBlock)
            {
                statement.Accept(this);
            }
        }
    }

    public void Visit(ForStatementNode node)
    {
        // 루프 변수는 암시적으로 선언됨
        _symbolTable.Declare(node.LoopVariable, "INT", node.StartLine);

        // 시작값, 종료값, 증가값 분석
        node.StartValue.Accept(this);
        node.EndValue.Accept(this);
        node.StepValue?.Accept(this);

        // 루프 본문 분석
        foreach (var statement in node.Body)
        {
            statement.Accept(this);
        }
    }

    public void Visit(WhileStatementNode node)
    {
        // 조건식 분석
        node.Condition.Accept(this);

        // 루프 본문 분석
        foreach (var statement in node.Body)
        {
            statement.Accept(this);
        }
    }

    public void Visit(RepeatStatementNode node)
    {
        // 루프 본문 분석
        foreach (var statement in node.Body)
        {
            statement.Accept(this);
        }

        // 종료 조건 분석
        node.UntilCondition.Accept(this);
    }

    public void Visit(ExitStatementNode node)
    {
        // EXIT는 특별한 분석 불필요
    }

    public void Visit(ReturnStatementNode node)
    {
        // RETURN은 특별한 분석 불필요
    }

    // ========================================================================
    // 표현식 (Expressions)
    // ========================================================================

    public void Visit(BinaryExpressionNode node)
    {
        // 좌우 피연산자 분석
        node.Left.Accept(this);
        node.Right.Accept(this);

        var leftType = node.Left.InferredType;
        var rightType = node.Right.InferredType;

        // 타입 추론
        if (leftType != null && rightType != null)
        {
            node.InferredType = InferBinaryExpressionType(leftType, rightType, node.Operator);
        }
    }

    public void Visit(UnaryExpressionNode node)
    {
        // 피연산자 분석
        node.Operand.Accept(this);

        // 타입 추론
        node.InferredType = node.Operand.InferredType;
    }

    public void Visit(LiteralNode node)
    {
        // 리터럴 타입 추론
        node.InferredType = node.Type switch
        {
            LiteralType.Integer => "INT",
            LiteralType.Real => "REAL",
            LiteralType.String => "STRING",
            LiteralType.WString => "WSTRING",
            LiteralType.Boolean => "BOOL",
            LiteralType.Time => "TIME",
            _ => "UNKNOWN"
        };
    }

    public void Visit(VariableReferenceNode node)
    {
        // 변수 선언 확인
        var symbol = _symbolTable.Lookup(node.Name);

        if (symbol == null)
        {
            _errors.Add(new SemanticError
            {
                Message = $"선언되지 않은 변수: '{node.Name}'",
                Line = node.StartLine,
                Column = node.StartColumn,
                Type = SemanticErrorType.UndeclaredVariable
            });

            node.InferredType = "UNKNOWN";
        }
        else
        {
            // 변수 사용 표시
            _symbolTable.MarkAsUsed(node.Name);

            // 타입 추론
            node.InferredType = symbol.Type;
        }

        // 배열 인덱스 분석
        if (node.ArrayIndices != null)
        {
            foreach (var index in node.ArrayIndices)
            {
                index.Accept(this);
            }
        }
    }

    public void Visit(FunctionCallNode node)
    {
        // 인자 분석
        foreach (var arg in node.Arguments)
        {
            arg.Value.Accept(this);
        }

        // 함수 반환 타입 추론 (여기서는 심볼 테이블에서 함수 정보가 필요)
        // 간단히 UNKNOWN으로 설정
        node.InferredType = "UNKNOWN";
    }

    // ========================================================================
    // 데이터 타입
    // ========================================================================

    public void Visit(DataTypeDeclarationNode node)
    {
        // 타입 정의 분석
        node.TypeDefinition.Accept(this);
    }

    public void Visit(StructTypeNode node)
    {
        // 필드 분석
        foreach (var field in node.Fields)
        {
            Visit(field);
        }
    }

    public void Visit(EnumTypeNode node)
    {
        // 열거형 값 분석 (중복 체크 등)
        var valueNames = new HashSet<string>();

        foreach (var value in node.Values)
        {
            if (!valueNames.Add(value.Name))
            {
                _errors.Add(new SemanticError
                {
                    Message = $"열거형 값 '{value.Name}'이(가) 중복 선언되었습니다.",
                    Line = value.StartLine,
                    Column = 0,
                    Type = SemanticErrorType.DuplicateDeclaration
                });
            }
        }
    }

    // ========================================================================
    // 헬퍼 메서드
    // ========================================================================

    /// <summary>
    /// 타입 호환성 검사
    /// </summary>
    private bool AreTypesCompatible(string targetType, string sourceType)
    {
        // 동일 타입
        if (targetType == sourceType)
            return true;

        // 숫자 타입 간 호환성 (간단한 구현)
        var numericTypes = new[] { "INT", "DINT", "REAL", "LREAL", "SINT", "USINT", "UINT", "UDINT", "LINT", "ULINT" };
        if (numericTypes.Contains(targetType) && numericTypes.Contains(sourceType))
            return true;

        return false;
    }

    /// <summary>
    /// 이항 연산 결과 타입 추론
    /// </summary>
    private string InferBinaryExpressionType(string leftType, string rightType, BinaryOperator op)
    {
        // 비교 연산자는 항상 BOOL 반환
        if (op >= BinaryOperator.Equal && op <= BinaryOperator.GreaterThanOrEqual)
        {
            return "BOOL";
        }

        // 논리 연산자는 BOOL 반환
        if (op == BinaryOperator.And || op == BinaryOperator.Or || op == BinaryOperator.Xor)
        {
            return "BOOL";
        }

        // 산술 연산자는 피연산자 타입 중 더 큰 타입 반환 (간단한 구현)
        if (leftType == "LREAL" || rightType == "LREAL")
            return "LREAL";
        if (leftType == "REAL" || rightType == "REAL")
            return "REAL";
        if (leftType == "DINT" || rightType == "DINT")
            return "DINT";

        return "INT";
    }

    /// <summary>
    /// 미사용 변수 검사
    /// </summary>
    private void CheckUnusedVariables()
    {
        foreach (var symbol in _symbolTable.GetUnusedVariables())
        {
            _errors.Add(new SemanticError
            {
                Message = $"미사용 변수: '{symbol.Name}'",
                Line = symbol.DeclarationLine,
                Column = 0,
                Type = SemanticErrorType.UnusedVariable
            });
        }
    }
}
