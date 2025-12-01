using FluentAssertions;
using Moq;
using TwinCatQA.Application.Rules;
using TwinCatQA.Domain.Contracts;
using TwinCatQA.Domain.Models;
using Xunit;

namespace TwinCatQA.Application.Tests.Rules;

/// <summary>
/// 한글 주석 규칙 검증 테스트
/// </summary>
public class KoreanCommentRuleTests
{
    private readonly Mock<IParserService> _mockParserService;

    public KoreanCommentRuleTests()
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
        var rule = new KoreanCommentRule(_mockParserService.Object);

        // Assert
        rule.RuleId.Should().Be("FR-1-KOREAN-COMMENT", "규칙 ID가 일치해야 함");
        rule.RuleName.Should().Be("한글 주석 비율 검증", "규칙 이름이 일치해야 함");
        rule.RelatedPrinciple.Should().Be(ConstitutionPrinciple.KoreanFirst,
            "관련 헌장 원칙이 한글 우선이어야 함");
        rule.DefaultSeverity.Should().Be(ViolationSeverity.High,
            "기본 심각도가 High여야 함");
        rule.IsEnabled.Should().BeTrue("기본적으로 활성화되어야 함");
        rule.SupportedLanguages.Should().Contain(ProgrammingLanguage.ST,
            "ST 언어를 지원해야 함");
    }

    #endregion

    #region 한글 주석 검증 테스트

    /// <summary>
    /// 한글 주석 비율이 기준치를 충족하면 위반 없음
    /// </summary>
    [Fact]
    public void Validate_KoreanComment_ShouldReturnNoViolations()
    {
        // Arrange
        var rule = new KoreanCommentRule(_mockParserService.Object);

        var syntaxTree = new SyntaxTree
        {
            SourceCode = @"
FUNCTION_BLOCK FB_Motor
VAR_INPUT
    iEnable : BOOL; // 모터 활성화 신호
END_VAR

VAR_OUTPUT
    oRunning : BOOL; // 모터 작동 상태
END_VAR

// 모터 제어 로직
IF iEnable THEN
    oRunning := TRUE;
END_IF
END_FUNCTION_BLOCK
",
            FilePath = @"D:\Test\FB_Motor.TcPOU",
            RootNode = new object()
        };

        var codeFile = new CodeFile
        {
            FilePath = syntaxTree.FilePath,
            Type = FileType.POU,
            Language = ProgrammingLanguage.ST,
            LineCount = 15,
            Ast = syntaxTree
        };

        // 한글 주석 설정
        var comments = new Dictionary<int, string>
        {
            { 4, "// 모터 활성화 신호" },
            { 8, "// 모터 작동 상태" },
            { 11, "// 모터 제어 로직" }
        };

        _mockParserService
            .Setup(x => x.ExtractComments(It.IsAny<SyntaxTree>()))
            .Returns(comments);

        // Act
        var violations = rule.Validate(codeFile).ToList();

        // Assert
        violations.Should().BeEmpty("한글 주석은 위반이 아님");
    }

    /// <summary>
    /// 영어 주석이 포함되면 위반 생성
    /// </summary>
    [Fact]
    public void Validate_EnglishComment_ShouldReturnViolation()
    {
        // Arrange
        var rule = new KoreanCommentRule(_mockParserService.Object);

        var syntaxTree = new SyntaxTree
        {
            SourceCode = @"
FUNCTION_BLOCK FB_Motor
VAR_INPUT
    iEnable : BOOL; // Enable motor signal
END_VAR

// Motor control logic
IF iEnable THEN
    // Turn on the motor
END_IF
END_FUNCTION_BLOCK
",
            FilePath = @"D:\Test\FB_Motor.TcPOU",
            RootNode = new object()
        };

        var codeFile = new CodeFile
        {
            FilePath = syntaxTree.FilePath,
            Type = FileType.POU,
            Language = ProgrammingLanguage.ST,
            LineCount = 11,
            Ast = syntaxTree
        };

        // 영어 주석 설정
        var comments = new Dictionary<int, string>
        {
            { 4, "// Enable motor signal" },
            { 7, "// Motor control logic" },
            { 9, "// Turn on the motor" }
        };

        _mockParserService
            .Setup(x => x.ExtractComments(It.IsAny<SyntaxTree>()))
            .Returns(comments);

        // Act
        var violations = rule.Validate(codeFile).ToList();

        // Assert
        violations.Should().NotBeEmpty("영어 주석은 위반임");
        violations.Should().HaveCount(3, "3개의 영어 주석 위반");

        violations.Should().AllSatisfy(v =>
        {
            v.RuleId.Should().Be("FR-1-KOREAN-COMMENT");
            v.Severity.Should().Be(ViolationSeverity.High);
            v.RelatedPrinciple.Should().Be(ConstitutionPrinciple.KoreanFirst);
            v.Message.Should().Contain("한글 비율");
        });
    }

    /// <summary>
    /// 한글과 영어가 혼합된 주석 - 한글 비율 95% 이상이면 통과
    /// </summary>
    [Fact]
    public void Validate_MixedCommentWithHighKoreanRatio_ShouldPass()
    {
        // Arrange
        var rule = new KoreanCommentRule(_mockParserService.Object);

        var syntaxTree = new SyntaxTree
        {
            SourceCode = @"
FUNCTION_BLOCK FB_Motor
VAR_INPUT
    iEnable : BOOL; // 모터 활성화 (Enable)
END_VAR
END_FUNCTION_BLOCK
",
            FilePath = @"D:\Test\FB_Motor.TcPOU",
            RootNode = new object()
        };

        var codeFile = new CodeFile
        {
            FilePath = syntaxTree.FilePath,
            Type = FileType.POU,
            Language = ProgrammingLanguage.ST,
            LineCount = 6,
            Ast = syntaxTree
        };

        // 한글 비율이 높은 혼합 주석
        var comments = new Dictionary<int, string>
        {
            { 4, "// 모터 활성화 신호입니다 (Enable)" } // 한글 비율 > 95%
        };

        _mockParserService
            .Setup(x => x.ExtractComments(It.IsAny<SyntaxTree>()))
            .Returns(comments);

        // Act
        var violations = rule.Validate(codeFile).ToList();

        // Assert
        violations.Should().BeEmpty("한글 비율이 95% 이상이면 통과");
    }

    /// <summary>
    /// 짧은 주석은 검사 제외 (기본 5자 미만)
    /// </summary>
    [Fact]
    public void Validate_ShortComment_ShouldBeIgnored()
    {
        // Arrange
        var rule = new KoreanCommentRule(_mockParserService.Object);

        var syntaxTree = new SyntaxTree
        {
            SourceCode = @"
FUNCTION_BLOCK FB_Motor
VAR_INPUT
    iEnable : BOOL; // OK
END_VAR
END_FUNCTION_BLOCK
",
            FilePath = @"D:\Test\FB_Motor.TcPOU",
            RootNode = new object()
        };

        var codeFile = new CodeFile
        {
            FilePath = syntaxTree.FilePath,
            Type = FileType.POU,
            Language = ProgrammingLanguage.ST,
            LineCount = 6,
            Ast = syntaxTree
        };

        // 짧은 주석
        var comments = new Dictionary<int, string>
        {
            { 4, "// OK" } // 2자 - 검사 제외
        };

        _mockParserService
            .Setup(x => x.ExtractComments(It.IsAny<SyntaxTree>()))
            .Returns(comments);

        // Act
        var violations = rule.Validate(codeFile).ToList();

        // Assert
        violations.Should().BeEmpty("5자 미만 주석은 검사 제외");
    }

    /// <summary>
    /// AST가 null이면 검증하지 않음
    /// </summary>
    [Fact]
    public void Validate_NullAst_ShouldReturnNoViolations()
    {
        // Arrange
        var rule = new KoreanCommentRule(_mockParserService.Object);

        var codeFile = new CodeFile
        {
            FilePath = @"D:\Test\FB_Motor.TcPOU",
            Type = FileType.POU,
            Language = ProgrammingLanguage.ST,
            LineCount = 10,
            Ast = null // AST 없음
        };

        // Act
        var violations = rule.Validate(codeFile).ToList();

        // Assert
        violations.Should().BeEmpty("AST가 없으면 검증 불가능");
        _mockParserService.Verify(
            x => x.ExtractComments(It.IsAny<SyntaxTree>()),
            Times.Never,
            "AST가 없으면 파서 서비스 호출하지 않아야 함");
    }

    /// <summary>
    /// 유효하지 않은 SyntaxTree는 건너뜀
    /// </summary>
    [Fact]
    public void Validate_InvalidSyntaxTree_ShouldReturnNoViolations()
    {
        // Arrange
        var rule = new KoreanCommentRule(_mockParserService.Object);

        var syntaxTree = new SyntaxTree
        {
            SourceCode = "INVALID CODE",
            FilePath = @"D:\Test\Invalid.TcPOU",
            RootNode = null!
        };

        // 파싱 오류 추가
        syntaxTree.Errors.Add(new ParsingError
        {
            Line = 1,
            Column = 0,
            Message = "구문 오류"
        });

        var codeFile = new CodeFile
        {
            FilePath = syntaxTree.FilePath,
            Type = FileType.POU,
            Language = ProgrammingLanguage.ST,
            LineCount = 1,
            Ast = syntaxTree
        };

        // Act
        var violations = rule.Validate(codeFile).ToList();

        // Assert
        violations.Should().BeEmpty("유효하지 않은 구문 트리는 건너뜀");
    }

    #endregion

    #region Configure 메서드 테스트

    /// <summary>
    /// required_korean_ratio 설정 변경이 반영되는지 검증
    /// </summary>
    [Fact]
    public void Configure_ShouldUpdateKoreanRatioThreshold()
    {
        // Arrange
        var rule = new KoreanCommentRule(_mockParserService.Object);

        var syntaxTree = new SyntaxTree
        {
            SourceCode = @"
FUNCTION_BLOCK FB_Test
// 한글과 English 혼합
END_FUNCTION_BLOCK
",
            FilePath = @"D:\Test\FB_Test.TcPOU",
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

        // 한글 비율 약 50%인 주석 (한글과영어혼합 = 6한글 / 12총 = 50%)
        var comments = new Dictionary<int, string>
        {
            { 3, "// 한글과영어혼합test" }
        };

        _mockParserService
            .Setup(x => x.ExtractComments(It.IsAny<SyntaxTree>()))
            .Returns(comments);

        // 임계값을 0.4 (40%)로 낮춤
        var config = new Dictionary<string, object>
        {
            { "required_korean_ratio", 0.4 }
        };

        // Act
        rule.Configure(config);
        var violations = rule.Validate(codeFile).ToList();

        // Assert
        violations.Should().BeEmpty("한글 비율 50%는 임계값 40%를 초과하므로 통과");
    }

    /// <summary>
    /// min_comment_length 설정 변경이 반영되는지 검증
    /// </summary>
    [Fact]
    public void Configure_ShouldUpdateMinCommentLength()
    {
        // Arrange
        var rule = new KoreanCommentRule(_mockParserService.Object);

        var syntaxTree = new SyntaxTree
        {
            SourceCode = @"
FUNCTION_BLOCK FB_Test
// OK
END_FUNCTION_BLOCK
",
            FilePath = @"D:\Test\FB_Test.TcPOU",
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

        // 짧은 영어 주석
        var comments = new Dictionary<int, string>
        {
            { 3, "// OK" } // 2자
        };

        _mockParserService
            .Setup(x => x.ExtractComments(It.IsAny<SyntaxTree>()))
            .Returns(comments);

        // 최소 길이를 2로 낮춤 (기본값 5)
        var config = new Dictionary<string, object>
        {
            { "min_comment_length", 2 }
        };

        // Act
        rule.Configure(config);
        var violations = rule.Validate(codeFile).ToList();

        // Assert
        violations.Should().NotBeEmpty("최소 길이 2로 설정했으므로 'OK' 주석도 검사됨");
    }

    /// <summary>
    /// 잘못된 required_korean_ratio 값은 예외 발생
    /// </summary>
    [Fact]
    public void Configure_InvalidKoreanRatio_ShouldThrowException()
    {
        // Arrange
        var rule = new KoreanCommentRule(_mockParserService.Object);

        var config = new Dictionary<string, object>
        {
            { "required_korean_ratio", 1.5 } // 1.0 초과 - 잘못된 값
        };

        // Act & Assert
        var act = () => rule.Configure(config);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*0.0에서 1.0 사이*")
            .And.Message.Should().Contain("1.5");
    }

    /// <summary>
    /// 잘못된 min_comment_length 값은 예외 발생
    /// </summary>
    [Fact]
    public void Configure_InvalidMinCommentLength_ShouldThrowException()
    {
        // Arrange
        var rule = new KoreanCommentRule(_mockParserService.Object);

        var config = new Dictionary<string, object>
        {
            { "min_comment_length", -5 } // 음수 - 잘못된 값
        };

        // Act & Assert
        var act = () => rule.Configure(config);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*양수*")
            .And.Message.Should().Contain("-5");
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
        var rule = new KoreanCommentRule(_mockParserService.Object);

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
        var act = () => new KoreanCommentRule(null!);

        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("parserService");
    }

    /// <summary>
    /// ExtractComments 호출 시 예외 발생해도 처리됨
    /// </summary>
    [Fact]
    public void Validate_ExtractCommentsThrows_ShouldReturnNoViolations()
    {
        // Arrange
        var rule = new KoreanCommentRule(_mockParserService.Object);

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

        _mockParserService
            .Setup(x => x.ExtractComments(It.IsAny<SyntaxTree>()))
            .Throws(new InvalidOperationException("파싱 오류"));

        // Act
        var violations = rule.Validate(codeFile).ToList();

        // Assert
        violations.Should().BeEmpty("예외 발생 시 빈 리스트 반환");
    }

    #endregion
}
