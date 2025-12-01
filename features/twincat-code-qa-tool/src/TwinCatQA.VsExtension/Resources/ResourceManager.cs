using System;
using System.Globalization;
using System.Resources;
using System.Threading;

namespace TwinCatQA.VsExtension.Resources
{
    /// <summary>
    /// 리소스 문자열 관리 클래스
    /// 다국어 지원 및 동적 언어 전환 기능 제공
    /// </summary>
    public static class ResourceManager
    {
        private static System.Resources.ResourceManager _resourceManager;
        private static CultureInfo _currentCulture;

        /// <summary>
        /// 정적 생성자 - 리소스 매니저 초기화
        /// </summary>
        static ResourceManager()
        {
            _resourceManager = new System.Resources.ResourceManager(
                "TwinCatQA.VsExtension.Resources.Strings",
                typeof(ResourceManager).Assembly);

            // 시스템 기본 언어 설정
            _currentCulture = Thread.CurrentThread.CurrentUICulture;
        }

        /// <summary>
        /// 현재 언어 문화권 가져오기
        /// </summary>
        public static CultureInfo CurrentCulture
        {
            get => _currentCulture;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                _currentCulture = value;
                Thread.CurrentThread.CurrentUICulture = value;
                OnCultureChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 언어 변경 이벤트
        /// </summary>
        public static event EventHandler OnCultureChanged;

        /// <summary>
        /// 리소스 문자열 가져오기
        /// </summary>
        /// <param name="key">리소스 키</param>
        /// <returns>리소스 문자열 또는 기본값</returns>
        public static string GetString(string key)
        {
            return GetString(key, _currentCulture);
        }

        /// <summary>
        /// 특정 문화권의 리소스 문자열 가져오기
        /// </summary>
        /// <param name="key">리소스 키</param>
        /// <param name="culture">문화권 정보</param>
        /// <returns>리소스 문자열 또는 기본값</returns>
        public static string GetString(string key, CultureInfo culture)
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;

            try
            {
                var value = _resourceManager.GetString(key, culture);

                // 리소스를 찾지 못한 경우 키 반환
                if (value == null)
                {
                    System.Diagnostics.Debug.WriteLine($"리소스 키를 찾을 수 없습니다: {key}");
                    return $"[{key}]";
                }

                return value;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"리소스 로드 오류 - 키: {key}, 오류: {ex.Message}");
                return $"[{key}]";
            }
        }

        /// <summary>
        /// 형식화된 리소스 문자열 가져오기
        /// </summary>
        /// <param name="key">리소스 키</param>
        /// <param name="args">형식 인자</param>
        /// <returns>형식화된 리소스 문자열</returns>
        public static string GetFormattedString(string key, params object[] args)
        {
            return GetFormattedString(key, _currentCulture, args);
        }

        /// <summary>
        /// 특정 문화권의 형식화된 리소스 문자열 가져오기
        /// </summary>
        /// <param name="key">리소스 키</param>
        /// <param name="culture">문화권 정보</param>
        /// <param name="args">형식 인자</param>
        /// <returns>형식화된 리소스 문자열</returns>
        public static string GetFormattedString(string key, CultureInfo culture, params object[] args)
        {
            var format = GetString(key, culture);

            if (args == null || args.Length == 0)
                return format;

            try
            {
                return string.Format(culture, format, args);
            }
            catch (FormatException ex)
            {
                System.Diagnostics.Debug.WriteLine($"문자열 형식화 오류 - 키: {key}, 오류: {ex.Message}");
                return format;
            }
        }

        /// <summary>
        /// 언어를 한글로 설정
        /// </summary>
        public static void SetKoreanLanguage()
        {
            CurrentCulture = new CultureInfo("ko-KR");
        }

        /// <summary>
        /// 언어를 영어로 설정
        /// </summary>
        public static void SetEnglishLanguage()
        {
            CurrentCulture = new CultureInfo("en-US");
        }

        /// <summary>
        /// 시스템 기본 언어로 설정
        /// </summary>
        public static void SetDefaultLanguage()
        {
            CurrentCulture = CultureInfo.InstalledUICulture;
        }

        /// <summary>
        /// 지원되는 언어 목록 가져오기
        /// </summary>
        /// <returns>지원 언어 배열</returns>
        public static CultureInfo[] GetSupportedCultures()
        {
            return new[]
            {
                new CultureInfo("ko-KR"), // 한글
                new CultureInfo("en-US")  // 영어
            };
        }

        /// <summary>
        /// 현재 언어가 한글인지 확인
        /// </summary>
        public static bool IsKorean => _currentCulture.TwoLetterISOLanguageName == "ko";

        /// <summary>
        /// 현재 언어가 영어인지 확인
        /// </summary>
        public static bool IsEnglish => _currentCulture.TwoLetterISOLanguageName == "en";
    }

    /// <summary>
    /// 리소스 문자열 접근을 위한 확장 클래스
    /// 강타입 리소스 키 제공
    /// </summary>
    public static class Strings
    {
        // UI 텍스트 - 버튼
        public static string Button_Analyze => ResourceManager.GetString("Button_Analyze");
        public static string Button_Cancel => ResourceManager.GetString("Button_Cancel");
        public static string Button_OK => ResourceManager.GetString("Button_OK");
        public static string Button_Apply => ResourceManager.GetString("Button_Apply");
        public static string Button_Close => ResourceManager.GetString("Button_Close");
        public static string Button_Save => ResourceManager.GetString("Button_Save");
        public static string Button_Load => ResourceManager.GetString("Button_Load");
        public static string Button_Export => ResourceManager.GetString("Button_Export");
        public static string Button_Import => ResourceManager.GetString("Button_Import");
        public static string Button_Refresh => ResourceManager.GetString("Button_Refresh");

        // UI 텍스트 - 레이블
        public static string Label_FileName => ResourceManager.GetString("Label_FileName");
        public static string Label_Severity => ResourceManager.GetString("Label_Severity");
        public static string Label_Line => ResourceManager.GetString("Label_Line");
        public static string Label_Message => ResourceManager.GetString("Label_Message");
        public static string Label_RuleName => ResourceManager.GetString("Label_RuleName");
        public static string Label_Category => ResourceManager.GetString("Label_Category");
        public static string Label_Status => ResourceManager.GetString("Label_Status");
        public static string Label_Progress => ResourceManager.GetString("Label_Progress");

        // UI 텍스트 - 메뉴
        public static string Menu_File => ResourceManager.GetString("Menu_File");
        public static string Menu_Edit => ResourceManager.GetString("Menu_Edit");
        public static string Menu_View => ResourceManager.GetString("Menu_View");
        public static string Menu_Tools => ResourceManager.GetString("Menu_Tools");
        public static string Menu_Help => ResourceManager.GetString("Menu_Help");
        public static string Menu_Options => ResourceManager.GetString("Menu_Options");

        // UI 텍스트 - 툴팁
        public static string Tooltip_Analyze => ResourceManager.GetString("Tooltip_Analyze");
        public static string Tooltip_Refresh => ResourceManager.GetString("Tooltip_Refresh");
        public static string Tooltip_Export => ResourceManager.GetString("Tooltip_Export");
        public static string Tooltip_Filter => ResourceManager.GetString("Tooltip_Filter");

        // 메시지 - 오류
        public static string Error_FileNotFound(string fileName) =>
            ResourceManager.GetFormattedString("Error_FileNotFound", fileName);
        public static string Error_InvalidFile(string fileName) =>
            ResourceManager.GetFormattedString("Error_InvalidFile", fileName);
        public static string Error_AnalysisFailed(string error) =>
            ResourceManager.GetFormattedString("Error_AnalysisFailed", error);
        public static string Error_SaveFailed(string error) =>
            ResourceManager.GetFormattedString("Error_SaveFailed", error);
        public static string Error_LoadFailed(string error) =>
            ResourceManager.GetFormattedString("Error_LoadFailed", error);
        public static string Error_UnexpectedError(string error) =>
            ResourceManager.GetFormattedString("Error_UnexpectedError", error);
        public static string Error_AccessDenied(string fileName) =>
            ResourceManager.GetFormattedString("Error_AccessDenied", fileName);

        // 메시지 - 경고
        public static string Warning_NoIssuesFound => ResourceManager.GetString("Warning_NoIssuesFound");
        public static string Warning_UnsavedChanges => ResourceManager.GetString("Warning_UnsavedChanges");
        public static string Warning_LargeFile => ResourceManager.GetString("Warning_LargeFile");
        public static string Warning_RuleDisabled => ResourceManager.GetString("Warning_RuleDisabled");

        // 메시지 - 정보
        public static string Info_AnalysisComplete => ResourceManager.GetString("Info_AnalysisComplete");
        public static string Info_IssuesFound(int count) =>
            ResourceManager.GetFormattedString("Info_IssuesFound", count);
        public static string Info_FileSaved(string fileName) =>
            ResourceManager.GetFormattedString("Info_FileSaved", fileName);
        public static string Info_ExportComplete(string fileName) =>
            ResourceManager.GetFormattedString("Info_ExportComplete", fileName);
        public static string Info_AnalysisProgress(int current, int total) =>
            ResourceManager.GetFormattedString("Info_AnalysisProgress", current, total);

        // 메시지 - 확인
        public static string Confirm_DeleteRule => ResourceManager.GetString("Confirm_DeleteRule");
        public static string Confirm_ResetSettings => ResourceManager.GetString("Confirm_ResetSettings");
        public static string Confirm_ClearResults => ResourceManager.GetString("Confirm_ClearResults");

        // 규칙 이름
        public static string Rule_NamingConvention => ResourceManager.GetString("Rule_NamingConvention");
        public static string Rule_UnusedVariable => ResourceManager.GetString("Rule_UnusedVariable");
        public static string Rule_MagicNumber => ResourceManager.GetString("Rule_MagicNumber");
        public static string Rule_FunctionComplexity => ResourceManager.GetString("Rule_FunctionComplexity");
        public static string Rule_MissingComment => ResourceManager.GetString("Rule_MissingComment");
        public static string Rule_LongFunction => ResourceManager.GetString("Rule_LongFunction");
        public static string Rule_DeepNesting => ResourceManager.GetString("Rule_DeepNesting");

        // 규칙 설명
        public static string RuleDesc_NamingConvention => ResourceManager.GetString("RuleDesc_NamingConvention");
        public static string RuleDesc_UnusedVariable => ResourceManager.GetString("RuleDesc_UnusedVariable");
        public static string RuleDesc_MagicNumber => ResourceManager.GetString("RuleDesc_MagicNumber");
        public static string RuleDesc_FunctionComplexity => ResourceManager.GetString("RuleDesc_FunctionComplexity");
        public static string RuleDesc_MissingComment => ResourceManager.GetString("RuleDesc_MissingComment");
        public static string RuleDesc_LongFunction => ResourceManager.GetString("RuleDesc_LongFunction");
        public static string RuleDesc_DeepNesting => ResourceManager.GetString("RuleDesc_DeepNesting");

        // 상태 메시지
        public static string Status_Ready => ResourceManager.GetString("Status_Ready");
        public static string Status_Analyzing => ResourceManager.GetString("Status_Analyzing");
        public static string Status_Complete => ResourceManager.GetString("Status_Complete");
        public static string Status_Failed => ResourceManager.GetString("Status_Failed");
        public static string Status_Cancelled => ResourceManager.GetString("Status_Cancelled");

        // 심각도
        public static string Severity_Error => ResourceManager.GetString("Severity_Error");
        public static string Severity_Warning => ResourceManager.GetString("Severity_Warning");
        public static string Severity_Info => ResourceManager.GetString("Severity_Info");
        public static string Severity_Hint => ResourceManager.GetString("Severity_Hint");

        // 카테고리
        public static string Category_Style => ResourceManager.GetString("Category_Style");
        public static string Category_Performance => ResourceManager.GetString("Category_Performance");
        public static string Category_Security => ResourceManager.GetString("Category_Security");
        public static string Category_Maintainability => ResourceManager.GetString("Category_Maintainability");
    }
}
