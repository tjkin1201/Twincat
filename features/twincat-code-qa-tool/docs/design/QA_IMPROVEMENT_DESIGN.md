# TwinCAT QA Tool 개선 설계서

## Level 2 & Level 3 통합 아키텍처

```
                              TwinCAT QA 분석 파이프라인
╔═══════════════════════════════════════════════════════════════════════════════╗
║                                                                               ║
║  ┌─────────────┐      ┌─────────────┐      ┌─────────────┐                   ║
║  │  Level 1    │      │  Level 2    │      │  Level 3    │                   ║
║  │  패턴 매칭  │ ───▶ │  AST 분석   │ ───▶ │  AI 분석    │                   ║
║  │  (현재)     │      │  (확장)     │      │  (신규)     │                   ║
║  └─────────────┘      └─────────────┘      └─────────────┘                   ║
║        │                     │                    │                          ║
║        ▼                     ▼                    ▼                          ║
║   [12,480개]           [~3,000개]            [~300개]                        ║
║   정규식 기반           AST + DFA             Claude API                      ║
║   높은 오탐률           중간 정확도           높은 정확도                      ║
║                                                                               ║
╚═══════════════════════════════════════════════════════════════════════════════╝
```

---

## 1. Level 2: AST 기반 정적 분석

### 1.1 현재 구현 현황

기존 코드베이스에 이미 AST 인프라가 구현되어 있습니다:

```
src/TwinCatQA.Domain/Models/AST/
├── ASTNode.cs              # 기본 추상 클래스
├── IASTVisitor.cs          # Visitor 패턴 인터페이스
├── ProgramStructureNodes.cs # FB, PROGRAM, FUNCTION 노드
├── VariableNodes.cs        # 변수 선언 노드
├── StatementNodes.cs       # IF, FOR, WHILE 등 문장 노드
├── ExpressionNodes.cs      # 수식 노드
├── DataTypeNodes.cs        # 데이터 타입 노드
└── SyntaxTree.cs           # 구문 트리 루트

src/TwinCatQA.Infrastructure/Parsers/
├── AntlrParserService.cs   # ANTLR4 파서 서비스
├── ASTBuilderVisitor.cs    # AST 생성 Visitor
└── SemanticAnalyzer.cs     # 의미 분석기
```

### 1.2 확장 필요 컴포넌트

#### 1.2.1 데이터 흐름 분석기 (Data Flow Analyzer)

```
src/TwinCatQA.Infrastructure/Analysis/DataFlow/
├── DataFlowAnalyzer.cs      # 메인 분석기
├── DefUseChain.cs           # 정의-사용 체인
├── ReachingDefinitions.cs   # 도달 정의 분석
├── LiveVariableAnalysis.cs  # 활성 변수 분석
└── ControlFlowGraph.cs      # 제어 흐름 그래프
```

**핵심 인터페이스:**

```csharp
namespace TwinCatQA.Infrastructure.Analysis.DataFlow;

/// <summary>
/// 데이터 흐름 분석 결과
/// </summary>
public class DataFlowResult
{
    /// <summary>정의-사용 체인</summary>
    public Dictionary<string, List<DefUseEntry>> DefUseChains { get; init; }

    /// <summary>미사용 변수 목록</summary>
    public List<UnusedVariable> UnusedVariables { get; init; }

    /// <summary>미초기화 변수 사용 위치</summary>
    public List<UninitializedUsage> UninitializedUsages { get; init; }

    /// <summary>도달 불가능 코드 블록</summary>
    public List<UnreachableBlock> UnreachableBlocks { get; init; }
}

/// <summary>
/// 정의-사용 엔트리
/// </summary>
public record DefUseEntry(
    string VariableName,
    int DefinitionLine,
    List<int> UsageLines,
    bool IsInitialized
);

/// <summary>
/// 제어 흐름 그래프 노드
/// </summary>
public class CFGNode
{
    public int Id { get; init; }
    public List<ASTNode> Statements { get; init; }
    public List<CFGNode> Predecessors { get; init; }
    public List<CFGNode> Successors { get; init; }
    public bool IsReachable { get; set; }
}
```

#### 1.2.2 신뢰도 시스템 (Confidence System)

```csharp
namespace TwinCatQA.Domain.Models.QA;

/// <summary>
/// 확장된 QA 이슈 (신뢰도 포함)
/// </summary>
public class EnhancedQAIssue : QAIssue
{
    /// <summary>
    /// 신뢰도 레벨
    /// </summary>
    public ConfidenceLevel Confidence { get; set; }

    /// <summary>
    /// 신뢰도 점수 (0-100)
    /// </summary>
    public int ConfidenceScore { get; set; }

    /// <summary>
    /// 신뢰도 판단 근거
    /// </summary>
    public List<string> ConfidenceReasons { get; set; }

    /// <summary>
    /// 분석 레벨 (1=패턴, 2=AST, 3=AI)
    /// </summary>
    public int AnalysisLevel { get; set; }

    /// <summary>
    /// AST 분석 결과 (Level 2)
    /// </summary>
    public ASTAnalysisResult? ASTResult { get; set; }

    /// <summary>
    /// AI 분석 결과 (Level 3)
    /// </summary>
    public AIAnalysisResult? AIResult { get; set; }
}

public enum ConfidenceLevel
{
    /// <summary>거의 확실한 문제 (90%+)</summary>
    High,

    /// <summary>검토 필요 (60-89%)</summary>
    Medium,

    /// <summary>참고용, 오탐 가능성 높음 (<60%)</summary>
    Low
}
```

#### 1.2.3 신뢰도 계산 알고리즘

```csharp
/// <summary>
/// 신뢰도 계산기
/// </summary>
public class ConfidenceCalculator
{
    /// <summary>
    /// AST 분석 기반 신뢰도 계산
    /// </summary>
    public ConfidenceResult Calculate(QAIssue issue, ASTAnalysisResult astResult)
    {
        var score = 50; // 기본 점수
        var reasons = new List<string>();

        // 1. AST에서 직접 확인된 경우 +30
        if (astResult.IsConfirmedByAST)
        {
            score += 30;
            reasons.Add("AST 분석으로 확인됨");
        }

        // 2. 데이터 흐름 분석으로 확인된 경우 +20
        if (astResult.DataFlowConfirmed)
        {
            score += 20;
            reasons.Add("데이터 흐름 분석으로 확인됨");
        }

        // 3. 같은 패턴이 여러 곳에서 발견된 경우 +10
        if (astResult.SimilarOccurrences > 3)
        {
            score += 10;
            reasons.Add($"유사 패턴 {astResult.SimilarOccurrences}곳에서 발견");
        }

        // 4. 컨텍스트가 명확하지 않은 경우 -20
        if (astResult.AmbiguousContext)
        {
            score -= 20;
            reasons.Add("컨텍스트 모호함");
        }

        // 5. 외부 참조 가능성 있는 경우 -15
        if (astResult.PossibleExternalReference)
        {
            score -= 15;
            reasons.Add("외부 참조 가능성 있음");
        }

        return new ConfidenceResult
        {
            Score = Math.Clamp(score, 0, 100),
            Level = score >= 90 ? ConfidenceLevel.High
                  : score >= 60 ? ConfidenceLevel.Medium
                  : ConfidenceLevel.Low,
            Reasons = reasons
        };
    }
}
```

### 1.3 예외 규칙 시스템

#### 1.3.1 설정 파일 구조

```json
// .twincat-qa.json
{
  "version": "2.0",
  "projectName": "PM1_Control",

  "globalExclusions": {
    "files": [
      "**/Generated/**",
      "**/Test/**"
    ],
    "rules": ["SA0029"]  // TODO 주석 규칙 전역 비활성화
  },

  "ruleOverrides": {
    "SA0033": {
      "enabled": true,
      "severity": "Warning",  // Critical → Warning으로 하향
      "exceptions": [
        {
          "pattern": "GVL_.*",  // GVL 변수는 예외
          "reason": "전역 변수는 외부에서 사용됨"
        }
      ]
    },
    "SA0049": {
      "enabled": true,
      "maxMagicNumberValue": 100,  // 100 이하는 허용
      "allowedValues": [0, 1, -1, 100, 1000]
    }
  },

  "fileOverrides": {
    "POUs/Safety/**": {
      "minSeverity": "Critical",  // Safety는 Critical만 보고
      "strictMode": true
    },
    "POUs/Debug/**": {
      "enabled": false  // 디버그 폴더 제외
    }
  },

  "inlineSuppressions": {
    "enabled": true,
    "commentPatterns": [
      "// qa-ignore: {ruleId}",
      "(* qa-ignore: {ruleId} *)"
    ]
  }
}
```

#### 1.3.2 인라인 억제 지원

```iecst
// 코드 내 예외 처리 예시

// 방법 1: 다음 줄 억제
// qa-ignore: SA0049
nTimeout := 5000;  // 매직 넘버 경고 억제

// 방법 2: 전체 블록 억제
(* qa-ignore-start: SA0033, SA0049 *)
VAR
    tempValue : INT;     // 미사용 변수 경고 억제
    debugFlag : BOOL;
END_VAR
(* qa-ignore-end *)

// 방법 3: 이유와 함께
// qa-ignore: SA0026 - 하드웨어 매핑 필수
MotorOutput AT %QW100 : INT;
```

---

## 2. Level 3: AI 기반 지능형 분석

> Level 3 상세 설계는 이전 응답에서 제공되었습니다.
> 주요 구성요소:
> - Claude API 통합
> - 컨텍스트 수집기
> - 프롬프트 엔지니어링
> - 지능형 필터링
> - 피드백 학습 시스템

### 2.1 Level 2 → Level 3 연동

```csharp
/// <summary>
/// 분석 파이프라인 오케스트레이터
/// </summary>
public class AnalysisPipeline
{
    private readonly Level1PatternMatcher _level1;
    private readonly Level2ASTAnalyzer _level2;
    private readonly Level3AIAnalyzer _level3;
    private readonly PipelineConfig _config;

    public async Task<PipelineResult> AnalyzeAsync(
        FolderComparisonResult comparison,
        PipelineOptions options)
    {
        var result = new PipelineResult();

        // Level 1: 패턴 매칭 (빠름, 높은 오탐)
        Console.WriteLine("[Level 1] 패턴 매칭 분석 시작...");
        var level1Issues = await _level1.AnalyzeAsync(comparison);
        result.Level1Issues = level1Issues;
        Console.WriteLine($"[Level 1] {level1Issues.Count}개 이슈 검출");

        if (!options.EnableLevel2) return result;

        // Level 2: AST 분석 (중간, 중간 정확도)
        Console.WriteLine("[Level 2] AST 기반 분석 시작...");
        var level2Issues = await _level2.AnalyzeAsync(level1Issues);
        result.Level2Issues = level2Issues;

        var filtered = level2Issues
            .Where(i => i.ConfidenceScore >= options.Level2Threshold)
            .ToList();
        Console.WriteLine($"[Level 2] {filtered.Count}개 이슈 통과 (신뢰도 {options.Level2Threshold}%+)");

        if (!options.EnableLevel3) return result;

        // Level 3: AI 분석 (느림, 높은 정확도)
        Console.WriteLine("[Level 3] AI 분석 시작...");

        // 비용 추정 및 확인
        var costEstimate = await _level3.EstimateCostAsync(filtered);
        Console.WriteLine($"[Level 3] 예상 비용: ${costEstimate.OptimizedCost:F4}");

        if (costEstimate.OptimizedCost > options.MaxCostUSD)
        {
            Console.WriteLine($"[Level 3] 비용 제한 초과, Critical 이슈만 분석");
            filtered = filtered.Where(i => i.Severity == Severity.Critical).ToList();
        }

        var level3Issues = await _level3.AnalyzeAsync(filtered);
        result.Level3Issues = level3Issues;

        // 최종 결과: 오탐 제거된 실제 이슈
        result.FinalIssues = level3Issues
            .Where(i => !i.AIResult?.FalsePositive.IsFalsePositive ?? true)
            .Where(i => i.AIResult?.FalsePositive.Confidence >= 70 ?? false)
            .OrderByDescending(i => i.AIResult?.RiskScore ?? 0)
            .ToList();

        Console.WriteLine($"[최종] {result.FinalIssues.Count}개 실제 이슈 확인");

        return result;
    }
}
```

---

## 3. 구현 우선순위 및 로드맵

### 3.1 단계별 구현 계획

```
Phase 1: Level 2 기반 구축 (2주)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
[높음] 기존 AST 인프라 활용하여 분석기 개선
  ├─ DataFlowAnalyzer 구현 (3일)
  ├─ ConfidenceCalculator 구현 (2일)
  └─ 예외 규칙 시스템 (.twincat-qa.json) (3일)

[중간] SA 규칙 AST 기반 재구현
  ├─ 주요 규칙 10개 선별 (SA0001, SA0003, SA0033, ...)
  └─ AST Visitor로 재구현 (5일)

Phase 2: Level 3 핵심 구현 (3주)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
[높음] Claude API 통합
  ├─ API 클라이언트 구현 (3일)
  ├─ Rate Limiter 구현 (1일)
  └─ 비용 관리 시스템 (2일)

[높음] 프롬프트 엔지니어링
  ├─ 시스템 프롬프트 개발 (3일)
  ├─ 도메인별 템플릿 5개 (5일)
  └─ Few-shot 예시 수집 (2일)

[중간] 지능형 필터링
  ├─ 오탐 제거 로직 (3일)
  └─ 위험도 점수화 (2일)

Phase 3: 학습 및 최적화 (2주)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
[중간] 피드백 시스템
  ├─ 피드백 수집 UI (3일)
  ├─ 학습 데이터 저장소 (2일)
  └─ 규칙 가중치 자동 조정 (3일)

[낮음] 배치 최적화
  ├─ 유사 이슈 그룹화 (2일)
  └─ 캐싱 시스템 (2일)

Phase 4: 통합 테스트 (1주)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
[높음] E2E 테스트
  ├─ 실제 프로젝트 검증 (PM1)
  └─ 정확도/비용 벤치마크
```

### 3.2 예상 효과

```
┌────────────────────────────────────────────────────────────────┐
│                      분석 정확도 향상                           │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│  현재 (Level 1만)                                              │
│  ├─ 검출 이슈: 12,480개                                        │
│  ├─ 추정 오탐률: ~70%                                          │
│  └─ 실제 문제: ~3,700개                                        │
│                                                                │
│  Level 2 적용 후                                               │
│  ├─ 검출 이슈: ~3,000개                                        │
│  ├─ 추정 오탐률: ~40%                                          │
│  └─ 실제 문제: ~1,800개                                        │
│                                                                │
│  Level 3 적용 후                                               │
│  ├─ 검출 이슈: ~300개                                          │
│  ├─ 추정 오탐률: ~10%                                          │
│  └─ 실제 문제: ~270개 (정확도 90%)                             │
│                                                                │
│  ✓ 개발자 검토 부담: 97.6% 감소 (12,480 → 300)                │
│  ✓ 실제 문제 발견율: 유지 (False Negative 최소화)             │
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

### 3.3 비용 추정 (Level 3)

```
┌────────────────────────────────────────────────────────────────┐
│                      월간 운영 비용 예상                        │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│  시나리오: 일일 1회 분석, 200개 이슈/회                        │
│                                                                │
│  기본 비용 (최적화 없음)                                       │
│  ├─ 일일: $6.74                                                │
│  └─ 월간: $202                                                 │
│                                                                │
│  최적화 적용 후                                                │
│  ├─ 배치 처리: -35%                                            │
│  ├─ 캐싱: -20%                                                 │
│  ├─ Haiku 혼용: -30%                                           │
│  ├─ 일일: $2.15                                                │
│  └─ 월간: $65                                                  │
│                                                                │
│  ROI 분석                                                      │
│  ├─ 개발자 시간 절감: 40시간/월                               │
│  ├─ 시간당 비용: $50 가정                                      │
│  ├─ 절감 가치: $2,000/월                                      │
│  └─ ROI: 30배 ($65 투자 → $2,000 절감)                        │
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

---

## 4. 기술 스택 요약

```
┌─────────────────────────────────────────────────────────────┐
│                     기술 스택                                │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Level 1 (현재)                                             │
│  ├─ C# .NET 9.0                                             │
│  ├─ System.Text.RegularExpressions                         │
│  └─ 179개 SA 규칙 (패턴 매칭)                               │
│                                                             │
│  Level 2 (확장)                                             │
│  ├─ ANTLR4 (기존 파서 활용)                                 │
│  ├─ AST Visitor 패턴                                        │
│  ├─ 데이터 흐름 분석 (DFA)                                  │
│  └─ JSON 설정 파일                                          │
│                                                             │
│  Level 3 (신규)                                             │
│  ├─ Anthropic Claude API (claude-3-5-sonnet)               │
│  ├─ System.Net.Http (API 클라이언트)                       │
│  ├─ SQLite (학습 데이터 저장)                              │
│  └─ System.Text.Json (응답 파싱)                           │
│                                                             │
│  공통 인프라                                                │
│  ├─ EPPlus (Excel 보고서)                                   │
│  ├─ Microsoft.Extensions.DependencyInjection               │
│  └─ xUnit (테스트)                                          │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## 5. 시작하기

### 5.1 Level 2 먼저 구현 (권장)

Level 2는 외부 API 의존 없이 기존 코드베이스를 확장하여 구현 가능합니다:

1. **DataFlowAnalyzer** 구현
2. **신뢰도 시스템** 추가
3. **예외 규칙** 설정 파일 지원
4. **SA 규칙 10개** AST 기반 재구현

### 5.2 Level 3 추가 (선택)

Level 3는 Claude API 키가 필요하며, 비용이 발생합니다:

1. **환경 변수** 설정 (`ANTHROPIC_API_KEY`)
2. **비용 제한** 설정
3. **프롬프트 템플릿** 개발
4. **피드백 시스템** 구축

---

## 문서 정보

- **작성일**: 2024-11-28
- **버전**: 1.0
- **작성자**: Claude (AI Assistant)
- **프로젝트**: TwinCAT Code QA Tool
