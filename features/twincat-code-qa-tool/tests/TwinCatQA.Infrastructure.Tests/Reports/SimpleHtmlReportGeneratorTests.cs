using TwinCatQA.Domain.Contracts;
using TwinCatQA.Domain.Models;
using TwinCatQA.Infrastructure.Reports;
using Xunit;

namespace TwinCatQA.Infrastructure.Tests.Reports;

/// <summary>
/// SimpleHtmlReportGenerator 테스트
/// </summary>
public class SimpleHtmlReportGeneratorTests
{
    private readonly IReportGenerator _generator;

    public SimpleHtmlReportGeneratorTests()
    {
        _generator = new SimpleHtmlReportGenerator();
    }

    [Fact]
    public void GenerateHtmlReport_ValidSession_CreatesFile()
    {
        // Arrange
        var session = CreateSampleSession();
        var outputDir = Path.Combine(Path.GetTempPath(), "TwinCatQA_Tests");
        Directory.CreateDirectory(outputDir);
        var outputPath = Path.Combine(outputDir, "test_report.html");

        // Act
        var result = _generator.GenerateHtmlReport(session, outputPath);

        // Assert
        Assert.True(File.Exists(result));
        var content = File.ReadAllText(result);
        Assert.Contains("TwinCAT 코드 QA 보고서", content);
        Assert.Contains("Critical", content);
        Assert.Contains("TestProject", content);

        // Cleanup
        if (File.Exists(result))
        {
            File.Delete(result);
        }
    }

    [Fact]
    public void GenerateHtmlReport_NullSession_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _generator.GenerateHtmlReport(null!, "test.html"));
    }

    [Fact]
    public void CreateQualityTrendChart_ValidSessions_ReturnsChartData()
    {
        // Arrange
        var sessions = new List<ValidationSession>
        {
            CreateSampleSession(),
            CreateSampleSession()
        };

        // Act
        var chartData = _generator.CreateQualityTrendChart(sessions);

        // Assert
        Assert.NotNull(chartData);
        Assert.Equal("line", chartData.Type);
        Assert.NotEmpty(chartData.Labels);
    }

    [Fact]
    public void CreateConstitutionComplianceChart_ValidSession_ReturnsChartData()
    {
        // Arrange
        var session = CreateSampleSession();

        // Act
        var chartData = _generator.CreateConstitutionComplianceChart(session);

        // Assert
        Assert.NotNull(chartData);
        Assert.Equal("radar", chartData.Type);
    }

    [Fact]
    public void CreateViolationDistributionChart_ValidSession_ReturnsChartData()
    {
        // Arrange
        var session = CreateSampleSession();

        // Act
        var chartData = _generator.CreateViolationDistributionChart(session);

        // Assert
        Assert.NotNull(chartData);
        Assert.Equal("pie", chartData.Type);
    }

    /// <summary>
    /// 샘플 검증 세션 생성
    /// </summary>
    private static ValidationSession CreateSampleSession()
    {
        var session = new ValidationSession
        {
            ProjectName = "TestProject",
            ProjectPath = @"C:\Projects\Test\Test.tsproj",
            Mode = ValidationMode.Full
        };

        // Critical 위반 추가
        session.Violations.Add(new Violation
        {
            RuleId = "SAFETY-001",
            RuleName = "배열 경계 검사 누락",
            Severity = ViolationSeverity.Critical,
            FilePath = @"C:\Projects\Test\POUs\MAIN.TcPOU",
            Line = 42,
            Column = 5,
            Message = "배열 인덱스에 대한 경계 검사가 없습니다. 런타임 오류 가능성이 있습니다.",
            CodeSnippet = "arr[index] := value; // 경계 검사 없음",
            Suggestion = "IF index >= 0 AND index < SIZEOF(arr) THEN\n    arr[index] := value;\nEND_IF",
            RelatedPrinciple = ConstitutionPrinciple.RealTimeSafety
        });

        // High 위반 추가
        session.Violations.Add(new Violation
        {
            RuleId = "NAMING-001",
            RuleName = "명명 규칙 위반",
            Severity = ViolationSeverity.High,
            FilePath = @"C:\Projects\Test\POUs\FB_Motor.TcPOU",
            Line = 15,
            Column = 8,
            Message = "Function Block 이름이 FB_ 접두사를 사용하지 않습니다.",
            CodeSnippet = "FUNCTION_BLOCK MotorControl",
            Suggestion = "FUNCTION_BLOCK FB_MotorControl로 변경하세요.",
            RelatedPrinciple = ConstitutionPrinciple.NamingConvention
        });

        // Medium 위반 추가
        session.Violations.Add(new Violation
        {
            RuleId = "DOC-001",
            RuleName = "한글 주석 누락",
            Severity = ViolationSeverity.Medium,
            FilePath = @"C:\Projects\Test\POUs\FB_Motor.TcPOU",
            Line = 20,
            Column = 1,
            Message = "Function Block에 한글 주석이 없습니다.",
            CodeSnippet = "FUNCTION_BLOCK FB_Motor\n// Missing Korean comment",
            Suggestion = "// 모터 제어 Function Block 주석을 추가하세요.",
            RelatedPrinciple = ConstitutionPrinciple.KoreanFirst
        });

        // Low 위반 추가
        session.Violations.Add(new Violation
        {
            RuleId = "STYLE-001",
            RuleName = "코드 포맷팅",
            Severity = ViolationSeverity.Low,
            FilePath = @"C:\Projects\Test\GVLs\GVL_Config.TcGVL",
            Line = 8,
            Column = 1,
            Message = "일관되지 않은 들여쓰기가 발견되었습니다.",
            CodeSnippet = "VAR_GLOBAL\n  MAX_SPEED : REAL := 100.0;",
            Suggestion = "4칸 또는 탭으로 일관되게 들여쓰기 하세요.",
            RelatedPrinciple = ConstitutionPrinciple.Documentation
        });

        // 헌장 준수율 설정
        session.ConstitutionCompliance[ConstitutionPrinciple.KoreanFirst] = 0.75;
        session.ConstitutionCompliance[ConstitutionPrinciple.RealTimeSafety] = 0.60;
        session.ConstitutionCompliance[ConstitutionPrinciple.Modularity] = 0.85;
        session.ConstitutionCompliance[ConstitutionPrinciple.StateMachineDesign] = 0.90;
        session.ConstitutionCompliance[ConstitutionPrinciple.NamingConvention] = 0.70;
        session.ConstitutionCompliance[ConstitutionPrinciple.Documentation] = 0.65;
        session.ConstitutionCompliance[ConstitutionPrinciple.VersionControl] = 0.95;
        session.ConstitutionCompliance[ConstitutionPrinciple.TestingSimulation] = 0.80;

        // 파일 정보 추가
        session.ScannedFiles.Add(new CodeFile
        {
            FilePath = @"C:\Projects\Test\POUs\MAIN.TcPOU",
            Language = ProgrammingLanguage.ST,
            LineCount = 150
        });

        session.ScannedFiles.Add(new CodeFile
        {
            FilePath = @"C:\Projects\Test\POUs\FB_Motor.TcPOU",
            Language = ProgrammingLanguage.ST,
            LineCount = 200
        });

        session.Complete();
        session.CalculateQualityScore();
        session.CalculateConstitutionCompliance();

        return session;
    }
}
