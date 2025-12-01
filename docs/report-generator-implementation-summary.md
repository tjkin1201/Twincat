# TwinCAT 코드 품질 검증 도구 - 리포트 생성기 구현 완료

## 개요
TwinCAT 프로젝트의 코드 품질 검증 결과를 시각적으로 표현하는 HTML 리포트 생성기를 구현했습니다.

## 구현된 파일 목록

### 1. 핵심 서비스
- **RazorReportGenerator.cs** (D:\01. Vscode\Twincat\features\twincat-code-qa-tool\src\TwinCatQA.Application\Services)
  - Razor 템플릿 엔진 기반 HTML/PDF 리포트 생성
  - Template Method 패턴 적용
  - 차트 데이터 JSON 직렬화 및 임베드

- **ChartDataBuilder.cs** (D:\01. Vscode\Twincat\features\twincat-code-qa-tool\src\TwinCatQA.Application\Services)
  - Chart.js 호환 차트 데이터 생성
  - 3가지 차트 타입 지원 (Line, Radar, Pie)

- **CodeHighlighter.cs** (D:\01. Vscode\Twincat\features\twincat-code-qa-tool\src\TwinCatQA.Application\Services)
  - Structured Text 코드 하이라이팅
  - 정규식 기반 구문 강조
  - HTML 이스케이프 처리

### 2. 데이터 모델
- **ChartData.cs** (D:\01. Vscode\Twincat\features\twincat-code-qa-tool\src\TwinCatQA.Application\Models)
  - Chart.js 호환 차트 데이터 구조
  - Dataset, ChartDataSet 클래스 정의

### 3. 템플릿 파일
- **report-template.cshtml** (D:\01. Vscode\Twincat\features\twincat-code-qa-tool\src\TwinCatQA.Application\Templates)
  - Razor 뷰 템플릿
  - 부트스트랩 5.3 반응형 디자인
  - Chart.js 차트 렌더링

- **report-styles.css** (D:\01. Vscode\Twincat\features\twincat-code-qa-tool\src\TwinCatQA.Application\Templates)
  - 커스텀 CSS 스타일
  - 심각도별 색상 코드
  - 반응형 및 프린트 친화적 스타일

### 4. 테스트 파일
- **RazorReportGeneratorTests.cs** (D:\01. Vscode\Twincat\features\twincat-code-qa-tool\tests\TwinCatQA.Application.Tests\Services)
  - 리포트 생성기 단위 테스트
  - HTML 생성 검증
  - 차트 데이터 검증

- **ChartDataBuilderTests.cs** (D:\01. Vscode\Twincat\features\twincat-code-qa-tool\tests\TwinCatQA.Application.Tests\Services)
  - 차트 데이터 빌더 단위 테스트
  - 3가지 차트 타입별 테스트
  - 엣지 케이스 검증

## 핵심 기능

### 1. HTML 리포트 생성
```csharp
var generator = new RazorReportGenerator();
await generator.GenerateHtmlReportAsync(session, "report.html");
```

**리포트 구성 요소:**
- 헤더: 프로젝트 정보, 검증 시간, 품질 점수
- 대시보드: 심각도별 통계 카드, 3가지 차트
- 위반 사항 상세: 파일, 라인, 메시지, 제안, 코드 스니펫
- 파일별 요약: 테이블 형식 통계
- 푸터: 리포트 생성 정보

### 2. 3가지 차트 타입

#### A. 품질 점수 추이 (Line Chart)
```csharp
var chartData = generator.CreateQualityTrendChart(sessions);
```
- 시간에 따른 품질 점수 변화 추적
- 여러 세션 비교 가능

#### B. 헌장 준수율 (Radar Chart)
```csharp
var chartData = generator.CreateConstitutionComplianceChart(session);
```
- 8가지 헌장 원칙별 준수율 시각화
- 명확성, 일관성, 단순성, 모듈화, 안전성, 성능, 유지보수성, 표준준수

#### C. 위반 분포 (Pie Chart)
```csharp
var chartData = generator.CreateViolationDistributionChart(session);
```
- 심각도별 위반 개수 비율
- Critical(빨강), High(주황), Medium(노랑), Low(파랑)

### 3. 코드 하이라이팅
```csharp
var highlighted = generator.HighlightCode(code, "st");
```
- Structured Text 키워드 강조
- 주석, 문자열, 숫자 하이라이팅
- HTML 안전 처리

## 디자인 패턴

### Template Method 패턴
```csharp
// RazorReportGenerator의 템플릿 메서드
private ReportModel CreateReportModel(ValidationSession session)
{
    // 1. 차트 데이터 생성
    // 2. 통계 계산
    // 3. 그룹핑
    // 4. 모델 반환
}
```

### 색상 코딩 시스템
```css
--color-critical: #dc3545;  /* 빨강 */
--color-high: #fd7e14;      /* 주황 */
--color-medium: #ffc107;    /* 노랑 */
--color-low: #0dcaf0;       /* 파랑 */
--color-success: #198754;   /* 초록 */
```

## 반응형 디자인

### 데스크톱 (1200px+)
- 3열 차트 그리드
- 4열 통계 카드
- 전체 폭 테이블

### 태블릿 (768px-1199px)
- 2열 차트 그리드
- 2열 통계 카드
- 반응형 테이블

### 모바일 (~767px)
- 1열 레이아웃
- 세로 스택 배치
- 터치 친화적 간격

## PDF 변환 (선택 사항)
```csharp
// PDF 변환은 라이선스 이슈로 NotImplementedException 발생
await generator.GeneratePdfReportAsync(session, "report.pdf");
```

**PDF 구현 옵션:**
- iText7 (AGPL 라이선스, 상업용은 유료)
- PuppeteerSharp (크로미움 기반 HTML to PDF)
- wkhtmltopdf (오픈소스, 외부 바이너리 필요)

## 의존성 패키지
```xml
<PackageReference Include="RazorLight" Version="2.3.0" />
<PackageReference Include="System.Text.Json" Version="8.0.0" />
```

## 사용 예시

### 기본 리포트 생성
```csharp
var session = new ValidationSession
{
    ProjectPath = @"C:\MyProject\Project.tsproj",
    QualityScore = new QualityScore { OverallScore = 85.5 },
    Violations = { /* ... */ }
};

var generator = new RazorReportGenerator();
var reportPath = await generator.GenerateHtmlReportAsync(
    session,
    @"C:\Reports\quality-report.html"
);

// 브라우저에서 리포트 열기
Process.Start(new ProcessStartInfo(reportPath) { UseShellExecute = true });
```

### 차트 데이터만 생성
```csharp
var generator = new RazorReportGenerator();

// 품질 추이
var trendData = generator.CreateQualityTrendChart(sessions);
var trendJson = JsonSerializer.Serialize(trendData);

// 헌장 준수율
var radarData = generator.CreateConstitutionComplianceChart(session);
var radarJson = JsonSerializer.Serialize(radarData);

// 위반 분포
var pieData = generator.CreateViolationDistributionChart(session);
var pieJson = JsonSerializer.Serialize(pieData);
```

## 테스트 커버리지
- RazorReportGenerator: 10개 테스트
  - HTML 생성 검증
  - 차트 데이터 포함 검증
  - 위반 사항 표시 검증
  - 예외 처리 검증

- ChartDataBuilder: 13개 테스트
  - 3가지 차트 타입별 테스트
  - 엣지 케이스 (빈 데이터, null 세션)
  - 색상 코드 검증
  - 정렬 및 그룹핑 검증

## 확장 포인트

### 1. 새로운 차트 추가
```csharp
public ChartData CreateComplexityTrendChart(ValidationSession session)
{
    return new ChartData
    {
        Type = "bar",
        Data = new ChartDataSet { /* ... */ }
    };
}
```

### 2. 커스텀 템플릿
```csharp
var html = await razorEngine.CompileRenderAsync(
    "custom-template.cshtml",
    model
);
```

### 3. 다국어 지원
```csharp
public class LocalizedReportGenerator : RazorReportGenerator
{
    private readonly IStringLocalizer _localizer;

    // 템플릿에서 _localizer 사용
}
```

## 보안 고려사항
- HTML 이스케이프 처리 (XSS 방지)
- 파일 경로 검증
- 템플릿 인젝션 방지
- 출력 디렉토리 권한 확인

## 성능 최적화
- RazorLight 메모리 캐싱
- Chart.js CDN 사용 (로컬 번들 가능)
- 대용량 위반 사항 페이징 (추후 구현 가능)
- 비동기 I/O 사용

## 향후 개선 사항
1. 리포트 템플릿 커스터마이징 UI
2. 리포트 이메일 발송 기능
3. CI/CD 통합 (자동 리포트 생성)
4. 대시보드 대화형 필터링
5. 리포트 비교 기능 (세션 간 차이 분석)
6. PDF 북마크 및 목차 생성
7. 코드 스니펫 다운로드 기능

## 요약
TwinCAT 코드 품질 검증 리포트 생성기가 완성되었습니다. HTML 리포트, Chart.js 차트, 코드 하이라이팅 기능을 모두 구현했으며, 반응형 디자인과 프린트 친화적 스타일을 적용했습니다. PDF 변환은 라이선스 이슈로 선택 사항으로 남겨두었습니다.
