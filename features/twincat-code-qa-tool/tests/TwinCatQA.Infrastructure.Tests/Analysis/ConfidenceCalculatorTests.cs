using Xunit;
using TwinCatQA.Infrastructure.Analysis.Confidence;
using TwinCatQA.Domain.Models.QA;

namespace TwinCatQA.Infrastructure.Tests.Analysis;

/// <summary>
/// ConfidenceCalculator 단위 테스트
/// </summary>
public class ConfidenceCalculatorTests
{
    private readonly ConfidenceCalculator _calculator;

    public ConfidenceCalculatorTests()
    {
        _calculator = new ConfidenceCalculator();
    }

    #region 기본 점수 계산 테스트

    [Fact]
    public void Calculate_기본AST결과_기본점수50점반환()
    {
        // Arrange
        var astResult = new ASTAnalysisResult();

        // Act
        var result = _calculator.Calculate(astResult);

        // Assert
        Assert.Equal(50, result.Score);
        Assert.Equal(ConfidenceLevel.Low, result.Level);
    }

    [Fact]
    public void Calculate_Null입력_낮은신뢰도반환()
    {
        // Act
        var result = _calculator.Calculate(null!);

        // Assert
        Assert.Equal(50, result.Score);
        Assert.Equal(ConfidenceLevel.Low, result.Level);
        Assert.Contains("AST 분석 결과가 없습니다", result.Reasons);
    }

    #endregion

    #region 가점 요소 테스트

    [Fact]
    public void Calculate_AST확인됨_30점가점()
    {
        // Arrange
        var astResult = new ASTAnalysisResult
        {
            IsConfirmedByAST = true
        };

        // Act
        var result = _calculator.Calculate(astResult);

        // Assert
        Assert.Equal(80, result.Score); // 50 + 30
        Assert.Equal(ConfidenceLevel.Medium, result.Level);
        Assert.Contains("AST에서 직접 확인된 이슈입니다", result.Reasons);
    }

    [Fact]
    public void Calculate_데이터흐름확인_20점가점()
    {
        // Arrange
        var astResult = new ASTAnalysisResult
        {
            DataFlowConfirmed = true
        };

        // Act
        var result = _calculator.Calculate(astResult);

        // Assert
        Assert.Equal(70, result.Score); // 50 + 20
        Assert.Equal(ConfidenceLevel.Medium, result.Level);
        Assert.Contains("데이터 흐름 분석을 통해 확인되었습니다", result.Reasons);
    }

    [Fact]
    public void Calculate_유사패턴3회이상_10점가점()
    {
        // Arrange
        var astResult = new ASTAnalysisResult
        {
            SimilarOccurrences = 5
        };

        // Act
        var result = _calculator.Calculate(astResult);

        // Assert
        Assert.Equal(60, result.Score); // 50 + 10
        Assert.Equal(ConfidenceLevel.Medium, result.Level);
        Assert.Contains(result.Reasons, r => r.Contains("5곳에서 발견되었습니다"));
    }

    [Fact]
    public void Calculate_유사패턴3회미만_가점없음()
    {
        // Arrange
        var astResult = new ASTAnalysisResult
        {
            SimilarOccurrences = 2
        };

        // Act
        var result = _calculator.Calculate(astResult);

        // Assert
        Assert.Equal(50, result.Score);
    }

    [Fact]
    public void Calculate_Critical심각도_5점가점()
    {
        // Arrange
        var astResult = new ASTAnalysisResult();
        var issue = new QAIssue
        {
            Severity = Severity.Critical
        };

        // Act
        var result = _calculator.Calculate(astResult, issue);

        // Assert
        Assert.Equal(55, result.Score); // 50 + 5
        Assert.Contains(result.Reasons, r => r.Contains("CRITICAL"));
    }

    #endregion

    #region 감점 요소 테스트

    [Fact]
    public void Calculate_모호한컨텍스트_20점감점()
    {
        // Arrange
        var astResult = new ASTAnalysisResult
        {
            AmbiguousContext = true
        };

        // Act
        var result = _calculator.Calculate(astResult);

        // Assert
        Assert.Equal(30, result.Score); // 50 - 20
        Assert.Equal(ConfidenceLevel.Low, result.Level);
        Assert.Contains(result.Reasons, r => r.Contains("컨텍스트가 모호합니다"));
    }

    [Fact]
    public void Calculate_외부참조가능성_15점감점()
    {
        // Arrange
        var astResult = new ASTAnalysisResult
        {
            PossibleExternalReference = true
        };

        // Act
        var result = _calculator.Calculate(astResult);

        // Assert
        Assert.Equal(35, result.Score); // 50 - 15
        Assert.Contains(result.Reasons, r => r.Contains("외부 라이브러리"));
    }

    [Fact]
    public void Calculate_IO변수_10점감점()
    {
        // Arrange
        var astResult = new ASTAnalysisResult
        {
            IsInputOutputVariable = true
        };

        // Act
        var result = _calculator.Calculate(astResult);

        // Assert
        Assert.Equal(40, result.Score); // 50 - 10
        Assert.Contains(result.Reasons, r => r.Contains("VAR_INPUT") || r.Contains("VAR_OUTPUT"));
    }

    [Fact]
    public void Calculate_전역변수_5점감점()
    {
        // Arrange
        var astResult = new ASTAnalysisResult
        {
            IsGlobalVariable = true
        };

        // Act
        var result = _calculator.Calculate(astResult);

        // Assert
        Assert.Equal(45, result.Score); // 50 - 5
        Assert.Contains(result.Reasons, r => r.Contains("전역 변수"));
    }

    #endregion

    #region 신뢰도 레벨 테스트

    [Fact]
    public void Calculate_90점이상_High레벨()
    {
        // Arrange - 최대 점수
        var astResult = new ASTAnalysisResult
        {
            IsConfirmedByAST = true,      // +30
            DataFlowConfirmed = true,      // +20
            SimilarOccurrences = 5         // +10
        };
        var issue = new QAIssue { Severity = Severity.Critical }; // +5

        // Act
        var result = _calculator.Calculate(astResult, issue);

        // Assert
        Assert.Equal(100, result.Score); // 50 + 30 + 20 + 10 + 5 = 115 → 100 (최대값)
        Assert.Equal(ConfidenceLevel.High, result.Level);
        Assert.True(result.IsHighConfidence);
    }

    [Fact]
    public void Calculate_60_89점_Medium레벨()
    {
        // Arrange
        var astResult = new ASTAnalysisResult
        {
            IsConfirmedByAST = true,       // +30
            IsGlobalVariable = true        // -5
        };

        // Act
        var result = _calculator.Calculate(astResult);

        // Assert
        Assert.Equal(75, result.Score); // 50 + 30 - 5
        Assert.Equal(ConfidenceLevel.Medium, result.Level);
        Assert.True(result.IsMediumOrHighConfidence);
    }

    [Fact]
    public void Calculate_60점미만_Low레벨()
    {
        // Arrange
        var astResult = new ASTAnalysisResult
        {
            AmbiguousContext = true,           // -20
            PossibleExternalReference = true   // -15
        };

        // Act
        var result = _calculator.Calculate(astResult);

        // Assert
        Assert.Equal(15, result.Score); // 50 - 20 - 15
        Assert.Equal(ConfidenceLevel.Low, result.Level);
        Assert.True(result.MayBeFalsePositive);
    }

    [Fact]
    public void Calculate_음수점수_0점으로제한()
    {
        // Arrange - 모든 감점 요소
        var astResult = new ASTAnalysisResult
        {
            AmbiguousContext = true,           // -20
            PossibleExternalReference = true,  // -15
            IsInputOutputVariable = true,      // -10
            IsGlobalVariable = true            // -5
        };

        // Act
        var result = _calculator.Calculate(astResult);

        // Assert
        Assert.Equal(0, result.Score); // 50 - 50 = 0
        Assert.Equal(ConfidenceLevel.Low, result.Level);
    }

    #endregion

    #region 복합 시나리오 테스트

    [Fact]
    public void Calculate_높은신뢰도_미사용로컬변수()
    {
        // Arrange - 전형적인 미사용 로컬 변수
        var astResult = new ASTAnalysisResult
        {
            IsConfirmedByAST = true,
            DataFlowConfirmed = true,
            SimilarOccurrences = 3,
            AmbiguousContext = false,
            PossibleExternalReference = false,
            IsInputOutputVariable = false,
            IsGlobalVariable = false
        };

        // Act
        var result = _calculator.Calculate(astResult);

        // Assert
        Assert.Equal(100, result.Score); // 50 + 30 + 20 + 10 = 110 → 100
        Assert.Equal(ConfidenceLevel.High, result.Level);
    }

    [Fact]
    public void Calculate_중간신뢰도_전역변수()
    {
        // Arrange - 전역 변수지만 AST로 확인됨
        var astResult = new ASTAnalysisResult
        {
            IsConfirmedByAST = true,
            DataFlowConfirmed = false,
            IsGlobalVariable = true
        };

        // Act
        var result = _calculator.Calculate(astResult);

        // Assert
        Assert.Equal(75, result.Score); // 50 + 30 - 5
        Assert.Equal(ConfidenceLevel.Medium, result.Level);
    }

    [Fact]
    public void Calculate_낮은신뢰도_IO변수()
    {
        // Arrange - I/O 변수이면서 외부 참조 가능
        var astResult = new ASTAnalysisResult
        {
            IsConfirmedByAST = false,
            AmbiguousContext = true,
            PossibleExternalReference = true,
            IsInputOutputVariable = true
        };

        // Act
        var result = _calculator.Calculate(astResult);

        // Assert
        Assert.Equal(5, result.Score); // 50 - 20 - 15 - 10
        Assert.Equal(ConfidenceLevel.Low, result.Level);
    }

    #endregion

    #region 점수 세부 내역 테스트

    [Fact]
    public void Calculate_점수세부내역_정확히기록됨()
    {
        // Arrange
        var astResult = new ASTAnalysisResult
        {
            IsConfirmedByAST = true,
            IsGlobalVariable = true
        };

        // Act
        var result = _calculator.Calculate(astResult);

        // Assert
        Assert.Contains("AST 직접 확인", result.ScoreBreakdown.Keys);
        Assert.Contains("전역 변수", result.ScoreBreakdown.Keys);
        Assert.Equal(30, result.ScoreBreakdown["AST 직접 확인"]);
        Assert.Equal(-5, result.ScoreBreakdown["전역 변수"]);
    }

    [Fact]
    public void Calculate_점수검증_세부내역합계일치()
    {
        // Arrange
        var astResult = new ASTAnalysisResult
        {
            IsConfirmedByAST = true,
            DataFlowConfirmed = true,
            IsGlobalVariable = true
        };

        // Act
        var result = _calculator.Calculate(astResult);

        // Assert
        Assert.True(result.ValidateScoreBreakdown());
    }

    #endregion

    #region 간단한 계산 메서드 테스트

    [Fact]
    public void CalculateSimple_AST확인됨_올바른점수()
    {
        // Act
        var result = _calculator.CalculateSimple(
            isConfirmedByAST: true,
            dataFlowConfirmed: false,
            similarOccurrences: 0);

        // Assert
        Assert.Equal(80, result.Score); // 50 + 30
    }

    [Fact]
    public void CalculateSimple_모든요소True_최대점수()
    {
        // Act
        var result = _calculator.CalculateSimple(
            isConfirmedByAST: true,
            dataFlowConfirmed: true,
            similarOccurrences: 5);

        // Assert
        Assert.Equal(100, result.Score); // 50 + 30 + 20 + 10 = 110 → 100
        Assert.Equal(ConfidenceLevel.High, result.Level);
    }

    #endregion

    #region 통합 계산 테스트

    [Fact]
    public void CalculateAggregate_여러결과_최고점수선택()
    {
        // Arrange
        var astResults = new List<ASTAnalysisResult>
        {
            new() { IsConfirmedByAST = true },           // 80점
            new() { DataFlowConfirmed = true },          // 70점
            new() { SimilarOccurrences = 5 }             // 60점
        };

        // Act
        var result = _calculator.CalculateAggregate(astResults);

        // Assert
        Assert.Equal(80, result.Score);
        Assert.Contains(result.Reasons, r => r.Contains("최고 신뢰도 선택"));
    }

    [Fact]
    public void CalculateAggregate_빈리스트_낮은신뢰도반환()
    {
        // Act
        var result = _calculator.CalculateAggregate(new List<ASTAnalysisResult>());

        // Assert
        Assert.Equal(50, result.Score);
        Assert.Equal(ConfidenceLevel.Low, result.Level);
    }

    #endregion

    #region 규칙별 기본 설정 테스트

    [Fact]
    public void CreateDefaultASTResult_미사용변수_높은신뢰도설정()
    {
        // Arrange
        var issue = new QAIssue { RuleId = "UNUSED_VARIABLE" };

        // Act
        var result = _calculator.CreateDefaultASTResult("UNUSED_VARIABLE", issue);

        // Assert
        Assert.True(result.IsConfirmedByAST);
        Assert.True(result.DataFlowConfirmed);
        Assert.False(result.AmbiguousContext);
    }

    [Fact]
    public void CreateDefaultASTResult_배열경계_AST확인설정()
    {
        // Arrange
        var issue = new QAIssue { RuleId = "ARRAY_BOUNDS" };

        // Act
        var result = _calculator.CreateDefaultASTResult("ARRAY_BOUNDS", issue);

        // Assert
        Assert.True(result.IsConfirmedByAST);
        Assert.False(result.AmbiguousContext);
    }

    [Fact]
    public void CreateDefaultASTResult_데드코드_모호함설정()
    {
        // Arrange
        var issue = new QAIssue { RuleId = "DEAD_CODE" };

        // Act
        var result = _calculator.CreateDefaultASTResult("DEAD_CODE", issue);

        // Assert
        Assert.True(result.IsConfirmedByAST);
        Assert.True(result.AmbiguousContext); // 조건부 컴파일 가능성
    }

    [Fact]
    public void CreateDefaultASTResult_알수없는규칙_보수적설정()
    {
        // Arrange
        var issue = new QAIssue { RuleId = "UNKNOWN_RULE" };

        // Act
        var result = _calculator.CreateDefaultASTResult("UNKNOWN_RULE", issue);

        // Assert
        Assert.False(result.IsConfirmedByAST);
        Assert.True(result.AmbiguousContext);
    }

    #endregion

    #region 통계 테스트

    [Fact]
    public void CalculateStatistics_여러결과_정확한통계반환()
    {
        // Arrange
        var results = new List<ConfidenceResult>
        {
            new() { Score = 95, Level = ConfidenceLevel.High },
            new() { Score = 75, Level = ConfidenceLevel.Medium },
            new() { Score = 45, Level = ConfidenceLevel.Low },
            new() { Score = 90, Level = ConfidenceLevel.High }
        };

        // Act
        var stats = _calculator.CalculateStatistics(results);

        // Assert
        Assert.Equal(4, stats.TotalIssues);
        Assert.Equal(2, stats.HighConfidenceCount);
        Assert.Equal(1, stats.MediumConfidenceCount);
        Assert.Equal(1, stats.LowConfidenceCount);
        Assert.Equal(76, stats.AverageScore); // (95+75+45+90)/4 = 76.25 → 76
        Assert.Equal(45, stats.MinScore);
        Assert.Equal(95, stats.MaxScore);
    }

    [Fact]
    public void CalculateStatistics_빈리스트_기본값반환()
    {
        // Act
        var stats = _calculator.CalculateStatistics(new List<ConfidenceResult>());

        // Assert
        Assert.Equal(0, stats.TotalIssues);
        Assert.Equal(0, stats.HighConfidenceCount);
    }

    #endregion

    #region ConfidenceResult 메서드 테스트

    [Fact]
    public void ConfidenceResult_GetSummary_올바른형식()
    {
        // Arrange
        var result = new ConfidenceResult(75, ConfidenceLevel.Medium);

        // Act
        var summary = result.GetSummary();

        // Assert
        Assert.Contains("75점", summary);
        Assert.Contains("검토 필요", summary);
    }

    [Fact]
    public void ConfidenceResult_GetDetailedReport_상세정보포함()
    {
        // Arrange
        var result = new ConfidenceResult(80, ConfidenceLevel.Medium);
        result.AddReason("테스트 근거");
        result.AddScoreBreakdown("AST 확인", 30);

        // Act
        var report = result.GetDetailedReport();

        // Assert
        Assert.Contains("80/100", report);
        Assert.Contains("Medium", report);
        Assert.Contains("테스트 근거", report);
        Assert.Contains("AST 확인", report);
    }

    #endregion

    #region ASTAnalysisResult 메서드 테스트

    [Fact]
    public void ASTAnalysisResult_GetPositiveFactorsCount_정확히계산()
    {
        // Arrange
        var result = new ASTAnalysisResult
        {
            IsConfirmedByAST = true,
            DataFlowConfirmed = true,
            SimilarOccurrences = 5
        };

        // Act
        var count = result.GetPositiveFactorsCount();

        // Assert
        Assert.Equal(3, count);
    }

    [Fact]
    public void ASTAnalysisResult_GetNegativeFactorsCount_정확히계산()
    {
        // Arrange
        var result = new ASTAnalysisResult
        {
            AmbiguousContext = true,
            PossibleExternalReference = true,
            IsGlobalVariable = true
        };

        // Act
        var count = result.GetNegativeFactorsCount();

        // Assert
        Assert.Equal(3, count);
    }

    [Fact]
    public void ASTAnalysisResult_AddNote_노트추가됨()
    {
        // Arrange
        var result = new ASTAnalysisResult();

        // Act
        result.AddNote("테스트 노트");

        // Assert
        Assert.Contains("테스트 노트", result.AnalysisNotes);
    }

    [Fact]
    public void ASTAnalysisResult_AddContext_컨텍스트추가됨()
    {
        // Arrange
        var result = new ASTAnalysisResult();

        // Act
        result.AddContext("TestKey", "TestValue");

        // Assert
        Assert.True(result.AdditionalContext.ContainsKey("TestKey"));
        Assert.Equal("TestValue", result.AdditionalContext["TestKey"]);
    }

    #endregion
}
