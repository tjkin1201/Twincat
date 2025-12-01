namespace TwinCatQA.Infrastructure.Analysis.DataFlow;

/// <summary>
/// 정의-사용 체인 (Definition-Use Chain)
/// 변수의 정의(할당) 위치와 사용 위치를 추적합니다.
/// </summary>
public class DefUseChain
{
    /// <summary>
    /// 변수명
    /// </summary>
    public string VariableName { get; init; } = string.Empty;

    /// <summary>
    /// 변수가 정의(할당)된 위치 목록
    /// </summary>
    public List<DefinitionSite> Definitions { get; init; } = new();

    /// <summary>
    /// 변수가 사용된 위치 목록
    /// </summary>
    public List<UsageSite> Usages { get; init; } = new();

    /// <summary>
    /// 초기화 여부 (선언 시 초기값 또는 첫 할당 전 사용 여부)
    /// </summary>
    public bool IsInitialized { get; set; }

    /// <summary>
    /// 선언 위치
    /// </summary>
    public DeclarationSite? Declaration { get; set; }

    /// <summary>
    /// 변수가 사용되는지 여부
    /// </summary>
    public bool IsUsed => Usages.Count > 0;

    /// <summary>
    /// 변수가 할당되는지 여부
    /// </summary>
    public bool IsAssigned => Definitions.Count > 0;

    /// <summary>
    /// 초기화되지 않은 사용이 있는지 확인
    /// </summary>
    public List<UsageSite> GetUninitializedUsages()
    {
        var uninitializedUsages = new List<UsageSite>();

        if (!IsInitialized && Declaration != null)
        {
            // 선언 시 초기화되지 않은 경우, 첫 정의 이전의 사용을 찾음
            var firstDefinitionLine = Definitions.Count > 0
                ? Definitions.Min(d => d.Line)
                : int.MaxValue;

            uninitializedUsages.AddRange(
                Usages.Where(u => u.Line < firstDefinitionLine)
            );
        }

        return uninitializedUsages;
    }
}

/// <summary>
/// 변수 선언 위치
/// </summary>
public class DeclarationSite
{
    /// <summary>
    /// 파일 경로
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// 라인 번호
    /// </summary>
    public int Line { get; init; }

    /// <summary>
    /// 컬럼 번호
    /// </summary>
    public int Column { get; init; }

    /// <summary>
    /// 변수 스코프 (Input, Output, Local 등)
    /// </summary>
    public string Scope { get; init; } = string.Empty;

    /// <summary>
    /// 데이터 타입
    /// </summary>
    public string DataType { get; init; } = string.Empty;

    /// <summary>
    /// 선언 시 초기값 여부
    /// </summary>
    public bool HasInitialValue { get; init; }

    public override string ToString()
        => $"{FilePath}:{Line}:{Column} ({Scope} {DataType})";
}

/// <summary>
/// 변수 정의(할당) 위치
/// </summary>
public class DefinitionSite
{
    /// <summary>
    /// 파일 경로
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// 라인 번호
    /// </summary>
    public int Line { get; init; }

    /// <summary>
    /// 컬럼 번호
    /// </summary>
    public int Column { get; init; }

    /// <summary>
    /// 할당 표현식 (우변)
    /// </summary>
    public string AssignmentExpression { get; init; } = string.Empty;

    /// <summary>
    /// 제어 흐름 그래프 상의 기본 블록 ID
    /// </summary>
    public int BasicBlockId { get; set; }

    /// <summary>
    /// 조건부 할당 여부 (IF, CASE 내부 등)
    /// </summary>
    public bool IsConditional { get; init; }

    /// <summary>
    /// 루프 내부 할당 여부
    /// </summary>
    public bool IsInLoop { get; init; }

    public override string ToString()
        => $"{FilePath}:{Line}:{Column} := {AssignmentExpression}";
}

/// <summary>
/// 변수 사용 위치
/// </summary>
public class UsageSite
{
    /// <summary>
    /// 파일 경로
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// 라인 번호
    /// </summary>
    public int Line { get; init; }

    /// <summary>
    /// 컬럼 번호
    /// </summary>
    public int Column { get; init; }

    /// <summary>
    /// 사용 컨텍스트 (표현식, 조건문 등)
    /// </summary>
    public string Context { get; init; } = string.Empty;

    /// <summary>
    /// 제어 흐름 그래프 상의 기본 블록 ID
    /// </summary>
    public int BasicBlockId { get; set; }

    /// <summary>
    /// 읽기 용도인지 여부 (vs 쓰기)
    /// </summary>
    public bool IsRead { get; init; } = true;

    public override string ToString()
        => $"{FilePath}:{Line}:{Column} in {Context}";
}

/// <summary>
/// 사용되지 않는 변수 정보
/// </summary>
public class UnusedVariable
{
    /// <summary>
    /// 변수명
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 선언 위치
    /// </summary>
    public DeclarationSite Declaration { get; init; } = null!;

    /// <summary>
    /// 할당은 되지만 사용되지 않는지 여부
    /// </summary>
    public bool IsWriteOnly { get; init; }

    /// <summary>
    /// 권장 사항
    /// </summary>
    public string Recommendation => IsWriteOnly
        ? "변수에 값을 할당하지만 사용하지 않습니다. 할당 제거를 고려하세요."
        : "변수가 선언되었지만 사용되지 않습니다. 제거를 고려하세요.";

    public override string ToString()
        => $"미사용 변수: {Name} ({Declaration})";
}

/// <summary>
/// 초기화되지 않은 사용 정보
/// </summary>
public class UninitializedUsage
{
    /// <summary>
    /// 변수명
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 사용 위치
    /// </summary>
    public UsageSite Usage { get; init; } = null!;

    /// <summary>
    /// 선언 위치
    /// </summary>
    public DeclarationSite? Declaration { get; init; }

    /// <summary>
    /// 경고 메시지
    /// </summary>
    public string Message => Declaration != null
        ? $"변수 '{Name}'이(가) 초기화되기 전에 사용되었습니다."
        : $"변수 '{Name}'이(가) 선언되지 않았거나 스코프 밖에서 사용되었습니다.";

    public override string ToString()
        => $"{Message} - {Usage}";
}
