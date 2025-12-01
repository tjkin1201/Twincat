# TwinCatQA 아키텍처 분석 보고서

**분석 일자**: 2025-11-28
**분석 대상**: D:\01. Vscode\Twincat\features\twincat-code-qa-tool
**분석자**: System Architect Agent

---

## 목차

1. [개요](#개요)
2. [아키텍처 개요](#아키텍처-개요)
3. [레이어별 분석](#레이어별-분석)
4. [Clean Architecture 원칙 준수 분석](#clean-architecture-원칙-준수-분석)
5. [의존성 방향 분석](#의존성-방향-분석)
6. [도메인 모델 설계 품질](#도메인-모델-설계-품질)
7. [인터페이스 분리 원칙](#인터페이스-분리-원칙)
8. [모듈 간 결합도 분석](#모듈-간-결합도-분석)
9. [확장성 및 유지보수성](#확장성-및-유지보수성)
10. [기술 부채 식별](#기술-부채-식별)
11. [개선 기회 및 권장사항](#개선-기회-및-권장사항)
12. [종합 평가](#종합-평가)

---

## 개요

TwinCatQA는 TwinCAT PLC 코드의 품질 분석 및 검증을 수행하는 도구입니다. Clean Architecture 패턴을 따라 6개의 주요 레이어로 구성되어 있으며, .NET 8.0/9.0 기반으로 개발되었습니다.

### 프로젝트 구조

```
TwinCatQA/
├── src/
│   ├── TwinCatQA.Domain          (도메인 레이어)
│   ├── TwinCatQA.Application     (애플리케이션 레이어)
│   ├── TwinCatQA.Infrastructure  (인프라스트럭처 레이어)
│   ├── TwinCatQA.CLI             (CLI 프레젠테이션 레이어)
│   ├── TwinCatQA.UI              (WPF UI 레이어)
│   ├── TwinCatQA.Grammar         (문법 파서 레이어)
│   └── TwinCatQA.VsExtension     (Visual Studio 확장)
└── tests/
    ├── TwinCatQA.Domain.Tests
    ├── TwinCatQA.Application.Tests
    ├── TwinCatQA.Infrastructure.Tests
    └── TwinCatQA.Integration.Tests
```

---

## 아키텍처 개요

### 레이어 구성

| 레이어 | 역할 | 주요 책임 |
|--------|------|----------|
| **Domain** | 비즈니스 로직 핵심 | 엔티티, 값 객체, 도메인 서비스 인터페이스 |
| **Application** | 유즈케이스 구현 | 비즈니스 워크플로우, 오케스트레이션 |
| **Infrastructure** | 기술 구현 | 파서, 컴파일러, Git, 파일 I/O |
| **CLI** | 명령줄 인터페이스 | 사용자 명령 처리, DI 설정 |
| **UI** | WPF 사용자 인터페이스 | 데스크톱 GUI |
| **Grammar** | ANTLR 문법 | Structured Text 파서 |

### 기술 스택

- **.NET**: 8.0 (Core), 9.0 (CLI)
- **파싱**: ANTLR4 (Antlr4.Runtime.Standard 4.11.1)
- **Git**: LibGit2Sharp 0.27.0
- **DI**: Microsoft.Extensions.DependencyInjection
- **로깅**: Microsoft.Extensions.Logging
- **UI**: WPF (.NET 8.0-windows)
- **템플릿**: RazorLight 2.3.0
- **설정**: YamlDotNet 13.7.1

---

## 레이어별 분석

### 1. Domain Layer (TwinCatQA.Domain)

**위치**: `src\TwinCatQA.Domain`

#### 구조

```
TwinCatQA.Domain/
├── Contracts/          # 인터페이스 정의
│   ├── IGitService.cs
│   ├── IParserService.cs
│   ├── IReportGenerator.cs
│   ├── IValidationEngine.cs
│   └── IValidationRule.cs
├── Models/            # 도메인 엔티티 및 값 객체
│   ├── AST/          # 추상 구문 트리
│   ├── QA/           # QA 분석 모델
│   ├── CodeFile.cs
│   ├── ValidationSession.cs
│   ├── Violation.cs
│   └── ...
└── Services/         # 도메인 서비스 인터페이스
    ├── IAdvancedAnalysisOrchestrator.cs
    ├── ICompilationService.cs
    ├── IDependencyAnalyzer.cs
    └── ...
```

#### 강점

✅ **완전한 의존성 역전**: 외부 레이어에 대한 의존성이 전혀 없음
✅ **명확한 계약 정의**: `Contracts/` 디렉토리에 모든 인터페이스 집중
✅ **풍부한 도메인 모델**: `CodeFile`, `ValidationSession`, `Violation` 등 핵심 엔티티 잘 정의됨
✅ **AST 모델링**: Visitor 패턴을 사용한 확장 가능한 AST 노드 구조

#### 이슈

⚠️ **동기/비동기 인터페이스 분리**: `IValidationEngine`과 `IAsyncValidationEngine`이 별도 인터페이스로 분리됨
- 파일: `src\TwinCatQA.Domain\Contracts\IValidationEngine.cs` (라인 16, 103)
- 이유: 동기 메서드와 비동기 메서드가 동일한 책임을 가짐에도 인터페이스가 분리됨
- 권장: 하나의 인터페이스로 통합하고 동기 버전을 비동기 래퍼로 제공

⚠️ **빈약한 엔티티 메서드**: 대부분 엔티티가 속성만 정의하고 행위가 부족함
- 예: `CodeFile.cs`에 품질 점수 계산 로직이 없음
- 권장: 도메인 로직을 엔티티 내부로 이동 (예: `CalculateQualityScore()`)

---

### 2. Application Layer (TwinCatQA.Application)

**위치**: `src\TwinCatQA.Application`

#### 구조

```
TwinCatQA.Application/
├── Configuration/     # 설정 관리
│   ├── ConfigurationService.cs
│   └── QualitySettings.cs
├── Models/           # 애플리케이션 전용 모델
│   └── ChartData.cs
├── Rules/            # 검증 규칙 구현 (❌ 위치 부적절)
│   ├── CyclomaticComplexityRule.cs
│   ├── KoreanCommentRule.cs
│   └── NamingConventionRule.cs
├── Services/         # 유즈케이스 구현
│   ├── AdvancedAnalysisOrchestrator.cs
│   ├── QARuleEngine.cs
│   ├── QaAnalysisService.cs
│   └── ...
└── Templates/        # 리포트 템플릿
```

#### 강점

✅ **명확한 오케스트레이션**: `AdvancedAnalysisOrchestrator`가 4가지 분석 통합
✅ **병렬 처리 최적화**: 파일 시스템 기반 vs 메모리 기반 분석 분리 (라인 96-183)
✅ **에러 핸들링**: `ContinueOnError` 옵션으로 부분 실패 허용

#### 이슈

🔴 **레이어 위반 (Critical)**: Application이 Infrastructure를 직접 참조
- 파일: `src\TwinCatQA.Application\Services\QaAnalysisService.cs` (라인 4)
- 코드: `using TwinCatQA.Infrastructure.Comparison;`
- 이유: Application이 Infrastructure의 구체 클래스 `FolderComparer`를 직접 인스턴스화 (라인 39)
- 영향: 의존성 방향 위반, 테스트 불가, 확장성 저하
- 권장: `IFolderComparer` 인터페이스를 Domain에 정의하고 DI로 주입

🔴 **규칙 위치 부적절**: `Rules/` 폴더가 Application에 존재
- 파일: `src\TwinCatQA.Application\Rules\*.cs`
- 이유: 규칙 구현은 Domain 관심사 또는 Infrastructure 구현
- 권장: Domain의 `IValidationRule` 구현체를 Infrastructure로 이동

⚠️ **의존성 주입 불일치**: `QaAnalysisService`가 `new FolderComparer()` 사용
- 파일: `src\TwinCatQA.Application\Services\QaAnalysisService.cs` (라인 39)
- 권장: 생성자 주입으로 변경

---

### 3. Infrastructure Layer (TwinCatQA.Infrastructure)

**위치**: `src\TwinCatQA.Infrastructure`

#### 구조

```
TwinCatQA.Infrastructure/
├── Analysis/         # 분석 구현
│   ├── DependencyAnalyzer.cs
│   ├── VariableUsageAnalyzer.cs
│   └── ...
├── Comparison/       # 비교 기능
│   ├── FolderComparer.cs
│   ├── VariableComparer.cs
│   └── ...
├── Compilation/      # TwinCAT 컴파일
│   └── TwinCatCompilationService.cs
├── Git/             # Git 통합
│   ├── LibGit2Service.cs
│   └── ...
├── Parsers/         # ANTLR 파서
│   ├── AntlrParserService.cs
│   └── Grammars/
├── QA/              # QA 규칙
│   └── Rules/       # 20개 규칙 구현
└── Reports/         # 리포트 생성
    ├── JsonReportGenerator.cs
    ├── MarkdownReportGenerator.cs
    └── SimpleHtmlReportGenerator.cs
```

#### 강점

✅ **완전한 구현체 제공**: Domain 인터페이스를 충실히 구현
✅ **ANTLR 통합**: Structured Text 문법 파서 완벽 구현
✅ **다양한 리포트 형식**: JSON, Markdown, HTML 지원

#### 이슈

⚠️ **FolderComparer의 기본 생성자**: 의존성 주입 우회
- 파일: `src\TwinCatQA.Infrastructure\Comparison\FolderComparer.cs` (라인 35-41)
- 코드: `new VariableComparer()`, `new IOMappingComparer()` 등
- 이유: DI 컨테이너를 사용하지 않는 경우를 위한 편의성
- 권장: Factory 패턴으로 분리하여 관심사 분리

⚠️ **DependencyInjection 불완전**: 일부 서비스만 등록됨
- 파일: `src\TwinCatQA.Infrastructure\DependencyInjection.cs`
- 현재: `IReportGenerator`만 등록
- 권장: 모든 Infrastructure 서비스를 등록하도록 확장

---

### 4. Presentation Layers (CLI, UI)

#### CLI Layer

**위치**: `src\TwinCatQA.CLI`

✅ **명확한 DI 설정**: `ServiceCollectionExtensions`에서 모든 서비스 등록
✅ **.NET 9.0 사용**: 최신 기능 활용
✅ **System.CommandLine 사용**: 표준 CLI 프레임워크

#### UI Layer

**위치**: `src\TwinCatQA.UI`

✅ **MVVM 패턴**: CommunityToolkit.Mvvm 사용
✅ **명확한 뷰모델 분리**: `ViewModels/` 디렉토리
⚠️ **WPF-Windows 종속성**: 크로스 플랫폼 확장 제한

---

## Clean Architecture 원칙 준수 분석

### 평가 기준

| 원칙 | 준수 여부 | 점수 |
|------|----------|------|
| **의존성 방향 (DIP)** | 부분 위반 | ⚠️ 70/100 |
| **레이어 분리 (SRP)** | 양호 | ✅ 85/100 |
| **인터페이스 추상화 (ISP)** | 우수 | ✅ 90/100 |
| **비즈니스 로직 격리** | 양호 | ✅ 80/100 |
| **테스트 가능성** | 보통 | ⚠️ 65/100 |

### 주요 위반 사항

#### 1. 의존성 방향 위반 (Critical)

**위치**: `TwinCatQA.Application → TwinCatQA.Infrastructure`

```csharp
// ❌ 잘못된 예시
// src\TwinCatQA.Application\Services\QaAnalysisService.cs
using TwinCatQA.Infrastructure.Comparison; // Application이 Infrastructure 참조

public async Task<QaAnalysisResult> AnalyzeAsync(...)
{
    var comparer = new FolderComparer(); // 구체 클래스 직접 생성
    // ...
}
```

**수정 방안**:

```csharp
// ✅ 올바른 예시
// 1. Domain에 인터페이스 정의
// src\TwinCatQA.Domain\Services\IFolderComparer.cs
namespace TwinCatQA.Domain.Services;
public interface IFolderComparer
{
    Task<FolderComparisonResult> CompareAsync(
        string oldPath, string newPath, CompareOptions? options = null);
}

// 2. Application에서 인터페이스 의존
// src\TwinCatQA.Application\Services\QaAnalysisService.cs
public class QaAnalysisService
{
    private readonly IFolderComparer _folderComparer;

    public QaAnalysisService(IFolderComparer folderComparer)
    {
        _folderComparer = folderComparer;
    }

    public async Task<QaAnalysisResult> AnalyzeAsync(...)
    {
        result.ComparisonResult = await _folderComparer.CompareAsync(...);
    }
}

// 3. Infrastructure에서 구현
// src\TwinCatQA.Infrastructure\Comparison\FolderComparer.cs
public class FolderComparer : IFolderComparer
{
    // 구현...
}
```

#### 2. 규칙 구현 위치 부적절

**현재**: `TwinCatQA.Application\Rules\*.cs`
**문제**: 검증 규칙 구현이 Application 레이어에 존재

**수정 방안**:

```
이동 전:
src/TwinCatQA.Application/Rules/
  ├── CyclomaticComplexityRule.cs
  ├── KoreanCommentRule.cs
  └── NamingConventionRule.cs

이동 후:
src/TwinCatQA.Infrastructure/Validation/Rules/
  ├── CyclomaticComplexityRule.cs
  ├── KoreanCommentRule.cs
  └── NamingConventionRule.cs
```

---

## 의존성 방향 분석

### 현재 의존성 그래프

```
┌─────────────────────────┐
│ TwinCatQA.CLI           │
│ TwinCatQA.UI            │
└───────┬─────────────────┘
        │ (모두 참조)
        ▼
┌─────────────────────────┐      ┌──────────────────────┐
│ TwinCatQA.Application   │◄─────│  (이슈!)             │
└───────┬─────────────────┘      └──────────────────────┘
        │                                    │
        │                                    ▼
        │                        ┌─────────────────────────┐
        │                        │ TwinCatQA.Infrastructure│
        │                        └───────┬─────────────────┘
        │                                │
        ▼                                │
┌─────────────────────────┐            │
│ TwinCatQA.Domain        │◄───────────┘
│ (인터페이스만 제공)      │
└─────────────────────────┘
```

### 의존성 위반 목록

| 소스 | 타겟 | 위반 유형 | 심각도 |
|------|------|----------|--------|
| Application | Infrastructure.Comparison | 직접 참조 | 🔴 Critical |
| Application | Infrastructure.QA.Rules | 네임스페이스 혼동 | ⚠️ Warning |

### 권장 의존성 그래프

```
┌─────────────────────────┐
│ Presentation Layers     │
│ (CLI, UI, VsExtension)  │
└───────┬─────────────────┘
        │
        ▼
┌─────────────────────────┐
│ TwinCatQA.Application   │
└───────┬─────────────────┘
        │
        ▼
┌─────────────────────────┐      ┌─────────────────────────┐
│ TwinCatQA.Domain        │◄─────│ TwinCatQA.Infrastructure│
│ (인터페이스 + 엔티티)    │      │ (구현체)                 │
└─────────────────────────┘      └─────────────────────────┘
```

---

## 도메인 모델 설계 품질

### 핵심 엔티티 분석

#### 1. ValidationSession (집계 루트)

**파일**: `src\TwinCatQA.Domain\Models\ValidationSession.cs`

**강점**:
- ✅ 명확한 집계 루트 역할
- ✅ 풍부한 메타데이터 (세션 ID, 시간, 프로젝트 정보)
- ✅ 계산 로직 포함 (`CalculateQualityScore()`, `CalculateConstitutionCompliance()`)
- ✅ 불변 속성과 가변 속성 명확히 구분 (`init` vs `set`)

**개선점**:
- ⚠️ 품질 점수 계산 로직이 단순함 (라인 175-198)
- 권장: 더 정교한 가중치 시스템, 파일 타입별 차등 적용

#### 2. CodeFile (엔티티)

**파일**: `src\TwinCatQA.Domain\Models\CodeFile.cs`

**강점**:
- ✅ 명확한 식별자 (Guid)
- ✅ 풍부한 메타데이터 (타입, 언어, 해시)
- ✅ 관계 정의 (FunctionBlocks, GlobalVariables, Violations)

**개선점**:
- ⚠️ 행위가 없음 (순수 데이터 컨테이너)
- 권장: 비즈니스 로직 추가 (예: `AddViolation()`, `CalculateComplexity()`)

```csharp
// 현재: 빈약한 도메인 모델
public class CodeFile
{
    public List<Violation> Violations { get; init; } = new();
    public double QualityScore { get; set; } // 외부에서 직접 설정
}

// 권장: 풍부한 도메인 모델
public class CodeFile
{
    private readonly List<Violation> _violations = new();
    public IReadOnlyList<Violation> Violations => _violations.AsReadOnly();

    public void AddViolation(Violation violation)
    {
        if (violation == null) throw new ArgumentNullException(nameof(violation));
        _violations.Add(violation);
        RecalculateQualityScore();
    }

    public double QualityScore { get; private set; }

    private void RecalculateQualityScore()
    {
        // 도메인 로직: 위반 사항 기반 점수 계산
        var penalty = Violations.Sum(v => GetSeverityPenalty(v.Severity));
        QualityScore = Math.Max(0, 100 - penalty);
    }
}
```

#### 3. AST 노드 (값 객체)

**파일**: `src\TwinCatQA.Domain\Models\AST\ASTNode.cs`

**강점**:
- ✅ Visitor 패턴 구현 (확장성)
- ✅ 제네릭 Visitor 지원
- ✅ 명확한 위치 정보 (라인, 컬럼)

**개선점**:
- ⚠️ `Parent` 속성이 `set` 가능 (불변성 위반)
- 권장: `init` 또는 생성자 주입으로 변경

---

## 인터페이스 분리 원칙

### 인터페이스 품질 평가

#### 우수 사례

**1. IValidationRule**

**파일**: `src\TwinCatQA.Domain\Contracts\IValidationRule.cs`

✅ **단일 책임**: 규칙 검증만 담당
✅ **명확한 계약**: 메타데이터 + 검증 메서드 + 설정
✅ **ISP 준수**: 규칙별 필수 기능만 정의

```csharp
public interface IValidationRule
{
    // 메타데이터
    string RuleId { get; }
    string RuleName { get; }
    ConstitutionPrinciple RelatedPrinciple { get; }

    // 핵심 기능
    IEnumerable<Violation> Validate(CodeFile file);

    // 설정
    void Configure(Dictionary<string, object> parameters);
}
```

**2. IAdvancedAnalysisOrchestrator**

**파일**: `src\TwinCatQA.Domain\Services\IAdvancedAnalysisOrchestrator.cs`

✅ **조합 가능**: 개별 분석 메서드 제공
✅ **옵션 패턴**: `AdvancedAnalysisOptions`로 유연성 확보

#### 개선 필요 사례

**1. IValidationEngine + IAsyncValidationEngine**

**파일**: `src\TwinCatQA.Domain\Contracts\IValidationEngine.cs`

⚠️ **불필요한 분리**: 동기/비동기가 별도 인터페이스
- 라인 16: `IValidationEngine`
- 라인 103: `IAsyncValidationEngine`

**권장 수정**:

```csharp
// 통합된 인터페이스
public interface IValidationEngine
{
    // 비동기 메서드만 제공 (동기는 .Result 사용)
    Task<ValidationSession> StartSessionAsync(...);
    Task ScanFilesAsync(...);
    Task RunValidationAsync(...);

    // 동기 확장 메서드 (별도 클래스)
}

public static class ValidationEngineExtensions
{
    public static ValidationSession StartSession(
        this IValidationEngine engine, string path, ValidationMode mode)
        => engine.StartSessionAsync(path, mode).Result;
}
```

---

## 모듈 간 결합도 분석

### 결합도 매트릭스

| 소스 / 타겟 | Domain | Application | Infrastructure | CLI | UI |
|------------|--------|-------------|----------------|-----|-----|
| **Domain** | - | 0 | 0 | 0 | 0 |
| **Application** | 🟢 낮음 | - | 🔴 높음 | 0 | 0 |
| **Infrastructure** | 🟢 낮음 | 0 | - | 0 | 0 |
| **CLI** | 🟢 낮음 | 🟢 낮음 | 🟢 낮음 | - | 0 |
| **UI** | 🟢 낮음 | 🟢 낮음 | 🟢 낮음 | 0 | - |

### 높은 결합도 이슈

#### 🔴 Application → Infrastructure.Comparison

**영향도**: Critical
**결합 유형**: 직접 클래스 참조
**발생 위치**:
- `src\TwinCatQA.Application\Services\QaAnalysisService.cs:4`
- `src\TwinCatQA.Application\Services\QaAnalysisService.cs:39`

**해결 방안**:
1. `IFolderComparer` 인터페이스를 Domain으로 이동
2. DI 컨테이너를 통한 주입
3. Factory 패턴 적용 (선택적)

---

## 확장성 및 유지보수성

### 확장성 평가

#### 강점

✅ **플러그인 아키텍처**: 규칙을 독립적으로 추가 가능
- 파일: `src\TwinCatQA.Infrastructure\QA\Rules\`
- 20개 규칙이 `IQARuleChecker` 구현

✅ **전략 패턴**: 여러 리포트 생성기
- JSON, Markdown, HTML 생성기 병렬 지원
- 파일: `src\TwinCatQA.Infrastructure\Reports\`

✅ **Visitor 패턴**: AST 순회 확장 가능
- 파일: `src\TwinCatQA.Domain\Models\AST\IASTVisitor.cs`
- 새로운 분석 추가 시 노드 수정 불필요

✅ **오케스트레이터 패턴**: 복잡한 워크플로우 관리
- 파일: `src\TwinCatQA.Application\Services\AdvancedAnalysisOrchestrator.cs`
- 병렬 실행, 오류 복구 등 통합 관리

#### 확장 시나리오

**시나리오 1: 새로운 QA 규칙 추가**

난이도: 🟢 쉬움

```csharp
// 1. Infrastructure에 규칙 클래스 추가
public class MyCustomRule : IQARuleChecker
{
    public string RuleId => "QA021";
    public string RuleName => "커스텀 규칙";
    // ...
}

// 2. DI 컨테이너에 등록
services.AddSingleton<IQARuleChecker, MyCustomRule>();
```

**시나리오 2: 새로운 분석 기능 추가 (예: 성능 분석)**

난이도: 🟡 보통

```csharp
// 1. Domain에 인터페이스 정의
public interface IPerformanceAnalyzer
{
    Task<PerformanceAnalysis> AnalyzeAsync(ValidationSession session);
}

// 2. Infrastructure에 구현
public class PerformanceAnalyzer : IPerformanceAnalyzer { }

// 3. Orchestrator에 통합
public class AdvancedAnalysisOrchestrator
{
    private readonly IPerformanceAnalyzer _perfAnalyzer;

    public async Task<ComprehensiveAnalysisResult> AnalyzeProjectAsync(...)
    {
        result.Performance = await _perfAnalyzer.AnalyzeAsync(session);
    }
}
```

**시나리오 3: 새로운 프레젠테이션 레이어 추가 (예: Web API)**

난이도: 🟢 쉬움

```
src/TwinCatQA.WebApi/
  ├── Controllers/
  │   └── ValidationController.cs
  ├── Startup.cs
  └── Program.cs
```

- Application 레이어 재사용
- DI 설정만 복사

### 유지보수성 평가

#### 강점

✅ **명확한 책임 분리**: 각 레이어가 명확한 역할
✅ **테스트 가능성**: 인터페이스 기반 설계
✅ **로깅 통합**: `ILogger<T>` 일관성 있게 사용
✅ **설정 관리**: YAML 기반 외부화

#### 개선점

⚠️ **문서화 부족**:
- 아키텍처 결정 기록(ADR) 없음
- 각 레이어의 경계 규칙 문서화 필요

⚠️ **테스트 커버리지 불명확**:
- 테스트 프로젝트는 존재하나 실제 커버리지 미확인

⚠️ **에러 처리 일관성**:
- 일부 서비스는 `ContinueOnError`, 일부는 throw
- 통일된 에러 전략 필요

---

## 기술 부채 식별

### Critical 기술 부채

#### 🔴 TD-001: Application → Infrastructure 의존성

**위치**: `src\TwinCatQA.Application\Services\QaAnalysisService.cs`
**영향**: 테스트 불가, 확장성 저하, Clean Architecture 위반
**예상 수정 시간**: 4시간
**우선순위**: P0 (즉시)

**수정 계획**:
1. `IFolderComparer` 인터페이스 Domain으로 이동
2. `QaAnalysisService` 생성자에 `IFolderComparer` 주입
3. DI 설정 업데이트
4. 단위 테스트 작성

#### 🔴 TD-002: 규칙 구현 위치 부적절

**위치**: `src\TwinCatQA.Application\Rules\`
**영향**: 레이어 책임 혼동, 재사용성 저하
**예상 수정 시간**: 2시간
**우선순위**: P1 (1주일 내)

**수정 계획**:
1. `Application\Rules\` → `Infrastructure\Validation\Rules\` 이동
2. 네임스페이스 변경
3. DI 등록 위치 확인

### Warning 기술 부채

#### ⚠️ TD-003: 동기/비동기 인터페이스 중복

**위치**: `src\TwinCatQA.Domain\Contracts\IValidationEngine.cs`
**영향**: API 복잡도 증가, 유지보수 부담
**예상 수정 시간**: 3시간
**우선순위**: P2 (1개월 내)

#### ⚠️ TD-004: FolderComparer의 기본 생성자

**위치**: `src\TwinCatQA.Infrastructure\Comparison\FolderComparer.cs:35`
**영향**: DI 우회 경로 존재, 테스트 어려움
**예상 수정 시간**: 2시간
**우선순위**: P2 (1개월 내)

#### ⚠️ TD-005: 빈약한 도메인 모델

**위치**: `src\TwinCatQA.Domain\Models\CodeFile.cs`
**영향**: 비즈니스 로직 분산, 캡슐화 부족
**예상 수정 시간**: 8시간
**우선순위**: P3 (3개월 내)

### Info 기술 부채

#### ℹ️ TD-006: DI 등록 불완전

**위치**: `src\TwinCatQA.Infrastructure\DependencyInjection.cs`
**영향**: 수동 등록 필요, 설정 누락 가능성
**예상 수정 시간**: 1시간
**우선순위**: P3 (3개월 내)

---

## 개선 기회 및 권장사항

### 즉시 개선 (High Priority)

#### 1. 의존성 방향 수정

**목표**: Application → Infrastructure 의존성 제거

**Action Items**:
- [ ] `IFolderComparer` 인터페이스를 Domain으로 이동
  - 경로: `src\TwinCatQA.Domain\Services\IFolderComparer.cs`
- [ ] `QaAnalysisService` 생성자 주입으로 변경
  - 파일: `src\TwinCatQA.Application\Services\QaAnalysisService.cs`
- [ ] DI 컨테이너에 바인딩 추가
  - 파일: `src\TwinCatQA.CLI\Services\ServiceCollectionExtensions.cs`

**예상 효과**:
- ✅ Clean Architecture 준수
- ✅ 테스트 용이성 향상
- ✅ Application 레이어의 재사용성 증가

#### 2. 규칙 구현 위치 정리

**목표**: 레이어 책임 명확화

**Action Items**:
- [ ] `Application\Rules\` → `Infrastructure\Validation\Rules\` 이동
- [ ] 네임스페이스 업데이트
- [ ] import 문 수정

**예상 효과**:
- ✅ 레이어 책임 명확화
- ✅ 규칙 재사용성 향상

### 단기 개선 (Medium Priority)

#### 3. 도메인 모델 풍부화

**목표**: 빈약한 도메인 모델을 풍부한 모델로 전환

**Action Items**:
- [ ] `CodeFile`에 `AddViolation()` 메서드 추가
- [ ] `ValidationSession`에 상태 전이 메서드 추가
- [ ] 계산 로직을 엔티티 내부로 이동

```csharp
public class CodeFile
{
    private readonly List<Violation> _violations = new();

    public void AddViolation(Violation violation)
    {
        Guard.Against.Null(violation);
        _violations.Add(violation);
        RecalculateQualityScore();
    }

    private void RecalculateQualityScore()
    {
        // 도메인 로직
    }
}
```

#### 4. 인터페이스 통합

**목표**: `IValidationEngine`과 `IAsyncValidationEngine` 통합

**Action Items**:
- [ ] 비동기 인터페이스만 유지
- [ ] 동기 확장 메서드 추가
- [ ] 기존 사용처 업데이트

### 장기 개선 (Low Priority)

#### 5. 아키텍처 문서화

**목표**: 유지보수성 향상

**Action Items**:
- [ ] ADR (Architecture Decision Records) 작성
- [ ] 레이어 간 계약 문서화
- [ ] 확장 가이드 작성

#### 6. 테스트 전략 수립

**목표**: 품질 보증

**Action Items**:
- [ ] 단위 테스트 커버리지 80% 목표
- [ ] 통합 테스트 시나리오 정의
- [ ] TDD 프로세스 도입

#### 7. 성능 최적화

**목표**: 대규모 프로젝트 지원

**Action Items**:
- [ ] 병렬 처리 최적화
- [ ] 메모리 사용량 프로파일링
- [ ] 캐싱 전략 수립

---

## 종합 평가

### 아키텍처 점수

| 항목 | 점수 | 평가 |
|------|------|------|
| **Clean Architecture 준수** | 75/100 | 🟡 양호 (일부 개선 필요) |
| **의존성 방향** | 70/100 | 🟡 양호 (Application→Infrastructure 위반) |
| **도메인 모델 설계** | 80/100 | 🟢 우수 (빈약한 모델 개선 필요) |
| **인터페이스 분리** | 90/100 | 🟢 우수 |
| **모듈 결합도** | 75/100 | 🟡 양호 (일부 높은 결합도) |
| **확장성** | 85/100 | 🟢 우수 |
| **유지보수성** | 80/100 | 🟢 우수 |
| **테스트 가능성** | 70/100 | 🟡 양호 (DI 개선 필요) |
| **전체 평균** | **78/100** | 🟢 **우수** |

### 강점 요약

1. ✅ **명확한 레이어 분리**: 6개 레이어가 논리적으로 잘 구성됨
2. ✅ **풍부한 인터페이스**: Domain 레이어에 추상화가 잘 정의됨
3. ✅ **확장 가능한 구조**: 플러그인, 전략, Visitor 패턴 활용
4. ✅ **비동기 지원**: Task 기반 비동기 패턴 일관성 있게 사용
5. ✅ **DI 통합**: Microsoft.Extensions.DependencyInjection 활용

### 주요 개선 필요 사항

1. 🔴 **Application → Infrastructure 의존성 제거** (Critical)
2. 🔴 **규칙 구현 위치 정리** (Critical)
3. ⚠️ **도메인 모델 풍부화** (Warning)
4. ⚠️ **인터페이스 통합** (Warning)
5. ℹ️ **문서화 강화** (Info)

### 권장 조치 순서

**Phase 1 (즉시, 1주일):**
1. `IFolderComparer` 인터페이스 Domain으로 이동
2. `QaAnalysisService` DI 적용
3. 규칙 구현 위치 정리

**Phase 2 (단기, 1개월):**
4. 도메인 모델에 행위 추가
5. 동기/비동기 인터페이스 통합
6. DI 등록 완전화

**Phase 3 (장기, 3개월):**
7. ADR 작성
8. 테스트 커버리지 향상
9. 성능 최적화

---

## 결론

TwinCatQA 프로젝트는 **전반적으로 우수한 아키텍처 설계**를 가지고 있습니다. Clean Architecture 원칙을 대부분 준수하며, 확장 가능하고 유지보수하기 쉬운 구조를 갖추고 있습니다.

다만, **Application 레이어가 Infrastructure를 직접 참조하는 Critical 이슈**와 **규칙 구현 위치 부적절** 문제를 즉시 해결해야 합니다. 이 두 가지 이슈를 해결하면 아키텍처 점수는 **85/100 이상**으로 향상될 것으로 예상됩니다.

장기적으로는 도메인 모델을 더 풍부하게 만들고, 문서화를 강화하며, 테스트 전략을 수립하는 것이 권장됩니다.

---

**분석 완료 일시**: 2025-11-28
**다음 검토 권장 일시**: 2026-02-28 (3개월 후)
