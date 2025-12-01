using Xunit;
using TwinCatQA.Infrastructure.Analysis.Confidence;
using TwinCatQA.Infrastructure.Configuration;
using TwinCatQA.Domain.Models.QA;
using System.Text.Json;

namespace TwinCatQA.Infrastructure.Tests.Analysis;

/// <summary>
/// Level 2 컴포넌트 통합 테스트
/// ConfidenceCalculator, EnhancedQAIssue 변환, 설정 파일 처리 등을 테스트
/// </summary>
public class Level2IntegrationTests
{
    private readonly ConfidenceCalculator _confidenceCalculator;

    public Level2IntegrationTests()
    {
        _confidenceCalculator = new ConfidenceCalculator();
    }

    #region ConfidenceCalculator 통합 테스트

    [Fact]
    public void ConfidenceCalculator_AST확인시_신뢰도증가()
    {
        // Arrange
        var astResult = new ASTAnalysisResult
        {
            IsConfirmedByAST = true,
            DataFlowConfirmed = false
        };

        // Act
        var confidence = _confidenceCalculator.Calculate(astResult);

        // Assert
        Assert.Equal(80, confidence.Score); // 50 + 30
        Assert.Equal(ConfidenceLevel.Medium, confidence.Level);
        Assert.Contains(confidence.Reasons, r => r.Contains("AST에서 직접 확인된 이슈입니다"));
    }

    [Fact]
    public void ConfidenceCalculator_전역변수시_신뢰도감소()
    {
        // Arrange
        var astResult = new ASTAnalysisResult
        {
            IsConfirmedByAST = true,
            IsGlobalVariable = true
        };

        // Act
        var confidence = _confidenceCalculator.Calculate(astResult);

        // Assert
        Assert.Equal(75, confidence.Score); // 50 + 30 - 5
        Assert.Equal(ConfidenceLevel.Medium, confidence.Level);
        Assert.Contains(confidence.Reasons, r => r.Contains("전역 변수"));
    }

    [Fact]
    public void ConfidenceCalculator_복합요소계산_정확한점수()
    {
        // Arrange
        var astResult = new ASTAnalysisResult
        {
            IsConfirmedByAST = true,          // +30
            DataFlowConfirmed = true,         // +20
            SimilarOccurrences = 5,           // +10
            IsGlobalVariable = true           // -5
        };
        var issue = new QAIssue
        {
            RuleId = "TEST001",
            Severity = Severity.Critical      // +5
        };

        // Act
        var confidence = _confidenceCalculator.Calculate(astResult, issue);

        // Assert
        Assert.Equal(100, confidence.Score); // 50 + 30 + 20 + 10 + 5 - 5 = 110 -> 100 (최대값)
        Assert.Equal(ConfidenceLevel.High, confidence.Level);
    }

    [Fact]
    public void ConfidenceCalculator_모호한컨텍스트_신뢰도대폭감소()
    {
        // Arrange
        var astResult = new ASTAnalysisResult
        {
            IsConfirmedByAST = true,          // +30
            AmbiguousContext = true,          // -20
            PossibleExternalReference = true  // -15
        };

        // Act
        var confidence = _confidenceCalculator.Calculate(astResult);

        // Assert
        Assert.Equal(45, confidence.Score); // 50 + 30 - 20 - 15
        Assert.Equal(ConfidenceLevel.Low, confidence.Level);
    }

    #endregion

    #region EnhancedQAIssue 변환 테스트

    [Fact]
    public void EnhancedQAIssue_QAIssue변환_모든속성복사됨()
    {
        // Arrange
        var originalIssue = new QAIssue
        {
            RuleId = "SA0001",
            Severity = Severity.Warning,
            Category = "타입 안전성",
            Title = "미사용 변수 탐지",
            Description = "변수 'test'가 선언되었지만 사용되지 않습니다",
            Location = "TestFile.st:10",
            FilePath = "TestFile.st",
            Line = 10,
            WhyDangerous = "메모리 낭비 및 코드 혼란",
            Recommendation = "변수를 제거하거나 사용하세요",
            OldCodeSnippet = "VAR test : INT; END_VAR",
            NewCodeSnippet = "// 변수 제거됨"
        };

        // Act
        var enhanced = EnhancedQAIssue.FromQAIssue(originalIssue);

        // Assert
        Assert.Equal(originalIssue.RuleId, enhanced.RuleId);
        Assert.Equal(originalIssue.Severity, enhanced.Severity);
        Assert.Equal(originalIssue.Category, enhanced.Category);
        Assert.Equal(originalIssue.Title, enhanced.Title);
        Assert.Equal(originalIssue.Description, enhanced.Description);
        Assert.Equal(originalIssue.Location, enhanced.Location);
        Assert.Equal(originalIssue.FilePath, enhanced.FilePath);
        Assert.Equal(originalIssue.Line, enhanced.Line);
        Assert.Equal(1, enhanced.AnalysisLevel); // 기본값
        Assert.Equal(ConfidenceLevel.Medium, enhanced.Confidence); // 기본값
    }

    [Fact]
    public void EnhancedQAIssue_신뢰도정보포함_올바르게저장됨()
    {
        // Arrange
        var issue = new QAIssue { RuleId = "SA0001" };
        var enhanced = EnhancedQAIssue.FromQAIssue(issue);

        // Act
        enhanced.Confidence = ConfidenceLevel.High;
        enhanced.ConfidenceScore = 95;
        enhanced.ConfidenceReasons.Add("AST 분석 확인");
        enhanced.ConfidenceReasons.Add("데이터 흐름 분석 확인");

        // Assert
        Assert.Equal(ConfidenceLevel.High, enhanced.Confidence);
        Assert.Equal(95, enhanced.ConfidenceScore);
        Assert.Equal(2, enhanced.ConfidenceReasons.Count);
        Assert.True(enhanced.IsHighConfidence);
        Assert.True(enhanced.IsMediumOrHighConfidence);
    }

    [Fact]
    public void EnhancedQAIssue_ASTContext설정_올바르게저장됨()
    {
        // Arrange
        var enhanced = EnhancedQAIssue.FromQAIssue(new QAIssue { RuleId = "SA0001" });

        // Act
        enhanced.ASTContext = new ASTAnalysisContext
        {
            IsConfirmedByAST = true,
            DataFlowConfirmed = true,
            VariableScope = "VAR",
            ASTNodeType = "VariableDeclaration",
            DefUseChain = new DefUseInfo
            {
                DefinitionLines = new List<int> { 10 },
                UsageLines = new List<int> { 15, 20 },
                IsInitialized = true,
                DefinedBeforeUse = true
            }
        };
        enhanced.AnalysisLevel = 2;

        // Assert
        Assert.NotNull(enhanced.ASTContext);
        Assert.True(enhanced.ASTContext.IsConfirmedByAST);
        Assert.True(enhanced.ASTContext.DataFlowConfirmed);
        Assert.Equal("VAR", enhanced.ASTContext.VariableScope);
        Assert.Equal(2, enhanced.AnalysisLevel);
        Assert.NotNull(enhanced.ASTContext.DefUseChain);
        Assert.Single(enhanced.ASTContext.DefUseChain.DefinitionLines);
        Assert.Equal(2, enhanced.ASTContext.DefUseChain.UsageLines.Count);
    }

    [Fact]
    public void EnhancedQAIssue_요약문자열생성_올바른형식()
    {
        // Arrange
        var enhanced = EnhancedQAIssue.FromQAIssue(new QAIssue
        {
            RuleId = "SA0001",
            Title = "미사용 변수"
        });
        enhanced.Confidence = ConfidenceLevel.High;
        enhanced.ConfidenceScore = 92;
        enhanced.AnalysisLevel = 2;

        // Act
        var summary = enhanced.GetSummary();

        // Assert
        Assert.Contains("[High]", summary);
        Assert.Contains("[SA0001]", summary);
        Assert.Contains("미사용 변수", summary);
        Assert.Contains("92%", summary);
        Assert.Contains("Level 2", summary);
    }

    [Fact]
    public void EnhancedQAIssue_억제된이슈_요약에표시됨()
    {
        // Arrange
        var enhanced = EnhancedQAIssue.FromQAIssue(new QAIssue { RuleId = "SA0001" });
        enhanced.IsSuppressed = true;
        enhanced.SuppressionReason = "의도된 동작";
        enhanced.SuppressionSource = "inline";

        // Act
        var summary = enhanced.GetSummary();

        // Assert
        Assert.Contains("[억제됨]", summary);
        Assert.True(enhanced.IsSuppressed);
        Assert.Equal("의도된 동작", enhanced.SuppressionReason);
        Assert.Equal("inline", enhanced.SuppressionSource);
    }

    #endregion

    #region 설정 파일 처리 테스트

    [Fact]
    public void QAConfiguration_JSON직렬화_올바르게동작()
    {
        // Arrange
        var config = new QAConfiguration
        {
            Version = "2.0",
            ProjectName = "TestProject",
            GlobalExclusions = new GlobalExclusions
            {
                Files = new List<string> { "**/Generated/**" },
                Rules = new List<string> { "SA0029" }
            },
            RuleOverrides = new Dictionary<string, RuleOverride>
            {
                ["SA0001"] = new RuleOverride
                {
                    Enabled = true,
                    Severity = "Warning"
                }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        var deserialized = JsonSerializer.Deserialize<QAConfiguration>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("2.0", deserialized.Version);
        Assert.Equal("TestProject", deserialized.ProjectName);
        Assert.Single(deserialized.GlobalExclusions.Files);
        Assert.Single(deserialized.GlobalExclusions.Rules);
        Assert.True(deserialized.RuleOverrides.ContainsKey("SA0001"));
    }

    [Fact]
    public void QAConfiguration_규칙필터링_올바르게동작()
    {
        // Arrange
        var config = new QAConfiguration
        {
            GlobalExclusions = new GlobalExclusions
            {
                Rules = new List<string> { "SA0001", "SA0002" }
            },
            RuleOverrides = new Dictionary<string, RuleOverride>
            {
                ["SA0003"] = new RuleOverride { Enabled = false }
            }
        };

        // Act & Assert
        Assert.Contains("SA0001", config.GlobalExclusions.Rules);
        Assert.Contains("SA0002", config.GlobalExclusions.Rules);
        Assert.False(config.RuleOverrides["SA0003"].Enabled);
    }

    [Fact]
    public void QAConfiguration_억제설정_올바르게로드됨()
    {
        // Arrange
        var config = new QAConfiguration
        {
            InlineSuppressions = new InlineSuppressionConfig
            {
                Enabled = true,
                CommentPatterns = new List<string>
                {
                    "// qa-ignore: {ruleId}",
                    "(* qa-ignore: {ruleId} *)"
                },
                WarnOnUnusedSuppressions = true
            }
        };

        // Act & Assert
        Assert.True(config.InlineSuppressions.Enabled);
        Assert.Equal(2, config.InlineSuppressions.CommentPatterns.Count);
        Assert.True(config.InlineSuppressions.WarnOnUnusedSuppressions);
    }

    [Fact]
    public void QAConfiguration_파일오버라이드_올바르게설정됨()
    {
        // Arrange
        var config = new QAConfiguration
        {
            FileOverrides = new Dictionary<string, FileOverride>
            {
                ["**/*.Tests.st"] = new FileOverride
                {
                    MinSeverity = "Warning",
                    StrictMode = false,
                    DisabledRules = new List<string> { "SA0029" }
                }
            }
        };

        // Act
        var fileOverride = config.FileOverrides["**/*.Tests.st"];

        // Assert
        Assert.NotNull(fileOverride);
        Assert.Equal("Warning", fileOverride.MinSeverity);
        Assert.False(fileOverride.StrictMode);
        Assert.Single(fileOverride.DisabledRules);
        Assert.Contains("SA0029", fileOverride.DisabledRules);
    }

    #endregion

    #region 통합 시나리오 테스트

    [Fact]
    public void 통합시나리오_설정파일로규칙필터링()
    {
        // Arrange
        var config = new QAConfiguration
        {
            GlobalExclusions = new GlobalExclusions
            {
                Rules = new List<string> { "SA0029" }
            },
            RuleOverrides = new Dictionary<string, RuleOverride>
            {
                ["SA0001"] = new RuleOverride
                {
                    Enabled = true,
                    Severity = "Critical"
                }
            }
        };

        var issues = new List<QAIssue>
        {
            new() { RuleId = "SA0001", Severity = Severity.Warning },
            new() { RuleId = "SA0029", Severity = Severity.Info },
            new() { RuleId = "SA0033", Severity = Severity.Critical }
        };

        // Act - 전역 제외 규칙 필터링
        var filteredIssues = issues
            .Where(i => !config.GlobalExclusions.Rules.Contains(i.RuleId))
            .ToList();

        // Assert
        Assert.Equal(2, filteredIssues.Count);
        Assert.DoesNotContain(filteredIssues, i => i.RuleId == "SA0029");
        Assert.True(config.RuleOverrides.ContainsKey("SA0001"));
        Assert.Equal("Critical", config.RuleOverrides["SA0001"].Severity);
    }

    [Fact]
    public void 통합시나리오_복수AST분석결과통합()
    {
        // Arrange - 여러 분석 결과
        var astResults = new List<ASTAnalysisResult>
        {
            new()
            {
                IsConfirmedByAST = true,
                DataFlowConfirmed = true
            },
            new()
            {
                IsConfirmedByAST = true,
                SimilarOccurrences = 5,
                IsGlobalVariable = true
            },
            new()
            {
                DataFlowConfirmed = true,
                SimilarOccurrences = 3
            }
        };

        // Act - 통합 신뢰도 계산
        var aggregateConfidence = _confidenceCalculator.CalculateAggregate(astResults);

        // Assert
        Assert.True(aggregateConfidence.Score >= 70); // 최고 점수 선택
        Assert.Contains(aggregateConfidence.Reasons, r => r.Contains("최고 신뢰도 선택"));
    }

    [Fact]
    public void 통합시나리오_EnhancedQAIssue_전체워크플로우()
    {
        // Arrange - 기존 QA 이슈 생성
        var originalIssue = new QAIssue
        {
            RuleId = "SA0001",
            Severity = Severity.Warning,
            Title = "미사용 변수 탐지",
            FilePath = "TestProgram.st",
            Line = 10
        };

        // Act 1: EnhancedQAIssue로 변환
        var enhanced = EnhancedQAIssue.FromQAIssue(originalIssue);

        // Act 2: AST 분석 결과 적용
        var astResult = new ASTAnalysisResult
        {
            IsConfirmedByAST = true,
            DataFlowConfirmed = true,
            SimilarOccurrences = 3
        };

        // Act 3: 신뢰도 계산
        var confidence = _confidenceCalculator.Calculate(astResult);

        // Act 4: 신뢰도 정보 적용
        enhanced.Confidence = confidence.Level;
        enhanced.ConfidenceScore = confidence.Score;
        enhanced.ConfidenceReasons = confidence.Reasons.ToList();
        enhanced.AnalysisLevel = 2;
        enhanced.ASTContext = new ASTAnalysisContext
        {
            IsConfirmedByAST = astResult.IsConfirmedByAST,
            DataFlowConfirmed = astResult.DataFlowConfirmed,
            SimilarOccurrences = astResult.SimilarOccurrences
        };

        // Assert
        Assert.Equal(100, enhanced.ConfidenceScore); // 50 + 30 + 20 + 10 = 110 -> 100
        Assert.Equal(ConfidenceLevel.High, enhanced.Confidence);
        Assert.Equal(2, enhanced.AnalysisLevel);
        Assert.True(enhanced.IsHighConfidence);
        Assert.NotNull(enhanced.ASTContext);
        Assert.True(enhanced.ASTContext.IsConfirmedByAST);
        Assert.True(enhanced.ASTContext.DataFlowConfirmed);
    }

    #endregion
}
