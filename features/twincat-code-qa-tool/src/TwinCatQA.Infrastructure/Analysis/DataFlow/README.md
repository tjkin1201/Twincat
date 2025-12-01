# DataFlow 분석기 (Level 2)

## 개요

TwinCAT Code QA Tool의 Level 2 분석 기능으로, 데이터 흐름 분석(Data Flow Analysis)을 수행합니다.
변수의 정의(Definition)와 사용(Use)을 추적하고, 제어 흐름 그래프(CFG)를 생성하여 코드의 품질과 안전성을 검증합니다.

## 구성 요소

### 1. DataFlowAnalyzer.cs
- **역할**: 메인 분석기 클래스
- **기능**:
  - Syntax Tree를 입력받아 전체 데이터 흐름 분석 수행
  - CFG 생성 및 Def-Use Chain 수집
  - 활성 변수 분석 (Liveness Analysis)
  - 도달 불가능한 코드 검출
  - 루프 감지 및 분석
  - 통계 정보 생성

### 2. DefUseChain.cs
- **역할**: 정의-사용 체인 추적
- **주요 클래스**:
  - `DefUseChain`: 변수의 정의와 사용 위치 추적
  - `DeclarationSite`: 변수 선언 위치
  - `DefinitionSite`: 변수 할당(정의) 위치
  - `UsageSite`: 변수 사용 위치
  - `UnusedVariable`: 사용되지 않는 변수 정보
  - `UninitializedUsage`: 초기화되지 않은 사용 정보

### 3. ControlFlowGraph.cs
- **역할**: 제어 흐름 그래프 생성 및 분석
- **주요 클래스**:
  - `ControlFlowGraph`: CFG 컨테이너
  - `BasicBlock`: 기본 블록 (순차 실행 구문 집합)
  - `LoopInfo`: 루프 정보
  - `ControlFlowGraphBuilder`: AST로부터 CFG 생성
- **분석 기능**:
  - 도달 가능성 분석
  - 역방향 포스트오더 순회
  - 루프 감지 (백 엣지 탐지)

### 4. DataFlowResult.cs
- **역할**: 분석 결과 모델
- **주요 클래스**:
  - `DataFlowResult`: 전체 분석 결과
  - `DataFlowStatistics`: 분석 통계
  - `LivenessAnalysisResult`: 활성 변수 분석 결과
  - `ReachingDefinitionsResult`: 도달 정의 분석 결과
  - `DefinitionPoint`: 정의 지점

### 5. DataFlowVisitor.cs
- **역할**: AST Visitor 패턴 구현
- **기능**:
  - AST를 순회하며 변수 정의 및 사용 추적
  - 스코프 관리 (PROGRAM, FB, FUNCTION)
  - 루프 및 조건부 컨텍스트 추적
  - VAR_INPUT, VAR_OUTPUT 등 IEC 61131-3 변수 스코프 처리

## 사용 예제

```csharp
using TwinCatQA.Infrastructure.Analysis.DataFlow;
using TwinCatQA.Domain.Models.AST;

// 1. Syntax Tree 파싱 (파서로부터 획득)
SyntaxTree syntaxTree = parser.Parse(sourceCode);

// 2. 데이터 흐름 분석 수행
IDataFlowAnalyzer analyzer = new DataFlowAnalyzer();
DataFlowResult result = analyzer.Analyze(syntaxTree);

// 3. 결과 조회
Console.WriteLine(result.GetSummary());

// 사용되지 않는 변수
foreach (var unused in result.UnusedVariables)
{
    Console.WriteLine($"[경고] {unused}");
}

// 초기화되지 않은 사용
foreach (var uninit in result.UninitializedUsages)
{
    Console.WriteLine($"[오류] {uninit}");
}

// 도달 불가능한 코드
foreach (var block in result.UnreachableBlocks)
{
    Console.WriteLine($"[경고] 도달 불가능한 블록: {block}");
}

// 특정 변수의 Def-Use Chain
var chain = result.GetChainForVariable("myVariable");
if (chain != null)
{
    Console.WriteLine($"변수 '{chain.VariableName}':");
    Console.WriteLine($"- 정의 횟수: {chain.Definitions.Count}");
    Console.WriteLine($"- 사용 횟수: {chain.Usages.Count}");
    Console.WriteLine($"- 초기화 여부: {chain.IsInitialized}");
}
```

## 분석 결과 해석

### 1. 사용되지 않는 변수 (UnusedVariable)
- **의미**: 선언되었지만 한 번도 사용되지 않는 변수
- **권장 사항**: 변수 제거 또는 코드 로직 검토
- **예외**: VAR_OUTPUT은 외부로 값을 전달하므로 제외

### 2. 초기화되지 않은 사용 (UninitializedUsage)
- **의미**: 값이 할당되기 전에 변수를 읽는 경우
- **위험도**: 높음 (예측 불가능한 동작 발생 가능)
- **권장 사항**: 변수 선언 시 초기값 지정 또는 사용 전 할당

### 3. 도달 불가능한 코드 (UnreachableBlocks)
- **의미**: 실행 흐름상 도달할 수 없는 코드 블록
- **원인**: RETURN 후 코드, 항상 거짓인 조건문 등
- **권장 사항**: 불필요한 코드 제거

### 4. 루프 정보 (LoopInfo)
- **의미**: FOR, WHILE, REPEAT 루프 구조
- **분석 항목**: 중첩 깊이, 루프 내 변수 할당
- **활용**: 성능 최적화, 무한 루프 검출

## 알고리즘 설명

### 1. Def-Use Chain 구축
1. AST를 순회하며 변수 선언 수집
2. 할당문에서 변수 정의(Definition) 기록
3. 표현식에서 변수 사용(Usage) 기록
4. 초기화 여부 추적 (선언 시 초기값 또는 첫 할당)

### 2. 제어 흐름 그래프 생성
1. 프로그램을 기본 블록(Basic Block)으로 분할
   - 기본 블록: 순차 실행되는 구문의 집합 (분기 없음)
2. 블록 간 선행자(Predecessor)/후속자(Successor) 연결
3. 조건문(IF, CASE)은 여러 후속자로 분기
4. 루프문(FOR, WHILE, REPEAT)은 백 엣지(Back Edge) 생성

### 3. 활성 변수 분석 (Liveness Analysis)
- **방향**: 역방향 데이터 흐름 분석
- **알고리즘**: Fixed-point Iteration
- **계산식**:
  ```
  Live-out[B] = ∪(후속자 블록들의 Live-in)
  Live-in[B] = Use[B] ∪ (Live-out[B] - Def[B])
  ```
- **활용**: 변수 생존 범위(Live Range) 계산, 레지스터 할당 최적화

### 4. 도달 가능성 분석
- **방법**: 깊이 우선 탐색 (DFS)
- **시작점**: Entry Block
- **결과**: 도달 가능한 블록 집합
- **차집합**: 전체 블록 - 도달 가능한 블록 = 도달 불가능한 블록

## IEC 61131-3 특화 처리

### 변수 스코프 처리
- **VAR_INPUT**: 외부에서 값 전달, 초기화됨으로 간주
- **VAR_OUTPUT**: 외부로 값 전달, 사용되지 않아도 경고 제외
- **VAR_IN_OUT**: 참조 전달, 초기화됨으로 간주
- **VAR**: 로컬 변수, 명시적 초기화 필요
- **VAR CONSTANT**: 상수, 초기값 필수

### FOR 루프 변수
- 루프 변수는 자동으로 초기화됨
- 루프 변수 정의는 루프 내부 할당으로 표시

### Function Block 인스턴스
- FB 인스턴스는 상태를 유지하므로 별도 처리 필요
- 메서드 호출은 함수 호출과 동일하게 처리

## 성능 고려사항

### 시간 복잡도
- **CFG 생성**: O(n), n = AST 노드 수
- **Def-Use Chain**: O(n)
- **활성 변수 분석**: O(블록 수 × 변수 수 × 반복 횟수)
  - 일반적으로 3-5회 반복으로 수렴
  - 최악의 경우 O(n³)

### 공간 복잡도
- **CFG**: O(블록 수 + 간선 수)
- **Def-Use Chain**: O(변수 수 × (정의 수 + 사용 수))
- **활성 변수 집합**: O(블록 수 × 변수 수)

### 최적화 기법
- 역방향 포스트오더 순회로 반복 횟수 감소
- 블록별 Def/Use 집합 캐싱
- 변경되지 않은 블록은 재계산 생략

## 확장 가능성

### 추가 가능한 분석
1. **도달 정의 분석 (Reaching Definitions)**
   - 특정 위치에서 변수의 가능한 정의들 추적
   - 상수 전파(Constant Propagation) 기반

2. **사용 가능한 표현식 (Available Expressions)**
   - 공통 부분식 제거(CSE) 최적화

3. **Very Busy Expressions**
   - 코드 이동 최적화

4. **포인터 분석 (Pointer Analysis)**
   - IEC 61131-3의 POINTER 타입 지원 시

5. **인터프로시저 분석 (Interprocedural Analysis)**
   - 함수/FB 간 데이터 흐름 추적

## 테스트 시나리오

### 1. 초기화되지 않은 사용 감지
```iecst
VAR
    counter : INT;  // 초기값 없음
END_VAR

IF counter > 10 THEN  // 오류: counter 초기화 전 사용
    counter := 0;
END_IF
```

### 2. 사용되지 않는 변수 감지
```iecst
VAR
    temp : REAL := 0.0;  // 경고: 변수 미사용
    result : REAL;
END_VAR

result := 100.0;  // temp는 사용되지 않음
```

### 3. 도달 불가능한 코드 감지
```iecst
IF FALSE THEN
    // 경고: 도달 불가능한 블록
    DoSomething();
END_IF

RETURN;
DoSomethingElse();  // 경고: RETURN 후 도달 불가능
```

### 4. 루프 내 변수 추적
```iecst
VAR
    i : INT;
    sum : INT := 0;
END_VAR

FOR i := 1 TO 10 DO
    sum := sum + i;  // 루프 내 할당 추적
END_FOR
```

## 통합 방법

### Level 1 분석기와 통합
```csharp
// Level 1: 기본 정적 분석
var level1Result = basicAnalyzer.Analyze(syntaxTree);

// Level 2: 데이터 흐름 분석
var level2Result = dataFlowAnalyzer.Analyze(syntaxTree);

// 결과 병합
var combinedIssues = new List<QAIssue>();
combinedIssues.AddRange(level1Result.Issues);

// Level 2 결과를 QAIssue로 변환
foreach (var uninit in level2Result.UninitializedUsages)
{
    combinedIssues.Add(new QAIssue
    {
        RuleId = "DF001",
        Severity = ViolationSeverity.High,
        Message = uninit.Message,
        Line = uninit.Usage.Line,
        Column = uninit.Usage.Column,
        FilePath = uninit.Usage.FilePath
    });
}
```

## 참고 자료

- **용어 정의**:
  - CFG (Control Flow Graph): 제어 흐름 그래프
  - Basic Block: 기본 블록, 순차 실행되는 구문 집합
  - Def-Use Chain: 정의-사용 체인
  - Liveness Analysis: 활성 변수 분석
  - Reaching Definitions: 도달 정의 분석

- **관련 논문/서적**:
  - "Compilers: Principles, Techniques, and Tools" (Dragon Book)
  - "Engineering a Compiler" by Cooper and Torczon
  - IEC 61131-3 표준 문서

## 버전 이력

- **v1.0.0** (2024-12-01)
  - 초기 구현
  - Def-Use Chain 분석
  - CFG 생성 및 도달 가능성 분석
  - 활성 변수 분석 (Liveness Analysis)
  - 루프 감지
  - IEC 61131-3 변수 스코프 지원
