# TwinCAT QA 도구 - 예외 규칙 시스템 가이드

## 개요

TwinCAT QA 도구는 `.twincat-qa.json` 설정 파일을 통해 유연한 규칙 관리와 예외 처리를 지원합니다.

## 설정 파일 위치

프로젝트 루트 디렉토리에 `.twincat-qa.json` 파일을 생성합니다.

```
YourProject/
├── .twincat-qa.json          ← 설정 파일
├── POUs/
│   ├── MAIN.TcPOU
│   └── ...
└── DUTs/
    └── ...
```

## 설정 파일 구조

### 1. 기본 정보

```json
{
  "version": "2.0",
  "projectName": "My TwinCAT Project"
}
```

- `version`: 설정 파일 버전 (현재 "2.0")
- `projectName`: 프로젝트 이름 (옵션)

### 2. 전역 제외 규칙

특정 파일이나 규칙을 전역적으로 제외합니다.

```json
{
  "globalExclusions": {
    "files": [
      "**/Generated/**",
      "**/Test/**",
      "**/*.g.st"
    ],
    "directories": [
      "**/bin",
      "**/obj"
    ],
    "rules": [
      "SA0029"
    ]
  }
}
```

- `files`: 제외할 파일 패턴 (Glob 패턴 지원)
- `directories`: 제외할 디렉토리 패턴
- `rules`: 전역적으로 비활성화할 규칙 ID 목록

### 3. 규칙별 오버라이드

특정 규칙의 동작을 커스터마이징합니다.

```json
{
  "ruleOverrides": {
    "SA0049": {
      "enabled": true,
      "severity": "Warning",
      "parameters": {
        "maxMagicNumberValue": 100,
        "allowedValues": [0, 1, -1, 100, 1000]
      },
      "filePatterns": ["POUs/**/*.st"],
      "excludePatterns": ["POUs/Test/**"]
    }
  }
}
```

- `enabled`: 규칙 활성화 여부
- `severity`: 심각도 수준 (`Info`, `Warning`, `Error`, `Critical`)
- `parameters`: 규칙별 커스텀 파라미터
- `filePatterns`: 이 규칙이 적용될 파일 패턴
- `excludePatterns`: 이 규칙에서 제외할 파일 패턴

### 4. 파일별 오버라이드

특정 파일 패턴에 대한 규칙 적용을 커스터마이징합니다.

```json
{
  "fileOverrides": {
    "POUs/Safety/**": {
      "minSeverity": "Critical",
      "strictMode": true,
      "enabledRules": ["SA0001", "SA0002", "SA0033"],
      "disabledRules": [],
      "parameters": {
        "requireDocumentation": true
      }
    }
  }
}
```

- `minSeverity`: 최소 심각도 수준 (이보다 낮은 이슈는 무시)
- `strictMode`: 엄격 모드 활성화
- `enabledRules`: 활성화할 규칙 목록 (지정 시 이 규칙들만 적용)
- `disabledRules`: 비활성화할 규칙 목록
- `parameters`: 파일별 커스텀 파라미터

### 5. 인라인 억제 설정

코드 내 주석으로 규칙을 억제하는 기능을 설정합니다.

```json
{
  "inlineSuppressions": {
    "enabled": true,
    "commentPatterns": [
      "// qa-ignore: {ruleId}",
      "(* qa-ignore: {ruleId} *)"
    ],
    "blockStartPatterns": [
      "// qa-ignore-start: {ruleId}",
      "(* qa-ignore-start: {ruleId} *)"
    ],
    "blockEndPatterns": [
      "// qa-ignore-end",
      "(* qa-ignore-end *)"
    ],
    "fileSuppressionPatterns": [
      "// qa-ignore-file: {ruleId}",
      "(* qa-ignore-file: {ruleId} *)"
    ],
    "warnOnUnusedSuppressions": false
  }
}
```

## 인라인 억제 사용법

### 단일 줄 억제

```iecst
PROGRAM MAIN
VAR
    // qa-ignore: SA0049
    timeout : TIME := T#5S;  // 매직 넘버 경고 억제
END_VAR
```

또는

```iecst
PROGRAM MAIN
VAR
    (* qa-ignore: SA0049 *)
    timeout : TIME := T#5S;
END_VAR
```

### 블록 억제

```iecst
PROGRAM MAIN
VAR
    // qa-ignore-start: SA0049
    timeout1 : TIME := T#5S;
    timeout2 : TIME := T#10S;
    timeout3 : TIME := T#15S;
    // qa-ignore-end
END_VAR
```

### 파일 전체 억제

파일 상단에 작성:

```iecst
// qa-ignore-file: SA0049

PROGRAM MAIN
VAR
    // 이 파일의 모든 SA0049 경고가 억제됨
    timeout : TIME := T#5S;
END_VAR
```

## Glob 패턴 예시

```json
{
  "files": [
    "**/*.st",              // 모든 .st 파일
    "POUs/**",              // POUs 디렉토리의 모든 파일
    "POUs/*.TcPOU",         // POUs 디렉토리의 TcPOU 파일만
    "**/Safety/**/*.st",    // Safety 디렉토리의 모든 .st 파일
    "!**/Test/**"           // Test 디렉토리 제외
  ]
}
```

## 사용 시나리오

### 시나리오 1: 안전 관련 코드에 엄격한 규칙 적용

```json
{
  "fileOverrides": {
    "POUs/Safety/**": {
      "minSeverity": "Critical",
      "strictMode": true,
      "enabledRules": ["SA0001", "SA0002", "SA0033", "SA0049"]
    }
  }
}
```

### 시나리오 2: 레거시 코드에 관대한 규칙 적용

```json
{
  "fileOverrides": {
    "POUs/Legacy/**": {
      "minSeverity": "Error",
      "disabledRules": ["SA0049", "SA0052", "SA0033"]
    }
  }
}
```

### 시나리오 3: 특정 규칙의 파라미터 조정

```json
{
  "ruleOverrides": {
    "SA0033": {
      "enabled": true,
      "severity": "Warning",
      "parameters": {
        "maxComplexity": 20,
        "maxNestingDepth": 5
      }
    }
  }
}
```

### 시나리오 4: 생성된 코드 제외

```json
{
  "globalExclusions": {
    "files": [
      "**/Generated/**",
      "**/*.g.st",
      "**/*.generated.st"
    ]
  }
}
```

## 사용 예제 (C#)

### 1. 설정 로드 및 사용

```csharp
using TwinCatQA.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;

// 로거 생성
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var configLogger = loggerFactory.CreateLogger<QAConfigurationLoader>();
var filterLogger = loggerFactory.CreateLogger<RuleFilter>();
var suppressionLogger = loggerFactory.CreateLogger<SuppressionChecker>();

// 설정 로더 생성
var loader = new QAConfigurationLoader(configLogger);

// 설정 로드
var projectPath = @"D:\TwinCAT_Projects\MyProject";
var config = await loader.LoadConfigurationAsync(projectPath);

// 억제 체커 생성
var suppressionChecker = new SuppressionChecker(
    suppressionLogger,
    config.InlineSuppressions);

// 규칙 필터 생성
var ruleFilter = new RuleFilter(filterLogger, config, suppressionChecker);
```

### 2. 파일 제외 확인

```csharp
var filePath = @"POUs\Generated\Main.st";

if (ruleFilter.IsFileGloballyExcluded(filePath))
{
    Console.WriteLine($"파일 {filePath}는 전역 제외 대상입니다.");
}
```

### 3. 규칙 활성화 확인

```csharp
var ruleId = "SA0049";
var filePath = @"POUs\Safety\SafetyLogic.st";

if (ruleFilter.IsRuleEnabledForFile(ruleId, filePath))
{
    Console.WriteLine($"규칙 {ruleId}가 파일 {filePath}에 대해 활성화되어 있습니다.");
}
```

### 4. 이슈 필터링

```csharp
var issues = new List<QAIssue>
{
    new QAIssue
    {
        RuleId = "SA0049",
        Severity = IssueSeverity.Warning,
        Message = "매직 넘버 사용",
        FilePath = @"POUs\Main.st",
        Line = 10,
        Column = 5
    }
};

// 파일 내용 로드 (인라인 억제 확인용)
var fileContents = new Dictionary<string, string>
{
    [@"POUs\Main.st"] = File.ReadAllText(@"D:\TwinCAT_Projects\MyProject\POUs\Main.st")
};

// 이슈 필터링
var filteredIssues = ruleFilter.FilterIssues(issues, fileContents);

Console.WriteLine($"필터링 후 이슈 수: {filteredIssues.Count}");
```

### 5. 인라인 억제 확인

```csharp
var fileContent = @"
PROGRAM MAIN
VAR
    // qa-ignore: SA0049
    timeout : TIME := T#5S;
END_VAR
";

var ruleId = "SA0049";
var lineNumber = 4;

if (suppressionChecker.IsSuppressed(ruleId, lineNumber, fileContent))
{
    Console.WriteLine($"규칙 {ruleId}가 줄 {lineNumber}에서 억제되었습니다.");
}
```

### 6. 기본 설정 파일 생성

```csharp
var projectPath = @"D:\TwinCAT_Projects\NewProject";
var projectName = "NewProject";

await loader.CreateDefaultConfigFileAsync(projectPath, projectName);

Console.WriteLine($"설정 파일이 생성되었습니다: {projectPath}\\.twincat-qa.json");
```

## 심각도 수준

1. **Info**: 정보성 메시지
2. **Warning**: 경고 (권장 사항)
3. **Error**: 오류 (수정 필요)
4. **Critical**: 치명적 (반드시 수정)

## 주의사항

1. **JSON 형식**: 설정 파일은 유효한 JSON 형식이어야 합니다
2. **주석 지원**: JSON 주석(`//`, `/* */`)은 파싱 시 무시됩니다
3. **대소문자**: 규칙 ID는 대소문자를 구분하지 않습니다
4. **패턴 우선순위**: 더 구체적인 패턴이 우선 적용됩니다

## 예제 파일

전체 예제는 `docs/.twincat-qa.example.json` 파일을 참고하세요.

## 아키텍처

### 구성 요소

1. **QAConfiguration.cs**: 설정 파일 모델
   - 전역 제외, 규칙 오버라이드, 파일 오버라이드, 인라인 억제 설정 정의

2. **QAConfigurationLoader.cs**: 설정 파일 로더
   - `.twincat-qa.json` 파일 찾기 및 로드
   - 기본 설정과 병합
   - 설정 유효성 검증
   - 캐싱 지원

3. **SuppressionChecker.cs**: 인라인 억제 처리
   - 주석 패턴 인식 (`// qa-ignore: SA0049`)
   - 블록 억제 처리 (`qa-ignore-start` ~ `qa-ignore-end`)
   - 파일 수준 억제 처리
   - 정규식 기반 패턴 매칭

4. **RuleFilter.cs**: 규칙 필터링
   - 전역 제외 규칙 적용
   - 파일별 오버라이드 적용
   - 규칙별 오버라이드 적용
   - Glob 패턴 매칭
   - 이슈 심각도 조정

### 처리 흐름

```
1. QAConfigurationLoader
   ↓
   설정 파일 로드 (.twincat-qa.json)
   ↓
2. SuppressionChecker
   ↓
   인라인 억제 패턴 컴파일
   ↓
3. RuleFilter
   ↓
   Glob 패턴 컴파일
   ↓
4. 이슈 필터링
   ↓
   - 파일 전역 제외 확인
   - 규칙 활성화 확인
   - 인라인 억제 확인
   - 심각도 기반 필터링
   - 심각도 조정
   ↓
5. 필터링된 이슈 반환
```

## 성능 최적화

1. **패턴 컴파일 캐싱**: Glob 패턴과 정규식을 미리 컴파일하여 재사용
2. **설정 캐싱**: 동일한 경로의 설정 파일은 캐시에서 반환
3. **지연 평가**: 필요할 때만 파일 내용을 로드하여 메모리 절약

## 문제 해결

### 설정 파일을 찾을 수 없음

설정 파일이 없으면 자동으로 기본 설정을 사용합니다. 명시적으로 생성하려면:

```csharp
await loader.CreateDefaultConfigFileAsync(projectPath);
```

### JSON 파싱 오류

JSON 파일의 구문이 올바른지 확인하세요. 온라인 JSON 검증 도구를 사용할 수 있습니다.

### Glob 패턴이 작동하지 않음

- 경로 구분자는 `/` 사용 (Windows에서도)
- `**`는 모든 하위 디렉토리를 의미
- `*`는 단일 디렉토리 또는 파일명을 의미
