using TwinCatQA.Domain.Models;

namespace TwinCatQA.Domain.Services;

/// <summary>
/// 동시성 분석기 인터페이스
/// </summary>
public interface IConcurrencyAnalyzer
{
    List<ConcurrencyIssue> Analyze(ValidationSession session);
}

/// <summary>
/// 상태머신 분석기 인터페이스
/// </summary>
public interface IStateMachineAnalyzer
{
    List<StateMachineIssue> Analyze(ValidationSession session);
}

/// <summary>
/// 통신 안전성 분석기 인터페이스
/// </summary>
public interface ICommunicationAnalyzer
{
    List<CommunicationIssue> Analyze(ValidationSession session);
}

/// <summary>
/// 리소스 안전성 분석기 인터페이스
/// </summary>
public interface IResourceAnalyzer
{
    List<ResourceIssue> Analyze(ValidationSession session);
}

/// <summary>
/// 센서 안전성 분석기 인터페이스
/// </summary>
public interface ISensorAnalyzer
{
    List<SensorIssue> Analyze(ValidationSession session);
}

/// <summary>
/// 에러 복구 분석기 인터페이스
/// </summary>
public interface IErrorRecoveryAnalyzer
{
    List<ErrorRecoveryIssue> Analyze(ValidationSession session);
}

/// <summary>
/// 로깅 분석기 인터페이스
/// </summary>
public interface ILoggingAnalyzer
{
    List<LoggingIssue> Analyze(ValidationSession session);
}

/// <summary>
/// 심화 안전성 분석 통합 인터페이스
/// </summary>
public interface IExtendedSafetyAnalyzer
{
    ExtendedSafetyAnalysis Analyze(ValidationSession session);
}
