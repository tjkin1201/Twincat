using System;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace TwinCatQA.Application.Configuration
{
    /// <summary>
    /// 설정 파일 로드 및 저장을 담당하는 서비스 클래스
    /// </summary>
    public class ConfigurationService
    {
        private readonly IDeserializer _deserializer;
        private readonly ISerializer _serializer;

        /// <summary>
        /// 설정 파일 기본 경로 (.twincat-qa/settings.yml)
        /// </summary>
        private const string DefaultSettingsFileName = "settings.yml";

        /// <summary>
        /// 설정 디렉토리 이름
        /// </summary>
        private const string SettingsDirectoryName = ".twincat-qa";

        /// <summary>
        /// ConfigurationService 생성자
        /// YamlDotNet의 직렬화/역직렬화 엔진을 초기화합니다.
        /// </summary>
        public ConfigurationService()
        {
            // YAML 역직렬화 설정 (camelCase 네이밍 컨벤션 사용)
            _deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties() // 알 수 없는 속성 무시
                .Build();

            // YAML 직렬화 설정 (camelCase 네이밍 컨벤션 사용)
            _serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults) // 기본값은 출력하지 않음
                .Build();
        }

        /// <summary>
        /// 프로젝트 경로에서 설정 파일을 로드합니다.
        /// 파일이 없으면 기본 설정을 반환합니다.
        /// </summary>
        /// <param name="projectPath">TwinCAT 프로젝트 루트 경로</param>
        /// <returns>로드된 설정 객체</returns>
        /// <exception cref="ArgumentNullException">projectPath가 null인 경우</exception>
        /// <exception cref="ConfigurationException">설정 파일 로드 실패 시</exception>
        public QualitySettings LoadSettings(string projectPath)
        {
            if (string.IsNullOrWhiteSpace(projectPath))
            {
                throw new ArgumentNullException(nameof(projectPath), "프로젝트 경로가 null이거나 비어있습니다.");
            }

            var settingsFilePath = GetSettingsFilePath(projectPath);

            // 설정 파일이 없으면 기본 설정 반환
            if (!File.Exists(settingsFilePath))
            {
                return GetDefaultSettings();
            }

            try
            {
                // YAML 파일 읽기
                var yamlContent = File.ReadAllText(settingsFilePath);

                // YAML을 객체로 역직렬화
                var settings = _deserializer.Deserialize<QualitySettings>(yamlContent);

                // null인 경우 기본값 반환
                if (settings == null)
                {
                    return GetDefaultSettings();
                }

                // 부분 설정을 기본값과 병합
                return MergeWithDefaults(settings);
            }
            catch (YamlDotNet.Core.YamlException ex)
            {
                throw new ConfigurationException(
                    $"YAML 파일 파싱 중 오류가 발생했습니다: {settingsFilePath}", ex);
            }
            catch (IOException ex)
            {
                throw new ConfigurationException(
                    $"설정 파일 읽기 중 오류가 발생했습니다: {settingsFilePath}", ex);
            }
            catch (Exception ex)
            {
                throw new ConfigurationException(
                    $"설정 파일 로드 중 예기치 않은 오류가 발생했습니다: {settingsFilePath}", ex);
            }
        }

        /// <summary>
        /// 설정을 YAML 파일로 저장합니다.
        /// </summary>
        /// <param name="projectPath">TwinCAT 프로젝트 루트 경로</param>
        /// <param name="settings">저장할 설정 객체</param>
        /// <exception cref="ArgumentNullException">매개변수가 null인 경우</exception>
        /// <exception cref="ConfigurationException">설정 파일 저장 실패 시</exception>
        public void SaveSettings(string projectPath, QualitySettings settings)
        {
            if (string.IsNullOrWhiteSpace(projectPath))
            {
                throw new ArgumentNullException(nameof(projectPath), "프로젝트 경로가 null이거나 비어있습니다.");
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings), "설정 객체가 null입니다.");
            }

            var settingsDirectoryPath = GetSettingsDirectoryPath(projectPath);
            var settingsFilePath = GetSettingsFilePath(projectPath);

            try
            {
                // 설정 디렉토리 생성 (없는 경우)
                if (!Directory.Exists(settingsDirectoryPath))
                {
                    Directory.CreateDirectory(settingsDirectoryPath);
                }

                // 객체를 YAML로 직렬화
                var yamlContent = _serializer.Serialize(settings);

                // 파일에 쓰기
                File.WriteAllText(settingsFilePath, yamlContent);
            }
            catch (IOException ex)
            {
                throw new ConfigurationException(
                    $"설정 파일 저장 중 오류가 발생했습니다: {settingsFilePath}", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new ConfigurationException(
                    $"설정 파일 저장 권한이 없습니다: {settingsFilePath}", ex);
            }
            catch (Exception ex)
            {
                throw new ConfigurationException(
                    $"설정 파일 저장 중 예기치 않은 오류가 발생했습니다: {settingsFilePath}", ex);
            }
        }

        /// <summary>
        /// 기본 설정 객체를 반환합니다.
        /// </summary>
        /// <returns>기본값으로 초기화된 설정 객체</returns>
        public QualitySettings GetDefaultSettings()
        {
            var settings = new QualitySettings
            {
                Global = new GlobalSettings
                {
                    DefaultMode = ValidationMode.Full,
                    EnableParallelProcessing = true,
                    MaxDegreeOfParallelism = 4,
                    TimeoutSeconds = 300,
                    LogLevel = "Info"
                },
                Rules = new RuleSettings
                {
                    EnableAllRules = true,
                    Configurations = GetDefaultRuleConfigurations()
                },
                Reports = new ReportSettings
                {
                    GenerateHtml = true,
                    GeneratePdf = false,
                    GenerateJson = true,
                    OutputPath = ".twincat-qa/reports",
                    IncludeSourceCode = true,
                    FileNameTemplate = "report-{timestamp}",
                    KeepReportsCount = 10
                },
                Git = new GitSettings
                {
                    EnablePreCommitHook = false,
                    BlockOnCriticalViolations = true,
                    BlockOnHighViolations = false,
                    IncrementalMode = true,
                    HooksPath = string.Empty
                }
            };

            return settings;
        }

        /// <summary>
        /// 부분 설정을 기본값과 병합합니다.
        /// null인 속성은 기본값으로 채웁니다.
        /// </summary>
        /// <param name="settings">사용자 정의 설정</param>
        /// <returns>기본값과 병합된 설정</returns>
        public QualitySettings MergeWithDefaults(QualitySettings settings)
        {
            var defaultSettings = GetDefaultSettings();

            // Global 설정 병합
            if (settings.Global == null)
            {
                settings.Global = defaultSettings.Global;
            }

            // Rules 설정 병합
            if (settings.Rules == null)
            {
                settings.Rules = defaultSettings.Rules;
            }
            else if (settings.Rules.Configurations == null)
            {
                settings.Rules.Configurations = defaultSettings.Rules.Configurations;
            }
            else
            {
                // 기본 규칙 설정을 사용자 설정과 병합
                foreach (var defaultRule in defaultSettings.Rules.Configurations)
                {
                    if (!settings.Rules.Configurations.ContainsKey(defaultRule.Key))
                    {
                        settings.Rules.Configurations[defaultRule.Key] = defaultRule.Value;
                    }
                }
            }

            // Reports 설정 병합
            if (settings.Reports == null)
            {
                settings.Reports = defaultSettings.Reports;
            }

            // Git 설정 병합
            if (settings.Git == null)
            {
                settings.Git = defaultSettings.Git;
            }

            return settings;
        }

        /// <summary>
        /// 설정 파일이 존재하는지 확인합니다.
        /// </summary>
        /// <param name="projectPath">TwinCAT 프로젝트 루트 경로</param>
        /// <returns>설정 파일 존재 여부</returns>
        public bool SettingsFileExists(string projectPath)
        {
            if (string.IsNullOrWhiteSpace(projectPath))
            {
                return false;
            }

            var settingsFilePath = GetSettingsFilePath(projectPath);
            return File.Exists(settingsFilePath);
        }

        /// <summary>
        /// 기본 규칙 설정을 생성합니다.
        /// </summary>
        /// <returns>규칙별 기본 설정 딕셔너리</returns>
        private System.Collections.Generic.Dictionary<string, RuleConfiguration> GetDefaultRuleConfigurations()
        {
            return new System.Collections.Generic.Dictionary<string, RuleConfiguration>
            {
                ["FR-2-KOREAN-COMMENT"] = new RuleConfiguration
                {
                    Enabled = true,
                    Severity = ViolationSeverity.High,
                    Parameters = new System.Collections.Generic.Dictionary<string, object>
                    {
                        ["requiredKoreanRatio"] = 0.95,
                        ["allowEnglishForTechnical"] = true
                    }
                },
                ["FR-7-CYCLOMATIC-COMPLEXITY"] = new RuleConfiguration
                {
                    Enabled = true,
                    Severity = ViolationSeverity.Medium,
                    Parameters = new System.Collections.Generic.Dictionary<string, object>
                    {
                        ["mediumThreshold"] = 10,
                        ["highThreshold"] = 15,
                        ["criticalThreshold"] = 20
                    }
                },
                ["FR-8-NAMING-CONVENTION"] = new RuleConfiguration
                {
                    Enabled = true,
                    Severity = ViolationSeverity.Medium,
                    Parameters = new System.Collections.Generic.Dictionary<string, object>
                    {
                        ["fbPrefixRequired"] = true,
                        ["varPrefixRequired"] = true,
                        ["strictPascalCase"] = true
                    }
                }
            };
        }

        /// <summary>
        /// 설정 디렉토리 전체 경로를 반환합니다.
        /// </summary>
        /// <param name="projectPath">프로젝트 루트 경로</param>
        /// <returns>설정 디렉토리 경로</returns>
        private string GetSettingsDirectoryPath(string projectPath)
        {
            return Path.Combine(projectPath, SettingsDirectoryName);
        }

        /// <summary>
        /// 설정 파일 전체 경로를 반환합니다.
        /// </summary>
        /// <param name="projectPath">프로젝트 루트 경로</param>
        /// <returns>설정 파일 경로</returns>
        private string GetSettingsFilePath(string projectPath)
        {
            return Path.Combine(GetSettingsDirectoryPath(projectPath), DefaultSettingsFileName);
        }
    }

    /// <summary>
    /// 설정 관련 예외 클래스
    /// </summary>
    public class ConfigurationException : Exception
    {
        /// <summary>
        /// ConfigurationException 생성자
        /// </summary>
        /// <param name="message">예외 메시지</param>
        public ConfigurationException(string message) : base(message)
        {
        }

        /// <summary>
        /// ConfigurationException 생성자 (내부 예외 포함)
        /// </summary>
        /// <param name="message">예외 메시지</param>
        /// <param name="innerException">내부 예외</param>
        public ConfigurationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
