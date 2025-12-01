namespace TwinCatQA.Domain.Models;

/// <summary>
/// 순환 복잡도 분석 결과
/// </summary>
public class CyclomaticComplexityResult
{
    /// <summary>POU 이름</summary>
    public string PouName { get; init; } = string.Empty;

    /// <summary>파일 경로</summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>순환 복잡도 값</summary>
    public int Complexity { get; init; }

    /// <summary>등급 (A-F)</summary>
    public ComplexityGrade Grade => Complexity switch
    {
        <= 5 => ComplexityGrade.A,   // 단순
        <= 10 => ComplexityGrade.B,  // 보통
        <= 20 => ComplexityGrade.C,  // 복잡
        <= 30 => ComplexityGrade.D,  // 매우 복잡
        _ => ComplexityGrade.F       // 위험
    };

    /// <summary>분기문 개수</summary>
    public int IfCount { get; init; }

    /// <summary>CASE 분기 개수</summary>
    public int CaseCount { get; init; }

    /// <summary>반복문 개수</summary>
    public int LoopCount { get; init; }

    /// <summary>AND/OR 논리 연산자 개수</summary>
    public int LogicalOperatorCount { get; init; }
}

/// <summary>
/// 복잡도 등급
/// </summary>
public enum ComplexityGrade
{
    /// <summary>단순 (1-5)</summary>
    A,
    /// <summary>보통 (6-10)</summary>
    B,
    /// <summary>복잡 (11-20)</summary>
    C,
    /// <summary>매우 복잡 (21-30)</summary>
    D,
    /// <summary>위험 (31+)</summary>
    F
}

/// <summary>
/// 데드 코드 검출 결과
/// </summary>
public class DeadCodeResult
{
    /// <summary>검출 타입</summary>
    public DeadCodeType Type { get; init; }

    /// <summary>파일 경로</summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>시작 라인</summary>
    public int StartLine { get; init; }

    /// <summary>종료 라인</summary>
    public int EndLine { get; init; }

    /// <summary>코드 스니펫</summary>
    public string CodeSnippet { get; init; } = string.Empty;

    /// <summary>심각도</summary>
    public IssueSeverity Severity { get; init; }

    /// <summary>설명</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>관련 조건문 (있는 경우)</summary>
    public string? Condition { get; init; }
}

/// <summary>
/// 배열 경계 검사 결과
/// </summary>
public class ArrayBoundsResult
{
    /// <summary>배열 변수명</summary>
    public string ArrayName { get; init; } = string.Empty;

    /// <summary>파일 경로</summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>라인 번호</summary>
    public int Line { get; init; }

    /// <summary>배열 선언 크기</summary>
    public int DeclaredSize { get; init; }

    /// <summary>배열 시작 인덱스</summary>
    public int LowerBound { get; init; }

    /// <summary>배열 종료 인덱스</summary>
    public int UpperBound { get; init; }

    /// <summary>접근 인덱스 (상수인 경우)</summary>
    public int? AccessIndex { get; init; }

    /// <summary>접근 표현식 (변수인 경우)</summary>
    public string? AccessExpression { get; init; }

    /// <summary>위반 타입</summary>
    public ArrayBoundsViolationType ViolationType { get; init; }

    /// <summary>심각도</summary>
    public IssueSeverity Severity { get; init; }

    /// <summary>설명</summary>
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// 배열 경계 위반 타입
/// </summary>
public enum ArrayBoundsViolationType
{
    /// <summary>상수 인덱스가 범위 초과</summary>
    ConstantOutOfBounds,

    /// <summary>FOR 루프 인덱스가 범위 초과 가능</summary>
    LoopIndexOutOfBounds,

    /// <summary>음수 인덱스 사용</summary>
    NegativeIndex,

    /// <summary>동적 인덱스 검증 없음</summary>
    UncheckedDynamicIndex
}

/// <summary>
/// 매직 넘버 검출 결과
/// </summary>
public class MagicNumberResult
{
    /// <summary>매직 넘버 값</summary>
    public string Value { get; init; } = string.Empty;

    /// <summary>파일 경로</summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>라인 번호</summary>
    public int Line { get; init; }

    /// <summary>사용 컨텍스트</summary>
    public string Context { get; init; } = string.Empty;

    /// <summary>권장 상수명</summary>
    public string? SuggestedConstantName { get; init; }

    /// <summary>심각도 (0, 1은 제외)</summary>
    public IssueSeverity Severity { get; init; }

    /// <summary>설명</summary>
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// 타이밍 분석 결과
/// </summary>
public class TimingAnalysisResult
{
    /// <summary>POU 이름</summary>
    public string PouName { get; init; } = string.Empty;

    /// <summary>파일 경로</summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>잠재적 무한 루프 위치</summary>
    public List<PotentialInfiniteLoop> InfiniteLoops { get; init; } = new();

    /// <summary>중첩된 타이머</summary>
    public List<NestedTimer> NestedTimers { get; init; } = new();

    /// <summary>WAIT 문 사용</summary>
    public List<WaitStatement> WaitStatements { get; init; } = new();
}

/// <summary>
/// 잠재적 무한 루프
/// </summary>
public class PotentialInfiniteLoop
{
    /// <summary>라인 번호</summary>
    public int Line { get; init; }

    /// <summary>루프 타입 (WHILE, REPEAT)</summary>
    public string LoopType { get; init; } = string.Empty;

    /// <summary>종료 조건</summary>
    public string Condition { get; init; } = string.Empty;

    /// <summary>위험 사유</summary>
    public string Reason { get; init; } = string.Empty;
}

/// <summary>
/// 중첩 타이머
/// </summary>
public class NestedTimer
{
    /// <summary>라인 번호</summary>
    public int Line { get; init; }

    /// <summary>타이머 타입 (TON, TOF, TP)</summary>
    public string TimerType { get; init; } = string.Empty;

    /// <summary>타이머 변수명</summary>
    public string TimerName { get; init; } = string.Empty;

    /// <summary>중첩 깊이</summary>
    public int NestingLevel { get; init; }
}

/// <summary>
/// WAIT 문
/// </summary>
public class WaitStatement
{
    /// <summary>라인 번호</summary>
    public int Line { get; init; }

    /// <summary>대기 시간 (ms)</summary>
    public int? WaitTimeMs { get; init; }

    /// <summary>심각도 (PLC 사이클 차단 위험)</summary>
    public IssueSeverity Severity { get; init; }
}

/// <summary>
/// 심화 분석 종합 결과
/// </summary>
public class AdvancedAnalysisResult
{
    /// <summary>순환 복잡도 결과</summary>
    public List<CyclomaticComplexityResult> ComplexityResults { get; init; } = new();

    /// <summary>데드 코드 결과</summary>
    public List<DeadCodeResult> DeadCodeResults { get; init; } = new();

    /// <summary>배열 경계 결과</summary>
    public List<ArrayBoundsResult> ArrayBoundsResults { get; init; } = new();

    /// <summary>매직 넘버 결과</summary>
    public List<MagicNumberResult> MagicNumberResults { get; init; } = new();

    /// <summary>메모리 영역 분석</summary>
    public MemoryRegionAnalysis? MemoryAnalysis { get; init; }

    /// <summary>타이밍 분석 결과</summary>
    public List<TimingAnalysisResult> TimingResults { get; init; } = new();

    /// <summary>총 이슈 수</summary>
    public int TotalIssues =>
        ComplexityResults.Count(c => c.Grade >= ComplexityGrade.D) +
        DeadCodeResults.Count +
        ArrayBoundsResults.Count +
        MagicNumberResults.Count +
        (MemoryAnalysis?.TotalIssues ?? 0) +
        TimingResults.Sum(t => t.InfiniteLoops.Count + t.NestedTimers.Count);
}
