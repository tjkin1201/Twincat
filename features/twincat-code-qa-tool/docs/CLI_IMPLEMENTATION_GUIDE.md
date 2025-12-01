# TwinCAT Code QA Tool - CLI 구현 가이드

## 📋 개요

**구현 날짜**: 2025-11-21
**상태**: 프로토타입 구조 완성, API 버전 조정 필요
**진행률**: 80%

## 🎯 구현된 기능

### 1. 프로젝트 구조

```
src/TwinCatQA.CLI/
├── Commands/
│   ├── AnalyzeCommand.cs      # 고급 분석 명령어
│   ├── GraphCommand.cs         # 의존성 그래프 생성
│   └── QualityCommand.cs       # 품질 점수 계산
├── Services/
│   └── ServiceCollectionExtensions.cs  # DI 설정
├── Utils/
│   └── FileScanner.cs          # 파일 스캔 유틸리티
├── Program.cs                  # 메인 진입점
└── TwinCatQA.CLI.csproj        # 프로젝트 파일
```

### 2. 의존성

- **System.CommandLine 2.0** (prerelease) - 현대적인 CLI 프레임워크
- **Microsoft.Extensions.DependencyInjection 10.0** - 의존성 주입
- **Microsoft.Extensions.Logging.Console 10.0** - 콘솔 로깅
- 기존 TwinCatQA 프로젝트 참조 (Domain, Application, Infrastructure)

### 3. 명령어 설계

#### `analyze` 명령어
**목적**: TwinCAT 프로젝트 고급 분석 실행

**사용법**:
```bash
twincat-qa analyze "C:\MyProject" --parallel --output result.json
```

**옵션**:
- `project-path`: (필수) 프로젝트 경로
- `--parallel, -p`: 병렬 실행 활성화 (기본값: true)
- `--output, -o`: 결과 파일 경로 (JSON)

**출력**:
- 프로젝트 정보
- 소요 시간
- 품질 점수 (0-100)
- 분석 성공 여부
- 총 이슈 수
- 컴파일/변수/의존성 상세 정보

#### `graph` 명령어
**목적**: 의존성 그래프 생성 (DOT/SVG)

**사용법**:
```bash
twincat-qa graph "C:\MyProject" --output dependency.svg --title "My Project"
```

**옵션**:
- `project-path`: (필수) 프로젝트 경로
- `--output, -o`: 출력 파일 (.svg 또는 .dot)
- `--title, -t`: 그래프 제목

**동작**:
1. 파일 스캔 및 파싱
2. 의존성 분석
3. DOT 형식 생성
4. Graphviz 설치 확인
5. SVG 변환 (가능한 경우) 또는 DOT만 저장

#### `quality` 명령어
**목적**: 빠른 품질 점수 계산 (CI/CD 품질 게이트)

**사용법**:
```bash
twincat-qa quality "C:\MyProject" --threshold 80.0
```

**옵션**:
- `project-path`: (필수) 프로젝트 경로
- `--threshold, -t`: 최소 품질 점수 (기본값: 80.0)

**출력**:
- 품질 점수
- 임계값 비교
- 통과/실패 여부
- 등급 (S/A/B/C/D/F)
- 종료 코드 (통과: 0, 실패: 1)

### 4. 공통 유틸리티

#### `FileScanner`
**파일**: `Utils/FileScanner.cs`

**기능**:
- TwinCAT 파일 재귀 검색 (.TcPOU, .TcDUT, .TcGVL)
- 파일 파싱 및 ValidationSession 생성
- 예외 처리 및 로깅

**사용 예시**:
```csharp
var session = FileScanner.CreateValidationSession(projectPath, parser);
Console.WriteLine($"파싱 완료: {session.SyntaxTrees.Count}개 파일");
```

### 5. 의존성 주입 설정

**파일**: `Services/ServiceCollectionExtensions.cs`

**등록된 서비스**:
```csharp
// 파싱
services.AddSingleton<IParserService, AntlrParserService>();

// 컴파일
services.AddSingleton<ICompilationService, TwinCatCompilationService>();

// 분석
services.AddSingleton<IVariableUsageAnalyzer, VariableUsageAnalyzer>();
services.AddSingleton<IDependencyAnalyzer, DependencyAnalyzer>();
services.AddSingleton<IIOMappingValidator, IOMappingValidator>();

// 오케스트레이터
services.AddSingleton<IAdvancedAnalysisOrchestrator, AdvancedAnalysisOrchestrator>();

// 시각화
services.AddSingleton<GraphvizVisualizationService>();
```

## 🔧 현재 문제 및 해결 방법

### 문제 1: System.CommandLine 2.0 API 변경

**증상**:
- `AddArgument()`, `AddOption()`, `SetHandler()` 메서드가 존재하지 않음
- `Argument<T>` 생성자 시그니처 변경
- `Option<T>` 생성자 시그니처 변경

**원인**:
System.CommandLine 2.0 (prerelease)의 API가 기존 예제와 다름

**해결 방법**:

#### 옵션 A: API 버전 맞추기 (권장)
System.CommandLine 2.0.0-beta4 사용:
```csharp
// 올바른 API 사용
public class AnalyzeCommand : Command
{
    public AnalyzeCommand() : base("analyze", "분석 실행")
    {
        var projectArg = new Argument<string>("project-path");
        projectArg.Description = "프로젝트 경로";

        var parallelOpt = new Option<bool>("--parallel");
        parallelOpt.SetDefaultValue(true);
        parallelOpt.Description = "병렬 실행";

        Add(projectArg);        // AddArgument 대신 Add 사용
        Add(parallelOpt);       // AddOption 대신 Add 사용

        // SetHandler 대신 Handler 속성 사용
        this.Handler = CommandHandler.Create<string, bool>(Execute);
    }

    private static void Execute(string projectPath, bool parallel)
    {
        // 실행 로직
    }
}
```

#### 옵션 B: 안정 버전으로 다운그레이드
```bash
dotnet remove package System.CommandLine
dotnet add package System.CommandLine --version 2.0.0-beta1.20574.7
```

### 문제 2: 비동기 핸들러 지원

**해결**:
```csharp
this.Handler = CommandHandler.Create<string, bool, IConsole, CancellationToken>(
    async (projectPath, parallel, console, cancellationToken) =>
    {
        // 비동기 로직
        await orchestrator.AnalyzeProjectAsync(projectPath, ...);
    });
```

## 📚 사용 예시

### CI/CD 통합

#### Azure DevOps
```yaml
- task: PowerShell@2
  displayName: 'TwinCAT 품질 검사'
  inputs:
    targetType: 'inline'
    script: |
      dotnet tool install --global TwinCatQA.CLI
      twincat-qa quality "$(Build.SourcesDirectory)" --threshold 85.0
```

#### GitHub Actions
```yaml
- name: TwinCAT Quality Check
  run: |
    dotnet tool install --global TwinCatQA.CLI
    twincat-qa quality ./TwinCatProject --threshold 85.0
```

### 로컬 사용

```bash
# 1. 전체 분석
twincat-qa analyze "C:\MyProject" --parallel --output analysis.json

# 2. 그래프 생성
twincat-qa graph "C:\MyProject" --output dependency.svg

# 3. 품질 점수만 빠르게 확인
twincat-qa quality "C:\MyProject" --threshold 80

# 4. 도움말
twincat-qa --help
twincat-qa analyze --help
```

## 🚀 다음 단계

### 우선순위: 높음
1. **System.CommandLine API 수정**: 버전 2.0 올바른 API 사용
2. **빌드 성공 확인**: 모든 컴파일 오류 해결
3. **기본 테스트 실행**: 3개 명령어 동작 확인

### 우선순위: 중간
4. **dotnet tool 패키징**: global tool로 설치 가능하도록
   ```xml
   <PackAsTool>true</PackAsTool>
   <ToolCommandName>twincat-qa</ToolCommandName>
   ```
5. **추가 옵션 구현**:
   - `--verbose`: 상세 로그 출력
   - `--config`: 설정 파일 지정
   - `--format`: 출력 형식 (json/xml/text)

### 우선순위: 낮음
6. **고급 기능**:
   - 진행 표시줄 (Spectre.Console 사용)
   - 색상 출력
   - 인터랙티브 모드
   - 리포트 템플릿 커스터마이징

## 💡 설계 결정사항

### 1. 파일 스캔 전략
**결정**: 직접 파일 스캔 구현 (FileScanner 유틸리티)
**이유**: IFileScanner 서비스가 존재하지 않음, 간단한 로직으로 충분

### 2. 명령어 구조
**결정**: 각 명령어를 독립적인 클래스로 분리
**이유**: SRP 원칙, 유지보수성, 확장성

### 3. 의존성 주입
**결정**: Microsoft.Extensions.DependencyInjection 사용
**이유**: .NET 표준, 기존 서비스와 통합 용이

### 4. 로깅
**결정**: Console 로깅 + ILogger<T>
**이유**: CLI 환경에 적합, 디버깅 용이

### 5. 오류 처리
**결정**: 예외 발생 시 사용자 친화적 메시지 + 종료 코드
**이유**: CI/CD 통합, 자동화 스크립트 지원

## 📊 통계

| 항목 | 수치 |
|------|------|
| 생성된 파일 | 7개 |
| 코드 라인 | ~500줄 |
| 명령어 수 | 3개 |
| 설치된 패키지 | 3개 |
| 구현 시간 | ~2시간 |
| 현재 진행률 | 80% |

## 🔗 관련 문서

- [System.CommandLine 문서](https://github.com/dotnet/command-line-api)
- [TwinCAT QA Tool 아키텍처](../README.md)
- [고급 분석 가이드](./NEXT_PHASE_IMPLEMENTATION_SUMMARY.md)
- [버그 수정 이력](./BUGFIX_ISSUCESS_LOGIC.md)

## ✅ 체크리스트

- [x] CLI 프로젝트 생성
- [x] 패키지 설치
- [x] 명령어 구조 설계
- [x] 파일 스캔 유틸리티
- [x] 의존성 주입 설정
- [ ] API 버전 수정
- [ ] 빌드 성공
- [ ] 기본 테스트
- [ ] dotnet tool 패키징
- [ ] 문서화

---

**최종 업데이트**: 2025-11-21
**작성자**: Claude Code
**상태**: 프로토타입 완성, API 수정 필요
