using TwinCatQA.Domain.Models;

namespace TwinCatQA.Domain.Services;

/// <summary>
/// 메모리 영역 분석기 인터페이스
/// </summary>
public interface IMemoryRegionAnalyzer
{
    /// <summary>
    /// 메모리 영역 분석 수행
    /// </summary>
    /// <param name="session">검증 세션</param>
    /// <returns>메모리 영역 분석 결과</returns>
    MemoryRegionAnalysis Analyze(ValidationSession session);
}

/// <summary>
/// 순환 복잡도 분석기 인터페이스
/// </summary>
public interface ICyclomaticComplexityAnalyzer
{
    /// <summary>
    /// 순환 복잡도 분석 수행
    /// </summary>
    /// <param name="session">검증 세션</param>
    /// <returns>POU별 복잡도 결과</returns>
    List<CyclomaticComplexityResult> Analyze(ValidationSession session);
}

/// <summary>
/// 데드 코드 검출기 인터페이스
/// </summary>
public interface IDeadCodeDetector
{
    /// <summary>
    /// 데드 코드 검출 수행
    /// </summary>
    /// <param name="session">검증 세션</param>
    /// <returns>데드 코드 검출 결과</returns>
    List<DeadCodeResult> Detect(ValidationSession session);
}

/// <summary>
/// 배열 경계 검사기 인터페이스
/// </summary>
public interface IArrayBoundsChecker
{
    /// <summary>
    /// 배열 경계 검사 수행
    /// </summary>
    /// <param name="session">검증 세션</param>
    /// <returns>배열 경계 위반 결과</returns>
    List<ArrayBoundsResult> Check(ValidationSession session);
}

/// <summary>
/// 매직 넘버 검출기 인터페이스
/// </summary>
public interface IMagicNumberDetector
{
    /// <summary>
    /// 매직 넘버 검출 수행
    /// </summary>
    /// <param name="session">검증 세션</param>
    /// <returns>매직 넘버 검출 결과</returns>
    List<MagicNumberResult> Detect(ValidationSession session);
}

/// <summary>
/// 타이밍 분석기 인터페이스
/// </summary>
public interface ITimingAnalyzer
{
    /// <summary>
    /// 타이밍 분석 수행 (무한 루프, 타이머 중첩 등)
    /// </summary>
    /// <param name="session">검증 세션</param>
    /// <returns>타이밍 분석 결과</returns>
    List<TimingAnalysisResult> Analyze(ValidationSession session);
}

/// <summary>
/// 심화 분석 통합 인터페이스
/// </summary>
public interface IAdvancedAnalyzerOrchestrator
{
    /// <summary>
    /// 모든 심화 분석 수행
    /// </summary>
    /// <param name="session">검증 세션</param>
    /// <returns>심화 분석 종합 결과</returns>
    AdvancedAnalysisResult AnalyzeAll(ValidationSession session);
}
