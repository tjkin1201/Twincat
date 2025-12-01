using TwinCatQA.Domain.Models;

namespace TwinCatQA.Domain.Services;

/// <summary>
/// 변수 사용 분석 서비스 인터페이스
/// </summary>
public interface IVariableUsageAnalyzer
{
    /// <summary>
    /// 프로젝트의 모든 변수 사용 분석
    /// </summary>
    /// <param name="session">검증 세션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>변수 사용 분석 결과</returns>
    Task<VariableUsageAnalysis> AnalyzeVariableUsageAsync(
        ValidationSession session,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 사용되지 않는 변수 탐지
    /// </summary>
    /// <param name="session">검증 세션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>사용되지 않는 변수 목록</returns>
    Task<List<UnusedVariable>> FindUnusedVariablesAsync(
        ValidationSession session,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 초기화되지 않은 변수 탐지
    /// </summary>
    /// <param name="session">검증 세션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>초기화되지 않은 변수 목록</returns>
    Task<List<UninitializedVariable>> FindUninitializedVariablesAsync(
        ValidationSession session,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Dead Code 탐지
    /// </summary>
    /// <param name="session">검증 세션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>Dead Code 목록</returns>
    Task<List<DeadCode>> FindDeadCodeAsync(
        ValidationSession session,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 특정 POU의 변수 사용 분석
    /// </summary>
    /// <param name="filePath">POU 파일 경로</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>변수 사용 분석 결과</returns>
    Task<VariableUsageAnalysis> AnalyzePouVariableUsageAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 변수 사용 통계 조회
    /// </summary>
    /// <param name="analysis">변수 사용 분석 결과</param>
    /// <returns>변수 사용 통계 (변수명 → 사용 횟수)</returns>
    Dictionary<string, int> GetVariableUsageStatistics(VariableUsageAnalysis analysis);
}
