using Microsoft.Extensions.Logging;
using System.Text;
using TwinCatQA.Domain.Contracts;
using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Services;

namespace TwinCatQA.Infrastructure.Analysis;

/// <summary>
/// ANTLR AST 기반 의존성 분석기
/// </summary>
public class DependencyAnalyzer : IDependencyAnalyzer
{
    private readonly ILogger<DependencyAnalyzer> _logger;
    private readonly IParserService _parserService;

    public DependencyAnalyzer(
        ILogger<DependencyAnalyzer> logger,
        IParserService parserService)
    {
        _logger = logger;
        _parserService = parserService;
    }

    /// <inheritdoc/>
    public async Task<DependencyAnalysis> AnalyzeDependenciesAsync(
        ValidationSession session,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("의존성 분석 시작: {ProjectPath}", session.ProjectPath);

        var graph = await BuildDependencyGraphAsync(session, cancellationToken);
        var circularReferences = DetectCircularReferences(graph);
        var callGraph = await BuildCallGraphAsync(session, cancellationToken);

        var analysis = new DependencyAnalysis
        {
            Graph = graph,
            CircularReferences = circularReferences,
            CallGraph = callGraph,
            ProjectPath = session.ProjectPath,
            AnalysisTime = DateTime.UtcNow
        };

        _logger.LogInformation("의존성 분석 완료: 노드 {NodeCount}개, 엣지 {EdgeCount}개, 순환 참조 {CircularCount}개",
            graph.Nodes.Count, graph.Edges.Count, circularReferences.Count);

        return analysis;
    }

    /// <inheritdoc/>
    public async Task<DependencyGraph> BuildDependencyGraphAsync(
        ValidationSession session,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("의존성 그래프 구축 시작");

        var graph = new DependencyGraph();

        foreach (var syntaxTree in session.SyntaxTrees)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var pouName = ExtractPouName(syntaxTree.FilePath);
            var pouType = DeterminePouType(syntaxTree);

            // 노드 추가
            graph.AddNode(new DependencyNode
            {
                Id = pouName,
                NodeType = pouType,
                FilePath = syntaxTree.FilePath,
                Description = $"{pouType}: {pouName}"
            });

            // 엣지 추가 (이 POU가 호출하는 다른 POU들)
            var dependencies = await ExtractDependenciesAsync(syntaxTree);

            foreach (var dependency in dependencies)
            {
                // 의존하는 POU 노드 추가 (아직 없다면)
                if (!graph.Nodes.Any(n => n.Id == dependency.TargetPou))
                {
                    graph.AddNode(new DependencyNode
                    {
                        Id = dependency.TargetPou,
                        NodeType = "UNKNOWN",
                        FilePath = string.Empty,
                        Description = $"외부 참조: {dependency.TargetPou}"
                    });
                }

                // 엣지 추가
                graph.AddEdge(new DependencyEdge
                {
                    From = pouName,
                    To = dependency.TargetPou,
                    Type = dependency.Type,
                    LineNumber = dependency.LineNumber
                });
            }
        }

        _logger.LogDebug("의존성 그래프 구축 완료: 노드 {NodeCount}개, 엣지 {EdgeCount}개",
            graph.Nodes.Count, graph.Edges.Count);

        return graph;
    }

    /// <inheritdoc/>
    public List<CircularReference> DetectCircularReferences(DependencyGraph graph)
    {
        _logger.LogDebug("순환 참조 탐지 시작");

        var circularReferences = new List<CircularReference>();
        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();

        foreach (var node in graph.Nodes)
        {
            if (!visited.Contains(node.Id))
            {
                var path = new List<string>();
                DetectCyclesRecursive(node.Id, graph, visited, recursionStack, path, circularReferences);
            }
        }

        _logger.LogDebug("순환 참조 {Count}개 발견", circularReferences.Count);
        return circularReferences;
    }

    /// <inheritdoc/>
    public async Task<CallGraph> BuildCallGraphAsync(
        ValidationSession session,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("함수 호출 그래프 구축 시작");

        var callGraph = new CallGraph();
        var callCounts = new Dictionary<string, int>();

        foreach (var syntaxTree in session.SyntaxTrees)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var pouName = ExtractPouName(syntaxTree.FilePath);
            var pouType = DeterminePouType(syntaxTree);

            // 노드 추가
            callGraph.Nodes.Add(new CallNode
            {
                Id = pouName,
                NodeType = pouType,
                FilePath = syntaxTree.FilePath,
                CallCount = 0
            });

            // 호출 관계 추출
            var calls = await ExtractFunctionCallsAsync(syntaxTree);

            foreach (var call in calls)
            {
                callGraph.Edges.Add(new CallEdge
                {
                    Caller = pouName,
                    Callee = call.CalleeName,
                    LineNumber = call.LineNumber,
                    FilePath = syntaxTree.FilePath
                });

                // 호출 횟수 카운트
                if (!callCounts.ContainsKey(call.CalleeName))
                {
                    callCounts[call.CalleeName] = 0;
                }
                callCounts[call.CalleeName]++;
            }
        }

        // 호출 횟수 업데이트
        foreach (var node in callGraph.Nodes)
        {
            if (callCounts.ContainsKey(node.Id))
            {
                node.CallCount = callCounts[node.Id];
            }
        }

        // 최대 호출 깊이 계산
        callGraph.MaxCallDepth = CalculateMaxCallDepth(callGraph);

        _logger.LogDebug("함수 호출 그래프 구축 완료: 노드 {NodeCount}개, 호출 {CallCount}개, 최대 깊이 {MaxDepth}",
            callGraph.Nodes.Count, callGraph.Edges.Count, callGraph.MaxCallDepth);

        return callGraph;
    }

    /// <inheritdoc/>
    public int CalculateMaxCallDepth(CallGraph callGraph)
    {
        if (callGraph.Nodes.Count == 0)
        {
            return 0;
        }

        // 진입점 함수들 (호출되지 않는 함수들)
        var entryPoints = callGraph.Nodes
            .Where(n => !callGraph.Edges.Any(e => e.Callee == n.Id))
            .Select(n => n.Id)
            .ToList();

        if (entryPoints.Count == 0)
        {
            // 모든 함수가 호출되는 경우 (순환 참조), 임의의 시작점 선택
            entryPoints.Add(callGraph.Nodes.First().Id);
        }

        int maxDepth = 0;

        foreach (var entryPoint in entryPoints)
        {
            int depth = callGraph.CalculateCallDepth(entryPoint);
            if (depth > maxDepth)
            {
                maxDepth = depth;
            }
        }

        return maxDepth;
    }

    /// <inheritdoc/>
    public List<DependencyNode> GetDependenciesForPou(string pouName, DependencyGraph graph)
    {
        return graph.GetDependencies(pouName);
    }

    /// <inheritdoc/>
    public List<DependencyNode> GetDependentsForPou(string pouName, DependencyGraph graph)
    {
        return graph.GetDependents(pouName);
    }

    /// <inheritdoc/>
    public string ExportToDotFormat(DependencyGraph graph)
    {
        _logger.LogDebug("의존성 그래프를 DOT 형식으로 내보내기");

        var sb = new StringBuilder();
        sb.AppendLine("digraph DependencyGraph {");
        sb.AppendLine("  rankdir=LR;");
        sb.AppendLine("  node [shape=box];");

        // 노드 정의
        foreach (var node in graph.Nodes)
        {
            var color = node.NodeType switch
            {
                "PROGRAM" => "lightblue",
                "FUNCTION_BLOCK" => "lightgreen",
                "FUNCTION" => "lightyellow",
                _ => "white"
            };

            sb.AppendLine($"  \"{node.Id}\" [label=\"{node.Id}\\n({node.NodeType})\", fillcolor={color}, style=filled];");
        }

        // 엣지 정의
        foreach (var edge in graph.Edges)
        {
            var style = edge.Type switch
            {
                DependencyType.FunctionCall => "solid",
                DependencyType.Inheritance => "dashed",
                DependencyType.InterfaceImplementation => "dotted",
                _ => "solid"
            };

            sb.AppendLine($"  \"{edge.From}\" -> \"{edge.To}\" [style={style}];");
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    // ============================================
    // Private Helper Methods
    // ============================================

    private void DetectCyclesRecursive(
        string nodeId,
        DependencyGraph graph,
        HashSet<string> visited,
        HashSet<string> recursionStack,
        List<string> path,
        List<CircularReference> circularReferences)
    {
        visited.Add(nodeId);
        recursionStack.Add(nodeId);
        path.Add(nodeId);

        var dependencies = graph.GetDependencies(nodeId);

        foreach (var dep in dependencies)
        {
            if (!visited.Contains(dep.Id))
            {
                DetectCyclesRecursive(dep.Id, graph, visited, recursionStack, path, circularReferences);
            }
            else if (recursionStack.Contains(dep.Id))
            {
                // 순환 참조 발견
                var cycleStartIndex = path.IndexOf(dep.Id);
                var cyclePath = path.Skip(cycleStartIndex).ToList();
                cyclePath.Add(dep.Id); // 순환을 명확히 하기 위해 시작 노드 다시 추가

                circularReferences.Add(new CircularReference
                {
                    CyclePath = cyclePath,
                    Severity = IssueSeverity.Error,
                    Description = $"순환 참조 발견: {string.Join(" → ", cyclePath)}"
                });
            }
        }

        path.RemoveAt(path.Count - 1);
        recursionStack.Remove(nodeId);
    }

    private async Task<List<DependencyInfo>> ExtractDependenciesAsync(SyntaxTree syntaxTree)
    {
        return await Task.Run(() =>
        {
            var dependencies = new List<DependencyInfo>();

            // 코드에서 다른 POU 호출 찾기
            var lines = syntaxTree.SourceCode.Split('\n');
            int lineNumber = 0;

            foreach (var line in lines)
            {
                lineNumber++;
                var trimmed = line.Trim();

                // 함수 호출 패턴 찾기 (예: FB_Name(), FUNC_Name() 등)
                if (trimmed.Contains('(') && !trimmed.StartsWith("//") && !trimmed.StartsWith("(*"))
                {
                    var callMatch = System.Text.RegularExpressions.Regex.Match(trimmed, @"(\w+)\s*\(");
                    if (callMatch.Success)
                    {
                        var calledPou = callMatch.Groups[1].Value;

                        // 키워드가 아닌 경우만 의존성으로 추가
                        if (!IsKeyword(calledPou))
                        {
                            dependencies.Add(new DependencyInfo
                            {
                                TargetPou = calledPou,
                                Type = DependencyType.FunctionCall,
                                LineNumber = lineNumber
                            });
                        }
                    }
                }
            }

            return dependencies;
        });
    }

    private async Task<List<FunctionCallInfo>> ExtractFunctionCallsAsync(SyntaxTree syntaxTree)
    {
        return await Task.Run(() =>
        {
            var calls = new List<FunctionCallInfo>();

            var lines = syntaxTree.SourceCode.Split('\n');
            int lineNumber = 0;

            foreach (var line in lines)
            {
                lineNumber++;
                var trimmed = line.Trim();

                if (trimmed.Contains('(') && !trimmed.StartsWith("//") && !trimmed.StartsWith("(*"))
                {
                    var callMatch = System.Text.RegularExpressions.Regex.Match(trimmed, @"(\w+)\s*\(");
                    if (callMatch.Success)
                    {
                        var calleeName = callMatch.Groups[1].Value;

                        if (!IsKeyword(calleeName))
                        {
                            calls.Add(new FunctionCallInfo
                            {
                                CalleeName = calleeName,
                                LineNumber = lineNumber
                            });
                        }
                    }
                }
            }

            return calls;
        });
    }

    private string ExtractPouName(string filePath)
    {
        return Path.GetFileNameWithoutExtension(filePath);
    }

    private string DeterminePouType(SyntaxTree syntaxTree)
    {
        var source = syntaxTree.SourceCode;

        if (source.Contains("PROGRAM ", StringComparison.OrdinalIgnoreCase))
            return "PROGRAM";
        if (source.Contains("FUNCTION_BLOCK ", StringComparison.OrdinalIgnoreCase))
            return "FUNCTION_BLOCK";
        if (source.Contains("FUNCTION ", StringComparison.OrdinalIgnoreCase))
            return "FUNCTION";
        if (source.Contains("INTERFACE ", StringComparison.OrdinalIgnoreCase))
            return "INTERFACE";

        return "UNKNOWN";
    }

    private bool IsKeyword(string word)
    {
        var keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "IF", "THEN", "ELSE", "END_IF", "FOR", "TO", "DO", "END_FOR",
            "WHILE", "END_WHILE", "CASE", "OF", "END_CASE", "RETURN",
            "VAR", "END_VAR", "TRUE", "FALSE", "NOT", "AND", "OR", "XOR"
        };

        return keywords.Contains(word);
    }

    private class DependencyInfo
    {
        public string TargetPou { get; set; } = string.Empty;
        public DependencyType Type { get; set; }
        public int LineNumber { get; set; }
    }

    private class FunctionCallInfo
    {
        public string CalleeName { get; set; } = string.Empty;
        public int LineNumber { get; set; }
    }
}
