# TwinCAT Code QA Tool - 검사 항목별 기준 가이드

**버전**: 2.1
**작성일**: 2025-11-21
**변경 이력**: 에러 복구, 로깅 검사 항목 추가, 압력/진공/차압 센서 지원
**대상**: PLC 개발자, 코드 리뷰어, QA 담당자

---

## 심각도 등급 정의

| 등급 | 의미 | 조치 |
|------|------|------|
| **Critical** | 즉시 수정 필수 - 장비 손상, 안전사고, 시스템 다운 가능 | 배포 차단 |
| **High** | 빠른 수정 필요 - 오작동, 데이터 손실 가능 | 48시간 내 수정 |
| **Medium** | 수정 권장 - 유지보수 어려움, 잠재적 버그 | 다음 릴리스 전 수정 |
| **Low** | 개선 권장 - 코드 품질, 가독성 향상 | 시간 여유 시 수정 |

---

## 1. 안전성 검사 (Safety)

### 1.1 비상정지 로직 미구현

| 항목 | 내용 |
|------|------|
| **심각도** | 🔴 Critical |
| **설명** | 출력(액추에이터)을 제어하는 코드에 비상정지 조건이 없음 |
| **위험** | 비상 상황에서 장비가 멈추지 않아 안전사고 발생 |

**❌ 나쁜 예시:**
```iecst
// 비상정지 조건 없이 모터 제어
IF bStartButton THEN
    bMotorRun := TRUE;  // 위험! 비상정지가 눌려도 모터가 계속 동작
END_IF
```

**✅ 좋은 예시:**
```iecst
// 비상정지 조건 포함
IF bStartButton AND NOT bEmergencyStop THEN
    bMotorRun := TRUE;
ELSE
    bMotorRun := FALSE;  // 비상정지 시 즉시 정지
END_IF
```

---

### 1.2 인터락 조건 누락

| 항목 | 내용 |
|------|------|
| **심각도** | 🔴 Critical |
| **설명** | 액추에이터(모터, 실린더, 밸브) 제어 시 안전 조건 확인 없음 |
| **위험** | 충돌, 과부하, 장비 손상 가능 |

**❌ 나쁜 예시:**
```iecst
// 안전 조건 없이 실린더 동작
bCylinderExtend := TRUE;  // 위험! 다른 실린더와 충돌 가능
```

**✅ 좋은 예시:**
```iecst
// 인터락 조건 확인 후 동작
IF bSafetyOK AND NOT bFault AND bCylinderRetracted THEN
    bCylinderExtend := TRUE;
END_IF
```

---

### 1.3 나눗셈 0 체크 누락

| 항목 | 내용 |
|------|------|
| **심각도** | 🔴 Critical |
| **설명** | 변수로 나눗셈 시 0 체크 없음 |
| **위험** | 런타임 에러로 PLC 프로그램 중단 |

**❌ 나쁜 예시:**
```iecst
// 0 체크 없이 나눗셈
rSpeed := rDistance / rTime;  // rTime이 0이면 에러!
```

**✅ 좋은 예시:**
```iecst
// 0 체크 후 나눗셈
IF rTime <> 0.0 THEN
    rSpeed := rDistance / rTime;
ELSE
    rSpeed := 0.0;  // 안전한 기본값
END_IF
```

---

### 1.4 입력을 출력으로 사용

| 항목 | 내용 |
|------|------|
| **심각도** | 🔴 Critical |
| **설명** | 물리적 입력(%I) 변수에 값을 할당하려고 시도 |
| **위험** | 컴파일 에러 또는 예상치 못한 동작 |

**❌ 나쁜 예시:**
```iecst
VAR_GLOBAL
    bSensor AT %IX0.0 : BOOL;  // 입력
END_VAR

bSensor := TRUE;  // 에러! 입력은 읽기 전용
```

**✅ 좋은 예시:**
```iecst
VAR_GLOBAL
    bSensor AT %IX0.0 : BOOL;  // 입력
END_VAR
VAR
    bSensorCopy : BOOL;  // 로컬 변수
END_VAR

bSensorCopy := bSensor;  // 입력 값을 복사해서 사용
```

---

## 2. 타이밍 검사 (Timing)

### 2.1 무한 루프 가능성

| 항목 | 내용 |
|------|------|
| **심각도** | 🔴 Critical |
| **설명** | WHILE/REPEAT 루프에서 종료 조건이 변경되지 않음 |
| **위험** | PLC 사이클 초과로 시스템 다운 |

**❌ 나쁜 예시:**
```iecst
// 무한 루프 - 종료 조건 변경 안됨
WHILE bRunning DO
    nCount := nCount + 1;  // bRunning이 변경되지 않음!
END_WHILE
```

**✅ 좋은 예시:**
```iecst
// 명확한 종료 조건
WHILE bRunning DO
    nCount := nCount + 1;
    IF nCount >= 100 THEN
        bRunning := FALSE;  // 종료 조건 변경
    END_IF
END_WHILE

// 또는 EXIT 사용
WHILE TRUE DO
    nCount := nCount + 1;
    IF nCount >= 100 THEN
        EXIT;  // 루프 탈출
    END_IF
END_WHILE
```

---

### 2.2 타이머 설정값 문제

| 항목 | 내용 |
|------|------|
| **심각도** | 🟡 High (너무 짧음) / 🟢 Low (너무 김) |
| **설명** | 타이머 시간이 PLC 사이클보다 짧거나 비정상적으로 김 |
| **위험** | 타이머 미작동 또는 비효율적 대기 |

**❌ 나쁜 예시:**
```iecst
// 너무 짧은 타이머 (PLC 사이클보다 짧을 수 있음)
tonDelay(IN := TRUE, PT := T#1ms);

// 너무 긴 타이머
tonTimeout(IN := TRUE, PT := T#24h);  // 24시간?
```

**✅ 좋은 예시:**
```iecst
// 적절한 타이머 설정
tonDelay(IN := TRUE, PT := T#100ms);  // PLC 사이클의 최소 10배
tonTimeout(IN := TRUE, PT := T#30s);  // 합리적인 타임아웃
```

---

## 3. 데이터 안전성 검사 (Data Safety)

### 3.1 형변환 데이터 손실

| 항목 | 내용 |
|------|------|
| **심각도** | 🟡 High |
| **설명** | 큰 타입에서 작은 타입으로 변환 시 데이터 손실 가능 |
| **위험** | 잘못된 계산 결과, 오버플로우 |

**❌ 나쁜 예시:**
```iecst
VAR
    nBigValue : DINT := 100000;  // 32비트
    nSmallValue : INT;            // 16비트 (최대 32767)
END_VAR

nSmallValue := DINT_TO_INT(nBigValue);  // 데이터 손실! 결과: -31072
```

**✅ 좋은 예시:**
```iecst
VAR
    nBigValue : DINT := 100000;
    nSmallValue : INT;
END_VAR

// 범위 체크 후 변환
IF nBigValue >= -32768 AND nBigValue <= 32767 THEN
    nSmallValue := DINT_TO_INT(nBigValue);
ELSE
    nSmallValue := 32767;  // 안전한 최대값
    bOverflowError := TRUE;
END_IF
```

---

### 3.2 부호 있음/없음 변환

| 항목 | 내용 |
|------|------|
| **심각도** | 🟡 High |
| **설명** | 부호 있는 타입에서 부호 없는 타입으로 변환 |
| **위험** | 음수 값이 큰 양수로 변환됨 |

**❌ 나쁜 예시:**
```iecst
VAR
    nSigned : INT := -1;
    nUnsigned : UINT;
END_VAR

nUnsigned := INT_TO_UINT(nSigned);  // 결과: 65535 (예상: -1)
```

**✅ 좋은 예시:**
```iecst
// 음수 체크 후 변환
IF nSigned >= 0 THEN
    nUnsigned := INT_TO_UINT(nSigned);
ELSE
    nUnsigned := 0;  // 안전한 기본값
    bNegativeError := TRUE;
END_IF
```

---

## 4. 메모리 검사 (Memory)

### 4.1 I/O 주소 중복

| 항목 | 내용 |
|------|------|
| **심각도** | 🔴 Critical |
| **설명** | 동일한 I/O 주소에 여러 변수가 매핑됨 |
| **위험** | 데이터 덮어쓰기, 예측 불가능한 동작 |

**❌ 나쁜 예시:**
```iecst
VAR_GLOBAL
    bStart AT %IX0.0 : BOOL;
    bStop AT %IX0.0 : BOOL;   // 중복! 같은 주소
END_VAR
```

**✅ 좋은 예시:**
```iecst
VAR_GLOBAL
    bStart AT %IX0.0 : BOOL;
    bStop AT %IX0.1 : BOOL;   // 다른 주소
END_VAR
```

---

### 4.2 메모리 영역 중첩

| 항목 | 내용 |
|------|------|
| **심각도** | 🔴 Critical |
| **설명** | 워드/더블워드 영역이 서로 겹침 |
| **위험** | 한 변수 변경 시 다른 변수도 변경됨 |

**❌ 나쁜 예시:**
```iecst
VAR_GLOBAL
    nWord1 AT %MW100 : WORD;   // 100-101 바이트
    nWord2 AT %MW101 : WORD;   // 101-102 바이트 (겹침!)
END_VAR
```

**✅ 좋은 예시:**
```iecst
VAR_GLOBAL
    nWord1 AT %MW100 : WORD;   // 100-101 바이트
    nWord2 AT %MW102 : WORD;   // 102-103 바이트 (겹침 없음)
END_VAR
```

---

### 4.3 배열 인덱스 범위 초과

| 항목 | 내용 |
|------|------|
| **심각도** | 🔴 Critical |
| **설명** | 배열 접근 시 선언된 범위를 초과하는 인덱스 사용 |
| **위험** | 메모리 침범, 예측 불가능한 동작 |

**❌ 나쁜 예시:**
```iecst
VAR
    arrData : ARRAY[0..9] OF INT;  // 인덱스 0-9
END_VAR

arrData[10] := 100;  // 범위 초과! (최대 9)

FOR i := 0 TO 15 DO  // 범위 초과!
    arrData[i] := 0;
END_FOR
```

**✅ 좋은 예시:**
```iecst
VAR
    arrData : ARRAY[0..9] OF INT;
END_VAR

// 범위 내 접근
FOR i := 0 TO 9 DO
    arrData[i] := 0;
END_FOR

// 동적 인덱스는 범위 체크
IF nIndex >= 0 AND nIndex <= 9 THEN
    arrData[nIndex] := 100;
END_IF
```

---

## 5. 구조 검사 (Structure)

### 5.1 FB 인스턴스 미호출

| 항목 | 내용 |
|------|------|
| **심각도** | 🟠 Medium |
| **설명** | Function Block을 선언했지만 호출하지 않음 |
| **위험** | 의도한 기능 미작동, 불필요한 메모리 사용 |

**❌ 나쁜 예시:**
```iecst
VAR
    fbMotor : FB_MotorControl;  // 선언만 함
END_VAR

// fbMotor()가 호출되지 않음 - 모터 제어 안됨!
```

**✅ 좋은 예시:**
```iecst
VAR
    fbMotor : FB_MotorControl;
END_VAR

// FB 호출
fbMotor(
    bEnable := TRUE,
    rTargetSpeed := 100.0
);
```

---

### 5.2 출력 변수 미할당

| 항목 | 내용 |
|------|------|
| **심각도** | 🟠 Medium |
| **설명** | VAR_OUTPUT으로 선언된 변수에 값을 할당하지 않음 |
| **위험** | 호출자가 잘못된 값(초기값)을 받음 |

**❌ 나쁜 예시:**
```iecst
FUNCTION_BLOCK FB_Calculate
VAR_OUTPUT
    nResult : INT;
    bDone : BOOL;    // 할당되지 않음!
END_VAR

nResult := nA + nB;
// bDone은 할당 안됨 - 항상 FALSE
END_FUNCTION_BLOCK
```

**✅ 좋은 예시:**
```iecst
FUNCTION_BLOCK FB_Calculate
VAR_OUTPUT
    nResult : INT;
    bDone : BOOL;
END_VAR

nResult := nA + nB;
bDone := TRUE;  // 모든 출력 할당
END_FUNCTION_BLOCK
```

---

## 6. 코드 품질 검사 (Code Quality)

### 6.1 순환 복잡도 초과

| 항목 | 내용 |
|------|------|
| **심각도** | 🟠 Medium (11-20) / 🟡 High (21+) |
| **설명** | 함수/FB의 분기가 너무 많아 복잡함 |
| **위험** | 테스트 어려움, 버그 발생 확률 증가, 유지보수 어려움 |

**❌ 나쁜 예시:**
```iecst
// 복잡도 높음 - IF 중첩이 많음
IF cond1 THEN
    IF cond2 THEN
        IF cond3 THEN
            FOR i := 0 TO 10 DO
                CASE state OF
                    1: IF x THEN y := 1; END_IF
                    2: IF x AND y THEN z := 1; END_IF
                    // ... 더 많은 분기
                END_CASE
            END_FOR
        END_IF
    END_IF
END_IF
```

**✅ 좋은 예시:**
```iecst
// 함수로 분리하여 복잡도 감소
IF NOT CheckPreconditions() THEN
    RETURN;
END_IF

ProcessState();
UpdateOutputs();

// 각 함수는 단순하게 유지
```

---

### 6.2 매직 넘버 사용

| 항목 | 내용 |
|------|------|
| **심각도** | 🟢 Low |
| **설명** | 의미 없는 하드코딩된 숫자 사용 |
| **위험** | 코드 이해 어려움, 수정 시 실수 가능 |

**❌ 나쁜 예시:**
```iecst
IF rTemperature > 85.5 THEN  // 85.5가 뭐지?
    bAlarm := TRUE;
END_IF

rSpeed := rRPM * 0.10472;  // 0.10472는 무슨 값?
```

**✅ 좋은 예시:**
```iecst
VAR CONSTANT
    TEMP_ALARM_THRESHOLD : REAL := 85.5;  // 온도 경보 임계값 (°C)
    RPM_TO_RAD_PER_SEC : REAL := 0.10472; // RPM → rad/s 변환 계수
END_VAR

IF rTemperature > TEMP_ALARM_THRESHOLD THEN
    bAlarm := TRUE;
END_IF

rSpeed := rRPM * RPM_TO_RAD_PER_SEC;
```

---

### 6.3 데드 코드

| 항목 | 내용 |
|------|------|
| **심각도** | 🟢 Low (주석 코드) / 🟠 Medium (도달 불가) |
| **설명** | 실행되지 않는 코드 존재 |
| **위험** | 코드 혼란, 유지보수 어려움 |

**❌ 나쁜 예시:**
```iecst
RETURN;
x := 10;  // 도달 불가 코드!

IF FALSE THEN
    y := 20;  // 절대 실행 안됨
END_IF

// IF oldLogic THEN
//     z := 30;  // 주석 처리된 코드
// END_IF
```

**✅ 좋은 예시:**
```iecst
// 불필요한 코드 제거
x := 10;
RETURN;

// 주석 코드는 삭제하고 Git 히스토리 활용
```

---

## 7. 심화 검사 (Advanced)

### 7.1 공유 변수 경쟁 조건

| 항목 | 내용 |
|------|------|
| **심각도** | 🔴 Critical |
| **설명** | 여러 태스크/POU에서 동일 전역 변수에 쓰기 접근 |
| **위험** | 데이터 손상, 예측 불가능한 동작 |

**❌ 나쁜 예시:**
```iecst
// Task1.TcPOU
gCounter := gCounter + 1;

// Task2.TcPOU
gCounter := 0;  // 경쟁 조건!
```

**✅ 좋은 예시:**
```iecst
// 세마포어 또는 태스크별 변수 분리
IF fbSemaphore.Claim() THEN
    gCounter := gCounter + 1;
    fbSemaphore.Release();
END_IF
```

---

### 7.2 상태머신 ELSE 분기 누락

| 항목 | 내용 |
|------|------|
| **심각도** | 🟠 Medium |
| **설명** | CASE 문에 ELSE 분기가 없어 정의되지 않은 상태값 처리 불가 |
| **위험** | 예상치 못한 상태에서 아무 동작 안함 |

**❌ 나쁜 예시:**
```iecst
CASE nState OF
    0: DoInit();
    1: DoRun();
    2: DoStop();
END_CASE  // nState가 3이면?
```

**✅ 좋은 예시:**
```iecst
CASE nState OF
    0: DoInit();
    1: DoRun();
    2: DoStop();
ELSE:
    nState := 0;  // 예외 상태 → 초기화
    bError := TRUE;
END_CASE
```

---

### 7.3 탈출 불가능한 상태

| 항목 | 내용 |
|------|------|
| **심각도** | 🟡 High |
| **설명** | 상태머신에서 전이가 없는 상태 (DONE/ERROR 제외) |
| **위험** | 프로그램이 특정 상태에서 멈춤 |

**❌ 나쁜 예시:**
```iecst
CASE nState OF
    0: nState := 1;
    1: nState := 2;
    2: x := 1;  // 탈출 불가! 전이 없음
END_CASE
```

**✅ 좋은 예시:**
```iecst
CASE nState OF
    0: nState := 1;
    1: nState := 2;
    2:
        x := 1;
        nState := 0;  // 다음 상태로 전이
END_CASE
```

---

### 7.4 통신 타임아웃 미처리

| 항목 | 내용 |
|------|------|
| **심각도** | 🟡 High |
| **설명** | ADS/Modbus/TCP 통신 시 타임아웃 처리 없음 |
| **위험** | 통신 실패 시 프로그램 무한 대기 |

**❌ 나쁜 예시:**
```iecst
fbAdsRead(NETID := '', PORT := 851, READ := TRUE);
// 타임아웃 처리 없음 - 응답 없으면 영원히 대기
```

**✅ 좋은 예시:**
```iecst
fbAdsRead(NETID := '', PORT := 851, READ := TRUE);
tonTimeout(IN := fbAdsRead.BUSY, PT := T#5s);

IF tonTimeout.Q THEN
    bCommError := TRUE;  // 타임아웃 처리
    fbAdsRead(READ := FALSE);
END_IF
```

---

### 7.5 수신 데이터 검증 없음

| 항목 | 내용 |
|------|------|
| **심각도** | 🔴 Critical |
| **설명** | 외부에서 수신한 데이터를 검증 없이 사용 |
| **위험** | 잘못된 데이터로 오작동, 장비 손상 가능 |

**❌ 나쁜 예시:**
```iecst
// 외부에서 받은 속도값 그대로 사용
rMotorSpeed := fbModbus.ReceivedData;  // 위험!
```

**✅ 좋은 예시:**
```iecst
// 범위 검증 후 사용
rReceivedSpeed := fbModbus.ReceivedData;
IF rReceivedSpeed >= 0 AND rReceivedSpeed <= MAX_SPEED THEN
    rMotorSpeed := rReceivedSpeed;
ELSE
    bDataError := TRUE;
    rMotorSpeed := 0;  // 안전한 기본값
END_IF
```

---

### 7.6 문자열 버퍼 오버플로우

| 항목 | 내용 |
|------|------|
| **심각도** | 🟡 High |
| **설명** | CONCAT 등 문자열 연결 시 길이 체크 없음 |
| **위험** | 버퍼 오버플로우로 데이터 손상 |

**❌ 나쁜 예시:**
```iecst
VAR
    sResult : STRING(20);  // 20자 제한
END_VAR

sResult := CONCAT('Hello ', 'World! How are you?');  // 오버플로우!
```

**✅ 좋은 예시:**
```iecst
IF LEN(sA) + LEN(sB) <= 20 THEN
    sResult := CONCAT(sA, sB);
ELSE
    bStringError := TRUE;
END_IF
```

---

### 7.7 포인터 널 체크 없음

| 항목 | 내용 |
|------|------|
| **심각도** | 🔴 Critical |
| **설명** | POINTER 역참조 시 널 체크 없음 |
| **위험** | 런타임 에러로 PLC 중단 |

**❌ 나쁜 예시:**
```iecst
VAR
    pData : POINTER TO INT;
END_VAR

nValue := pData^;  // pData가 0이면 에러!
```

**✅ 좋은 예시:**
```iecst
IF pData <> 0 THEN
    nValue := pData^;
ELSE
    bPointerError := TRUE;
END_IF
```

---

### 7.8 아날로그 센서 범위 체크 없음

| 항목 | 내용 |
|------|------|
| **심각도** | 🟡 High |
| **설명** | 아날로그 입력 값의 유효 범위 확인 없음 |
| **위험** | 비정상 센서값으로 계산 오류 |

**❌ 나쁜 예시:**
```iecst
VAR_GLOBAL
    nPressure AT %IW100 : INT;  // 0-10V → 0-32767
END_VAR

rPressureBar := nPressure * 0.001;  // 범위 체크 없음
```

**✅ 좋은 예시:**
```iecst
IF nPressure >= 0 AND nPressure <= 32767 THEN
    rPressureBar := nPressure * 0.001;
ELSE
    bSensorError := TRUE;
    rPressureBar := 0;
END_IF
```

---

### 7.9 센서 고장 감지 없음

| 항목 | 내용 |
|------|------|
| **심각도** | 🟡 High |
| **설명** | 센서 단선(0) 또는 단락(MAX) 상태 감지 로직 없음 |
| **위험** | 센서 고장 시 잘못된 값으로 제어 |

**❌ 나쁜 예시:**
```iecst
// 센서 고장 감지 없이 그대로 사용
rTemperature := nTempSensor * 0.1;
```

**✅ 좋은 예시:**
```iecst
// 단선(0) 또는 단락(MAX) 감지
IF nTempSensor = 0 OR nTempSensor = 32767 THEN
    bSensorFault := TRUE;
    rTemperature := 0;
ELSE
    bSensorFault := FALSE;
    rTemperature := nTempSensor * 0.1;
END_IF
```

---

### 7.10 디지털 센서 채터링 필터 없음

| 항목 | 내용 |
|------|------|
| **심각도** | 🟠 Medium |
| **설명** | 근접/포토센서 신호 떨림 필터링 없음 |
| **위험** | 센서 ON/OFF 반복으로 오작동 |

**❌ 나쁜 예시:**
```iecst
// 센서 신호 직접 사용 - 채터링 발생 가능
IF bProxSensor THEN
    nCount := nCount + 1;  // 떨림으로 여러 번 카운트될 수 있음
END_IF
```

**✅ 좋은 예시:**
```iecst
// TON으로 채터링 필터링
tonFilter(IN := bProxSensor, PT := T#50ms);

// R_TRIG로 상승 에지만 감지
rtrigSensor(CLK := tonFilter.Q);
IF rtrigSensor.Q THEN
    nCount := nCount + 1;
END_IF
```

---

### 7.11 에러 리셋 로직 없음

| 항목 | 내용 |
|------|------|
| **심각도** | 🟡 High |
| **설명** | 에러 변수가 TRUE로 설정되지만 FALSE로 리셋하는 로직이 없음 |
| **위험** | 에러 발생 후 시스템을 재시작해야만 복구 가능 |

**❌ 나쁜 예시:**
```iecst
// 에러 설정만 있고 리셋 없음
IF bOverPressure THEN
    bError := TRUE;  // 한번 에러 나면 복구 불가!
END_IF
```

**✅ 좋은 예시:**
```iecst
// 에러 설정
IF bOverPressure THEN
    bError := TRUE;
END_IF

// 에러 리셋 버튼으로 복구
IF bErrorReset THEN
    bError := FALSE;
END_IF
```

---

### 7.12 워치독 타이머 없음

| 항목 | 내용 |
|------|------|
| **심각도** | 🟡 High |
| **설명** | MAIN 프로그램에 워치독 타이머가 없음 |
| **위험** | 프로그램이 멈춰도(Hang) 감지 불가 |

**❌ 나쁜 예시:**
```iecst
PROGRAM MAIN
// 워치독 없음 - 프로그램 행(Hang) 감지 불가
nCounter := nCounter + 1;
END_PROGRAM
```

**✅ 좋은 예시:**
```iecst
PROGRAM MAIN
VAR
    bWatchdog : BOOL;  // 매 사이클 토글
END_VAR

// 워치독 비트 - 외부에서 모니터링
bWatchdog := NOT bWatchdog;

nCounter := nCounter + 1;
END_PROGRAM
```

---

### 7.13 에러 발생 시 안전 상태 전환 없음

| 항목 | 내용 |
|------|------|
| **심각도** | 🟡 High |
| **설명** | 에러 조건에서 출력을 OFF하거나 안전 상태로 전환하지 않음 |
| **위험** | 에러 상황에서도 위험한 동작 지속 |

**❌ 나쁜 예시:**
```iecst
IF bMachineError THEN
    nErrorCode := 100;  // 에러 코드만 설정
    // 출력 OFF나 상태 전환 없음!
END_IF
```

**✅ 좋은 예시:**
```iecst
IF bMachineError THEN
    nErrorCode := 100;
    bMotorRun := FALSE;    // 모터 정지
    bHeaterOn := FALSE;    // 히터 OFF
    nState := STATE_ERROR; // 에러 상태로 전환
END_IF
```

---

### 7.14 에러 로깅 없음

| 항목 | 내용 |
|------|------|
| **심각도** | 🟠 Medium |
| **설명** | 에러/알람 발생 시 로깅하지 않음 |
| **위험** | 문제 발생 시 원인 추적 어려움 |

**❌ 나쁜 예시:**
```iecst
IF bCondition THEN
    bMachineError := TRUE;  // 에러만 설정, 로깅 없음
END_IF
```

**✅ 좋은 예시:**
```iecst
IF bCondition THEN
    bMachineError := TRUE;
    ADSLOGSTR(ADSLOG_MSGTYPE_ERROR, 'Machine error: Condition X');
    // 또는 FB_EventLogger 사용
END_IF
```

---

### 7.15 상태 변경 로깅 없음

| 항목 | 내용 |
|------|------|
| **심각도** | 🟢 Low |
| **설명** | 상태머신에서 상태 전이 시 로깅이 없음 |
| **위험** | 디버깅 시 상태 변화 추적 어려움 |

**❌ 나쁜 예시:**
```iecst
CASE nState OF
    0: nState := 1;
    1: nState := 2;
    2: nState := 0;
END_CASE
// 어떤 상태 변화도 기록되지 않음
```

**✅ 좋은 예시:**
```iecst
// 상태 변경 전 이전 상태 저장
nPrevState := nState;

CASE nState OF
    0: nState := 1;
    1: nState := 2;
    2: nState := 0;
END_CASE

// 상태 변경 시 로깅
IF nPrevState <> nState THEN
    fbLogger.AddLog(CONCAT('State: ', INT_TO_STRING(nPrevState), ' -> ', INT_TO_STRING(nState)));
END_IF
```

---

## 8. 검사 항목 요약표

| 검사 항목 | 심각도 | 자동 검출 |
|-----------|--------|-----------|
| **안전성** | | |
| 비상정지 로직 미구현 | 🔴 Critical | ✅ |
| 인터락 조건 누락 | 🔴 Critical | ✅ |
| 나눗셈 0 체크 누락 | 🔴 Critical | ✅ |
| 입력을 출력으로 사용 | 🔴 Critical | ✅ |
| 무한 루프 가능성 | 🔴 Critical | ✅ |
| **메모리** | | |
| I/O 주소 중복 | 🔴 Critical | ✅ |
| 메모리 영역 중첩 | 🔴 Critical | ✅ |
| 배열 인덱스 범위 초과 | 🔴 Critical | ✅ |
| **데이터** | | |
| 형변환 데이터 손실 | 🟡 High | ✅ |
| 부호 변환 손실 | 🟡 High | ✅ |
| 타이머 설정 문제 | 🟡 High | ✅ |
| **구조** | | |
| FB 인스턴스 미호출 | 🟠 Medium | ✅ |
| 출력 변수 미할당 | 🟠 Medium | ✅ |
| **코드 품질** | | |
| 순환 복잡도 초과 | 🟠 Medium | ✅ |
| 매직 넘버 사용 | 🟢 Low | ✅ |
| 데드 코드 | 🟢 Low | ✅ |
| **심화 (동시성)** | | |
| 공유 변수 경쟁 조건 | 🔴 Critical | ✅ |
| **심화 (상태머신)** | | |
| ELSE 분기 누락 | 🟠 Medium | ✅ |
| 탈출 불가능한 상태 | 🟡 High | ✅ |
| **심화 (통신)** | | |
| 통신 타임아웃 미처리 | 🟡 High | ✅ |
| 수신 데이터 검증 없음 | 🔴 Critical | ✅ |
| 통신 에러 핸들링 없음 | 🟡 High | ✅ |
| **심화 (리소스)** | | |
| 문자열 버퍼 오버플로우 | 🟡 High | ✅ |
| 포인터 널 체크 없음 | 🔴 Critical | ✅ |
| **심화 (센서)** | | |
| 아날로그 센서 범위 체크 없음 | 🟡 High | ✅ |
| 압력/진공/차압 센서 범위 체크 없음 | 🔴 Critical | ✅ |
| 센서 고장 감지 없음 | 🟡 High | ✅ |
| 디지털 센서 채터링 필터 없음 | 🟠 Medium | ✅ |
| **심화 (에러 복구)** | | |
| 에러 리셋 로직 없음 | 🟡 High | ✅ |
| 워치독 타이머 없음 | 🟡 High | ✅ |
| 안전 상태 전환 없음 | 🟡 High | ✅ |
| **심화 (로깅)** | | |
| 에러 로깅 없음 | 🟠 Medium | ✅ |
| 상태 변경 로깅 없음 | 🟢 Low | ✅ |
| 중요 동작 로깅 없음 | 🟠 Medium | ✅ |

---

## 9. CI/CD 품질 게이트 설정 예시

```bash
# Critical 이슈가 있으면 빌드 실패
twincat-qa analyze ./Project --fail-on critical

# 품질 점수 80점 미만이면 실패
twincat-qa quality ./Project --threshold 80

# 종료 코드
# 0 = 통과
# 1 = 실패 (Critical 이슈 또는 점수 미달)
```

---

**문서 끝**
