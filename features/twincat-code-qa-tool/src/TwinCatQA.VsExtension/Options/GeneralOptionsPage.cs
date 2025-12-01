using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace TwinCatQA.VsExtension.Options
{
    /// <summary>
    /// TwinCAT QA 일반 설정 페이지
    /// Tools > Options > TwinCAT QA > 일반
    /// </summary>
    [Guid("6B8E3A1C-9F2D-4E5A-B7C3-1D4F8A9E2C5B")]
    [ComVisible(true)]
    public class GeneralOptionsPage : DialogPage
    {
        private QASettings _settings;
        private SettingsManager _settingsManager;

        public GeneralOptionsPage()
        {
            // 설정 관리자는 LoadSettingsFromStorage에서 초기화
        }

        #region 속성

        /// <summary>
        /// 자동 분석 활성화
        /// </summary>
        [Category("분석")]
        [DisplayName("자동 분석 활성화")]
        [Description("파일 저장 시 자동으로 코드 분석을 수행합니다.")]
        [DefaultValue(true)]
        public bool EnableAutoAnalysis { get; set; } = true;

        /// <summary>
        /// 분석 지연 시간
        /// </summary>
        [Category("분석")]
        [DisplayName("분석 지연 시간 (밀리초)")]
        [Description("파일 변경 후 분석을 시작하기까지의 지연 시간입니다. (100-5000ms)")]
        [DefaultValue(500)]
        public int AnalysisDelayMs { get; set; } = 500;

        /// <summary>
        /// 저장 시 분석 실행
        /// </summary>
        [Category("분석")]
        [DisplayName("저장 시 분석 실행")]
        [Description("파일 저장 시 즉시 분석을 실행합니다.")]
        [DefaultValue(true)]
        public bool AnalyzeOnSave { get; set; } = true;

        /// <summary>
        /// 빌드 시 분석 실행
        /// </summary>
        [Category("분석")]
        [DisplayName("빌드 시 분석 실행")]
        [Description("프로젝트 빌드 시 전체 분석을 실행합니다.")]
        [DefaultValue(true)]
        public bool AnalyzeOnBuild { get; set; } = true;

        /// <summary>
        /// 출력 상세도
        /// </summary>
        [Category("출력")]
        [DisplayName("출력 상세도")]
        [Description("출력 창에 표시되는 정보의 상세도를 설정합니다.")]
        [DefaultValue(OutputVerbosity.Normal)]
        [TypeConverter(typeof(EnumConverter))]
        public OutputVerbosity Verbosity { get; set; } = OutputVerbosity.Normal;

        /// <summary>
        /// 오류 목록에 표시
        /// </summary>
        [Category("출력")]
        [DisplayName("오류 목록에 표시")]
        [Description("분석 결과를 Visual Studio 오류 목록에 표시합니다.")]
        [DefaultValue(true)]
        public bool ShowInErrorList { get; set; } = true;

        /// <summary>
        /// 출력 창에 표시
        /// </summary>
        [Category("출력")]
        [DisplayName("출력 창에 표시")]
        [Description("분석 결과를 출력 창에 표시합니다.")]
        [DefaultValue(true)]
        public bool ShowInOutputWindow { get; set; } = true;

        /// <summary>
        /// 성공 메시지 표시
        /// </summary>
        [Category("출력")]
        [DisplayName("성공 메시지 표시")]
        [Description("문제가 발견되지 않았을 때도 메시지를 표시합니다.")]
        [DefaultValue(false)]
        public bool ShowSuccessMessages { get; set; } = false;

        /// <summary>
        /// 성능 메트릭 표시
        /// </summary>
        [Category("고급")]
        [DisplayName("성능 메트릭 표시")]
        [Description("분석 시간 및 성능 정보를 표시합니다.")]
        [DefaultValue(false)]
        public bool ShowPerformanceMetrics { get; set; } = false;

        /// <summary>
        /// 진단 로깅 활성화
        /// </summary>
        [Category("고급")]
        [DisplayName("진단 로깅 활성화")]
        [Description("상세한 진단 로그를 출력 창에 기록합니다.")]
        [DefaultValue(false)]
        public bool EnableDiagnosticLogging { get; set; } = false;

        /// <summary>
        /// 백그라운드 분석 활성화
        /// </summary>
        [Category("고급")]
        [DisplayName("백그라운드 분석 활성화")]
        [Description("백그라운드에서 비동기적으로 분석을 수행합니다.")]
        [DefaultValue(true)]
        public bool EnableBackgroundAnalysis { get; set; } = true;

        /// <summary>
        /// 최대 병렬 작업 수
        /// </summary>
        [Category("고급")]
        [DisplayName("최대 병렬 작업 수")]
        [Description("동시에 실행할 수 있는 분석 작업의 최대 개수입니다. (1-8, 0=자동)")]
        [DefaultValue(0)]
        public int MaxParallelTasks { get; set; } = 0;

        #endregion

        #region DialogPage 재정의

        /// <summary>
        /// 설정 로드
        /// </summary>
        public override void LoadSettingsFromStorage()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                // 설정 관리자 초기화
                if (_settingsManager == null)
                {
                    _settingsManager = SettingsManager.GetInstance(Site);
                }

                // 설정 로드
                _settings = _settingsManager.LoadSettings();

                // 속성에 설정 반영
                EnableAutoAnalysis = _settings.EnableAutoAnalysis;
                AnalysisDelayMs = _settings.AnalysisDelayMs;
                Verbosity = _settings.Verbosity;

                base.LoadSettingsFromStorage();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"일반 설정 로드 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 설정 저장
        /// </summary>
        public override void SaveSettingsToStorage()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                if (_settingsManager == null)
                {
                    _settingsManager = SettingsManager.GetInstance(Site);
                }

                // 현재 설정 또는 새 설정 객체 사용
                if (_settings == null)
                {
                    _settings = new QASettings();
                }

                // 속성 값을 설정 객체에 저장
                _settings.EnableAutoAnalysis = EnableAutoAnalysis;
                _settings.AnalysisDelayMs = Math.Max(100, Math.Min(5000, AnalysisDelayMs));
                _settings.Verbosity = Verbosity;

                // 설정 저장
                _settingsManager.SaveSettings(_settings);

                base.SaveSettingsToStorage();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"일반 설정 저장 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 설정 초기화
        /// </summary>
        public override void ResetSettings()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                if (_settingsManager == null)
                {
                    _settingsManager = SettingsManager.GetInstance(Site);
                }

                _settingsManager.ResetSettings();
                LoadSettingsFromStorage();

                base.ResetSettings();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"일반 설정 초기화 실패: {ex.Message}");
            }
        }

        #endregion

        /// <summary>
        /// 현재 설정 가져오기
        /// </summary>
        public QASettings GetSettings()
        {
            return _settings?.Clone() ?? new QASettings();
        }
    }
}
