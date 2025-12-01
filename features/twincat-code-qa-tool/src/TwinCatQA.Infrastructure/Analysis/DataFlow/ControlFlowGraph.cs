using TwinCatQA.Domain.Models.AST;

namespace TwinCatQA.Infrastructure.Analysis.DataFlow;

/// <summary>
/// 제어 흐름 그래프 (Control Flow Graph)
/// 프로그램의 실행 흐름을 기본 블록과 간선으로 표현합니다.
/// </summary>
public class ControlFlowGraph
{
    /// <summary>
    /// 기본 블록 목록
    /// </summary>
    public List<BasicBlock> Blocks { get; init; } = new();

    /// <summary>
    /// 진입 블록 (Entry Block)
    /// </summary>
    public BasicBlock? EntryBlock { get; set; }

    /// <summary>
    /// 종료 블록 (Exit Block)
    /// </summary>
    public BasicBlock? ExitBlock { get; set; }

    /// <summary>
    /// 특정 블록 조회
    /// </summary>
    public BasicBlock? GetBlock(int id)
        => Blocks.FirstOrDefault(b => b.Id == id);

    /// <summary>
    /// 도달 가능한 블록들을 찾습니다 (깊이 우선 탐색)
    /// </summary>
    public HashSet<BasicBlock> GetReachableBlocks()
    {
        if (EntryBlock == null)
            return new HashSet<BasicBlock>();

        var reachable = new HashSet<BasicBlock>();
        var stack = new Stack<BasicBlock>();
        stack.Push(EntryBlock);

        while (stack.Count > 0)
        {
            var block = stack.Pop();
            if (reachable.Add(block))
            {
                foreach (var successor in block.Successors)
                {
                    stack.Push(successor);
                }
            }
        }

        return reachable;
    }

    /// <summary>
    /// 도달 불가능한 블록들을 찾습니다
    /// </summary>
    public List<BasicBlock> GetUnreachableBlocks()
    {
        var reachable = GetReachableBlocks();
        return Blocks.Where(b => !reachable.Contains(b)).ToList();
    }

    /// <summary>
    /// 역방향 포스트오더 순회 (Reverse Post-Order)
    /// 데이터 흐름 분석에 유용한 순서입니다.
    /// </summary>
    public List<BasicBlock> GetReversePostOrder()
    {
        if (EntryBlock == null)
            return new List<BasicBlock>();

        var visited = new HashSet<BasicBlock>();
        var postOrder = new List<BasicBlock>();

        void PostOrderTraversal(BasicBlock block)
        {
            if (!visited.Add(block))
                return;

            foreach (var successor in block.Successors)
            {
                PostOrderTraversal(successor);
            }

            postOrder.Add(block);
        }

        PostOrderTraversal(EntryBlock);
        postOrder.Reverse();
        return postOrder;
    }

    /// <summary>
    /// 루프 감지 (간단한 백 엣지 탐지)
    /// </summary>
    public List<LoopInfo> DetectLoops()
    {
        var loops = new List<LoopInfo>();
        var visited = new HashSet<BasicBlock>();
        var recursionStack = new HashSet<BasicBlock>();

        void FindBackEdges(BasicBlock block, List<BasicBlock> path)
        {
            visited.Add(block);
            recursionStack.Add(block);
            path.Add(block);

            foreach (var successor in block.Successors)
            {
                if (!visited.Contains(successor))
                {
                    FindBackEdges(successor, new List<BasicBlock>(path));
                }
                else if (recursionStack.Contains(successor))
                {
                    // 백 엣지 발견 (루프)
                    var loopHeaderIndex = path.IndexOf(successor);
                    var loopBlocks = path.Skip(loopHeaderIndex).ToList();

                    loops.Add(new LoopInfo
                    {
                        Header = successor,
                        Blocks = loopBlocks
                    });
                }
            }

            recursionStack.Remove(block);
        }

        if (EntryBlock != null)
        {
            FindBackEdges(EntryBlock, new List<BasicBlock>());
        }

        return loops;
    }
}

/// <summary>
/// 기본 블록 (Basic Block)
/// 순차적으로 실행되는 구문들의 집합으로, 중간에 분기가 없습니다.
/// </summary>
public class BasicBlock
{
    /// <summary>
    /// 블록 ID (고유 식별자)
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// 블록에 포함된 구문 목록
    /// </summary>
    public List<StatementNode> Statements { get; init; } = new();

    /// <summary>
    /// 선행자 블록 목록 (이 블록으로 진입할 수 있는 블록들)
    /// </summary>
    public List<BasicBlock> Predecessors { get; init; } = new();

    /// <summary>
    /// 후속자 블록 목록 (이 블록에서 진출할 수 있는 블록들)
    /// </summary>
    public List<BasicBlock> Successors { get; init; } = new();

    /// <summary>
    /// 블록 라벨 (디버깅 및 시각화용)
    /// </summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>
    /// 블록 타입
    /// </summary>
    public BasicBlockType Type { get; init; } = BasicBlockType.Normal;

    /// <summary>
    /// 시작 라인 번호
    /// </summary>
    public int StartLine => Statements.Count > 0 ? Statements.First().StartLine : 0;

    /// <summary>
    /// 종료 라인 번호
    /// </summary>
    public int EndLine => Statements.Count > 0 ? Statements.Last().EndLine : 0;

    /// <summary>
    /// 블록에 정의된 변수들 (Def Set)
    /// </summary>
    public HashSet<string> DefinedVariables { get; init; } = new();

    /// <summary>
    /// 블록에서 사용된 변수들 (Use Set)
    /// </summary>
    public HashSet<string> UsedVariables { get; init; } = new();

    /// <summary>
    /// 진입 시점의 활성 변수 집합 (Live-in Set)
    /// 데이터 흐름 분석에서 계산됩니다.
    /// </summary>
    public HashSet<string> LiveIn { get; set; } = new();

    /// <summary>
    /// 종료 시점의 활성 변수 집합 (Live-out Set)
    /// 데이터 흐름 분석에서 계산됩니다.
    /// </summary>
    public HashSet<string> LiveOut { get; set; } = new();

    /// <summary>
    /// 선행자 블록 추가
    /// </summary>
    public void AddPredecessor(BasicBlock predecessor)
    {
        if (!Predecessors.Contains(predecessor))
        {
            Predecessors.Add(predecessor);
        }
    }

    /// <summary>
    /// 후속자 블록 추가
    /// </summary>
    public void AddSuccessor(BasicBlock successor)
    {
        if (!Successors.Contains(successor))
        {
            Successors.Add(successor);
        }
    }

    public override string ToString()
        => $"Block {Id} ({Type}): Lines {StartLine}-{EndLine}, {Statements.Count} stmts";
}

/// <summary>
/// 기본 블록 타입
/// </summary>
public enum BasicBlockType
{
    /// <summary>
    /// 일반 블록
    /// </summary>
    Normal,

    /// <summary>
    /// 진입 블록
    /// </summary>
    Entry,

    /// <summary>
    /// 종료 블록
    /// </summary>
    Exit,

    /// <summary>
    /// 조건 분기 블록 (IF, CASE)
    /// </summary>
    Conditional,

    /// <summary>
    /// 루프 헤더 블록 (FOR, WHILE, REPEAT)
    /// </summary>
    LoopHeader,

    /// <summary>
    /// 루프 본문 블록
    /// </summary>
    LoopBody,

    /// <summary>
    /// 루프 종료 블록
    /// </summary>
    LoopExit
}

/// <summary>
/// 루프 정보
/// </summary>
public class LoopInfo
{
    /// <summary>
    /// 루프 헤더 블록 (루프의 진입점)
    /// </summary>
    public BasicBlock Header { get; init; } = null!;

    /// <summary>
    /// 루프에 포함된 블록들
    /// </summary>
    public List<BasicBlock> Blocks { get; init; } = new();

    /// <summary>
    /// 루프 깊이 (중첩된 루프의 경우)
    /// </summary>
    public int Depth { get; set; }

    /// <summary>
    /// 부모 루프 (중첩된 경우)
    /// </summary>
    public LoopInfo? Parent { get; set; }

    /// <summary>
    /// 자식 루프들
    /// </summary>
    public List<LoopInfo> Children { get; init; } = new();

    public override string ToString()
        => $"Loop at Block {Header.Id}, {Blocks.Count} blocks, Depth {Depth}";
}

/// <summary>
/// CFG 빌더 - AST로부터 제어 흐름 그래프를 생성합니다.
/// </summary>
public class ControlFlowGraphBuilder
{
    private int _nextBlockId = 0;
    private ControlFlowGraph _cfg = null!;

    /// <summary>
    /// 프로그램 노드로부터 CFG를 생성합니다.
    /// </summary>
    public ControlFlowGraph Build(ProgramNode program)
    {
        _nextBlockId = 0;
        _cfg = new ControlFlowGraph();

        // 진입 블록 생성
        var entryBlock = CreateBlock(BasicBlockType.Entry, "ENTRY");
        _cfg.EntryBlock = entryBlock;

        // 종료 블록 생성
        var exitBlock = CreateBlock(BasicBlockType.Exit, "EXIT");
        _cfg.ExitBlock = exitBlock;

        // 프로그램 구문들을 처리
        var lastBlock = BuildStatements(program.Statements, entryBlock, exitBlock);

        // 마지막 블록이 종료 블록으로 연결되지 않은 경우 연결
        if (lastBlock != null && !lastBlock.Successors.Contains(exitBlock))
        {
            ConnectBlocks(lastBlock, exitBlock);
        }

        return _cfg;
    }

    /// <summary>
    /// Function Block 노드로부터 CFG를 생성합니다.
    /// </summary>
    public ControlFlowGraph Build(FunctionBlockNode functionBlock)
    {
        _nextBlockId = 0;
        _cfg = new ControlFlowGraph();

        var entryBlock = CreateBlock(BasicBlockType.Entry, "ENTRY");
        _cfg.EntryBlock = entryBlock;

        var exitBlock = CreateBlock(BasicBlockType.Exit, "EXIT");
        _cfg.ExitBlock = exitBlock;

        var lastBlock = BuildStatements(functionBlock.Statements, entryBlock, exitBlock);

        if (lastBlock != null && !lastBlock.Successors.Contains(exitBlock))
        {
            ConnectBlocks(lastBlock, exitBlock);
        }

        return _cfg;
    }

    /// <summary>
    /// Function 노드로부터 CFG를 생성합니다.
    /// </summary>
    public ControlFlowGraph Build(FunctionNode function)
    {
        _nextBlockId = 0;
        _cfg = new ControlFlowGraph();

        var entryBlock = CreateBlock(BasicBlockType.Entry, "ENTRY");
        _cfg.EntryBlock = entryBlock;

        var exitBlock = CreateBlock(BasicBlockType.Exit, "EXIT");
        _cfg.ExitBlock = exitBlock;

        var lastBlock = BuildStatements(function.Statements, entryBlock, exitBlock);

        if (lastBlock != null && !lastBlock.Successors.Contains(exitBlock))
        {
            ConnectBlocks(lastBlock, exitBlock);
        }

        return _cfg;
    }

    /// <summary>
    /// 구문 목록으로부터 블록들을 생성하고 연결합니다.
    /// </summary>
    private BasicBlock? BuildStatements(
        List<StatementNode> statements,
        BasicBlock startBlock,
        BasicBlock exitBlock)
    {
        if (statements.Count == 0)
            return startBlock;

        BasicBlock currentBlock = startBlock;

        foreach (var statement in statements)
        {
            currentBlock = BuildStatement(statement, currentBlock, exitBlock);
        }

        return currentBlock;
    }

    /// <summary>
    /// 개별 구문을 처리하고 블록을 생성합니다.
    /// </summary>
    private BasicBlock BuildStatement(
        StatementNode statement,
        BasicBlock currentBlock,
        BasicBlock exitBlock)
    {
        return statement switch
        {
            AssignmentStatementNode assign => BuildAssignment(assign, currentBlock),
            IfStatementNode ifStmt => BuildIfStatement(ifStmt, currentBlock, exitBlock),
            CaseStatementNode caseStmt => BuildCaseStatement(caseStmt, currentBlock, exitBlock),
            ForStatementNode forStmt => BuildForLoop(forStmt, currentBlock, exitBlock),
            WhileStatementNode whileStmt => BuildWhileLoop(whileStmt, currentBlock, exitBlock),
            RepeatStatementNode repeatStmt => BuildRepeatLoop(repeatStmt, currentBlock, exitBlock),
            ReturnStatementNode => BuildReturn(currentBlock, exitBlock),
            ExitStatementNode => currentBlock, // EXIT는 루프 문맥에서 처리
            _ => BuildSimpleStatement(statement, currentBlock)
        };
    }

    /// <summary>
    /// 할당문 처리
    /// </summary>
    private BasicBlock BuildAssignment(AssignmentStatementNode assign, BasicBlock currentBlock)
    {
        // 현재 블록이 비어있지 않으면 새 블록 생성
        if (currentBlock.Statements.Count > 0 && currentBlock.Type != BasicBlockType.Normal)
        {
            var newBlock = CreateBlock(BasicBlockType.Normal);
            ConnectBlocks(currentBlock, newBlock);
            currentBlock = newBlock;
        }

        currentBlock.Statements.Add(assign);
        return currentBlock;
    }

    /// <summary>
    /// IF 문 처리
    /// </summary>
    private BasicBlock BuildIfStatement(
        IfStatementNode ifStmt,
        BasicBlock currentBlock,
        BasicBlock exitBlock)
    {
        // 조건 평가 블록
        var conditionBlock = currentBlock.Statements.Count == 0
            ? currentBlock
            : CreateBlockAndConnect(BasicBlockType.Conditional, currentBlock);

        // 병합 블록 (IF 문 이후)
        var mergeBlock = CreateBlock(BasicBlockType.Normal, "IF_MERGE");

        // THEN 블록
        var thenBlock = CreateBlock(BasicBlockType.Normal, "THEN");
        ConnectBlocks(conditionBlock, thenBlock);
        var thenExit = BuildStatements(ifStmt.ThenBlock, thenBlock, exitBlock);
        if (thenExit != null)
        {
            ConnectBlocks(thenExit, mergeBlock);
        }

        // ELSIF 블록들
        BasicBlock? currentElsifEntry = null;
        foreach (var elsif in ifStmt.ElsifClauses)
        {
            var elsifCondBlock = CreateBlock(BasicBlockType.Conditional, "ELSIF");
            if (currentElsifEntry == null)
            {
                ConnectBlocks(conditionBlock, elsifCondBlock);
            }

            var elsifThenBlock = CreateBlock(BasicBlockType.Normal, "ELSIF_THEN");
            ConnectBlocks(elsifCondBlock, elsifThenBlock);
            var elsifExit = BuildStatements(elsif.Statements, elsifThenBlock, exitBlock);
            if (elsifExit != null)
            {
                ConnectBlocks(elsifExit, mergeBlock);
            }

            currentElsifEntry = elsifCondBlock;
        }

        // ELSE 블록
        if (ifStmt.ElseBlock != null)
        {
            var elseBlock = CreateBlock(BasicBlockType.Normal, "ELSE");
            var connectFrom = currentElsifEntry ?? conditionBlock;
            ConnectBlocks(connectFrom, elseBlock);

            var elseExit = BuildStatements(ifStmt.ElseBlock, elseBlock, exitBlock);
            if (elseExit != null)
            {
                ConnectBlocks(elseExit, mergeBlock);
            }
        }
        else
        {
            // ELSE가 없으면 조건이 거짓일 때 병합 블록으로
            var connectFrom = currentElsifEntry ?? conditionBlock;
            ConnectBlocks(connectFrom, mergeBlock);
        }

        return mergeBlock;
    }

    /// <summary>
    /// CASE 문 처리
    /// </summary>
    private BasicBlock BuildCaseStatement(
        CaseStatementNode caseStmt,
        BasicBlock currentBlock,
        BasicBlock exitBlock)
    {
        var caseBlock = currentBlock.Statements.Count == 0
            ? currentBlock
            : CreateBlockAndConnect(BasicBlockType.Conditional, currentBlock);

        var mergeBlock = CreateBlock(BasicBlockType.Normal, "CASE_MERGE");

        foreach (var element in caseStmt.CaseElements)
        {
            var elementBlock = CreateBlock(BasicBlockType.Normal, "CASE_ELEMENT");
            ConnectBlocks(caseBlock, elementBlock);

            var elementExit = BuildStatements(element.Statements, elementBlock, exitBlock);
            if (elementExit != null)
            {
                ConnectBlocks(elementExit, mergeBlock);
            }
        }

        if (caseStmt.ElseBlock != null)
        {
            var elseBlock = CreateBlock(BasicBlockType.Normal, "CASE_ELSE");
            ConnectBlocks(caseBlock, elseBlock);

            var elseExit = BuildStatements(caseStmt.ElseBlock, elseBlock, exitBlock);
            if (elseExit != null)
            {
                ConnectBlocks(elseExit, mergeBlock);
            }
        }
        else
        {
            ConnectBlocks(caseBlock, mergeBlock);
        }

        return mergeBlock;
    }

    /// <summary>
    /// FOR 루프 처리
    /// </summary>
    private BasicBlock BuildForLoop(
        ForStatementNode forStmt,
        BasicBlock currentBlock,
        BasicBlock exitBlock)
    {
        var loopHeader = CreateBlockAndConnect(BasicBlockType.LoopHeader, currentBlock, "FOR_HEADER");
        var loopBody = CreateBlock(BasicBlockType.LoopBody, "FOR_BODY");
        var loopExit = CreateBlock(BasicBlockType.LoopExit, "FOR_EXIT");

        ConnectBlocks(loopHeader, loopBody);
        ConnectBlocks(loopHeader, loopExit); // 조건이 거짓일 때

        var bodyExit = BuildStatements(forStmt.Body, loopBody, exitBlock);
        if (bodyExit != null)
        {
            ConnectBlocks(bodyExit, loopHeader); // 백 엣지
        }

        return loopExit;
    }

    /// <summary>
    /// WHILE 루프 처리
    /// </summary>
    private BasicBlock BuildWhileLoop(
        WhileStatementNode whileStmt,
        BasicBlock currentBlock,
        BasicBlock exitBlock)
    {
        var loopHeader = CreateBlockAndConnect(BasicBlockType.LoopHeader, currentBlock, "WHILE_HEADER");
        var loopBody = CreateBlock(BasicBlockType.LoopBody, "WHILE_BODY");
        var loopExit = CreateBlock(BasicBlockType.LoopExit, "WHILE_EXIT");

        ConnectBlocks(loopHeader, loopBody);
        ConnectBlocks(loopHeader, loopExit);

        var bodyExit = BuildStatements(whileStmt.Body, loopBody, exitBlock);
        if (bodyExit != null)
        {
            ConnectBlocks(bodyExit, loopHeader);
        }

        return loopExit;
    }

    /// <summary>
    /// REPEAT 루프 처리
    /// </summary>
    private BasicBlock BuildRepeatLoop(
        RepeatStatementNode repeatStmt,
        BasicBlock currentBlock,
        BasicBlock exitBlock)
    {
        var loopBody = CreateBlockAndConnect(BasicBlockType.LoopBody, currentBlock, "REPEAT_BODY");
        var loopHeader = CreateBlock(BasicBlockType.LoopHeader, "REPEAT_HEADER");
        var loopExit = CreateBlock(BasicBlockType.LoopExit, "REPEAT_EXIT");

        var bodyExit = BuildStatements(repeatStmt.Body, loopBody, exitBlock);
        if (bodyExit != null)
        {
            ConnectBlocks(bodyExit, loopHeader);
        }

        ConnectBlocks(loopHeader, loopBody); // UNTIL이 거짓일 때
        ConnectBlocks(loopHeader, loopExit); // UNTIL이 참일 때

        return loopExit;
    }

    /// <summary>
    /// RETURN 문 처리
    /// </summary>
    private BasicBlock BuildReturn(BasicBlock currentBlock, BasicBlock exitBlock)
    {
        ConnectBlocks(currentBlock, exitBlock);
        return CreateBlock(BasicBlockType.Normal); // 도달 불가능한 블록
    }

    /// <summary>
    /// 일반 구문 처리
    /// </summary>
    private BasicBlock BuildSimpleStatement(StatementNode statement, BasicBlock currentBlock)
    {
        currentBlock.Statements.Add(statement);
        return currentBlock;
    }

    /// <summary>
    /// 새 블록 생성
    /// </summary>
    private BasicBlock CreateBlock(BasicBlockType type = BasicBlockType.Normal, string label = "")
    {
        var block = new BasicBlock
        {
            Id = _nextBlockId++,
            Type = type,
            Label = string.IsNullOrEmpty(label) ? $"B{_nextBlockId - 1}" : label
        };

        _cfg.Blocks.Add(block);
        return block;
    }

    /// <summary>
    /// 새 블록을 생성하고 이전 블록과 연결
    /// </summary>
    private BasicBlock CreateBlockAndConnect(
        BasicBlockType type,
        BasicBlock predecessor,
        string label = "")
    {
        var block = CreateBlock(type, label);
        ConnectBlocks(predecessor, block);
        return block;
    }

    /// <summary>
    /// 두 블록을 연결
    /// </summary>
    private void ConnectBlocks(BasicBlock from, BasicBlock to)
    {
        from.AddSuccessor(to);
        to.AddPredecessor(from);
    }
}
