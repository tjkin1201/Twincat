using TwinCatQA.Domain.Models;

namespace TwinCatQA.Domain.Services;

/// <summary>
/// 변수 비교 분석기 인터페이스
/// </summary>
public interface IVariableComparer
{
    List<VariableChange> Compare(List<CodeFile> oldFiles, List<CodeFile> newFiles);
}

/// <summary>
/// I/O 매핑 비교 분석기 인터페이스
/// </summary>
public interface IIOMappingComparer
{
    List<IOMappingChange> Compare(List<CodeFile> oldFiles, List<CodeFile> newFiles);
}

/// <summary>
/// 로직 비교 분석기 인터페이스
/// </summary>
public interface ILogicComparer
{
    List<LogicChange> Compare(List<CodeFile> oldFiles, List<CodeFile> newFiles);
}

/// <summary>
/// 데이터 타입 비교 분석기 인터페이스
/// </summary>
public interface IDataTypeComparer
{
    List<DataTypeChange> Compare(List<CodeFile> oldFiles, List<CodeFile> newFiles);
}

/// <summary>
/// 폴더 비교 통합 인터페이스
/// </summary>
public interface IFolderComparer
{
    /// <summary>
    /// 두 폴더를 비교하여 변경 사항을 분석
    /// </summary>
    /// <param name="oldFolderPath">이전 폴더 경로</param>
    /// <param name="newFolderPath">새 폴더 경로</param>
    /// <param name="options">비교 옵션</param>
    /// <returns>비교 결과</returns>
    Task<FolderComparisonResult> CompareAsync(string oldFolderPath, string newFolderPath, CompareOptions? options = null);
}

/// <summary>
/// 비교 옵션
/// </summary>
public class CompareOptions
{
    /// <summary>변수 비교 포함</summary>
    public bool IncludeVariables { get; init; } = true;

    /// <summary>I/O 매핑 비교 포함</summary>
    public bool IncludeIOMapping { get; init; } = true;

    /// <summary>로직 비교 포함</summary>
    public bool IncludeLogic { get; init; } = true;

    /// <summary>데이터 타입 비교 포함</summary>
    public bool IncludeDataTypes { get; init; } = true;

    /// <summary>파일 패턴 필터 (예: *.TcPOU)</summary>
    public string[]? FilePatterns { get; init; }

    /// <summary>제외할 파일 패턴</summary>
    public string[]? ExcludePatterns { get; init; }

    /// <summary>기본 옵션 (전체 비교)</summary>
    public static CompareOptions Default => new();

    /// <summary>변수만 비교</summary>
    public static CompareOptions VariablesOnly => new()
    {
        IncludeVariables = true,
        IncludeIOMapping = false,
        IncludeLogic = false,
        IncludeDataTypes = false
    };

    /// <summary>I/O 매핑만 비교</summary>
    public static CompareOptions IOMappingOnly => new()
    {
        IncludeVariables = false,
        IncludeIOMapping = true,
        IncludeLogic = false,
        IncludeDataTypes = false
    };

    /// <summary>로직만 비교</summary>
    public static CompareOptions LogicOnly => new()
    {
        IncludeVariables = false,
        IncludeIOMapping = false,
        IncludeLogic = true,
        IncludeDataTypes = false
    };
}
