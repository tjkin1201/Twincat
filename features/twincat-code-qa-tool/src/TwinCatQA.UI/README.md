# TwinCAT 폴더 비교 도구

TwinCAT 프로젝트 폴더를 비교하여 변경사항을 분석하는 도구입니다.

## 기능

- ?? 윈도우 탐색기 스타일의 폴더 선택 UI
- ?? Source vs Target 폴더 비교
- ?? 변수, I/O 매핑, 로직, 데이터 타입 변경 감지
- ?? 시각적인 결과 표시 (WPF UI)
- ?? CLI 명령어 지원

## 사용 방법

### 1. WPF GUI 실행

```bash
cd src/TwinCatQA.UI
dotnet run
```

1. **Source 폴더 선택**: 비교 기준이 되는 폴더 선택
2. **Target 폴더 선택**: 비교 대상 폴더 선택
3. **비교 옵션 선택**: 
   - 변수 비교
   - I/O 매핑 비교
   - 로직 비교
   - 데이터 타입 비교
4. **비교 시작** 버튼 클릭

### 2. CLI 사용

```bash
# 기본 비교
dotnet run --project src/TwinCatQA.CLI compare -s "C:\Project\Old" -t "C:\Project\New"

# 변수만 비교
dotnet run --project src/TwinCatQA.CLI compare -s "C:\Project\Old" -t "C:\Project\New" --variables true --io-mapping false --logic false --data-types false

# 결과를 파일로 저장
dotnet run --project src/TwinCatQA.CLI compare -s "C:\Project\Old" -t "C:\Project\New" -o result.json
```

### CLI 옵션

| 옵션 | 축약 | 설명 | 기본값 |
|------|------|------|--------|
| `--source` | `-s` | Source 폴더 경로 (필수) | - |
| `--target` | `-t` | Target 폴더 경로 (필수) | - |
| `--variables` | - | 변수 비교 포함 | true |
| `--io-mapping` | - | I/O 매핑 비교 포함 | true |
| `--logic` | - | 로직 비교 포함 | true |
| `--data-types` | - | 데이터 타입 비교 포함 | true |
| `--output` | `-o` | 결과를 JSON 파일로 저장 | - |

## 비교 결과

### 변경 유형

- ? **Added (추가)**: 새로운 항목이 Target에 추가됨
- ? **Removed (제거)**: Source에 있던 항목이 Target에서 제거됨
- ?? **Modified (수정)**: 기존 항목이 변경됨

### 비교 항목

1. **변수 변경**
   - 변수명
   - 데이터 타입
   - 초기값
   - 위치 (파일)

2. **I/O 매핑 변경**
   - 변수명
   - I/O 주소
   - 매핑 타입

3. **로직 변경**
   - 함수/Function Block
   - 코드 내용
   - 제어 구조

4. **데이터 타입 변경**
   - 구조체 정의
   - 열거형
   - 별칭 타입

## 프로젝트 구조

```
src/
├── TwinCatQA.UI/                    # WPF UI 프로젝트
│   ├── Views/
│   │   ├── FolderComparisonWindow.xaml
│   │   └── FolderComparisonWindow.xaml.cs
│   ├── ViewModels/
│   │   └── FolderComparisonViewModel.cs
│   ├── Converters/
│   │   └── BooleanToVisibilityConverter.cs
│   └── TwinCatQA.UI.csproj
├── TwinCatQA.CLI/                   # CLI 프로젝트
│   ├── Commands/
│   │   └── CompareCommand.cs
│   └── TwinCatQA.CLI.csproj
├── TwinCatQA.Infrastructure/         # 비교 로직 구현
│   └── Comparison/
│       └── FolderComparer.cs
└── TwinCatQA.Domain/                 # 도메인 모델
    ├── Models/
    │   └── ComparisonResult.cs
    └── Services/
        └── IComparisonAnalyzer.cs
```

## 빌드

```bash
# 전체 솔루션 빌드
dotnet build

# UI 프로젝트만 빌드
dotnet build src/TwinCatQA.UI

# CLI 프로젝트만 빌드
dotnet build src/TwinCatQA.CLI
```

## 요구사항

- .NET 8.0 SDK 이상
- Windows (WPF UI)
- TwinCAT 프로젝트 파일 (.TcPOU, .TcGVL, .TcDUT, .TcIO)

## 예제

### CLI 사용 예제

```bash
# 두 버전 비교
dotnet run --project src/TwinCatQA.CLI compare \
  -s "C:\TwinCAT\Projects\MyProject_v1.0" \
  -t "C:\TwinCAT\Projects\MyProject_v1.1" \
  -o comparison_result.json

# 변수와 I/O만 비교
dotnet run --project src/TwinCatQA.CLI compare \
  -s "C:\TwinCAT\Projects\Old" \
  -t "C:\TwinCAT\Projects\New" \
  --logic false \
  --data-types false
```

### 출력 예제

```
??????????????????????????????????????????????
?            비교 결과 요약                  ?
??????????????????????????????????????????????

?? 총 변경사항: 15개

  ? 추가됨: 5개
  ? 제거됨: 3개
  ??  수정됨: 7개

─────────────────────────────────────────────
?? 변수 변경 내역
─────────────────────────────────────────────
  ? Added      nNewCounter          (DINT)
  ? Removed    bOldFlag             (BOOL)
  ??  Modified   fTemperature         REAL → LREAL
```

## 라이선스

이 프로젝트는 MIT 라이선스를 따릅니다.
