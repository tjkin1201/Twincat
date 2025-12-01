using System;
using System.Text.RegularExpressions;

namespace TwinCatQA.Application.Services
{
    /// <summary>
    /// 코드 하이라이팅 유틸리티
    /// </summary>
    public class CodeHighlighter
    {
        /// <summary>
        /// Structured Text 코드를 HTML 하이라이팅
        /// </summary>
        /// <param name="code">원본 ST 코드</param>
        /// <returns>하이라이팅 적용된 HTML</returns>
        public string HighlightStructuredText(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return string.Empty;
            }

            var highlighted = code;

            // HTML 이스케이프
            highlighted = System.Web.HttpUtility.HtmlEncode(highlighted);

            // 키워드 하이라이팅
            highlighted = HighlightKeywords(highlighted);

            // 주석 하이라이팅
            highlighted = HighlightComments(highlighted);

            // 문자열 하이라이팅
            highlighted = HighlightStrings(highlighted);

            // 숫자 하이라이팅
            highlighted = HighlightNumbers(highlighted);

            return $"<pre class=\"code-highlight\"><code>{highlighted}</code></pre>";
        }

        /// <summary>
        /// ST 키워드 하이라이팅
        /// </summary>
        private string HighlightKeywords(string code)
        {
            var keywords = new[]
            {
                // 제어 구조
                "IF", "THEN", "ELSIF", "ELSE", "END_IF",
                "CASE", "OF", "END_CASE",
                "FOR", "TO", "BY", "DO", "END_FOR",
                "WHILE", "END_WHILE",
                "REPEAT", "UNTIL", "END_REPEAT",

                // 선언
                "VAR", "VAR_INPUT", "VAR_OUTPUT", "VAR_IN_OUT", "VAR_TEMP",
                "VAR_GLOBAL", "VAR_EXTERNAL", "END_VAR",
                "PROGRAM", "END_PROGRAM",
                "FUNCTION", "END_FUNCTION",
                "FUNCTION_BLOCK", "END_FUNCTION_BLOCK",
                "METHOD", "END_METHOD",
                "PROPERTY", "END_PROPERTY",
                "INTERFACE", "END_INTERFACE",
                "ACTION", "END_ACTION",

                // 데이터 타입
                "BOOL", "BYTE", "WORD", "DWORD", "LWORD",
                "SINT", "USINT", "INT", "UINT", "DINT", "UDINT", "LINT", "ULINT",
                "REAL", "LREAL",
                "STRING", "WSTRING",
                "TIME", "DATE", "TIME_OF_DAY", "DATE_AND_TIME",
                "ARRAY", "POINTER", "REFERENCE",
                "STRUCT", "END_STRUCT",
                "ENUM", "END_ENUM",
                "TYPE", "END_TYPE",

                // 기타
                "RETURN", "EXIT",
                "CONST", "RETAIN", "PERSISTENT",
                "AT", "REF", "REF_TO",
                "THIS", "SUPER",
                "ABSTRACT", "FINAL", "EXTENDS", "IMPLEMENTS"
            };

            var highlighted = code;
            foreach (var keyword in keywords)
            {
                var pattern = $@"\b{keyword}\b";
                highlighted = Regex.Replace(
                    highlighted,
                    pattern,
                    $"<span class=\"keyword\">{keyword}</span>",
                    RegexOptions.IgnoreCase
                );
            }

            return highlighted;
        }

        /// <summary>
        /// 주석 하이라이팅
        /// </summary>
        private string HighlightComments(string code)
        {
            // 한 줄 주석 (//)
            var highlighted = Regex.Replace(
                code,
                @"(//.*?)(&lt;br&gt;|$)",
                "<span class=\"comment\">$1</span>$2"
            );

            // 블록 주석 (* ... *)
            highlighted = Regex.Replace(
                highlighted,
                @"\(\*(.*?)\*\)",
                "<span class=\"comment\">(*$1*)</span>",
                RegexOptions.Singleline
            );

            return highlighted;
        }

        /// <summary>
        /// 문자열 하이라이팅
        /// </summary>
        private string HighlightStrings(string code)
        {
            // 작은따옴표 문자열
            return Regex.Replace(
                code,
                @"'([^']*)'",
                "<span class=\"string\">'$1'</span>"
            );
        }

        /// <summary>
        /// 숫자 하이라이팅
        /// </summary>
        private string HighlightNumbers(string code)
        {
            // 정수 및 실수
            return Regex.Replace(
                code,
                @"\b(\d+\.?\d*)\b",
                "<span class=\"number\">$1</span>"
            );
        }

        /// <summary>
        /// 일반 코드 하이라이팅 (다른 언어)
        /// </summary>
        public string HighlightCode(string code, string language)
        {
            return language.ToLowerInvariant() switch
            {
                "st" or "structured-text" => HighlightStructuredText(code),
                _ => $"<pre><code>{System.Web.HttpUtility.HtmlEncode(code)}</code></pre>"
            };
        }
    }
}
