# E2E 통합 테스트 실행 스크립트
# TwinCAT QA Tool의 전체 워크플로우 테스트를 실행하고 결과를 요약합니다.

param(
    [string]$Configuration = "Release",
    [switch]$Verbose,
    [switch]$Coverage,
    [string]$Filter = "*E2E*"
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# 색상 출력 함수
function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

function Write-Header {
    param([string]$Title)
    Write-ColorOutput "`n═══════════════════════════════════════════════════════════════" "Cyan"
    Write-ColorOutput "  $Title" "Cyan"
    Write-ColorOutput "═══════════════════════════════════════════════════════════════" "Cyan"
}

function Write-Section {
    param([string]$Title)
    Write-ColorOutput "`n───────────────────────────────────────────────────────────────" "Yellow"
    Write-ColorOutput "  📌 $Title" "Yellow"
    Write-ColorOutput "───────────────────────────────────────────────────────────────" "Yellow"
}

function Write-Success {
    param([string]$Message)
    Write-ColorOutput "✅ $Message" "Green"
}

function Write-Error {
    param([string]$Message)
    Write-ColorOutput "❌ $Message" "Red"
}

function Write-Info {
    param([string]$Message)
    Write-ColorOutput "ℹ️  $Message" "Cyan"
}

# 스크립트 시작
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
$testProject = Join-Path $projectRoot "tests\TwinCatQA.Integration.Tests\TwinCatQA.Integration.Tests.csproj"

Write-Header "🔍 TwinCAT QA Tool - E2E 통합 테스트 실행"

Write-Info "프로젝트 루트: $projectRoot"
Write-Info "빌드 구성: $Configuration"
Write-Info "테스트 필터: $Filter"

# 1단계: 프로젝트 빌드
Write-Section "1️⃣  프로젝트 빌드"

try {
    $buildStartTime = Get-Date

    Write-Info "솔루션 복원 중..."
    dotnet restore "$projectRoot\TwinCatQA.sln" --verbosity quiet

    Write-Info "프로젝트 빌드 중..."
    $buildOutput = dotnet build "$projectRoot\TwinCatQA.sln" `
        --configuration $Configuration `
        --no-restore `
        --verbosity minimal 2>&1

    if ($LASTEXITCODE -ne 0) {
        Write-Error "빌드 실패"
        Write-Host $buildOutput
        exit 1
    }

    $buildTime = (Get-Date) - $buildStartTime
    Write-Success "빌드 완료 (소요 시간: $($buildTime.TotalSeconds.ToString('F2'))초)"
}
catch {
    Write-Error "빌드 중 오류 발생: $_"
    exit 1
}

# 2단계: E2E 테스트 실행
Write-Section "2️⃣  E2E 통합 테스트 실행"

try {
    $testStartTime = Get-Date

    # 테스트 인자 구성
    $testArgs = @(
        "test"
        $testProject
        "--configuration", $Configuration
        "--no-build"
        "--logger", "trx"
        "--logger", "console;verbosity=normal"
        "--filter", $Filter
    )

    if ($Verbose) {
        $testArgs += "--verbosity", "detailed"
    }
    else {
        $testArgs += "--verbosity", "normal"
    }

    if ($Coverage) {
        Write-Info "코드 커버리지 수집 활성화"
        $testArgs += "--collect", "XPlat Code Coverage"
    }

    Write-Info "테스트 실행 중... (필터: $Filter)"
    Write-Host ""

    $testOutput = dotnet @testArgs 2>&1
    $testExitCode = $LASTEXITCODE

    # 테스트 출력 표시
    $testOutput | ForEach-Object {
        if ($_ -match "Passed!") {
            Write-ColorOutput $_ "Green"
        }
        elseif ($_ -match "Failed!") {
            Write-ColorOutput $_ "Red"
        }
        elseif ($_ -match "Skipped") {
            Write-ColorOutput $_ "Yellow"
        }
        else {
            Write-Host $_
        }
    }

    $testTime = (Get-Date) - $testStartTime

    if ($testExitCode -eq 0) {
        Write-Success "모든 테스트 통과! (소요 시간: $($testTime.TotalSeconds.ToString('F2'))초)"
    }
    else {
        Write-Error "일부 테스트 실패"
        exit 1
    }
}
catch {
    Write-Error "테스트 실행 중 오류 발생: $_"
    exit 1
}

# 3단계: 테스트 결과 요약
Write-Section "3️⃣  테스트 결과 요약"

try {
    # TRX 파일 찾기
    $trxFiles = Get-ChildItem -Path "$projectRoot\tests\TwinCatQA.Integration.Tests\TestResults" `
        -Filter "*.trx" `
        -Recurse `
        -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if ($trxFiles) {
        Write-Info "테스트 결과 파일: $($trxFiles.FullName)"

        # TRX 파일 파싱 (간단한 요약)
        [xml]$trxContent = Get-Content $trxFiles.FullName
        $summary = $trxContent.TestRun.ResultSummary

        if ($summary) {
            $outcome = $summary.outcome
            $total = $summary.Counters.total
            $passed = $summary.Counters.passed
            $failed = $summary.Counters.failed
            $skipped = $summary.Counters.notExecuted

            Write-Host ""
            Write-ColorOutput "📊 테스트 요약:" "Cyan"
            Write-ColorOutput "  총 테스트: $total" "White"
            Write-ColorOutput "  ✅ 통과: $passed" "Green"

            if ([int]$failed -gt 0) {
                Write-ColorOutput "  ❌ 실패: $failed" "Red"
            }
            else {
                Write-ColorOutput "  ❌ 실패: 0" "Green"
            }

            if ([int]$skipped -gt 0) {
                Write-ColorOutput "  ⏭️  건너뜀: $skipped" "Yellow"
            }

            $passRate = if ([int]$total -gt 0) { ([int]$passed / [int]$total * 100).ToString("F1") } else { "0.0" }
            Write-ColorOutput "  성공률: $passRate%" $(if ([int]$failed -eq 0) { "Green" } else { "Yellow" })
        }
    }
    else {
        Write-Info "테스트 결과 파일을 찾을 수 없습니다."
    }
}
catch {
    Write-Info "테스트 결과 요약을 생성할 수 없습니다: $_"
}

# 4단계: 커버리지 리포트 (옵션)
if ($Coverage) {
    Write-Section "4️⃣  코드 커버리지 분석"

    try {
        $coverageFiles = Get-ChildItem -Path "$projectRoot\tests\TwinCatQA.Integration.Tests\TestResults" `
            -Filter "coverage.cobertura.xml" `
            -Recurse `
            -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTime -Descending |
            Select-Object -First 1

        if ($coverageFiles) {
            Write-Success "커버리지 파일 생성: $($coverageFiles.FullName)"

            # 간단한 커버리지 요약
            [xml]$coverageXml = Get-Content $coverageFiles.FullName
            $lineRate = $coverageXml.coverage.'line-rate'
            $branchRate = $coverageXml.coverage.'branch-rate'

            if ($lineRate) {
                $lineCoverage = ([double]$lineRate * 100).ToString("F1")
                Write-ColorOutput "  라인 커버리지: $lineCoverage%" "Cyan"
            }

            if ($branchRate) {
                $branchCoverage = ([double]$branchRate * 100).ToString("F1")
                Write-ColorOutput "  브랜치 커버리지: $branchCoverage%" "Cyan"
            }

            Write-Info "상세 리포트 생성을 위해 ReportGenerator를 사용하세요:"
            Write-Info "  dotnet tool install -g dotnet-reportgenerator-globaltool"
            Write-Info "  reportgenerator -reports:$($coverageFiles.FullName) -targetdir:coverage-report"
        }
        else {
            Write-Info "커버리지 파일을 찾을 수 없습니다."
        }
    }
    catch {
        Write-Info "커버리지 분석 중 오류: $_"
    }
}

# 5단계: 성능 메트릭 (선택적)
Write-Section "5️⃣  성능 메트릭"

try {
    # 로그에서 성능 정보 추출 시도
    $performanceTests = $testOutput | Select-String -Pattern "성능|소요 시간|처리량" -Context 0,1

    if ($performanceTests) {
        Write-Info "성능 테스트 결과:"
        $performanceTests | ForEach-Object {
            Write-ColorOutput "  $($_.Line)" "Cyan"
        }
    }
}
catch {
    # 성능 메트릭 추출 실패는 무시
}

# 완료
Write-Header "✅ E2E 테스트 완료"

$totalTime = (Get-Date) - $buildStartTime
Write-Info "총 소요 시간: $($totalTime.TotalSeconds.ToString('F2'))초"

Write-Host ""
Write-ColorOutput "🎉 모든 E2E 통합 테스트가 성공적으로 완료되었습니다!" "Green"
Write-Host ""

exit 0
