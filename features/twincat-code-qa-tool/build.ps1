# TwinCAT 코드 품질 검증 도구 빌드 스크립트
# 사용법: .\build.ps1 [Release|Debug]
# 작성일: 2025-11-20

param(
    [string]$Configuration = "Release"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " TwinCAT 코드 품질 검증 도구 빌드" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 1. ANTLR4 문법 컴파일
Write-Host "[1/5] ANTLR4 문법 컴파일 중..." -ForegroundColor Yellow
cd src/TwinCatQA.Infrastructure/Parsers/Grammars

if (-not (Test-Path "antlr-4.11.1-complete.jar")) {
    Write-Host "  - ANTLR4 JAR 다운로드 중..." -ForegroundColor Gray
    Invoke-WebRequest -Uri "https://www.antlr.org/download/antlr-4.11.1-complete.jar" -OutFile "antlr-4.11.1-complete.jar"
}

Write-Host "  - StructuredText.g4 컴파일 중..." -ForegroundColor Gray
java -jar antlr-4.11.1-complete.jar -Dlanguage=CSharp StructuredText.g4

if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ ANTLR4 컴파일 실패" -ForegroundColor Red
    exit 1
}
Write-Host "✓ ANTLR4 컴파일 완료" -ForegroundColor Green

cd ../../../..

# 2. NuGet 패키지 복원
Write-Host ""
Write-Host "[2/5] NuGet 패키지 복원 중..." -ForegroundColor Yellow
dotnet restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ 패키지 복원 실패" -ForegroundColor Red
    exit 1
}
Write-Host "✓ 패키지 복원 완료" -ForegroundColor Green

# 3. 솔루션 빌드
Write-Host ""
Write-Host "[3/5] 솔루션 빌드 중 ($Configuration)..." -ForegroundColor Yellow
dotnet build --configuration $Configuration --no-restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ 빌드 실패" -ForegroundColor Red
    exit 1
}
Write-Host "✓ 빌드 완료" -ForegroundColor Green

# 4. 테스트 실행
Write-Host ""
Write-Host "[4/5] 단위 테스트 실행 중..." -ForegroundColor Yellow
dotnet test --configuration $Configuration --no-build --verbosity quiet

if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ 테스트 실패" -ForegroundColor Red
    exit 1
}
Write-Host "✓ 테스트 통과" -ForegroundColor Green

# 5. 완료
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "✓ 빌드 성공!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "빌드 출력 위치: src/*/bin/$Configuration/net6.0/" -ForegroundColor Gray
Write-Host ""
