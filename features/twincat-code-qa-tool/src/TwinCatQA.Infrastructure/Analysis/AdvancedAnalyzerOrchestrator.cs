using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Services;

namespace TwinCatQA.Infrastructure.Analysis;

/// <summary>
/// 심화 분석 통합 오케스트레이터
/// 모든 심화 분석기를 조율하여 종합 결과 생성
/// </summary>
public class AdvancedAnalyzerOrchestrator : IAdvancedAnalyzerOrchestrator
{
    private readonly IMemoryRegionAnalyzer _memoryAnalyzer;
    private readonly ICyclomaticComplexityAnalyzer _complexityAnalyzer;
    private readonly IDeadCodeDetector _deadCodeDetector;
    private readonly IArrayBoundsChecker _arrayBoundsChecker;
    private readonly IMagicNumberDetector _magicNumberDetector;

    public AdvancedAnalyzerOrchestrator(
        IMemoryRegionAnalyzer memoryAnalyzer,
        ICyclomaticComplexityAnalyzer complexityAnalyzer,
        IDeadCodeDetector deadCodeDetector,
        IArrayBoundsChecker arrayBoundsChecker,
        IMagicNumberDetector magicNumberDetector)
    {
        _memoryAnalyzer = memoryAnalyzer;
        _complexityAnalyzer = complexityAnalyzer;
        _deadCodeDetector = deadCodeDetector;
        _arrayBoundsChecker = arrayBoundsChecker;
        _magicNumberDetector = magicNumberDetector;
    }

    /// <summary>
    /// 모든 심화 분석 병렬 수행
    /// </summary>
    public AdvancedAnalysisResult AnalyzeAll(ValidationSession session)
    {
        // 병렬 실행
        var memoryTask = Task.Run(() => _memoryAnalyzer.Analyze(session));
        var complexityTask = Task.Run(() => _complexityAnalyzer.Analyze(session));
        var deadCodeTask = Task.Run(() => _deadCodeDetector.Detect(session));
        var arrayBoundsTask = Task.Run(() => _arrayBoundsChecker.Check(session));
        var magicNumberTask = Task.Run(() => _magicNumberDetector.Detect(session));

        Task.WaitAll(memoryTask, complexityTask, deadCodeTask, arrayBoundsTask, magicNumberTask);

        return new AdvancedAnalysisResult
        {
            MemoryAnalysis = memoryTask.Result,
            ComplexityResults = complexityTask.Result,
            DeadCodeResults = deadCodeTask.Result,
            ArrayBoundsResults = arrayBoundsTask.Result,
            MagicNumberResults = magicNumberTask.Result
        };
    }
}

/// <summary>
/// 순환 복잡도 분석기 구현
/// </summary>
public class CyclomaticComplexityAnalyzer : ICyclomaticComplexityAnalyzer
{
    public List<CyclomaticComplexityResult> Analyze(ValidationSession session)
    {
        var results = new List<CyclomaticComplexityResult>();

        foreach (var file in session.Files)
        {
            // 각 POU별 복잡도 계산
            foreach (var fb in file.FunctionBlocks)
            {
                var complexity = CalculateComplexity(fb.Body);

                results.Add(new CyclomaticComplexityResult
                {
                    PouName = fb.Name,
                    FilePath = file.FilePath,
                    Complexity = complexity.Total,
                    IfCount = complexity.IfCount,
                    CaseCount = complexity.CaseCount,
                    LoopCount = complexity.LoopCount,
                    LogicalOperatorCount = complexity.LogicalCount
                });
            }
        }

        return results.OrderByDescending(r => r.Complexity).ToList();
    }

    /// <summary>
    /// 복잡도 계산
    /// </summary>
    private (int Total, int IfCount, int CaseCount, int LoopCount, int LogicalCount) CalculateComplexity(string code)
    {
        if (string.IsNullOrEmpty(code))
            return (1, 0, 0, 0, 0);

        int ifCount = System.Text.RegularExpressions.Regex.Matches(
            code, @"\bIF\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Count;

        int elsifCount = System.Text.RegularExpressions.Regex.Matches(
            code, @"\bELSIF\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Count;

        int caseCount = System.Text.RegularExpressions.Regex.Matches(
            code, @"^\s*\d+\s*:", System.Text.RegularExpressions.RegexOptions.Multiline).Count;

        int forCount = System.Text.RegularExpressions.Regex.Matches(
            code, @"\bFOR\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Count;

        int whileCount = System.Text.RegularExpressions.Regex.Matches(
            code, @"\bWHILE\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Count;

        int repeatCount = System.Text.RegularExpressions.Regex.Matches(
            code, @"\bREPEAT\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Count;

        int andCount = System.Text.RegularExpressions.Regex.Matches(
            code, @"\bAND\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Count;

        int orCount = System.Text.RegularExpressions.Regex.Matches(
            code, @"\bOR\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Count;

        int loopCount = forCount + whileCount + repeatCount;
        int logicalCount = andCount + orCount;
        int total = 1 + ifCount + elsifCount + caseCount + loopCount + logicalCount;

        return (total, ifCount + elsifCount, caseCount, loopCount, logicalCount);
    }
}
