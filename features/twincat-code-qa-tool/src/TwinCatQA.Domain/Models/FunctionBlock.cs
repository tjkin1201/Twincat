namespace TwinCatQA.Domain.Models;

/// <summary>
/// IEC 61131-3 Function Block 또는 Function을 나타내는 엔티티
/// </summary>
public class FunctionBlock
{
    /// <summary>
    /// 고유 식별자
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Function Block 이름
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Function Block 타입 (FB, Function, Program, Method)
    /// </summary>
    public FunctionBlockType Type { get; init; }

    #region 위치 정보

    /// <summary>
    /// 파일 경로
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// 시작 라인 번호
    /// </summary>
    public int StartLine { get; init; }

    /// <summary>
    /// 종료 라인 번호
    /// </summary>
    public int EndLine { get; init; }

    #endregion

    #region 코드 메트릭

    /// <summary>
    /// 사이클로매틱 복잡도 (McCabe 복잡도)
    /// </summary>
    public int CyclomaticComplexity { get; set; }

    /// <summary>
    /// 총 라인 수
    /// </summary>
    public int LineCount { get; init; }

    /// <summary>
    /// 주석 라인 수
    /// </summary>
    public int CommentLineCount { get; set; }

    /// <summary>
    /// 주석 비율 (0.0 ~ 1.0)
    /// </summary>
    public double CommentRatio { get; set; }

    #endregion

    #region 변수 및 의존성

    /// <summary>
    /// 입력 변수 목록
    /// </summary>
    public List<Variable> InputVariables { get; init; } = new();

    /// <summary>
    /// 출력 변수 목록
    /// </summary>
    public List<Variable> OutputVariables { get; init; } = new();

    /// <summary>
    /// 로컬 변수 목록
    /// </summary>
    public List<Variable> LocalVariables { get; init; } = new();

    /// <summary>
    /// 의존성 (호출하는 다른 FB 이름)
    /// </summary>
    public List<string> Dependencies { get; init; } = new();

    #endregion

    #region 상태 머신 정보

    /// <summary>
    /// 상태 머신 포함 여부
    /// </summary>
    public bool HasStateMachine { get; set; }

    /// <summary>
    /// 상태 ENUM 타입명 (예: E_ControllerState)
    /// </summary>
    public string? StateEnumType { get; set; }

    /// <summary>
    /// 정의된 모든 상태 목록
    /// </summary>
    public List<string> States { get; init; } = new();

    #endregion

    #region 주석 및 문서화

    /// <summary>
    /// FB 목적 설명 주석
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 한글 주석 존재 여부
    /// </summary>
    public bool HasKoreanComment { get; set; }

    #endregion

    #region 코드 본문

    /// <summary>
    /// FB/함수 본문 코드 (정적 분석용)
    /// </summary>
    public string Body { get; init; } = string.Empty;

    #endregion
}
