using TwinCatQA.Domain.Models;

namespace TwinCatQA.Domain.Services;

/// <summary>
/// I/O 매핑 검증 서비스 인터페이스
/// </summary>
public interface IIOMappingValidator
{
    /// <summary>
    /// I/O 매핑 검증
    /// </summary>
    /// <param name="projectPath">프로젝트 파일 경로</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>I/O 매핑 검증 결과</returns>
    Task<IOMappingValidationResult> ValidateIOMappingAsync(
        string projectPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// I/O 디바이스 목록 조회
    /// </summary>
    /// <param name="projectPath">프로젝트 파일 경로</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>I/O 디바이스 목록</returns>
    Task<List<IODevice>> GetIODevicesAsync(
        string projectPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// EtherCAT 설정 검증
    /// </summary>
    /// <param name="projectPath">프로젝트 파일 경로</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>EtherCAT 마스터 정보</returns>
    Task<EtherCATMaster?> ValidateEtherCATConfigurationAsync(
        string projectPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 사용되지 않는 I/O 탐지
    /// </summary>
    /// <param name="projectPath">프로젝트 파일 경로</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>사용되지 않는 I/O 매핑 목록</returns>
    Task<List<IOMapping>> FindUnusedIOMappingsAsync(
        string projectPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// I/O 매핑 충돌 검사
    /// </summary>
    /// <param name="projectPath">프로젝트 파일 경로</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>I/O 매핑 오류 목록</returns>
    Task<List<IOMappingError>> CheckIOMappingConflictsAsync(
        string projectPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 통신 사이클 타임 검증
    /// </summary>
    /// <param name="projectPath">프로젝트 파일 경로</param>
    /// <param name="recommendedCycleTimeMicroseconds">권장 사이클 타임 (마이크로초)</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>사이클 타임 검증 경고 목록</returns>
    Task<List<IOMappingWarning>> ValidateCycleTimeAsync(
        string projectPath,
        int recommendedCycleTimeMicroseconds = 4000,
        CancellationToken cancellationToken = default);
}
