using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TwinCatQA.Application.Services;
using TwinCatQA.Infrastructure.Parsers;
using TwinCatQA.Infrastructure.Analyzers;
using Task = System.Threading.Tasks.Task;

namespace TwinCatQA.VsExtension
{
    /// <summary>
    /// TwinCAT QA Visual Studio Extension 메인 패키지
    /// Visual Studio와 통합되어 TwinCAT 코드 품질 분석 기능을 제공합니다.
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(Microsoft.VisualStudio.Shell.Interop.UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideToolWindow(typeof(TwinCatQAToolWindow))]
    public sealed class TwinCatQAPackage : AsyncPackage
    {
        /// <summary>
        /// 패키지 GUID 문자열
        /// </summary>
        public const string PackageGuidString = "3f2a8e5c-9d7b-4a1f-8e3c-5b6a9d2e4f7c";

        private IServiceProvider? _serviceProvider;
        private ILogger<TwinCatQAPackage>? _logger;

        /// <summary>
        /// TwinCAT QA 패키지 생성자
        /// 백그라운드 스레드에서 초기화됩니다.
        /// </summary>
        public TwinCatQAPackage()
        {
            // 생성자는 가볍게 유지 - 실제 초기화는 InitializeAsync에서 수행
        }

        /// <summary>
        /// 패키지 비동기 초기화
        /// Visual Studio 확장 프로그램이 로드될 때 호출됩니다.
        /// </summary>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <param name="progress">진행률 보고</param>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // 백그라운드 스레드에서 초기화 시작
            await base.InitializeAsync(cancellationToken, progress);

            // 의존성 주입 컨테이너 설정
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            // 로거 초기화
            _logger = _serviceProvider.GetService<ILogger<TwinCatQAPackage>>();
            _logger?.LogInformation("TwinCAT QA Extension 초기화 시작");

            // UI 스레드로 전환 (메뉴 명령 등록을 위해)
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // 메뉴 명령 등록
            await InitializeCommandsAsync();

            _logger?.LogInformation("TwinCAT QA Extension 초기화 완료");
        }

        /// <summary>
        /// 의존성 주입 서비스 구성
        /// </summary>
        /// <param name="services">서비스 컬렉션</param>
        private void ConfigureServices(IServiceCollection services)
        {
            // 로깅 서비스 등록
            services.AddLogging(builder =>
            {
                builder.AddDebug();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            // Domain 서비스 등록
            // (도메인 계층은 의존성이 없으므로 별도 등록 불필요)

            // Infrastructure 서비스 등록
            services.AddSingleton<IStructuredTextParser, AntlrStructuredTextParser>();
            services.AddSingleton<IStaticAnalyzer, CompositeAnalyzer>();
            services.AddSingleton<ComplexityAnalyzer>();
            services.AddSingleton<NamingConventionAnalyzer>();
            services.AddSingleton<CodeSmellAnalyzer>();

            // Application 서비스 등록
            services.AddSingleton<AnalysisOrchestrator>();
            services.AddSingleton<ConfigurationService>();
            services.AddSingleton<ReportGenerator>();

            // Extension 특화 서비스 등록
            services.AddSingleton<IVsIntegrationService, VsIntegrationService>();
            services.AddSingleton<IOutputWindowService, OutputWindowService>();
        }

        /// <summary>
        /// Visual Studio 메뉴 명령 초기화
        /// </summary>
        private async Task InitializeCommandsAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // 분석 명령 등록
            if (_serviceProvider != null)
            {
                await Commands.AnalyzeCommand.InitializeAsync(this, _serviceProvider);
                await Commands.ShowToolWindowCommand.InitializeAsync(this, _serviceProvider);
                await Commands.SettingsCommand.InitializeAsync(this, _serviceProvider);
            }
        }

        /// <summary>
        /// 패키지 리소스 정리
        /// </summary>
        /// <param name="disposing">관리되는 리소스 해제 여부</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _logger?.LogInformation("TwinCAT QA Extension 종료");

                // 서비스 프로바이더 정리
                if (_serviceProvider is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// 도구 창 표시
        /// </summary>
        public async Task ShowToolWindowAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var window = await ShowToolWindowAsync(
                typeof(TwinCatQAToolWindow),
                0,
                create: true,
                cancellationToken: DisposalToken);

            if (window?.Frame == null)
            {
                _logger?.LogError("도구 창을 생성할 수 없습니다.");
                throw new NotSupportedException("도구 창을 생성할 수 없습니다.");
            }
        }
    }

    /// <summary>
    /// Visual Studio 통합 서비스 인터페이스
    /// </summary>
    public interface IVsIntegrationService
    {
        /// <summary>
        /// 현재 활성 문서의 전체 경로 가져오기
        /// </summary>
        Task<string?> GetActiveDocumentPathAsync();

        /// <summary>
        /// 현재 활성 프로젝트의 전체 경로 가져오기
        /// </summary>
        Task<string?> GetActiveProjectPathAsync();

        /// <summary>
        /// 솔루션의 모든 TwinCAT 파일 가져오기
        /// </summary>
        Task<IEnumerable<string>> GetAllTwinCatFilesAsync();
    }

    /// <summary>
    /// 출력 창 서비스 인터페이스
    /// </summary>
    public interface IOutputWindowService
    {
        /// <summary>
        /// 출력 창에 메시지 작성
        /// </summary>
        void WriteLine(string message);

        /// <summary>
        /// 출력 창에 경고 메시지 작성
        /// </summary>
        void WriteWarning(string message);

        /// <summary>
        /// 출력 창에 오류 메시지 작성
        /// </summary>
        void WriteError(string message);

        /// <summary>
        /// 출력 창 지우기
        /// </summary>
        void Clear();

        /// <summary>
        /// 출력 창 활성화
        /// </summary>
        void Activate();
    }
}
