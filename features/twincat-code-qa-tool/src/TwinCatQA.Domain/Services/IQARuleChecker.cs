using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Models.QA;

namespace TwinCatQA.Domain.Services;

/// <summary>
/// QA 규칙 검사기 인터페이스
/// </summary>
public interface IQARuleChecker
{
    /// <summary>
    /// 규칙 ID
    /// </summary>
    string RuleId { get; }

    /// <summary>
    /// 규칙 이름
    /// </summary>
    string RuleName { get; }

    /// <summary>
    /// 규칙 설명
    /// </summary>
    string Description { get; }

    /// <summary>
    /// 심각도
    /// </summary>
    Severity Severity { get; }

    /// <summary>
    /// 변수 변경 검사
    /// </summary>
    List<QAIssue> CheckVariableChange(VariableChange change);

    /// <summary>
    /// 로직 변경 검사
    /// </summary>
    List<QAIssue> CheckLogicChange(LogicChange change);

    /// <summary>
    /// 데이터 타입 변경 검사
    /// </summary>
    List<QAIssue> CheckDataTypeChange(DataTypeChange change);
}
