namespace TwinCatQA.Domain.Models;

/// <summary>
/// 위반 사항의 심각도 수준
/// </summary>
public enum ViolationSeverity
{
    /// <summary>
    /// 낮음 - 권장 사항
    /// </summary>
    Low,

    /// <summary>
    /// 중간 - 주의 필요
    /// </summary>
    Medium,

    /// <summary>
    /// 높음 - 수정 권장
    /// </summary>
    High,

    /// <summary>
    /// 심각 - 커밋 차단 (pre-commit hook)
    /// </summary>
    Critical
}

/// <summary>
/// 프로젝트 헌장의 원칙
/// </summary>
public enum ConstitutionPrinciple
{
    /// <summary>
    /// 없음
    /// </summary>
    None = 0,

    /// <summary>
    /// 원칙 1: 한글 우선
    /// </summary>
    KoreanFirst = 1,

    /// <summary>
    /// 원칙 2: 실시간 안전성
    /// </summary>
    RealTimeSafety = 2,

    /// <summary>
    /// 원칙 3: 모듈화 및 재사용성
    /// </summary>
    Modularity = 3,

    /// <summary>
    /// 원칙 4: 상태 기반 설계
    /// </summary>
    StateMachineDesign = 4,

    /// <summary>
    /// 원칙 5: 명명 규칙
    /// </summary>
    NamingConvention = 5,

    /// <summary>
    /// 원칙 6: 문서화 의무
    /// </summary>
    Documentation = 6,

    /// <summary>
    /// 원칙 7: 버전 관리
    /// </summary>
    VersionControl = 7,

    /// <summary>
    /// 원칙 8: 테스트 및 시뮬레이션
    /// </summary>
    TestingSimulation = 8
}

/// <summary>
/// 파일 타입
/// </summary>
public enum FileType
{
    /// <summary>
    /// Program Organization Unit (FB, Function, Program)
    /// </summary>
    POU,

    /// <summary>
    /// Data Unit Type (Struct, Enum, Union)
    /// </summary>
    DUT,

    /// <summary>
    /// Global Variable List
    /// </summary>
    GVL,

    /// <summary>
    /// 알 수 없음
    /// </summary>
    Unknown
}

/// <summary>
/// PLC 프로그래밍 언어
/// </summary>
public enum ProgrammingLanguage
{
    /// <summary>
    /// Structured Text
    /// </summary>
    ST,

    /// <summary>
    /// Ladder Diagram
    /// </summary>
    LD,

    /// <summary>
    /// Function Block Diagram
    /// </summary>
    FBD,

    /// <summary>
    /// Sequential Function Chart
    /// </summary>
    SFC,

    /// <summary>
    /// Instruction List (legacy)
    /// </summary>
    IL,

    /// <summary>
    /// 알 수 없음
    /// </summary>
    Unknown
}

/// <summary>
/// 검증 모드
/// </summary>
public enum ValidationMode
{
    /// <summary>
    /// 프로젝트 전체 검증
    /// </summary>
    Full,

    /// <summary>
    /// Git diff 기반 증분 검증
    /// </summary>
    Incremental,

    /// <summary>
    /// 특정 파일만 검증
    /// </summary>
    FileSpecific
}

/// <summary>
/// Function Block 타입
/// </summary>
public enum FunctionBlockType
{
    /// <summary>
    /// FUNCTION_BLOCK
    /// </summary>
    FunctionBlock,

    /// <summary>
    /// FUNCTION
    /// </summary>
    Function,

    /// <summary>
    /// PROGRAM
    /// </summary>
    Program,

    /// <summary>
    /// METHOD (클래스 메서드)
    /// </summary>
    Method
}

/// <summary>
/// 변수 스코프
/// </summary>
public enum VariableScope
{
    /// <summary>
    /// VAR_INPUT
    /// </summary>
    Input,

    /// <summary>
    /// VAR_OUTPUT
    /// </summary>
    Output,

    /// <summary>
    /// VAR_IN_OUT
    /// </summary>
    InOut,

    /// <summary>
    /// VAR (로컬)
    /// </summary>
    Local,

    /// <summary>
    /// GVL에 선언된 전역 변수
    /// </summary>
    Global,

    /// <summary>
    /// VAR CONSTANT
    /// </summary>
    Constant,

    /// <summary>
    /// VAR PERSISTENT (영구 변수)
    /// </summary>
    Persistent,

    /// <summary>
    /// VAR RETAIN (유지 변수)
    /// </summary>
    Retain
}

/// <summary>
/// 리포트 형식
/// </summary>
public enum ReportFormat
{
    /// <summary>
    /// HTML 형식
    /// </summary>
    HTML,

    /// <summary>
    /// PDF 형식
    /// </summary>
    PDF
}

/// <summary>
/// 데이터 타입 종류
/// </summary>
public enum DataTypeKind
{
    /// <summary>
    /// TYPE T_MyStruct : STRUCT ... END_STRUCT END_TYPE
    /// </summary>
    Struct,

    /// <summary>
    /// TYPE E_State : (Idle, Running, Error) END_TYPE
    /// </summary>
    Enum,

    /// <summary>
    /// TYPE T_Union : UNION ... END_UNION END_TYPE
    /// </summary>
    Union,

    /// <summary>
    /// TYPE T_MyInt : INT END_TYPE
    /// </summary>
    Alias
}

/// <summary>
/// 품질 등급
/// </summary>
public enum QualityGrade
{
    /// <summary>
    /// 나쁨 (0-49점)
    /// </summary>
    Poor,

    /// <summary>
    /// 보통 (50-74점)
    /// </summary>
    Fair,

    /// <summary>
    /// 좋음 (75-89점)
    /// </summary>
    Good,

    /// <summary>
    /// 우수 (90-100점)
    /// </summary>
    Excellent
}

/// <summary>
/// 차트 타입
/// </summary>
public enum ChartType
{
    /// <summary>
    /// 막대 그래프
    /// </summary>
    Bar,

    /// <summary>
    /// 원형 그래프
    /// </summary>
    Pie,

    /// <summary>
    /// 선 그래프 (추세)
    /// </summary>
    Line,

    /// <summary>
    /// 레이더 차트 (헌장 준수율)
    /// </summary>
    Radar
}

/// <summary>
/// Dead Code 타입
/// </summary>
public enum DeadCodeType
{
    /// <summary>도달할 수 없는 코드</summary>
    UnreachableCode,

    /// <summary>사용되지 않는 함수</summary>
    UnusedFunction,

    /// <summary>항상 거짓인 조건문</summary>
    AlwaysFalseCondition,

    /// <summary>항상 참인 조건문</summary>
    AlwaysTrueCondition,

    /// <summary>주석 처리된 코드</summary>
    CommentedOutCode
}

/// <summary>
/// 이슈 심각도 (고급 분석용)
/// </summary>
public enum IssueSeverity
{
    /// <summary>정보</summary>
    Info,

    /// <summary>낮음 (코드 품질)</summary>
    Low,

    /// <summary>경고</summary>
    Warning,

    /// <summary>보통 (유지보수성)</summary>
    Medium,

    /// <summary>높음 (오동작 가능)</summary>
    High,

    /// <summary>오류</summary>
    Error,

    /// <summary>치명적 (시스템 다운/안전)</summary>
    Critical
}
