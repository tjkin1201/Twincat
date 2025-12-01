using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Models.QA;
using TwinCatQA.Domain.Services;

namespace TwinCatQA.Infrastructure.QA.Rules.TE1200;

/// <summary>
/// TE1200 Static Analysis 규칙 기본 클래스
/// Beckhoff TE1200 규칙 ID 체계 (SA0001-SA0180) 준수
/// </summary>
public abstract class SARuleBase : IQARuleChecker
{
    /// <summary>
    /// SA 규칙 ID (예: SA0001)
    /// </summary>
    public abstract string RuleId { get; }

    /// <summary>
    /// 규칙 이름
    /// </summary>
    public abstract string RuleName { get; }

    /// <summary>
    /// 규칙 설명
    /// </summary>
    public abstract string Description { get; }

    /// <summary>
    /// 심각도
    /// </summary>
    public abstract Severity Severity { get; }

    /// <summary>
    /// 규칙 카테고리 (Beckhoff 분류)
    /// </summary>
    public abstract SARuleCategory Category { get; }

    /// <summary>
    /// 기본 활성화 여부 (Beckhoff 기본값 기준)
    /// </summary>
    public virtual bool EnabledByDefault => true;

    /// <summary>
    /// Precompile 검사 가능 여부
    /// </summary>
    public virtual bool SupportsPrecompile => false;

    /// <summary>
    /// PLCopen 가이드라인 관련 여부
    /// </summary>
    public virtual bool IsPLCopenRelated => false;

    /// <summary>
    /// 변수 변경 검사
    /// </summary>
    public virtual List<QAIssue> CheckVariableChange(VariableChange change)
    {
        return new List<QAIssue>();
    }

    /// <summary>
    /// 로직 변경 검사
    /// </summary>
    public virtual List<QAIssue> CheckLogicChange(LogicChange change)
    {
        return new List<QAIssue>();
    }

    /// <summary>
    /// 데이터 타입 변경 검사
    /// </summary>
    public virtual List<QAIssue> CheckDataTypeChange(DataTypeChange change)
    {
        return new List<QAIssue>();
    }

    /// <summary>
    /// QA 이슈 생성 헬퍼
    /// </summary>
    protected QAIssue CreateIssue(
        string title,
        string description,
        string filePath,
        int line,
        string whyDangerous,
        string recommendation,
        string oldCode = "",
        string newCode = "",
        List<string>? examples = null)
    {
        return new QAIssue
        {
            Severity = Severity,
            Category = Category.ToString(),
            Title = title,
            Description = description,
            Location = $"{filePath}:{line}",
            FilePath = filePath,
            Line = line,
            WhyDangerous = whyDangerous,
            Recommendation = recommendation,
            OldCodeSnippet = oldCode,
            NewCodeSnippet = newCode,
            Examples = examples ?? new List<string>(),
            RuleId = RuleId
        };
    }
}

/// <summary>
/// SA 규칙 카테고리 (Beckhoff TE1200 분류 기준)
/// </summary>
public enum SARuleCategory
{
    /// <summary>
    /// 도달 불가능/불필요 코드
    /// </summary>
    UnreachableUnusedCode,

    /// <summary>
    /// 변환 (타입 변환)
    /// </summary>
    Conversions,

    /// <summary>
    /// 연산
    /// </summary>
    Operations,

    /// <summary>
    /// 변수/상수
    /// </summary>
    VariablesAndConstants,

    /// <summary>
    /// 선언
    /// </summary>
    Declarations,

    /// <summary>
    /// 초기화
    /// </summary>
    Initialization,

    /// <summary>
    /// 동시성 (멀티태스크)
    /// </summary>
    Concurrency,

    /// <summary>
    /// 객체지향
    /// </summary>
    ObjectOriented,

    /// <summary>
    /// 명명 규칙
    /// </summary>
    NamingConventions,

    /// <summary>
    /// 메트릭스/복잡도
    /// </summary>
    Metrics,

    /// <summary>
    /// 주석
    /// </summary>
    Comments,

    /// <summary>
    /// IEC 61131-3 엄격 규칙
    /// </summary>
    StrictIEC,

    /// <summary>
    /// 메모리/레이아웃
    /// </summary>
    MemoryLayout,

    /// <summary>
    /// 안전성
    /// </summary>
    Safety,

    /// <summary>
    /// 기타
    /// </summary>
    Miscellaneous
}
