# TwinCAT 3 Motion Control (NC) API 가이드

## 목차
1. [개요](#개요)
2. [NC 축 제어 인터페이스](#nc-축-제어-인터페이스)
3. [PLCopen Motion Control 함수 블록](#plcopen-motion-control-함수-블록)
4. [캠 프로파일 및 동기화 기능](#캠-프로파일-및-동기화-기능)
5. [CNC 프로그래밍 인터페이스](#cnc-프로그래밍-인터페이스)
6. [Kinematic Transformation API](#kinematic-transformation-api)
7. [최신 기능 (Build 4024/4026)](#최신-기능-build-40244026)

---

## 개요

TwinCAT 3 Motion Control은 PC 기반 실시간 모션 제어 시스템으로, PLCopen 표준을 준수하며 다양한 산업 자동화 애플리케이션을 지원합니다.

### 주요 구성 요소

- **TF5000**: TwinCAT 3 NC PTP (Point-to-Point) - 최대 10축 제어
- **TF5050**: TwinCAT 3 NC Camming - 캠 프로파일 기반 동기화
- **TF5100**: TwinCAT 3 NC I - DIN 66025 기반 G-Code 인터폴레이션
- **TF5200**: TwinCAT 3 CNC - 고급 CNC 기능
- **TF5240**: TwinCAT 3 CNC Kinematic Transformations - 로봇 및 다축 좌표 변환

### 라이브러리

- **Tc2_MC2**: PLCopen Motion Control V2.0 표준 함수 블록
- **Tc3_DriveMotionControl**: 드라이브 특화 모션 제어
- **Tc2_MC2_Camming**: 캠 테이블 및 동기화 함수

---

## NC 축 제어 인터페이스

### AXIS_REF 데이터 타입

모든 NC 축 제어는 `AXIS_REF` 데이터 타입을 통해 이루어집니다. 각 축마다 하나의 인스턴스가 필요하며, PLC와 NC 간의 인터페이스 역할을 합니다.

```iecst
VAR
    Axis1 : AXIS_REF;  // 축 1 참조
    Axis2 : AXIS_REF;  // 축 2 참조
END_VAR
```

### 축 상태 관리

NC 축은 다음과 같은 상태를 가집니다:

- **Disabled**: 비활성화 상태
- **Standstill**: 정지 상태
- **Homing**: 원점 복귀 중
- **Stopping**: 정지 중
- **Discrete Motion**: 개별 모션 실행 중
- **Continuous Motion**: 연속 모션 실행 중
- **Synchronized Motion**: 동기화 모션 실행 중
- **ErrorStop**: 오류로 인한 정지

### 기본 축 설정

```iecst
// 축 파라미터 설정 예시
VAR_GLOBAL
    // 축 동작 파라미터
    MAX_VELOCITY : REAL := 1000.0;      // 최대 속도 [mm/s]
    MAX_ACCELERATION : REAL := 5000.0;  // 최대 가속도 [mm/s²]
    MAX_JERK : REAL := 50000.0;         // 최대 저크 [mm/s³]
END_VAR
```

---

## PLCopen Motion Control 함수 블록

### 1. 축 활성화 및 제어

#### MC_Power - 축 활성화

축을 활성화하고 구동 방향을 설정합니다.

```iecst
FUNCTION_BLOCK MC_Power
VAR_INPUT
    Axis : AXIS_REF;          // 축 참조
    Enable : BOOL;            // 활성화 신호
    Enable_Positive : BOOL;   // 정방향 활성화
    Enable_Negative : BOOL;   // 역방향 활성화
    Override : REAL := 100.0; // 속도 오버라이드 [%]
    BufferMode : MC_BufferMode; // 버퍼 모드
END_VAR
VAR_OUTPUT
    Status : BOOL;   // 활성화 상태
    Busy : BOOL;     // 실행 중
    Active : BOOL;   // 활성 상태
    Error : BOOL;    // 오류 발생
    ErrorID : UDINT; // 오류 ID
END_VAR
```

**사용 예시:**

```iecst
PROGRAM MAIN
VAR
    fbPower : MC_Power;
    Axis1 : AXIS_REF;
    bEnable : BOOL := TRUE;
END_VAR

// 축 활성화
fbPower(
    Axis := Axis1,
    Enable := bEnable,
    Enable_Positive := TRUE,
    Enable_Negative := TRUE
);

IF fbPower.Status THEN
    // 축이 정상적으로 활성화됨
END_IF
```

#### MC_Reset - 오류 리셋

축의 오류 상태를 초기화합니다.

```iecst
FUNCTION_BLOCK MC_Reset
VAR_INPUT
    Axis : AXIS_REF;    // 축 참조
    Execute : BOOL;     // 실행 트리거
END_VAR
VAR_OUTPUT
    Done : BOOL;        // 완료
    Busy : BOOL;        // 실행 중
    Error : BOOL;       // 오류
    ErrorID : UDINT;    // 오류 ID
END_VAR
```

### 2. 위치 제어 함수 블록

#### MC_MoveAbsolute - 절대 위치 이동

축을 절대 좌표로 이동시킵니다.

```iecst
FUNCTION_BLOCK MC_MoveAbsolute
VAR_INPUT
    Axis : AXIS_REF;           // 축 참조
    Execute : BOOL;            // 실행 트리거
    Position : LREAL;          // 목표 위치
    Velocity : LREAL;          // 이동 속도
    Acceleration : LREAL;      // 가속도
    Deceleration : LREAL;      // 감속도
    Jerk : LREAL;              // 저크
    BufferMode : MC_BufferMode; // 버퍼 모드
END_VAR
VAR_OUTPUT
    Done : BOOL;               // 완료
    Busy : BOOL;               // 실행 중
    Active : BOOL;             // 활성 상태
    CommandAborted : BOOL;     // 명령 중단됨
    Error : BOOL;              // 오류
    ErrorID : UDINT;           // 오류 ID
END_VAR
```

**사용 예시:**

```iecst
VAR
    fbMoveAbs : MC_MoveAbsolute;
    bStart : BOOL;
END_VAR

// 위치 100mm로 이동
fbMoveAbs(
    Axis := Axis1,
    Execute := bStart,
    Position := 100.0,
    Velocity := 500.0,
    Acceleration := 2000.0,
    Deceleration := 2000.0
);

IF fbMoveAbs.Done THEN
    bStart := FALSE;  // 에지 트리거 리셋
    // 이동 완료 처리
END_IF
```

#### MC_MoveRelative - 상대 위치 이동

현재 위치에서 상대적인 거리만큼 이동합니다.

```iecst
FUNCTION_BLOCK MC_MoveRelative
VAR_INPUT
    Axis : AXIS_REF;
    Execute : BOOL;
    Distance : LREAL;          // 이동 거리
    Velocity : LREAL;
    Acceleration : LREAL;
    Deceleration : LREAL;
    Jerk : LREAL;
    BufferMode : MC_BufferMode;
END_VAR
VAR_OUTPUT
    Done : BOOL;
    Busy : BOOL;
    Active : BOOL;
    CommandAborted : BOOL;
    Error : BOOL;
    ErrorID : UDINT;
END_VAR
```

#### MC_MoveAdditive - 추가 이동

실행 중인 모션에 추가 이동 거리를 더합니다.

```iecst
FUNCTION_BLOCK MC_MoveAdditive
VAR_INPUT
    Axis : AXIS_REF;
    Execute : BOOL;
    Distance : LREAL;  // 추가 이동 거리
    Velocity : LREAL;
    Acceleration : LREAL;
    Deceleration : LREAL;
    Jerk : LREAL;
END_VAR
```

### 3. 속도 제어 함수 블록

#### MC_MoveVelocity - 연속 속도 이동

지정된 속도로 연속 이동을 시작합니다.

```iecst
FUNCTION_BLOCK MC_MoveVelocity
VAR_INPUT
    Axis : AXIS_REF;
    Execute : BOOL;
    Velocity : LREAL;          // 목표 속도
    Acceleration : LREAL;
    Deceleration : LREAL;
    Jerk : LREAL;
    Direction : MC_Direction;  // 이동 방향
    BufferMode : MC_BufferMode;
END_VAR
VAR_OUTPUT
    InVelocity : BOOL;  // 목표 속도 도달
    Busy : BOOL;
    Active : BOOL;
    CommandAborted : BOOL;
    Error : BOOL;
    ErrorID : UDINT;
END_VAR
```

**MC_Direction 열거형:**

```iecst
TYPE MC_Direction :
(
    mcPositiveDirection := 1,  // 정방향
    mcNegativeDirection := 2,  // 역방향
    mcCurrentDirection := 3,   // 현재 방향
    mcShortestWay := 4,       // 최단 경로
    mcDirectionDefault := 5    // 기본 방향
);
END_TYPE
```

**사용 예시:**

```iecst
VAR
    fbMoveVel : MC_MoveVelocity;
    bStartContinuous : BOOL;
END_VAR

// 연속 이동 시작
fbMoveVel(
    Axis := Axis1,
    Execute := bStartContinuous,
    Velocity := 300.0,
    Acceleration := 1000.0,
    Deceleration := 1000.0,
    Direction := mcPositiveDirection
);

IF fbMoveVel.InVelocity THEN
    // 목표 속도에 도달함
END_IF
```

### 4. 정지 함수 블록

#### MC_Stop - 축 정지 (잠금)

축을 정지시키고 추가 명령을 차단합니다.

```iecst
FUNCTION_BLOCK MC_Stop
VAR_INPUT
    Axis : AXIS_REF;
    Execute : BOOL;
    Deceleration : LREAL;
    Jerk : LREAL;
END_VAR
VAR_OUTPUT
    Done : BOOL;
    Busy : BOOL;
    Active : BOOL;
    CommandAborted : BOOL;
    Error : BOOL;
    ErrorID : UDINT;
END_VAR
```

#### MC_Halt - 축 정지 (잠금 없음)

축을 정지시키지만 추가 명령을 허용합니다. **일반적인 모션 시퀀스에는 MC_Halt를 권장합니다.**

```iecst
FUNCTION_BLOCK MC_Halt
VAR_INPUT
    Axis : AXIS_REF;
    Execute : BOOL;
    Deceleration : LREAL;
    Jerk : LREAL;
    BufferMode : MC_BufferMode;
END_VAR
VAR_OUTPUT
    Done : BOOL;
    Busy : BOOL;
    Active : BOOL;
    CommandAborted : BOOL;
    Error : BOOL;
    ErrorID : UDINT;
END_VAR
```

**MC_Stop vs MC_Halt 차이점:**

- **MC_Stop**: 축을 Stopping 상태로 전환하고 추가 모션 명령을 차단 (잠금)
- **MC_Halt**: 축을 StandStill 상태로 전환하지만 정지 후 새 명령 허용

### 5. 원점 복귀

#### MC_Home - 원점 복귀

축을 원점으로 이동시킵니다.

```iecst
FUNCTION_BLOCK MC_Home
VAR_INPUT
    Axis : AXIS_REF;
    Execute : BOOL;
    Position : LREAL;          // 원점 위치 값
    HomingMode : MC_HomingMode; // 원점 복귀 모드
    BufferMode : MC_BufferMode;
END_VAR
VAR_OUTPUT
    Done : BOOL;
    Busy : BOOL;
    Active : BOOL;
    CommandAborted : BOOL;
    Error : BOOL;
    ErrorID : UDINT;
END_VAR
```

### 6. 버퍼 모드

PLCopen 함수 블록은 다양한 버퍼 모드를 지원하여 여러 모션 명령을 체인으로 연결할 수 있습니다.

```iecst
TYPE MC_BufferMode :
(
    mcAborting := 0,      // 기존 모션 중단
    mcBuffered := 1,      // 버퍼에 추가 (순차 실행)
    mcBlendingLow := 2,   // 낮은 우선순위로 블렌딩
    mcBlendingPrevious := 3, // 이전 모션과 블렌딩
    mcBlendingNext := 4,  // 다음 모션과 블렌딩
    mcBlendingHigh := 5   // 높은 우선순위로 블렌딩
);
END_TYPE
```

---

## 캠 프로파일 및 동기화 기능

### 개요

TwinCAT 3 NC Camming (TF5050)은 마스터 축과 슬레이브 축 간의 비선형 관계를 테이블 기반으로 동기화하는 기능을 제공합니다. 전자 캠은 기계식 캠의 전자적 대체품으로 사용됩니다.

### 전자 캠의 특징

- **비선형 동기화**: 마스터 위치에 따른 슬레이브 위치를 테이블로 정의
- **보간**: NC가 테이블의 보간점 사이를 자동 보간 (위치 및 속도)
- **유연성**: 런타임 중 캠 테이블 전환 가능
- **정밀도**: 가속도 및 저크까지 고려한 부드러운 모션

### 캠 테이블 생성

#### TE1510 - TwinCAT 3 Cam Design Tool 사용

Beckhoff의 Cam Design Tool을 사용하여 캠 프로파일을 설계할 수 있습니다:

- **모션 법칙 조합**: 수정된 사인파, 하모닉 조합, 다항식 함수 등
- **시각화**: 속도, 가속도, 저크 그래프 표시
- **내보내기**: NC로 전송 가능한 테이블 또는 모션 함수로 생성

#### 캠 테이블 구조

캠 테이블은 마스터 위치와 슬레이브 위치의 매핑 포인트로 구성됩니다.

```iecst
// 캠 테이블 예시 데이터
VAR_GLOBAL
    CamTable : ARRAY[0..100] OF ST_CamPoint;
END_VAR

TYPE ST_CamPoint :
STRUCT
    MasterPos : LREAL;  // 마스터 위치
    SlavePos : LREAL;   // 슬레이브 위치
END_STRUCT
END_TYPE
```

### 캠 동기화 함수 블록

#### MC_CamIn - 캠 동기화 시작

마스터-슬레이브 캠 커플링을 활성화합니다.

```iecst
FUNCTION_BLOCK MC_CamIn
VAR_INPUT
    Master : AXIS_REF;         // 마스터 축
    Slave : AXIS_REF;          // 슬레이브 축
    Execute : BOOL;            // 실행 트리거
    MasterOffset : LREAL;      // 마스터 오프셋
    SlaveOffset : LREAL;       // 슬레이브 오프셋
    MasterScaling : LREAL;     // 마스터 스케일링
    SlaveScaling : LREAL;      // 슬레이브 스케일링
    StartMode : MC_StartMode;  // 시작 모드
    CamTableID : MC_CAM_ID;    // 캠 테이블 ID
    BufferMode : MC_BufferMode;
END_VAR
VAR_OUTPUT
    InSync : BOOL;       // 동기화 중
    Busy : BOOL;
    Active : BOOL;
    CommandAborted : BOOL;
    Error : BOOL;
    ErrorID : UDINT;
END_VAR
```

**사용 예시:**

```iecst
VAR
    fbCamIn : MC_CamIn;
    MasterAxis : AXIS_REF;
    SlaveAxis : AXIS_REF;
    bStartCam : BOOL;
    CamID : MC_CAM_ID := 1;
END_VAR

// 캠 동기화 시작
fbCamIn(
    Master := MasterAxis,
    Slave := SlaveAxis,
    Execute := bStartCam,
    MasterOffset := 0.0,
    SlaveOffset := 0.0,
    MasterScaling := 1.0,
    SlaveScaling := 1.0,
    StartMode := mcImmediately,
    CamTableID := CamID
);

IF fbCamIn.InSync THEN
    // 캠 동기화 활성 상태
END_IF
```

#### MC_CamOut - 캠 동기화 종료

마스터-슬레이브 캠 커플링을 비활성화합니다.

```iecst
FUNCTION_BLOCK MC_CamOut
VAR_INPUT
    Slave : AXIS_REF;
    Execute : BOOL;
    Deceleration : LREAL;
    Jerk : LREAL;
END_VAR
VAR_OUTPUT
    Done : BOOL;
    Busy : BOOL;
    CommandAborted : BOOL;
    Error : BOOL;
    ErrorID : UDINT;
END_VAR
```

#### MC_CamScaling - 캠 스케일링

활성 캠 커플링의 스케일링을 변경합니다.

```iecst
FUNCTION_BLOCK MC_CamScaling
VAR_INPUT
    Master : AXIS_REF;
    Slave : AXIS_REF;
    Execute : BOOL;
    MasterScaling : LREAL;  // 새로운 마스터 스케일링
    SlaveScaling : LREAL;   // 새로운 슬레이브 스케일링
END_VAR
```

### 전자 기어링 (Electronic Gearing)

전자 기어링은 고정된 기어비를 가진 선형 커플링입니다.

#### MC_GearIn - 기어 동기화 시작

```iecst
FUNCTION_BLOCK MC_GearIn
VAR_INPUT
    Master : AXIS_REF;
    Slave : AXIS_REF;
    Execute : BOOL;
    RatioNumerator : LREAL;    // 기어비 분자
    RatioDenominator : LREAL;  // 기어비 분모
    MasterOffset : LREAL;
    SlaveOffset : LREAL;
    Acceleration : LREAL;
    Deceleration : LREAL;
    Jerk : LREAL;
    BufferMode : MC_BufferMode;
END_VAR
```

**사용 예시:**

```iecst
// 2:1 기어비로 동기화 (마스터가 2회전하면 슬레이브는 1회전)
fbGearIn(
    Master := MasterAxis,
    Slave := SlaveAxis,
    Execute := bStartGear,
    RatioNumerator := 0.5,     // 또는 1.0 (분자)
    RatioDenominator := 1.0,   // 또는 2.0 (분모)
    Acceleration := 1000.0
);
```

#### MC_GearOut - 기어 동기화 종료

```iecst
FUNCTION_BLOCK MC_GearOut
VAR_INPUT
    Slave : AXIS_REF;
    Execute : BOOL;
    Deceleration : LREAL;
    Jerk : LREAL;
END_VAR
```

### 실제 응용 예시: Flying Saw (플라잉 쏘)

```iecst
PROGRAM FlyingSaw
VAR
    ConveyorAxis : AXIS_REF;   // 컨베이어 (마스터)
    SawAxis : AXIS_REF;        // 쏘 (슬레이브)

    fbCamIn : MC_CamIn;
    fbCamOut : MC_CamOut;

    bStartCut : BOOL;
    bCutComplete : BOOL;

    CamTableCutting : MC_CAM_ID := 1;  // 절단용 캠 테이블
END_VAR

// 상태 머신
CASE nState OF
    0: // 대기
        IF bStartCut THEN
            nState := 10;
        END_IF

    10: // 캠 동기화 시작
        fbCamIn(
            Master := ConveyorAxis,
            Slave := SawAxis,
            Execute := TRUE,
            CamTableID := CamTableCutting,
            StartMode := mcImmediately
        );

        IF fbCamIn.InSync THEN
            nState := 20;
        END_IF

    20: // 절단 중
        IF bCutComplete THEN
            nState := 30;
        END_IF

    30: // 캠 동기화 종료
        fbCamOut(
            Slave := SawAxis,
            Execute := TRUE,
            Deceleration := 5000.0
        );

        IF fbCamOut.Done THEN
            nState := 0;
            bStartCut := FALSE;
        END_IF
END_CASE
```

---

## CNC 프로그래밍 인터페이스

### TF5100 - TwinCAT 3 NC I

NC I는 DIN 66025 기반의 G-Code 프로그래밍을 지원하는 인터폴레이션 NC 시스템입니다.

### 주요 기능

- **3D 인터폴레이션**: 3개의 경로 축 + 최대 5개의 보조 축 제어
- **DIN 66025 G-Code**: 표준 G-Code 명령 지원
- **GST (G-Code + ST)**: G-Code와 Structured Text의 결합
- **통합 PLC**: NC 인터페이스가 있는 통합 PLC

### G-Code 프로그래밍 기본

#### 기본 명령어

```gcode
; G-Code 프로그램 예시

N10 G90 G71          ; 절대 좌표 모드, mm 단위
N20 G00 X0 Y0 Z50    ; 급속 이동 (Z축 안전 높이)
N30 G01 Z0 F200      ; 직선 보간 이동 (Z축 하강, 속도 200mm/min)
N40 G01 X100 Y50     ; 직선 이동 (X100, Y50)
N50 G02 X150 Y0 I50 J0 F300  ; 시계방향 원호 보간
N60 G01 Z50          ; Z축 상승
N70 G00 X0 Y0        ; 원점 복귀
N80 M30              ; 프로그램 종료
```

#### 주요 G-Code 명령

| G-Code | 기능 | 설명 |
|--------|------|------|
| G00 | Rapid Traverse | 급속 이동 (위치결정) |
| G01 | Linear Interpolation | 직선 보간 이동 |
| G02 | Circular Interpolation CW | 시계방향 원호 보간 |
| G03 | Circular Interpolation CCW | 반시계방향 원호 보간 |
| G17 | XY Plane Selection | XY 평면 선택 |
| G18 | ZX Plane Selection | ZX 평면 선택 |
| G19 | YZ Plane Selection | YZ 평면 선택 |
| G40 | Tool Radius Compensation Off | 공구 반경 보정 해제 |
| G41 | Tool Radius Compensation Left | 좌측 공구 반경 보정 |
| G42 | Tool Radius Compensation Right | 우측 공구 반경 보정 |
| G54-G59 | Work Offset | 작업 좌표계 오프셋 |
| G90 | Absolute Programming | 절대 좌표 프로그래밍 |
| G91 | Incremental Programming | 증분 좌표 프로그래밍 |

#### M-Code (보조 기능)

| M-Code | 기능 |
|--------|------|
| M00 | Program Stop |
| M02 | Program End |
| M30 | Program End with Reset |
| M03 | Spindle On CW |
| M04 | Spindle On CCW |
| M05 | Spindle Stop |
| M08 | Coolant On |
| M09 | Coolant Off |

### GST 프로그래밍 (G-Code + Structured Text)

GST는 DIN 66025 G-Code와 Structured Text를 결합한 고급 언어입니다.

```gcode
; GST 프로그램 예시

VAR
    i : INT;
    holes : INT := 5;
    spacing : REAL := 50.0;
END_VAR

; 원점 이동
N10 G90 G71
N20 G00 X0 Y0 Z50

; Structured Text 반복문 사용
FOR i := 1 TO holes DO
    ; 각 구멍 위치로 이동
    G00 X:=(i * spacing) Y0
    G01 Z-10 F200       ; 드릴링
    G01 Z50             ; 복귀
END_FOR

N100 M30
```

### PLC에서 NC 제어

PLC 프로그램에서 직접 NC 채널을 제어할 수 있습니다.

```iecst
PROGRAM PLC_NC_Control
VAR
    fbChannelCmd : MC_ChannelCommand;
    sNCProgram : STRING(255);
    bStartProgram : BOOL;
END_VAR

// NC 프로그램 실행
sNCProgram := 'N10 G01 X100 Y50 F500$N N20 M30';

fbChannelCmd(
    Execute := bStartProgram,
    Command := sNCProgram
);
```

---

## Kinematic Transformation API

### TF5240 - TwinCAT 3 CNC Kinematic Transformations

키네마틱 변환은 작업 공간 좌표(데카르트 좌표)와 관절 공간 좌표(축 좌표) 간의 변환을 제공합니다.

### 주요 응용 분야

- **로봇 제어**: 6축 산업용 로봇
- **Delta 로봇**: 고속 픽앤플레이스
- **SCARA 로봇**: 조립 작업
- **5축 가공**: CNC 5축 밀링/선반
- **Gantry 시스템**: 복수 축 동기화

### 키네마틱 유형

#### 1. 사전 정의된 키네마틱

Beckhoff는 다양한 표준 키네마틱을 제공합니다:

- **ID 1-10**: 표준 로봇 키네마틱 (6축 수직 다관절, SCARA 등)
- **ID 11-20**: Delta 키네마틱
- **ID 21-30**: 5축 CNC 키네마틱
- **ID 31-40**: Gantry 및 H-Bot 시스템

#### 2. Universal Kinematic (ID 91)

사용자 정의 키네마틱을 자유롭게 구성할 수 있습니다.

```iecst
// Universal Kinematic 설정 예시
VAR_GLOBAL
    KinematicConfig : ST_UniversalKinematic;
END_VAR

KinematicConfig.KinematicID := 91;
KinematicConfig.NumberOfAxes := 6;
// DH 파라미터 또는 변환 행렬 설정
```

#### 3. Coupled Kinematic (ID 210)

여러 부분 키네마틱을 연결하여 복합 시스템을 구성합니다.

### 변환 인터페이스 (TF5200)

#### 채널 파라미터

```iecst
TYPE ST_ChannelParameters :
STRUCT
    ToolLength : ARRAY[1..3] OF LREAL;    // 공구 길이 벡터
    ToolOrientation : ARRAY[1..3] OF LREAL; // 공구 방향
    WorkpieceOffset : ARRAY[1..6] OF LREAL; // 워크피스 오프셋
    KinematicID : UINT;                    // 키네마틱 ID
END_STRUCT
END_TYPE
```

#### 순방향 변환 (Forward Kinematics)

관절 공간 → 작업 공간

```iecst
FUNCTION_BLOCK FB_ForwardKinematics
VAR_INPUT
    JointPositions : ARRAY[1..6] OF LREAL;  // 축 위치 입력
    KinematicID : UINT;
END_VAR
VAR_OUTPUT
    CartesianPosition : ARRAY[1..6] OF LREAL; // X, Y, Z, A, B, C
    bValid : BOOL;
    nErrorID : UDINT;
END_VAR
```

#### 역방향 변환 (Inverse Kinematics)

작업 공간 → 관절 공간

```iecst
FUNCTION_BLOCK FB_InverseKinematics
VAR_INPUT
    CartesianPosition : ARRAY[1..6] OF LREAL; // 데카르트 좌표
    KinematicID : UINT;
    Configuration : BYTE;  // 로봇 구성 (왼손/오른손 등)
END_VAR
VAR_OUTPUT
    JointPositions : ARRAY[1..6] OF LREAL;  // 계산된 축 위치
    bValid : BOOL;
    nErrorID : UDINT;
END_VAR
```

### DIN 66025 로봇 프로그래밍

키네마틱 변환과 함께 G-Code를 사용하여 로봇을 프로그래밍할 수 있습니다.

```gcode
; 6축 로봇 G-Code 프로그램

N10 G90 G71              ; 절대 좌표, mm
N20 G00 X100 Y200 Z300   ; 데카르트 좌표로 이동
N30 G01 X200 Y200 Z300 F500  ; 직선 이동
N40 G02 X200 Y100 I0 J-100   ; 원호 이동
N50 G00 X100 Y100 Z400       ; 안전 위치로 복귀
N60 M30
```

### PLCopen 로봇 제어

```iecst
PROGRAM RobotControl
VAR
    fbMoveCart : MC_MoveCartesian;  // 데카르트 좌표 이동
    Robot : AXIS_REF_ROBOT;         // 로봇 참조

    CartPos : ST_CartesianPosition;
    bStartMove : BOOL;
END_VAR

// 데카르트 좌표 설정
CartPos.X := 500.0;
CartPos.Y := 300.0;
CartPos.Z := 200.0;
CartPos.A := 0.0;
CartPos.B := 90.0;
CartPos.C := 0.0;

// 로봇 이동
fbMoveCart(
    Robot := Robot,
    Execute := bStartMove,
    Position := CartPos,
    Velocity := 1000.0,
    Acceleration := 5000.0
);
```

### 사용자 정의 키네마틱 구현 (C++)

고급 사용자는 C++로 자체 키네마틱을 구현할 수 있습니다.

```cpp
// C++ 키네마틱 인터페이스 예시

class MyCustomKinematic : public ITransformation
{
public:
    // 순방향 변환 구현
    virtual bool ForwardTransformation(
        const double* pJointPos,
        double* pCartPos,
        void* pChannelParams
    ) override
    {
        // 관절 좌표 → 데카르트 좌표 변환 로직
        // ...
        return true;
    }

    // 역방향 변환 구현
    virtual bool InverseTransformation(
        const double* pCartPos,
        double* pJointPos,
        void* pChannelParams,
        unsigned char config
    ) override
    {
        // 데카르트 좌표 → 관절 좌표 변환 로직
        // ...
        return true;
    }
};
```

---

## 최신 기능 (Build 4024/4026)

### TwinCAT 3.1 Build 4024 주요 개선사항

#### 1. PLC 프로그래밍 향상

**인터페이스 포인터 역참조 지원**

```iecst
// 이제 가능한 문법
INTERFACE I_Motor
    METHOD Start : BOOL;
END_INTERFACE

VAR
    pMotor : POINTER TO I_Motor;
END_VAR

// 포인터를 통한 메서드 호출
IF pMotor <> 0 THEN
    pMotor^.Start();
END_IF
```

**추상 클래스 지원**

```iecst
// 추상 베이스 클래스
{attribute 'abstract'}
FUNCTION_BLOCK FB_BaseSensor IMPLEMENTS I_Sensor
VAR
    sensorValue : REAL;
END_VAR

// 추상 메서드 정의
METHOD ABSTRACT Read : REAL
```

#### 2. 보안 통신

**ADS over TLS 암호화**

```iecst
// TLS 보안 연결 설정
VAR
    fbSecureADS : FB_SecureADSConnection;
    secureConnection : ST_ADS_SecureConnection;
END_VAR

secureConnection.UseTLS := TRUE;
secureConnection.CertificatePath := 'C:\TwinCAT\Cert\device.crt';

fbSecureADS(
    Connection := secureConnection,
    NetId := '192.168.1.100.1.1',
    Port := 851
);
```

#### 3. 다중 코어 모션 제어 (TwinCAT MC3)

**모듈식 아키텍처**

- **무제한 축 수**: 하드웨어 성능에만 의존
- **멀티 코어 지원**: CPU 코어 간 모션 분산
- **멀티 태스크 동기화**: 여러 코어에 걸친 축 동기화

```iecst
// 멀티 코어 축 할당 예시
VAR_GLOBAL
    Axis1 : AXIS_REF;  // Core 1에 할당
    Axis2 : AXIS_REF;  // Core 1에 할당
    Axis3 : AXIS_REF;  // Core 2에 할당
    Axis4 : AXIS_REF;  // Core 2에 할당
END_VAR

// 코어 간 동기화된 모션
fbSyncMotion(
    AxisGroup := [Axis1, Axis2, Axis3, Axis4],
    SyncMode := mcCrossCore
);
```

#### 4. Visual Studio 2019 지원

- Build 4024.10 이상에서 Visual Studio 2019 완전 지원
- Visual Studio 2017 Shell 통합 유지

### TwinCAT 3.1 Build 4026 추가 기능

Build 4026은 Build 4024의 모든 기능을 포함하며 추가 개선사항이 있습니다.

**주요 개선사항:**
- 성능 최적화
- 안정성 향상
- 추가 프로토콜 지원
- 디버깅 도구 개선

### TwinCAT 3 Motion Designer

**드라이브 치수 계산 및 시뮬레이션 도구**

- 모션 프로파일 설계 및 시각화
- 부하 계산 및 드라이브 선정
- 에너지 소비 분석
- 최적화된 모션 파라미터 생성

---

## 모범 사례 및 권장사항

### 1. 축 제어 초기화 패턴

```iecst
PROGRAM AxisInitialization
VAR
    Axis1 : AXIS_REF;
    fbPower : MC_Power;
    fbReset : MC_Reset;
    fbHome : MC_Home;

    nInitStep : INT := 0;
END_VAR

CASE nInitStep OF
    0: // 오류 확인 및 리셋
        IF Axis1.Status.Error THEN
            fbReset(Axis := Axis1, Execute := TRUE);
            IF fbReset.Done THEN
                fbReset(Execute := FALSE);
                nInitStep := 10;
            END_IF
        ELSE
            nInitStep := 10;
        END_IF

    10: // 축 활성화
        fbPower(
            Axis := Axis1,
            Enable := TRUE,
            Enable_Positive := TRUE,
            Enable_Negative := TRUE
        );

        IF fbPower.Status THEN
            nInitStep := 20;
        END_IF

    20: // 원점 복귀
        fbHome(
            Axis := Axis1,
            Execute := TRUE,
            Position := 0.0,
            HomingMode := MC_DefaultHoming
        );

        IF fbHome.Done THEN
            fbHome(Execute := FALSE);
            nInitStep := 30;
        END_IF

    30: // 초기화 완료
        // 정상 운전 모드로 전환
        ;
END_CASE
```

### 2. 에러 처리

```iecst
FUNCTION_BLOCK FB_MotionErrorHandler
VAR_INPUT
    Axis : AXIS_REF;
END_VAR
VAR_OUTPUT
    ErrorDescription : STRING(255);
    ErrorRecovered : BOOL;
END_VAR
VAR
    fbReset : MC_Reset;
    nRetryCount : INT;
END_VAR

IF Axis.Status.Error THEN
    // 에러 코드에 따른 처리
    CASE Axis.Status.ErrorID OF
        16#4260: // Following error
            ErrorDescription := '추종 오류: 위치 편차 과대';

        16#4263: // I/O error
            ErrorDescription := 'I/O 오류: 드라이브 통신 실패';

        16#4B09: // Software limit switch
            ErrorDescription := '소프트웨어 리미트 스위치 도달';

        ELSE
            ErrorDescription := CONCAT('알 수 없는 오류: 0x',
                                      UDINT_TO_HEXSTR(Axis.Status.ErrorID));
    END_CASE

    // 자동 복구 시도
    IF nRetryCount < 3 THEN
        fbReset(Axis := Axis, Execute := TRUE);
        IF fbReset.Done THEN
            fbReset(Execute := FALSE);
            nRetryCount := nRetryCount + 1;
            ErrorRecovered := TRUE;
        END_IF
    END_IF
ELSE
    nRetryCount := 0;
    ErrorRecovered := FALSE;
END_IF
```

### 3. 버퍼 모드 활용

```iecst
// 연속 모션 프로파일 (부드러운 블렌딩)
fbMove1(
    Axis := Axis1,
    Execute := TRUE,
    Position := 100.0,
    Velocity := 500.0,
    BufferMode := mcAborting  // 첫 번째 모션
);

fbMove2(
    Axis := Axis1,
    Execute := TRUE,
    Position := 200.0,
    Velocity := 500.0,
    BufferMode := mcBlendingPrevious  // 이전 모션과 블렌딩
);

fbMove3(
    Axis := Axis1,
    Execute := TRUE,
    Position := 300.0,
    Velocity := 500.0,
    BufferMode := mcBlendingPrevious  // 부드러운 연속 동작
);
```

---

## 참고 자료

### 공식 문서

- [Beckhoff Information System - TwinCAT 3](https://infosys.beckhoff.com/)
- [TF5000 - NC PTP Manual](https://download.beckhoff.com/download/document/automation/twincat3/TF50x0_TC3_NC_PTP_EN.pdf)
- [TF5050 - NC Camming Manual](https://download.beckhoff.com/download/document/automation/twincat3/TF5050_TC3_NC_Camming_EN.pdf)
- [TF5100 - NC I Manual](https://download.beckhoff.com/download/document/automation/twincat3/TF5100_TC3_NC_I_EN.pdf)
- [TF5240 - Kinematic Transformations Manual](https://download.beckhoff.com/download/document/automation/twincat3/TF5240_kinematic_transformation_en.pdf)

### 라이브러리 문서

- [Tc2_MC2 Library Reference](https://infosys.beckhoff.com/content/1033/tcplclib_tc2_mc2/)
- [Tc3_DriveMotionControl Library](https://download.beckhoff.com/download/document/automation/twincat3/TwinCAT_3_PLC_Lib_Tc3_DriveMotionControl_EN.pdf)

### PLCopen 표준

- [PLCopen Motion Control Part 1-3](https://plcopen.org/technical-activities/motion-control)

### GitHub 리소스

- [LCLS TwinCAT Motion Library](https://github.com/pcdshub/lcls-twincat-motion)

---

**마지막 업데이트**: 2025-11-24
**TwinCAT 버전**: 3.1 Build 4024/4026
**문서 버전**: 1.0
