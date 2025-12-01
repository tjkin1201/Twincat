# TwinCAT 코드 품질 검증 도구 - 설정 가이드

## 목차
1. [개요](#개요)
2. [설정 파일 구조](#설정-파일-구조)
3. [설정 항목 상세](#설정-항목-상세)
4. [사용 예제](#사용-예제)
5. [고급 설정](#고급-설정)

---

## 개요

TwinCAT 코드 품질 검증 도구는 YAML 기반의 설정 파일을 사용합니다. 설정 파일은 프로젝트 루트의 `.twincat-qa/settings.yml` 경로에 위치합니다.

### 주요 특징
- **YAML 형식**: 사람이 읽고 편집하기 쉬움
- **타입 안전성**: 강타입 C# 클래스로 관리
- **기본값 제공**: 설정 파일 없이도 동작 가능
- **부분 설정 지원**: 일부 항목만 설정해도 기본값과 병합

---

## 설정 파일 구조

```yaml
global:           # 전역 설정
  defaultMode: Full
  enableParallelProcessing: true
  maxDegreeOfParallelism: 4

rules:            # 검증 규칙 설정
  enableAllRules: true
  configurations:
    FR-2-KOREAN-COMMENT:
      enabled: true
      severity: High
      parameters:
        requiredKoreanRatio: 0.95

reports:          # 보고서 설정
  generateHtml: true
  generatePdf: false
  outputPath: .twincat-qa/reports

git:              # Git 통합 설정
  enablePreCommitHook: false
  blockOnCriticalViolations: true
```

---

## 설정 항목 상세

### 1. 전역 설정 (global)

| 항목 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `defaultMode` | enum | `Full` | 검증 모드 (Full, Incremental, Quick) |
| `enableParallelProcessing` | bool | `true` | 병렬 처리 활성화 여부 |
| `maxDegreeOfParallelism` | int | `4` | 병렬 처리 최대 스레드 수 |
| `timeoutSeconds` | int | `300` | 검증 타임아웃 (초) |
| `logLevel` | string | `Info` | 로깅 수준 (Debug, Info, Warning, Error) |

**검증 모드 설명:**
- `Full`: 프로젝트 전체 파일 검증
- `Incremental`: 변경된 파일만 검증 (Git 기반)
- `Quick`: 중요 규칙만 빠르게 검증

### 2. 규칙 설정 (rules)

#### 2.1 전역 규칙 설정

| 항목 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `enableAllRules` | bool | `true` | 모든 규칙 활성화 여부 |
| `customRulesPath` | string | `""` | 커스텀 규칙 디렉토리 경로 |

#### 2.2 개별 규칙 설정

각 규칙은 다음과 같은 구조를 가집니다:

```yaml
rules:
  configurations:
    RULE-ID:
      enabled: true              # 규칙 활성화 여부
      severity: Medium           # 위반 심각도
      parameters:                # 규칙별 매개변수
        param1: value1
        param2: value2
      excludePatterns:           # 예외 파일 패턴
        - "**/ThirdParty/**"
```

**심각도 수준:**
- `Info`: 정보성 (권장사항)
- `Low`: 낮음 (사소한 문제)
- `Medium`: 보통 (개선 필요)
- `High`: 높음 (수정 필요)
- `Critical`: 치명적 (즉시 수정 필요)

#### 2.3 기본 제공 규칙

##### FR-2-KOREAN-COMMENT (한글 주석)
```yaml
FR-2-KOREAN-COMMENT:
  enabled: true
  severity: High
  parameters:
    requiredKoreanRatio: 0.95           # 한글 비율 (0.95 = 95%)
    allowEnglishForTechnical: true      # 기술 용어 영어 허용
```

##### FR-7-CYCLOMATIC-COMPLEXITY (순환 복잡도)
```yaml
FR-7-CYCLOMATIC-COMPLEXITY:
  enabled: true
  severity: Medium
  parameters:
    mediumThreshold: 10      # 보통 수준
    highThreshold: 15        # 높음 수준
    criticalThreshold: 20    # 치명적 수준
```

##### FR-8-NAMING-CONVENTION (명명 규칙)
```yaml
FR-8-NAMING-CONVENTION:
  enabled: true
  severity: Medium
  parameters:
    fbPrefixRequired: true       # FB_ 접두사 필수
    varPrefixRequired: true      # 변수 접두사 필수
    strictPascalCase: true       # PascalCase 엄격 적용
```

### 3. 보고서 설정 (reports)

| 항목 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `generateHtml` | bool | `true` | HTML 보고서 생성 여부 |
| `generatePdf` | bool | `false` | PDF 보고서 생성 여부 |
| `generateJson` | bool | `true` | JSON 보고서 생성 여부 (CI/CD용) |
| `outputPath` | string | `.twincat-qa/reports` | 보고서 출력 경로 |
| `includeSourceCode` | bool | `true` | 소스 코드 포함 여부 |
| `fileNameTemplate` | string | `report-{timestamp}` | 파일명 템플릿 |
| `keepReportsCount` | int | `10` | 보관할 보고서 개수 (0: 무제한) |

**파일명 템플릿 변수:**
- `{timestamp}`: 타임스탬프 (20251120-143025)
- `{date}`: 날짜 (20251120)
- `{time}`: 시간 (143025)
- `{project}`: 프로젝트 이름

### 4. Git 통합 설정 (git)

| 항목 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `enablePreCommitHook` | bool | `false` | Pre-commit 훅 활성화 여부 |
| `blockOnCriticalViolations` | bool | `true` | Critical 위반 시 커밋 차단 |
| `blockOnHighViolations` | bool | `false` | High 위반 시 커밋 차단 |
| `incrementalMode` | bool | `true` | 증분 검증 모드 (변경된 파일만) |
| `hooksPath` | string | `""` | Git 훅 설치 경로 (자동 감지) |

---

## 사용 예제

### 1. 기본 사용법

```csharp
using TwinCatQA.Application.Configuration;

// 설정 서비스 생성
var configService = new ConfigurationService();
var projectPath = @"D:\TwinCAT_Projects\MyProject";

// 설정 로드 (파일 없으면 기본값 반환)
var settings = configService.LoadSettings(projectPath);

// 설정 수정
settings.Global.MaxDegreeOfParallelism = 8;
settings.Reports.GeneratePdf = true;

// 설정 저장
configService.SaveSettings(projectPath, settings);
```

### 2. 프로젝트 초기화

```csharp
var configService = new ConfigurationService();
var projectPath = @"D:\TwinCAT_Projects\NewProject";

// 기본 설정 파일 생성
configService.InitializeSettingsFile(projectPath);
// 결과: .twincat-qa/settings.yml 파일 생성
```

### 3. 규칙 설정 변경

```csharp
var settings = configService.LoadSettings(projectPath);

// 메서드 체이닝으로 간편하게 설정
settings
    .SetRuleEnabled("FR-2-KOREAN-COMMENT", true)
    .SetRuleSeverity("FR-2-KOREAN-COMMENT", ViolationSeverity.Critical)
    .SetRuleParameter("FR-2-KOREAN-COMMENT", "requiredKoreanRatio", 0.90);

configService.SaveSettings(projectPath, settings);
```

### 4. 설정 유효성 검사

```csharp
var settings = configService.LoadSettings(projectPath);
var validationResult = settings.Validate();

if (!validationResult.IsValid)
{
    foreach (var error in validationResult.Errors)
    {
        Console.WriteLine($"오류: {error}");
    }
}
```

### 5. 설정 요약 출력

```csharp
var settings = configService.LoadSettings(projectPath);
Console.WriteLine(settings.ToSummaryString());
```

출력 예시:
```
TwinCAT 코드 품질 검증 도구 설정 요약
=====================================

[전역 설정]
- 검증 모드: Full
- 병렬 처리: 활성화 (최대 4 스레드)
- 타임아웃: 300초
- 로그 수준: Info

[규칙 설정]
- 활성화된 규칙 수: 3개
- 전체 규칙 수: 3개

[보고서 설정]
- HTML: 생성
- PDF: 생성 안 함
- JSON: 생성
- 출력 경로: .twincat-qa/reports

[Git 통합]
- Pre-commit 훅: 비활성화
- Critical 위반 차단: 활성화
- 증분 모드: 활성화
```

---

## 고급 설정

### 1. 커스텀 규칙 디렉토리

```yaml
rules:
  customRulesPath: "CustomRules"
```

프로젝트 내 `CustomRules` 디렉토리에 커스텀 규칙을 배치할 수 있습니다.

### 2. 파일 패턴 제외

```yaml
rules:
  configurations:
    FR-2-KOREAN-COMMENT:
      excludePatterns:
        - "**/ThirdParty/**"
        - "**/Generated/**"
        - "**/*.Generated.st"
```

Glob 패턴을 사용하여 특정 파일을 검증에서 제외할 수 있습니다.

### 3. 환경별 설정

개발/운영 환경에 따라 다른 설정을 사용할 수 있습니다:

**개발 환경 (settings.dev.yml):**
```yaml
global:
  logLevel: Debug
reports:
  generatePdf: false
git:
  enablePreCommitHook: false
```

**운영 환경 (settings.prod.yml):**
```yaml
global:
  logLevel: Warning
reports:
  generatePdf: true
git:
  enablePreCommitHook: true
  blockOnHighViolations: true
```

### 4. 성능 튜닝

대규모 프로젝트의 경우:

```yaml
global:
  enableParallelProcessing: true
  maxDegreeOfParallelism: 8      # CPU 코어 수에 맞게 조정
  timeoutSeconds: 600            # 타임아웃 증가
```

소규모 프로젝트의 경우:

```yaml
global:
  enableParallelProcessing: false
  maxDegreeOfParallelism: 1
  timeoutSeconds: 60
```

---

## 문제 해결

### 설정 파일을 찾을 수 없음

설정 파일이 없으면 자동으로 기본 설정을 사용합니다. 명시적으로 생성하려면:

```csharp
configService.InitializeSettingsFile(projectPath);
```

### YAML 파싱 오류

YAML 파일의 들여쓰기가 올바른지 확인하세요. YAML은 공백 2칸 또는 4칸으로 일관되게 들여쓰기해야 합니다.

잘못된 예:
```yaml
global:
defaultMode: Full    # 들여쓰기 없음 (오류!)
```

올바른 예:
```yaml
global:
  defaultMode: Full  # 2칸 들여쓰기
```

### 설정이 적용되지 않음

1. 설정 파일 경로 확인: `.twincat-qa/settings.yml`
2. 파일 권한 확인 (읽기/쓰기 가능)
3. 설정 유효성 검사 실행

---

## 참고 자료

- [YamlDotNet 문서](https://github.com/aaubry/YamlDotNet)
- [YAML 문법 가이드](https://yaml.org/spec/1.2/spec.html)
- [Glob 패턴 가이드](https://en.wikipedia.org/wiki/Glob_(programming))
