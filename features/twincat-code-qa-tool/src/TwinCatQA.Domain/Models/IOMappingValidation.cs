namespace TwinCatQA.Domain.Models;

/// <summary>
/// I/O 매핑 검증 결과
/// </summary>
public class IOMappingValidationResult
{
    /// <summary>
    /// 검증 성공 여부
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// I/O 매핑 오류 목록
    /// </summary>
    public List<IOMappingError> Errors { get; init; } = new();

    /// <summary>
    /// I/O 매핑 경고 목록
    /// </summary>
    public List<IOMappingWarning> Warnings { get; init; } = new();

    /// <summary>
    /// I/O 디바이스 목록
    /// </summary>
    public List<IODevice> Devices { get; init; } = new();

    /// <summary>
    /// EtherCAT 마스터 정보
    /// </summary>
    public EtherCATMaster? EtherCATMaster { get; init; }

    /// <summary>
    /// 프로젝트 경로
    /// </summary>
    public string ProjectPath { get; init; } = string.Empty;

    /// <summary>
    /// 검증 시간
    /// </summary>
    public DateTime ValidationTime { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// 총 I/O 포인트 수
    /// </summary>
    public int TotalIOPoints => Devices.Sum(d => d.InputCount + d.OutputCount);
}

/// <summary>
/// I/O 매핑 오류
/// </summary>
public class IOMappingError
{
    /// <summary>
    /// 오류 타입
    /// </summary>
    public IOMappingErrorType ErrorType { get; init; } = IOMappingErrorType.MissingMapping;

    /// <summary>
    /// 오류 메시지
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// 디바이스 이름
    /// </summary>
    public string DeviceName { get; init; } = string.Empty;

    /// <summary>
    /// 변수 이름
    /// </summary>
    public string? VariableName { get; init; }

    /// <summary>
    /// 심각도
    /// </summary>
    public IssueSeverity Severity { get; init; } = IssueSeverity.Error;
}

/// <summary>
/// I/O 매핑 경고
/// </summary>
public class IOMappingWarning
{
    /// <summary>
    /// 경고 타입
    /// </summary>
    public IOMappingWarningType WarningType { get; init; } = IOMappingWarningType.UnusedIO;

    /// <summary>
    /// 경고 메시지
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// 디바이스 이름
    /// </summary>
    public string DeviceName { get; init; } = string.Empty;

    /// <summary>
    /// 변수 이름
    /// </summary>
    public string? VariableName { get; init; }
}

/// <summary>
/// I/O 디바이스
/// </summary>
public class IODevice
{
    /// <summary>
    /// 디바이스 이름
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 디바이스 타입 (EtherCAT, Profinet 등)
    /// </summary>
    public string DeviceType { get; init; } = string.Empty;

    /// <summary>
    /// 제조사
    /// </summary>
    public string Vendor { get; init; } = string.Empty;

    /// <summary>
    /// 제품 코드
    /// </summary>
    public string ProductCode { get; init; } = string.Empty;

    /// <summary>
    /// 입력 개수
    /// </summary>
    public int InputCount { get; set; }

    /// <summary>
    /// 출력 개수
    /// </summary>
    public int OutputCount { get; set; }

    /// <summary>
    /// 디바이스 상태
    /// </summary>
    public DeviceStatus Status { get; set; } = DeviceStatus.Unknown;

    /// <summary>
    /// I/O 매핑 목록
    /// </summary>
    public List<IOMapping> Mappings { get; set; } = new();
}

/// <summary>
/// I/O 매핑 정보
/// </summary>
public class IOMapping
{
    /// <summary>
    /// 변수 이름
    /// </summary>
    public string VariableName { get; init; } = string.Empty;

    /// <summary>
    /// I/O 방향 (Input, Output)
    /// </summary>
    public IODirection Direction { get; init; } = IODirection.Input;

    /// <summary>
    /// 데이터 타입
    /// </summary>
    public string DataType { get; init; } = string.Empty;

    /// <summary>
    /// 비트 오프셋
    /// </summary>
    public int BitOffset { get; init; }

    /// <summary>
    /// 바이트 크기
    /// </summary>
    public int ByteSize { get; init; }

    /// <summary>
    /// 사용 여부
    /// </summary>
    public bool IsUsed { get; set; }
}

/// <summary>
/// EtherCAT 마스터 정보
/// </summary>
public class EtherCATMaster
{
    /// <summary>
    /// 마스터 이름
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 사이클 타임 (마이크로초)
    /// </summary>
    public int CycleTimeMicroseconds { get; init; }

    /// <summary>
    /// 슬레이브 개수
    /// </summary>
    public int SlaveCount { get; init; }

    /// <summary>
    /// Distributed Clock 사용 여부
    /// </summary>
    public bool UseDistributedClock { get; init; }

    /// <summary>
    /// 통신 상태
    /// </summary>
    public CommunicationStatus CommunicationStatus { get; init; } = CommunicationStatus.Unknown;
}

/// <summary>
/// I/O 매핑 오류 타입
/// </summary>
public enum IOMappingErrorType
{
    /// <summary>매핑 누락</summary>
    MissingMapping,

    /// <summary>중복 매핑</summary>
    DuplicateMapping,

    /// <summary>타입 불일치</summary>
    TypeMismatch,

    /// <summary>주소 충돌</summary>
    AddressConflict,

    /// <summary>디바이스 연결 실패</summary>
    DeviceNotConnected
}

/// <summary>
/// I/O 매핑 경고 타입
/// </summary>
public enum IOMappingWarningType
{
    /// <summary>사용되지 않는 I/O</summary>
    UnusedIO,

    /// <summary>최적화되지 않은 매핑</summary>
    NonOptimalMapping,

    /// <summary>사이클 타임 경고</summary>
    CycleTimeWarning
}

/// <summary>
/// 디바이스 상태
/// </summary>
public enum DeviceStatus
{
    /// <summary>알 수 없음</summary>
    Unknown,

    /// <summary>정상</summary>
    OK,

    /// <summary>경고</summary>
    Warning,

    /// <summary>오류</summary>
    Error,

    /// <summary>연결 안 됨</summary>
    NotConnected
}

/// <summary>
/// I/O 방향
/// </summary>
public enum IODirection
{
    /// <summary>입력</summary>
    Input,

    /// <summary>출력</summary>
    Output
}

/// <summary>
/// 통신 상태
/// </summary>
public enum CommunicationStatus
{
    /// <summary>알 수 없음</summary>
    Unknown,

    /// <summary>정상</summary>
    OK,

    /// <summary>경고</summary>
    Warning,

    /// <summary>오류</summary>
    Error
}
