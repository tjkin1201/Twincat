# TwinCAT QA 분석 결과 해석 가이드

## 개요

이 문서는 TwinCAT QA 도구가 제공하는 분석 결과를 개발자가 이해하고, 왜 해당 이슈를 수정해야 하는지 설명합니다.

**핵심 원칙**: QA 도구는 코드가 "동작하는지"가 아니라 "안전하고 유지보수 가능한지"를 검사합니다.

---

## 목차

1. [왜 QA 분석이 필요한가?](#왜-qa-분석이-필요한가)
2. [심각도 수준 이해하기](#심각도-수준-이해하기)
3. [주요 이슈 카테고리별 설명](#주요-이슈-카테고리별-설명)
4. [실제 사고 사례](#실제-사고-사례)
5. [이슈 해결 우선순위](#이슈-해결-우선순위)
6. [자주 묻는 질문](#자주-묻는-질문)

---

## 왜 QA 분석이 필요한가?

### PLC 코드의 특수성

PLC 코드는 일반 소프트웨어와 다릅니다:

| 구분 | 일반 소프트웨어 | PLC 코드 |
|------|----------------|----------|
| 실행 환경 | 서버/PC | 실시간 제어 시스템 |
| 버그 영향 | 서비스 중단 | **물리적 손상, 인명 피해** |
| 디버깅 | 로그 분석, 재현 가능 | 현장에서만 재현, 로그 제한적 |
| 수정 비용 | 배포 후 패치 | **라인 정지, 생산 손실** |

### 코드 변경의 위험성

```
변경 전 (6개월간 안정 운영)     변경 후 (테스트 통과)
         ↓                            ↓
   "문제없이 동작함"              "로직은 동일함"
                                      ↓
                              특정 조건에서 오동작
                                      ↓
                              ❌ 장비 충돌 / 제품 불량
```

**QA 도구는 이 "특정 조건"을 사전에 탐지합니다.**

---

## 심각도 수준 이해하기

### 심각도 정의

| 심각도 | 의미 | 조치 기한 | 예시 |
|--------|------|----------|------|
| 🔴 **Critical** | 즉시 사고 가능 | **배포 전 필수 수정** | 타입 오버플로우, 무한 루프 |
| 🟠 **Warning** | 특정 조건에서 오동작 | 1주 내 수정 | 초기화 누락, 경계값 오류 |
| 🟡 **Info** | 유지보수 어려움 | 다음 스프린트 | 복잡도 높음, 명명규칙 위반 |

### 심각도별 실제 영향

#### 🔴 Critical - 즉시 조치 필요

```iecst
// QA001: 타입 축소로 인한 데이터 손실
// 변경 전
nPosition : DINT := 50000;  // -2,147,483,648 ~ 2,147,483,647

// 변경 후 (Critical 이슈!)
nPosition : INT := 50000;   // -32,768 ~ 32,767
                            // ❌ 32,767을 초과하면 오버플로우!
```

**실제 영향**:
- 위치값이 32,767을 초과하면 -32,768로 점프
- 모터가 반대 방향으로 급격히 이동
- **장비 충돌 또는 작업자 부상 위험**

#### 🟠 Warning - 조기 조치 필요

```iecst
// QA006: 초기화되지 않은 변수 사용
VAR
    bFirstRun : BOOL;  // 초기값 없음 (Warning!)
END_VAR

IF bFirstRun THEN      // ❌ 첫 사이클에서 예측 불가
    InitializeSystem();
END_IF
```

**실제 영향**:
- PLC 재시작 시 변수값이 이전 상태 유지 또는 임의값
- 콜드 스타트 vs 웜 스타트에서 다른 동작
- **간헐적 오동작으로 원인 파악 어려움**

#### 🟡 Info - 개선 권장

```iecst
// QA017: 높은 순환 복잡도 (복잡도 25)
// 복잡한 상태 머신 로직
IF cond1 THEN
    IF cond2 AND cond3 THEN
        CASE state OF
            0: IF a OR b THEN ...
            1: FOR i := 0 TO 10 DO
                  IF sensor[i] THEN ...
               END_FOR
            // ... 많은 분기
        END_CASE
    END_IF
END_IF
```

**실제 영향**:
- 새 기능 추가 시 버그 발생률 높음
- 코드 리뷰 시 누락 가능성 증가
- **유지보수 비용 증가**

---

## 주요 이슈 카테고리별 설명

### 1. 타입 안전성 (Type Safety)

#### 왜 중요한가?

PLC는 고정 크기 메모리를 사용합니다. 타입 변경은 곧 메모리 레이아웃 변경입니다.

```
DINT (32비트)                    INT (16비트)
┌────────────────────────────┐   ┌──────────────┐
│      2,147,483,647         │ → │   32,767     │  ← 오버플로우!
└────────────────────────────┘   └──────────────┘
```

#### 관련 규칙

| 규칙 | 설명 | 위험성 |
|------|------|--------|
| QA001 | 타입 축소 감지 | 데이터 손실, 오버플로우 |
| QA002 | 부호 변경 감지 | 음수값 오류 |
| SA0033 | 암시적 포인터 변환 | 메모리 접근 오류 |

#### 올바른 대응

```iecst
// ❌ 위험: 직접 타입 변경
nValue : INT := DINT_TO_INT(nLargeValue);

// ✅ 안전: 범위 체크 후 변환
IF nLargeValue >= -32768 AND nLargeValue <= 32767 THEN
    nValue := DINT_TO_INT(nLargeValue);
ELSE
    bRangeError := TRUE;
    nValue := 32767;  // 또는 적절한 기본값
END_IF
```

---

### 2. 초기화 및 변수 관리

#### 왜 중요한가?

PLC의 변수 초기화는 시작 모드에 따라 다릅니다:

| 시작 모드 | RETAIN 변수 | 일반 변수 |
|-----------|-------------|-----------|
| 콜드 스타트 | 초기값으로 | 초기값으로 |
| 웜 스타트 | **이전값 유지** | 초기값으로 |
| 리셋 | 0 또는 랜덤 | 0 또는 랜덤 |

#### 관련 규칙

| 규칙 | 설명 | 위험성 |
|------|------|--------|
| QA006 | 초기화 누락 변수 | 예측 불가 동작 |
| QA012 | 미사용 변수 | 코드 혼란 |
| SA0020 | 상수 변수 재할당 | 의도치 않은 변경 |

#### 올바른 대응

```iecst
// ❌ 위험: 초기화 없음
VAR
    bMachineReady : BOOL;
    nCycleCount : INT;
END_VAR

// ✅ 안전: 명시적 초기화
VAR
    bMachineReady : BOOL := FALSE;
    nCycleCount : INT := 0;
END_VAR

// 또는 초기화 로직 사용
IF bFirstCycle THEN
    bMachineReady := FALSE;
    nCycleCount := 0;
    bFirstCycle := FALSE;
END_IF
```

---

### 3. 배열 및 메모리 접근

#### 왜 중요한가?

PLC에서 배열 경계 초과는 다른 변수를 덮어쓸 수 있습니다.

```
메모리 레이아웃:
┌─────────────────────────────────────────┐
│ arrData[0] │ arrData[1] │ nImportant   │
├─────────────────────────────────────────┤
│    100     │    200     │   12345      │
└─────────────────────────────────────────┘
                              ↑
            arrData[2] := 999; // ❌ nImportant 덮어씀!
```

#### 관련 규칙

| 규칙 | 설명 | 위험성 |
|------|------|--------|
| QA003 | 배열 경계 검사 | 메모리 손상 |
| QA004 | 포인터 NULL 검사 | 시스템 크래시 |
| SA0006 | 경계 초과 접근 | 예측 불가 동작 |

#### 올바른 대응

```iecst
// ❌ 위험: 경계 체크 없음
arrData[nIndex] := nValue;

// ✅ 안전: 경계 체크 포함
IF nIndex >= 0 AND nIndex <= UPPER_BOUND(arrData, 1) THEN
    arrData[nIndex] := nValue;
ELSE
    bIndexError := TRUE;
END_IF
```

---

### 4. 코드 복잡도 (Cyclomatic Complexity)

#### 왜 중요한가?

코드 복잡도와 버그 발생률은 직접적 상관관계가 있습니다.

| 복잡도 | 버그 발생 확률 | 테스트 필요 경로 |
|--------|---------------|-----------------|
| 1-10 | 낮음 (<5%) | 관리 가능 |
| 11-20 | 중간 (5-20%) | 주의 필요 |
| 21-50 | 높음 (20-40%) | 리팩토링 권장 |
| 50+ | 매우 높음 (>40%) | **리팩토링 필수** |

*출처: Carnegie Mellon Software Engineering Institute*

#### McCabe 복잡도 계산

```iecst
// 복잡도 계산 예시
FUNCTION_BLOCK FB_Example
    // 기본값 = 1

    IF cond1 THEN           // +1 → 복잡도 2
        IF cond2 THEN       // +1 → 복잡도 3
            // ...
        ELSIF cond3 THEN    // +1 → 복잡도 4
            // ...
        END_IF
    END_IF

    FOR i := 0 TO 10 DO     // +1 → 복잡도 5
        IF cond4 OR cond5 THEN  // +1 (IF) +1 (OR) → 복잡도 7
            // ...
        END_IF
    END_FOR

    // 최종 복잡도: 7
END_FUNCTION_BLOCK
```

#### 복잡도 감소 방법

```iecst
// ❌ 복잡한 코드 (복잡도 15+)
IF sensor1 AND sensor2 AND NOT error THEN
    IF mode = 1 THEN
        IF position > 100 AND position < 200 THEN
            // 깊은 중첩...
        END_IF
    ELSIF mode = 2 THEN
        // ...
    END_IF
END_IF

// ✅ 리팩토링된 코드 (복잡도 5)
// 조건을 함수로 추출
IF NOT IsSafeToOperate() THEN
    RETURN;
END_IF

CASE mode OF
    1: ProcessMode1();
    2: ProcessMode2();
END_CASE

METHOD IsSafeToOperate : BOOL
    IsSafeToOperate := sensor1 AND sensor2 AND NOT error;
END_METHOD
```

---

### 5. 실수(REAL) 비교

#### 왜 중요한가?

부동소수점은 정확한 비교가 불가능합니다.

```iecst
VAR
    rValue : REAL;
END_VAR

rValue := 0.1 + 0.2;

// ❌ 예상: TRUE, 실제: FALSE!
IF rValue = 0.3 THEN
    // 이 코드는 실행되지 않음
    // 실제 rValue = 0.30000001192...
END_IF
```

#### 관련 규칙

| 규칙 | 설명 | 위험성 |
|------|------|--------|
| QA005 | 부동소수점 직접 비교 | 조건문 오동작 |
| SA0039 | REAL 동등 비교 | 무한 루프 가능 |

#### 올바른 대응

```iecst
// ❌ 위험: 직접 비교
IF rActual = rSetpoint THEN

// ✅ 안전: 허용오차 사용
VAR CONSTANT
    EPSILON : REAL := 0.0001;  // 허용 오차
END_VAR

IF ABS(rActual - rSetpoint) < EPSILON THEN
    // 안전한 비교
END_IF
```

---

### 6. 동시성 및 태스크 안전성

#### 왜 중요한가?

TwinCAT은 멀티태스크 환경입니다. 공유 변수 접근 시 경쟁 조건이 발생할 수 있습니다.

```
Task 1 (1ms)          Task 2 (10ms)
     │                     │
     ├─ READ nCounter ─────┤
     │     (= 100)         │
     │                     ├─ READ nCounter
     │                     │     (= 100)
     ├─ WRITE 101 ─────────┤
     │                     │
     │                     ├─ WRITE 101  ← 증가 누락!
     │                     │
```

#### 관련 규칙

| 규칙 | 설명 | 위험성 |
|------|------|--------|
| SA0071 | 태스크 간 비보호 접근 | 데이터 불일치 |
| SA0072 | 원자성 위반 | 경쟁 조건 |
| QA008 | 무한 루프 위험 | 태스크 중단 |

#### 올바른 대응

```iecst
// ❌ 위험: 직접 접근
VAR_GLOBAL
    nSharedCounter : INT;
END_VAR

// Task 1
nSharedCounter := nSharedCounter + 1;

// ✅ 안전: 원자적 연산 또는 동기화
VAR_GLOBAL
    nSharedCounter : INT;
    fbCriticalSection : FB_CriticalSection;
END_VAR

// Task 1
IF fbCriticalSection.Enter() THEN
    nSharedCounter := nSharedCounter + 1;
    fbCriticalSection.Leave();
END_IF

// 또는 TwinCAT 원자적 함수 사용
INTERLOCKEDINCREMENT(nSharedCounter);
```

---

## 실제 사고 사례

### 사례 1: 타입 변경으로 인한 위치 오류

**상황**: 서보 모터 위치 변수를 DINT에서 INT로 변경

```iecst
// 변경 전
nTargetPosition : DINT := 100000;  // 100mm 위치

// 변경 후
nTargetPosition : INT := 100000;   // ❌ 오버플로우 → -31072
```

**결과**:
- 모터가 예상과 반대 방향으로 이동
- 기구부 충돌로 생산라인 3시간 정지
- 수리비 및 생산 손실 약 5,000만원

**QA 도구가 탐지**: QA001 (Critical) - 타입 축소로 인한 데이터 손실 가능

---

### 사례 2: 초기화 누락으로 인한 간헐적 오류

**상황**: 신규 Function Block 추가 시 상태 변수 초기화 누락

```iecst
VAR
    eState : E_MACHINE_STATE;  // 초기값 없음
END_VAR

CASE eState OF
    IDLE: ...
    RUNNING: ...
    // eState가 임의값이면 아무 분기도 실행 안됨
END_CASE
```

**결과**:
- PLC 재시작 시 10번 중 1번 정도 기계가 응답하지 않음
- 원인 파악에 2주 소요 (현장에서만 재현)
- 고객 불만 및 신뢰도 하락

**QA 도구가 탐지**: QA006 (Warning) - 초기화되지 않은 변수 사용

---

### 사례 3: 배열 인덱스 오류로 인한 데이터 손상

**상황**: 레시피 시스템에서 배열 인덱스 검증 누락

```iecst
// nRecipeNo는 사용자 입력값 (0~99 예상)
arrRecipeData[nRecipeNo].rTemperature := rNewTemp;
// nRecipeNo가 100 이상이면?
```

**결과**:
- 특정 레시피 선택 시 시스템 설정값이 변경됨
- 다른 제품의 온도 설정이 임의로 변경
- 불량품 100개 생산 후 발견

**QA 도구가 탐지**: QA003 (Warning) - 배열 경계 검사 없음

---

## 이슈 해결 우선순위

### 우선순위 결정 매트릭스

```
            높음 ─────────────────────────────────── 낮음
              │                                      │
영향도 높음   │  🔴 Critical        🟠 Warning       │
              │  즉시 수정          1주 내 수정       │
              │                                      │
영향도 낮음   │  🟠 Warning         🟡 Info          │
              │  계획된 수정        개선 권장         │
              └──────────────────────────────────────┘
                          발생 가능성
```

### 배포 전 필수 수정

다음 이슈는 **배포 전 반드시 수정**해야 합니다:

1. **Critical 심각도 전체**
   - 타입 축소 (QA001)
   - 무한 루프 가능성 (QA008)
   - 포인터 NULL 미검사 (QA004)

2. **안전 관련 Warning**
   - 초기화 누락 (QA006)
   - 배열 경계 미검사 (QA003)
   - 부동소수점 직접 비교 (QA005)

### 개선 권장 (다음 스프린트)

1. **코드 품질 Warning**
   - 높은 복잡도 (QA017)
   - 깊은 중첩 (QA009)
   - 미사용 변수 (QA012)

2. **Info 전체**
   - 명명 규칙 위반
   - 주석 부족
   - 코드 스타일

---

## 자주 묻는 질문

### Q1: "코드가 잘 동작하는데 왜 수정해야 하나요?"

**A**: QA 도구는 "정상 상황"이 아닌 "예외 상황"을 탐지합니다.

```
정상 상황 (99%)     예외 상황 (1%)
     │                   │
     ├── 테스트 통과      └── 오버플로우
     │                        런타임 에러
     └── 현장 동작              사고 발생
```

1% 예외 상황이 발생하면 그때는 이미 늦습니다.

---

### Q2: "False Positive(오탐)가 많은 것 같아요"

**A**: 신뢰도(Confidence) 필터를 사용하세요.

```bash
# 높은 신뢰도 이슈만 표시
twincat-qa --min-confidence high

# 특정 규칙 제외
twincat-qa --exclude-rules QA010,QA011
```

또는 `.twincat-qa.json` 설정으로 프로젝트별 예외 처리:

```json
{
  "suppressions": [
    {
      "ruleId": "QA017",
      "file": "FB_Legacy.TcPOU",
      "reason": "레거시 코드, 다음 버전에서 리팩토링 예정"
    }
  ]
}
```

---

### Q3: "모든 이슈를 수정할 시간이 없어요"

**A**: 우선순위에 따라 단계적으로 수정하세요.

1. **이번 배포**: Critical 전체 + 안전 관련 Warning
2. **다음 스프린트**: 나머지 Warning
3. **기술 부채 관리**: Info 이슈 점진적 개선

```
이슈 수
│
│████████████████████████  ← 초기 상태
│████████████████         ← 1단계 (Critical 수정)
│██████████               ← 2단계 (Warning 수정)
│████                     ← 3단계 (Info 개선)
└─────────────────────────→ 시간
```

---

### Q4: "이 규칙이 우리 프로젝트에는 맞지 않아요"

**A**: 규칙별 비활성화 또는 임계값 조정이 가능합니다.

```json
// .twincat-qa.json
{
  "rules": {
    "QA017": {
      "enabled": true,
      "thresholds": {
        "info": 15,      // 기본값 10
        "warning": 25,   // 기본값 15
        "critical": 35   // 기본값 20
      }
    },
    "QA010": {
      "enabled": false,  // 규칙 비활성화
      "reason": "프로젝트 특성상 예외"
    }
  }
}
```

---

### Q5: "팀원들이 QA 결과를 무시해요"

**A**: 다음 방법을 권장합니다:

1. **CI/CD 통합**: Critical 이슈 있으면 빌드 실패
   ```yaml
   # Azure DevOps Pipeline 예시
   - script: |
       twincat-qa --min-severity critical --fail-on-issues
     displayName: 'QA Check'
   ```

2. **코드 리뷰 필수 항목**: PR 체크리스트에 QA 통과 추가

3. **메트릭 시각화**: 시간에 따른 이슈 감소 추이 공유

4. **사례 공유**: 과거 사고 사례와 QA 탐지 가능성 연결

---

## 부록: 심각도별 규칙 요약

### 🔴 Critical 규칙

| ID | 규칙명 | 주요 원인 |
|----|--------|----------|
| QA001 | 타입 축소 | DINT→INT, LINT→DINT 변경 |
| QA004 | NULL 포인터 | 포인터 검증 없이 사용 |
| QA008 | 무한 루프 | 종료 조건 없는 WHILE |
| SA0003 | 배열 범위 초과 | 상수 인덱스 오류 |
| SA0033 | 포인터 크기 불일치 | 32비트↔64비트 변환 |

### 🟠 Warning 규칙

| ID | 규칙명 | 주요 원인 |
|----|--------|----------|
| QA003 | 배열 경계 | 동적 인덱스 미검증 |
| QA005 | REAL 비교 | = 또는 <> 연산자 사용 |
| QA006 | 초기화 누락 | 선언만 하고 초기값 없음 |
| QA009 | 깊은 중첩 | 3단계 이상 IF 중첩 |
| SA0020 | 상수 재할당 | CONSTANT 변수 수정 시도 |

### 🟡 Info 규칙

| ID | 규칙명 | 주요 원인 |
|----|--------|----------|
| QA010 | 명명 규칙 | 접두어 누락, 불일치 |
| QA011 | 주석 부족 | 복잡한 로직 설명 없음 |
| QA012 | 미사용 변수 | 선언 후 사용 안됨 |
| QA017 | 높은 복잡도 | 복잡도 10 초과 |
| SA0029 | TODO 주석 | 미완료 작업 표시 |

---

## 문서 정보

- **버전**: 1.0
- **작성일**: 2024년
- **작성**: TwinCAT QA 도구 팀
- **문의**: GitHub Issues

이 문서는 지속적으로 업데이트됩니다. 피드백은 언제든 환영합니다.
