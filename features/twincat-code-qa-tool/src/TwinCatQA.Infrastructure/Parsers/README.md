# TwinCAT 파서 구현 가이드

## 개요

이 디렉토리는 TwinCAT ST (Structured Text) 코드를 파싱하고 분석하는 인프라스트럭처를 포함합니다.

## 구성 요소

### 1. StructuredText.g4
**목적**: IEC 61131-3 Structured Text 언어의 ANTLR4 문법 정의

**포함 규칙**:
- 변수 선언 (`VAR`, `VAR_INPUT`, `VAR_OUTPUT`, `VAR_IN_OUT`, `VAR_TEMP`)
- Function Block/Function/Program 선언
- 제어문 (`IF`, `CASE`, `FOR`, `WHILE`, `REPEAT`)
- 표현식 (산술, 논리, 비교 연산)
- 데이터 타입 (원시 타입, 구조체, 배열, 포인터)
- 주석 (블록 `(* *)`, 라인 `//`)

**ANTLR4 컴파일 방법**:
```bash
# 1. ANTLR4 JAR 다운로드
# https://www.antlr.org/download.html

# 2. 문법 파일 컴파일 (C# 타겟)
cd src/TwinCatQA.Infrastructure/Parsers/Grammars
java -jar antlr-4.11.1-complete.jar -Dlanguage=CSharp StructuredText.g4

# 3. 생성된 파일 확인
# - StructuredTextLexer.cs
# - StructuredTextParser.cs
# - StructuredTextVisitor.cs
# - StructuredTextBaseVisitor.cs
```

### 2. AntlrParserService.cs
**목적**: `IParserService` 인터페이스 구현

**주요 기능**:
- TwinCAT XML 파일 파싱 (LINQ to XML)
- ST 코드 추출
- ANTLR4 파서 통합 (생성 파일 추가 후 활성화)
- AST 생성 및 관리

**현재 상태**:
- ✅ XML 파싱 구현 완료
- ✅ 주석 추출 구현 완료
- ⏳ ANTLR4 파서 통합 대기 (스켈레톤 준비 완료)

**사용 예시**:
```csharp
var parserService = new AntlrParserService();
var syntaxTree = parserService.ParseFile("POUs/FB_Example.TcPOU");

if (syntaxTree.Errors.Any())
{
    // 파싱 오류 처리
    foreach (var error in syntaxTree.Errors)
    {
        Console.WriteLine($"{error.Line}:{error.Column} - {error.Message}");
    }
}
else
{
    // AST 분석
    var functionBlocks = parserService.ExtractFunctionBlocks(syntaxTree);
    var comments = parserService.ExtractComments(syntaxTree);
}
```

### 3. CyclomaticComplexityVisitor.cs
**목적**: McCabe 사이클로매틱 복잡도 계산

**복잡도 규칙**:
- 기본값: `1` (단일 경로)
- `IF` 문: `+1`
- `ELSIF` 절: 각각 `+1`
- `CASE` 문: 각 case 요소마다 `+1`
- `FOR`, `WHILE`, `REPEAT` 문: 각각 `+1`
- `EXIT` 문: `+1` (조기 탈출)
- `RETURN` 문: `+1` (중간 반환)

**임계값**:
- `1-10`: 단순 (Good)
- `11-15`: 보통 (Medium warning)
- `16-20`: 복잡 (High warning)
- `21+`: 매우 복잡 (Critical, 리팩토링 필수)

**현재 상태**:
- ✅ 복잡도 계산 로직 주석으로 상세 설명
- ⏳ ANTLR4 Visitor 상속 대기 (ANTLR 생성 파일 필요)

**사용 예시**:
```csharp
var visitor = new CyclomaticComplexityVisitor();
int complexity = visitor.Calculate(functionBlock);

if (complexity > 15)
{
    Console.WriteLine($"경고: 복잡도가 {complexity}로 높습니다. 리팩토링을 권장합니다.");
}

// 위반 심각도 판단
var severity = CyclomaticComplexityVisitor.GetViolationSeverity(
    complexity,
    mediumThreshold: 10,
    highThreshold: 15,
    criticalThreshold: 20
);
```

## ANTLR4 통합 절차

### 단계 1: ANTLR4 컴파일
```bash
cd src/TwinCatQA.Infrastructure/Parsers/Grammars
java -jar antlr-4.11.1-complete.jar -Dlanguage=CSharp StructuredText.g4
```

### 단계 2: 생성 파일 프로젝트에 추가
- `StructuredTextLexer.cs`
- `StructuredTextParser.cs`
- `StructuredTextVisitor.cs`
- `StructuredTextBaseVisitor.cs`

Visual Studio 또는 Rider에서 프로젝트에 포함:
```xml
<ItemGroup>
  <Compile Include="Parsers\Grammars\StructuredTextLexer.cs" />
  <Compile Include="Parsers\Grammars\StructuredTextParser.cs" />
  <Compile Include="Parsers\Grammars\StructuredTextVisitor.cs" />
  <Compile Include="Parsers\Grammars\StructuredTextBaseVisitor.cs" />
</ItemGroup>
```

### 단계 3: NuGet 패키지 설치
```bash
dotnet add package Antlr4.Runtime.Standard --version 4.11.1
```

### 단계 4: AntlrParserService 주석 해제
`AntlrParserService.cs`에서 다음 주석 해제:
- ANTLR4 파서 초기화 코드
- `ParsingErrorListener` 클래스

### 단계 5: CyclomaticComplexityVisitor 상속 활성화
```csharp
public class CyclomaticComplexityVisitor : StructuredTextBaseVisitor<int>
{
    // Visitor 메서드 주석 해제
}
```

### 단계 6: 테스트
```bash
dotnet test TwinCatQA.Tests
```

## 파일 구조

```
Parsers/
├── Grammars/
│   ├── StructuredText.g4           # ANTLR4 문법 정의
│   ├── StructuredTextLexer.cs      # ANTLR 생성 (TODO)
│   ├── StructuredTextParser.cs     # ANTLR 생성 (TODO)
│   ├── StructuredTextVisitor.cs    # ANTLR 생성 (TODO)
│   └── StructuredTextBaseVisitor.cs # ANTLR 생성 (TODO)
├── AntlrParserService.cs           # IParserService 구현
├── CyclomaticComplexityVisitor.cs  # 복잡도 계산 Visitor
└── README.md                       # 본 파일
```

## TwinCAT XML 파일 구조

### POU 파일 (.TcPOU)
```xml
<TcPlcObject Version="1.1.0.1" ProductVersion="3.1.4024.0">
  <POU Name="FB_Example" Id="{guid}">
    <Declaration><![CDATA[
FUNCTION_BLOCK FB_Example
VAR_INPUT
    enable : BOOL;
END_VAR
VAR_OUTPUT
    running : BOOL;
END_VAR
    ]]></Declaration>
    <Implementation>
      <ST><![CDATA[
IF enable THEN
    running := TRUE;
ELSE
    running := FALSE;
END_IF;
      ]]></ST>
    </Implementation>
  </POU>
</TcPlcObject>
```

### DUT 파일 (.TcDUT)
```xml
<TcPlcObject Version="1.1.0.1" ProductVersion="3.1.4024.0">
  <DUT Name="T_MyStruct" Id="{guid}">
    <Declaration><![CDATA[
TYPE T_MyStruct :
STRUCT
    field1 : INT;
    field2 : REAL;
END_STRUCT
END_TYPE
    ]]></Declaration>
  </DUT>
</TcPlcObject>
```

## 주의사항

1. **한글 주석**: 모든 코드 주석은 한글로 작성되어야 합니다.
2. **ANTLR4 버전**: Antlr4.Runtime.Standard 4.11.1 이상 사용
3. **인코딩**: TwinCAT XML 파일은 UTF-8 인코딩 사용
4. **오류 처리**: 파싱 오류 발생 시 `SyntaxTree.Errors`에 수집

## 다음 단계

- [ ] ANTLR4 문법 컴파일
- [ ] 생성된 파서 파일 프로젝트에 추가
- [ ] `AntlrParserService` 주석 해제 및 통합 테스트
- [ ] `CyclomaticComplexityVisitor` 구현 완료
- [ ] 추가 Visitor 구현 (`FunctionBlockExtractorVisitor`, `VariableExtractorVisitor`)

## 참조

- [ANTLR4 공식 문서](https://github.com/antlr/antlr4/blob/master/doc/index.md)
- [IEC 61131-3 표준](https://www.plcopen.org/iec-61131-3)
- [McCabe 복잡도](https://en.wikipedia.org/wiki/Cyclomatic_complexity)
