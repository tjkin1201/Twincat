# TwinCAT QA Visual Studio 명령어 가이드

## 개요

TwinCAT QA Visual Studio Extension은 다양한 메뉴와 명령어를 통해 TwinCAT 코드 품질 분석 기능을 제공합니다.

## 생성된 파일

### 1. Commands/AnalyzeCommand.cs
현재 파일 및 전체 프로젝트 분석 명령어를 구현합니다.

**주요 기능:**
- **현재 파일 분석**: 활성 편집기에서 열려 있는 TwinCAT 파일 분석
- **전체 프로젝트 분석**: 프로젝트의 모든 TwinCAT 파일을 일괄 분석
- 분석 결과를 출력 창에 표시
- 복잡도, 유지보수성 지수, 코드 이슈 등을 보고

**명령어 ID:**
- `AnalyzeCurrentFileCommandId` (0x0100): 현재 파일 분석
- `AnalyzeProjectCommandId` (0x0101): 전체 프로젝트 분석

### 2. Commands/ShowToolWindowCommand.cs
TwinCAT QA 도구 창을 표시하는 명령어입니다.

**주요 기능:**
- 분석 결과를 시각적으로 표시하는 도구 창 열기
- 오류 처리 및 사용자 알림

**명령어 ID:**
- `ShowToolWindowCommandId` (0x0102): QA 결과 창 표시

### 3. Commands/SettingsCommand.cs
TwinCAT QA 설정 페이지를 여는 명령어입니다.

**주요 기능:**
- Visual Studio 도구 > 옵션 메뉴에서 TwinCAT QA 설정 페이지 열기
- 분석 규칙 및 임계값 조정

**명령어 ID:**
- `SettingsCommandId` (0x0103): 설정 열기

### 4. TwinCatQAPackage.vsct
Visual Studio Command Table 파일로 메뉴 구조와 명령어를 정의합니다.

## 메뉴 구조

### Tools 메뉴
```
Tools
└── TwinCAT QA
    ├── 현재 파일 분석 (Ctrl+Shift+Q)
    ├── 전체 프로젝트 분석 (Ctrl+Shift+Alt+Q)
    ├── QA 결과 창 표시
    └── 설정...
```

### Solution Explorer 컨텍스트 메뉴

**프로젝트 우클릭:**
```
프로젝트 우클릭
├── ...
└── TwinCAT QA 분석
```

**파일 우클릭:**
```
파일 우클릭
├── ...
└── TwinCAT QA 분석
```

## 키보드 단축키

| 단축키 | 명령어 | 설명 |
|--------|--------|------|
| `Ctrl+Shift+Q` | 현재 파일 분석 | 활성 파일을 분석합니다 |
| `Ctrl+Shift+Alt+Q` | 전체 프로젝트 분석 | 프로젝트 전체를 분석합니다 |

## 사용 방법

### 1. 현재 파일 분석

**방법 1: 키보드 단축키**
1. TwinCAT 파일 열기
2. `Ctrl+Shift+Q` 입력

**방법 2: 메뉴**
1. Tools > TwinCAT QA > 현재 파일 분석 선택

**방법 3: 컨텍스트 메뉴**
1. Solution Explorer에서 파일 우클릭
2. TwinCAT QA 분석 선택

### 2. 전체 프로젝트 분석

**방법 1: 키보드 단축키**
1. `Ctrl+Shift+Alt+Q` 입력

**방법 2: 메뉴**
1. Tools > TwinCAT QA > 전체 프로젝트 분석 선택

**방법 3: 컨텍스트 메뉴**
1. Solution Explorer에서 프로젝트 우클릭
2. TwinCAT QA 분석 선택

### 3. 결과 확인

분석 결과는 다음 위치에 표시됩니다:
- **출력 창**: View > Output (TwinCAT QA 채널 선택)
- **QA 결과 창**: Tools > TwinCAT QA > QA 결과 창 표시

### 4. 설정 변경

1. Tools > TwinCAT QA > 설정... 선택
2. 또는 Tools > Options > TwinCAT QA
3. 분석 규칙 및 임계값 조정

## 아이콘 리소스

### Commands.png 생성 가이드

VSCT 파일은 `Resources\Commands.png` 파일을 참조합니다. 이 파일은 4개의 16x16 아이콘을 가로로 배열한 64x16 크기의 PNG 이미지여야 합니다.

**아이콘 순서 (왼쪽에서 오른쪽):**
1. **bmpAnalyze**: 현재 파일 분석 아이콘
2. **bmpAnalyzeProject**: 전체 프로젝트 분석 아이콘
3. **bmpToolWindow**: 도구 창 표시 아이콘
4. **bmpSettings**: 설정 아이콘

**생성 방법:**
```bash
# Resources 폴더로 이동
cd "D:\01. Vscode\Twincat\features\twincat-code-qa-tool\src\TwinCatQA.VsExtension\Resources"

# 64x16 크기의 PNG 파일 생성 (이미지 편집 도구 사용)
# 또는 Visual Studio Image Library에서 적절한 아이콘 추출
```

**권장 아이콘:**
- 분석: 돋보기, 체크마크
- 프로젝트 분석: 폴더 + 돋보기
- 도구 창: 창 아이콘
- 설정: 톱니바퀴

### 임시 해결 방법

아이콘 파일이 없을 경우, VSCT 파일의 아이콘 참조를 제거하거나 Visual Studio의 기본 KnownMonikers를 사용할 수 있습니다.

```xml
<!-- 아이콘 대신 KnownMonikers 사용 예시 -->
<Button ...>
  <Icon guid="ImageCatalogGuid" id="StatusInformation"/>
  <CommandFlag>IconIsMoniker</CommandFlag>
  ...
</Button>
```

## GUID 정보

### 패키지 GUID
```
3f2a8e5c-9d7b-4a1f-8e3c-5b6a9d2e4f7c
```

### 명령어 세트 GUID
```
a1b2c3d4-e5f6-4a5b-9c8d-7e6f5a4b3c2d
```

### 아이콘 GUID
```
d9f0a8e5-4b2c-4d3a-9e7f-6c5b8a9d2e4f
```

## 명령어 ID 요약

| ID | 상수 이름 | 명령어 |
|----|----------|--------|
| 0x0100 | AnalyzeCurrentFileCommandId | 현재 파일 분석 |
| 0x0101 | AnalyzeProjectCommandId | 전체 프로젝트 분석 |
| 0x0102 | ShowToolWindowCommandId | QA 결과 창 표시 |
| 0x0103 | SettingsCommandId | 설정 열기 |
| 0x0104 | ContextAnalyzeProjectCommandId | 컨텍스트: 프로젝트 분석 |
| 0x0105 | ContextAnalyzeFileCommandId | 컨텍스트: 파일 분석 |

## 그룹 ID

| ID | 이름 | 부모 |
|----|------|------|
| 0x1000 | TwinCatQAMenuGroup | Tools 메뉴 |
| 0x1001 | TwinCatQASubMenuGroup | TwinCAT QA 서브메뉴 |
| 0x1002 | TwinCatQAContextMenuGroup | Solution Explorer 프로젝트 컨텍스트 메뉴 |
| 0x1003 | TwinCatQAContextMenuFileGroup | Solution Explorer 파일 컨텍스트 메뉴 |

## 메뉴 ID

| ID | 이름 | 설명 |
|----|------|------|
| 0x1100 | TwinCatQASubMenu | TwinCAT QA 서브메뉴 |

## 트러블슈팅

### 명령어가 메뉴에 표시되지 않음

1. **VSIX 재빌드**: 프로젝트를 Clean 후 Rebuild
2. **실험적 인스턴스 재설정**:
   ```bash
   devenv.exe /resetSettings
   devenv.exe /updateConfiguration
   ```
3. **VSCT 파일 확인**: 컴파일 오류가 없는지 확인

### 아이콘이 표시되지 않음

1. `Resources\Commands.png` 파일이 존재하는지 확인
2. 파일 속성에서 "Copy to Output Directory"가 "Always"로 설정되었는지 확인
3. 또는 KnownMonikers를 사용하도록 VSCT 수정

### 명령어가 실행되지 않음

1. 출력 창에서 오류 메시지 확인
2. TwinCatQAPackage.cs의 InitializeCommandsAsync() 메서드가 호출되는지 확인
3. 명령어 초기화 로깅 확인

## 다음 단계

1. **아이콘 리소스 생성**: Commands.png 파일 작성
2. **테스트**: 각 명령어 동작 확인
3. **문서화**: 사용자 가이드 업데이트
4. **배포**: VSIX 패키지 생성 및 배포

## 참고 자료

- [Visual Studio SDK 문서](https://docs.microsoft.com/visualstudio/extensibility/)
- [VSCT Schema 레퍼런스](https://docs.microsoft.com/visualstudio/extensibility/vsct-xml-schema-reference)
- [VS 명령어 개발 가이드](https://docs.microsoft.com/visualstudio/extensibility/adding-commands-to-toolbars)
