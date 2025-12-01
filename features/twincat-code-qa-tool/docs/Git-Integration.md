# TwinCAT 코드 품질 검증 도구 - Git 통합 가이드

## 개요

LibGit2Sharp 라이브러리를 사용하여 Git 저장소와 통합된 TwinCAT 코드 품질 검증 서비스입니다.

## 핵심 기능

### 1. 저장소 관리
- **Git 저장소 초기화**: 새로운 Git 저장소 생성
- **저장소 여부 확인**: .git 디렉토리 탐색 (상위 디렉토리 포함)
- **커밋 해시 조회**: HEAD 커밋의 SHA-1 해시
- **워킹 디렉토리 상태**: 변경사항 유무 확인

### 2. Diff 분석
- **변경 파일 목록**: Index, WorkingDirectory, All 대상 비교
- **변경 라인 추출**: Patch 파싱하여 Added/Modified/Deleted 라인 식별
- **커밋 간 비교**: 두 커밋의 Tree Diff 분석

### 3. Pre-commit Hook
- **자동 설치**: Bash (Linux/Mac) 및 Batch (Windows) 스크립트
- **품질 검증 통합**: 커밋 전 자동 검증 실행
- **커밋 차단**: Critical 위반 시 커밋 중단

### 4. 컨텍스트 분석
- **FunctionBlock 탐색**: 변경 라인이 속한 FB 식별
- **제어 구조 탐색**: CASE/FOR/IF/WHILE 블록 재귀 탐색
- **주변 라인 범위**: 기본 ±10줄 범위 반환

## 아키텍처

### Adapter 패턴
```
LibGit2Sharp (외부 API)
    ↓
DiffParser / ContextAnalyzer (Adapter)
    ↓
Domain Model (LineChange, CodeContext)
    ↓
LibGit2Service (Service Layer)
```

### 클래스 구조

```
TwinCatQA.Infrastructure.Git/
├── IGitService.cs              # 서비스 인터페이스
├── LibGit2Service.cs           # 메인 구현
├── DiffParser.cs               # Patch → LineChange 변환
├── ContextAnalyzer.cs          # 코드 컨텍스트 분석
├── DiffTarget.cs               # Diff 대상 열거형
├── LineChangeType.cs           # 라인 변경 유형
├── LineChange.cs               # 변경 라인 모델
├── CodeContext.cs              # 컨텍스트 정보 모델
└── Templates/
    ├── pre-commit.sh           # Bash Hook 스크립트
    └── pre-commit.bat          # Windows Hook 스크립트
```

## 사용법

### 1. 서비스 등록 (DI)

```csharp
// Startup.cs 또는 Program.cs
services.AddSingleton<IGitService, LibGit2Service>();
```

### 2. 저장소 관리

```csharp
var gitService = serviceProvider.GetRequiredService<IGitService>();

// Git 저장소 초기화
bool initialized = gitService.InitializeRepository(@"D:\MyProject");

// 저장소 여부 확인
bool isGit = gitService.IsGitRepository(@"D:\MyProject\src");

// 현재 커밋 해시
string? commitHash = gitService.GetCurrentCommitHash(@"D:\MyProject");
Console.WriteLine($"Current commit: {commitHash}");

// 워킹 디렉토리 상태
bool isClean = gitService.IsWorkingDirectoryClean(@"D:\MyProject");
```

### 3. Diff 분석

```csharp
// 변경된 파일 목록 (워킹 디렉토리 vs HEAD)
var changedFiles = gitService.GetChangedFiles(
    @"D:\MyProject",
    DiffTarget.All
);

foreach (var file in changedFiles)
{
    Console.WriteLine($"Changed: {file}");
}

// 특정 파일의 변경 라인
var lineChanges = gitService.GetChangedLines(
    @"D:\MyProject",
    "src/MyProgram.st"
);

foreach (var change in lineChanges)
{
    Console.WriteLine($"{change.ChangeType} Line {change.LineNumber}: {change.Content}");
}

// 커밋 간 비교
var diffFiles = gitService.GetDiffBetweenCommits(
    @"D:\MyProject",
    "abc123",  // From commit
    "def456"   // To commit
);
```

### 4. Pre-commit Hook 설치

```csharp
// Hook 설치 (Critical 위반 시 커밋 차단)
bool installed = gitService.InstallPreCommitHook(
    @"D:\MyProject",
    blockOnCritical: true
);

// Hook 설치 여부 확인
bool isInstalled = gitService.IsPreCommitHookInstalled(@"D:\MyProject");

// Hook 제거
bool uninstalled = gitService.UninstallPreCommitHook(@"D:\MyProject");
```

### 5. 컨텍스트 분석

```csharp
// CodeFile 객체 로드 (파서 사용)
var codeFile = await parserService.ParseFileAsync("MyProgram.st");

// 변경 라인의 컨텍스트 결정
var context = gitService.DetermineContext(codeFile, changedLine: 42);

Console.WriteLine($"Context Type: {context.ContextType}");
Console.WriteLine($"Context Name: {context.ContextName}");
Console.WriteLine($"Range: {context.StartLine} - {context.EndLine}");
```

## 증분 검증 워크플로우

```csharp
public async Task<ValidationResult> ValidateIncrementalAsync(string repoPath)
{
    // 1. 변경된 파일 목록 조회
    var changedFiles = gitService.GetChangedFiles(repoPath, DiffTarget.All);

    var violations = new List<Violation>();

    foreach (var filePath in changedFiles)
    {
        // 2. 변경된 라인 목록
        var lineChanges = gitService.GetChangedLines(repoPath, filePath);

        // 3. 파일 파싱
        var codeFile = await parserService.ParseFileAsync(filePath);

        foreach (var lineChange in lineChanges)
        {
            // 4. 컨텍스트 범위 결정
            var context = gitService.DetermineContext(codeFile, lineChange.LineNumber);

            // 5. 컨텍스트 범위만 검증
            var contextViolations = await validationService.ValidateRangeAsync(
                codeFile,
                context.StartLine,
                context.EndLine
            );

            violations.AddRange(contextViolations);
        }
    }

    return new ValidationResult(violations);
}
```

## Pre-commit Hook 동작

### Bash 스크립트 (Linux/Mac)
```bash
#!/bin/bash
# .git/hooks/pre-commit

echo "TwinCAT 코드 품질 검증 중..."

dotnet twincat-qa validate --mode Incremental --fail-on-critical

if [ $? -ne 0 ]; then
    echo "❌ 품질 검증 실패: 커밋 차단"
    exit 1
fi

echo "✅ 품질 검증 통과"
exit 0
```

### Batch 스크립트 (Windows)
```batch
@echo off
REM .git/hooks/pre-commit.bat

echo TwinCAT 코드 품질 검증 중...

dotnet twincat-qa validate --mode Incremental --fail-on-critical

if %ERRORLEVEL% NEQ 0 (
    echo ❌ 품질 검증 실패: 커밋 차단
    exit /b 1
)

echo ✅ 품질 검증 통과
exit /b 0
```

### Hook 우회 (권장하지 않음)
```bash
git commit --no-verify -m "임시 커밋"
```

## 오류 회복성

### Git 저장소가 없는 경우
```csharp
// 모든 메서드는 null/false/empty 반환 (예외 발생 안 함)
bool isGit = gitService.IsGitRepository(@"D:\NotGitRepo");
// Result: false

var files = gitService.GetChangedFiles(@"D:\NotGitRepo", DiffTarget.All);
// Result: Empty list (Array.Empty<string>())

// 검증 도구는 정상 동작 (전체 검증 모드로 전환)
```

### 커밋이 없는 빈 저장소
```csharp
string? commitHash = gitService.GetCurrentCommitHash(@"D:\EmptyRepo");
// Result: null (로그 경고만 출력)
```

### Patch 파싱 실패
```csharp
// DiffParser는 빈 목록 반환 (예외 전파 안 함)
var lineChanges = gitService.GetChangedLines(repoPath, "invalid.file");
// Result: Empty list
```

## 성능 최적화

### Diff 대상 선택
```csharp
// 가장 빠름: Index vs HEAD (스테이징된 파일만)
var stagedFiles = gitService.GetChangedFiles(repoPath, DiffTarget.Index);

// 중간: 워킹 디렉토리 vs Index (추적되지 않은 파일 포함)
var workingFiles = gitService.GetChangedFiles(repoPath, DiffTarget.WorkingDirectory);

// 느림: 워킹 디렉토리 vs HEAD (모든 변경사항)
var allFiles = gitService.GetChangedFiles(repoPath, DiffTarget.All);
```

### 컨텍스트 범위 최소화
```csharp
// FunctionBlock 단위 (가장 정확, 범위 최소)
var fbContext = contextAnalyzer.FindContainingFunctionBlock(file, line);

// 제어 구조 단위 (중간 범위)
var controlContext = contextAnalyzer.FindContainingControlStructure(ast, line);

// 주변 라인 (기본 ±10줄, 가장 빠름)
var surroundingContext = contextAnalyzer.GetSurroundingLines(file, line);
```

## 보안 고려사항

### Hook 파일 권한
```bash
# Unix 시스템에서 실행 권한 필요
chmod +x .git/hooks/pre-commit

# LibGit2Service는 자동으로 chmod 실행 시도
# 실패 시 수동으로 권한 부여 필요
```

### 민감한 정보 차단
```csharp
// Pre-commit Hook에서 민감 파일 검사
var sensitiveFiles = new[] { ".env", "credentials.json", "*.pem" };

foreach (var pattern in sensitiveFiles)
{
    var matches = changedFiles.Where(f => f.Contains(pattern));
    if (matches.Any())
    {
        Console.WriteLine("❌ 민감한 파일이 포함되어 있습니다: {0}", string.Join(", ", matches));
        return false;
    }
}
```

## 로깅 및 디버깅

### 로그 레벨별 출력
```csharp
// Information: 정상 동작
_logger.LogInformation("Git 저장소 초기화 완료: {RepoPath}", repoPath);

// Warning: Git 저장소가 아님 (정상 동작)
_logger.LogWarning("Git 저장소가 아닙니다: {RepoPath}", repoPath);

// Error: 예상치 못한 오류
_logger.LogError(ex, "Diff 조회 실패: {RepoPath}", repoPath);

// Debug: 상세 진단 정보
_logger.LogDebug("Hook 설치 여부 확인: {IsInstalled}", isInstalled);
```

### 진단 명령어
```bash
# Git 상태 확인
git status

# Hook 파일 확인
ls -la .git/hooks/pre-commit
cat .git/hooks/pre-commit

# Hook 수동 실행 테스트
.git/hooks/pre-commit

# 검증 도구 단독 실행
dotnet twincat-qa validate --mode Incremental --verbose
```

## 제약사항

1. **LibGit2Sharp 의존성**: .NET Standard 2.0+ 필요
2. **플랫폼별 Hook**: Bash (Unix) / Batch (Windows) 각각 생성
3. **대용량 저장소**: Diff 분석 시 메모리 사용량 증가 가능
4. **동시 실행**: Repository 객체는 Thread-safe 아님 (using 블록 필수)

## 참고 자료

- [LibGit2Sharp 공식 문서](https://github.com/libgit2/libgit2sharp)
- [Git Hooks 가이드](https://git-scm.com/book/en/v2/Customizing-Git-Git-Hooks)
- [TwinCAT 코드 품질 규칙](./Quality-Rules.md)

---

**생성일**: 2025-11-20
**버전**: 1.0.0
**작성자**: TwinCAT QA Team
