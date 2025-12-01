using TwinCatQA.Domain.Models;

namespace TwinCatQA.Domain.Services;

/// <summary>
/// 의존성 분석 서비스 인터페이스
/// </summary>
public interface IDependencyAnalyzer
{
    /// <summary>
    /// 프로젝트 전체 의존성 분석
    /// </summary>
    /// <param name="session">검증 세션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>의존성 분석 결과</returns>
    Task<DependencyAnalysis> AnalyzeDependenciesAsync(
        ValidationSession session,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 의존성 그래프 구축
    /// </summary>
    /// <param name="session">검증 세션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>의존성 그래프</returns>
    Task<DependencyGraph> BuildDependencyGraphAsync(
        ValidationSession session,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 순환 참조 탐지
    /// </summary>
    /// <param name="graph">의존성 그래프</param>
    /// <returns>순환 참조 목록</returns>
    List<CircularReference> DetectCircularReferences(DependencyGraph graph);

    /// <summary>
    /// 함수 호출 그래프 구축
    /// </summary>
    /// <param name="session">검증 세션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>함수 호출 그래프</returns>
    Task<CallGraph> BuildCallGraphAsync(
        ValidationSession session,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 최대 호출 깊이 계산
    /// </summary>
    /// <param name="callGraph">함수 호출 그래프</param>
    /// <returns>최대 호출 깊이</returns>
    int CalculateMaxCallDepth(CallGraph callGraph);

    /// <summary>
    /// 특정 POU의 의존성 조회
    /// </summary>
    /// <param name="pouName">POU 이름</param>
    /// <param name="graph">의존성 그래프</param>
    /// <returns>의존성 노드 목록</returns>
    List<DependencyNode> GetDependenciesForPou(string pouName, DependencyGraph graph);

    /// <summary>
    /// 특정 POU에 의존하는 POU 목록 조회
    /// </summary>
    /// <param name="pouName">POU 이름</param>
    /// <param name="graph">의존성 그래프</param>
    /// <returns>의존 POU 노드 목록</returns>
    List<DependencyNode> GetDependentsForPou(string pouName, DependencyGraph graph);

    /// <summary>
    /// 의존성 그래프를 DOT 형식으로 내보내기 (Graphviz 시각화용)
    /// </summary>
    /// <param name="graph">의존성 그래프</param>
    /// <returns>DOT 형식 문자열</returns>
    string ExportToDotFormat(DependencyGraph graph);
}
