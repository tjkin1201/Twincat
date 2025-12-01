namespace TwinCatQA.Domain.Models;

/// <summary>
/// 변경 타입
/// </summary>
public enum ChangeType
{
    /// <summary>추가됨</summary>
    Added,

    /// <summary>삭제됨</summary>
    Removed,

    /// <summary>수정됨</summary>
    Modified,

    /// <summary>이동됨 (위치 변경)</summary>
    Moved
}

/// <summary>
/// 비교 항목 카테고리
/// </summary>
public enum CompareCategory
{
    /// <summary>변수</summary>
    Variable,

    /// <summary>I/O 매핑</summary>
    IOMapping,

    /// <summary>로직 (FB 본문)</summary>
    Logic,

    /// <summary>데이터 타입 (구조체, 열거형)</summary>
    DataType,

    /// <summary>Function Block 정의</summary>
    FunctionBlock
}

#region 변수 비교

/// <summary>
/// 변수 변경 항목
/// </summary>
public class VariableChange
{
    public ChangeType ChangeType { get; init; }
    public string VariableName { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
    public int Line { get; init; }

    /// <summary>이전 데이터 타입</summary>
    public string? OldDataType { get; init; }

    /// <summary>새 데이터 타입</summary>
    public string? NewDataType { get; init; }

    /// <summary>이전 초기값</summary>
    public string? OldInitialValue { get; init; }

    /// <summary>새 초기값</summary>
    public string? NewInitialValue { get; init; }

    /// <summary>변수 범위 (VAR, VAR_GLOBAL, VAR_INPUT 등)</summary>
    public string Scope { get; init; } = string.Empty;

    public string Description => ChangeType switch
    {
        ChangeType.Added => $"추가: {VariableName} : {NewDataType}",
        ChangeType.Removed => $"삭제: {VariableName} : {OldDataType}",
        ChangeType.Modified => $"변경: {VariableName} ({OldDataType} → {NewDataType})",
        _ => $"{VariableName}"
    };
}

#endregion

#region I/O 매핑 비교

/// <summary>
/// I/O 매핑 변경 항목
/// </summary>
public class IOMappingChange
{
    public ChangeType ChangeType { get; init; }
    public string VariableName { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
    public int Line { get; init; }

    /// <summary>이전 I/O 주소</summary>
    public string? OldAddress { get; init; }

    /// <summary>새 I/O 주소</summary>
    public string? NewAddress { get; init; }

    /// <summary>데이터 타입</summary>
    public string DataType { get; init; } = string.Empty;

    public string Description => ChangeType switch
    {
        ChangeType.Added => $"추가: {VariableName} AT {NewAddress} : {DataType}",
        ChangeType.Removed => $"삭제: {VariableName} AT {OldAddress} : {DataType}",
        ChangeType.Modified => $"변경: {VariableName} ({OldAddress} → {NewAddress})",
        _ => $"{VariableName}"
    };
}

#endregion

#region 로직 비교

/// <summary>
/// 검토 의견 심각도
/// </summary>
public enum ReviewSeverity
{
    /// <summary>정보성 (참고)</summary>
    Info,

    /// <summary>주의 필요</summary>
    Warning,

    /// <summary>위험 - 반드시 검토 필요</summary>
    Critical
}

/// <summary>
/// 검토 의견 카테고리
/// </summary>
public enum ReviewCategory
{
    /// <summary>안전 관련</summary>
    Safety,

    /// <summary>타이밍/성능</summary>
    Timing,

    /// <summary>로직 변경</summary>
    Logic,

    /// <summary>변수/파라미터</summary>
    Variable,

    /// <summary>제어 흐름</summary>
    ControlFlow,

    /// <summary>I/O 관련</summary>
    IO,

    /// <summary>일반</summary>
    General
}

/// <summary>
/// 자동 생성된 검토 의견
/// </summary>
public class ReviewComment
{
    /// <summary>심각도</summary>
    public ReviewSeverity Severity { get; init; }

    /// <summary>카테고리</summary>
    public ReviewCategory Category { get; init; }

    /// <summary>제목 (한 줄 요약)</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>상세 설명</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>영향 범위</summary>
    public string Impact { get; init; } = string.Empty;

    /// <summary>권장 조치</summary>
    public string Recommendation { get; init; } = string.Empty;

    /// <summary>관련 라인 번호</summary>
    public int? LineNumber { get; init; }

    /// <summary>관련 코드 스니펫</summary>
    public string? CodeSnippet { get; init; }
}

/// <summary>
/// 로직 변경 항목
/// </summary>
public class LogicChange
{
    public ChangeType ChangeType { get; init; }
    public string ElementName { get; init; } = string.Empty;

    /// <summary>함수/FB 이름 (ElementName과 동일, UI 호환성)</summary>
    public string Name => ElementName;
    public string FunctionName => ElementName;

    public string FilePath { get; init; } = string.Empty;
    public int StartLine { get; init; }
    public int EndLine { get; init; }

    /// <summary>이전 코드 (일부)</summary>
    public string? OldCode { get; init; }

    /// <summary>새 코드 (일부)</summary>
    public string? NewCode { get; init; }

    /// <summary>변경된 라인 수</summary>
    public int ChangedLineCount { get; init; }

    /// <summary>변경 요약</summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>자동 생성된 검토 의견 목록</summary>
    public List<ReviewComment> ReviewComments { get; init; } = new();

    /// <summary>검토 의견 개수</summary>
    public int ReviewCount => ReviewComments.Count;

    /// <summary>Critical 검토 의견 개수</summary>
    public int CriticalCount => ReviewComments.Count(r => r.Severity == ReviewSeverity.Critical);

    /// <summary>Warning 검토 의견 개수</summary>
    public int WarningCount => ReviewComments.Count(r => r.Severity == ReviewSeverity.Warning);

    public string Description => ChangeType switch
    {
        ChangeType.Added => $"추가: {ElementName} ({ChangedLineCount}줄)",
        ChangeType.Removed => $"삭제: {ElementName}",
        ChangeType.Modified => $"변경: {ElementName} (라인 {StartLine}-{EndLine}, {ChangedLineCount}줄 변경)",
        _ => $"{ElementName}"
    };
}

#endregion

#region 데이터 타입 비교

/// <summary>
/// 데이터 타입 변경 항목 (구조체, 열거형)
/// </summary>
public class DataTypeChange
{
    public ChangeType ChangeType { get; init; }
    public string TypeName { get; init; } = string.Empty;
    public DataTypeKind Kind { get; init; }
    public string FilePath { get; init; } = string.Empty;
    public int Line { get; init; }

    /// <summary>이전 정의 (전체 코드)</summary>
    public string? OldDefinition { get; init; }

    /// <summary>새 정의 (전체 코드)</summary>
    public string? NewDefinition { get; init; }

    /// <summary>변경된 필드 목록 (구조체)</summary>
    public List<FieldChange> FieldChanges { get; init; } = new();

    /// <summary>변경된 열거형 값 목록</summary>
    public List<EnumValueChange> EnumChanges { get; init; } = new();

    public string Description => ChangeType switch
    {
        ChangeType.Added => $"추가: {Kind} {TypeName}",
        ChangeType.Removed => $"삭제: {Kind} {TypeName}",
        ChangeType.Modified => $"변경: {Kind} {TypeName} ({FieldChanges.Count + EnumChanges.Count}개 항목 변경)",
        _ => $"{TypeName}"
    };
}

/// <summary>
/// 구조체 필드 변경
/// </summary>
public class FieldChange
{
    public ChangeType ChangeType { get; init; }
    public string FieldName { get; init; } = string.Empty;
    public string? OldDataType { get; init; }
    public string? NewDataType { get; init; }
}

/// <summary>
/// 열거형 값 변경
/// </summary>
public class EnumValueChange
{
    public ChangeType ChangeType { get; init; }
    public string ValueName { get; init; } = string.Empty;
    public int? OldValue { get; init; }
    public int? NewValue { get; init; }
}

#endregion

#region 종합 비교 결과

/// <summary>
/// 폴더 비교 종합 결과
/// </summary>
public class FolderComparisonResult
{
    /// <summary>비교 시작 시각</summary>
    public DateTime ComparedAt { get; init; } = DateTime.UtcNow;

    /// <summary>이전 폴더 경로</summary>
    public string OldFolderPath { get; init; } = string.Empty;

    /// <summary>새 폴더 경로</summary>
    public string NewFolderPath { get; init; } = string.Empty;

    /// <summary>변수 변경 목록</summary>
    public List<VariableChange> VariableChanges { get; init; } = new();

    /// <summary>I/O 매핑 변경 목록</summary>
    public List<IOMappingChange> IOMappingChanges { get; init; } = new();

    /// <summary>로직 변경 목록</summary>
    public List<LogicChange> LogicChanges { get; init; } = new();

    /// <summary>데이터 타입 변경 목록</summary>
    public List<DataTypeChange> DataTypeChanges { get; init; } = new();

    #region 통계

    public int TotalChanges =>
        VariableChanges.Count +
        IOMappingChanges.Count +
        LogicChanges.Count +
        DataTypeChanges.Count;

    public int AddedCount =>
        VariableChanges.Count(c => c.ChangeType == ChangeType.Added) +
        IOMappingChanges.Count(c => c.ChangeType == ChangeType.Added) +
        LogicChanges.Count(c => c.ChangeType == ChangeType.Added) +
        DataTypeChanges.Count(c => c.ChangeType == ChangeType.Added);

    public int RemovedCount =>
        VariableChanges.Count(c => c.ChangeType == ChangeType.Removed) +
        IOMappingChanges.Count(c => c.ChangeType == ChangeType.Removed) +
        LogicChanges.Count(c => c.ChangeType == ChangeType.Removed) +
        DataTypeChanges.Count(c => c.ChangeType == ChangeType.Removed);

    public int ModifiedCount =>
        VariableChanges.Count(c => c.ChangeType == ChangeType.Modified) +
        IOMappingChanges.Count(c => c.ChangeType == ChangeType.Modified) +
        LogicChanges.Count(c => c.ChangeType == ChangeType.Modified) +
        DataTypeChanges.Count(c => c.ChangeType == ChangeType.Modified);

    #endregion

    /// <summary>
    /// 변경 사항이 있는지 여부
    /// </summary>
    public bool HasChanges => TotalChanges > 0;
}

#endregion
