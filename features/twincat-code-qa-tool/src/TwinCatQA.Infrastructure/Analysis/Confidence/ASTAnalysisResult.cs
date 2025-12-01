namespace TwinCatQA.Infrastructure.Analysis.Confidence;

/// <summary>
/// AST 분석 중간 결과를 나타내는 클래스
/// QA 이슈의 신뢰도 계산을 위한 AST 기반 분석 정보를 포함
/// </summary>
public class ASTAnalysisResult
{
    /// <summary>
    /// AST에서 직접 확인된 이슈인지 여부
    /// true인 경우 신뢰도 +30점
    /// </summary>
    public bool IsConfirmedByAST { get; set; }

    /// <summary>
    /// 데이터 흐름 분석을 통해 확인된 이슈인지 여부
    /// true인 경우 신뢰도 +20점
    /// </summary>
    public bool DataFlowConfirmed { get; set; }

    /// <summary>
    /// 유사한 패턴이 발견된 횟수
    /// 3회 이상인 경우 신뢰도 +10점
    /// </summary>
    public int SimilarOccurrences { get; set; }

    /// <summary>
    /// 컨텍스트가 모호한지 여부
    /// true인 경우 신뢰도 -20점
    /// 예: 조건부 컴파일, 복잡한 매크로, 불명확한 스코프
    /// </summary>
    public bool AmbiguousContext { get; set; }

    /// <summary>
    /// 외부 참조 가능성 여부
    /// true인 경우 신뢰도 -15점
    /// 예: 외부 라이브러리, 링크된 프로젝트, 런타임 바인딩
    /// </summary>
    public bool PossibleExternalReference { get; set; }

    /// <summary>
    /// VAR_INPUT 또는 VAR_OUTPUT 변수인지 여부
    /// true인 경우 신뢰도 -10점
    /// 외부에서 사용될 가능성이 높음
    /// </summary>
    public bool IsInputOutputVariable { get; set; }

    /// <summary>
    /// 전역 변수 리스트(GVL)에 정의된 변수인지 여부
    /// true인 경우 신뢰도 -5점
    /// 다른 POU에서 사용될 가능성이 있음
    /// </summary>
    public bool IsGlobalVariable { get; set; }

    /// <summary>
    /// AST 분석에서 발견된 추가 컨텍스트 정보
    /// 예: 변수 스코프, 사용 위치, 선언 형태 등
    /// </summary>
    public Dictionary<string, object> AdditionalContext { get; set; } = new();

    /// <summary>
    /// 분석 과정에서 발생한 경고 또는 참고사항
    /// </summary>
    public List<string> AnalysisNotes { get; set; } = new();

    /// <summary>
    /// 기본 생성자
    /// </summary>
    public ASTAnalysisResult()
    {
        IsConfirmedByAST = false;
        DataFlowConfirmed = false;
        SimilarOccurrences = 0;
        AmbiguousContext = false;
        PossibleExternalReference = false;
        IsInputOutputVariable = false;
        IsGlobalVariable = false;
    }

    /// <summary>
    /// 완전한 AST 분석 결과를 생성하는 생성자
    /// </summary>
    public ASTAnalysisResult(
        bool isConfirmedByAST,
        bool dataFlowConfirmed,
        int similarOccurrences,
        bool ambiguousContext,
        bool possibleExternalReference,
        bool isInputOutputVariable,
        bool isGlobalVariable)
    {
        IsConfirmedByAST = isConfirmedByAST;
        DataFlowConfirmed = dataFlowConfirmed;
        SimilarOccurrences = similarOccurrences;
        AmbiguousContext = ambiguousContext;
        PossibleExternalReference = possibleExternalReference;
        IsInputOutputVariable = isInputOutputVariable;
        IsGlobalVariable = isGlobalVariable;
    }

    /// <summary>
    /// 분석 노트 추가
    /// </summary>
    public void AddNote(string note)
    {
        if (!string.IsNullOrWhiteSpace(note))
        {
            AnalysisNotes.Add(note);
        }
    }

    /// <summary>
    /// 추가 컨텍스트 정보 추가
    /// </summary>
    public void AddContext(string key, object value)
    {
        if (!string.IsNullOrWhiteSpace(key) && value != null)
        {
            AdditionalContext[key] = value;
        }
    }

    /// <summary>
    /// 긍정적 요소(신뢰도 증가 요인) 개수 계산
    /// </summary>
    public int GetPositiveFactorsCount()
    {
        int count = 0;
        if (IsConfirmedByAST) count++;
        if (DataFlowConfirmed) count++;
        if (SimilarOccurrences >= 3) count++;
        return count;
    }

    /// <summary>
    /// 부정적 요소(신뢰도 감소 요인) 개수 계산
    /// </summary>
    public int GetNegativeFactorsCount()
    {
        int count = 0;
        if (AmbiguousContext) count++;
        if (PossibleExternalReference) count++;
        if (IsInputOutputVariable) count++;
        if (IsGlobalVariable) count++;
        return count;
    }

    /// <summary>
    /// 디버깅 및 로깅을 위한 문자열 표현
    /// </summary>
    public override string ToString()
    {
        return $"AST분석: 확정={IsConfirmedByAST}, 데이터흐름={DataFlowConfirmed}, " +
               $"유사패턴={SimilarOccurrences}, 모호함={AmbiguousContext}, " +
               $"외부참조={PossibleExternalReference}, I/O변수={IsInputOutputVariable}, " +
               $"전역변수={IsGlobalVariable}";
    }
}
