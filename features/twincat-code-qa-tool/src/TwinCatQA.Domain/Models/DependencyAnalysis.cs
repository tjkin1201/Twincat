namespace TwinCatQA.Domain.Models;

/// <summary>
/// 의존성 분석 결과
/// </summary>
public class DependencyAnalysis
{
    /// <summary>
    /// 의존성 그래프
    /// </summary>
    public DependencyGraph Graph { get; init; } = new();

    /// <summary>
    /// 순환 참조 목록
    /// </summary>
    public List<CircularReference> CircularReferences { get; init; } = new();

    /// <summary>
    /// 함수 호출 그래프
    /// </summary>
    public CallGraph CallGraph { get; init; } = new();

    /// <summary>
    /// 분석된 프로젝트 경로
    /// </summary>
    public string ProjectPath { get; init; } = string.Empty;

    /// <summary>
    /// 분석 시간
    /// </summary>
    public DateTime AnalysisTime { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// 순환 참조 존재 여부
    /// </summary>
    public bool HasCircularReferences => CircularReferences.Count > 0;
}

/// <summary>
/// 의존성 그래프
/// </summary>
public class DependencyGraph
{
    /// <summary>
    /// 노드 (POU) 목록
    /// </summary>
    public List<DependencyNode> Nodes { get; init; } = new();

    /// <summary>
    /// 엣지 (의존성) 목록
    /// </summary>
    public List<DependencyEdge> Edges { get; init; } = new();

    /// <summary>
    /// 노드 추가
    /// </summary>
    public void AddNode(DependencyNode node)
    {
        if (!Nodes.Any(n => n.Id == node.Id))
        {
            Nodes.Add(node);
        }
    }

    /// <summary>
    /// 엣지 추가
    /// </summary>
    public void AddEdge(DependencyEdge edge)
    {
        if (!Edges.Any(e => e.From == edge.From && e.To == edge.To))
        {
            Edges.Add(edge);
        }
    }

    /// <summary>
    /// 특정 노드의 의존성 목록 조회
    /// </summary>
    public List<DependencyNode> GetDependencies(string nodeId)
    {
        var dependencyIds = Edges
            .Where(e => e.From == nodeId)
            .Select(e => e.To)
            .ToList();

        return Nodes.Where(n => dependencyIds.Contains(n.Id)).ToList();
    }

    /// <summary>
    /// 특정 노드에 의존하는 노드 목록 조회
    /// </summary>
    public List<DependencyNode> GetDependents(string nodeId)
    {
        var dependentIds = Edges
            .Where(e => e.To == nodeId)
            .Select(e => e.From)
            .ToList();

        return Nodes.Where(n => dependentIds.Contains(n.Id)).ToList();
    }
}

/// <summary>
/// 의존성 노드 (POU)
/// </summary>
public class DependencyNode
{
    /// <summary>
    /// 노드 ID (POU 이름)
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// 노드 타입 (PROGRAM, FUNCTION_BLOCK, FUNCTION 등)
    /// </summary>
    public string NodeType { get; init; } = string.Empty;

    /// <summary>
    /// 파일 경로
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// 설명
    /// </summary>
    public string Description { get; init; } = string.Empty;
}

/// <summary>
/// 의존성 엣지 (A → B 관계)
/// </summary>
public class DependencyEdge
{
    /// <summary>
    /// 시작 노드 ID
    /// </summary>
    public string From { get; init; } = string.Empty;

    /// <summary>
    /// 종료 노드 ID
    /// </summary>
    public string To { get; init; } = string.Empty;

    /// <summary>
    /// 의존성 타입 (호출, 상속, 사용 등)
    /// </summary>
    public DependencyType Type { get; init; } = DependencyType.FunctionCall;

    /// <summary>
    /// 라인 번호 (의존성이 발생한 위치)
    /// </summary>
    public int LineNumber { get; init; }
}

/// <summary>
/// 순환 참조
/// </summary>
public class CircularReference
{
    /// <summary>
    /// 순환 참조 경로 (A → B → C → A)
    /// </summary>
    public List<string> CyclePath { get; init; } = new();

    /// <summary>
    /// 심각도
    /// </summary>
    public IssueSeverity Severity { get; init; } = IssueSeverity.Error;

    /// <summary>
    /// 설명
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// 순환 참조 경로 문자열
    /// </summary>
    public string CyclePathString => string.Join(" → ", CyclePath);
}

/// <summary>
/// 함수 호출 그래프
/// </summary>
public class CallGraph
{
    /// <summary>
    /// 호출 노드 목록
    /// </summary>
    public List<CallNode> Nodes { get; init; } = new();

    /// <summary>
    /// 호출 관계 목록
    /// </summary>
    public List<CallEdge> Edges { get; init; } = new();

    /// <summary>
    /// 최대 호출 깊이
    /// </summary>
    public int MaxCallDepth { get; set; }

    /// <summary>
    /// 특정 함수의 호출 깊이 계산
    /// </summary>
    public int CalculateCallDepth(string functionName)
    {
        var visited = new HashSet<string>();
        return CalculateCallDepthRecursive(functionName, visited);
    }

    private int CalculateCallDepthRecursive(string functionName, HashSet<string> visited)
    {
        if (visited.Contains(functionName))
        {
            return 0; // 순환 참조 방지
        }

        visited.Add(functionName);

        var callees = Edges
            .Where(e => e.Caller == functionName)
            .Select(e => e.Callee)
            .ToList();

        if (callees.Count == 0)
        {
            return 1;
        }

        var maxDepth = callees
            .Select(callee => CalculateCallDepthRecursive(callee, new HashSet<string>(visited)))
            .Max();

        return maxDepth + 1;
    }
}

/// <summary>
/// 호출 노드 (함수/프로그램/펑션블록)
/// </summary>
public class CallNode
{
    /// <summary>
    /// 노드 ID (함수명)
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// 노드 타입
    /// </summary>
    public string NodeType { get; init; } = string.Empty;

    /// <summary>
    /// 파일 경로
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// 호출 횟수 (이 함수가 호출되는 횟수)
    /// </summary>
    public int CallCount { get; set; }
}

/// <summary>
/// 호출 관계 (A calls B)
/// </summary>
public class CallEdge
{
    /// <summary>
    /// 호출자 함수
    /// </summary>
    public string Caller { get; init; } = string.Empty;

    /// <summary>
    /// 피호출자 함수
    /// </summary>
    public string Callee { get; init; } = string.Empty;

    /// <summary>
    /// 호출 라인 번호
    /// </summary>
    public int LineNumber { get; init; }

    /// <summary>
    /// 파일 경로
    /// </summary>
    public string FilePath { get; init; } = string.Empty;
}

/// <summary>
/// 의존성 타입
/// </summary>
public enum DependencyType
{
    /// <summary>함수 호출</summary>
    FunctionCall,

    /// <summary>상속</summary>
    Inheritance,

    /// <summary>인터페이스 구현</summary>
    InterfaceImplementation,

    /// <summary>타입 사용</summary>
    TypeUsage,

    /// <summary>변수 참조</summary>
    VariableReference
}
