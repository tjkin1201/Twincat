using System.CommandLine;
using TwinCatQA.CLI.Commands;

namespace TwinCatQA.CLI;

/// <summary>
/// TwinCAT Code QA Tool CLI
///
/// TwinCAT 프로젝트의 코드 품질 분석 도구
/// - compare: 폴더 비교
/// </summary>
class Program
{
    static async Task<int> Main(string[] args)
    {
        // 루트 명령어 생성
        var rootCommand = new RootCommand("TwinCAT Code QA Tool - 코드 품질 분석 도구")
        {
            CompareCommand.Create(),
            QaCommand.Create()
        };

        // 명령어 실행
        return await rootCommand.InvokeAsync(args);
    }
}
