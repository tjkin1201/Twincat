namespace TwinCatQA.Domain.Models;

/// <summary>
/// TwinCAT 프로젝트의 개별 코드 파일을 나타내는 엔티티
/// </summary>
public class CodeFile
{
    #region 식별자

    /// <summary>
    /// 고유 식별자
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// 파일의 절대 경로
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    #endregion

    #region 메타데이터

    /// <summary>
    /// 파일 타입 (POU, DUT, GVL)
    /// </summary>
    public FileType Type { get; init; }

    /// <summary>
    /// 프로그래밍 언어 (ST, LD, FBD, SFC)
    /// </summary>
    public ProgrammingLanguage Language { get; init; }

    /// <summary>
    /// 총 라인 수
    /// </summary>
    public int LineCount { get; init; }

    /// <summary>
    /// 파일 원본 내용 (정적 분석용)
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// 파일 최종 수정 시각
    /// </summary>
    public DateTime LastModified { get; init; }

    /// <summary>
    /// 파일 해시 (SHA256, 변경 감지용)
    /// </summary>
    public string FileHash { get; init; } = string.Empty;

    #endregion

    #region 파싱 결과

    /// <summary>
    /// ANTLR4 추상 구문 트리 (AST)
    /// </summary>
    /// <remarks>
    /// 실제 구현에서는 Antlr4.Runtime.Tree.IParseTree 타입 사용
    /// </remarks>
    public object? Ast { get; set; }

    /// <summary>
    /// 파일에 포함된 Function Block 목록
    /// </summary>
    public List<FunctionBlock> FunctionBlocks { get; init; } = new();

    /// <summary>
    /// 파일에 포함된 데이터 타입 목록
    /// </summary>
    public List<DataType> DataTypes { get; init; } = new();

    /// <summary>
    /// 파일에 포함된 전역 변수 목록 (GVL 파일의 경우)
    /// </summary>
    public List<Variable> GlobalVariables { get; init; } = new();

    #endregion

    #region 검증 결과

    /// <summary>
    /// 이 파일에서 발견된 위반 사항 목록
    /// </summary>
    public List<Violation> Violations { get; init; } = new();

    /// <summary>
    /// 파일 품질 점수 (0-100)
    /// </summary>
    public double QualityScore { get; set; }

    #endregion
}

/// <summary>
/// 사용자 정의 데이터 타입 (구조체, 열거형 등)
/// </summary>
public class DataType
{
    /// <summary>
    /// 데이터 타입 이름 (예: T_MyStruct, E_State)
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 데이터 타입 종류 (Struct, Enum, Union, Alias)
    /// </summary>
    public DataTypeKind Kind { get; init; }

    /// <summary>
    /// 구조체 필드 목록 (Kind == Struct인 경우)
    /// </summary>
    public List<StructField> Fields { get; init; } = new();

    /// <summary>
    /// 열거형 값 목록 (Kind == Enum인 경우)
    /// </summary>
    public List<EnumValue> EnumValues { get; init; } = new();

    /// <summary>
    /// 파일 경로
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// 선언된 라인 번호
    /// </summary>
    public int DeclarationLine { get; init; }
}

/// <summary>
/// 구조체 필드
/// </summary>
public class StructField
{
    /// <summary>
    /// 필드 이름
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 필드 데이터 타입
    /// </summary>
    public string DataType { get; init; } = string.Empty;

    /// <summary>
    /// 필드 설명 주석
    /// </summary>
    public string? Comment { get; init; }
}

/// <summary>
/// 열거형 값
/// </summary>
public class EnumValue
{
    /// <summary>
    /// 열거형 항목 이름
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 명시적 값 (선택적)
    /// </summary>
    public int? Value { get; init; }
}
