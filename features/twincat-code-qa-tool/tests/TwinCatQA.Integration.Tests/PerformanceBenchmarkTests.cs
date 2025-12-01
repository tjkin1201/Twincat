using System.Diagnostics;
using FluentAssertions;
using TwinCatQA.Application.Services;
using Xunit;
using Xunit.Abstractions;
using static TwinCatQA.Application.Services.QaAnalysisService;
using static TwinCatQA.Application.Services.QaReportGenerator;

namespace TwinCatQA.Integration.Tests;

/// <summary>
/// 성능 벤치마크 테스트
/// 다양한 크기의 프로젝트에 대한 성능 측정 및 메모리 사용량 확인
/// </summary>
public class PerformanceBenchmarkTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _benchmarkDir;

    public PerformanceBenchmarkTests(ITestOutputHelper output)
    {
        _output = output;
        _benchmarkDir = Path.Combine(Path.GetTempPath(), $"qa_benchmark_{Guid.NewGuid()}");
        Directory.CreateDirectory(_benchmarkDir);
    }

    #region 1. 파일 수에 따른 성능 테스트

    [Theory]
    [InlineData(10, 2.0)]   // 10개 파일, 2초 이내
    [InlineData(50, 8.0)]   // 50개 파일, 8초 이내
    [InlineData(100, 15.0)] // 100개 파일, 15초 이내
    public async Task 성능벤치마크_파일수별_처리시간측정(int fileCount, double maxSeconds)
    {
        // Arrange
        var (oldFolder, newFolder) = CreateBenchmarkFolders($"scale_{fileCount}");
        GenerateTestFiles(oldFolder, newFolder, fileCount);

        var qaService = CreateQaAnalysisService();

        // Act
        var stopwatch = Stopwatch.StartNew();
        var initialMemory = GC.GetTotalMemory(forceFullCollection: true);

        var result = await qaService.AnalyzeAsync(
            oldFolder,
            newFolder,
            new QaAnalysisOptions { MinSeverity = Domain.Models.QA.Severity.Info });

        stopwatch.Stop();
        var finalMemory = GC.GetTotalMemory(forceFullCollection: false);
        var memoryUsed = (finalMemory - initialMemory) / 1024.0 / 1024.0; // MB

        // Assert
        result.Success.Should().BeTrue();
        stopwatch.Elapsed.TotalSeconds.Should().BeLessThan(maxSeconds,
            $"{fileCount}개 파일은 {maxSeconds}초 이내에 처리되어야 함");

        var throughput = fileCount / stopwatch.Elapsed.TotalSeconds;

        _output.WriteLine($"📊 성능 벤치마크 결과 ({fileCount}개 파일):");
        _output.WriteLine($"  ⏱️  소요 시간: {stopwatch.Elapsed.TotalSeconds:F2}초 (목표: {maxSeconds}초)");
        _output.WriteLine($"  🚀 처리량: {throughput:F1} 파일/초");
        _output.WriteLine($"  💾 메모리 사용: {memoryUsed:F2} MB");
        _output.WriteLine($"  📈 변경 감지: {result.ComparisonResult.VariableChanges.Count + result.ComparisonResult.LogicChanges.Count}건");
        _output.WriteLine($"  🚨 이슈 발견: {result.Issues.Count}건");
    }

    #endregion

    #region 2. 파일 크기에 따른 성능 테스트

    [Theory]
    [InlineData(100, 1.0)]    // 100줄, 1초 이내
    [InlineData(500, 3.0)]    // 500줄, 3초 이내
    [InlineData(1000, 6.0)]   // 1000줄, 6초 이내
    public async Task 성능벤치마크_파일크기별_처리시간측정(int linesPerFile, double maxSeconds)
    {
        // Arrange
        var (oldFolder, newFolder) = CreateBenchmarkFolders($"lines_{linesPerFile}");
        GenerateLargeFile(oldFolder, newFolder, "LargeFile.TcPOU", linesPerFile);

        var qaService = CreateQaAnalysisService();

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await qaService.AnalyzeAsync(
            oldFolder,
            newFolder,
            new QaAnalysisOptions());

        stopwatch.Stop();

        // Assert
        result.Success.Should().BeTrue();
        stopwatch.Elapsed.TotalSeconds.Should().BeLessThan(maxSeconds,
            $"{linesPerFile}줄 파일은 {maxSeconds}초 이내에 처리되어야 함");

        var linesPerSecond = linesPerFile / stopwatch.Elapsed.TotalSeconds;

        _output.WriteLine($"📊 파일 크기별 성능 ({linesPerFile}줄):");
        _output.WriteLine($"  ⏱️  소요 시간: {stopwatch.Elapsed.TotalSeconds:F2}초");
        _output.WriteLine($"  🚀 처리 속도: {linesPerSecond:F0} 줄/초");
        _output.WriteLine($"  📈 변경 감지: {result.ComparisonResult.VariableChanges.Count}건");
    }

    #endregion

    #region 3. 메모리 사용량 테스트

    [Fact]
    public async Task 메모리사용량_대규모프로젝트_100MB이내()
    {
        // Arrange - 100개 파일 생성
        var (oldFolder, newFolder) = CreateBenchmarkFolders("memory_test");
        GenerateTestFiles(oldFolder, newFolder, 100);

        var qaService = CreateQaAnalysisService();

        // Act
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var memoryBefore = GC.GetTotalMemory(forceFullCollection: true);

        var result = await qaService.AnalyzeAsync(oldFolder, newFolder, new QaAnalysisOptions());

        var memoryAfter = GC.GetTotalMemory(forceFullCollection: false);
        var memoryUsedMB = (memoryAfter - memoryBefore) / 1024.0 / 1024.0;

        // Assert
        result.Success.Should().BeTrue();
        memoryUsedMB.Should().BeLessThan(100.0, "100개 파일 처리 시 메모리 사용량은 100MB 이내여야 함");

        _output.WriteLine($"💾 메모리 사용량 분석:");
        _output.WriteLine($"  - 이전: {memoryBefore / 1024.0 / 1024.0:F2} MB");
        _output.WriteLine($"  - 이후: {memoryAfter / 1024.0 / 1024.0:F2} MB");
        _output.WriteLine($"  - 사용량: {memoryUsedMB:F2} MB");
        _output.WriteLine($"  - 파일당 평균: {memoryUsedMB / 100.0:F3} MB");
    }

    [Fact]
    public async Task 메모리누수_반복실행_메모리안정성()
    {
        // Arrange
        var (oldFolder, newFolder) = CreateBenchmarkFolders("memory_leak_test");
        GenerateTestFiles(oldFolder, newFolder, 10);

        var qaService = CreateQaAnalysisService();

        // Act - 10회 반복 실행
        var memoryReadings = new List<long>();

        for (int i = 0; i < 10; i++)
        {
            await qaService.AnalyzeAsync(oldFolder, newFolder, new QaAnalysisOptions());

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            memoryReadings.Add(GC.GetTotalMemory(forceFullCollection: true));
        }

        // Assert - 메모리 사용량이 일정 수준 이상 증가하지 않아야 함
        var firstReading = memoryReadings[0];
        var lastReading = memoryReadings[9];
        var memoryIncreaseMB = (lastReading - firstReading) / 1024.0 / 1024.0;

        memoryIncreaseMB.Should().BeLessThan(10.0,
            "10회 반복 후 메모리 증가량이 10MB 미만이어야 함 (메모리 누수 없음)");

        _output.WriteLine($"🔄 메모리 누수 테스트 (10회 반복):");
        _output.WriteLine($"  - 초기 메모리: {firstReading / 1024.0 / 1024.0:F2} MB");
        _output.WriteLine($"  - 최종 메모리: {lastReading / 1024.0 / 1024.0:F2} MB");
        _output.WriteLine($"  - 증가량: {memoryIncreaseMB:F2} MB");

        for (int i = 0; i < memoryReadings.Count; i++)
        {
            _output.WriteLine($"    실행 {i + 1}: {memoryReadings[i] / 1024.0 / 1024.0:F2} MB");
        }
    }

    #endregion

    #region 4. 리포트 생성 성능 테스트

    [Fact]
    public async Task 리포트생성성능_모든형식_1초이내()
    {
        // Arrange
        var (oldFolder, newFolder) = CreateBenchmarkFolders("report_perf");
        GenerateTestFiles(oldFolder, newFolder, 20);

        var qaService = CreateQaAnalysisService();
        var reportGenerator = new QaReportGenerator();

        var analysisResult = await qaService.AnalyzeAsync(oldFolder, newFolder, new QaAnalysisOptions());

        // Act - 리포트 생성 성능 측정
        var reportPath = Path.Combine(_benchmarkDir, "reports");

        var stopwatch = Stopwatch.StartNew();
        var reportFiles = await reportGenerator.GenerateReportsAsync(
            analysisResult,
            reportPath,
            ReportFormat.All);
        stopwatch.Stop();

        // Assert
        stopwatch.Elapsed.TotalSeconds.Should().BeLessThan(1.0,
            "4개 리포트(HTML, MD, JSON, Excel) 생성은 1초 이내여야 함");

        reportFiles.Should().HaveCount(4);

        _output.WriteLine($"📝 리포트 생성 성능:");
        _output.WriteLine($"  ⏱️  소요 시간: {stopwatch.Elapsed.TotalMilliseconds:F0}ms");
        _output.WriteLine($"  📄 생성 파일: {reportFiles.Count}개");

        foreach (var file in reportFiles)
        {
            var fileInfo = new FileInfo(file);
            _output.WriteLine($"    - {Path.GetFileName(file)}: {fileInfo.Length / 1024.0:F1} KB");
        }
    }

    #endregion

    #region 5. 병렬 처리 성능 테스트

    [Fact]
    public async Task 병렬처리_복수프로젝트_동시분석()
    {
        // Arrange - 3개의 독립 프로젝트 생성
        var projects = new List<(string old, string newFolder)>();
        for (int i = 1; i <= 3; i++)
        {
            var (oldFolder, newFolder) = CreateBenchmarkFolders($"parallel_project{i}");
            GenerateTestFiles(oldFolder, newFolder, 20);
            projects.Add((oldFolder, newFolder));
        }

        var qaService = CreateQaAnalysisService();

        // Act - 순차 실행
        var sequentialStopwatch = Stopwatch.StartNew();
        foreach (var (old, newFolder) in projects)
        {
            await qaService.AnalyzeAsync(old, newFolder, new QaAnalysisOptions());
        }
        sequentialStopwatch.Stop();

        // Act - 병렬 실행
        var parallelStopwatch = Stopwatch.StartNew();
        var parallelTasks = projects.Select(p =>
            qaService.AnalyzeAsync(p.old, p.newFolder, new QaAnalysisOptions()));
        var parallelResults = await Task.WhenAll(parallelTasks);
        parallelStopwatch.Stop();

        // Assert
        parallelResults.Should().OnlyContain(r => r.Success);

        var speedup = sequentialStopwatch.Elapsed.TotalSeconds / parallelStopwatch.Elapsed.TotalSeconds;

        _output.WriteLine($"🔀 병렬 처리 성능 (3개 프로젝트):");
        _output.WriteLine($"  순차 실행: {sequentialStopwatch.Elapsed.TotalSeconds:F2}초");
        _output.WriteLine($"  병렬 실행: {parallelStopwatch.Elapsed.TotalSeconds:F2}초");
        _output.WriteLine($"  성능 향상: {speedup:F2}x");

        // 병렬 처리가 순차 처리보다 빨라야 함
        parallelStopwatch.Elapsed.Should().BeLessThan(sequentialStopwatch.Elapsed);
    }

    #endregion

    #region 6. 복잡도에 따른 성능 테스트

    [Theory]
    [InlineData(5, 2.0)]    // 낮은 복잡도 (중첩 5단계)
    [InlineData(10, 4.0)]   // 중간 복잡도 (중첩 10단계)
    [InlineData(15, 7.0)]   // 높은 복잡도 (중첩 15단계)
    public async Task 복잡도성능_중첩깊이별_처리시간(int nestingDepth, double maxSeconds)
    {
        // Arrange
        var (oldFolder, newFolder) = CreateBenchmarkFolders($"complexity_{nestingDepth}");
        GenerateComplexNestedCode(oldFolder, newFolder, nestingDepth);

        var qaService = CreateQaAnalysisService();

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await qaService.AnalyzeAsync(oldFolder, newFolder, new QaAnalysisOptions());
        stopwatch.Stop();

        // Assert
        result.Success.Should().BeTrue();
        stopwatch.Elapsed.TotalSeconds.Should().BeLessThan(maxSeconds);

        _output.WriteLine($"🔢 복잡도 성능 테스트 (중첩 {nestingDepth}단계):");
        _output.WriteLine($"  ⏱️  소요 시간: {stopwatch.Elapsed.TotalSeconds:F2}초");
        _output.WriteLine($"  📊 감지된 변경: {result.ComparisonResult.LogicChanges.Count}건");
    }

    #endregion

    #region 테스트 데이터 생성 헬퍼

    private (string oldFolder, string newFolder) CreateBenchmarkFolders(string name)
    {
        var oldFolder = Path.Combine(_benchmarkDir, name, "old");
        var newFolder = Path.Combine(_benchmarkDir, name, "new");

        Directory.CreateDirectory(oldFolder);
        Directory.CreateDirectory(newFolder);

        return (oldFolder, newFolder);
    }

    private void GenerateTestFiles(string oldFolder, string newFolder, int fileCount)
    {
        for (int i = 1; i <= fileCount; i++)
        {
            var oldCode = GenerateSampleCode(i, isOld: true);
            var newCode = GenerateSampleCode(i, isOld: false);

            File.WriteAllText(Path.Combine(oldFolder, $"FB_Test{i}.TcPOU"), oldCode);
            File.WriteAllText(Path.Combine(newFolder, $"FB_Test{i}.TcPOU"), newCode);
        }
    }

    private string GenerateSampleCode(int index, bool isOld)
    {
        if (isOld)
        {
            return $@"
FUNCTION_BLOCK FB_Test{index}
VAR_INPUT
    input{index} : INT;
END_VAR
VAR_OUTPUT
    output{index} : INT;
END_VAR
VAR
    counter{index} : DINT := 0;
END_VAR

// 로직
IF input{index} > 0 THEN
    output{index} := input{index} * 2;
    counter{index} := counter{index} + 1;
END_IF
END_FUNCTION_BLOCK";
        }
        else
        {
            return $@"
FUNCTION_BLOCK FB_Test{index}
VAR_INPUT
    input{index} : REAL;  // 타입 변경: INT -> REAL
    enableFlag{index} : BOOL;  // 새 입력
END_VAR
VAR_OUTPUT
    output{index} : REAL;  // 타입 변경
    status{index} : INT;  // 새 출력
END_VAR
VAR
    counter{index} : DINT := 0;
    tempValue{index} : REAL;  // 새 변수
END_VAR

// 개선된 로직
IF enableFlag{index} AND (input{index} > 0.0) THEN
    tempValue{index} := input{index} * 2.5;
    output{index} := tempValue{index};
    counter{index} := counter{index} + 1;
    status{index} := 1;
ELSE
    status{index} := 0;
END_IF
END_FUNCTION_BLOCK";
        }
    }

    private void GenerateLargeFile(string oldFolder, string newFolder, string fileName, int lineCount)
    {
        var oldCode = new System.Text.StringBuilder();
        var newCode = new System.Text.StringBuilder();

        oldCode.AppendLine("FUNCTION_BLOCK FB_LargeFile");
        oldCode.AppendLine("VAR");

        newCode.AppendLine("FUNCTION_BLOCK FB_LargeFile");
        newCode.AppendLine("VAR");

        // 변수 선언 (줄 수의 1/3)
        for (int i = 1; i <= lineCount / 3; i++)
        {
            oldCode.AppendLine($"    var{i} : INT;");
            newCode.AppendLine($"    var{i} : REAL;  // 타입 변경");
        }

        oldCode.AppendLine("END_VAR");
        newCode.AppendLine("END_VAR");

        // 로직 (줄 수의 2/3)
        for (int i = 1; i <= lineCount * 2 / 3; i++)
        {
            oldCode.AppendLine($"// 로직 라인 {i}");
            oldCode.AppendLine($"var{i % (lineCount / 3) + 1} := var{i % (lineCount / 3) + 1} + 1;");

            newCode.AppendLine($"// 개선된 로직 {i}");
            newCode.AppendLine($"var{i % (lineCount / 3) + 1} := var{i % (lineCount / 3) + 1} + 1.0;");
        }

        oldCode.AppendLine("END_FUNCTION_BLOCK");
        newCode.AppendLine("END_FUNCTION_BLOCK");

        File.WriteAllText(Path.Combine(oldFolder, fileName), oldCode.ToString());
        File.WriteAllText(Path.Combine(newFolder, fileName), newCode.ToString());
    }

    private void GenerateComplexNestedCode(string oldFolder, string newFolder, int nestingDepth)
    {
        var oldCode = new System.Text.StringBuilder();
        var newCode = new System.Text.StringBuilder();

        oldCode.AppendLine("FUNCTION_BLOCK FB_ComplexNesting");
        oldCode.AppendLine("VAR");
        oldCode.AppendLine("    value : INT;");
        oldCode.AppendLine("END_VAR");

        newCode.AppendLine("FUNCTION_BLOCK FB_ComplexNesting");
        newCode.AppendLine("VAR");
        newCode.AppendLine("    value : REAL;  // 타입 변경");
        newCode.AppendLine("    newValue : BOOL;  // 새 변수");
        newCode.AppendLine("END_VAR");

        // 중첩된 IF 문 생성
        for (int i = 1; i <= nestingDepth; i++)
        {
            var indent = new string(' ', i * 4);
            oldCode.AppendLine($"{indent}IF value > {i} THEN");
            newCode.AppendLine($"{indent}IF value > {i}.0 THEN");
        }

        var maxIndent = new string(' ', (nestingDepth + 1) * 4);
        oldCode.AppendLine($"{maxIndent}value := value + 1;");
        newCode.AppendLine($"{maxIndent}value := value + 1.0;");
        newCode.AppendLine($"{maxIndent}newValue := TRUE;  // 새 로직");

        // END_IF 닫기
        for (int i = nestingDepth; i >= 1; i--)
        {
            var indent = new string(' ', i * 4);
            oldCode.AppendLine($"{indent}END_IF");
            newCode.AppendLine($"{indent}END_IF");
        }

        oldCode.AppendLine("END_FUNCTION_BLOCK");
        newCode.AppendLine("END_FUNCTION_BLOCK");

        File.WriteAllText(Path.Combine(oldFolder, "FB_ComplexNesting.TcPOU"), oldCode.ToString());
        File.WriteAllText(Path.Combine(newFolder, "FB_ComplexNesting.TcPOU"), newCode.ToString());
    }

    #endregion

    #region 서비스 생성 헬퍼

    private QaAnalysisService CreateQaAnalysisService()
    {
        // QA 규칙 체커들 생성
        var ruleCheckers = new List<TwinCatQA.Domain.Services.IQARuleChecker>
        {
            new TwinCatQA.Infrastructure.QA.Rules.TypeNarrowingRule(),
            new TwinCatQA.Infrastructure.QA.Rules.NullCheckRule(),
            new TwinCatQA.Infrastructure.QA.Rules.MagicNumberRule(),
            new TwinCatQA.Infrastructure.QA.Rules.LongFunctionRule(),
            new TwinCatQA.Infrastructure.QA.Rules.DeepNestingRule(),
            new TwinCatQA.Infrastructure.QA.Rules.UnusedVariableRule(),
            new TwinCatQA.Infrastructure.QA.Rules.UninitializedVariableRule(),
            new TwinCatQA.Infrastructure.QA.Rules.InsufficientCommentsRule(),
            new TwinCatQA.Infrastructure.QA.Rules.NamingConventionRule(),
            new TwinCatQA.Infrastructure.QA.Rules.HighComplexityRule(),
            new TwinCatQA.Infrastructure.QA.Rules.DuplicateCodeRule(),
            new TwinCatQA.Infrastructure.QA.Rules.GlobalVariableOveruseRule(),
            new TwinCatQA.Infrastructure.QA.Rules.TooManyParametersRule(),
            new TwinCatQA.Infrastructure.QA.Rules.FloatingPointComparisonRule(),
            new TwinCatQA.Infrastructure.QA.Rules.ArrayBoundsRule(),
            new TwinCatQA.Infrastructure.QA.Rules.InfiniteLoopRiskRule(),
            new TwinCatQA.Infrastructure.QA.Rules.HardcodedIOAddressRule(),
            new TwinCatQA.Infrastructure.QA.Rules.MissingCaseElseRule(),
            new TwinCatQA.Infrastructure.QA.Rules.InconsistentStyleRule(),
            new TwinCatQA.Infrastructure.QA.Rules.ExcessivelyLongNameRule()
        };

        return new QaAnalysisService(ruleCheckers);
    }

    #endregion

    public void Dispose()
    {
        // 벤치마크 디렉토리 정리
        if (Directory.Exists(_benchmarkDir))
        {
            try
            {
                Directory.Delete(_benchmarkDir, true);
            }
            catch
            {
                // 정리 실패 무시
            }
        }
    }
}
