namespace TwinCatQA.Domain.Models;

/// <summary>
/// PLC 변수 (입력, 출력, 로컬, 전역)
/// </summary>
public class Variable
{
    /// <summary>
    /// 변수명
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 변수 스코프 (Input, Output, Local 등)
    /// </summary>
    public VariableScope Scope { get; init; }

    /// <summary>
    /// 데이터 타입 (예: INT, REAL, T_MyStruct)
    /// </summary>
    public string DataType { get; init; } = string.Empty;

    /// <summary>
    /// 초기값 (선택적)
    /// </summary>
    public string? InitialValue { get; init; }

    /// <summary>
    /// 상수 여부
    /// </summary>
    public bool IsConstant { get; init; }

    /// <summary>
    /// 선언된 라인 번호
    /// </summary>
    public int DeclarationLine { get; init; }

    /// <summary>
    /// 코드 내에서 사용되었는지 여부 (미사용 변수 감지용)
    /// </summary>
    public bool IsUsed { get; set; }

    /// <summary>
    /// 명명 규칙을 따르는지 여부
    /// </summary>
    public bool FollowsNamingConvention { get; set; }
}
