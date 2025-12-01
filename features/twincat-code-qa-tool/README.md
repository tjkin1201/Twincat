# TwinCAT Folder Comparison Tool

> ⚠️ **프로젝트 현재 상태**: 이 프로젝트는 개발 초기 단계로, **폴더 비교 기능만 완전히 동작**합니다.
> 코드 품질 분석 기능은 아키텍처와 인터페이스는 완성되었으나 핵심 파서(ANTLR4) 미구현으로 실행되지 않습니다.

TwinCAT 프로젝트 간 변경 사항을 비교하고 추적하는 도구입니다.

---

## 📋 목차

- [프로젝트 개요](#프로젝트-개요)
- [현재 구현 상태](#현재-구현-상태)
- [설치 및 실행](#설치-및-실행)
- [사용 가능한 기능](#사용-가능한-기능)
- [사용 방법](#사용-방법)
- [아키텍처](#아키텍처)
- [향후 계획](#향후-계획)
- [기여하기](#기여하기)

---

## 🎯 프로젝트 개요

### 목적
TwinCAT PLC 프로젝트의 버전 간 차이를 분석하여:
- 변수 변경 사항 추적
- I/O 매핑 변경 감지
- 로직 코드 차이 분석
- 데이터 타입 변경 확인

### 대상 사용자
- TwinCAT PLC 개발자
- 자동화 시스템 엔지니어
- 품질 관리 담당자
- DevOps 엔지니어

---

## 📊 현재 구현 상태

### ✅ 완전히 동작하는 기능 (약 10%)

| 기능 | 상태 | 설명 |
|------|------|------|
| **폴더 비교** | ✅ 완성 | 두 TwinCAT 프로젝트 폴더의 XML 파일 비교 |
| CLI 인터페이스 | ✅ 완성 | 명령줄 도구 (System.CommandLine) |
| WPF UI | ✅ 완성 | 그래픽 사용자 인터페이스 |
| 변수 변경 분석 | ✅ 동작 | 변수 선언 변경 감지 |
| I/O 매핑 비교 | ✅ 동작 | I/O 주소 변경 추적 |
| 로직 Diff | ✅ 동작 | 코드 파일 변경 내역 |
| 데이터 타입 비교 | ✅ 동작 | 구조체/열거형 변경 |

### ⚠️ 구현 중/미완성 기능 (약 90%)

| 기능 | 상태 | 차단 요인 |
|------|------|-----------|
| TwinCAT ST 파서 | ❌ 미완성 | ANTLR4 문법 파일 미작성 |
| Function Block 추출 | ❌ 미동작 | 파서 없음 |
| 변수 추출 | ❌ 미동작 | 파서 없음 |
| 명명 규칙 검증 | ❌ 미동작 | 파싱 데이터 필요 |
| 복잡도 계산 | ❌ 미동작 | AST 분석 필요 |
| 안전성 분석 | ❌ 미동작 | 파싱 데이터 필요 |
| 품질 점수 | ❌ 미동작 | 검증 결과 필요 |
| HTML/PDF 리포트 | ⚠️ 부분 | 템플릿만 존재 |

---

## 🚀 설치 및 실행

### 시스템 요구사항
- **.NET 8.0 SDK** 이상
- **Windows 10/11** (WPF UI 사용 시)
- **Git** (선택 사항)

### 빌드

```bash
# 저장소 클론 (이미 있다면 생략)
git clone <repository-url>
cd features/twincat-code-qa-tool

# 의존성 복원 및 빌드
dotnet restore
dotnet build

# 테스트 실행
dotnet test
```

---

## 💻 사용 가능한 기능

### 1️⃣ CLI (명령줄 인터페이스)

#### 폴더 비교
두 TwinCAT 프로젝트 폴더를 비교하여 변경 사항 분석

```bash
cd src/TwinCatQA.CLI

# 기본 비교
dotnet run -- compare \
  --source "C:\TwinCAT\Project_V1.0" \
  --target "C:\TwinCAT\Project_V2.0"

# 결과를 JSON 파일로 저장
dotnet run -- compare \
  --source "C:\TwinCAT\Project_V1.0" \
  --target "C:\TwinCAT\Project_V2.0" \
  --output comparison_result.json

# 특정 항목만 비교
dotnet run -- compare \
  --source "C:\TwinCAT\Project_V1.0" \
  --target "C:\TwinCAT\Project_V2.0" \
  --variables true \
  --io-mapping true \
  --logic false \
  --data-types false
```

#### 도움말
```bash
# 전체 도움말
dotnet run -- --help

# compare 명령어 도움말
dotnet run -- compare --help
```

### 2️⃣ WPF UI (그래픽 인터페이스)

```bash
cd src/TwinCatQA.UI
dotnet run
```

**사용 단계**:
1. **Source Folder**: 비교 기준 프로젝트 폴더 선택 (Browse 버튼)
2. **Target Folder**: 비교 대상 프로젝트 폴더 선택 (Browse 버튼)
3. **옵션 선택**: Variables, I/O Mapping, Logic, Data Types 체크
4. **Start Compare**: 비교 실행
5. **결과 확인**: 탭별로 변경 사항 확인
   - **Summary**: 전체 통계
   - **Variable Changes**: 변수 변경 내역
   - **I/O Mapping Changes**: I/O 주소 변경
   - **Logic Changes**: 코드 변경 (파일 diff)
   - **Data Type Changes**: 데이터 타입 변경

---

## 📖 사용 방법

### 시나리오 1: 버전 간 변수 변경 추적

**상황**: V1.0에서 V2.0으로 업그레이드 시 어떤 변수가 변경되었는지 확인

```bash
dotnet run -- compare \
  --source "D:\Projects\Machine_V1.0" \
  --target "D:\Projects\Machine_V2.0" \
  --variables true \
  --output variables_diff.json
```

**결과 예시**:
```json
{
  "totalChanges": 15,
  "addedCount": 5,
  "removedCount": 3,
  "modifiedCount": 7,
  "variableChanges": [
    {
      "changeType": "Added",
      "name": "bEmergencyStop",
      "dataType": "BOOL",
      "path": "GVL_Safety"
    },
    {
      "changeType": "Modified",
      "name": "iMotorSpeed",
      "dataType": "INT -> REAL",
      "oldValue": "INT",
      "newValue": "REAL",
      "path": "FB_MotorControl"
    }
  ]
}
```

### 시나리오 2: I/O 매핑 변경 확인

**상황**: 하드웨어 교체 후 I/O 주소가 올바르게 매핑되었는지 검증

```bash
dotnet run -- compare \
  --source "D:\Backup\Project_BeforeHWChange" \
  --target "D:\Current\Project_AfterHWChange" \
  --io-mapping true
```

### 시나리오 3: UI로 빠른 비교

WPF UI를 실행하여 시각적으로 비교:
1. `dotnet run` (TwinCatQA.UI 프로젝트에서)
2. Source/Target 폴더 선택
3. 모든 옵션 체크
4. "Start Compare" 클릭
5. 탭별로 결과 확인

---

## 🏗️ 아키텍처

### Clean Architecture

```
┌─────────────────────────────────────────────────┐
│  Presentation Layer                             │
│  ┌──────────────┐  ┌──────────────┐            │
│  │  CLI         │  │  WPF UI      │            │
│  │  (Console)   │  │  (XAML/MVVM) │            │
│  └──────────────┘  └──────────────┘            │
└─────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────┐
│  Application Layer                              │
│  ┌────────────────────────────────────┐         │
│  │  Services (ValidationEngine, etc)  │         │
│  │  Rules (NamingConvention, etc)     │         │
│  └────────────────────────────────────┘         │
└─────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────┐
│  Domain Layer                                   │
│  ┌────────────────────────────────────┐         │
│  │  Models (CodeFile, Violation, etc) │         │
│  │  Contracts (Interfaces)            │         │
│  └────────────────────────────────────┘         │
└─────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────┐
│  Infrastructure Layer                           │
│  ┌──────────────────────────────────┐           │
│  │  Parsers (ANTLR4 - 미구현)       │           │
│  │  Comparison (Folder, XML) ✅     │           │
│  │  Analysis (Safety, etc - 미구현) │           │
│  │  Git (LibGit2Sharp)              │           │
│  └──────────────────────────────────┘           │
└─────────────────────────────────────────────────┘
```

### 주요 컴포넌트

#### 완성된 컴포넌트
- **FolderComparer**: XML 파일 기반 프로젝트 비교
- **VariableComparer**: 변수 선언 비교
- **IOMappingComparer**: I/O 주소 매핑 비교
- **LogicComparer**: 코드 파일 diff
- **DataTypeComparer**: 데이터 타입 비교

#### 미완성 컴포넌트 (인터페이스만 존재)
- **AntlrParserService**: Structured Text 파서 (TODO)
- **NamingConventionRule**: 명명 규칙 검증
- **CyclomaticComplexityRule**: 복잡도 계산
- **ExtendedSafetyAnalyzer**: 안전성 분석

---

## 🛠️ 향후 계획

### Phase 1: 파서 구현 (2~3개월)
- [ ] ANTLR4 Structured Text 문법 파일 작성
- [ ] Lexer/Parser 생성 및 통합
- [ ] Function Block, Variable 추출 Visitor 구현
- [ ] 단위 테스트 및 통합 테스트

### Phase 2: 검증 규칙 활성화 (1개월)
- [ ] 명명 규칙 검증 활성화
- [ ] 복잡도 계산 구현
- [ ] 주석 비율 검증
- [ ] 품질 점수 계산

### Phase 3: 고급 분석 (1개월)
- [ ] 안전성 분석 (동시성, 상태머신, 센서)
- [ ] 리포트 생성 (HTML/PDF)
- [ ] 대시보드 UI

### Phase 4: 최적화 및 배포 (1개월)
- [ ] 성능 최적화 (캐싱, 병렬 처리)
- [ ] CI/CD 파이프라인
- [ ] 설치 패키지 (MSI, NuGet)

---

## 🤝 기여하기

### 기여 방법
1. Fork 저장소
2. Feature 브랜치 생성 (`git checkout -b feature/AmazingFeature`)
3. 변경 사항 커밋 (`git commit -m '기능: 멋진 기능 추가'`)
4. 브랜치 푸시 (`git push origin feature/AmazingFeature`)
5. Pull Request 생성

### 코딩 스타일
- **언어**: C# 12.0
- **프레임워크**: .NET 8.0/9.0
- **주석**: 한글 (public API 필수)
- **명명 규칙**: PascalCase (클래스/메서드), camelCase (변수)
- **아키텍처**: Clean Architecture 원칙 준수

### 우선순위 높은 기여 항목
1. **ANTLR4 파서 구현** ⭐⭐⭐⭐⭐
2. 로깅 프레임워크 통합 ⭐⭐⭐⭐
3. 성능 최적화 (정규식 컴파일) ⭐⭐⭐
4. 추가 테스트 작성 ⭐⭐

---

## 📄 라이선스

이 프로젝트는 MIT 라이선스로 배포됩니다. 자세한 내용은 [LICENSE](LICENSE) 파일을 참조하세요.

---

## 📞 문의 및 지원

- **Issues**: [GitHub Issues](https://github.com/your-repo/issues)
- **Email**: your-email@example.com
- **문서**: [Wiki](https://github.com/your-repo/wiki)

---

## 🙏 감사의 말

이 프로젝트는 다음 오픈소스 라이브러리를 사용합니다:
- **ANTLR4**: 파서 생성기
- **LibGit2Sharp**: Git 통합
- **RazorLight**: 템플릿 엔진
- **iText7**: PDF 생성
- **System.CommandLine**: CLI 프레임워크

---

**마지막 업데이트**: 2025-11-24
**버전**: 0.1.0 (Alpha)
**상태**: 개발 초기 단계
