using FluentAssertions;
using Moq;
using TwinCatQA.Application.Rules;
using TwinCatQA.Domain.Contracts;
using TwinCatQA.Domain.Models;
using Xunit;

namespace TwinCatQA.Application.Tests.Rules;

/// <summary>
/// 사이클로매틱 복잡도 규칙 검증 테스트
/// </summary>
public class CyclomaticComplexityRuleTests
{
    private readonly Mock<IParserService> _mockParserService;

    public CyclomaticComplexityRuleTests()
    {
        _mockParserService = new Mock<IParserService>();
    }

    #region 규칙 속성 테스트

    /// <summary>
    /// 규칙 기본 속성이 올바르게 설정되어 있는지 검증
    /// </summary>
    [Fact]
    public void Rule_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var rule = new CyclomaticComplexityRule(_mockParserService.Object);

        // Assert
        rule.RuleId.Should().Be("FR-4-COMPLEXITY", "규칙 ID가 일치해야 함");
        rule.RuleName.Should().Be("사이클로매틱 복잡도 검증", "규칙 이름이 일치해야 함");
        rule.RelatedPrinciple.Should().Be(ConstitutionPrinciple.StateMachineDesign,
            "관련 헌장 원칙이 상태 기반 설계여야 함");
        rule.DefaultSeverity.Should().Be(ViolationSeverity.Medium,
            "기본 심각도가 Medium이어야 함");
        rule.IsEnabled.Should().BeTrue("기본적으로 활성화되어야 함");
        rule.SupportedLanguages.Should().Contain(ProgrammingLanguage.ST,
            "ST 언어만 지원해야 함");
        rule.SupportedLanguages.Should().HaveCount(1, "ST 언어만 지원");
    }

    #endregion

    #region 복잡도 검증 테스트

    /// <summary>
    /// 복잡도 10 미만이면 위반 없음
    /// </summary>
    [Fact]
    public void Validate_LowComplexity_ShouldReturnNoViolations()
    {
        // Arrange
        var rule = new CyclomaticComplexityRule(_mockParserService.Object);

        var syntaxTree = new SyntaxTree
        {
            SourceCode = @"
FUNCTION_BLOCK FB_Simple
VAR_INPUT
    iEnable : BOOL;
END_VAR

IF iEnable THEN
    // 간단한 로직
END_IF
END_FUNCTION_BLOCK
",
            FilePath = @"D:\Test\FB_Simple.TcPOU",
            RootNode = new object()
        };

        var codeFile = new CodeFile
        {
            FilePath = syntaxTree.FilePath,
            Type = FileType.POU,
            Language = ProgrammingLanguage.ST, // ST 언어
            LineCount = 10,
            Ast = syntaxTree
        };

        var functionBlocks = new List<FunctionBlock>
        {
            new FunctionBlock
            {
                Name = "FB_Simple",
                Type = FunctionBlockType.FunctionBlock,
                FilePath = syntaxTree.FilePath,
                StartLine = 2,
                EndLine = 10,
                LineCount = 9
            }
        };

        _mockParserService
            .Setup(x => x.ExtractFunctionBlocks(It.IsAny<SyntaxTree>()))
            .Returns(functionBlocks);

        _mockParserService
            .Setup(x => x.CalculateCyclomaticComplexity(It.IsAny<FunctionBlock>()))
            .Returns(5); // 복잡도 5

        // Act
        var violations = rule.Validate(codeFile).ToList();

        // Assert
        violations.Should().BeEmpty("복잡도 5는 임계값 10 미만이므로 위반 없음");

        // FunctionBlock에 복잡도가 저장되었는지 확인
        functionBlocks[0].CyclomaticComplexity.Should().Be(5,
            "계산된 복잡도가 FunctionBlock에 저장되어야 함");
    }

    /// <summary>
    /// 복잡도 15 이상이면 High 위반 생성
    /// </summary>
    [Fact]
    public void Validate_HighComplexity_ShouldReturnViolation()
    {
        // Arrange
        var rule = new CyclomaticComplexityRule(_mockParserService.Object);

        var syntaxTree = new SyntaxTree
        {
            SourceCode = @"
FUNCTION_BLOCK FB_Complex
VAR_INPUT
    iMode : INT;
END_VAR

CASE iMode OF
    0: // State 0
    1: // State 1
    2: // State 2
    3: // State 3
    4: // State 4
    5: // State 5
    6: // State 6
    7: // State 7
    8: // State 8
    9: // State 9
    10: // State 10
END_CASE
END_FUNCTION_BLOCK
",
            FilePath = @"D:\Test\FB_Complex.TcPOU",
            RootNode = new object()
        };

        var codeFile = new CodeFile
        {
            FilePath = syntaxTree.FilePath,
            Type = FileType.POU,
            Language = ProgrammingLanguage.ST,
            LineCount = 21,
            Ast = syntaxTree
        };

        var functionBlocks = new List<FunctionBlock>
        {
            new FunctionBlock
            {
                Name = "FB_Complex",
                Type = FunctionBlockType.FunctionBlock,
                FilePath = syntaxTree.FilePath,
                StartLine = 2,
                EndLine = 21,
                LineCount = 20
            }
        };

        _mockParserService
            .Setup(x => x.ExtractFunctionBlocks(It.IsAny<SyntaxTree>()))
            .Returns(functionBlocks);

        _mockParserService
            .Setup(x => x.CalculateCyclomaticComplexity(It.IsAny<FunctionBlock>()))
            .Returns(16); // 복잡도 16 (High)

        // Act
        var violations = rule.Validate(codeFile).ToList();

        // Assert
        violations.Should().HaveCount(1, "복잡도 16은 High 위반");

        var violation = violations[0];
        violation.RuleId.Should().Be("FR-4-COMPLEXITY");
        violation.Severity.Should().Be(ViolationSeverity.High,
            "복잡도 16은 High 심각도 (15 이상)");
        violation.RelatedPrinciple.Should().Be(ConstitutionPrinciple.StateMachineDesign);
        violation.Message.Should().Contain("FB_Complex");
        violation.Message.Should().Contain("16");
        violation.Suggestion.Should().NotBeNullOrEmpty("수정 제안이 제공되어야 함");
    }

    /// <summary>
    /// 복잡도 20 이상이면 Critical 위반 생성
    /// </summary>
    [Fact]
    public void Validate_CriticalComplexity_ShouldReturnCriticalViolation()
    {
        // Arrange
        var rule = new CyclomaticComplexityRule(_mockParserService.Object);

        var syntaxTree = new SyntaxTree
        {
            SourceCode = @"
FUNCTION_BLOCK FB_VeryCom plex
// 매우 복잡한 제어 로직
END_FUNCTION_BLOCK
",
            FilePath = @"D:\Test\FB_VeryComplex.TcPOU",
            RootNode = new object()
        };

        var codeFile = new CodeFile
        {
            FilePath = syntaxTree.FilePath,
            Type = FileType.POU,
            Language = ProgrammingLanguage.ST,
            LineCount = 4,
            Ast = syntaxTree
        };

        var functionBlocks = new List<FunctionBlock>
        {
            new FunctionBlock
            {
                Name = "FB_VeryComplex",
                Type = FunctionBlockType.FunctionBlock,
                FilePath = syntaxTree.FilePath,
                StartLine = 2,
                EndLine = 4,
                LineCount = 3
            }
        };

        _mockParserService
            .Setup(x => x.ExtractFunctionBlocks(It.IsAny<SyntaxTree>()))
            .Returns(functionBlocks);

        _mockParserService
            .Setup(x => x.CalculateCyclomaticComplexity(It.IsAny<FunctionBlock>()))
            .Returns(25); // 복잡도 25 (Critical)

        // Act
        var violations = rule.Validate(codeFile).ToList();

        // Assert
        violations.Should().HaveCount(1, "복잡도 25는 Critical 위반");

        var violation = violations[0];
        violation.Severity.Should().Be(ViolationSeverity.Critical,
            "복잡도 25는 Critical 심각도 (20 이상)");
        violation.Message.Should().Contain("25");
        violation.Suggestion.Should().Contain("CRITICAL",
            "Critical 위반은 긴급 수정 제안 포함");
    }

    /// <summary>
    /// 복잡도 10~14는 Medium 위반 생성
    /// </summary>
    [Fact]
    public void Validate_MediumComplexity_ShouldReturnMediumViolation()
    {
        // Arrange
        var rule = new CyclomaticComplexityRule(_mockParserService.Object);

        var syntaxTree = new SyntaxTree
        {
            SourceCode = "FUNCTION_BLOCK FB_Medium END_FUNCTION_BLOCK",
            FilePath = @"D:\Test\FB_Medium.TcPOU",
            RootNode = new object()
        };

        var codeFile = new CodeFile
        {
            FilePath = syntaxTree.FilePath,
            Type = FileType.POU,
            Language = ProgrammingLanguage.ST,
            LineCount = 1,
            Ast = syntaxTree
        };

        var functionBlocks = new List<FunctionBlock>
        {
            new FunctionBlock
            {
                Name = "FB_Medium",
                Type = FunctionBlockType.FunctionBlock,
                FilePath = syntaxTree.FilePath,
                StartLine = 1,
                EndLine = 1,
                LineCount = 1
            }
        };

        _mockParserService
            .Setup(x => x.ExtractFunctionBlocks(It.IsAny<SyntaxTree>()))
            .Returns(functionBlocks);

        _mockParserService
            .Setup(x => x.CalculateCyclomaticComplexity(It.IsAny<FunctionBlock>()))
            .Returns(12); // 복잡도 12 (Medium)

        // Act
        var violations = rule.Validate(codeFile).ToList();

        // Assert
        violations.Should().HaveCount(1);
        violations[0].Severity.Should().Be(ViolationSeverity.Medium,
            "복잡도 12는 Medium 심각도 (10~14)");
    }

    /// <summary>
    /// 여러 Function Block이 있을 때 각각 검증
    /// </summary>
    [Fact]
    public void Validate_MultipleFunctionBlocks_ShouldValidateEach()
    {
        // Arrange
        var rule = new CyclomaticComplexityRule(_mockParserService.Object);

        var syntaxTree = new SyntaxTree
        {
            SourceCode = "MULTIPLE FBs",
            FilePath = @"D:\Test\Multiple.TcPOU",
            RootNode = new object()
        };

        var codeFile = new CodeFile
        {
            FilePath = syntaxTree.FilePath,
            Type = FileType.POU,
            Language = ProgrammingLanguage.ST,
            LineCount = 1,
            Ast = syntaxTree
        };

        var functionBlocks = new List<FunctionBlock>
        {
            new FunctionBlock { Name = "FB_Simple", FilePath = syntaxTree.FilePath, StartLine = 1, EndLine = 10 },
            new FunctionBlock { Name = "FB_Complex", FilePath = syntaxTree.FilePath, StartLine = 12, EndLine = 50 },
            new FunctionBlock { Name = "FB_Medium", FilePath = syntaxTree.FilePath, StartLine = 52, EndLine = 80 }
        };

        _mockParserService
            .Setup(x => x.ExtractFunctionBlocks(It.IsAny<SyntaxTree>()))
            .Returns(functionBlocks);

        // 각 FB마다 다른 복잡도 반환
        _mockParserService
            .Setup(x => x.CalculateCyclomaticComplexity(It.Is<FunctionBlock>(fb => fb.Name == "FB_Simple")))
            .Returns(5); // 위반 없음

        _mockParserService
            .Setup(x => x.CalculateCyclomaticComplexity(It.Is<FunctionBlock>(fb => fb.Name == "FB_Complex")))
            .Returns(22); // Critical

        _mockParserService
            .Setup(x => x.CalculateCyclomaticComplexity(It.Is<FunctionBlock>(fb => fb.Name == "FB_Medium")))
            .Returns(11); // Medium

        // Act
        var violations = rule.Validate(codeFile).ToList();

        // Assert
        violations.Should().HaveCount(2, "Simple은 통과, Complex와 Medium은 위반");

        violations.Should().Contain(v => v.Message.Contains("FB_Complex") && v.Severity == ViolationSeverity.Critical);
        violations.Should().Contain(v => v.Message.Contains("FB_Medium") && v.Severity == ViolationSeverity.Medium);
    }

    #endregion

    #region 언어 필터링 테스트

    /// <summary>
    /// ST 언어가 아니면 검증하지 않음
    /// </summary>
    [Fact]
    public void Validate_NonSTLanguage_ShouldReturnNoViolations()
    {
        // Arrange
        var rule = new CyclomaticComplexityRule(_mockParserService.Object);

        var syntaxTree = new SyntaxTree
        {
            SourceCode = "LADDER DIAGRAM",
            FilePath = @"D:\Test\Ladder.TcPOU",
            RootNode = new object()
        };

        var codeFile = new CodeFile
        {
            FilePath = syntaxTree.FilePath,
            Type = FileType.POU,
            Language = ProgrammingLanguage.LD, // Ladder Diagram
            LineCount = 1,
            Ast = syntaxTree
        };

        // Act
        var violations = rule.Validate(codeFile).ToList();

        // Assert
        violations.Should().BeEmpty("LD 언어는 지원하지 않으므로 검증하지 않음");

        _mockParserService.Verify(
            x => x.ExtractFunctionBlocks(It.IsAny<SyntaxTree>()),
            Times.Never,
            "ST가 아닌 언어는 파서 호출하지 않아야 함");
    }

    #endregion

    #region Configure 메서드 테스트

    /// <summary>
    /// medium_threshold 설정 변경이 반영되는지 검증
    /// </summary>
    [Fact]
    public void Configure_ShouldUpdateMediumThreshold()
    {
        // Arrange
        var rule = new CyclomaticComplexityRule(_mockParserService.Object);

        var syntaxTree = new SyntaxTree
        {
            SourceCode = "FUNCTION_BLOCK FB_Test END_FUNCTION_BLOCK",
            FilePath = @"D:\Test\FB_Test.TcPOU",
            RootNode = new object()
        };

        var codeFile = new CodeFile
        {
            FilePath = syntaxTree.FilePath,
            Type = FileType.POU,
            Language = ProgrammingLanguage.ST,
            LineCount = 1,
            Ast = syntaxTree
        };

        var functionBlocks = new List<FunctionBlock>
        {
            new FunctionBlock
            {
                Name = "FB_Test",
                FilePath = syntaxTree.FilePath,
                StartLine = 1,
                EndLine = 1
            }
        };

        _mockParserService
            .Setup(x => x.ExtractFunctionBlocks(It.IsAny<SyntaxTree>()))
            .Returns(functionBlocks);

        _mockParserService
            .Setup(x => x.CalculateCyclomaticComplexity(It.IsAny<FunctionBlock>()))
            .Returns(12);

        // medium_threshold를 15로 변경 (기본값 10)
        var config = new Dictionary<string, object>
        {
            { "medium_threshold", 15 }
        };

        // Act
        rule.Configure(config);
        var violations = rule.Validate(codeFile).ToList();

        // Assert
        violations.Should().BeEmpty("복잡도 12는 새 임계값 15 미만이므로 통과");
    }

    /// <summary>
    /// high_threshold와 critical_threshold 설정 변경 검증
    /// </summary>
    [Fact]
    public void Configure_ShouldUpdateAllThresholds()
    {
        // Arrange
        var rule = new CyclomaticComplexityRule(_mockParserService.Object);

        var config = new Dictionary<string, object>
        {
            { "medium_threshold", 20 },
            { "high_threshold", 30 },
            { "critical_threshold", 40 }
        };

        // Act
        rule.Configure(config);

        // 복잡도 25인 Function Block 검증
        var syntaxTree = new SyntaxTree
        {
            SourceCode = "FB",
            FilePath = @"D:\Test\FB.TcPOU",
            RootNode = new object()
        };

        var codeFile = new CodeFile
        {
            FilePath = syntaxTree.FilePath,
            Type = FileType.POU,
            Language = ProgrammingLanguage.ST,
            LineCount = 1,
            Ast = syntaxTree
        };

        var functionBlocks = new List<FunctionBlock>
        {
            new FunctionBlock { Name = "FB_Test", FilePath = syntaxTree.FilePath, StartLine = 1, EndLine = 1 }
        };

        _mockParserService
            .Setup(x => x.ExtractFunctionBlocks(It.IsAny<SyntaxTree>()))
            .Returns(functionBlocks);

        _mockParserService
            .Setup(x => x.CalculateCyclomaticComplexity(It.IsAny<FunctionBlock>()))
            .Returns(25);

        var violations = rule.Validate(codeFile).ToList();

        // Assert
        violations.Should().HaveCount(1);
        violations[0].Severity.Should().Be(ViolationSeverity.Medium,
            "복잡도 25는 새 임계값 기준 Medium (20~29)");
    }

    /// <summary>
    /// 잘못된 임계값 설정은 예외 발생
    /// </summary>
    [Fact]
    public void Configure_InvalidThreshold_ShouldThrowException()
    {
        // Arrange
        var rule = new CyclomaticComplexityRule(_mockParserService.Object);

        // high_threshold가 medium_threshold보다 작음 - 잘못된 설정
        var config = new Dictionary<string, object>
        {
            { "medium_threshold", 20 },
            { "high_threshold", 15 } // 20보다 작음
        };

        // Act & Assert
        var act = () => rule.Configure(config);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*medium_threshold보다 커야*");
    }

    #endregion

    #region 예외 처리 테스트

    /// <summary>
    /// CodeFile이 null이면 ArgumentNullException 발생
    /// </summary>
    [Fact]
    public void Validate_NullCodeFile_ShouldThrowArgumentNullException()
    {
        // Arrange
        var rule = new CyclomaticComplexityRule(_mockParserService.Object);

        // Act & Assert
        var act = () => rule.Validate(null!).ToList();

        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("file");
    }

    /// <summary>
    /// ParserService가 null이면 ArgumentNullException 발생
    /// </summary>
    [Fact]
    public void Constructor_NullParserService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new CyclomaticComplexityRule(null!);

        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("parserService");
    }

    /// <summary>
    /// AST가 null이면 검증하지 않음
    /// </summary>
    [Fact]
    public void Validate_NullAst_ShouldReturnNoViolations()
    {
        // Arrange
        var rule = new CyclomaticComplexityRule(_mockParserService.Object);

        var codeFile = new CodeFile
        {
            FilePath = @"D:\Test\FB.TcPOU",
            Type = FileType.POU,
            Language = ProgrammingLanguage.ST,
            LineCount = 1,
            Ast = null // AST 없음
        };

        // Act
        var violations = rule.Validate(codeFile).ToList();

        // Assert
        violations.Should().BeEmpty("AST가 없으면 검증 불가능");
    }

    /// <summary>
    /// ExtractFunctionBlocks 호출 시 예외 발생해도 처리됨
    /// </summary>
    [Fact]
    public void Validate_ExtractFunctionBlocksThrows_ShouldReturnNoViolations()
    {
        // Arrange
        var rule = new CyclomaticComplexityRule(_mockParserService.Object);

        var syntaxTree = new SyntaxTree
        {
            SourceCode = "FB",
            FilePath = @"D:\Test\FB.TcPOU",
            RootNode = new object()
        };

        var codeFile = new CodeFile
        {
            FilePath = syntaxTree.FilePath,
            Type = FileType.POU,
            Language = ProgrammingLanguage.ST,
            LineCount = 1,
            Ast = syntaxTree
        };

        _mockParserService
            .Setup(x => x.ExtractFunctionBlocks(It.IsAny<SyntaxTree>()))
            .Throws(new InvalidOperationException("파싱 오류"));

        // Act
        var violations = rule.Validate(codeFile).ToList();

        // Assert
        violations.Should().BeEmpty("예외 발생 시 빈 리스트 반환");
    }

    /// <summary>
    /// CalculateCyclomaticComplexity 호출 시 예외 발생하면 해당 FB는 건너뜀
    /// </summary>
    [Fact]
    public void Validate_CalculateComplexityThrows_ShouldSkipFunctionBlock()
    {
        // Arrange
        var rule = new CyclomaticComplexityRule(_mockParserService.Object);

        var syntaxTree = new SyntaxTree
        {
            SourceCode = "FB",
            FilePath = @"D:\Test\FB.TcPOU",
            RootNode = new object()
        };

        var codeFile = new CodeFile
        {
            FilePath = syntaxTree.FilePath,
            Type = FileType.POU,
            Language = ProgrammingLanguage.ST,
            LineCount = 1,
            Ast = syntaxTree
        };

        var functionBlocks = new List<FunctionBlock>
        {
            new FunctionBlock { Name = "FB_Error", FilePath = syntaxTree.FilePath, StartLine = 1, EndLine = 1 },
            new FunctionBlock { Name = "FB_OK", FilePath = syntaxTree.FilePath, StartLine = 3, EndLine = 10 }
        };

        _mockParserService
            .Setup(x => x.ExtractFunctionBlocks(It.IsAny<SyntaxTree>()))
            .Returns(functionBlocks);

        // FB_Error는 예외 발생
        _mockParserService
            .Setup(x => x.CalculateCyclomaticComplexity(It.Is<FunctionBlock>(fb => fb.Name == "FB_Error")))
            .Throws(new InvalidOperationException("복잡도 계산 오류"));

        // FB_OK는 정상 처리
        _mockParserService
            .Setup(x => x.CalculateCyclomaticComplexity(It.Is<FunctionBlock>(fb => fb.Name == "FB_OK")))
            .Returns(15);

        // Act
        var violations = rule.Validate(codeFile).ToList();

        // Assert
        violations.Should().HaveCount(1, "FB_Error는 건너뛰고 FB_OK만 검증");
        violations[0].Message.Should().Contain("FB_OK");
    }

    #endregion
}
