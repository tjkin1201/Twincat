using System.Text.Json;
using TwinCatQA.Domain.Models.QA;
using TwinCatQA.Infrastructure.Reports;
using Xunit;

namespace TwinCatQA.Infrastructure.Tests.Reports;

/// <summary>
/// Markdown 및 JSON 리포트 생성기 테스트
/// </summary>
public class ReportGeneratorTests
{
    private readonly MarkdownReportGenerator _markdownGenerator;
    private readonly JsonReportGenerator _jsonGenerator;

    public ReportGeneratorTests()
    {
        _markdownGenerator = new MarkdownReportGenerator();
        _jsonGenerator = new JsonReportGenerator(prettyPrint: true);
    }

    #region Markdown 리포트 테스트

    [Fact]
    public void MarkdownGenerator_Generate_ValidReport_ReturnsMarkdown()
    {
        // Arrange
        var report = CreateSampleReport();

        // Act
        var markdown = _markdownGenerator.Generate(report);

        // Assert
        Assert.NotNull(markdown);
        Assert.NotEmpty(markdown);
        Assert.Contains("# 🔍 TwinCAT Code QA Report", markdown);
        Assert.Contains("## 📊 Summary", markdown);
        Assert.Contains("## 🔴 Critical Issues", markdown);
        Assert.Contains("## 🟡 Warning Issues", markdown);
        Assert.Contains("## 📈 Statistics", markdown);
    }

    [Fact]
    public void MarkdownGenerator_Generate_NullReport_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _markdownGenerator.Generate(null!));
    }

    [Fact]
    public void MarkdownGenerator_Generate_ContainsCriticalIssues()
    {
        // Arrange
        var report = CreateSampleReport();

        // Act
        var markdown = _markdownGenerator.Generate(report);

        // Assert
        Assert.Contains("🔴", markdown); // Critical 아이콘
        Assert.Contains("[SAFETY-001]", markdown); // 규칙 ID
        Assert.Contains("타입 안전성 검증 누락", markdown); // 제목
    }

    [Fact]
    public void MarkdownGenerator_Generate_ContainsWarningIssues()
    {
        // Arrange
        var report = CreateSampleReport();

        // Act
        var markdown = _markdownGenerator.Generate(report);

        // Assert
        Assert.Contains("🟡", markdown); // Warning 아이콘
        Assert.Contains("[NAMING-001]", markdown); // 규칙 ID
    }

    [Fact]
    public void MarkdownGenerator_Generate_ContainsStatistics()
    {
        // Arrange
        var report = CreateSampleReport();

        // Act
        var markdown = _markdownGenerator.Generate(report);

        // Assert
        Assert.Contains("카테고리별 이슈 분포", markdown);
        Assert.Contains("위반 규칙 TOP 5", markdown);
        Assert.Contains("이슈가 가장 많은 파일 TOP 10", markdown);
    }

    [Fact]
    public void MarkdownGenerator_GenerateToFile_CreatesFile()
    {
        // Arrange
        var report = CreateSampleReport();
        var outputDir = Path.Combine(Path.GetTempPath(), "TwinCatQA_Tests", "Markdown");
        Directory.CreateDirectory(outputDir);
        var outputPath = Path.Combine(outputDir, "test_report.md");

        try
        {
            // Act
            var result = _markdownGenerator.GenerateToFile(report, outputPath);

            // Assert
            Assert.True(File.Exists(result));
            var content = File.ReadAllText(result);
            Assert.Contains("# 🔍 TwinCAT Code QA Report", content);
            Assert.Contains("## 📊 Summary", content);
        }
        finally
        {
            // Cleanup
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public void MarkdownGenerator_GenerateToFile_AutoGeneratesPath()
    {
        // Arrange
        var report = CreateSampleReport();

        string? generatedPath = null;
        try
        {
            // Act
            generatedPath = _markdownGenerator.GenerateToFile(report);

            // Assert
            Assert.NotNull(generatedPath);
            Assert.True(File.Exists(generatedPath));
            Assert.EndsWith(".md", generatedPath);
        }
        finally
        {
            // Cleanup
            if (generatedPath != null && File.Exists(generatedPath))
            {
                File.Delete(generatedPath);
            }
        }
    }

    [Fact]
    public void MarkdownGenerator_Generate_EscapesSpecialCharacters()
    {
        // Arrange
        var report = new QAReport
        {
            GeneratedAt = DateTime.Now,
            SourceFolder = @"C:\Test*Folder",
            TargetFolder = @"C:\Output_Folder",
            TotalChanges = 1,
            CriticalCount = 1,
            WarningCount = 0,
            InfoCount = 0,
            Issues = new List<QAIssue>
            {
                new QAIssue
                {
                    Severity = Severity.Critical,
                    RuleId = "TEST-001",
                    Category = "Test*Category",
                    Title = "Test [Issue] with *special* characters",
                    Description = "Description with `backticks` and _underscores_",
                    FilePath = @"C:\Test\File.st",
                    Line = 1,
                    WhyDangerous = "Dangerous because of # hash",
                    Recommendation = "Use proper escaping"
                }
            }
        };

        // Act
        var markdown = _markdownGenerator.Generate(report);

        // Assert
        Assert.Contains(@"\*", markdown); // 이스케이프된 *
        Assert.Contains(@"\[", markdown); // 이스케이프된 [
        Assert.Contains(@"\`", markdown); // 이스케이프된 `
    }

    #endregion

    #region JSON 리포트 테스트

    [Fact]
    public void JsonGenerator_Generate_ValidReport_ReturnsJson()
    {
        // Arrange
        var report = CreateSampleReport();

        // Act
        var json = _jsonGenerator.Generate(report);

        // Assert
        Assert.NotNull(json);
        Assert.NotEmpty(json);

        // JSON 파싱 검증
        var jsonDoc = JsonDocument.Parse(json);
        Assert.NotNull(jsonDoc.RootElement.GetProperty("metadata"));
        Assert.NotNull(jsonDoc.RootElement.GetProperty("project"));
        Assert.NotNull(jsonDoc.RootElement.GetProperty("summary"));
        Assert.NotNull(jsonDoc.RootElement.GetProperty("statistics"));
        Assert.NotNull(jsonDoc.RootElement.GetProperty("issues"));
    }

    [Fact]
    public void JsonGenerator_Generate_NullReport_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _jsonGenerator.Generate(null!));
    }

    [Fact]
    public void JsonGenerator_Generate_ContainsMetadata()
    {
        // Arrange
        var report = CreateSampleReport();

        // Act
        var json = _jsonGenerator.Generate(report);
        var jsonDoc = JsonDocument.Parse(json);

        // Assert
        var metadata = jsonDoc.RootElement.GetProperty("metadata");
        Assert.Equal("TwinCAT Code QA Tool - JsonReportGenerator", metadata.GetProperty("generator").GetString());
        Assert.Equal("1.0.0", metadata.GetProperty("version").GetString());
    }

    [Fact]
    public void JsonGenerator_Generate_ContainsSummary()
    {
        // Arrange
        var report = CreateSampleReport();

        // Act
        var json = _jsonGenerator.Generate(report);
        var jsonDoc = JsonDocument.Parse(json);

        // Assert
        var summary = jsonDoc.RootElement.GetProperty("summary");
        Assert.Equal(4, summary.GetProperty("totalIssues").GetInt32());
        Assert.True(summary.GetProperty("hasCriticalIssues").GetBoolean());

        var severityCounts = summary.GetProperty("severityCounts");
        Assert.Equal(2, severityCounts.GetProperty("critical").GetInt32());
        Assert.Equal(1, severityCounts.GetProperty("warning").GetInt32());
        Assert.Equal(1, severityCounts.GetProperty("info").GetInt32());
    }

    [Fact]
    public void JsonGenerator_Generate_ContainsStatistics()
    {
        // Arrange
        var report = CreateSampleReport();

        // Act
        var json = _jsonGenerator.Generate(report);
        var jsonDoc = JsonDocument.Parse(json);

        // Assert
        var statistics = jsonDoc.RootElement.GetProperty("statistics");
        Assert.NotNull(statistics.GetProperty("byCategory"));
        Assert.NotNull(statistics.GetProperty("byRule"));
        Assert.NotNull(statistics.GetProperty("byFile"));

        // 카테고리별 통계 확인
        var byCategory = statistics.GetProperty("byCategory");
        Assert.True(byCategory.GetArrayLength() > 0);
    }

    [Fact]
    public void JsonGenerator_Generate_ContainsIssues()
    {
        // Arrange
        var report = CreateSampleReport();

        // Act
        var json = _jsonGenerator.Generate(report);
        var jsonDoc = JsonDocument.Parse(json);

        // Assert
        var issues = jsonDoc.RootElement.GetProperty("issues");
        Assert.Equal(4, issues.GetArrayLength());

        // 첫 번째 이슈 확인
        var firstIssue = issues[0];
        Assert.Equal("SAFETY-001", firstIssue.GetProperty("ruleId").GetString());
        Assert.Equal("Critical", firstIssue.GetProperty("severity").GetString());
        Assert.NotNull(firstIssue.GetProperty("location"));
        Assert.NotNull(firstIssue.GetProperty("details"));
    }

    [Fact]
    public void JsonGenerator_GenerateToFile_CreatesFile()
    {
        // Arrange
        var report = CreateSampleReport();
        var outputDir = Path.Combine(Path.GetTempPath(), "TwinCatQA_Tests", "Json");
        Directory.CreateDirectory(outputDir);
        var outputPath = Path.Combine(outputDir, "test_report.json");

        try
        {
            // Act
            var result = _jsonGenerator.GenerateToFile(report, outputPath);

            // Assert
            Assert.True(File.Exists(result));
            var content = File.ReadAllText(result);
            var jsonDoc = JsonDocument.Parse(content);
            Assert.NotNull(jsonDoc.RootElement.GetProperty("metadata"));
        }
        finally
        {
            // Cleanup
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public async Task JsonGenerator_GenerateToStreamAsync_WritesToStream()
    {
        // Arrange
        var report = CreateSampleReport();

        using var stream = new MemoryStream();

        // Act
        await _jsonGenerator.GenerateToStreamAsync(report, stream);

        // Assert
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();

        Assert.NotEmpty(content);
        var jsonDoc = JsonDocument.Parse(content);
        Assert.NotNull(jsonDoc.RootElement.GetProperty("metadata"));
    }

    [Fact]
    public void JsonGenerator_GenerateSummary_ReturnsSimplifiedJson()
    {
        // Arrange
        var report = CreateSampleReport();

        // Act
        var json = _jsonGenerator.GenerateSummary(report);
        var jsonDoc = JsonDocument.Parse(json);

        // Assert
        Assert.NotNull(jsonDoc.RootElement.GetProperty("generatedAt"));
        Assert.NotNull(jsonDoc.RootElement.GetProperty("totalIssues"));
        Assert.NotNull(jsonDoc.RootElement.GetProperty("severityCounts"));
        Assert.NotNull(jsonDoc.RootElement.GetProperty("topCategories"));

        // issues 배열이 없어야 함 (요약만)
        Assert.False(jsonDoc.RootElement.TryGetProperty("issues", out _));
    }

    #endregion

    #region CI/CD 포맷터 테스트

    [Fact]
    public void CICDFormatter_ToGitHubActionsAnnotations_ReturnsValidFormat()
    {
        // Arrange
        var report = CreateSampleReport();

        // Act
        var annotations = CICDFormatter.ToGitHubActionsAnnotations(report);

        // Assert
        Assert.NotNull(annotations);
        Assert.Contains("::error", annotations); // Critical 이슈
        Assert.Contains("::warning", annotations); // Warning 이슈
        Assert.Contains("::notice", annotations); // Info 이슈
        Assert.Contains("[SAFETY-001]", annotations);
    }

    [Fact]
    public void CICDFormatter_ToAzureDevOpsLog_ReturnsValidFormat()
    {
        // Arrange
        var report = CreateSampleReport();

        // Act
        var log = CICDFormatter.ToAzureDevOpsLog(report);

        // Assert
        Assert.NotNull(log);
        Assert.Contains("##vso[task.logissue type=error", log);
        Assert.Contains("##vso[task.logissue type=warning", log);
        Assert.Contains("sourcepath=", log);
        Assert.Contains("linenumber=", log);
    }

    [Fact]
    public void CICDFormatter_ToJUnitXml_ReturnsValidXml()
    {
        // Arrange
        var report = CreateSampleReport();

        // Act
        var xml = CICDFormatter.ToJUnitXml(report);

        // Assert
        Assert.NotNull(xml);
        Assert.Contains("<?xml version=\"1.0\" encoding=\"UTF-8\"?>", xml);
        Assert.Contains("<testsuite", xml);
        Assert.Contains("<testcase", xml);
        Assert.Contains("<failure", xml);
        Assert.Contains("</testsuite>", xml);

        // 숫자 검증
        Assert.Contains("tests=\"4\"", xml); // 총 4개 이슈
        Assert.Contains("failures=\"3\"", xml); // Critical 2 + Warning 1
        Assert.Contains("skipped=\"1\"", xml); // Info 1
    }

    #endregion

    #region 통합 테스트

    [Fact]
    public void IntegrationTest_GenerateAllFormats_Success()
    {
        // Arrange
        var report = CreateSampleReport();
        var outputDir = Path.Combine(Path.GetTempPath(), "TwinCatQA_Tests", "Integration");
        Directory.CreateDirectory(outputDir);

        var mdPath = Path.Combine(outputDir, "report.md");
        var jsonPath = Path.Combine(outputDir, "report.json");

        try
        {
            // Act - Markdown 생성
            var mdResult = _markdownGenerator.GenerateToFile(report, mdPath);
            Assert.True(File.Exists(mdResult));

            // Act - JSON 생성
            var jsonResult = _jsonGenerator.GenerateToFile(report, jsonPath);
            Assert.True(File.Exists(jsonResult));

            // Act - GitHub Actions 포맷
            var githubAnnotations = CICDFormatter.ToGitHubActionsAnnotations(report);
            Assert.NotEmpty(githubAnnotations);

            // Act - Azure DevOps 포맷
            var azureLog = CICDFormatter.ToAzureDevOpsLog(report);
            Assert.NotEmpty(azureLog);

            // Act - JUnit XML 포맷
            var junitXml = CICDFormatter.ToJUnitXml(report);
            Assert.NotEmpty(junitXml);

            // Assert - 모든 리포트가 정상적으로 생성되었는지 확인
            var mdContent = File.ReadAllText(mdResult);
            Assert.Contains("TwinCAT Code QA Report", mdContent);

            var jsonContent = File.ReadAllText(jsonResult);
            var jsonDoc = JsonDocument.Parse(jsonContent);
            Assert.NotNull(jsonDoc.RootElement);
        }
        finally
        {
            // Cleanup
            if (File.Exists(mdPath)) File.Delete(mdPath);
            if (File.Exists(jsonPath)) File.Delete(jsonPath);
        }
    }

    #endregion

    #region 헬퍼 메서드

    /// <summary>
    /// 샘플 QA 보고서 생성
    /// </summary>
    private static QAReport CreateSampleReport()
    {
        var issues = new List<QAIssue>
        {
            // Critical 이슈 1
            new QAIssue
            {
                Severity = Severity.Critical,
                RuleId = "SAFETY-001",
                Category = "타입 안전성",
                Title = "타입 안전성 검증 누락",
                Description = "ANY 타입 사용으로 인한 타입 안전성 위험",
                FilePath = @"C:\Projects\Test\POUs\MAIN.TcPOU",
                Line = 42,
                Location = @"C:\Projects\Test\POUs\MAIN.TcPOU:42",
                WhyDangerous = "런타임에 예상치 못한 타입 변환 오류가 발생할 수 있습니다.",
                Recommendation = "구체적인 타입(INT, REAL 등)을 명시적으로 사용하세요.",
                OldCodeSnippet = "VAR\n    myVar : ANY;\nEND_VAR",
                NewCodeSnippet = "VAR\n    myVar : INT;  // 구체적 타입 명시\nEND_VAR",
                Examples = new List<string>
                {
                    "// 올바른 예시\nVAR\n    speed : REAL;\n    counter : INT;\nEND_VAR"
                }
            },
            // Critical 이슈 2
            new QAIssue
            {
                Severity = Severity.Critical,
                RuleId = "SAFETY-002",
                Category = "초기화",
                Title = "변수 초기화 누락",
                Description = "중요 변수의 초기값이 설정되지 않음",
                FilePath = @"C:\Projects\Test\POUs\FB_Motor.TcPOU",
                Line = 15,
                Location = @"C:\Projects\Test\POUs\FB_Motor.TcPOU:15",
                WhyDangerous = "초기화되지 않은 변수는 예측 불가능한 값을 가질 수 있습니다.",
                Recommendation = "모든 변수에 초기값을 명시적으로 할당하세요.",
                OldCodeSnippet = "VAR\n    motorSpeed : REAL;\nEND_VAR",
                NewCodeSnippet = "VAR\n    motorSpeed : REAL := 0.0;  // 초기값 설정\nEND_VAR"
            },
            // Warning 이슈
            new QAIssue
            {
                Severity = Severity.Warning,
                RuleId = "NAMING-001",
                Category = "명명 규칙",
                Title = "Function Block 명명 규칙 위반",
                Description = "FB_ 접두사 누락",
                FilePath = @"C:\Projects\Test\POUs\MotorControl.TcPOU",
                Line = 8,
                Location = @"C:\Projects\Test\POUs\MotorControl.TcPOU:8",
                WhyDangerous = "일관되지 않은 명명으로 코드 가독성이 저하됩니다.",
                Recommendation = "Function Block은 FB_ 접두사를 사용하세요.",
                OldCodeSnippet = "FUNCTION_BLOCK MotorControl",
                NewCodeSnippet = "FUNCTION_BLOCK FB_MotorControl"
            },
            // Info 이슈
            new QAIssue
            {
                Severity = Severity.Info,
                RuleId = "DOC-001",
                Category = "문서화",
                Title = "한글 주석 권장",
                Description = "영어 주석 대신 한글 주석 사용 권장",
                FilePath = @"C:\Projects\Test\POUs\MAIN.TcPOU",
                Line = 5,
                Location = @"C:\Projects\Test\POUs\MAIN.TcPOU:5",
                WhyDangerous = "팀 내 커뮤니케이션 효율성이 저하될 수 있습니다.",
                Recommendation = "프로젝트 헌장에 따라 한글 주석을 사용하세요.",
                OldCodeSnippet = "// Initialize motor",
                NewCodeSnippet = "// 모터 초기화"
            }
        };

        return new QAReport
        {
            GeneratedAt = DateTime.Now,
            SourceFolder = @"C:\Projects\Test\Source",
            TargetFolder = @"C:\Projects\Test\Target",
            TotalChanges = 25,
            CriticalCount = 2,
            WarningCount = 1,
            InfoCount = 1,
            Issues = issues
        };
    }

    #endregion
}
