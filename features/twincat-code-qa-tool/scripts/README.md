# TwinCAT QA Tool - 스크립트 모음

이 디렉토리에는 TwinCAT QA Tool의 테스트 및 빌드를 자동화하는 PowerShell 스크립트가 포함되어 있습니다.

## 📋 사용 가능한 스크립트

### 1. `run-e2e-tests.ps1` - E2E 통합 테스트 실행

전체 워크플로우(폴더 비교 → QA 분석 → 리포트 생성)를 검증하는 통합 테스트를 실행합니다.

**기본 사용법:**
```powershell
.\scripts\run-e2e-tests.ps1
```

**옵션:**
```powershell
# Release 구성으로 실행
.\scripts\run-e2e-tests.ps1 -Configuration Release

# 상세 출력
.\scripts\run-e2e-tests.ps1 -Verbose

# 코드 커버리지 수집
.\scripts\run-e2e-tests.ps1 -Coverage

# 특정 테스트만 실행
.\scripts\run-e2e-tests.ps1 -Filter "*워크플로우*"

# 모든 옵션 조합
.\scripts\run-e2e-tests.ps1 -Configuration Release -Verbose -Coverage
```

**출력 정보:**
- 빌드 상태 및 소요 시간
- 테스트 실행 결과 (통과/실패/건너뜀)
- 테스트 요약 (총 개수, 성공률)
- 코드 커버리지 (옵션)
- 성능 메트릭 (소요 시간, 처리량)

---

### 2. `run-performance-benchmark.ps1` - 성능 벤치마크 실행

성능 테스트만 집중적으로 실행하고 결과를 분석합니다.

**기본 사용법:**
```powershell
.\scripts\run-performance-benchmark.ps1
```

**옵션:**
```powershell
# Release 구성으로 실행 (권장)
.\scripts\run-performance-benchmark.ps1 -Configuration Release

# 상세 출력
.\scripts\run-performance-benchmark.ps1 -Verbose

# 결과 저장 디렉토리 지정
.\scripts\run-performance-benchmark.ps1 -OutputDir "my-benchmark-results"
```

**출력 정보:**
- 파일 수별 처리 시간 (10, 50, 100개)
- 파일 크기별 성능 (100, 500, 1000줄)
- 메모리 사용량 분석
- 병렬 처리 성능 향상 비율
- 복잡도별 처리 시간

**벤치마크 결과 해석:**
- **처리량 (파일/초)**: 높을수록 좋음 (최소 1 파일/초 이상)
- **메모리 사용량**: 100개 파일 기준 100MB 이내 권장
- **병렬 처리 향상**: 2x 이상이면 병렬화가 효과적
- **리포트 생성**: 1초 이내 권장

---

## 🚀 빠른 시작

### 전체 E2E 테스트 실행
```powershell
# 기본 실행
.\scripts\run-e2e-tests.ps1

# 상세 모드 + 커버리지
.\scripts\run-e2e-tests.ps1 -Verbose -Coverage
```

### 성능 벤치마크 실행
```powershell
# Release 모드 권장 (최적화된 성능)
.\scripts\run-performance-benchmark.ps1 -Configuration Release
```

### CI/CD 파이프라인 통합
```powershell
# CI/CD에서 사용할 경우
.\scripts\run-e2e-tests.ps1 -Configuration Release -Filter "*E2E*"

# 실패 시 exit code 확인
if ($LASTEXITCODE -ne 0) {
    Write-Error "테스트 실패"
    exit 1
}
```

---

## 📊 결과 파일

### E2E 테스트 결과
- **위치**: `tests\TwinCatQA.Integration.Tests\TestResults\`
- **파일**: `*.trx` (테스트 결과), `coverage.cobertura.xml` (커버리지)

### 벤치마크 결과
- **위치**: `benchmark-results\` (기본값)
- **파일**: `benchmark_result_YYYYMMDD_HHMMSS.txt`

---

## 🔧 요구 사항

- **.NET 8.0 SDK** 이상
- **PowerShell 5.1** 이상 (Windows) 또는 **PowerShell Core 7.0+** (크로스 플랫폼)
- **dotnet CLI** 환경 변수 설정

---

## 💡 팁

### 1. 정확한 성능 측정을 위해
```powershell
# 백그라운드 프로세스 최소화
# Release 구성 사용
# 여러 번 실행하여 평균값 산출
for ($i=1; $i -le 5; $i++) {
    Write-Host "Run $i/5"
    .\scripts\run-performance-benchmark.ps1 -Configuration Release
}
```

### 2. 특정 테스트만 실행
```powershell
# E2EWorkflowTests만
.\scripts\run-e2e-tests.ps1 -Filter "*E2EWorkflowTests*"

# 성능 테스트 제외
.\scripts\run-e2e-tests.ps1 -Filter "*E2E*&Category!=Performance"
```

### 3. 커버리지 리포트 생성
```powershell
# 1. 커버리지 데이터 수집
.\scripts\run-e2e-tests.ps1 -Coverage

# 2. ReportGenerator 설치 (한 번만)
dotnet tool install -g dotnet-reportgenerator-globaltool

# 3. HTML 리포트 생성
reportgenerator `
    -reports:"tests\TwinCatQA.Integration.Tests\TestResults\**\coverage.cobertura.xml" `
    -targetdir:"coverage-report" `
    -reporttypes:"Html;HtmlSummary"

# 4. 브라우저에서 열기
Start-Process "coverage-report\index.html"
```

---

## 🐛 문제 해결

### 빌드 실패
```powershell
# 솔루션 클린 후 재빌드
dotnet clean
dotnet restore
.\scripts\run-e2e-tests.ps1
```

### 테스트 타임아웃
```powershell
# 성능 테스트 제외하고 실행
.\scripts\run-e2e-tests.ps1 -Filter "*E2E*&FullyQualifiedName!~Performance"
```

### 권한 오류 (PowerShell)
```powershell
# 실행 정책 변경 (관리자 권한)
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

---

## 📝 추가 정보

- **전체 테스트 스위트 실행**: 프로젝트 루트의 `build.ps1` 사용
- **문서**: 프로젝트 루트의 `README.md` 참조
- **이슈 리포트**: GitHub Issues 활용

---

**작성일**: 2025-11-25
**버전**: 1.0.0
**유지보수**: TwinCAT QA Tool Team
