using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Models.AST;

namespace TwinCatQA.Infrastructure.Parsers;

/// <summary>
/// ANTLR4 Parse Tree를 AST로 변환하는 Visitor
/// StructuredTextParser의 Parse Tree를 순회하며 도메인 AST 노드를 생성합니다.
/// </summary>
public class ASTBuilderVisitor : StructuredTextBaseVisitor<ASTNode>
{
    private readonly string _filePath;

    public ASTBuilderVisitor(string filePath)
    {
        _filePath = filePath;
    }

    // ========================================================================
    // 프로그램 구조
    // ========================================================================

    /// <summary>
    /// 프로그램 진입점 방문
    /// </summary>
    public override ASTNode VisitProgram(StructuredTextParser.ProgramContext context)
    {
        // 모든 프로그램 단위 노드를 수집
        var nodes = context.programUnit()
            .Select(pu => Visit(pu))
            .Where(node => node != null)
            .ToList();

        // 여러 노드를 반환하기 위해 첫 번째 노드를 반환하고,
        // 실제로는 SyntaxTree.RootNodes에 모두 추가됨
        return nodes.FirstOrDefault() ?? CreateEmptyNode(context);
    }

    /// <summary>
    /// 프로그램 단위 방문 (FB, Function, Program, Var, DataType 등)
    /// </summary>
    public override ASTNode VisitProgramUnit(StructuredTextParser.ProgramUnitContext context)
    {
        if (context.functionBlockDeclaration() != null)
            return VisitFunctionBlockDeclaration(context.functionBlockDeclaration());

        if (context.functionDeclaration() != null)
            return VisitFunctionDeclaration(context.functionDeclaration());

        if (context.programDeclaration() != null)
            return VisitProgramDeclaration(context.programDeclaration());

        if (context.varDeclaration() != null)
            return VisitVarDeclaration(context.varDeclaration());

        if (context.dataTypeDeclaration() != null)
            return VisitDataTypeDeclaration(context.dataTypeDeclaration());

        return CreateEmptyNode(context);
    }

    /// <summary>
    /// FUNCTION_BLOCK 선언 방문
    /// </summary>
    public override ASTNode VisitFunctionBlockDeclaration(StructuredTextParser.FunctionBlockDeclarationContext context)
    {
        var name = context.IDENTIFIER().GetText();

        // 변수 선언 목록
        var varDecls = context.varDeclaration()
            .Select(vd => (VariableDeclarationListNode)Visit(vd))
            .ToList();

        // 구문 목록
        var statements = context.statement()
            .Select(s => (StatementNode)Visit(s))
            .Where(s => s != null)
            .ToList();

        return new FunctionBlockNode
        {
            Name = name,
            VariableDeclarations = varDecls,
            Statements = statements,
            FilePath = _filePath,
            StartLine = context.Start.Line,
            EndLine = context.Stop?.Line ?? context.Start.Line,
            StartColumn = context.Start.Column,
            EndColumn = context.Stop?.Column ?? context.Start.Column
        };
    }

    /// <summary>
    /// FUNCTION 선언 방문
    /// </summary>
    public override ASTNode VisitFunctionDeclaration(StructuredTextParser.FunctionDeclarationContext context)
    {
        var name = context.IDENTIFIER().GetText();
        var returnType = context.dataType().GetText();

        var varDecls = context.varDeclaration()
            .Select(vd => (VariableDeclarationListNode)Visit(vd))
            .ToList();

        var statements = context.statement()
            .Select(s => (StatementNode)Visit(s))
            .Where(s => s != null)
            .ToList();

        return new FunctionNode
        {
            Name = name,
            ReturnType = returnType,
            VariableDeclarations = varDecls,
            Statements = statements,
            FilePath = _filePath,
            StartLine = context.Start.Line,
            EndLine = context.Stop?.Line ?? context.Start.Line,
            StartColumn = context.Start.Column,
            EndColumn = context.Stop?.Column ?? context.Start.Column
        };
    }

    /// <summary>
    /// PROGRAM 선언 방문
    /// </summary>
    public override ASTNode VisitProgramDeclaration(StructuredTextParser.ProgramDeclarationContext context)
    {
        var name = context.IDENTIFIER().GetText();

        var varDecls = context.varDeclaration()
            .Select(vd => (VariableDeclarationListNode)Visit(vd))
            .ToList();

        var statements = context.statement()
            .Select(s => (StatementNode)Visit(s))
            .Where(s => s != null)
            .ToList();

        return new ProgramNode
        {
            Name = name,
            VariableDeclarations = varDecls,
            Statements = statements,
            FilePath = _filePath,
            StartLine = context.Start.Line,
            EndLine = context.Stop?.Line ?? context.Start.Line,
            StartColumn = context.Start.Column,
            EndColumn = context.Stop?.Column ?? context.Start.Column
        };
    }

    // ========================================================================
    // 변수 선언
    // ========================================================================

    /// <summary>
    /// 변수 선언 블록 방문 (VAR, VAR_INPUT 등)
    /// </summary>
    public override ASTNode VisitVarDeclaration(StructuredTextParser.VarDeclarationContext context)
    {
        var scope = GetVariableScope(context);
        var isConstant = context.CONSTANT() != null;

        var variables = context.varDeclList()?.varDecl()
            .Select(vd => (VariableDeclarationNode)VisitVarDecl(vd))
            .ToList() ?? new List<VariableDeclarationNode>();

        return new VariableDeclarationListNode
        {
            Scope = scope,
            IsConstant = isConstant,
            Variables = variables,
            FilePath = _filePath,
            StartLine = context.Start.Line,
            EndLine = context.Stop?.Line ?? context.Start.Line,
            StartColumn = context.Start.Column,
            EndColumn = context.Stop?.Column ?? context.Start.Column
        };
    }

    /// <summary>
    /// 개별 변수 선언 방문
    /// </summary>
    public override ASTNode VisitVarDecl(StructuredTextParser.VarDeclContext context)
    {
        // 변수명 (쉼표로 여러 개 선언 가능)
        var identifiers = context.IDENTIFIER().Select(id => id.GetText()).ToList();
        var dataType = context.dataType().GetText();

        ExpressionNode? initialValue = null;
        if (context.expression() != null)
        {
            initialValue = (ExpressionNode)Visit(context.expression());
        }

        // 첫 번째 변수만 반환 (여러 개인 경우 별도 처리 필요)
        var name = identifiers.First();

        return new VariableDeclarationNode
        {
            Name = name,
            DataType = dataType,
            InitialValue = initialValue,
            IsArray = context.dataType().ARRAY() != null,
            IsPointer = context.dataType().POINTER() != null,
            IsReference = context.dataType().REFERENCE() != null,
            FilePath = _filePath,
            StartLine = context.Start.Line,
            EndLine = context.Stop?.Line ?? context.Start.Line,
            StartColumn = context.Start.Column,
            EndColumn = context.Stop?.Column ?? context.Start.Column
        };
    }

    // ========================================================================
    // 구문 (Statements)
    // ========================================================================

    /// <summary>
    /// 할당문 방문
    /// </summary>
    public override ASTNode VisitAssignmentStatement(StructuredTextParser.AssignmentStatementContext context)
    {
        var left = (VariableReferenceNode)Visit(context.variable());
        var right = (ExpressionNode)Visit(context.expression());

        return new AssignmentStatementNode
        {
            Left = left,
            Right = right,
            FilePath = _filePath,
            StartLine = context.Start.Line,
            EndLine = context.Stop?.Line ?? context.Start.Line,
            StartColumn = context.Start.Column,
            EndColumn = context.Stop?.Column ?? context.Start.Column
        };
    }

    /// <summary>
    /// IF 문 방문
    /// </summary>
    public override ASTNode VisitIfStatement(StructuredTextParser.IfStatementContext context)
    {
        var condition = (ExpressionNode)Visit(context.expression(0));
        var thenBlock = context.statement()
            .TakeWhile((s, i) => i < GetThenBlockCount(context))
            .Select(s => (StatementNode)Visit(s))
            .Where(s => s != null)
            .ToList();

        // ELSIF 절 처리
        var elsifClauses = new List<ElsifClause>();
        var elsifConditions = context.expression().Skip(1).ToList();
        // ELSIF 블록 파싱은 복잡하므로 기본 구현

        // ELSE 블록
        List<StatementNode>? elseBlock = null;
        if (context.ELSE() != null)
        {
            elseBlock = context.statement()
                .Skip(thenBlock.Count)
                .Select(s => (StatementNode)Visit(s))
                .Where(s => s != null)
                .ToList();
        }

        return new IfStatementNode
        {
            Condition = condition,
            ThenBlock = thenBlock,
            ElsifClauses = elsifClauses,
            ElseBlock = elseBlock,
            FilePath = _filePath,
            StartLine = context.Start.Line,
            EndLine = context.Stop?.Line ?? context.Start.Line,
            StartColumn = context.Start.Column,
            EndColumn = context.Stop?.Column ?? context.Start.Column
        };
    }

    /// <summary>
    /// CASE 문 방문
    /// </summary>
    public override ASTNode VisitCaseStatement(StructuredTextParser.CaseStatementContext context)
    {
        var expression = (ExpressionNode)Visit(context.expression());

        var caseElements = context.caseElement()
            .Select(ce => new CaseElement
            {
                Values = ce.constantExpression()
                    .Select(expr => (ExpressionNode)Visit(expr))
                    .ToList(),
                Statements = ce.statement()
                    .Select(s => (StatementNode)Visit(s))
                    .Where(s => s != null)
                    .ToList(),
                StartLine = ce.Start.Line,
                EndLine = ce.Stop?.Line ?? ce.Start.Line
            })
            .ToList();

        List<StatementNode>? elseBlock = null;
        if (context.ELSE() != null)
        {
            // ELSE 블록의 statement 추출
            elseBlock = new List<StatementNode>();
        }

        return new CaseStatementNode
        {
            Expression = expression,
            CaseElements = caseElements,
            ElseBlock = elseBlock,
            FilePath = _filePath,
            StartLine = context.Start.Line,
            EndLine = context.Stop?.Line ?? context.Start.Line,
            StartColumn = context.Start.Column,
            EndColumn = context.Stop?.Column ?? context.Start.Column
        };
    }

    /// <summary>
    /// FOR 루프 방문
    /// </summary>
    public override ASTNode VisitForStatement(StructuredTextParser.ForStatementContext context)
    {
        var loopVar = context.IDENTIFIER().GetText();
        var startValue = (ExpressionNode)Visit(context.expression(0));
        var endValue = (ExpressionNode)Visit(context.expression(1));

        ExpressionNode? stepValue = null;
        if (context.expression().Length > 2)
        {
            stepValue = (ExpressionNode)Visit(context.expression(2));
        }

        var body = context.statement()
            .Select(s => (StatementNode)Visit(s))
            .Where(s => s != null)
            .ToList();

        return new ForStatementNode
        {
            LoopVariable = loopVar,
            StartValue = startValue,
            EndValue = endValue,
            StepValue = stepValue,
            Body = body,
            FilePath = _filePath,
            StartLine = context.Start.Line,
            EndLine = context.Stop?.Line ?? context.Start.Line,
            StartColumn = context.Start.Column,
            EndColumn = context.Stop?.Column ?? context.Start.Column
        };
    }

    /// <summary>
    /// WHILE 루프 방문
    /// </summary>
    public override ASTNode VisitWhileStatement(StructuredTextParser.WhileStatementContext context)
    {
        var condition = (ExpressionNode)Visit(context.expression());
        var body = context.statement()
            .Select(s => (StatementNode)Visit(s))
            .Where(s => s != null)
            .ToList();

        return new WhileStatementNode
        {
            Condition = condition,
            Body = body,
            FilePath = _filePath,
            StartLine = context.Start.Line,
            EndLine = context.Stop?.Line ?? context.Start.Line,
            StartColumn = context.Start.Column,
            EndColumn = context.Stop?.Column ?? context.Start.Column
        };
    }

    /// <summary>
    /// REPEAT 루프 방문
    /// </summary>
    public override ASTNode VisitRepeatStatement(StructuredTextParser.RepeatStatementContext context)
    {
        var body = context.statement()
            .Select(s => (StatementNode)Visit(s))
            .Where(s => s != null)
            .ToList();
        var untilCondition = (ExpressionNode)Visit(context.expression());

        return new RepeatStatementNode
        {
            Body = body,
            UntilCondition = untilCondition,
            FilePath = _filePath,
            StartLine = context.Start.Line,
            EndLine = context.Stop?.Line ?? context.Start.Line,
            StartColumn = context.Start.Column,
            EndColumn = context.Stop?.Column ?? context.Start.Column
        };
    }

    /// <summary>
    /// EXIT 문 방문
    /// </summary>
    public override ASTNode VisitExitStatement(StructuredTextParser.ExitStatementContext context)
    {
        return new ExitStatementNode
        {
            FilePath = _filePath,
            StartLine = context.Start.Line,
            EndLine = context.Stop?.Line ?? context.Start.Line,
            StartColumn = context.Start.Column,
            EndColumn = context.Stop?.Column ?? context.Start.Column
        };
    }

    /// <summary>
    /// RETURN 문 방문
    /// </summary>
    public override ASTNode VisitReturnStatement(StructuredTextParser.ReturnStatementContext context)
    {
        return new ReturnStatementNode
        {
            FilePath = _filePath,
            StartLine = context.Start.Line,
            EndLine = context.Stop?.Line ?? context.Start.Line,
            StartColumn = context.Start.Column,
            EndColumn = context.Stop?.Column ?? context.Start.Column
        };
    }

    // ========================================================================
    // 표현식 (Expressions)
    // ========================================================================

    /// <summary>
    /// 표현식 방문 (재귀적)
    /// </summary>
    public override ASTNode VisitExpression(StructuredTextParser.ExpressionContext context)
    {
        // 리터럴
        if (context.literal() != null)
        {
            return VisitLiteral(context.literal());
        }

        // 변수
        if (context.variable() != null)
        {
            return Visit(context.variable());
        }

        // 함수 호출
        if (context.functionCall() != null)
        {
            return VisitFunctionCall(context.functionCall());
        }

        // 괄호
        if (context.GetChild(0).GetText() == "(")
        {
            return Visit(context.expression(0));
        }

        // 단항 연산자 (NOT)
        if (context.NOT() != null)
        {
            var operand = (ExpressionNode)Visit(context.expression(0));
            return new UnaryExpressionNode
            {
                Operand = operand,
                Operator = UnaryOperator.Not,
                FilePath = _filePath,
                StartLine = context.Start.Line,
                EndLine = context.Stop?.Line ?? context.Start.Line,
                StartColumn = context.Start.Column,
                EndColumn = context.Stop?.Column ?? context.Start.Column
            };
        }

        // 이항 연산자
        if (context.expression().Length == 2)
        {
            var left = (ExpressionNode)Visit(context.expression(0));
            var right = (ExpressionNode)Visit(context.expression(1));
            var op = GetBinaryOperator(context.op);

            return new BinaryExpressionNode
            {
                Left = left,
                Right = right,
                Operator = op,
                FilePath = _filePath,
                StartLine = context.Start.Line,
                EndLine = context.Stop?.Line ?? context.Start.Line,
                StartColumn = context.Start.Column,
                EndColumn = context.Stop?.Column ?? context.Start.Column
            };
        }

        return CreateEmptyExpression(context);
    }

    /// <summary>
    /// 리터럴 방문
    /// </summary>
    public override ASTNode VisitLiteral(StructuredTextParser.LiteralContext context)
    {
        LiteralType type;
        string value;

        if (context.INTEGER_LITERAL() != null)
        {
            type = LiteralType.Integer;
            value = context.INTEGER_LITERAL().GetText();
        }
        else if (context.REAL_LITERAL() != null)
        {
            type = LiteralType.Real;
            value = context.REAL_LITERAL().GetText();
        }
        else if (context.STRING_LITERAL() != null)
        {
            type = LiteralType.String;
            value = context.STRING_LITERAL().GetText();
        }
        else if (context.WSTRING_LITERAL() != null)
        {
            type = LiteralType.WString;
            value = context.WSTRING_LITERAL().GetText();
        }
        else if (context.BOOLEAN_LITERAL() != null)
        {
            type = LiteralType.Boolean;
            value = context.BOOLEAN_LITERAL().GetText();
        }
        else if (context.TIME_LITERAL() != null)
        {
            type = LiteralType.Time;
            value = context.TIME_LITERAL().GetText();
        }
        else
        {
            type = LiteralType.Integer;
            value = "0";
        }

        return new LiteralNode
        {
            Type = type,
            Value = value,
            FilePath = _filePath,
            StartLine = context.Start.Line,
            EndLine = context.Stop?.Line ?? context.Start.Line,
            StartColumn = context.Start.Column,
            EndColumn = context.Stop?.Column ?? context.Start.Column
        };
    }

    /// <summary>
    /// 변수 참조 방문
    /// </summary>
    public override ASTNode VisitVariable(StructuredTextParser.VariableContext context)
    {
        var identifiers = context.IDENTIFIER().Select(id => id.GetText()).ToList();
        var name = identifiers.First();
        var fieldAccess = identifiers.Skip(1).ToList();

        List<ExpressionNode>? arrayIndices = null;
        if (context.expression() != null && context.expression().Length > 0)
        {
            arrayIndices = context.expression()
                .Select(e => (ExpressionNode)Visit(e))
                .ToList();
        }

        return new VariableReferenceNode
        {
            Name = name,
            FieldAccess = fieldAccess.Any() ? fieldAccess : null,
            ArrayIndices = arrayIndices,
            FilePath = _filePath,
            StartLine = context.Start.Line,
            EndLine = context.Stop?.Line ?? context.Start.Line,
            StartColumn = context.Start.Column,
            EndColumn = context.Stop?.Column ?? context.Start.Column
        };
    }

    /// <summary>
    /// 함수 호출 방문
    /// </summary>
    public override ASTNode VisitFunctionCall(StructuredTextParser.FunctionCallContext context)
    {
        var functionName = context.IDENTIFIER().GetText();

        var arguments = context.argumentList()?.argument()
            .Select(arg =>
            {
                string? paramName = null;
                if (arg.IDENTIFIER() != null)
                {
                    paramName = arg.IDENTIFIER().GetText();
                }

                var value = (ExpressionNode)Visit(arg.expression());

                return new FunctionArgument
                {
                    Name = paramName,
                    Value = value
                };
            })
            .ToList() ?? new List<FunctionArgument>();

        return new FunctionCallNode
        {
            FunctionName = functionName,
            Arguments = arguments,
            FilePath = _filePath,
            StartLine = context.Start.Line,
            EndLine = context.Stop?.Line ?? context.Start.Line,
            StartColumn = context.Start.Column,
            EndColumn = context.Stop?.Column ?? context.Start.Column
        };
    }

    // ========================================================================
    // 데이터 타입 선언
    // ========================================================================

    /// <summary>
    /// 데이터 타입 선언 방문
    /// </summary>
    public override ASTNode VisitDataTypeDeclaration(StructuredTextParser.DataTypeDeclarationContext context)
    {
        var name = context.IDENTIFIER().GetText();
        var structType = context.structType();

        DataTypeKind kind;
        ASTNode typeDefinition;

        if (structType.STRUCT() != null)
        {
            kind = DataTypeKind.Struct;
            typeDefinition = VisitStructType(structType);
        }
        else if (structType.GetChild(0).GetText() == "(")
        {
            kind = DataTypeKind.Enum;
            typeDefinition = VisitEnumType(structType);
        }
        else if (structType.UNION() != null)
        {
            kind = DataTypeKind.Union;
            typeDefinition = VisitStructType(structType); // UNION도 STRUCT와 유사
        }
        else
        {
            kind = DataTypeKind.Alias;
            typeDefinition = CreateEmptyNode(structType);
        }

        return new DataTypeDeclarationNode
        {
            Name = name,
            Kind = kind,
            TypeDefinition = typeDefinition,
            FilePath = _filePath,
            StartLine = context.Start.Line,
            EndLine = context.Stop?.Line ?? context.Start.Line,
            StartColumn = context.Start.Column,
            EndColumn = context.Stop?.Column ?? context.Start.Column
        };
    }

    /// <summary>
    /// STRUCT 타입 방문
    /// </summary>
    public override ASTNode VisitStructType(StructuredTextParser.StructTypeContext context)
    {
        var fields = context.varDeclList()?.varDecl()
            .Select(vd => (VariableDeclarationNode)VisitVarDecl(vd))
            .ToList() ?? new List<VariableDeclarationNode>();

        return new StructTypeNode
        {
            Fields = fields,
            FilePath = _filePath,
            StartLine = context.Start.Line,
            EndLine = context.Stop?.Line ?? context.Start.Line,
            StartColumn = context.Start.Column,
            EndColumn = context.Stop?.Column ?? context.Start.Column
        };
    }

    /// <summary>
    /// ENUM 타입 방문
    /// </summary>
    public ASTNode VisitEnumType(StructuredTextParser.StructTypeContext context)
    {
        var values = context.enumValue()
            .Select(ev =>
            {
                var name = ev.IDENTIFIER().GetText();
                int? explicitValue = null;

                if (ev.INTEGER_LITERAL() != null)
                {
                    explicitValue = int.Parse(ev.INTEGER_LITERAL().GetText());
                }

                return new TwinCatQA.Domain.Models.AST.EnumValue
                {
                    Name = name,
                    ExplicitValue = explicitValue,
                    StartLine = ev.Start.Line
                };
            })
            .ToList();

        return new EnumTypeNode
        {
            Values = values,
            FilePath = _filePath,
            StartLine = context.Start.Line,
            EndLine = context.Stop?.Line ?? context.Start.Line,
            StartColumn = context.Start.Column,
            EndColumn = context.Stop?.Column ?? context.Start.Column
        };
    }

    // ========================================================================
    // 헬퍼 메서드
    // ========================================================================

    /// <summary>
    /// 변수 스코프 추출
    /// </summary>
    private VariableScope GetVariableScope(StructuredTextParser.VarDeclarationContext context)
    {
        if (context.VAR_INPUT() != null) return VariableScope.Input;
        if (context.VAR_OUTPUT() != null) return VariableScope.Output;
        if (context.VAR_IN_OUT() != null) return VariableScope.InOut;
        if (context.VAR_TEMP() != null) return VariableScope.Local;
        if (context.VAR_GLOBAL() != null) return VariableScope.Global;
        if (context.CONSTANT() != null) return VariableScope.Constant;

        return VariableScope.Local;
    }

    /// <summary>
    /// 이항 연산자 추출
    /// </summary>
    private BinaryOperator GetBinaryOperator(IToken token)
    {
        if (token == null) return BinaryOperator.Add;

        return token.Text switch
        {
            "+" => BinaryOperator.Add,
            "-" => BinaryOperator.Subtract,
            "*" => BinaryOperator.Multiply,
            "/" => BinaryOperator.Divide,
            "MOD" => BinaryOperator.Modulo,
            "=" => BinaryOperator.Equal,
            "<>" => BinaryOperator.NotEqual,
            "<" => BinaryOperator.LessThan,
            "<=" => BinaryOperator.LessThanOrEqual,
            ">" => BinaryOperator.GreaterThan,
            ">=" => BinaryOperator.GreaterThanOrEqual,
            "AND" or "&" => BinaryOperator.And,
            "OR" => BinaryOperator.Or,
            "XOR" => BinaryOperator.Xor,
            _ => BinaryOperator.Add
        };
    }

    /// <summary>
    /// THEN 블록의 statement 개수 계산 (ELSIF, ELSE 전까지)
    /// </summary>
    private int GetThenBlockCount(StructuredTextParser.IfStatementContext context)
    {
        // 간단한 구현: ELSIF나 ELSE가 없으면 모든 statement
        if (context.ELSIF().Length == 0 && context.ELSE() == null)
        {
            return context.statement().Length;
        }

        // 정확한 파싱은 Parse Tree 구조를 더 자세히 분석해야 함
        // 여기서는 간단히 첫 번째 블록만 반환
        return 1;
    }

    /// <summary>
    /// 빈 노드 생성 (오류 방지)
    /// </summary>
    private ASTNode CreateEmptyNode(ParserRuleContext context)
    {
        return new ProgramNode
        {
            Name = "Empty",
            FilePath = _filePath,
            StartLine = context.Start.Line,
            EndLine = context.Stop?.Line ?? context.Start.Line,
            StartColumn = context.Start.Column,
            EndColumn = context.Stop?.Column ?? context.Start.Column
        };
    }

    /// <summary>
    /// 빈 표현식 생성 (오류 방지)
    /// </summary>
    private ExpressionNode CreateEmptyExpression(ParserRuleContext context)
    {
        return new LiteralNode
        {
            Type = LiteralType.Integer,
            Value = "0",
            FilePath = _filePath,
            StartLine = context.Start.Line,
            EndLine = context.Stop?.Line ?? context.Start.Line,
            StartColumn = context.Start.Column,
            EndColumn = context.Stop?.Column ?? context.Start.Column
        };
    }
}
