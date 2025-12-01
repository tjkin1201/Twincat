# TwinCAT QA 규칙 레퍼런스

TwinCAT 코드 품질 검증 도구에서 사용하는 200개의 QA 규칙에 대한 상세 설명입니다.

## 목차

- [개요](#개요)
- [심각도 레벨](#심각도-레벨)
- [기본 QA 규칙 (20개)](#기본-qa-규칙-20개)
- [TE1200 Static Analysis 규칙 (180개)](#te1200-static-analysis-규칙-180개)
- [규칙 설정 방법](#규칙-설정-방법)

---

## 개요

| 구분 | 개수 | 설명 |
|------|------|------|
| 기본 QA 규칙 | 20개 | TwinCAT 프로젝트를 위한 핵심 품질 규칙 |
| TE1200 규칙 | 180개 | Beckhoff TE1200 Static Analysis 호환 규칙 |
| **총계** | **200개** | |

---

## 심각도 레벨

| 레벨 | 아이콘 | 설명 | 조치 |
|------|--------|------|------|
| **Critical** | 🔴 | 런타임 오류, 데이터 손실, 안전 문제 발생 가능 | 반드시 수정 필요 |
| **Warning** | 🟡 | 잠재적 버그, 유지보수 문제 발생 가능 | 수정 권장 |
| **Info** | 🔵 | 코드 스타일, 가독성 개선 권장사항 | 선택적 수정 |

---

## 기본 QA 규칙 (20개)

### 타입 안전성 (Type Safety)

#### QA001: 타입 축소 감지 🔴 Critical

**설명**: DINT → INT, LINT → DINT 등 값 범위가 줄어드는 타입 변경 감지

**위험성**:
- 기존 값이 새 타입의 범위를 초과하면 예측 불가능한 값으로 변환
- 오버플로우로 인한 시스템 오동작

**예시**:
```iecst
// ❌ 위험: 타입 축소
nValue : DINT := 50000;
nValue : INT := 50000;  // 오버플로우 발생!

// ✅ 안전: 범위 체크 후 변환
IF nValue >= -32768 AND nValue <= 32767 THEN
    nSmallValue := DINT_TO_INT(nValue);
ELSE
    bError := TRUE;
END_IF
```

---

#### QA004: NULL 포인터 검사 누락 🔴 Critical

**설명**: POINTER/REFERENCE 타입 역참조 전 NULL 체크 누락 감지

**위험성**:
- PLC 런타임 크래시
- 예측 불가능한 메모리 접근
- 안전 시스템 기능 상실

**예시**:
```iecst
// ❌ 위험: NULL 체크 없음
value := pData^;

// ✅ 안전: NULL 체크 후 사용
IF pData <> NULL THEN
    value := pData^;
ELSE
    bError := TRUE;
END_IF

// REFERENCE 타입의 경우
IF __ISVALIDREF(refData) THEN
    value := refData.member;
END_IF
```

---

### 코드 품질 (Code Quality)

#### QA002: 매직 넘버 사용 🟡 Warning

**설명**: 의미 없는 리터럴 숫자 사용 감지

**예시**:
```iecst
// ❌ 나쁜 예
IF nCount > 100 THEN  // 100이 무엇을 의미?

// ✅ 좋은 예
VAR_CONSTANT
    MAX_COUNT : INT := 100;
END_VAR
IF nCount > MAX_COUNT THEN
```

---

#### QA003: 긴 함수 🟡 Warning

**설명**: 지나치게 긴 함수/프로그램 감지 (기본: 200줄 초과)

**권장**: 함수를 더 작은 단위로 분리

---

#### QA005: 깊은 중첩 🟡 Warning

**설명**: IF/FOR/WHILE 등의 깊은 중첩 감지 (기본: 4단계 초과)

**예시**:
```iecst
// ❌ 나쁜 예: 5단계 중첩
IF a THEN
    IF b THEN
        FOR i := 1 TO 10 DO
            WHILE c DO
                IF d THEN  // 5단계!

// ✅ 좋은 예: 함수 분리로 중첩 감소
IF NOT CheckConditions() THEN
    RETURN;
END_IF
ProcessData();
```

---

#### QA006: 미사용 변수 🔵 Info

**설명**: 선언되었지만 사용되지 않는 변수 감지

---

#### QA007: 미초기화 변수 🔴 Critical

**설명**: 초기화 없이 사용되는 변수 감지

**예시**:
```iecst
// ❌ 위험: 초기화 없이 사용
VAR
    nValue : INT;
END_VAR
nResult := nValue + 10;  // nValue가 초기화되지 않음!

// ✅ 안전: 명시적 초기화
VAR
    nValue : INT := 0;
END_VAR
```

---

#### QA008: 주석 부족 🔵 Info

**설명**: 코드 대비 주석이 부족한 경우 감지 (기본: 10% 미만)

---

#### QA009: 명명 규칙 위반 🔵 Info

**설명**: 변수/함수 명명 규칙 위반 감지

**권장 규칙**:
| 유형 | 접두사 예시 |
|------|------------|
| BOOL | b, is, has |
| INT/DINT | n, i |
| REAL | f, r |
| STRING | s, str |
| POINTER | p |
| ARRAY | a, arr |
| Function Block | FB_ |
| ENUM | E_ |

---

#### QA010: 높은 복잡도 🟡 Warning

**설명**: 순환 복잡도(Cyclomatic Complexity)가 높은 함수 감지 (기본: 10 초과)

---

#### QA011: 중복 코드 🟡 Warning

**설명**: 유사한 코드 블록 반복 감지

---

#### QA012: 전역 변수 과다 사용 🟡 Warning

**설명**: 전역 변수 과다 사용 감지 (기본: 50개 초과)

---

#### QA013: 과다 파라미터 🟡 Warning

**설명**: 함수/FB의 파라미터가 너무 많은 경우 감지 (기본: 7개 초과)

---

#### QA014: 부동소수점 비교 🔴 Critical

**설명**: REAL/LREAL 타입의 직접 비교(=, <>) 감지

**예시**:
```iecst
// ❌ 위험: 직접 비교
IF fValue = 1.5 THEN  // 부동소수점 오차로 실패 가능!

// ✅ 안전: 허용 오차 사용
VAR_CONSTANT
    EPSILON : REAL := 0.0001;
END_VAR
IF ABS(fValue - 1.5) < EPSILON THEN
```

---

#### QA015: 배열 경계 🔴 Critical

**설명**: 배열 인덱스 범위 초과 가능성 감지

---

#### QA016: 무한 루프 위험 🔴 Critical

**설명**: 종료 조건이 없거나 불명확한 루프 감지

---

#### QA017: 하드코딩된 I/O 주소 🟡 Warning

**설명**: 직접 I/O 주소 사용 감지

**예시**:
```iecst
// ❌ 나쁜 예
bOutput AT %QX0.0 : BOOL;  // 하드코딩된 주소

// ✅ 좋은 예: I/O 매핑 테이블 사용
bOutput : BOOL;  // 심볼릭 변수, TwinCAT에서 매핑
```

---

#### QA018: CASE ELSE 누락 🟡 Warning

**설명**: CASE 문에 ELSE 절이 없는 경우 감지

**예시**:
```iecst
// ❌ 경고: ELSE 누락
CASE nState OF
    1: DoState1();
    2: DoState2();
END_CASE  // 3, 4, ... 처리 안됨!

// ✅ 안전: ELSE 추가
CASE nState OF
    1: DoState1();
    2: DoState2();
ELSE
    HandleUnknownState();
END_CASE
```

---

#### QA019: 일관성 없는 스타일 🔵 Info

**설명**: 코드 스타일 불일치 감지 (들여쓰기, 괄호 등)

---

#### QA020: 과도하게 긴 이름 🔵 Info

**설명**: 변수/함수 이름이 너무 긴 경우 감지 (기본: 50자 초과)

---

## TE1200 Static Analysis 규칙 (180개)

Beckhoff TE1200 Static Analysis와 호환되는 SA 규칙입니다.

### 카테고리별 분류

| 카테고리 | 설명 | 규칙 수 |
|----------|------|---------|
| UnreachableUnusedCode | 도달 불가능/미사용 코드 | ~30 |
| Conversions | 타입 변환 | ~20 |
| Operations | 연산 | ~25 |
| VariablesAndConstants | 변수/상수 | ~20 |
| Declarations | 선언 | ~15 |
| Initialization | 초기화 | ~10 |
| Concurrency | 멀티태스킹 | ~15 |
| ObjectOriented | 객체지향 | ~10 |
| NamingConventions | 명명 규칙 | ~10 |
| Metrics | 복잡도 메트릭 | ~10 |
| Comments | 주석 | ~5 |
| Safety | 안전성 | ~10 |

---

### SA0001-SA0030: 기본 검사

| ID | 이름 | 심각도 | 설명 |
|----|------|--------|------|
| SA0001 | UnreachableCode | Warning | RETURN/EXIT 이후 도달 불가능한 코드 |
| SA0002 | EmptyObjects | Warning | 빈 POU, 빈 메서드 감지 |
| SA0003 | EmptyStatements | Info | 빈 문장(;;;) 감지 |
| SA0004 | MultipleWriteOnOutput | Critical | 한 사이클에서 출력 변수 다중 쓰기 |
| SA0006 | MultiTaskWriteAccess | Critical | 멀티태스크에서 동일 변수 쓰기 접근 |
| SA0007 | AddressOfConstant | Warning | 상수의 주소 참조 시도 |
| SA0008 | SubrangeTypeCheck | Warning | 서브레인지 타입 범위 검사 |
| SA0009 | UnusedReturnValue | Warning | 함수 반환값 미사용 |
| SA0010 | SingleElementArray | Info | 단일 요소 배열 (불필요) |
| SA0011 | SingleMemberEnum | Info | 단일 멤버 열거형 (불필요) |
| SA0012 | VariableCouldBeConstant | Info | 상수로 변환 가능한 변수 |
| SA0013 | SameVariableName | Warning | 동일 이름의 지역/전역 변수 |
| SA0014 | InstanceAssignment | Warning | FB 인스턴스 직접 할당 |
| SA0015 | GlobalAccessInFBInit | Warning | FB_Init에서 전역 변수 접근 |
| SA0016 | GapsInStructures | Info | 구조체 내 메모리 갭 |
| SA0017 | IrregularPointerAssignment | Critical | 비정상적인 포인터 할당 |
| SA0018 | UnusualBitAccess | Warning | 비정상적인 비트 접근 |
| SA0019 | ImplicitPointerConversion | Warning | 암시적 포인터 변환 |
| SA0020 | TruncatedRealAssignment | Warning | REAL 값 잘림 할당 |
| SA0021 | AddressOfTemporary | Critical | 임시 변수의 주소 참조 |
| SA0022 | NonRejectedReturnValue | Info | 반환값 무시 (명시적이지 않음) |
| SA0023 | ComplexReturnValue | Warning | 복잡한 반환값 표현식 |
| SA0024 | UntypedLiterals | Info | 타입 없는 리터럴 |
| SA0025 | UnqualifiedEnumConstants | Info | 정규화되지 않은 열거형 상수 |
| SA0026 | UseOfDirectAddresses | Warning | 직접 주소 사용 (%IX, %QX) |
| SA0027 | UnsafeTypeConversion | Critical | 안전하지 않은 타입 변환 |
| SA0028 | NestedComments | Info | 중첩된 주석 |
| SA0029 | TODO_Comments | Info | TODO/FIXME 주석 |
| SA0030 | MissingErrorHandling | Warning | 에러 처리 누락 |

---

### SA0031-SA0050: 미사용/연산/매직넘버

| ID | 이름 | 심각도 | 설명 |
|----|------|--------|------|
| SA0031 | UnusedSignatures | Info | 미사용 메서드 시그니처 |
| SA0032 | UnusedEnumConstants | Info | 미사용 열거형 상수 |
| SA0033 | UnusedVariables | Info | 미사용 변수 (VAR) |
| SA0034 | UnusedInputVariables | Warning | 미사용 입력 변수 |
| SA0035 | UnusedOutputVariables | Warning | 미사용 출력 변수 |
| SA0036 | UnusedInOutVariables | Warning | 미사용 VAR_IN_OUT 변수 |
| SA0037 | UnusedTempVariables | Info | 미사용 VAR_TEMP 변수 |
| SA0038 | WriteOnlyVariables | Warning | 쓰기만 하는 변수 |
| SA0039 | ReadOnlyAsVariable | Info | VAR_INPUT을 VAR로 변경 가능 |
| SA0040 | DivisionByZero | Critical | 0으로 나누기 |
| SA0041 | LoopInvariantCode | Info | 루프 불변 코드 |
| SA0042 | InconsistentNamespaceAccess | Warning | 일관성 없는 네임스페이스 접근 |
| SA0043 | SuspiciousSemicolon | Warning | 의심스러운 세미콜론 |
| SA0044 | ParenthesisMismatch | Critical | 괄호 불일치 |
| SA0045 | AssignmentInCondition | Warning | 조건문 내 할당 |
| SA0046 | UnnecessaryComparison | Info | 불필요한 비교 (TRUE = TRUE) |
| SA0047 | DuplicateCondition | Warning | 중복 조건 |
| SA0048 | InefficientStringConcat | Info | 비효율적 문자열 연결 |
| SA0049 | MagicNumbers | Warning | 매직 넘버 사용 |
| SA0050 | ComplexExpression | Warning | 복잡한 표현식 |

---

### SA0051-SA0070: 메트릭/주석/포인터

| ID | 이름 | 심각도 | 설명 |
|----|------|--------|------|
| SA0051 | FunctionTooLong | Warning | 함수가 너무 김 (>200줄) |
| SA0052 | TooManyParameters | Warning | 파라미터가 너무 많음 (>7개) |
| SA0053 | NestingTooDeep | Warning | 중첩이 너무 깊음 (>4레벨) |
| SA0054 | CyclomaticComplexity | Warning | 순환 복잡도 초과 (>10) |
| SA0055 | CognitiveComplexity | Warning | 인지 복잡도 초과 |
| SA0056 | InsufficientComments | Info | 주석 부족 |
| SA0057 | MissingHeaderComment | Info | 헤더 주석 누락 |
| SA0058 | OutdatedComments | Warning | 오래된 주석 (코드와 불일치) |
| SA0059 | CommentedOutCode | Info | 주석 처리된 코드 |
| SA0060 | IneffectiveOperation | Warning | 효과 없는 연산 (x := x) |
| SA0061 | SuspiciousPointerOperation | Critical | 의심스러운 포인터 연산 |
| SA0062 | ConstantCondition | Warning | 상수 조건 (IF TRUE) |
| SA0063 | FloatEquality | Critical | 부동소수점 직접 비교 |
| SA0064 | SuspiciousPointerArithmetic | Critical | 의심스러운 포인터 산술 |
| SA0065 | UninitializedVariable | Critical | 미초기화 변수 사용 |
| SA0066 | ArrayOutOfBounds | Critical | 배열 범위 초과 |
| SA0067 | GlobalInFunction | Warning | 함수 내 전역 변수 접근 |
| SA0068 | CircularReference | Critical | 순환 참조 |
| SA0069 | UnimplementedInterface | Warning | 미구현 인터페이스 |
| SA0070 | EmptyCaseBranch | Warning | 빈 CASE 분기 |

---

### SA0071-SA0100: 명명/타입/초기화

| ID | 이름 | 심각도 | 설명 |
|----|------|--------|------|
| SA0071 | MissingElse | Warning | IF에 ELSE 누락 |
| SA0072 | CaseMissingDefault | Warning | CASE에 ELSE 누락 |
| SA0073 | VariableNamingViolation | Info | 변수 명명 규칙 위반 |
| SA0074 | FBNamingViolation | Info | FB 명명 규칙 위반 (FB_ 접두사) |
| SA0075 | InterfaceNamingViolation | Info | 인터페이스 명명 규칙 위반 (I_ 접두사) |
| SA0076 | EnumNamingViolation | Info | 열거형 명명 규칙 위반 (E_ 접두사) |
| SA0077 | StructNamingViolation | Info | 구조체 명명 규칙 위반 (ST_ 접두사) |
| SA0078 | ConstantNamingViolation | Info | 상수 명명 규칙 위반 (대문자) |
| SA0079 | GlobalVarNamingViolation | Info | 전역 변수 명명 규칙 위반 (g 접두사) |
| SA0080 | ImplicitConversion | Warning | 암시적 타입 변환 |
| SA0081 | DangerousConversion | Critical | 위험한 타입 변환 |
| SA0082 | SignedUnsignedConversion | Warning | 부호 있음/없음 변환 |
| SA0083 | StringLengthOverflow | Critical | 문자열 길이 초과 |
| SA0084 | TimerCounterNotReset | Warning | 타이머/카운터 미리셋 |
| SA0085 | PersistentInitialization | Warning | PERSISTENT 변수 초기화 |
| SA0086 | RetainVariableWarning | Info | RETAIN 변수 경고 |
| SA0087 | AtDirectiveWarning | Warning | AT 지시어 사용 |
| SA0088 | VarAccessUsage | Info | VAR_ACCESS 사용 |
| SA0089 | AttributeUsage | Info | 속성(Attribute) 사용 |
| SA0090 | PragmaUsage | Info | Pragma 사용 |
| SA0091 | DuplicateTypeDefinition | Warning | 중복 타입 정의 |
| SA0092 | CircularTypeDependency | Critical | 순환 타입 의존성 |
| SA0093 | NonStandardDataType | Info | 비표준 데이터 타입 |
| SA0094 | ExitStatement | Info | EXIT 문 사용 |
| SA0095 | ContinueStatement | Info | CONTINUE 문 사용 |
| SA0096 | JmpStatement | Warning | JMP 문 사용 (비권장) |
| SA0097 | EmptyLoop | Warning | 빈 루프 |
| SA0098 | PotentialInfiniteLoop | Critical | 잠재적 무한 루프 |
| SA0099 | ForLoopVariableModification | Critical | FOR 루프 변수 수정 |
| SA0100 | ImproperSizeOf | Warning | SIZEOF 부적절 사용 |

---

### SA0101-SA0130: 고급검사/OOP/동시성

| ID | 이름 | 심각도 | 설명 |
|----|------|--------|------|
| SA0101 | UnusedLibraryReference | Info | 미사용 라이브러리 참조 |
| SA0102 | InefficientArrayInit | Info | 비효율적 배열 초기화 |
| SA0103 | ExcessiveVariableScope | Warning | 과도한 변수 범위 |
| SA0104 | UnsafeMemcpy | Critical | 안전하지 않은 MEMCPY |
| SA0105 | RecursiveCall | Warning | 재귀 호출 |
| SA0106 | DynamicMemory | Warning | 동적 메모리 사용 |
| SA0107 | OutputInitInFbInit | Warning | FB_Init에서 출력 초기화 |
| SA0108 | MissingSuperCall | Warning | SUPER 호출 누락 |
| SA0109 | ThisPointerStorage | Critical | THIS 포인터 저장 |
| SA0110 | NonVirtualOverride | Warning | 비가상 메서드 오버라이드 |
| SA0111 | InterfaceSegregation | Info | 인터페이스 분리 원칙 위반 |
| SA0112 | SingleResponsibility | Info | 단일 책임 원칙 위반 |
| SA0113 | HighCoupling | Warning | 높은 결합도 |
| SA0114 | LowCohesion | Warning | 낮은 응집도 |
| SA0115 | HardcodedIP | Warning | 하드코딩된 IP 주소 |
| SA0116 | HardcodedPath | Warning | 하드코딩된 파일 경로 |
| SA0117 | BitOperationPrecedence | Warning | 비트 연산 우선순위 |
| SA0118 | IntegerOverflow | Critical | 정수 오버플로우 |
| SA0119 | TimeOperation | Warning | TIME 연산 주의 |
| SA0120 | StringWstringMix | Warning | STRING/WSTRING 혼용 |
| SA0121 | EnumRangeOverflow | Warning | 열거형 범위 초과 |
| SA0122 | NestedStructDepth | Warning | 중첩 구조체 깊이 |
| SA0123 | UnsafeCast | Critical | 안전하지 않은 캐스트 |
| SA0124 | MultipleInheritance | Warning | 다중 상속 주의 |
| SA0125 | PropertyMisuse | Warning | 속성 오용 |
| SA0126 | StringBufferSize | Warning | 문자열 버퍼 크기 |
| SA0127 | ArraySizeMismatch | Critical | 배열 크기 불일치 |
| SA0128 | ActionMisuse | Warning | 액션 오용 |
| SA0129 | FbReinitUsage | Warning | FB_reinit 사용 |
| SA0130 | DirectIOAccess | Warning | 직접 I/O 접근 |

---

### SA0131-SA0160: 안전/병렬/IEC

| ID | 이름 | 심각도 | 설명 |
|----|------|--------|------|
| SA0131 | UnsafePointerDereference | Critical | 안전하지 않은 포인터 역참조 |
| SA0132 | ArrayIndexValidation | Critical | 배열 인덱스 검증 필요 |
| SA0133 | FloatLoopCounter | Warning | 부동소수점 루프 카운터 |
| SA0134 | MissingUnitTest | Info | 단위 테스트 누락 |
| SA0135 | FixmeComment | Warning | FIXME 주석 발견 |
| SA0136 | DangerousCast | Critical | 위험한 캐스트 |
| SA0137 | RedundantConditionCheck | Info | 중복 조건 검사 |
| SA0138 | BooleanLiteralReturn | Info | BOOL 리터럴 반환 |
| SA0139 | EmptyExceptionHandler | Warning | 빈 예외 처리기 |
| SA0140 | TooManyReturns | Warning | 너무 많은 RETURN 문 |
| SA0141 | SharedVariable | Critical | 공유 변수 (멀티태스크) |
| SA0142 | SemaphoreUsage | Info | 세마포어 사용 |
| SA0143 | TaskPriority | Warning | 태스크 우선순위 주의 |
| SA0144 | BlockingCall | Critical | 블로킹 호출 |
| SA0145 | SpinLockPattern | Warning | 스핀락 패턴 |
| SA0146 | AtomicOperationNeeded | Critical | 원자적 연산 필요 |
| SA0147 | CycleTimeRisk | Warning | 사이클 시간 위험 |
| SA0148 | WatchdogConsideration | Info | 워치독 고려 필요 |
| SA0149 | DeadlockRisk | Critical | 데드락 위험 |
| SA0150 | InterruptDisable | Warning | 인터럽트 비활성화 |
| SA0151 | PLCopenFBRule | Info | PLCopen FB 규칙 |
| SA0152 | IECTypeSize | Info | IEC 타입 크기 |
| SA0153 | DirectAddressNotation | Warning | 직접 주소 표기법 |
| SA0154 | LanguageCompatibility | Info | 언어 호환성 |
| SA0155 | VarConfigUsage | Warning | VAR_CONFIG 사용 |
| SA0156 | UseStandardLibrary | Info | 표준 라이브러리 사용 권장 |
| SA0157 | BitAccessNotation | Info | 비트 접근 표기법 |
| SA0158 | DataTypeRangeDoc | Info | 데이터 타입 범위 문서화 |
| SA0159 | UnitConsistency | Warning | 단위 일관성 |
| SA0160 | ProgramStructureComplexity | Warning | 프로그램 구조 복잡도 |

---

### SA0161-SA0180: 고급분석/성능/문서화

| ID | 이름 | 심각도 | 설명 |
|----|------|--------|------|
| SA0161 | CircularDependency | Critical | 순환 의존성 |
| SA0162 | ModuleSizeExceeded | Warning | 모듈 크기 초과 |
| SA0163 | ConditionalCompilation | Info | 조건부 컴파일 |
| SA0164 | DuplicateConstants | Info | 중복 상수 |
| SA0165 | IncompleteInitialization | Warning | 불완전한 초기화 |
| SA0166 | MemoryAlignment | Info | 메모리 정렬 |
| SA0167 | ComplexInheritance | Warning | 복잡한 상속 구조 |
| SA0168 | HardcodedTiming | Warning | 하드코딩된 타이밍 |
| SA0169 | IncompleteImplementation | Warning | 불완전한 구현 |
| SA0170 | UnusedUsing | Info | 미사용 USING |
| SA0171 | SafetyVariableProtection | Critical | 안전 변수 보호 필요 |
| SA0172 | DangerousOperationOrder | Critical | 위험한 연산 순서 |
| SA0173 | InfiniteRetry | Critical | 무한 재시도 |
| SA0174 | ExpensiveOperation | Warning | 비용이 큰 연산 |
| SA0175 | CacheInefficientAccess | Info | 캐시 비효율적 접근 |
| SA0176 | StringOperationOptimization | Info | 문자열 연산 최적화 |
| SA0177 | BitOperationOptimization | Info | 비트 연산 최적화 |
| SA0178 | ResourceLeak | Critical | 리소스 누수 |
| SA0179 | StateMachineCompleteness | Warning | 상태 머신 완전성 |
| SA0180 | DocumentationLevel | Info | 문서화 수준 |

---

## 규칙 설정 방법

### .twincat-qa.json 설정 파일

```json
{
    "version": "2.0",
    "projectName": "MyProject",
    "globalExclusions": {
        "files": ["**/Generated/**"],
        "rules": ["SA0029"]
    },
    "ruleOverrides": {
        "SA0049": {
            "enabled": true,
            "severity": "Info"
        },
        "QA003": {
            "enabled": true,
            "parameters": {
                "maxLines": 300
            }
        }
    },
    "inlineSuppressions": {
        "enabled": true,
        "commentPatterns": [
            "// qa-ignore: {ruleId}",
            "(* qa-ignore: {ruleId} *)"
        ]
    }
}
```

### 인라인 억제

```iecst
// 특정 규칙 억제
// qa-ignore: SA0049
nMagicNumber := 42;

(* 여러 규칙 억제 *)
(* qa-ignore: SA0033, SA0056 *)
VAR
    unusedVar : INT;  // 경고 억제됨
END_VAR
```

### CLI 옵션

```bash
# 특정 규칙만 실행
twincat-qa qa old new --rules SA0001,SA0040,QA001

# 특정 규칙 제외
twincat-qa qa old new --exclude-rules SA0029,SA0056

# 최소 심각도 설정
twincat-qa qa old new --min-severity warning
```

---

## 참고 자료

- [Beckhoff TE1200 Static Analysis](https://infosys.beckhoff.com/english.php?content=../content/1033/te1200_tc3_plcstaticanalysis/index.html)
- [IEC 61131-3 표준](https://www.plcopen.org/)
- [PLCopen 코딩 가이드라인](https://www.plcopen.org/technical-activities/coding-guidelines)

---

*이 문서는 TwinCAT QA 코드 분석 도구 v1.0 기준으로 작성되었습니다.*
