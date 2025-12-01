# TwinCAT 코드 품질 검증 도구 - 빌드 가이드

## 📋 목차

1. [시작하기 전에](#시작하기-전에)
2. [환경 설정](#환경-설정)
3. [ANTLR4 설정](#antlr4-설정)
4. [프로젝트 빌드](#프로젝트-빌드)
5. [테스트 실행](#테스트-실행)
6. [문제 해결](#문제-해결)

---

## 시작하기 전에

### 필수 요구사항

- **운영체제**: Windows 10/11 (64-bit)
- **.NET SDK**: 6.0 이상 ([다운로드](https://dotnet.microsoft.com/download/dotnet/6.0))
- **Java Runtime**: 11+ (ANTLR4 컴파일용) ([다운로드](https://adoptium.net/))
- **Git**: 2.30+ (선택 사항, Git 통합 기능용)
- **Visual Studio**: 2019/2022 (선택 사항, VSIX 개발용)

### 권장 도구

- **Visual Studio Code** + C# Dev Kit
- **Git Bash** (Windows)
- **PowerShell 7+**

---

## 환경 설정

### 1. 저장소 클론 (또는 다운로드)

```bash
cd "D:\01. Vscode\Twincat\features\twincat-code-qa-tool"
```

### 2. .NET SDK 설치 확인

```bash
dotnet --version
# 출력 예시: 8.0.100 또는 6.0.x
```

### 3. Java 설치 확인

```bash
java -version
# 출력 예시: openjdk version "17.0.2"
```

---

## ANTLR4 설정

### 1. ANTLR4 JAR 다운로드

```bash
cd src/TwinCatQA.Infrastructure/Parsers/Grammars

# Windows PowerShell
Invoke-WebRequest -Uri "https://www.antlr.org/download/antlr-4.11.1-complete.jar" -OutFile "antlr-4.11.1-complete.jar"

# 또는 Bash
curl -O https://www.antlr.org/download/antlr-4.11.1-complete.jar
```

### 2. ST 문법 파일 컴파일

```bash
# StructuredText.g4 → C# 파서 코드 생성
java -jar antlr-4.11.1-complete.jar -Dlanguage=CSharp StructuredText.g4
```

**생성되는 파일**:
- `StructuredTextLexer.cs`
- `StructuredTextParser.cs`
- `StructuredTextVisitor.cs`
- `StructuredTextBaseVisitor.cs`

### 3. 생성된 파일 확인

```bash
ls -la *.cs
# 4개 파일이 존재해야 함
```

**주의**: 이 파일들은 `.gitignore`에 포함되어 있으므로 빌드 시 매번 재생성해야 합니다.

---

## 프로젝트 빌드

### 1. NuGet 패키지 복원

프로젝트 루트로 이동:
```bash
cd "D:\01. Vscode\Twincat\features\twincat-code-qa-tool"
```

패키지 복원:
```bash
dotnet restore
```

**예상 출력**:
```
Determining projects to restore...
Restored D:\01. Vscode\Twincat\features\twincat-code-qa-tool\src\TwinCatQA.Domain\TwinCatQA.Domain.csproj
Restored D:\01. Vscode\Twincat\features\twincat-code-qa-tool\src\TwinCatQA.Infrastructure\TwinCatQA.Infrastructure.csproj
Restored D:\01. Vscode\Twincat\features\twincat-code-qa-tool\src\TwinCatQA.Application\TwinCatQA.Application.csproj
```

### 2. 디버그 빌드

```bash
dotnet build --configuration Debug
```

### 3. 릴리스 빌드

```bash
dotnet build --configuration Release
```

**빌드 출력 위치**:
- Debug: `src/*/bin/Debug/net6.0/`
- Release: `src/*/bin/Release/net6.0/`

### 4. 빌드 오류 확인

빌드 실패 시 다음 항목을 확인하세요:
- ANTLR4 파서 파일이 생성되었는가?
- NuGet 패키지가 복원되었는가?
- .NET SDK 버전이 6.0 이상인가?

---

## 테스트 실행

### 1. 전체 테스트 실행

```bash
dotnet test
```

**예상 출력**:
```
Passed! - Failed: 0, Passed: 69, Skipped: 0, Total: 69, Duration: 3.2s
```

### 2. 특정 프로젝트만 테스트

```bash
# 도메인 테스트만 실행
dotnet test tests/TwinCatQA.Domain.Tests/TwinCatQA.Domain.Tests.csproj

# 애플리케이션 테스트만 실행
dotnet test tests/TwinCatQA.Application.Tests/TwinCatQA.Application.Tests.csproj

# 인프라 테스트만 실행
dotnet test tests/TwinCatQA.Infrastructure.Tests/TwinCatQA.Infrastructure.Tests.csproj
```

### 3. 특정 테스트 클래스만 실행

```bash
# ValidationSessionTests만 실행
dotnet test --filter "FullyQualifiedName~ValidationSessionTests"

# KoreanCommentRuleTests만 실행
dotnet test --filter "FullyQualifiedName~KoreanCommentRuleTests"
```

### 4. 테스트 커버리지 확인 (선택 사항)

```bash
dotnet test --collect:"XPlat Code Coverage"
```

---

## 빌드 자동화 스크립트

### PowerShell 스크립트 (Windows)

**파일**: `build.ps1`

```powershell
# TwinCAT 코드 품질 검증 도구 빌드 스크립트
# 사용법: .\build.ps1 [Release|Debug]

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
```

**실행 방법**:
```powershell
# 릴리스 빌드
.\build.ps1

# 디버그 빌드
.\build.ps1 -Configuration Debug
```

### Bash 스크립트 (Linux/Mac/Git Bash)

**파일**: `build.sh`

```bash
#!/bin/bash
# TwinCAT 코드 품질 검증 도구 빌드 스크립트
# 사용법: ./build.sh [Release|Debug]

CONFIGURATION="${1:-Release}"

echo "========================================"
echo " TwinCAT 코드 품질 검증 도구 빌드"
echo "========================================"
echo ""

# 1. ANTLR4 문법 컴파일
echo "[1/5] ANTLR4 문법 컴파일 중..."
cd src/TwinCatQA.Infrastructure/Parsers/Grammars

if [ ! -f "antlr-4.11.1-complete.jar" ]; then
    echo "  - ANTLR4 JAR 다운로드 중..."
    curl -O https://www.antlr.org/download/antlr-4.11.1-complete.jar
fi

echo "  - StructuredText.g4 컴파일 중..."
java -jar antlr-4.11.1-complete.jar -Dlanguage=CSharp StructuredText.g4

if [ $? -ne 0 ]; then
    echo "✗ ANTLR4 컴파일 실패"
    exit 1
fi
echo "✓ ANTLR4 컴파일 완료"

cd ../../../..

# 2. NuGet 패키지 복원
echo ""
echo "[2/5] NuGet 패키지 복원 중..."
dotnet restore

if [ $? -ne 0 ]; then
    echo "✗ 패키지 복원 실패"
    exit 1
fi
echo "✓ 패키지 복원 완료"

# 3. 솔루션 빌드
echo ""
echo "[3/5] 솔루션 빌드 중 ($CONFIGURATION)..."
dotnet build --configuration $CONFIGURATION --no-restore

if [ $? -ne 0 ]; then
    echo "✗ 빌드 실패"
    exit 1
fi
echo "✓ 빌드 완료"

# 4. 테스트 실행
echo ""
echo "[4/5] 단위 테스트 실행 중..."
dotnet test --configuration $CONFIGURATION --no-build --verbosity quiet

if [ $? -ne 0 ]; then
    echo "✗ 테스트 실패"
    exit 1
fi
echo "✓ 테스트 통과"

# 5. 완료
echo ""
echo "========================================"
echo "✓ 빌드 성공!"
echo "========================================"
echo ""
echo "빌드 출력 위치: src/*/bin/$CONFIGURATION/net6.0/"
```

**실행 방법**:
```bash
# 실행 권한 부여
chmod +x build.sh

# 릴리스 빌드
./build.sh

# 디버그 빌드
./build.sh Debug
```

---

## 문제 해결

### 문제 1: ANTLR4 JAR을 찾을 수 없음
**오류**:
```
Error: Unable to access jarfile antlr-4.11.1-complete.jar
```

**해결**:
```bash
cd src/TwinCatQA.Infrastructure/Parsers/Grammars
curl -O https://www.antlr.org/download/antlr-4.11.1-complete.jar
```

---

### 문제 2: Java를 찾을 수 없음
**오류**:
```
'java' is not recognized as an internal or external command
```

**해결**:
1. Java 11+ 설치: https://adoptium.net/
2. 환경 변수 `JAVA_HOME` 설정
3. `PATH`에 `%JAVA_HOME%\bin` 추가

---

### 문제 3: NuGet 패키지 복원 실패
**오류**:
```
Unable to load the service index for source https://api.nuget.org/v3/index.json
```

**해결**:
```bash
# NuGet 캐시 클리어
dotnet nuget locals all --clear

# 재시도
dotnet restore
```

---

### 문제 4: 빌드 오류 (CS0246: The type or namespace name could not be found)
**원인**: ANTLR4 파서 파일이 생성되지 않았음

**해결**:
```bash
cd src/TwinCatQA.Infrastructure/Parsers/Grammars
java -jar antlr-4.11.1-complete.jar -Dlanguage=CSharp StructuredText.g4
```

---

### 문제 5: 테스트 실패
**오류**:
```
Failed! - Failed: 1, Passed: 68, Skipped: 0
```

**해결**:
```bash
# 상세 출력으로 재실행
dotnet test --verbosity normal

# 특정 테스트만 실행하여 원인 파악
dotnet test --filter "FullyQualifiedName~ValidationSessionTests"
```

---

## CI/CD 통합 (향후 계획)

### GitHub Actions 워크플로우 예시

```yaml
name: Build and Test

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    runs-on: windows-latest

    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 6.0.x

    - name: Setup Java
      uses: actions/setup-java@v3
      with:
        distribution: 'temurin'
        java-version: '17'

    - name: Compile ANTLR4 Grammar
      run: |
        cd src/TwinCatQA.Infrastructure/Parsers/Grammars
        Invoke-WebRequest -Uri "https://www.antlr.org/download/antlr-4.11.1-complete.jar" -OutFile "antlr-4.11.1-complete.jar"
        java -jar antlr-4.11.1-complete.jar -Dlanguage=CSharp StructuredText.g4

    - name: Restore dependencies
      run: dotnet restore

    - name: Build
      run: dotnet build --no-restore --configuration Release

    - name: Test
      run: dotnet test --no-build --configuration Release --verbosity normal
```

---

**작성일**: 2025-11-20
**버전**: 1.0.0
