using System;
using System.Collections.Generic;
using System.Linq;
using TwinCatQA.Domain.Contracts;
using TwinCatQA.Domain.Models;

namespace TwinCatQA.Application.Rules
{
    /// <summary>
    /// 사이클로매틱 복잡도 검증 규칙
    ///
    /// 헌장 원칙 4 (상태 기반 설계)와 관련된 코드 복잡도를 검증합니다.
    /// Function Block의 복잡도가 높을수록 유지보수가 어려워지고 버그 발생 가능성이 증가합니다.
    /// McCabe의 사이클로매틱 복잡도를 기반으로 제어 흐름의 복잡도를 측정합니다.
    /// </summary>
    public class CyclomaticComplexityRule : IValidationRule
    {
        private readonly IParserService _parserService;

        /// <summary>
        /// Medium 심각도 임계값 (기본: 10)
        /// </summary>
        private int _mediumThreshold = 10;

        /// <summary>
        /// High 심각도 임계값 (기본: 15)
        /// </summary>
        private int _highThreshold = 15;

        /// <summary>
        /// Critical 심각도 임계값 (기본: 20)
        /// </summary>
        private int _criticalThreshold = 20;

        #region IValidationRule 속성

        /// <inheritdoc />
        public string RuleId => "FR-4-COMPLEXITY";

        /// <inheritdoc />
        public string RuleName => "사이클로매틱 복잡도 검증";

        /// <inheritdoc />
        public string Description =>
            "Function Block의 사이클로매틱 복잡도를 검증합니다. " +
            "복잡도가 높으면 테스트가 어렵고 버그 발생 확률이 증가합니다. " +
            "임계값: Medium(10), High(15), Critical(20)";

        /// <inheritdoc />
        public ConstitutionPrinciple RelatedPrinciple => ConstitutionPrinciple.StateMachineDesign;

        /// <inheritdoc />
        public ViolationSeverity DefaultSeverity => ViolationSeverity.Medium;

        /// <inheritdoc />
        public bool IsEnabled { get; set; } = true;

        /// <inheritdoc />
        public ProgrammingLanguage[] SupportedLanguages => new[]
        {
            ProgrammingLanguage.ST
        };

        #endregion

        /// <summary>
        /// CyclomaticComplexityRule 생성자
        /// </summary>
        /// <param name="parserService">파서 서비스 (의존성 주입)</param>
        /// <exception cref="ArgumentNullException">parserService가 null인 경우</exception>
        public CyclomaticComplexityRule(IParserService parserService)
        {
            _parserService = parserService ?? throw new ArgumentNullException(nameof(parserService));
        }

        /// <inheritdoc />
        public IEnumerable<Violation> Validate(CodeFile file)
        {
            // 파일 검증
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            if (file.Ast == null)
            {
                // AST가 없으면 검증 불가능
                yield break;
            }

            // ST 언어만 지원
            if (file.Language != ProgrammingLanguage.ST)
            {
                yield break;
            }

            // AST를 SyntaxTree로 변환
            var syntaxTree = file.Ast as SyntaxTree;
            if (syntaxTree == null || !syntaxTree.IsValid)
            {
                yield break;
            }

            // Function Block 추출
            List<FunctionBlock> functionBlocks;
            try
            {
                functionBlocks = _parserService.ExtractFunctionBlocks(syntaxTree);
            }
            catch (Exception ex)
            {
                // 파싱 오류 발생 시 경고 로그
                Console.WriteLine($"Function Block 추출 중 오류 발생: {ex.Message}");
                yield break;
            }

            // 각 Function Block에 대해 복잡도 검증
            foreach (var fb in functionBlocks)
            {
                // 복잡도 계산
                int complexity;
                try
                {
                    complexity = _parserService.CalculateCyclomaticComplexity(fb);

                    // FunctionBlock 엔티티에 복잡도 저장
                    fb.CyclomaticComplexity = complexity;
                }
                catch (Exception ex)
                {
                    // 복잡도 계산 실패 시 경고 로그
                    Console.WriteLine($"복잡도 계산 중 오류 발생 ({fb.Name}): {ex.Message}");
                    continue;
                }

                // 임계값 초과 여부 확인
                if (complexity >= _mediumThreshold)
                {
                    // 심각도 결정
                    ViolationSeverity severity = DetermineSeverity(complexity);

                    // 위반 생성
                    yield return new Violation
                    {
                        RuleId = RuleId,
                        RuleName = RuleName,
                        RelatedPrinciple = RelatedPrinciple,
                        Severity = severity,
                        FilePath = file.FilePath,
                        Line = fb.StartLine,
                        Column = 0,
                        CodeSnippet = ExtractFunctionBlockSnippet(syntaxTree.SourceCode, fb),
                        Message = $"Function Block '{fb.Name}'의 사이클로매틱 복잡도가 {complexity}로 임계값을 초과합니다. " +
                                  $"(현재: {complexity}, 권장: {_mediumThreshold} 미만)",
                        Suggestion = GenerateSuggestion(complexity, fb.Name),
                        DocumentationUrl = null
                    };
                }
            }
        }

        /// <inheritdoc />
        public void Configure(Dictionary<string, object> parameters)
        {
            if (parameters == null)
            {
                return;
            }

            // medium_threshold 설정 로드
            if (parameters.ContainsKey("medium_threshold"))
            {
                if (parameters["medium_threshold"] is int mediumValue)
                {
                    if (mediumValue > 0)
                    {
                        _mediumThreshold = mediumValue;
                    }
                    else
                    {
                        throw new ArgumentException(
                            $"medium_threshold는 양수여야 합니다. 입력값: {mediumValue}");
                    }
                }
            }

            // high_threshold 설정 로드
            if (parameters.ContainsKey("high_threshold"))
            {
                if (parameters["high_threshold"] is int highValue)
                {
                    if (highValue > _mediumThreshold)
                    {
                        _highThreshold = highValue;
                    }
                    else
                    {
                        throw new ArgumentException(
                            $"high_threshold는 medium_threshold보다 커야 합니다. " +
                            $"입력값: {highValue}, medium_threshold: {_mediumThreshold}");
                    }
                }
            }

            // critical_threshold 설정 로드
            if (parameters.ContainsKey("critical_threshold"))
            {
                if (parameters["critical_threshold"] is int criticalValue)
                {
                    if (criticalValue > _highThreshold)
                    {
                        _criticalThreshold = criticalValue;
                    }
                    else
                    {
                        throw new ArgumentException(
                            $"critical_threshold는 high_threshold보다 커야 합니다. " +
                            $"입력값: {criticalValue}, high_threshold: {_highThreshold}");
                    }
                }
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// 복잡도 값에 따라 심각도를 결정합니다.
        /// </summary>
        /// <param name="complexity">사이클로매틱 복잡도</param>
        /// <returns>위반 심각도</returns>
        private ViolationSeverity DetermineSeverity(int complexity)
        {
            if (complexity >= _criticalThreshold)
            {
                return ViolationSeverity.Critical;
            }
            else if (complexity >= _highThreshold)
            {
                return ViolationSeverity.High;
            }
            else if (complexity >= _mediumThreshold)
            {
                return ViolationSeverity.Medium;
            }
            else
            {
                return ViolationSeverity.Low;
            }
        }

        /// <summary>
        /// 복잡도에 따른 수정 제안을 생성합니다.
        /// </summary>
        /// <param name="complexity">사이클로매틱 복잡도</param>
        /// <param name="fbName">Function Block 이름</param>
        /// <returns>수정 제안 메시지</returns>
        private string GenerateSuggestion(int complexity, string fbName)
        {
            var suggestions = new List<string>
            {
                $"'{fbName}'을(를) 더 작은 Function Block으로 분할하세요.",
                "복잡한 IF-ELSE 체인을 CASE 문으로 리팩토링하세요.",
                "중첩된 조건문을 Early Return 패턴으로 단순화하세요.",
                "상태 머신 패턴을 사용하여 제어 흐름을 명확하게 하세요."
            };

            if (complexity >= _criticalThreshold)
            {
                suggestions.Add("CRITICAL: 즉시 리팩토링이 필요합니다. 테스트와 유지보수가 매우 어렵습니다.");
            }
            else if (complexity >= _highThreshold)
            {
                suggestions.Add("우선순위를 높여 리팩토링을 진행하세요.");
            }

            return string.Join("\n", suggestions);
        }

        /// <summary>
        /// Function Block의 코드 스니펫을 추출합니다.
        /// </summary>
        /// <param name="sourceCode">전체 소스 코드</param>
        /// <param name="fb">Function Block 정보</param>
        /// <returns>코드 스니펫 (시작 부분 10줄)</returns>
        private string ExtractFunctionBlockSnippet(string sourceCode, FunctionBlock fb)
        {
            if (string.IsNullOrEmpty(sourceCode))
            {
                return string.Empty;
            }

            string[] lines = sourceCode.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            // 라인 번호 유효성 검증
            if (fb.StartLine < 1 || fb.StartLine > lines.Length)
            {
                return string.Empty;
            }

            // Function Block 시작 부분 10줄 추출
            int startLine = fb.StartLine - 1;
            int endLine = Math.Min(lines.Length - 1, startLine + 9);

            // 스니펫 생성
            var snippetLines = new List<string>();
            for (int i = startLine; i <= endLine; i++)
            {
                snippetLines.Add($"{i + 1,4}: {lines[i]}");
            }

            if (fb.EndLine - fb.StartLine > 10)
            {
                snippetLines.Add("    ...");
                snippetLines.Add($"{fb.EndLine,4}: END_FUNCTION_BLOCK");
            }

            return string.Join(Environment.NewLine, snippetLines);
        }

        #endregion
    }
}
