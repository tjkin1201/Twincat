using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using TwinCatQA.Application.Services;
using TwinCatQA.Domain.Services;
using TwinCatQA.Infrastructure.Comparison;

namespace TwinCatQA.TestRunner;

/// <summary>
/// RealProjectQATests를 실행하기 위한 간단한 테스트 러너
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("==============================================");
        Console.WriteLine("TwinCAT QA Tool - Real Project QA Tests");
        Console.WriteLine("==============================================\n");

        try
        {
            // 샘플 프로젝트 경로 설정
            var samplesPath = FindSamplesPath();
            var oldVersionPath = Path.Combine(samplesPath, "old_version");
            var newVersionPath = Path.Combine(samplesPath, "new_version");

            Console.WriteLine($"샘플 경로: {samplesPath}");
            Console.WriteLine($"Old Version: {oldVersionPath}");
            Console.WriteLine($"New Version: {newVersionPath}\n");

            // 폴더 존재 확인
            if (!Directory.Exists(oldVersionPath) || !Directory.Exists(newVersionPath))
            {
                Console.WriteLine("⚠️ 샘플 프로젝트 폴더가 존재하지 않습니다.");
                return;
            }

            // DI 컨테이너 설정
            var serviceProvider = SetupServices();
            var folderComparer = serviceProvider.GetRequiredService<IFolderComparer>();
            var qaEngine = serviceProvider.GetRequiredService<IQARuleEngine>();

            // 테스트 1: 기본 QA 분석
            Console.WriteLine("\n=== 테스트 1: 기본 QA 분석 ===");
            await TestBasicQAAnalysis(folderComparer, qaEngine, oldVersionPath, newVersionPath);

            // 테스트 2: Critical 이슈 검출
            Console.WriteLine("\n=== 테스트 2: Critical 이슈 검출 ===");
            await TestCriticalIssueDetection(folderComparer, qaEngine, oldVersionPath, newVersionPath);

            // 테스트 3: 파일별 이슈 그룹화
            Console.WriteLine("\n=== 테스트 3: 파일별 이슈 그룹화 ===");
            await TestIssueGroupingByFile(folderComparer, qaEngine, oldVersionPath, newVersionPath);

            Console.WriteLine("\n==============================================");
            Console.WriteLine("✅ 모든 테스트 완료");
            Console.WriteLine("==============================================");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ 오류 발생: {ex.Message}");
            Console.WriteLine($"스택 트레이스:\n{ex.StackTrace}");
        }
    }

    static async Task TestBasicQAAnalysis(
        IFolderComparer folderComparer,
        IQARuleEngine qaEngine,
        string oldPath,
        string newPath)
    {
        var comparisonResult = await folderComparer.CompareAsync(oldPath, newPath);

        Console.WriteLine($"변수 변경: {comparisonResult.VariableChanges.Count}개");
        Console.WriteLine($"I/O 매핑 변경: {comparisonResult.IOMappingChanges.Count}개");
        Console.WriteLine($"로직 변경: {comparisonResult.LogicChanges.Count}개");
        Console.WriteLine($"데이터 타입 변경: {comparisonResult.DataTypeChanges.Count}개");
        Console.WriteLine($"총 변경: {comparisonResult.TotalChanges}개");

        var qaReport = qaEngine.AnalyzeComparisonResult(comparisonResult);

        Console.WriteLine($"\n📊 QA 분석 결과:");
        Console.WriteLine($"  총 이슈: {qaReport.TotalIssues}개");
        Console.WriteLine($"  - Critical: {qaReport.CriticalCount}개");
        Console.WriteLine($"  - Warning: {qaReport.WarningCount}개");
        Console.WriteLine($"  - Info: {qaReport.InfoCount}개");

        if (qaReport.HasCriticalIssues)
        {
            Console.WriteLine("\n⚠️ Critical 이슈가 발견되었습니다!");
        }
        else
        {
            Console.WriteLine("\n✅ Critical 이슈가 없습니다.");
        }
    }

    static async Task TestCriticalIssueDetection(
        IFolderComparer folderComparer,
        IQARuleEngine qaEngine,
        string oldPath,
        string newPath)
    {
        var comparisonResult = await folderComparer.CompareAsync(oldPath, newPath);
        var qaReport = qaEngine.AnalyzeComparisonResult(comparisonResult);

        var criticalIssues = qaReport.IssuesBySeverity.ContainsKey(TwinCatQA.Domain.Models.QA.Severity.Critical)
            ? qaReport.IssuesBySeverity[TwinCatQA.Domain.Models.QA.Severity.Critical]
            : new System.Collections.Generic.List<TwinCatQA.Domain.Models.QA.QAIssue>();

        Console.WriteLine($"Critical 이슈 수: {criticalIssues.Count}개\n");

        foreach (var issue in criticalIssues.Take(5))
        {
            Console.WriteLine($"🔴 [{issue.RuleId}] {issue.Title}");
            Console.WriteLine($"   파일: {Path.GetFileName(issue.FilePath)}:{issue.Line}");
            Console.WriteLine($"   설명: {issue.Description}");
            if (!string.IsNullOrEmpty(issue.Recommendation))
            {
                Console.WriteLine($"   권장사항: {issue.Recommendation}");
            }
            Console.WriteLine();
        }
    }

    static async Task TestIssueGroupingByFile(
        IFolderComparer folderComparer,
        IQARuleEngine qaEngine,
        string oldPath,
        string newPath)
    {
        var comparisonResult = await folderComparer.CompareAsync(oldPath, newPath);
        var qaReport = qaEngine.AnalyzeComparisonResult(comparisonResult);

        var issuesByFile = qaReport.IssuesByFile;
        Console.WriteLine($"이슈가 있는 파일 수: {issuesByFile.Count}개\n");

        foreach (var (filePath, issues) in issuesByFile.Take(5))
        {
            var fileName = Path.GetFileName(filePath);
            var criticalCount = issues.Count(i => i.Severity == TwinCatQA.Domain.Models.QA.Severity.Critical);
            var warningCount = issues.Count(i => i.Severity == TwinCatQA.Domain.Models.QA.Severity.Warning);
            var infoCount = issues.Count(i => i.Severity == TwinCatQA.Domain.Models.QA.Severity.Info);

            Console.WriteLine($"📄 {fileName}");
            Console.WriteLine($"   총 이슈: {issues.Count}개 (Critical: {criticalCount}, Warning: {warningCount}, Info: {infoCount})");

            foreach (var issue in issues.OrderBy(i => i.Severity).Take(2))
            {
                Console.WriteLine($"   - [{issue.RuleId}] {issue.Title} (라인 {issue.Line})");
            }
            Console.WriteLine();
        }
    }

    static string FindSamplesPath()
    {
        var currentDir = Directory.GetCurrentDirectory();

        // 프로젝트 루트 찾기
        var searchDir = currentDir;
        for (int i = 0; i < 5; i++)
        {
            var samplesPath = Path.Combine(searchDir, "samples");
            if (Directory.Exists(samplesPath))
            {
                return samplesPath;
            }

            var parent = Directory.GetParent(searchDir);
            if (parent == null) break;
            searchDir = parent.FullName;
        }

        // 대체 경로
        var fallbackPath = @"D:\01. Vscode\Twincat\features\twincat-code-qa-tool\samples";
        if (Directory.Exists(fallbackPath))
        {
            return fallbackPath;
        }

        throw new DirectoryNotFoundException($"samples 폴더를 찾을 수 없습니다. 검색 시작 위치: {currentDir}");
    }

    static IServiceProvider SetupServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IVariableComparer, VariableComparer>();
        services.AddSingleton<IIOMappingComparer, IOMappingComparer>();
        services.AddSingleton<ILogicComparer, LogicComparer>();
        services.AddSingleton<IDataTypeComparer, DataTypeComparer>();
        services.AddSingleton<IFolderComparer, FolderComparer>();
        services.AddSingleton<IQARuleEngine, QARuleEngine>();

        return services.BuildServiceProvider();
    }
}
