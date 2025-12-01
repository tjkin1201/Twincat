using TwinCatQA.Domain.Models.QA;

namespace TwinCatQA.Infrastructure.Analysis.Confidence;

/// <summary>
/// QA 이슈의 신뢰도 계산 결과를 나타내는 클래스
/// Domain 레이어의 ConfidenceLevel 열거형을 사용합니다.
/// </summary>
public class ConfidenceResult
{
    /// <summary>
    /// 신뢰도 점수 (0-100)
    /// </summary>
    public int Score { get; set; }

    /// <summary>
    /// 신뢰도 레벨 (High/Medium/Low)
    /// </summary>
    public ConfidenceLevel Level { get; set; }

    /// <summary>
    /// 신뢰도 판단 근거 목록
    /// 각 요소는 점수에 영향을 준 이유를 설명
    /// </summary>
    public List<string> Reasons { get; set; } = new();

    /// <summary>
    /// 점수 계산 세부 내역
    /// Key: 요인 이름, Value: 점수 변화량
    /// </summary>
    public Dictionary<string, int> ScoreBreakdown { get; set; } = new();

    /// <summary>
    /// 기본 점수 (50점)
    /// </summary>
    public const int BaseScore = 50;

    /// <summary>
    /// 기본 생성자
    /// </summary>
    public ConfidenceResult()
    {
        Score = BaseScore;
        Level = ConfidenceLevel.Low;
    }

    /// <summary>
    /// 점수와 레벨을 지정하는 생성자
    /// </summary>
    public ConfidenceResult(int score, ConfidenceLevel level)
    {
        Score = Math.Clamp(score, 0, 100);
        Level = level;
    }

    /// <summary>
    /// 완전한 결과를 생성하는 생성자
    /// </summary>
    public ConfidenceResult(int score, ConfidenceLevel level, List<string> reasons)
    {
        Score = Math.Clamp(score, 0, 100);
        Level = level;
        Reasons = reasons ?? new List<string>();
    }

    /// <summary>
    /// 근거 추가
    /// </summary>
    public void AddReason(string reason)
    {
        if (!string.IsNullOrWhiteSpace(reason))
        {
            Reasons.Add(reason);
        }
    }

    /// <summary>
    /// 점수 세부 내역 추가
    /// </summary>
    public void AddScoreBreakdown(string factor, int scoreChange)
    {
        if (!string.IsNullOrWhiteSpace(factor))
        {
            ScoreBreakdown[factor] = scoreChange;
        }
    }

    /// <summary>
    /// 점수를 기반으로 레벨 자동 결정
    /// 90+ : High
    /// 60-89 : Medium
    /// 60 미만 : Low
    /// </summary>
    public void DetermineLevelFromScore()
    {
        if (Score >= 90)
        {
            Level = ConfidenceLevel.High;
        }
        else if (Score >= 60)
        {
            Level = ConfidenceLevel.Medium;
        }
        else
        {
            Level = ConfidenceLevel.Low;
        }
    }

    /// <summary>
    /// 레벨에 대한 한글 설명 반환
    /// </summary>
    public string GetLevelDescription()
    {
        return Level switch
        {
            ConfidenceLevel.High => "거의 확실한 문제",
            ConfidenceLevel.Medium => "검토 필요",
            ConfidenceLevel.Low => "참고용 (오탐 가능성)",
            _ => "알 수 없음"
        };
    }

    /// <summary>
    /// 신뢰도가 높은지 여부 (90점 이상)
    /// </summary>
    public bool IsHighConfidence => Level == ConfidenceLevel.High;

    /// <summary>
    /// 신뢰도가 중간 이상인지 여부 (60점 이상)
    /// </summary>
    public bool IsMediumOrHighConfidence => Level >= ConfidenceLevel.Medium;

    /// <summary>
    /// 오탐 가능성이 있는지 여부 (60점 미만)
    /// </summary>
    public bool MayBeFalsePositive => Level == ConfidenceLevel.Low;

    /// <summary>
    /// 점수 세부 내역의 합계가 최종 점수와 일치하는지 검증
    /// </summary>
    public bool ValidateScoreBreakdown()
    {
        if (ScoreBreakdown.Count == 0)
            return true; // 세부 내역이 없으면 검증 생략

        int calculatedScore = BaseScore + ScoreBreakdown.Values.Sum();
        return Math.Clamp(calculatedScore, 0, 100) == Score;
    }

    /// <summary>
    /// 사용자 친화적인 요약 정보 반환
    /// </summary>
    public string GetSummary()
    {
        return $"신뢰도 {Score}점 ({GetLevelDescription()})";
    }

    /// <summary>
    /// 디버깅 및 로깅을 위한 상세 문자열 표현
    /// </summary>
    public override string ToString()
    {
        var reasonsText = Reasons.Count > 0
            ? string.Join(", ", Reasons.Take(3)) + (Reasons.Count > 3 ? "..." : "")
            : "근거 없음";

        return $"신뢰도: {Score}점 ({Level}) - {reasonsText}";
    }

    /// <summary>
    /// 상세 리포트 생성 (다중 라인)
    /// </summary>
    public string GetDetailedReport()
    {
        var report = new System.Text.StringBuilder();
        report.AppendLine($"=== 신뢰도 분석 결과 ===");
        report.AppendLine($"점수: {Score}/100");
        report.AppendLine($"레벨: {Level} ({GetLevelDescription()})");

        if (ScoreBreakdown.Count > 0)
        {
            report.AppendLine($"\n점수 세부 내역:");
            report.AppendLine($"  기본 점수: {BaseScore}");
            foreach (var (factor, change) in ScoreBreakdown.OrderByDescending(x => Math.Abs(x.Value)))
            {
                var sign = change > 0 ? "+" : "";
                report.AppendLine($"  {factor}: {sign}{change}");
            }
        }

        if (Reasons.Count > 0)
        {
            report.AppendLine($"\n판단 근거:");
            for (int i = 0; i < Reasons.Count; i++)
            {
                report.AppendLine($"  {i + 1}. {Reasons[i]}");
            }
        }

        return report.ToString();
    }
}
