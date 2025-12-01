# TwinCAT 코드 품질 검증 도구 - 단위 테스트

## 개요

이 디렉토리는 TwinCAT 코드 품질 검증 도구의 포괄적인 단위 테스트를 포함합니다.
모든 테스트는 **AAA 패턴**(Arrange-Act-Assert), **Moq**을 사용한 Mocking, **FluentAssertions**를 사용한 검증으로 작성되었습니다.

## 테스트 프로젝트 구조

```
tests/
├── TwinCatQA.Domain.Tests/          # 도메인 모델 테스트
│   └── Models/
│       └── ValidationSessionTests.cs
├── TwinCatQA.Application.Tests/     # 애플리케이션 계층 테스트
│   ├── Rules/
│   │   ├── KoreanCommentRuleTests.cs
│   │   ├── CyclomaticComplexityRuleTests.cs
│   │   └── NamingConventionRuleTests.cs
│   ├── Configuration/
│   │   └── ConfigurationServiceTests.cs
│   └── Services/
│       ├── ChartDataBuilderTests.cs (기존)
│       └── RazorReportGeneratorTests.cs (기존)
└── TwinCatQA.Infrastructure.Tests/  # 인프라 계층 테스트
    └── Git/
        └── (향후 LibGit2ServiceTests.cs 추가 예정)
```

## 생성된 테스트 스위트

### 1. ValidationSessionTests.cs (11개 테스트)

**파일 경로**: `tests/TwinCatQA.Domain.Tests/Models/ValidationSessionTests.cs`

**테스트 케이스**:
- `Complete_ShouldSetEndTimeAndDuration()` - 세션 완료 시 EndTime과 Duration 설정 검증
- `Duration_WithoutComplete_ShouldCalculateFromCurrentTime()` - Complete 호출 없이 Duration 계산 검증
- `CalculateQualityScore_WithViolations_ShouldReturnCorrectScore()` - 위반별 품질 점수 계산 검증
- `CalculateQualityScore_WithNoFiles_ShouldReturnZero()` - 파일 없을 때 점수 0 검증
- `CalculateQualityScore_WithNoViolations_ShouldReturn100()` - 위반 없을 때 만점 검증
- `CalculateQualityScore_WithManyViolations_ShouldNotGoBelowZero()` - 점수 음수 방지 검증
- `CalculateConstitutionCompliance_ShouldReturnCorrectRatio()` - 헌장 준수율 계산 검증
- `CalculateConstitutionCompliance_WithNoViolations_ShouldReturnAllOnes()` - 위반 없을 때 100% 준수율 검증
- `CalculateConstitutionCompliance_ShouldBeBetweenZeroAndOne()` - 준수율 범위 검증 (0~1)
- `ViolationsBySeverity_ShouldGroupBySeverityCorrectly()` - 심각도별 위반 집계 검증
- `Constructor_ShouldInitializeWithDefaultValues()` - 생성자 기본값 초기화 검증

### 2. KoreanCommentRuleTests.cs (14개 테스트)

**파일 경로**: `tests/TwinCatQA.Application.Tests/Rules/KoreanCommentRuleTests.cs`

**테스트 케이스**:
- `Rule_ShouldHaveCorrectProperties()` - 규칙 속성 검증
- `Validate_KoreanComment_ShouldReturnNoViolations()` - 한글 주석 통과 검증
- `Validate_EnglishComment_ShouldReturnViolation()` - 영어 주석 위반 검증
- `Validate_MixedCommentWithHighKoreanRatio_ShouldPass()` - 한글 비율 95% 이상 통과 검증
- `Validate_ShortComment_ShouldBeIgnored()` - 짧은 주석 제외 검증
- `Validate_NullAst_ShouldReturnNoViolations()` - AST null 처리 검증
- `Validate_InvalidSyntaxTree_ShouldReturnNoViolations()` - 유효하지 않은 구문 트리 처리 검증
- `Configure_ShouldUpdateKoreanRatioThreshold()` - 한글 비율 임계값 설정 변경 검증
- `Configure_ShouldUpdateMinCommentLength()` - 최소 주석 길이 설정 변경 검증
- `Configure_InvalidKoreanRatio_ShouldThrowException()` - 잘못된 비율 값 예외 검증
- `Configure_InvalidMinCommentLength_ShouldThrowException()` - 잘못된 길이 값 예외 검증
- `Validate_NullCodeFile_ShouldThrowArgumentNullException()` - null CodeFile 예외 검증
- `Constructor_NullParserService_ShouldThrowArgumentNullException()` - null ParserService 예외 검증
- `Validate_ExtractCommentsThrows_ShouldReturnNoViolations()` - ExtractComments 예외 처리 검증

### 3. CyclomaticComplexityRuleTests.cs (15개 테스트)

**파일 경로**: `tests/TwinCatQA.Application.Tests/Rules/CyclomaticComplexityRuleTests.cs`

**테스트 케이스**:
- `Rule_ShouldHaveCorrectProperties()` - 규칙 속성 검증
- `Validate_LowComplexity_ShouldReturnNoViolations()` - 복잡도 10 미만 통과 검증
- `Validate_HighComplexity_ShouldReturnViolation()` - 복잡도 15 이상 High 위반 검증
- `Validate_CriticalComplexity_ShouldReturnCriticalViolation()` - 복잡도 20 이상 Critical 위반 검증
- `Validate_MediumComplexity_ShouldReturnMediumViolation()` - 복잡도 10~14 Medium 위반 검증
- `Validate_MultipleFunctionBlocks_ShouldValidateEach()` - 여러 Function Block 각각 검증
- `Validate_NonSTLanguage_ShouldReturnNoViolations()` - ST 언어가 아니면 제외 검증
- `Configure_ShouldUpdateMediumThreshold()` - medium_threshold 설정 변경 검증
- `Configure_ShouldUpdateAllThresholds()` - 모든 임계값 설정 변경 검증
- `Configure_InvalidThreshold_ShouldThrowException()` - 잘못된 임계값 예외 검증
- `Validate_NullCodeFile_ShouldThrowArgumentNullException()` - null CodeFile 예외 검증
- `Constructor_NullParserService_ShouldThrowArgumentNullException()` - null ParserService 예외 검증
- `Validate_NullAst_ShouldReturnNoViolations()` - AST null 처리 검증
- `Validate_ExtractFunctionBlocksThrows_ShouldReturnNoViolations()` - ExtractFunctionBlocks 예외 처리 검증
- `Validate_CalculateComplexityThrows_ShouldSkipFunctionBlock()` - CalculateComplexity 예외 처리 검증

### 4. NamingConventionRuleTests.cs (17개 테스트)

**파일 경로**: `tests/TwinCatQA.Application.Tests/Rules/NamingConventionRuleTests.cs`

**테스트 케이스**:
- `Rule_ShouldHaveCorrectProperties()` - 규칙 속성 검증
- `Validate_ValidFBName_ShouldReturnNoViolations()` - FB_ 접두사 통과 검증
- `Validate_InvalidFBName_ShouldReturnViolation()` - 접두사 없음 위반 검증
- `Validate_ValidFCPrefix_ShouldPass()` - FC_ 접두사 통과 검증
- `Validate_ValidPRGPrefix_ShouldPass()` - PRG_ 접두사 통과 검증
- `Validate_InputVariableWithCorrectPrefix_ShouldPass()` - 입력 변수 i 접두사 통과 검증
- `Validate_InputVariableWithInPrefix_ShouldPass()` - 입력 변수 in 접두사 통과 검증
- `Validate_InputVariableWithWrongPrefix_ShouldReturnViolation()` - 잘못된 입력 변수 접두사 위반 검증
- `Validate_OutputVariableWithCorrectPrefix_ShouldPass()` - 출력 변수 o 접두사 통과 검증
- `Validate_GlobalVariableWithCorrectPrefix_ShouldPass()` - 전역 변수 g 접두사 통과 검증
- `Validate_LocalVariableWithCamelCase_ShouldPass()` - 로컬 변수 카멜케이스 통과 검증
- `Configure_DisableFBPrefixRequired_ShouldNotCheckPrefix()` - FB 접두사 검증 비활성화 검증
- `Configure_DisableVarPrefixRequired_ShouldNotCheckVariablePrefix()` - 변수 접두사 검증 비활성화 검증
- `Validate_NullCodeFile_ShouldThrowArgumentNullException()` - null CodeFile 예외 검증
- `Constructor_NullParserService_ShouldThrowArgumentNullException()` - null ParserService 예외 검증
- `Validate_NullAst_ShouldReturnNoViolations()` - AST null 처리 검증
- `Validate_ExtractFunctionBlocksThrows_ShouldReturnNoViolations()` - ExtractFunctionBlocks 예외 처리 검증

### 5. ConfigurationServiceTests.cs (12개 테스트)

**파일 경로**: `tests/TwinCatQA.Application.Tests/Configuration/ConfigurationServiceTests.cs`

**주의**: ConfigurationService 실제 구현 완료 후 주석 해제하여 활성화 필요

**테스트 케이스**:
- `LoadSettings_FileNotExists_ShouldReturnDefaultSettings()` - 파일 없으면 기본값 반환 검증
- `LoadSettings_ValidYamlFile_ShouldParseCorrectly()` - 유효한 YAML 파일 파싱 검증
- `LoadSettings_InvalidYaml_ShouldThrowException()` - 잘못된 YAML 예외 검증
- `SaveSettings_ShouldCreateYamlFile()` - YAML 파일 생성 검증
- `SaveSettings_ExistingFile_ShouldOverwrite()` - 기존 파일 덮어쓰기 검증
- `MergeWithDefaults_ShouldCombineSettings()` - 부분 설정 병합 검증
- `MergeWithDefaults_UserSettingsShouldOverrideDefaults()` - 사용자 설정 우선순위 검증
- `GetRuleConfiguration_ValidRuleId_ShouldReturnConfig()` - 규칙 설정 조회 검증
- `GetRuleConfiguration_InvalidRuleId_ShouldReturnNull()` - 존재하지 않는 규칙 null 반환 검증
- `ValidateSettings_ValidSettings_ShouldReturnTrue()` - 유효한 설정 검증 통과
- `ValidateSettings_InvalidSettings_ShouldReturnFalse()` - 잘못된 설정 검증 실패
- `LoadSettings_WithEnvironmentVariables_ShouldSubstitute()` - 환경 변수 대체 검증

## 총 테스트 개수

| 테스트 스위트 | 테스트 개수 | 상태 |
|-------------|-----------|------|
| ValidationSessionTests | 11 | ✅ 완료 |
| KoreanCommentRuleTests | 14 | ✅ 완료 |
| CyclomaticComplexityRuleTests | 15 | ✅ 완료 |
| NamingConventionRuleTests | 17 | ✅ 완료 |
| ConfigurationServiceTests | 12 | ⚠️ 구현 대기 중 |
| **합계** | **69** | - |

## 테스트 실행 방법

### 전체 테스트 실행

```bash
# 프로젝트 루트에서
dotnet test

# 또는 특정 테스트 프로젝트만 실행
dotnet test tests/TwinCatQA.Domain.Tests/TwinCatQA.Domain.Tests.csproj
dotnet test tests/TwinCatQA.Application.Tests/TwinCatQA.Application.Tests.csproj
```

### 특정 테스트 클래스 실행

```bash
# ValidationSessionTests만 실행
dotnet test --filter "FullyQualifiedName~ValidationSessionTests"

# KoreanCommentRuleTests만 실행
dotnet test --filter "FullyQualifiedName~KoreanCommentRuleTests"
```

### 커버리지 리포트 생성

```bash
# Coverlet을 사용한 코드 커버리지
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## 테스트 패턴 및 모범 사례

### AAA 패턴 (Arrange-Act-Assert)

모든 테스트는 명확한 3단계 구조를 따릅니다:

```csharp
[Fact]
public void TestMethod()
{
    // Arrange (준비) - 테스트 데이터 및 Mock 설정
    var mockService = new Mock<IService>();
    var sut = new SystemUnderTest(mockService.Object);

    // Act (실행) - 테스트 대상 메서드 호출
    var result = sut.DoSomething();

    // Assert (검증) - FluentAssertions로 결과 검증
    result.Should().NotBeNull();
}
```

### Moq을 사용한 Mocking

의존성 주입된 서비스는 Moq으로 모킹합니다:

```csharp
var mockParserService = new Mock<IParserService>();
mockParserService
    .Setup(x => x.ExtractComments(It.IsAny<SyntaxTree>()))
    .Returns(new Dictionary<int, string>());
```

### FluentAssertions로 가독성 높은 검증

```csharp
violations.Should().HaveCount(2, "2개의 위반이 예상됨");
violation.Severity.Should().Be(ViolationSeverity.High);
result.Should().BeGreaterThan(0, "결과는 양수여야 함");
```

### 한글 주석

모든 테스트 메서드에는 한글 XML 문서화 주석이 포함되어 있습니다:

```csharp
/// <summary>
/// 복잡도 10 미만이면 위반 없음
/// </summary>
[Fact]
public void Validate_LowComplexity_ShouldReturnNoViolations()
{
    // ...
}
```

## 향후 작업

### 추가 예정 테스트

1. **DefaultValidationEngineTests.cs** - 검증 엔진 통합 테스트
   - `StartSession_WithValidPath_ShouldCreateSession()`
   - `ScanFiles_ShouldFindAllTcPOUFiles()`
   - `RunValidation_ShouldExecuteActiveRulesOnly()`
   - `CompleteSession_ShouldSaveToJson()`

2. **LibGit2ServiceTests.cs** - Git 통합 테스트
   - `IsGitRepository_WithGitRepo_ShouldReturnTrue()`
   - `GetChangedFiles_ShouldReturnModifiedFiles()`
   - `InstallPreCommitHook_ShouldCreateHookFile()`

3. **통합 테스트** - 전체 워크플로우 End-to-End 테스트

### CI/CD 통합

- GitHub Actions에서 자동 테스트 실행
- PR 시 테스트 커버리지 체크
- 코드 품질 게이트 설정

## 사용된 NuGet 패키지

- **xUnit** 2.6.2 - 테스트 프레임워크
- **Moq** 4.20.70 - Mocking 라이브러리
- **FluentAssertions** 6.12.0 - 검증 라이브러리
- **Microsoft.NET.Test.Sdk** 17.8.0 - 테스트 SDK
- **coverlet.collector** 6.0.0 - 코드 커버리지

## 라이선스

이 프로젝트는 MIT 라이선스를 따릅니다.
