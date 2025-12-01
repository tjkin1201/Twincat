// TODO: DefaultValidationEngine 구현 후 테스트 활성화
// 현재는 QARuleEngine으로 대체됨
#if FALSE  // 임시 비활성화 - DefaultValidationEngine 미구현
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Moq;
using TwinCatQA.Application.Services;
using TwinCatQA.Domain.Contracts;
using TwinCatQA.Domain.Models;
using Xunit;

namespace TwinCatQA.Application.Tests.Services;

/// <summary>
/// DefaultValidationEngine 통합 테스트 (비활성화됨 - QARuleEngine 사용 중)
/// </summary>
public class DefaultValidationEngineTests : IDisposable
{
    private readonly Mock<IParserService> _mockParser;
    private readonly Mock<IReportGenerator> _mockReportGenerator;
    private readonly List<IValidationRule> _rules;
    private readonly DefaultValidationEngine _engine;
    private readonly string _tempDir;
    private readonly string _testProjectPath;

    public DefaultValidationEngineTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"validation_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);

        _testProjectPath = Path.Combine(_tempDir, "TestProject.tsproj");
        File.WriteAllText(_testProjectPath, "<?xml version=\"1.0\"?><TcSmProject></TcSmProject>");

        _mockParser = new Mock<IParserService>();
        _mockReportGenerator = new Mock<IReportGenerator>();
        _rules = new List<IValidationRule>();

        _engine = new DefaultValidationEngine(
            _mockParser.Object,
            _mockReportGenerator.Object,
            _rules);
    }

    #region 세션 관리 테스트

    [Fact]
    public void StartSession_유효한경로_세션생성()
    {
        // Act
        var session = _engine.StartSession(_testProjectPath, ValidationMode.Full);

        // Assert
        session.Should().NotBeNull();
        session.SessionId.Should().NotBeEmpty();
        session.ProjectPath.Should().Be(_testProjectPath);
        session.ProjectName.Should().Be("TestProject");
        session.Mode.Should().Be(ValidationMode.Full);
        session.StartedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void StartSession_빈경로_예외발생()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _engine.StartSession("", ValidationMode.Full));
    }

    [Fact]
    public void StartSession_상대경로_예외발생()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _engine.StartSession("relative\\path\\project.tsproj", ValidationMode.Full));
    }

    [Fact]
    public void StartSession_잘못된확장자_예외발생()
    {
        // Arrange
        var wrongPath = Path.Combine(_tempDir, "test.txt");

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _engine.StartSession(wrongPath, ValidationMode.Full));
    }

    [Fact]
    public void StartSession_존재하지않는디렉토리_예외발생()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_tempDir, "nonexistent", "project.tsproj");

        // Act & Assert
        Assert.Throws<DirectoryNotFoundException>(() =>
            _engine.StartSession(nonExistentPath, ValidationMode.Full));
    }

    [Fact]
    public void CancelSession_유효한세션_정상취소()
    {
        // Arrange
        var session = _engine.StartSession(_testProjectPath, ValidationMode.Full);

        // Act
        _engine.CancelSession(session.SessionId);

        // Assert
        _engine.RunningSessions.Should().NotContain(s => s.SessionId == session.SessionId);
    }

    [Fact]
    public void CancelSession_존재하지않는세션_예외발생()
    {
        // Arrange
        var invalidSessionId = Guid.NewGuid();

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() =>
            _engine.CancelSession(invalidSessionId));
    }

    #endregion

    #region 파일 스캔 테스트

    [Fact]
    public void ScanFiles_유효한프로젝트_파일스캔성공()
    {
        // Arrange
        var session = _engine.StartSession(_testProjectPath, ValidationMode.Full);
        CreateTestFiles();

        // Act
        _engine.ScanFiles(session);

        // Assert
        session.ScannedFiles.Should().NotBeEmpty();
        session.ScannedFiles.Should().Contain(f => f.Type == FileType.POU);
    }

    [Fact]
    public void ScanFiles_여러파일타입_모두감지()
    {
        // Arrange
        var session = _engine.StartSession(_testProjectPath, ValidationMode.Full);
        CreateTestFiles(includePOU: true, includeGVL: true, includeDUT: true);

        // Act
        _engine.ScanFiles(session);

        // Assert
        session.ScannedFiles.Should().Contain(f => f.Type == FileType.POU);
        session.ScannedFiles.Should().Contain(f => f.Type == FileType.GVL);
        session.ScannedFiles.Should().Contain(f => f.Type == FileType.DUT);
    }

    [Fact]
    public void ScanFiles_Null세션_예외발생()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _engine.ScanFiles(null!));
    }

    #endregion

    #region 파일 파싱 테스트

    [Fact]
    public void ParseFiles_정상파일_파싱성공()
    {
        // Arrange
        var session = _engine.StartSession(_testProjectPath, ValidationMode.Full);
        CreateTestFiles();
        _engine.ScanFiles(session);

        _mockParser.Setup(p => p.ParseFile(It.IsAny<string>()))
            .Returns(new object()); // Syntax tree mock

        _mockParser.Setup(p => p.ExtractFunctionBlocks(It.IsAny<object>()))
            .Returns(new List<FunctionBlock>());

        _mockParser.Setup(p => p.ExtractDataTypes(It.IsAny<object>()))
            .Returns(new List<object>());

        _mockParser.Setup(p => p.ExtractVariables(It.IsAny<object>(), It.IsAny<VariableScope>()))
            .Returns(new List<Variable>());

        // Act
        _engine.ParseFiles(session);

        // Assert
        session.ScannedFiles.Should().OnlyContain(f => f.Ast != null);
    }

    [Fact]
    public void ParseFiles_파싱오류_위반사항기록()
    {
        // Arrange
        var session = _engine.StartSession(_testProjectPath, ValidationMode.Full);
        CreateTestFiles();
        _engine.ScanFiles(session);

        _mockParser.Setup(p => p.ParseFile(It.IsAny<string>()))
            .Throws(new Exception("파싱 오류"));

        // Act
        _engine.ParseFiles(session);

        // Assert
        session.Violations.Should().Contain(v => v.RuleId == "PARSE-ERROR");
    }

    [Fact]
    public void ParseFiles_진행률콜백_정상호출()
    {
        // Arrange
        var session = _engine.StartSession(_testProjectPath, ValidationMode.Full);
        CreateTestFiles();
        _engine.ScanFiles(session);

        var progressValues = new List<double>();
        _mockParser.Setup(p => p.ParseFile(It.IsAny<string>()))
            .Returns(new object());

        // Act
        _engine.ParseFiles(session, progress => progressValues.Add(progress));

        // Assert
        progressValues.Should().NotBeEmpty();
        progressValues.Last().Should().Be(1.0);
    }

    #endregion

    #region 검증 실행 테스트

    [Fact]
    public void RunValidation_활성화된규칙_실행성공()
    {
        // Arrange
        var mockRule = new Mock<IValidationRule>();
        mockRule.Setup(r => r.IsEnabled).Returns(true);
        mockRule.Setup(r => r.RuleId).Returns("TEST-001");
        mockRule.Setup(r => r.RuleName).Returns("테스트 규칙");
        mockRule.Setup(r => r.RelatedPrinciple).Returns(ConstitutionPrinciple.None);
        mockRule.Setup(r => r.SupportedLanguages).Returns(new[] { ProgrammingLanguage.ST });
        mockRule.Setup(r => r.Validate(It.IsAny<CodeFile>()))
            .Returns(new List<Violation>
            {
                new Violation
                {
                    RuleId = "TEST-001",
                    RuleName = "테스트 규칙",
                    Severity = ViolationSeverity.Low,
                    Message = "테스트 위반",
                    FilePath = "test.TcPOU",
                    Line = 10
                }
            });

        _rules.Add(mockRule.Object);

        var session = _engine.StartSession(_testProjectPath, ValidationMode.Full);
        CreateTestFiles();
        _engine.ScanFiles(session);

        _mockParser.Setup(p => p.ParseFile(It.IsAny<string>()))
            .Returns(new object());

        _engine.ParseFiles(session);

        // Act
        _engine.RunValidation(session);

        // Assert
        session.Violations.Should().NotBeEmpty();
        session.Violations.Should().Contain(v => v.RuleId == "TEST-001");
    }

    [Fact]
    public void RunValidation_비활성화된규칙_실행안됨()
    {
        // Arrange
        var mockRule = new Mock<IValidationRule>();
        mockRule.Setup(r => r.IsEnabled).Returns(false);
        mockRule.Setup(r => r.Validate(It.IsAny<CodeFile>()))
            .Returns(new List<Violation> { new Violation() });

        _rules.Add(mockRule.Object);

        var session = _engine.StartSession(_testProjectPath, ValidationMode.Full);
        CreateTestFiles();
        _engine.ScanFiles(session);

        // Act
        _engine.RunValidation(session);

        // Assert
        mockRule.Verify(r => r.Validate(It.IsAny<CodeFile>()), Times.Never);
    }

    [Fact]
    public void RunValidation_규칙오류_오류기록후계속()
    {
        // Arrange
        var mockRule = new Mock<IValidationRule>();
        mockRule.Setup(r => r.IsEnabled).Returns(true);
        mockRule.Setup(r => r.RuleId).Returns("TEST-001");
        mockRule.Setup(r => r.RuleName).Returns("테스트 규칙");
        mockRule.Setup(r => r.RelatedPrinciple).Returns(ConstitutionPrinciple.None);
        mockRule.Setup(r => r.SupportedLanguages).Returns(new[] { ProgrammingLanguage.ST });
        mockRule.Setup(r => r.Validate(It.IsAny<CodeFile>()))
            .Throws(new Exception("규칙 실행 오류"));

        _rules.Add(mockRule.Object);

        var session = _engine.StartSession(_testProjectPath, ValidationMode.Full);
        CreateTestFiles();
        _engine.ScanFiles(session);

        _mockParser.Setup(p => p.ParseFile(It.IsAny<string>()))
            .Returns(new object());

        _engine.ParseFiles(session);

        // Act
        _engine.RunValidation(session);

        // Assert
        session.Violations.Should().Contain(v =>
            v.Message.Contains("규칙 실행 중 오류가 발생했습니다"));
    }

    #endregion

    #region 품질 점수 계산 테스트

    [Fact]
    public void CalculateQualityScores_위반없음_만점()
    {
        // Arrange
        var session = _engine.StartSession(_testProjectPath, ValidationMode.Full);
        CreateTestFiles();
        _engine.ScanFiles(session);

        // Act
        _engine.CalculateQualityScores(session);

        // Assert
        session.QualityScore.Should().Be(100.0);
    }

    [Fact]
    public void CalculateQualityScores_심각위반_점수감소()
    {
        // Arrange
        var session = _engine.StartSession(_testProjectPath, ValidationMode.Full);
        CreateTestFiles();
        _engine.ScanFiles(session);

        session.Violations.Add(new Violation
        {
            Severity = ViolationSeverity.Critical,
            FilePath = "test.TcPOU"
        });

        // Act
        _engine.CalculateQualityScores(session);

        // Assert
        session.QualityScore.Should().BeLessThan(100.0);
    }

    [Fact]
    public void CalculateQualityScores_파일별점수_개별계산()
    {
        // Arrange
        var session = _engine.StartSession(_testProjectPath, ValidationMode.Full);
        CreateTestFiles();
        _engine.ScanFiles(session);

        var file = session.ScannedFiles.First();
        file.Violations.Add(new Violation
        {
            Severity = ViolationSeverity.Low,
            FilePath = file.FilePath
        });

        // Act
        _engine.CalculateQualityScores(session);

        // Assert
        file.QualityScore.Should().BeLessThan(100.0);
    }

    #endregion

    #region 리포트 생성 테스트

    [Fact]
    public void GenerateReports_정상세션_리포트생성()
    {
        // Arrange
        var session = _engine.StartSession(_testProjectPath, ValidationMode.Full);
        var htmlPath = Path.Combine(_tempDir, "report.html");

        _mockReportGenerator.Setup(r =>
            r.GenerateHtmlReport(It.IsAny<ValidationSession>(), It.IsAny<string>()))
            .Returns(htmlPath);

        // Act
        _engine.GenerateReports(session);

        // Assert
        session.ReportHtmlPath.Should().NotBeNullOrEmpty();
        _mockReportGenerator.Verify(r =>
            r.GenerateHtmlReport(session, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void GenerateReports_PDF생성실패_HTML만생성()
    {
        // Arrange
        var session = _engine.StartSession(_testProjectPath, ValidationMode.Full);
        var htmlPath = Path.Combine(_tempDir, "report.html");

        _mockReportGenerator.Setup(r =>
            r.GenerateHtmlReport(It.IsAny<ValidationSession>(), It.IsAny<string>()))
            .Returns(htmlPath);

        _mockReportGenerator.Setup(r =>
            r.GeneratePdfReport(It.IsAny<ValidationSession>(), It.IsAny<string>()))
            .Throws(new Exception("PDF 생성 실패"));

        // Act
        _engine.GenerateReports(session);

        // Assert
        session.ReportHtmlPath.Should().NotBeNullOrEmpty();
        session.ReportPdfPath.Should().BeNullOrEmpty();
    }

    #endregion

    #region 세션 완료 테스트

    [Fact]
    public void CompleteSession_정상세션_완료처리()
    {
        // Arrange
        var session = _engine.StartSession(_testProjectPath, ValidationMode.Full);

        // Act
        _engine.CompleteSession(session);

        // Assert
        session.CompletedAt.Should().NotBeNull();
        _engine.RunningSessions.Should().NotContain(session);
    }

    [Fact]
    public void CompleteSession_세션파일저장_정상저장()
    {
        // Arrange
        var session = _engine.StartSession(_testProjectPath, ValidationMode.Full);

        // Act
        _engine.CompleteSession(session);

        // Assert
        var sessionFile = Path.Combine(_tempDir, ".twincat-qa", "sessions", $"{session.SessionId}.json");
        File.Exists(sessionFile).Should().BeTrue();
    }

    #endregion

    #region 통합 워크플로우 테스트

    [Fact]
    public void 전체워크플로우_세션시작부터완료_정상실행()
    {
        // Arrange
        CreateTestFiles();
        var mockRule = CreateMockRule();
        _rules.Add(mockRule);

        _mockParser.Setup(p => p.ParseFile(It.IsAny<string>()))
            .Returns(new object());
        _mockParser.Setup(p => p.ExtractFunctionBlocks(It.IsAny<object>()))
            .Returns(new List<FunctionBlock>());
        _mockParser.Setup(p => p.ExtractDataTypes(It.IsAny<object>()))
            .Returns(new List<object>());
        _mockParser.Setup(p => p.ExtractVariables(It.IsAny<object>(), It.IsAny<VariableScope>()))
            .Returns(new List<Variable>());

        _mockReportGenerator.Setup(r =>
            r.GenerateHtmlReport(It.IsAny<ValidationSession>(), It.IsAny<string>()))
            .Returns(Path.Combine(_tempDir, "report.html"));

        // Act
        var session = _engine.StartSession(_testProjectPath, ValidationMode.Full);
        _engine.ScanFiles(session);
        _engine.ParseFiles(session);
        _engine.RunValidation(session);
        _engine.CalculateQualityScores(session);
        _engine.GenerateReports(session);
        _engine.CompleteSession(session);

        // Assert
        session.CompletedAt.Should().NotBeNull();
        session.QualityScore.Should().BeGreaterThanOrEqualTo(0);
        session.ScannedFiles.Should().NotBeEmpty();
    }

    #endregion

    #region 헬퍼 메서드

    private void CreateTestFiles(bool includePOU = true, bool includeGVL = false, bool includeDUT = false)
    {
        if (includePOU)
        {
            var pouPath = Path.Combine(_tempDir, "Main.TcPOU");
            File.WriteAllText(pouPath, "PROGRAM Main\nEND_PROGRAM");
        }

        if (includeGVL)
        {
            var gvlPath = Path.Combine(_tempDir, "GlobalVars.TcGVL");
            File.WriteAllText(gvlPath, "VAR_GLOBAL\nEND_VAR");
        }

        if (includeDUT)
        {
            var dutPath = Path.Combine(_tempDir, "MyStruct.TcDUT");
            File.WriteAllText(dutPath, "TYPE MyStruct : STRUCT\nEND_STRUCT\nEND_TYPE");
        }
    }

    private IValidationRule CreateMockRule()
    {
        var mockRule = new Mock<IValidationRule>();
        mockRule.Setup(r => r.IsEnabled).Returns(true);
        mockRule.Setup(r => r.RuleId).Returns("TEST-001");
        mockRule.Setup(r => r.RuleName).Returns("테스트 규칙");
        mockRule.Setup(r => r.RelatedPrinciple).Returns(ConstitutionPrinciple.None);
        mockRule.Setup(r => r.SupportedLanguages).Returns(new[] { ProgrammingLanguage.ST });
        mockRule.Setup(r => r.Validate(It.IsAny<CodeFile>()))
            .Returns(new List<Violation>());

        return mockRule.Object;
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try
            {
                Directory.Delete(_tempDir, true);
            }
            catch
            {
                // 테스트 환경에서 파일 삭제 실패는 무시
            }
        }
    }

    #endregion
}
#endif
