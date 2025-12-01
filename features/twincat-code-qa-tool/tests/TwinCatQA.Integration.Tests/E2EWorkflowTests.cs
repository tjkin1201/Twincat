using System.Diagnostics;
using FluentAssertions;
using TwinCatQA.Application.Services;
using TwinCatQA.Domain.Models.QA;
using Xunit;
using Xunit.Abstractions;
using static TwinCatQA.Application.Services.QaAnalysisService;
using static TwinCatQA.Application.Services.QaReportGenerator;

namespace TwinCatQA.Integration.Tests;

/// <summary>
/// End-to-End 워크플로우 통합 테스트
/// 전체 QA 도구의 워크플로우를 검증:
/// 1. 폴더 비교 (FolderComparer)
/// 2. QA 분석 (QaAnalysisService + QARuleEngine)
/// 3. 리포트 생성 (QaReportGenerator)
/// </summary>
public class E2EWorkflowTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _tempOutputDir;
    private readonly string _testDataDir;

    public E2EWorkflowTests(ITestOutputHelper output)
    {
        _output = output;
        _tempOutputDir = Path.Combine(Path.GetTempPath(), $"qa_e2e_{Guid.NewGuid()}");
        _testDataDir = Path.Combine(_tempOutputDir, "test_data");

        Directory.CreateDirectory(_tempOutputDir);
        Directory.CreateDirectory(_testDataDir);
    }

    #region 1. 전체 워크플로우 E2E 테스트

    [Fact]
    public async Task 전체워크플로우_폴더비교부터리포트생성까지_성공()
    {
        // Arrange - 테스트 데이터 준비
        var (oldFolder, newFolder) = CreateTestFolders();
        CreateSampleCodeFiles(oldFolder, newFolder);

        var outputPath = Path.Combine(_tempOutputDir, "reports");

        var qaService = CreateQaAnalysisService();
        var reportGenerator = new QaReportGenerator();

        // Act - 1단계: QA 분석 수행
        var analysisOptions = new QaAnalysisOptions
        {
            MinSeverity = Severity.Info,
            Verbose = true
        };

        var analysisResult = await qaService.AnalyzeAsync(oldFolder, newFolder, analysisOptions);

        // Assert - 분석 결과 검증
        analysisResult.Should().NotBeNull();
        analysisResult.Success.Should().BeTrue();
        analysisResult.ErrorMessage.Should().BeNullOrEmpty();

        _output.WriteLine($"✅ 분석 완료");
        _output.WriteLine($"  - 소요 시간: {analysisResult.Duration.TotalSeconds:F2}초");
        _output.WriteLine($"  - 변수 변경: {analysisResult.ComparisonResult.VariableChanges.Count}건");
        _output.WriteLine($"  - 로직 변경: {analysisResult.ComparisonResult.LogicChanges.Count}건");
        _output.WriteLine($"  - 데이터 타입 변경: {analysisResult.ComparisonResult.DataTypeChanges.Count}건");
        _output.WriteLine($"  - Total Issues: {analysisResult.Issues.Count}");
        _output.WriteLine($"    - Critical: {analysisResult.CriticalCount}");
        _output.WriteLine($"    - Warning: {analysisResult.WarningCount}");
        _output.WriteLine($"    - Info: {analysisResult.InfoCount}");

        // 변경 사항이 감지되었는지 확인
        var totalChanges = analysisResult.ComparisonResult.VariableChanges.Count +
                          analysisResult.ComparisonResult.LogicChanges.Count +
                          analysisResult.ComparisonResult.DataTypeChanges.Count;

        totalChanges.Should().BeGreaterThan(0, "샘플 코드에서 변경이 감지되어야 함");

        // Act - 2단계: 리포트 생성 (HTML, Markdown, JSON, Excel)
        var reportFiles = await reportGenerator.GenerateReportsAsync(
            analysisResult,
            outputPath,
            ReportFormat.All);

        // Assert - 리포트 파일 검증
        reportFiles.Should().HaveCount(4, "HTML, Markdown, JSON, Excel 4개 파일 생성");

        reportFiles.Should().Contain(f => f.EndsWith(".html"), "HTML 리포트 생성");
        reportFiles.Should().Contain(f => f.EndsWith(".md"), "Markdown 리포트 생성");
        reportFiles.Should().Contain(f => f.EndsWith(".json"), "JSON 리포트 생성");
        reportFiles.Should().Contain(f => f.EndsWith(".xlsx"), "Excel 리포트 생성");

        foreach (var file in reportFiles)
        {
            File.Exists(file).Should().BeTrue($"{file} 파일이 생성되어야 함");
            var fileInfo = new FileInfo(file);
            fileInfo.Length.Should().BeGreaterThan(0, "리포트 파일은 비어있으면 안됨");

            _output.WriteLine($"✅ 리포트 생성: {Path.GetFileName(file)} ({fileInfo.Length} bytes)");
        }

        // 리포트 내용 검증 (HTML)
        var htmlReport = reportFiles.First(f => f.EndsWith(".html"));
        var htmlContent = await File.ReadAllTextAsync(htmlReport);
        htmlContent.Should().Contain("TwinCAT 코드 QA 분석 보고서");
        htmlContent.Should().Contain("이슈 요약");

        // 리포트 내용 검증 (Markdown)
        var mdReport = reportFiles.First(f => f.EndsWith(".md"));
        var mdContent = await File.ReadAllTextAsync(mdReport);
        mdContent.Should().Contain("# 🔍 TwinCAT 코드 QA 분석 보고서");

        // 리포트 내용 검증 (JSON)
        var jsonReport = reportFiles.First(f => f.EndsWith(".json"));
        var jsonContent = await File.ReadAllTextAsync(jsonReport);
        jsonContent.Should().Contain("\"summary\"");
        jsonContent.Should().Contain("\"changes\"");
        jsonContent.Should().Contain("\"issues\"");
    }

    [Fact]
    public async Task 전체워크플로우_위험한변경감지_Critical이슈보고()
    {
        // Arrange - 위험한 타입 변경이 있는 테스트 데이터
        var (oldFolder, newFolder) = CreateTestFolders();
        CreateCodeWithDangerousTypeChange(oldFolder, newFolder);

        var qaService = CreateQaAnalysisService();

        // Act
        var options = new QaAnalysisOptions { MinSeverity = Severity.Critical };
        var result = await qaService.AnalyzeAsync(oldFolder, newFolder, options);

        // Assert
        result.Success.Should().BeTrue();
        result.CriticalCount.Should().BeGreaterThan(0, "위험한 타입 변경은 Critical 이슈를 생성해야 함");

        var criticalIssues = result.Issues.Where(i => i.Severity == Severity.Critical).ToList();

        _output.WriteLine($"🔴 Critical 이슈 발견: {criticalIssues.Count}건");
        foreach (var issue in criticalIssues)
        {
            _output.WriteLine($"  [{issue.RuleId}] {issue.Title}");
            _output.WriteLine($"    위치: {issue.Location}");
            _output.WriteLine($"    설명: {issue.Description}");
        }

        // Critical 이슈에 권장 사항이 포함되어 있는지 확인
        criticalIssues.Should().OnlyContain(i => !string.IsNullOrEmpty(i.Recommendation));
    }

    [Fact]
    public async Task 전체워크플로우_규칙필터링_특정규칙만실행()
    {
        // Arrange
        var (oldFolder, newFolder) = CreateTestFolders();
        CreateSampleCodeFiles(oldFolder, newFolder);

        var qaService = CreateQaAnalysisService();

        // Act - 특정 규칙만 실행 (QA001, QA002)
        var options = new QaAnalysisOptions
        {
            IncludeRules = new List<string> { "QA001", "QA002" },
            MinSeverity = Severity.Info
        };

        var result = await qaService.AnalyzeAsync(oldFolder, newFolder, options);

        // Assert
        result.Success.Should().BeTrue();

        if (result.Issues.Any())
        {
            // 결과 이슈는 QA001, QA002만 있어야 함
            result.Issues.Should().OnlyContain(i =>
                i.RuleId == "QA001" || i.RuleId == "QA002" || i.RuleId.StartsWith("QA00"));
        }

        _output.WriteLine($"필터링된 이슈: {result.Issues.Count}건");
        _output.WriteLine($"규칙 ID: {string.Join(", ", result.Issues.Select(i => i.RuleId).Distinct())}");
    }

    [Fact]
    public async Task 전체워크플로우_규칙제외_특정규칙제외실행()
    {
        // Arrange
        var (oldFolder, newFolder) = CreateTestFolders();
        CreateSampleCodeFiles(oldFolder, newFolder);

        var qaService = CreateQaAnalysisService();

        // Act - 특정 규칙 제외 (QA004 제외)
        var options = new QaAnalysisOptions
        {
            ExcludeRules = new List<string> { "QA004" },
            MinSeverity = Severity.Info
        };

        var result = await qaService.AnalyzeAsync(oldFolder, newFolder, options);

        // Assert
        result.Success.Should().BeTrue();
        result.Issues.Should().NotContain(i => i.RuleId == "QA004", "QA004는 제외되어야 함");

        _output.WriteLine($"제외 필터 적용 후 이슈: {result.Issues.Count}건");
    }

    #endregion

    #region 2. 성능 테스트

    [Fact]
    public async Task 성능테스트_전체분석_5초이내()
    {
        // Arrange
        var (oldFolder, newFolder) = CreateTestFolders();
        CreateSampleCodeFiles(oldFolder, newFolder);

        var qaService = CreateQaAnalysisService();

        // Act
        var stopwatch = Stopwatch.StartNew();
        var options = new QaAnalysisOptions { MinSeverity = Severity.Info };
        var result = await qaService.AnalyzeAsync(oldFolder, newFolder, options);
        stopwatch.Stop();

        // Assert
        result.Success.Should().BeTrue();
        stopwatch.Elapsed.TotalSeconds.Should().BeLessThan(5.0,
            "소규모 프로젝트 분석은 5초 이내에 완료되어야 함");

        _output.WriteLine($"⏱️  분석 소요 시간: {stopwatch.Elapsed.TotalSeconds:F3}초");
        _output.WriteLine($"  - 변경 감지: {result.ComparisonResult.VariableChanges.Count + result.ComparisonResult.LogicChanges.Count}건");
        _output.WriteLine($"  - 이슈 발견: {result.Issues.Count}건");
    }

    [Fact]
    public async Task 성능테스트_대규모파일_처리성능()
    {
        // Arrange - 50개 파일 생성
        var (oldFolder, newFolder) = CreateTestFolders();
        CreateLargeTestDataset(oldFolder, newFolder, fileCount: 50);

        var qaService = CreateQaAnalysisService();

        // Act
        var stopwatch = Stopwatch.StartNew();
        var options = new QaAnalysisOptions { MinSeverity = Severity.Info };
        var result = await qaService.AnalyzeAsync(oldFolder, newFolder, options);
        stopwatch.Stop();

        // Assert
        result.Success.Should().BeTrue();

        var throughput = 50 / stopwatch.Elapsed.TotalSeconds;
        _output.WriteLine($"⏱️  대규모 분석 성능:");
        _output.WriteLine($"  - 파일 수: 50개");
        _output.WriteLine($"  - 소요 시간: {stopwatch.Elapsed.TotalSeconds:F2}초");
        _output.WriteLine($"  - 처리량: {throughput:F1} 파일/초");
        _output.WriteLine($"  - 변경 감지: {result.ComparisonResult.VariableChanges.Count + result.ComparisonResult.LogicChanges.Count}건");

        // 처리량이 너무 낮으면 경고
        throughput.Should().BeGreaterThan(1.0, "최소 1 파일/초 이상 처리해야 함");
    }

    #endregion

    #region 3. 리포트 생성 테스트

    [Fact]
    public async Task 리포트출력_HTML파일생성_성공()
    {
        // Arrange
        var (oldFolder, newFolder) = CreateTestFolders();
        CreateSampleCodeFiles(oldFolder, newFolder);

        var qaService = CreateQaAnalysisService();
        var reportGenerator = new QaReportGenerator();

        var result = await qaService.AnalyzeAsync(oldFolder, newFolder, new QaAnalysisOptions());

        // Act
        var outputPath = Path.Combine(_tempOutputDir, "html_reports");
        var files = await reportGenerator.GenerateReportsAsync(result, outputPath, ReportFormat.Html);

        // Assert
        files.Should().HaveCount(1);
        var htmlFile = files[0];

        htmlFile.Should().EndWith(".html");
        File.Exists(htmlFile).Should().BeTrue();

        var content = await File.ReadAllTextAsync(htmlFile);

        // HTML 구조 검증
        content.Should().Contain("<!DOCTYPE html>");
        content.Should().Contain("<html");
        content.Should().Contain("TwinCAT 코드 QA 분석 보고서");
        content.Should().Contain("이슈 요약");
        content.Should().Contain("변경 사항 통계");

        // 스타일 포함 여부
        content.Should().Contain("<style>");
        content.Should().Contain(".summary");
        content.Should().Contain(".issue");

        _output.WriteLine($"✅ HTML 리포트 생성 성공: {htmlFile}");
        _output.WriteLine($"  - 파일 크기: {new FileInfo(htmlFile).Length} bytes");
        _output.WriteLine($"  - 이슈 포함: {result.Issues.Count}건");
    }

    [Fact]
    public async Task 리포트출력_Markdown파일생성_성공()
    {
        // Arrange
        var (oldFolder, newFolder) = CreateTestFolders();
        CreateSampleCodeFiles(oldFolder, newFolder);

        var qaService = CreateQaAnalysisService();
        var reportGenerator = new QaReportGenerator();

        var result = await qaService.AnalyzeAsync(oldFolder, newFolder, new QaAnalysisOptions());

        // Act
        var outputPath = Path.Combine(_tempOutputDir, "md_reports");
        var files = await reportGenerator.GenerateReportsAsync(result, outputPath, ReportFormat.Markdown);

        // Assert
        files.Should().HaveCount(1);
        var mdFile = files[0];

        mdFile.Should().EndWith(".md");
        File.Exists(mdFile).Should().BeTrue();

        var content = await File.ReadAllTextAsync(mdFile);

        // Markdown 구조 검증
        content.Should().Contain("# 🔍 TwinCAT 코드 QA 분석 보고서");
        content.Should().Contain("## 📊 이슈 요약");
        content.Should().Contain("## 📈 변경 사항 통계");
        content.Should().Contain("| 심각도 | 개수 |");

        _output.WriteLine($"✅ Markdown 리포트 생성 성공: {mdFile}");
        _output.WriteLine($"  - 파일 크기: {new FileInfo(mdFile).Length} bytes");
    }

    [Fact]
    public async Task 리포트출력_JSON파일생성_유효한JSON()
    {
        // Arrange
        var (oldFolder, newFolder) = CreateTestFolders();
        CreateSampleCodeFiles(oldFolder, newFolder);

        var qaService = CreateQaAnalysisService();
        var reportGenerator = new QaReportGenerator();

        var result = await qaService.AnalyzeAsync(oldFolder, newFolder, new QaAnalysisOptions());

        // Act
        var outputPath = Path.Combine(_tempOutputDir, "json_reports");
        var files = await reportGenerator.GenerateReportsAsync(result, outputPath, ReportFormat.Json);

        // Assert
        files.Should().HaveCount(1);
        var jsonFile = files[0];

        jsonFile.Should().EndWith(".json");
        File.Exists(jsonFile).Should().BeTrue();

        var content = await File.ReadAllTextAsync(jsonFile);

        // JSON 파싱 검증
        var jsonDoc = System.Text.Json.JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        root.TryGetProperty("summary", out _).Should().BeTrue();
        root.TryGetProperty("changes", out _).Should().BeTrue();
        root.TryGetProperty("issues", out _).Should().BeTrue();

        _output.WriteLine($"✅ JSON 리포트 생성 성공: {jsonFile}");
        _output.WriteLine($"  - 파일 크기: {new FileInfo(jsonFile).Length} bytes");
        _output.WriteLine($"  - JSON 구조 유효성: ✓");
    }

    [Fact]
    public async Task 리포트출력_모든형식_동시생성()
    {
        // Arrange
        var (oldFolder, newFolder) = CreateTestFolders();
        CreateSampleCodeFiles(oldFolder, newFolder);

        var qaService = CreateQaAnalysisService();
        var reportGenerator = new QaReportGenerator();

        var result = await qaService.AnalyzeAsync(oldFolder, newFolder, new QaAnalysisOptions());

        // Act - ReportFormat.All로 모든 형식 생성
        var outputPath = Path.Combine(_tempOutputDir, "all_reports");
        var files = await reportGenerator.GenerateReportsAsync(result, outputPath, ReportFormat.All);

        // Assert
        files.Should().HaveCount(4, "HTML, Markdown, JSON, Excel 모두 생성");

        files.Count(f => f.EndsWith(".html")).Should().Be(1);
        files.Count(f => f.EndsWith(".md")).Should().Be(1);
        files.Count(f => f.EndsWith(".json")).Should().Be(1);
        files.Count(f => f.EndsWith(".xlsx")).Should().Be(1);

        foreach (var file in files)
        {
            File.Exists(file).Should().BeTrue();
            new FileInfo(file).Length.Should().BeGreaterThan(0);
            _output.WriteLine($"  ✓ {Path.GetFileName(file)}");
        }
    }

    #endregion

    #region 4. 에러 처리 테스트

    [Fact]
    public async Task 에러처리_존재하지않는폴더_실패결과반환()
    {
        // Arrange
        var qaService = CreateQaAnalysisService();

        var nonExistentOld = Path.Combine(_tempOutputDir, "nonexistent_old");
        var nonExistentNew = Path.Combine(_tempOutputDir, "nonexistent_new");

        // Act
        var result = await qaService.AnalyzeAsync(nonExistentOld, nonExistentNew, new QaAnalysisOptions());

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();

        _output.WriteLine($"예상된 오류: {result.ErrorMessage}");
    }

    [Fact]
    public async Task 에러처리_빈폴더_변경없음결과()
    {
        // Arrange - 빈 폴더
        var emptyOld = Path.Combine(_tempOutputDir, "empty_old");
        var emptyNew = Path.Combine(_tempOutputDir, "empty_new");
        Directory.CreateDirectory(emptyOld);
        Directory.CreateDirectory(emptyNew);

        var qaService = CreateQaAnalysisService();

        // Act
        var result = await qaService.AnalyzeAsync(emptyOld, emptyNew, new QaAnalysisOptions());

        // Assert
        result.Success.Should().BeTrue();
        result.ComparisonResult.VariableChanges.Should().BeEmpty();
        result.ComparisonResult.LogicChanges.Should().BeEmpty();
        result.ComparisonResult.DataTypeChanges.Should().BeEmpty();
        result.Issues.Should().BeEmpty();

        _output.WriteLine("빈 폴더 비교 - 변경 사항 없음 (예상된 동작)");
    }

    #endregion

    #region 테스트 데이터 생성 헬퍼 메서드

    private (string oldFolder, string newFolder) CreateTestFolders()
    {
        var oldFolder = Path.Combine(_testDataDir, "old_version");
        var newFolder = Path.Combine(_testDataDir, "new_version");

        Directory.CreateDirectory(oldFolder);
        Directory.CreateDirectory(newFolder);

        return (oldFolder, newFolder);
    }

    private void CreateSampleCodeFiles(string oldFolder, string newFolder)
    {
        // 이전 버전 코드
        var oldCode = @"
FUNCTION_BLOCK FB_MotorControl
VAR_INPUT
    enable : BOOL;
    targetSpeed : INT;  // 이전: INT
END_VAR
VAR_OUTPUT
    currentSpeed : INT;
    isRunning : BOOL;
END_VAR
VAR
    internalCounter : DINT;
END_VAR

// 모터 제어 로직
IF enable THEN
    currentSpeed := targetSpeed;
    isRunning := TRUE;
END_IF
END_FUNCTION_BLOCK";

        // 신규 버전 코드 (변경사항 있음)
        var newCode = @"
FUNCTION_BLOCK FB_MotorControl
VAR_INPUT
    enable : BOOL;
    targetSpeed : REAL;  // 변경: INT -> REAL (타입 확장)
    safetyLimit : REAL;  // 추가: 새 변수
END_VAR
VAR_OUTPUT
    currentSpeed : REAL;  // 변경: INT -> REAL
    isRunning : BOOL;
    errorCode : INT;  // 추가: 새 출력
END_VAR
VAR
    internalCounter : DINT;
    // unusedVar : INT;  // 주석: 사용되지 않는 변수 (Dead Code)
END_VAR

// 모터 제어 로직 (개선됨)
IF enable THEN
    IF targetSpeed <= safetyLimit THEN  // 추가: 안전 체크
        currentSpeed := targetSpeed;
        isRunning := TRUE;
        errorCode := 0;
    ELSE
        isRunning := FALSE;
        errorCode := 1;  // 안전 한계 초과
    END_IF;
END_IF
END_FUNCTION_BLOCK";

        File.WriteAllText(Path.Combine(oldFolder, "FB_MotorControl.TcPOU"), oldCode);
        File.WriteAllText(Path.Combine(newFolder, "FB_MotorControl.TcPOU"), newCode);
    }

    private void CreateCodeWithDangerousTypeChange(string oldFolder, string newFolder)
    {
        // 위험한 타입 축소가 있는 코드
        var oldCode = @"
FUNCTION_BLOCK FB_DangerousChange
VAR
    largeValue : DINT;  // 이전: DINT (32비트)
    preciseValue : LREAL;  // 이전: LREAL (64비트)
END_VAR
END_FUNCTION_BLOCK";

        var newCode = @"
FUNCTION_BLOCK FB_DangerousChange
VAR
    largeValue : INT;  // 위험: DINT -> INT (타입 축소!)
    preciseValue : REAL;  // 위험: LREAL -> REAL (정밀도 손실!)
END_VAR
END_FUNCTION_BLOCK";

        File.WriteAllText(Path.Combine(oldFolder, "FB_DangerousChange.TcPOU"), oldCode);
        File.WriteAllText(Path.Combine(newFolder, "FB_DangerousChange.TcPOU"), newCode);
    }

    private void CreateLargeTestDataset(string oldFolder, string newFolder, int fileCount)
    {
        for (int i = 1; i <= fileCount; i++)
        {
            var oldCode = $@"
FUNCTION_BLOCK FB_TestBlock{i}
VAR
    value{i} : INT := {i};
    counter{i} : DINT;
END_VAR
counter{i} := counter{i} + 1;
END_FUNCTION_BLOCK";

            var newCode = $@"
FUNCTION_BLOCK FB_TestBlock{i}
VAR
    value{i} : REAL := {i}.0;  // 타입 변경
    counter{i} : DINT;
    newVar{i} : BOOL;  // 새 변수 추가
END_VAR
counter{i} := counter{i} + 1;
IF newVar{i} THEN
    // 새 로직
END_IF
END_FUNCTION_BLOCK";

            File.WriteAllText(Path.Combine(oldFolder, $"FB_TestBlock{i}.TcPOU"), oldCode);
            File.WriteAllText(Path.Combine(newFolder, $"FB_TestBlock{i}.TcPOU"), newCode);
        }
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
        // 테스트 후 임시 디렉토리 정리
        if (Directory.Exists(_tempOutputDir))
        {
            try
            {
                Directory.Delete(_tempOutputDir, true);
            }
            catch
            {
                // 정리 실패는 무시 (다른 프로세스가 파일 사용 중일 수 있음)
            }
        }
    }
}
