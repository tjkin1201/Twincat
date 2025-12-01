using FluentAssertions;
using Moq;
using TwinCatQA.Application.Rules;
using TwinCatQA.Domain.Contracts;
using TwinCatQA.Domain.Models;
using Xunit;

namespace TwinCatQA.Application.Tests.Rules;

/// <summary>
/// 명명 규칙 검증 테스트
/// </summary>
public class NamingConventionRuleTests
{
    private readonly Mock<IParserService> _mockParserService;

    public NamingConventionRuleTests()
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
        var rule = new NamingConventionRule(_mockParserService.Object);

        // Assert
        rule.RuleId.Should().Be("FR-5-NAMING", "규칙 ID가 일치해야 함");
        rule.RuleName.Should().Be("명명 규칙 검증", "규칙 이름이 일치해야 함");
        rule.RelatedPrinciple.Should().Be(ConstitutionPrinciple.NamingConvention,
            "관련 헌장 원칙이 명명 규칙이어야 함");
        rule.DefaultSeverity.Should().Be(ViolationSeverity.Medium,
            "기본 심각도가 Medium이어야 함");
        rule.IsEnabled.Should().BeTrue("기본적으로 활성화되어야 함");
        rule.SupportedLanguages.Should().Contain(new[]
        {
            ProgrammingLanguage.ST,
            ProgrammingLanguage.LD,
            ProgrammingLanguage.FBD,
            ProgrammingLanguage.SFC
        });
    }

    #endregion

    #region Function Block 명명 규칙 테스트

    /// <summary>
    /// 올바른 FB_ 접두사를 가진 Function Block은 위반 없음
    /// </summary>
    [Fact]
    public void Validate_ValidFBName_ShouldReturnNoViolations()
    {
        // Arrange
        var rule = new NamingConventionRule(_mockParserService.Object);

        var syntaxTree = new SyntaxTree
        {
            SourceCode = "FUNCTION_BLOCK FB_MotorControl END_FUNCTION_BLOCK",
            FilePath = @"D:\Test\FB_MotorControl.TcPOU",
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
                Name = "FB_MotorControl", // 올바른 접두사
                Type = FunctionBlockType.FunctionBlock,
                FilePath = syntaxTree.FilePath,
                StartLine = 1,
                EndLine = 1
            }
        };

        _mockParserService
            .Setup(x => x.ExtractFunctionBlocks(It.IsAny<SyntaxTree>()))
            .Returns(functionBlocks);

        _mockParserService
            .Setup(x => x.ExtractVariables(It.IsAny<SyntaxTree>(), It.IsAny<VariableScope?>()))
            .Returns(new List<Variable>());

        // Act
        var violations = rule.Validate(codeFile).ToList();

        // Assert
        violations.Should().BeEmpty("FB_ 접두사는 유효함");
    }

    /// <summary>
    /// 접두사가 없는 Function Block은 위반 생성
    /// </summary>
    [Fact]
    public void Validate_InvalidFBName_ShouldReturnViolation()
    {
        // Arrange
        var rule = new NamingConventionRule(_mockParserService.Object);

        var syntaxTree = new SyntaxTree
        {
            SourceCode = "FUNCTION_BLOCK MotorControl END_FUNCTION_BLOCK",
            FilePath = @"D:\Test\MotorControl.TcPOU",
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
                Name = "MotorControl", // 접두사 없음
                Type = FunctionBlockType.FunctionBlock,
                FilePath = syntaxTree.FilePath,
                StartLine = 1,
                EndLine = 1
            }
        };

        _mockParserService
            .Setup(x => x.ExtractFunctionBlocks(It.IsAny<SyntaxTree>()))
            .Returns(functionBlocks);

        _mockParserService
            .Setup(x => x.ExtractVariables(It.IsAny<SyntaxTree>(), It.IsAny<VariableScope?>()))
            .Returns(new List<Variable>());

        // Act
        var violations = rule.Validate(codeFile).ToList();

        // Assert
        violations.Should().HaveCount(1, "접두사 없는 FB는 위반");

        var violation = violations[0];
        violation.RuleId.Should().Be("FR-5-NAMING");
        violation.Severity.Should().Be(ViolationSeverity.Medium);
        violation.Message.Should().Contain("MotorControl");
        violation.Message.Should().Contain("FB_");
        violation.Suggestion.Should().Contain("FB_MotorControl");
    }

    /// <summary>
    /// FC_ 접두사도 허용됨
    /// </summary>
    [Fact]
    public void Validate_ValidFCPrefix_ShouldPass()
    {
        // Arrange
        var rule = new NamingConventionRule(_mockParserService.Object);

        var syntaxTree = new SyntaxTree
        {
            SourceCode = "FUNCTION FC_Calculate END_FUNCTION",
            FilePath = @"D:\Test\FC_Calculate.TcPOU",
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
                Name = "FC_Calculate", // FC_ 접두사
                Type = FunctionBlockType.Function,
                FilePath = syntaxTree.FilePath,
                StartLine = 1,
                EndLine = 1
            }
        };

        _mockParserService
            .Setup(x => x.ExtractFunctionBlocks(It.IsAny<SyntaxTree>()))
            .Returns(functionBlocks);

        _mockParserService
            .Setup(x => x.ExtractVariables(It.IsAny<SyntaxTree>(), It.IsAny<VariableScope?>()))
            .Returns(new List<Variable>());

        // Act
        var violations = rule.Validate(codeFile).ToList();

        // Assert
        violations.Should().BeEmpty("FC_ 접두사도 유효함");
    }

    /// <summary>
    /// PRG_ 접두사도 허용됨
    /// </summary>
    [Fact]
    public void Validate_ValidPRGPrefix_ShouldPass()
    {
        // Arrange
        var rule = new NamingConventionRule(_mockParserService.Object);

        var syntaxTree = new SyntaxTree
        {
            SourceCode = "PROGRAM PRG_Main END_PROGRAM",
            FilePath = @"D:\Test\PRG_Main.TcPOU",
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
                Name = "PRG_Main", // PRG_ 접두사
                Type = FunctionBlockType.Program,
                FilePath = syntaxTree.FilePath,
                StartLine = 1,
                EndLine = 1
            }
        };

        _mockParserService
            .Setup(x => x.ExtractFunctionBlocks(It.IsAny<SyntaxTree>()))
            .Returns(functionBlocks);

        _mockParserService
            .Setup(x => x.ExtractVariables(It.IsAny<SyntaxTree>(), It.IsAny<VariableScope?>()))
            .Returns(new List<Variable>());

        // Act
        var violations = rule.Validate(codeFile).ToList();

        // Assert
        violations.Should().BeEmpty("PRG_ 접두사도 유효함");
    }

    #endregion

    #region 변수 명명 규칙 테스트

    /// <summary>
    /// 올바른 입력 변수 접두사 (i 또는 in)는 통과
    /// </summary>
    [Fact]
    public void Validate_InputVariableWithCorrectPrefix_ShouldPass()
    {
        // Arrange
        var rule = new NamingConventionRule(_mockParserService.Object);

        var syntaxTree = new SyntaxTree
        {
            SourceCode = "VAR_INPUT iEnable : BOOL; END_VAR",
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
            .Setup(x => x.ExtractFunctionBlocks(It.IsAny<SyntaxTree>()))
            .Returns(new List<FunctionBlock>());

        var variables = new List<Variable>
        {
            new Variable
            {
                Name = "iEnable", // 올바른 입력 변수 접두사
                Scope = VariableScope.Input,
                DataType = "BOOL",
                DeclarationLine = 1
            }
        };

        _mockParserService
            .Setup(x => x.ExtractVariables(It.IsAny<SyntaxTree>(), It.IsAny<VariableScope?>()))
            .Returns(variables);

        // Act
        var violations = rule.Validate(codeFile).ToList();

        // Assert
        violations.Should().BeEmpty("i 접두사는 입력 변수에 유효함");
    }

    /// <summary>
    /// in 접두사도 입력 변수에 허용됨
    /// </summary>
    [Fact]
    public void Validate_InputVariableWithInPrefix_ShouldPass()
    {
        // Arrange
        var rule = new NamingConventionRule(_mockParserService.Object);

        var syntaxTree = new SyntaxTree
        {
            SourceCode = "VAR_INPUT inSpeed : REAL; END_VAR",
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
            .Setup(x => x.ExtractFunctionBlocks(It.IsAny<SyntaxTree>()))
            .Returns(new List<FunctionBlock>());

        var variables = new List<Variable>
        {
            new Variable
            {
                Name = "inSpeed", // in 접두사
                Scope = VariableScope.Input,
                DataType = "REAL",
                DeclarationLine = 1
            }
        };

        _mockParserService
            .Setup(x => x.ExtractVariables(It.IsAny<SyntaxTree>(), It.IsAny<VariableScope?>()))
            .Returns(variables);

        // Act
        var violations = rule.Validate(codeFile).ToList();

        // Assert
        violations.Should().BeEmpty("in 접두사도 입력 변수에 유효함");
    }

    /// <summary>
    /// 잘못된 입력 변수 접두사는 위반 생성
    /// </summary>
    [Fact]
    public void Validate_InputVariableWithWrongPrefix_ShouldReturnViolation()
    {
        // Arrange
        var rule = new NamingConventionRule(_mockParserService.Object);

        var syntaxTree = new SyntaxTree
        {
            SourceCode = "VAR_INPUT Enable : BOOL; END_VAR",
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
            .Setup(x => x.ExtractFunctionBlocks(It.IsAny<SyntaxTree>()))
            .Returns(new List<FunctionBlock>());

        var variables = new List<Variable>
        {
            new Variable
            {
                Name = "Enable", // 접두사 없음
                Scope = VariableScope.Input,
                DataType = "BOOL",
                DeclarationLine = 1
            }
        };

        _mockParserService
            .Setup(x => x.ExtractVariables(It.IsAny<SyntaxTree>(), It.IsAny<VariableScope?>()))
            .Returns(variables);

        // Act
        var violations = rule.Validate(codeFile).ToList();

        // Assert
        violations.Should().HaveCount(1, "접두사 없는 입력 변수는 위반");

        var violation = violations[0];
        violation.Severity.Should().Be(ViolationSeverity.Low);
        violation.Message.Should().Contain("Enable");
        violation.Message.Should().Contain("Input");
        violation.Suggestion.Should().MatchRegex(@"(iEnable|inEnable)", "접두사 제안이 포함되어야 함");
    }

    /// <summary>
    /// 올바른 출력 변수 접두사 (o 또는 out)는 통과
    /// </summary>
    [Fact]
    public void Validate_OutputVariableWithCorrectPrefix_ShouldPass()
    {
        // Arrange
        var rule = new NamingConventionRule(_mockParserService.Object);

        var syntaxTree = new SyntaxTree
        {
            SourceCode = "VAR_OUTPUT oRunning : BOOL; END_VAR",
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
            .Setup(x => x.ExtractFunctionBlocks(It.IsAny<SyntaxTree>()))
            .Returns(new List<FunctionBlock>());

        var variables = new List<Variable>
        {
            new Variable
            {
                Name = "oRunning", // o 접두사
                Scope = VariableScope.Output,
                DataType = "BOOL",
                DeclarationLine = 1
            }
        };

        _mockParserService
            .Setup(x => x.ExtractVariables(It.IsAny<SyntaxTree>(), It.IsAny<VariableScope?>()))
            .Returns(variables);

        // Act
        var violations = rule.Validate(codeFile).ToList();

        // Assert
        violations.Should().BeEmpty("o 접두사는 출력 변수에 유효함");
    }

    /// <summary>
    /// 올바른 전역 변수 접두사 (g)는 통과
    /// </summary>
    [Fact]
    public void Validate_GlobalVariableWithCorrectPrefix_ShouldPass()
    {
        // Arrange
        var rule = new NamingConventionRule(_mockParserService.Object);

        var syntaxTree = new SyntaxTree
        {
            SourceCode = "VAR_GLOBAL gSystemState : INT; END_VAR",
            FilePath = @"D:\Test\GVL_System.TcGVL",
            RootNode = new object()
        };

        var codeFile = new CodeFile
        {
            FilePath = syntaxTree.FilePath,
            Type = FileType.GVL,
            Language = ProgrammingLanguage.ST,
            LineCount = 1,
            Ast = syntaxTree
        };

        _mockParserService
            .Setup(x => x.ExtractFunctionBlocks(It.IsAny<SyntaxTree>()))
            .Returns(new List<FunctionBlock>());

        var variables = new List<Variable>
        {
            new Variable
            {
                Name = "gSystemState", // g 접두사
                Scope = VariableScope.Global,
                DataType = "INT",
                DeclarationLine = 1
            }
        };

        _mockParserService
            .Setup(x => x.ExtractVariables(It.IsAny<SyntaxTree>(), It.IsAny<VariableScope?>()))
            .Returns(variables);

        // Act
        var violations = rule.Validate(codeFile).ToList();

        // Assert
        violations.Should().BeEmpty("g 접두사는 전역 변수에 유효함");
    }

    /// <summary>
    /// 로컬 변수는 카멜케이스 검증
    /// </summary>
    [Fact]
    public void Validate_LocalVariableWithCamelCase_ShouldPass()
    {
        // Arrange
        var rule = new NamingConventionRule(_mockParserService.Object);

        var syntaxTree = new SyntaxTree
        {
            SourceCode = "VAR counter : INT; END_VAR",
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
            .Setup(x => x.ExtractFunctionBlocks(It.IsAny<SyntaxTree>()))
            .Returns(new List<FunctionBlock>());

        var variables = new List<Variable>
        {
            new Variable
            {
                Name = "counter", // 카멜케이스
                Scope = VariableScope.Local,
                DataType = "INT",
                DeclarationLine = 1
            }
        };

        _mockParserService
            .Setup(x => x.ExtractVariables(It.IsAny<SyntaxTree>(), It.IsAny<VariableScope?>()))
            .Returns(variables);

        // Act
        var violations = rule.Validate(codeFile).ToList();

        // Assert
        violations.Should().BeEmpty("카멜케이스 로컬 변수는 유효함");
    }

    #endregion

    #region Configure 메서드 테스트

    /// <summary>
    /// fb_prefix_required 설정 변경이 반영되는지 검증
    /// </summary>
    [Fact]
    public void Configure_DisableFBPrefixRequired_ShouldNotCheckPrefix()
    {
        // Arrange
        var rule = new NamingConventionRule(_mockParserService.Object);

        var syntaxTree = new SyntaxTree
        {
            SourceCode = "FUNCTION_BLOCK MotorControl END_FUNCTION_BLOCK",
            FilePath = @"D:\Test\MotorControl.TcPOU",
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
                Name = "MotorControl", // 접두사 없음
                FilePath = syntaxTree.FilePath,
                StartLine = 1,
                EndLine = 1
            }
        };

        _mockParserService
            .Setup(x => x.ExtractFunctionBlocks(It.IsAny<SyntaxTree>()))
            .Returns(functionBlocks);

        _mockParserService
            .Setup(x => x.ExtractVariables(It.IsAny<SyntaxTree>(), It.IsAny<VariableScope?>()))
            .Returns(new List<Variable>());

        // 접두사 검증 비활성화
        var config = new Dictionary<string, object>
        {
            { "fb_prefix_required", false }
        };

        // Act
        rule.Configure(config);
        var violations = rule.Validate(codeFile).ToList();

        // Assert
        violations.Should().BeEmpty("접두사 검증이 비활성화되면 위반 없음");
    }

    /// <summary>
    /// var_prefix_required 설정 변경이 반영되는지 검증
    /// </summary>
    [Fact]
    public void Configure_DisableVarPrefixRequired_ShouldNotCheckVariablePrefix()
    {
        // Arrange
        var rule = new NamingConventionRule(_mockParserService.Object);

        var syntaxTree = new SyntaxTree
        {
            SourceCode = "VAR_INPUT Enable : BOOL; END_VAR",
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
            .Setup(x => x.ExtractFunctionBlocks(It.IsAny<SyntaxTree>()))
            .Returns(new List<FunctionBlock>());

        var variables = new List<Variable>
        {
            new Variable
            {
                Name = "Enable", // 접두사 없음
                Scope = VariableScope.Input,
                DataType = "BOOL",
                DeclarationLine = 1
            }
        };

        _mockParserService
            .Setup(x => x.ExtractVariables(It.IsAny<SyntaxTree>(), It.IsAny<VariableScope?>()))
            .Returns(variables);

        // 변수 접두사 검증 비활성화
        var config = new Dictionary<string, object>
        {
            { "var_prefix_required", false }
        };

        // Act
        rule.Configure(config);
        var violations = rule.Validate(codeFile).ToList();

        // Assert
        violations.Should().BeEmpty("변수 접두사 검증이 비활성화되면 위반 없음");
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
        var rule = new NamingConventionRule(_mockParserService.Object);

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
        var act = () => new NamingConventionRule(null!);

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
        var rule = new NamingConventionRule(_mockParserService.Object);

        var codeFile = new CodeFile
        {
            FilePath = @"D:\Test\FB_Test.TcPOU",
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
        var rule = new NamingConventionRule(_mockParserService.Object);

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

    #endregion
}
