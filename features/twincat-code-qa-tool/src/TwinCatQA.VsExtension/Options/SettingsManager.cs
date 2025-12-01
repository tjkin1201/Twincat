using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;

namespace TwinCatQA.VsExtension.Options
{
    /// <summary>
    /// TwinCAT QA 설정 저장 및 로드 관리자
    /// </summary>
    public class SettingsManager
    {
        private const string CollectionPath = "TwinCatQA";
        private const string SettingsKey = "Settings";

        private readonly WritableSettingsStore _settingsStore;
        private static SettingsManager _instance;
        private static readonly object _lock = new object();

        /// <summary>
        /// 설정 변경 이벤트
        /// </summary>
        public event EventHandler<SettingsChangedEventArgs> SettingsChanged;

        private SettingsManager(IServiceProvider serviceProvider)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var shellSettingsManager = new ShellSettingsManager(serviceProvider);
            _settingsStore = shellSettingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

            // 컬렉션이 없으면 생성
            if (!_settingsStore.CollectionExists(CollectionPath))
            {
                _settingsStore.CreateCollection(CollectionPath);
            }
        }

        /// <summary>
        /// 싱글톤 인스턴스 가져오기
        /// </summary>
        public static SettingsManager GetInstance(IServiceProvider serviceProvider)
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new SettingsManager(serviceProvider);
                    }
                }
            }
            return _instance;
        }

        /// <summary>
        /// 설정 로드
        /// </summary>
        public QASettings LoadSettings()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                if (_settingsStore.PropertyExists(CollectionPath, SettingsKey))
                {
                    var json = _settingsStore.GetString(CollectionPath, SettingsKey);
                    if (!string.IsNullOrEmpty(json))
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            WriteIndented = true,
                            Converters = { new JsonStringEnumConverter() }
                        };

                        var settings = JsonSerializer.Deserialize<QASettings>(json, options);
                        if (settings != null)
                        {
                            return settings;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 로드 실패 시 기본 설정 반환
                System.Diagnostics.Debug.WriteLine($"설정 로드 실패: {ex.Message}");
            }

            // 기본 설정 반환
            return new QASettings();
        }

        /// <summary>
        /// 설정 저장
        /// </summary>
        public void SaveSettings(QASettings settings)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true,
                    Converters = { new JsonStringEnumConverter() }
                };

                var json = JsonSerializer.Serialize(settings, options);
                _settingsStore.SetString(CollectionPath, SettingsKey, json);

                // 설정 변경 이벤트 발생
                OnSettingsChanged(new SettingsChangedEventArgs(settings));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"설정 저장 실패: {ex.Message}");
                throw new InvalidOperationException("설정을 저장할 수 없습니다.", ex);
            }
        }

        /// <summary>
        /// 설정 초기화
        /// </summary>
        public void ResetSettings()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                if (_settingsStore.PropertyExists(CollectionPath, SettingsKey))
                {
                    _settingsStore.DeleteProperty(CollectionPath, SettingsKey);
                }

                var defaultSettings = new QASettings();
                SaveSettings(defaultSettings);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"설정 초기화 실패: {ex.Message}");
                throw new InvalidOperationException("설정을 초기화할 수 없습니다.", ex);
            }
        }

        /// <summary>
        /// 설정 내보내기
        /// </summary>
        public void ExportSettings(string filePath, QASettings settings)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true,
                    Converters = { new JsonStringEnumConverter() }
                };

                var json = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"설정 내보내기 실패: {ex.Message}");
                throw new InvalidOperationException("설정을 내보낼 수 없습니다.", ex);
            }
        }

        /// <summary>
        /// 설정 가져오기
        /// </summary>
        public QASettings ImportSettings(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException("설정 파일을 찾을 수 없습니다.", filePath);
                }

                var json = File.ReadAllText(filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true,
                    Converters = { new JsonStringEnumConverter() }
                };

                var settings = JsonSerializer.Deserialize<QASettings>(json, options);
                if (settings == null)
                {
                    throw new InvalidOperationException("설정 파일이 올바르지 않습니다.");
                }

                SaveSettings(settings);
                return settings;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"설정 가져오기 실패: {ex.Message}");
                throw new InvalidOperationException("설정을 가져올 수 없습니다.", ex);
            }
        }

        /// <summary>
        /// 특정 규칙 설정 업데이트
        /// </summary>
        public void UpdateRuleConfiguration(string ruleId, RuleConfiguration config)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var settings = LoadSettings();
            settings.RuleConfigurations[ruleId] = config;
            SaveSettings(settings);
        }

        /// <summary>
        /// 특정 규칙 활성화/비활성화
        /// </summary>
        public void SetRuleEnabled(string ruleId, bool enabled)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var settings = LoadSettings();
            if (settings.RuleConfigurations.TryGetValue(ruleId, out var config))
            {
                config.Enabled = enabled;
                SaveSettings(settings);
            }
        }

        /// <summary>
        /// 특정 규칙 심각도 변경
        /// </summary>
        public void SetRuleSeverity(string ruleId, RuleSeverity severity)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var settings = LoadSettings();
            if (settings.RuleConfigurations.TryGetValue(ruleId, out var config))
            {
                config.Severity = severity;
                SaveSettings(settings);
            }
        }

        /// <summary>
        /// 모든 규칙 활성화
        /// </summary>
        public void EnableAllRules()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var settings = LoadSettings();
            foreach (var config in settings.RuleConfigurations.Values)
            {
                config.Enabled = true;
            }
            SaveSettings(settings);
        }

        /// <summary>
        /// 모든 규칙 비활성화
        /// </summary>
        public void DisableAllRules()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var settings = LoadSettings();
            foreach (var config in settings.RuleConfigurations.Values)
            {
                config.Enabled = false;
            }
            SaveSettings(settings);
        }

        /// <summary>
        /// 설정 변경 이벤트 발생
        /// </summary>
        protected virtual void OnSettingsChanged(SettingsChangedEventArgs e)
        {
            SettingsChanged?.Invoke(this, e);
        }
    }

    /// <summary>
    /// 설정 변경 이벤트 인수
    /// </summary>
    public class SettingsChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 변경된 설정
        /// </summary>
        public QASettings Settings { get; }

        /// <summary>
        /// 변경 시각
        /// </summary>
        public DateTime ChangedAt { get; }

        public SettingsChangedEventArgs(QASettings settings)
        {
            Settings = settings;
            ChangedAt = DateTime.Now;
        }
    }
}
