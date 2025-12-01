using System.Collections.Generic;
using TwinCatQA.Domain.Models;

namespace TwinCatQA.Domain.Contracts
{
    /// <summary>
    /// 검증 규칙 인터페이스
    ///
    /// 모든 검증 규칙은 이 인터페이스를 구현해야 합니다.
    /// 각 규칙은 헌장 원칙 중 하나 이상과 연관되며, AST를 순회하며 위반 사항을 수집합니다.
    /// </summary>
    public interface IValidationRule
    {
        /// <summary>
        /// 규칙 고유 식별자 (예: "FR-1-COMPLEXITY")
        /// </summary>
        string RuleId { get; }

        /// <summary>
        /// 규칙 이름 (한글, 예: "사이클로매틱 복잡도 검증")
        /// </summary>
        string RuleName { get; }

        /// <summary>
        /// 규칙 설명 (한글)
        /// </summary>
        string Description { get; }

        /// <summary>
        /// 연관된 헌장 원칙
        /// </summary>
        ConstitutionPrinciple RelatedPrinciple { get; }

        /// <summary>
        /// 기본 심각도
        /// </summary>
        ViolationSeverity DefaultSeverity { get; }

        /// <summary>
        /// 규칙 활성화 여부
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// 코드 파일에 대해 검증을 수행하고 위반 사항을 반환합니다.
        /// </summary>
        /// <param name="file">검증할 코드 파일</param>
        /// <returns>발견된 위반 사항 목록</returns>
        IEnumerable<Violation> Validate(CodeFile file);

        /// <summary>
        /// 규칙별 설정 파라미터를 적용합니다.
        /// </summary>
        /// <param name="parameters">설정 파라미터 (YAML에서 로드)</param>
        void Configure(Dictionary<string, object> parameters);

        /// <summary>
        /// 규칙이 지원하는 프로그래밍 언어
        /// </summary>
        ProgrammingLanguage[] SupportedLanguages { get; }
    }
}
