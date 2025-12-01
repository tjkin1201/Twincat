using TwinCatQA.Domain.Contracts;

namespace TwinCatQA.Domain.Models;

/// <summary>
/// 한 번의 검증 실행을 나타내는 엔티티 (집계 루트)
/// </summary>
public class ValidationSession
{
    #region 식별자 및 시간

    /// <summary>
    /// 세션 고유 식별자
    /// </summary>
    public Guid SessionId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// 검증 시작 시각
    /// </summary>
    public DateTime StartTime { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// 검증 종료 시각
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 검증 소요 시간
    /// </summary>
    public TimeSpan Duration
    {
        get => EndTime.HasValue
            ? EndTime.Value - StartTime
            : DateTime.UtcNow - StartTime;
    }

    #endregion

    #region 프로젝트 정보

    /// <summary>
    /// .tsproj 파일의 절대 경로
    /// </summary>
    public string ProjectPath { get; init; } = string.Empty;

    /// <summary>
    /// 프로젝트 이름
    /// </summary>
    public string ProjectName { get; init; } = string.Empty;

    #endregion

    #region 검증 모드

    /// <summary>
    /// 검증 모드 (Full, Incremental, FileSpecific)
    /// </summary>
    public ValidationMode Mode { get; init; }

    /// <summary>
    /// Git 커밋 해시 (증분 검증 시 기준 커밋)
    /// </summary>
    public string? GitCommitHash { get; init; }

    #endregion

    #region 통계

    /// <summary>
    /// 스캔된 파일 수
    /// </summary>
    public int ScannedFilesCount { get; set; }

    /// <summary>
    /// 총 코드 라인 수
    /// </summary>
    public int TotalLinesOfCode { get; set; }

    /// <summary>
    /// 총 위반 사항 수
    /// </summary>
    public int ViolationsCount => Violations.Count;

    /// <summary>
    /// 심각도별 위반 사항 수
    /// </summary>
    public Dictionary<ViolationSeverity, int> ViolationsBySeverity
    {
        get
        {
            return Violations
                .GroupBy(v => v.Severity)
                .ToDictionary(g => g.Key, g => g.Count());
        }
    }

    #endregion

    #region 품질 점수

    /// <summary>
    /// 전체 품질 점수 (0-100)
    /// </summary>
    public double OverallQualityScore { get; set; }

    /// <summary>
    /// 헌장 원칙별 준수율 (0.0 ~ 1.0)
    /// </summary>
    public Dictionary<ConstitutionPrinciple, double> ConstitutionCompliance { get; init; } = new();

    #endregion

    #region 결과

    /// <summary>
    /// 스캔된 파일 목록
    /// </summary>
    public List<CodeFile> ScannedFiles { get; init; } = new();

    /// <summary>
    /// 스캔된 파일 목록 (ScannedFiles의 별칭)
    /// </summary>
    public List<CodeFile> Files => ScannedFiles;

    /// <summary>
    /// 파싱된 구문 트리 목록 (고급 분석용)
    /// </summary>
    public List<SyntaxTree> SyntaxTrees { get; init; } = new();

    /// <summary>
    /// 모든 위반 사항 목록
    /// </summary>
    public List<Violation> Violations { get; init; } = new();

    /// <summary>
    /// 생성된 HTML 리포트 경로
    /// </summary>
    public string? ReportHtmlPath { get; set; }

    /// <summary>
    /// 생성된 PDF 리포트 경로
    /// </summary>
    public string? ReportPdfPath { get; set; }

    #endregion

    #region 사용자 정보

    /// <summary>
    /// 실행한 사용자 (Windows 계정)
    /// </summary>
    public string ExecutedBy { get; init; } = Environment.UserName;

    #endregion

    #region 메서드

    /// <summary>
    /// 세션을 완료 상태로 전환
    /// </summary>
    public void Complete()
    {
        EndTime = DateTime.UtcNow;
        ScannedFilesCount = ScannedFiles.Count;
        TotalLinesOfCode = ScannedFiles.Sum(f => f.LineCount);
    }

    /// <summary>
    /// 전체 품질 점수를 계산
    /// </summary>
    /// <remarks>
    /// 품질 점수 = 100 - (가중치 적용된 위반 점수)
    /// Critical: -10점, High: -5점, Medium: -2점, Low: -1점
    /// </remarks>
    public void CalculateQualityScore()
    {
        if (ScannedFiles.Count == 0)
        {
            OverallQualityScore = 0;
            return;
        }

        var severityWeights = new Dictionary<ViolationSeverity, double>
        {
            { ViolationSeverity.Critical, 10.0 },
            { ViolationSeverity.High, 5.0 },
            { ViolationSeverity.Medium, 2.0 },
            { ViolationSeverity.Low, 1.0 }
        };

        double penalty = Violations.Sum(v => severityWeights[v.Severity]);
        double maxScore = 100.0;

        // 파일당 페널티 정규화
        double normalizedPenalty = penalty / ScannedFiles.Count;

        OverallQualityScore = Math.Max(0, Math.Min(100, maxScore - normalizedPenalty));
    }

    /// <summary>
    /// 헌장 준수율을 계산
    /// </summary>
    public void CalculateConstitutionCompliance()
    {
        // 각 원칙별 위반 수 집계
        var violationsByPrinciple = Violations
            .Where(v => v.RelatedPrinciple != ConstitutionPrinciple.None)
            .GroupBy(v => v.RelatedPrinciple)
            .ToDictionary(g => g.Key, g => g.Count());

        // 총 체크 항목 수 (파일당 원칙별 체크)
        int totalChecks = ScannedFiles.Count * 8; // 8개 원칙

        foreach (ConstitutionPrinciple principle in Enum.GetValues<ConstitutionPrinciple>())
        {
            if (principle == ConstitutionPrinciple.None) continue;

            int violationCount = violationsByPrinciple.GetValueOrDefault(principle, 0);
            double compliance = totalChecks > 0
                ? 1.0 - (double)violationCount / (ScannedFiles.Count > 0 ? ScannedFiles.Count : 1)
                : 1.0;

            ConstitutionCompliance[principle] = Math.Max(0, Math.Min(1.0, compliance));
        }
    }

    #endregion
}
