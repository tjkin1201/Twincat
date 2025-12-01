using TwinCatQA.Domain.Models;

namespace TwinCatQA.Domain.Services;

/// <summary>
/// TwinCAT 프로젝트 컴파일 서비스 인터페이스
/// </summary>
public interface ICompilationService
{
    /// <summary>
    /// TwinCAT 프로젝트 컴파일
    /// </summary>
    /// <param name="projectPath">프로젝트 파일 경로 (.tsproj 또는 .sln)</param>
    /// <param name="buildConfiguration">빌드 구성 (Debug, Release 등)</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>컴파일 결과</returns>
    Task<CompilationResult> CompileProjectAsync(
        string projectPath,
        string buildConfiguration = "Debug",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// TwinCAT 프로젝트 빌드 (컴파일 + 링크)
    /// </summary>
    /// <param name="projectPath">프로젝트 파일 경로</param>
    /// <param name="buildConfiguration">빌드 구성</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>컴파일 결과</returns>
    Task<CompilationResult> BuildProjectAsync(
        string projectPath,
        string buildConfiguration = "Debug",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 프로젝트 정리 (Clean)
    /// </summary>
    /// <param name="projectPath">프로젝트 파일 경로</param>
    /// <param name="cancellationToken">취소 토큰</param>
    Task CleanProjectAsync(
        string projectPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 프로젝트 재빌드 (Clean + Build)
    /// </summary>
    /// <param name="projectPath">프로젝트 파일 경로</param>
    /// <param name="buildConfiguration">빌드 구성</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>컴파일 결과</returns>
    Task<CompilationResult> RebuildProjectAsync(
        string projectPath,
        string buildConfiguration = "Debug",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 컴파일 오류 및 경고 조회
    /// </summary>
    /// <param name="projectPath">프로젝트 파일 경로</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>컴파일 결과 (오류/경고만)</returns>
    Task<CompilationResult> GetCompilationErrorsAsync(
        string projectPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// TwinCAT 설치 여부 확인
    /// </summary>
    /// <returns>TwinCAT 설치 여부</returns>
    bool IsTwinCATInstalled();

    /// <summary>
    /// TwinCAT 버전 조회
    /// </summary>
    /// <returns>TwinCAT 버전 문자열</returns>
    string? GetTwinCATVersion();
}
