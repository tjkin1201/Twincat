using System.Xml.Linq;
using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Services;

namespace TwinCatQA.Infrastructure.Comparison;

/// <summary>
/// 폴더 비교 통합 오케스트레이터 구현
/// </summary>
public class FolderComparer : IFolderComparer
{
    private readonly IVariableComparer _variableComparer;
    private readonly IIOMappingComparer _ioMappingComparer;
    private readonly ILogicComparer _logicComparer;
    private readonly IDataTypeComparer _dataTypeComparer;

    // TwinCAT 파일 확장자
    private static readonly string[] TwinCatExtensions = { ".TcPOU", ".TcGVL", ".TcDUT", ".TcIO" };

    public FolderComparer(
        IVariableComparer variableComparer,
        IIOMappingComparer ioMappingComparer,
        ILogicComparer logicComparer,
        IDataTypeComparer dataTypeComparer)
    {
        _variableComparer = variableComparer;
        _ioMappingComparer = ioMappingComparer;
        _logicComparer = logicComparer;
        _dataTypeComparer = dataTypeComparer;
    }

    /// <summary>
    /// 기본 생성자 (DI 없이 사용 시)
    /// </summary>
    public FolderComparer()
    {
        _variableComparer = new VariableComparer();
        _ioMappingComparer = new IOMappingComparer();
        _logicComparer = new LogicComparer();
        _dataTypeComparer = new DataTypeComparer();
    }

    public async Task<FolderComparisonResult> CompareAsync(
        string oldFolderPath,
        string newFolderPath,
        CompareOptions? options = null)
    {
        options ??= CompareOptions.Default;

        // 파일 로드
        var oldFiles = await LoadFilesAsync(oldFolderPath, options);
        var newFiles = await LoadFilesAsync(newFolderPath, options);

        var result = new FolderComparisonResult
        {
            OldFolderPath = oldFolderPath,
            NewFolderPath = newFolderPath
        };

        // 병렬 비교 실행
        var tasks = new List<Task>();

        if (options.IncludeVariables)
        {
            tasks.Add(Task.Run(() =>
            {
                var changes = _variableComparer.Compare(oldFiles, newFiles);
                lock (result.VariableChanges)
                {
                    result.VariableChanges.AddRange(changes);
                }
            }));
        }

        if (options.IncludeIOMapping)
        {
            tasks.Add(Task.Run(() =>
            {
                var changes = _ioMappingComparer.Compare(oldFiles, newFiles);
                lock (result.IOMappingChanges)
                {
                    result.IOMappingChanges.AddRange(changes);
                }
            }));
        }

        if (options.IncludeLogic)
        {
            tasks.Add(Task.Run(() =>
            {
                var changes = _logicComparer.Compare(oldFiles, newFiles);
                lock (result.LogicChanges)
                {
                    result.LogicChanges.AddRange(changes);
                }
            }));
        }

        if (options.IncludeDataTypes)
        {
            tasks.Add(Task.Run(() =>
            {
                var changes = _dataTypeComparer.Compare(oldFiles, newFiles);
                lock (result.DataTypeChanges)
                {
                    result.DataTypeChanges.AddRange(changes);
                }
            }));
        }

        await Task.WhenAll(tasks);

        return result;
    }

    /// <summary>
    /// 폴더에서 TwinCAT 파일 로드
    /// </summary>
    private async Task<List<CodeFile>> LoadFilesAsync(string folderPath, CompareOptions options)
    {
        var files = new List<CodeFile>();

        if (!Directory.Exists(folderPath))
        {
            throw new DirectoryNotFoundException($"폴더를 찾을 수 없습니다: {folderPath}");
        }

        var patterns = options.FilePatterns ?? TwinCatExtensions.Select(e => $"*{e}").ToArray();

        foreach (var pattern in patterns)
        {
            var matchedFiles = Directory.GetFiles(folderPath, pattern, SearchOption.AllDirectories);

            foreach (var filePath in matchedFiles)
            {
                // 제외 패턴 확인
                if (options.ExcludePatterns != null)
                {
                    bool excluded = options.ExcludePatterns.Any(p =>
                        filePath.Contains(p, StringComparison.OrdinalIgnoreCase));
                    if (excluded) continue;
                }

                var rawContent = await File.ReadAllTextAsync(filePath);

                // TwinCAT XML 파일에서 ST 코드 추출
                var content = ExtractStructuredTextFromXml(rawContent, filePath);
                var lineCount = content.Split('\n').Length;

                files.Add(new CodeFile
                {
                    FilePath = filePath,
                    Content = content,
                    LineCount = lineCount,
                    Type = DetermineFileType(filePath),
                    Language = ProgrammingLanguage.ST,
                    LastModified = File.GetLastWriteTimeUtc(filePath)
                });
            }
        }

        return files;
    }

    /// <summary>
    /// TwinCAT XML 파일에서 ST 코드 추출 (CDATA 섹션에서)
    /// </summary>
    /// <param name="xmlContent">XML 파일 내용</param>
    /// <param name="filePath">파일 경로 (오류 메시지용)</param>
    /// <returns>추출된 ST 코드</returns>
    private string ExtractStructuredTextFromXml(string xmlContent, string filePath)
    {
        // XML이 아닌 경우 원본 반환
        if (string.IsNullOrWhiteSpace(xmlContent) || !xmlContent.TrimStart().StartsWith("<?xml"))
        {
            return xmlContent;
        }

        try
        {
            var doc = XDocument.Parse(xmlContent);
            var extractedParts = new List<string>();

            // Declaration 섹션 추출 (VAR 블록 등)
            var declarations = doc.Descendants("Declaration")
                .Select(e => e.Value)
                .Where(v => !string.IsNullOrWhiteSpace(v));
            extractedParts.AddRange(declarations);

            // Implementation/ST 섹션 추출 (로직 코드)
            var stCodes = doc.Descendants("ST")
                .Select(e => e.Value)
                .Where(v => !string.IsNullOrWhiteSpace(v));
            extractedParts.AddRange(stCodes);

            // 추출된 내용이 있으면 결합하여 반환
            if (extractedParts.Any())
            {
                return string.Join("\n\n", extractedParts);
            }

            // 추출된 내용이 없으면 원본 반환 (비 TwinCAT XML 파일)
            return xmlContent;
        }
        catch (Exception)
        {
            // XML 파싱 실패 시 원본 반환 (순수 ST 코드일 수 있음)
            return xmlContent;
        }
    }

    private FileType DetermineFileType(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLower();
        return ext switch
        {
            ".tcpou" => FileType.POU,
            ".tcgvl" => FileType.GVL,
            ".tcdut" => FileType.DUT,
            ".tcio" => FileType.POU, // I/O 설정
            _ => FileType.POU
        };
    }
}

/// <summary>
/// 비교 결과 포맷터
/// </summary>
public static class ComparisonResultFormatter
{
    /// <summary>
    /// 비교 결과를 콘솔 출력용 문자열로 변환
    /// </summary>
    public static string FormatAsText(FolderComparisonResult result)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine($"📊 비교 결과: {Path.GetFileName(result.OldFolderPath)} vs {Path.GetFileName(result.NewFolderPath)}");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine();

        // 요약
        sb.AppendLine($"📈 요약: 총 {result.TotalChanges}개 변경");
        sb.AppendLine($"   ➕ 추가: {result.AddedCount}개");
        sb.AppendLine($"   ✏️  변경: {result.ModifiedCount}개");
        sb.AppendLine($"   ➖ 삭제: {result.RemovedCount}개");
        sb.AppendLine();

        // 변수 변경
        if (result.VariableChanges.Any())
        {
            sb.AppendLine("──────────────────────────────────────────────────────────────");
            sb.AppendLine("📦 변수 변경");
            sb.AppendLine("──────────────────────────────────────────────────────────────");

            foreach (var change in result.VariableChanges.OrderBy(c => c.ChangeType))
            {
                var icon = GetChangeIcon(change.ChangeType);
                sb.AppendLine($"  {icon} {change.Description}");
                sb.AppendLine($"     📁 {Path.GetFileName(change.FilePath)}:{change.Line}");
            }
            sb.AppendLine();
        }

        // I/O 매핑 변경
        if (result.IOMappingChanges.Any())
        {
            sb.AppendLine("──────────────────────────────────────────────────────────────");
            sb.AppendLine("🔌 I/O 매핑 변경");
            sb.AppendLine("──────────────────────────────────────────────────────────────");

            foreach (var change in result.IOMappingChanges.OrderBy(c => c.ChangeType))
            {
                var icon = GetChangeIcon(change.ChangeType);
                sb.AppendLine($"  {icon} {change.Description}");
                sb.AppendLine($"     📁 {Path.GetFileName(change.FilePath)}:{change.Line}");
            }
            sb.AppendLine();
        }

        // 로직 변경
        if (result.LogicChanges.Any())
        {
            sb.AppendLine("──────────────────────────────────────────────────────────────");
            sb.AppendLine("⚙️  로직 변경");
            sb.AppendLine("──────────────────────────────────────────────────────────────");

            foreach (var change in result.LogicChanges.OrderBy(c => c.ChangeType))
            {
                var icon = GetChangeIcon(change.ChangeType);
                sb.AppendLine($"  {icon} {change.Description}");
                sb.AppendLine($"     📁 {Path.GetFileName(change.FilePath)}:{change.StartLine}-{change.EndLine}");
                if (!string.IsNullOrEmpty(change.Summary))
                    sb.AppendLine($"     💡 {change.Summary}");
            }
            sb.AppendLine();
        }

        // 데이터 타입 변경
        if (result.DataTypeChanges.Any())
        {
            sb.AppendLine("──────────────────────────────────────────────────────────────");
            sb.AppendLine("📐 데이터 타입 변경");
            sb.AppendLine("──────────────────────────────────────────────────────────────");

            foreach (var change in result.DataTypeChanges.OrderBy(c => c.ChangeType))
            {
                var icon = GetChangeIcon(change.ChangeType);
                sb.AppendLine($"  {icon} {change.Description}");
                sb.AppendLine($"     📁 {Path.GetFileName(change.FilePath)}:{change.Line}");

                if (change.FieldChanges.Any())
                {
                    foreach (var field in change.FieldChanges)
                    {
                        var fieldIcon = GetChangeIcon(field.ChangeType);
                        sb.AppendLine($"       {fieldIcon} 필드: {field.FieldName}");
                    }
                }

                if (change.EnumChanges.Any())
                {
                    foreach (var enumVal in change.EnumChanges)
                    {
                        var enumIcon = GetChangeIcon(enumVal.ChangeType);
                        sb.AppendLine($"       {enumIcon} 값: {enumVal.ValueName}");
                    }
                }
            }
            sb.AppendLine();
        }

        if (!result.HasChanges)
        {
            sb.AppendLine("✅ 변경 사항 없음");
        }

        sb.AppendLine("═══════════════════════════════════════════════════════════════");

        return sb.ToString();
    }

    private static string GetChangeIcon(ChangeType type) => type switch
    {
        ChangeType.Added => "➕",
        ChangeType.Removed => "➖",
        ChangeType.Modified => "✏️ ",
        ChangeType.Moved => "🔄",
        _ => "•"
    };
}
