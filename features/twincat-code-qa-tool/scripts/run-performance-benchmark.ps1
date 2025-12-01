# 성능 벤치마크 테스트 전용 스크립트
# PerformanceBenchmarkTests만 실행하고 결과를 분석합니다.

param(
    [string]$Configuration = "Release",
    [switch]$Verbose,
    [string]$OutputDir = "benchmark-results"
)

$ErrorActionPreference = "Stop"

function Write-ColorOutput {
    param([string]$Message, [string]$Color = "White")
    Write-Host $Message -ForegroundColor $Color
}

function Write-Header {
    param([string]$Title)
    Write-ColorOutput "`n═══════════════════════════════════════════════════════════════" "Cyan"
    Write-ColorOutput "  $Title" "Cyan"
    Write-ColorOutput "═══════════════════════════════════════════════════════════════" "Cyan"
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
$testProject = Join-Path $projectRoot "tests\TwinCatQA.Integration.Tests\TwinCatQA.Integration.Tests.csproj"
$resultsDir = Join-Path $projectRoot $OutputDir

Write-Header "⚡ TwinCAT QA Tool - 성능 벤치마크"

# 결과 디렉토리 생성
if (-not (Test-Path $resultsDir)) {
    New-Item -ItemType Directory -Path $resultsDir -Force | Out-Null
}

Write-ColorOutput "`n📊 벤치마크 설정:" "Yellow"
Write-ColorOutput "  구성: $Configuration" "White"
Write-ColorOutput "  결과 디렉토리: $resultsDir" "White"

# 빌드
Write-ColorOutput "`n🔨 프로젝트 빌드 중..." "Yellow"
dotnet build "$projectRoot\TwinCatQA.sln" --configuration $Configuration --verbosity quiet

if ($LASTEXITCODE -ne 0) {
    Write-ColorOutput "❌ 빌드 실패" "Red"
    exit 1
}

Write-ColorOutput "✅ 빌드 완료" "Green"

# 성능 벤치마크 실행
Write-ColorOutput "`n🚀 성능 벤치마크 실행 중..." "Yellow"

$benchmarkStartTime = Get-Date

$testArgs = @(
    "test"
    $testProject
    "--configuration", $Configuration
    "--no-build"
    "--filter", "FullyQualifiedName~PerformanceBenchmarkTests"
    "--logger", "console;verbosity=detailed"
)

if ($Verbose) {
    $testArgs += "--verbosity", "detailed"
}

$testOutput = dotnet @testArgs 2>&1
$testExitCode = $LASTEXITCODE

$benchmarkTime = (Get-Date) - $benchmarkStartTime

# 결과 파싱
Write-Header "📈 벤치마크 결과"

# 성능 메트릭 추출
$performanceMetrics = @{
    "파일수별_10개" = $null
    "파일수별_50개" = $null
    "파일수별_100개" = $null
    "메모리사용량" = $null
    "리포트생성" = $null
}

$testOutput | ForEach-Object {
    $line = $_.ToString()

    # 성능 데이터 추출
    if ($line -match "성능 벤치마크 결과.*\((\d+)개 파일\)") {
        Write-ColorOutput "`n$line" "Cyan"
    }
    elseif ($line -match "소요 시간: ([\d.]+)초") {
        Write-ColorOutput "  ⏱️  $line" "White"
    }
    elseif ($line -match "처리량: ([\d.]+) 파일/초") {
        Write-ColorOutput "  🚀 $line" "Green"
    }
    elseif ($line -match "메모리 사용: ([\d.]+) MB") {
        Write-ColorOutput "  💾 $line" "White"
    }
    elseif ($line -match "메모리 사용량 분석") {
        Write-ColorOutput "`n$line" "Cyan"
    }
    elseif ($line -match "병렬 처리 성능") {
        Write-ColorOutput "`n$line" "Cyan"
    }
    elseif ($line -match "성능 향상: ([\d.]+)x") {
        Write-ColorOutput "  📊 $line" "Green"
    }
}

# 테스트 결과 요약
Write-Header "✅ 벤치마크 완료"

if ($testExitCode -eq 0) {
    Write-ColorOutput "✅ 모든 성능 벤치마크 통과" "Green"
}
else {
    Write-ColorOutput "⚠️  일부 벤치마크 실패 또는 시간 초과" "Yellow"
}

Write-ColorOutput "`n총 벤치마크 소요 시간: $($benchmarkTime.TotalSeconds.ToString('F2'))초" "White"

# 결과 저장
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$resultFile = Join-Path $resultsDir "benchmark_result_$timestamp.txt"

$testOutput | Out-File -FilePath $resultFile -Encoding UTF8

Write-ColorOutput "`n📄 결과 저장: $resultFile" "Cyan"

Write-ColorOutput "`n💡 추천 사항:" "Yellow"
Write-ColorOutput "  - 벤치마크를 여러 번 실행하여 평균값을 측정하세요" "White"
Write-ColorOutput "  - Release 구성으로 실행하면 더 정확한 성능을 측정할 수 있습니다" "White"
Write-ColorOutput "  - 백그라운드 프로세스를 최소화하여 측정 정확도를 높이세요" "White"

exit $testExitCode
