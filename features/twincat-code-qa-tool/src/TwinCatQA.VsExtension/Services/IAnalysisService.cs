using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Models.QA;

namespace TwinCatQA.VsExtension.Services;

/// <summary>
/// 코드 분석 서비스 인터페이스
/// TcPOU 파일에 대한 비동기 분석 기능 제공
/// </summary>
public interface IAnalysisService
{
    #region 문서 분석

    /// <summary>
    /// 단일 문서 전체 분석
    /// </summary>
    /// <param name="filePath">분석할 파일 경로</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>검증 위반 사항 목록</returns>
    Task<List<Violation>> AnalyzeDocumentAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 문서 내용으로 직접 분석 (저장되지 않은 변경사항 분석)
    /// </summary>
    /// <param name="filePath">파일 경로 (위치 정보용)</param>
    /// <param name="content">분석할 내용</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>검증 위반 사항 목록</returns>
    Task<List<Violation>> AnalyzeContentAsync(
        string filePath,
        string content,
        CancellationToken cancellationToken = default);

    #endregion

    #region 증분 분석

    /// <summary>
    /// 변경된 라인만 증분 분석
    /// </summary>
    /// <param name="filePath">파일 경로</param>
    /// <param name="changedLines">변경된 라인 번호 목록</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>변경된 부분에 대한 위반 사항</returns>
    Task<List<Violation>> AnalyzeIncrementalAsync(
        string filePath,
        IEnumerable<int> changedLines,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 특정 텍스트 범위만 분석
    /// </summary>
    /// <param name="filePath">파일 경로</param>
    /// <param name="startLine">시작 라인</param>
    /// <param name="endLine">종료 라인</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>해당 범위의 위반 사항</returns>
    Task<List<Violation>> AnalyzeRangeAsync(
        string filePath,
        int startLine,
        int endLine,
        CancellationToken cancellationToken = default);

    #endregion

    #region 프로젝트 분석

    /// <summary>
    /// 전체 프로젝트 분석 (병렬 처리)
    /// </summary>
    /// <param name="projectPath">프로젝트 루트 경로</param>
    /// <param name="progress">진행률 리포트 콜백</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>프로젝트 전체 위반 사항</returns>
    Task<Dictionary<string, List<Violation>>> AnalyzeProjectAsync(
        string projectPath,
        IProgress<AnalysisProgress>? progress = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region QA 이슈 분석

    /// <summary>
    /// QA 규칙 엔진을 사용한 고급 분석
    /// </summary>
    /// <param name="filePath">파일 경로</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>QA 이슈 목록</returns>
    Task<List<QAIssue>> AnalyzeWithQARulesAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    #endregion

    #region 캐시 관리

    /// <summary>
    /// 분석 결과 캐시 무효화
    /// </summary>
    /// <param name="filePath">무효화할 파일 경로 (null이면 전체 캐시)</param>
    void InvalidateCache(string? filePath = null);

    /// <summary>
    /// 캐시된 결과 가져오기
    /// </summary>
    /// <param name="filePath">파일 경로</param>
    /// <returns>캐시된 위반 사항 (없으면 null)</returns>
    List<Violation>? GetCachedResult(string filePath);

    #endregion
}

/// <summary>
/// 분석 진행률 정보
/// </summary>
public class AnalysisProgress
{
    /// <summary>
    /// 전체 파일 수
    /// </summary>
    public int TotalFiles { get; init; }

    /// <summary>
    /// 처리된 파일 수
    /// </summary>
    public int ProcessedFiles { get; init; }

    /// <summary>
    /// 현재 처리 중인 파일
    /// </summary>
    public string CurrentFile { get; init; } = string.Empty;

    /// <summary>
    /// 진행률 (0-100)
    /// </summary>
    public int Percentage => TotalFiles > 0
        ? (int)((double)ProcessedFiles / TotalFiles * 100)
        : 0;

    /// <summary>
    /// 발견된 총 위반 사항 수
    /// </summary>
    public int TotalViolations { get; init; }
}
