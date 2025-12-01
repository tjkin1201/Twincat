# TwinCAT QA Visual Studio 옵션 페이지

TwinCAT QA 도구의 Visual Studio 옵션 페이지 구현입니다.

## 개요

Visual Studio의 **Tools > Options > TwinCAT QA** 메뉴에서 접근할 수 있는 설정 페이지를 제공합니다.

## 파일 구조

```
Options/
├── QASettings.cs                    # 설정 데이터 모델
├── SettingsManager.cs               # 설정 저장/로드 관리자
├── GeneralOptionsPage.cs            # 일반 설정 페이지
├── RulesOptionsPage.cs              # 규칙 설정 페이지
├── PathOptionsPage.cs               # 경로 설정 페이지
├── PackageIntegrationExample.cs     # Package 통합 예제
└── README.md                        # 이 파일
```

## 설정 페이지

### 1. 일반 설정 (GeneralOptionsPage)

**경로**: `Tools > Options > TwinCAT QA > 일반`

#### 분석 설정
- **자동 분석 활성화**: 파일 저장 시 자동으로 코드 분석 수행
- **분석 지연 시간**: 파일 변경 후 분석 시작까지의 지연 시간 (100-5000ms)
- **저장 시 분석 실행**: 파일 저장 시 즉시 분석 실행
- **빌드 시 분석 실행**: 프로젝트 빌드 시 전체 분석 실행

#### 출력 설정
- **출력 상세도**: 최소/일반/상세/진단
- **오류 목록에 표시**: Visual Studio 오류 목록에 결과 표시
- **출력 창에 표시**: 출력 창에 결과 표시
- **성공 메시지 표시**: 문제 없을 때도 메시지 표시

#### 고급 설정
- **성능 메트릭 표시**: 분석 시간 및 성능 정보 표시
- **진단 로깅 활성화**: 상세한 진단 로그 기록
- **백그라운드 분석 활성화**: 비동기 백그라운드 분석
- **최대 병렬 작업 수**: 동시 실행 작업 수 (1-8, 0=자동)

### 2. 규칙 설정 (RulesOptionsPage)

**경로**: `Tools > Options > TwinCAT QA > 규칙`

각 규칙별로 활성화/비활성화 및 심각도(정보/경고/오류) 설정 가능

#### 규칙 카테고리

##### 1. 명명 규칙 (Naming Conventions)
- **NC001**: 함수 블록 명명 규칙 (FB_ 접두사)
- **NC002**: 변수 명명 규칙 (camelCase)
- **NC003**: 상수 명명 규칙 (UPPER_CASE)
- **NC004**: 전역 변수 명명 규칙 (g 접두사)

##### 2. 복잡도 (Complexity)
- **CC001**: 사이클로매틱 복잡도 임계값
- **CC002**: 중첩 깊이 임계값
- **CC003**: 함수 길이 임계값

##### 3. 코드 품질 (Code Quality)
- **CQ001**: 주석 누락 확인
- **CQ002**: 매직 넘버 사용 확인
- **CQ003**: 미사용 변수 확인
- **CQ004**: 중복 코드 확인

##### 4. 안전성 (Safety)
- **SF001**: 0으로 나누기 가능성
- **SF002**: 배열 인덱스 범위 초과
- **SF003**: NULL 참조 가능성

##### 5. 성능 (Performance)
- **PF001**: 루프 내 복잡한 연산
- **PF002**: 불필요한 형변환

##### 6. IEC 61131-3 표준 (Standards)
- **ST001**: 데이터 타입 표준 준수
- **ST002**: 제어 구조 표준 준수

### 3. 경로 설정 (PathOptionsPage)

**경로**: `Tools > Options > TwinCAT QA > 경로`

#### 프로젝트 설정
- **프로젝트 루트 경로**: TwinCAT 프로젝트 루트 디렉토리
  - 비어있으면 현재 솔루션 경로 사용

#### 출력 설정
- **출력 폴더**: 분석 결과 및 리포트 저장 폴더
  - 비어있으면 프로젝트 루트의 'QAReports' 폴더 사용
- **리포트 파일명 형식**: 리포트 파일명 형식 ({0}=날짜, {1}=시간, {2}=프로젝트명)
- **자동 폴더 생성**: 출력 폴더가 없을 경우 자동 생성
- **리포트 보관 기간**: 이전 리포트 보관 일수 (0=삭제 안 함)

#### 필터 설정
- **제외할 파일 패턴**: 분석에서 제외할 파일 패턴 (세미콜론 구분)
  - 기본값: `*.bak;*.tmp;*.old`
- **제외할 폴더**: 분석에서 제외할 폴더 (세미콜론 구분)
  - 기본값: `_Boot;_CompileInfo;_Libraries`
- **포함할 파일 확장자**: 분석에 포함할 확장자 (세미콜론 구분)
  - 기본값: `.TcPOU;.TcDUT;.TcGVL`
- **하위 폴더 포함**: 모든 하위 폴더 분석 포함

#### 고급 설정
- **템플릿 폴더**: 리포트 템플릿 파일 저장 폴더
- **캐시 폴더**: 분석 결과 캐시 폴더
- **캐시 활성화**: 성능 향상을 위한 캐시 사용
- **캐시 만료 시간**: 캐시 유효 시간 (분, 0=파일 수정 시까지)

## Package 통합 방법

### 1. Package 클래스에 속성 추가

```csharp
[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[Guid(PackageGuidString)]
[ProvideMenuResource("Menus.ctmenu", 1)]

// TwinCAT QA 옵션 페이지 등록
[ProvideOptionPage(typeof(GeneralOptionsPage), "TwinCAT QA", "일반", 0, 0, true)]
[ProvideOptionPage(typeof(RulesOptionsPage), "TwinCAT QA", "규칙", 0, 0, true)]
[ProvideOptionPage(typeof(PathOptionsPage), "TwinCAT QA", "경로", 0, 0, true)]

public sealed class TwinCatQAPackage : AsyncPackage
{
    public const string PackageGuidString = "your-package-guid-here";
    // ...
}
```

### 2. 설정 가져오기

```csharp
// 방법 1: DialogPage를 통해 직접 가져오기
var generalPage = (GeneralOptionsPage)GetDialogPage(typeof(GeneralOptionsPage));
bool autoAnalysis = generalPage.EnableAutoAnalysis;

// 방법 2: SettingsManager를 통해 가져오기 (권장)
var settingsManager = SettingsManager.GetInstance(this);
var settings = settingsManager.LoadSettings();
bool autoAnalysis = settings.EnableAutoAnalysis;
```

### 3. 설정 변경 이벤트 구독

```csharp
protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
{
    await base.InitializeAsync(cancellationToken, progress);
    await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

    // 설정 변경 이벤트 구독
    var settingsManager = SettingsManager.GetInstance(this);
    settingsManager.SettingsChanged += OnSettingsChanged;
}

private void OnSettingsChanged(object sender, SettingsChangedEventArgs e)
{
    ThreadHelper.ThrowIfNotOnUIThread();

    // 설정 변경 처리
    var settings = e.Settings;
    // ...
}
```

## 설정 저장 위치

설정은 Visual Studio의 사용자 설정 저장소에 JSON 형식으로 저장됩니다:

```
%LOCALAPPDATA%\Microsoft\VisualStudio\{Version}\Settings\
```

## 설정 내보내기/가져오기

```csharp
// 설정 내보내기
var settingsManager = SettingsManager.GetInstance(ServiceProvider.GlobalProvider);
var settings = settingsManager.LoadSettings();
settingsManager.ExportSettings("settings.json", settings);

// 설정 가져오기
var importedSettings = settingsManager.ImportSettings("settings.json");
```

## 규칙 동적 관리

```csharp
var settingsManager = SettingsManager.GetInstance(ServiceProvider.GlobalProvider);

// 특정 규칙 활성화/비활성화
settingsManager.SetRuleEnabled("NC001", true);

// 특정 규칙 심각도 변경
settingsManager.SetRuleSeverity("NC001", RuleSeverity.Error);

// 모든 규칙 활성화
settingsManager.EnableAllRules();

// 모든 규칙 비활성화
settingsManager.DisableAllRules();
```

## 사용 예제

전체 사용 예제는 `PackageIntegrationExample.cs` 파일을 참조하세요.

## 주의사항

1. **UI 스레드**: 설정 로드/저장 시 반드시 UI 스레드에서 실행해야 합니다.
   ```csharp
   ThreadHelper.ThrowIfNotOnUIThread();
   ```

2. **경로 유효성**: 경로 설정 시 유효성 검사가 자동으로 수행됩니다.

3. **설정 변경 알림**: 설정 변경 시 이벤트가 발생하므로 필요한 곳에서 구독하여 사용합니다.

4. **기본값**: 설정이 없을 경우 자동으로 기본값이 사용됩니다.

## 문제 해결

### 설정이 저장되지 않음
- Visual Studio를 관리자 권한으로 실행했는지 확인
- 설정 저장소 경로에 쓰기 권한이 있는지 확인

### 옵션 페이지가 나타나지 않음
- Package 클래스에 ProvideOptionPage 속성이 올바르게 추가되었는지 확인
- VSIX 매니페스트에 Package가 등록되었는지 확인
- Visual Studio Experimental Instance를 재시작

### 설정 변경이 반영되지 않음
- 설정 변경 이벤트가 올바르게 구독되었는지 확인
- 캐시된 설정을 사용하고 있지 않은지 확인

## 라이선스

이 프로젝트는 TwinCAT QA 도구의 일부입니다.
