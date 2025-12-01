using System.Diagnostics;
using TwinCatQA.Domain.Models.AST;

namespace TwinCatQA.Infrastructure.Analysis.DataFlow;

/// <summary>
/// 데이터 흐름 분석기 인터페이스
/// </summary>
public interface IDataFlowAnalyzer
{
    /// <summary>
    /// Syntax Tree를 분석하여 데이터 흐름 결과를 반환합니다.
    /// </summary>
    DataFlowResult Analyze(SyntaxTree syntaxTree);
}

/// <summary>
/// 데이터 흐름 분석기
/// Level 2 분석: 정의-사용 체인, 제어 흐름 그래프, 활성 변수 분석
/// </summary>
public class DataFlowAnalyzer : IDataFlowAnalyzer
{
    /// <summary>
    /// Syntax Tree를 분석하여 데이터 흐름 결과를 반환합니다.
    /// </summary>
    public DataFlowResult Analyze(SyntaxTree syntaxTree)
    {
        var stopwatch = Stopwatch.StartNew();

        // 1. 각 POU(Program Organization Unit)별로 분석
        var allChains = new Dictionary<string, DefUseChain>();
        var allCFGs = new List<ControlFlowGraph>();

        foreach (var rootNode in syntaxTree.RootNodes)
        {
            ControlFlowGraph? cfg = null;
            DataFlowVisitor? visitor = null;

            // CFG 생성
            cfg = BuildControlFlowGraph(rootNode);
            if (cfg == null)
                continue;

            allCFGs.Add(cfg);

            // Def-Use Chain 수집
            visitor = CollectDefUseChains(rootNode, cfg);
            if (visitor != null)
            {
                // 체인 병합
                foreach (var (varName, chain) in visitor.VariableChains)
                {
                    if (!allChains.ContainsKey(varName))
                    {
                        allChains[varName] = chain;
                    }
                    else
                    {
                        // 동일 변수가 여러 POU에 있는 경우 병합
                        MergeChains(allChains[varName], chain);
                    }
                }
            }
        }

        // 주 CFG 선택 (첫 번째 또는 가장 큰 CFG)
        var mainCFG = allCFGs.FirstOrDefault() ?? new ControlFlowGraph();

        // 2. 분석 결과 생성
        var result = new DataFlowResult
        {
            VariableChains = allChains,
            CFG = mainCFG,
            FilePath = syntaxTree.FilePath
        };

        // 3. 도달 불가능한 블록 검출
        result.UnreachableBlocks.AddRange(mainCFG.GetUnreachableBlocks());

        // 4. 루프 검출
        result.Loops.AddRange(mainCFG.DetectLoops());

        // 5. 사용되지 않는 변수 검출
        result.UnusedVariables.AddRange(FindUnusedVariables(allChains));

        // 6. 초기화되지 않은 사용 검출
        result.UninitializedUsages.AddRange(FindUninitializedUsages(allChains));

        // 7. 활성 변수 분석 (Liveness Analysis)
        result.LivenessResult = PerformLivenessAnalysis(mainCFG, allChains);

        // 8. 통계 계산
        CalculateStatistics(result);

        stopwatch.Stop();
        result.AnalysisTimeMs = stopwatch.ElapsedMilliseconds;

        return result;
    }

    /// <summary>
    /// AST 노드로부터 제어 흐름 그래프를 생성합니다.
    /// </summary>
    private ControlFlowGraph? BuildControlFlowGraph(ASTNode node)
    {
        var builder = new ControlFlowGraphBuilder();

        return node switch
        {
            ProgramNode program => builder.Build(program),
            FunctionBlockNode fb => builder.Build(fb),
            FunctionNode function => builder.Build(function),
            _ => null
        };
    }

    /// <summary>
    /// AST 노드로부터 Def-Use Chain을 수집합니다.
    /// </summary>
    private DataFlowVisitor? CollectDefUseChains(ASTNode node, ControlFlowGraph cfg)
    {
        var visitor = new DataFlowVisitor();

        // CFG의 각 블록을 순회하며 변수 사용 추적
        foreach (var block in cfg.Blocks)
        {
            visitor.SetCurrentBlock(block.Id);
            visitor.SetLoopContext(block.Type == BasicBlockType.LoopBody ||
                                   block.Type == BasicBlockType.LoopHeader);
            visitor.SetConditionalContext(block.Type == BasicBlockType.Conditional);

            foreach (var statement in block.Statements)
            {
                statement.Accept(visitor);

                // 블록의 Def/Use 집합 업데이트
                if (statement is AssignmentStatementNode assignment)
                {
                    block.DefinedVariables.Add(assignment.Left.Name);
                }

                // 사용된 변수 추적 (재귀적으로 표현식 탐색 필요)
                var usedVars = ExtractUsedVariables(statement);
                foreach (var varName in usedVars)
                {
                    block.UsedVariables.Add(varName);
                }
            }
        }

        // 전체 노드 방문 (CFG에 포함되지 않은 선언 등)
        node.Accept(visitor);

        return visitor;
    }

    /// <summary>
    /// 구문에서 사용된 변수들을 추출합니다.
    /// </summary>
    private HashSet<string> ExtractUsedVariables(StatementNode statement)
    {
        var usedVars = new HashSet<string>();

        void ExtractFromExpression(ExpressionNode? expr)
        {
            if (expr == null)
                return;

            switch (expr)
            {
                case VariableReferenceNode varRef:
                    usedVars.Add(varRef.Name);
                    if (varRef.ArrayIndices != null)
                    {
                        foreach (var index in varRef.ArrayIndices)
                        {
                            ExtractFromExpression(index);
                        }
                    }
                    break;

                case BinaryExpressionNode binary:
                    ExtractFromExpression(binary.Left);
                    ExtractFromExpression(binary.Right);
                    break;

                case UnaryExpressionNode unary:
                    ExtractFromExpression(unary.Operand);
                    break;

                case FunctionCallNode funcCall:
                    foreach (var arg in funcCall.Arguments)
                    {
                        ExtractFromExpression(arg.Value);
                    }
                    break;
            }
        }

        switch (statement)
        {
            case AssignmentStatementNode assignment:
                ExtractFromExpression(assignment.Right);
                break;

            case IfStatementNode ifStmt:
                ExtractFromExpression(ifStmt.Condition);
                break;

            case CaseStatementNode caseStmt:
                ExtractFromExpression(caseStmt.Expression);
                break;

            case ForStatementNode forStmt:
                ExtractFromExpression(forStmt.StartValue);
                ExtractFromExpression(forStmt.EndValue);
                ExtractFromExpression(forStmt.StepValue);
                break;

            case WhileStatementNode whileStmt:
                ExtractFromExpression(whileStmt.Condition);
                break;

            case RepeatStatementNode repeatStmt:
                ExtractFromExpression(repeatStmt.UntilCondition);
                break;
        }

        return usedVars;
    }

    /// <summary>
    /// 두 Def-Use Chain을 병합합니다.
    /// </summary>
    private void MergeChains(DefUseChain target, DefUseChain source)
    {
        target.Definitions.AddRange(source.Definitions);
        target.Usages.AddRange(source.Usages);

        if (source.IsInitialized)
        {
            target.IsInitialized = true;
        }

        // 선언 정보 병합 (우선순위: target 유지)
        target.Declaration ??= source.Declaration;
    }

    /// <summary>
    /// 사용되지 않는 변수를 찾습니다.
    /// </summary>
    private List<UnusedVariable> FindUnusedVariables(Dictionary<string, DefUseChain> chains)
    {
        var unused = new List<UnusedVariable>();

        foreach (var (varName, chain) in chains)
        {
            // 선언은 있지만 사용되지 않는 변수
            if (chain.Declaration != null && !chain.IsUsed)
            {
                // VAR_OUTPUT은 외부로 값을 전달하므로 제외
                if (chain.Declaration.Scope == "Output")
                    continue;

                unused.Add(new UnusedVariable
                {
                    Name = varName,
                    Declaration = chain.Declaration,
                    IsWriteOnly = chain.IsAssigned
                });
            }
        }

        return unused;
    }

    /// <summary>
    /// 초기화되지 않은 사용을 찾습니다.
    /// </summary>
    private List<UninitializedUsage> FindUninitializedUsages(Dictionary<string, DefUseChain> chains)
    {
        var uninitializedList = new List<UninitializedUsage>();

        foreach (var (varName, chain) in chains)
        {
            var uninitUsages = chain.GetUninitializedUsages();

            foreach (var usage in uninitUsages)
            {
                uninitializedList.Add(new UninitializedUsage
                {
                    Name = varName,
                    Usage = usage,
                    Declaration = chain.Declaration
                });
            }
        }

        return uninitializedList;
    }

    /// <summary>
    /// 활성 변수 분석 (Liveness Analysis)을 수행합니다.
    /// 역방향 데이터 흐름 분석으로 구현됩니다.
    /// </summary>
    private LivenessAnalysisResult PerformLivenessAnalysis(
        ControlFlowGraph cfg,
        Dictionary<string, DefUseChain> chains)
    {
        var result = new LivenessAnalysisResult();

        // 초기화: 모든 블록의 Live-in, Live-out을 빈 집합으로
        foreach (var block in cfg.Blocks)
        {
            result.LiveInSets[block.Id] = new HashSet<string>();
            result.LiveOutSets[block.Id] = new HashSet<string>();
        }

        // 반복적으로 계산 (Fixed-point iteration)
        bool changed = true;
        int maxIterations = 100;
        int iteration = 0;

        while (changed && iteration < maxIterations)
        {
            changed = false;
            iteration++;

            // 역방향 포스트오더로 순회 (효율성)
            var blocks = cfg.GetReversePostOrder();
            blocks.Reverse(); // 역방향으로

            foreach (var block in blocks)
            {
                // Live-out[B] = ∪(후속자 블록들의 Live-in)
                var newLiveOut = new HashSet<string>();
                foreach (var successor in block.Successors)
                {
                    if (result.LiveInSets.TryGetValue(successor.Id, out var successorLiveIn))
                    {
                        newLiveOut.UnionWith(successorLiveIn);
                    }
                }

                // Live-in[B] = Use[B] ∪ (Live-out[B] - Def[B])
                var newLiveIn = new HashSet<string>(block.UsedVariables);
                var liveOutMinusDef = new HashSet<string>(newLiveOut);
                liveOutMinusDef.ExceptWith(block.DefinedVariables);
                newLiveIn.UnionWith(liveOutMinusDef);

                // 변경 검사
                if (!newLiveIn.SetEquals(result.LiveInSets[block.Id]) ||
                    !newLiveOut.SetEquals(result.LiveOutSets[block.Id]))
                {
                    changed = true;
                    result.LiveInSets[block.Id] = newLiveIn;
                    result.LiveOutSets[block.Id] = newLiveOut;
                }
            }
        }

        // 블록 객체에도 결과 저장
        foreach (var block in cfg.Blocks)
        {
            if (result.LiveInSets.TryGetValue(block.Id, out var liveIn))
            {
                block.LiveIn = liveIn;
            }

            if (result.LiveOutSets.TryGetValue(block.Id, out var liveOut))
            {
                block.LiveOut = liveOut;
            }
        }

        return result;
    }

    /// <summary>
    /// 분석 통계를 계산합니다.
    /// </summary>
    private void CalculateStatistics(DataFlowResult result)
    {
        var stats = result.Statistics;

        stats.TotalVariables = result.VariableChains.Count;

        foreach (var (varName, chain) in result.VariableChains)
        {
            // 스코프별 변수 수 계산
            if (chain.Declaration != null)
            {
                switch (chain.Declaration.Scope)
                {
                    case "Input":
                        stats.InputVariables++;
                        break;
                    case "Output":
                        stats.OutputVariables++;
                        break;
                    case "Local":
                        stats.LocalVariables++;
                        break;
                    case "Global":
                        stats.GlobalVariableUsages++;
                        break;
                }
            }

            // 할당 및 참조 횟수
            stats.TotalAssignments += chain.Definitions.Count;
            stats.TotalReferences += chain.Usages.Count;

            // 조건부 할당 및 루프 내 할당
            foreach (var def in chain.Definitions)
            {
                if (def.IsConditional)
                {
                    stats.ConditionalAssignments++;
                }

                if (def.IsInLoop)
                {
                    stats.LoopAssignments++;
                }
            }
        }
    }
}
