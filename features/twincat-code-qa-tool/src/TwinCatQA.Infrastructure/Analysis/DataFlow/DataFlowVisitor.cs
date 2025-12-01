using TwinCatQA.Domain.Models.AST;
using TwinCatQA.Domain.Models;

namespace TwinCatQA.Infrastructure.Analysis.DataFlow;

/// <summary>
/// 데이터 흐름 분석용 AST Visitor
/// 변수의 정의와 사용을 추적합니다.
/// </summary>
public class DataFlowVisitor : IASTVisitor
{
    private readonly Dictionary<string, DefUseChain> _variableChains = new();
    private readonly Dictionary<string, VariableDeclarationNode> _declarations = new();
    private readonly Stack<string> _scopeStack = new();
    private string _currentFilePath = string.Empty;
    private int _currentBlockId = 0;
    private bool _isInLoop = false;
    private bool _isConditional = false;

    /// <summary>
    /// 수집된 변수 체인
    /// </summary>
    public Dictionary<string, DefUseChain> VariableChains => _variableChains;

    /// <summary>
    /// 변수 선언 정보
    /// </summary>
    public Dictionary<string, VariableDeclarationNode> Declarations => _declarations;

    /// <summary>
    /// 현재 기본 블록 ID 설정
    /// </summary>
    public void SetCurrentBlock(int blockId)
    {
        _currentBlockId = blockId;
    }

    /// <summary>
    /// 루프 컨텍스트 설정
    /// </summary>
    public void SetLoopContext(bool inLoop)
    {
        _isInLoop = inLoop;
    }

    /// <summary>
    /// 조건부 컨텍스트 설정
    /// </summary>
    public void SetConditionalContext(bool isConditional)
    {
        _isConditional = isConditional;
    }

    #region Program Structure Visits

    public void Visit(ProgramNode node)
    {
        _currentFilePath = node.FilePath;
        _scopeStack.Push($"PROGRAM:{node.Name}");

        // 변수 선언 처리
        foreach (var varList in node.VariableDeclarations)
        {
            varList.Accept(this);
        }

        // 구문 처리
        foreach (var statement in node.Statements)
        {
            statement.Accept(this);
        }

        _scopeStack.Pop();
    }

    public void Visit(FunctionBlockNode node)
    {
        _currentFilePath = node.FilePath;
        _scopeStack.Push($"FB:{node.Name}");

        foreach (var varList in node.VariableDeclarations)
        {
            varList.Accept(this);
        }

        foreach (var statement in node.Statements)
        {
            statement.Accept(this);
        }

        _scopeStack.Pop();
    }

    public void Visit(FunctionNode node)
    {
        _currentFilePath = node.FilePath;
        _scopeStack.Push($"FUNCTION:{node.Name}");

        foreach (var varList in node.VariableDeclarations)
        {
            varList.Accept(this);
        }

        foreach (var statement in node.Statements)
        {
            statement.Accept(this);
        }

        _scopeStack.Pop();
    }

    #endregion

    #region Variable Declaration Visits

    public void Visit(VariableDeclarationListNode node)
    {
        foreach (var variable in node.Variables)
        {
            variable.Accept(this);

            // 변수 체인 생성
            var chain = GetOrCreateChain(variable.Name);
            chain.Declaration = new DeclarationSite
            {
                FilePath = _currentFilePath,
                Line = variable.StartLine,
                Column = variable.StartColumn,
                Scope = node.Scope.ToString(),
                DataType = variable.DataType,
                HasInitialValue = variable.InitialValue != null
            };

            // 초기값이 있으면 초기화됨으로 표시
            if (variable.InitialValue != null)
            {
                chain.IsInitialized = true;

                // 초기값도 정의로 추가
                chain.Definitions.Add(new DefinitionSite
                {
                    FilePath = _currentFilePath,
                    Line = variable.StartLine,
                    Column = variable.StartColumn,
                    AssignmentExpression = "초기값",
                    BasicBlockId = _currentBlockId,
                    IsConditional = false,
                    IsInLoop = false
                });

                // 초기값 표현식 내의 변수 사용 추적
                if (variable.InitialValue != null)
                {
                    variable.InitialValue.Accept(this);
                }
            }

            // VAR_INPUT, VAR_OUTPUT은 외부에서 값이 전달되므로 초기화된 것으로 간주
            if (node.Scope == VariableScope.Input || node.Scope == VariableScope.Output)
            {
                chain.IsInitialized = true;
            }

            _declarations[variable.Name] = variable;
        }
    }

    public void Visit(VariableDeclarationNode node)
    {
        // VariableDeclarationListNode에서 처리됨
    }

    #endregion

    #region Statement Visits

    public void Visit(AssignmentStatementNode node)
    {
        // 좌변: 변수 정의
        var varName = node.Left.Name;
        var chain = GetOrCreateChain(varName);

        chain.Definitions.Add(new DefinitionSite
        {
            FilePath = _currentFilePath,
            Line = node.StartLine,
            Column = node.StartColumn,
            AssignmentExpression = "할당문",
            BasicBlockId = _currentBlockId,
            IsConditional = _isConditional,
            IsInLoop = _isInLoop
        });

        // 할당 후에는 초기화됨
        chain.IsInitialized = true;

        // 우변: 변수 사용
        node.Right.Accept(this);
    }

    public void Visit(IfStatementNode node)
    {
        // 조건식의 변수 사용
        node.Condition.Accept(this);

        // THEN 블록
        var wasConditional = _isConditional;
        _isConditional = true;

        foreach (var statement in node.ThenBlock)
        {
            statement.Accept(this);
        }

        // ELSIF 블록들
        foreach (var elsif in node.ElsifClauses)
        {
            elsif.Condition.Accept(this);
            foreach (var statement in elsif.Statements)
            {
                statement.Accept(this);
            }
        }

        // ELSE 블록
        if (node.ElseBlock != null)
        {
            foreach (var statement in node.ElseBlock)
            {
                statement.Accept(this);
            }
        }

        _isConditional = wasConditional;
    }

    public void Visit(CaseStatementNode node)
    {
        // CASE 표현식의 변수 사용
        node.Expression.Accept(this);

        var wasConditional = _isConditional;
        _isConditional = true;

        // CASE 분기들
        foreach (var element in node.CaseElements)
        {
            foreach (var value in element.Values)
            {
                value.Accept(this);
            }

            foreach (var statement in element.Statements)
            {
                statement.Accept(this);
            }
        }

        // ELSE 블록
        if (node.ElseBlock != null)
        {
            foreach (var statement in node.ElseBlock)
            {
                statement.Accept(this);
            }
        }

        _isConditional = wasConditional;
    }

    public void Visit(ForStatementNode node)
    {
        // FOR 루프 변수 정의
        var loopVarChain = GetOrCreateChain(node.LoopVariable);
        loopVarChain.IsInitialized = true;
        loopVarChain.Definitions.Add(new DefinitionSite
        {
            FilePath = _currentFilePath,
            Line = node.StartLine,
            Column = node.StartColumn,
            AssignmentExpression = "FOR 루프 변수",
            BasicBlockId = _currentBlockId,
            IsConditional = false,
            IsInLoop = true
        });

        // 시작값, 종료값, 증가값의 변수 사용
        node.StartValue.Accept(this);
        node.EndValue.Accept(this);
        node.StepValue?.Accept(this);

        // 루프 본문
        var wasInLoop = _isInLoop;
        _isInLoop = true;

        foreach (var statement in node.Body)
        {
            statement.Accept(this);
        }

        _isInLoop = wasInLoop;
    }

    public void Visit(WhileStatementNode node)
    {
        // 조건식의 변수 사용
        node.Condition.Accept(this);

        // 루프 본문
        var wasInLoop = _isInLoop;
        _isInLoop = true;

        foreach (var statement in node.Body)
        {
            statement.Accept(this);
        }

        _isInLoop = wasInLoop;
    }

    public void Visit(RepeatStatementNode node)
    {
        var wasInLoop = _isInLoop;
        _isInLoop = true;

        // 루프 본문
        foreach (var statement in node.Body)
        {
            statement.Accept(this);
        }

        // UNTIL 조건식의 변수 사용
        node.UntilCondition.Accept(this);

        _isInLoop = wasInLoop;
    }

    public void Visit(ExitStatementNode node)
    {
        // EXIT 문은 변수에 영향 없음
    }

    public void Visit(ReturnStatementNode node)
    {
        // RETURN 문은 변수에 영향 없음
    }

    #endregion

    #region Expression Visits

    public void Visit(BinaryExpressionNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
    }

    public void Visit(UnaryExpressionNode node)
    {
        node.Operand.Accept(this);
    }

    public void Visit(LiteralNode node)
    {
        // 리터럴은 변수 사용 없음
    }

    public void Visit(VariableReferenceNode node)
    {
        // 변수 사용 기록
        var chain = GetOrCreateChain(node.Name);

        chain.Usages.Add(new UsageSite
        {
            FilePath = _currentFilePath,
            Line = node.StartLine,
            Column = node.StartColumn,
            Context = GetCurrentContext(),
            BasicBlockId = _currentBlockId,
            IsRead = true
        });

        // 배열 인덱스의 변수 사용
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
        // 함수 인자의 변수 사용
        foreach (var arg in node.Arguments)
        {
            arg.Value.Accept(this);
        }
    }

    #endregion

    #region Data Type Visits

    public void Visit(DataTypeDeclarationNode node)
    {
        // 데이터 타입 선언은 변수 흐름에 영향 없음
    }

    public void Visit(StructTypeNode node)
    {
        // 구조체 타입은 변수 흐름에 영향 없음
    }

    public void Visit(EnumTypeNode node)
    {
        // 열거형 타입은 변수 흐름에 영향 없음
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// 변수 체인을 가져오거나 생성합니다.
    /// </summary>
    private DefUseChain GetOrCreateChain(string variableName)
    {
        if (!_variableChains.TryGetValue(variableName, out var chain))
        {
            chain = new DefUseChain
            {
                VariableName = variableName,
                IsInitialized = false
            };
            _variableChains[variableName] = chain;
        }

        return chain;
    }

    /// <summary>
    /// 현재 컨텍스트 정보를 문자열로 반환합니다.
    /// </summary>
    private string GetCurrentContext()
    {
        var context = _scopeStack.Count > 0 ? _scopeStack.Peek() : "Unknown";

        if (_isInLoop)
        {
            context += " (Loop)";
        }

        if (_isConditional)
        {
            context += " (Conditional)";
        }

        return context;
    }

    #endregion
}
