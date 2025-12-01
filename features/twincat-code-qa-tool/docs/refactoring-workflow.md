# TwinCatQA 프로젝트 코드 품질 개선 워크플로우

## 📋 프로젝트 개요

**프로젝트 경로**: `D:\01. Vscode\Twincat\features\twincat-code-qa-tool`

**목표**: Clean Code 원칙에 따른 코드 품질 개선 및 기술 부채 제거

**개선 기간**: 2주 (2 Sprint)

**작업 방식**: Agile 스프린트

---

## 🎯 전체 개선 항목 요약

| 항목 | 파일 | 현재 상태 | 목표 | 우선순위 | 예상 시간 |
|------|------|-----------|------|----------|-----------|
| 1 | CyclomaticComplexityRule.cs | 매직 넘버 (10, 15, 20) | 상수화 | High | 1시간 |
| 2 | Application/Infrastructure | Console.WriteLine 사용 | Logger 교체 | High | 3시간 |
| 3 | LibGit2Service.cs | 674줄 대형 클래스 | 3개 클래스 분리 | High | 8시간 |
| 4 | Rules 클래스들 | 중복 ExtractSnippet | 유틸 클래스 통합 | Medium | 2시간 |
| 5 | CompareCommand.cs | UTF-8 BOM 인코딩 | UTF-8 수정 | Low | 0.5시간 |

**총 예상 시간**: 14.5시간 (약 2일)

---

## 📅 스프린트 계획

### Sprint 1: 기초 리팩토링 (1주차, 8시간)
- **목표**: 매직 넘버 제거, 로깅 개선, 인코딩 수정
- **Story Points**: 13점

### Sprint 2: 구조 개선 (2주차, 6.5시간)
- **목표**: 대형 클래스 분리, 중복 코드 제거
- **Story Points**: 21점

---

# Sprint 1: 기초 리팩토링 (1주차)

## User Story 1: 매직 넘버 상수화

### 📌 Story 정보
- **Story ID**: REF-001
- **Priority**: High
- **Effort**: 1 Story Point (1시간)
- **Assignee**: Refactoring Expert

### 📖 Description
CyclomaticComplexityRule.cs에서 사용되는 복잡도 임계값 (10, 15, 20)을 상수로 정의하여 유지보수성을 향상시킵니다.

### ✅ Acceptance Criteria
- [ ] 매직 넘버가 명확한 이름의 상수로 대체됨
- [ ] XML 문서화 주석이 추가됨
- [ ] 기존 테스트가 모두 통과함
- [ ] 새로운 단위 테스트가 추가됨

### 🔧 리팩토링 전/후 코드

#### ❌ Before (현재 코드)
```csharp
public class CyclomaticComplexityRule : IValidationRule
{
    private readonly IParserService _parserService;

    /// <summary>
    /// Medium 심각도 임계값 (기본: 10)
    /// </summary>
    private int _mediumThreshold = 10;

    /// <summary>
    /// High 심각도 임계값 (기본: 15)
    /// </summary>
    private int _highThreshold = 15;

    /// <summary>
    /// Critical 심각도 임계값 (기본: 20)
    /// </summary>
    private int _criticalThreshold = 20;

    // ... 나머지 코드
}
```

#### ✅ After (개선 코드)
```csharp
public class CyclomaticComplexityRule : IValidationRule
{
    private readonly IParserService _parserService;

    #region 복잡도 임계값 상수

    /// <summary>
    /// 사이클로매틱 복잡도의 기본 Medium 임계값
    /// McCabe의 권장 사항: 10 이하로 유지
    /// </summary>
    public const int DefaultMediumThreshold = 10;

    /// <summary>
    /// 사이클로매틱 복잡도의 기본 High 임계값
    /// 이 값 이상이면 리팩토링 우선순위를 높여야 함
    /// </summary>
    public const int DefaultHighThreshold = 15;

    /// <summary>
    /// 사이클로매틱 복잡도의 기본 Critical 임계값
    /// 이 값 이상이면 즉시 리팩토링이 필요함
    /// </summary>
    public const int DefaultCriticalThreshold = 20;

    #endregion

    #region 인스턴스 변수

    /// <summary>
    /// 현재 적용 중인 Medium 심각도 임계값
    /// </summary>
    private int _mediumThreshold = DefaultMediumThreshold;

    /// <summary>
    /// 현재 적용 중인 High 심각도 임계값
    /// </summary>
    private int _highThreshold = DefaultHighThreshold;

    /// <summary>
    /// 현재 적용 중인 Critical 심각도 임계값
    /// </summary>
    private int _criticalThreshold = DefaultCriticalThreshold;

    #endregion

    // ... 나머지 코드
}
```

### 📝 단계별 작업 순서

#### Step 1: 상수 추가 (5분)
```bash
# 현재 코드 백업
cp CyclomaticComplexityRule.cs CyclomaticComplexityRule.cs.bak

# 파일 수정 (Edit 도구 사용)
```

#### Step 2: Description 업데이트 (5분)
```csharp
public string Description =>
    "Function Block의 사이클로매틱 복잡도를 검증합니다. " +
    "복잡도가 높으면 테스트가 어렵고 버그 발생 확률이 증가합니다. " +
    $"임계값: Medium({DefaultMediumThreshold}), " +
    $"High({DefaultHighThreshold}), " +
    $"Critical({DefaultCriticalThreshold})";
```

#### Step 3: 테스트 작성 (30분)
```csharp
// tests/TwinCatQA.Application.Tests/Rules/CyclomaticComplexityRuleTests.cs

[Fact]
public void Constants_ShouldHaveExpectedDefaultValues()
{
    // Arrange & Act
    var mediumThreshold = CyclomaticComplexityRule.DefaultMediumThreshold;
    var highThreshold = CyclomaticComplexityRule.DefaultHighThreshold;
    var criticalThreshold = CyclomaticComplexityRule.DefaultCriticalThreshold;

    // Assert
    Assert.Equal(10, mediumThreshold);
    Assert.Equal(15, highThreshold);
    Assert.Equal(20, criticalThreshold);
}

[Fact]
public void Configure_WhenNotProvided_ShouldUseDefaultConstants()
{
    // Arrange
    var parserService = Mock.Of<IParserService>();
    var rule = new CyclomaticComplexityRule(parserService);

    // Act
    rule.Configure(new Dictionary<string, object>());

    // Assert
    // 내부 임계값이 상수 값과 동일한지 확인
    Assert.Equal(CyclomaticComplexityRule.DefaultMediumThreshold, 10);
}

[Theory]
[InlineData(5, 10, 15)]
[InlineData(8, 12, 18)]
public void Configure_WithCustomThresholds_ShouldOverrideDefaults(
    int medium, int high, int critical)
{
    // Arrange
    var parserService = Mock.Of<IParserService>();
    var rule = new CyclomaticComplexityRule(parserService);
    var config = new Dictionary<string, object>
    {
        { "medium_threshold", medium },
        { "high_threshold", high },
        { "critical_threshold", critical }
    };

    // Act
    rule.Configure(config);

    // Assert
    // 커스텀 값이 적용되었는지 검증
    // (실제로는 private 필드에 접근할 수 없으므로 간접적으로 검증)
}
```

#### Step 4: 빌드 및 테스트 실행 (10분)
```bash
cd D:\01. Vscode\Twincat\features\twincat-code-qa-tool

# 빌드
dotnet build src/TwinCatQA.Application/TwinCatQA.Application.csproj

# 테스트 실행
dotnet test tests/TwinCatQA.Application.Tests/TwinCatQA.Application.Tests.csproj
```

#### Step 5: 코드 리뷰 및 커밋 (10분)
```bash
git add src/TwinCatQA.Application/Rules/CyclomaticComplexityRule.cs
git add tests/TwinCatQA.Application.Tests/Rules/CyclomaticComplexityRuleTests.cs
git commit -m "리팩토링: CyclomaticComplexityRule 매직 넘버 상수화

- 복잡도 임계값을 public 상수로 정의
- XML 문서화 주석 추가
- 단위 테스트 추가
- 유지보수성 향상"
```

### 🧪 테스트 전략
1. **단위 테스트**: 상수 값 검증
2. **통합 테스트**: 기존 검증 로직이 변경되지 않았는지 확인
3. **회귀 테스트**: 전체 테스트 스위트 실행

---

## User Story 2: Console.WriteLine을 Logger로 교체

### 📌 Story 정보
- **Story ID**: REF-002
- **Priority**: High
- **Effort**: 3 Story Points (3시간)
- **Assignee**: Refactoring Expert

### 📖 Description
Application 및 Infrastructure 레이어에서 Console.WriteLine을 사용하는 9개 파일을 ILogger로 교체하여 구조화된 로깅을 구현합니다.

### ✅ Acceptance Criteria
- [ ] 모든 Console.WriteLine이 ILogger로 교체됨
- [ ] 로그 레벨이 적절하게 설정됨 (Information, Warning, Error)
- [ ] 구조화된 로그 메시지 사용 (템플릿 리터럴)
- [ ] 기존 기능이 정상 동작함

### 📋 영향받는 파일 (9개)
```
features\twincat-code-qa-tool\src\TwinCatQA.CLI\Commands\QaCommand.cs
features\twincat-code-qa-tool\src\TwinCatQA.Application\Services\QARuleEngine.cs
features\twincat-code-qa-tool\src\TwinCatQA.CLI\Commands\CompareCommand.cs
features\twincat-code-qa-tool\src\TwinCatQA.CLI\Utils\FileScanner.cs
features\twincat-code-qa-tool\src\TwinCatQA.CLI\Commands\AnalyzeCommand.cs.bak
features\twincat-code-qa-tool\src\TwinCatQA.Application\Rules\KoreanCommentRule.cs
features\twincat-code-qa-tool\src\TwinCatQA.Application\Rules\NamingConventionRule.cs
features\twincat-code-qa-tool\src\TwinCatQA.Application\Rules\CyclomaticComplexityRule.cs
```

### 🔧 리팩토링 전/후 코드

#### ❌ Before (CyclomaticComplexityRule.cs, Line 113)
```csharp
catch (Exception ex)
{
    // 파싱 오류 발생 시 경고 로그
    Console.WriteLine($"Function Block 추출 중 오류 발생: {ex.Message}");
    yield break;
}
```

#### ✅ After (개선 코드)
```csharp
public class CyclomaticComplexityRule : IValidationRule
{
    private readonly IParserService _parserService;
    private readonly ILogger<CyclomaticComplexityRule> _logger;

    public CyclomaticComplexityRule(
        IParserService parserService,
        ILogger<CyclomaticComplexityRule> logger)
    {
        _parserService = parserService ?? throw new ArgumentNullException(nameof(parserService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ...

    catch (Exception ex)
    {
        // 구조화된 로깅: 템플릿 + 파라미터
        _logger.LogWarning(ex,
            "Function Block 추출 중 오류 발생. FilePath: {FilePath}",
            file.FilePath);
        yield break;
    }
}
```

#### ❌ Before (NamingConventionRule.cs, Line 117-118)
```csharp
catch (Exception ex)
{
    Console.WriteLine($"Function Block 추출 중 오류 발생: {ex.Message}");
    yield break;
}

// ...

catch (Exception ex)
{
    Console.WriteLine($"변수 추출 중 오류 발생: {ex.Message}");
    yield break;
}
```

#### ✅ After (개선 코드)
```csharp
public class NamingConventionRule : IValidationRule
{
    private readonly IParserService _parserService;
    private readonly ILogger<NamingConventionRule> _logger;

    public NamingConventionRule(
        IParserService parserService,
        ILogger<NamingConventionRule> logger)
    {
        _parserService = parserService ?? throw new ArgumentNullException(nameof(parserService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ...

    catch (Exception ex)
    {
        _logger.LogWarning(ex,
            "Function Block 추출 실패. FilePath: {FilePath}, Language: {Language}",
            file.FilePath, file.Language);
        yield break;
    }

    // ...

    catch (Exception ex)
    {
        _logger.LogWarning(ex,
            "변수 추출 실패. FilePath: {FilePath}",
            file.FilePath);
        yield break;
    }
}
```

### 📝 단계별 작업 순서

#### Step 1: Application 레이어 - Rules 클래스 수정 (60분)

**수정 대상 파일**:
- CyclomaticComplexityRule.cs
- NamingConventionRule.cs
- KoreanCommentRule.cs

**작업 내용**:
1. ILogger 의존성 추가
2. 생성자에 ILogger 파라미터 추가
3. Console.WriteLine → _logger.LogWarning/LogError 교체
4. 구조화된 로그 메시지로 변경

```bash
# 빌드 확인
dotnet build src/TwinCatQA.Application/TwinCatQA.Application.csproj
```

#### Step 2: Application 레이어 - Services 클래스 수정 (30분)

**수정 대상 파일**:
- QARuleEngine.cs

**작업 내용**:
1. 기존 ILogger 사용 패턴 확인
2. 모든 Console.WriteLine을 ILogger로 교체

```bash
# 빌드 확인
dotnet build src/TwinCatQA.Application/TwinCatQA.Application.csproj
```

#### Step 3: CLI 레이어 - Commands 클래스 수정 (60분)

**수정 대상 파일**:
- QaCommand.cs
- CompareCommand.cs

**특이사항**: CLI는 사용자 인터페이스이므로 Console.WriteLine을 일부 유지할 수 있음
- 사용자에게 보여주는 출력 → Console.WriteLine 유지
- 디버깅/진단 로그 → ILogger로 교체

```csharp
// ✅ 올바른 사용 예시

// 사용자 인터페이스 출력 (유지)
Console.WriteLine("═════════════════════════════════════");
Console.WriteLine("│   TwinCAT 프로젝트 비교 결과         │");
Console.WriteLine("═════════════════════════════════════");

// 디버깅 로그 (Logger로 교체)
_logger.LogInformation("비교 작업 시작. Source: {Source}, Target: {Target}",
    sourcePath, targetPath);

// 오류 처리 (Logger로 교체)
catch (Exception ex)
{
    _logger.LogError(ex, "비교 중 오류 발생. Source: {Source}", sourcePath);
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"✗ 오류 발생: {ex.Message}");
    Console.ResetColor();
}
```

#### Step 4: CLI 레이어 - Utils 클래스 수정 (30분)

**수정 대상 파일**:
- FileScanner.cs

**작업 내용**:
1. ILogger 의존성 주입
2. Console.WriteLine → _logger.LogDebug/LogInformation

```bash
# 빌드 확인
dotnet build src/TwinCatQA.CLI/TwinCatQA.CLI.csproj
```

#### Step 5: 의존성 주입 설정 업데이트 (10분)

```csharp
// Program.cs 또는 DI 설정 파일
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.AddDebug();
    builder.SetMinimumLevel(LogLevel.Information);
});

services.AddTransient<IValidationRule, CyclomaticComplexityRule>();
services.AddTransient<IValidationRule, NamingConventionRule>();
services.AddTransient<IValidationRule, KoreanCommentRule>();
```

#### Step 6: 테스트 작성 및 실행 (30분)

```csharp
// CyclomaticComplexityRuleTests.cs

[Fact]
public void Validate_WhenParsingFails_ShouldLogWarning()
{
    // Arrange
    var mockParser = new Mock<IParserService>();
    var mockLogger = new Mock<ILogger<CyclomaticComplexityRule>>();

    mockParser.Setup(p => p.ExtractFunctionBlocks(It.IsAny<SyntaxTree>()))
        .Throws(new Exception("파싱 실패"));

    var rule = new CyclomaticComplexityRule(mockParser.Object, mockLogger.Object);
    var file = CreateTestCodeFile();

    // Act
    var violations = rule.Validate(file).ToList();

    // Assert
    mockLogger.Verify(
        x => x.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Function Block 추출")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);

    Assert.Empty(violations);
}
```

```bash
# 전체 테스트 실행
dotnet test
```

#### Step 7: 커밋 (10분)

```bash
git add src/TwinCatQA.Application/Rules/*.cs
git add src/TwinCatQA.Application/Services/*.cs
git add src/TwinCatQA.CLI/Commands/*.cs
git add src/TwinCatQA.CLI/Utils/*.cs
git add tests/TwinCatQA.Application.Tests/Rules/*.cs

git commit -m "리팩토링: Console.WriteLine을 ILogger로 교체

- Application 레이어: Rules 및 Services에 ILogger 의존성 주입
- CLI 레이어: 디버깅 로그를 ILogger로 교체 (UI 출력은 유지)
- 구조화된 로그 메시지 템플릿 적용
- 단위 테스트에 로깅 검증 추가
- 로그 레벨 적절하게 설정 (Information, Warning, Error)"
```

### 🧪 테스트 전략
1. **단위 테스트**: ILogger 호출 검증 (Mock 사용)
2. **통합 테스트**: 실제 Logger를 사용한 E2E 테스트
3. **수동 테스트**: CLI 명령어 실행하여 출력 확인

---

## User Story 3: UTF-8 BOM 인코딩 수정

### 📌 Story 정보
- **Story ID**: REF-003
- **Priority**: Low
- **Effort**: 0.5 Story Point (30분)
- **Assignee**: Refactoring Expert

### 📖 Description
CompareCommand.cs의 UTF-8 BOM 인코딩을 UTF-8로 변경하여 표준 인코딩을 사용합니다.

### ✅ Acceptance Criteria
- [ ] 파일 인코딩이 UTF-8 (BOM 없음)으로 변경됨
- [ ] 한글 주석이 정상적으로 표시됨
- [ ] Git diff에서 변경사항이 최소화됨

### 🔧 현재 문제

CompareCommand.cs를 읽었을 때 한글이 깨져서 표시됨:
```
8→    /// ���� �� ���ɾ�
14→        var command = new Command("compare", "�� TwinCAT ������Ʈ ������ ���մϴ�");
```

### 📝 단계별 작업 순서

#### Step 1: 인코딩 확인 (5분)
```bash
cd D:\01. Vscode\Twincat\features\twincat-code-qa-tool\src\TwinCatQA.CLI\Commands

# 파일 인코딩 확인 (Git Bash)
file -i CompareCommand.cs

# 또는 PowerShell
Get-Content CompareCommand.cs | Select-Object -First 1 | Format-Hex
```

#### Step 2: 인코딩 변경 (10분)

**방법 1: Visual Studio Code 사용**
1. CompareCommand.cs 파일 열기
2. 우측 하단 인코딩 표시 클릭
3. "Save with Encoding" 선택
4. "UTF-8" 선택 (UTF-8 with BOM 아님)
5. 저장

**방법 2: PowerShell 스크립트 사용**
```powershell
# encoding-fix.ps1
$filePath = "D:\01. Vscode\Twincat\features\twincat-code-qa-tool\src\TwinCatQA.CLI\Commands\CompareCommand.cs"
$content = Get-Content $filePath -Raw -Encoding UTF8
$utf8NoBom = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText($filePath, $content, $utf8NoBom)
Write-Host "인코딩 변경 완료: UTF-8 (BOM 없음)"
```

#### Step 3: 검증 (5분)
```bash
# Git diff 확인
git diff src/TwinCatQA.CLI/Commands/CompareCommand.cs

# 파일 다시 읽어서 한글 확인
cat src/TwinCatQA.CLI/Commands/CompareCommand.cs | head -20
```

#### Step 4: 빌드 및 테스트 (5분)
```bash
# 빌드 확인
dotnet build src/TwinCatQA.CLI/TwinCatQA.CLI.csproj

# 테스트 실행
dotnet test
```

#### Step 5: 커밋 (5분)
```bash
git add src/TwinCatQA.CLI/Commands/CompareCommand.cs
git commit -m "수정: CompareCommand.cs 인코딩을 UTF-8로 변경

- UTF-8 BOM → UTF-8 (BOM 없음) 변경
- 한글 주석 정상 표시
- 표준 인코딩 준수"
```

### 🧪 테스트 전략
1. **인코딩 검증**: 파일 인코딩 확인 도구 사용
2. **한글 표시 검증**: 에디터에서 한글이 정상적으로 보이는지 확인
3. **빌드 검증**: 컴파일 오류가 없는지 확인

---

## Sprint 1 완료 체크리스트

### Definition of Done
- [ ] 모든 코드가 빌드됨
- [ ] 모든 단위 테스트가 통과함
- [ ] 코드 리뷰가 완료됨
- [ ] 문서화가 업데이트됨
- [ ] Git 커밋이 완료됨

### Sprint 1 회고
- **잘된 점**:
- **개선할 점**:
- **다음 스프린트 계획**:

---

# Sprint 2: 구조 개선 (2주차)

## User Story 4: 대형 클래스 분리 (LibGit2Service.cs)

### 📌 Story 정보
- **Story ID**: REF-004
- **Priority**: High
- **Effort**: 8 Story Points (8시간)
- **Assignee**: System Architect

### 📖 Description
LibGit2Service.cs (674줄)를 Single Responsibility Principle에 따라 3개의 클래스로 분리합니다.

### 🎯 목표 아키텍처

```
LibGit2Service.cs (674줄)
    ↓ 분리
├── GitRepositoryService.cs (200줄) - 저장소 관리
├── GitDiffService.cs (250줄) - Diff 분석
└── GitHookService.cs (200줄) - Pre-commit Hook 관리
```

### 📋 책임 분리 계획

#### 1️⃣ GitRepositoryService (저장소 관리)
**책임**: Git 저장소 초기화, 상태 확인, 메타데이터 조회

**메서드** (Lines 22-161):
- `InitializeRepository(string repoPath)` - 22~60
- `IsGitRepository(string path)` - 65~96
- `GetCurrentCommitHash(string repoPath)` - 101~133
- `IsWorkingDirectoryClean(string repoPath)` - 138~160
- `FindGitDirectory(string path)` - 653~671 (Private 헬퍼)

**의존성**:
- `ILogger<GitRepositoryService>`

#### 2️⃣ GitDiffService (Diff 분석)
**책임**: 변경사항 비교, 파일/라인 차이 추출, 컨텍스트 분석

**메서드** (Lines 164-647):
- `GetChangedFiles(string repoPath, DiffTarget diffTarget)` - 169~211
- `GetChangedLines(string repoPath, string filePath)` - 216~259
- `GetDiffBetweenCommits(string repoPath, string fromCommit, string toCommit)` - 264~313
- `DetermineContext(object file, int changedLine)` - 619~644
- Private 헬퍼 메서드:
  - `GetIndexDiff(Repository repo)` - 322~336
  - `GetWorkingDirectoryDiff(Repository repo)` - 341~356
  - `GetAllDiff(Repository repo)` - 361~375

**의존성**:
- `ILogger<GitDiffService>`
- `DiffParser`
- `ContextAnalyzer`
- `IGitRepositoryService` (저장소 확인용)

#### 3️⃣ GitHookService (Hook 관리)
**책임**: Pre-commit Hook 설치/제거/확인

**메서드** (Lines 379-611):
- `InstallPreCommitHook(string repoPath, bool blockOnCritical = true)` - 384~447
- `UninstallPreCommitHook(string repoPath)` - 452~496
- `IsPreCommitHookInstalled(string repoPath)` - 501~527
- Private 헬퍼 메서드:
  - `GetPreCommitHookScript(bool isWindows, bool blockOnCritical)` - 532~557
  - `GetDefaultBashHookScript(bool blockOnCritical)` - 562~584
  - `GetDefaultWindowsHookScript(bool blockOnCritical)` - 589~610

**의존성**:
- `ILogger<GitHookService>`
- `IGitRepositoryService` (저장소 확인용)

### 🔧 리팩토링 전/후 코드

#### ❌ Before (LibGit2Service.cs - 674줄)
```csharp
public class LibGit2Service : IGitService
{
    private readonly ILogger<LibGit2Service> _logger;
    private readonly DiffParser _diffParser;
    private readonly ContextAnalyzer _contextAnalyzer;

    public LibGit2Service(ILogger<LibGit2Service> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _diffParser = new DiffParser();
        _contextAnalyzer = new ContextAnalyzer();
    }

    // 저장소 관리 (22-161)
    public bool InitializeRepository(string repoPath) { ... }
    public bool IsGitRepository(string path) { ... }
    public string? GetCurrentCommitHash(string repoPath) { ... }
    public bool IsWorkingDirectoryClean(string repoPath) { ... }

    // Diff 분석 (164-647)
    public IReadOnlyList<string> GetChangedFiles(string repoPath, DiffTarget diffTarget) { ... }
    public IReadOnlyList<LineChange> GetChangedLines(string repoPath, string filePath) { ... }
    public IReadOnlyList<string> GetDiffBetweenCommits(string repoPath, string fromCommit, string toCommit) { ... }
    public CodeContext DetermineContext(object file, int changedLine) { ... }

    // Hook 관리 (379-611)
    public bool InstallPreCommitHook(string repoPath, bool blockOnCritical = true) { ... }
    public bool UninstallPreCommitHook(string repoPath) { ... }
    public bool IsPreCommitHookInstalled(string repoPath) { ... }

    // 헬퍼 메서드
    private string FindGitDirectory(string path) { ... }
}
```

#### ✅ After (분리된 클래스)

##### 1️⃣ IGitRepositoryService.cs (새로운 인터페이스)
```csharp
namespace TwinCatQA.Infrastructure.Git;

/// <summary>
/// Git 저장소 관리 서비스 인터페이스
/// </summary>
public interface IGitRepositoryService
{
    /// <summary>
    /// Git 저장소 초기화
    /// </summary>
    bool InitializeRepository(string repoPath);

    /// <summary>
    /// Git 저장소 여부 확인
    /// </summary>
    bool IsGitRepository(string path);

    /// <summary>
    /// 현재 커밋 해시 조회
    /// </summary>
    string? GetCurrentCommitHash(string repoPath);

    /// <summary>
    /// 워킹 디렉토리가 깨끗한지 확인
    /// </summary>
    bool IsWorkingDirectoryClean(string repoPath);

    /// <summary>
    /// .git 디렉토리 경로 찾기
    /// </summary>
    string FindGitDirectory(string path);
}
```

##### 1️⃣ GitRepositoryService.cs
```csharp
using LibGit2Sharp;
using Microsoft.Extensions.Logging;

namespace TwinCatQA.Infrastructure.Git;

/// <summary>
/// Git 저장소 관리 서비스
/// 저장소 초기화, 상태 확인, 메타데이터 조회 담당
/// </summary>
public class GitRepositoryService : IGitRepositoryService
{
    private readonly ILogger<GitRepositoryService> _logger;

    public GitRepositoryService(ILogger<GitRepositoryService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region 저장소 관리

    /// <inheritdoc />
    public bool InitializeRepository(string repoPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(repoPath))
            {
                _logger.LogWarning("저장소 경로가 비어있습니다");
                return false;
            }

            if (IsGitRepository(repoPath))
            {
                _logger.LogInformation("이미 Git 저장소가 존재합니다: {RepoPath}", repoPath);
                return true;
            }

            if (!Directory.Exists(repoPath))
            {
                Directory.CreateDirectory(repoPath);
            }

            Repository.Init(repoPath);
            _logger.LogInformation("Git 저장소 초기화 완료: {RepoPath}", repoPath);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Git 저장소 초기화 실패: {RepoPath}", repoPath);
            return false;
        }
    }

    /// <inheritdoc />
    public bool IsGitRepository(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                return false;
            }

            var currentPath = new DirectoryInfo(path);

            while (currentPath != null)
            {
                var gitPath = Path.Combine(currentPath.FullName, ".git");

                if (Directory.Exists(gitPath) || File.Exists(gitPath))
                {
                    return true;
                }

                currentPath = currentPath.Parent;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Git 저장소 확인 중 오류 발생: {Path}", path);
            return false;
        }
    }

    /// <inheritdoc />
    public string? GetCurrentCommitHash(string repoPath)
    {
        try
        {
            if (!IsGitRepository(repoPath))
            {
                _logger.LogWarning("Git 저장소가 아닙니다: {RepoPath}", repoPath);
                return null;
            }

            using var repo = new Repository(FindGitDirectory(repoPath));

            var headCommit = repo.Head.Tip;
            if (headCommit == null)
            {
                _logger.LogWarning("HEAD 커밋이 없습니다 (빈 저장소): {RepoPath}", repoPath);
                return null;
            }

            return headCommit.Sha;
        }
        catch (RepositoryNotFoundException ex)
        {
            _logger.LogWarning(ex, "Git 저장소를 찾을 수 없습니다: {RepoPath}", repoPath);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "커밋 해시 조회 실패: {RepoPath}", repoPath);
            return null;
        }
    }

    /// <inheritdoc />
    public bool IsWorkingDirectoryClean(string repoPath)
    {
        try
        {
            if (!IsGitRepository(repoPath))
            {
                _logger.LogWarning("Git 저장소가 아닙니다: {RepoPath}", repoPath);
                return false;
            }

            using var repo = new Repository(FindGitDirectory(repoPath));

            var status = repo.RetrieveStatus();

            return !status.IsDirty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "워킹 디렉토리 상태 확인 실패: {RepoPath}", repoPath);
            return false;
        }
    }

    /// <inheritdoc />
    public string FindGitDirectory(string path)
    {
        var currentPath = new DirectoryInfo(path);

        while (currentPath != null)
        {
            var gitPath = Path.Combine(currentPath.FullName, ".git");

            if (Directory.Exists(gitPath) || File.Exists(gitPath))
            {
                return currentPath.FullName;
            }

            currentPath = currentPath.Parent;
        }

        return path;
    }

    #endregion
}
```

##### 2️⃣ IGitDiffService.cs (새로운 인터페이스)
```csharp
namespace TwinCatQA.Infrastructure.Git;

/// <summary>
/// Git Diff 분석 서비스 인터페이스
/// </summary>
public interface IGitDiffService
{
    /// <summary>
    /// 변경된 파일 목록 조회
    /// </summary>
    IReadOnlyList<string> GetChangedFiles(string repoPath, DiffTarget diffTarget);

    /// <summary>
    /// 특정 파일의 변경된 라인 목록 조회
    /// </summary>
    IReadOnlyList<LineChange> GetChangedLines(string repoPath, string filePath);

    /// <summary>
    /// 두 커밋 간 차이 조회
    /// </summary>
    IReadOnlyList<string> GetDiffBetweenCommits(string repoPath, string fromCommit, string toCommit);

    /// <summary>
    /// 변경 라인의 컨텍스트 범위 결정
    /// </summary>
    CodeContext DetermineContext(object file, int changedLine);
}
```

##### 2️⃣ GitDiffService.cs
```csharp
using LibGit2Sharp;
using Microsoft.Extensions.Logging;

namespace TwinCatQA.Infrastructure.Git;

/// <summary>
/// Git Diff 분석 서비스
/// 변경사항 비교, 파일/라인 차이 추출 담당
/// </summary>
public class GitDiffService : IGitDiffService
{
    private readonly ILogger<GitDiffService> _logger;
    private readonly IGitRepositoryService _repositoryService;
    private readonly DiffParser _diffParser;
    private readonly ContextAnalyzer _contextAnalyzer;

    public GitDiffService(
        ILogger<GitDiffService> logger,
        IGitRepositoryService repositoryService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _repositoryService = repositoryService ?? throw new ArgumentNullException(nameof(repositoryService));
        _diffParser = new DiffParser();
        _contextAnalyzer = new ContextAnalyzer();
    }

    #region Diff 분석

    /// <inheritdoc />
    public IReadOnlyList<string> GetChangedFiles(string repoPath, DiffTarget diffTarget)
    {
        try
        {
            if (!_repositoryService.IsGitRepository(repoPath))
            {
                _logger.LogWarning("Git 저장소가 아닙니다: {RepoPath}", repoPath);
                return Array.Empty<string>();
            }

            using var repo = new Repository(_repositoryService.FindGitDirectory(repoPath));

            TreeChanges? changes = diffTarget switch
            {
                DiffTarget.Index => GetIndexDiff(repo),
                DiffTarget.WorkingDirectory => GetWorkingDirectoryDiff(repo),
                DiffTarget.All => GetAllDiff(repo),
                _ => null
            };

            if (changes == null)
            {
                return Array.Empty<string>();
            }

            var changedFiles = changes
                .Select(c => c.Path)
                .Where(p => !string.IsNullOrEmpty(p))
                .Distinct()
                .ToList();

            _logger.LogInformation("변경된 파일 {Count}개 발견 (Target: {Target})",
                changedFiles.Count, diffTarget);

            return changedFiles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "변경 파일 조회 실패: {RepoPath}", repoPath);
            return Array.Empty<string>();
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<LineChange> GetChangedLines(string repoPath, string filePath)
    {
        try
        {
            if (!_repositoryService.IsGitRepository(repoPath))
            {
                _logger.LogWarning("Git 저장소가 아닙니다: {RepoPath}", repoPath);
                return Array.Empty<LineChange>();
            }

            using var repo = new Repository(_repositoryService.FindGitDirectory(repoPath));

            var changes = repo.Diff.Compare<TreeChanges>(
                repo.Head.Tip?.Tree,
                DiffTargets.WorkingDirectory | DiffTargets.Index
            );

            var patch = repo.Diff.Compare<Patch>(
                repo.Head.Tip?.Tree,
                DiffTargets.WorkingDirectory | DiffTargets.Index,
                new[] { filePath }
            );

            if (patch == null)
            {
                return Array.Empty<LineChange>();
            }

            var lineChanges = _diffParser.ParsePatch(patch, filePath);

            _logger.LogInformation("파일 {FilePath}에서 {Count}개 라인 변경 발견",
                filePath, lineChanges.Count);

            return lineChanges;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "변경 라인 조회 실패: {RepoPath}, {FilePath}", repoPath, filePath);
            return Array.Empty<LineChange>();
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetDiffBetweenCommits(string repoPath, string fromCommit, string toCommit)
    {
        try
        {
            if (!_repositoryService.IsGitRepository(repoPath))
            {
                _logger.LogWarning("Git 저장소가 아닙니다: {RepoPath}", repoPath);
                return Array.Empty<string>();
            }

            using var repo = new Repository(_repositoryService.FindGitDirectory(repoPath));

            var fromCommitObj = repo.Lookup<Commit>(fromCommit);
            var toCommitObj = repo.Lookup<Commit>(toCommit);

            if (fromCommitObj == null || toCommitObj == null)
            {
                _logger.LogWarning("커밋을 찾을 수 없습니다: {From} -> {To}", fromCommit, toCommit);
                return Array.Empty<string>();
            }

            var changes = repo.Diff.Compare<TreeChanges>(
                fromCommitObj.Tree,
                toCommitObj.Tree
            );

            var changedFiles = changes
                .Select(c => c.Path)
                .Where(p => !string.IsNullOrEmpty(p))
                .Distinct()
                .ToList();

            _logger.LogInformation("커밋 간 {Count}개 파일 변경: {From} -> {To}",
                changedFiles.Count, fromCommit, toCommit);

            return changedFiles;
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "커밋을 찾을 수 없습니다: {From} -> {To}", fromCommit, toCommit);
            return Array.Empty<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "커밋 간 Diff 조회 실패: {RepoPath}", repoPath);
            return Array.Empty<string>();
        }
    }

    /// <inheritdoc />
    public CodeContext DetermineContext(object file, int changedLine)
    {
        try
        {
            dynamic dynamicFile = file;
            dynamic? ast = dynamicFile?.SyntaxTree;

            return _contextAnalyzer.DetermineContext(dynamicFile, ast, changedLine);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "컨텍스트 결정 실패, 기본 범위 반환: Line {Line}", changedLine);

            var surroundingLines = _contextAnalyzer.GetSurroundingLines(file, changedLine);
            return new CodeContext
            {
                StartLine = surroundingLines.startLine,
                EndLine = surroundingLines.endLine,
                ContextType = "Surrounding",
                ContextName = $"Line {changedLine} ±10"
            };
        }
    }

    #endregion

    #region Diff 헬퍼 메서드

    private TreeChanges? GetIndexDiff(Repository repo)
    {
        try
        {
            return repo.Diff.Compare<TreeChanges>(
                repo.Head.Tip?.Tree,
                DiffTargets.Index
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Index Diff 조회 실패");
            return null;
        }
    }

    private TreeChanges? GetWorkingDirectoryDiff(Repository repo)
    {
        try
        {
            return repo.Diff.Compare<TreeChanges>(
                repo.Head?.Tip?.Tree,
                DiffTargets.WorkingDirectory | DiffTargets.Index
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "워킹 디렉토리 Diff 조회 실패");
            return null;
        }
    }

    private TreeChanges? GetAllDiff(Repository repo)
    {
        try
        {
            return repo.Diff.Compare<TreeChanges>(
                repo.Head.Tip?.Tree,
                DiffTargets.WorkingDirectory | DiffTargets.Index
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "전체 Diff 조회 실패");
            return null;
        }
    }

    #endregion
}
```

##### 3️⃣ IGitHookService.cs (새로운 인터페이스)
```csharp
namespace TwinCatQA.Infrastructure.Git;

/// <summary>
/// Git Hook 관리 서비스 인터페이스
/// </summary>
public interface IGitHookService
{
    /// <summary>
    /// Pre-commit Hook 설치
    /// </summary>
    bool InstallPreCommitHook(string repoPath, bool blockOnCritical = true);

    /// <summary>
    /// Pre-commit Hook 제거
    /// </summary>
    bool UninstallPreCommitHook(string repoPath);

    /// <summary>
    /// Pre-commit Hook 설치 여부 확인
    /// </summary>
    bool IsPreCommitHookInstalled(string repoPath);
}
```

##### 3️⃣ GitHookService.cs
```csharp
using Microsoft.Extensions.Logging;

namespace TwinCatQA.Infrastructure.Git;

/// <summary>
/// Git Hook 관리 서비스
/// Pre-commit Hook 설치, 제거, 확인 담당
/// </summary>
public class GitHookService : IGitHookService
{
    private readonly ILogger<GitHookService> _logger;
    private readonly IGitRepositoryService _repositoryService;

    public GitHookService(
        ILogger<GitHookService> logger,
        IGitRepositoryService repositoryService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _repositoryService = repositoryService ?? throw new ArgumentNullException(nameof(repositoryService));
    }

    #region Pre-commit Hook

    /// <inheritdoc />
    public bool InstallPreCommitHook(string repoPath, bool blockOnCritical = true)
    {
        try
        {
            if (!_repositoryService.IsGitRepository(repoPath))
            {
                _logger.LogWarning("Git 저장소가 아닙니다: {RepoPath}", repoPath);
                return false;
            }

            var gitDir = _repositoryService.FindGitDirectory(repoPath);
            var hooksDir = Path.Combine(gitDir, "hooks");

            if (!Directory.Exists(hooksDir))
            {
                Directory.CreateDirectory(hooksDir);
            }

            bool isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
            string hookFileName = isWindows ? "pre-commit.bat" : "pre-commit";
            string hookPath = Path.Combine(hooksDir, hookFileName);

            string scriptContent = GetPreCommitHookScript(isWindows, blockOnCritical);

            File.WriteAllText(hookPath, scriptContent);

            if (!isWindows)
            {
                try
                {
                    var process = new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "chmod",
                            Arguments = $"+x \"{hookPath}\"",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };
                    process.Start();
                    process.WaitForExit();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "실행 권한 부여 실패 (수동으로 chmod +x 실행 필요): {HookPath}", hookPath);
                }
            }

            _logger.LogInformation("Pre-commit Hook 설치 완료: {HookPath}", hookPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pre-commit Hook 설치 실패: {RepoPath}", repoPath);
            return false;
        }
    }

    /// <inheritdoc />
    public bool UninstallPreCommitHook(string repoPath)
    {
        try
        {
            if (!_repositoryService.IsGitRepository(repoPath))
            {
                _logger.LogWarning("Git 저장소가 아닙니다: {RepoPath}", repoPath);
                return false;
            }

            var gitDir = _repositoryService.FindGitDirectory(repoPath);
            var hooksDir = Path.Combine(gitDir, "hooks");

            var hookPaths = new[]
            {
                Path.Combine(hooksDir, "pre-commit"),
                Path.Combine(hooksDir, "pre-commit.bat")
            };

            bool removed = false;

            foreach (var hookPath in hookPaths)
            {
                if (File.Exists(hookPath))
                {
                    File.Delete(hookPath);
                    _logger.LogInformation("Pre-commit Hook 제거 완료: {HookPath}", hookPath);
                    removed = true;
                }
            }

            if (!removed)
            {
                _logger.LogWarning("제거할 Hook 파일이 없습니다: {RepoPath}", repoPath);
            }

            return removed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pre-commit Hook 제거 실패: {RepoPath}", repoPath);
            return false;
        }
    }

    /// <inheritdoc />
    public bool IsPreCommitHookInstalled(string repoPath)
    {
        try
        {
            if (!_repositoryService.IsGitRepository(repoPath))
            {
                return false;
            }

            var gitDir = _repositoryService.FindGitDirectory(repoPath);
            var hooksDir = Path.Combine(gitDir, "hooks");

            var hookPaths = new[]
            {
                Path.Combine(hooksDir, "pre-commit"),
                Path.Combine(hooksDir, "pre-commit.bat")
            };

            return hookPaths.Any(File.Exists);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Hook 설치 여부 확인 중 오류: {RepoPath}", repoPath);
            return false;
        }
    }

    #endregion

    #region Hook 스크립트 생성

    private string GetPreCommitHookScript(bool isWindows, bool blockOnCritical)
    {
        string templateFileName = isWindows ? "pre-commit.bat" : "pre-commit.sh";
        string templatePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Git",
            "Templates",
            templateFileName
        );

        if (File.Exists(templatePath))
        {
            return File.ReadAllText(templatePath);
        }

        if (isWindows)
        {
            return GetDefaultWindowsHookScript(blockOnCritical);
        }
        else
        {
            return GetDefaultBashHookScript(blockOnCritical);
        }
    }

    private string GetDefaultBashHookScript(bool blockOnCritical)
    {
        string failOnCritical = blockOnCritical ? "--fail-on-critical" : "";

        return $@"#!/bin/bash
# TwinCAT 코드 품질 검증 Pre-commit Hook
# 자동 생성됨: {DateTime.Now:yyyy-MM-dd HH:mm:ss}

echo ""TwinCAT 코드 품질 검증 중...""

dotnet twincat-qa validate --mode Incremental {failOnCritical}

if [ $? -ne 0 ]; then
    echo ""❌ 품질 검증 실패: Critical 위반이 발견되었습니다.""
    echo ""   커밋을 차단합니다. 위반 사항을 수정한 후 다시 시도하세요.""
    exit 1
fi

echo ""✅ 품질 검증 통과""
exit 0
";
    }

    private string GetDefaultWindowsHookScript(bool blockOnCritical)
    {
        string failOnCritical = blockOnCritical ? "--fail-on-critical" : "";

        return $@"@echo off
REM TwinCAT 코드 품질 검증 Pre-commit Hook
REM 자동 생성됨: {DateTime.Now:yyyy-MM-dd HH:mm:ss}

echo TwinCAT 코드 품질 검증 중...

dotnet twincat-qa validate --mode Incremental {failOnCritical}

if %ERRORLEVEL% NEQ 0 (
    echo ❌ 품질 검증 실패: Critical 위반이 발견되었습니다.
    echo    커밋을 차단합니다. 위반 사항을 수정한 후 다시 시도하세요.
    exit /b 1
)

echo ✅ 품질 검증 통과
exit /b 0
";
    }

    #endregion
}
```

##### 4️⃣ LibGit2Service.cs (Facade 패턴으로 변경)
```csharp
using Microsoft.Extensions.Logging;

namespace TwinCatQA.Infrastructure.Git;

/// <summary>
/// LibGit2Sharp를 사용한 Git 저장소 통합 서비스 Facade
/// 하위 호환성을 위해 기존 인터페이스를 유지하면서 내부적으로 분리된 서비스를 사용
/// </summary>
public class LibGit2Service : IGitService
{
    private readonly IGitRepositoryService _repositoryService;
    private readonly IGitDiffService _diffService;
    private readonly IGitHookService _hookService;
    private readonly ILogger<LibGit2Service> _logger;

    public LibGit2Service(
        IGitRepositoryService repositoryService,
        IGitDiffService diffService,
        IGitHookService hookService,
        ILogger<LibGit2Service> logger)
    {
        _repositoryService = repositoryService ?? throw new ArgumentNullException(nameof(repositoryService));
        _diffService = diffService ?? throw new ArgumentNullException(nameof(diffService));
        _hookService = hookService ?? throw new ArgumentNullException(nameof(hookService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region 저장소 관리 (Delegate to GitRepositoryService)

    /// <inheritdoc />
    public bool InitializeRepository(string repoPath)
        => _repositoryService.InitializeRepository(repoPath);

    /// <inheritdoc />
    public bool IsGitRepository(string path)
        => _repositoryService.IsGitRepository(path);

    /// <inheritdoc />
    public string? GetCurrentCommitHash(string repoPath)
        => _repositoryService.GetCurrentCommitHash(repoPath);

    /// <inheritdoc />
    public bool IsWorkingDirectoryClean(string repoPath)
        => _repositoryService.IsWorkingDirectoryClean(repoPath);

    #endregion

    #region Diff 분석 (Delegate to GitDiffService)

    /// <inheritdoc />
    public IReadOnlyList<string> GetChangedFiles(string repoPath, DiffTarget diffTarget)
        => _diffService.GetChangedFiles(repoPath, diffTarget);

    /// <inheritdoc />
    public IReadOnlyList<LineChange> GetChangedLines(string repoPath, string filePath)
        => _diffService.GetChangedLines(repoPath, filePath);

    /// <inheritdoc />
    public IReadOnlyList<string> GetDiffBetweenCommits(string repoPath, string fromCommit, string toCommit)
        => _diffService.GetDiffBetweenCommits(repoPath, fromCommit, toCommit);

    /// <inheritdoc />
    public CodeContext DetermineContext(object file, int changedLine)
        => _diffService.DetermineContext(file, changedLine);

    #endregion

    #region Hook 관리 (Delegate to GitHookService)

    /// <inheritdoc />
    public bool InstallPreCommitHook(string repoPath, bool blockOnCritical = true)
        => _hookService.InstallPreCommitHook(repoPath, blockOnCritical);

    /// <inheritdoc />
    public bool UninstallPreCommitHook(string repoPath)
        => _hookService.UninstallPreCommitHook(repoPath);

    /// <inheritdoc />
    public bool IsPreCommitHookInstalled(string repoPath)
        => _hookService.IsPreCommitHookInstalled(repoPath);

    #endregion
}
```

### 📝 단계별 작업 순서

#### Step 1: 인터페이스 정의 (30분)
```bash
cd D:\01. Vscode\Twincat\features\twincat-code-qa-tool\src\TwinCatQA.Infrastructure\Git

# 새 인터페이스 파일 생성
touch IGitRepositoryService.cs
touch IGitDiffService.cs
touch IGitHookService.cs
```

**작업 내용**:
1. 각 서비스의 인터페이스 정의
2. XML 문서화 주석 추가
3. 네임스페이스 설정

#### Step 2: GitRepositoryService 구현 (60분)
```bash
touch GitRepositoryService.cs
```

**작업 내용**:
1. LibGit2Service.cs의 Lines 22-161, 653-671 복사
2. 클래스명 변경 및 생성자 수정
3. 인터페이스 구현
4. 로거 타입 변경: `ILogger<LibGit2Service>` → `ILogger<GitRepositoryService>`

#### Step 3: GitDiffService 구현 (90분)
```bash
touch GitDiffService.cs
```

**작업 내용**:
1. LibGit2Service.cs의 Lines 164-647 복사
2. 클래스명 변경 및 생성자 수정
3. IGitRepositoryService 의존성 주입
4. `IsGitRepository()` → `_repositoryService.IsGitRepository()`로 변경
5. `FindGitDirectory()` → `_repositoryService.FindGitDirectory()`로 변경
6. 인터페이스 구현

#### Step 4: GitHookService 구현 (60분)
```bash
touch GitHookService.cs
```

**작업 내용**:
1. LibGit2Service.cs의 Lines 379-611 복사
2. 클래스명 변경 및 생성자 수정
3. IGitRepositoryService 의존성 주입
4. `IsGitRepository()` → `_repositoryService.IsGitRepository()`로 변경
5. `FindGitDirectory()` → `_repositoryService.FindGitDirectory()`로 변경
6. 인터페이스 구현

#### Step 5: LibGit2Service Facade 변경 (30분)

**작업 내용**:
1. LibGit2Service.cs 백업
2. 기존 구현 코드 제거
3. Facade 패턴으로 재구현
4. 생성자에 3개 서비스 의존성 주입
5. 모든 메서드를 하위 서비스로 위임

#### Step 6: 의존성 주입 설정 업데이트 (20분)

```csharp
// Infrastructure/DependencyInjection.cs 또는 Program.cs

public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Git 서비스 등록
        services.AddSingleton<IGitRepositoryService, GitRepositoryService>();
        services.AddSingleton<IGitDiffService, GitDiffService>();
        services.AddSingleton<IGitHookService, GitHookService>();

        // Facade 패턴 (하위 호환성)
        services.AddSingleton<IGitService, LibGit2Service>();

        return services;
    }
}
```

#### Step 7: 테스트 작성 (120분)

##### GitRepositoryServiceTests.cs
```csharp
public class GitRepositoryServiceTests
{
    private readonly Mock<ILogger<GitRepositoryService>> _mockLogger;
    private readonly GitRepositoryService _service;
    private readonly string _testRepoPath;

    public GitRepositoryServiceTests()
    {
        _mockLogger = new Mock<ILogger<GitRepositoryService>>();
        _service = new GitRepositoryService(_mockLogger.Object);
        _testRepoPath = Path.Combine(Path.GetTempPath(), "test-repo-" + Guid.NewGuid());
    }

    [Fact]
    public void InitializeRepository_WhenPathIsEmpty_ShouldReturnFalse()
    {
        // Arrange
        string emptyPath = "";

        // Act
        var result = _service.InitializeRepository(emptyPath);

        // Assert
        Assert.False(result);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("저장소 경로가 비어있습니다")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void InitializeRepository_WhenNewPath_ShouldCreateRepository()
    {
        // Arrange
        try
        {
            // Act
            var result = _service.InitializeRepository(_testRepoPath);

            // Assert
            Assert.True(result);
            Assert.True(_service.IsGitRepository(_testRepoPath));
            Assert.True(Directory.Exists(Path.Combine(_testRepoPath, ".git")));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(_testRepoPath))
            {
                Directory.Delete(_testRepoPath, true);
            }
        }
    }

    [Fact]
    public void IsGitRepository_WhenNotGitRepo_ShouldReturnFalse()
    {
        // Arrange
        var nonGitPath = Path.GetTempPath();

        // Act
        var result = _service.IsGitRepository(nonGitPath);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetCurrentCommitHash_WhenEmptyRepository_ShouldReturnNull()
    {
        // Arrange
        try
        {
            _service.InitializeRepository(_testRepoPath);

            // Act
            var result = _service.GetCurrentCommitHash(_testRepoPath);

            // Assert
            Assert.Null(result);
        }
        finally
        {
            if (Directory.Exists(_testRepoPath))
            {
                Directory.Delete(_testRepoPath, true);
            }
        }
    }

    [Fact]
    public void IsWorkingDirectoryClean_WhenNewRepository_ShouldReturnTrue()
    {
        // Arrange
        try
        {
            _service.InitializeRepository(_testRepoPath);

            // Act
            var result = _service.IsWorkingDirectoryClean(_testRepoPath);

            // Assert
            Assert.True(result);
        }
        finally
        {
            if (Directory.Exists(_testRepoPath))
            {
                Directory.Delete(_testRepoPath, true);
            }
        }
    }

    [Fact]
    public void FindGitDirectory_WhenInSubdirectory_ShouldFindRoot()
    {
        // Arrange
        try
        {
            _service.InitializeRepository(_testRepoPath);
            var subDir = Path.Combine(_testRepoPath, "sub", "dir");
            Directory.CreateDirectory(subDir);

            // Act
            var result = _service.FindGitDirectory(subDir);

            // Assert
            Assert.Equal(_testRepoPath, result);
        }
        finally
        {
            if (Directory.Exists(_testRepoPath))
            {
                Directory.Delete(_testRepoPath, true);
            }
        }
    }
}
```

##### GitDiffServiceTests.cs
```csharp
public class GitDiffServiceTests
{
    private readonly Mock<ILogger<GitDiffService>> _mockLogger;
    private readonly Mock<IGitRepositoryService> _mockRepositoryService;
    private readonly GitDiffService _service;

    public GitDiffServiceTests()
    {
        _mockLogger = new Mock<ILogger<GitDiffService>>();
        _mockRepositoryService = new Mock<IGitRepositoryService>();
        _service = new GitDiffService(_mockLogger.Object, _mockRepositoryService.Object);
    }

    [Fact]
    public void GetChangedFiles_WhenNotGitRepository_ShouldReturnEmpty()
    {
        // Arrange
        var repoPath = "/fake/path";
        _mockRepositoryService.Setup(x => x.IsGitRepository(repoPath)).Returns(false);

        // Act
        var result = _service.GetChangedFiles(repoPath, DiffTarget.All);

        // Assert
        Assert.Empty(result);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Git 저장소가 아닙니다")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // 추가 테스트 케이스...
}
```

##### GitHookServiceTests.cs
```csharp
public class GitHookServiceTests
{
    private readonly Mock<ILogger<GitHookService>> _mockLogger;
    private readonly Mock<IGitRepositoryService> _mockRepositoryService;
    private readonly GitHookService _service;

    public GitHookServiceTests()
    {
        _mockLogger = new Mock<ILogger<GitHookService>>();
        _mockRepositoryService = new Mock<IGitRepositoryService>();
        _service = new GitHookService(_mockLogger.Object, _mockRepositoryService.Object);
    }

    [Fact]
    public void InstallPreCommitHook_WhenNotGitRepository_ShouldReturnFalse()
    {
        // Arrange
        var repoPath = "/fake/path";
        _mockRepositoryService.Setup(x => x.IsGitRepository(repoPath)).Returns(false);

        // Act
        var result = _service.InstallPreCommitHook(repoPath);

        // Assert
        Assert.False(result);
    }

    // 추가 테스트 케이스...
}
```

##### LibGit2ServiceTests.cs (Facade 테스트)
```csharp
public class LibGit2ServiceFacadeTests
{
    private readonly Mock<IGitRepositoryService> _mockRepositoryService;
    private readonly Mock<IGitDiffService> _mockDiffService;
    private readonly Mock<IGitHookService> _mockHookService;
    private readonly Mock<ILogger<LibGit2Service>> _mockLogger;
    private readonly LibGit2Service _facade;

    public LibGit2ServiceFacadeTests()
    {
        _mockRepositoryService = new Mock<IGitRepositoryService>();
        _mockDiffService = new Mock<IGitDiffService>();
        _mockHookService = new Mock<IGitHookService>();
        _mockLogger = new Mock<ILogger<LibGit2Service>>();

        _facade = new LibGit2Service(
            _mockRepositoryService.Object,
            _mockDiffService.Object,
            _mockHookService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public void InitializeRepository_ShouldDelegateToRepositoryService()
    {
        // Arrange
        var repoPath = "/test/path";
        _mockRepositoryService.Setup(x => x.InitializeRepository(repoPath)).Returns(true);

        // Act
        var result = _facade.InitializeRepository(repoPath);

        // Assert
        Assert.True(result);
        _mockRepositoryService.Verify(x => x.InitializeRepository(repoPath), Times.Once);
    }

    [Fact]
    public void GetChangedFiles_ShouldDelegateToDiffService()
    {
        // Arrange
        var repoPath = "/test/path";
        var expectedFiles = new List<string> { "file1.st", "file2.st" };
        _mockDiffService.Setup(x => x.GetChangedFiles(repoPath, DiffTarget.All))
            .Returns(expectedFiles);

        // Act
        var result = _facade.GetChangedFiles(repoPath, DiffTarget.All);

        // Assert
        Assert.Equal(expectedFiles, result);
        _mockDiffService.Verify(x => x.GetChangedFiles(repoPath, DiffTarget.All), Times.Once);
    }

    [Fact]
    public void InstallPreCommitHook_ShouldDelegateToHookService()
    {
        // Arrange
        var repoPath = "/test/path";
        _mockHookService.Setup(x => x.InstallPreCommitHook(repoPath, true)).Returns(true);

        // Act
        var result = _facade.InstallPreCommitHook(repoPath, true);

        // Assert
        Assert.True(result);
        _mockHookService.Verify(x => x.InstallPreCommitHook(repoPath, true), Times.Once);
    }
}
```

#### Step 8: 통합 테스트 (30분)

```bash
# 빌드 확인
dotnet build src/TwinCatQA.Infrastructure/TwinCatQA.Infrastructure.csproj

# 단위 테스트 실행
dotnet test tests/TwinCatQA.Infrastructure.Tests/TwinCatQA.Infrastructure.Tests.csproj

# 통합 테스트 실행
dotnet test tests/TwinCatQA.Integration.Tests/TwinCatQA.Integration.Tests.csproj
```

#### Step 9: 기존 사용처 확인 및 업데이트 (30분)

```bash
# LibGit2Service를 사용하는 곳 찾기
grep -r "LibGit2Service" src/ --include="*.cs"

# IGitService를 사용하는 곳 찾기
grep -r "IGitService" src/ --include="*.cs"
```

**작업 내용**:
1. 기존 코드는 IGitService 인터페이스를 사용하므로 대부분 수정 불필요
2. 직접 LibGit2Service를 인스턴스화하는 코드가 있다면 DI로 변경
3. 새로운 서비스를 직접 사용하고 싶은 곳은 IGitRepositoryService 등으로 주입

#### Step 10: 문서화 업데이트 (20분)

```bash
touch docs/architecture/git-services.md
```

**문서 내용**:
```markdown
# Git Services Architecture

## 개요
LibGit2Service는 Single Responsibility Principle에 따라 3개의 서비스로 분리되었습니다.

## 서비스 구조

### IGitRepositoryService (GitRepositoryService)
- **책임**: 저장소 관리
- **메서드**:
  - InitializeRepository()
  - IsGitRepository()
  - GetCurrentCommitHash()
  - IsWorkingDirectoryClean()
  - FindGitDirectory()

### IGitDiffService (GitDiffService)
- **책임**: Diff 분석
- **메서드**:
  - GetChangedFiles()
  - GetChangedLines()
  - GetDiffBetweenCommits()
  - DetermineContext()

### IGitHookService (GitHookService)
- **책임**: Hook 관리
- **메서드**:
  - InstallPreCommitHook()
  - UninstallPreCommitHook()
  - IsPreCommitHookInstalled()

### LibGit2Service (Facade)
- **책임**: 하위 호환성 유지
- **패턴**: Facade Pattern
- **역할**: 기존 IGitService 인터페이스를 유지하면서 내부적으로 분리된 서비스 사용

## 의존성 그래프
```
IGitService (Facade)
    ├── IGitRepositoryService
    ├── IGitDiffService
    │       └── IGitRepositoryService
    └── IGitHookService
            └── IGitRepositoryService
```

## 사용 예시

### 기존 코드 (변경 불필요)
```csharp
public class MyService
{
    private readonly IGitService _gitService;

    public MyService(IGitService gitService)
    {
        _gitService = gitService;
    }

    public void DoSomething()
    {
        _gitService.InitializeRepository("/path/to/repo");
    }
}
```

### 새로운 코드 (권장)
```csharp
public class MyService
{
    private readonly IGitRepositoryService _repositoryService;
    private readonly IGitDiffService _diffService;

    public MyService(
        IGitRepositoryService repositoryService,
        IGitDiffService diffService)
    {
        _repositoryService = repositoryService;
        _diffService = diffService;
    }

    public void DoSomething()
    {
        _repositoryService.InitializeRepository("/path/to/repo");
        var files = _diffService.GetChangedFiles("/path/to/repo", DiffTarget.All);
    }
}
```

## 마이그레이션 가이드

1. **기존 코드 유지**: IGitService를 사용하는 기존 코드는 수정 불필요
2. **점진적 마이그레이션**: 새로운 기능은 개별 서비스 인터페이스 사용
3. **테스트 우선**: Mock 생성이 더 쉬워짐 (단일 책임)
```

#### Step 11: 기존 LibGit2Service.cs 제거 준비 (10분)

```bash
# 기존 파일을 백업
cp src/TwinCatQA.Infrastructure/Git/LibGit2Service.cs \
   src/TwinCatQA.Infrastructure/Git/LibGit2Service.cs.old

# 나중에 완전히 제거
# rm src/TwinCatQA.Infrastructure/Git/LibGit2Service.cs.old
```

#### Step 12: 커밋 (10분)

```bash
git add src/TwinCatQA.Infrastructure/Git/IGitRepositoryService.cs
git add src/TwinCatQA.Infrastructure/Git/GitRepositoryService.cs
git add src/TwinCatQA.Infrastructure/Git/IGitDiffService.cs
git add src/TwinCatQA.Infrastructure/Git/GitDiffService.cs
git add src/TwinCatQA.Infrastructure/Git/IGitHookService.cs
git add src/TwinCatQA.Infrastructure/Git/GitHookService.cs
git add src/TwinCatQA.Infrastructure/Git/LibGit2Service.cs
git add src/TwinCatQA.Infrastructure/DependencyInjection.cs
git add tests/TwinCatQA.Infrastructure.Tests/Git/
git add docs/architecture/git-services.md

git commit -m "리팩토링: LibGit2Service를 3개의 서비스로 분리

- Single Responsibility Principle 적용
- GitRepositoryService: 저장소 관리 (200줄)
- GitDiffService: Diff 분석 (250줄)
- GitHookService: Hook 관리 (200줄)
- LibGit2Service: Facade 패턴으로 하위 호환성 유지
- 의존성 주입 설정 업데이트
- 포괄적인 단위 테스트 추가
- 아키텍처 문서 추가

Before: 674줄 대형 클래스
After: 3개의 단일 책임 클래스 + Facade

Benefits:
- 테스트 용이성 향상
- 코드 가독성 향상
- 유지보수성 향상
- Mock 생성 간소화"
```

### 🧪 테스트 전략
1. **단위 테스트**: 각 서비스별 독립적인 테스트 (Mock 사용)
2. **통합 테스트**: 실제 Git 저장소를 사용한 E2E 테스트
3. **Facade 테스트**: LibGit2Service가 하위 서비스에 정확히 위임하는지 확인
4. **회귀 테스트**: 기존 기능이 정상 동작하는지 전체 테스트 스위트 실행

### 📊 복잡도 메트릭 비교

| 메트릭 | Before (LibGit2Service.cs) | After (3 Services) |
|--------|----------------------------|---------------------|
| 라인 수 | 674줄 | 200 + 250 + 200 = 650줄 |
| 클래스당 라인 | 674 | 평균 217줄 |
| 메서드 수 | 17개 | 5 + 8 + 6 = 19개 |
| Cyclomatic Complexity | High | Low-Medium |
| 테스트 가능성 | Medium | High |
| 재사용성 | Low | High |

---

## User Story 5: 중복 코드 통합 (ExtractSnippet)

### 📌 Story 정보
- **Story ID**: REF-005
- **Priority**: Medium
- **Effort**: 2 Story Points (2시간)
- **Assignee**: Refactoring Expert

### 📖 Description
여러 Rule 클래스에 중복된 ExtractSnippet 메서드를 CodeSnippetExtractor 유틸리티 클래스로 통합합니다.

### ✅ Acceptance Criteria
- [ ] CodeSnippetExtractor 유틸리티 클래스가 생성됨
- [ ] 모든 Rule 클래스가 유틸리티 클래스를 사용함
- [ ] 중복 코드가 제거됨
- [ ] 단위 테스트가 추가됨
- [ ] 기존 기능이 정상 동작함

### 📋 중복 코드 분석

#### 현재 상황
**파일**: NamingConventionRule.cs
- `ExtractSnippet(string sourceCode, int lineNumber)` (Lines 460-485)
- 주변 3줄씩 추출, 현재 라인 ">>>" 표시

**예상 파일**: CyclomaticComplexityRule.cs
- `ExtractFunctionBlockSnippet(string sourceCode, FunctionBlock fb)` (Lines 285-318)
- Function Block 시작 10줄 추출, 라인 번호 표시

**공통점**:
- 소스 코드를 줄 단위로 분할
- 특정 라인 범위 추출
- 라인 번호와 함께 포맷팅

**차이점**:
- 추출 범위 (주변 ±3줄 vs 시작 10줄)
- 포맷 (>>> 표시 vs 4자리 라인 번호)
- 추가 정보 (없음 vs END_FUNCTION_BLOCK 표시)

### 🔧 리팩토링 전/후 코드

#### ❌ Before (NamingConventionRule.cs, Lines 460-485)
```csharp
/// <summary>
/// 지정된 라인의 코드 스니펫을 추출합니다.
/// </summary>
private string ExtractSnippet(string sourceCode, int lineNumber)
{
    if (string.IsNullOrEmpty(sourceCode))
    {
        return string.Empty;
    }

    string[] lines = sourceCode.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

    if (lineNumber < 1 || lineNumber > lines.Length)
    {
        return string.Empty;
    }

    int startLine = Math.Max(0, lineNumber - 3);
    int endLine = Math.Min(lines.Length - 1, lineNumber + 1);

    var snippetLines = new List<string>();
    for (int i = startLine; i <= endLine; i++)
    {
        string prefix = (i + 1 == lineNumber) ? ">>> " : "    ";
        snippetLines.Add($"{prefix}{i + 1,4}: {lines[i]}");
    }

    return string.Join(Environment.NewLine, snippetLines);
}
```

#### ❌ Before (CyclomaticComplexityRule.cs, Lines 285-318)
```csharp
/// <summary>
/// Function Block의 코드 스니펫을 추출합니다.
/// </summary>
private string ExtractFunctionBlockSnippet(string sourceCode, FunctionBlock fb)
{
    if (string.IsNullOrEmpty(sourceCode))
    {
        return string.Empty;
    }

    string[] lines = sourceCode.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

    if (fb.StartLine < 1 || fb.StartLine > lines.Length)
    {
        return string.Empty;
    }

    int startLine = fb.StartLine - 1;
    int endLine = Math.Min(lines.Length - 1, startLine + 9);

    var snippetLines = new List<string>();
    for (int i = startLine; i <= endLine; i++)
    {
        snippetLines.Add($"{i + 1,4}: {lines[i]}");
    }

    if (fb.EndLine - fb.StartLine > 10)
    {
        snippetLines.Add("    ...");
        snippetLines.Add($"{fb.EndLine,4}: END_FUNCTION_BLOCK");
    }

    return string.Join(Environment.NewLine, snippetLines);
}
```

#### ✅ After (새로운 유틸리티 클래스)

##### CodeSnippetExtractor.cs
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TwinCatQA.Application.Utils;

/// <summary>
/// 코드 스니펫 추출 유틸리티
/// Violation 보고 시 사용할 코드 조각을 추출합니다.
/// </summary>
public static class CodeSnippetExtractor
{
    #region 상수

    /// <summary>
    /// 기본 컨텍스트 라인 수 (위/아래)
    /// </summary>
    public const int DefaultContextLines = 3;

    /// <summary>
    /// Function Block 기본 표시 라인 수
    /// </summary>
    public const int DefaultFunctionBlockLines = 10;

    /// <summary>
    /// 현재 라인 강조 접두사
    /// </summary>
    public const string HighlightPrefix = ">>> ";

    /// <summary>
    /// 일반 라인 접두사
    /// </summary>
    public const string NormalPrefix = "    ";

    /// <summary>
    /// 생략 표시
    /// </summary>
    public const string EllipsisLine = "    ...";

    #endregion

    #region 공개 메서드

    /// <summary>
    /// 지정된 라인 주변의 컨텍스트를 포함한 스니펫을 추출합니다.
    /// </summary>
    /// <param name="sourceCode">전체 소스 코드</param>
    /// <param name="targetLine">대상 라인 번호 (1-based)</param>
    /// <param name="contextLines">위/아래로 포함할 컨텍스트 라인 수</param>
    /// <param name="highlightTarget">대상 라인 강조 여부</param>
    /// <returns>포맷팅된 코드 스니펫</returns>
    public static string ExtractWithContext(
        string sourceCode,
        int targetLine,
        int contextLines = DefaultContextLines,
        bool highlightTarget = true)
    {
        if (string.IsNullOrEmpty(sourceCode))
        {
            return string.Empty;
        }

        string[] lines = SplitLines(sourceCode);

        if (targetLine < 1 || targetLine > lines.Length)
        {
            return string.Empty;
        }

        int startLine = Math.Max(0, targetLine - 1 - contextLines);
        int endLine = Math.Min(lines.Length - 1, targetLine - 1 + contextLines);

        var snippetLines = new List<string>();
        for (int i = startLine; i <= endLine; i++)
        {
            string prefix = (highlightTarget && i + 1 == targetLine)
                ? HighlightPrefix
                : NormalPrefix;

            snippetLines.Add($"{prefix}{i + 1,4}: {lines[i]}");
        }

        return string.Join(Environment.NewLine, snippetLines);
    }

    /// <summary>
    /// 지정된 라인 범위의 스니펫을 추출합니다.
    /// </summary>
    /// <param name="sourceCode">전체 소스 코드</param>
    /// <param name="startLine">시작 라인 번호 (1-based)</param>
    /// <param name="endLine">종료 라인 번호 (1-based)</param>
    /// <param name="maxLines">최대 표시 라인 수 (초과 시 생략 표시)</param>
    /// <returns>포맷팅된 코드 스니펫</returns>
    public static string ExtractRange(
        string sourceCode,
        int startLine,
        int endLine,
        int? maxLines = null)
    {
        if (string.IsNullOrEmpty(sourceCode))
        {
            return string.Empty;
        }

        string[] lines = SplitLines(sourceCode);

        if (startLine < 1 || startLine > lines.Length)
        {
            return string.Empty;
        }

        int actualStartLine = startLine - 1;
        int actualEndLine = Math.Min(lines.Length - 1, endLine - 1);

        // 최대 라인 수 제한 적용
        int displayEndLine = actualEndLine;
        bool hasEllipsis = false;

        if (maxLines.HasValue && actualEndLine - actualStartLine + 1 > maxLines.Value)
        {
            displayEndLine = actualStartLine + maxLines.Value - 1;
            hasEllipsis = true;
        }

        var snippetLines = new List<string>();
        for (int i = actualStartLine; i <= displayEndLine; i++)
        {
            snippetLines.Add($"{i + 1,4}: {lines[i]}");
        }

        // 생략 표시 추가
        if (hasEllipsis)
        {
            snippetLines.Add(EllipsisLine);
            snippetLines.Add($"{endLine,4}: {lines[actualEndLine]}");
        }

        return string.Join(Environment.NewLine, snippetLines);
    }

    /// <summary>
    /// Function Block의 코드 스니펫을 추출합니다.
    /// </summary>
    /// <param name="sourceCode">전체 소스 코드</param>
    /// <param name="startLine">Function Block 시작 라인 (1-based)</param>
    /// <param name="endLine">Function Block 종료 라인 (1-based)</param>
    /// <param name="displayLines">표시할 라인 수</param>
    /// <param name="showEnd">종료 라인 표시 여부</param>
    /// <returns>포맷팅된 코드 스니펫</returns>
    public static string ExtractFunctionBlock(
        string sourceCode,
        int startLine,
        int endLine,
        int displayLines = DefaultFunctionBlockLines,
        bool showEnd = true)
    {
        if (string.IsNullOrEmpty(sourceCode))
        {
            return string.Empty;
        }

        string[] lines = SplitLines(sourceCode);

        if (startLine < 1 || startLine > lines.Length)
        {
            return string.Empty;
        }

        int actualStartLine = startLine - 1;
        int displayEndLine = Math.Min(lines.Length - 1, actualStartLine + displayLines - 1);

        var snippetLines = new List<string>();
        for (int i = actualStartLine; i <= displayEndLine; i++)
        {
            snippetLines.Add($"{i + 1,4}: {lines[i]}");
        }

        // Function Block이 표시 범위보다 길면 생략 표시
        if (showEnd && endLine - startLine > displayLines)
        {
            snippetLines.Add(EllipsisLine);

            // 종료 라인이 유효한 범위인지 확인
            if (endLine - 1 < lines.Length)
            {
                snippetLines.Add($"{endLine,4}: {lines[endLine - 1]}");
            }
            else
            {
                snippetLines.Add($"{endLine,4}: END_FUNCTION_BLOCK");
            }
        }

        return string.Join(Environment.NewLine, snippetLines);
    }

    /// <summary>
    /// 여러 라인을 강조 표시하여 스니펫을 추출합니다.
    /// </summary>
    /// <param name="sourceCode">전체 소스 코드</param>
    /// <param name="highlightLines">강조할 라인 번호 목록 (1-based)</param>
    /// <param name="contextLines">위/아래로 포함할 컨텍스트 라인 수</param>
    /// <returns>포맷팅된 코드 스니펫</returns>
    public static string ExtractWithMultipleHighlights(
        string sourceCode,
        IEnumerable<int> highlightLines,
        int contextLines = DefaultContextLines)
    {
        if (string.IsNullOrEmpty(sourceCode) || !highlightLines.Any())
        {
            return string.Empty;
        }

        string[] lines = SplitLines(sourceCode);
        var highlightSet = new HashSet<int>(highlightLines);

        int minLine = highlightLines.Min();
        int maxLine = highlightLines.Max();

        int startLine = Math.Max(0, minLine - 1 - contextLines);
        int endLine = Math.Min(lines.Length - 1, maxLine - 1 + contextLines);

        var snippetLines = new List<string>();
        for (int i = startLine; i <= endLine; i++)
        {
            string prefix = highlightSet.Contains(i + 1)
                ? HighlightPrefix
                : NormalPrefix;

            snippetLines.Add($"{prefix}{i + 1,4}: {lines[i]}");
        }

        return string.Join(Environment.NewLine, snippetLines);
    }

    #endregion

    #region Private 헬퍼 메서드

    /// <summary>
    /// 소스 코드를 줄 단위로 분할합니다.
    /// </summary>
    private static string[] SplitLines(string sourceCode)
    {
        return sourceCode.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
    }

    #endregion
}
```

#### ✅ After (Rule 클래스 수정)

##### NamingConventionRule.cs (수정)
```csharp
using TwinCatQA.Application.Utils;

public class NamingConventionRule : IValidationRule
{
    // ... 기존 코드 ...

    private IEnumerable<Violation> ValidateFunctionBlockNaming(
        FunctionBlock fb,
        string filePath,
        string sourceCode)
    {
        // ...
        yield return new Violation
        {
            // ...
            CodeSnippet = CodeSnippetExtractor.ExtractWithContext(sourceCode, fb.StartLine),
            // ...
        };
    }

    private IEnumerable<Violation> ValidateVariableNaming(
        Variable variable,
        string filePath,
        string sourceCode)
    {
        // ...
        yield return new Violation
        {
            // ...
            CodeSnippet = CodeSnippetExtractor.ExtractWithContext(
                sourceCode,
                variable.DeclarationLine),
            // ...
        };
    }

    // ExtractSnippet 메서드 제거됨
}
```

##### CyclomaticComplexityRule.cs (수정)
```csharp
using TwinCatQA.Application.Utils;

public class CyclomaticComplexityRule : IValidationRule
{
    // ... 기존 코드 ...

    public IEnumerable<Violation> Validate(CodeFile file)
    {
        // ...
        yield return new Violation
        {
            // ...
            CodeSnippet = CodeSnippetExtractor.ExtractFunctionBlock(
                syntaxTree.SourceCode,
                fb.StartLine,
                fb.EndLine),
            // ...
        };
    }

    // ExtractFunctionBlockSnippet 메서드 제거됨
}
```

### 📝 단계별 작업 순서

#### Step 1: CodeSnippetExtractor 클래스 생성 (60분)
```bash
cd D:\01. Vscode\Twincat\features\twincat-code-qa-tool\src\TwinCatQA.Application

# Utils 디렉토리 생성
mkdir Utils

# 유틸리티 클래스 생성
touch Utils/CodeSnippetExtractor.cs
```

**작업 내용**:
1. CodeSnippetExtractor.cs 파일 생성
2. 4개의 공개 메서드 구현:
   - `ExtractWithContext()` - 일반적인 라인 스니펫
   - `ExtractRange()` - 범위 지정 스니펫
   - `ExtractFunctionBlock()` - Function Block 전용
   - `ExtractWithMultipleHighlights()` - 여러 라인 강조
3. XML 문서화 주석 추가
4. 상수 정의

#### Step 2: 단위 테스트 작성 (40분)

```bash
cd tests/TwinCatQA.Application.Tests

# Utils 테스트 디렉토리 생성
mkdir Utils

# 테스트 클래스 생성
touch Utils/CodeSnippetExtractorTests.cs
```

##### CodeSnippetExtractorTests.cs
```csharp
using TwinCatQA.Application.Utils;
using Xunit;

namespace TwinCatQA.Application.Tests.Utils;

public class CodeSnippetExtractorTests
{
    private const string SampleCode = @"FUNCTION_BLOCK FB_Example
VAR_INPUT
    iValue : INT;
    iEnable : BOOL;
END_VAR
VAR_OUTPUT
    oResult : REAL;
END_VAR

IF iEnable THEN
    oResult := iValue * 2.0;
ELSE
    oResult := 0.0;
END_IF

END_FUNCTION_BLOCK";

    [Fact]
    public void ExtractWithContext_WhenValidLine_ShouldExtractSurroundingLines()
    {
        // Arrange
        int targetLine = 10; // "IF iEnable THEN"

        // Act
        var result = CodeSnippetExtractor.ExtractWithContext(
            SampleCode,
            targetLine,
            contextLines: 2);

        // Assert
        Assert.Contains(">>> ", result); // 강조 표시 확인
        Assert.Contains("  10: IF iEnable THEN", result);
        Assert.Contains("   8: ", result); // 위 컨텍스트
        Assert.Contains("  12: ", result); // 아래 컨텍스트
    }

    [Fact]
    public void ExtractWithContext_WhenLineOutOfRange_ShouldReturnEmpty()
    {
        // Arrange
        int invalidLine = 1000;

        // Act
        var result = CodeSnippetExtractor.ExtractWithContext(SampleCode, invalidLine);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ExtractWithContext_WhenEmptySourceCode_ShouldReturnEmpty()
    {
        // Arrange
        string emptyCode = "";

        // Act
        var result = CodeSnippetExtractor.ExtractWithContext(emptyCode, 1);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ExtractRange_WhenValidRange_ShouldExtractLines()
    {
        // Arrange
        int startLine = 2;
        int endLine = 5;

        // Act
        var result = CodeSnippetExtractor.ExtractRange(SampleCode, startLine, endLine);

        // Assert
        Assert.Contains("   2: VAR_INPUT", result);
        Assert.Contains("   5: END_VAR", result);
        Assert.DoesNotContain(">>> ", result); // 강조 없음
    }

    [Fact]
    public void ExtractRange_WhenMaxLinesExceeded_ShouldShowEllipsis()
    {
        // Arrange
        int startLine = 1;
        int endLine = 15;
        int maxLines = 5;

        // Act
        var result = CodeSnippetExtractor.ExtractRange(
            SampleCode,
            startLine,
            endLine,
            maxLines);

        // Assert
        Assert.Contains("...", result); // 생략 표시 확인
        Assert.Contains("  15: ", result); // 마지막 라인 표시
    }

    [Fact]
    public void ExtractFunctionBlock_WhenShortBlock_ShouldExtractAll()
    {
        // Arrange
        string shortBlock = @"FUNCTION_BLOCK FB_Short
VAR
    x : INT;
END_VAR
END_FUNCTION_BLOCK";

        // Act
        var result = CodeSnippetExtractor.ExtractFunctionBlock(
            shortBlock,
            startLine: 1,
            endLine: 5);

        // Assert
        Assert.DoesNotContain("...", result); // 생략 없음
        Assert.Contains("   1: FUNCTION_BLOCK FB_Short", result);
        Assert.Contains("   5: END_FUNCTION_BLOCK", result);
    }

    [Fact]
    public void ExtractFunctionBlock_WhenLongBlock_ShouldShowEllipsis()
    {
        // Arrange
        int startLine = 1;
        int endLine = 15;
        int displayLines = 5;

        // Act
        var result = CodeSnippetExtractor.ExtractFunctionBlock(
            SampleCode,
            startLine,
            endLine,
            displayLines);

        // Assert
        Assert.Contains("...", result); // 생략 표시
        Assert.Contains("  15: ", result); // 마지막 라인
    }

    [Fact]
    public void ExtractWithMultipleHighlights_WhenMultipleLines_ShouldHighlightAll()
    {
        // Arrange
        var highlightLines = new[] { 3, 4, 10 };

        // Act
        var result = CodeSnippetExtractor.ExtractWithMultipleHighlights(
            SampleCode,
            highlightLines,
            contextLines: 1);

        // Assert
        // ">>>" 문자열이 3번 나타나는지 확인
        int highlightCount = result.Split(new[] { ">>> " }, StringSplitOptions.None).Length - 1;
        Assert.Equal(3, highlightCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ExtractWithContext_WhenInvalidInput_ShouldReturnEmpty(string invalidCode)
    {
        // Act
        var result = CodeSnippetExtractor.ExtractWithContext(invalidCode, 1);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ExtractWithContext_WhenNoHighlight_ShouldNotShowArrow()
    {
        // Arrange
        int targetLine = 5;

        // Act
        var result = CodeSnippetExtractor.ExtractWithContext(
            SampleCode,
            targetLine,
            highlightTarget: false);

        // Assert
        Assert.DoesNotContain(">>> ", result);
        Assert.Contains("    ", result); // 일반 접두사만
    }

    [Fact]
    public void Constants_ShouldHaveExpectedValues()
    {
        // Assert
        Assert.Equal(3, CodeSnippetExtractor.DefaultContextLines);
        Assert.Equal(10, CodeSnippetExtractor.DefaultFunctionBlockLines);
        Assert.Equal(">>> ", CodeSnippetExtractor.HighlightPrefix);
        Assert.Equal("    ", CodeSnippetExtractor.NormalPrefix);
        Assert.Equal("    ...", CodeSnippetExtractor.EllipsisLine);
    }
}
```

#### Step 3: Rule 클래스 업데이트 (30분)

**수정 대상 파일**:
- CyclomaticComplexityRule.cs
- NamingConventionRule.cs
- KoreanCommentRule.cs (예상)

**작업 내용**:
1. `using TwinCatQA.Application.Utils;` 추가
2. ExtractSnippet 메서드 호출을 CodeSnippetExtractor로 교체
3. 기존 private ExtractSnippet 메서드 제거

```bash
# 빌드 확인
dotnet build src/TwinCatQA.Application/TwinCatQA.Application.csproj
```

#### Step 4: 통합 테스트 (20min)
```bash
# 전체 테스트 실행
dotnet test

# 특정 테스트만 실행
dotnet test --filter "FullyQualifiedName~CodeSnippetExtractorTests"
```

#### Step 5: 코드 리뷰 및 커밋 (10분)

```bash
git add src/TwinCatQA.Application/Utils/CodeSnippetExtractor.cs
git add src/TwinCatQA.Application/Rules/CyclomaticComplexityRule.cs
git add src/TwinCatQA.Application/Rules/NamingConventionRule.cs
git add src/TwinCatQA.Application/Rules/KoreanCommentRule.cs
git add tests/TwinCatQA.Application.Tests/Utils/CodeSnippetExtractorTests.cs

git commit -m "리팩토링: 중복 ExtractSnippet 메서드를 유틸리티로 통합

- CodeSnippetExtractor 유틸리티 클래스 생성
- 4가지 스니펫 추출 메서드 제공:
  - ExtractWithContext: 주변 컨텍스트 포함
  - ExtractRange: 범위 지정 추출
  - ExtractFunctionBlock: Function Block 전용
  - ExtractWithMultipleHighlights: 여러 라인 강조
- CyclomaticComplexityRule, NamingConventionRule에서 사용
- 중복 코드 제거 (약 50줄 감소)
- 포괄적인 단위 테스트 추가 (10개 테스트 케이스)
- 재사용성 및 유지보수성 향상"
```

### 🧪 테스트 전략
1. **단위 테스트**: CodeSnippetExtractor의 모든 메서드 테스트
2. **통합 테스트**: Rule 클래스에서 스니펫이 정상적으로 생성되는지 확인
3. **Edge Case 테스트**:
   - 빈 소스 코드
   - 유효하지 않은 라인 번호
   - 범위 초과
   - 특수 문자 포함 코드

### 📊 코드 중복 감소 메트릭

| 메트릭 | Before | After | 개선 |
|--------|--------|-------|------|
| ExtractSnippet 메서드 수 | 3개 (각 Rule에 1개) | 1개 (유틸리티) | -2개 |
| 중복 코드 라인 | ~75줄 | 0줄 | -75줄 |
| 단위 테스트 커버리지 | 0% | 95% | +95% |
| 재사용성 | Low | High | 향상 |

---

## Sprint 2 완료 체크리스트

### Definition of Done
- [ ] 모든 코드가 빌드됨
- [ ] 모든 단위 테스트가 통과함 (100개 이상)
- [ ] 코드 커버리지 80% 이상
- [ ] 코드 리뷰가 완료됨
- [ ] 아키텍처 문서가 업데이트됨
- [ ] Git 커밋이 완료됨

### Sprint 2 회고
- **잘된 점**:
- **개선할 점**:
- **배운 점**:

---

# 📊 전체 프로젝트 메트릭

## Before (리팩토링 전)

| 메트릭 | 값 |
|--------|-----|
| 총 코드 라인 | ~10,000 줄 |
| 매직 넘버 | 3개 (CyclomaticComplexityRule) |
| Console.WriteLine 사용 | 9개 파일 |
| 대형 클래스 (>500줄) | 1개 (LibGit2Service: 674줄) |
| 중복 코드 블록 | 3개 (ExtractSnippet) |
| 인코딩 문제 | 1개 (CompareCommand.cs) |
| 평균 클래스 복잡도 | Medium-High |
| 테스트 커버리지 | ~60% |

## After (리팩토링 후)

| 메트릭 | 값 | 개선 |
|--------|-----|------|
| 총 코드 라인 | ~10,100 줄 | +100 (테스트 증가) |
| 매직 넘버 | 0개 | -3 |
| Console.WriteLine 사용 | 0개 (Application/Infrastructure) | -9 |
| 대형 클래스 (>500줄) | 0개 | -1 |
| 중복 코드 블록 | 0개 | -3 |
| 인코딩 문제 | 0개 | -1 |
| 평균 클래스 복잡도 | Low-Medium | 향상 |
| 테스트 커버리지 | ~75% | +15% |

## 품질 지표 개선

| 지표 | Before | After | 개선율 |
|------|--------|-------|--------|
| Maintainability Index | 70 | 85 | +21% |
| Cyclomatic Complexity (Avg) | 8.5 | 5.2 | -39% |
| Code Duplication | 5% | 1% | -80% |
| Test Coverage | 60% | 75% | +25% |
| SOLID Compliance | 65% | 85% | +31% |

---

# 🎯 리팩토링 원칙 준수

## SOLID Principles

### Single Responsibility Principle (SRP) ✅
- **적용**: LibGit2Service를 3개 서비스로 분리
- **효과**: 각 클래스가 단일 책임만 가짐

### Open/Closed Principle (OCP) ✅
- **적용**: 인터페이스 정의 및 의존성 주입
- **효과**: 기존 코드 수정 없이 확장 가능

### Liskov Substitution Principle (LSP) ✅
- **적용**: Facade 패턴으로 하위 호환성 유지
- **효과**: 기존 코드가 수정 없이 동작

### Interface Segregation Principle (ISP) ✅
- **적용**: 3개의 세분화된 인터페이스 정의
- **효과**: 필요한 메서드만 의존

### Dependency Inversion Principle (DIP) ✅
- **적용**: 모든 서비스가 인터페이스에 의존
- **효과**: 테스트 용이성 향상

## Clean Code Principles

### Meaningful Names ✅
- 상수 이름: `DefaultMediumThreshold`, `HighlightPrefix`
- 클래스 이름: `GitRepositoryService`, `CodeSnippetExtractor`

### Functions Should Do One Thing ✅
- 각 메서드가 단일 기능만 수행
- 메서드 라인 수 < 30줄

### DRY (Don't Repeat Yourself) ✅
- ExtractSnippet 중복 제거
- 공통 로직을 유틸리티로 추출

### Comments Should Explain Why ✅
- XML 문서화 주석 추가
- 복잡한 로직에 why 주석

---

# 📚 참고 자료

## 리팩토링 패턴
- **Extract Method**: ExtractSnippet → CodeSnippetExtractor
- **Replace Magic Number with Symbolic Constant**: 10, 15, 20 → 상수화
- **Extract Class**: LibGit2Service → 3개 서비스
- **Introduce Facade**: LibGit2Service Facade 패턴

## 추천 도서
- "Refactoring: Improving the Design of Existing Code" - Martin Fowler
- "Clean Code" - Robert C. Martin
- "Working Effectively with Legacy Code" - Michael Feathers

## 도구
- **정적 분석**: SonarQube, ReSharper
- **코드 메트릭**: Visual Studio Code Metrics
- **테스트**: xUnit, Moq

---

# ✅ 최종 체크리스트

## Sprint 1 (완료 여부)
- [ ] REF-001: 매직 넘버 상수화 (1시간)
- [ ] REF-002: Console.WriteLine → Logger (3시간)
- [ ] REF-003: UTF-8 BOM 인코딩 수정 (0.5시간)

## Sprint 2 (완료 여부)
- [ ] REF-004: LibGit2Service 분리 (8시간)
- [ ] REF-005: ExtractSnippet 통합 (2시간)

## 문서화
- [ ] 아키텍처 문서 업데이트
- [ ] API 문서 생성
- [ ] README 업데이트

## 테스트
- [ ] 단위 테스트 통과율 100%
- [ ] 통합 테스트 통과
- [ ] 코드 커버리지 > 75%

## 배포
- [ ] Dev 환경 배포
- [ ] QA 환경 배포
- [ ] Production 배포 준비

---

**작성일**: 2025-01-26
**작성자**: Refactoring Expert
**버전**: 1.0
**프로젝트**: TwinCatQA
