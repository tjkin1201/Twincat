using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;
using TwinCatQA.Core.Models;

namespace TwinCatQA.VsExtension.Editor
{
    /// <summary>
    /// 코드 품질 위반 사항을 나타내는 에러 태그
    /// </summary>
    public class ErrorTag : IErrorTag
    {
        /// <summary>
        /// 에러 타입
        /// </summary>
        public string ErrorType { get; }

        /// <summary>
        /// 툴팁 내용
        /// </summary>
        public object ToolTipContent { get; }

        /// <summary>
        /// 위반 사항 정보
        /// </summary>
        public Violation Violation { get; }

        /// <summary>
        /// ErrorTag 생성자
        /// </summary>
        /// <param name="violation">위반 사항</param>
        public ErrorTag(Violation violation)
        {
            Violation = violation;
            ErrorType = GetErrorType(violation.Severity);
            ToolTipContent = CreateToolTipContent(violation);
        }

        /// <summary>
        /// 심각도에 따른 에러 타입 반환
        /// </summary>
        /// <param name="severity">심각도</param>
        /// <returns>에러 타입 문자열</returns>
        private static string GetErrorType(ViolationSeverity severity)
        {
            switch (severity)
            {
                case ViolationSeverity.Error:
                    return PredefinedErrorTypeNames.SyntaxError;
                case ViolationSeverity.Warning:
                    return PredefinedErrorTypeNames.Warning;
                case ViolationSeverity.Information:
                    return PredefinedErrorTypeNames.Suggestion;
                default:
                    return PredefinedErrorTypeNames.OtherError;
            }
        }

        /// <summary>
        /// 툴팁 내용 생성
        /// </summary>
        /// <param name="violation">위반 사항</param>
        /// <returns>툴팁 내용</returns>
        private static string CreateToolTipContent(Violation violation)
        {
            var tooltip = $"[{violation.RuleId}] {violation.Message}";

            if (!string.IsNullOrEmpty(violation.Category))
            {
                tooltip += $"\n\n카테고리: {violation.Category}";
            }

            if (!string.IsNullOrEmpty(violation.FixDescription))
            {
                tooltip += $"\n\n수정 제안:\n{violation.FixDescription}";
            }

            if (!string.IsNullOrEmpty(violation.HelpUrl))
            {
                tooltip += $"\n\n자세한 정보: {violation.HelpUrl}";
            }

            return tooltip;
        }
    }
}
