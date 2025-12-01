using TwinCatQA.Domain.Models.QA;

namespace TwinCatQA.Infrastructure.Analysis.Confidence;

/// <summary>
/// QA 이슈의 신뢰도를 계산하는 클래스
/// AST 분석 결과와 컨텍스트를 기반으로 0-100 점수 산출
/// </summary>
public class ConfidenceCalculator
{
    // 점수 가중치 상수
    private const int SCORE_AST_CONFIRMED = 30;           // AST 직접 확인
    private const int SCORE_DATAFLOW_CONFIRMED = 20;      // 데이터 흐름 확인
    private const int SCORE_SIMILAR_OCCURRENCES = 10;     // 유사 패턴 3회 이상
    private const int SCORE_CRITICAL_SEVERITY = 5;        // CRITICAL 심각도

    private const int PENALTY_AMBIGUOUS_CONTEXT = -20;    // 컨텍스트 모호함
    private const int PENALTY_EXTERNAL_REFERENCE = -15;   // 외부 참조 가능성
    private const int PENALTY_IO_VARIABLE = -10;          // VAR_INPUT/VAR_OUTPUT
    private const int PENALTY_GLOBAL_VARIABLE = -5;       // 전역 변수(GVL)

    /// <summary>
    /// AST 분석 결과를 기반으로 신뢰도 계산
    /// </summary>
    /// <param name="astResult">AST 분석 결과</param>
    /// <param name="issue">QA 이슈 (선택사항, 심각도 정보 등 활용)</param>
    /// <returns>신뢰도 계산 결과</returns>
    public ConfidenceResult Calculate(ASTAnalysisResult astResult, QAIssue? issue = null)
    {
        if (astResult == null)
        {
            return CreateLowConfidenceResult("AST 분석 결과가 없습니다");
        }

        var result = new ConfidenceResult
        {
            Score = ConfidenceResult.BaseScore
        };

        int totalScore = ConfidenceResult.BaseScore;

        // 가점 요소 계산
        totalScore = ApplyPositiveFactors(astResult, issue, result, totalScore);

        // 감점 요소 계산
        totalScore = ApplyNegativeFactors(astResult, result, totalScore);

        // 최종 점수 설정 (0-100 범위)
        result.Score = Math.Clamp(totalScore, 0, 100);

        // 레벨 결정
        result.DetermineLevelFromScore();

        // 추가 컨텍스트 정보 반영
        AddContextualReasons(astResult, result);

        return result;
    }

    /// <summary>
    /// 가점 요소 적용
    /// </summary>
    private int ApplyPositiveFactors(ASTAnalysisResult astResult, QAIssue? issue, ConfidenceResult result, int currentScore)
    {
        int score = currentScore;

        // +30: AST에서 직접 확인됨
        if (astResult.IsConfirmedByAST)
        {
            score += SCORE_AST_CONFIRMED;
            result.AddReason("AST에서 직접 확인된 이슈입니다");
            result.AddScoreBreakdown("AST 직접 확인", SCORE_AST_CONFIRMED);
        }

        // +20: 데이터 흐름 분석으로 확인됨
        if (astResult.DataFlowConfirmed)
        {
            score += SCORE_DATAFLOW_CONFIRMED;
            result.AddReason("데이터 흐름 분석을 통해 확인되었습니다");
            result.AddScoreBreakdown("데이터 흐름 확인", SCORE_DATAFLOW_CONFIRMED);
        }

        // +10: 유사 패턴 3곳 이상에서 발견
        if (astResult.SimilarOccurrences >= 3)
        {
            score += SCORE_SIMILAR_OCCURRENCES;
            result.AddReason($"유사한 패턴이 {astResult.SimilarOccurrences}곳에서 발견되었습니다");
            result.AddScoreBreakdown($"유사 패턴 {astResult.SimilarOccurrences}회", SCORE_SIMILAR_OCCURRENCES);
        }

        // +5: CRITICAL 심각도
        if (issue != null && issue.Severity == Severity.Critical)
        {
            score += SCORE_CRITICAL_SEVERITY;
            result.AddReason("심각도가 CRITICAL입니다");
            result.AddScoreBreakdown("CRITICAL 심각도", SCORE_CRITICAL_SEVERITY);
        }

        return score;
    }

    /// <summary>
    /// 감점 요소 적용
    /// </summary>
    private int ApplyNegativeFactors(ASTAnalysisResult astResult, ConfidenceResult result, int currentScore)
    {
        int score = currentScore;

        // -20: 컨텍스트 모호함
        if (astResult.AmbiguousContext)
        {
            score += PENALTY_AMBIGUOUS_CONTEXT;
            result.AddReason("컨텍스트가 모호합니다 (조건부 컴파일, 복잡한 매크로 등)");
            result.AddScoreBreakdown("모호한 컨텍스트", PENALTY_AMBIGUOUS_CONTEXT);
        }

        // -15: 외부 참조 가능성
        if (astResult.PossibleExternalReference)
        {
            score += PENALTY_EXTERNAL_REFERENCE;
            result.AddReason("외부 라이브러리나 링크된 프로젝트에서 사용될 가능성이 있습니다");
            result.AddScoreBreakdown("외부 참조 가능성", PENALTY_EXTERNAL_REFERENCE);
        }

        // -10: VAR_INPUT/VAR_OUTPUT 변수
        if (astResult.IsInputOutputVariable)
        {
            score += PENALTY_IO_VARIABLE;
            result.AddReason("VAR_INPUT 또는 VAR_OUTPUT 변수로 외부에서 사용될 수 있습니다");
            result.AddScoreBreakdown("I/O 변수", PENALTY_IO_VARIABLE);
        }

        // -5: 전역 변수(GVL)
        if (astResult.IsGlobalVariable)
        {
            score += PENALTY_GLOBAL_VARIABLE;
            result.AddReason("전역 변수로 다른 POU에서 사용될 가능성이 있습니다");
            result.AddScoreBreakdown("전역 변수", PENALTY_GLOBAL_VARIABLE);
        }

        return score;
    }

    /// <summary>
    /// 추가 컨텍스트 정보를 근거로 추가
    /// </summary>
    private void AddContextualReasons(ASTAnalysisResult astResult, ConfidenceResult result)
    {
        // 분석 노트가 있으면 추가
        foreach (var note in astResult.AnalysisNotes)
        {
            result.AddReason($"참고: {note}");
        }

        // 추가 컨텍스트 정보 요약
        if (astResult.AdditionalContext.Count > 0)
        {
            var contextInfo = string.Join(", ", astResult.AdditionalContext
                .Take(3)
                .Select(kvp => $"{kvp.Key}={kvp.Value}"));

            if (!string.IsNullOrWhiteSpace(contextInfo))
            {
                result.AddReason($"추가 정보: {contextInfo}");
            }
        }
    }

    /// <summary>
    /// 간단한 신뢰도 계산 (AST 결과만 사용)
    /// </summary>
    public ConfidenceResult CalculateSimple(
        bool isConfirmedByAST,
        bool dataFlowConfirmed = false,
        int similarOccurrences = 0)
    {
        var astResult = new ASTAnalysisResult(
            isConfirmedByAST,
            dataFlowConfirmed,
            similarOccurrences,
            ambiguousContext: false,
            possibleExternalReference: false,
            isInputOutputVariable: false,
            isGlobalVariable: false);

        return Calculate(astResult);
    }

    /// <summary>
    /// 낮은 신뢰도 결과 생성 (기본값)
    /// </summary>
    private ConfidenceResult CreateLowConfidenceResult(string reason)
    {
        var result = new ConfidenceResult
        {
            Score = ConfidenceResult.BaseScore,
            Level = ConfidenceLevel.Low
        };
        result.AddReason(reason);
        return result;
    }

    /// <summary>
    /// 여러 AST 분석 결과를 통합하여 신뢰도 계산
    /// 예: 여러 규칙에서 같은 이슈를 다르게 분석한 경우
    /// </summary>
    public ConfidenceResult CalculateAggregate(List<ASTAnalysisResult> astResults, QAIssue? issue = null)
    {
        if (astResults == null || astResults.Count == 0)
        {
            return CreateLowConfidenceResult("AST 분석 결과가 없습니다");
        }

        // 각 결과에 대해 신뢰도 계산
        var individualResults = astResults
            .Select(ast => Calculate(ast, issue))
            .ToList();

        // 가장 높은 신뢰도를 최종 결과로 선택
        var bestResult = individualResults
            .OrderByDescending(r => r.Score)
            .First();

        // 통합 정보 추가
        bestResult.AddReason($"{astResults.Count}개의 분석 결과 중 최고 신뢰도 선택");

        return bestResult;
    }

    /// <summary>
    /// 규칙 타입별 기본 AST 분석 결과 생성
    /// 규칙 체커가 AST 분석을 수행하지 않는 경우 사용
    /// </summary>
    public ASTAnalysisResult CreateDefaultASTResult(string ruleType, QAIssue issue)
    {
        var result = new ASTAnalysisResult();

        // 규칙 타입에 따라 기본 설정
        switch (ruleType?.ToUpper())
        {
            case "UNUSED_VARIABLE":
                // 미사용 변수: AST 분석으로 확실하게 판단 가능
                result.IsConfirmedByAST = true;
                result.DataFlowConfirmed = true;
                break;

            case "ARRAY_BOUNDS":
                // 배열 경계: 컨텍스트에 따라 다름
                result.IsConfirmedByAST = true;
                result.AmbiguousContext = false;
                break;

            case "MAGIC_NUMBER":
                // 매직 넘버: 쉽게 확인 가능
                result.IsConfirmedByAST = true;
                break;

            case "DEAD_CODE":
                // 데드 코드: 조건부 컴파일 등으로 모호할 수 있음
                result.IsConfirmedByAST = true;
                result.AmbiguousContext = true;
                break;

            default:
                // 기본값: 보수적으로 낮은 신뢰도
                result.IsConfirmedByAST = false;
                result.AmbiguousContext = true;
                break;
        }

        // 이슈 정보에서 추가 정보 추출
        if (issue != null)
        {
            result.AddContext("RuleId", issue.RuleId);
            result.AddContext("Category", issue.Category);
            result.AddContext("Severity", issue.Severity.ToString());
            result.AddContext("Location", issue.Location);
        }

        return result;
    }

    /// <summary>
    /// 신뢰도 통계 계산 (여러 이슈에 대해)
    /// </summary>
    public ConfidenceStatistics CalculateStatistics(List<ConfidenceResult> results)
    {
        if (results == null || results.Count == 0)
        {
            return new ConfidenceStatistics();
        }

        return new ConfidenceStatistics
        {
            TotalIssues = results.Count,
            HighConfidenceCount = results.Count(r => r.Level == ConfidenceLevel.High),
            MediumConfidenceCount = results.Count(r => r.Level == ConfidenceLevel.Medium),
            LowConfidenceCount = results.Count(r => r.Level == ConfidenceLevel.Low),
            AverageScore = (int)results.Average(r => r.Score),
            MinScore = results.Min(r => r.Score),
            MaxScore = results.Max(r => r.Score)
        };
    }
}

/// <summary>
/// 신뢰도 통계 정보
/// </summary>
public class ConfidenceStatistics
{
    public int TotalIssues { get; set; }
    public int HighConfidenceCount { get; set; }
    public int MediumConfidenceCount { get; set; }
    public int LowConfidenceCount { get; set; }
    public int AverageScore { get; set; }
    public int MinScore { get; set; }
    public int MaxScore { get; set; }

    public double HighConfidencePercentage => TotalIssues > 0
        ? (double)HighConfidenceCount / TotalIssues * 100
        : 0;

    public double MediumOrHighPercentage => TotalIssues > 0
        ? (double)(HighConfidenceCount + MediumConfidenceCount) / TotalIssues * 100
        : 0;

    public override string ToString()
    {
        return $"총 {TotalIssues}개 이슈 - High: {HighConfidenceCount}, " +
               $"Medium: {MediumConfidenceCount}, Low: {LowConfidenceCount} " +
               $"(평균: {AverageScore}점)";
    }
}
