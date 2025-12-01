using System.CommandLine;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using TwinCatQA.Application.Services;
using TwinCatQA.Domain.Models.QA;
using static TwinCatQA.Application.Services.QaAnalysisService;
using static TwinCatQA.Application.Services.QaReportGenerator;

namespace TwinCatQA.CLI.Commands;

/// <summary>
/// QA 분석 명령어
/// </summary>
public static class QaCommand
{
    public static Command Create()
    {
        var command = new Command("qa", "코드 변경 사항에 대한 QA 분석을 수행합니다");

        // 필수 인자
        var oldFolderArg = new Argument<string>(
            "old-folder",
            "이전 버전 폴더 경로");

        var newFolderArg = new Argument<string>(
            "new-folder",
            "신규 버전 폴더 경로");

        // 옵션
        var outputOption = new Option<string>(
            "--output",
            () => "./qa_report",
            "보고서 출력 경로");
        outputOption.AddAlias("-o");

        var formatOption = new Option<string>(
            "--format",
            () => "html",
            "보고서 형식: html, markdown, json, excel, all");
        formatOption.AddAlias("-f");

        var minSeverityOption = new Option<string>(
            "--min-severity",
            () => "info",
            "최소 심각도: critical, warning, info");

        var minConfidenceOption = new Option<string>(
            "--min-confidence",
            () => "low",
            "최소 신뢰도 레벨: high, medium, low (신뢰도 기반 필터링)");
        minConfidenceOption.AddAlias("-c");

        var showSuppressedOption = new Option<bool>(
            "--show-suppressed",
            () => false,
            "억제된 이슈도 표시 (기본값: false)");

        var configOption = new Option<string?>(
            "--config",
            ".twincat-qa.json 설정 파일 경로 (이슈 억제 규칙 포함)");

        var rulesOption = new Option<string?>(
            "--rules",
            "실행할 규칙 ID (쉼표 구분, 예: QA001,QA002,QA003)");

        var excludeRulesOption = new Option<string?>(
            "--exclude-rules",
            "제외할 규칙 ID (쉼표 구분)");

        var verboseOption = new Option<bool>(
            "--verbose",
            "상세 출력");
        verboseOption.AddAlias("-v");

        var jsonOutputOption = new Option<bool>(
            "--json",
            "JSON 형식으로 콘솔 출력 (다른 도구와 연동용)");

        command.Add(oldFolderArg);
        command.Add(newFolderArg);
        command.Add(outputOption);
        command.Add(formatOption);
        command.Add(minSeverityOption);
        command.Add(minConfidenceOption);
        command.Add(showSuppressedOption);
        command.Add(configOption);
        command.Add(rulesOption);
        command.Add(excludeRulesOption);
        command.Add(verboseOption);
        command.Add(jsonOutputOption);

        command.SetHandler(async (context) =>
        {
            var oldFolder = context.ParseResult.GetValueForArgument(oldFolderArg);
            var newFolder = context.ParseResult.GetValueForArgument(newFolderArg);
            var output = context.ParseResult.GetValueForOption(outputOption);
            var format = context.ParseResult.GetValueForOption(formatOption);
            var minSeverity = context.ParseResult.GetValueForOption(minSeverityOption);
            var minConfidence = context.ParseResult.GetValueForOption(minConfidenceOption);
            var showSuppressed = context.ParseResult.GetValueForOption(showSuppressedOption);
            var config = context.ParseResult.GetValueForOption(configOption);
            var rules = context.ParseResult.GetValueForOption(rulesOption);
            var excludeRules = context.ParseResult.GetValueForOption(excludeRulesOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);
            var jsonOutput = context.ParseResult.GetValueForOption(jsonOutputOption);

            await ExecuteQaAsync(oldFolder, newFolder, output, format, minSeverity, minConfidence,
                showSuppressed, config, rules, excludeRules, verbose, jsonOutput);
        });

        return command;
    }

    private static async Task ExecuteQaAsync(
        string oldFolder,
        string newFolder,
        string outputPath,
        string formatStr,
        string minSeverityStr,
        string minConfidenceStr,
        bool showSuppressed,
        string? configPath,
        string? rulesStr,
        string? excludeRulesStr,
        bool verbose,
        bool jsonOutput)
    {
        var stopwatch = Stopwatch.StartNew();

        if (!jsonOutput)
        {
            PrintHeader();
        }

        // 입력 검증
        if (!Directory.Exists(oldFolder))
        {
            PrintError($"이전 버전 폴더가 존재하지 않습니다: {oldFolder}");
            return;
        }

        if (!Directory.Exists(newFolder))
        {
            PrintError($"신규 버전 폴더가 존재하지 않습니다: {newFolder}");
            return;
        }

        if (!jsonOutput)
        {
            Console.WriteLine($"📂 비교 대상:");
            Console.WriteLine($"   - 이전: {Path.GetFullPath(oldFolder)}");
            Console.WriteLine($"   - 신규: {Path.GetFullPath(newFolder)}");
            Console.WriteLine();
        }

        try
        {
            // 옵션 파싱
            var minSeverity = ParseSeverity(minSeverityStr);
            var minConfidence = ParseConfidenceLevel(minConfidenceStr);
            var format = ParseFormat(formatStr);
            var includeRules = rulesStr?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(r => r.Trim()).ToList();
            var excludeRules = excludeRulesStr?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(r => r.Trim()).ToList();

            // 서비스 초기화
            var services = new ServiceCollection();
            TwinCatQA.CLI.Services.ServiceCollectionExtensions.AddTwinCatQAServices(services);
            var serviceProvider = services.BuildServiceProvider();

            var qaService = serviceProvider.GetRequiredService<QaAnalysisService>();
            var enhancedQaService = serviceProvider.GetRequiredService<IEnhancedQAAnalysisService>();
            var reportGenerator = new QaReportGenerator();

            // 설정 파일 로드
            var effectiveConfigPath = !string.IsNullOrWhiteSpace(configPath) ? configPath : newFolder;
            if (!jsonOutput && verbose)
            {
                Console.WriteLine($"📄 설정 파일 로드 경로: {effectiveConfigPath}");
            }
            await enhancedQaService.LoadConfigurationAsync(effectiveConfigPath);

            // QA 분석 실행
            if (!jsonOutput && verbose)
            {
                Console.WriteLine("🔍 TwinCAT 코드 QA 분석 시작...");
                Console.WriteLine();
            }

            var options = new QaAnalysisOptions
            {
                MinSeverity = minSeverity,
                IncludeRules = includeRules,
                ExcludeRules = excludeRules,
                Verbose = verbose
            };

            var result = await qaService.AnalyzeAsync(oldFolder, newFolder, options);

            stopwatch.Stop();

            if (!result.Success)
            {
                PrintError($"분석 실패: {result.ErrorMessage}");
                return;
            }

            // Level 2: 이슈를 EnhancedQAIssue로 변환하고 신뢰도 계산
            // 소스 코드 파일 수집 (ST, TcPOU 등 TwinCAT 파일 포함)
            var sourceExtensions = new[] { "*.st", "*.TcPOU", "*.TcDUT", "*.TcGVL" };
            var sourceFiles = sourceExtensions
                .SelectMany(ext => Directory.GetFiles(newFolder, ext, SearchOption.AllDirectories))
                .ToList();

            var enhancedIssues = new List<EnhancedQAIssue>();
            var processedIssueKeys = new HashSet<string>();

            foreach (var file in sourceFiles)
            {
                try
                {
                    var sourceCode = await File.ReadAllTextAsync(file);
                    var fileName = Path.GetFileName(file);
                    var fileNameWithoutExt = Path.GetFileNameWithoutExtension(file);

                    // 정확한 파일명 매칭 (확장자 제외하고 비교)
                    var fileIssues = result.Issues.Where(i =>
                    {
                        if (string.IsNullOrEmpty(i.FilePath) && string.IsNullOrEmpty(i.Location))
                            return false;

                        var issueFileName = !string.IsNullOrEmpty(i.FilePath)
                            ? Path.GetFileNameWithoutExtension(i.FilePath)
                            : null;
                        var locationFileName = !string.IsNullOrEmpty(i.Location)
                            ? i.Location.Split(':')[0].Trim()
                            : null;

                        return string.Equals(issueFileName, fileNameWithoutExt, StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(locationFileName, fileNameWithoutExt, StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(locationFileName, fileName, StringComparison.OrdinalIgnoreCase);
                    }).ToList();

                    if (fileIssues.Any())
                    {
                        var enhanced = enhancedQaService.EnhanceIssues(fileIssues, sourceCode, file);
                        foreach (var e in enhanced)
                        {
                            var key = $"{e.RuleId}|{e.Line}|{e.FilePath}";
                            if (processedIssueKeys.Add(key))
                            {
                                enhancedIssues.Add(e);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (verbose)
                    {
                        Console.WriteLine($"⚠️ 파일 향상 중 오류: {file} - {ex.Message}");
                    }
                }
            }

            // 파일에 매핑되지 않은 이슈도 기본 향상 적용
            foreach (var issue in result.Issues)
            {
                var key = $"{issue.RuleId}|{issue.Line}|{issue.FilePath}";
                if (!processedIssueKeys.Contains(key))
                {
                    var enhanced = EnhancedQAIssue.FromQAIssue(issue);
                    enhanced.AnalysisLevel = 1; // Level 1 분석 (휴리스틱)
                    enhanced.ConfidenceScore = 50;
                    enhanced.Confidence = ConfidenceLevel.Medium;
                    enhanced.ConfidenceReasons.Add("소스 파일을 찾을 수 없어 기본 신뢰도 적용");
                    enhancedIssues.Add(enhanced);
                }
            }

            // 신뢰도 필터링
            var filteredByConfidence = enhancedQaService.FilterByConfidence(enhancedIssues, minConfidence);

            // 억제된 이슈 필터링
            var finalIssues = showSuppressed
                ? filteredByConfidence
                : enhancedQaService.ExcludeSuppressed(filteredByConfidence);

            // 결과에 향상된 이슈 적용 (QAIssue 타입으로 캐스팅)
            var displayIssues = finalIssues.Cast<QAIssue>().ToList();

            if (verbose && !jsonOutput)
            {
                var stats = enhancedQaService.GetStatistics(enhancedIssues);
                Console.WriteLine($"\n📊 신뢰도 통계: {stats}");
                Console.WriteLine($"   - 필터링 후: {finalIssues.Count}개 이슈 표시");
            }

            // 결과 업데이트 (필터링된 이슈로)
            var filteredResult = new QaAnalysisResult
            {
                Success = result.Success,
                ComparisonResult = result.ComparisonResult,
                Issues = displayIssues
                // CriticalCount, WarningCount, InfoCount는 Issues에서 자동 계산됨
            };

            // JSON 출력 모드
            if (jsonOutput)
            {
                var jsonReport = new
                {
                    success = true,
                    elapsedSeconds = stopwatch.Elapsed.TotalSeconds,
                    summary = new
                    {
                        total = filteredResult.Issues.Count,
                        critical = filteredResult.CriticalCount,
                        warning = filteredResult.WarningCount,
                        info = filteredResult.InfoCount,
                        originalTotal = result.Issues.Count,
                        filteredOut = result.Issues.Count - filteredResult.Issues.Count
                    },
                    confidence = new
                    {
                        minLevel = minConfidence.ToString(),
                        highConfidence = finalIssues.Count(i => i.Confidence == ConfidenceLevel.High),
                        mediumConfidence = finalIssues.Count(i => i.Confidence == ConfidenceLevel.Medium),
                        lowConfidence = finalIssues.Count(i => i.Confidence == ConfidenceLevel.Low),
                        suppressed = enhancedIssues.Count(i => i.IsSuppressed)
                    },
                    changes = new
                    {
                        variables = result.ComparisonResult.VariableChanges.Count,
                        logic = result.ComparisonResult.LogicChanges.Count,
                        dataTypes = result.ComparisonResult.DataTypeChanges.Count
                    },
                    issues = finalIssues.Select(i => new
                    {
                        ruleId = i.RuleId,
                        severity = i.Severity.ToString(),
                        title = i.Title,
                        description = i.Description,
                        filePath = i.FilePath,
                        line = i.Line,
                        confidence = i.Confidence.ToString(),
                        confidenceScore = i.ConfidenceScore,
                        analysisLevel = i.AnalysisLevel,
                        isSuppressed = i.IsSuppressed,
                        recommendation = i.Recommendation
                    })
                };
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(jsonReport, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                return;
            }

            // 일반 출력 모드
            PrintAnalysisResult(filteredResult, verbose);

            // 보고서 생성
            if (!string.IsNullOrWhiteSpace(outputPath))
            {
                Console.WriteLine();
                Console.WriteLine("📝 보고서 생성 중...");
                var files = await reportGenerator.GenerateReportsAsync(filteredResult, outputPath, format);

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine();
                Console.WriteLine("✅ 보고서 생성 완료:");
                foreach (var file in files)
                {
                    Console.WriteLine($"   - {Path.GetFullPath(file)}");
                }
                Console.ResetColor();
            }

            // 최종 요약
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✅ 분석 완료! (소요 시간: {stopwatch.Elapsed.TotalSeconds:F2}초)");
            Console.ResetColor();

            // 신뢰도 정보 출력
            if (!verbose && finalIssues.Any())
            {
                var highCount = finalIssues.Count(i => i.Confidence == ConfidenceLevel.High);
                if (highCount > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"💡 높은 신뢰도 이슈: {highCount}개 (--verbose로 상세 정보 확인)");
                    Console.ResetColor();
                }
            }

            // Critical 이슈가 있으면 경고
            if (filteredResult.CriticalCount > 0)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"⚠️  주의: {filteredResult.CriticalCount}개의 Critical 이슈가 발견되었습니다!");
                Console.ResetColor();
            }
        }
        catch (Exception ex)
        {
            PrintError($"예상치 못한 오류 발생: {ex.Message}");
            if (verbose)
            {
                Console.WriteLine(ex.StackTrace);
            }
        }
    }

    private static void PrintHeader()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("   🔍 TwinCAT 코드 QA 분석 도구");
        Console.ResetColor();
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();
    }

    private static void PrintAnalysisResult(QaAnalysisResult result, bool verbose)
    {
        Console.WriteLine();
        Console.WriteLine("📊 변경 사항 감지:");
        Console.WriteLine($"   - 변수 변경: {result.ComparisonResult.VariableChanges.Count}건");
        Console.WriteLine($"   - 로직 변경: {result.ComparisonResult.LogicChanges.Count}건");
        Console.WriteLine($"   - 데이터 타입 변경: {result.ComparisonResult.DataTypeChanges.Count}건");
        Console.WriteLine();

        Console.WriteLine("🚨 QA 이슈 발견:");
        Console.WriteLine("┌─────────────┬───────┬──────────────────────────────────┐");
        Console.WriteLine("│ 심각도      │ 개수  │ 설명                              │");
        Console.WriteLine("├─────────────┼───────┼──────────────────────────────────┤");

        Console.Write("│ ");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write("🔴 Critical");
        Console.ResetColor();
        Console.WriteLine($" │ {result.CriticalCount,-5} │ 타입 축소, NULL 체크 누락 등      │");

        Console.Write("│ ");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("🟡 Warning ");
        Console.ResetColor();
        Console.WriteLine($" │ {result.WarningCount,-5} │ 매직 넘버, 긴 함수 등             │");

        Console.Write("│ ");
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.Write("🔵 Info    ");
        Console.ResetColor();
        Console.WriteLine($" │ {result.InfoCount,-5} │ 주석 부족, 스타일 불일치          │");

        Console.WriteLine("└─────────────┴───────┴──────────────────────────────────┘");

        // 상세 이슈 목록
        if (result.Issues.Any())
        {
            Console.WriteLine();
            Console.WriteLine("📋 상세 이슈 목록:");
            Console.WriteLine();

            var displayCount = verbose ? result.Issues.Count : Math.Min(5, result.Issues.Count);
            foreach (var issue in result.Issues.OrderByDescending(i => i.Severity).Take(displayCount))
            {
                PrintIssue(issue, verbose);
            }

            if (!verbose && result.Issues.Count > 5)
            {
                Console.WriteLine($"... 외 {result.Issues.Count - 5}건 (--verbose 옵션으로 전체 보기)");
            }
        }
        else
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✅ 발견된 이슈가 없습니다!");
            Console.ResetColor();
        }
    }

    private static void PrintIssue(QAIssue issue, bool verbose)
    {
        // 심각도 배지
        var (icon, color) = issue.Severity switch
        {
            Severity.Critical => ("🔴", ConsoleColor.Red),
            Severity.Warning => ("🟡", ConsoleColor.Yellow),
            _ => ("🔵", ConsoleColor.Blue)
        };

        // EnhancedQAIssue인 경우 신뢰도 정보 표시
        var confidenceInfo = "";
        if (issue is EnhancedQAIssue enhanced)
        {
            var confidenceIcon = enhanced.Confidence switch
            {
                ConfidenceLevel.High => "⭐",
                ConfidenceLevel.Medium => "⚡",
                ConfidenceLevel.Low => "❓",
                _ => ""
            };
            confidenceInfo = $" {confidenceIcon}[{enhanced.Confidence} {enhanced.ConfidenceScore}%]";

            if (enhanced.IsSuppressed)
            {
                confidenceInfo += " [억제됨]";
            }
        }

        Console.Write($"[{issue.RuleId}] ");
        Console.ForegroundColor = color;
        Console.Write($"{icon} {issue.Severity}");
        Console.ResetColor();
        Console.Write(confidenceInfo);
        Console.WriteLine($" - {issue.Title}");

        Console.WriteLine($"  📍 {issue.Location}");
        Console.WriteLine($"  📝 {issue.Description}");

        if (verbose)
        {
            if (!string.IsNullOrEmpty(issue.Recommendation))
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"  💡 {issue.Recommendation.Trim().Replace("\n", "\n     ")}");
                Console.ResetColor();
            }

            if (issue.Examples?.Any() == true)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("  예시:");
                foreach (var example in issue.Examples.Take(3))
                {
                    Console.WriteLine($"     {example}");
                }
                Console.ResetColor();
            }
        }

        Console.WriteLine();
    }

    private static void PrintError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"❌ 오류: {message}");
        Console.ResetColor();
    }

    private static Severity ParseSeverity(string severityStr)
    {
        return severityStr.ToLower() switch
        {
            "critical" => Severity.Critical,
            "warning" => Severity.Warning,
            "info" => Severity.Info,
            _ => Severity.Info
        };
    }

    private static ConfidenceLevel ParseConfidenceLevel(string confidenceStr)
    {
        return confidenceStr.ToLower() switch
        {
            "high" => ConfidenceLevel.High,
            "medium" => ConfidenceLevel.Medium,
            "low" => ConfidenceLevel.Low,
            _ => ConfidenceLevel.Low
        };
    }

    private static ReportFormat ParseFormat(string formatStr)
    {
        return formatStr.ToLower() switch
        {
            "html" => ReportFormat.Html,
            "markdown" or "md" => ReportFormat.Markdown,
            "json" => ReportFormat.Json,
            "excel" or "xlsx" => ReportFormat.Excel,
            "all" => ReportFormat.All,
            _ => ReportFormat.Html
        };
    }
}
