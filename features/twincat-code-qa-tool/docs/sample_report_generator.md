# SimpleHtmlReportGenerator 사용 예제

## 개요

이 문서는 `SimpleHtmlReportGenerator`를 사용하여 QA 보고서를 생성하는 방법을 설명합니다.

## 주요 기능

1. **HTML 리포트 생성** - GitHub PR 스타일의 인라인 코멘트 UI
2. **Markdown 리포트 생성** - GitHub README 스타일
3. **JSON 리포트 생성** - CI/CD 파이프라인 통합용
4. **차트 데이터 생성** - Chart.js 호환 데이터

## 사용 방법

### 1. SimpleHtmlReportGenerator 생성

```csharp
using TwinCatQA.Domain.Contracts;
using TwinCatQA.Infrastructure.Reports;

IReportGenerator generator = new SimpleHtmlReportGenerator();
```

### 2. ValidationSession 준비

```csharp
var session = new ValidationSession
{
    ProjectName = "MyProject",
    ProjectPath = @"C:\Projects\MyProject\MyProject.tsproj",
    Mode = ValidationMode.Full
};

// 위반 사항 추가
session.Violations.Add(new Violation
{
    RuleId = "SAFETY-001",
    RuleName = "배열 경계 검사 누락",
    Severity = ViolationSeverity.Critical,
    FilePath = @"C:\Projects\MyProject\POUs\MAIN.TcPOU",
    Line = 42,
    Column = 5,
    Message = "배열 인덱스에 대한 경계 검사가 없습니다.",
    Suggestion = "IF index >= 0 AND index < SIZEOF(arr) THEN\n    arr[index] := value;\nEND_IF"
});

session.Complete();
session.CalculateQualityScore();
```

### 3. HTML 리포트 생성

```csharp
string htmlPath = generator.GenerateHtmlReport(session, "reports/qa_report.html");
Console.WriteLine($"HTML 리포트 생성됨: {htmlPath}");
```

## HTML 리포트 특징

### GitHub PR 스타일 UI

- **반응형 디자인**: 모바일, 태블릿, 데스크톱 지원
- **색상 구분**: Critical(빨강), High(주황), Medium(노랑), Low(파랑)
- **접기/펼치기**: 파일별로 이슈 접기/펼치기 가능
- **코드 스니펫**: 문제가 발생한 코드 라인 표시
- **권장 해결 방법**: 각 이슈별 구체적인 수정 제안

### 요약 카드

- Critical 위반 수
- High 위반 수
- Medium 위반 수
- Low 위반 수
- 품질 점수 (0-100)
- 총 위반 수

### 파일별 이슈 그룹화

- 파일 경로별로 이슈를 그룹화
- 심각도별로 정렬
- 라인 번호 및 컬럼 정보 표시

## 의존성 주입 사용

```csharp
using Microsoft.Extensions.DependencyInjection;
using TwinCatQA.Infrastructure;

var services = new ServiceCollection();
services.AddInfrastructure(); // SimpleHtmlReportGenerator 자동 등록

var serviceProvider = services.BuildServiceProvider();
var generator = serviceProvider.GetRequiredService<IReportGenerator>();

string htmlPath = generator.GenerateHtmlReport(session);
```

## 테스트 실행

```bash
cd tests/TwinCatQA.Infrastructure.Tests
dotnet test --filter "FullyQualifiedName~SimpleHtmlReportGeneratorTests"
```

## 출력 예시

생성된 HTML 파일은 다음과 같은 구조를 가집니다:

```
<!DOCTYPE html>
<html lang="ko">
<head>
    <meta charset="UTF-8">
    <title>TwinCAT 코드 QA 보고서</title>
    <style>...</style>
</head>
<body>
    <div class="container">
        <!-- 헤더 -->
        <div class="header">...</div>

        <!-- 요약 카드 -->
        <div class="summary-cards">...</div>

        <!-- 파일별 이슈 -->
        <div class="file-section">...</div>
    </div>
</body>
</html>
```

## 기타 메서드

### CreateQualityTrendChart

```csharp
var sessions = new List<ValidationSession> { session1, session2, session3 };
ChartData chartData = generator.CreateQualityTrendChart(sessions);
```

### CreateConstitutionComplianceChart

```csharp
ChartData chartData = generator.CreateConstitutionComplianceChart(session);
```

### CreateViolationDistributionChart

```csharp
ChartData chartData = generator.CreateViolationDistributionChart(session);
```

## 주의사항

1. **PDF 생성 미지원**: `GeneratePdfReport()`는 `NotImplementedException`을 발생시킵니다.
2. **템플릿 커스터마이징 미지원**: `SetCustomTemplate()`는 `NotImplementedException`을 발생시킵니다.
3. Razor 템플릿 기반의 고급 기능이 필요한 경우 `RazorReportGenerator`를 사용하세요.
