# TwinCAT 코드 품질 검증 및 리뷰 도구 - 소스 코드

## 📂 프로젝트 구조

```
src/
├── TwinCatQA.Domain/                  # 도메인 레이어 (비즈니스 로직)
│   ├── Models/                        # 엔티티 및 도메인 모델
│   │   ├── Enums.cs                   # 11개 열거형 (ViolationSeverity, ConstitutionPrinciple 등)
│   │   ├── Variable.cs                # 변수 엔티티
│   │   ├── FunctionBlock.cs           # Function Block 엔티티
│   │   ├── Violation.cs               # 위반 사항 엔티티
│   │   ├── CodeFile.cs                # 코드 파일 엔티티
│   │   └── ValidationSession.cs       # 검증 세션 엔티티 (집계 루트)
│   └── Contracts/                     # 인터페이스 계약
│       ├── IValidationRule.cs         # 검증 규칙 인터페이스
│       ├── IValidationEngine.cs       # 검증 엔진 인터페이스
│       ├── IParserService.cs          # 파서 서비스 인터페이스
│       ├── IReportGenerator.cs        # 리포트 생성 인터페이스
│       └── IGitService.cs             # Git 서비스 인터페이스
│
├── TwinCatQA.Application/             # 애플리케이션 레이어 (유즈케이스)
│   ├── Services/                      # 애플리케이션 서비스
│   │   ├── DefaultValidationEngine.cs # 검증 엔진 구현
│   │   ├── RazorReportGenerator.cs    # HTML/PDF 리포트 생성
│   │   ├── ChartDataBuilder.cs        # Chart.js 차트 데이터 생성
│   │   └── CodeHighlighter.cs         # ST 코드 하이라이팅
│   ├── Rules/                         # 검증 규칙 구현
│   │   ├── KoreanCommentRule.cs       # FR-1: 한글 주석 검증
│   │   ├── CyclomaticComplexityRule.cs # FR-4: 복잡도 검증
│   │   └── NamingConventionRule.cs    # FR-5: 명명 규칙 검증
│   ├── Configuration/                 # 설정 관리
│   │   ├── QualitySettings.cs         # 설정 모델 클래스
│   │   ├── ConfigurationService.cs    # YAML 설정 로드/저장
│   │   └── ConfigurationServiceExtensions.cs # 확장 메서드
│   ├── Templates/                     # 템플릿 파일
│   │   ├── report-template.cshtml     # HTML 리포트 Razor 템플릿
│   │   ├── report-styles.css          # CSS 스타일
│   │   └── default-settings.yml       # 기본 설정 YAML
│   └── Models/                        # ViewModel, DTO
│       └── ChartData.cs               # Chart.js 데이터 구조
│
└── TwinCatQA.Infrastructure/          # 인프라 레이어 (외부 통합)
    ├── Parsers/                       # 파서 구현
    │   ├── Grammars/                  # ANTLR4 문법 파일
    │   │   └── StructuredText.g4      # IEC 61131-3 ST 문법
    │   ├── AntlrParserService.cs      # ANTLR4 파서 서비스
    │   ├── CyclomaticComplexityVisitor.cs # 복잡도 계산 Visitor
    │   └── README.md                  # 파서 통합 가이드
    └── Git/                           # Git 통합
        ├── LibGit2Service.cs          # LibGit2Sharp 서비스
        ├── DiffParser.cs              # Diff Patch 파싱
        ├── ContextAnalyzer.cs         # 코드 컨텍스트 분석
        └── Templates/                 # Pre-commit Hook 템플릿
            ├── pre-commit.sh          # Bash 스크립트 (Linux/Mac)
            └── pre-commit.bat         # Batch 스크립트 (Windows)
```

---

## 🏗️ 아키텍처 개요

### 레이어 아키텍처 (Layer Architecture)

```
┌─────────────────────────────────────────────┐
│  Presentation Layer (미구현, 향후 VSIX)      │
│  - Visual Studio Tool Window                │
│  - WPF UI (XAML + MVVM)                     │
└─────────────────────────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────────┐
│  Application Layer (유즈케이스)              │
│  - ValidationEngine (검증 오케스트레이션)     │
│  - ReportGenerator (리포트 생성)             │
│  - ConfigurationService (설정 관리)          │
│  - Validation Rules (검증 규칙 구현)          │
└─────────────────────────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────────┐
│  Domain Layer (도메인 모델 및 계약)           │
│  - Entities (CodeFile, Violation, Session)  │
│  - Interfaces (IValidationRule, IParser)    │
│  - Enums (Severity, Principle, FileType)   │
└─────────────────────────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────────┐
│  Infrastructure Layer (외부 통합)            │
│  - ANTLR4 Parser (ST 파싱)                  │
│  - LibGit2Sharp (Git 통합)                  │
│  - YamlDotNet (설정 관리)                    │
│  - RazorLight (HTML 템플릿)                 │
└─────────────────────────────────────────────┘
```

---

## 🔑 핵심 컴포넌트

### 1. DefaultValidationEngine (Application/Services/)
**책임**: 검증 프로세스 오케스트레이션

**워크플로우**:
```
StartSession → ScanFiles → ParseFiles → RunValidation
→ CalculateQualityScores → GenerateReports → CompleteSession
```

**주요 메서드**:
- `StartSession()`: 검증 세션 생성
- `ScanFiles()`: .TcPOU/.TcDUT/.TcGVL 파일 재귀 탐색
- `ParseFiles()`: ANTLR4 파서로 AST 생성
- `RunValidation()`: 활성화된 규칙 실행
- `GenerateReports()`: HTML/PDF 리포트 생성
- `CompleteSession()`: JSON 저장

---

### 2. Validation Rules (Application/Rules/)
**패턴**: Strategy 패턴 (IValidationRule 인터페이스)

**구현된 규칙**:
1. **KoreanCommentRule** (FR-1)
   - 주석 한글 비율 검증 (기본 95%)
   - 심각도: High

2. **CyclomaticComplexityRule** (FR-4)
   - McCabe 복잡도 계산
   - 임계값: Medium(10), High(15), Critical(20)

3. **NamingConventionRule** (FR-5)
   - FB/FC/PRG 접두사 검증
   - 변수 접두사 검증 (i/in, o/out, g)
   - 카멜케이스/파스칼케이스 검증

---

### 3. AntlrParserService (Infrastructure/Parsers/)
**책임**: TwinCAT 파일 파싱 및 AST 생성

**파싱 흐름**:
```
.TcPOU (XML)
  └─ LINQ to XML 파싱
      └─ <Declaration> → 변수 선언 추출
      └─ <Implementation> → ST 코드 추출
          └─ ANTLR4 Lexer/Parser
              └─ AST 생성
                  └─ FunctionBlock, Variable 추출
```

**주요 메서드**:
- `ParseFile()`: 파일 → AST 변환
- `ExtractFunctionBlocks()`: FB/Function 추출
- `ExtractVariables()`: 변수 추출
- `CalculateCyclomaticComplexity()`: 복잡도 계산

---

### 4. LibGit2Service (Infrastructure/Git/)
**책임**: Git 저장소 통합 및 Diff 분석

**주요 기능**:
- 변경 파일 목록 조회 (Index/WorkingDirectory/All)
- 변경 라인 추출 (Patch 파싱)
- Pre-commit Hook 설치/제거
- 컨텍스트 범위 결정 (FunctionBlock/CASE/FOR/IF/주변 라인)

**증분 검증 지원**:
```csharp
var changedFiles = gitService.GetChangedFiles(repoPath, DiffTarget.All);
var lineChanges = gitService.GetChangedLines(repoPath, filePath);
var context = gitService.DetermineContext(codeFile, changedLine);
```

---

### 5. RazorReportGenerator (Application/Services/)
**책임**: HTML/PDF 리포트 생성

**포함 내용**:
- 프로젝트 정보 및 품질 점수
- Chart.js 차트 3개 (품질 추이, 헌장 준수율, 위반 분포)
- 위반 사항 상세 목록
- ST 코드 하이라이팅

---

## 📦 의존성 (NuGet Packages)

### TwinCatQA.Infrastructure
- `Antlr4.Runtime.Standard` (4.11.1) - ANTLR4 파서
- `LibGit2Sharp` (0.27.0) - Git 통합
- `System.Linq.Async` (6.0.0) - 비동기 LINQ

### TwinCatQA.Application
- `YamlDotNet` (13.7.1) - YAML 설정 관리
- `RazorLight` (2.3.0) - Razor 템플릿 엔진
- `itext7` (8.0.0) - PDF 생성 (AGPL 라이선스 주의)

### 테스트 프로젝트
- `xunit` (2.4.2) - 테스트 프레임워크
- `Moq` (4.18.0) - Mocking 프레임워크
- `FluentAssertions` (6.11.0) - 가독성 높은 검증

---

## 🚀 빌드 및 실행

### 필수 요구사항
- .NET 6.0 SDK 이상
- Java 11+ (ANTLR4 컴파일용)
- Visual Studio 2019/2022 (선택 사항)

### 빌드 단계

```bash
# 1. ANTLR4 문법 컴파일
cd src/TwinCatQA.Infrastructure/Parsers/Grammars
java -jar antlr-4.11.1-complete.jar -Dlanguage=CSharp StructuredText.g4

# 2. NuGet 패키지 복원
cd ../../../..
dotnet restore

# 3. 솔루션 빌드
dotnet build --configuration Release

# 4. 테스트 실행
dotnet test
```

---

## 📝 설정 파일 예시

**`.twincat-qa/settings.yml`**:
```yaml
global:
  defaultMode: Full
  enableParallelProcessing: true
  maxDegreeOfParallelism: 4

rules:
  configurations:
    FR-1-KOREAN-COMMENT:
      enabled: true
      severity: High
      parameters:
        requiredKoreanRatio: 0.95

    FR-4-COMPLEXITY:
      enabled: true
      severity: Medium
      parameters:
        mediumThreshold: 10
        highThreshold: 15
        criticalThreshold: 20

reports:
  generateHtml: true
  generatePdf: false
  outputPath: .twincat-qa/reports

git:
  enablePreCommitHook: false
  blockOnCriticalViolations: true
```

---

## 📚 추가 문서

- [기술 조사 보고서](../research.md)
- [데이터 모델 설계](../data-model.md)
- [구현 계획서](../plan.md)
- [작업 목록](../tasks.md)
- [빠른 시작 가이드](../quickstart.md)
- [설정 가이드](../docs/configuration-guide.md)
- [Git 통합 가이드](../docs/Git-Integration.md)

---

**작성일**: 2025-11-20
**버전**: 1.0.0
**상태**: MVP 구현 완료 (Phase 1-9)
