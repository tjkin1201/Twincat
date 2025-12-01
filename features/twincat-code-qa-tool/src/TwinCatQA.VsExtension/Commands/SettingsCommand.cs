using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace TwinCatQA.VsExtension.Commands
{
    /// <summary>
    /// TwinCAT QA 설정 열기 명령어
    /// Visual Studio 도구 > 옵션에서 TwinCAT QA 설정 페이지를 엽니다.
    /// </summary>
    internal sealed class SettingsCommand
    {
        /// <summary>
        /// 명령어 ID
        /// </summary>
        public const int CommandId = 0x0103;

        /// <summary>
        /// 명령어 세트 GUID
        /// </summary>
        public static readonly Guid CommandSet = new Guid("a1b2c3d4-e5f6-4a5b-9c8d-7e6f5a4b3c2d");

        private readonly AsyncPackage _package;
        private readonly ILogger<SettingsCommand> _logger;

        /// <summary>
        /// SettingsCommand 생성자
        /// </summary>
        /// <param name="package">VS 패키지</param>
        /// <param name="serviceProvider">서비스 프로바이더</param>
        private SettingsCommand(AsyncPackage package, IServiceProvider serviceProvider)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
            _logger = serviceProvider.GetService<ILogger<SettingsCommand>>();

            // 명령어 서비스 등록
            if (serviceProvider.GetService(typeof(IMenuCommandService)) is OleMenuCommandService commandService)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(Execute, menuCommandID);
                commandService.AddCommand(menuItem);
            }
        }

        /// <summary>
        /// 명령어 인스턴스
        /// </summary>
        public static SettingsCommand Instance { get; private set; }

        /// <summary>
        /// 명령어 초기화
        /// </summary>
        /// <param name="package">VS 패키지</param>
        /// <param name="serviceProvider">서비스 프로바이더</param>
        public static async Task InitializeAsync(AsyncPackage package, IServiceProvider serviceProvider)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            Instance = new SettingsCommand(package, serviceProvider);
        }

        /// <summary>
        /// 명령어 실행 - 설정 페이지 열기
        /// </summary>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                _logger?.LogInformation("TwinCAT QA 설정 페이지 열기");

                // Visual Studio 옵션 대화상자 표시
                var guid = typeof(Options.GeneralOptionsPage).GUID;
                _package.ShowOptionPage(typeof(Options.GeneralOptionsPage));

                _logger?.LogInformation("설정 페이지가 열렸습니다.");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "설정 페이지 열기 중 오류 발생");

                // 사용자에게 오류 메시지 표시
                VsShellUtilities.ShowMessageBox(
                    _package,
                    $"설정 페이지를 열 수 없습니다: {ex.Message}",
                    "TwinCAT QA",
                    Microsoft.VisualStudio.Shell.Interop.OLEMSGICON.OLEMSGICON_CRITICAL,
                    Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    Microsoft.VisualStudio.Shell.Interop.OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }
    }
}
