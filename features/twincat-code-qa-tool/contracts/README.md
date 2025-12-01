# API 계약 (Contracts)

**목적**: 도메인 서비스 및 인프라스트럭처 인터페이스 정의
**작성일**: 2025-11-20

---

## 개요

이 디렉토리는 TwinCAT 코드 품질 검증 도구의 핵심 인터페이스를 정의합니다.
모든 인터페이스는 C#으로 작성되며, 구현 세부사항과 독립적으로 계약을 명시합니다.

---

## 인터페이스 목록

### 1. IValidationRule
**파일**: `IValidationRule.cs`
**목적**: 검증 규칙 인터페이스
**책임**:
- 코드 파일에 대한 검증 수행
- 위반 사항 반환
- 규칙별 설정 파라미터 적용

**구현 예시**:
- `KoreanCommentRule` (원칙 1: 한글 주석)
- `CyclomaticComplexityRule` (원칙 4: 복잡도)
- `NamingConventionRule` (원칙 5: 명명 규칙)

---

### 2. IValidationEngine
**파일**: `IValidationEngine.cs`
**목적**: 검증 엔진 오케스트레이션
**책임**:
- 검증 세션 수명 주기 관리
- 파일 스캔, 파싱, 규칙 실행 조율
- 리포트 생성 및 저장

**주요 메서드**:
```csharp
ValidationSession StartSession(string projectPath, ValidationMode mode);
void ScanFiles(ValidationSession session);
void RunValidation(ValidationSession session);
void GenerateReports(ValidationSession session);
```

---

### 3. IParserService
**파일**: `IParserService.cs`
**목적**: TwinCAT 파일 파싱 및 AST 생성
**책임**:
- ANTLR4 기반 ST 파싱
- LINQ to XML 기반 TwinCAT XML 파싱
- AST에서 엔티티 추출 (FB, 변수, DUT 등)
- 사이클로매틱 복잡도 계산

**주요 메서드**:
```csharp
SyntaxTree ParseFile(string filePath);
List<FunctionBlock> ExtractFunctionBlocks(SyntaxTree ast);
int CalculateCyclomaticComplexity(FunctionBlock fb);
```

---

### 4. IReportGenerator
**파일**: `IReportGenerator.cs`
**목적**: HTML 및 PDF 리포트 생성
**책임**:
- Razor 템플릿 기반 HTML 생성
- iText 기반 PDF 변환
- 차트 데이터 생성 (품질 추세, 헌장 준수율 등)
- 코드 하이라이팅

**주요 메서드**:
```csharp
string GenerateHtmlReport(ValidationSession session);
string GeneratePdfReport(ValidationSession session);
ChartData CreateQualityTrendChart(List<ValidationSession> sessions);
```

---

### 5. IGitService
**파일**: `IGitService.cs`
**목적**: Git 작업 및 Diff 분석
**책임**:
- LibGit2Sharp 기반 Git 작업
- 변경된 파일 및 라인 추출
- Pre-commit hook 관리
- 증분 검증을 위한 컨텍스트 범위 결정

**주요 메서드**:
```csharp
List<string> GetChangedFiles(string repoPath);
List<LineChange> GetChangedLines(string repoPath, string filePath);
void InstallPreCommitHook(string repoPath);
CodeContext DetermineContext(CodeFile file, int changedLine);
```

---

## 의존성 관계

```
IValidationEngine
    ├─→ IParserService
    ├─→ IValidationRule[]
    ├─→ IReportGenerator
    └─→ IGitService (선택적)

IValidationRule
    └─→ IParserService (AST 접근)

IReportGenerator
    └─→ (외부 라이브러리: RazorLight, iText)

IGitService
    └─→ (외부 라이브러리: LibGit2Sharp)
```

---

## 구현 계획

### Phase 1: 핵심 인터페이스 구현
- [ ] `IParserService` → `AntlrParserService` (ANTLR4 기반)
- [ ] `IValidationRule` → 기본 규칙 3개 (FR-1, FR-2, FR-7)
- [ ] `IValidationEngine` → `DefaultValidationEngine`

### Phase 2: 고급 기능
- [ ] `IReportGenerator` → `RazorReportGenerator`
- [ ] `IGitService` → `LibGit2Service`
- [ ] 도메인 특화 규칙 (FR-3, FR-4, FR-5, FR-6)

### Phase 3: 확장성
- [ ] 플러그인 시스템 (커스텀 규칙 로드)
- [ ] 템플릿 커스터마이징
- [ ] 성능 최적화

---

## 네이밍 규칙

- 인터페이스: `I` 접두사 (예: `IValidationRule`)
- 구현 클래스: 구체적 이름 (예: `KoreanCommentRule`, `DefaultValidationEngine`)
- 서비스: `Service` 접미사 (예: `IParserService`)

---

## 다음 단계

1. **quickstart.md** 작성: 개발 환경 설정 가이드
2. **plan.md** 작성: 최종 구현 계획서 통합
3. 인터페이스 구현 시작 (Phase 1)

---

**계약 문서화 완료**: 2025-11-20
**총 인터페이스**: 5개
**다음 문서**: quickstart.md
