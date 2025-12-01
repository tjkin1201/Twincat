namespace TwinCatQA.Infrastructure.Analysis.DataFlow;

/// <summary>
/// 데이터 흐름 분석 결과
/// </summary>
public class DataFlowResult
{
    /// <summary>
    /// 변수별 정의-사용 체인
    /// Key: 변수명, Value: DefUseChain
    /// </summary>
    public Dictionary<string, DefUseChain> VariableChains { get; init; } = new();

    /// <summary>
    /// 사용되지 않는 변수 목록
    /// </summary>
    public List<UnusedVariable> UnusedVariables { get; init; } = new();

    /// <summary>
    /// 초기화되지 않은 사용 목록
    /// </summary>
    public List<UninitializedUsage> UninitializedUsages { get; init; } = new();

    /// <summary>
    /// 제어 흐름 그래프
    /// </summary>
    public ControlFlowGraph CFG { get; init; } = null!;

    /// <summary>
    /// 도달 불가능한 코드 블록 목록
    /// </summary>
    public List<BasicBlock> UnreachableBlocks { get; init; } = new();

    /// <summary>
    /// 감지된 루프 정보
    /// </summary>
    public List<LoopInfo> Loops { get; init; } = new();

    /// <summary>
    /// 분석 대상 파일 경로
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// 분석 시간 (밀리초)
    /// </summary>
    public long AnalysisTimeMs { get; set; }

    /// <summary>
    /// 분석 통계
    /// </summary>
    public DataFlowStatistics Statistics { get; init; } = new();

    /// <summary>
    /// 활성 변수 분석 결과 (Liveness Analysis)
    /// </summary>
    public LivenessAnalysisResult? LivenessResult { get; set; }

    /// <summary>
    /// 특정 변수의 Def-Use 체인 조회
    /// </summary>
    public DefUseChain? GetChainForVariable(string variableName)
        => VariableChains.TryGetValue(variableName, out var chain) ? chain : null;

    /// <summary>
    /// 문제가 있는 변수 목록 반환
    /// </summary>
    public List<string> GetProblematicVariables()
    {
        var problematic = new HashSet<string>();

        foreach (var unused in UnusedVariables)
        {
            problematic.Add(unused.Name);
        }

        foreach (var uninit in UninitializedUsages)
        {
            problematic.Add(uninit.Name);
        }

        return problematic.ToList();
    }

    /// <summary>
    /// 분석 요약 생성
    /// </summary>
    public string GetSummary()
    {
        return $@"데이터 흐름 분석 결과:
- 분석 파일: {FilePath}
- 분석 시간: {AnalysisTimeMs}ms
- 총 변수 수: {Statistics.TotalVariables}
- 사용되지 않는 변수: {UnusedVariables.Count}개
- 초기화되지 않은 사용: {UninitializedUsages.Count}개
- 도달 불가능한 블록: {UnreachableBlocks.Count}개
- 감지된 루프: {Loops.Count}개
- CFG 블록 수: {CFG.Blocks.Count}개";
    }
}

/// <summary>
/// 데이터 흐름 분석 통계
/// </summary>
public class DataFlowStatistics
{
    /// <summary>
    /// 총 변수 수
    /// </summary>
    public int TotalVariables { get; set; }

    /// <summary>
    /// Input 변수 수
    /// </summary>
    public int InputVariables { get; set; }

    /// <summary>
    /// Output 변수 수
    /// </summary>
    public int OutputVariables { get; set; }

    /// <summary>
    /// 로컬 변수 수
    /// </summary>
    public int LocalVariables { get; set; }

    /// <summary>
    /// 전역 변수 사용 수
    /// </summary>
    public int GlobalVariableUsages { get; set; }

    /// <summary>
    /// 총 할당 횟수
    /// </summary>
    public int TotalAssignments { get; set; }

    /// <summary>
    /// 총 변수 참조 횟수
    /// </summary>
    public int TotalReferences { get; set; }

    /// <summary>
    /// 조건부 할당 횟수
    /// </summary>
    public int ConditionalAssignments { get; set; }

    /// <summary>
    /// 루프 내 할당 횟수
    /// </summary>
    public int LoopAssignments { get; set; }

    /// <summary>
    /// 평균 변수 사용 빈도
    /// </summary>
    public double AverageUsageFrequency => TotalVariables > 0
        ? (double)TotalReferences / TotalVariables
        : 0.0;

    /// <summary>
    /// 할당 대비 참조 비율
    /// </summary>
    public double AssignmentToReferenceRatio => TotalAssignments > 0
        ? (double)TotalReferences / TotalAssignments
        : 0.0;
}

/// <summary>
/// 활성 변수 분석 결과 (Liveness Analysis)
/// </summary>
public class LivenessAnalysisResult
{
    /// <summary>
    /// 각 블록의 Live-in 집합
    /// Key: Block ID, Value: 활성 변수 집합
    /// </summary>
    public Dictionary<int, HashSet<string>> LiveInSets { get; init; } = new();

    /// <summary>
    /// 각 블록의 Live-out 집합
    /// Key: Block ID, Value: 활성 변수 집합
    /// </summary>
    public Dictionary<int, HashSet<string>> LiveOutSets { get; init; } = new();

    /// <summary>
    /// 특정 위치에서 활성 변수 조회
    /// </summary>
    public HashSet<string> GetLiveVariablesAt(int blockId, bool atEntry = true)
    {
        var sets = atEntry ? LiveInSets : LiveOutSets;
        return sets.TryGetValue(blockId, out var liveVars)
            ? liveVars
            : new HashSet<string>();
    }

    /// <summary>
    /// 전체 분석에서 한 번이라도 활성이었던 변수들
    /// </summary>
    public HashSet<string> GetAllLiveVariables()
    {
        var allLive = new HashSet<string>();

        foreach (var liveSet in LiveInSets.Values)
        {
            allLive.UnionWith(liveSet);
        }

        foreach (var liveSet in LiveOutSets.Values)
        {
            allLive.UnionWith(liveSet);
        }

        return allLive;
    }

    /// <summary>
    /// 활성 변수 범위 (Live Range) 계산
    /// 변수가 활성인 블록들의 범위를 반환합니다.
    /// </summary>
    public Dictionary<string, List<int>> CalculateLiveRanges()
    {
        var liveRanges = new Dictionary<string, List<int>>();

        foreach (var (blockId, liveVars) in LiveInSets)
        {
            foreach (var variable in liveVars)
            {
                if (!liveRanges.ContainsKey(variable))
                {
                    liveRanges[variable] = new List<int>();
                }
                if (!liveRanges[variable].Contains(blockId))
                {
                    liveRanges[variable].Add(blockId);
                }
            }
        }

        foreach (var (blockId, liveVars) in LiveOutSets)
        {
            foreach (var variable in liveVars)
            {
                if (!liveRanges.ContainsKey(variable))
                {
                    liveRanges[variable] = new List<int>();
                }
                if (!liveRanges[variable].Contains(blockId))
                {
                    liveRanges[variable].Add(blockId);
                }
            }
        }

        // 블록 ID 정렬
        foreach (var variable in liveRanges.Keys)
        {
            liveRanges[variable].Sort();
        }

        return liveRanges;
    }
}

/// <summary>
/// 도달 정의 분석 결과 (Reaching Definitions)
/// </summary>
public class ReachingDefinitionsResult
{
    /// <summary>
    /// 각 블록의 진입 시점의 도달 정의 집합
    /// Key: Block ID, Value: 도달 가능한 정의들
    /// </summary>
    public Dictionary<int, HashSet<DefinitionPoint>> ReachingIn { get; init; } = new();

    /// <summary>
    /// 각 블록의 종료 시점의 도달 정의 집합
    /// Key: Block ID, Value: 도달 가능한 정의들
    /// </summary>
    public Dictionary<int, HashSet<DefinitionPoint>> ReachingOut { get; init; } = new();

    /// <summary>
    /// 특정 위치에서 특정 변수의 도달 정의들 조회
    /// </summary>
    public List<DefinitionPoint> GetReachingDefinitions(int blockId, string variableName, bool atEntry = true)
    {
        var definitions = atEntry ? ReachingIn : ReachingOut;

        if (!definitions.TryGetValue(blockId, out var defs))
        {
            return new List<DefinitionPoint>();
        }

        return defs.Where(d => d.VariableName == variableName).ToList();
    }
}

/// <summary>
/// 정의 지점 (Definition Point)
/// </summary>
public class DefinitionPoint
{
    /// <summary>
    /// 변수명
    /// </summary>
    public string VariableName { get; init; } = string.Empty;

    /// <summary>
    /// 정의가 발생한 블록 ID
    /// </summary>
    public int BlockId { get; init; }

    /// <summary>
    /// 정의 위치 정보
    /// </summary>
    public DefinitionSite Site { get; init; } = null!;

    public override bool Equals(object? obj)
    {
        if (obj is not DefinitionPoint other)
            return false;

        return VariableName == other.VariableName &&
               BlockId == other.BlockId &&
               Site.Line == other.Site.Line &&
               Site.Column == other.Site.Column;
    }

    public override int GetHashCode()
        => HashCode.Combine(VariableName, BlockId, Site.Line, Site.Column);

    public override string ToString()
        => $"{VariableName} at Block {BlockId} ({Site})";
}
