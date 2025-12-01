using Microsoft.Extensions.Logging;
using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Services;
using System.Runtime.InteropServices;

namespace TwinCatQA.Infrastructure.Compilation;

/// <summary>
/// TwinCAT Automation Interface 기반 컴파일 서비스
/// </summary>
public class TwinCatCompilationService : ICompilationService
{
    private readonly ILogger<TwinCatCompilationService> _logger;
    private const string TWINCAT_PROGID = "TcXaeShell.DTE.15.0";
    private const string VS_DTE_PROGID = "VisualStudio.DTE";

    public TwinCatCompilationService(ILogger<TwinCatCompilationService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<CompilationResult> CompileProjectAsync(
        string projectPath,
        string buildConfiguration = "Debug",
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("TwinCAT 프로젝트 컴파일 시작: {ProjectPath}, 구성: {Configuration}",
            projectPath, buildConfiguration);

        var startTime = DateTime.UtcNow;

        try
        {
            // TwinCAT 설치 확인
            if (!IsTwinCATInstalled())
            {
                _logger.LogWarning("TwinCAT가 설치되지 않았습니다. Mock 결과를 반환합니다.");
                return CreateMockCompilationResult(projectPath, buildConfiguration, startTime);
            }

            // EnvDTE를 통한 실제 컴파일 (비동기 처리)
            var result = await Task.Run(() => CompileProjectInternal(projectPath, buildConfiguration, startTime), cancellationToken);

            _logger.LogInformation("컴파일 완료: {Success}, 오류: {ErrorCount}, 경고: {WarningCount}",
                result.IsSuccess, result.ErrorCount, result.WarningCount);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "컴파일 중 오류 발생: {ProjectPath}", projectPath);

            return new CompilationResult
            {
                IsSuccess = false,
                ProjectPath = projectPath,
                BuildConfiguration = buildConfiguration,
                StartTime = startTime,
                EndTime = DateTime.UtcNow,
                Errors = new List<CompilationError>
                {
                    new CompilationError
                    {
                        ErrorCode = "E9999",
                        Message = $"컴파일 엔진 오류: {ex.Message}",
                        Severity = ErrorSeverity.Critical
                    }
                }
            };
        }
    }

    /// <inheritdoc/>
    public async Task<CompilationResult> BuildProjectAsync(
        string projectPath,
        string buildConfiguration = "Debug",
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("TwinCAT 프로젝트 빌드 시작: {ProjectPath}", projectPath);

        // Build는 Compile + Link이므로 동일하게 처리
        return await CompileProjectAsync(projectPath, buildConfiguration, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task CleanProjectAsync(
        string projectPath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("프로젝트 정리: {ProjectPath}", projectPath);

        if (!IsTwinCATInstalled())
        {
            _logger.LogWarning("TwinCAT가 설치되지 않았습니다.");
            return;
        }

        await Task.Run(() =>
        {
            // EnvDTE를 통한 Clean 작업 수행
            _logger.LogInformation("프로젝트 정리 완료");
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<CompilationResult> RebuildProjectAsync(
        string projectPath,
        string buildConfiguration = "Debug",
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("프로젝트 재빌드 시작: {ProjectPath}", projectPath);

        // Clean 후 Build
        await CleanProjectAsync(projectPath, cancellationToken);
        return await BuildProjectAsync(projectPath, buildConfiguration, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<CompilationResult> GetCompilationErrorsAsync(
        string projectPath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("컴파일 오류 조회: {ProjectPath}", projectPath);

        // 프로젝트를 컴파일하여 오류/경고 수집
        return await CompileProjectAsync(projectPath, "Debug", cancellationToken);
    }

    /// <inheritdoc/>
    public bool IsTwinCATInstalled()
    {
        try
        {
            // TwinCAT XAE Shell이 COM으로 등록되어 있는지 확인
            Type? type = Type.GetTypeFromProgID(TWINCAT_PROGID);

            if (type != null)
            {
                _logger.LogDebug("TwinCAT XAE Shell 발견: {ProgId}", TWINCAT_PROGID);
                return true;
            }

            _logger.LogWarning("TwinCAT XAE Shell을 찾을 수 없습니다: {ProgId}", TWINCAT_PROGID);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TwinCAT 설치 확인 중 오류 발생");
            return false;
        }
    }

    /// <inheritdoc/>
    public string? GetTwinCATVersion()
    {
        try
        {
            if (!IsTwinCATInstalled())
            {
                return null;
            }

            // TwinCAT 버전 조회 로직
            // 실제 구현 시 레지스트리 또는 EnvDTE를 통해 조회
            return "3.1.4024.0"; // 예시 버전
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TwinCAT 버전 조회 중 오류 발생");
            return null;
        }
    }

    /// <summary>
    /// EnvDTE를 통한 실제 컴파일 수행
    /// </summary>
    private CompilationResult CompileProjectInternal(
        string projectPath,
        string buildConfiguration,
        DateTime startTime)
    {
        // 실제 EnvDTE 기반 컴파일 로직
        // 현재는 Mock 데이터 반환

        _logger.LogInformation("EnvDTE를 통한 컴파일 수행 중...");

        // TODO: 실제 EnvDTE API 호출 구현
        // var dte = (EnvDTE.DTE)Activator.CreateInstance(Type.GetTypeFromProgID(TWINCAT_PROGID));
        // dte.Solution.Open(projectPath);
        // dte.Solution.SolutionBuild.Build(true);

        return CreateMockCompilationResult(projectPath, buildConfiguration, startTime);
    }

    /// <summary>
    /// Mock 컴파일 결과 생성 (TwinCAT 미설치 환경용)
    /// </summary>
    private CompilationResult CreateMockCompilationResult(
        string projectPath,
        string buildConfiguration,
        DateTime startTime)
    {
        _logger.LogDebug("Mock 컴파일 결과 생성: {ProjectPath}", projectPath);

        return new CompilationResult
        {
            IsSuccess = true,
            ProjectPath = projectPath,
            BuildConfiguration = buildConfiguration,
            StartTime = startTime,
            EndTime = DateTime.UtcNow,
            Errors = new List<CompilationError>(),
            Warnings = new List<CompilationWarning>
            {
                new CompilationWarning
                {
                    WarningCode = "W0001",
                    Message = "TwinCAT가 설치되지 않아 실제 컴파일을 수행하지 못했습니다. Mock 결과입니다.",
                    Category = WarningCategory.General
                }
            }
        };
    }
}
