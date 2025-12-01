using TwinCatQA.Infrastructure.Analysis.Confidence;
using TwinCatQA.Domain.Models.QA;

namespace TwinCatQA.Examples;

/// <summary>
/// ConfidenceCalculator 사용 예제
/// </summary>
public class ConfidenceCalculatorExample
{
    public static void Main(string[] args)
    {
        Console.WriteLine("=== TwinCAT QA 신뢰도 계산 시스템 예제 ===\n");

        // 예제 1: 높은 신뢰도 - 미사용 변수 (AST로 확실히 확인)
        Example1_HighConfidence();

        Console.WriteLine("\n" + new string('-', 60) + "\n");

        // 예제 2: 중간 신뢰도 - 전역 변수 (다른 곳에서 사용될 가능성)
        Example2_MediumConfidence();

        Console.WriteLine("\n" + new string('-', 60) + "\n");

        // 예제 3: 낮은 신뢰도 - 모호한 컨텍스트
        Example3_LowConfidence();

        Console.WriteLine("\n" + new string('-', 60) + "\n");

        // 예제 4: 통계 계산
        Example4_Statistics();
    }

    /// <summary>
    /// 예제 1: 높은 신뢰도 (90점 이상)
    /// </summary>
    private static void Example1_HighConfidence()
    {
        Console.WriteLine("예제 1: 높은 신뢰도 - 미사용 로컬 변수");

        var astResult = new ASTAnalysisResult
        {
            IsConfirmedByAST = true,        // AST로 확인됨
            DataFlowConfirmed = true,        // 데이터 흐름 분석으로도 확인
            SimilarOccurrences = 5,          // 유사 패턴 5개
            AmbiguousContext = false,        // 컨텍스트 명확
            PossibleExternalReference = false,
            IsInputOutputVariable = false,
            IsGlobalVariable = false
        };

        var issue = new QAIssue
        {
            RuleId = "UNUSED_VARIABLE",
            Severity = Severity.Warning,
            Category = "코드 품질",
            Title = "미사용 변수: tempValue",
            Description = "변수 'tempValue'가 선언되었으나 사용되지 않습니다.",
            Location = "FB_MotorControl.st:45",
            FilePath = "FB_MotorControl.st",
            Line = 45
        };

        var calculator = new ConfidenceCalculator();
        var confidence = calculator.Calculate(astResult, issue);

        PrintResult(confidence);
        // 예상 점수: 50 + 30(AST) + 20(데이터흐름) + 10(유사패턴) = 110 → 100 (최대값)
    }

    /// <summary>
    /// 예제 2: 중간 신뢰도 (60-89점)
    /// </summary>
    private static void Example2_MediumConfidence()
    {
        Console.WriteLine("예제 2: 중간 신뢰도 - 전역 변수");

        var astResult = new ASTAnalysisResult
        {
            IsConfirmedByAST = true,         // AST로 확인됨
            DataFlowConfirmed = false,       // 현재 파일에서는 미사용
            SimilarOccurrences = 1,
            AmbiguousContext = false,
            PossibleExternalReference = false,
            IsInputOutputVariable = false,
            IsGlobalVariable = true          // 전역 변수 (-5점)
        };

        var issue = new QAIssue
        {
            RuleId = "UNUSED_VARIABLE",
            Severity = Severity.Warning,
            Category = "코드 품질",
            Title = "미사용 전역 변수: gSystemStatus",
            Description = "전역 변수 'gSystemStatus'가 현재 파일에서 사용되지 않습니다.",
            Location = "GVL_Main.st:12",
            FilePath = "GVL_Main.st",
            Line = 12
        };

        var calculator = new ConfidenceCalculator();
        var confidence = calculator.Calculate(astResult, issue);

        PrintResult(confidence);
        // 예상 점수: 50 + 30(AST) - 5(전역변수) = 75 (Medium)
    }

    /// <summary>
    /// 예제 3: 낮은 신뢰도 (60점 미만)
    /// </summary>
    private static void Example3_LowConfidence()
    {
        Console.WriteLine("예제 3: 낮은 신뢰도 - 모호한 컨텍스트");

        var astResult = new ASTAnalysisResult
        {
            IsConfirmedByAST = false,        // AST로 확실히 판단 불가
            DataFlowConfirmed = false,
            SimilarOccurrences = 0,
            AmbiguousContext = true,         // 조건부 컴파일 등 (-20점)
            PossibleExternalReference = true, // 외부 참조 가능성 (-15점)
            IsInputOutputVariable = true,    // I/O 변수 (-10점)
            IsGlobalVariable = false
        };

        var issue = new QAIssue
        {
            RuleId = "UNUSED_VARIABLE",
            Severity = Severity.Warning,
            Category = "코드 품질",
            Title = "미사용 가능성: externalData",
            Description = "변수 'externalData'가 현재 컨텍스트에서 사용되지 않으나 외부 참조 가능성이 있습니다.",
            Location = "FB_Interface.st:23",
            FilePath = "FB_Interface.st",
            Line = 23
        };

        astResult.AddNote("조건부 컴파일 블록 내부에 위치");
        astResult.AddNote("외부 라이브러리 인터페이스 변수");

        var calculator = new ConfidenceCalculator();
        var confidence = calculator.Calculate(astResult, issue);

        PrintResult(confidence);
        // 예상 점수: 50 - 20(모호함) - 15(외부참조) - 10(I/O변수) = 5 (Low)
    }

    /// <summary>
    /// 예제 4: 여러 이슈에 대한 통계 계산
    /// </summary>
    private static void Example4_Statistics()
    {
        Console.WriteLine("예제 4: 신뢰도 통계 계산");

        var calculator = new ConfidenceCalculator();
        var confidenceResults = new List<ConfidenceResult>();

        // 여러 이슈 시뮬레이션
        for (int i = 0; i < 100; i++)
        {
            var astResult = CreateRandomASTResult(i);
            var confidence = calculator.Calculate(astResult);
            confidenceResults.Add(confidence);
        }

        // 통계 계산
        var stats = calculator.CalculateStatistics(confidenceResults);

        Console.WriteLine($"\n총 이슈 개수: {stats.TotalIssues}");
        Console.WriteLine($"High 신뢰도: {stats.HighConfidenceCount}개 ({stats.HighConfidencePercentage:F1}%)");
        Console.WriteLine($"Medium 신뢰도: {stats.MediumConfidenceCount}개");
        Console.WriteLine($"Low 신뢰도: {stats.LowConfidenceCount}개");
        Console.WriteLine($"\n평균 점수: {stats.AverageScore}점");
        Console.WriteLine($"최소 점수: {stats.MinScore}점");
        Console.WriteLine($"최대 점수: {stats.MaxScore}점");
        Console.WriteLine($"\nMedium 이상 비율: {stats.MediumOrHighPercentage:F1}%");
    }

    /// <summary>
    /// 랜덤 AST 분석 결과 생성 (테스트용)
    /// </summary>
    private static ASTAnalysisResult CreateRandomASTResult(int seed)
    {
        var random = new Random(seed);

        return new ASTAnalysisResult
        {
            IsConfirmedByAST = random.Next(100) < 70,    // 70% 확률
            DataFlowConfirmed = random.Next(100) < 50,    // 50% 확률
            SimilarOccurrences = random.Next(0, 10),
            AmbiguousContext = random.Next(100) < 20,     // 20% 확률
            PossibleExternalReference = random.Next(100) < 15, // 15% 확률
            IsInputOutputVariable = random.Next(100) < 10,     // 10% 확률
            IsGlobalVariable = random.Next(100) < 25           // 25% 확률
        };
    }

    /// <summary>
    /// 신뢰도 결과 출력
    /// </summary>
    private static void PrintResult(ConfidenceResult confidence)
    {
        Console.WriteLine($"\n신뢰도 점수: {confidence.Score}/100");
        Console.WriteLine($"신뢰도 레벨: {confidence.Level} - {confidence.GetLevelDescription()}");

        if (confidence.ScoreBreakdown.Count > 0)
        {
            Console.WriteLine("\n점수 세부 내역:");
            Console.WriteLine($"  기본 점수: {ConfidenceResult.BaseScore}");
            foreach (var (factor, change) in confidence.ScoreBreakdown.OrderByDescending(x => Math.Abs(x.Value)))
            {
                var sign = change > 0 ? "+" : "";
                Console.WriteLine($"  {factor}: {sign}{change}");
            }
        }

        if (confidence.Reasons.Count > 0)
        {
            Console.WriteLine("\n판단 근거:");
            for (int i = 0; i < confidence.Reasons.Count; i++)
            {
                Console.WriteLine($"  {i + 1}. {confidence.Reasons[i]}");
            }
        }

        Console.WriteLine($"\n조치 권장:");
        if (confidence.IsHighConfidence)
        {
            Console.WriteLine("  ✓ 즉시 검토 및 수정을 권장합니다.");
        }
        else if (confidence.IsMediumOrHighConfidence)
        {
            Console.WriteLine("  ⚠ 코드 리뷰 시 확인이 필요합니다.");
        }
        else
        {
            Console.WriteLine("  ℹ 참고용입니다. 낮은 우선순위로 검토하세요.");
        }

        // 점수 검증
        if (!confidence.ValidateScoreBreakdown())
        {
            Console.WriteLine("\n⚠ 경고: 점수 세부 내역 합계가 일치하지 않습니다!");
        }
    }
}
