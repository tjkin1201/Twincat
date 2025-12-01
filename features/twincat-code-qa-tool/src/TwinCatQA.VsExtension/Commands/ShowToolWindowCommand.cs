using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace TwinCatQA.VsExtension.Commands
{
    /// <summary>
    /// TwinCAT QA 도구 창 표시 명령어
    /// </summary>
    internal sealed class ShowToolWindowCommand
    {
        /// <summary>
        /// 명령어 ID
        /// </summary>
        public const int CommandId = 0x0102;

        /// <summary>
        /// 명령어 세트 GUID
        /// </summary>
        public static readonly Guid CommandSet = new Guid("a1b2c3d4-e5f6-4a5b-9c8d-7e6f5a4b3c2d");

        private readonly AsyncPackage _package;
        private readonly ILogger<ShowToolWindowCommand> _logger;

        /// <summary>
        /// ShowToolWindowCommand 생성자
        /// </summary>
        /// <param name="package">VS 패키지</param>
        /// <param name="serviceProvider">서비스 프로바이더</param>
        private ShowToolWindowCommand(AsyncPackage package, IServiceProvider serviceProvider)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
            _logger = serviceProvider.GetService<ILogger<ShowToolWindowCommand>>();

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
        public static ShowToolWindowCommand Instance { get; private set; }

        /// <summary>
        /// 명령어 초기화
        /// </summary>
        /// <param name="package">VS 패키지</param>
        /// <param name="serviceProvider">서비스 프로바이더</param>
        public static async Task InitializeAsync(AsyncPackage package, IServiceProvider serviceProvider)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            Instance = new ShowToolWindowCommand(package, serviceProvider);
        }

        /// <summary>
        /// 명령어 실행 - 도구 창 표시
        /// </summary>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                _logger?.LogInformation("TwinCAT QA 도구 창 표시");

                // 패키지의 도구 창 표시 메서드 호출
                if (_package is TwinCatQAPackage qaPackage)
                {
                    ThreadHelper.JoinableTaskFactory.Run(async () =>
                    {
                        await qaPackage.ShowToolWindowAsync();
                    });
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "도구 창 표시 중 오류 발생");

                // 사용자에게 오류 메시지 표시
                VsShellUtilities.ShowMessageBox(
                    _package,
                    $"도구 창을 표시할 수 없습니다: {ex.Message}",
                    "TwinCAT QA",
                    Microsoft.VisualStudio.Shell.Interop.OLEMSGICON.OLEMSGICON_CRITICAL,
                    Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    Microsoft.VisualStudio.Shell.Interop.OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }
    }
}
