namespace TwinCatQA.Domain.Models.QA;

/// <summary>
/// 확장된 QA 이슈 (Level 2/3 분석 결과 포함)
/// 기존 QAIssue를 상속하여 신뢰도, AST 분석, AI 분석 결과를 추가합니다.
/// </summary>
public class EnhancedQAIssue : QAIssue
{
    /// <summary>
    /// 신뢰도 레벨 (High/Medium/Low)
    /// </summary>
    public ConfidenceLevel Confidence { get; set; } = ConfidenceLevel.Medium;

    /// <summary>
    /// 신뢰도 점수 (0-100)
    /// 90+: High (거의 확실한 문제)
    /// 60-89: Medium (검토 필요)
    /// &lt;60: Low (참고용, 오탐 가능성)
    /// </summary>
    public int ConfidenceScore { get; set; } = 50;

    /// <summary>
    /// 신뢰도 판단 근거 목록
    /// </summary>
    public List<string> ConfidenceReasons { get; set; } = new();

    /// <summary>
    /// 분석 레벨
    /// 1 = 패턴 매칭 (Level 1)
    /// 2 = AST 분석 (Level 2)
    /// 3 = AI 분석 (Level 3)
    /// </summary>
    public int AnalysisLevel { get; set; } = 1;

    /// <summary>
    /// AST 분석 컨텍스트 (Level 2 결과)
    /// </summary>
    public ASTAnalysisContext? ASTContext { get; set; }

    /// <summary>
    /// AI 분석 컨텍스트 (Level 3 결과)
    /// </summary>
    public AIAnalysisContext? AIContext { get; set; }

    /// <summary>
    /// 억제(Suppression) 여부
    /// 인라인 주석 또는 설정 파일에 의해 억제됨
    /// </summary>
    public bool IsSuppressed { get; set; }

    /// <summary>
    /// 억제 이유 (억제된 경우)
    /// </summary>
    public string? SuppressionReason { get; set; }

    /// <summary>
    /// 억제 소스 (inline/config/manual)
    /// </summary>
    public string? SuppressionSource { get; set; }

    /// <summary>
    /// 관련 이슈 ID 목록 (중복/유사 이슈)
    /// </summary>
    public List<string>? RelatedIssueIds { get; set; }

    /// <summary>
    /// 이슈 고유 ID
    /// </summary>
    public string IssueId { get; set; } = Guid.NewGuid().ToString("N")[..12];

    /// <summary>
    /// 개발자 피드백 (Accept/Reject/Ignore)
    /// </summary>
    public DeveloperFeedback? Feedback { get; set; }

    /// <summary>
    /// 위험도 점수 (0-100, AI 분석 시 계산)
    /// </summary>
    public int RiskScore { get; set; }

    /// <summary>
    /// 신뢰도가 중간 이상인지 여부
    /// </summary>
    public bool IsMediumOrHighConfidence => Confidence >= ConfidenceLevel.Medium;

    /// <summary>
    /// 신뢰도가 높은지 여부
    /// </summary>
    public bool IsHighConfidence => Confidence == ConfidenceLevel.High;

    /// <summary>
    /// 기존 QAIssue에서 EnhancedQAIssue 생성
    /// </summary>
    public static EnhancedQAIssue FromQAIssue(QAIssue issue)
    {
        return new EnhancedQAIssue
        {
            Severity = issue.Severity,
            Category = issue.Category,
            Title = issue.Title,
            Description = issue.Description,
            Location = issue.Location,
            FilePath = issue.FilePath,
            Line = issue.Line,
            WhyDangerous = issue.WhyDangerous,
            Recommendation = issue.Recommendation,
            Examples = issue.Examples,
            RuleId = issue.RuleId,
            OldCodeSnippet = issue.OldCodeSnippet,
            NewCodeSnippet = issue.NewCodeSnippet,
            AnalysisLevel = 1  // 기본값: Level 1 (패턴 매칭)
        };
    }

    /// <summary>
    /// 요약 문자열 반환
    /// </summary>
    public string GetSummary()
    {
        var status = IsSuppressed ? "[억제됨]" : $"[{Confidence}]";
        return $"{status} [{RuleId}] {Title} - 신뢰도 {ConfidenceScore}% (Level {AnalysisLevel})";
    }
}

/// <summary>
/// 신뢰도 레벨 열거형
/// </summary>
public enum ConfidenceLevel
{
    /// <summary>
    /// 참고용, 오탐 가능성 높음 (&lt;60%)
    /// </summary>
    Low = 0,

    /// <summary>
    /// 검토 필요 (60-89%)
    /// </summary>
    Medium = 1,

    /// <summary>
    /// 거의 확실한 문제 (90%+)
    /// </summary>
    High = 2
}

/// <summary>
/// AST 분석 컨텍스트 (Level 2 결과)
/// </summary>
public class ASTAnalysisContext
{
    /// <summary>
    /// AST에서 직접 확인됨 (+30점)
    /// </summary>
    public bool IsConfirmedByAST { get; set; }

    /// <summary>
    /// 데이터 흐름 분석으로 확인됨 (+20점)
    /// </summary>
    public bool DataFlowConfirmed { get; set; }

    /// <summary>
    /// 유사 패턴 발견 횟수 (3 이상이면 +10점)
    /// </summary>
    public int SimilarOccurrences { get; set; }

    /// <summary>
    /// 컨텍스트가 모호함 (-20점)
    /// 조건부 컴파일, 매크로 등
    /// </summary>
    public bool AmbiguousContext { get; set; }

    /// <summary>
    /// 외부 참조 가능성 있음 (-15점)
    /// 라이브러리, 링크된 프로젝트 등
    /// </summary>
    public bool PossibleExternalReference { get; set; }

    /// <summary>
    /// VAR_INPUT/VAR_OUTPUT 변수인지 (-10점)
    /// </summary>
    public bool IsIOVariable { get; set; }

    /// <summary>
    /// 전역 변수(GVL)인지 (-5점)
    /// </summary>
    public bool IsGlobalVariable { get; set; }

    /// <summary>
    /// AST 노드 타입 (예: IfStatement, AssignmentStatement)
    /// </summary>
    public string? ASTNodeType { get; set; }

    /// <summary>
    /// 상위 노드 타입 (예: FunctionBlock, Program)
    /// </summary>
    public string? ParentNodeType { get; set; }

    /// <summary>
    /// 변수 스코프 (VAR, VAR_INPUT, VAR_OUTPUT 등)
    /// </summary>
    public string? VariableScope { get; set; }

    /// <summary>
    /// 정의-사용 체인 정보
    /// </summary>
    public DefUseInfo? DefUseChain { get; set; }

    /// <summary>
    /// 제어 흐름 정보
    /// </summary>
    public ControlFlowInfo? ControlFlow { get; set; }

    /// <summary>
    /// 추가 메타데이터
    /// </summary>
    public Dictionary<string, object>? AdditionalData { get; set; }
}

/// <summary>
/// 정의-사용 체인 정보
/// </summary>
public class DefUseInfo
{
    /// <summary>
    /// 변수가 정의된 라인 번호 목록
    /// </summary>
    public List<int> DefinitionLines { get; set; } = new();

    /// <summary>
    /// 변수가 사용된 라인 번호 목록
    /// </summary>
    public List<int> UsageLines { get; set; } = new();

    /// <summary>
    /// 초기화 여부
    /// </summary>
    public bool IsInitialized { get; set; }

    /// <summary>
    /// 사용되기 전에 정의되었는지
    /// </summary>
    public bool DefinedBeforeUse { get; set; }
}

/// <summary>
/// 제어 흐름 정보
/// </summary>
public class ControlFlowInfo
{
    /// <summary>
    /// 도달 가능한지 여부
    /// </summary>
    public bool IsReachable { get; set; } = true;

    /// <summary>
    /// 루프 내부인지
    /// </summary>
    public bool IsInsideLoop { get; set; }

    /// <summary>
    /// 조건문 내부인지
    /// </summary>
    public bool IsInsideConditional { get; set; }

    /// <summary>
    /// 루프 중첩 깊이
    /// </summary>
    public int LoopNestingDepth { get; set; }

    /// <summary>
    /// 조건문 중첩 깊이
    /// </summary>
    public int ConditionalNestingDepth { get; set; }
}

/// <summary>
/// AI 분석 컨텍스트 (Level 3 결과)
/// </summary>
public class AIAnalysisContext
{
    /// <summary>
    /// 오탐(False Positive)으로 판단됨
    /// </summary>
    public bool IsFalsePositive { get; set; }

    /// <summary>
    /// AI 신뢰도 (0-100)
    /// </summary>
    public int AIConfidence { get; set; }

    /// <summary>
    /// AI 판단 근거
    /// </summary>
    public string? AIReasoning { get; set; }

    /// <summary>
    /// 권장 수정 방법
    /// </summary>
    public string? RecommendedFix { get; set; }

    /// <summary>
    /// 수정 코드 예시
    /// </summary>
    public string? FixCodeExample { get; set; }

    /// <summary>
    /// 재평가된 심각도
    /// </summary>
    public Severity? ReassessedSeverity { get; set; }

    /// <summary>
    /// 잠재적 영향 분석
    /// </summary>
    public ImpactAnalysis? Impact { get; set; }

    /// <summary>
    /// API 호출 비용 (USD)
    /// </summary>
    public double? CostUSD { get; set; }

    /// <summary>
    /// 사용된 모델 (예: claude-3-5-sonnet)
    /// </summary>
    public string? ModelUsed { get; set; }

    /// <summary>
    /// 분석 타임스탬프
    /// </summary>
    public DateTime? AnalyzedAt { get; set; }
}

/// <summary>
/// 영향 분석 결과
/// </summary>
public class ImpactAnalysis
{
    /// <summary>
    /// 런타임 영향
    /// </summary>
    public string? RuntimeImpact { get; set; }

    /// <summary>
    /// 안전성 영향
    /// </summary>
    public string? SafetyImpact { get; set; }

    /// <summary>
    /// 유지보수성 영향
    /// </summary>
    public string? MaintainabilityImpact { get; set; }

    /// <summary>
    /// 발생 가능한 시나리오 목록
    /// </summary>
    public List<string>? Scenarios { get; set; }
}

/// <summary>
/// 개발자 피드백
/// </summary>
public class DeveloperFeedback
{
    /// <summary>
    /// 피드백 액션
    /// </summary>
    public FeedbackAction Action { get; set; }

    /// <summary>
    /// 개발자 코멘트
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// 수정된 심각도 (있는 경우)
    /// </summary>
    public Severity? ModifiedSeverity { get; set; }

    /// <summary>
    /// 피드백 시간
    /// </summary>
    public DateTime FeedbackTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 피드백 제공자 (익명 또는 식별자)
    /// </summary>
    public string? FeedbackBy { get; set; }
}

/// <summary>
/// 피드백 액션 열거형
/// </summary>
public enum FeedbackAction
{
    /// <summary>
    /// 이슈 수락 (진짜 문제로 확인)
    /// </summary>
    Accept,

    /// <summary>
    /// 이슈 거부 (오탐으로 판단)
    /// </summary>
    Reject,

    /// <summary>
    /// 이슈 무시 (나중에 처리)
    /// </summary>
    Ignore,

    /// <summary>
    /// 심각도 변경
    /// </summary>
    ModifySeverity
}
