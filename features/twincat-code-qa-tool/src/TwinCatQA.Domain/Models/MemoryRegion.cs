namespace TwinCatQA.Domain.Models;

/// <summary>
/// 메모리 영역 타입
/// </summary>
public enum MemoryRegionType
{
    /// <summary>입력 영역 (%I)</summary>
    Input,

    /// <summary>출력 영역 (%Q)</summary>
    Output,

    /// <summary>메모리 영역 (%M)</summary>
    Memory,

    /// <summary>알 수 없음</summary>
    Unknown
}

/// <summary>
/// 메모리 데이터 크기
/// </summary>
public enum MemorySize
{
    /// <summary>비트 (X) - 1비트</summary>
    Bit,

    /// <summary>바이트 (B) - 8비트</summary>
    Byte,

    /// <summary>워드 (W) - 16비트</summary>
    Word,

    /// <summary>더블워드 (D) - 32비트</summary>
    DWord,

    /// <summary>롱워드 (L) - 64비트</summary>
    LWord
}

/// <summary>
/// 메모리 할당 정보
/// </summary>
public class MemoryAllocation
{
    /// <summary>변수 이름</summary>
    public string VariableName { get; init; } = string.Empty;

    /// <summary>파일 경로</summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>라인 번호</summary>
    public int Line { get; init; }

    /// <summary>메모리 영역 타입</summary>
    public MemoryRegionType RegionType { get; init; }

    /// <summary>메모리 크기</summary>
    public MemorySize Size { get; init; }

    /// <summary>시작 주소 (바이트)</summary>
    public int StartAddress { get; init; }

    /// <summary>비트 오프셋 (0-7, 비트 주소인 경우)</summary>
    public int BitOffset { get; init; }

    /// <summary>종료 주소 (바이트)</summary>
    public int EndAddress => StartAddress + GetByteSize();

    /// <summary>원본 주소 문자열 (예: %IX0.0)</summary>
    public string RawAddress { get; init; } = string.Empty;

    /// <summary>데이터 타입</summary>
    public string DataType { get; init; } = string.Empty;

    /// <summary>바이트 크기 반환</summary>
    private int GetByteSize() => Size switch
    {
        MemorySize.Bit => 0,  // 비트는 바이트 내에 존재
        MemorySize.Byte => 1,
        MemorySize.Word => 2,
        MemorySize.DWord => 4,
        MemorySize.LWord => 8,
        _ => 0
    };
}

/// <summary>
/// 메모리 충돌 정보
/// </summary>
public class MemoryConflict
{
    /// <summary>충돌 타입</summary>
    public MemoryConflictType ConflictType { get; init; }

    /// <summary>첫 번째 할당</summary>
    public MemoryAllocation First { get; init; } = null!;

    /// <summary>두 번째 할당</summary>
    public MemoryAllocation Second { get; init; } = null!;

    /// <summary>심각도</summary>
    public IssueSeverity Severity { get; init; }

    /// <summary>설명 메시지</summary>
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// 메모리 충돌 타입
/// </summary>
public enum MemoryConflictType
{
    /// <summary>동일 주소 중복 사용</summary>
    Overlap,

    /// <summary>부분 중첩 (워드/바이트 영역이 겹침)</summary>
    PartialOverlap,

    /// <summary>비트와 바이트/워드 혼용</summary>
    MixedAccess,

    /// <summary>연속 영역 갭 (비효율적 사용)</summary>
    FragmentedAllocation,

    /// <summary>타입 불일치</summary>
    TypeMismatch
}

/// <summary>
/// 메모리 영역 사용 통계
/// </summary>
public class MemoryUsageStatistics
{
    /// <summary>영역 타입</summary>
    public MemoryRegionType RegionType { get; init; }

    /// <summary>총 할당 변수 수</summary>
    public int TotalAllocations { get; init; }

    /// <summary>사용된 바이트 수</summary>
    public int UsedBytes { get; init; }

    /// <summary>사용된 비트 수</summary>
    public int UsedBits { get; init; }

    /// <summary>최소 주소</summary>
    public int MinAddress { get; init; }

    /// <summary>최대 주소</summary>
    public int MaxAddress { get; init; }

    /// <summary>단편화율 (0-100%)</summary>
    public double FragmentationRate { get; init; }

    /// <summary>갭 목록 (비어있는 영역)</summary>
    public List<(int Start, int End)> Gaps { get; init; } = new();
}

/// <summary>
/// 메모리 영역 분석 결과
/// </summary>
public class MemoryRegionAnalysis
{
    /// <summary>모든 메모리 할당</summary>
    public List<MemoryAllocation> Allocations { get; init; } = new();

    /// <summary>메모리 충돌 목록</summary>
    public List<MemoryConflict> Conflicts { get; init; } = new();

    /// <summary>영역별 사용 통계</summary>
    public List<MemoryUsageStatistics> Statistics { get; init; } = new();

    /// <summary>총 이슈 수</summary>
    public int TotalIssues => Conflicts.Count;

    /// <summary>Critical 이슈 수</summary>
    public int CriticalIssues => Conflicts.Count(c => c.Severity == IssueSeverity.Critical);

    /// <summary>분석 성공 여부</summary>
    public bool IsSuccess => CriticalIssues == 0;
}
