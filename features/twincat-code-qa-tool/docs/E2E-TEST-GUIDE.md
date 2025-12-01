# E2E 통합 테스트 가이드

TwinCAT QA Tool의 End-to-End 통합 테스트 실행 및 검증 가이드입니다.

## 📋 목차

1. [개요](#개요)
2. [테스트 구조](#테스트-구조)
3. [실행 방법](#실행-방법)
4. [테스트 항목](#테스트-항목)
5. [성능 벤치마크](#성능-벤치마크)
6. [문제 해결](#문제-해결)

---

## 개요

### E2E 테스트란?

E2E(End-to-End) 테스트는 TwinCAT QA Tool의 전체 워크플로우를 실제 사용 시나리오처럼 검증합니다:

```
입력 (Old/New 폴더)
    ↓
[1] 폴더 비교 (FolderComparer)
    ↓
[2] QA 분석 (QaAnalysisService + QARuleEngine)
    ↓
[3] 리포트 생성 (QaReportGenerator)
    ↓
출력 (HTML/MD/JSON 리포트)
```

### 테스트 파일 위치

```
tests/TwinCatQA.Integration.Tests/
├── E2EWorkflowTests.cs              # 전체 워크플로우 테스트
├── PerformanceBenchmarkTests.cs     # 성능 벤치마크 테스트
├── AdvancedFeaturesIntegrationTests.cs
├── RealProjectValidationTests.cs
└── RealProjectParsingTests.cs
```

---

## 테스트 구조

### 1. E2EWorkflowTests.cs

**전체 워크플로우 검증 테스트 (15개 테스트)**

#### 전체 워크플로우 테스트 (4개)
- `전체워크플로우_폴더비교부터리포트생성까지_성공`
  - 폴더 비교 → QA 분석 → 리포트 생성 전체 과정 검증
  - HTML, Markdown, JSON 리포트 파일 생성 확인
  - 리포트 내용 유효성 검증

- `전체워크플로우_위험한변경감지_Critical이슈보고`
  - 위험한 타입 축소(DINT → INT, LREAL → REAL) 감지
  - Critical 이슈 생성 및 권장사항 포함 확인

- `전체워크플로우_규칙필터링_특정규칙만실행`
  - QA 규칙 필터링 (IncludeRules 옵션)
  - 특정 규칙만 실행되는지 검증

- `전체워크플로우_규칙제외_특정규칙제외실행`
  - QA 규칙 제외 (ExcludeRules 옵션)
  - 제외된 규칙이 실행되지 않는지 검증

#### 성능 테스트 (2개)
- `성능테스트_전체분석_5초이내`
  - 소규모 프로젝트 분석이 5초 이내 완료

- `성능테스트_대규모파일_처리성능`
  - 50개 파일 처리 성능 측정
  - 처리량(파일/초) 측정

#### 리포트 생성 테스트 (4개)
- `리포트출력_HTML파일생성_성공`
  - HTML 구조 및 스타일 검증

- `리포트출력_Markdown파일생성_성공`
  - Markdown 형식 검증

- `리포트출력_JSON파일생성_유효한JSON`
  - JSON 파싱 및 구조 검증

- `리포트출력_모든형식_동시생성`
  - ReportFormat.All로 3개 형식 동시 생성

#### 에러 처리 테스트 (2개)
- `에러처리_존재하지않는폴더_실패결과반환`
- `에러처리_빈폴더_변경없음결과`

### 2. PerformanceBenchmarkTests.cs

**성능 및 메모리 사용량 벤치마크 (11개 테스트)**

#### 파일 수별 성능 (3개)
- `성능벤치마크_파일수별_처리시간측정(10개, 2초 이내)`
- `성능벤치마크_파일수별_처리시간측정(50개, 8초 이내)`
- `성능벤치마크_파일수별_처리시간측정(100개, 15초 이내)`

#### 파일 크기별 성능 (3개)
- `성능벤치마크_파일크기별_처리시간측정(100줄, 1초 이내)`
- `성능벤치마크_파일크기별_처리시간측정(500줄, 3초 이내)`
- `성능벤치마크_파일크기별_처리시간측정(1000줄, 6초 이내)`

#### 메모리 사용량 (2개)
- `메모리사용량_대규모프로젝트_100MB이내`
  - 100개 파일 처리 시 메모리 100MB 이내

- `메모리누수_반복실행_메모리안정성`
  - 10회 반복 실행 후 메모리 증가량 10MB 미만

#### 기타 성능 (3개)
- `리포트생성성능_모든형식_1초이내`
- `병렬처리_복수프로젝트_동시분석`
- `복잡도성능_중첩깊이별_처리시간`

---

## 실행 방법

### 1. 기본 실행

```bash
# 모든 E2E 테스트 실행
dotnet test tests/TwinCatQA.Integration.Tests/TwinCatQA.Integration.Tests.csproj --filter "FullyQualifiedName~E2E"

# E2EWorkflowTests만 실행
dotnet test tests/TwinCatQA.Integration.Tests/TwinCatQA.Integration.Tests.csproj --filter "FullyQualifiedName~E2EWorkflowTests"

# PerformanceBenchmarkTests만 실행
dotnet test tests/TwinCatQA.Integration.Tests/TwinCatQA.Integration.Tests.csproj --filter "FullyQualifiedName~PerformanceBenchmarkTests"
```

### 2. PowerShell 스크립트 사용 (권장)

```powershell
# 전체 E2E 테스트 + 리포트
.\scripts\run-e2e-tests.ps1

# 상세 출력 + 코드 커버리지
.\scripts\run-e2e-tests.ps1 -Verbose -Coverage

# Release 모드 (성능 최적화)
.\scripts\run-e2e-tests.ps1 -Configuration Release

# 성능 벤치마크 전용
.\scripts\run-performance-benchmark.ps1 -Configuration Release
```

### 3. Visual Studio에서 실행

1. **Test Explorer** 열기 (`Ctrl+E, T`)
2. **E2EWorkflowTests** 또는 **PerformanceBenchmarkTests** 선택
3. **Run All** 또는 개별 테스트 실행

---

## 테스트 항목

### ✅ 기능 테스트

#### 1. 전체 워크플로우 검증
- [x] 폴더 비교 성공
- [x] QA 규칙 적용
- [x] 변경사항 감지 (변수, 로직, 데이터 타입)
- [x] 이슈 생성 (Critical, Warning, Info)
- [x] 리포트 생성 (HTML, MD, JSON)

#### 2. 위험한 변경 감지
- [x] 타입 축소 (DINT → INT, LREAL → REAL)
- [x] Critical 이슈 분류
- [x] 권장 해결 방법 제공

#### 3. 규칙 필터링
- [x] IncludeRules: 특정 규칙만 실행
- [x] ExcludeRules: 특정 규칙 제외
- [x] MinSeverity: 심각도 필터링

#### 4. 리포트 형식
- [x] HTML: 스타일 포함, 브라우저에서 바로 열림
- [x] Markdown: GitHub 호환, 문서화 용이
- [x] JSON: 자동화 파이프라인 통합 가능

#### 5. 에러 처리
- [x] 존재하지 않는 폴더 → 실패 결과 반환
- [x] 빈 폴더 → 변경 없음 결과
- [x] 예외 발생 시 ErrorMessage 포함

### ⚡ 성능 테스트

#### 성능 목표

| 항목 | 목표 | 측정 방법 |
|------|------|-----------|
| 10개 파일 | 2초 이내 | `성능벤치마크_파일수별_처리시간측정(10, 2.0)` |
| 50개 파일 | 8초 이내 | `성능벤치마크_파일수별_처리시간측정(50, 8.0)` |
| 100개 파일 | 15초 이내 | `성능벤치마크_파일수별_처리시간측정(100, 15.0)` |
| 처리량 | 최소 1 파일/초 | 모든 벤치마크 |
| 메모리 | 100MB 이내 (100개 파일) | `메모리사용량_대규모프로젝트_100MB이내` |
| 메모리 누수 | 10회 반복 후 10MB 미만 증가 | `메모리누수_반복실행_메모리안정성` |
| 리포트 생성 | 1초 이내 (3개 형식) | `리포트생성성능_모든형식_1초이내` |

#### 성능 메트릭

벤치마크 실행 후 다음 메트릭이 출력됩니다:

```
📊 성능 벤치마크 결과 (50개 파일):
  ⏱️  소요 시간: 5.23초 (목표: 8.0초)
  🚀 처리량: 9.6 파일/초
  💾 메모리 사용: 45.2 MB
  📈 변경 감지: 150건
  🚨 이슈 발견: 23건
```

---

## 성능 벤치마크

### 실행 가이드

```powershell
# Release 모드로 벤치마크 실행 (권장)
.\scripts\run-performance-benchmark.ps1 -Configuration Release -Verbose

# 결과 저장 디렉토리 지정
.\scripts\run-performance-benchmark.ps1 -OutputDir "my-benchmark-results"
```

### 벤치마크 결과 해석

#### 1. 처리량 (파일/초)
- **좋음**: 5 파일/초 이상
- **보통**: 1-5 파일/초
- **느림**: 1 파일/초 미만

#### 2. 메모리 사용량
- **좋음**: 50MB 이하 (100개 파일 기준)
- **보통**: 50-100MB
- **과다**: 100MB 이상

#### 3. 병렬 처리 효율
- **좋음**: 2x 이상 성능 향상
- **보통**: 1.5-2x
- **비효율**: 1.5x 미만

#### 4. 메모리 누수
- **안정적**: 10회 반복 후 10MB 미만 증가
- **주의**: 10-50MB 증가
- **문제**: 50MB 이상 증가

### 벤치마크 결과 예시

```
📊 성능 벤치마크 결과 (10개 파일):
  ⏱️  소요 시간: 1.45초 (목표: 2초)
  🚀 처리량: 6.9 파일/초
  💾 메모리 사용: 12.3 MB
  📈 변경 감지: 30건
  🚨 이슈 발견: 8건

📊 성능 벤치마크 결과 (50개 파일):
  ⏱️  소요 시간: 6.12초 (목표: 8초)
  🚀 처리량: 8.2 파일/초
  💾 메모리 사용: 43.7 MB
  📈 변경 감지: 150건
  🚨 이슈 발견: 35건

💾 메모리 사용량 분석:
  - 이전: 125.34 MB
  - 이후: 168.21 MB
  - 사용량: 42.87 MB
  - 파일당 평균: 0.429 MB

🔀 병렬 처리 성능 (3개 프로젝트):
  순차 실행: 9.56초
  병렬 실행: 4.12초
  성능 향상: 2.32x
```

---

## 문제 해결

### 1. 빌드 실패

```powershell
# 솔루션 클린 후 재빌드
dotnet clean
dotnet restore
dotnet build tests/TwinCatQA.Integration.Tests/TwinCatQA.Integration.Tests.csproj
```

### 2. 테스트 타임아웃

일부 성능 테스트가 타임아웃되는 경우:

```powershell
# 성능 테스트 제외하고 기능 테스트만 실행
.\scripts\run-e2e-tests.ps1 -Filter "*E2E*&FullyQualifiedName!~Performance"

# 또는 개별 테스트 실행
dotnet test --filter "FullyQualifiedName~전체워크플로우_폴더비교부터리포트생성까지_성공"
```

### 3. 메모리 부족

대규모 벤치마크 실행 시 메모리 부족:

```powershell
# 파일 수 줄여서 실행
# PerformanceBenchmarkTests.cs에서 [InlineData] 수정
# 예: [InlineData(100, 15.0)] → [InlineData(50, 8.0)]
```

### 4. 테스트 데이터 정리

테스트 후 임시 파일이 남아있는 경우:

```bash
# Windows
Remove-Item -Path $env:TEMP\qa_* -Recurse -Force -ErrorAction SilentlyContinue

# Linux/Mac
rm -rf /tmp/qa_*
```

### 5. 코드 커버리지 리포트 생성 실패

```powershell
# ReportGenerator 설치
dotnet tool install -g dotnet-reportgenerator-globaltool

# 커버리지 수집
.\scripts\run-e2e-tests.ps1 -Coverage

# 리포트 생성
reportgenerator `
    -reports:"tests\TwinCatQA.Integration.Tests\TestResults\**\coverage.cobertura.xml" `
    -targetdir:"coverage-report" `
    -reporttypes:"Html;HtmlSummary"

# 브라우저에서 열기
Start-Process "coverage-report\index.html"
```

---

## 추가 정보

### CI/CD 통합

GitHub Actions, Azure DevOps, Jenkins 등에서 사용:

```yaml
# .github/workflows/e2e-tests.yml 예시
name: E2E Tests

on: [push, pull_request]

jobs:
  e2e-tests:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'

      - name: Run E2E Tests
        run: |
          dotnet test tests/TwinCatQA.Integration.Tests/TwinCatQA.Integration.Tests.csproj `
            --filter "FullyQualifiedName~E2E" `
            --logger "trx" `
            --collect "XPlat Code Coverage"

      - name: Upload Test Results
        uses: actions/upload-artifact@v3
        with:
          name: test-results
          path: tests/TwinCatQA.Integration.Tests/TestResults/
```

### 테스트 커버리지 목표

- **E2EWorkflowTests**: 전체 워크플로우 커버리지 80% 이상
- **PerformanceBenchmarkTests**: 성능 회귀 방지

### 참고 자료

- [xUnit 문서](https://xunit.net/)
- [FluentAssertions 문서](https://fluentassertions.com/)
- [프로젝트 README](../README.md)
- [빌드 가이드](../BUILD.md)

---

**작성일**: 2025-11-25
**버전**: 1.0.0
**유지보수**: TwinCAT QA Tool Team
