using System.Threading;
using System.Threading.Tasks;
using TwinCatQA.Domain.Models;

namespace TwinCatQA.Domain.Services;

/// <summary>
/// 고급 분석 오케스트레이터 인터페이스
///
/// 4가지 고급 분석 기능을 통합 실행합니다:
/// 1. TwinCAT API 기반 컴파일
/// 2. ANTLR AST 기반 변수 사용 분석
/// 3. 의존성 그래프 분석
/// 4. I/O 매핑 검증
/// </summary>
public interface IAdvancedAnalysisOrchestrator
{
    /// <summary>
    /// 전체 고급 분석 실행
    /// </summary>
    /// <param name="projectPath">TwinCAT 프로젝트 경로</param>
    /// <param name="session">검증 세션 (파싱된 구문 트리 포함)</param>
    /// <param name="options">분석 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>통합 분석 결과</returns>
    Task<ComprehensiveAnalysisResult> AnalyzeProjectAsync(
        string projectPath,
        ValidationSession session,
        AdvancedAnalysisOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 컴파일 분석만 실행
    /// </summary>
    Task<CompilationResult> RunCompilationAnalysisAsync(
        string projectPath,
        string buildConfiguration = "Debug",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 변수 사용 분석만 실행
    /// </summary>
    Task<VariableUsageAnalysis> RunVariableUsageAnalysisAsync(
        ValidationSession session,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 의존성 분석만 실행
    /// </summary>
    Task<DependencyAnalysis> RunDependencyAnalysisAsync(
        ValidationSession session,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// I/O 매핑 검증만 실행
    /// </summary>
    Task<IOMappingValidationResult> RunIOMappingValidationAsync(
        string projectPath,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 고급 분석 옵션
/// </summary>
public class AdvancedAnalysisOptions
{
    /// <summary>
    /// 컴파일 분석 실행 여부
    /// </summary>
    public bool RunCompilation { get; set; } = true;

    /// <summary>
    /// 변수 사용 분석 실행 여부
    /// </summary>
    public bool RunVariableAnalysis { get; set; } = true;

    /// <summary>
    /// 의존성 분석 실행 여부
    /// </summary>
    public bool RunDependencyAnalysis { get; set; } = true;

    /// <summary>
    /// I/O 매핑 검증 실행 여부
    /// </summary>
    public bool RunIOMappingValidation { get; set; } = true;

    /// <summary>
    /// 빌드 구성 (Debug/Release)
    /// </summary>
    public string BuildConfiguration { get; set; } = "Debug";

    /// <summary>
    /// 병렬 실행 활성화
    /// </summary>
    /// <remarks>
    /// 컴파일과 I/O 매핑은 파일 시스템 접근,
    /// 변수/의존성 분석은 메모리 기반 AST 분석으로 병렬 실행 가능
    /// </remarks>
    public bool EnableParallelExecution { get; set; } = true;

    /// <summary>
    /// 오류 발생 시 계속 진행 여부
    /// </summary>
    /// <remarks>
    /// true: 한 분석이 실패해도 나머지 분석 계속 진행
    /// false: 첫 번째 오류 발생 시 즉시 중단
    /// </remarks>
    public bool ContinueOnError { get; set; } = true;
}
