using TwinCatQA.Domain.Models;

namespace TwinCatQA.Domain.Services;

/// <summary>
/// 종합 안전성 분석기 인터페이스
/// </summary>
public interface IComprehensiveSafetyAnalyzer
{
    /// <summary>
    /// 종합 안전성 분석 수행
    /// </summary>
    ComprehensiveSafetyAnalysis Analyze(ValidationSession session);
}

/// <summary>
/// 나눗셈 안전성 검사기 인터페이스
/// </summary>
public interface IDivisionSafetyChecker
{
    /// <summary>
    /// 나눗셈 0 체크 검사
    /// </summary>
    List<DivisionCheckResult> Check(ValidationSession session);
}

/// <summary>
/// 타이머 안전성 분석기 인터페이스
/// </summary>
public interface ITimerSafetyAnalyzer
{
    /// <summary>
    /// 타이머 안전성 분석
    /// </summary>
    List<TimerAnalysisResult> Analyze(ValidationSession session);
}

/// <summary>
/// 형변환 안전성 검사기 인터페이스
/// </summary>
public interface ITypeConversionChecker
{
    /// <summary>
    /// 형변환 데이터 손실 검사
    /// </summary>
    List<TypeConversionResult> Check(ValidationSession session);
}

/// <summary>
/// 루프 안전성 분석기 인터페이스
/// </summary>
public interface ILoopSafetyAnalyzer
{
    /// <summary>
    /// 무한 루프 가능성 분석
    /// </summary>
    List<SafetyIssue> Analyze(ValidationSession session);
}

/// <summary>
/// FB 초기화 검사기 인터페이스
/// </summary>
public interface IFBInitializationChecker
{
    /// <summary>
    /// FB 인스턴스 초기화 검사
    /// </summary>
    List<FBInitializationResult> Check(ValidationSession session);
}

/// <summary>
/// 출력 할당 검사기 인터페이스
/// </summary>
public interface IOutputAssignmentChecker
{
    /// <summary>
    /// 출력 변수 할당 검사
    /// </summary>
    List<OutputAssignmentResult> Check(ValidationSession session);
}

/// <summary>
/// I/O 방향성 검사기 인터페이스
/// </summary>
public interface IIODirectionChecker
{
    /// <summary>
    /// 입력/출력 방향 위반 검사
    /// </summary>
    List<SafetyIssue> Check(ValidationSession session);
}
