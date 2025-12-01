# Level 2 신뢰도 계산 시스템 가이드

## 개요

TwinCAT QA 도구의 신뢰도 계산 시스템은 각 QA 이슈가 실제 문제일 가능성을 0-100점 척도로 평가합니다.
AST 분석 결과와 컨텍스트 정보를 기반으로 신뢰도를 계산하여 오탐률을 줄이고 리뷰 우선순위를 결정합니다.

## 신뢰도 레벨

### High (90점 이상)
- **의미**: 거의 확실한 문제
- **행동**: 즉시 검토 및 수정 권장
- **예시**: AST에서 명확히 확인된 미사용 변수

### Medium (60-89점)
- **의미**: 검토 필요
- **행동**: 코드 리뷰 시 확인 필요
- **예시**: 데이터 흐름 분석으로 확인된 잠재적 문제

### Low (60점 미만)
- **의미**: 참고용 (오탐 가능성)
- **행동**: 참고만 하거나 낮은 우선순위로 검토
- **예시**: 전역 변수여서 다른 곳에서 사용될 가능성이 있는 경우

## 점수 계산 규칙

### 기본 점수
- 모든 이슈는 **50점**에서 시작

### 가점 요소
| 요소 | 점수 | 설명 |
|------|------|------|
| AST 직접 확인 | +30 | AST 분석으로 명확히 확인된 이슈 |
| 데이터 흐름 확인 | +20 | 데이터 흐름 분석으로 검증됨 |
| 유사 패턴 3회 이상 | +10 | 같은 패턴이 여러 곳에서 발견됨 |
| CRITICAL 심각도 | +5 | 이슈의 심각도가 높음 |

### 감점 요소
| 요소 | 점수 | 설명 |
|------|------|------|
| 컨텍스트 모호함 | -20 | 조건부 컴파일, 복잡한 매크로 등 |
| 외부 참조 가능성 | -15 | 외부 라이브러리나 링크된 프로젝트에서 사용 가능 |
| VAR_INPUT/VAR_OUTPUT | -10 | 인터페이스 변수로 외부에서 사용 가능 |
| 전역 변수(GVL) | -5 | 다른 POU에서 사용될 가능성 |

## 사용 예제

### 1. 기본 사용법

```csharp
using TwinCatQA.Infrastructure.Analysis.Confidence;
using TwinCatQA.Domain.Models.QA;

// AST 분석 결과 생성
var astResult = new ASTAnalysisResult
{
    IsConfirmedByAST = true,
    DataFlowConfirmed = true,
    SimilarOccurrences = 5,
    AmbiguousContext = false,
    PossibleExternalReference = false,
    IsInputOutputVariable = false,
    IsGlobalVariable = false
};

// QA 이슈 (선택사항)
var issue = new QAIssue
{
    RuleId = "UNUSED_VARIABLE",
    Severity = Severity.Warning,
    Category = "코드 품질",
    Title = "미사용 변수",
    Location = "Main.st:15"
};

// 신뢰도 계산
var calculator = new ConfidenceCalculator();
var confidence = calculator.Calculate(astResult, issue);

// 결과 사용
Console.WriteLine($"신뢰도 점수: {confidence.Score}");
Console.WriteLine($"신뢰도 레벨: {confidence.Level}");
Console.WriteLine($"설명: {confidence.GetLevelDescription()}");

foreach (var reason in confidence.Reasons)
{
    Console.WriteLine($"- {reason}");
}
```

**출력 예시:**
```
신뢰도 점수: 90
신뢰도 레벨: High
설명: 거의 확실한 문제
- AST에서 직접 확인된 이슈입니다
- 데이터 흐름 분석을 통해 확인되었습니다
- 유사한 패턴이 5곳에서 발견되었습니다
```

### 2. 간단한 계산

```csharp
var calculator = new ConfidenceCalculator();

// AST 확인 + 데이터 흐름 확인
var confidence = calculator.CalculateSimple(
    isConfirmedByAST: true,
    dataFlowConfirmed: true,
    similarOccurrences: 3
);

// 기본 점수 50 + AST 30 + 데이터흐름 20 + 유사패턴 10 = 110 → 100 (최대값)
Console.WriteLine($"점수: {confidence.Score}"); // 100
```

### 3. 규칙 타입별 기본 설정

```csharp
var calculator = new ConfidenceCalculator();
var issue = new QAIssue { RuleId = "UNUSED_VARIABLE" };

// 규칙 타입에 맞는 기본 AST 결과 생성
var astResult = calculator.CreateDefaultASTResult("UNUSED_VARIABLE", issue);

// 미사용 변수는 AST로 확실히 판단 가능
Console.WriteLine($"AST 확인: {astResult.IsConfirmedByAST}"); // true
Console.WriteLine($"데이터 흐름: {astResult.DataFlowConfirmed}"); // true
```

### 4. 여러 분석 결과 통합

```csharp
var calculator = new ConfidenceCalculator();

// 여러 규칙이 같은 이슈를 다르게 분석한 경우
var astResults = new List<ASTAnalysisResult>
{
    new ASTAnalysisResult { IsConfirmedByAST = true, DataFlowConfirmed = false },
    new ASTAnalysisResult { IsConfirmedByAST = false, DataFlowConfirmed = true },
    new ASTAnalysisResult { IsConfirmedByAST = true, DataFlowConfirmed = true }
};

// 가장 높은 신뢰도를 선택
var confidence = calculator.CalculateAggregate(astResults, issue);
```

### 5. 통계 계산

```csharp
var calculator = new ConfidenceCalculator();
var confidenceResults = new List<ConfidenceResult>();

// 여러 이슈에 대해 신뢰도 계산
foreach (var issue in qaIssues)
{
    var astResult = AnalyzeIssue(issue);
    var confidence = calculator.Calculate(astResult, issue);
    confidenceResults.Add(confidence);
}

// 통계 계산
var stats = calculator.CalculateStatistics(confidenceResults);

Console.WriteLine(stats.ToString());
// 출력: 총 150개 이슈 - High: 45, Medium: 72, Low: 33 (평균: 68점)

Console.WriteLine($"높은 신뢰도 비율: {stats.HighConfidencePercentage:F1}%");
Console.WriteLine($"중간 이상 비율: {stats.MediumOrHighPercentage:F1}%");
```

### 6. 상세 리포트 생성

```csharp
var calculator = new ConfidenceCalculator();
var astResult = new ASTAnalysisResult
{
    IsConfirmedByAST = true,
    DataFlowConfirmed = false,
    SimilarOccurrences = 2,
    IsGlobalVariable = true
};

var confidence = calculator.Calculate(astResult, issue);

// 상세 리포트 출력
Console.WriteLine(confidence.GetDetailedReport());
```

**출력 예시:**
```
=== 신뢰도 분석 결과 ===
점수: 75/100
레벨: Medium (검토 필요)

점수 세부 내역:
  기본 점수: 50
  AST 직접 확인: +30
  전역 변수: -5

판단 근거:
  1. AST에서 직접 확인된 이슈입니다
  2. 전역 변수로 다른 POU에서 사용될 가능성이 있습니다
```

## IQARuleChecker와 연동

### 기존 규칙 체커 확장

```csharp
public class MyCustomRule : IQARuleChecker
{
    private readonly ConfidenceCalculator _confidenceCalculator = new();

    public List<QAIssue> Check(SourceFile sourceFile)
    {
        var issues = new List<QAIssue>();

        // ... 이슈 탐지 로직 ...

        foreach (var detectedIssue in detectedIssues)
        {
            // AST 분석 수행
            var astResult = PerformASTAnalysis(detectedIssue);

            // 신뢰도 계산
            var confidence = _confidenceCalculator.Calculate(astResult, detectedIssue);

            // 낮은 신뢰도 이슈는 필터링 (선택적)
            if (confidence.Level == ConfidenceLevel.Low)
            {
                // 로그만 남기고 스킵
                Logger.LogDebug($"낮은 신뢰도로 스킵: {detectedIssue.Title}");
                continue;
            }

            // 신뢰도 정보를 이슈 Description에 추가
            detectedIssue.Description +=
                $"\n\n[신뢰도: {confidence.Score}점 ({confidence.GetLevelDescription()})]";

            issues.Add(detectedIssue);
        }

        return issues;
    }

    private ASTAnalysisResult PerformASTAnalysis(QAIssue issue)
    {
        var result = new ASTAnalysisResult();

        // 실제 AST 분석 로직 구현
        // 예: ANTLR 파서를 사용한 코드 분석

        result.IsConfirmedByAST = true;
        result.AddNote("ANTLR 파서로 확인됨");

        return result;
    }
}
```

## 실전 예제: 미사용 변수 검출

```csharp
public class UnusedVariableRuleWithConfidence : IQARuleChecker
{
    private readonly ConfidenceCalculator _confidenceCalculator = new();

    public List<QAIssue> Check(SourceFile sourceFile)
    {
        var issues = new List<QAIssue>();

        // 1. 모든 변수 선언 찾기
        var declaredVariables = FindAllVariableDeclarations(sourceFile);

        // 2. 각 변수의 사용 여부 확인
        foreach (var variable in declaredVariables)
        {
            var usageCount = CountVariableUsage(sourceFile, variable.Name);

            if (usageCount == 0)
            {
                // 3. AST 분석 결과 생성
                var astResult = new ASTAnalysisResult
                {
                    IsConfirmedByAST = true, // 파서로 확인
                    DataFlowConfirmed = true, // 사용처 없음 확인
                    SimilarOccurrences = GetSimilarUnusedVariablesCount(sourceFile),

                    // 컨텍스트 확인
                    IsInputOutputVariable = IsIOVariable(variable),
                    IsGlobalVariable = IsGlobalVariable(variable),
                    AmbiguousContext = HasConditionalCompilation(variable),
                    PossibleExternalReference = IsInExternalLibrary(variable)
                };

                // 4. 이슈 생성
                var issue = new QAIssue
                {
                    RuleId = "UNUSED_VARIABLE",
                    Severity = Severity.Warning,
                    Category = "코드 품질",
                    Title = $"미사용 변수: {variable.Name}",
                    Location = $"{sourceFile.FilePath}:{variable.Line}",
                    // ... 기타 정보 ...
                };

                // 5. 신뢰도 계산
                var confidence = _confidenceCalculator.Calculate(astResult, issue);

                // 6. 신뢰도가 중간 이상인 경우만 보고
                if (confidence.IsMediumOrHighConfidence)
                {
                    issue.Description += $"\n\n{confidence.GetSummary()}";
                    issues.Add(issue);
                }
            }
        }

        return issues;
    }
}
```

## 팁과 모범 사례

### 1. 신뢰도 임계값 설정
```csharp
// 프로젝트 설정에 따라 임계값 조정
public class QASettings
{
    public int MinimumConfidenceScore { get; set; } = 60; // Medium 이상
    public bool ShowLowConfidenceIssues { get; set; } = false;
}

// 필터링
var filteredIssues = issues
    .Where(i => i.ConfidenceScore >= settings.MinimumConfidenceScore)
    .ToList();
```

### 2. 점수 세부 내역 로깅
```csharp
var confidence = calculator.Calculate(astResult, issue);

// 디버깅용 상세 정보
Logger.LogDebug(confidence.GetDetailedReport());

// 점수 세부 내역 검증
if (!confidence.ValidateScoreBreakdown())
{
    Logger.LogWarning("점수 계산 오류!");
}
```

### 3. 컨텍스트 정보 활용
```csharp
var astResult = new ASTAnalysisResult();

// 추가 정보 저장
astResult.AddContext("VariableScope", "Local");
astResult.AddContext("DeclarationType", "VAR");
astResult.AddContext("UsageCount", 0);

astResult.AddNote("조건부 컴파일 블록 내부에 위치");
```

### 4. 규칙별 가중치 커스터마이징
특정 규칙에 대해 더 높은/낮은 신뢰도를 부여하고 싶다면 ConfidenceCalculator를 상속하여 커스터마이징:

```csharp
public class CustomConfidenceCalculator : ConfidenceCalculator
{
    public ConfidenceResult CalculateForRule(string ruleId, ASTAnalysisResult astResult, QAIssue issue)
    {
        var result = Calculate(astResult, issue);

        // 특정 규칙에 대해 가중치 조정
        if (ruleId == "CRITICAL_SAFETY_ISSUE")
        {
            result.Score = Math.Min(result.Score + 10, 100);
            result.DetermineLevelFromScore();
            result.AddReason("안전 관련 규칙으로 신뢰도 상향 조정");
        }

        return result;
    }
}
```

## 문제 해결

### Q: 모든 이슈가 낮은 신뢰도로 나옵니다.
A: AST 분석이 제대로 수행되고 있는지 확인하세요. `IsConfirmedByAST`와 `DataFlowConfirmed`가 false이면 기본 점수 50점에서 감점만 적용됩니다.

### Q: 전역 변수 이슈의 신뢰도가 너무 낮습니다.
A: 전역 변수는 다른 곳에서 사용될 가능성이 있어 자동으로 -5점이 적용됩니다. 실제 사용 여부를 전체 프로젝트 스코프에서 확인하면 신뢰도를 높일 수 있습니다.

### Q: 신뢰도를 더 세밀하게 조정하고 싶습니다.
A: `ConfidenceCalculator`를 상속하여 점수 가중치 상수를 재정의하거나, 커스텀 계산 로직을 추가할 수 있습니다.

## 참고

- **네임스페이스**: `TwinCatQA.Infrastructure.Analysis.Confidence`
- **주요 클래스**:
  - `ConfidenceCalculator`: 신뢰도 계산기
  - `ConfidenceResult`: 계산 결과
  - `ASTAnalysisResult`: AST 분석 중간 결과
  - `ConfidenceLevel`: 신뢰도 레벨 (High/Medium/Low)

## 다음 단계

1. 기존 규칙 체커에 신뢰도 계산 통합
2. 리포트에 신뢰도 정보 표시
3. CI/CD 파이프라인에서 신뢰도 기반 필터링 적용
4. 신뢰도 통계를 대시보드에 표시
