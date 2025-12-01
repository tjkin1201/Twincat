# TwinCatQA 아키텍처 개선 워크플로우

## 프로젝트 개요

**프로젝트**: TwinCAT Code QA Tool
**현재 위치**: `D:\01. Vscode\Twincat\features\twincat-code-qa-tool`
**아키텍처**: Clean Architecture (Domain-Driven Design)
**목표**: 의존성 역전 원칙 준수 및 미구현 기능 완료

---

## 현재 아키텍처 분석

### 프로젝트 구조
```
TwinCatQA/
├── Domain/              # 핵심 비즈니스 로직 (의존성 없음)
├── Application/         # 유스케이스 및 오케스트레이션
│   └── ❌ Infrastructure 직접 참조 (문제!)
├── Infrastructure/      # 외부 시스템 구현 (ANTLR4, Git, File I/O)
├── Grammar/            # ANTLR4 생성 파일
├── CLI/                # 진입점 (DI 컨테이너)
└── UI/                 # 사용자 인터페이스 (향후)
```

### 의존성 다이어그램 (현재)

```
┌─────────────────────────────────────────────────────────────┐
│                          CLI Layer                          │
│  (Program.cs, ServiceCollectionExtensions.cs)              │
│                                                             │
│  • DI 컨테이너 초기화                                        │
│  • 모든 구현체 등록                                          │
└─────────────────┬───────────────────────┬───────────────────┘
                  │                       │
                  ▼                       ▼
┌─────────────────────────────┐  ┌─────────────────────────────┐
│    Application Layer        │  │   Infrastructure Layer      │
│                             │  │                             │
│  • QaAnalysisService        │◄─┤  • AntlrParserService       │
│  • QaReportGenerator        │  │  • VariableUsageAnalyzer    │
│  • AdvancedAnalysisOrc...   │  │  • DependencyAnalyzer       │
│                             │  │  • TwinCatCompilationSvc    │
│  ❌ Infrastructure 참조!     │  │                             │
│  (TwinCatQA.Infrastructure  │  └─────────────┬───────────────┘
│   .csproj에 명시)            │                │
└──────────────┬──────────────┘                │
               │                                │
               ▼                                ▼
┌─────────────────────────────────────────────────────────────┐
│                        Domain Layer                         │
│                                                             │
│  • IParserService (인터페이스)                               │
│  • ICompilationService (인터페이스)                          │
│  • IVariableUsageAnalyzer (인터페이스)                       │
│  • FunctionBlock, Variable, SyntaxTree (모델)               │
│                                                             │
│  ✅ 의존성 없음 (순수 비즈니스 로직)                          │
└─────────────────────────────────────────────────────────────┘
```

---

## 개선 항목 1: Application → Infrastructure 직접 의존성 제거

### 문제점

**현재 상태**:
- `TwinCatQA.Application.csproj` 파일의 26번째 줄:
  ```xml
  <ProjectReference Include="..\TwinCatQA.Infrastructure\TwinCatQA.Infrastructure.csproj" />
  ```
- Application 레이어가 Infrastructure 레이어를 직접 참조
- Clean Architecture의 의존성 역전 원칙(Dependency Inversion Principle) 위반

**영향**:
- Application 레이어가 Infrastructure의 구현 세부사항에 결합됨
- 테스트 시 Mock 객체 주입이 어려워짐
- 향후 Infrastructure 구현 교체 시 Application 코드 수정 필요

### 목표 아키텍처

```
┌─────────────────────────────────────────────────────────────┐
│                          CLI Layer                          │
│                                                             │
│  • DI 컨테이너에서 인터페이스와 구현체 바인딩                  │
└──────────────┬──────────────────────────┬───────────────────┘
               │                          │
               ▼                          ▼
┌──────────────────────────┐  ┌──────────────────────────────┐
│   Application Layer      │  │   Infrastructure Layer       │
│                          │  │                              │
│  ✅ Domain 인터페이스만   │  │  • IParserService 구현        │
│     참조                 │  │  • ICompilationService 구현   │
│                          │  │  • IAnalyzer 구현             │
└────────────┬─────────────┘  └────────────┬─────────────────┘
             │                              │
             │                              │
             └──────────────┬───────────────┘
                            ▼
             ┌──────────────────────────────┐
             │       Domain Layer           │
             │                              │
             │  • 인터페이스 정의            │
             │  • 도메인 모델                │
             └──────────────────────────────┘
```

### 마이그레이션 단계

#### 1단계: Domain 인터페이스 검증 및 보완 (1시간)

**작업 내용**:
```bash
# 1. Domain 레이어에 필요한 모든 인터페이스가 정의되어 있는지 확인
src/TwinCatQA.Domain/Contracts/
├── IParserService.cs               ✅ 존재
├── ICompilationService.cs          ✅ 존재
├── IVariableUsageAnalyzer.cs       ✅ 존재
├── IDependencyAnalyzer.cs          ✅ 존재
├── IIOMappingValidator.cs          ✅ 존재
└── IAdvancedAnalysisOrchestrator.cs ✅ 존재

# 2. 누락된 인터페이스 추가 (필요 시)
```

**검증 방법**:
```bash
# Application에서 사용 중인 Infrastructure 타입 검색
cd "D:\01. Vscode\Twincat\features\twincat-code-qa-tool"
grep -r "using TwinCatQA.Infrastructure" src/TwinCatQA.Application/
```

**예상 결과**: Application에서 Infrastructure 네임스페이스를 직접 사용하는 경우 발견 시 Domain 인터페이스로 대체

#### 2단계: Application.csproj 수정 (30분)

**변경 전** (`TwinCatQA.Application.csproj`):
```xml
<ItemGroup>
  <!-- Domain 레이어 참조 -->
  <ProjectReference Include="..\TwinCatQA.Domain\TwinCatQA.Domain.csproj" />

  <!-- Infrastructure 레이어 참조 ❌ -->
  <ProjectReference Include="..\TwinCatQA.Infrastructure\TwinCatQA.Infrastructure.csproj" />
</ItemGroup>
```

**변경 후**:
```xml
<ItemGroup>
  <!-- Domain 레이어만 참조 ✅ -->
  <ProjectReference Include="..\TwinCatQA.Domain\TwinCatQA.Domain.csproj" />
</ItemGroup>
```

**실행 명령**:
```bash
# 파일 백업
cd "D:\01. Vscode\Twincat\features\twincat-code-qa-tool"
cp src/TwinCatQA.Application/TwinCatQA.Application.csproj src/TwinCatQA.Application/TwinCatQA.Application.csproj.bak

# 수동 편집 또는 Edit 도구 사용
```

#### 3단계: Application 코드 리팩토링 (2시간)

**작업 내용**:
1. Application 레이어의 모든 `.cs` 파일에서 Infrastructure 네임스페이스 사용 제거
2. Infrastructure 타입을 Domain 인터페이스로 교체

**예시 변경**:

**변경 전**:
```csharp
using TwinCatQA.Infrastructure.Parsers;        // ❌ Infrastructure 직접 참조

public class QaAnalysisService
{
    private readonly AntlrParserService _parser;  // ❌ 구체적인 구현체

    public QaAnalysisService(AntlrParserService parser)
    {
        _parser = parser;
    }
}
```

**변경 후**:
```csharp
using TwinCatQA.Domain.Contracts;              // ✅ Domain 인터페이스만 참조

public class QaAnalysisService
{
    private readonly IParserService _parser;    // ✅ 인터페이스 의존성

    public QaAnalysisService(IParserService parser)
    {
        _parser = parser;
    }
}
```

**검색 및 수정 명령**:
```bash
# Infrastructure 직접 사용 검색
grep -rn "using TwinCatQA.Infrastructure" src/TwinCatQA.Application/

# 구체적인 타입 사용 검색 (예: AntlrParserService)
grep -rn "AntlrParserService\|VariableUsageAnalyzer\|DependencyAnalyzer" src/TwinCatQA.Application/
```

#### 4단계: 빌드 및 컴파일 오류 수정 (1시간)

**빌드 실행**:
```bash
cd "D:\01. Vscode\Twincat\features\twincat-code-qa-tool"
dotnet build src/TwinCatQA.Application/TwinCatQA.Application.csproj
```

**예상 오류 및 해결**:
- **오류**: `The type or namespace name 'Infrastructure' could not be found`
  - **해결**: Domain 인터페이스로 교체
- **오류**: `Cannot implicitly convert type`
  - **해결**: 생성자 파라미터 타입을 인터페이스로 변경

#### 5단계: DI 등록 검증 (30분)

**검증 항목**:
- CLI의 `ServiceCollectionExtensions.cs`에서 모든 인터페이스-구현체 바인딩 확인
- Application은 인터페이스만 의존하고, Infrastructure 구현체는 CLI에서 주입

**현재 DI 등록** (`ServiceCollectionExtensions.cs`):
```csharp
// Infrastructure Layer - 파싱 ✅
services.AddSingleton<IParserService, AntlrParserService>();

// Infrastructure Layer - 컴파일 ✅
services.AddSingleton<ICompilationService, TwinCatCompilationService>();

// 모든 인터페이스가 올바르게 등록되어 있는지 확인
```

#### 6단계: 통합 테스트 (1시간)

**테스트 시나리오**:
1. CLI 명령어 실행: `dotnet run --project src/TwinCatQA.CLI -- qa --help`
2. 실제 프로젝트 분석: `dotnet run --project src/TwinCatQA.CLI -- qa [테스트 프로젝트 경로]`
3. 결과 보고서 생성 확인

**테스트 스크립트**:
```bash
# 빌드
dotnet build

# CLI 실행 테스트
dotnet run --project src/TwinCatQA.CLI -- qa samples/SimplePLC/

# 결과 확인
ls -la output/
```

### 예상 소요 시간: 6시간

| 단계 | 작업 | 시간 |
|------|------|------|
| 1 | Domain 인터페이스 검증 및 보완 | 1시간 |
| 2 | Application.csproj 수정 | 0.5시간 |
| 3 | Application 코드 리팩토링 | 2시간 |
| 4 | 빌드 및 컴파일 오류 수정 | 1시간 |
| 5 | DI 등록 검증 | 0.5시간 |
| 6 | 통합 테스트 | 1시간 |

---

## 개선 항목 2: DI 등록 완료

### 문제점

**현재 상태**:
- `ServiceCollectionExtensions.cs`에서 일부 서비스가 주석 처리되지 않고 정상 등록됨
- 모든 QA 규칙은 등록되어 있음 (54-73번 줄)
- 핵심 서비스도 등록되어 있음 (33-51번 줄)

**검증 필요 사항**:
1. 등록된 서비스의 생명주기(Singleton, Scoped, Transient)가 적절한지 확인
2. 등록되지 않은 서비스가 있는지 확인

### 마이그레이션 단계

#### 1단계: 서비스 인벤토리 작성 (1시간)

**작업 내용**:
```bash
# 모든 인터페이스 검색
find src/TwinCatQA.Domain/Contracts -name "I*.cs"

# 모든 구현체 검색
find src/TwinCatQA.Infrastructure -name "*.cs" -type f | grep -v obj | grep -v bin

# Application 레이어 서비스 검색
find src/TwinCatQA.Application/Services -name "*.cs"
```

**생성 문서**: `docs/service-inventory.md`
```markdown
| 인터페이스 | 구현체 | DI 등록 여부 | 생명주기 |
|-----------|--------|--------------|---------|
| IParserService | AntlrParserService | ✅ | Singleton |
| ICompilationService | TwinCatCompilationService | ✅ | Singleton |
| ... | ... | ... | ... |
```

#### 2단계: 생명주기 최적화 (1시간)

**가이드라인**:
- **Singleton**: 상태를 가지지 않고 애플리케이션 전체에서 재사용 (Parser, Analyzer)
- **Scoped**: HTTP 요청 당 인스턴스 (웹 애플리케이션용, 현재는 해당 없음)
- **Transient**: 호출마다 새 인스턴스 (가벼운 유틸리티 클래스)

**현재 DI 등록 검토**:
```csharp
// ✅ 적절: Parser는 Singleton (무거운 초기화, 상태 없음)
services.AddSingleton<IParserService, AntlrParserService>();

// 🔍 검토 필요: QaAnalysisService는 Singleton이 적절한가?
services.AddSingleton<QaAnalysisService>();

// 만약 QaAnalysisService가 분석 세션 상태를 가진다면 Transient가 더 적절
// services.AddTransient<QaAnalysisService>();
```

**변경 제안**:
```csharp
// 상태를 가지는 서비스는 Transient로 변경
services.AddTransient<QaAnalysisService>();
services.AddTransient<QaReportGenerator>();

// 무상태 분석기는 Singleton 유지
services.AddSingleton<IVariableUsageAnalyzer, VariableUsageAnalyzer>();
```

#### 3단계: 누락된 서비스 추가 (30분)

**확인 항목**:
1. Domain의 모든 인터페이스가 구현체와 바인딩되었는지 확인
2. Application의 공개 서비스가 모두 등록되었는지 확인

**검증 스크립트**:
```bash
# Domain 인터페이스 목록
grep -r "^public interface I" src/TwinCatQA.Domain/Contracts/ | wc -l

# DI 등록된 인터페이스 개수
grep "AddSingleton<I" src/TwinCatQA.CLI/Services/ServiceCollectionExtensions.cs | wc -l

# 불일치 시 누락된 인터페이스 찾기
```

#### 4단계: 통합 테스트 (1시간)

**테스트 케이스**:
1. CLI 실행 시 DI 컨테이너 초기화 성공 확인
2. 모든 서비스가 올바르게 주입되는지 확인
3. 런타임 오류 없이 QA 분석 완료 확인

**테스트 명령**:
```bash
# 디버그 모드 빌드
dotnet build --configuration Debug

# DI 오류 확인 (서비스 해석 실패 시 예외 발생)
dotnet run --project src/TwinCatQA.CLI -- qa samples/SimplePLC/
```

### 예상 소요 시간: 3.5시간

| 단계 | 작업 | 시간 |
|------|------|------|
| 1 | 서비스 인벤토리 작성 | 1시간 |
| 2 | 생명주기 최적화 | 1시간 |
| 3 | 누락된 서비스 추가 | 0.5시간 |
| 4 | 통합 테스트 | 1시간 |

---

## 개선 항목 3: TODO 기능 구현 완료

### 3.1 ExtractFunctionBlocks 구현

#### 현재 상태

**파일**: `src/TwinCatQA.Infrastructure/Parsers/AntlrParserService.cs:132-144`
```csharp
public List<FunctionBlock> ExtractFunctionBlocks(SyntaxTree ast)
{
    var functionBlocks = new List<FunctionBlock>();

    // TODO: ANTLR4 Visitor 패턴으로 구현
    // 현재는 스켈레톤: 빈 리스트 반환
    return functionBlocks;
}
```

#### 목표 구조

```
ANTLR4 Parse Tree
       │
       ▼
┌─────────────────────────────────────┐
│  FunctionBlockExtractorVisitor      │
│                                     │
│  Visit(ProgramUnitContext)          │
│    └─ IsFunctionBlock?              │
│         └─ Create FunctionBlock     │
│              ├─ Name                │
│              ├─ Variables (VAR)     │
│              ├─ Inputs (VAR_INPUT)  │
│              ├─ Outputs (VAR_OUTPUT)│
│              └─ Body (ST Code)      │
└─────────────────────────────────────┘
```

#### 구현 단계

##### 1단계: Visitor 클래스 생성 (2시간)

**파일**: `src/TwinCatQA.Infrastructure/Parsers/FunctionBlockExtractorVisitor.cs`

```csharp
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime.Tree;
using TwinCatQA.Domain.Models;
using TwinCatQA.Infrastructure.Parsers.Grammars;

namespace TwinCatQA.Infrastructure.Parsers
{
    /// <summary>
    /// ANTLR4 구문 트리에서 Function Block을 추출하는 Visitor
    /// </summary>
    public class FunctionBlockExtractorVisitor : StructuredTextBaseVisitor<object>
    {
        public List<FunctionBlock> FunctionBlocks { get; } = new List<FunctionBlock>();
        private string _currentFilePath;

        public FunctionBlockExtractorVisitor(string filePath)
        {
            _currentFilePath = filePath;
        }

        /// <summary>
        /// FUNCTION_BLOCK 선언 방문
        /// </summary>
        public override object VisitFunctionBlockDeclaration(
            StructuredTextParser.FunctionBlockDeclarationContext context)
        {
            var fb = new FunctionBlock
            {
                Name = context.Identifier()?.GetText() ?? "Unknown",
                FilePath = _currentFilePath,
                StartLine = context.Start.Line,
                EndLine = context.Stop.Line
            };

            // 변수 섹션 추출
            var variableSections = context.variableDeclarations();
            if (variableSections != null)
            {
                foreach (var section in variableSections.varDeclaration())
                {
                    ExtractVariablesFromSection(fb, section);
                }
            }

            // 구현부 추출
            var implementation = context.functionBlockBody();
            if (implementation != null)
            {
                fb.SourceCode = implementation.GetText();
            }

            FunctionBlocks.Add(fb);

            return base.VisitFunctionBlockDeclaration(context);
        }

        /// <summary>
        /// 변수 섹션에서 변수 추출
        /// </summary>
        private void ExtractVariablesFromSection(
            FunctionBlock fb,
            StructuredTextParser.VarDeclarationContext context)
        {
            var varType = context.varType()?.GetText();
            var scope = DetermineVariableScope(varType);

            var variables = context.variableDeclaration();
            foreach (var varDecl in variables)
            {
                var variable = new Variable
                {
                    Name = varDecl.Identifier()?.GetText() ?? "Unknown",
                    DataType = varDecl.dataType()?.GetText() ?? "Unknown",
                    Scope = scope,
                    Line = varDecl.Start.Line
                };

                // 초기값 추출
                var initialValue = varDecl.initialValue();
                if (initialValue != null)
                {
                    variable.InitialValue = initialValue.GetText();
                    variable.IsInitialized = true;
                }

                fb.Variables.Add(variable);
            }
        }

        /// <summary>
        /// 변수 타입에서 Scope 결정
        /// </summary>
        private VariableScope DetermineVariableScope(string varType)
        {
            return varType switch
            {
                "VAR_INPUT" => VariableScope.Input,
                "VAR_OUTPUT" => VariableScope.Output,
                "VAR_IN_OUT" => VariableScope.InOut,
                "VAR_TEMP" => VariableScope.Local,
                "VAR_STAT" => VariableScope.Static,
                _ => VariableScope.Local
            };
        }
    }
}
```

##### 2단계: AntlrParserService 수정 (30분)

**파일**: `src/TwinCatQA.Infrastructure/Parsers/AntlrParserService.cs:132-144`

**변경 전**:
```csharp
public List<FunctionBlock> ExtractFunctionBlocks(SyntaxTree ast)
{
    var functionBlocks = new List<FunctionBlock>();

    // TODO: ANTLR4 Visitor 패턴으로 구현
    return functionBlocks;
}
```

**변경 후**:
```csharp
public List<FunctionBlock> ExtractFunctionBlocks(SyntaxTree ast)
{
    if (ast.RootNode == null)
    {
        return new List<FunctionBlock>();
    }

    var visitor = new FunctionBlockExtractorVisitor(ast.FilePath);
    visitor.Visit(ast.RootNode);

    return visitor.FunctionBlocks;
}
```

##### 3단계: 단위 테스트 작성 (2시간)

**파일**: `tests/TwinCatQA.Infrastructure.Tests/Parsers/FunctionBlockExtractorVisitorTests.cs`

```csharp
using Xunit;
using TwinCatQA.Infrastructure.Parsers;
using TwinCatQA.Domain.Models;

namespace TwinCatQA.Infrastructure.Tests.Parsers
{
    public class FunctionBlockExtractorVisitorTests
    {
        [Fact]
        public void ExtractFunctionBlocks_SimpleFB_ShouldReturnOneBlock()
        {
            // Arrange
            var stCode = @"
FUNCTION_BLOCK FB_Motor
VAR_INPUT
    bEnable : BOOL;
    rSpeed : REAL;
END_VAR

VAR_OUTPUT
    bRunning : BOOL;
END_VAR

VAR
    _state : INT;
END_VAR

// 구현부
IF bEnable THEN
    bRunning := TRUE;
END_IF
END_FUNCTION_BLOCK
";
            var parserService = new AntlrParserService();
            var syntaxTree = CreateSyntaxTreeFromCode(stCode);

            // Act
            var functionBlocks = parserService.ExtractFunctionBlocks(syntaxTree);

            // Assert
            Assert.Single(functionBlocks);
            Assert.Equal("FB_Motor", functionBlocks[0].Name);
            Assert.Equal(3, functionBlocks[0].Variables.Count); // bEnable, rSpeed, bRunning, _state
        }

        [Fact]
        public void ExtractFunctionBlocks_MultipleFBs_ShouldReturnAllBlocks()
        {
            // Arrange
            var stCode = @"
FUNCTION_BLOCK FB_First
END_FUNCTION_BLOCK

FUNCTION_BLOCK FB_Second
END_FUNCTION_BLOCK
";
            var syntaxTree = CreateSyntaxTreeFromCode(stCode);
            var parserService = new AntlrParserService();

            // Act
            var functionBlocks = parserService.ExtractFunctionBlocks(syntaxTree);

            // Assert
            Assert.Equal(2, functionBlocks.Count);
        }

        [Fact]
        public void ExtractFunctionBlocks_WithVariables_ShouldExtractCorrectScopes()
        {
            // Arrange
            var stCode = @"
FUNCTION_BLOCK FB_Example
VAR_INPUT
    inputVar : INT;
END_VAR

VAR_OUTPUT
    outputVar : BOOL;
END_VAR

VAR
    localVar : REAL;
END_VAR
END_FUNCTION_BLOCK
";
            var syntaxTree = CreateSyntaxTreeFromCode(stCode);
            var parserService = new AntlrParserService();

            // Act
            var functionBlocks = parserService.ExtractFunctionBlocks(syntaxTree);
            var fb = functionBlocks[0];

            // Assert
            Assert.Equal(3, fb.Variables.Count);
            Assert.Contains(fb.Variables, v => v.Scope == VariableScope.Input);
            Assert.Contains(fb.Variables, v => v.Scope == VariableScope.Output);
            Assert.Contains(fb.Variables, v => v.Scope == VariableScope.Local);
        }

        private SyntaxTree CreateSyntaxTreeFromCode(string code)
        {
            // 테스트용 임시 파일 생성 및 파싱
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, WrapInXml(code));

            var parserService = new AntlrParserService();
            return parserService.ParseFile(tempFile);
        }

        private string WrapInXml(string stCode)
        {
            return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<TcPlcObject>
  <POU Name=""Test"">
    <Declaration><![CDATA[{stCode}]]></Declaration>
  </POU>
</TcPlcObject>";
        }
    }
}
```

##### 4단계: 통합 테스트 (1시간)

**테스트 명령**:
```bash
# 단위 테스트 실행
dotnet test tests/TwinCatQA.Infrastructure.Tests/

# 실제 프로젝트로 통합 테스트
dotnet run --project src/TwinCatQA.CLI -- qa samples/SimplePLC/
```

#### 예상 소요 시간: 5.5시간

### 3.2 ExtractVariables 구현

#### 현재 상태

**파일**: `src/TwinCatQA.Infrastructure/Parsers/AntlrParserService.cs:152-162`
```csharp
public List<Variable> ExtractVariables(SyntaxTree ast, VariableScope? scope = null)
{
    var variables = new List<Variable>();

    // TODO: ANTLR4 Visitor 패턴으로 구현
    return variables;
}
```

#### 구현 단계

##### 1단계: Visitor 클래스 생성 (1.5시간)

**파일**: `src/TwinCatQA.Infrastructure/Parsers/VariableExtractorVisitor.cs`

```csharp
using System.Collections.Generic;
using TwinCatQA.Domain.Models;
using TwinCatQA.Infrastructure.Parsers.Grammars;

namespace TwinCatQA.Infrastructure.Parsers
{
    /// <summary>
    /// ANTLR4 구문 트리에서 변수를 추출하는 Visitor
    /// </summary>
    public class VariableExtractorVisitor : StructuredTextBaseVisitor<object>
    {
        public List<Variable> Variables { get; } = new List<Variable>();
        private readonly VariableScope? _filterScope;
        private VariableScope _currentScope = VariableScope.Local;

        public VariableExtractorVisitor(VariableScope? filterScope = null)
        {
            _filterScope = filterScope;
        }

        /// <summary>
        /// VAR 선언부 방문
        /// </summary>
        public override object VisitVarDeclaration(
            StructuredTextParser.VarDeclarationContext context)
        {
            // VAR 타입 결정 (VAR, VAR_INPUT, VAR_OUTPUT 등)
            var varTypeText = context.varType()?.GetText();
            _currentScope = DetermineScope(varTypeText);

            // 필터가 설정되어 있고 현재 스코프가 필터와 맞지 않으면 건너뛰기
            if (_filterScope.HasValue && _currentScope != _filterScope.Value)
            {
                return null;
            }

            return base.VisitVarDeclaration(context);
        }

        /// <summary>
        /// 개별 변수 선언 방문
        /// </summary>
        public override object VisitVariableDeclaration(
            StructuredTextParser.VariableDeclarationContext context)
        {
            var variable = new Variable
            {
                Name = context.Identifier()?.GetText() ?? "Unknown",
                DataType = context.dataType()?.GetText() ?? "Unknown",
                Scope = _currentScope,
                Line = context.Start.Line
            };

            // 초기값 확인
            var initialValue = context.initialValue();
            if (initialValue != null)
            {
                variable.InitialValue = initialValue.GetText();
                variable.IsInitialized = true;
            }
            else
            {
                variable.IsInitialized = false;
            }

            // 주석 추출 (있는 경우)
            var comment = ExtractComment(context);
            if (!string.IsNullOrEmpty(comment))
            {
                variable.Comment = comment;
            }

            Variables.Add(variable);

            return base.VisitVariableDeclaration(context);
        }

        private VariableScope DetermineScope(string varType)
        {
            return varType switch
            {
                "VAR_INPUT" => VariableScope.Input,
                "VAR_OUTPUT" => VariableScope.Output,
                "VAR_IN_OUT" => VariableScope.InOut,
                "VAR_TEMP" => VariableScope.Local,
                "VAR_STAT" => VariableScope.Static,
                "VAR_GLOBAL" => VariableScope.Global,
                _ => VariableScope.Local
            };
        }

        private string ExtractComment(StructuredTextParser.VariableDeclarationContext context)
        {
            // ANTLR4의 Hidden 채널에서 주석 추출
            // 구현 세부사항은 문법 파일의 COMMENT 토큰 정의에 따라 다름
            return string.Empty; // 향후 구현
        }
    }
}
```

##### 2단계: AntlrParserService 수정 (30분)

**변경 후**:
```csharp
public List<Variable> ExtractVariables(SyntaxTree ast, VariableScope? scope = null)
{
    if (ast.RootNode == null)
    {
        return new List<Variable>();
    }

    var visitor = new VariableExtractorVisitor(scope);
    visitor.Visit(ast.RootNode);

    return visitor.Variables;
}
```

##### 3단계: 단위 테스트 작성 (1.5시간)

**파일**: `tests/TwinCatQA.Infrastructure.Tests/Parsers/VariableExtractorVisitorTests.cs`

```csharp
using Xunit;
using TwinCatQA.Infrastructure.Parsers;
using TwinCatQA.Domain.Models;

namespace TwinCatQA.Infrastructure.Tests.Parsers
{
    public class VariableExtractorVisitorTests
    {
        [Fact]
        public void ExtractVariables_AllScopes_ShouldReturnAllVariables()
        {
            // Arrange
            var stCode = @"
FUNCTION_BLOCK FB_Test
VAR_INPUT
    inputVar : INT;
END_VAR

VAR_OUTPUT
    outputVar : BOOL;
END_VAR

VAR
    localVar : REAL;
END_VAR
END_FUNCTION_BLOCK
";
            var syntaxTree = CreateSyntaxTreeFromCode(stCode);
            var parserService = new AntlrParserService();

            // Act
            var variables = parserService.ExtractVariables(syntaxTree);

            // Assert
            Assert.Equal(3, variables.Count);
            Assert.Contains(variables, v => v.Name == "inputVar" && v.Scope == VariableScope.Input);
            Assert.Contains(variables, v => v.Name == "outputVar" && v.Scope == VariableScope.Output);
            Assert.Contains(variables, v => v.Name == "localVar" && v.Scope == VariableScope.Local);
        }

        [Fact]
        public void ExtractVariables_FilterByInputScope_ShouldReturnOnlyInputs()
        {
            // Arrange
            var stCode = @"
FUNCTION_BLOCK FB_Test
VAR_INPUT
    inputVar1 : INT;
    inputVar2 : BOOL;
END_VAR

VAR_OUTPUT
    outputVar : REAL;
END_VAR
END_FUNCTION_BLOCK
";
            var syntaxTree = CreateSyntaxTreeFromCode(stCode);
            var parserService = new AntlrParserService();

            // Act
            var variables = parserService.ExtractVariables(syntaxTree, VariableScope.Input);

            // Assert
            Assert.Equal(2, variables.Count);
            Assert.All(variables, v => Assert.Equal(VariableScope.Input, v.Scope));
        }

        [Fact]
        public void ExtractVariables_WithInitialValue_ShouldMarkAsInitialized()
        {
            // Arrange
            var stCode = @"
VAR
    initializedVar : INT := 100;
    uninitializedVar : INT;
END_VAR
";
            var syntaxTree = CreateSyntaxTreeFromCode(stCode);
            var parserService = new AntlrParserService();

            // Act
            var variables = parserService.ExtractVariables(syntaxTree);

            // Assert
            var initialized = variables.Find(v => v.Name == "initializedVar");
            var uninitialized = variables.Find(v => v.Name == "uninitializedVar");

            Assert.NotNull(initialized);
            Assert.True(initialized.IsInitialized);
            Assert.Equal("100", initialized.InitialValue);

            Assert.NotNull(uninitialized);
            Assert.False(uninitialized.IsInitialized);
        }
    }
}
```

#### 예상 소요 시간: 3.5시간

### 3.3 CalculateCyclomaticComplexity 구현

#### 현재 상태

**파일**: `src/TwinCatQA.Infrastructure/Parsers/AntlrParserService.cs:186-195`
```csharp
public int CalculateCyclomaticComplexity(FunctionBlock fb)
{
    // TODO: CyclomaticComplexityVisitor 사용
    return 1;
}
```

**파일**: `src/TwinCatQA.Infrastructure/Parsers/CyclomaticComplexityVisitor.cs:20-43`
```csharp
public class CyclomaticComplexityVisitor // TODO: ANTLR 생성 후 상속
{
    public int CalculateComplexity(object astNode)
    {
        // TODO: ANTLR4 통합 후 구현
        return 1;
    }
}
```

#### 사이클로매틱 복잡도 계산 공식

**Thomas McCabe의 공식**:
```
M = E - N + 2P

여기서:
- M: 사이클로매틱 복잡도
- E: 제어 흐름 그래프의 엣지 수
- N: 노드 수
- P: 연결된 컴포넌트 수 (일반적으로 1)

단순화된 계산법 (Structured Text):
M = 1 + (결정 포인트 개수)

결정 포인트:
- IF ... THEN
- ELSIF
- CASE ... OF (각 CASE 문)
- FOR ... DO
- WHILE ... DO
- REPEAT ... UNTIL
- AND, OR (논리 연산자)
- 삼항 연산자 (?) (있는 경우)
```

#### 구현 단계

##### 1단계: CyclomaticComplexityVisitor 완성 (2시간)

**파일**: `src/TwinCatQA.Infrastructure/Parsers/CyclomaticComplexityVisitor.cs`

```csharp
using System;
using Antlr4.Runtime.Tree;
using TwinCatQA.Infrastructure.Parsers.Grammars;

namespace TwinCatQA.Infrastructure.Parsers
{
    /// <summary>
    /// 사이클로매틱 복잡도 계산 Visitor
    ///
    /// McCabe의 공식을 사용하여 코드의 복잡도를 계산합니다.
    /// 복잡도 = 1 + (결정 포인트 개수)
    /// </summary>
    public class CyclomaticComplexityVisitor : StructuredTextBaseVisitor<int>
    {
        private int _complexity = 1; // 기본 복잡도는 1부터 시작

        /// <summary>
        /// AST 노드에서 사이클로매틱 복잡도를 계산합니다.
        /// </summary>
        public int CalculateComplexity(IParseTree astNode)
        {
            _complexity = 1; // 초기화
            Visit(astNode);
            return _complexity;
        }

        // IF 문 방문
        public override int VisitIfStatement(StructuredTextParser.IfStatementContext context)
        {
            _complexity++; // IF는 +1

            // ELSIF가 있으면 각각 +1
            var elsifClauses = context.elsifClause();
            if (elsifClauses != null)
            {
                _complexity += elsifClauses.Length;
            }

            // ELSE는 복잡도에 영향 없음 (새로운 경로가 아님)

            return base.VisitIfStatement(context);
        }

        // CASE 문 방문
        public override int VisitCaseStatement(StructuredTextParser.CaseStatementContext context)
        {
            var caseElements = context.caseElement();
            if (caseElements != null && caseElements.Length > 0)
            {
                _complexity += caseElements.Length; // 각 CASE는 +1
            }

            return base.VisitCaseStatement(context);
        }

        // FOR 루프 방문
        public override int VisitForStatement(StructuredTextParser.ForStatementContext context)
        {
            _complexity++; // FOR는 +1
            return base.VisitForStatement(context);
        }

        // WHILE 루프 방문
        public override int VisitWhileStatement(StructuredTextParser.WhileStatementContext context)
        {
            _complexity++; // WHILE는 +1
            return base.VisitWhileStatement(context);
        }

        // REPEAT 루프 방문
        public override int VisitRepeatStatement(StructuredTextParser.RepeatStatementContext context)
        {
            _complexity++; // REPEAT는 +1
            return base.VisitRepeatStatement(context);
        }

        // 논리 AND 연산자 방문
        public override int VisitAndExpression(StructuredTextParser.AndExpressionContext context)
        {
            // AND 연산자는 단락 평가(short-circuit)로 인해 복잡도 증가
            _complexity++;
            return base.VisitAndExpression(context);
        }

        // 논리 OR 연산자 방문
        public override int VisitOrExpression(StructuredTextParser.OrExpressionContext context)
        {
            // OR 연산자도 단락 평가로 인해 복잡도 증가
            _complexity++;
            return base.VisitOrExpression(context);
        }

        // EXIT 문 (루프 종료)
        public override int VisitExitStatement(StructuredTextParser.ExitStatementContext context)
        {
            _complexity++; // EXIT는 +1 (조기 탈출 경로)
            return base.VisitExitStatement(context);
        }

        // RETURN 문 (함수 종료)
        public override int VisitReturnStatement(StructuredTextParser.ReturnStatementContext context)
        {
            _complexity++; // RETURN은 +1 (조기 종료 경로)
            return base.VisitReturnStatement(context);
        }

        // 기본 방문 메서드 (모든 자식 노드 방문)
        protected override int DefaultResult => 0;

        protected override int AggregateResult(int aggregate, int nextResult)
        {
            return aggregate + nextResult;
        }
    }
}
```

##### 2단계: AntlrParserService 수정 (30분)

**변경 후**:
```csharp
public int CalculateCyclomaticComplexity(FunctionBlock fb)
{
    if (fb.AstNode == null)
    {
        return 1; // 기본값
    }

    var visitor = new CyclomaticComplexityVisitor();
    return visitor.CalculateComplexity(fb.AstNode);
}
```

**추가 수정**: `FunctionBlock` 모델에 `AstNode` 속성 추가

**파일**: `src/TwinCatQA.Domain/Models/FunctionBlock.cs`

```csharp
public class FunctionBlock
{
    // 기존 속성들...

    /// <summary>
    /// ANTLR4 Parse Tree 노드 (복잡도 계산용)
    /// </summary>
    public object AstNode { get; set; }
}
```

**FunctionBlockExtractorVisitor 수정**:
```csharp
public override object VisitFunctionBlockDeclaration(
    StructuredTextParser.FunctionBlockDeclarationContext context)
{
    var fb = new FunctionBlock
    {
        // ...
        AstNode = context // Parse Tree 노드 저장
    };

    // ...
}
```

##### 3단계: 단위 테스트 작성 (2시간)

**파일**: `tests/TwinCatQA.Infrastructure.Tests/Parsers/CyclomaticComplexityVisitorTests.cs`

```csharp
using Xunit;
using TwinCatQA.Infrastructure.Parsers;

namespace TwinCatQA.Infrastructure.Tests.Parsers
{
    public class CyclomaticComplexityVisitorTests
    {
        [Fact]
        public void CalculateComplexity_EmptyFunction_ShouldReturn1()
        {
            // Arrange
            var stCode = @"
FUNCTION_BLOCK FB_Empty
VAR
END_VAR

// 구현부: 아무 코드 없음
END_FUNCTION_BLOCK
";
            var fb = ParseFunctionBlock(stCode);

            // Act
            var parserService = new AntlrParserService();
            var complexity = parserService.CalculateCyclomaticComplexity(fb);

            // Assert
            Assert.Equal(1, complexity);
        }

        [Fact]
        public void CalculateComplexity_SingleIfStatement_ShouldReturn2()
        {
            // Arrange
            var stCode = @"
FUNCTION_BLOCK FB_SingleIf
VAR
    bCondition : BOOL;
END_VAR

IF bCondition THEN
    // 코드
END_IF
END_FUNCTION_BLOCK
";
            var fb = ParseFunctionBlock(stCode);

            // Act
            var parserService = new AntlrParserService();
            var complexity = parserService.CalculateCyclomaticComplexity(fb);

            // Assert
            Assert.Equal(2, complexity); // 1 (기본) + 1 (IF)
        }

        [Fact]
        public void CalculateComplexity_IfWithElsif_ShouldReturn3()
        {
            // Arrange
            var stCode = @"
FUNCTION_BLOCK FB_IfElsif
VAR
    nValue : INT;
END_VAR

IF nValue = 1 THEN
    // 경로 1
ELSIF nValue = 2 THEN
    // 경로 2
ELSE
    // 경로 3 (ELSE는 복잡도에 영향 없음)
END_IF
END_FUNCTION_BLOCK
";
            var fb = ParseFunctionBlock(stCode);

            // Act
            var parserService = new AntlrParserService();
            var complexity = parserService.CalculateCyclomaticComplexity(fb);

            // Assert
            Assert.Equal(3, complexity); // 1 (기본) + 1 (IF) + 1 (ELSIF)
        }

        [Fact]
        public void CalculateComplexity_CaseStatement_ShouldCountCases()
        {
            // Arrange
            var stCode = @"
FUNCTION_BLOCK FB_Case
VAR
    nState : INT;
END_VAR

CASE nState OF
    1: // 경로 1
    2: // 경로 2
    3: // 경로 3
    ELSE: // ELSE는 복잡도에 영향 없음
END_CASE
END_FUNCTION_BLOCK
";
            var fb = ParseFunctionBlock(stCode);

            // Act
            var parserService = new AntlrParserService();
            var complexity = parserService.CalculateCyclomaticComplexity(fb);

            // Assert
            Assert.Equal(4, complexity); // 1 (기본) + 3 (CASE 분기)
        }

        [Fact]
        public void CalculateComplexity_NestedLoops_ShouldCountAll()
        {
            // Arrange
            var stCode = @"
FUNCTION_BLOCK FB_NestedLoops
VAR
    i, j : INT;
END_VAR

FOR i := 1 TO 10 DO
    FOR j := 1 TO 5 DO
        // 중첩 루프
    END_FOR
END_FOR
END_FUNCTION_BLOCK
";
            var fb = ParseFunctionBlock(stCode);

            // Act
            var parserService = new AntlrParserService();
            var complexity = parserService.CalculateCyclomaticComplexity(fb);

            // Assert
            Assert.Equal(3, complexity); // 1 (기본) + 1 (FOR i) + 1 (FOR j)
        }

        [Fact]
        public void CalculateComplexity_LogicalOperators_ShouldIncreaseComplexity()
        {
            // Arrange
            var stCode = @"
FUNCTION_BLOCK FB_LogicalOps
VAR
    bA, bB, bC : BOOL;
END_VAR

IF (bA AND bB) OR bC THEN
    // 코드
END_IF
END_FUNCTION_BLOCK
";
            var fb = ParseFunctionBlock(stCode);

            // Act
            var parserService = new AntlrParserService();
            var complexity = parserService.CalculateCyclomaticComplexity(fb);

            // Assert
            Assert.Equal(4, complexity); // 1 (기본) + 1 (IF) + 1 (AND) + 1 (OR)
        }

        [Fact]
        public void CalculateComplexity_ComplexFunction_ShouldReturnAccurateCount()
        {
            // Arrange
            var stCode = @"
FUNCTION_BLOCK FB_Complex
VAR
    nState : INT;
    bCondition : BOOL;
    i : INT;
END_VAR

CASE nState OF
    1:
        IF bCondition THEN
            FOR i := 1 TO 10 DO
                // 코드
            END_FOR
        END_IF
    2:
        WHILE bCondition DO
            // 코드
        END_WHILE
    3:
        // 코드
END_CASE
END_FUNCTION_BLOCK
";
            var fb = ParseFunctionBlock(stCode);

            // Act
            var parserService = new AntlrParserService();
            var complexity = parserService.CalculateCyclomaticComplexity(fb);

            // Assert
            // 1 (기본) + 3 (CASE 분기) + 1 (IF) + 1 (FOR) + 1 (WHILE) = 7
            Assert.Equal(7, complexity);
        }

        private FunctionBlock ParseFunctionBlock(string stCode)
        {
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, WrapInXml(stCode));

            var parserService = new AntlrParserService();
            var syntaxTree = parserService.ParseFile(tempFile);
            var functionBlocks = parserService.ExtractFunctionBlocks(syntaxTree);

            return functionBlocks.First();
        }

        private string WrapInXml(string stCode)
        {
            return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<TcPlcObject>
  <POU Name=""Test"">
    <Declaration><![CDATA[{stCode}]]></Declaration>
  </POU>
</TcPlcObject>";
        }
    }
}
```

##### 4단계: 통합 테스트 (1시간)

**테스트 케이스**:
1. 간단한 Function Block의 복잡도 계산
2. 복잡한 Function Block (중첩 IF, CASE, 루프)의 복잡도 계산
3. QA 규칙 `HighComplexityRule`과 통합 테스트

**실행 명령**:
```bash
# 단위 테스트
dotnet test tests/TwinCatQA.Infrastructure.Tests/

# 통합 테스트 (실제 프로젝트)
dotnet run --project src/TwinCatQA.CLI -- qa samples/ComplexPLC/

# 결과에서 "High Cyclomatic Complexity" 경고 확인
```

#### 예상 소요 시간: 5.5시간

### TODO 기능 구현 총 예상 소요 시간: 14.5시간

| 기능 | 소요 시간 |
|------|----------|
| ExtractFunctionBlocks | 5.5시간 |
| ExtractVariables | 3.5시간 |
| CalculateCyclomaticComplexity | 5.5시간 |

---

## 개선 항목 4: 아키텍처 결정 기록(ADR) 작성

### ADR (Architecture Decision Record) 개요

**목적**:
- 중요한 아키텍처 결정의 맥락과 근거를 문서화
- 향후 유지보수자가 "왜 이렇게 설계되었는지" 이해
- 기술 부채 발생 시 의사결정 추적

**형식** (Michael Nygard의 ADR 템플릿):
```markdown
# ADR-[번호]: [결정 제목]

## 상태
[제안 | 승인 | 거부 | 폐기 | 대체됨]

## 맥락
무엇이 이 결정을 필요로 했는가?

## 결정
우리는 [선택]을 결정했다.

## 결과
- 긍정적 결과
- 부정적 결과
- 트레이드오프
```

### ADR 작성 단계

#### ADR-001: Clean Architecture 채택

**파일**: `docs/architecture/adr/001-clean-architecture.md`

```markdown
# ADR-001: Clean Architecture 채택

## 상태
승인 (2024-11-26)

## 맥락

TwinCatQA 도구는 TwinCAT PLC 프로젝트의 코드 품질을 분석하는 시스템입니다. 다음과 같은 요구사항이 있었습니다:

1. **테스트 가능성**: 단위 테스트와 통합 테스트가 용이해야 함
2. **확장성**: 새로운 QA 규칙과 분석기를 쉽게 추가할 수 있어야 함
3. **유지보수성**: 비즈니스 로직과 인프라 구현을 분리하여 변경 영향도를 최소화
4. **기술 독립성**: 파서 라이브러리(ANTLR4)나 파일 시스템 구현을 교체할 수 있어야 함
5. **다중 진입점**: CLI, UI, API 등 다양한 인터페이스를 지원해야 함

기존 아키텍처 옵션:
- **레이어드 아키텍처**: 전통적인 3계층 (Presentation - Business - Data)
- **Clean Architecture**: 의존성 역전 원칙 기반 (Domain 중심)
- **헥사고널 아키텍처**: 포트와 어댑터 패턴

## 결정

**Clean Architecture (Uncle Bob의 설계 원칙)**를 채택합니다.

### 레이어 구조:
```
┌─────────────────────────────────────┐
│    Presentation Layer (CLI, UI)    │  의존 방향: ↓
├─────────────────────────────────────┤
│    Application Layer                │  의존 방향: ↓
│    (Use Cases, Orchestration)       │
├─────────────────────────────────────┤
│    Domain Layer                     │  ← 핵심 (의존성 없음)
│    (Entities, Interfaces)           │
└─────────────────────────────────────┘
         ↑ 구현
┌─────────────────────────────────────┐
│    Infrastructure Layer             │
│    (ANTLR4, File I/O, Git)          │
└─────────────────────────────────────┘
```

### 원칙:
1. **의존성 규칙**: 외부 레이어는 내부 레이어에만 의존 (역방향 의존성 금지)
2. **도메인 중심**: 비즈니스 규칙(Domain)은 기술 세부사항(Infrastructure)과 독립
3. **인터페이스 분리**: Domain이 인터페이스를 정의하고 Infrastructure가 구현
4. **DI 패턴**: 진입점(CLI/UI)에서 모든 의존성 주입

## 결과

### 긍정적 결과

✅ **테스트 용이성 향상**:
- Domain과 Application 레이어를 Mock 객체 없이 단위 테스트 가능
- Infrastructure 구현을 Fake 객체로 대체하여 통합 테스트 가능

✅ **비즈니스 로직 명확화**:
- `IParserService`, `ICompilationService` 등 인터페이스가 핵심 기능을 명확히 표현
- Domain 모델(`FunctionBlock`, `Variable`)이 순수한 비즈니스 개념 표현

✅ **기술 교체 가능성**:
- ANTLR4를 다른 파서로 교체하더라도 Domain과 Application 레이어는 변경 불필요
- 파일 시스템을 S3나 데이터베이스로 교체 가능

✅ **다중 진입점 지원**:
- CLI, UI, API 서버가 동일한 Application 레이어를 재사용
- 각 진입점은 DI 설정만 다르게 구성

### 부정적 결과

❌ **초기 복잡도 증가**:
- 간단한 기능도 인터페이스-구현체 분리 필요
- 레이어 간 DTO 매핑 코드 증가 (Domain 모델 ↔ Infrastructure 모델)

❌ **러닝 커브**:
- 팀원이 의존성 역전 원칙을 이해하는 데 시간 필요
- "왜 Application이 Infrastructure를 참조하면 안 되는가?"에 대한 교육 필요

❌ **보일러플레이트 코드**:
- 모든 서비스에 대해 인터페이스 정의 필요
- DI 등록 코드가 길어짐

### 트레이드오프

**선택**: Clean Architecture의 엄격한 레이어 분리
**포기**: 간단한 CRUD 스타일의 빠른 개발 속도
**근거**: TwinCatQA는 장기적으로 유지보수되는 도구이므로 초기 복잡도보다 장기 유지보수성을 우선

### 예외 사항

다음 경우는 Clean Architecture 원칙을 완화할 수 있습니다:
1. **프로토타입**: 개념 검증(PoC) 단계에서는 레이어 분리 생략 가능
2. **일회성 스크립트**: `scripts/` 디렉토리의 유틸리티는 아키텍처 제약 없음
3. **테스트 코드**: 테스트는 모든 레이어를 직접 참조 가능

## 참고 자료

- Robert C. Martin, "Clean Architecture: A Craftsman's Guide to Software Structure and Design" (2017)
- Microsoft, ".NET Microservices: Architecture for Containerized .NET Applications" - Clean Architecture 챕터
- 프로젝트 내부: `docs/architecture/clean-architecture-overview.md`
```

#### ADR-002: ANTLR4 파서 선택

**파일**: `docs/architecture/adr/002-antlr4-parser-selection.md`

```markdown
# ADR-002: ANTLR4를 TwinCAT Structured Text 파서로 선택

## 상태
승인 (2024-11-26)

## 맥락

TwinCatQA 도구의 핵심 기능은 TwinCAT PLC 프로젝트의 ST (Structured Text) 코드를 파싱하여 구문 트리(AST)를 생성하는 것입니다.

### 요구사항:
1. **IEC 61131-3 표준 지원**: ST 언어의 모든 구문 (IF, CASE, FOR, WHILE, FUNCTION_BLOCK 등)
2. **정확한 구문 분석**: 문법 오류를 정확히 감지하고 위치 정보 제공
3. **AST 생성**: 코드 분석을 위한 구조화된 트리 필요
4. **확장 가능성**: 새로운 문법 규칙을 쉽게 추가할 수 있어야 함
5. **C# 통합**: .NET 프로젝트에서 사용 가능해야 함

### 평가한 옵션:

#### 옵션 1: 정규표현식 기반 파싱
**장점**:
- 구현이 빠르고 간단
- 외부 의존성 없음

**단점**:
- 복잡한 중첩 구조 (IF-ELSIF-ELSE, CASE) 처리 어려움
- 문법 오류 위치 정확도 낮음
- 유지보수 어려움 (문법 변경 시 정규식 전체 재작성)

#### 옵션 2: 수동 작성 재귀 하강 파서 (Recursive Descent Parser)
**장점**:
- 완전한 제어 가능
- 외부 의존성 없음
- 오류 메시지 커스터마이징 용이

**단점**:
- 개발 시간이 매우 김 (문법 규칙 수백 개)
- 문법 변경 시 파서 코드 수동 수정 필요
- 테스트 케이스 작성 부담

#### 옵션 3: ANTLR4 (ANother Tool for Language Recognition)
**장점**:
- **문법 파일 기반**: `.g4` 파일에 BNF 형식으로 문법 정의
- **자동 코드 생성**: Lexer, Parser, Visitor 클래스 자동 생성
- **강력한 오류 처리**: 구문 오류 위치와 메시지 자동 제공
- **C# 지원**: `Antlr4.Runtime.Standard` NuGet 패키지
- **커뮤니티 지원**: IEC 61131-3 문법 예제 존재 (GitHub)

**단점**:
- 외부 의존성 추가 (`Antlr4.Runtime.Standard`)
- 초기 학습 곡선 (ANTLR4 문법 작성법)
- 빌드 단계 추가 (문법 파일 컴파일)

#### 옵션 4: Roslyn 기반 커스텀 파서
**장점**:
- Microsoft의 공식 컴파일러 플랫폼
- 강력한 IDE 지원 (IntelliSense, 리팩토링)

**단점**:
- C# 전용 (ST 언어는 별도 문법 정의 필요)
- 오버킬 (TwinCAT ST는 C#보다 간단한 문법)

## 결정

**ANTLR4를 TwinCAT Structured Text 파서로 선택합니다.**

### 근거:

1. **문법 중심 개발**:
   - ST 문법을 `.g4` 파일에 선언적으로 정의
   - 문법 변경 시 코드 재작성 불필요 (재컴파일만)

2. **자동 코드 생성**:
   - Lexer (어휘 분석), Parser (구문 분석), Visitor (트리 순회) 자동 생성
   - 수동 파서 작성 대비 개발 시간 80% 절감 (예상)

3. **강력한 오류 처리**:
   - 구문 오류의 정확한 라인/컬럼 위치 제공
   - 사용자 친화적인 오류 메시지 (예: "';' expected at line 42")

4. **검증된 기술**:
   - 업계 표준 (Java, Python, TypeScript 파서에도 사용)
   - IEC 61131-3 문법 참고 자료 존재 (GitHub: `iec61131-3.g4`)

5. **Visitor 패턴 지원**:
   - AST 순회를 위한 Visitor 클래스 제공
   - QA 규칙 구현 시 `StructuredTextBaseVisitor<T>` 상속

### 구현 전략:

```
1. 문법 파일 작성: src/TwinCatQA.Grammar/StructuredText.g4
2. ANTLR4 컴파일: java -jar antlr4.jar StructuredText.g4 -Dlanguage=CSharp
3. 생성 파일: StructuredTextLexer.cs, StructuredTextParser.cs, StructuredTextVisitor.cs
4. Infrastructure에 통합: AntlrParserService.cs
```

## 결과

### 긍정적 결과

✅ **개발 속도 향상**:
- 문법 파일 작성 후 파서 코드 자동 생성 (1주 → 2일)
- Visitor 패턴으로 QA 규칙 구현 간소화

✅ **정확한 파싱**:
- ANTLR4의 LL(*) 파싱 알고리즘으로 복잡한 중첩 구조 처리
- 문법 애매성(ambiguity) 자동 감지 및 경고

✅ **유지보수성**:
- 문법 변경 시 `.g4` 파일만 수정 후 재컴파일
- Visitor 클래스는 변경 불필요 (새 메서드 추가만)

✅ **확장성**:
- 새로운 QA 규칙 = 새로운 Visitor 클래스 추가
- `CyclomaticComplexityVisitor`, `NamingConventionVisitor` 등 독립적 구현

### 부정적 결과

❌ **외부 의존성**:
- `Antlr4.Runtime.Standard` NuGet 패키지 필수 (4.11.1 버전, 4.2MB)
- ANTLR4 버전 업그레이드 시 문법 호환성 확인 필요

❌ **빌드 복잡도**:
- 문법 파일 변경 시 수동 컴파일 단계 필요
- CI/CD 파이프라인에 ANTLR4 설치 필요 (Java 런타임)

❌ **디버깅 어려움**:
- 생성된 파서 코드는 읽기 어려움 (자동 생성 코드)
- 파싱 오류 디버깅 시 Parse Tree 시각화 도구 필요 (ANTLR4 Lab)

❌ **초기 학습 비용**:
- 팀원이 ANTLR4 문법 작성법을 학습해야 함
- Visitor 패턴에 익숙하지 않은 경우 교육 필요

### 트레이드오프

**선택**: ANTLR4의 강력한 기능과 자동화
**포기**: 완전한 코드 제어와 의존성 제로
**근거**: TwinCAT ST 문법의 복잡도(IF, CASE, 중첩 루프 등)를 고려할 때, 수동 파서 작성은 비현실적

### 리스크 및 완화 전략

| 리스크 | 확률 | 영향 | 완화 전략 |
|--------|------|------|-----------|
| ANTLR4 버전 호환성 문제 | 낮음 | 중간 | 4.x 버전 고정 (Semantic Versioning) |
| ST 문법의 모호성 | 중간 | 높음 | IEC 61131-3 표준 문서 참조, 테스트 케이스 확충 |
| 파싱 성능 저하 (대규모 프로젝트) | 낮음 | 중간 | 병렬 파싱, 캐싱 전략 도입 |
| ANTLR4 라이선스 문제 | 없음 | - | BSD 라이선스 (상업적 사용 가능) |

### 대안 시나리오

ANTLR4가 실패할 경우 다음 대안을 고려:
1. **단기 대안**: 정규표현식 기반 간단한 파서 (기능 제한)
2. **장기 대안**: Roslyn 기반 커스텀 파서 (투자 증가)

## 참고 자료

- ANTLR4 공식 문서: https://www.antlr.org/
- IEC 61131-3 문법 참고: https://github.com/jubnzv/iec-checker
- 프로젝트 내부: `src/TwinCatQA.Grammar/StructuredText.g4`
- NuGet 패키지: https://www.nuget.org/packages/Antlr4.Runtime.Standard/
```

### ADR 작성 소요 시간: 3시간

| ADR | 소요 시간 |
|-----|----------|
| ADR-001: Clean Architecture | 1.5시간 |
| ADR-002: ANTLR4 파서 선택 | 1.5시간 |

---

## 전체 개선 워크플로우 요약

### 우선순위 및 의존성

```
[1단계] ADR 작성 (병렬 가능)
   ↓
[2단계] Application → Infrastructure 의존성 제거
   ├─ Domain 인터페이스 검증
   ├─ Application.csproj 수정
   └─ 코드 리팩토링
   ↓
[3단계] DI 등록 검증 및 최적화 (병렬 가능)
   ↓
[4단계] TODO 기능 구현 (병렬 가능)
   ├─ ExtractFunctionBlocks
   ├─ ExtractVariables
   └─ CalculateCyclomaticComplexity
   ↓
[5단계] 통합 테스트 및 검증
```

### 총 예상 소요 시간

| 개선 항목 | 예상 시간 | 우선순위 |
|-----------|----------|---------|
| 1. Application → Infrastructure 의존성 제거 | 6시간 | 높음 |
| 2. DI 등록 완료 | 3.5시간 | 중간 |
| 3. TODO 기능 구현 | 14.5시간 | 높음 |
| 4. ADR 작성 | 3시간 | 낮음 |
| **총계** | **27시간** | - |

**추천 일정** (1주일, 하루 4시간 작업 기준):
- **1일차**: ADR 작성 (3시간) + Domain 인터페이스 검증 (1시간)
- **2일차**: Application 의존성 제거 (4시간)
- **3일차**: Application 의존성 제거 완료 (1시간) + DI 등록 최적화 (3시간)
- **4일차**: ExtractFunctionBlocks 구현 (4시간)
- **5일차**: ExtractFunctionBlocks 완료 (1.5시간) + ExtractVariables 구현 (2.5시간)
- **6일차**: ExtractVariables 완료 (1시간) + CalculateCyclomaticComplexity 구현 (3시간)
- **7일차**: CalculateCyclomaticComplexity 완료 (2.5시간) + 통합 테스트 (1.5시간)

---

## 테스트 전략

### 단위 테스트

**범위**:
- Infrastructure: 각 Visitor 클래스의 동작 검증
- Application: 인터페이스 기반 Mock 테스트
- Domain: 도메인 모델 검증

**도구**:
- xUnit
- Moq (Mock 객체)

**커버리지 목표**: 80% 이상

### 통합 테스트

**시나리오**:
1. **엔드투엔드 QA 분석**:
   - 샘플 TwinCAT 프로젝트 파싱 → QA 규칙 실행 → 보고서 생성
   - 예상 결과와 실제 결과 비교

2. **오류 처리**:
   - 문법 오류가 있는 ST 코드 파싱 시 적절한 오류 메시지 반환

3. **성능 테스트**:
   - 1000개 Function Block이 있는 대규모 프로젝트 분석 (시간 측정)

### 리그레션 테스트

**목적**: 아키텍처 변경 후 기존 기능 유지 확인

**방법**:
1. 변경 전 테스트 실행 → 결과 스냅샷 저장
2. 변경 적용
3. 변경 후 테스트 실행 → 결과 비교

**도구**:
```bash
# 변경 전
dotnet test --logger "trx;LogFileName=before.trx"

# 변경 후
dotnet test --logger "trx;LogFileName=after.trx"

# 비교
diff before.trx after.trx
```

---

## 리스크 관리

### 주요 리스크

| 리스크 | 확률 | 영향 | 완화 전략 |
|--------|------|------|-----------|
| Application 리팩토링 중 버그 발생 | 중간 | 높음 | 변경 전 스냅샷 테스트, 단계별 커밋 |
| ANTLR4 생성 파일 누락 | 낮음 | 높음 | Grammar 프로젝트에 빌드 스크립트 추가 |
| DI 설정 오류 (런타임 오류) | 중간 | 중간 | 시작 시 DI 컨테이너 검증 로직 추가 |
| 테스트 커버리지 부족 | 높음 | 중간 | 코드 리뷰에서 테스트 필수 체크 |

### 롤백 계획

각 단계마다 Git 커밋을 생성하여 롤백 가능하도록 합니다:

```bash
# 변경 전 백업 브랜치 생성
git checkout -b architecture-improvement-backup

# 각 단계마다 커밋
git checkout -b architecture-improvement
git commit -m "1단계: Domain 인터페이스 검증 완료"
git commit -m "2단계: Application.csproj 수정 완료"
# ...

# 문제 발생 시 롤백
git reset --hard HEAD~1  # 마지막 커밋 취소
```

---

## 성공 기준

### 기술적 기준

✅ **빌드 성공**:
- `dotnet build` 명령어가 오류 없이 완료
- 모든 프로젝트(Domain, Application, Infrastructure, CLI)가 정상 빌드

✅ **테스트 통과**:
- 모든 단위 테스트 통과 (0 failed)
- 통합 테스트 통과
- 코드 커버리지 80% 이상

✅ **아키텍처 원칙 준수**:
- Application.csproj에서 Infrastructure 참조 제거 확인
- Domain 레이어가 외부 의존성 없음 확인

✅ **기능 완성**:
- `ExtractFunctionBlocks` 구현 및 테스트 통과
- `ExtractVariables` 구현 및 테스트 통과
- `CalculateCyclomaticComplexity` 구현 및 테스트 통과

### 비즈니스 기준

✅ **사용자 관점**:
- CLI 명령어 실행 시 정상 동작
- QA 보고서가 올바른 결과 출력 (Function Block, 변수, 복잡도 포함)

✅ **문서화**:
- ADR-001, ADR-002 작성 완료
- 각 변경사항에 대한 커밋 메시지 작성

---

## 다음 단계 (선택적)

아키텍처 개선 완료 후 추가로 고려할 사항:

1. **성능 최적화**:
   - 병렬 파싱 (여러 파일 동시 처리)
   - Parse Tree 캐싱

2. **고급 분석 기능**:
   - 데드 코드 탐지
   - 순환 복잡도 히트맵 생성

3. **UI 개선**:
   - Avalonia UI로 데스크톱 애플리케이션 개발
   - 웹 기반 대시보드 (ASP.NET Core)

4. **CI/CD 통합**:
   - GitHub Actions에서 자동 QA 분석
   - Pull Request 코멘트에 품질 점수 표시

---

## 참고 자료

### 내부 문서
- `D:\01. Vscode\Twincat\features\twincat-code-qa-tool\spec.md` - 프로젝트 명세
- `D:\01. Vscode\Twincat\features\twincat-code-qa-tool\plan.md` - 구현 계획
- `D:\01. Vscode\Twincat\features\twincat-code-qa-tool\README.md` - 프로젝트 개요

### 외부 자료
- Robert C. Martin, "Clean Architecture" (2017)
- ANTLR4 공식 문서: https://www.antlr.org/
- Microsoft .NET Dependency Injection: https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection

---

**문서 작성일**: 2024-11-26
**작성자**: System Architect Agent
**버전**: 1.0
