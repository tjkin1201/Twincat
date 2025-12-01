using System.Collections.Generic;
using TwinCatQA.Domain.Models;

namespace TwinCatQA.Domain.Contracts
{
    /// <summary>
    /// 검증 규칙 인터페이스
    ///
    /// 모든 검증 규칙은 이 인터페이스를 구현해야 합니다.
    /// 각 규칙은 헌장 원칙 중 하나 이상과 연관되며, AST를 순회하며 위반 사항을 수집합니다.
    /// 인터페이스 분리 원칙(ISP)에 따라 규칙별 핵심 기능만 정의합니다.
    /// </summary>
    public interface IValidationRule
    {
        /// <summary>
        /// 규칙 고유 식별자를 가져옵니다 (예: "FR-1-COMPLEXITY").
        /// 각 규칙은 헌장 원칙과 연관된 고유한 ID를 가집니다.
        /// </summary>
        string RuleId { get; }

        /// <summary>
        /// 규칙 이름을 가져옵니다 (예: "사이클로매틱 복잡도 검증").
        /// 한글로 작성된 명확한 규칙명을 제공합니다.
        /// </summary>
        string RuleName { get; }

        /// <summary>
        /// 규칙에 대한 상세 설명을 가져옵니다.
        /// 규칙의 목적, 검증 기준, 위반 시 영향 등을 한글로 설명합니다.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// 연관된 헌장 원칙을 가져옵니다.
        /// 규칙이 어떤 코딩 헌장 원칙을 강제하는지 나타냅니다.
        /// </summary>
        ConstitutionPrinciple RelatedPrinciple { get; }

        /// <summary>
        /// 기본 심각도 수준을 가져옵니다.
        /// 위반 발견 시 기본적으로 적용되는 심각도입니다.
        /// </summary>
        ViolationSeverity DefaultSeverity { get; }

        /// <summary>
        /// 규칙 활성화 여부를 가져오거나 설정합니다.
        /// 비활성화된 규칙은 검증 시 실행되지 않습니다.
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// 규칙이 지원하는 프로그래밍 언어 목록을 가져옵니다.
        /// Structured Text, Ladder Diagram 등 TwinCAT에서 사용하는 언어를 지원합니다.
        /// </summary>
        ProgrammingLanguage[] SupportedLanguages { get; }

        /// <summary>
        /// 코드 파일에 대해 검증을 수행하고 위반 사항을 반환합니다.
        /// AST를 분석하여 규칙 위반을 식별하고 상세 정보를 제공합니다.
        /// </summary>
        /// <param name="file">검증할 코드 파일 (AST 포함)</param>
        /// <returns>발견된 위반 사항 목록. 위반이 없으면 빈 컬렉션을 반환합니다.</returns>
        IEnumerable<Violation> Validate(CodeFile file);

        /// <summary>
        /// 규칙별 설정 파라미터를 적용합니다.
        /// YAML 또는 JSON 설정에서 로드된 파라미터를 사용하여 규칙 동작을 커스터마이징합니다.
        /// </summary>
        /// <param name="parameters">
        /// 설정 파라미터 딕셔너리.
        /// 예: { "MaxComplexity": 10, "Threshold": 5 }
        /// </param>
        void Configure(Dictionary<string, object> parameters);
    }
}
