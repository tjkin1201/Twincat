# 구현 작업 목록: TwinCAT 코드 품질 검증 및 리뷰 도구

**작성일**: 2025-11-20
**버전**: 1.0.0
**총 작업 수**: 52개
**예상 기간**: 12주

---

## 목차

1. [개요](#개요)
2. [작업 구성](#작업-구성)
3. [Phase 1: 프로젝트 설정](#phase-1-프로젝트-설정-setup)
4. [Phase 2: 기반 인프라](#phase-2-기반-인프라-foundational)
5. [Phase 3: US1 - 전체 코드 품질 검증](#phase-3-us1---전체-코드-품질-검증)
6. [Phase 4: US2 - 온도 제어 로직 특화 검증](#phase-4-us2---온도-제어-로직-특화-검증)
7. [Phase 5: US3 - 통신 인터페이스 검증](#phase-5-us3---통신-인터페이스-검증)
8. [Phase 6: US4 - 레시피 관리 검증](#phase-6-us4---레시피-관리-검증)
9. [Phase 7: US5 - 품질 리포트 생성](#phase-7-us5---품질-리포트-생성)
10. [Phase 8: US6 - Git Diff 기반 증분 검증](#phase-8-us6---git-diff-기반-증분-검증)
11. [Phase 9: 통합 및 마무리](#phase-9-통합-및-마무리-polish)
12. [의존성 그래프](#의존성-그래프)
13. [병렬 실행 예시](#병렬-실행-예시)

---

## 개요

이 문서는 TwinCAT 코드 품질 검증 도구의 구현을 위한 작업 목록입니다.
각 작업은 사용자 시나리오(User Story)별로 그룹화되어 있으며, 독립적으로 테스트 가능한 증분 방식으로 구성되었습니다.

### 사용자 시나리오 매핑

| 시나리오 | 우선순위 | Phase | 설명 |
|---------|---------|-------|------|
| **US1** | P1 | Phase 3 | 전체 코드 품질 검증 (핵심 파서 + 3개 규칙) |
| **US2** | P2 | Phase 4 | 온도 제어 로직 특화 검증 |
| **US3** | P2 | Phase 5 | 통신 인터페이스 검증 |
| **US4** | P2 | Phase 6 | 레시피 관리 검증 |
| **US5** | P1 | Phase 7 | 품질 리포트 생성 (HTML/PDF) |
| **US6** | P2 | Phase 8 | Git Diff 기반 증분 검증 |

### MVP 범위

**Phase 1-3** (US1)만으로 최소 기능 제품(MVP)을 구성할 수 있습니다:
- 프로젝트 설정
- ANTLR4 파서 + 핵심 3개 규칙
- Visual Studio Tool Window

---

## 작업 구성

### 범례

- **[P]**: 병렬 실행 가능 (다른 파일 작업 시)
- **[Story]**: 사용자 시나리오 레이블 (US1, US2, US3...)
- **⚠️**: 차단 작업 (이전 작업 완료 필수)
- **✅**: 체크포인트 (사용자 시나리오 완료 확인)

---

## Phase 1: 프로젝트 설정 (Setup)

**목표**: 개발 환경 구성 및 프로젝트 구조 생성

### T001: Visual Studio 솔루션 생성
**파일**: `TwinCatQA.sln`
**설명**: Visual Studio 2022에서 VSIX 프로젝트 및 클래스 라이브러리 프로젝트 생성
**단계**:
1. Visual Studio 2022 실행
2. "Create a new project" 선택
3. "VSIX Project" 템플릿으로 `TwinCatQA` 프로젝트 생성
4. 솔루션에 다음 프로젝트 추가:
   - `TwinCatQA.Domain` (Class Library, .NET 6.0)
   - `TwinCatQA.Infrastructure` (Class Library, .NET 6.0)
   - `TwinCatQA.Application` (Class Library, .NET 6.0)
   - `TwinCatQA.Tests` (xUnit Test Project)
5. 프로젝트 참조 설정:
   - `TwinCatQA` → `TwinCatQA.Application` → `TwinCatQA.Domain`
   - `TwinCatQA.Application` → `TwinCatQA.Infrastructure`
   - `TwinCatQA.Tests` → 모든 프로젝트

**수락 기준**:
- 솔루션 빌드 성공
- 5개 프로젝트 모두 생성 완료

---

### T002: [P] NuGet 패키지 설치
**파일**: 각 프로젝트의 `.csproj`
**설명**: 필요한 외부 라이브러리 설치
**단계**:
1. `TwinCatQA.Infrastructure`:
   ```
   dotnet add package Antlr4.Runtime.Standard --version 4.11.1
   dotnet add package LibGit2Sharp --version 0.27.0
   dotnet add package System.Linq.Async --version 6.0.0
   ```
2. `TwinCatQA.Application`:
   ```
   dotnet add package YamlDotNet --version 13.0.0
   dotnet add package RazorLight --version 2.3.0
   dotnet add package itext7 --version 8.0.0
   ```
3. `TwinCatQA.Domain`:
   ```
   dotnet add package System.Text.Json --version 7.0.0
   ```
4. `TwinCatQA.Tests`:
   ```
   dotnet add package xunit --version 2.4.2
   dotnet add package Moq --version 4.18.0
   dotnet add package FluentAssertions --version 6.11.0
   ```

**수락 기준**:
- 모든 패키지 복원 성공
- 빌드 오류 없음

---

### T003: [P] 프로젝트 디렉토리 구조 생성
**파일**: 각 프로젝트 내 폴더
**설명**: 표준 디렉토리 구조 생성
**단계**:
1. `TwinCatQA.Domain/`:
   - `Models/` (CodeFile, Violation, ValidationSession 등)
   - `Contracts/` (IValidationRule, IValidationEngine 등)
2. `TwinCatQA.Infrastructure/`:
   - `Parsers/`
   - `Parsers/Grammars/` (ANTLR4 .g4 파일)
   - `Git/`
   - `Storage/`
3. `TwinCatQA.Application/`:
   - `Services/`
   - `Rules/` (검증 규칙 구현)
   - `Templates/` (Razor 템플릿)
4. `TwinCatQA/`:
   - `ToolWindows/`
   - `Commands/`

**수락 기준**:
- 모든 디렉토리 생성 완료

---

## Phase 2: 기반 인프라 (Foundational)

**목표**: 모든 사용자 시나리오에 필요한 핵심 인프라 구축

⚠️ **차단 작업**: Phase 1 완료 필수

### T004: 도메인 모델 정의 - 핵심 엔티티
**파일**: `TwinCatQA.Domain/Models/`
**설명**: 핵심 엔티티 클래스 작성
**단계**:
1. `CodeFile.cs`: 파일 경로, 타입, 언어, 라인 수, AST 등
2. `Violation.cs`: 규칙 ID, 심각도, 파일 경로, 라인, 메시지 등
3. `ValidationSession.cs`: 세션 ID, 프로젝트 경로, 통계, 품질 점수 등
4. `FunctionBlock.cs`: FB 이름, 복잡도, 변수 목록 등
5. `Variable.cs`: 변수명, 스코프, 데이터 타입 등
6. `Enums.cs`: ViolationSeverity, ConstitutionPrinciple, FileType 등

**수락 기준**:
- 6개 모델 클래스 작성 완료
- XML 주석 (한글) 추가
- 빌드 성공

---

### T005: [P] 계약 인터페이스 정의
**파일**: `TwinCatQA.Domain/Contracts/`
**설명**: 서비스 인터페이스 작성 (contracts/ 폴더 내용 복사)
**단계**:
1. `contracts/` 폴더의 `.cs` 파일 5개를 `TwinCatQA.Domain/Contracts/`로 복사:
   - `IValidationRule.cs`
   - `IValidationEngine.cs`
   - `IParserService.cs`
   - `IReportGenerator.cs`
   - `IGitService.cs`
2. 네임스페이스 수정: `TwinCatQA.Domain.Contracts`
3. 필요 시 `using` 구문 추가

**수락 기준**:
- 5개 인터페이스 파일 복사 완료
- 빌드 성공

---

### T006: ANTLR4 ST 문법 파일 작성
**파일**: `TwinCatQA.Infrastructure/Parsers/Grammars/StructuredText.g4`
**설명**: IEC 61131-3 Structured Text 언어 문법 정의
**단계**:
1. `quickstart.md`의 ANTLR4 문법 예시 복사
2. 다음 규칙 포함:
   - 변수 선언 (VAR, VAR_INPUT, VAR_OUTPUT)
   - Function Block 선언
   - 제어문 (IF, CASE, FOR, WHILE, REPEAT)
   - 표현식 (산술, 논리, 비교)
   - 주석 (블록, 라인)
3. 토큰 정의: IDENTIFIER, INTEGER_LITERAL, STRING_LITERAL 등

**수락 기준**:
- `.g4` 파일 작성 완료
- 문법 검증 (ANTLR4 테스트)

---

### T007: ANTLR4 문법 컴파일
**파일**: `TwinCatQA.Infrastructure/Parsers/Grammars/` (생성된 파일)
**설명**: ANTLR4로 C# 파서 코드 생성
**단계**:
1. ANTLR4 JAR 다운로드 (v4.11.1)
2. 명령 실행:
   ```bash
   cd TwinCatQA.Infrastructure/Parsers/Grammars
   java -jar antlr-4.11.1-complete.jar -Dlanguage=CSharp StructuredText.g4
   ```
3. 생성된 파일 확인:
   - `StructuredTextLexer.cs`
   - `StructuredTextParser.cs`
   - `StructuredTextVisitor.cs`
   - `StructuredTextBaseVisitor.cs`
4. 생성된 파일을 프로젝트에 포함

**수락 기준**:
- 4개 파일 생성 완료
- 프로젝트 빌드 성공

---

### T008: ParserService 구현 - 기본 구조
**파일**: `TwinCatQA.Infrastructure/Parsers/AntlrParserService.cs`
**설명**: IParserService 인터페이스 구현 (기본 골격)
**단계**:
1. `IParserService` 구현 클래스 생성
2. 주요 메서드 골격 작성:
   - `ParseFile(string filePath)`: TwinCAT XML 파싱 + ST 코드 추출
   - `ExtractFunctionBlocks(SyntaxTree ast)`
   - `ExtractVariables(SyntaxTree ast)`
   - `CalculateCyclomaticComplexity(FunctionBlock fb)`
   - `ExtractComments(SyntaxTree ast)`
3. LINQ to XML을 사용한 TwinCAT `.TcPOU` 파일 파싱:
   - `<Declaration>` 태그에서 변수 선언 추출
   - `<Implementation>` 태그에서 ST 코드 추출
4. ANTLR4 파서 호출 및 AST 생성

**수락 기준**:
- 클래스 작성 완료
- 간단한 ST 코드 파싱 테스트 성공

---

### T009: ValidationEngine 구현 - 기본 구조
**파일**: `TwinCatQA.Application/Services/DefaultValidationEngine.cs`
**설명**: IValidationEngine 인터페이스 구현
**단계**:
1. `IValidationEngine` 구현 클래스 생성
2. 주요 메서드 골격 작성:
   - `StartSession(string projectPath, ValidationMode mode)`
   - `ScanFiles(ValidationSession session)`: `.TcPOU` 파일 탐색
   - `ParseFiles(ValidationSession session, Action<double> progress)`
   - `RunValidation(ValidationSession session, Action<double> progress)`
   - `CalculateQualityScores(ValidationSession session)`
   - `GenerateReports(ValidationSession session)`
   - `CompleteSession(ValidationSession session)`
3. 규칙 로딩 로직 (리플렉션):
   - `IValidationRule` 구현체 자동 탐색
   - 활성화된 규칙만 필터링

**수락 기준**:
- 클래스 작성 완료
- 빈 세션 생성 및 완료 테스트 성공

---

✅ **Checkpoint: 기반 인프라 완료**
- 도메인 모델, 인터페이스, 파서, 엔진 골격 완성
- 다음 Phase부터 사용자 시나리오별 구현 시작

---

## Phase 3: US1 - 전체 코드 품질 검증

**목표**: 개발자가 Visual Studio에서 TwinCAT 프로젝트를 열고 전체 코드 검증을 실행할 수 있음

**사용자 시나리오**: 시나리오 1 - 코드 커밋 전 자동 검증

**독립 테스트 기준**:
- TwinCAT 프로젝트 열기 → 검증 실행 → 위반 사항 표시
- 핵심 3개 규칙 (한글 주석, 복잡도, 명명 규칙) 정상 작동
- 500줄 코드 10초 이내 검증

⚠️ **차단 작업**: Phase 2 완료 필수

---

### T010: [Story:US1] 규칙 1 - KoreanCommentRule 구현
**파일**: `TwinCatQA.Application/Rules/KoreanCommentRule.cs`
**설명**: 원칙 1 (한글 우선) - 주석이 한글인지 검증
**단계**:
1. `IValidationRule` 구현
2. `Validate(CodeFile file)` 메서드:
   - AST에서 주석 추출 (`IParserService.ExtractComments` 호출)
   - 각 주석에 한글 유니코드 범위 (U+AC00 ~ U+D7A3) 포함 여부 확인
   - 한글 비율 < 95% 시 위반 생성
3. `Configure(Dictionary<string, object> parameters)`:
   - `required_korean_ratio` 파라미터 로드
4. 위반 메시지 (한글):
   - "주석이 한글이 아닙니다: {주석 내용}"
   - "한글 주석 비율이 {ratio}%로 목표(95%)에 미달합니다."

**수락 기준**:
- 영어 주석 발견 시 위반 생성
- 한글 주석 비율 계산 정확

---

### T011: [Story:US1][P] 규칙 2 - CyclomaticComplexityRule 구현
**파일**: `TwinCatQA.Application/Rules/CyclomaticComplexityRule.cs`
**설명**: 원칙 4 (상태 머신) - 사이클로매틱 복잡도 검증
**단계**:
1. `IValidationRule` 구현
2. `Validate(CodeFile file)` 메서드:
   - `IParserService.ExtractFunctionBlocks` 호출
   - 각 FB에 대해 `IParserService.CalculateCyclomaticComplexity` 호출
   - 복잡도 >= 10: Medium 위반
   - 복잡도 >= 15: High 위반
3. `Configure`: `medium_threshold`, `high_threshold` 파라미터
4. 위반 메시지 (한글):
   - "Function Block '{name}'의 복잡도가 {complexity}로 임계값({threshold})을 초과합니다."

**수락 기준**:
- 복잡한 FB 감지
- 임계값에 따라 심각도 구분

---

### T012: [Story:US1][P] 규칙 3 - NamingConventionRule 구현
**파일**: `TwinCatQA.Application/Rules/NamingConventionRule.cs`
**설명**: 원칙 5 (명명 규칙) - 변수/FB 명명 규칙 검증
**단계**:
1. `IValidationRule` 구현
2. `Validate(CodeFile file)` 메서드:
   - Function Block: `FB_` 접두사 확인
   - 전역 변수: `g` 접두사 확인
   - 로컬 변수: camelCase 확인 (정규식: `^[a-z][a-zA-Z0-9]*$`)
   - 상수: UPPER_CASE 확인 (정규식: `^[A-Z][A-Z0-9_]*$`)
3. 위반 메시지 (한글):
   - "Function Block '{name}'은(는) 'FB_' 접두사로 시작해야 합니다."
   - "전역 변수 '{name}'은(는) 'g' 접두사로 시작해야 합니다."

**수락 기준**:
- 명명 규칙 위반 감지
- 헌장 원칙 참조 링크 포함

---

### T013: [Story:US1] Visual Studio Tool Window 생성
**파일**: `TwinCatQA/ToolWindows/QualityWindow.cs`, `QualityWindowControl.xaml`
**설명**: VS 하단/측면 패널에 품질 검증 도구 창 추가
**단계**:
1. `ToolWindowPane` 상속 클래스 생성
2. WPF UserControl 생성 (`QualityWindowControl.xaml`):
   - 도구 모음: "전체 검증", "증분 검증", "리포트 열기", "설정" 버튼
   - TabControl:
     - 탭 1: 위반 사항 목록 (DataGrid)
     - 탭 2: 품질 점수 (TextBlock + ListView)
   - 상태 표시줄: 검증 진행 상태
3. MVVM 패턴 적용:
   - `QualityWindowViewModel.cs` 생성
   - `ICommand` 구현 (RunValidationCommand 등)
4. VS 메뉴 통합:
   - "도구" → "TwinCAT 품질 검증" 메뉴 추가
   - `source.extension.vsixmanifest` 수정

**수락 기준**:
- Tool Window 표시
- 버튼 클릭 시 이벤트 핸들러 호출

---

### T014: [Story:US1] ValidationEngine과 Tool Window 연결
**파일**: `TwinCatQA/ToolWindows/QualityWindowControl.xaml.cs`
**설명**: 검증 버튼 클릭 시 ValidationEngine 실행 및 결과 표시
**단계**:
1. `RunValidation_Click` 이벤트 핸들러:
   - DTE (Development Tools Environment) API로 현재 솔루션 경로 가져오기
   - `IValidationEngine.StartSession` 호출
   - `ScanFiles`, `ParseFiles`, `RunValidation` 순차 호출
   - 진행률 콜백으로 ProgressBar 업데이트
2. 검증 완료 후 결과 표시:
   - `ViolationsGrid.ItemsSource = session.Violations`
   - `QualityScoreText.Text = session.OverallQualityScore.ToString("F1")`
   - `ConstitutionComplianceList.ItemsSource = session.ConstitutionCompliance`
3. 위반 항목 더블 클릭 시 해당 파일/라인으로 이동 (DTE API)

**수락 기준**:
- 검증 실행 성공
- 위반 사항 DataGrid에 표시
- 더블 클릭 시 코드 위치 이동

---

### T015: [Story:US1][P] 단위 테스트 - 파서
**파일**: `TwinCatQA.Tests/ParserTests.cs`
**설명**: ParserService 단위 테스트
**단계**:
1. 테스트 케이스 작성:
   - `ParseFile_ValidStCode_ReturnsAst`: 유효한 ST 코드 파싱
   - `ExtractFunctionBlocks_MultipleBlocks_ReturnsAll`: 여러 FB 추출
   - `CalculateCyclomaticComplexity_NestedIf_ReturnsCorrectValue`: 복잡도 계산
   - `ExtractComments_KoreanAndEnglish_ReturnsBoth`: 주석 추출
2. Moq로 의존성 모킹
3. FluentAssertions로 검증

**수락 기준**:
- 4개 테스트 통과
- 코드 커버리지 > 80%

---

### T016: [Story:US1][P] 단위 테스트 - 규칙
**파일**: `TwinCatQA.Tests/RuleTests.cs`
**설명**: 3개 검증 규칙 단위 테스트
**단계**:
1. 테스트 케이스:
   - `KoreanCommentRule_EnglishComment_ReturnsViolation`
   - `CyclomaticComplexityRule_HighComplexity_ReturnsHighSeverity`
   - `NamingConventionRule_InvalidFbName_ReturnsViolation`
2. 테스트 데이터: 간단한 ST 코드 스니펫
3. Assert: 위반 개수, 심각도, 메시지 내용

**수락 기준**:
- 9개 테스트 통과 (규칙당 3개)

---

✅ **Checkpoint: US1 완료**
- MVP 기능 완성: TwinCAT 프로젝트 검증 + 위반 표시
- Visual Studio Tool Window 정상 작동
- 핵심 3개 규칙 구현 완료

---

## Phase 4: US2 - 온도 제어 로직 특화 검증

**목표**: QA 엔지니어가 온도 제어 관련 Function Block의 안전성을 자동 검증할 수 있음

**사용자 시나리오**: 시나리오 2 - 온도 제어 로직 특화 검증

**독립 테스트 기준**:
- 온도 관련 변수명 패턴 자동 인식
- 센서 데이터 유효성 검사 누락 시 Critical 경고
- 알람 처리 로직 누락 시 High 경고
- 출력 변화율 임계값 초과 시 경고

⚠️ **차단 작업**: Phase 3 완료 필수

---

### T017: [Story:US2] 규칙 4 - TemperatureStabilityRule 구현
**파일**: `TwinCatQA.Application/Rules/TemperatureStabilityRule.cs`
**설명**: FR-3 - 온도 제어 안정성 검증
**단계**:
1. `IValidationRule` 구현
2. 온도 관련 변수 패턴 인식:
   - 정규식: `(temp|temperature|sensor)` (대소문자 무시)
3. 검증 항목:
   - **센서 데이터 범위 검증**: IF 문에서 범위 체크 (`temp > max` 또는 `temp < min`) 존재 확인
   - **알람 발생 로직**: 임계값 초과 시 `alarm := TRUE` 패턴 확인
   - **출력 변화율**: 연속된 출력 변경이 10% 초과하는지 AST에서 탐지 (경험적 휴리스틱)
4. 위반 생성:
   - 범위 검증 누락: Critical
   - 알람 로직 누락: High
   - 급격한 변화: Medium

**수락 기준**:
- 온도 변수 자동 인식
- 안전 로직 누락 시 위반 생성

---

### T018: [Story:US2][P] 단위 테스트 - 온도 규칙
**파일**: `TwinCatQA.Tests/TemperatureRuleTests.cs`
**설명**: TemperatureStabilityRule 테스트
**단계**:
1. 테스트 케이스:
   - `TemperatureVariable_NoRangeCheck_ReturnsCritical`
   - `TemperatureVariable_NoAlarm_ReturnsHigh`
   - `OutputChange_ExceedsThreshold_ReturnsMedium`
2. 테스트 ST 코드 작성

**수락 기준**:
- 3개 테스트 통과

---

✅ **Checkpoint: US2 완료**
- 온도 제어 특화 규칙 구현 완료
- QA 엔지니어가 온도 관련 FB 검증 가능

---

## Phase 5: US3 - 통신 인터페이스 검증

**목표**: 시니어 개발자가 EtherCAT/DeviceNet/RS-232 통신 코드의 오류 처리를 검증할 수 있음

**사용자 시나리오**: 시나리오 3 - 통신 인터페이스 코드 패턴 검증

**독립 테스트 기준**:
- 통신 타임아웃 처리 누락 시 High 경고
- 재연결 로직 누락 시 권고
- 에러 처리 누락 시 Critical 경고

⚠️ **차단 작업**: Phase 3 완료 필수 (Phase 4와 병렬 가능)

---

### T019: [Story:US3] 규칙 5 - CommunicationProtocolRule 구현
**파일**: `TwinCatQA.Application/Rules/CommunicationProtocolRule.cs`
**설명**: FR-5 - 통신 인터페이스 검증
**단계**:
1. 통신 관련 Function Block 패턴 인식:
   - FB 이름에 `(EtherCAT|DeviceNet|RS232|Communication|Comm)` 포함 (대소문자 무시)
2. 검증 항목:
   - **타임아웃 처리**: 변수명에 `timeout` 포함 또는 시간 관련 IF 조건 확인
   - **재연결 로직**: `WHILE` 루프 + `reconnect` 패턴
   - **에러 처리**: `IF error THEN` 또는 `CASE errorCode OF` 패턴
3. 위반 생성:
   - 타임아웃 누락: High
   - 재연결 누락: Medium
   - 에러 처리 누락: Critical

**수락 기준**:
- 통신 FB 자동 인식
- 오류 처리 패턴 감지

---

### T020: [Story:US3][P] 단위 테스트 - 통신 규칙
**파일**: `TwinCatQA.Tests/CommunicationRuleTests.cs`
**설명**: CommunicationProtocolRule 테스트
**단계**:
1. 테스트 케이스:
   - `CommunicationFb_NoTimeout_ReturnsHigh`
   - `CommunicationFb_NoErrorHandling_ReturnsCritical`
   - `CommunicationFb_NoReconnect_ReturnsMedium`

**수락 기준**:
- 3개 테스트 통과

---

✅ **Checkpoint: US3 완료**
- 통신 인터페이스 규칙 구현 완료
- 시니어 개발자가 통신 코드 검증 가능

---

## Phase 6: US4 - 레시피 관리 검증

**목표**: QA 엔지니어가 레시피 로드/저장 로직의 데이터 무결성을 검증할 수 있음

**사용자 시나리오**: 시나리오 4 - 레시피 관리 로직 일관성 검증

**독립 테스트 기준**:
- 레시피 DUT 자동 식별
- 유효성 검사 누락 시 High 경고
- 버전 필드 누락 시 권고

⚠️ **차단 작업**: Phase 3 완료 필수 (Phase 4-5와 병렬 가능)

---

### T021: [Story:US4] 규칙 6 - RecipeManagementRule 구현
**파일**: `TwinCatQA.Application/Rules/RecipeManagementRule.cs`
**설명**: FR-4 - 레시피 관리 검증
**단계**:
1. 레시피 DUT 패턴 인식:
   - DUT 이름에 `Recipe` 포함
   - 또는 주석에 "레시피" 키워드 포함
2. 검증 항목:
   - **구조체 일관성**: 모든 레시피 DUT가 동일한 필드 구조 보유
   - **유효성 검사**: `LoadRecipe` 함수에 `IF` 조건으로 검증 로직 존재
   - **버전 필드**: `version` 또는 `timestamp` 필드 존재
3. 위반 생성:
   - 유효성 검사 누락: High
   - 버전 필드 누락: Medium

**수락 기준**:
- 레시피 DUT 자동 식별
- 필수 필드 및 검증 로직 확인

---

### T022: [Story:US4][P] 단위 테스트 - 레시피 규칙
**파일**: `TwinCatQA.Tests/RecipeRuleTests.cs`
**설명**: RecipeManagementRule 테스트
**단계**:
1. 테스트 케이스:
   - `RecipeDut_NoVersionField_ReturnsMedium`
   - `LoadRecipe_NoValidation_ReturnsHigh`
   - `RecipeDut_InconsistentStructure_ReturnsHigh`

**수락 기준**:
- 3개 테스트 통과

---

✅ **Checkpoint: US4 완료**
- 레시피 관리 규칙 구현 완료
- QA 엔지니어가 레시피 로직 검증 가능

---

## Phase 7: US5 - 품질 리포트 생성

**목표**: 프로젝트 관리자가 전체 프로젝트의 코드 품질 현황을 리포트로 확인할 수 있음

**사용자 시나리오**: 시나리오 5 - 프로젝트 전체 코드 품질 리포트 생성

**독립 테스트 기준**:
- HTML 리포트 생성 < 5초
- 그래프 및 차트 포함
- 과거 리포트와 비교 가능

⚠️ **차단 작업**: Phase 3 완료 필수

---

### T023: [Story:US5] ReportGenerator 구현 - HTML
**파일**: `TwinCatQA.Application/Services/RazorReportGenerator.cs`
**설명**: IReportGenerator 구현 (HTML 리포트)
**단계**:
1. `IReportGenerator` 구현
2. Razor 템플릿 엔진 초기화 (RazorLight)
3. `GenerateHtmlReport(ValidationSession session)`:
   - 템플릿 파일 로드 (`templates/report.cshtml`)
   - ReportModel 생성 (session 데이터 변환)
   - Razor 렌더링
   - HTML 파일 저장 (`.twincat-qa/reports/`)
4. 자동으로 브라우저에서 열기 (optional)

**수락 기준**:
- HTML 파일 생성
- 브라우저에서 정상 표시

---

### T024: [Story:US5] Razor 템플릿 작성
**파일**: `TwinCatQA.Application/Templates/report.cshtml`
**설명**: HTML 리포트 Razor 템플릿
**단계**:
1. 템플릿 구조:
   - 헤더: 프로젝트 이름, 검증 날짜
   - 요약 섹션: 전체 품질 점수, 위반 개수
   - 위반 목록 테이블: 심각도, 규칙, 파일, 라인, 메시지
   - 헌장 준수율 레이더 차트 (Chart.js)
   - 품질 추세 선 그래프 (과거 세션 비교)
2. Highlight.js 통합 (ST 언어 커스텀 정의):
   ```javascript
   hljs.registerLanguage('st', function(hljs) {
     return {
       keywords: 'IF THEN ELSE CASE OF FOR TO WHILE END_IF END_CASE...',
       contains: [...]
     };
   });
   ```
3. Chart.js 차트 데이터 생성 (Razor @model 사용)

**수락 기준**:
- 템플릿 렌더링 성공
- 차트 표시

---

### T025: [Story:US5][P] ReportGenerator 구현 - PDF
**파일**: `TwinCatQA.Application/Services/RazorReportGenerator.cs` (확장)
**설명**: PDF 리포트 생성 (iText 7)
**단계**:
1. `GeneratePdfReport(ValidationSession session)`:
   - HTML 리포트 생성 (`GenerateHtmlReport` 재사용)
   - iText 7 HTML to PDF 변환:
     ```csharp
     var html = File.ReadAllText(htmlPath);
     var pdf = new PdfDocument(new PdfWriter(pdfPath));
     HtmlConverter.ConvertToPdf(html, pdf);
     ```
   - PDF 파일 저장

**수락 기준**:
- PDF 파일 생성
- PDF 뷰어에서 정상 표시

---

### T026: [Story:US5] Tool Window에 리포트 열기 기능 추가
**파일**: `TwinCatQA/ToolWindows/QualityWindowControl.xaml.cs`
**설명**: "리포트 열기" 버튼 동작
**단계**:
1. `OpenReport_Click` 이벤트 핸들러:
   - 최근 검증 세션의 리포트 경로 가져오기
   - `Process.Start(reportPath)` 호출 (기본 브라우저 또는 PDF 뷰어)
2. 리포트 목록 표시 (선택 사항):
   - 과거 세션 목록 표시
   - 사용자가 특정 리포트 선택하여 열기

**수락 기준**:
- 버튼 클릭 시 리포트 열림

---

### T027: [Story:US5][P] 단위 테스트 - 리포트 생성
**파일**: `TwinCatQA.Tests/ReportGeneratorTests.cs`
**설명**: ReportGenerator 테스트
**단계**:
1. 테스트 케이스:
   - `GenerateHtmlReport_ValidSession_CreatesFile`
   - `GeneratePdfReport_ValidSession_CreatesFile`
   - `Report_ContainsAllViolations_ReturnsTrue`

**수락 기준**:
- 3개 테스트 통과

---

✅ **Checkpoint: US5 완료**
- HTML 및 PDF 리포트 생성 완료
- 프로젝트 관리자가 품질 현황 확인 가능

---

## Phase 8: US6 - Git Diff 기반 증분 검증

**목표**: 시니어 개발자가 변경된 코드만 집중적으로 검증하여 코드 리뷰 효율성을 극대화할 수 있음

**사용자 시나리오**: 시나리오 6 - Git Diff 기반 증분 코드 리뷰

**독립 테스트 기준**:
- Git diff 분석 < 3초 (100 파일)
- 증분 검증 시간 80% 단축
- 변경 전후 비교 리포트

⚠️ **차단 작업**: Phase 3 완료 필수

---

### T028: [Story:US6] GitService 구현
**파일**: `TwinCatQA.Infrastructure/Git/LibGit2Service.cs`
**설명**: IGitService 구현 (LibGit2Sharp)
**단계**:
1. `IGitService` 구현
2. 주요 메서드:
   - `IsGitRepository(string path)`: `.git` 폴더 존재 확인
   - `GetChangedFiles(string repoPath, DiffTarget target)`:
     - LibGit2Sharp `Repository` 객체 생성
     - `Diff.Compare` 호출 (스테이징 vs HEAD)
     - 변경된 `.TcPOU` 파일 필터링
   - `GetChangedLines(string repoPath, string filePath)`:
     - `Patch` 객체에서 추가/수정/삭제 라인 추출
     - `LineChange` 목록 반환
   - `DetermineContext(CodeFile file, int changedLine)`:
     - 변경 라인을 포함하는 최소 논리 단위 찾기 (FB, CASE, IF 블록)
     - 컨텍스트 범위 반환 (LineRange)

**수락 기준**:
- Git diff 분석 성공
- 변경 라인 정확히 추출

---

### T029: [Story:US6] ValidationEngine - 증분 검증 모드
**파일**: `TwinCatQA.Application/Services/DefaultValidationEngine.cs` (확장)
**설명**: ValidationMode.Incremental 지원
**단계**:
1. `StartSession` 메서드 수정:
   - `mode == ValidationMode.Incremental` 시 Git diff 분석
   - `IGitService.GetChangedFiles` 호출
   - 변경된 파일만 세션에 추가
2. `ParseFiles` 메서드 수정:
   - 변경 파일만 파싱
3. `RunValidation` 메서드 수정:
   - 각 위반에 대해 `IsChangedLine` 플래그 추가
   - 변경 라인 컨텍스트 범위 내 위반만 리포트

**수락 기준**:
- 증분 검증 모드 실행
- 변경 파일만 검증

---

### T030: [Story:US6] Tool Window - 증분 검증 버튼
**파일**: `TwinCatQA/ToolWindows/QualityWindowControl.xaml.cs`
**설명**: "증분 검증" 버튼 동작
**단계**:
1. `RunIncremental_Click` 이벤트 핸들러:
   - `ValidationMode.Incremental`로 세션 시작
   - 진행률 표시
   - 결과를 별도 탭 또는 필터로 표시
2. 변경 전후 비교 UI:
   - "신규 위반", "해결된 위반", "기존 위반" 구분 표시
   - 품질 점수 변화 델타 (예: +5.2)

**수락 기준**:
- 버튼 클릭 시 증분 검증 실행
- 변경 사항만 표시

---

### T031: [Story:US6] Git Hook 설치 기능
**파일**: `TwinCatQA.Infrastructure/Git/LibGit2Service.cs` (확장)
**설명**: Pre-commit hook 자동 설치
**단계**:
1. `InstallPreCommitHook(string repoPath, bool blockOnCritical)`:
   - `.git/hooks/pre-commit` 파일 생성
   - PowerShell 스크립트 작성:
     ```powershell
     # TwinCAT QA Pre-Commit Hook
     dotnet tool run twincat-qa --mode incremental --severity critical
     if ($LASTEXITCODE -ne 0) {
         Write-Error "Critical violations found. Commit blocked."
         exit 1
     }
     exit 0
     ```
   - Windows에서 실행 권한 설정
2. `UninstallPreCommitHook(string repoPath)`:
   - 훅 파일 삭제

**수락 기준**:
- Hook 설치 성공
- Git 커밋 시 자동 검증 실행

---

### T032: [Story:US6][P] 단위 테스트 - Git 서비스
**파일**: `TwinCatQA.Tests/GitServiceTests.cs`
**설명**: GitService 테스트
**단계**:
1. 테스트 케이스:
   - `GetChangedFiles_ModifiedFile_ReturnsFile`
   - `GetChangedLines_AddedLines_ReturnsCorrectCount`
   - `DetermineContext_ChangedLineInFb_ReturnsFbContext`

**수락 기준**:
- 3개 테스트 통과

---

✅ **Checkpoint: US6 완료**
- Git Diff 기반 증분 검증 완료
- Pre-commit hook 설치 가능
- 시니어 개발자가 변경 사항만 검증 가능

---

## Phase 9: 통합 및 마무리 (Polish)

**목표**: 도메인 특화 규칙 추가 및 전체 시스템 통합 테스트

⚠️ **차단 작업**: Phase 3-8 완료 필수

---

### T033: [P] 규칙 7 - StateMachineValidationRule 구현
**파일**: `TwinCatQA.Application/Rules/StateMachineValidationRule.cs`
**설명**: FR-6 - 상태 머신 완전성 검증
**단계**:
1. 상태 ENUM 타입 인식
2. CASE 문에서 모든 ENUM 값 처리 확인
3. 기본 케이스 (ELSE) 존재 확인
4. 순환 탐지 (데드락 가능성)

**수락 기준**:
- 미처리 상태 감지
- 기본 케이스 누락 경고

---

### T034: [P] 규칙 8 - DocumentationQualityRule 구현
**파일**: `TwinCatQA.Application/Rules/DocumentationQualityRule.cs`
**설명**: FR-8 - 주석 품질 검증
**단계**:
1. Function Block 주석 존재 확인
2. 복잡도 > 10 시 주석 필수
3. TODO/FIXME 주석 추적

**수락 기준**:
- 주석 누락 시 경고
- TODO 주석 감지

---

### T035: [P] 설정 파일 로드 - ConfigurationManager
**파일**: `TwinCatQA.Application/Services/ConfigurationManager.cs`
**설명**: YAML 설정 파일 로드 및 적용
**단계**:
1. YamlDotNet으로 `.twincat-qa/config.yaml` 파싱
2. `ConfigurationProfile` 객체 생성
3. 각 규칙에 파라미터 전달 (`IValidationRule.Configure`)
4. 기본 설정 파일 없으면 생성

**수락 기준**:
- YAML 파싱 성공
- 규칙별 설정 적용

---

### T036: Tool Window - 설정 UI
**파일**: `TwinCatQA/ToolWindows/SettingsWindow.cs`
**설명**: "설정" 버튼 클릭 시 설정 창 표시
**단계**:
1. 새 Tool Window 또는 Dialog 생성
2. 규칙 목록 표시 (CheckBox로 활성화/비활성화)
3. 임계값 조정 (NumericUpDown)
4. 저장 버튼 클릭 시 YAML 파일 업데이트

**수락 기준**:
- 설정 UI 표시
- 변경 사항 저장

---

### T037: [P] 데이터 저장 - FileSystemRepository
**파일**: `TwinCatQA.Infrastructure/Storage/FileSystemRepository.cs`
**설명**: 검증 세션 JSON 저장 및 로드
**단계**:
1. `IValidationSessionRepository` 구현
2. `Save(ValidationSession session)`:
   - `.twincat-qa/sessions/{sessionId}.json` 저장
   - `System.Text.Json`으로 직렬화
3. `GetById(Guid sessionId)`: JSON 로드 및 역직렬화
4. `GetRecentSessions(int days)`: 최근 세션 목록 반환

**수락 기준**:
- JSON 파일 저장/로드 성공

---

### T038: 통합 테스트 - 전체 워크플로우
**파일**: `TwinCatQA.Tests/IntegrationTests.cs`
**설명**: 엔드투엔드 테스트
**단계**:
1. 테스트 케이스:
   - `FullValidation_CompleteProject_GeneratesReport`:
     - 테스트 TwinCAT 프로젝트 생성
     - 전체 검증 실행
     - 리포트 생성 확인
     - 위반 개수 검증
   - `IncrementalValidation_ChangedFile_OnlyValidatesChanges`:
     - Git 저장소 초기화
     - 파일 수정 커밋
     - 증분 검증 실행
     - 변경 파일만 검증 확인
2. Assert: 예상 위반 개수, 리포트 파일 존재

**수락 기준**:
- 2개 통합 테스트 통과

---

### T039: [P] 성능 최적화
**파일**: 여러 파일
**설명**: 병렬 처리 및 캐싱
**단계**:
1. `ValidationEngine`:
   - `Parallel.ForEach`로 파일별 병렬 검증
   - 진행률 콜백 thread-safe 구현
2. `ParserService`:
   - AST 캐싱 (Dictionary<string, SyntaxTree>)
   - 파일 해시 비교로 재파싱 회피
3. 성능 테스트:
   - 1000줄 코드 검증 시간 측정
   - 목표: < 5분

**수락 기준**:
- 병렬 처리 정상 작동
- 성능 목표 달성

---

### T040: 오류 처리 및 로깅
**파일**: 여러 파일
**설명**: 예외 처리 및 로그 추가
**단계**:
1. 모든 public 메서드에 try-catch 추가
2. 로그 라이브러리 통합 (Serilog 또는 NLog)
3. 로그 파일: `.twincat-qa/logs/twincat-qa.log`
4. Tool Window에 오류 메시지 표시

**수락 기준**:
- 예외 발생 시 앱 크래시 없음
- 로그 파일 생성

---

### T041: 사용자 가이드 작성
**파일**: `/docs/user-guide.md`
**설명**: 최종 사용자 가이드 (한글)
**단계**:
1. 설치 방법
2. 기본 사용법 (전체 검증, 증분 검증)
3. 설정 파일 커스터마이징
4. Git Hook 설치
5. 리포트 해석
6. 문제 해결 (Troubleshooting)

**수락 기준**:
- 가이드 문서 작성 완료

---

### T042: VSIX 패키지 빌드
**파일**: `TwinCatQA.vsix`
**설명**: Visual Studio Marketplace 배포 준비
**단계**:
1. `source.extension.vsixmanifest` 최종 수정:
   - 버전, 설명, 아이콘, 스크린샷 추가
   - 지원 VS 버전 명시 (2019, 2022)
2. Release 모드 빌드
3. VSIX 파일 생성 확인
4. 로컬 설치 테스트

**수락 기준**:
- VSIX 파일 생성
- 로컬 VS에 설치 성공

---

✅ **Checkpoint: 전체 구현 완료**
- 12개 검증 규칙 모두 구현
- Visual Studio 확장 완성
- HTML/PDF 리포트 생성
- Git 통합 완료
- 배포 준비 완료

---

## 의존성 그래프

### 사용자 시나리오 완료 순서

```
Phase 1 (Setup)
   └─→ Phase 2 (Foundational)
          ├─→ Phase 3 (US1: 전체 검증) ─┐
          │                            ├─→ Phase 7 (US5: 리포트)
          ├─→ Phase 4 (US2: 온도) ─────┤
          ├─→ Phase 5 (US3: 통신) ─────┤
          ├─→ Phase 6 (US4: 레시피) ───┘
          │
          └─→ Phase 8 (US6: Git Diff)
                 └─→ Phase 9 (통합 및 마무리)
```

### Phase 간 의존성

- **Phase 1 → Phase 2**: 필수 (프로젝트 구조 필요)
- **Phase 2 → Phase 3-8**: 필수 (파서 및 엔진 필요)
- **Phase 3 → Phase 4-8**: 권장 (핵심 기능 먼저)
- **Phase 4-6**: 병렬 가능 (독립적 규칙)
- **Phase 3 → Phase 7**: 권장 (검증 결과 필요)
- **Phase 3 → Phase 8**: 권장 (기본 검증 로직 필요)
- **Phase 3-8 → Phase 9**: 필수 (통합 전 개별 기능 완성)

---

## 병렬 실행 예시

### 예시 1: Phase 2 작업 병렬화

```bash
# 병렬 실행 가능 (다른 파일)
- T004: 도메인 모델 정의 (CodeFile.cs, Violation.cs...)
- T005: 계약 인터페이스 정의 (IValidationRule.cs...)
```

### 예시 2: Phase 3 규칙 구현 병렬화

```bash
# 병렬 실행 가능 (다른 파일)
- T010: KoreanCommentRule.cs
- T011: CyclomaticComplexityRule.cs
- T012: NamingConventionRule.cs

# 순차 실행 필요 (T013은 T010-T012 규칙 필요)
- T013: Tool Window 생성
- T014: ValidationEngine 연결
```

### 예시 3: Phase 4-6 사용자 시나리오 병렬화

```bash
# 병렬 실행 가능 (독립적 시나리오)
Phase 4 (US2: 온도)
Phase 5 (US3: 통신)
Phase 6 (US4: 레시피)
```

### 예시 4: Phase 9 통합 작업 병렬화

```bash
# 병렬 실행 가능
- T033: StateMachineValidationRule.cs
- T034: DocumentationQualityRule.cs
- T035: ConfigurationManager.cs
- T037: FileSystemRepository.cs

# 순차 실행 필요
- T038: 통합 테스트 (모든 컴포넌트 필요)
- T039: 성능 최적화 (테스트 결과 필요)
```

---

## 구현 전략

### MVP 우선 (Phase 1-3)

**목표**: 4주 내 사용 가능한 최소 제품
**범위**:
- 프로젝트 설정 (Phase 1)
- 기반 인프라 (Phase 2)
- 전체 코드 검증 (Phase 3: US1)

**결과**: 개발자가 Visual Studio에서 TwinCAT 프로젝트를 열고, 검증 실행하여 위반 사항을 확인할 수 있음

---

### 증분 배포 (Phase 4-8)

각 사용자 시나리오를 독립적으로 완성하고 배포:
- **Week 5-6**: US2 (온도) + US3 (통신)
- **Week 7**: US4 (레시피)
- **Week 8-9**: US5 (리포트)
- **Week 10-11**: US6 (Git Diff)

---

### 최종 통합 (Phase 9)

- **Week 12**: 도메인 규칙 추가 + 통합 테스트 + 배포

---

## 작업 요약

| Phase | 작업 수 | 예상 기간 | 주요 산출물 |
|-------|---------|----------|------------|
| **Phase 1** | 3 | 1일 | 솔루션, NuGet, 폴더 구조 |
| **Phase 2** | 6 | 2주 | 모델, 인터페이스, 파서, 엔진 |
| **Phase 3** | 7 | 2주 | US1 - MVP 완성 |
| **Phase 4** | 2 | 1주 | US2 - 온도 규칙 |
| **Phase 5** | 2 | 1주 | US3 - 통신 규칙 |
| **Phase 6** | 2 | 1주 | US4 - 레시피 규칙 |
| **Phase 7** | 5 | 2주 | US5 - 리포트 생성 |
| **Phase 8** | 5 | 2주 | US6 - Git 통합 |
| **Phase 9** | 10 | 1주 | 통합 및 배포 |
| **총계** | **42** | **12주** | 완성된 VSIX 패키지 |

---

## 다음 단계

1. **Phase 1 착수**: T001 (솔루션 생성) 실행
2. **주간 체크포인트**: 각 Phase 완료 후 독립 테스트 수행
3. **사용자 피드백**: MVP (Phase 3) 완료 후 내부 사용자 테스트
4. **반복 개선**: 피드백 반영하여 Phase 4-9 조정

---

**작업 목록 작성 완료**: 2025-11-20
**총 작업 수**: 42개
**MVP 범위**: Phase 1-3 (US1)
**배포 준비**: Phase 9 완료 후
