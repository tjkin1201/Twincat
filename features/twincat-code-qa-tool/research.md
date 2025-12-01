# 기술 조사 보고서: TwinCAT 코드 품질 검증 도구

**작성일**: 2025-11-20
**버전**: 1.0.0
**목적**: 구현 계획 수립을 위한 기술 스택 선정 및 아키텍처 결정

---

## 1. IEC 61131-3 파서 (Parser)

### 결정 (Decision)
**자체 파서 개발 (ANTLR4 기반)**

### 근거 (Rationale)
- **오픈소스 파서 부족**: IEC 61131-3의 모든 언어(ST, LD, FBD, SFC)를 완전히 지원하는 오픈소스 C# 파서가 존재하지 않음
- **TwinCAT 파일 형식**: TwinCAT 프로젝트는 XML 기반 (.tsproj, .TcPOU)이므로 XML 파싱 + ST 코드 추출이 필요
- **커스터마이징 필요**: 헌장 원칙 검증을 위한 도메인 특화 분석이 필요하므로 AST 제어 필요
- **ANTLR4 장점**:
  - IEC 61131-3 ST 언어에 대한 참조 문법(grammar) 존재
  - C# 타겟 코드 생성 지원
  - 강력한 AST 생성 및 Visitor 패턴 지원

### 고려한 대안 (Alternatives)
1. **ST-Parser (GitHub)**: Python 기반 파서로 C# 통합 어려움
2. **PLCopen XML**: LD/FBD 그래픽 표현만 제공, ST 파싱 불가
3. **정규식 기반 파싱**: 복잡한 문법 처리 한계, 중첩 구조 분석 불가능

### 구현 고려사항 (Implementation Notes)
- **Phase 1**: Structured Text (ST) 언어만 우선 지원
- **Phase 2**: LD/FBD는 TwinCAT XML에서 메타데이터 추출로 간접 검증
- **문법 파일**: IEC 61131-3 Part 3 표준 기반 ANTLR4 grammar 작성
- **TwinCAT 파일 파싱**:
  - `.tsproj`: XML 파싱으로 프로젝트 구조 추출
  - `.TcPOU`: XML 내 `<Declaration>`, `<Implementation>` 태그에서 ST 코드 추출
  - `System.Xml.Linq` (LINQ to XML) 사용

---

## 2. Visual Studio 확장 개발 (VSIX Extension)

### 결정 (Decision)
**Visual Studio SDK 2022 + VSIX 프로젝트**

### 근거 (Rationale)
- **TwinCAT XAE 통합**: TwinCAT XAE는 Visual Studio 위에서 실행되므로 VS 확장이 최적
- **Tool Window 제공**: 명세 요구사항(FR-11)에 맞춰 하단/측면 패널 도구 창 생성 가능
- **DTE (Development Tools Environment) API**: TwinCAT 프로젝트 구조에 네이티브 접근 가능
- **Marketplace 배포**: Visual Studio Marketplace를 통한 자동 배포 및 업데이트 지원

### 고려한 대안 (Alternatives)
1. **독립 실행형 애플리케이션**: VS 통합 없이 별도 실행 → 사용자 경험 저하
2. **VSCode 확장**: TwinCAT이 VSCode를 지원하지 않음

### 구현 고려사항 (Implementation Notes)
- **최소 요구사항**: Visual Studio 2019/2022 (TwinCAT 3.1 호환)
- **Tool Window 구현**: `ToolWindowPane` 클래스 상속
- **WPF UI**: Tool Window 내부는 WPF로 구현 (XAML + MVVM 패턴)
- **명령 통합**: VS 메뉴 바에 "TwinCAT QA" 메뉴 추가
- **이벤트 훅**:
  - 파일 저장 시 자동 검증 트리거 (`DocumentEvents.DocumentSaved`)
  - 솔루션 로드 시 설정 파일 로드

**참고 문서**:
- [Visual Studio SDK Documentation](https://docs.microsoft.com/en-us/visualstudio/extensibility/)
- [Creating a Tool Window](https://docs.microsoft.com/en-us/visualstudio/extensibility/creating-an-extension-with-a-tool-window)

---

## 3. 정적 코드 분석 엔진

### 결정 (Decision)
**규칙 기반 아키텍처 (Rule-Based Engine) + Visitor 패턴**

### 근거 (Rationale)
- **확장성**: 새로운 검증 규칙을 플러그인 방식으로 추가 가능
- **유지보수성**: 각 헌장 원칙을 독립된 규칙 클래스로 구현
- **성능**: AST 단일 순회(traversal)로 모든 규칙 적용 가능

### 아키텍처 설계

#### 3.1 핵심 인터페이스

```csharp
// 검증 규칙 인터페이스
public interface IValidationRule
{
    string RuleId { get; }
    string RuleName { get; }
    ConstitutionPrinciple RelatedPrinciple { get; }
    ViolationSeverity DefaultSeverity { get; }

    IEnumerable<Violation> Validate(SyntaxTree ast, CodeFile file);
}

// 위반 사항 모델
public class Violation
{
    public string RuleId { get; set; }
    public string FilePath { get; set; }
    public int Line { get; set; }
    public int Column { get; set; }
    public ViolationSeverity Severity { get; set; }
    public string Message { get; set; }
    public string Suggestion { get; set; }
}

// 심각도 열거형
public enum ViolationSeverity
{
    Low,
    Medium,
    High,
    Critical
}
```

#### 3.2 사이클로매틱 복잡도 계산

**라이브러리**: 자체 구현 (AST 기반)

**알고리즘**:
- McCabe's Cyclomatic Complexity: `M = E - N + 2P`
  - E = 간선(edge) 수
  - N = 노드(node) 수
  - P = 연결된 컴포넌트 수
- 간단한 계산: 결정 포인트(IF, CASE, FOR, WHILE) 개수 + 1

**구현**:
```csharp
public class CyclomaticComplexityCalculator : AntlrBaseVisitor<int>
{
    public override int VisitIfStatement(IfStatementContext context)
    {
        return 1 + base.VisitIfStatement(context);
    }

    public override int VisitCaseStatement(CaseStatementContext context)
    {
        return context.CaseElement().Length + base.VisitCaseStatement(context);
    }

    // FOR, WHILE, REPEAT 등 추가
}
```

#### 3.3 규칙 엔진 흐름

```
1. CodeFile 로드 → XML 파싱
2. ST 코드 추출 → ANTLR4 파싱 → AST 생성
3. 모든 IValidationRule 인스턴스에 AST 전달
4. 각 규칙이 Visitor 패턴으로 AST 순회하며 위반 수집
5. 위반 사항 집계 → ValidationSession 생성
6. 리포트 생성
```

### 고려한 대안 (Alternatives)
1. **Roslyn 기반 분석**: C# 전용이므로 ST 언어에 부적합
2. **SonarQube 플러그인**: PLC 언어 미지원, 커스터마이징 제약

### 구현 고려사항 (Implementation Notes)
- **규칙 검색**: 리플렉션으로 `IValidationRule` 구현체 자동 탐색
- **설정 파일**: YAML로 규칙별 활성화/비활성화 및 임계값 설정
- **성능 최적화**: 병렬 처리 (`Parallel.ForEach`)로 파일별 독립 검증

---

## 4. 리포트 생성

### 결정 (Decision)
- **HTML 리포트**: Razor Engine (RazorLight)
- **PDF 리포트**: iText 7 Community (AGPL 라이선스)
- **코드 하이라이팅**: Highlight.js (JavaScript, 클라이언트 측)

### 근거 (Rationale)
- **RazorLight**:
  - ASP.NET Razor 문법 사용 가능
  - 템플릿 기반 HTML 생성으로 유지보수 용이
  - MIT 라이선스 (오픈소스)
- **iText 7 Community**:
  - HTML to PDF 변환 지원
  - AGPL 라이선스 (내부 사용 시 문제없음, 배포 시 라이선스 공개 필요)
- **Highlight.js**:
  - 300+ 언어 지원 (커스텀 ST 문법 추가 가능)
  - HTML 리포트에 포함하여 브라우저에서 렌더링

### 고려한 대안 (Alternatives)
1. **PdfSharp**: PDF 생성만 가능, HTML 변환 미지원
2. **DinkToPdf**: wkhtmltopdf 래퍼, 외부 바이너리 의존성
3. **iTextSharp (v5)**: 구버전, 라이선스 복잡

### 구현 고려사항 (Implementation Notes)
- **템플릿 구조**:
  ```
  /templates/
    report.cshtml        # 메인 리포트 템플릿
    violation-list.cshtml # 위반 사항 목록
    chart.cshtml         # 품질 추세 차트
  ```
- **ST 언어 하이라이팅**: Highlight.js 커스텀 언어 정의
  ```javascript
  hljs.registerLanguage('st', function(hljs) {
    return {
      keywords: 'IF THEN ELSE ELSIF END_IF CASE OF END_CASE FOR TO DO END_FOR WHILE END_WHILE REPEAT UNTIL VAR END_VAR',
      contains: [/* ... */]
    };
  });
  ```
- **차트 생성**: Chart.js (JavaScript 라이브러리)로 품질 추세 시각화

---

## 5. Git 통합

### 결정 (Decision)
**LibGit2Sharp (v0.27+)**

### 근거 (Rationale)
- **순수 .NET 라이브러리**: C#에서 Git 작업 수행 (외부 git.exe 불필요)
- **LibGit2 기반**: 안정적이고 널리 사용되는 Git 라이브러리
- **Git Hook 관리**: `.git/hooks/` 파일 읽기/쓰기 가능
- **Diff 분석**: 변경된 파일 및 라인 추출 지원
- MIT 라이선스

### 주요 기능 구현

#### 5.1 Pre-Commit Hook 설치

```csharp
public void InstallPreCommitHook(string repoPath)
{
    var hookPath = Path.Combine(repoPath, ".git", "hooks", "pre-commit");
    var hookScript = @"#!/bin/sh
# TwinCAT QA Tool Pre-Commit Hook
dotnet tool run twincat-qa --mode diff --severity critical
exit $?
";

    File.WriteAllText(hookPath, hookScript);
    // Unix: chmod +x 설정 필요
}
```

#### 5.2 Git Diff 분석

```csharp
using (var repo = new Repository(repoPath))
{
    // 스테이징 영역 vs HEAD 비교
    var diff = repo.Diff.Compare<TreeChanges>(
        repo.Head.Tip.Tree,
        DiffTargets.Index
    );

    foreach (var change in diff)
    {
        if (change.Path.EndsWith(".TcPOU"))
        {
            var patch = repo.Diff.Compare<Patch>(
                repo.Head.Tip.Tree,
                DiffTargets.Index,
                new[] { change.Path }
            );

            // 변경된 라인 추출
            foreach (var line in patch[change.Path].LinesAdded)
            {
                // 라인 번호: line.LineNumber
                // 내용: line.Content
            }
        }
    }
}
```

#### 5.3 증분 검증 로직

**컨텍스트 범위 결정**:
```csharp
public CodeContext DetermineContext(int changedLine, SyntaxTree ast)
{
    // 변경 라인을 포함하는 최소 논리 단위 찾기
    var node = ast.FindNodeAt(changedLine);

    if (node is FunctionBlockNode fb)
        return new CodeContext { Type = ContextType.FunctionBlock, Lines = fb.LineRange };

    if (node is CaseStatementNode cs)
        return new CodeContext { Type = ContextType.CaseBlock, Lines = cs.LineRange };

    // 기본: ±10줄
    return new CodeContext {
        Type = ContextType.SurroundingLines,
        Lines = (changedLine - 10, changedLine + 10)
    };
}
```

### 고려한 대안 (Alternatives)
1. **Process.Start("git.exe")**: 외부 의존성, 출력 파싱 복잡
2. **NGit**: 개발 중단됨

### 구현 고려사항 (Implementation Notes)
- **Hook 스크립트**: Windows에서 `.sh` 실행을 위해 Git Bash 필요
- **대안**: PowerShell 스크립트 (`.ps1`) 사용
- **권한 문제**: Windows에서 Hook 실행 권한 체크 필요
- **성능**: 대용량 diff는 파일별 병렬 처리

---

## 6. 설정 관리

### 결정 (Decision)
**YAML 설정 파일 (YamlDotNet)**

### 근거 (Rationale)
- **가독성**: JSON보다 주석 및 중첩 구조 표현이 명확
- **YamlDotNet**: .NET 표준 YAML 라이브러리, MIT 라이선스

### 설정 파일 구조

```yaml
# .twincat-qa/config.yaml

# 전역 설정
global:
  enabled: true
  report_format: [html, pdf]
  auto_open_report: true

# 규칙 설정
rules:
  # FR-1: 복잡도 검증
  cyclomatic_complexity:
    enabled: true
    thresholds:
      medium: 10
      high: 15
    severity: medium

  # FR-2: 헌장 원칙 검증
  korean_comment:
    enabled: true
    required_ratio: 0.95
    severity: high

  # FR-3: 온도 제어 안정성
  temperature_stability:
    enabled: true
    change_rate_threshold: 0.10  # 10%
    consecutive_changes: 3
    severity: high

# Git Hook 설정
git:
  pre_commit:
    enabled: true
    block_on_critical: true
    incremental_validation: true

# 리포트 설정
report:
  output_dir: .twincat-qa/reports
  retention_months: 12
  include_charts: true
```

### 구현 고려사항 (Implementation Notes)
- **기본 설정**: 설정 파일 없을 시 내장 기본값 사용
- **검증**: 설정 파일 로드 시 스키마 검증
- **오버라이드**: 프로젝트 설정 → 사용자 설정 → 기본값 순으로 병합

---

## 7. 데이터 저장

### 결정 (Decision)
**로컬 파일 시스템 (JSON + HTML/PDF)**

### 근거 (Rationale)
- **명세 요구사항**: 로컬 파일 시스템 저장 명시됨
- **간단함**: 데이터베이스 설치 불필요
- **Git 친화적**: JSON 파일은 버전 관리 가능

### 디렉토리 구조

```
<TwinCAT 프로젝트 루트>/
  .twincat-qa/
    config.yaml                    # 설정 파일
    sessions/
      2025-11-20_103045.json      # 검증 세션 메타데이터
      2025-11-20_103045_violations.json  # 위반 사항 목록
    reports/
      2025-11-20_103045.html      # HTML 리포트
      2025-11-20_103045.pdf       # PDF 리포트
    history/
      quality-trend.json          # 품질 추세 데이터
```

### JSON 스키마 예시

```json
{
  "session_id": "2025-11-20_103045",
  "timestamp": "2025-11-20T10:30:45Z",
  "project_path": "D:\\Projects\\TwinCAT\\MyProject.tsproj",
  "scanned_files": 25,
  "total_lines": 3542,
  "quality_score": 87.5,
  "violations_by_severity": {
    "critical": 2,
    "high": 5,
    "medium": 12,
    "low": 8
  },
  "constitution_compliance": {
    "principle_1_korean": 0.96,
    "principle_2_safety": 1.0,
    "principle_3_modularity": 0.89,
    "...": "..."
  }
}
```

### 구현 고려사항 (Implementation Notes)
- **직렬화**: `System.Text.Json` (Newtonsoft.Json 대안)
- **보존 정책**: 12개월 이상 된 파일 자동 아카이브 제안
- **용량 관리**: 대용량 프로젝트 시 세션 파일 압축 (GZip)

---

## 8. 아키텍처 개요

### 전체 시스템 구성도

```
┌─────────────────────────────────────────────────────────┐
│                  Visual Studio Extension                 │
│  ┌─────────────────────────────────────────────────┐    │
│  │         Tool Window (WPF UI)                    │    │
│  │  - 검증 결과 표시                                │    │
│  │  - 위반 사항 목록                                │    │
│  │  - 품질 점수 차트                                │    │
│  └─────────────────────────────────────────────────┘    │
│                         ▲                                │
│                         │                                │
│  ┌─────────────────────────────────────────────────┐    │
│  │        Validation Engine (Core Logic)           │    │
│  │  ┌───────────┐  ┌──────────┐  ┌──────────┐     │    │
│  │  │ Rule 1    │  │ Rule 2   │  │ Rule N   │     │    │
│  │  │ (Korean)  │  │ (Safety) │  │ (...)    │     │    │
│  │  └───────────┘  └──────────┘  └──────────┘     │    │
│  └─────────────────────────────────────────────────┘    │
│                         ▲                                │
│                         │                                │
│  ┌─────────────────────────────────────────────────┐    │
│  │          Parser & AST Builder                   │    │
│  │  - ANTLR4 (ST 언어)                              │    │
│  │  - LINQ to XML (TwinCAT 파일)                    │    │
│  └─────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────┘
                          ▲
                          │
          ┌───────────────┴───────────────┐
          │                               │
  ┌───────────────┐              ┌────────────────┐
  │ TwinCAT Files │              │  Git Repo      │
  │ (.tsproj,     │              │  (LibGit2Sharp)│
  │  .TcPOU)      │              └────────────────┘
  └───────────────┘
          │
          ▼
  ┌───────────────────────┐
  │  Reports & Sessions   │
  │  (.twincat-qa/)       │
  └───────────────────────┘
```

### 레이어 아키텍처

1. **Presentation Layer** (VSIX Extension):
   - Tool Window (WPF)
   - Menu Commands
   - Event Handlers

2. **Application Layer**:
   - Validation Orchestrator
   - Report Generator
   - Configuration Manager

3. **Domain Layer**:
   - Validation Rules (IValidationRule 구현체)
   - Domain Models (CodeFile, Violation, ValidationSession)

4. **Infrastructure Layer**:
   - Parser (ANTLR4)
   - Git Integration (LibGit2Sharp)
   - File System Access

---

## 9. 성능 고려사항

### 목표
- 1000줄 코드 검증: < 5초 (명세 요구사항)
- Git 커밋 증분 검증: < 10초

### 최적화 전략

1. **병렬 처리**:
   ```csharp
   var violations = files.AsParallel()
       .SelectMany(file => ValidateFile(file))
       .ToList();
   ```

2. **증분 파싱**:
   - 변경된 파일만 재파싱
   - 이전 AST 캐싱 (메모리 허용 시)

3. **규칙 필터링**:
   - 설정에서 비활성화된 규칙은 로드하지 않음

4. **비동기 I/O**:
   ```csharp
   await File.ReadAllTextAsync(filePath);
   ```

---

## 10. 구현 우선순위

### Phase 1: MVP (Minimum Viable Product)
- [ ] ANTLR4 기반 ST 파서
- [ ] 핵심 규칙 5개 (FR-1, FR-2, FR-7)
- [ ] HTML 리포트 생성
- [ ] Visual Studio Tool Window

### Phase 2: Git 통합
- [ ] LibGit2Sharp 통합
- [ ] Pre-commit Hook
- [ ] 증분 검증 (FR-12)

### Phase 3: 도메인 특화 규칙
- [ ] 온도 제어 규칙 (FR-3)
- [ ] 레시피 관리 규칙 (FR-4)
- [ ] 통신 인터페이스 규칙 (FR-5)
- [ ] 상태 머신 규칙 (FR-6)

### Phase 4: 고급 기능
- [ ] PDF 리포트
- [ ] 커스텀 규칙 플러그인 (FR-11)
- [ ] 품질 추세 분석

---

## 11. 라이선스 요약

| 라이브러리 | 라이선스 | 상업적 사용 | 비고 |
|-----------|---------|------------|------|
| ANTLR4 | BSD-3-Clause | ✅ 가능 | 무제한 |
| Visual Studio SDK | Microsoft EULA | ✅ 가능 | VS 확장 용도 |
| RazorLight | MIT | ✅ 가능 | 무제한 |
| iText 7 Community | AGPL | ⚠️ 제한적 | 소스 공개 필요 또는 상용 라이선스 구매 |
| LibGit2Sharp | MIT | ✅ 가능 | 무제한 |
| YamlDotNet | MIT | ✅ 가능 | 무제한 |

**권장 조치**: iText 7은 AGPL이므로, PDF 기능을 선택적으로 제공하거나 상용 라이선스 구매 고려

---

## 12. 리스크 및 대응

### 리스크 1: TwinCAT 파일 형식 변경
- **가능성**: 낮음 (TwinCAT 3.1 이상 안정적)
- **영향**: 파서 호환성 깨짐
- **대응**: XML 스키마 버전 체크, 경고 메시지 표시

### 리스크 2: Visual Studio 버전 호환성
- **가능성**: 중간 (VS 2019/2022 차이)
- **영향**: API 변경으로 빌드 실패
- **대응**: 최소 VS 버전 명시, 조건부 컴파일

### 리스크 3: 성능 저하 (대규모 프로젝트)
- **가능성**: 중간 (10,000줄 이상 프로젝트)
- **영향**: 검증 시간 초과
- **대응**: 병렬 처리, 증분 검증, 진행률 표시

---

## 13. 다음 단계

1. **data-model.md** 작성: 엔티티 및 관계 정의
2. **contracts/** 생성: API 계약 (내부 인터페이스)
3. **quickstart.md** 작성: 개발 환경 설정 가이드
4. **plan.md** 작성: 구현 계획서 종합

---

**조사 완료**: 2025-11-20
**다음 문서**: data-model.md
