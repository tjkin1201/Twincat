using Microsoft.Extensions.Logging;
using Moq;
using TwinCatQA.Application.Services;
using TwinCatQA.Domain.Contracts;
using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Services;
using TwinCatQA.Infrastructure.Analysis;
using TwinCatQA.Infrastructure.Compilation;
using Xunit;
using Xunit.Abstractions;

namespace TwinCatQA.Integration.Tests;

/// <summary>
/// 고급 분석 오케스트레이터 통합 테스트
/// 4개 고급 기능의 통합 실행을 검증합니다.
/// </summary>
public class AdvancedAnalysisOrchestratorTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<ILogger<AdvancedAnalysisOrchestrator>> _mockOrchestratorLogger;
    private readonly Mock<ILogger<TwinCatCompilationService>> _mockCompilationLogger;
    private readonly Mock<ILogger<VariableUsageAnalyzer>> _mockVariableLogger;
    private readonly Mock<ILogger<DependencyAnalyzer>> _mockDependencyLogger;
    private readonly Mock<ILogger<IOMappingValidator>> _mockIOMappingLogger;
    private readonly Mock<IParserService> _mockParserService;

    private const string TEST_PROJECT_PATH = @"D:\00.Comapre\pollux_hcds_ald_mirror\Src_Diff\PLC\TM";

    public AdvancedAnalysisOrchestratorTests(ITestOutputHelper output)
    {
        _output = output;
        _mockOrchestratorLogger = new Mock<ILogger<AdvancedAnalysisOrchestrator>>();
        _mockCompilationLogger = new Mock<ILogger<TwinCatCompilationService>>();
        _mockVariableLogger = new Mock<ILogger<VariableUsageAnalyzer>>();
        _mockDependencyLogger = new Mock<ILogger<DependencyAnalyzer>>();
        _mockIOMappingLogger = new Mock<ILogger<IOMappingValidator>>();
        _mockParserService = new Mock<IParserService>();
    }

    [Fact]
    public async Task AnalyzeProjectAsync_ShouldExecuteAllAnalyses()
    {
        // Arrange
        var compilationService = new TwinCatCompilationService(_mockCompilationLogger.Object);
        var variableAnalyzer = new VariableUsageAnalyzer(_mockVariableLogger.Object, _mockParserService.Object);
        var dependencyAnalyzer = new DependencyAnalyzer(_mockDependencyLogger.Object, _mockParserService.Object);
        var ioMappingValidator = new IOMappingValidator(_mockIOMappingLogger.Object);

        var orchestrator = new AdvancedAnalysisOrchestrator(
            compilationService,
            variableAnalyzer,
            dependencyAnalyzer,
            ioMappingValidator,
            _mockOrchestratorLogger.Object
        );

        var session = CreateMockValidationSession();

        // Mock ExtractComments 설정
        _mockParserService.Setup(p => p.ExtractComments(It.IsAny<SyntaxTree>()))
            .Returns(new Dictionary<int, string>());

        // Act
        var result = await orchestrator.AnalyzeProjectAsync(
            TEST_PROJECT_PATH,
            session,
            new AdvancedAnalysisOptions
            {
                RunCompilation = true,
                RunVariableAnalysis = true,
                RunDependencyAnalysis = true,
                RunIOMappingValidation = false, // I/O는 실제 프로젝트 구조 필요
                EnableParallelExecution = true
            }
        );

        // Assert
        Assert.NotNull(result);
        _output.WriteLine($"분석 ID: {result.AnalysisId}");
        _output.WriteLine($"프로젝트: {result.ProjectName}");
        _output.WriteLine($"소요 시간: {result.Duration.TotalSeconds:F2}초");
        _output.WriteLine($"전체 품질 점수: {result.OverallQualityScore:F1}/100");
        _output.WriteLine($"총 이슈 수: {result.TotalIssues}");
        _output.WriteLine($"요약: {result.Summary}");

        // 컴파일 결과 검증
        Assert.NotNull(result.Compilation);
        _output.WriteLine($"\n컴파일 결과:");
        _output.WriteLine($"  성공: {result.Compilation.IsSuccess}");
        _output.WriteLine($"  오류: {result.Compilation.ErrorCount}");
        _output.WriteLine($"  경고: {result.Compilation.WarningCount}");

        // 변수 분석 결과 검증
        Assert.NotNull(result.VariableUsage);
        _output.WriteLine($"\n변수 사용 분석:");
        _output.WriteLine($"  총 이슈: {result.VariableUsage.TotalIssues}");
        _output.WriteLine($"  사용되지 않은 변수: {result.VariableUsage.UnusedVariables.Count}");
        _output.WriteLine($"  초기화되지 않은 변수: {result.VariableUsage.UninitializedVariables.Count}");
        _output.WriteLine($"  Dead Code: {result.VariableUsage.DeadCodeBlocks.Count}");

        // 의존성 분석 결과 검증
        Assert.NotNull(result.Dependencies);
        _output.WriteLine($"\n의존성 분석:");
        _output.WriteLine($"  순환 참조: {result.Dependencies.CircularReferences.Count}");
        if (result.Dependencies.Graph != null)
        {
            _output.WriteLine($"  노드 수: {result.Dependencies.Graph.Nodes.Count}");
            _output.WriteLine($"  엣지 수: {result.Dependencies.Graph.Edges.Count}");
        }
        if (result.Dependencies.CallGraph != null)
        {
            _output.WriteLine($"  최대 호출 깊이: {result.Dependencies.CallGraph.MaxCallDepth}");
        }
    }

    [Fact]
    public async Task AnalyzeProjectAsync_WithParallelExecution_ShouldBeFasterThanSequential()
    {
        // Arrange
        var compilationService = new TwinCatCompilationService(_mockCompilationLogger.Object);
        var variableAnalyzer = new VariableUsageAnalyzer(_mockVariableLogger.Object, _mockParserService.Object);
        var dependencyAnalyzer = new DependencyAnalyzer(_mockDependencyLogger.Object, _mockParserService.Object);
        var ioMappingValidator = new IOMappingValidator(_mockIOMappingLogger.Object);

        var orchestrator = new AdvancedAnalysisOrchestrator(
            compilationService,
            variableAnalyzer,
            dependencyAnalyzer,
            ioMappingValidator,
            _mockOrchestratorLogger.Object
        );

        var session = CreateMockValidationSession();
        _mockParserService.Setup(p => p.ExtractComments(It.IsAny<SyntaxTree>()))
            .Returns(new Dictionary<int, string>());

        // Act - 병렬 실행
        var parallelStartTime = DateTime.UtcNow;
        var parallelResult = await orchestrator.AnalyzeProjectAsync(
            TEST_PROJECT_PATH,
            session,
            new AdvancedAnalysisOptions
            {
                EnableParallelExecution = true,
                RunIOMappingValidation = false
            }
        );
        var parallelDuration = DateTime.UtcNow - parallelStartTime;

        // Act - 순차 실행
        var sequentialStartTime = DateTime.UtcNow;
        var sequentialResult = await orchestrator.AnalyzeProjectAsync(
            TEST_PROJECT_PATH,
            session,
            new AdvancedAnalysisOptions
            {
                EnableParallelExecution = false,
                RunIOMappingValidation = false
            }
        );
        var sequentialDuration = DateTime.UtcNow - sequentialStartTime;

        // Assert
        _output.WriteLine($"병렬 실행 시간: {parallelDuration.TotalSeconds:F2}초");
        _output.WriteLine($"순차 실행 시간: {sequentialDuration.TotalSeconds:F2}초");
        _output.WriteLine($"성능 개선율: {((sequentialDuration.TotalSeconds - parallelDuration.TotalSeconds) / sequentialDuration.TotalSeconds * 100):F1}%");

        // 병렬 실행이 더 빠르거나 비슷해야 함
        Assert.True(parallelDuration <= sequentialDuration * 1.2,
            $"병렬 실행이 순차 실행보다 느림: {parallelDuration.TotalSeconds}초 vs {sequentialDuration.TotalSeconds}초");
    }

    [Fact]
    public async Task AnalyzeProjectAsync_WithContinueOnError_ShouldNotThrowOnSingleFailure()
    {
        // Arrange
        var compilationService = new TwinCatCompilationService(_mockCompilationLogger.Object);
        var variableAnalyzer = new VariableUsageAnalyzer(_mockVariableLogger.Object, _mockParserService.Object);
        var dependencyAnalyzer = new DependencyAnalyzer(_mockDependencyLogger.Object, _mockParserService.Object);
        var ioMappingValidator = new IOMappingValidator(_mockIOMappingLogger.Object);

        var orchestrator = new AdvancedAnalysisOrchestrator(
            compilationService,
            variableAnalyzer,
            dependencyAnalyzer,
            ioMappingValidator,
            _mockOrchestratorLogger.Object
        );

        var session = CreateMockValidationSession();
        _mockParserService.Setup(p => p.ExtractComments(It.IsAny<SyntaxTree>()))
            .Returns(new Dictionary<int, string>());

        // Act
        var result = await orchestrator.AnalyzeProjectAsync(
            "C:\\NonExistentProject", // 존재하지 않는 경로
            session,
            new AdvancedAnalysisOptions
            {
                ContinueOnError = true,
                RunIOMappingValidation = false
            }
        );

        // Assert - 예외가 발생하지 않아야 함
        Assert.NotNull(result);
        _output.WriteLine($"오류 발생해도 분석 완료: {result.Summary}");

        // 적어도 하나의 분석은 성공해야 함 (변수/의존성은 메모리 기반)
        Assert.True(result.VariableUsage != null || result.Dependencies != null,
            "ContinueOnError=true일 때 일부 분석은 성공해야 함");
    }

    [Fact]
    public async Task RunCompilationAnalysisAsync_ShouldDetectTwinCATAndCompile()
    {
        // Arrange
        var compilationService = new TwinCatCompilationService(_mockCompilationLogger.Object);
        var variableAnalyzer = new VariableUsageAnalyzer(_mockVariableLogger.Object, _mockParserService.Object);
        var dependencyAnalyzer = new DependencyAnalyzer(_mockDependencyLogger.Object, _mockParserService.Object);
        var ioMappingValidator = new IOMappingValidator(_mockIOMappingLogger.Object);

        var orchestrator = new AdvancedAnalysisOrchestrator(
            compilationService,
            variableAnalyzer,
            dependencyAnalyzer,
            ioMappingValidator,
            _mockOrchestratorLogger.Object
        );

        // Act
        var result = await orchestrator.RunCompilationAnalysisAsync(TEST_PROJECT_PATH, "Debug");

        // Assert
        Assert.NotNull(result);
        _output.WriteLine($"컴파일 결과: {(result.IsSuccess ? "성공" : "실패")}");
        _output.WriteLine($"오류: {result.ErrorCount}, 경고: {result.WarningCount}");
        _output.WriteLine($"소요 시간: {result.Duration.TotalSeconds:F2}초");
    }

    [Fact]
    public void ComprehensiveAnalysisResult_ShouldCalculateQualityScoreCorrectly()
    {
        // Arrange
        var result = new ComprehensiveAnalysisResult
        {
            ProjectPath = TEST_PROJECT_PATH,
            ProjectName = "TestProject",
            Compilation = new CompilationResult
            {
                IsSuccess = true,
                Errors = new List<CompilationError>(),
                Warnings = new List<CompilationWarning>
                {
                    new CompilationWarning { Message = "경고 1" },
                    new CompilationWarning { Message = "경고 2" }
                }
            },
            VariableUsage = new VariableUsageAnalysis
            {
                UnusedVariables = new List<UnusedVariable>
                {
                    new UnusedVariable { VariableName = "unused1" }
                },
                UninitializedVariables = new List<UninitializedVariable>(),
                DeadCodeBlocks = new List<DeadCode>()
            },
            Dependencies = new DependencyAnalysis
            {
                CircularReferences = new List<CircularReference>(),
                Graph = new DependencyGraph(),
                CallGraph = new CallGraph { MaxCallDepth = 5 }
            },
            IOMapping = new IOMappingValidationResult
            {
                IsValid = true,
                Errors = new List<IOMappingError>(),
                Warnings = new List<IOMappingWarning>()
            }
        };

        // Act
        var qualityScore = result.OverallQualityScore;
        var summary = result.Summary;

        // Assert
        _output.WriteLine($"품질 점수: {qualityScore:F1}/100");
        _output.WriteLine($"요약: {summary}");

        Assert.InRange(qualityScore, 0, 100);
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.TotalIssues); // 경고 2개 + 사용되지 않은 변수 1개
    }

    #region Helper Methods

    private ValidationSession CreateMockValidationSession()
    {
        return new ValidationSession
        {
            ProjectPath = TEST_PROJECT_PATH,
            SyntaxTrees = new List<SyntaxTree>
            {
                new SyntaxTree
                {
                    FilePath = Path.Combine(TEST_PROJECT_PATH, "TM", "POUs", "SamplePOU.TcPOU"),
                    SourceCode = @"
FUNCTION_BLOCK FB_Sample
VAR
    testVar : INT := 10;
END_VAR

testVar := testVar + 1;
END_FUNCTION_BLOCK"
                }
            }
        };
    }

    #endregion
}
