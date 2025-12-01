using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TwinCatQA.Application.Services;
using Task = System.Threading.Tasks.Task;

namespace TwinCatQA.VsExtension.Commands
{
    /// <summary>
    /// TwinCAT 코드 분석 명령어
    /// 현재 파일 또는 전체 프로젝트를 분석합니다.
    /// </summary>
    internal sealed class AnalyzeCommand
    {
        /// <summary>
        /// 현재 파일 분석 명령어 ID
        /// </summary>
        public const int AnalyzeCurrentFileCommandId = 0x0100;

        /// <summary>
        /// 전체 프로젝트 분석 명령어 ID
        /// </summary>
        public const int AnalyzeProjectCommandId = 0x0101;

        /// <summary>
        /// 명령어 세트 GUID
        /// </summary>
        public static readonly Guid CommandSet = new Guid("a1b2c3d4-e5f6-4a5b-9c8d-7e6f5a4b3c2d");

        private readonly AsyncPackage _package;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AnalyzeCommand> _logger;

        /// <summary>
        /// AnalyzeCommand 생성자
        /// </summary>
        /// <param name="package">VS 패키지</param>
        /// <param name="serviceProvider">서비스 프로바이더</param>
        private AnalyzeCommand(AsyncPackage package, IServiceProvider serviceProvider)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = serviceProvider.GetService<ILogger<AnalyzeCommand>>();

            // 명령어 서비스 등록
            if (serviceProvider.GetService(typeof(IMenuCommandService)) is OleMenuCommandService commandService)
            {
                // 현재 파일 분석 명령어
                var currentFileMenuCommandID = new CommandID(CommandSet, AnalyzeCurrentFileCommandId);
                var currentFileMenuItem = new MenuCommand(ExecuteAnalyzeCurrentFile, currentFileMenuCommandID);
                commandService.AddCommand(currentFileMenuItem);

                // 전체 프로젝트 분석 명령어
                var projectMenuCommandID = new CommandID(CommandSet, AnalyzeProjectCommandId);
                var projectMenuItem = new MenuCommand(ExecuteAnalyzeProject, projectMenuCommandID);
                commandService.AddCommand(projectMenuItem);
            }
        }

        /// <summary>
        /// 명령어 인스턴스
        /// </summary>
        public static AnalyzeCommand Instance { get; private set; }

        /// <summary>
        /// 명령어 초기화
        /// </summary>
        /// <param name="package">VS 패키지</param>
        /// <param name="serviceProvider">서비스 프로바이더</param>
        public static async Task InitializeAsync(AsyncPackage package, IServiceProvider serviceProvider)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            Instance = new AnalyzeCommand(package, serviceProvider);
        }

        /// <summary>
        /// 현재 파일 분석 실행
        /// </summary>
        private void ExecuteAnalyzeCurrentFile(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                _logger?.LogInformation("현재 파일 분석 시작");

                // VS 통합 서비스 가져오기
                var vsIntegration = _serviceProvider.GetService<IVsIntegrationService>();
                var outputService = _serviceProvider.GetService<IOutputWindowService>();

                if (vsIntegration == null || outputService == null)
                {
                    _logger?.LogError("필요한 서비스를 찾을 수 없습니다.");
                    return;
                }

                // 출력 창 활성화 및 초기화
                outputService.Activate();
                outputService.Clear();
                outputService.WriteLine("=== TwinCAT QA 코드 분석 시작 ===");
                outputService.WriteLine($"시작 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                outputService.WriteLine("");

                // 현재 파일 경로 가져오기
                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    var filePath = await vsIntegration.GetActiveDocumentPathAsync();

                    if (string.IsNullOrEmpty(filePath))
                    {
                        outputService.WriteWarning("활성 문서를 찾을 수 없습니다.");
                        return;
                    }

                    outputService.WriteLine($"분석 대상 파일: {filePath}");
                    outputService.WriteLine("");

                    // 분석 실행
                    await AnalyzeFileAsync(filePath, outputService);
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "파일 분석 중 오류 발생");
                var outputService = _serviceProvider.GetService<IOutputWindowService>();
                outputService?.WriteError($"분석 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 전체 프로젝트 분석 실행
        /// </summary>
        private void ExecuteAnalyzeProject(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                _logger?.LogInformation("프로젝트 전체 분석 시작");

                // VS 통합 서비스 가져오기
                var vsIntegration = _serviceProvider.GetService<IVsIntegrationService>();
                var outputService = _serviceProvider.GetService<IOutputWindowService>();

                if (vsIntegration == null || outputService == null)
                {
                    _logger?.LogError("필요한 서비스를 찾을 수 없습니다.");
                    return;
                }

                // 출력 창 활성화 및 초기화
                outputService.Activate();
                outputService.Clear();
                outputService.WriteLine("=== TwinCAT QA 프로젝트 전체 분석 시작 ===");
                outputService.WriteLine($"시작 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                outputService.WriteLine("");

                // 프로젝트의 모든 TwinCAT 파일 가져오기
                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    var files = await vsIntegration.GetAllTwinCatFilesAsync();

                    var fileList = files as string[] ?? files.ToArray();
                    if (!fileList.Any())
                    {
                        outputService.WriteWarning("분석할 TwinCAT 파일을 찾을 수 없습니다.");
                        return;
                    }

                    outputService.WriteLine($"발견된 파일 수: {fileList.Length}개");
                    outputService.WriteLine("");

                    // 각 파일 분석
                    int successCount = 0;
                    int failCount = 0;

                    foreach (var file in fileList)
                    {
                        outputService.WriteLine($"분석 중: {System.IO.Path.GetFileName(file)}");

                        try
                        {
                            await AnalyzeFileAsync(file, outputService);
                            successCount++;
                        }
                        catch (Exception ex)
                        {
                            outputService.WriteError($"  오류: {ex.Message}");
                            failCount++;
                        }

                        outputService.WriteLine("");
                    }

                    // 요약 정보
                    outputService.WriteLine("=== 분석 완료 ===");
                    outputService.WriteLine($"성공: {successCount}개, 실패: {failCount}개");
                    outputService.WriteLine($"종료 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "프로젝트 분석 중 오류 발생");
                var outputService = _serviceProvider.GetService<IOutputWindowService>();
                outputService?.WriteError($"분석 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 파일 분석 실행
        /// </summary>
        /// <param name="filePath">파일 경로</param>
        /// <param name="outputService">출력 서비스</param>
        private async Task AnalyzeFileAsync(string filePath, IOutputWindowService outputService)
        {
            try
            {
                // 분석 오케스트레이터 가져오기
                var orchestrator = _serviceProvider.GetService<AnalysisOrchestrator>();
                if (orchestrator == null)
                {
                    outputService.WriteError("분석 서비스를 초기화할 수 없습니다.");
                    return;
                }

                // 파일 분석 실행
                var result = await orchestrator.AnalyzeFileAsync(filePath);

                if (!result.IsSuccess)
                {
                    outputService.WriteError($"  분석 실패: {result.ErrorMessage}");
                    return;
                }

                // 분석 결과 출력
                outputService.WriteLine($"  복잡도 점수: {result.ComplexityScore:F2}");
                outputService.WriteLine($"  유지보수성 지수: {result.MaintainabilityIndex:F2}");

                if (result.Issues.Any())
                {
                    outputService.WriteWarning($"  발견된 이슈: {result.Issues.Count}개");
                    foreach (var issue in result.Issues.Take(5)) // 처음 5개만 표시
                    {
                        outputService.WriteLine($"    - [{issue.Severity}] {issue.Message} (라인 {issue.LineNumber})");
                    }

                    if (result.Issues.Count > 5)
                    {
                        outputService.WriteLine($"    ... 외 {result.Issues.Count - 5}개");
                    }
                }
                else
                {
                    outputService.WriteLine("  이슈 없음");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "파일 분석 실패: {FilePath}", filePath);
                throw;
            }
        }
    }
}
