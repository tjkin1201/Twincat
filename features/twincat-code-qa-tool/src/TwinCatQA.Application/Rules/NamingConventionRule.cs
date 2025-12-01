using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TwinCatQA.Domain.Contracts;
using TwinCatQA.Domain.Models;

namespace TwinCatQA.Application.Rules
{
    /// <summary>
    /// 명명 규칙 검증 규칙
    ///
    /// 헌장 원칙 5 (명명 규칙)를 강제합니다.
    /// Function Block, 변수 등의 이름이 프로젝트 명명 규칙을 따르는지 검증합니다.
    /// 일관된 명명 규칙은 코드 가독성과 유지보수성을 크게 향상시킵니다.
    /// </summary>
    public class NamingConventionRule : IValidationRule
    {
        private readonly IParserService _parserService;

        /// <summary>
        /// Function Block 접두사 검증 필수 여부 (기본: true)
        /// </summary>
        private bool _fbPrefixRequired = true;

        /// <summary>
        /// 변수 접두사 검증 필수 여부 (기본: true)
        /// </summary>
        private bool _varPrefixRequired = true;

        /// <summary>
        /// 카멜케이스/파스칼케이스 검증 여부 (기본: true)
        /// </summary>
        private bool _casingRequired = true;

        /// <summary>
        /// 허용되는 Function Block 접두사 목록
        /// </summary>
        private readonly HashSet<string> _allowedFbPrefixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "FB_", "FC_", "PRG_"
        };

        #region IValidationRule 속성

        /// <inheritdoc />
        public string RuleId => "FR-5-NAMING";

        /// <inheritdoc />
        public string RuleName => "명명 규칙 검증";

        /// <inheritdoc />
        public string Description =>
            "Function Block과 변수의 명명 규칙을 검증합니다. " +
            "FB는 FB_/FC_/PRG_ 접두사를 사용해야 하며, " +
            "변수는 스코프에 따라 적절한 접두사(i/o/g)를 사용해야 합니다.";

        /// <inheritdoc />
        public ConstitutionPrinciple RelatedPrinciple => ConstitutionPrinciple.NamingConvention;

        /// <inheritdoc />
        public ViolationSeverity DefaultSeverity => ViolationSeverity.Medium;

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
        /// NamingConventionRule 생성자
        /// </summary>
        /// <param name="parserService">파서 서비스 (의존성 주입)</param>
        /// <exception cref="ArgumentNullException">parserService가 null인 경우</exception>
        public NamingConventionRule(IParserService parserService)
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
                yield break;
            }

            // AST를 SyntaxTree로 변환
            var syntaxTree = file.Ast as SyntaxTree;
            if (syntaxTree == null || !syntaxTree.IsValid)
            {
                yield break;
            }

            // Function Block 명명 규칙 검증
            List<FunctionBlock> functionBlocks;
            try
            {
                functionBlocks = _parserService.ExtractFunctionBlocks(syntaxTree);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Function Block 추출 중 오류 발생: {ex.Message}");
                yield break;
            }

            foreach (var fb in functionBlocks)
            {
                foreach (var violation in ValidateFunctionBlockNaming(fb, file.FilePath, syntaxTree.SourceCode))
                {
                    yield return violation;
                }
            }

            // 변수 명명 규칙 검증
            List<Variable> variables;
            try
            {
                variables = _parserService.ExtractVariables(syntaxTree);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"변수 추출 중 오류 발생: {ex.Message}");
                yield break;
            }

            foreach (var variable in variables)
            {
                foreach (var violation in ValidateVariableNaming(variable, file.FilePath, syntaxTree.SourceCode))
                {
                    yield return violation;
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

            // fb_prefix_required 설정 로드
            if (parameters.ContainsKey("fb_prefix_required"))
            {
                if (parameters["fb_prefix_required"] is bool fbPrefixValue)
                {
                    _fbPrefixRequired = fbPrefixValue;
                }
            }

            // var_prefix_required 설정 로드
            if (parameters.ContainsKey("var_prefix_required"))
            {
                if (parameters["var_prefix_required"] is bool varPrefixValue)
                {
                    _varPrefixRequired = varPrefixValue;
                }
            }

            // casing_required 설정 로드
            if (parameters.ContainsKey("casing_required"))
            {
                if (parameters["casing_required"] is bool casingValue)
                {
                    _casingRequired = casingValue;
                }
            }
        }

        #region Private Helper Methods - Function Block Validation

        /// <summary>
        /// Function Block 명명 규칙을 검증합니다.
        /// </summary>
        private IEnumerable<Violation> ValidateFunctionBlockNaming(
            FunctionBlock fb,
            string filePath,
            string sourceCode)
        {
            if (string.IsNullOrWhiteSpace(fb.Name))
            {
                yield break;
            }

            // 접두사 검증
            if (_fbPrefixRequired)
            {
                bool hasValidPrefix = _allowedFbPrefixes.Any(prefix =>
                    fb.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

                if (!hasValidPrefix)
                {
                    yield return new Violation
                    {
                        RuleId = RuleId,
                        RuleName = RuleName,
                        RelatedPrinciple = RelatedPrinciple,
                        Severity = ViolationSeverity.Medium,
                        FilePath = filePath,
                        Line = fb.StartLine,
                        Column = 0,
                        CodeSnippet = ExtractSnippet(sourceCode, fb.StartLine),
                        Message = $"Function Block '{fb.Name}'의 이름이 명명 규칙을 따르지 않습니다. " +
                                  $"허용되는 접두사: {string.Join(", ", _allowedFbPrefixes)}",
                        Suggestion = $"Function Block 이름을 다음 형식으로 변경하세요:\n" +
                                     $"  - FB_{fb.Name} (Function Block)\n" +
                                     $"  - FC_{fb.Name} (Function)\n" +
                                     $"  - PRG_{fb.Name} (Program)",
                        DocumentationUrl = null
                    };
                }
            }

            // 파스칼케이스 검증 (접두사 제외)
            if (_casingRequired)
            {
                string nameWithoutPrefix = RemovePrefix(fb.Name);
                if (!IsPascalCase(nameWithoutPrefix))
                {
                    yield return new Violation
                    {
                        RuleId = RuleId,
                        RuleName = RuleName,
                        RelatedPrinciple = RelatedPrinciple,
                        Severity = ViolationSeverity.Low,
                        FilePath = filePath,
                        Line = fb.StartLine,
                        Column = 0,
                        CodeSnippet = ExtractSnippet(sourceCode, fb.StartLine),
                        Message = $"Function Block '{fb.Name}'이(가) 파스칼케이스(PascalCase)를 따르지 않습니다.",
                        Suggestion = $"파스칼케이스로 변경하세요. 예: {ConvertToPascalCase(nameWithoutPrefix)}",
                        DocumentationUrl = null
                    };
                }
            }
        }

        #endregion

        #region Private Helper Methods - Variable Validation

        /// <summary>
        /// 변수 명명 규칙을 검증합니다.
        /// </summary>
        private IEnumerable<Violation> ValidateVariableNaming(
            Variable variable,
            string filePath,
            string sourceCode)
        {
            if (string.IsNullOrWhiteSpace(variable.Name))
            {
                yield break;
            }

            // 접두사 검증
            if (_varPrefixRequired)
            {
                string expectedPrefix = GetExpectedVariablePrefix(variable.Scope);
                if (!string.IsNullOrEmpty(expectedPrefix))
                {
                    bool hasValidPrefix = variable.Name.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase) ||
                                          (variable.Scope == VariableScope.Input &&
                                           variable.Name.StartsWith("in", StringComparison.OrdinalIgnoreCase)) ||
                                          (variable.Scope == VariableScope.Output &&
                                           variable.Name.StartsWith("out", StringComparison.OrdinalIgnoreCase));

                    if (!hasValidPrefix)
                    {
                        yield return new Violation
                        {
                            RuleId = RuleId,
                            RuleName = RuleName,
                            RelatedPrinciple = RelatedPrinciple,
                            Severity = ViolationSeverity.Low,
                            FilePath = filePath,
                            Line = variable.DeclarationLine,
                            Column = 0,
                            CodeSnippet = ExtractSnippet(sourceCode, variable.DeclarationLine),
                            Message = $"{variable.Scope} 변수 '{variable.Name}'이(가) 명명 규칙을 따르지 않습니다. " +
                                      $"권장 접두사: '{expectedPrefix}'",
                            Suggestion = GenerateVariableNamingSuggestion(variable),
                            DocumentationUrl = null
                        };
                    }
                }
            }

            // 카멜케이스 검증 (로컬 변수)
            if (_casingRequired && variable.Scope == VariableScope.Local)
            {
                if (!IsCamelCase(variable.Name))
                {
                    yield return new Violation
                    {
                        RuleId = RuleId,
                        RuleName = RuleName,
                        RelatedPrinciple = RelatedPrinciple,
                        Severity = ViolationSeverity.Low,
                        FilePath = filePath,
                        Line = variable.DeclarationLine,
                        Column = 0,
                        CodeSnippet = ExtractSnippet(sourceCode, variable.DeclarationLine),
                        Message = $"로컬 변수 '{variable.Name}'이(가) 카멜케이스(camelCase)를 따르지 않습니다.",
                        Suggestion = $"카멜케이스로 변경하세요. 예: {ConvertToCamelCase(variable.Name)}",
                        DocumentationUrl = null
                    };
                }
            }

            // 변수 엔티티에 명명 규칙 준수 여부 저장
            variable.FollowsNamingConvention = true;
        }

        /// <summary>
        /// 변수 스코프에 따른 권장 접두사를 반환합니다.
        /// </summary>
        private string GetExpectedVariablePrefix(VariableScope scope)
        {
            return scope switch
            {
                VariableScope.Input => "i",
                VariableScope.Output => "o",
                VariableScope.Global => "g",
                _ => string.Empty
            };
        }

        /// <summary>
        /// 변수 명명 제안을 생성합니다.
        /// </summary>
        private string GenerateVariableNamingSuggestion(Variable variable)
        {
            string prefix = GetExpectedVariablePrefix(variable.Scope);
            string baseName = variable.Name;

            return variable.Scope switch
            {
                VariableScope.Input => $"입력 변수는 'i' 또는 'in' 접두사를 사용하세요.\n" +
                                       $"예: i{char.ToUpper(baseName[0])}{baseName.Substring(1)} 또는 in{char.ToUpper(baseName[0])}{baseName.Substring(1)}",
                VariableScope.Output => $"출력 변수는 'o' 또는 'out' 접두사를 사용하세요.\n" +
                                        $"예: o{char.ToUpper(baseName[0])}{baseName.Substring(1)} 또는 out{char.ToUpper(baseName[0])}{baseName.Substring(1)}",
                VariableScope.Global => $"전역 변수는 'g' 접두사를 사용하세요.\n" +
                                        $"예: g{char.ToUpper(baseName[0])}{baseName.Substring(1)}",
                VariableScope.Local => $"로컬 변수는 소문자로 시작하는 카멜케이스를 사용하세요.\n" +
                                       $"예: {ConvertToCamelCase(baseName)}",
                _ => "명명 규칙을 참조하여 적절한 이름을 지정하세요."
            };
        }

        #endregion

        #region Private Helper Methods - Casing Validation

        /// <summary>
        /// 파스칼케이스 검증 (첫 글자 대문자, 이후 카멜케이스)
        /// </summary>
        private bool IsPascalCase(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            // 첫 글자가 대문자인지 확인
            if (!char.IsUpper(name[0]))
            {
                return false;
            }

            // 언더스코어나 공백이 없는지 확인
            return !name.Contains('_') && !name.Contains(' ');
        }

        /// <summary>
        /// 카멜케이스 검증 (첫 글자 소문자, 이후 파스칼케이스)
        /// </summary>
        private bool IsCamelCase(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            // 첫 글자가 소문자인지 확인
            if (!char.IsLower(name[0]))
            {
                return false;
            }

            // 언더스코어나 공백이 없는지 확인
            return !name.Contains('_') && !name.Contains(' ');
        }

        /// <summary>
        /// Function Block 이름에서 접두사를 제거합니다.
        /// </summary>
        private string RemovePrefix(string name)
        {
            foreach (var prefix in _allowedFbPrefixes)
            {
                if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return name.Substring(prefix.Length);
                }
            }
            return name;
        }

        /// <summary>
        /// 문자열을 파스칼케이스로 변환합니다.
        /// </summary>
        private string ConvertToPascalCase(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            // 언더스코어 기준으로 분리
            string[] words = name.Split(new[] { '_', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var result = string.Join("", words.Select(w =>
                char.ToUpper(w[0]) + w.Substring(1).ToLower()));

            return result;
        }

        /// <summary>
        /// 문자열을 카멜케이스로 변환합니다.
        /// </summary>
        private string ConvertToCamelCase(string name)
        {
            string pascalCase = ConvertToPascalCase(name);
            if (string.IsNullOrEmpty(pascalCase))
            {
                return pascalCase;
            }

            return char.ToLower(pascalCase[0]) + pascalCase.Substring(1);
        }

        /// <summary>
        /// 지정된 라인의 코드 스니펫을 추출합니다.
        /// </summary>
        private string ExtractSnippet(string sourceCode, int lineNumber)
        {
            if (string.IsNullOrEmpty(sourceCode))
            {
                return string.Empty;
            }

            string[] lines = sourceCode.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            if (lineNumber < 1 || lineNumber > lines.Length)
            {
                return string.Empty;
            }

            int startLine = Math.Max(0, lineNumber - 3);
            int endLine = Math.Min(lines.Length - 1, lineNumber + 1);

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
