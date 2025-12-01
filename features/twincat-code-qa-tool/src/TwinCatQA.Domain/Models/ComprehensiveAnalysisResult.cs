using System;

namespace TwinCatQA.Domain.Models;

/// <summary>
/// 통합 분석 결과
///
/// 4가지 고급 분석 기능의 결과를 통합하여 제공합니다:
/// - 컴파일 기반 검증
/// - 변수 사용 분석
/// - 의존성 분석
/// - I/O 매핑 검증
/// </summary>
public class ComprehensiveAnalysisResult
{
    /// <summary>
    /// 분석 세션 ID
    /// </summary>
    public Guid AnalysisId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// 분석 시작 시간
    /// </summary>
    public DateTime StartTime { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// 분석 종료 시간
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 분석 소요 시간
    /// </summary>
    public TimeSpan Duration => EndTime.HasValue
        ? EndTime.Value - StartTime
        : DateTime.UtcNow - StartTime;

    /// <summary>
    /// 프로젝트 경로
    /// </summary>
    public string ProjectPath { get; init; } = string.Empty;

    /// <summary>
    /// 프로젝트 이름
    /// </summary>
    public string ProjectName { get; init; } = string.Empty;

    /// <summary>
    /// 컴파일 결과
    /// </summary>
    public CompilationResult? Compilation { get; set; }

    /// <summary>
    /// 변수 사용 분석 결과
    /// </summary>
    public VariableUsageAnalysis? VariableUsage { get; set; }

    /// <summary>
    /// 의존성 분석 결과
    /// </summary>
    public DependencyAnalysis? Dependencies { get; set; }

    /// <summary>
    /// I/O 매핑 검증 결과
    /// </summary>
    public IOMappingValidationResult? IOMapping { get; set; }

    /// <summary>
    /// 전체 분석 성공 여부
    /// </summary>
    /// <remarks>
    /// 다음 조건을 모두 만족해야 성공:
    /// 1. 컴파일 오류 없음 (경고는 허용)
    /// 2. I/O 매핑 오류 없음 (경고는 허용)
    /// 3. 초기화되지 않은 변수 없음 (치명적 이슈)
    /// 4. 순환 참조 없음 (치명적 이슈)
    ///
    /// 사용되지 않은 변수와 Dead Code는 경고로 간주하여 성공 여부에 영향 없음
    /// </remarks>
    public bool IsSuccess
    {
        get
        {
            bool compilationSuccess = Compilation?.IsSuccess ?? true;
            bool ioMappingSuccess = IOMapping?.IsValid ?? true;

            // 치명적 이슈: 초기화되지 않은 변수와 순환 참조만
            int criticalIssues = (VariableUsage?.UninitializedVariables.Count ?? 0)
                               + (Dependencies?.CircularReferences.Count ?? 0);

            return compilationSuccess && ioMappingSuccess && criticalIssues == 0;
        }
    }

    /// <summary>
    /// 총 이슈 수 (모든 분석 결과 통합)
    /// </summary>
    public int TotalIssues
    {
        get
        {
            int compilationIssues = (Compilation?.ErrorCount ?? 0) + (Compilation?.WarningCount ?? 0);
            int variableIssues = VariableUsage?.TotalIssues ?? 0;
            int dependencyIssues = Dependencies?.CircularReferences.Count ?? 0;
            int ioIssues = (IOMapping?.Errors.Count ?? 0) + (IOMapping?.Warnings.Count ?? 0);

            return compilationIssues + variableIssues + dependencyIssues + ioIssues;
        }
    }

    /// <summary>
    /// 전체 품질 점수 (0-100)
    /// </summary>
    /// <remarks>
    /// 각 분석 결과를 가중치를 적용하여 통합 점수 계산:
    /// - 컴파일 성공: 30%
    /// - 변수 사용: 25%
    /// - 의존성: 25%
    /// - I/O 매핑: 20%
    /// </remarks>
    public double OverallQualityScore
    {
        get
        {
            double compilationScore = CalculateCompilationScore();
            double variableScore = CalculateVariableScore();
            double dependencyScore = CalculateDependencyScore();
            double ioScore = CalculateIOScore();

            return (compilationScore * 0.30)
                 + (variableScore * 0.25)
                 + (dependencyScore * 0.25)
                 + (ioScore * 0.20);
        }
    }

    /// <summary>
    /// 분석 요약 메시지
    /// </summary>
    public string Summary
    {
        get
        {
            if (IsSuccess)
            {
                return $"모든 분석 통과. 품질 점수: {OverallQualityScore:F1}/100";
            }

            string issues = $"총 {TotalIssues}개 이슈 발견";

            if (Compilation != null && !Compilation.IsSuccess)
            {
                issues += $" (컴파일 오류: {Compilation.ErrorCount}개)";
            }

            if (VariableUsage != null && VariableUsage.TotalIssues > 0)
            {
                issues += $" (변수 이슈: {VariableUsage.TotalIssues}개)";
            }

            if (Dependencies != null && Dependencies.CircularReferences.Count > 0)
            {
                issues += $" (순환 참조: {Dependencies.CircularReferences.Count}개)";
            }

            return issues;
        }
    }

    /// <summary>
    /// 컴파일 점수 계산 (0-100)
    /// </summary>
    private double CalculateCompilationScore()
    {
        if (Compilation == null) return 100.0;
        if (!Compilation.IsSuccess) return 0.0;

        // 경고만 있는 경우 페널티 적용
        double warningPenalty = Compilation.WarningCount * 2.0;
        return Math.Max(0, 100.0 - warningPenalty);
    }

    /// <summary>
    /// 변수 사용 점수 계산 (0-100)
    /// </summary>
    private double CalculateVariableScore()
    {
        if (VariableUsage == null) return 100.0;

        double unusedPenalty = VariableUsage.UnusedVariables.Count * 5.0;
        double uninitializedPenalty = VariableUsage.UninitializedVariables.Count * 10.0; // 더 높은 페널티
        double deadCodePenalty = VariableUsage.DeadCodeBlocks.Count * 3.0;

        double totalPenalty = unusedPenalty + uninitializedPenalty + deadCodePenalty;
        return Math.Max(0, 100.0 - totalPenalty);
    }

    /// <summary>
    /// 의존성 점수 계산 (0-100)
    /// </summary>
    private double CalculateDependencyScore()
    {
        if (Dependencies == null) return 100.0;

        // 순환 참조는 치명적 이슈
        if (Dependencies.CircularReferences.Count > 0)
        {
            double circularPenalty = Dependencies.CircularReferences.Count * 20.0;
            return Math.Max(0, 100.0 - circularPenalty);
        }

        // 호출 깊이가 너무 깊으면 페널티
        double depthPenalty = Math.Max(0, (Dependencies.CallGraph?.MaxCallDepth ?? 0) - 10) * 2.0;
        return Math.Max(0, 100.0 - depthPenalty);
    }

    /// <summary>
    /// I/O 매핑 점수 계산 (0-100)
    /// </summary>
    private double CalculateIOScore()
    {
        if (IOMapping == null) return 100.0;
        if (!IOMapping.IsValid) return 0.0;

        double errorPenalty = IOMapping.Errors.Count * 10.0;
        double warningPenalty = IOMapping.Warnings.Count * 3.0;

        double totalPenalty = errorPenalty + warningPenalty;
        return Math.Max(0, 100.0 - totalPenalty);
    }

    /// <summary>
    /// 분석 완료 처리
    /// </summary>
    public void Complete()
    {
        EndTime = DateTime.UtcNow;
    }
}
