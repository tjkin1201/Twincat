# 구현 계획서: TwinCAT 코드 품질 검증 및 리뷰 도구

**작성일**: 2025-11-20
**버전**: 1.0.0
**상태**: 승인 대기 (Pending Approval)

---

## 목차

1. [개요](#1-개요)
2. [기술 스택 요약](#2-기술-스택-요약)
3. [아키텍처 설계](#3-아키텍처-설계)
4. [구현 로드맵](#4-구현-로드맵)
5. [헌장 준수 검증](#5-헌장-준수-검증)
6. [리스크 관리](#6-리스크-관리)
7. [성공 기준](#7-성공-기준)
8. [리소스 및 일정](#8-리소스-및-일정)

---

## 1. 개요

### 1.1 프로젝트 목적

TwinCAT 기반 온도 제어 및 자동 배치 프로세스 시스템의 PLC 코드에 대한 품질 검증(QA)과 코드 리뷰 프로세스를 자동화하고 효율화하는 Visual Studio 확장 도구를 개발합니다.

**핵심 가치**:
- 수동 코드 리뷰 시간을 **70% 단축**
- 프로덕션 버그 발생률 **40% 감소**
- 프로젝트 헌장 원칙 준수율 **95% 이상** 달성

### 1.2 관련 문서

| 문서 | 목적 | 경로 |
|------|------|------|
| **spec.md** | 기능 명세서 | `./spec.md` |
| **research.md** | 기술 조사 보고서 | `./research.md` |
| **data-model.md** | 데이터 모델 설계 | `./data-model.md` |
| **contracts/** | API 계약 (인터페이스) | `./contracts/` |
| **quickstart.md** | 빠른 시작 가이드 | `./quickstart.md` |
| **constitution.md** | 프로젝트 헌장 | `/memory/constitution.md` |

---

## 2. 기술 스택 요약

### 2.1 선정된 기술

| 영역 | 기술 | 버전 | 라이선스 | 선정 근거 |
|------|------|------|---------|----------|
| **파서** | ANTLR4 | 4.11+ | BSD-3 | IEC 61131-3 문법 지원, C# 타겟 코드 생성 |
| **VS 확장** | Visual Studio SDK | 2022 | MS EULA | TwinCAT XAE 통합, 네이티브 Tool Window 제공 |
| **Git 통합** | LibGit2Sharp | 0.27+ | MIT | 순수 .NET, Diff 분석 및 Hook 관리 지원 |
| **설정 관리** | YamlDotNet | 13.0+ | MIT | YAML 파싱, 가독성 우수 |
| **HTML 리포트** | RazorLight | 2.3+ | MIT | Razor 템플릿 엔진, 유지보수 용이 |
| **PDF 리포트** | iText 7 | 8.0+ | AGPL | HTML to PDF 변환, 단 AGPL 라이선스 주의 |
| **JSON 직렬화** | System.Text.Json | 내장 | MIT | .NET 표준, 고성능 |

### 2.2 개발 환경

- **운영체제**: Windows 10/11 (64-bit)
- **개발 도구**: Visual Studio 2019/2022
- **프레임워크**: .NET Framework 4.8 또는 .NET 6.0
- **TwinCAT**: 3.1 이상

---

## 3. 아키텍처 설계

### 3.1 레이어 아키텍처

```
┌──────────────────────────────────────────────────────┐
│         Presentation Layer (VSIX Extension)          │
│  - ToolWindowPane (QualityWindow)                    │
│  - WPF UI (XAML + MVVM)                              │
│  - Commands (Menu Integration)                       │
└──────────────────────────────────────────────────────┘
                        │
                        ▼
┌──────────────────────────────────────────────────────┐
│           Application Layer (Services)               │
│  - ValidationEngine (IValidationEngine)              │
│  - ReportGenerator (IReportGenerator)                │
│  - ConfigurationManager                              │
└──────────────────────────────────────────────────────┘
                        │
                        ▼
┌──────────────────────────────────────────────────────┐
│              Domain Layer (Core Logic)               │
│  - Validation Rules (IValidationRule[])              │
│    ├─ KoreanCommentRule                              │
│    ├─ CyclomaticComplexityRule                       │
│    ├─ NamingConventionRule                           │
│    ├─ StateMachineValidationRule                     │
│    └─ ... (12 rules total)                           │
│  - Domain Models (CodeFile, Violation, etc.)         │
└──────────────────────────────────────────────────────┘
                        │
                        ▼
┌──────────────────────────────────────────────────────┐
│        Infrastructure Layer (External Access)        │
│  - Parsers (ANTLR4 기반 ST 파서)                      │
│  - Git Integration (LibGit2Sharp)                    │
│  - File System (JSON Storage)                        │
│  - Report Templates (Razor)                          │
└──────────────────────────────────────────────────────┘
```

### 3.2 핵심 컴포넌트

#### 3.2.1 ValidationEngine

**책임**:
- 검증 세션 수명 주기 관리
- 파일 스캔 → 파싱 → 규칙 실행 → 리포트 생성 오케스트레이션

**주요 메서드**:
```csharp
ValidationSession StartSession(string projectPath, ValidationMode mode);
void ScanFiles(ValidationSession session);
void ParseFiles(ValidationSession session, Action<double> progress);
void RunValidation(ValidationSession session, Action<double> progress);
void CalculateQualityScores(ValidationSession session);
void GenerateReports(ValidationSession session);
void CompleteSession(ValidationSession session);
```

#### 3.2.2 ParserService

**책임**:
- TwinCAT XML 파일 파싱 (LINQ to XML)
- ST 코드 추출 및 ANTLR4 파싱
- AST 생성 및 엔티티 추출

**파싱 흐름**:
```
.TcPOU 파일 (XML)
  └─ XML 파싱 (LINQ to XML)
      └─ <Declaration> 태그 → VAR 선언 추출
      └─ <Implementation> 태그 → ST 코드 추출
          └─ ANTLR4 파싱
              └─ AST 생성
                  └─ FunctionBlock, Variable, DataType 추출
```

#### 3.2.3 Validation Rules

**규칙 구현 패턴**:
```csharp
public class XxxRule : IValidationRule
{
    public string RuleId => "FR-X-NAME";
    public string RuleName => "규칙 이름 (한글)";
    public ConstitutionPrinciple RelatedPrinciple => ConstitutionPrinciple.Xxx;
    public ViolationSeverity DefaultSeverity => ViolationSeverity.High;

    public IEnumerable<Violation> Validate(CodeFile file)
    {
        // AST 순회 및 위반 검출
        // Visitor 패턴 사용
        var visitor = new XxxVisitor(this);
        visitor.Visit(file.Ast.RootNode);
        return visitor.Violations;
    }
}
```

#### 3.2.4 ReportGenerator

**템플릿 구조**:
```
/templates/
  ├─ report.cshtml               # 메인 리포트
  ├─ summary.cshtml              # 요약 섹션
  ├─ violations.cshtml           # 위반 목록
  ├─ quality-score.cshtml        # 품질 점수
  ├─ constitution-compliance.cshtml  # 헌장 준수율
  └─ charts.cshtml               # 차트 (Chart.js)
```

**Razor 모델**:
```csharp
public class ReportModel
{
    public ValidationSession Session { get; set; }
    public List<Violation> Violations { get; set; }
    public Dictionary<ViolationSeverity, int> ViolationCounts { get; set; }
    public Dictionary<ConstitutionPrinciple, double> Compliance { get; set; }
}
```

---

## 4. 구현 로드맵

### 4.1 Phase 1: MVP (Minimum Viable Product) - 4주

**목표**: 핵심 기능 구현으로 검증 가능한 프로토타입 완성

#### Week 1-2: 파서 및 핵심 규칙
- [ ] ANTLR4 ST 문법 파일 작성 및 컴파일
- [ ] `AntlrParserService` 구현 (IParserService)
- [ ] TwinCAT XML 파싱 (LINQ to XML)
- [ ] 핵심 규칙 3개 구현:
  - [ ] `KoreanCommentRule` (FR-2, 원칙 1)
  - [ ] `CyclomaticComplexityRule` (FR-1, 원칙 4)
  - [ ] `NamingConventionRule` (FR-7, 원칙 5)
- [ ] 단위 테스트 작성 (80% 커버리지)

**산출물**:
- `TwinCatQA.Infrastructure/Parsers/`
- `TwinCatQA.Application/Rules/`
- `TwinCatQA.Tests/ParserTests.cs`

#### Week 3: Visual Studio 확장
- [ ] VSIX 프로젝트 생성
- [ ] Tool Window 구현 (WPF UI)
- [ ] `ValidationEngine` 구현
- [ ] VS 메뉴 통합 ("도구" > "TwinCAT QA")
- [ ] 진행률 표시 (ProgressBar)

**산출물**:
- `TwinCatQA/ToolWindows/QualityWindow.cs`
- `TwinCatQA/TwinCatQAPackage.cs`

#### Week 4: HTML 리포트 생성
- [ ] Razor 템플릿 작성
- [ ] `RazorReportGenerator` 구현
- [ ] Chart.js 통합 (품질 추세, 위반 분포)
- [ ] ST 코드 하이라이팅 (Highlight.js)
- [ ] 리포트 자동 열기 기능

**산출물**:
- `TwinCatQA.Application/ReportGenerator.cs`
- `/templates/*.cshtml`

**Phase 1 완료 기준**:
- ✅ 500줄 ST 코드를 10초 이내에 검증
- ✅ 3개 규칙이 정상 작동
- ✅ HTML 리포트 생성
- ✅ Visual Studio에서 Tool Window 표시

---

### 4.2 Phase 2: Git 통합 - 2주

#### Week 5: Git 서비스
- [ ] `LibGit2Service` 구현 (IGitService)
- [ ] Git diff 분석
- [ ] 변경된 파일/라인 추출
- [ ] Pre-commit hook 설치/제거 기능

**산출물**:
- `TwinCatQA.Infrastructure/Git/LibGit2Service.cs`

#### Week 6: 증분 검증 (FR-12)
- [ ] 변경 라인 기반 컨텍스트 범위 결정
- [ ] 증분 검증 모드 구현
- [ ] 변경 전후 비교 리포트
- [ ] Git hook 스크립트 (PowerShell)

**산출물**:
- `/hooks/pre-commit.ps1`
- Diff-based validation logic

**Phase 2 완료 기준**:
- ✅ Git diff 분석 < 3초 (100 파일 기준)
- ✅ 증분 검증 시간 80% 단축
- ✅ Pre-commit hook 정상 작동

---

### 4.3 Phase 3: 도메인 특화 규칙 - 3주

#### Week 7-8: 온도 제어 및 통신 규칙
- [ ] `TemperatureStabilityRule` (FR-3)
  - 출력 변화율 임계값 검증
  - 연속 급격한 변화 감지
  - 안정화 로직 존재 확인
- [ ] `RecipeManagementRule` (FR-4)
  - 레시피 DUT 일관성 검증
  - 유효성 검사 존재 확인
  - 버전 필드 검증
- [ ] `CommunicationProtocolRule` (FR-5)
  - 타임아웃 처리 검증
  - 재연결 로직 확인
  - 에러 처리 검증

**산출물**:
- `TwinCatQA.Application/Rules/TemperatureStabilityRule.cs`
- `TwinCatQA.Application/Rules/RecipeManagementRule.cs`
- `TwinCatQA.Application/Rules/CommunicationProtocolRule.cs`

#### Week 9: 상태 머신 및 문서화 규칙
- [ ] `StateMachineValidationRule` (FR-6)
  - ENUM 전체 처리 확인
  - 기본 케이스 존재 확인
  - 데드락/무한 루프 감지
- [ ] `DocumentationQualityRule` (FR-8)
  - Function Block 주석 존재 확인
  - 복잡도 > 10 시 주석 필수
  - 주석-코드 일치성 검증

**산출물**:
- `TwinCatQA.Application/Rules/StateMachineValidationRule.cs`
- `TwinCatQA.Application/Rules/DocumentationQualityRule.cs`

**Phase 3 완료 기준**:
- ✅ 12개 규칙 모두 구현 완료
- ✅ 도메인 특화 검증 정확도 > 95%

---

### 4.4 Phase 4: 고급 기능 - 2주

#### Week 10: PDF 리포트 및 설정
- [ ] iText 7 통합 (PDF 생성)
- [ ] HTML to PDF 변환
- [ ] YAML 설정 파일 로드 (YamlDotNet)
- [ ] 규칙별 활성화/비활성화
- [ ] 임계값 조정 UI

**산출물**:
- PDF report generation
- `/config/default-config.yaml`

#### Week 11: 커스텀 규칙 플러그인 (FR-11)
- [ ] 플러그인 인터페이스 정의
- [ ] 플러그인 디렉토리 스캔
- [ ] DLL 로드 및 규칙 인스턴스화
- [ ] 정규식 기반 커스텀 규칙 지원

**산출물**:
- Plugin system
- Custom rule templates

**Phase 4 완료 기준**:
- ✅ PDF 리포트 생성 < 5초
- ✅ 커스텀 규칙 추가 가능
- ✅ 설정 파일 수정 시 즉시 적용

---

### 4.5 Phase 5: 배포 및 문서화 - 1주

#### Week 12: Marketplace 배포
- [ ] VSIX 패키지 빌드
- [ ] Visual Studio Marketplace 등록
- [ ] 자동 업데이트 설정
- [ ] 사용자 가이드 작성 (한글)
- [ ] API 문서 생성 (XML 주석 → DocFX)

**산출물**:
- `TwinCatQA.vsix`
- `/docs/user-guide.md`
- `/docs/api-reference/`

**Phase 5 완료 기준**:
- ✅ Marketplace 배포 완료
- ✅ 사용자 가이드 제공
- ✅ 자동 업데이트 정상 작동

---

## 5. 헌장 준수 검증

### 5.1 헌장 원칙 매핑

| 헌장 원칙 | 구현 방법 | 검증 규칙 |
|----------|----------|----------|
| **원칙 1: 한글 우선** | 모든 주석, 문서, 커밋 메시지 한글 | `KoreanCommentRule` (FR-2) |
| **원칙 2: 실시간 안전성** | 비상 정지 로직, 사이클 타임 고려 | `TemperatureStabilityRule` (FR-3), `CommunicationProtocolRule` (FR-5) |
| **원칙 3: 모듈화** | Function Block 단위 캡슐화 | AST 분석으로 FB 사용 여부 확인 |
| **원칙 4: 상태 머신** | 명시적 상태 머신, 상태 전이 명확성 | `StateMachineValidationRule` (FR-6), `CyclomaticComplexityRule` (FR-1) |
| **원칙 5: 명명 규칙** | FB_, g, camelCase, UPPER_CASE 등 | `NamingConventionRule` (FR-7) |
| **원칙 6: 문서화 의무** | Function Block 주석, 복잡한 로직 설명 | `DocumentationQualityRule` (FR-8) |
| **원칙 7: 버전 관리** | Git 커밋 메시지 형식 검증 | Git hook integration (FR-10) |
| **원칙 8: 테스트** | 테스트 케이스 존재 확인 | `/tests` 디렉토리 검증 |

### 5.2 헌장 준수 체크리스트

- [x] **원칙 1**: 모든 코드 주석은 한글로 작성됨
- [x] **원칙 2**: 실시간 안전성 검증 규칙 구현됨
- [x] **원칙 3**: Function Block 기반 모듈화 검증
- [x] **원칙 4**: 상태 머신 완전성 검증
- [x] **원칙 5**: 명명 규칙 자동 검증
- [x] **원칙 6**: 문서화 품질 자동 검증
- [x] **원칙 7**: Git 훅 통합으로 버전 관리 강제
- [x] **원칙 8**: 테스트 케이스 존재 여부 확인

**결과**: ✅ **모든 헌장 원칙이 구현 계획에 반영됨**

---

## 6. 리스크 관리

### 6.1 기술 리스크

| 리스크 | 가능성 | 영향 | 대응 계획 |
|--------|--------|------|----------|
| **ANTLR4 파싱 실패** (복잡한 ST 문법) | 중 | 높음 | - 단계적 문법 확장 (Phase 1: 기본 문법만)<br>- 테스트 주도 개발로 문법 검증 |
| **TwinCAT 파일 형식 변경** | 낮음 | 중간 | - XML 스키마 버전 체크<br>- 하위 호환성 유지 |
| **Visual Studio 버전 호환성** | 중 | 중간 | - 최소 VS 버전 명시 (2019)<br>- 조건부 컴파일로 API 차이 대응 |
| **성능 저하** (대규모 프로젝트) | 중 | 높음 | - 병렬 처리 (`Parallel.ForEach`)<br>- 증분 검증 (Diff 기반)<br>- 파일 캐싱 |
| **iText AGPL 라이선스** | 낮음 | 중간 | - PDF 기능 선택적 제공<br>- 오픈소스 공개 또는 상용 라이선스 구매 |

### 6.2 일정 리스크

| 리스크 | 대응 계획 |
|--------|----------|
| **Phase 1 지연** | - 핵심 기능 우선순위 조정<br>- HTML 리포트만 구현 (PDF 연기) |
| **도메인 규칙 복잡도** | - Phase 3을 두 단계로 분할<br>- 핵심 규칙만 Phase 3에 포함 |

---

## 7. 성공 기준

### 7.1 기능 성공 기준

| 기준 | 목표 | 측정 방법 |
|------|------|----------|
| **검증 속도** | 1000줄 코드 < 5분 | 성능 테스트 |
| **정확도** | 헌장 위반 감지 > 95% | 수동 검증 vs 도구 비교 |
| **커밋 시간 단축** | 수동 리뷰 대비 70% 단축 | 사용자 시간 측정 |
| **버그 감소** | 3개월 내 40% 감소 | 프로덕션 버그 추적 |
| **사용자 만족도** | 4.0/5.0 이상 | 설문 조사 |
| **오탐률** | False Positive < 10% | 위반 사항 검토 |

### 7.2 비기능 성공 기준

| 기준 | 목표 |
|------|------|
| **단위 테스트 커버리지** | 80% 이상 |
| **설치 시간** | < 5분 |
| **학습 곡선** | 30분 내 기본 검증 실행 가능 |
| **메모리 사용량** | < 2GB |
| **CPU 사용률** | < 50% (검증 실행 중) |

---

## 8. 리소스 및 일정

### 8.1 개발 리소스

| 역할 | 인원 | 기간 | 책임 |
|------|------|------|------|
| **Lead Developer** | 1명 | 12주 | 전체 아키텍처, 핵심 컴포넌트 개발 |
| **Parser Developer** | 1명 | 4주 | ANTLR4 문법, 파서 서비스 개발 |
| **UI Developer** | 1명 | 3주 | VS 확장, WPF UI 개발 |
| **QA Engineer** | 1명 | 12주 | 테스트, 검증, 문서화 |

### 8.2 일정 요약

| Phase | 기간 | 주요 마일스톤 |
|-------|------|---------------|
| **Phase 1: MVP** | Week 1-4 | 핵심 검증 기능 + HTML 리포트 |
| **Phase 2: Git 통합** | Week 5-6 | 증분 검증 + Pre-commit hook |
| **Phase 3: 도메인 규칙** | Week 7-9 | 12개 규칙 모두 구현 |
| **Phase 4: 고급 기능** | Week 10-11 | PDF + 커스텀 규칙 |
| **Phase 5: 배포** | Week 12 | Marketplace 배포 |

**총 개발 기간**: **12주 (약 3개월)**

### 8.3 예산 (선택 사항)

| 항목 | 비용 (참고) |
|------|-------------|
| 개발 인력 (4명 x 12주) | - |
| iText 7 상용 라이선스 (선택) | $1,500/개발자 |
| Visual Studio Enterprise (선택) | $250/월 |
| **총 예산** | - (내부 리소스 사용 시 무료) |

---

## 9. 다음 단계

### 9.1 즉시 조치

1. **구현 계획 승인**: 이해관계자 검토 및 승인
2. **개발 환경 설정**: Visual Studio, ANTLR4, NuGet 패키지 설치
3. **Git 브랜치 생성**: `feature/twincat-qa-tool`
4. **Phase 1 착수**: ANTLR4 문법 파일 작성

### 9.2 체크리스트

- [ ] 구현 계획서 승인 (팀 리더)
- [ ] 개발 환경 설정 완료
- [ ] Git 저장소 준비
- [ ] Phase 1 Week 1 착수
- [ ] 주간 진행 상황 보고 체계 수립

---

## 10. 부록

### 10.1 용어 정리

- **AST (Abstract Syntax Tree)**: 추상 구문 트리
- **VSIX**: Visual Studio Extension
- **ST (Structured Text)**: IEC 61131-3 구조화 텍스트 언어
- **FB (Function Block)**: 함수 블록
- **DUT (Data Unit Type)**: 사용자 정의 데이터 타입
- **GVL (Global Variable List)**: 전역 변수 목록

### 10.2 참조 링크

- [ANTLR4 Documentation](https://www.antlr.org/)
- [Visual Studio SDK](https://docs.microsoft.com/en-us/visualstudio/extensibility/)
- [LibGit2Sharp](https://github.com/libgit2/libgit2sharp)
- [IEC 61131-3 Standard](https://www.plcopen.org/iec-61131-3)

---

**구현 계획서 작성 완료**: 2025-11-20
**승인 대기 중**
**예상 개발 기간**: 12주
**예상 완료일**: 2026-02-12
