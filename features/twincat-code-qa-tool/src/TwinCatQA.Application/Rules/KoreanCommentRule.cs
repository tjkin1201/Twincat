using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TwinCatQA.Domain.Contracts;
using TwinCatQA.Domain.Models;

namespace TwinCatQA.Application.Rules
{
    /// <summary>
    /// 한글 주석 비율 검증 규칙
    ///
    /// 헌장 원칙 1 (한글 우선)을 강제합니다.
    /// 모든 주석은 최소 95% 이상의 한글 문자를 포함해야 합니다.
    /// 변수명이나 기술 용어 등을 고려하여 일부 영문 혼용을 허용합니다.
    /// </summary>
    public class KoreanCommentRule : IValidationRule
    {
        private readonly IParserService _parserService;

        /// <summary>
        /// 한글 비율 임계값 (기본: 0.95 = 95%)
        /// </summary>
        private double _requiredKoreanRatio = 0.95;

        /// <summary>
        /// 최소 주석 길이 (이보다 짧은 주석은 검사 제외)
        /// </summary>
        private int _minCommentLength = 5;

        #region IValidationRule 속성

        /// <inheritdoc />
        public string RuleId => "FR-1-KOREAN-COMMENT";

        /// <inheritdoc />
        public string RuleName => "한글 주석 비율 검증";

        /// <inheritdoc />
        public string Description =>
            "모든 주석은 한글로 작성되어야 합니다. " +
            "한글 문자(유니코드 U+AC00 ~ U+D7A3) 비율이 설정된 임계값 미만인 주석은 위반으로 간주됩니다.";

        /// <inheritdoc />
        public ConstitutionPrinciple RelatedPrinciple => ConstitutionPrinciple.KoreanFirst;

        /// <inheritdoc />
        public ViolationSeverity DefaultSeverity => ViolationSeverity.High;

        /// <inheritdoc />
        public bool IsEnabled { get; set; } = true;

        /// <inheritdoc />
        public ProgrammingLanguage[] SupportedLanguages => new[]
        {
            ProgrammingLanguage.ST,
            ProgrammingLanguage.LD,
            ProgrammingLanguage.FBD,
            ProgrammingLanguage.SFC
        };

        #endregion

        /// <summary>
        /// KoreanCommentRule 생성자
        /// </summary>
        /// <param name="parserService">파서 서비스 (의존성 주입)</param>
        /// <exception cref="ArgumentNullException">parserService가 null인 경우</exception>
        public KoreanCommentRule(IParserService parserService)
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
                // AST가 없으면 검증 불가능 (파싱 오류일 가능성)
                yield break;
            }

            // AST를 SyntaxTree로 변환
            var syntaxTree = file.Ast as SyntaxTree;
            if (syntaxTree == null || !syntaxTree.IsValid)
            {
                // 유효하지 않은 구문 트리는 건너뜀
                yield break;
            }

            // 주석 추출
            Dictionary<int, string> comments;
            try
            {
                comments = _parserService.ExtractComments(syntaxTree);
            }
            catch (Exception ex)
            {
                // 파싱 오류 발생 시 경고 로그 (실제 구현에서는 ILogger 사용)
                Console.WriteLine($"주석 추출 중 오류 발생: {ex.Message}");
                yield break;
            }

            // 각 주석에 대해 한글 비율 검증
            foreach (var comment in comments)
            {
                int lineNumber = comment.Key;
                string commentText = comment.Value.Trim();

                // 주석 마커 제거 (// 또는 (* ... *))
                string cleanedComment = RemoveCommentMarkers(commentText);

                // 빈 주석이거나 너무 짧은 주석은 건너뜀
                if (string.IsNullOrWhiteSpace(cleanedComment) || cleanedComment.Length < _minCommentLength)
                {
                    continue;
                }

                // 한글 비율 계산
                double koreanRatio = CalculateKoreanRatio(cleanedComment);

                // 한글 비율이 임계값 미만이면 위반 생성
                if (koreanRatio < _requiredKoreanRatio)
                {
                    yield return new Violation
                    {
                        RuleId = RuleId,
                        RuleName = RuleName,
                        RelatedPrinciple = RelatedPrinciple,
                        Severity = DefaultSeverity,
                        FilePath = file.FilePath,
                        Line = lineNumber,
                        Column = 0,
                        CodeSnippet = ExtractCodeSnippet(syntaxTree.SourceCode, lineNumber),
                        Message = $"주석의 한글 비율이 {koreanRatio:P1}로 기준치({_requiredKoreanRatio:P0}) 미만입니다. " +
                                  $"주석은 한글로 작성해야 합니다.",
                        Suggestion = "주석을 한글로 작성하세요. 변수명이나 기술 용어는 괄호로 구분하여 추가할 수 있습니다.\n" +
                                     $"예: // 모터 속도 제어 (Motor Speed Control)",
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

            // required_korean_ratio 설정 로드
            if (parameters.ContainsKey("required_korean_ratio"))
            {
                if (parameters["required_korean_ratio"] is double ratio)
                {
                    if (ratio >= 0.0 && ratio <= 1.0)
                    {
                        _requiredKoreanRatio = ratio;
                    }
                    else
                    {
                        throw new ArgumentException(
                            $"required_korean_ratio는 0.0에서 1.0 사이의 값이어야 합니다. 입력값: {ratio}");
                    }
                }
            }

            // min_comment_length 설정 로드
            if (parameters.ContainsKey("min_comment_length"))
            {
                if (parameters["min_comment_length"] is int length)
                {
                    if (length > 0)
                    {
                        _minCommentLength = length;
                    }
                    else
                    {
                        throw new ArgumentException(
                            $"min_comment_length는 양수여야 합니다. 입력값: {length}");
                    }
                }
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// 주석 마커를 제거합니다 (// 또는 (* ... *))
        /// </summary>
        /// <param name="comment">원본 주석 텍스트</param>
        /// <returns>마커가 제거된 주석 내용</returns>
        private string RemoveCommentMarkers(string comment)
        {
            if (string.IsNullOrEmpty(comment))
            {
                return string.Empty;
            }

            // // 스타일 주석 마커 제거
            string result = Regex.Replace(comment, @"^//\s*", "");

            // (* ... *) 스타일 주석 마커 제거
            result = Regex.Replace(result, @"^\(\*\s*|\s*\*\)$", "");

            return result.Trim();
        }

        /// <summary>
        /// 텍스트에서 한글 문자의 비율을 계산합니다.
        /// 괄호로 감싸진 영문 부분(예: (Motor Speed Control))은 제외합니다.
        /// </summary>
        /// <param name="text">분석할 텍스트</param>
        /// <returns>한글 비율 (0.0 ~ 1.0)</returns>
        private double CalculateKoreanRatio(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0.0;
            }

            // 괄호 안의 내용 제거 (변수명, 기술 용어 등)
            // 예: "모터 속도 제어 (Motor Speed Control)" → "모터 속도 제어"
            string textWithoutParentheses = Regex.Replace(text, @"\([^)]*\)", "");

            // 공백 제거 (공백은 카운트에서 제외)
            string textWithoutSpaces = Regex.Replace(textWithoutParentheses, @"\s+", "");

            if (textWithoutSpaces.Length == 0)
            {
                return 0.0;
            }

            // 한글 문자 개수 계산 (유니코드 범위: U+AC00 ~ U+D7A3)
            int koreanCharCount = textWithoutSpaces.Count(c => c >= '\uAC00' && c <= '\uD7A3');

            // 비율 계산
            return (double)koreanCharCount / textWithoutSpaces.Length;
        }

        /// <summary>
        /// 지정된 라인 주변의 코드 스니펫을 추출합니다 (컨텍스트 5줄).
        /// </summary>
        /// <param name="sourceCode">전체 소스 코드</param>
        /// <param name="lineNumber">중심 라인 번호</param>
        /// <returns>코드 스니펫</returns>
        private string ExtractCodeSnippet(string sourceCode, int lineNumber)
        {
            if (string.IsNullOrEmpty(sourceCode))
            {
                return string.Empty;
            }

            string[] lines = sourceCode.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            // 라인 번호 유효성 검증
            if (lineNumber < 1 || lineNumber > lines.Length)
            {
                return string.Empty;
            }

            // 컨텍스트 범위 계산 (전후 2줄)
            int startLine = Math.Max(0, lineNumber - 3);
            int endLine = Math.Min(lines.Length - 1, lineNumber + 1);

            // 스니펫 생성
            var snippetLines = new List<string>();
            for (int i = startLine; i <= endLine; i++)
            {
                string prefix = (i + 1 == lineNumber) ? ">>> " : "    ";
                snippetLines.Add($"{prefix}{i + 1,4}: {lines[i]}");
            }

            return string.Join(Environment.NewLine, snippetLines);
        }

        #endregion
    }
}
