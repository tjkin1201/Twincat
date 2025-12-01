using System.CommandLine;
using TwinCatQA.Domain.Services;
using TwinCatQA.Infrastructure.Comparison;

namespace TwinCatQA.CLI.Commands;

/// <summary>
/// 폴더 비교 명령어
/// </summary>
public static class CompareCommand
{
    public static Command Create()
    {
        var command = new Command("compare", "두 TwinCAT 프로젝트 폴더를 비교합니다");

        var sourceOption = new Option<string>(
            "--source",
            "Source 폴더 경로 (비교 기준)")
        { IsRequired = true };
        sourceOption.AddAlias("-s");

        var targetOption = new Option<string>(
            "--target",
            "Target 폴더 경로 (비교 대상)")
        { IsRequired = true };
        targetOption.AddAlias("-t");

        var variablesOption = new Option<bool>(
            "--variables",
            () => true,
            "변수 변경 비교 포함");

        var ioMappingOption = new Option<bool>(
            "--io-mapping",
            () => true,
            "I/O 매핑 변경 비교 포함");

        var logicOption = new Option<bool>(
            "--logic",
            () => true,
            "로직 변경 비교 포함");

        var dataTypesOption = new Option<bool>(
            "--data-types",
            () => true,
            "데이터 타입 변경 비교 포함");

        var outputOption = new Option<string?>(
            "--output",
            "결과를 파일로 저장 (JSON 형식)");
        outputOption.AddAlias("-o");

        command.Add(sourceOption);
        command.Add(targetOption);
        command.Add(variablesOption);
        command.Add(ioMappingOption);
        command.Add(logicOption);
        command.Add(dataTypesOption);
        command.Add(outputOption);

        command.SetHandler(async (source, target, variables, ioMapping, logic, dataTypes, output) =>
        {
            await ExecuteCompareAsync(source!, target!, variables, ioMapping, logic, dataTypes, output);
        },
        sourceOption, targetOption, variablesOption, ioMappingOption, logicOption, dataTypesOption, outputOption);

        return command;
    }

    private static async Task ExecuteCompareAsync(
        string sourcePath,
        string targetPath,
        bool includeVariables,
        bool includeIOMapping,
        bool includeLogic,
        bool includeDataTypes,
        string? outputPath)
    {
        Console.WriteLine("??????????????????????????????????????????????");
        Console.WriteLine("?   TwinCAT 프로젝트 폴더 비교 도구         ?");
        Console.WriteLine("??????????????????????????????????????????????");
        Console.WriteLine();

        // 입력 검증
        if (!Directory.Exists(sourcePath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"? 오류: Source 폴더가 존재하지 않습니다: {sourcePath}");
            Console.ResetColor();
            return;
        }

        if (!Directory.Exists(targetPath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"? 오류: Target 폴더가 존재하지 않습니다: {targetPath}");
            Console.ResetColor();
            return;
        }

        Console.WriteLine($"?? Source: {sourcePath}");
        Console.WriteLine($"?? Target: {targetPath}");
        Console.WriteLine();

        // 비교 옵션 설정
        var options = new CompareOptions
        {
            IncludeVariables = includeVariables,
            IncludeIOMapping = includeIOMapping,
            IncludeLogic = includeLogic,
            IncludeDataTypes = includeDataTypes
        };

        Console.WriteLine("비교 옵션:");
        Console.WriteLine($"  ? 변수 비교: {(includeVariables ? "포함" : "제외")}");
        Console.WriteLine($"  ? I/O 매핑: {(includeIOMapping ? "포함" : "제외")}");
        Console.WriteLine($"  ? 로직 비교: {(includeLogic ? "포함" : "제외")}");
        Console.WriteLine($"  ? 데이터 타입: {(includeDataTypes ? "포함" : "제외")}");
        Console.WriteLine();

        try
        {
            Console.WriteLine("? 폴더를 비교하는 중...");
            var comparer = new FolderComparer();
            var result = await comparer.CompareAsync(sourcePath, targetPath, options);

            Console.WriteLine();
            Console.WriteLine("??????????????????????????????????????????????");
            Console.WriteLine("?            비교 결과 요약                  ?");
            Console.WriteLine("??????????????????????????????????????????????");
            Console.WriteLine();

            if (!result.HasChanges)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("? 두 폴더의 내용이 동일합니다. 변경사항이 없습니다.");
                Console.ResetColor();
                return;
            }

            // 통계 출력
            Console.WriteLine($"?? 총 변경사항: {result.TotalChanges}개");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  ? 추가됨: {result.AddedCount}개");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ? 제거됨: {result.RemovedCount}개");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  ??  수정됨: {result.ModifiedCount}개");
            Console.ResetColor();
            Console.WriteLine();

            // 상세 결과 출력
            if (result.VariableChanges.Any())
            {
                PrintVariableChanges(result.VariableChanges);
            }

            if (result.IOMappingChanges.Any())
            {
                PrintIOMappingChanges(result.IOMappingChanges);
            }

            if (result.LogicChanges.Any())
            {
                PrintLogicChanges(result.LogicChanges);
            }

            if (result.DataTypeChanges.Any())
            {
                PrintDataTypeChanges(result.DataTypeChanges);
            }

            // 파일로 저장
            if (!string.IsNullOrWhiteSpace(outputPath))
            {
                await SaveResultToFileAsync(result, outputPath);
            }

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("? 비교 완료!");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"? 오류 발생: {ex.Message}");
            Console.ResetColor();
        }
    }

    private static void PrintVariableChanges(List<TwinCatQA.Domain.Models.VariableChange> changes)
    {
        Console.WriteLine("─────────────────────────────────────────────");
        Console.WriteLine("?? 변수 변경 내역");
        Console.WriteLine("─────────────────────────────────────────────");

        foreach (var change in changes.Take(10))
        {
            Console.Write($"  {GetChangeTypeSymbol(change.ChangeType)} ");
            Console.ForegroundColor = GetChangeTypeColor(change.ChangeType);
            Console.Write($"{change.ChangeType,-10}");
            Console.ResetColor();
            Console.Write($" {change.VariableName,-20}");

            if (change.ChangeType == TwinCatQA.Domain.Models.ChangeType.Modified)
            {
                Console.Write($" {change.OldDataType} → {change.NewDataType}");
            }
            else if (change.ChangeType == TwinCatQA.Domain.Models.ChangeType.Added)
            {
                Console.Write($" ({change.NewDataType})");
            }
            else
            {
                Console.Write($" ({change.OldDataType})");
            }

            Console.WriteLine();
        }

        if (changes.Count > 10)
        {
            Console.WriteLine($"  ... 외 {changes.Count - 10}개");
        }

        Console.WriteLine();
    }

    private static void PrintIOMappingChanges(List<TwinCatQA.Domain.Models.IOMappingChange> changes)
    {
        Console.WriteLine("─────────────────────────────────────────────");
        Console.WriteLine("?? I/O 매핑 변경 내역");
        Console.WriteLine("─────────────────────────────────────────────");

        foreach (var change in changes.Take(10))
        {
            Console.Write($"  {GetChangeTypeSymbol(change.ChangeType)} ");
            Console.ForegroundColor = GetChangeTypeColor(change.ChangeType);
            Console.Write($"{change.ChangeType,-10}");
            Console.ResetColor();
            Console.Write($" {change.VariableName,-20}");

            if (change.ChangeType == TwinCatQA.Domain.Models.ChangeType.Modified)
            {
                Console.Write($" {change.OldAddress} → {change.NewAddress}");
            }
            else if (change.ChangeType == TwinCatQA.Domain.Models.ChangeType.Added)
            {
                Console.Write($" ({change.NewAddress})");
            }
            else
            {
                Console.Write($" ({change.OldAddress})");
            }

            Console.WriteLine();
        }

        if (changes.Count > 10)
        {
            Console.WriteLine($"  ... 외 {changes.Count - 10}개");
        }

        Console.WriteLine();
    }

    private static void PrintLogicChanges(List<TwinCatQA.Domain.Models.LogicChange> changes)
    {
        Console.WriteLine("─────────────────────────────────────────────");
        Console.WriteLine("??  로직 변경 내역");
        Console.WriteLine("─────────────────────────────────────────────");

        foreach (var change in changes.Take(10))
        {
            Console.Write($"  {GetChangeTypeSymbol(change.ChangeType)} ");
            Console.ForegroundColor = GetChangeTypeColor(change.ChangeType);
            Console.Write($"{change.ChangeType,-10}");
            Console.ResetColor();
            Console.WriteLine($" {change.Name}");
            if (!string.IsNullOrEmpty(change.Description))
            {
                Console.WriteLine($"     {change.Description}");
            }
        }

        if (changes.Count > 10)
        {
            Console.WriteLine($"  ... 외 {changes.Count - 10}개");
        }

        Console.WriteLine();
    }

    private static void PrintDataTypeChanges(List<TwinCatQA.Domain.Models.DataTypeChange> changes)
    {
        Console.WriteLine("─────────────────────────────────────────────");
        Console.WriteLine("?? 데이터 타입 변경 내역");
        Console.WriteLine("─────────────────────────────────────────────");

        foreach (var change in changes.Take(10))
        {
            Console.Write($"  {GetChangeTypeSymbol(change.ChangeType)} ");
            Console.ForegroundColor = GetChangeTypeColor(change.ChangeType);
            Console.Write($"{change.ChangeType,-10}");
            Console.ResetColor();
            Console.WriteLine($" {change.TypeName}");
            if (!string.IsNullOrEmpty(change.Description))
            {
                Console.WriteLine($"     {change.Description}");
            }
        }

        if (changes.Count > 10)
        {
            Console.WriteLine($"  ... 외 {changes.Count - 10}개");
        }

        Console.WriteLine();
    }

    private static string GetChangeTypeSymbol(TwinCatQA.Domain.Models.ChangeType changeType)
    {
        return changeType switch
        {
            TwinCatQA.Domain.Models.ChangeType.Added => "?",
            TwinCatQA.Domain.Models.ChangeType.Removed => "?",
            TwinCatQA.Domain.Models.ChangeType.Modified => "??",
            _ => "?"
        };
    }

    private static ConsoleColor GetChangeTypeColor(TwinCatQA.Domain.Models.ChangeType changeType)
    {
        return changeType switch
        {
            TwinCatQA.Domain.Models.ChangeType.Added => ConsoleColor.Green,
            TwinCatQA.Domain.Models.ChangeType.Removed => ConsoleColor.Red,
            TwinCatQA.Domain.Models.ChangeType.Modified => ConsoleColor.Yellow,
            _ => ConsoleColor.White
        };
    }

    private static async Task SaveResultToFileAsync(TwinCatQA.Domain.Models.FolderComparisonResult result, string outputPath)
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(outputPath, json);

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"?? 결과가 파일로 저장되었습니다: {outputPath}");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"? 파일 저장 실패: {ex.Message}");
            Console.ResetColor();
        }
    }
}
