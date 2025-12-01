using System;
using System.IO;

namespace TwinCatQA.Application.Configuration
{
    /// <summary>
    /// ConfigurationService의 확장 메서드를 제공하는 클래스
    /// </summary>
    public static class ConfigurationServiceExtensions
    {
        /// <summary>
        /// 기본 설정 파일을 프로젝트에 생성합니다.
        /// </summary>
        /// <param name="service">설정 서비스 인스턴스</param>
        /// <param name="projectPath">TwinCAT 프로젝트 루트 경로</param>
        /// <param name="overwriteExisting">기존 파일 덮어쓰기 여부 (기본값: false)</param>
        /// <exception cref="InvalidOperationException">파일이 이미 존재하고 overwriteExisting이 false인 경우</exception>
        public static void InitializeSettingsFile(
            this ConfigurationService service,
            string projectPath,
            bool overwriteExisting = false)
        {
            if (string.IsNullOrWhiteSpace(projectPath))
            {
                throw new ArgumentNullException(nameof(projectPath), "프로젝트 경로가 null이거나 비어있습니다.");
            }

            if (!overwriteExisting && service.SettingsFileExists(projectPath))
            {
                throw new InvalidOperationException(
                    "설정 파일이 이미 존재합니다. overwriteExisting 매개변수를 true로 설정하여 덮어쓸 수 있습니다.");
            }

            // 기본 설정 생성 및 저장
            var defaultSettings = service.GetDefaultSettings();
            service.SaveSettings(projectPath, defaultSettings);
        }

        /// <summary>
        /// 특정 규칙의 활성화 여부를 변경합니다.
        /// </summary>
        /// <param name="settings">설정 객체</param>
        /// <param name="ruleId">규칙 ID</param>
        /// <param name="enabled">활성화 여부</param>
        /// <returns>변경된 설정 객체 (메서드 체이닝용)</returns>
        public static QualitySettings SetRuleEnabled(
            this QualitySettings settings,
            string ruleId,
            bool enabled)
        {
            if (settings.Rules.Configurations.ContainsKey(ruleId))
            {
                settings.Rules.Configurations[ruleId].Enabled = enabled;
            }
            else
            {
                // 규칙이 없으면 새로 추가
                settings.Rules.Configurations[ruleId] = new RuleConfiguration
                {
                    Enabled = enabled
                };
            }

            return settings;
        }

        /// <summary>
        /// 특정 규칙의 심각도를 변경합니다.
        /// </summary>
        /// <param name="settings">설정 객체</param>
        /// <param name="ruleId">규칙 ID</param>
        /// <param name="severity">새로운 심각도</param>
        /// <returns>변경된 설정 객체 (메서드 체이닝용)</returns>
        public static QualitySettings SetRuleSeverity(
            this QualitySettings settings,
            string ruleId,
            ViolationSeverity severity)
        {
            if (settings.Rules.Configurations.ContainsKey(ruleId))
            {
                settings.Rules.Configurations[ruleId].Severity = severity;
            }
            else
            {
                settings.Rules.Configurations[ruleId] = new RuleConfiguration
                {
                    Severity = severity
                };
            }

            return settings;
        }

        /// <summary>
        /// 특정 규칙의 매개변수 값을 설정합니다.
        /// </summary>
        /// <param name="settings">설정 객체</param>
        /// <param name="ruleId">규칙 ID</param>
        /// <param name="parameterName">매개변수 이름</param>
        /// <param name="value">매개변수 값</param>
        /// <returns>변경된 설정 객체 (메서드 체이닝용)</returns>
        public static QualitySettings SetRuleParameter(
            this QualitySettings settings,
            string ruleId,
            string parameterName,
            object value)
        {
            if (!settings.Rules.Configurations.ContainsKey(ruleId))
            {
                settings.Rules.Configurations[ruleId] = new RuleConfiguration();
            }

            settings.Rules.Configurations[ruleId].Parameters[parameterName] = value;

            return settings;
        }

        /// <summary>
        /// 설정 파일의 유효성을 검사합니다.
        /// </summary>
        /// <param name="settings">검사할 설정 객체</param>
        /// <returns>유효성 검사 결과</returns>
        public static ValidationResult Validate(this QualitySettings settings)
        {
            var result = new ValidationResult { IsValid = true };

            // Global 설정 검증
            if (settings.Global.MaxDegreeOfParallelism < 1)
            {
                result.IsValid = false;
                result.Errors.Add("병렬 처리 최대 스레드 수는 1 이상이어야 합니다.");
            }

            if (settings.Global.TimeoutSeconds < 1)
            {
                result.IsValid = false;
                result.Errors.Add("타임아웃은 1초 이상이어야 합니다.");
            }

            // Reports 설정 검증
            if (string.IsNullOrWhiteSpace(settings.Reports.OutputPath))
            {
                result.IsValid = false;
                result.Errors.Add("보고서 출력 경로는 필수입니다.");
            }

            if (settings.Reports.KeepReportsCount < 0)
            {
                result.IsValid = false;
                result.Errors.Add("보관할 보고서 개수는 0 이상이어야 합니다.");
            }

            // Rules 설정 검증
            foreach (var rule in settings.Rules.Configurations)
            {
                if (string.IsNullOrWhiteSpace(rule.Key))
                {
                    result.IsValid = false;
                    result.Errors.Add("규칙 ID는 비어있을 수 없습니다.");
                }
            }

            return result;
        }

        /// <summary>
        /// 설정 객체를 사람이 읽기 쉬운 문자열로 변환합니다.
        /// </summary>
        /// <param name="settings">설정 객체</param>
        /// <returns>설정 요약 문자열</returns>
        public static string ToSummaryString(this QualitySettings settings)
        {
            var summary = $@"
TwinCAT 코드 품질 검증 도구 설정 요약
=====================================

[전역 설정]
- 검증 모드: {settings.Global.DefaultMode}
- 병렬 처리: {(settings.Global.EnableParallelProcessing ? "활성화" : "비활성화")} (최대 {settings.Global.MaxDegreeOfParallelism} 스레드)
- 타임아웃: {settings.Global.TimeoutSeconds}초
- 로그 수준: {settings.Global.LogLevel}

[규칙 설정]
- 활성화된 규칙 수: {CountEnabledRules(settings.Rules)}개
- 전체 규칙 수: {settings.Rules.Configurations.Count}개

[보고서 설정]
- HTML: {(settings.Reports.GenerateHtml ? "생성" : "생성 안 함")}
- PDF: {(settings.Reports.GeneratePdf ? "생성" : "생성 안 함")}
- JSON: {(settings.Reports.GenerateJson ? "생성" : "생성 안 함")}
- 출력 경로: {settings.Reports.OutputPath}

[Git 통합]
- Pre-commit 훅: {(settings.Git.EnablePreCommitHook ? "활성화" : "비활성화")}
- Critical 위반 차단: {(settings.Git.BlockOnCriticalViolations ? "활성화" : "비활성화")}
- 증분 모드: {(settings.Git.IncrementalMode ? "활성화" : "비활성화")}
";

            return summary.Trim();
        }

        /// <summary>
        /// 활성화된 규칙 개수를 계산합니다.
        /// </summary>
        private static int CountEnabledRules(RuleSettings ruleSettings)
        {
            int count = 0;
            foreach (var rule in ruleSettings.Configurations.Values)
            {
                if (rule.Enabled)
                {
                    count++;
                }
            }
            return count;
        }
    }

    /// <summary>
    /// 설정 유효성 검사 결과
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// 유효성 검사 통과 여부
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 오류 메시지 목록
        /// </summary>
        public System.Collections.Generic.List<string> Errors { get; set; } = new();

        /// <summary>
        /// 경고 메시지 목록
        /// </summary>
        public System.Collections.Generic.List<string> Warnings { get; set; } = new();
    }
}
