using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;

namespace TwinCatQA.VsExtension.Options
{
    /// <summary>
    /// TwinCAT QA 경로 설정 페이지
    /// Tools > Options > TwinCAT QA > 경로
    /// </summary>
    [Guid("8D1E5C3F-9A4B-4E7D-B9F6-3C2A8D5E7B9A")]
    [ComVisible(true)]
    public class PathOptionsPage : DialogPage
    {
        private QASettings _settings;
        private SettingsManager _settingsManager;

        public PathOptionsPage()
        {
            // 설정 관리자는 LoadSettingsFromStorage에서 초기화
        }

        #region 속성

        /// <summary>
        /// 프로젝트 루트 경로
        /// </summary>
        [Category("프로젝트")]
        [DisplayName("프로젝트 루트 경로")]
        [Description("TwinCAT 프로젝트의 루트 디렉토리 경로입니다. 비어있으면 현재 솔루션 경로를 사용합니다.")]
        [DefaultValue("")]
        [Editor(typeof(System.Windows.Forms.Design.FolderNameEditor), typeof(System.Drawing.Design.UITypeEditor))]
        public string ProjectRoot { get; set; } = string.Empty;

        /// <summary>
        /// 출력 폴더 경로
        /// </summary>
        [Category("출력")]
        [DisplayName("출력 폴더")]
        [Description("분석 결과 및 리포트를 저장할 폴더 경로입니다. 비어있으면 프로젝트 루트의 'QAReports' 폴더를 사용합니다.")]
        [DefaultValue("")]
        [Editor(typeof(System.Windows.Forms.Design.FolderNameEditor), typeof(System.Drawing.Design.UITypeEditor))]
        public string OutputFolder { get; set; } = string.Empty;

        /// <summary>
        /// 리포트 파일명 형식
        /// </summary>
        [Category("출력")]
        [DisplayName("리포트 파일명 형식")]
        [Description("리포트 파일명 형식입니다. {0}=날짜, {1}=시간, {2}=프로젝트명")]
        [DefaultValue("QAReport_{2}_{0}_{1}.html")]
        public string ReportFileNameFormat { get; set; } = "QAReport_{2}_{0}_{1}.html";

        /// <summary>
        /// 자동으로 출력 폴더 생성
        /// </summary>
        [Category("출력")]
        [DisplayName("자동 폴더 생성")]
        [Description("출력 폴더가 없을 경우 자동으로 생성합니다.")]
        [DefaultValue(true)]
        public bool AutoCreateOutputFolder { get; set; } = true;

        /// <summary>
        /// 이전 리포트 보관 기간 (일)
        /// </summary>
        [Category("출력")]
        [DisplayName("리포트 보관 기간 (일)")]
        [Description("이전 리포트를 보관할 기간입니다. 0이면 삭제하지 않습니다.")]
        [DefaultValue(30)]
        public int ReportRetentionDays { get; set; } = 30;

        /// <summary>
        /// 제외할 파일 패턴
        /// </summary>
        [Category("필터")]
        [DisplayName("제외할 파일 패턴")]
        [Description("분석에서 제외할 파일 패턴입니다. 세미콜론(;)으로 구분합니다. 예: *.bak;*.tmp")]
        [DefaultValue("*.bak;*.tmp;*.old")]
        public string ExcludeFilePatterns { get; set; } = "*.bak;*.tmp;*.old";

        /// <summary>
        /// 제외할 폴더
        /// </summary>
        [Category("필터")]
        [DisplayName("제외할 폴더")]
        [Description("분석에서 제외할 폴더 경로입니다. 세미콜론(;)으로 구분합니다.")]
        [DefaultValue("_Boot;_CompileInfo;_Libraries")]
        public string ExcludeFolders { get; set; } = "_Boot;_CompileInfo;_Libraries";

        /// <summary>
        /// 포함할 파일 확장자
        /// </summary>
        [Category("필터")]
        [DisplayName("포함할 파일 확장자")]
        [Description("분석에 포함할 파일 확장자입니다. 세미콜론(;)으로 구분합니다.")]
        [DefaultValue(".TcPOU;.TcDUT;.TcGVL")]
        public string IncludeFileExtensions { get; set; } = ".TcPOU;.TcDUT;.TcGVL";

        /// <summary>
        /// 하위 폴더 포함
        /// </summary>
        [Category("필터")]
        [DisplayName("하위 폴더 포함")]
        [Description("프로젝트의 모든 하위 폴더를 분석에 포함합니다.")]
        [DefaultValue(true)]
        public bool IncludeSubfolders { get; set; } = true;

        /// <summary>
        /// 템플릿 폴더 경로
        /// </summary>
        [Category("고급")]
        [DisplayName("템플릿 폴더")]
        [Description("리포트 템플릿 파일을 저장할 폴더 경로입니다.")]
        [DefaultValue("")]
        [Editor(typeof(System.Windows.Forms.Design.FolderNameEditor), typeof(System.Drawing.Design.UITypeEditor))]
        public string TemplateFolder { get; set; } = string.Empty;

        /// <summary>
        /// 캐시 폴더 경로
        /// </summary>
        [Category("고급")]
        [DisplayName("캐시 폴더")]
        [Description("분석 결과를 캐시할 폴더 경로입니다. 성능 향상에 도움됩니다.")]
        [DefaultValue("")]
        [Editor(typeof(System.Windows.Forms.Design.FolderNameEditor), typeof(System.Drawing.Design.UITypeEditor))]
        public string CacheFolder { get; set; } = string.Empty;

        /// <summary>
        /// 캐시 활성화
        /// </summary>
        [Category("고급")]
        [DisplayName("캐시 활성화")]
        [Description("분석 결과를 캐시하여 성능을 향상시킵니다.")]
        [DefaultValue(true)]
        public bool EnableCache { get; set; } = true;

        /// <summary>
        /// 캐시 만료 시간 (분)
        /// </summary>
        [Category("고급")]
        [DisplayName("캐시 만료 시간 (분)")]
        [Description("캐시가 유효한 시간입니다. 0이면 파일 수정 시까지 유지됩니다.")]
        [DefaultValue(0)]
        public int CacheExpirationMinutes { get; set; } = 0;

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
                if (_settingsManager == null)
                {
                    _settingsManager = SettingsManager.GetInstance(Site);
                }

                _settings = _settingsManager.LoadSettings();

                // 속성에 설정 반영
                ProjectRoot = _settings.ProjectRoot;
                OutputFolder = _settings.OutputFolder;

                base.LoadSettingsFromStorage();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"경로 설정 로드 실패: {ex.Message}");
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

                if (_settings == null)
                {
                    _settings = new QASettings();
                }

                // 경로 유효성 검사
                if (!string.IsNullOrEmpty(ProjectRoot) && !Directory.Exists(ProjectRoot))
                {
                    var result = MessageBox.Show(
                        $"프로젝트 루트 경로가 존재하지 않습니다:\n{ProjectRoot}\n\n계속하시겠습니까?",
                        "TwinCAT QA",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (result == DialogResult.No)
                    {
                        return;
                    }
                }

                if (!string.IsNullOrEmpty(OutputFolder))
                {
                    if (!Directory.Exists(OutputFolder))
                    {
                        if (AutoCreateOutputFolder)
                        {
                            try
                            {
                                Directory.CreateDirectory(OutputFolder);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(
                                    $"출력 폴더를 생성할 수 없습니다:\n{ex.Message}",
                                    "TwinCAT QA",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error);
                                return;
                            }
                        }
                        else
                        {
                            var result = MessageBox.Show(
                                $"출력 폴더가 존재하지 않습니다:\n{OutputFolder}\n\n계속하시겠습니까?",
                                "TwinCAT QA",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Warning);

                            if (result == DialogResult.No)
                            {
                                return;
                            }
                        }
                    }
                }

                // 속성 값을 설정 객체에 저장
                _settings.ProjectRoot = ProjectRoot;
                _settings.OutputFolder = OutputFolder;

                // 설정 저장
                _settingsManager.SaveSettings(_settings);

                base.SaveSettingsToStorage();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"경로 설정 저장 실패: {ex.Message}");
                MessageBox.Show(
                    $"설정을 저장하는 중 오류가 발생했습니다:\n{ex.Message}",
                    "TwinCAT QA",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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
                var result = MessageBox.Show(
                    "모든 경로 설정을 기본값으로 초기화하시겠습니까?",
                    "TwinCAT QA",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    if (_settingsManager == null)
                    {
                        _settingsManager = SettingsManager.GetInstance(Site);
                    }

                    _settingsManager.ResetSettings();
                    LoadSettingsFromStorage();

                    base.ResetSettings();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"경로 설정 초기화 실패: {ex.Message}");
            }
        }

        #endregion

        #region 헬퍼 메서드

        /// <summary>
        /// 유효한 프로젝트 루트 경로 가져오기
        /// </summary>
        public string GetValidProjectRoot()
        {
            if (!string.IsNullOrEmpty(ProjectRoot) && Directory.Exists(ProjectRoot))
            {
                return ProjectRoot;
            }

            // 기본값: 현재 솔루션 경로
            // 실제 구현에서는 DTE를 통해 솔루션 경로를 가져와야 함
            return Environment.CurrentDirectory;
        }

        /// <summary>
        /// 유효한 출력 폴더 경로 가져오기
        /// </summary>
        public string GetValidOutputFolder()
        {
            if (!string.IsNullOrEmpty(OutputFolder))
            {
                if (Directory.Exists(OutputFolder))
                {
                    return OutputFolder;
                }

                if (AutoCreateOutputFolder)
                {
                    try
                    {
                        Directory.CreateDirectory(OutputFolder);
                        return OutputFolder;
                    }
                    catch
                    {
                        // 생성 실패 시 기본 폴더 사용
                    }
                }
            }

            // 기본값: 프로젝트 루트의 QAReports 폴더
            var defaultFolder = Path.Combine(GetValidProjectRoot(), "QAReports");
            if (!Directory.Exists(defaultFolder) && AutoCreateOutputFolder)
            {
                try
                {
                    Directory.CreateDirectory(defaultFolder);
                }
                catch
                {
                    // 생성 실패 시 임시 폴더 사용
                    return Path.GetTempPath();
                }
            }

            return defaultFolder;
        }

        /// <summary>
        /// 제외 파일 패턴 배열 가져오기
        /// </summary>
        public string[] GetExcludeFilePatterns()
        {
            if (string.IsNullOrWhiteSpace(ExcludeFilePatterns))
            {
                return Array.Empty<string>();
            }

            return ExcludeFilePatterns.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// 제외 폴더 배열 가져오기
        /// </summary>
        public string[] GetExcludeFolders()
        {
            if (string.IsNullOrWhiteSpace(ExcludeFolders))
            {
                return Array.Empty<string>();
            }

            return ExcludeFolders.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// 포함 파일 확장자 배열 가져오기
        /// </summary>
        public string[] GetIncludeFileExtensions()
        {
            if (string.IsNullOrWhiteSpace(IncludeFileExtensions))
            {
                return new[] { ".TcPOU", ".TcDUT", ".TcGVL" };
            }

            return IncludeFileExtensions.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        }

        #endregion
    }
}
