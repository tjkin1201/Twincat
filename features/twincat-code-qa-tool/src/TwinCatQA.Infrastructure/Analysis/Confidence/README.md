# Confidence (신뢰도 계산 시스템)

## 개요

QA 이슈의 신뢰도를 0-100점 척도로 계산하여 오탐 가능성을 판단하고 리뷰 우선순위를 결정하는 시스템입니다.

## 파일 구성

### 1. ASTAnalysisResult.cs (158줄)
AST 분석 중간 결과를 저장하는 데이터 클래스입니다.

**주요 속성:**
- `IsConfirmedByAST`: AST로 직접 확인 여부 (+30점)
- `DataFlowConfirmed`: 데이터 흐름 분석 확인 (+20점)
- `SimilarOccurrences`: 유사 패턴 발견 횟수 (3회 이상 +10점)
- `AmbiguousContext`: 컨텍스트 모호함 (-20점)
- `PossibleExternalReference`: 외부 참조 가능성 (-15점)
- `IsInputOutputVariable`: VAR_INPUT/VAR_OUTPUT 변수 (-10점)
- `IsGlobalVariable`: 전역 변수(GVL) (-5점)

**주요 메서드:**
- `AddNote(string note)`: 분석 노트 추가
- `AddContext(string key, object value)`: 추가 컨텍스트 정보 저장
- `GetPositiveFactorsCount()`: 긍정적 요소 개수 계산
- `GetNegativeFactorsCount()`: 부정적 요소 개수 계산

### 2. ConfidenceResult.cs (224줄)
신뢰도 계산 결과를 저장하는 클래스입니다.

**주요 속성:**
- `Score`: 신뢰도 점수 (0-100)
- `Level`: 신뢰도 레벨 (High/Medium/Low)
- `Reasons`: 판단 근거 목록
- `ScoreBreakdown`: 점수 계산 세부 내역

**신뢰도 레벨:**
- `High` (90점 이상): 거의 확실한 문제, 즉시 수정 권장
- `Medium` (60-89점): 검토 필요
- `Low` (60점 미만): 참고용, 오탐 가능성

**주요 메서드:**
- `AddReason(string reason)`: 판단 근거 추가
- `AddScoreBreakdown(string factor, int scoreChange)`: 점수 세부 내역 추가
- `DetermineLevelFromScore()`: 점수 기반 레벨 자동 결정
- `GetSummary()`: 요약 정보 반환
- `GetDetailedReport()`: 상세 리포트 생성
- `ValidateScoreBreakdown()`: 점수 세부 내역 검증

### 3. ConfidenceCalculator.cs (333줄)
신뢰도를 계산하는 핵심 클래스입니다.

**점수 계산 규칙:**

기본 점수: 50점

가점:
- AST 직접 확인: +30점
- 데이터 흐름 확인: +20점
- 유사 패턴 3회 이상: +10점
- CRITICAL 심각도: +5점

감점:
- 컨텍스트 모호함: -20점
- 외부 참조 가능성: -15점
- VAR_INPUT/VAR_OUTPUT: -10점
- 전역 변수(GVL): -5점

**주요 메서드:**
- `Calculate(ASTAnalysisResult, QAIssue?)`: 기본 신뢰도 계산
- `CalculateSimple(bool, bool, int)`: 간단한 신뢰도 계산
- `CalculateAggregate(List<ASTAnalysisResult>, QAIssue?)`: 여러 분석 결과 통합
- `CreateDefaultASTResult(string ruleType, QAIssue)`: 규칙 타입별 기본 AST 결과 생성
- `CalculateStatistics(List<ConfidenceResult>)`: 신뢰도 통계 계산

**지원되는 규칙 타입:**
- `UNUSED_VARIABLE`: 높은 신뢰도 (AST + 데이터 흐름 확인)
- `ARRAY_BOUNDS`: 높은 신뢰도 (AST 확인)
- `MAGIC_NUMBER`: 높은 신뢰도 (AST 확인)
- `DEAD_CODE`: 중간 신뢰도 (조건부 컴파일 가능성)
- 기타: 보수적 설정 (낮은 신뢰도)

## 사용 예제

### 기본 사용

```csharp
using TwinCatQA.Infrastructure.Analysis.Confidence;
using TwinCatQA.Domain.Models.QA;

// AST 분석 결과 생성
var astResult = new ASTAnalysisResult
{
    IsConfirmedByAST = true,
    DataFlowConfirmed = true,
    SimilarOccurrences = 5
};

// 신뢰도 계산
var calculator = new ConfidenceCalculator();
var confidence = calculator.Calculate(astResult);

// 결과 확인
Console.WriteLine($"점수: {confidence.Score}점");
Console.WriteLine($"레벨: {confidence.Level}");
```

### 규칙 체커 통합

```csharp
public class MyRule : IQARuleChecker
{
    private readonly ConfidenceCalculator _calculator = new();

    public List<QAIssue> Check(SourceFile sourceFile)
    {
        var issues = new List<QAIssue>();

        foreach (var detectedIssue in FindIssues(sourceFile))
        {
            var astResult = AnalyzeWithAST(detectedIssue);
            var confidence = _calculator.Calculate(astResult, detectedIssue);

            // 중간 신뢰도 이상만 보고
            if (confidence.IsMediumOrHighConfidence)
            {
                detectedIssue.Description += $"\n신뢰도: {confidence.GetSummary()}";
                issues.Add(detectedIssue);
            }
        }

        return issues;
    }
}
```

## 통합 방법

### 1. IQARuleChecker와 연동

각 규칙 체커에서 AST 분석을 수행하고 신뢰도를 계산합니다:

```csharp
// AST 분석 결과 생성
var astResult = new ASTAnalysisResult();
astResult.IsConfirmedByAST = CheckWithParser(code);
astResult.DataFlowConfirmed = AnalyzeDataFlow(variable);
astResult.IsGlobalVariable = IsInGVL(variable);

// 신뢰도 계산
var confidence = calculator.Calculate(astResult, issue);
```

### 2. 리포트에 신뢰도 표시

```csharp
// 이슈 리포트 생성 시
foreach (var issue in issues)
{
    var confidence = issue.Confidence; // 이미 계산된 신뢰도
    Console.WriteLine($"{issue.Title} - {confidence.GetSummary()}");
}
```

### 3. 필터링 적용

```csharp
// 높은 신뢰도 이슈만 필터링
var highConfidenceIssues = issues
    .Where(i => i.Confidence.IsHighConfidence)
    .ToList();

// 설정 가능한 임계값
var filteredIssues = issues
    .Where(i => i.Confidence.Score >= settings.MinConfidenceScore)
    .ToList();
```

## 테스트

단위 테스트: `tests/TwinCatQA.Infrastructure.Tests/Analysis/ConfidenceCalculatorTests.cs`

테스트 커버리지:
- 기본 점수 계산
- 가점/감점 요소
- 신뢰도 레벨 결정
- 복합 시나리오
- 통계 계산
- 검증 메서드

## 참고 문서

상세 가이드: `docs/confidence-system-guide.md`

## 성능 특성

- **시간 복잡도**: O(1) - 단순 점수 계산
- **메모리**: 최소화 (구조체 사용 권장)
- **스레드 안전성**: ConfidenceCalculator는 상태 없음 (thread-safe)

## 향후 개선 사항

1. 머신러닝 기반 신뢰도 조정
2. 프로젝트별 가중치 커스터마이징
3. 이력 기반 신뢰도 학습
4. 실시간 피드백 반영

## 라이선스

TwinCAT QA Tool의 라이선스를 따릅니다.
