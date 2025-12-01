using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Services;

namespace TwinCatQA.Application.Services;

/// <summary>
/// 고급 분석 오케스트레이터 구현
///
/// 4가지 고급 분석 기능을 통합 실행하고 결과를 종합합니다.
/// 병렬 실행 및 오류 처리를 통해 효율성과 안정성을 보장합니다.
/// </summary>
public class AdvancedAnalysisOrchestrator : IAdvancedAnalysisOrchestrator
{
    private readonly ICompilationService _compilationService;
    private readonly IVariableUsageAnalyzer _variableAnalyzer;
    private readonly IDependencyAnalyzer _dependencyAnalyzer;
    private readonly IIOMappingValidator _ioMappingValidator;
    private readonly ILogger<AdvancedAnalysisOrchestrator> _logger;

    public AdvancedAnalysisOrchestrator(
        ICompilationService compilationService,
        IVariableUsageAnalyzer variableAnalyzer,
        IDependencyAnalyzer dependencyAnalyzer,
        IIOMappingValidator ioMappingValidator,
        ILogger<AdvancedAnalysisOrchestrator> logger)
    {
        _compilationService = compilationService ?? throw new ArgumentNullException(nameof(compilationService));
        _variableAnalyzer = variableAnalyzer ?? throw new ArgumentNullException(nameof(variableAnalyzer));
        _dependencyAnalyzer = dependencyAnalyzer ?? throw new ArgumentNullException(nameof(dependencyAnalyzer));
        _ioMappingValidator = ioMappingValidator ?? throw new ArgumentNullException(nameof(ioMappingValidator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 전체 고급 분석 실행
    /// </summary>
    public async Task<ComprehensiveAnalysisResult> AnalyzeProjectAsync(
        string projectPath,
        ValidationSession session,
        AdvancedAnalysisOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // 기본 옵션 설정
        options ??= new AdvancedAnalysisOptions();

        _logger.LogInformation($"고급 분석 시작: {projectPath}");

        var result = new ComprehensiveAnalysisResult
        {
            ProjectPath = projectPath,
            ProjectName = Path.GetFileName(projectPath),
            StartTime = DateTime.UtcNow
        };

        try
        {
            if (options.EnableParallelExecution)
            {
                // 병렬 실행: 파일 시스템 기반 분석 vs 메모리 기반 분석
                await RunAnalysesInParallelAsync(projectPath, session, options, result, cancellationToken);
            }
            else
            {
                // 순차 실행
                await RunAnalysesSequentiallyAsync(projectPath, session, options, result, cancellationToken);
            }

            result.Complete();
            _logger.LogInformation($"고급 분석 완료. 총 소요 시간: {result.Duration.TotalSeconds:F2}초");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "고급 분석 중 예외 발생");
            result.Complete();
            throw;
        }
    }

    /// <summary>
    /// 병렬 분석 실행
    /// </summary>
    /// <remarks>
    /// Group 1 (파일 시스템): 컴파일 + I/O 매핑 (병렬)
    /// Group 2 (메모리 AST): 변수 분석 + 의존성 분석 (병렬)
    /// 두 그룹은 순차 실행 (파일 시스템 충돌 방지)
    /// </remarks>
    private async Task RunAnalysesInParallelAsync(
        string projectPath,
        ValidationSession session,
        AdvancedAnalysisOptions options,
        ComprehensiveAnalysisResult result,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("병렬 분석 모드 활성화");

        // Group 1: 파일 시스템 기반 분석 (병렬 실행)
        var fileSystemTasks = new List<Task>();

        if (options.RunCompilation)
        {
            fileSystemTasks.Add(Task.Run(async () =>
            {
                try
                {
                    result.Compilation = await RunCompilationAnalysisAsync(
                        projectPath,
                        options.BuildConfiguration,
                        cancellationToken);
                }
                catch (Exception ex) when (options.ContinueOnError)
                {
                    _logger.LogWarning(ex, "컴파일 분석 실패 (계속 진행)");
                }
            }, cancellationToken));
        }

        if (options.RunIOMappingValidation)
        {
            fileSystemTasks.Add(Task.Run(async () =>
            {
                try
                {
                    result.IOMapping = await RunIOMappingValidationAsync(
                        projectPath,
                        cancellationToken);
                }
                catch (Exception ex) when (options.ContinueOnError)
                {
                    _logger.LogWarning(ex, "I/O 매핑 검증 실패 (계속 진행)");
                }
            }, cancellationToken));
        }

        await Task.WhenAll(fileSystemTasks);

        // Group 2: 메모리 기반 AST 분석 (병렬 실행)
        var memoryTasks = new List<Task>();

        if (options.RunVariableAnalysis)
        {
            memoryTasks.Add(Task.Run(async () =>
            {
                try
                {
                    result.VariableUsage = await RunVariableUsageAnalysisAsync(
                        session,
                        cancellationToken);
                }
                catch (Exception ex) when (options.ContinueOnError)
                {
                    _logger.LogWarning(ex, "변수 사용 분석 실패 (계속 진행)");
                }
            }, cancellationToken));
        }

        if (options.RunDependencyAnalysis)
        {
            memoryTasks.Add(Task.Run(async () =>
            {
                try
                {
                    result.Dependencies = await RunDependencyAnalysisAsync(
                        session,
                        cancellationToken);
                }
                catch (Exception ex) when (options.ContinueOnError)
                {
                    _logger.LogWarning(ex, "의존성 분석 실패 (계속 진행)");
                }
            }, cancellationToken));
        }

        await Task.WhenAll(memoryTasks);
    }

    /// <summary>
    /// 순차 분석 실행
    /// </summary>
    private async Task RunAnalysesSequentiallyAsync(
        string projectPath,
        ValidationSession session,
        AdvancedAnalysisOptions options,
        ComprehensiveAnalysisResult result,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("순차 분석 모드 활성화");

        // 1. 컴파일 분석
        if (options.RunCompilation)
        {
            try
            {
                result.Compilation = await RunCompilationAnalysisAsync(
                    projectPath,
                    options.BuildConfiguration,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "컴파일 분석 실패");
                if (!options.ContinueOnError) throw;
            }
        }

        // 2. 변수 사용 분석
        if (options.RunVariableAnalysis)
        {
            try
            {
                result.VariableUsage = await RunVariableUsageAnalysisAsync(
                    session,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "변수 사용 분석 실패");
                if (!options.ContinueOnError) throw;
            }
        }

        // 3. 의존성 분석
        if (options.RunDependencyAnalysis)
        {
            try
            {
                result.Dependencies = await RunDependencyAnalysisAsync(
                    session,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "의존성 분석 실패");
                if (!options.ContinueOnError) throw;
            }
        }

        // 4. I/O 매핑 검증
        if (options.RunIOMappingValidation)
        {
            try
            {
                result.IOMapping = await RunIOMappingValidationAsync(
                    projectPath,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "I/O 매핑 검증 실패");
                if (!options.ContinueOnError) throw;
            }
        }
    }

    /// <summary>
    /// 컴파일 분석 실행
    /// </summary>
    public async Task<CompilationResult> RunCompilationAnalysisAsync(
        string projectPath,
        string buildConfiguration = "Debug",
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation($"컴파일 분석 시작: {projectPath} ({buildConfiguration})");

        var startTime = DateTime.UtcNow;
        var result = await _compilationService.CompileProjectAsync(projectPath, buildConfiguration, cancellationToken);

        _logger.LogInformation($"컴파일 분석 완료. 오류: {result.ErrorCount}, 경고: {result.WarningCount}, 소요시간: {(DateTime.UtcNow - startTime).TotalSeconds:F2}초");

        return result;
    }

    /// <summary>
    /// 변수 사용 분석 실행
    /// </summary>
    public async Task<VariableUsageAnalysis> RunVariableUsageAnalysisAsync(
        ValidationSession session,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("변수 사용 분석 시작");

        var startTime = DateTime.UtcNow;
        var result = await _variableAnalyzer.AnalyzeVariableUsageAsync(session, cancellationToken);

        _logger.LogInformation($"변수 사용 분석 완료. 총 이슈: {result.TotalIssues}, 소요시간: {(DateTime.UtcNow - startTime).TotalSeconds:F2}초");

        return result;
    }

    /// <summary>
    /// 의존성 분석 실행
    /// </summary>
    public async Task<DependencyAnalysis> RunDependencyAnalysisAsync(
        ValidationSession session,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("의존성 분석 시작");

        var startTime = DateTime.UtcNow;
        var result = await _dependencyAnalyzer.AnalyzeDependenciesAsync(session, cancellationToken);

        _logger.LogInformation($"의존성 분석 완료. 순환 참조: {result.CircularReferences.Count}, 소요시간: {(DateTime.UtcNow - startTime).TotalSeconds:F2}초");

        return result;
    }

    /// <summary>
    /// I/O 매핑 검증 실행
    /// </summary>
    public async Task<IOMappingValidationResult> RunIOMappingValidationAsync(
        string projectPath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation($"I/O 매핑 검증 시작: {projectPath}");

        var startTime = DateTime.UtcNow;
        var result = await _ioMappingValidator.ValidateIOMappingAsync(projectPath, cancellationToken);

        _logger.LogInformation($"I/O 매핑 검증 완료. 디바이스: {result.Devices.Count}, 오류: {result.Errors.Count}, 소요시간: {(DateTime.UtcNow - startTime).TotalSeconds:F2}초");

        return result;
    }
}
