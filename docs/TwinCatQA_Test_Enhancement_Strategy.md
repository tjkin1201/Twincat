# TwinCatQA 프로젝트 테스트 강화 전략 (Test Enhancement Strategy)

## 📊 현재 상태 분석

### 프로젝트 규모
- **소스 파일**: 311개 C# 파일
- **테스트 파일**: 70개 C# 테스트 파일
- **현재 커버리지**: ~70%
- **테스트 프로젝트**: 5개
  - TwinCatQA.Domain.Tests (11개 테스트)
  - TwinCatQA.Application.Tests (57개+ 테스트)
  - TwinCatQA.Infrastructure.Tests (36개+ 테스트)
  - TwinCatQA.Integration.Tests (E2E 테스트)
  - TwinCatQA.Grammar.Tests (파서 테스트)

### 테스트 도구 스택
- **테스트 프레임워크**: xUnit 2.6.2
- **Assertion 라이브러리**: FluentAssertions 6.12.0
- **Mocking**: Moq 4.20.70
- **커버리지**: Coverlet 6.0.0
- **테스트 SDK**: Microsoft.NET.Test.Sdk 17.8.0

---

## 🎯 목표 및 KPI

### 주요 목표
1. **커버리지 향상**: 70% → 90%+ (목표 95%)
2. **E2E 테스트 확장**: 현재 5개 → 20개 시나리오
3. **성능/부하 테스트 추가**: 0개 → 10개 벤치마크
4. **CI/CD 파이프라인 구축**: GitHub Actions 기반 자동화

### 성공 지표 (KPI)
- **라인 커버리지**: 95% 이상
- **브랜치 커버리지**: 85% 이상
- **테스트 실행 시간**: 5분 이내
- **테스트 안정성**: 99.9% 이상 (flaky test < 0.1%)
- **테스트 유지보수 시간**: 월 2시간 이내

---

## 📈 1. 커버리지 향상 전략 (70% → 90%+)

### 1.1 미커버 영역 분석

#### 높은 우선순위 (커버리지 < 60%)
1. **TwinCatQA.Grammar (파서 계층)**
   - `StructuredTextParser.cs` - 핵심 파싱 로직
   - `StructuredTextLexer.cs` - 토큰화 로직
   - `ASTBuilder.cs` - 구문 트리 생성
   - **추가 필요 테스트**: 30개

2. **TwinCatQA.Infrastructure.Git**
   - `LibGit2Service.cs` - Git 통합
   - `PreCommitHookInstaller.cs` - Git Hook 설치
   - **추가 필요 테스트**: 15개

3. **TwinCatQA.Application.Services**
   - `GraphvizVisualizationService.cs` - 시각화
   - `QaReportGenerator.cs` - 리포트 생성 (부분 커버)
   - `AdvancedAnalysisOrchestrator.cs` - 고급 분석
   - **추가 필요 테스트**: 25개

#### 중간 우선순위 (커버리지 60-80%)
4. **TwinCatQA.Domain.Models.AST**
   - `ExpressionNodes.cs` - 표현식 노드
   - `StatementNodes.cs` - 구문 노드
   - `IASTVisitor.cs` - Visitor 패턴
   - **추가 필요 테스트**: 20개

5. **TwinCatQA.Application.Rules**
   - 기존 규칙의 엣지 케이스
   - 에러 핸들링 시나리오
   - **추가 필요 테스트**: 15개

#### 낮은 우선순위 (커버리지 > 80%)
6. **TwinCatQA.CLI**
   - 명령줄 인터페이스 (사용자 입력 시나리오)
   - **추가 필요 테스트**: 10개

7. **TwinCatQA.UI**
   - WPF 사용자 인터페이스 (UI 테스트)
   - **추가 필요 테스트**: 5개 (기본 시나리오만)

### 1.2 추가 테스트 케이스 목록 (총 120개)

#### A. Grammar/Parser 테스트 (30개)

**StructuredTextParserTests.cs**
1. `Parse_SimpleFunctionBlock_ShouldReturnValidAST()` - 단순 FB 파싱
2. `Parse_ComplexFunctionBlock_WithNestedIF_ShouldReturnValidAST()` - 중첩 IF 문
3. `Parse_CASE_Statement_WithMultipleBranches_ShouldParseCorrectly()` - CASE 문
4. `Parse_FOR_Loop_WithStepIncrement_ShouldParseCorrectly()` - FOR 루프
5. `Parse_WHILE_Loop_WithComplexCondition_ShouldParseCorrectly()` - WHILE 루프
6. `Parse_REPEAT_UNTIL_Loop_ShouldParseCorrectly()` - REPEAT 루프
7. `Parse_VariableDeclaration_AllTypes_ShouldReturnCorrectNodes()` - 모든 변수 타입
8. `Parse_ArrayDeclaration_MultiDimensional_ShouldParseCorrectly()` - 다차원 배열
9. `Parse_StructDeclaration_WithNestedTypes_ShouldParseCorrectly()` - 구조체
10. `Parse_EnumDeclaration_ShouldReturnEnumNode()` - 열거형
11. `Parse_FunctionCall_WithParameters_ShouldReturnCallNode()` - 함수 호출
12. `Parse_PropertyAccess_DotNotation_ShouldParseCorrectly()` - 속성 접근
13. `Parse_ArrayAccess_WithExpression_ShouldParseCorrectly()` - 배열 접근
14. `Parse_BinaryExpression_AllOperators_ShouldParseCorrectly()` - 이항 연산
15. `Parse_UnaryExpression_AllOperators_ShouldParseCorrectly()` - 단항 연산
16. `Parse_Comment_SingleLine_ShouldSkip()` - 주석 처리
17. `Parse_Comment_MultiLine_ShouldSkip()` - 블록 주석
18. `Parse_Pragma_Directives_ShouldReturnPragmaNode()` - 프라그마
19. `Parse_InvalidSyntax_MissingEND_FUNCTION_BLOCK_ShouldThrowException()` - 구문 오류
20. `Parse_InvalidSyntax_UnexpectedToken_ShouldReturnError()` - 토큰 오류
21. `Parse_LargeFile_1000Lines_ShouldCompleteWithinTimeout()` - 성능 테스트
22. `Parse_EmptyFile_ShouldReturnEmptyAST()` - 빈 파일
23. `Parse_UTF8_WithBOM_ShouldHandleCorrectly()` - UTF-8 BOM
24. `Parse_MixedNewLines_CRLF_LF_ShouldNormalize()` - 줄바꿈 처리
25. `Parse_EscapeSequences_InStrings_ShouldHandleCorrectly()` - 이스케이프 시퀀스
26. `Parse_OperatorPrecedence_ComplexExpression_ShouldRespectOrder()` - 연산자 우선순위
27. `Parse_TypeConversion_ExplicitCast_ShouldParseCorrectly()` - 타입 변환
28. `Parse_PointerType_Declaration_ShouldReturnPointerNode()` - 포인터
29. `Parse_ReferenceType_Declaration_ShouldReturnReferenceNode()` - 참조
30. `Parse_InterfaceImplementation_ShouldReturnInterfaceNode()` - 인터페이스

**StructuredTextLexerTests.cs** (이미 존재할 경우 추가)
- 토큰화 정확성 검증 (키워드, 식별자, 리터럴, 연산자)

---

#### B. Git 통합 테스트 (15개)

**LibGit2ServiceTests.cs**
1. `IsGitRepository_ValidRepo_ShouldReturnTrue()` - Git 저장소 검증
2. `IsGitRepository_NonGitFolder_ShouldReturnFalse()` - 비Git 폴더
3. `GetChangedFiles_StagedFiles_ShouldReturnList()` - 스테이징된 파일
4. `GetChangedFiles_WorkingDirectory_ShouldIncludeModified()` - 작업 디렉토리 변경
5. `GetChangedFiles_EmptyCommit_ShouldReturnEmpty()` - 변경 없음
6. `GetFileContent_ValidCommit_ShouldReturnContent()` - 파일 내용 조회
7. `GetFileContent_NonExistentFile_ShouldReturnNull()` - 존재하지 않는 파일
8. `GetDiff_TwoCommits_ShouldReturnDiffText()` - 커밋 간 diff
9. `GetDiff_UncommittedChanges_ShouldReturnWorkingDiff()` - 미커밋 변경
10. `GetBranches_ShouldReturnAllBranches()` - 브랜치 목록
11. `GetCurrentBranch_ShouldReturnActiveBranch()` - 현재 브랜치
12. `GetCommitHistory_WithLimit_ShouldReturnTopN()` - 커밋 이력
13. `GetCommitHistory_WithDateRange_ShouldFilterCorrectly()` - 날짜 필터
14. `Initialize_EmptyFolder_ShouldCreateGitRepo()` - Git 초기화
15. `Clone_RemoteRepo_ShouldSucceed()` - 저장소 복제 (통합 테스트)

**PreCommitHookInstallerTests.cs**
1. `InstallHook_ValidRepo_ShouldCreateHookFile()` - Hook 설치
2. `InstallHook_ExistingHook_ShouldBackupAndOverwrite()` - 기존 Hook 백업
3. `UninstallHook_ShouldRemoveHookFile()` - Hook 제거
4. `IsHookInstalled_AfterInstall_ShouldReturnTrue()` - Hook 설치 확인
5. `GenerateHookScript_ShouldIncludeQACommand()` - Hook 스크립트 생성

---

#### C. 고급 분석 서비스 테스트 (25개)

**GraphvizVisualizationServiceTests.cs**
1. `GenerateCallGraph_SimpleFB_ShouldReturnDotFormat()` - 호출 그래프 생성
2. `GenerateCallGraph_CircularDependency_ShouldDetectCycle()` - 순환 의존성
3. `GenerateDataFlowDiagram_ShouldShowVariableFlow()` - 데이터 흐름도
4. `GenerateControlFlowGraph_WithBranches_ShouldShowAllPaths()` - 제어 흐름 그래프
5. `RenderGraph_AsPNG_ShouldCreateImageFile()` - PNG 렌더링
6. `RenderGraph_AsSVG_ShouldCreateVectorFile()` - SVG 렌더링
7. `GenerateDependencyMatrix_ShouldShowModuleDeps()` - 의존성 매트릭스

**QaReportGeneratorTests.cs** (확장)
1. `GenerateReport_AllFormats_ShouldSucceed()` - 모든 형식 생성 (HTML, JSON, Markdown)
2. `GenerateReport_EmptySession_ShouldCreateMinimalReport()` - 빈 세션
3. `GenerateReport_LargeSession_1000Files_ShouldCompleteWithinTimeout()` - 대용량
4. `GenerateReport_CustomTemplate_ShouldApplyTemplate()` - 사용자 정의 템플릿
5. `GenerateReport_WithCharts_ShouldIncludeChartData()` - 차트 포함
6. `GenerateReport_WithImages_ShouldEmbedOrLinkImages()` - 이미지 포함
7. `GenerateReport_MultiLanguage_Korean_ShouldFormatCorrectly()` - 한글 처리

**AdvancedAnalysisOrchestratorTests.cs**
1. `RunFullAnalysis_CompleteWorkflow_ShouldGenerateAllResults()` - 전체 워크플로우
2. `RunFullAnalysis_WithParallelization_ShouldBeFasterThanSerial()` - 병렬 처리
3. `AnalyzeComplexity_CyclomaticComplexity_ShouldCalculateCorrectly()` - 복잡도
4. `AnalyzeMaintainability_ShouldScoreCodeQuality()` - 유지보수성
5. `AnalyzeSafety_CriticalSections_ShouldIdentifyRisks()` - 안전성
6. `AnalyzeDependencies_ShouldBuildDependencyGraph()` - 의존성 분석
7. `AnalyzePerformance_EstimateExecutionTime_ShouldProvideMetrics()` - 성능 분석
8. `AnalyzeTestability_ShouldScoreTestCoverage()` - 테스트 가능성
9. `AnalyzeDocumentation_CommentCoverage_ShouldCalculateRatio()` - 문서화
10. `AnalyzeArchitecture_LayerViolations_ShouldDetectIssues()` - 아키텍처 검증
11. `CancelAnalysis_LongRunning_ShouldStopGracefully()` - 취소 처리

---

#### D. AST 모델 테스트 (20개)

**ExpressionNodesTests.cs**
1. `BinaryExpression_Addition_ShouldCalculateConstantValue()` - 상수 폴딩
2. `BinaryExpression_Division_ByZero_ShouldValidate()` - 0으로 나누기
3. `UnaryExpression_NOT_ShouldInvertBoolean()` - 논리 반전
4. `FunctionCallExpression_WithArguments_ShouldMatchSignature()` - 함수 호출
5. `ArrayAccessExpression_OutOfBounds_ShouldDetect()` - 배열 범위 초과
6. `PropertyAccessExpression_ChainedAccess_ShouldResolve()` - 체인 접근
7. `CastExpression_InvalidCast_ShouldValidate()` - 타입 변환 검증
8. `LiteralExpression_AllTypes_ShouldParseCorrectly()` - 리터럴

**StatementNodesTests.cs**
1. `IfStatement_NestedConditions_ShouldEvaluateCorrectly()` - 중첩 조건문
2. `CaseStatement_DefaultBranch_ShouldCoverUnmatchedCases()` - CASE 기본 분기
3. `ForLoop_EmptyBody_ShouldHandleGracefully()` - 빈 루프
4. `WhileLoop_InfiniteLoop_ShouldDetect()` - 무한 루프 감지
5. `RepeatLoop_ExitCondition_ShouldValidate()` - REPEAT 종료 조건
6. `AssignmentStatement_TypeMismatch_ShouldValidate()` - 할당 타입 검증
7. `ReturnStatement_InFunction_ShouldValidate()` - 반환문

**IASTVisitorTests.cs**
1. `Visitor_TraverseEntireTree_ShouldVisitAllNodes()` - 전체 순회
2. `Visitor_DepthFirstSearch_ShouldFollowOrder()` - 깊이 우선
3. `Visitor_BreadthFirstSearch_ShouldFollowOrder()` - 너비 우선
4. `Visitor_CustomFilter_ShouldSkipNodes()` - 필터링
5. `Visitor_Transform_ShouldModifyTree()` - AST 변환

---

#### E. 규칙 엣지 케이스 테스트 (15개)

**KoreanCommentRule - 추가 케이스**
1. `Validate_EmojiInComment_ShouldNotCountAsKorean()` - 이모지 처리
2. `Validate_ChineseCharacters_ShouldNotCountAsKorean()` - 한자
3. `Validate_JapaneseCharacters_ShouldNotCountAsKorean()` - 일본어
4. `Validate_MixedScriptComment_50Percent_ShouldViolate()` - 혼합 스크립트
5. `Validate_CommentWithCode_ShouldExcludeCodeTokens()` - 코드 포함 주석

**CyclomaticComplexityRule - 추가 케이스**
1. `Validate_NestedLoops_5Levels_ShouldCalculateCorrectComplexity()` - 중첩 루프
2. `Validate_ShortCircuitEvaluation_ShouldCountBranches()` - 단락 평가
3. `Validate_TernaryOperator_ShouldAddComplexity()` - 삼항 연산자
4. `Validate_ExceptionHandling_TRY_CATCH_ShouldAddComplexity()` - 예외 처리

**NamingConventionRule - 추가 케이스**
1. `Validate_UnicodeIdentifiers_ShouldValidateCorrectly()` - 유니코드 식별자
2. `Validate_ReservedKeywords_AsIdentifiers_ShouldViolate()` - 예약어
3. `Validate_UnderscorePrefix_PrivateMembers_ShouldPass()` - 언더스코어
4. `Validate_SCREAMING_SNAKE_CASE_Constants_ShouldPass()` - 상수 네이밍
5. `Validate_PascalCase_Enums_ShouldPass()` - 열거형 네이밍
6. `Validate_CamelCase_Parameters_ShouldPass()` - 매개변수 네이밍

---

#### F. CLI 및 UI 테스트 (15개)

**QaCommandTests.cs** (CLI)
1. `Execute_Analyze_WithValidPath_ShouldSucceed()` - 분석 명령
2. `Execute_Compare_TwoFolders_ShouldShowDifferences()` - 비교 명령
3. `Execute_Report_GenerateHTML_ShouldCreateFile()` - 리포트 명령
4. `Execute_Init_CreateConfigFile_ShouldSucceed()` - 초기화 명령
5. `Execute_Validate_CheckConfigFile_ShouldReportErrors()` - 검증 명령
6. `Execute_Help_ShouldDisplayUsage()` - 도움말
7. `Execute_Version_ShouldDisplayVersion()` - 버전
8. `Execute_InvalidCommand_ShouldDisplayError()` - 잘못된 명령
9. `Execute_WithVerboseFlag_ShouldShowDetailedOutput()` - Verbose 플래그
10. `Execute_WithQuietFlag_ShouldSuppressOutput()` - Quiet 플래그

**MainWindowViewModelTests.cs** (UI - WPF)
1. `LoadProject_ValidPath_ShouldPopulateFileList()` - 프로젝트 로드
2. `StartAnalysis_ShouldUpdateProgressBar()` - 진행률 업데이트
3. `CancelAnalysis_ShouldStopExecution()` - 분석 취소
4. `FilterViolations_BySeverity_ShouldUpdateList()` - 위반 필터링
5. `ExportResults_ToExcel_ShouldCreateFile()` - 결과 내보내기

---

### 1.3 커버리지 측정 및 모니터링

#### Coverlet 설정 파일 생성

**coverlet.runsettings** 파일 생성:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<RunSettings>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="XPlat Code Coverage">
        <Configuration>
          <Format>opencover,cobertura,json,lcov</Format>
          <Exclude>[xunit.*]*,[*.Tests]*,[*.TestHelpers]*</Exclude>
          <ExcludeByAttribute>Obsolete,GeneratedCode,CompilerGenerated</ExcludeByAttribute>
          <ExcludeByFile>**/Migrations/*.cs,**/*Designer.cs</ExcludeByFile>
          <IncludeTestAssembly>false</IncludeTestAssembly>
          <SingleHit>false</SingleHit>
          <UseSourceLink>true</UseSourceLink>
          <IncludeDirectory>../../src</IncludeDirectory>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
  <RunConfiguration>
    <MaxCpuCount>0</MaxCpuCount>
  </RunConfiguration>
</RunSettings>
```

#### 커버리지 실행 스크립트

**scripts/run-coverage.ps1**:

```powershell
# 테스트 및 커버리지 수집
dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings --results-directory ./TestResults

# 커버리지 리포트 생성 (ReportGenerator)
dotnet tool install --global dotnet-reportgenerator-globaltool
reportgenerator "-reports:./TestResults/**/coverage.opencover.xml" "-targetdir:./TestResults/CoverageReport" "-reporttypes:Html;Badges"

# 브라우저에서 리포트 열기
Start-Process "./TestResults/CoverageReport/index.html"
```

#### 커버리지 임계값 설정

**Directory.Build.props** 파일에 추가:

```xml
<PropertyGroup>
  <CoverletOutputFormat>opencover,cobertura</CoverletOutputFormat>
  <Threshold>90</Threshold>
  <ThresholdType>line,branch</ThresholdType>
  <ThresholdStat>total</ThresholdStat>
</PropertyGroup>
```

---

## 🔄 2. E2E (End-to-End) 테스트 확장 전략

### 2.1 E2E 테스트 시나리오 목록 (20개)

#### 핵심 워크플로우 시나리오 (High Priority)

**TwinCatQA.Integration.Tests/Scenarios/**

1. **완전한 QA 분석 워크플로우**
   - `Scenario_01_FullQAWorkflow_NewProject_ShouldGenerateReport()`
   - 단계:
     1. 새 TwinCAT 프로젝트 로드
     2. 모든 .TcPOU 파일 스캔
     3. 전체 규칙 실행
     4. HTML 리포트 생성
     5. 결과 검증 (위반 개수, 심각도 분포)

2. **Git 통합 워크플로우**
   - `Scenario_02_GitIntegration_PreCommitHook_ShouldBlockBadCode()`
   - 단계:
     1. Git 저장소 초기화
     2. Pre-commit Hook 설치
     3. 품질 기준 미달 코드 커밋 시도
     4. Hook이 커밋 차단 확인
     5. 코드 수정 후 커밋 성공 확인

3. **규칙 사용자 정의 워크플로우**
   - `Scenario_03_CustomRules_LoadAndExecute_ShouldApplyUserSettings()`
   - 단계:
     1. YAML 설정 파일 생성 (커스텀 규칙 정의)
     2. 설정 로드
     3. 사용자 정의 규칙 실행
     4. 사용자 정의 임계값 적용 확인

4. **대용량 프로젝트 분석**
   - `Scenario_04_LargeProject_500Files_ShouldCompleteWithinTimeLimit()`
   - 단계:
     1. 500개 파일이 포함된 프로젝트 생성
     2. 병렬 분석 실행
     3. 5분 이내 완료 확인
     4. 메모리 사용량 500MB 이하 확인

5. **증분 분석 (Incremental Analysis)**
   - `Scenario_05_IncrementalAnalysis_OnlyChangedFiles_ShouldBeFaster()`
   - 단계:
     1. 전체 분석 실행 (baseline)
     2. 5개 파일만 수정
     3. 증분 분석 실행
     4. 수정된 파일만 재분석 확인
     5. 성능 개선 (80% 시간 단축) 확인

6. **폴더 비교 워크플로우**
   - `Scenario_06_FolderComparison_TwoVersions_ShouldShowDelta()`
   - 단계:
     1. Version 1.0 프로젝트 분석
     2. Version 2.0 프로젝트 분석
     3. 두 버전 비교
     4. 개선된 메트릭, 새로운 위반 확인

7. **CI/CD 통합 워크플로우**
   - `Scenario_07_CICD_AutomatedPipeline_ShouldGenerateArtifacts()`
   - 단계:
     1. CLI 명령으로 분석 실행
     2. JSON 리포트 생성
     3. JUnit XML 생성 (테스트 결과 형식)
     4. 품질 게이트 평가 (90% 이상 통과)
     5. CI/CD 시스템에서 결과 파싱 확인

#### 안전성 검증 시나리오 (Safety Critical)

8. **안전 규칙 검증 - 배열 범위 체크**
   - `Scenario_08_SafetyRules_ArrayBoundsCheck_ShouldDetectUnsafeCode()`
   - 단계:
     1. 범위 체크 없는 배열 접근 코드 로드
     2. ArrayBoundsRule 실행
     3. Critical 위반 감지 확인

9. **안전 규칙 검증 - 부동소수점 비교**
   - `Scenario_09_SafetyRules_FloatingPointComparison_ShouldDetectDirect Equality()`
   - 단계:
     1. REAL 타입 직접 비교 코드 로드
     2. FloatingPointComparisonRule 실행
     3. Critical 위반 및 Epsilon 사용 권장 확인

10. **안전 규칙 검증 - NULL 체크**
    - `Scenario_10_SafetyRules_NullCheck_ShouldDetectMissingChecks()`
    - 단계:
      1. 포인터/참조 NULL 체크 누락 코드 로드
      2. NullCheckRule 실행
      3. Critical 위반 감지 확인

#### 성능 및 확장성 시나리오

11. **병렬 분석 성능**
    - `Scenario_11_ParallelAnalysis_MultiCore_ShouldUtilizeAllCores()`
    - 단계:
      1. 100개 파일 프로젝트 준비
      2. 직렬 분석 실행 (baseline)
      3. 병렬 분석 실행 (4코어)
      4. 3배 이상 성능 향상 확인

12. **메모리 효율성**
    - `Scenario_12_MemoryEfficiency_LargeAST_ShouldNotCauseOOM()`
    - 단계:
      1. 5000라인 초대형 함수 블록 로드
      2. AST 생성
      3. 메모리 사용량 모니터링
      4. 메모리 누수 없음 확인

13. **캐싱 효율성**
    - `Scenario_13_CachingEfficiency_RepeatedAnalysis_ShouldReuseResults()`
    - 단계:
      1. 동일한 파일 3번 분석
      2. 첫 번째: Full 파싱
      3. 두 번째, 세 번째: 캐시 사용
      4. 90% 시간 단축 확인

#### 복잡한 코드 패턴 시나리오

14. **중첩 구조 분석**
    - `Scenario_14_ComplexCode_Nested5Levels_ShouldParseCorrectly()`
    - 단계:
      1. 5단계 중첩 IF-CASE-FOR 코드 로드
      2. 파싱 및 복잡도 계산
      3. 정확한 Cyclomatic Complexity 확인

15. **재귀 함수 분석**
    - `Scenario_15_RecursiveFunction_ShouldDetectRecursion()`
    - 단계:
      1. 재귀 함수 코드 로드
      2. 호출 그래프 생성
      3. 재귀 감지 확인

16. **함수 포인터 및 델리게이트**
    - `Scenario_16_FunctionPointers_ShouldResolveIndirectCalls()`
    - 단계:
      1. 함수 포인터 사용 코드 로드
      2. 간접 호출 분석
      3. 가능한 호출 대상 목록 생성

#### 리포팅 및 시각화 시나리오

17. **다중 형식 리포트 생성**
    - `Scenario_17_MultiFormatReport_HTML_JSON_Markdown_ShouldGenerateAll()`
    - 단계:
      1. 분석 실행
      2. HTML, JSON, Markdown 동시 생성
      3. 각 형식의 무결성 검증

18. **Graphviz 시각화**
    - `Scenario_18_Graphviz_CallGraph_ShouldGeneratePNG()`
    - 단계:
      1. 복잡한 프로젝트 분석
      2. 호출 그래프 생성 (DOT 형식)
      3. Graphviz로 PNG 렌더링
      4. 이미지 파일 생성 확인

19. **트렌드 분석 (Time Series)**
    - `Scenario_19_TrendAnalysis_MultipleRuns_ShouldShowImprovement()`
    - 단계:
      1. 동일 프로젝트 주간 분석 (5주치 데이터)
      2. 트렌드 차트 생성
      3. 품질 지표 개선 추세 확인

#### 에러 핸들링 및 복원력 시나리오

20. **손상된 파일 처리**
    - `Scenario_20_CorruptedFile_ShouldSkipAndContinue()`
    - 단계:
      1. 10개 정상 파일 + 1개 손상된 파일 로드
      2. 분석 실행
      3. 손상된 파일 스킵하고 나머지 파일 분석
      4. 에러 로그에 손상된 파일 기록 확인

### 2.2 E2E 테스트 환경 설정

#### 테스트 픽스처 프로젝트

**TwinCatQA.Integration.Tests/Fixtures/** 디렉토리 구조:

```
Fixtures/
├── SimpleProject/             # 단순 프로젝트 (10 파일)
├── MediumProject/             # 중간 프로젝트 (50 파일)
├── LargeProject/              # 대형 프로젝트 (500 파일)
├── SafetyCriticalProject/     # 안전 규칙 테스트용
├── CorruptedProject/          # 에러 처리 테스트용
└── RealWorldProject/          # 실제 프로젝트 샘플
```

#### E2E 테스트 헬퍼

**TwinCatQA.Integration.Tests/Helpers/E2ETestHelper.cs**:

```csharp
public class E2ETestHelper
{
    public string CreateTempProject(string templateName);
    public void CleanupTempProjects();
    public ValidationSession RunFullAnalysis(string projectPath);
    public void AssertReportGenerated(string reportPath, ReportFormat format);
    public void AssertQualityThreshold(ValidationSession session, double minScore);
}
```

---

## ⚡ 3. 성능 및 부하 테스트 전략

### 3.1 성능 테스트 벤치마크 기준 (10개)

#### 벤치마크 라이브러리: BenchmarkDotNet

**NuGet 패키지 추가**:
```bash
dotnet add package BenchmarkDotNet
```

#### 벤치마크 테스트 프로젝트

**TwinCatQA.Benchmarks/TwinCatQA.Benchmarks.csproj**:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="BenchmarkDotNet" Version="0.13.12" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\TwinCatQA.Application\TwinCatQA.Application.csproj" />
    <ProjectReference Include="..\..\src\TwinCatQA.Grammar\TwinCatQA.Grammar.csproj" />
  </ItemGroup>
</Project>
```

### 3.2 벤치마크 시나리오

#### 1. 파서 성능 벤치마크

**ParserBenchmarks.cs**:

```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class ParserBenchmarks
{
    private string _smallFile;  // 100 라인
    private string _mediumFile; // 1,000 라인
    private string _largeFile;  // 10,000 라인
    private StructuredTextParser _parser;

    [GlobalSetup]
    public void Setup()
    {
        _smallFile = File.ReadAllText("Fixtures/small.st");
        _mediumFile = File.ReadAllText("Fixtures/medium.st");
        _largeFile = File.ReadAllText("Fixtures/large.st");
        _parser = new StructuredTextParser();
    }

    [Benchmark(Baseline = true)]
    public SyntaxTree Parse_SmallFile_100Lines()
    {
        return _parser.Parse(_smallFile);
    }

    [Benchmark]
    public SyntaxTree Parse_MediumFile_1000Lines()
    {
        return _parser.Parse(_mediumFile);
    }

    [Benchmark]
    public SyntaxTree Parse_LargeFile_10000Lines()
    {
        return _parser.Parse(_largeFile);
    }
}
```

**성능 목표**:
- Small (100 라인): < 10ms
- Medium (1,000 라인): < 100ms
- Large (10,000 라인): < 1,000ms (1초)

---

#### 2. 규칙 실행 성능 벤치마크

**RuleExecutionBenchmarks.cs**:

```csharp
[MemoryDiagnoser]
public class RuleExecutionBenchmarks
{
    private CodeFile _codeFile;
    private List<IValidationRule> _rules;

    [GlobalSetup]
    public void Setup()
    {
        _codeFile = LoadTestFile("complex_function_block.st");
        _rules = new List<IValidationRule>
        {
            new KoreanCommentRule(...),
            new CyclomaticComplexityRule(...),
            new NamingConventionRule(...),
            // ... 15개 규칙
        };
    }

    [Benchmark]
    public List<Violation> ExecuteAllRules_SingleFile()
    {
        var violations = new List<Violation>();
        foreach (var rule in _rules)
        {
            violations.AddRange(rule.Validate(_codeFile));
        }
        return violations;
    }

    [Benchmark]
    public List<Violation> ExecuteAllRules_ParallelExecution()
    {
        return _rules
            .AsParallel()
            .SelectMany(rule => rule.Validate(_codeFile))
            .ToList();
    }
}
```

**성능 목표**:
- 단일 파일, 15개 규칙: < 50ms
- 병렬 실행 시: 2배 이상 성능 향상

---

#### 3. AST 순회 성능 벤치마크

**ASTTraversalBenchmarks.cs**:

```csharp
[MemoryDiagnoser]
public class ASTTraversalBenchmarks
{
    private SyntaxTree _shallowTree;  // Depth: 5, Nodes: 100
    private SyntaxTree _deepTree;     // Depth: 20, Nodes: 1,000

    [Benchmark]
    public int CountNodes_DepthFirst_ShallowTree()
    {
        var visitor = new CountingVisitor();
        visitor.Visit(_shallowTree.Root);
        return visitor.NodeCount;
    }

    [Benchmark]
    public int CountNodes_DepthFirst_DeepTree()
    {
        var visitor = new CountingVisitor();
        visitor.Visit(_deepTree.Root);
        return visitor.NodeCount;
    }

    [Benchmark]
    public int CountNodes_BreadthFirst_DeepTree()
    {
        return BFSCount(_deepTree.Root);
    }
}
```

**성능 목표**:
- Shallow Tree (100 nodes): < 1ms
- Deep Tree (1,000 nodes): < 10ms

---

#### 4. 리포트 생성 성능 벤치마크

**ReportGenerationBenchmarks.cs**:

```csharp
[MemoryDiagnoser]
public class ReportGenerationBenchmarks
{
    private ValidationSession _smallSession;  // 10 files, 50 violations
    private ValidationSession _largeSession;  // 500 files, 5,000 violations

    [Benchmark]
    public string Generate_HTMLReport_SmallSession()
    {
        var generator = new HtmlReportGenerator();
        return generator.Generate(_smallSession);
    }

    [Benchmark]
    public string Generate_HTMLReport_LargeSession()
    {
        var generator = new HtmlReportGenerator();
        return generator.Generate(_largeSession);
    }

    [Benchmark]
    public string Generate_JSONReport_LargeSession()
    {
        var generator = new JsonReportGenerator();
        return generator.Generate(_largeSession);
    }

    [Benchmark]
    public string Generate_MarkdownReport_LargeSession()
    {
        var generator = new MarkdownReportGenerator();
        return generator.Generate(_largeSession);
    }
}
```

**성능 목표**:
- Small Session HTML: < 50ms
- Large Session HTML: < 2,000ms (2초)
- JSON/Markdown: HTML의 50% 시간

---

#### 5. 전체 워크플로우 부하 테스트

**FullWorkflowLoadTests.cs** (NBomber 사용):

```csharp
[Fact]
public void LoadTest_ConcurrentAnalysis_10Users_ShouldMaintainPerformance()
{
    var scenario = Scenario.Create("qa_analysis", async context =>
    {
        var engine = new DefaultValidationEngine(...);
        var session = engine.StartSession(GetRandomProject());
        await engine.RunValidationAsync(session);
        engine.CompleteSession(session);

        return Response.Ok();
    })
    .WithLoadSimulations(
        Simulation.KeepConstant(copies: 10, during: TimeSpan.FromMinutes(5))
    );

    var stats = NBomberRunner
        .RegisterScenarios(scenario)
        .Run();

    // 성공률 95% 이상
    Assert.True(stats.ScenarioStats[0].Ok.Request.RPS >= 9.5);
}
```

**성능 목표**:
- 동시 사용자 10명: 95% 이상 성공
- 평균 응답 시간: < 5초
- 99th percentile: < 10초

---

#### 6-10. 추가 벤치마크 시나리오

6. **Git 작업 성능** - 파일 diff, 변경 감지
7. **메모리 효율성** - 대용량 파일 스트리밍 vs 전체 로드
8. **캐싱 효율성** - 반복 분석 시 캐시 히트율
9. **직렬화 성능** - JSON vs MessagePack vs Protobuf
10. **정규식 성능** - 주석 추출, 식별자 매칭

### 3.3 성능 회귀 테스트

**성능 게이트 설정** (CI/CD에 통합):

```yaml
# .github/workflows/performance-tests.yml
performance-gates:
  - metric: parser_100_lines
    baseline: 10ms
    threshold: +20%  # 20% 이상 느려지면 실패

  - metric: full_analysis_50_files
    baseline: 5000ms
    threshold: +15%

  - metric: memory_large_file
    baseline: 100MB
    threshold: +30%
```

---

## 🔧 4. CI/CD 파이프라인 설계

### 4.1 GitHub Actions 워크플로우

#### 메인 CI 워크플로우

**`.github/workflows/ci.yml`**:

```yaml
name: CI - Build and Test

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main, develop ]

env:
  DOTNET_VERSION: '8.0.x'
  SOLUTION_FILE: 'TwinCatQA.sln'

jobs:
  build:
    name: Build
    runs-on: windows-latest

    steps:
    - name: Checkout code
      uses: actions/checkout@v4
      with:
        fetch-depth: 0  # Shallow clones should be disabled for better analysis

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}

    - name: Restore dependencies
      run: dotnet restore ${{ env.SOLUTION_FILE }}

    - name: Build solution
      run: dotnet build ${{ env.SOLUTION_FILE }} --configuration Release --no-restore

    - name: Upload build artifacts
      uses: actions/upload-artifact@v4
      with:
        name: build-artifacts
        path: |
          **/bin/Release/**
          !**/*.pdb

  test-unit:
    name: Unit Tests
    needs: build
    runs-on: windows-latest

    steps:
    - name: Checkout code
      uses: actions/checkout@v4

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}

    - name: Restore dependencies
      run: dotnet restore ${{ env.SOLUTION_FILE }}

    - name: Run unit tests
      run: |
        dotnet test `
          --configuration Release `
          --no-build `
          --verbosity normal `
          --logger "trx;LogFileName=test-results.trx" `
          --collect:"XPlat Code Coverage" `
          --settings coverlet.runsettings `
          --filter "FullyQualifiedName!~Integration&FullyQualifiedName!~E2E" `
          -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover

    - name: Publish test results
      uses: dorny/test-reporter@v1
      if: always()
      with:
        name: Unit Test Results
        path: '**/test-results.trx'
        reporter: dotnet-trx

    - name: Upload coverage to Codecov
      uses: codecov/codecov-action@v4
      with:
        files: '**/coverage.opencover.xml'
        flags: unittests
        name: codecov-unit-tests

  test-integration:
    name: Integration Tests
    needs: build
    runs-on: windows-latest

    steps:
    - name: Checkout code
      uses: actions/checkout@v4

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}

    - name: Install Graphviz (for visualization tests)
      run: choco install graphviz -y

    - name: Restore dependencies
      run: dotnet restore ${{ env.SOLUTION_FILE }}

    - name: Run integration tests
      run: |
        dotnet test `
          --configuration Release `
          --no-build `
          --verbosity normal `
          --logger "trx;LogFileName=integration-test-results.trx" `
          --filter "FullyQualifiedName~Integration"

    - name: Publish test results
      uses: dorny/test-reporter@v1
      if: always()
      with:
        name: Integration Test Results
        path: '**/integration-test-results.trx'
        reporter: dotnet-trx

  test-e2e:
    name: E2E Tests
    needs: build
    runs-on: windows-latest

    steps:
    - name: Checkout code
      uses: actions/checkout@v4

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}

    - name: Setup test fixtures
      run: |
        cd tests/TwinCatQA.Integration.Tests/Fixtures
        .\setup-fixtures.ps1

    - name: Run E2E tests
      run: |
        dotnet test `
          --configuration Release `
          --no-build `
          --verbosity normal `
          --logger "trx;LogFileName=e2e-test-results.trx" `
          --filter "FullyQualifiedName~E2E|FullyQualifiedName~Scenario"

    - name: Publish test results
      uses: dorny/test-reporter@v1
      if: always()
      with:
        name: E2E Test Results
        path: '**/e2e-test-results.trx'
        reporter: dotnet-trx

    - name: Upload E2E artifacts
      if: failure()
      uses: actions/upload-artifact@v4
      with:
        name: e2e-failure-logs
        path: |
          **/TestResults/**
          **/logs/**

  code-quality:
    name: Code Quality Analysis
    needs: build
    runs-on: windows-latest

    steps:
    - name: Checkout code
      uses: actions/checkout@v4
      with:
        fetch-depth: 0

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}

    - name: Install SonarScanner
      run: dotnet tool install --global dotnet-sonarscanner

    - name: Begin SonarQube analysis
      run: |
        dotnet sonarscanner begin `
          /k:"TwinCatQA" `
          /d:sonar.host.url="${{ secrets.SONAR_HOST_URL }}" `
          /d:sonar.login="${{ secrets.SONAR_TOKEN }}" `
          /d:sonar.cs.opencover.reportsPaths="**/coverage.opencover.xml"

    - name: Build
      run: dotnet build ${{ env.SOLUTION_FILE }} --configuration Release

    - name: Run tests with coverage
      run: |
        dotnet test `
          --configuration Release `
          --no-build `
          --collect:"XPlat Code Coverage" `
          --settings coverlet.runsettings

    - name: End SonarQube analysis
      run: dotnet sonarscanner end /d:sonar.login="${{ secrets.SONAR_TOKEN }}"

    - name: Quality Gate check
      uses: SonarSource/sonarqube-quality-gate-action@master
      timeout-minutes: 5
      env:
        SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}

  performance-tests:
    name: Performance Tests
    needs: build
    runs-on: windows-latest

    steps:
    - name: Checkout code
      uses: actions/checkout@v4

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}

    - name: Run benchmarks
      run: |
        cd tests/TwinCatQA.Benchmarks
        dotnet run -c Release -- --filter * --exporters json

    - name: Upload benchmark results
      uses: actions/upload-artifact@v4
      with:
        name: benchmark-results
        path: '**/BenchmarkDotNet.Artifacts/**'

    - name: Compare with baseline
      run: |
        # Performance regression check
        # Compare current results with baseline (stored in repo or artifact)
        python scripts/compare-benchmarks.py `
          --current BenchmarkDotNet.Artifacts/results/results.json `
          --baseline benchmarks/baseline.json `
          --threshold 20

  security-scan:
    name: Security Scan
    needs: build
    runs-on: windows-latest

    steps:
    - name: Checkout code
      uses: actions/checkout@v4

    - name: Run Trivy vulnerability scanner
      uses: aquasecurity/trivy-action@master
      with:
        scan-type: 'fs'
        scan-ref: '.'
        format: 'sarif'
        output: 'trivy-results.sarif'

    - name: Upload Trivy results to GitHub Security tab
      uses: github/codeql-action/upload-sarif@v3
      with:
        sarif_file: 'trivy-results.sarif'

    - name: Dependency vulnerability scan
      run: dotnet list package --vulnerable --include-transitive

    - name: OWASP Dependency Check
      uses: dependency-check/Dependency-Check_Action@main
      with:
        project: 'TwinCatQA'
        path: '.'
        format: 'HTML'
        args: >
          --failOnCVSS 7
          --suppression dependency-check-suppressions.xml

  mutation-testing:
    name: Mutation Testing (Stryker)
    needs: test-unit
    runs-on: windows-latest
    if: github.event_name == 'pull_request'

    steps:
    - name: Checkout code
      uses: actions/checkout@v4

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}

    - name: Install Stryker
      run: dotnet tool install --global dotnet-stryker

    - name: Run Stryker mutation tests
      run: |
        cd tests/TwinCatQA.Application.Tests
        dotnet stryker --reporter "html" --reporter "dashboard"

    - name: Upload mutation report
      uses: actions/upload-artifact@v4
      with:
        name: mutation-report
        path: '**/StrykerOutput/**'

  publish-coverage-report:
    name: Publish Coverage Report
    needs: [test-unit, test-integration]
    runs-on: windows-latest

    steps:
    - name: Checkout code
      uses: actions/checkout@v4

    - name: Download coverage artifacts
      uses: actions/download-artifact@v4

    - name: Install ReportGenerator
      run: dotnet tool install --global dotnet-reportgenerator-globaltool

    - name: Generate coverage report
      run: |
        reportgenerator `
          "-reports:**/coverage.opencover.xml" `
          "-targetdir:CoverageReport" `
          "-reporttypes:Html;Badges;Cobertura"

    - name: Publish coverage report
      uses: actions/upload-artifact@v4
      with:
        name: coverage-report
        path: CoverageReport/**

    - name: Comment PR with coverage
      if: github.event_name == 'pull_request'
      uses: romeovs/lcov-reporter-action@v0.3.1
      with:
        lcov-file: CoverageReport/Cobertura.xml
        github-token: ${{ secrets.GITHUB_TOKEN }}

  release:
    name: Release
    needs: [test-unit, test-integration, test-e2e, code-quality, security-scan]
    runs-on: windows-latest
    if: github.ref == 'refs/heads/main' && github.event_name == 'push'

    steps:
    - name: Checkout code
      uses: actions/checkout@v4

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}

    - name: Build Release
      run: dotnet build ${{ env.SOLUTION_FILE }} --configuration Release

    - name: Pack NuGet packages
      run: dotnet pack --configuration Release --output ./nupkgs

    - name: Push to NuGet
      run: dotnet nuget push "./nupkgs/*.nupkg" --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json

    - name: Create GitHub Release
      uses: actions/create-release@v1
      env:
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
      with:
        tag_name: v${{ github.run_number }}
        release_name: Release ${{ github.run_number }}
        draft: false
        prerelease: false
```

---

#### 야간 빌드 워크플로우

**`.github/workflows/nightly.yml`**:

```yaml
name: Nightly Build

on:
  schedule:
    - cron: '0 2 * * *'  # 매일 오전 2시 (UTC)
  workflow_dispatch:

jobs:
  extended-tests:
    name: Extended Test Suite
    runs-on: windows-latest

    steps:
    - name: Checkout code
      uses: actions/checkout@v4

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '8.0.x'

    - name: Run all tests (including slow tests)
      run: |
        dotnet test --configuration Release --verbosity normal

    - name: Run load tests
      run: |
        cd tests/TwinCatQA.LoadTests
        dotnet run -c Release

    - name: Generate nightly report
      run: |
        # Send email or Slack notification with results
        python scripts/generate-nightly-report.py
```

---

### 4.2 커버리지 품질 게이트

#### Codecov 설정

**`codecov.yml`**:

```yaml
coverage:
  status:
    project:
      default:
        target: 90%
        threshold: 2%
        base: auto
    patch:
      default:
        target: 85%
        threshold: 5%

comment:
  layout: "reach,diff,flags,files,footer"
  behavior: default
  require_changes: true

ignore:
  - "**/*.Designer.cs"
  - "**/obj/**"
  - "**/bin/**"
  - "**/*Tests.cs"
```

---

### 4.3 테스트 실행 전략

#### 계층별 테스트 실행

```bash
# 빠른 피드백 (30초 이내)
dotnet test --filter "Category=Fast"

# 유닛 테스트 (2분 이내)
dotnet test --filter "FullyQualifiedName!~Integration"

# 통합 테스트 (5분 이내)
dotnet test --filter "FullyQualifiedName~Integration"

# 전체 테스트 스위트 (10분 이내)
dotnet test
```

---

## 📋 5. 구체적인 테스트 케이스 목록 (최소 20개)

### 핵심 테스트 케이스 (Priority 1)

#### Parser/Grammar 테스트

1. **`Parse_ComplexFunctionBlock_ShouldReturnValidAST`**
   - **목적**: 복잡한 Function Block 파싱 정확성 검증
   - **입력**: 중첩 IF, CASE, FOR 문을 포함한 500라인 FB
   - **예상 결과**: 정확한 AST 노드 구조, 모든 변수 및 문장 파싱 성공
   - **우선순위**: Critical

2. **`Parse_MultiDimensionalArray_ShouldHandleCorrectly`**
   - **목적**: 다차원 배열 선언 및 접근 파싱
   - **입력**: `VAR arr : ARRAY[1..10, 1..5] OF INT; END_VAR`
   - **예상 결과**: ArrayTypeNode with dimensions [10, 5]
   - **우선순위**: High

3. **`Lexer_AllKeywords_ShouldTokenizeCorrectly`**
   - **목적**: IEC 61131-3 모든 키워드 토큰화
   - **입력**: IF, THEN, ELSE, END_IF, CASE, FOR, WHILE, REPEAT, VAR, END_VAR, ...
   - **예상 결과**: 각 키워드가 올바른 TokenType으로 분류
   - **우선순위**: Critical

4. **`Parse_InvalidSyntax_ShouldThrowDescriptiveException`**
   - **목적**: 구문 오류 시 명확한 에러 메시지
   - **입력**: `IF condition THEN ... END_FOR` (잘못된 END)
   - **예상 결과**: SyntaxException with line number and expected token
   - **우선순위**: High

#### 규칙 검증 테스트

5. **`KoreanCommentRule_MixedLanguage_70PercentKorean_ShouldViolate`**
   - **목적**: 한글 비율 임계값 정확성
   - **입력**: 주석 "This is 테스트 코멘트 입니다" (70% 한글)
   - **예상 결과**: Violation (threshold 95%)
   - **우선순위**: High

6. **`CyclomaticComplexity_Nested5Levels_ShouldCalculateCorrectly`**
   - **목적**: 복잡도 계산 정확성
   - **입력**: 5단계 중첩 IF-FOR-WHILE-CASE-IF
   - **예상 결과**: Complexity = 32 (정확한 McCabe 복잡도)
   - **우선순위**: Critical

7. **`NamingConvention_AllVariableTypes_ShouldValidate`**
   - **목적**: 모든 변수 타입 네이밍 검증
   - **입력**: VAR_INPUT, VAR_OUTPUT, VAR, VAR_GLOBAL, VAR_STAT
   - **예상 결과**: 각 타입별 접두사 규칙 적용
   - **우선순위**: High

8. **`ArrayBoundsRule_UncheckedAccess_ShouldDetectCritical`**
   - **목적**: 배열 범위 체크 누락 감지
   - **입력**: `arr[index]` without `IF index >= 1 AND index <= 10`
   - **예상 결과**: Critical Violation with suggested fix
   - **우선순위**: Critical (안전성)

9. **`FloatingPointComparison_DirectEquality_ShouldDetectCritical`**
   - **목적**: 부동소수점 직접 비교 감지
   - **입력**: `IF realValue = 1.0 THEN`
   - **예상 결과**: Critical Violation, suggest `ABS(realValue - 1.0) < 0.0001`
   - **우선순위**: Critical (안전성)

10. **`NullCheckRule_PointerDereference_ShouldDetectMissing`**
    - **목적**: NULL 체크 누락 감지
    - **입력**: `ptr^.field := value;` without `IF ptr <> 0 THEN`
    - **예상 결과**: Critical Violation
    - **우선순위**: Critical (안전성)

#### 통합 워크플로우 테스트

11. **`FullWorkflow_50Files_AllRules_ShouldCompleteWithin5Minutes`**
    - **목적**: 전체 워크플로우 성능
    - **입력**: 50개 파일, 평균 500라인, 15개 규칙
    - **예상 결과**: < 300초 완료, 메모리 < 500MB
    - **우선순위**: High

12. **`IncrementalAnalysis_5ChangedFiles_ShouldBe5xFaster`**
    - **목적**: 증분 분석 효율성
    - **입력**: 100개 파일 중 5개만 변경
    - **예상 결과**: 전체 분석의 20% 이내 시간
    - **우선순위**: Medium

13. **`GitIntegration_PreCommitHook_ShouldBlockBadCode`**
    - **목적**: Git Hook 동작 검증
    - **입력**: 품질 기준 미달 코드 커밋 시도
    - **예상 결과**: Hook이 커밋 차단, exit code 1
    - **우선순위**: High

14. **`ReportGeneration_HTML_ShouldIncludeAllSections`**
    - **목적**: 리포트 완전성
    - **입력**: 분석 완료된 ValidationSession
    - **예상 결과**: HTML with 메타데이터, 요약, 위반 목록, 차트, 추천사항
    - **우선순위**: High

15. **`FolderComparison_TwoVersions_ShouldShowDelta`**
    - **목적**: 버전 비교 기능
    - **입력**: Version 1.0 (100개 위반), Version 2.0 (80개 위반)
    - **예상 결과**: 20개 해결, 0개 새로 추가, 품질 20% 향상
    - **우선순위**: Medium

#### 성능/부하 테스트

16. **`ParserPerformance_10000Lines_ShouldParseWithin1Second`**
    - **목적**: 파서 성능
    - **입력**: 10,000라인 단일 파일
    - **예상 결과**: < 1,000ms, memory < 100MB
    - **우선순위**: High

17. **`ConcurrentAnalysis_10Users_ShouldMaintain95PercentSuccess`**
    - **목적**: 동시성 처리
    - **입력**: 10명 사용자 동시 분석 (각 50파일)
    - **예상 결과**: 95% 이상 성공, 평균 응답 < 5초
    - **우선순위**: Medium

18. **`MemoryEfficiency_LargeAST_ShouldNotCauseOOM`**
    - **목적**: 메모리 관리
    - **입력**: 5,000라인 초대형 함수 블록
    - **예상 결과**: Peak memory < 200MB, no memory leaks
    - **우선순위**: High

#### 에러 핸들링 테스트

19. **`CorruptedFile_ShouldSkipAndContinue`**
    - **목적**: 손상된 파일 처리
    - **입력**: 10개 정상 + 1개 손상된 파일
    - **예상 결과**: 10개 파일 분석 성공, 1개 에러 로깅, 전체 프로세스 계속
    - **우선순위**: High

20. **`UnknownFileEncoding_ShouldAutoDetectOrFallback`**
    - **목적**: 파일 인코딩 처리
    - **입력**: UTF-8, UTF-16, Windows-1252 혼합 파일
    - **예상 결과**: 자동 감지 또는 graceful fallback
    - **우선순위**: Medium

#### 고급 기능 테스트

21. **`Graphviz_CallGraph_ShouldGenerateValidDOT`**
    - **목적**: 시각화 생성
    - **입력**: 20개 함수가 상호 호출하는 프로젝트
    - **예상 결과**: 유효한 DOT 형식, Graphviz 렌더링 가능
    - **우선순위**: Medium

22. **`CustomRuleEngine_UserDefinedRule_ShouldExecute`**
    - **목적**: 사용자 정의 규칙
    - **입력**: YAML 파일로 정의된 커스텀 규칙
    - **예상 결과**: 규칙이 로드되고 실행됨
    - **우선순위**: High

23. **`TrendAnalysis_5Weeks_ShouldShowImprovement`**
    - **목적**: 품질 추세 분석
    - **입력**: 5주치 분석 결과
    - **예상 결과**: 차트 with 품질 점수 상승 추세
    - **우선순위**: Low

24. **`CICD_JUnitXML_ShouldBeValidFormat`**
    - **목적**: CI/CD 통합
    - **입력**: 분석 결과
    - **예상 결과**: 유효한 JUnit XML (Jenkins, Azure DevOps 호환)
    - **우선순위**: High

25. **`MultiLanguage_Korean_ShouldFormatCorrectly`**
    - **목적**: 다국어 지원
    - **입력**: 한글 주석, 변수명, 에러 메시지
    - **예상 결과**: 인코딩 문제 없음, 올바른 렌더링
    - **우선순위**: High

---

## 📊 6. 테스트 메트릭 및 모니터링

### 6.1 추적할 메트릭

#### 커버리지 메트릭
- **라인 커버리지** (Line Coverage): 목표 95%
- **브랜치 커버리지** (Branch Coverage): 목표 85%
- **메서드 커버리지** (Method Coverage): 목표 98%
- **클래스 커버리지** (Class Coverage): 목표 100%

#### 품질 메트릭
- **테스트 성공률**: 목표 99.9%
- **Flaky Test 비율**: 목표 < 0.1%
- **평균 테스트 실행 시간**: 목표 < 5분
- **Mutation Score** (Stryker): 목표 > 80%

#### 성능 메트릭
- **파서 속도**: 1,000 라인/초 이상
- **규칙 실행 속도**: 50ms/파일 이하
- **전체 워크플로우**: 100 파일 < 60초
- **메모리 사용량**: Peak < 500MB

### 6.2 테스트 대시보드

#### Grafana 대시보드 구성

**패널 구성**:
1. **커버리지 트렌드** (Time Series)
   - Line, Branch, Method Coverage over time
2. **테스트 실행 시간** (Gauge)
   - 현재 빌드 시간 vs 목표 (5분)
3. **실패 테스트 Top 10** (Table)
   - 가장 자주 실패하는 테스트
4. **성능 회귀** (Heatmap)
   - 벤치마크 결과 변화
5. **Flaky Tests** (Alert List)
   - 간헐적 실패 테스트 목록

---

## 📚 7. 테스트 유지보수 전략

### 7.1 테스트 코드 품질 가이드

#### AAA 패턴 엄격 준수

```csharp
/// <summary>
/// 복잡도 10 미만이면 위반 없음
/// </summary>
[Fact]
public void Validate_LowComplexity_ShouldReturnNoViolations()
{
    // Arrange (준비)
    var mockParserService = new Mock<IParserService>();
    var ast = CreateSyntaxTreeWithComplexity(5);
    mockParserService.Setup(x => x.Parse(It.IsAny<string>())).Returns(ast);

    var rule = new CyclomaticComplexityRule(mockParserService.Object);
    var codeFile = new CodeFile("test.st", "content", LanguageType.StructuredText);

    // Act (실행)
    var violations = rule.Validate(codeFile);

    // Assert (검증)
    violations.Should().BeEmpty("복잡도가 임계값 미만이므로 위반 없음");
}
```

#### Given-When-Then 네이밍 (선택적)

```csharp
[Theory]
[InlineData(5, 0)]   // 복잡도 5 -> 위반 0개
[InlineData(15, 1)]  // 복잡도 15 -> 위반 1개
[InlineData(25, 1)]  // 복잡도 25 -> 위반 1개 (Critical)
public void GivenComplexity_WhenValidating_ThenReturnsExpectedViolations(
    int complexity, int expectedViolationCount)
{
    // ... 테스트 구현
}
```

### 7.2 테스트 리팩토링 가이드

#### 중복 제거 - Test Fixtures

```csharp
public class ParserTestFixture : IDisposable
{
    public Mock<IParserService> MockParserService { get; }
    public StructuredTextParser Parser { get; }

    public ParserTestFixture()
    {
        MockParserService = new Mock<IParserService>();
        Parser = new StructuredTextParser();
    }

    public SyntaxTree CreateSyntaxTreeWithComplexity(int complexity)
    {
        // Helper method
    }

    public void Dispose()
    {
        // Cleanup
    }
}

public class CyclomaticComplexityRuleTests : IClassFixture<ParserTestFixture>
{
    private readonly ParserTestFixture _fixture;

    public CyclomaticComplexityRuleTests(ParserTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Test1()
    {
        // Use _fixture.MockParserService
    }
}
```

#### Builder 패턴 활용

```csharp
public class CodeFileBuilder
{
    private string _path = "test.st";
    private string _content = "default content";
    private LanguageType _language = LanguageType.StructuredText;

    public CodeFileBuilder WithPath(string path)
    {
        _path = path;
        return this;
    }

    public CodeFileBuilder WithContent(string content)
    {
        _content = content;
        return this;
    }

    public CodeFile Build()
    {
        return new CodeFile(_path, _content, _language);
    }
}

// 사용 예시
var codeFile = new CodeFileBuilder()
    .WithPath("MyFB.st")
    .WithContent("FUNCTION_BLOCK FB_Test ... END_FUNCTION_BLOCK")
    .Build();
```

### 7.3 Flaky Test 방지

#### 시간 의존성 제거

```csharp
// ❌ Bad - 시스템 시간 의존
public void Test_TimeDependent()
{
    var session = new ValidationSession(...);
    Thread.Sleep(1000);
    session.Complete();

    Assert.True(session.Duration.TotalSeconds >= 1); // Flaky!
}

// ✅ Good - 시간 주입
public interface ITimeProvider
{
    DateTime Now { get; }
}

public void Test_TimeInjected()
{
    var mockTime = new Mock<ITimeProvider>();
    mockTime.SetupSequence(x => x.Now)
        .Returns(new DateTime(2025, 1, 1, 10, 0, 0))
        .Returns(new DateTime(2025, 1, 1, 10, 0, 5));

    var session = new ValidationSession(mockTime.Object);
    session.Complete();

    Assert.Equal(5, session.Duration.TotalSeconds); // Stable!
}
```

#### 파일 시스템 격리

```csharp
// ✅ Good - 임시 디렉토리 사용
public class FileSystemTests : IDisposable
{
    private readonly string _tempDir;

    public FileSystemTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public void Test_FileOperations()
    {
        var filePath = Path.Combine(_tempDir, "test.st");
        File.WriteAllText(filePath, "content");

        // Test logic

        // No cleanup needed - Dispose handles it
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }
}
```

---

## 🚀 8. 실행 계획 및 우선순위

### 8.1 단계별 실행 계획 (4주)

#### Week 1: 기초 인프라 구축
- [ ] Coverlet 설정 파일 생성 (coverlet.runsettings)
- [ ] GitHub Actions CI/CD 워크플로우 구축
- [ ] BenchmarkDotNet 프로젝트 생성
- [ ] 테스트 픽스처 디렉토리 구조 생성
- [ ] Baseline 성능 측정 및 문서화

**예상 결과**: CI/CD 파이프라인 작동, 현재 커버리지 정확히 측정

#### Week 2: 파서 및 핵심 계층 테스트 추가 (커버리지 70% → 80%)
- [ ] StructuredTextParserTests (30개 테스트 추가)
- [ ] StructuredTextLexerTests (15개 테스트 추가)
- [ ] AST 노드 테스트 (20개 테스트 추가)
- [ ] 안전 규칙 테스트 확장 (15개 추가)

**예상 결과**: Parser 커버리지 90%+, 전체 커버리지 80%

#### Week 3: 통합 및 E2E 테스트 (커버리지 80% → 90%)
- [ ] LibGit2ServiceTests (15개 테스트)
- [ ] 고급 분석 서비스 테스트 (25개 테스트)
- [ ] E2E 시나리오 (20개 구현)
- [ ] CLI/UI 기본 테스트 (15개)

**예상 결과**: 전체 커버리지 90%, E2E 시나리오 커버

#### Week 4: 성능 테스트 및 최적화
- [ ] BenchmarkDotNet 벤치마크 10개 구현
- [ ] 부하 테스트 (NBomber) 3개 시나리오
- [ ] 성능 회귀 테스트 CI 통합
- [ ] Mutation Testing (Stryker) 설정
- [ ] 최종 문서화 및 가이드 작성

**예상 결과**: 성능 기준선 수립, 커버리지 95%+ 달성

### 8.2 우선순위 매트릭스

| 영역 | 우선순위 | 현재 커버리지 | 목표 커버리지 | 테스트 수 | 예상 시간 |
|------|---------|------------|------------|---------|---------|
| Parser/Lexer | **Critical** | ~50% | 95% | 45개 | 5일 |
| 안전 규칙 | **Critical** | ~80% | 95% | 15개 | 2일 |
| Domain Models | **High** | ~70% | 90% | 20개 | 3일 |
| Application Services | **High** | ~75% | 90% | 25개 | 4일 |
| Git 통합 | **High** | ~40% | 85% | 15개 | 3일 |
| E2E 시나리오 | **High** | ~30% | 80% | 20개 | 4일 |
| CLI/UI | **Medium** | ~60% | 75% | 15개 | 2일 |
| 성능 테스트 | **Medium** | 0% | N/A | 10개 | 3일 |
| **총계** | - | **~70%** | **90%+** | **165개** | **26일** |

---

## 📖 9. 추가 리소스 및 도구

### 9.1 추천 NuGet 패키지

```xml
<!-- 테스트 프레임워크 -->
<PackageReference Include="xUnit" Version="2.6.2" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.5.4" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />

<!-- Assertion 및 Mocking -->
<PackageReference Include="FluentAssertions" Version="6.12.0" />
<PackageReference Include="Moq" Version="4.20.70" />
<PackageReference Include="NSubstitute" Version="5.1.0" /> <!-- 대안 -->

<!-- 커버리지 -->
<PackageReference Include="coverlet.collector" Version="6.0.0" />
<PackageReference Include="coverlet.msbuild" Version="6.0.0" />

<!-- 성능 테스트 -->
<PackageReference Include="BenchmarkDotNet" Version="0.13.12" />
<PackageReference Include="NBomber" Version="5.5.0" />

<!-- Mutation Testing -->
<PackageReference Include="Stryker.NET" Version="4.0.2" />

<!-- Snapshot Testing -->
<PackageReference Include="Verify.Xunit" Version="23.3.0" />

<!-- Fake Data Generation -->
<PackageReference Include="Bogus" Version="35.4.0" />
<PackageReference Include="AutoFixture" Version="4.18.1" />

<!-- Approval Testing -->
<PackageReference Include="ApprovalTests" Version="6.5.0" />
```

### 9.2 외부 도구

#### 커버리지 시각화
- **Codecov** (https://codecov.io) - GitHub 통합 커버리지 리포트
- **Coveralls** (https://coveralls.io) - 대안 커버리지 서비스
- **ReportGenerator** - 로컬 HTML 리포트 생성

#### 코드 품질 분석
- **SonarQube** - 종합 코드 품질 플랫폼
- **NDepend** - .NET 전용 정적 분석
- **Roslyn Analyzers** - 컴파일 타임 분석

#### 성능 모니터링
- **Application Insights** - Azure 기반 APM
- **Grafana + Prometheus** - 오픈소스 모니터링
- **dotTrace** - JetBrains 프로파일러

---

## 🎯 10. 성공 기준 및 완료 조건

### 10.1 정량적 목표

| 지표 | 현재 | 목표 | 달성 조건 |
|------|------|------|----------|
| 라인 커버리지 | 70% | 95% | ✅ 95% 이상 |
| 브랜치 커버리지 | ~60% | 85% | ✅ 85% 이상 |
| Mutation Score | N/A | 80% | ✅ 80% 이상 |
| 테스트 수 | ~70개 | 200개+ | ✅ 200개 이상 |
| E2E 시나리오 | ~5개 | 20개 | ✅ 20개 이상 |
| 성능 벤치마크 | 0개 | 10개 | ✅ 10개 이상 |
| CI/CD 파이프라인 | 부분적 | 완전 자동화 | ✅ 모든 단계 자동화 |
| 테스트 실행 시간 | ~3분 | < 5분 | ✅ 5분 이내 |
| Flaky Test 비율 | N/A | < 0.1% | ✅ 0.1% 미만 |

### 10.2 정성적 목표

- [ ] **개발자 경험**: 테스트 작성이 쉽고 명확함
- [ ] **유지보수성**: 테스트 코드가 프로덕션 코드만큼 깔끔함
- [ ] **신뢰성**: CI/CD가 안정적이고 예측 가능함
- [ ] **가시성**: 커버리지 및 품질 메트릭이 대시보드에 표시됨
- [ ] **문서화**: 모든 테스트 전략이 문서화되어 있음

### 10.3 검증 체크리스트

#### Week 1 완료 체크리스트
- [ ] `dotnet test --collect:"XPlat Code Coverage"` 실행 성공
- [ ] GitHub Actions에서 자동 테스트 실행 확인
- [ ] Codecov에서 커버리지 리포트 확인
- [ ] Baseline 성능 벤치마크 결과 문서화

#### Week 2 완료 체크리스트
- [ ] Parser 테스트 30개 추가 및 통과
- [ ] 전체 커버리지 80% 달성
- [ ] 안전 규칙 Critical 시나리오 모두 커버
- [ ] 테스트 실행 시간 < 3분 유지

#### Week 3 완료 체크리스트
- [ ] E2E 시나리오 20개 구현 및 통과
- [ ] Git 통합 테스트 15개 추가
- [ ] 전체 커버리지 90% 달성
- [ ] 통합 테스트 격리 환경 구축

#### Week 4 완료 체크리스트
- [ ] 성능 벤치마크 10개 실행
- [ ] Mutation Testing 80% 이상 달성
- [ ] 최종 커버리지 95% 달성
- [ ] 모든 문서 업데이트 완료
- [ ] 팀 트레이닝 세션 완료

---

## 📞 11. 연락처 및 지원

### 11.1 프로젝트 담당자

- **QA 엔지니어 리드**: [담당자 이름]
- **CI/CD 담당**: [담당자 이름]
- **성능 테스트 담당**: [담당자 이름]

### 11.2 지원 채널

- **Slack**: #twincat-qa-testing
- **Email**: twincat-qa-team@company.com
- **Wiki**: https://wiki.company.com/TwinCatQA/Testing

---

## 📝 12. 개정 이력

| 버전 | 날짜 | 작성자 | 변경 내용 |
|------|------|--------|----------|
| 1.0 | 2025-11-27 | Quality Engineer | 초기 테스트 강화 전략 수립 |

---

**문서 끝**

이 문서는 TwinCatQA 프로젝트의 테스트 커버리지를 70%에서 90%+ 향상시키기 위한 종합 전략입니다.
4주 실행 계획을 따라 단계적으로 구현하면 목표를 달성할 수 있습니다.
