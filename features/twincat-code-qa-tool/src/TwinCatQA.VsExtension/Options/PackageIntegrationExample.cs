using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace TwinCatQA.VsExtension.Options
{
    /// <summary>
    /// Visual Studio Package에 옵션 페이지를 등록하는 방법 예제
    ///
    /// 실제 Package 클래스 (예: TwinCatQAPackage.cs)에 다음과 같이 ProvideOptionPage 속성을 추가하세요:
    /// </summary>
    /// <example>
    /// <code>
    /// [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    /// [Guid(PackageGuidString)]
    /// [ProvideMenuResource("Menus.ctmenu", 1)]
    ///
    /// // TwinCAT QA 옵션 페이지 등록
    /// [ProvideOptionPage(typeof(GeneralOptionsPage), "TwinCAT QA", "일반", 0, 0, true)]
    /// [ProvideOptionPage(typeof(RulesOptionsPage), "TwinCAT QA", "규칙", 0, 0, true)]
    /// [ProvideOptionPage(typeof(PathOptionsPage), "TwinCAT QA", "경로", 0, 0, true)]
    ///
    /// public sealed class TwinCatQAPackage : AsyncPackage
    /// {
    ///     public const string PackageGuidString = "your-package-guid-here";
    ///
    ///     // ... 나머지 Package 코드
    /// }
    /// </code>
    /// </example>
    public class PackageIntegrationExample
    {
        /// <summary>
        /// Package에서 설정 가져오기 예제
        /// </summary>
        /// <example>
        /// <code>
        /// // Package 클래스 내에서 설정 가져오기
        /// public void GetSettings()
        /// {
        ///     ThreadHelper.ThrowIfNotOnUIThread();
        ///
        ///     // 방법 1: DialogPage를 통해 직접 가져오기
        ///     var generalPage = (GeneralOptionsPage)GetDialogPage(typeof(GeneralOptionsPage));
        ///     bool autoAnalysis = generalPage.EnableAutoAnalysis;
        ///
        ///     // 방법 2: SettingsManager를 통해 가져오기 (권장)
        ///     var settingsManager = SettingsManager.GetInstance(this);
        ///     var settings = settingsManager.LoadSettings();
        ///     bool autoAnalysis2 = settings.EnableAutoAnalysis;
        /// }
        /// </code>
        /// </example>
        public void GetSettingsExample() { }

        /// <summary>
        /// 설정 변경 이벤트 구독 예제
        /// </summary>
        /// <example>
        /// <code>
        /// // Package 초기화 시 이벤트 구독
        /// protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress&lt;ServiceProgressData&gt; progress)
        /// {
        ///     await base.InitializeAsync(cancellationToken, progress);
        ///
        ///     await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        ///
        ///     // 설정 변경 이벤트 구독
        ///     var settingsManager = SettingsManager.GetInstance(this);
        ///     settingsManager.SettingsChanged += OnSettingsChanged;
        /// }
        ///
        /// private void OnSettingsChanged(object sender, SettingsChangedEventArgs e)
        /// {
        ///     ThreadHelper.ThrowIfNotOnUIThread();
        ///
        ///     // 설정이 변경되었을 때 처리
        ///     var settings = e.Settings;
        ///
        ///     if (settings.EnableAutoAnalysis)
        ///     {
        ///         // 자동 분석 활성화
        ///         StartAutoAnalysis();
        ///     }
        ///     else
        ///     {
        ///         // 자동 분석 비활성화
        ///         StopAutoAnalysis();
        ///     }
        ///
        ///     // 출력 창에 메시지 표시
        ///     WriteToOutput($"설정이 변경되었습니다: {e.ChangedAt}");
        /// }
        /// </code>
        /// </example>
        public void SettingsChangedExample() { }

        /// <summary>
        /// 명령에서 설정 사용 예제
        /// </summary>
        /// <example>
        /// <code>
        /// // Command 클래스에서 설정 사용
        /// private void Execute(object sender, EventArgs e)
        /// {
        ///     ThreadHelper.ThrowIfNotOnUIThread();
        ///
        ///     // 설정 가져오기
        ///     var settingsManager = SettingsManager.GetInstance(ServiceProvider.GlobalProvider);
        ///     var settings = settingsManager.LoadSettings();
        ///
        ///     // 규칙 설정 확인
        ///     var nc001 = settings.GetRule("NC001");
        ///     if (nc001 != null &amp;&amp; nc001.Enabled)
        ///     {
        ///         // NC001 규칙 실행
        ///         RunNamingConventionCheck();
        ///     }
        ///
        ///     // 출력 상세도에 따라 메시지 표시
        ///     if (settings.Verbosity >= OutputVerbosity.Detailed)
        ///     {
        ///         WriteDetailedLog("분석 시작...");
        ///     }
        /// }
        /// </code>
        /// </example>
        public void CommandExample() { }

        /// <summary>
        /// 설정 내보내기/가져오기 명령 예제
        /// </summary>
        /// <example>
        /// <code>
        /// // 설정 내보내기 명령
        /// private void ExportSettings(object sender, EventArgs e)
        /// {
        ///     ThreadHelper.ThrowIfNotOnUIThread();
        ///
        ///     var dialog = new SaveFileDialog
        ///     {
        ///         Filter = "JSON 파일 (*.json)|*.json",
        ///         DefaultExt = "json",
        ///         FileName = "TwinCatQA-Settings.json"
        ///     };
        ///
        ///     if (dialog.ShowDialog() == DialogResult.OK)
        ///     {
        ///         var settingsManager = SettingsManager.GetInstance(ServiceProvider.GlobalProvider);
        ///         var settings = settingsManager.LoadSettings();
        ///         settingsManager.ExportSettings(dialog.FileName, settings);
        ///
        ///         MessageBox.Show("설정을 성공적으로 내보냈습니다.", "TwinCAT QA");
        ///     }
        /// }
        ///
        /// // 설정 가져오기 명령
        /// private void ImportSettings(object sender, EventArgs e)
        /// {
        ///     ThreadHelper.ThrowIfNotOnUIThread();
        ///
        ///     var dialog = new OpenFileDialog
        ///     {
        ///         Filter = "JSON 파일 (*.json)|*.json",
        ///         DefaultExt = "json"
        ///     };
        ///
        ///     if (dialog.ShowDialog() == DialogResult.OK)
        ///     {
        ///         try
        ///         {
        ///             var settingsManager = SettingsManager.GetInstance(ServiceProvider.GlobalProvider);
        ///             settingsManager.ImportSettings(dialog.FileName);
        ///
        ///             MessageBox.Show("설정을 성공적으로 가져왔습니다.", "TwinCAT QA");
        ///         }
        ///         catch (Exception ex)
        ///         {
        ///             MessageBox.Show($"설정 가져오기 실패: {ex.Message}", "TwinCAT QA",
        ///                 MessageBoxButtons.OK, MessageBoxIcon.Error);
        ///         }
        ///     }
        /// }
        /// </code>
        /// </example>
        public void ImportExportExample() { }

        /// <summary>
        /// 규칙 설정 동적 업데이트 예제
        /// </summary>
        /// <example>
        /// <code>
        /// // 사용자 액션에 따라 규칙 활성화/비활성화
        /// private void ToggleRule(string ruleId, bool enabled)
        /// {
        ///     ThreadHelper.ThrowIfNotOnUIThread();
        ///
        ///     var settingsManager = SettingsManager.GetInstance(ServiceProvider.GlobalProvider);
        ///     settingsManager.SetRuleEnabled(ruleId, enabled);
        ///
        ///     // UI 업데이트
        ///     UpdateRuleCheckbox(ruleId, enabled);
        /// }
        ///
        /// // 모든 규칙 활성화
        /// private void EnableAllRules()
        /// {
        ///     ThreadHelper.ThrowIfNotOnUIThread();
        ///
        ///     var settingsManager = SettingsManager.GetInstance(ServiceProvider.GlobalProvider);
        ///     settingsManager.EnableAllRules();
        ///
        ///     MessageBox.Show("모든 규칙이 활성화되었습니다.", "TwinCAT QA");
        /// }
        ///
        /// // 특정 카테고리 규칙만 활성화
        /// private void EnableCategoryRules(string category)
        /// {
        ///     ThreadHelper.ThrowIfNotOnUIThread();
        ///
        ///     var settingsManager = SettingsManager.GetInstance(ServiceProvider.GlobalProvider);
        ///     var settings = settingsManager.LoadSettings();
        ///
        ///     foreach (var rule in settings.RuleConfigurations.Values)
        ///     {
        ///         // 예: NC로 시작하는 규칙만 활성화
        ///         if (rule.Id.StartsWith(category))
        ///         {
        ///             rule.Enabled = true;
        ///         }
        ///     }
        ///
        ///     settingsManager.SaveSettings(settings);
        /// }
        /// </code>
        /// </example>
        public void DynamicRuleUpdateExample() { }
    }

    /// <summary>
    /// vsct 파일에 옵션 명령 추가 예제
    /// </summary>
    /// <remarks>
    /// .vsct 파일에 다음과 같이 옵션 페이지를 여는 명령을 추가할 수 있습니다:
    ///
    /// <code>
    /// &lt;Button guid="guidTwinCatQAPackageCmdSet" id="cmdidOpenOptions" priority="0x0100" type="Button"&gt;
    ///   &lt;Parent guid="guidTwinCatQAPackageCmdSet" id="TwinCatQAMenuGroup" /&gt;
    ///   &lt;Icon guid="guidImages" id="bmpPic1" /&gt;
    ///   &lt;Strings&gt;
    ///     &lt;ButtonText&gt;옵션...&lt;/ButtonText&gt;
    ///   &lt;/Strings&gt;
    /// &lt;/Button&gt;
    /// </code>
    ///
    /// 그리고 Command 클래스에서:
    ///
    /// <code>
    /// private void Execute(object sender, EventArgs e)
    /// {
    ///     ThreadHelper.ThrowIfNotOnUIThread();
    ///
    ///     // Visual Studio 옵션 대화상자 열기
    ///     var dte = (DTE2)ServiceProvider.GlobalProvider.GetService(typeof(DTE));
    ///     dte.ExecuteCommand("Tools.Options", "TwinCAT QA");
    /// }
    /// </code>
    /// </remarks>
    public class VsctIntegrationExample { }
}
