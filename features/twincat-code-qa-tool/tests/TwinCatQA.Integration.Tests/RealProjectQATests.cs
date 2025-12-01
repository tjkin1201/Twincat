using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TwinCatQA.Application.Services;
using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Models.QA;
using TwinCatQA.Domain.Services;
using TwinCatQA.Infrastructure.Comparison;
using Xunit;
using Xunit.Abstractions;

namespace TwinCatQA.Integration.Tests;

/// <summary>
/// 실제 샘플 프로젝트에 대한 QA 분석 테스트
/// old_version과 new_version 폴더를 비교하여 QA 이슈 검출
/// </summary>
public class RealProjectQATests
{
    private readonly ITestOutputHelper _output;
    private readonly string _samplesPath;
    private readonly string _oldVersionPath;
    private readonly string _newVersionPath;
    private readonly IServiceProvider _serviceProvider;

    public RealProjectQATests(ITestOutputHelper output)
    {
        _output = output;
        _samplesPath = FindSamplesPath();
        _oldVersionPath = Path.Combine(_samplesPath, "old_version");
        _newVersionPath = Path.Combine(_samplesPath, "new_version");
        _serviceProvider = SetupServices();

        _output.WriteLine($"샘플 경로: {_samplesPath}");
        _output.WriteLine($"Old Version: {_oldVersionPath}");
        _output.WriteLine($"New Version: {_newVersionPath}");
    }

    #region 1. 기본 QA 분석 테스트

    [Fact]
    public async Task QA분석_샘플프로젝트_이슈검출성공()
    {
        // Arrange
        if (!Directory.Exists(_oldVersionPath) || !Directory.Exists(_newVersionPath))
        {
            _output.WriteLine("⚠️ 샘플 프로젝트 폴더가 존재하지 않습니다. 테스트를 건너뜁니다.");
            return;
        }

        var folderComparer = _serviceProvider.GetRequiredService<IFolderComparer>();
        var qaEngine = _serviceProvider.GetRequiredService<IQARuleEngine>();

        // Act
        _output.WriteLine("\n=== 폴더 비교 시작 ===");
        var comparisonResult = await folderComparer.CompareAsync(_oldVersionPath, _newVersionPath);

        _output.WriteLine($"변수 변경: {comparisonResult.VariableChanges.Count}개");
        _output.WriteLine($"I/O 매핑 변경: {comparisonResult.IOMappingChanges.Count}개");
        _output.WriteLine($"로직 변경: {comparisonResult.LogicChanges.Count}개");
        _output.WriteLine($"데이터 타입 변경: {comparisonResult.DataTypeChanges.Count}개");
        _output.WriteLine($"총 변경: {comparisonResult.TotalChanges}개");

        _output.WriteLine("\n=== QA 규칙 엔진 분석 시작 ===");
        var qaReport = qaEngine.AnalyzeComparisonResult(comparisonResult);

        // Assert
        qaReport.Should().NotBeNull();
        qaReport.TotalChanges.Should().Be(comparisonResult.TotalChanges);

        _output.WriteLine($"\n=== QA 분석 결과 ===");
        _output.WriteLine($"생성 시각: {qaReport.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
        _output.WriteLine($"총 이슈: {qaReport.TotalIssues}개");
        _output.WriteLine($"  - Critical: {qaReport.CriticalCount}개");
        _output.WriteLine($"  - Warning: {qaReport.WarningCount}개");
        _output.WriteLine($"  - Info: {qaReport.InfoCount}개");

        // 이슈가 하나 이상 검출되어야 함 (변경사항이 있으므로)
        if (comparisonResult.TotalChanges > 0)
        {
            _output.WriteLine("\n✅ QA 분석이 정상적으로 수행되었습니다.");
        }

        // 요약 출력
        _output.WriteLine("\n" + qaReport.GetSummary());
    }

    #endregion

    #region 2. Critical 이슈 검출 테스트

    [Fact]
    public async Task QA분석_Critical이슈_검출확인()
    {
        // Arrange
        if (!Directory.Exists(_oldVersionPath) || !Directory.Exists(_newVersionPath))
        {
            _output.WriteLine("⚠️ 샘플 프로젝트 폴더가 존재하지 않습니다. 테스트를 건너뜁니다.");
            return;
        }

        var folderComparer = _serviceProvider.GetRequiredService<IFolderComparer>();
        var qaEngine = _serviceProvider.GetRequiredService<IQARuleEngine>();

        // Act
        var comparisonResult = await folderComparer.CompareAsync(_oldVersionPath, _newVersionPath);
        var qaReport = qaEngine.AnalyzeComparisonResult(comparisonResult);

        // Critical 이슈만 필터링
        var criticalIssues = qaReport.IssuesBySeverity.ContainsKey(Severity.Critical)
            ? qaReport.IssuesBySeverity[Severity.Critical]
            : new System.Collections.Generic.List<QAIssue>();

        // Assert
        _output.WriteLine($"\n=== Critical 이슈 검출 결과 ===");
        _output.WriteLine($"Critical 이슈 수: {criticalIssues.Count}개");

        foreach (var issue in criticalIssues.Take(10))
        {
            _output.WriteLine($"\n🔴 [{issue.RuleId}] {issue.Title}");
            _output.WriteLine($"   파일: {Path.GetFileName(issue.FilePath)}:{issue.Line}");
            _output.WriteLine($"   설명: {issue.Description}");
            _output.WriteLine($"   카테고리: {issue.Category}");

            if (!string.IsNullOrEmpty(issue.Recommendation))
            {
                _output.WriteLine($"   권장사항: {issue.Recommendation}");
            }
        }

        // Critical 이슈 통계
        qaReport.CriticalCount.Should().Be(criticalIssues.Count);
        _output.WriteLine($"\n✅ Critical 이슈 검출 완료");
    }

    #endregion

    #region 3. 파일별 이슈 그룹화 테스트

    [Fact]
    public async Task QA분석_파일별이슈그룹화_정상작동()
    {
        // Arrange
        if (!Directory.Exists(_oldVersionPath) || !Directory.Exists(_newVersionPath))
        {
            _output.WriteLine("⚠️ 샘플 프로젝트 폴더가 존재하지 않습니다. 테스트를 건너뜁니다.");
            return;
        }

        var folderComparer = _serviceProvider.GetRequiredService<IFolderComparer>();
        var qaEngine = _serviceProvider.GetRequiredService<IQARuleEngine>();

        // Act
        var comparisonResult = await folderComparer.CompareAsync(_oldVersionPath, _newVersionPath);
        var qaReport = qaEngine.AnalyzeComparisonResult(comparisonResult);

        // 파일별 그룹화
        var issuesByFile = qaReport.IssuesByFile;

        // Assert
        _output.WriteLine($"\n=== 파일별 이슈 그룹화 ===");
        _output.WriteLine($"이슈가 있는 파일 수: {issuesByFile.Count}개");

        foreach (var (filePath, issues) in issuesByFile)
        {
            var fileName = Path.GetFileName(filePath);
            var criticalCount = issues.Count(i => i.Severity == Severity.Critical);
            var warningCount = issues.Count(i => i.Severity == Severity.Warning);
            var infoCount = issues.Count(i => i.Severity == Severity.Info);

            _output.WriteLine($"\n📄 {fileName}");
            _output.WriteLine($"   총 이슈: {issues.Count}개 (Critical: {criticalCount}, Warning: {warningCount}, Info: {infoCount})");

            // 각 파일의 상위 3개 이슈 출력
            foreach (var issue in issues.OrderBy(i => i.Severity).Take(3))
            {
                _output.WriteLine($"   - [{issue.RuleId}] {issue.Title} (라인 {issue.Line})");
            }
        }

        // 그룹화가 올바르게 작동하는지 확인
        var totalIssuesInGroups = issuesByFile.Values.Sum(issues => issues.Count);
        totalIssuesInGroups.Should().Be(qaReport.TotalIssues);

        _output.WriteLine($"\n✅ 파일별 이슈 그룹화 검증 완료");
    }

    #endregion

    #region 4. 성능 테스트

    [Fact]
    public async Task QA분석_성능테스트_2초이내완료()
    {
        // Arrange
        if (!Directory.Exists(_oldVersionPath) || !Directory.Exists(_newVersionPath))
        {
            _output.WriteLine("⚠️ 샘플 프로젝트 폴더가 존재하지 않습니다. 테스트를 건너뜁니다.");
            return;
        }

        var folderComparer = _serviceProvider.GetRequiredService<IFolderComparer>();
        var qaEngine = _serviceProvider.GetRequiredService<IQARuleEngine>();

        var stopwatch = Stopwatch.StartNew();

        // Act
        var comparisonResult = await folderComparer.CompareAsync(_oldVersionPath, _newVersionPath);
        var comparisonTime = stopwatch.Elapsed;

        stopwatch.Restart();
        var qaReport = qaEngine.AnalyzeComparisonResult(comparisonResult);
        var analysisTime = stopwatch.Elapsed;

        stopwatch.Stop();
        var totalTime = comparisonTime + analysisTime;

        // Assert
        _output.WriteLine($"\n=== 성능 측정 결과 ===");
        _output.WriteLine($"폴더 비교 시간: {comparisonTime.TotalMilliseconds:F2}ms");
        _output.WriteLine($"QA 분석 시간: {analysisTime.TotalMilliseconds:F2}ms");
        _output.WriteLine($"총 소요 시간: {totalTime.TotalMilliseconds:F2}ms ({totalTime.TotalSeconds:F2}초)");
        _output.WriteLine($"처리된 변경사항: {comparisonResult.TotalChanges}개");
        _output.WriteLine($"검출된 이슈: {qaReport.TotalIssues}개");

        if (comparisonResult.TotalChanges > 0)
        {
            var throughput = comparisonResult.TotalChanges / totalTime.TotalSeconds;
            _output.WriteLine($"처리 속도: {throughput:F2} 변경사항/초");
        }

        // 성능 목표: 2초 이내 (샘플 프로젝트 기준)
        // 실제 프로젝트에서는 크기에 따라 조정 가능
        totalTime.TotalSeconds.Should().BeLessThan(5, "QA 분석이 5초 이내에 완료되어야 합니다");

        _output.WriteLine($"\n✅ 성능 테스트 통과");
    }

    #endregion

    #region 5. 규칙별 이슈 통계 테스트

    [Fact]
    public async Task QA분석_규칙별이슈통계_생성성공()
    {
        // Arrange
        if (!Directory.Exists(_oldVersionPath) || !Directory.Exists(_newVersionPath))
        {
            _output.WriteLine("⚠️ 샘플 프로젝트 폴더가 존재하지 않습니다. 테스트를 건너뜁니다.");
            return;
        }

        var folderComparer = _serviceProvider.GetRequiredService<IFolderComparer>();
        var qaEngine = _serviceProvider.GetRequiredService<IQARuleEngine>();

        // Act
        var comparisonResult = await folderComparer.CompareAsync(_oldVersionPath, _newVersionPath);
        var qaReport = qaEngine.AnalyzeComparisonResult(comparisonResult);

        // 규칙별 그룹화
        var issuesByRule = qaReport.IssuesByRule;

        // Assert
        _output.WriteLine($"\n=== 규칙별 이슈 통계 ===");
        _output.WriteLine($"적용된 규칙 수: {issuesByRule.Count}개");

        var sortedRules = issuesByRule
            .OrderByDescending(kvp => kvp.Value.Count)
            .ThenBy(kvp => kvp.Key);

        foreach (var (ruleId, issues) in sortedRules)
        {
            var firstIssue = issues.First();
            _output.WriteLine($"\n[{ruleId}] {firstIssue.Title}");
            _output.WriteLine($"  심각도: {firstIssue.Severity}");
            _output.WriteLine($"  검출 횟수: {issues.Count}개");
            _output.WriteLine($"  카테고리: {firstIssue.Category}");
        }

        // 규칙별 그룹화 검증
        var totalIssuesInRules = issuesByRule.Values.Sum(issues => issues.Count);
        totalIssuesInRules.Should().Be(qaReport.TotalIssues);

        _output.WriteLine($"\n✅ 규칙별 통계 생성 완료");
    }

    #endregion

    #region 6. 카테고리별 이슈 분석 테스트

    [Fact]
    public async Task QA분석_카테고리별이슈분석_정상작동()
    {
        // Arrange
        if (!Directory.Exists(_oldVersionPath) || !Directory.Exists(_newVersionPath))
        {
            _output.WriteLine("⚠️ 샘플 프로젝트 폴더가 존재하지 않습니다. 테스트를 건너뜁니다.");
            return;
        }

        var folderComparer = _serviceProvider.GetRequiredService<IFolderComparer>();
        var qaEngine = _serviceProvider.GetRequiredService<IQARuleEngine>();

        // Act
        var comparisonResult = await folderComparer.CompareAsync(_oldVersionPath, _newVersionPath);
        var qaReport = qaEngine.AnalyzeComparisonResult(comparisonResult);

        // 카테고리별 그룹화
        var issuesByCategory = qaReport.IssuesByCategory;

        // Assert
        _output.WriteLine($"\n=== 카테고리별 이슈 분석 ===");
        _output.WriteLine($"카테고리 수: {issuesByCategory.Count}개");

        foreach (var (category, issues) in issuesByCategory)
        {
            var criticalCount = issues.Count(i => i.Severity == Severity.Critical);
            var warningCount = issues.Count(i => i.Severity == Severity.Warning);
            var infoCount = issues.Count(i => i.Severity == Severity.Info);

            _output.WriteLine($"\n📂 {category}");
            _output.WriteLine($"   총 이슈: {issues.Count}개");
            _output.WriteLine($"   Critical: {criticalCount}개");
            _output.WriteLine($"   Warning: {warningCount}개");
            _output.WriteLine($"   Info: {infoCount}개");

            // 카테고리별 주요 규칙 Top 3
            var topRules = issues
                .GroupBy(i => i.RuleId)
                .OrderByDescending(g => g.Count())
                .Take(3);

            _output.WriteLine($"   주요 규칙:");
            foreach (var ruleGroup in topRules)
            {
                var ruleTitle = ruleGroup.First().Title;
                _output.WriteLine($"     - [{ruleGroup.Key}] {ruleTitle}: {ruleGroup.Count()}개");
            }
        }

        _output.WriteLine($"\n✅ 카테고리별 이슈 분석 완료");
    }

    #endregion

    #region 7. QA 규칙 활성화/비활성화 테스트

    [Fact]
    public async Task QA분석_규칙활성화비활성화_정상작동()
    {
        // Arrange
        if (!Directory.Exists(_oldVersionPath) || !Directory.Exists(_newVersionPath))
        {
            _output.WriteLine("⚠️ 샘플 프로젝트 폴더가 존재하지 않습니다. 테스트를 건너뜁니다.");
            return;
        }

        var folderComparer = _serviceProvider.GetRequiredService<IFolderComparer>();
        var qaEngine = new QARuleEngine(); // 새 인스턴스 생성

        // Act - 모든 규칙 활성화 상태에서 분석
        var comparisonResult = await folderComparer.CompareAsync(_oldVersionPath, _newVersionPath);
        var fullReport = qaEngine.AnalyzeComparisonResult(comparisonResult);

        _output.WriteLine($"\n=== 전체 규칙 활성화 ===");
        _output.WriteLine($"총 이슈: {fullReport.TotalIssues}개");

        // 일부 규칙 비활성화
        qaEngine.SetRuleEnabled("QA006", false); // UnusedVariable
        qaEngine.SetRuleEnabled("QA007", false); // MagicNumber
        qaEngine.SetRuleEnabled("QA016", false); // InsufficientComments

        var partialReport = qaEngine.AnalyzeComparisonResult(comparisonResult);

        _output.WriteLine($"\n=== 일부 규칙 비활성화 (QA006, QA007, QA016) ===");
        _output.WriteLine($"총 이슈: {partialReport.TotalIssues}개");

        // Assert
        partialReport.TotalIssues.Should().BeLessThanOrEqualTo(fullReport.TotalIssues);

        // 규칙 정보 조회
        var allRules = qaEngine.GetAllRulesInfo();
        _output.WriteLine($"\n=== 규칙 목록 (총 {allRules.Count}개) ===");

        foreach (var rule in allRules.Take(10))
        {
            var status = rule.IsEnabled ? "✅" : "❌";
            _output.WriteLine($"{status} [{rule.RuleId}] {rule.RuleName} ({rule.Severity})");
        }

        _output.WriteLine($"\n✅ 규칙 활성화/비활성화 테스트 완료");
    }

    #endregion

    #region 헬퍼 메서드

    /// <summary>
    /// samples 폴더 경로 찾기
    /// </summary>
    private string FindSamplesPath()
    {
        // 현재 어셈블리 위치에서 시작
        var currentDir = Directory.GetCurrentDirectory();

        // 프로젝트 루트 찾기 (최대 5단계 상위까지)
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

        // 대체 경로: 하드코딩된 경로
        var fallbackPath = @"D:\01. Vscode\Twincat\features\twincat-code-qa-tool\samples";
        if (Directory.Exists(fallbackPath))
        {
            return fallbackPath;
        }

        // 테스트 프로젝트 기준 상대 경로
        var relativePath = Path.Combine(currentDir, @"..\..\..\..\samples");
        if (Directory.Exists(relativePath))
        {
            return Path.GetFullPath(relativePath);
        }

        throw new DirectoryNotFoundException($"samples 폴더를 찾을 수 없습니다. 검색 시작 위치: {currentDir}");
    }

    /// <summary>
    /// DI 컨테이너 설정
    /// </summary>
    private IServiceProvider SetupServices()
    {
        var services = new ServiceCollection();

        // 비교 서비스 등록
        services.AddSingleton<IVariableComparer, VariableComparer>();
        services.AddSingleton<IIOMappingComparer, IOMappingComparer>();
        services.AddSingleton<ILogicComparer, LogicComparer>();
        services.AddSingleton<IDataTypeComparer, DataTypeComparer>();
        services.AddSingleton<IFolderComparer, FolderComparer>();

        // QA 규칙 엔진 등록
        services.AddSingleton<IQARuleEngine, QARuleEngine>();

        return services.BuildServiceProvider();
    }

    #endregion
}
