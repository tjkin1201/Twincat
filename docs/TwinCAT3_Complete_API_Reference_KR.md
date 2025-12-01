# 🚀 TwinCAT 3 완전한 API 레퍼런스

> **📘 Beckhoff TwinCAT 3 - 모든 API 기능 종합 가이드**
> 최종 업데이트: 2025년 1월
> 작성자: Deep Research Agent with Context7 MCP
> 버전: 1.0

---

## 📑 목차

- [1️⃣ 개요 및 아키텍처](#1️⃣-개요-및-아키텍처)
- [2️⃣ ADS (Automation Device Specification) API](#2️⃣-ads-automation-device-specification-api)
- [3️⃣ PLC 프로그래밍 API](#3️⃣-plc-프로그래밍-api)
- [4️⃣ Motion Control (NC) API](#4️⃣-motion-control-nc-api)
- [5️⃣ HMI (Human Machine Interface) API](#5️⃣-hmi-human-machine-interface-api)
- [6️⃣ IoT & 통신 API](#6️⃣-iot--통신-api)
- [7️⃣ Vision (머신 비전) API](#7️⃣-vision-머신-비전-api)
- [8️⃣ Database Server API](#8️⃣-database-server-api)
- [9️⃣ Scope & Measurement API](#9️⃣-scope--measurement-api)
- [🔟 Safety (안전) API](#🔟-safety-안전-api)
- [1️⃣1️⃣ Analytics & Machine Learning API](#1️⃣1️⃣-analytics--machine-learning-api)
- [1️⃣2️⃣ Automation Interface (.NET API)](#1️⃣2️⃣-automation-interface-net-api)

---

## 1️⃣ 개요 및 아키텍처

### 🎯 TwinCAT 3이란?

**TwinCAT (The Windows Control and Automation Technology)**는 Beckhoff의 PC 기반 실시간 제어 플랫폼입니다.

```
┌─────────────────────────────────────────────────────────┐
│                    TwinCAT 3 아키텍처                     │
├─────────────────────────────────────────────────────────┤
│  Applications                                            │
│  ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────┐   │
│  │   HMI   │  │ Database│  │   IoT   │  │ Analytics│   │
│  └────┬────┘  └────┬────┘  └────┬────┘  └────┬────┘   │
│       └────────────┴────────────┴────────────┘         │
│                       │                                  │
│  ┌─────────────────────────────────────────────────┐   │
│  │           ADS (통신 프로토콜 레이어)              │   │
│  └─────────────────────────────────────────────────┘   │
│                       │                                  │
│  ┌─────────────────────────────────────────────────┐   │
│  │       TwinCAT Runtime (실시간 커널)               │   │
│  │  ┌──────┐  ┌──────┐  ┌──────┐  ┌──────┐        │   │
│  │  │ PLC  │  │  NC  │  │ I/O  │  │Vision│        │   │
│  │  └──────┘  └──────┘  └──────┘  └──────┘        │   │
│  └─────────────────────────────────────────────────┘   │
│                       │                                  │
│  ┌─────────────────────────────────────────────────┐   │
│  │           EtherCAT / Fieldbus                     │   │
│  └─────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
```

### 🌟 주요 특징

| 특징 | 설명 |
|------|------|
| 🔄 **실시간 성능** | Windows 위에서 하드 실시간 제어 (마이크로초 단위) |
| 🧩 **모듈화** | PLC, Motion, Vision, IoT 등 독립 모듈 구성 |
| 🌐 **개방성** | C++, C#, Python 등 다양한 언어 지원 |
| 📡 **통신** | ADS, OPC UA, MQTT, EtherCAT 등 |
| 🛡️ **안전성** | IEC 61508 SIL 3 인증 |

### 📦 주요 API 카테고리

```
TwinCAT 3 APIs
│
├── 🔌 Core Communication APIs
│   ├── ADS (Automation Device Specification)
│   └── OPC UA
│
├── 💻 Programming APIs
│   ├── IEC 61131-3 (ST, LD, FBD, SFC, IL)
│   ├── C++ (TcCOM)
│   └── .NET (Automation Interface)
│
├── 🤖 Motion & Control APIs
│   ├── PLCopen Motion Control
│   ├── CNC Programming
│   └── Kinematic Transformations
│
├── 🖥️ HMI & Visualization APIs
│   ├── JavaScript/TypeScript API
│   └── Server Extensions (C#)
│
├── 🌐 IoT & Connectivity APIs
│   ├── MQTT
│   ├── Cloud (AWS, Azure)
│   └── Web Services
│
└── 🔬 Advanced APIs
    ├── Machine Vision
    ├── Database Integration
    ├── Measurement & Scope
    ├── Safety (TwinSAFE)
    └── Machine Learning
```

---

## 2️⃣ ADS (Automation Device Specification) API

### 📡 ADS란?

**ADS (Automation Device Specification)**는 TwinCAT의 핵심 통신 프로토콜로, 모든 TwinCAT 모듈 간 데이터 교환을 담당합니다.

### 🔑 핵심 개념

#### AmsNetId (AMS 네트워크 ID)

```
형식: xxx.xxx.xxx.xxx.xxx.xxx
예시: 192.168.1.100.1.1

┌─────────┬─────────┐
│ NetId   │ Port    │
├─────────┼─────────┤
│ IPv4식  │ 2바이트 │
│ 6바이트 │         │
└─────────┴─────────┘
```

#### 주요 포트 번호

| 포트 | 용도 | 설명 |
|------|------|------|
| 🎯 **10000** | ADS Router | ADS 라우터 서비스 |
| 🔵 **350** | System Service | 시스템 서비스 |
| 🟢 **851** | PLC Runtime (첫 번째) | PLC 런타임 TC3 Port |
| 🟡 **501** | NC (첫 번째 축) | NC I 인터프리터 |
| 🔴 **500** | NC SAF | NC Safety |

### 🛠️ ADS API 함수

#### 기본 읽기/쓰기

```cpp
// ✅ C++ - 변수 읽기
long AdsSyncReadReq(
    PAmsAddr pAddr,        // 대상 AMS 주소
    unsigned long indexGroup,   // IndexGroup
    unsigned long indexOffset,  // IndexOffset
    unsigned long length,       // 읽을 데이터 길이
    void* pData                 // 데이터 버퍼
);

// ✅ C++ - 변수 쓰기
long AdsSyncWriteReq(
    PAmsAddr pAddr,
    unsigned long indexGroup,
    unsigned long indexOffset,
    unsigned long length,
    void* pData
);
```

```csharp
// ✅ C# - TwinCAT.Ads 라이브러리
using TwinCAT.Ads;

TcAdsClient client = new TcAdsClient();
client.Connect("192.168.1.100.1.1", 851);

// 변수 읽기 (심볼릭 방식)
int varHandle = client.CreateVariableHandle("MAIN.counter");
int value = (int)client.ReadAny(varHandle, typeof(int));
client.DeleteVariableHandle(varHandle);

// 변수 쓰기
int newHandle = client.CreateVariableHandle("MAIN.speed");
client.WriteAny(newHandle, 100.5);
client.DeleteVariableHandle(newHandle);
```

```python
# ✅ Python - pyads 라이브러리
import pyads

plc = pyads.Connection('192.168.1.100.1.1', 851)
plc.open()

# 변수 읽기
temperature = plc.read_by_name("MAIN.temperature", pyads.PLCTYPE_REAL)
print(f"온도: {temperature}°C")

# 변수 쓰기
plc.write_by_name("MAIN.setpoint", 25.5, pyads.PLCTYPE_REAL)

plc.close()
```

#### 📢 ADS Notification (알림)

실시간 변수 변화 감지를 위한 **푸시(Push) 메커니즘**입니다.

```csharp
// C# - 알림 등록
using TwinCAT.Ads;

TcAdsClient client = new TcAdsClient();
client.Connect("192.168.1.100.1.1", 851);

// 알림 핸들러 등록
client.AdsNotification += (sender, e) =>
{
    var value = BitConverter.ToInt32(e.Data, 0);
    Console.WriteLine($"변수 변경: {value}");
};

// 알림 추가 (1초마다 또는 값 변경 시)
int notificationHandle = client.AddDeviceNotification(
    "MAIN.counter",
    new AdsStream(4),
    AdsTransMode.OnChange,  // OnChange 또는 Cyclic
    1000,  // 사이클 타임 (ms)
    0,
    null
);

// ... 프로그램 실행 ...

// 알림 해제
client.DeleteDeviceNotification(notificationHandle);
```

```python
# Python - pyads 알림
import pyads
import time

def callback(notification, data):
    """알림 콜백 함수"""
    value = notification.value
    print(f"온도 변경: {value}°C")

plc = pyads.Connection('192.168.1.100.1.1', 851)
plc.open()

# 알림 등록
attr = pyads.NotificationAttrib(4)  # 4바이트 (REAL)
handle = plc.add_device_notification(
    "MAIN.temperature",
    attr,
    callback
)

# 프로그램 실행 (알림 수신 대기)
time.sleep(60)

# 알림 해제
plc.del_device_notification(handle)
plc.close()
```

#### ⚡ Sum Command (일괄 처리)

여러 변수를 한 번에 읽기/쓰기하여 **성능을 극대화**합니다.

```csharp
// C# - Sum Command로 여러 변수 한 번에 읽기
using TwinCAT.Ads;

TcAdsClient client = new TcAdsClient();
client.Connect("192.168.1.100.1.1", 851);

// 여러 변수 핸들 생성
int handle1 = client.CreateVariableHandle("MAIN.temp1");
int handle2 = client.CreateVariableHandle("MAIN.temp2");
int handle3 = client.CreateVariableHandle("MAIN.temp3");

// Sum Command로 일괄 읽기
SumRead sumRead = new SumRead(client);
sumRead.AddReadCommand(handle1, typeof(float));
sumRead.AddReadCommand(handle2, typeof(float));
sumRead.AddReadCommand(handle3, typeof(float));

sumRead.Execute();

// 결과 추출
float temp1 = (float)sumRead.ReadValues[0];
float temp2 = (float)sumRead.ReadValues[1];
float temp3 = (float)sumRead.ReadValues[2];

Console.WriteLine($"온도1: {temp1}, 온도2: {temp2}, 온도3: {temp3}");

// 핸들 삭제
client.DeleteVariableHandle(handle1);
client.DeleteVariableHandle(handle2);
client.DeleteVariableHandle(handle3);
```

### 🔐 Secure ADS

**TLS 1.2 암호화** 통신 지원 (TwinCAT 3.1.4024 이상)

```ini
# TcAdsSecure.ini 설정
[GENERAL]
UseTLS=1

[CERTIFICATES]
ServerCertificate=server.pem
ServerKey=server.key
TrustedCerts=ca.pem
```

```csharp
// C# - Secure ADS 연결
TcAdsClient client = new TcAdsClient();
client.Connect("192.168.1.100.1.1", 8016);  // 포트 8016 (Secure ADS)
```

### 📊 ADS 에러 코드

| 코드 | 이름 | 설명 | 해결 방법 |
|------|------|------|-----------|
| `0x0` | **ERR_NOERROR** | 성공 | - |
| `0x700` (1792) | **ERR_TARGETPORTNOTFOUND** | 대상 포트를 찾을 수 없음 | PLC가 실행 중인지 확인, 포트 번호 확인 |
| `0x704` (1796) | **ERR_TARGETMACHINENOTFOUND** | 대상 머신을 찾을 수 없음 | AmsNetId 확인, 라우터 설정 확인 |
| `0x745` (1861) | **ERR_SYMBOLNOTFOUND** | 심볼을 찾을 수 없음 | 변수명 확인, 온라인 모드 확인 |
| `0x1013` (4115) | **ERR_INVALIDSIZE** | 잘못된 크기 | 데이터 타입 크기 확인 |

### 🎓 ADS API 학습 경로

```
1단계: 기본 읽기/쓰기
   ├── AdsSyncReadReq
   └── AdsSyncWriteReq

2단계: 심볼릭 액세스
   ├── CreateVariableHandle
   ├── ReadWrite
   └── DeleteVariableHandle

3단계: 알림 (Notification)
   ├── AddDeviceNotification
   └── DeleteDeviceNotification

4단계: 성능 최적화
   ├── Sum Command
   ├── 비동기 I/O
   └── 핸들 재사용

5단계: 고급 기능
   ├── Secure ADS
   ├── 라우터 프로그래밍
   └── 멀티스레딩
```

---

## 3️⃣ PLC 프로그래밍 API

### 💡 IEC 61131-3 프로그래밍 언어

TwinCAT 3는 **5가지 표준 PLC 프로그래밍 언어**를 지원합니다.

| 언어 | 아이콘 | 유형 | 사용 사례 |
|------|--------|------|-----------|
| **ST** (Structured Text) | 📝 | 텍스트 | 복잡한 알고리즘, 수학 연산 |
| **LD** (Ladder Diagram) | 🪜 | 그래픽 | 릴레이 로직, 간단한 제어 |
| **FBD** (Function Block Diagram) | 🔷 | 그래픽 | 신호 흐름, 프로세스 제어 |
| **SFC** (Sequential Function Chart) | 📊 | 그래픽 | 순차 제어, 상태 머신 |
| **IL** (Instruction List) | 🔤 | 텍스트 | 저수준 최적화 (레거시) |

### 🧱 POU (Program Organization Unit)

```
POU 구조
│
├── PROGRAM (프로그램)
│   └── 사이클릭 실행 (MAIN 등)
│
├── FUNCTION_BLOCK (함수 블록)
│   ├── 내부 상태 유지
│   └── 인스턴스 필요
│
└── FUNCTION (함수)
    ├── 상태 없음
    └── 순수 함수
```

#### ST 예제: Function Block

```iecst
// ✅ 모터 제어 Function Block
FUNCTION_BLOCK FB_MotorControl
VAR_INPUT
    bEnable : BOOL;           // 모터 활성화
    fTargetSpeed : REAL;      // 목표 속도 [rpm]
    bReset : BOOL;            // 리셋
END_VAR

VAR_OUTPUT
    fCurrentSpeed : REAL;     // 현재 속도 [rpm]
    bRunning : BOOL;          // 실행 중
    bFault : BOOL;            // 고장 상태
END_VAR

VAR
    fAcceleration : REAL := 10.0;  // 가속도 [rpm/s]
    fMaxSpeed : REAL := 1500.0;    // 최대 속도
    eState : (IDLE, STARTING, RUNNING, STOPPING, FAULT) := IDLE;
END_VAR

// 메인 로직
CASE eState OF
    IDLE:
        IF bEnable THEN
            eState := STARTING;
        END_IF;

    STARTING:
        IF fCurrentSpeed < fTargetSpeed THEN
            fCurrentSpeed := fCurrentSpeed + fAcceleration * 0.01;  // 10ms 사이클 가정
        ELSE
            eState := RUNNING;
        END_IF;

    RUNNING:
        bRunning := TRUE;
        IF NOT bEnable THEN
            eState := STOPPING;
        ELSIF fCurrentSpeed > fMaxSpeed THEN
            eState := FAULT;
        END_IF;

    STOPPING:
        IF fCurrentSpeed > 0 THEN
            fCurrentSpeed := fCurrentSpeed - fAcceleration * 0.01;
        ELSE
            fCurrentSpeed := 0;
            bRunning := FALSE;
            eState := IDLE;
        END_IF;

    FAULT:
        bFault := TRUE;
        fCurrentSpeed := 0;
        bRunning := FALSE;
        IF bReset THEN
            bFault := FALSE;
            eState := IDLE;
        END_IF;
END_CASE
```

### 📚 주요 시스템 라이브러리

#### 1. **Tc2_System** - 시스템 함수

```iecst
// ✅ 파일 쓰기 예제
PROGRAM FileWriteExample
VAR
    fbFileOpen : FB_FileOpen;
    fbFileWrite : FB_FileWrite;
    fbFileClose : FB_FileClose;

    sFileName : STRING := 'C:\Temp\log.txt';
    sContent : STRING := 'TwinCAT 로그 데이터';
    hFile : UINT;
    bBusy : BOOL;
    bError : BOOL;
END_VAR

// 파일 열기
fbFileOpen(
    sPathName := sFileName,
    nMode := FOPEN_MODEWRITE OR FOPEN_MODETEXT,
    ePath := PATH_GENERIC,
    bExecute := TRUE
);

IF fbFileOpen.bError THEN
    bError := TRUE;
ELSIF NOT fbFileOpen.bBusy AND fbFileOpen.hFile <> 0 THEN
    hFile := fbFileOpen.hFile;

    // 파일 쓰기
    fbFileWrite(
        hFile := hFile,
        pWriteBuff := ADR(sContent),
        cbWriteLen := LEN(sContent),
        bExecute := TRUE
    );

    IF NOT fbFileWrite.bBusy THEN
        // 파일 닫기
        fbFileClose(
            hFile := hFile,
            bExecute := TRUE
        );
    END_IF;
END_IF
```

```iecst
// ✅ 시간 함수
PROGRAM TimeExample
VAR
    currentTime : DT;
    sysTime : TIMESTRUCT;
END_VAR

// 현재 시스템 시간 가져오기
currentTime := NT_GetTime();

// TIMESTRUCT로 변환
SYSTEMTIME_TO_DT(currentTime, sysTime);

// 로그 출력
ADSLOGSTR(
    msgCtrlMask := ADSLOG_MSGTYPE_HINT,
    msgFmtStr := '현재 시간: %s',
    strArg := DT_TO_STRING(currentTime)
);
```

#### 2. **Tc2_Utilities** - 유틸리티 함수

```iecst
// ✅ 문자열 포맷팅
PROGRAM StringFormatExample
VAR
    fbFormatString : FB_FormatString;
    sFormat : STRING := '온도: %.2f°C, 습도: %d%%';
    fTemperature : REAL := 23.456;
    nHumidity : INT := 65;
    sResult : STRING(255);
END_VAR

fbFormatString(
    sFormat := sFormat,
    arg1 := F_REAL(fTemperature),
    arg2 := F_INT(nHumidity),
    sOut => sResult
);
// 결과: "온도: 23.46°C, 습도: 65%"
```

```iecst
// ✅ 문자열 검색 및 치환
PROGRAM StringSearchExample
VAR
    sSource : STRING := 'TwinCAT PLC 프로그래밍';
    sFind : STRING := 'PLC';
    sReplace : STRING := 'Automation';
    sResult : STRING;
    nPosition : INT;
END_VAR

// 문자열 검색
nPosition := FIND2(sSource, sFind);  // 반환: 9

// 문자열 치환
sResult := REPLACE2(sSource, sFind, sReplace, 1);
// 결과: "TwinCAT Automation 프로그래밍"
```

#### 3. **Tc2_MC2** / **Tc3_MC3** - Motion Control

```iecst
// ✅ PLCopen Motion Control 예제
PROGRAM MotionExample
VAR
    axis : AXIS_REF;              // 축 참조
    mcPower : MC_Power;           // 전원 제어
    mcHome : MC_Home;             // 홈 복귀
    mcMoveAbs : MC_MoveAbsolute;  // 절대 위치 이동

    bExecute : BOOL;
    fPosition : LREAL := 100.0;   // 목표 위치 [mm]
    fVelocity : LREAL := 50.0;    // 속도 [mm/s]
END_VAR

// 1단계: 축 전원 켜기
mcPower(
    Axis := axis,
    Enable := TRUE,
    Enable_Positive := TRUE,
    Enable_Negative := TRUE
);

// 2단계: 홈 복귀
IF mcPower.Status THEN
    mcHome(
        Axis := axis,
        Execute := bExecute,
        Position := 0.0
    );
END_IF

// 3단계: 절대 위치 이동
IF mcHome.Done THEN
    mcMoveAbs(
        Axis := axis,
        Execute := bExecute,
        Position := fPosition,
        Velocity := fVelocity,
        Acceleration := 100.0,
        Deceleration := 100.0
    );
END_IF
```

### 🎨 객체 지향 프로그래밍 (OOP)

TwinCAT 3는 **METHOD**, **PROPERTY**, **INTERFACE** 등 OOP 기능을 지원합니다.

```iecst
// ✅ METHOD 예제
FUNCTION_BLOCK FB_Tank
VAR
    fLevel : REAL;           // 탱크 레벨 [%]
    fCapacity : REAL := 1000.0;  // 용량 [L]
END_VAR

// METHOD: FillTank (탱크 채우기)
METHOD FillTank : BOOL
VAR_INPUT
    fAmount : REAL;  // 채울 양 [L]
END_VAR

IF (fLevel + fAmount / fCapacity * 100.0) <= 100.0 THEN
    fLevel := fLevel + fAmount / fCapacity * 100.0;
    FillTank := TRUE;  // 성공
ELSE
    FillTank := FALSE; // 실패 (넘침)
END_IF
```

```iecst
// ✅ PROPERTY 예제
PROPERTY Level : REAL
// Getter
Level := fLevel;

// Setter
SET:
    IF Level >= 0 AND Level <= 100 THEN
        fLevel := Level;
    END_IF
```

### 🔗 ADS를 통한 PLC 제어 (C#)

```csharp
// ✅ C#에서 PLC 시작/중지
using TwinCAT.Ads;

TcAdsClient client = new TcAdsClient();
client.Connect("192.168.1.100.1.1", 10000);  // System Service 포트

// PLC 상태 읽기
AdsState state;
ushort deviceState;
client.ReadState(out state, out deviceState);

if (state == AdsState.Stop)
{
    // PLC 시작
    client.WriteControl(new StateInfo(AdsState.Run, deviceState));
    Console.WriteLine("PLC 시작됨");
}
else if (state == AdsState.Run)
{
    // PLC 중지
    client.WriteControl(new StateInfo(AdsState.Stop, deviceState));
    Console.WriteLine("PLC 중지됨");
}
```

### 📊 데이터 타입 레퍼런스

#### 기본 데이터 타입

| 타입 | 크기 | 범위 | 예제 |
|------|------|------|------|
| **BOOL** | 1bit | TRUE/FALSE | `bFlag : BOOL := TRUE;` |
| **BYTE** | 8bit | 0..255 | `byData : BYTE := 16#FF;` |
| **WORD** | 16bit | 0..65535 | `wValue : WORD := 16#ABCD;` |
| **DWORD** | 32bit | 0..4294967295 | `dwCounter : DWORD;` |
| **SINT** | 8bit | -128..127 | `siTemp : SINT := -50;` |
| **INT** | 16bit | -32768..32767 | `iSpeed : INT := 1000;` |
| **DINT** | 32bit | -2^31..2^31-1 | `diPosition : DINT;` |
| **REAL** | 32bit | IEEE 754 | `fTemperature : REAL := 23.5;` |
| **LREAL** | 64bit | IEEE 754 | `lfPreciseValue : LREAL;` |
| **STRING** | 가변 | 문자열 | `sName : STRING := 'TwinCAT';` |
| **TIME** | 32bit | 시간 간격 | `tDelay : TIME := T#5s;` |
| **DT** | 32bit | 날짜/시간 | `dtNow : DT;` |

#### 복합 데이터 타입

```iecst
// ✅ STRUCT (구조체)
TYPE ST_Recipe :
STRUCT
    sName : STRING(50);        // 레시피 이름
    fTemperature : REAL;       // 온도 [°C]
    tDuration : TIME;          // 지속 시간
    nPriority : INT;           // 우선순위
END_STRUCT
END_TYPE

// 사용 예시
VAR
    recipe1 : ST_Recipe := (
        sName := '레시피A',
        fTemperature := 80.0,
        tDuration := T#30m,
        nPriority := 1
    );
END_VAR
```

```iecst
// ✅ ENUM (열거형)
TYPE E_MachineState :
(
    IDLE := 0,
    STARTING := 10,
    RUNNING := 20,
    STOPPING := 30,
    ERROR := 99
);
END_TYPE

VAR
    eCurrentState : E_MachineState := E_MachineState.IDLE;
END_VAR

// 상태 전환
IF bStart THEN
    eCurrentState := E_MachineState.STARTING;
END_IF
```

```iecst
// ✅ ARRAY (배열)
VAR
    aTemperatures : ARRAY[1..10] OF REAL;  // 1차원 배열
    aMatrix : ARRAY[1..3, 1..3] OF INT;    // 2차원 배열
    i : INT;
END_VAR

// 배열 초기화
FOR i := 1 TO 10 DO
    aTemperatures[i] := 20.0;
END_FOR
```

---

## 4️⃣ Motion Control (NC) API

### 🤖 Motion Control 개요

TwinCAT Motion Control은 **PLCopen 표준**을 기반으로 단일 축부터 다축 동기화, CNC 가공까지 지원합니다.

```
Motion Control 계층 구조
│
├── 📐 Kinematic Transformations
│   └── 로봇, 5축 CNC 등
│
├── 🎯 CNC Programming
│   └── G-Code, GST
│
├── 🔄 Cam & Synchronization
│   └── 캠 프로파일, 전자 기어링
│
├── ⚙️ PLCopen Motion Control
│   └── MC_Power, MC_MoveAbsolute 등
│
└── 🔌 NC Axis (실축/가상축)
    └── EtherCAT 드라이브
```

### ⚙️ PLCopen Motion Control

#### 주요 Function Block

| FB | 기능 | 설명 |
|-----|------|------|
| **MC_Power** | 축 전원 | 축 활성화/비활성화 |
| **MC_Home** | 홈 복귀 | 기준점 설정 |
| **MC_Reset** | 리셋 | 에러 리셋 |
| **MC_MoveAbsolute** | 절대 위치 이동 | 절대 좌표로 이동 |
| **MC_MoveRelative** | 상대 위치 이동 | 현재 위치 기준 이동 |
| **MC_MoveVelocity** | 속도 제어 | 일정 속도로 이동 (조깅) |
| **MC_Stop** | 정지 | 감속 정지 |
| **MC_Halt** | 긴급 정지 | 즉시 정지 |
| **MC_Jog** | 조깅 | 수동 이동 |

#### 실전 예제: 컨베이어 제어

```iecst
// ✅ 컨베이어 벨트 제어 프로그램
PROGRAM ConveyorControl
VAR
    // 축 참조
    axisConveyor : AXIS_REF;

    // Motion Function Blocks
    mcPower : MC_Power;
    mcMoveVelocity : MC_MoveVelocity;
    mcStop : MC_Stop;

    // 제어 변수
    bEnable : BOOL;               // 활성화
    bStartForward : BOOL;         // 정방향 시작
    bStartReverse : BOOL;         // 역방향 시작
    bStop : BOOL;                 // 정지
    fSpeed : LREAL := 100.0;      // 속도 [mm/s]

    // 상태
    eState : (IDLE, FORWARD, REVERSE, STOPPING);
END_VAR

// 상태 머신
CASE eState OF
    IDLE:
        // 축 전원 켜기
        mcPower(
            Axis := axisConveyor,
            Enable := bEnable,
            Enable_Positive := TRUE,
            Enable_Negative := TRUE
        );

        IF mcPower.Status THEN
            IF bStartForward THEN
                eState := FORWARD;
            ELSIF bStartReverse THEN
                eState := REVERSE;
            END_IF
        END_IF;

    FORWARD:
        // 정방향 이동
        mcMoveVelocity(
            Axis := axisConveyor,
            Execute := TRUE,
            Velocity := fSpeed,
            Acceleration := 200.0,
            Deceleration := 200.0,
            Direction := MC_Positive_Direction
        );

        IF bStop THEN
            eState := STOPPING;
        ELSIF bStartReverse THEN
            eState := REVERSE;
        END_IF;

    REVERSE:
        // 역방향 이동
        mcMoveVelocity(
            Axis := axisConveyor,
            Execute := TRUE,
            Velocity := fSpeed,
            Acceleration := 200.0,
            Deceleration := 200.0,
            Direction := MC_Negative_Direction
        );

        IF bStop THEN
            eState := STOPPING;
        ELSIF bStartForward THEN
            eState := FORWARD;
        END_IF;

    STOPPING:
        // 정지
        mcStop(
            Axis := axisConveyor,
            Execute := TRUE,
            Deceleration := 200.0
        );

        IF mcStop.Done THEN
            eState := IDLE;
        END_IF;
END_CASE
```

### 🔄 캠 프로파일 & 동기화 (TF5050)

**캠 (Cam)**은 마스터 축과 슬레이브 축 간의 **비선형 관계**를 정의합니다.

```iecst
// ✅ 캠 동기화 예제
PROGRAM CamSyncExample
VAR
    axisMaster : AXIS_REF;        // 마스터 축 (예: 메인 컨베이어)
    axisSlave : AXIS_REF;         // 슬레이브 축 (예: 픽업 로봇)

    mcCamTableSelect : MC_CamTableSelect;  // 캠 테이블 선택
    mcCamIn : MC_CamIn;                    // 캠 동기화 시작
    mcCamOut : MC_CamOut;                  // 캠 동기화 종료

    bSelectCam : BOOL;
    bEngageCam : BOOL;
    bDisengageCam : BOOL;
END_VAR

// 1단계: 캠 테이블 선택
mcCamTableSelect(
    Master := axisMaster,
    Slave := axisSlave,
    Execute := bSelectCam,
    CamTable := 1,  // 캠 테이블 ID
    Periodic := TRUE
);

// 2단계: 캠 동기화 진입
IF mcCamTableSelect.Done THEN
    mcCamIn(
        Master := axisMaster,
        Slave := axisSlave,
        Execute := bEngageCam,
        MasterSyncPosition := 0.0,
        SlaveStartPosition := 0.0,
        StartMode := MC_CAMSTART_RELATIVE
    );
END_IF

// 3단계: 캠 동기화 해제
IF mcCamIn.InSync AND bDisengageCam THEN
    mcCamOut(
        Slave := axisSlave,
        Execute := TRUE
    );
END_IF
```

### 📐 CNC 프로그래밍 (TF5100)

TwinCAT은 **DIN 66025 G-Code**를 지원합니다.

```gcode
; ✅ G-Code 예제 (원 가공)
N10 G90 G54              ; 절대 좌표, 워크 좌표계
N20 G00 X0 Y0 Z100       ; 급속 이동 (안전 높이)
N30 G00 X50 Y50          ; 원 중심 근처로 이동
N40 G01 Z-10 F100        ; Z축 하강 (절삭 깊이)
N50 G02 X50 Y50 I25 J0 F200  ; 시계방향 원호 (반지름 25mm)
N60 G01 Z100 F100        ; Z축 상승
N70 M30                  ; 프로그램 종료
```

#### GST (G-Code in Structured Text)

```iecst
// ✅ GST - ST에서 G-Code 사용
PROGRAM CncProgram
VAR
    channel : NCCHANNEL_REF;
END_VAR

// G-Code 블록 실행
CASE nStep OF
    0:
        // 급속 이동
        G00(channel, X := 100.0, Y := 50.0);
        nStep := 10;

    10:
        // 직선 보간
        G01(channel, X := 200.0, Y := 150.0, F := 500.0);
        nStep := 20;

    20:
        // 원호 보간 (시계방향)
        G02(channel, X := 250.0, Y := 100.0, I := 25.0, J := 0.0, F := 300.0);
        nStep := 30;
END_CASE
```

### 🦾 Kinematic Transformations (TF5240)

로봇 제어를 위한 **순방향/역방향 변환**을 제공합니다.

```iecst
// ✅ 6축 로봇 제어
PROGRAM RobotControl
VAR
    kinematics : KINE_REF;          // 키네마틱 참조
    mcGroupEnable : MC_GroupEnable;
    mcMoveLinear : MC_MoveLinearAbsolute;

    // 목표 위치 (카르테시안 좌표)
    targetPos : MC_CartesianCoordinates := (
        X := 300.0,  // [mm]
        Y := 150.0,
        Z := 200.0,
        A := 0.0,    // [degree]
        B := 90.0,
        C := 0.0
    );
END_VAR

// 그룹 활성화
mcGroupEnable(
    AxesGroup := kinematics,
    Enable := TRUE
);

// 직선 이동 (카르테시안 좌표)
IF mcGroupEnable.Valid THEN
    mcMoveLinear(
        AxesGroup := kinematics,
        Execute := bExecute,
        CoordSystem := MC_CS_MCS,  // Machine Coordinate System
        Transition := MC_TRANSITION_NONE,
        Point := targetPos,
        Velocity := 50.0,
        Acceleration := 100.0,
        Deceleration := 100.0
    );
END_IF
```

### 📊 AXIS_REF 구조

```iecst
// AXIS_REF 주요 멤버
TYPE AXIS_REF :
STRUCT
    // 상태 정보
    NcToPlc : ST_NcToPlcAxle;  // NC → PLC 데이터
    PlcToNc : ST_PlcToNcAxle;  // PLC → NC 데이터

    // 위치/속도
    ActPos : LREAL;           // 현재 위치 [사용자 단위]
    ActVelo : LREAL;          // 현재 속도

    // 상태 플래그
    bEnabled : BOOL;          // 축 활성화
    bHomed : BOOL;            // 홈 완료
    bError : BOOL;            // 에러 발생
END_STRUCT
END_TYPE
```

---

## 5️⃣ HMI (Human Machine Interface) API

### 🖥️ TwinCAT HMI Framework

TwinCAT HMI는 **HTML5/JavaScript 기반** 웹 HMI 솔루션입니다.

```
TwinCAT HMI 아키텍처
│
├── 🌐 Browser (클라이언트)
│   ├── HTML5
│   ├── JavaScript/TypeScript
│   └── CSS3
│
├── 🔌 WebSocket (통신)
│   └── JSON 기반 ADS 프로토콜
│
└── 🖥️ HMI Server
    ├── Server Extensions (C#)
    └── ADS 연결 (PLC)
```

### 📱 JavaScript API

#### 심볼 읽기/쓰기

```javascript
// ✅ JavaScript - PLC 변수 읽기
(function (TcHmi) {
    // 심볼 읽기
    TcHmi.Symbol.read('MAIN.temperature', function (data) {
        if (data.error === TcHmi.Errors.NONE) {
            console.log('온도:', data.value, '°C');

            // 화면 요소 업데이트
            var textBox = TcHmi.Controls.get('TextBox_Temperature');
            textBox.setText(data.value.toFixed(2) + '°C');
        } else {
            console.error('읽기 오류:', data.error);
        }
    });
})(TcHmi);
```

```javascript
// ✅ JavaScript - PLC 변수 쓰기
(function (TcHmi) {
    var button = TcHmi.Controls.get('Button_SetSpeed');

    button.onPressed = function () {
        var newSpeed = 1500; // rpm

        // 심볼 쓰기
        TcHmi.Symbol.write('MAIN.motorSpeed', newSpeed, function (data) {
            if (data.error === TcHmi.Errors.NONE) {
                console.log('속도 설정 완료:', newSpeed);
            } else {
                console.error('쓰기 오류:', data.error);
            }
        });
    };
})(TcHmi);
```

#### 실시간 데이터 바인딩

```javascript
// ✅ 구조체 배열 읽기
(function (TcHmi) {
    // PLC의 구조체 배열
    // VAR
    //     aSensors : ARRAY[1..5] OF ST_Sensor;
    // END_VAR

    TcHmi.Symbol.read('MAIN.aSensors', function (data) {
        if (data.error === TcHmi.Errors.NONE) {
            var sensors = data.value;

            // 각 센서 데이터 처리
            sensors.forEach(function (sensor, index) {
                console.log('센서 ' + (index + 1) + ':');
                console.log('  이름:', sensor.sName);
                console.log('  값:', sensor.fValue);
                console.log('  활성:', sensor.bActive);
            });
        }
    });
})(TcHmi);
```

### 🔧 서버 확장 (Server Extension)

C#으로 서버 기능을 확장할 수 있습니다.

```csharp
// ✅ C# - 서버 확장 예제
using TcHmiSrv.Core;
using TcHmiSrv.Core.General;
using TcHmiSrv.Core.Tools.Management;

namespace MyHmiExtension
{
    // 커맨드: 데이터베이스 쿼리
    [Command(Name = "QueryDatabase")]
    public Value QueryDatabase(Context context, string query)
    {
        // SQL 쿼리 실행
        var connection = new SqlConnection("...");
        var command = new SqlCommand(query, connection);

        connection.Open();
        var reader = command.ExecuteReader();

        var results = new List<Dictionary<string, object>>();
        while (reader.Read())
        {
            var row = new Dictionary<string, object>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.GetValue(i);
            }
            results.Add(row);
        }

        connection.Close();

        // HMI로 결과 반환
        return Value.Create(results);
    }
}
```

```javascript
// ✅ JavaScript - 서버 확장 호출
(function (TcHmi) {
    TcHmi.Server.execute('MyHmiExtension', 'QueryDatabase', {
        query: 'SELECT * FROM production_log WHERE date = CURDATE()'
    }, function (data) {
        if (data.error === TcHmi.Errors.NONE) {
            var results = data.response;
            console.log('쿼리 결과:', results);

            // 데이터그리드에 표시
            var grid = TcHmi.Controls.get('DataGrid_ProductionLog');
            grid.setSrcData(results);
        }
    });
})(TcHmi);
```

### 🔔 이벤트 처리

```javascript
// ✅ 컨트롤 이벤트 처리
(function (TcHmi) {
    var slider = TcHmi.Controls.get('Slider_Speed');

    // 값 변경 이벤트
    slider.onValueChanged = function (newValue) {
        console.log('슬라이더 값:', newValue);

        // PLC에 쓰기
        TcHmi.Symbol.write('MAIN.setpointSpeed', newValue);
    };

    // 마우스 다운 이벤트
    slider.onMouseDown = function () {
        console.log('슬라이더 조작 시작');
    };

    // 마우스 업 이벤트
    slider.onMouseUp = function () {
        console.log('슬라이더 조작 종료');
    };
})(TcHmi);
```

### 👤 사용자 관리 및 권한

```javascript
// ✅ 현재 로그인 사용자 확인
(function (TcHmi) {
    TcHmi.Server.getCurrentUser(function (data) {
        if (data.error === TcHmi.Errors.NONE) {
            var user = data.user;
            console.log('사용자:', user.name);
            console.log('권한 그룹:', user.group);

            // 권한에 따라 버튼 표시/숨김
            if (user.group === 'Administrator') {
                var adminButton = TcHmi.Controls.get('Button_AdminSettings');
                adminButton.setVisibility(TcHmi.Visibility.Visible);
            }
        }
    });
})(TcHmi);
```

```javascript
// ✅ 로그인/로그아웃
(function (TcHmi) {
    // 로그인
    TcHmi.Server.login('operator1', 'password123', function (data) {
        if (data.error === TcHmi.Errors.NONE) {
            console.log('로그인 성공');
        } else {
            alert('로그인 실패: ' + data.error);
        }
    });

    // 로그아웃
    TcHmi.Server.logout(function (data) {
        console.log('로그아웃됨');
    });
})(TcHmi);
```

### 📊 차트 및 트렌드

```javascript
// ✅ 실시간 라인 차트
(function (TcHmi) {
    var chart = TcHmi.Controls.get('LineChart_Temperature');

    // 차트 데이터 초기화
    var chartData = [];
    var maxPoints = 100; // 최대 100개 포인트

    // 주기적 데이터 업데이트 (1초마다)
    setInterval(function () {
        // PLC에서 온도 읽기
        TcHmi.Symbol.read('MAIN.temperature', function (data) {
            if (data.error === TcHmi.Errors.NONE) {
                var temperature = data.value;
                var timestamp = new Date();

                // 데이터 포인트 추가
                chartData.push({
                    x: timestamp,
                    y: temperature
                });

                // 오래된 데이터 제거
                if (chartData.length > maxPoints) {
                    chartData.shift();
                }

                // 차트 업데이트
                chart.setSrcData(chartData);
            }
        });
    }, 1000);
})(TcHmi);
```

---

## 6️⃣ IoT & 통신 API

### 📡 MQTT (TF6701)

**MQTT (Message Queuing Telemetry Transport)**는 경량 IoT 프로토콜입니다.

```iecst
// ✅ MQTT 클라이언트 (TwinCAT PLC)
PROGRAM MqttClientExample
VAR
    fbMqttClient : FB_MqttClient;
    fbPublish : FB_MqttPublish;
    fbSubscribe : FB_MqttSubscribe;

    // 연결 설정
    stConnectionConfig : ST_MqttConnectionConfig := (
        sBrokerAddress := 'mqtt.example.com',
        nBrokerPort := 1883,
        sClientId := 'TwinCAT_PLC_001',
        sUsername := 'user',
        sPassword := 'pass'
    );

    // 퍼블리시 데이터
    sPublishTopic : STRING := 'factory/machine1/temperature';
    fTemperature : REAL := 23.5;
    sPayload : STRING(255);

    // 서브스크라이브
    sSubscribeTopic : STRING := 'factory/commands/machine1';
    sReceivedMessage : STRING(255);
END_VAR

// MQTT 클라이언트 연결
fbMqttClient(
    stConfig := stConnectionConfig,
    bConnect := TRUE
);

// 메시지 퍼블리시
IF fbMqttClient.bConnected THEN
    // JSON 페이로드 생성
    sPayload := CONCAT('{"temperature":', REAL_TO_STRING(fTemperature));
    sPayload := CONCAT(sPayload, ',"unit":"celsius"}');

    fbPublish(
        sTopicName := sPublishTopic,
        sPayload := sPayload,
        eQoS := eMqttQoS.AtLeastOnce_1,
        bExecute := TRUE
    );
END_IF

// 메시지 서브스크라이브
fbSubscribe(
    sTopicFilter := sSubscribeTopic,
    eQoS := eMqttQoS.AtLeastOnce_1,
    bExecute := TRUE
);

// 수신된 메시지 처리
IF fbSubscribe.bNewMessage THEN
    sReceivedMessage := fbSubscribe.sPayload;
    ADSLOGSTR(ADSLOG_MSGTYPE_HINT, 'MQTT 메시지 수신: %s', sReceivedMessage);
END_IF
```

### 🌐 OPC UA (TF6100)

**OPC UA (Unified Architecture)**는 산업 표준 통신 프로토콜입니다.

```iecst
// ✅ OPC UA 서버 심볼 노출
(*
PLC 변수를 OPC UA로 자동 노출하려면:
1. 변수 선언 위에 {attribute 'OPC.UA.DA' := '1'} 추가
2. TwinCAT OPC UA Configurator에서 심볼 활성화
*)

{attribute 'OPC.UA.DA' := '1'}
{attribute 'OPC.UA.DA.Description' := '컨베이어 속도 [mm/s]'}
VAR_GLOBAL
    gConveyorSpeed : REAL := 0.0;
END_VAR

{attribute 'OPC.UA.DA' := '1'}
{attribute 'OPC.UA.DA.Access' := 'Read'}  // 읽기 전용
VAR_GLOBAL
    gProductionCount : DINT := 0;
END_VAR
```

```csharp
// ✅ C# - OPC UA 클라이언트
using Opc.Ua;
using Opc.Ua.Client;

// OPC UA 서버 연결
var endpointUrl = "opc.tcp://192.168.1.100:4840";
var endpoint = CoreClientUtils.SelectEndpoint(endpointUrl, false);
var config = EndpointConfiguration.Create();
var endpoint = new ConfiguredEndpoint(null, endpoint, config);

using (var session = Session.Create(
    new ApplicationConfiguration(),
    endpoint,
    false,
    "OPC UA Client",
    60000,
    new UserIdentity(new AnonymousIdentityToken()),
    null))
{
    // 변수 읽기
    var nodeId = new NodeId("MAIN.gConveyorSpeed", 4);  // Namespace 4
    var value = session.ReadValue(nodeId);
    Console.WriteLine($"컨베이어 속도: {value.Value} mm/s");

    // 변수 쓰기
    var writeValue = new WriteValue
    {
        NodeId = nodeId,
        AttributeId = Attributes.Value,
        Value = new DataValue(new Variant(150.0f))
    };
    session.Write(null, new[] { writeValue }, out var results, out _);
}
```

### ☁️ AWS IoT Core 연동

```iecst
// ✅ AWS IoT Core MQTT 연결
PROGRAM AwsIotExample
VAR
    fbMqttClient : FB_MqttClient;

    // AWS IoT Core 설정
    stConfig : ST_MqttConnectionConfig := (
        sBrokerAddress := 'xxxxx-ats.iot.us-east-1.amazonaws.com',
        nBrokerPort := 8883,  // TLS 포트
        sClientId := 'TwinCAT_Device_001',
        bUseTls := TRUE,
        sCertificateFile := 'C:\Certs\device-cert.pem',
        sPrivateKeyFile := 'C:\Certs\device-key.pem',
        sRootCaFile := 'C:\Certs\AmazonRootCA1.pem'
    );

    // 디바이스 섀도우 업데이트
    sShadowTopic : STRING := '$aws/things/Machine001/shadow/update';
    sPayload : STRING(512);
END_VAR

fbMqttClient(stConfig := stConfig, bConnect := TRUE);

IF fbMqttClient.bConnected THEN
    // 섀도우 JSON 생성
    sPayload := '{';
    sPayload := CONCAT(sPayload, '"state":{');
    sPayload := CONCAT(sPayload, '"reported":{');
    sPayload := CONCAT(sPayload, '"temperature":');
    sPayload := CONCAT(sPayload, REAL_TO_STRING(gTemperature));
    sPayload := CONCAT(sPayload, ',');
    sPayload := CONCAT(sPayload, '"speed":');
    sPayload := CONCAT(sPayload, REAL_TO_STRING(gSpeed));
    sPayload := CONCAT(sPayload, '}}}');

    // 퍼블리시
    fbPublish(
        sTopicName := sShadowTopic,
        sPayload := sPayload,
        bExecute := TRUE
    );
END_IF
```

### 🌍 Azure IoT Hub 연동

```csharp
// ✅ C# - Azure IoT Hub 디바이스 클라이언트
using Microsoft.Azure.Devices.Client;
using TwinCAT.Ads;

// IoT Hub 연결 문자열
var connectionString = "HostName=myiothub.azure-devices.net;DeviceId=TwinCAT001;SharedAccessKey=xxxxx";
var deviceClient = DeviceClient.CreateFromConnectionString(connectionString, TransportType.Mqtt);

// TwinCAT ADS 연결
var adsClient = new TcAdsClient();
adsClient.Connect("192.168.1.100.1.1", 851);

// 주기적 텔레메트리 전송
var timer = new Timer(async _ =>
{
    // PLC에서 데이터 읽기
    var temperature = (float)adsClient.ReadAny(
        adsClient.CreateVariableHandle("MAIN.temperature"),
        typeof(float)
    );

    var humidity = (float)adsClient.ReadAny(
        adsClient.CreateVariableHandle("MAIN.humidity"),
        typeof(float)
    );

    // JSON 텔레메트리 메시지 생성
    var telemetry = new
    {
        temperature = temperature,
        humidity = humidity,
        timestamp = DateTime.UtcNow
    };

    var message = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(telemetry)));
    message.Properties.Add("temperatureAlert", temperature > 30 ? "true" : "false");

    // IoT Hub로 전송
    await deviceClient.SendEventAsync(message);
    Console.WriteLine($"텔레메트리 전송: 온도={temperature}°C, 습도={humidity}%");

}, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
```

---

## 7️⃣ Vision (머신 비전) API

### 📷 TwinCAT Vision (TF7xxx)

TwinCAT Vision은 **이미지 처리 및 검사**를 위한 통합 솔루션입니다.

```
TwinCAT Vision 모듈
│
├── 📸 TF7000 - Vision Base
│   └── 이미지 취득, 기본 처리
│
├── 🔍 TF7100 - Vision Pattern Matching
│   └── 패턴 검출 및 정렬
│
├── 📊 TF7200 - Vision Barcode/QR Code
│   └── 1D/2D 코드 읽기
│
├── 📏 TF7300 - Vision Metrology
│   └── 측정 및 캘리브레이션
│
└── 🤖 TF7400 - Vision Deep Learning
    └── AI 기반 검사
```

### 📸 이미지 취득

```iecst
// ✅ 카메라 이미지 취득
PROGRAM VisionAcquisition
VAR
    fbCamera : FB_VN_GevCameraControl;  // GigE Vision 카메라
    ipImageIn : ITcVnImage;
    ipImageProvider : ITcVnImageProvider;

    bTrigger : BOOL;
    hrResult : HRESULT;
END_VAR

// 카메라 초기화
fbCamera.StartAcquisition();

// 이미지 트리거
IF bTrigger THEN
    fbCamera.TriggerImage();
    bTrigger := FALSE;
END_IF

// 이미지 가져오기
hrResult := fbCamera.GetCurrentImage(ipImageIn);
IF SUCCEEDED(hrResult) THEN
    // 이미지 처리 수행
    ProcessImage(ipImageIn);
END_IF
```

### 🖼️ 이미지 처리

```iecst
// ✅ 기본 이미지 처리 함수
PROGRAM ImageProcessing
VAR
    ipImageIn : ITcVnImage;
    ipImageGray : ITcVnImage;
    ipImageBlurred : ITcVnImage;
    ipImageEdges : ITcVnImage;

    hrResult : HRESULT;
END_VAR

// 1. 그레이스케일 변환
hrResult := F_VN_ConvertColorSpace(
    ipImageIn,
    ipImageGray,
    TCVN_CST_BAYER_BG_TO_GRAY,  // 변환 타입
    hrPrev := hrResult
);

// 2. 가우시안 블러 (노이즈 제거)
hrResult := F_VN_GaussianBlur(
    ipSrcImage := ipImageGray,
    ipDestImage := ipImageBlurred,
    nKernelWidth := 5,
    nKernelHeight := 5,
    hrPrev := hrResult
);

// 3. 캐니 엣지 검출
hrResult := F_VN_Canny(
    ipSrcImage := ipImageBlurred,
    ipDestImage := ipImageEdges,
    fThreshold1 := 50.0,
    fThreshold2 := 150.0,
    hrPrev := hrResult
);
```

### 🎯 패턴 매칭 (TF7100)

```iecst
// ✅ 템플릿 매칭으로 부품 찾기
PROGRAM PatternMatching
VAR
    fbTemplateMatching : FB_VN_TemplateMatching;
    ipTemplateImage : ITcVnImage;  // 템플릿 이미지
    ipSearchImage : ITcVnImage;    // 검색 이미지

    aMatches : ARRAY[1..10] OF TcVnPoint2_REAL;  // 검출된 위치
    nMatchCount : UDINT;
    fScore : REAL;  // 매칭 점수 (0.0 ~ 1.0)
END_VAR

// 템플릿 매칭 실행
fbTemplateMatching(
    ipTemplateImage := ipTemplateImage,
    ipSearchImage := ipSearchImage,
    fMatchThreshold := 0.8,  // 80% 이상 일치
    nMaxMatches := 10
);

IF fbTemplateMatching.bDone THEN
    nMatchCount := fbTemplateMatching.nMatchCount;

    // 검출된 모든 위치 처리
    FOR i := 1 TO nMatchCount DO
        aMatches[i] := fbTemplateMatching.aMatches[i-1];
        ADSLOGSTR(
            ADSLOG_MSGTYPE_HINT,
            '부품 발견: X=%.2f, Y=%.2f, 점수=%.2f',
            aMatches[i].fX,
            aMatches[i].fY,
            fbTemplateMatching.aScores[i-1]
        );
    END_FOR;
END_IF
```

### 📊 바코드/QR 코드 읽기 (TF7200)

```iecst
// ✅ QR 코드 디코딩
PROGRAM BarcodeReading
VAR
    fbBarcodeReader : FB_VN_2DCodeReader;
    ipImage : ITcVnImage;

    sDecodedText : STRING(255);
    eCodeType : ETcVn2DCodeType;
    bNewCode : BOOL;
END_VAR

// 바코드 읽기
fbBarcodeReader(
    ipSrcImage := ipImage,
    eCodeType := TCVN_BCT_QRCODE  // QR 코드
);

IF fbBarcodeReader.bCodeFound THEN
    sDecodedText := fbBarcodeReader.sDecodedText;
    bNewCode := TRUE;

    ADSLOGSTR(
        ADSLOG_MSGTYPE_HINT,
        'QR 코드 읽음: %s',
        sDecodedText
    );

    // 디코딩된 정보로 작업 수행
    ProcessProductInfo(sDecodedText);
END_IF
```

### 📏 측정 (Metrology - TF7300)

```iecst
// ✅ 원 검출 및 직경 측정
PROGRAM CircleMeasurement
VAR
    fbFindCircles : FB_VN_FindCircles;
    ipImage : ITcVnImage;

    aCircles : ARRAY[1..5] OF TcVnCircle;
    nCircleCount : UDINT;
    fDiameter : REAL;  // [mm]
    fPixelToMm : REAL := 0.05;  // 캘리브레이션: 1픽셀 = 0.05mm
END_VAR

// 원 검출
fbFindCircles(
    ipSrcImage := ipImage,
    fMinRadius := 50.0,   // 최소 반지름 [픽셀]
    fMaxRadius := 200.0,  // 최대 반지름
    nMaxCircles := 5
);

IF fbFindCircles.bDone THEN
    nCircleCount := fbFindCircles.nCircleCount;

    FOR i := 1 TO nCircleCount DO
        aCircles[i] := fbFindCircles.aCircles[i-1];

        // 직경 계산 (픽셀 → mm)
        fDiameter := aCircles[i].fRadius * 2.0 * fPixelToMm;

        ADSLOGSTR(
            ADSLOG_MSGTYPE_HINT,
            '원 검출: 중심=(%.1f, %.1f), 직경=%.2fmm',
            aCircles[i].fCenterX,
            aCircles[i].fCenterY,
            fDiameter
        );

        // 공차 검사
        IF fDiameter < 9.9 OR fDiameter > 10.1 THEN
            // 불량품 처리
            RejectProduct();
        END_IF
    END_FOR;
END_IF
```

---

## 8️⃣ Database Server API

### 🗄️ TwinCAT Database Server (TF6420)

SQL 데이터베이스와 **직접 연동**할 수 있습니다.

지원 데이터베이스:
- ✅ Microsoft SQL Server
- ✅ MySQL / MariaDB
- ✅ PostgreSQL
- ✅ SQLite
- ✅ Oracle (via ODBC)

### 📝 INSERT (데이터 삽입)

```iecst
// ✅ 생산 로그 삽입
PROGRAM DatabaseInsert
VAR
    fbDatabase : FB_SQLDatabaseEvt;
    fbInsert : FB_DBRecordInsert;

    // 데이터베이스 연결 설정
    stConnectionConfig : ST_DBConnectionConfig := (
        sServerName := 'localhost',
        sDatabase := 'factory_db',
        sUserName := 'plc_user',
        sPassword := 'plc_pass',
        eDBMS := E_DBMS.eMSSQL  // MS SQL Server
    );

    // 삽입할 데이터
    dtTimestamp : DT;
    nProductId : DINT := 12345;
    fCycleTime : REAL := 4.5;  // [초]
    bQualityOK : BOOL := TRUE;

    sQuery : STRING(512);
END_VAR

// 데이터베이스 연결
fbDatabase.Connect(stConnectionConfig);

IF fbDatabase.bConnected THEN
    // INSERT 쿼리 생성
    dtTimestamp := NT_GetTime();

    sQuery := 'INSERT INTO production_log (timestamp, product_id, cycle_time, quality_ok) VALUES (';
    sQuery := CONCAT(sQuery, CHR(39));  // 작은따옴표
    sQuery := CONCAT(sQuery, DT_TO_STRING(dtTimestamp));
    sQuery := CONCAT(sQuery, CHR(39));
    sQuery := CONCAT(sQuery, ', ');
    sQuery := CONCAT(sQuery, DINT_TO_STRING(nProductId));
    sQuery := CONCAT(sQuery, ', ');
    sQuery := CONCAT(sQuery, REAL_TO_STRING(fCycleTime));
    sQuery := CONCAT(sQuery, ', ');
    sQuery := CONCAT(sQuery, BOOL_TO_STRING(bQualityOK));
    sQuery := CONCAT(sQuery, ')');

    // 실행
    fbInsert.Execute(
        hDBID := fbDatabase.hDBID,
        sQuery := sQuery
    );

    IF fbInsert.bDone THEN
        ADSLOGSTR(ADSLOG_MSGTYPE_HINT, '데이터 삽입 완료', '');
    ELSIF fbInsert.bError THEN
        ADSLOGSTR(ADSLOG_MSGTYPE_ERROR, '삽입 오류: %d', fbInsert.nErrorID);
    END_IF
END_IF
```

### 🔍 SELECT (데이터 조회)

```iecst
// ✅ 최근 생산 기록 조회
PROGRAM DatabaseSelect
VAR
    fbDatabase : FB_SQLDatabaseEvt;
    fbSelect : FB_DBRecordSelect;

    sQuery : STRING := 'SELECT TOP 10 * FROM production_log ORDER BY timestamp DESC';

    // 결과 저장
    aResults : ARRAY[1..10] OF ST_ProductionLog;
    nRowCount : UDINT;
    i : INT;
END_VAR

TYPE ST_ProductionLog :
STRUCT
    timestamp : DT;
    product_id : DINT;
    cycle_time : REAL;
    quality_ok : BOOL;
END_STRUCT
END_TYPE

// SELECT 실행
IF fbDatabase.bConnected THEN
    fbSelect.Execute(
        hDBID := fbDatabase.hDBID,
        sQuery := sQuery
    );

    IF fbSelect.bDone THEN
        nRowCount := fbSelect.nRecordCount;

        // 각 행 데이터 읽기
        FOR i := 1 TO nRowCount DO
            fbSelect.GetColumn(i, 'timestamp', aResults[i].timestamp);
            fbSelect.GetColumn(i, 'product_id', aResults[i].product_id);
            fbSelect.GetColumn(i, 'cycle_time', aResults[i].cycle_time);
            fbSelect.GetColumn(i, 'quality_ok', aResults[i].quality_ok);
        END_FOR;

        ADSLOGSTR(ADSLOG_MSGTYPE_HINT, '총 %d개 레코드 조회됨', nRowCount);
    END_IF
END_IF
```

### 🔄 Stored Procedure 호출

```iecst
// ✅ 저장 프로시저 실행
PROGRAM CallStoredProcedure
VAR
    fbDatabase : FB_SQLDatabaseEvt;
    fbExecute : FB_DBExecute;

    nMachineId : INT := 1;
    dtStartDate : DT;
    dtEndDate : DT;

    sQuery : STRING(512);
    nTotalProduction : DINT;  // OUTPUT 파라미터
END_VAR

// Stored Procedure 호출
sQuery := 'EXEC sp_GetMachineProduction @MachineID=';
sQuery := CONCAT(sQuery, INT_TO_STRING(nMachineId));
sQuery := CONCAT(sQuery, ', @StartDate=''');
sQuery := CONCAT(sQuery, DT_TO_STRING(dtStartDate));
sQuery := CONCAT(sQuery, ''', @EndDate=''');
sQuery := CONCAT(sQuery, DT_TO_STRING(dtEndDate));
sQuery := CONCAT(sQuery, '''');

fbExecute.Execute(
    hDBID := fbDatabase.hDBID,
    sQuery := sQuery
);

IF fbExecute.bDone THEN
    // 결과 읽기
    nTotalProduction := fbExecute.GetScalar();
    ADSLOGSTR(ADSLOG_MSGTYPE_HINT, '생산량: %d개', nTotalProduction);
END_IF
```

---

## 9️⃣ Scope & Measurement API

### 📊 TwinCAT Scope (TE13xx)

**실시간 데이터 수집 및 시각화** 도구입니다.

```iecst
// ✅ Scope로 신호 기록
(*
1. Scope 프로젝트 생성 (*.tcscopex)
2. 차트 추가 (YT Chart 또는 XY Chart)
3. Acquisition 설정:
   - Trigger: Rising Edge, Level 등
   - 샘플링 레이트: 1ms ~ 1s
4. 변수 드래그 앤 드롭
*)

PROGRAM ScopeExample
VAR
    fSignal1 : REAL;  // 측정 신호 1
    fSignal2 : REAL;  // 측정 신호 2
    bTrigger : BOOL;  // 트리거 신호

    fTime : REAL;
    fAmplitude : REAL := 10.0;
    fFrequency : REAL := 1.0;  // [Hz]
END_VAR

// 사인파 생성
fTime := fTime + 0.001;  // 1ms 사이클
fSignal1 := fAmplitude * SIN(2.0 * 3.14159 * fFrequency * fTime);
fSignal2 := fAmplitude * COS(2.0 * 3.14159 * fFrequency * fTime);

// 트리거 조건
IF fSignal1 > 5.0 AND NOT bTrigger THEN
    bTrigger := TRUE;
END_IF
```

### 🎛️ C#에서 Scope 제어

```csharp
// ✅ Automation Interface로 Scope 제어
using Beckhoff.TwinCAT.Scope;

// Scope 프로젝트 로드
var scopeProject = new ScopeProject(@"C:\TwinCAT\Scope\MyProject.tcscopex");

// 차트 가져오기
var chart = scopeProject.GetChart("Chart1");

// 데이터 수집 시작
chart.StartRecord();

// 10초 대기
Thread.Sleep(10000);

// 데이터 수집 중지
chart.StopRecord();

// 데이터를 CSV로 내보내기
chart.ExportData(@"C:\Data\scope_data.csv", ExportFormat.CSV);

Console.WriteLine("Scope 데이터 저장 완료");
```

### 📈 FFT 분석

```iecst
// ✅ FFT로 주파수 분석
(*
Scope에서 FFT (Fast Fourier Transform) 사용:
1. YT Chart 추가
2. Analysis 탭에서 FFT 활성화
3. 윈도우 함수 선택 (Hanning, Hamming 등)
4. 주파수 범위 설정
*)

// FFT 결과는 Scope View에서 확인 가능
// 진동 분석, 모터 불균형 검출 등에 활용
```

---

## 🔟 Safety (안전) API

### 🛡️ TwinSAFE (TF6xxx)

**IEC 61508 SIL 3** 인증 안전 시스템입니다.

```
TwinSAFE 아키텍처
│
├── 🔒 Safety PLC (TwinSAFE Logic)
│   └── IEC 61131-3 (안전 로직)
│
├── 📡 FSoE (Fail-Safe over EtherCAT)
│   └── 안전 통신 프로토콜
│
└── 🔌 Safety I/O
    ├── 비상 정지 버튼
    ├── 라이트 커튼
    ├── 안전 릴레이
    └── 안전 모터 드라이브
```

### 🚨 비상 정지 (E-Stop)

```iecst
// ✅ TwinSAFE - 비상 정지 로직
PROGRAM SafetyEstop
VAR
    // 안전 입력
    EStopButton1 AT %I* : BOOL;  // 비상 정지 버튼 1
    EStopButton2 AT %I* : BOOL;  // 비상 정지 버튼 2

    // 안전 출력
    SafeMotorEnable AT %Q* : BOOL;  // 모터 활성화 (안전)

    // PLCopen Safety FB
    fbEstop : SF_EmergencyStop;
    fbEdm : SF_EDM;  // External Device Monitoring

    bResetRequest : BOOL;
    bSafetyOK : BOOL;
END_VAR

// 비상 정지 Function Block
fbEstop(
    Activate := TRUE,
    SEstopIn1 := EStopButton1,
    SEstopIn2 := EStopButton2,
    Reset := bResetRequest,
    DiagCode => ,
    SEstopOut => bSafetyOK
);

// 안전 출력
SafeMotorEnable := bSafetyOK;
```

### 👐 양손 조작 (Two-Hand Control)

```iecst
// ✅ 양손 제어 (EN ISO 13851 Type IIIC)
PROGRAM SafetyTwoHand
VAR
    ButtonLeft AT %I* : BOOL;   // 왼손 버튼
    ButtonRight AT %I* : BOOL;  // 오른손 버튼

    SafePressEnable AT %Q* : BOOL;  // 프레스 활성화

    fbTwoHand : SF_TwoHandControlTypeIIIC;

    tDiscrepancyTime : TIME := T#500ms;  // 동시 누름 허용 시간
END_VAR

fbTwoHand(
    Activate := TRUE,
    SButtonLeft := ButtonLeft,
    SButtonRight := ButtonRight,
    DiscrepancyTime := tDiscrepancyTime,
    DiagCode => ,
    SOutValid => SafePressEnable
);
```

### 🚧 라이트 커튼 (Safety Light Curtain)

```iecst
// ✅ 라이트 커튼 모니터링
PROGRAM SafetyLightCurtain
VAR
    LightCurtainOSSD1 AT %I* : BOOL;  // Output Signal Switching Device 1
    LightCurtainOSSD2 AT %I* : BOOL;  // Output Signal Switching Device 2

    SafeRobotEnable AT %Q* : BOOL;

    fbAOPD : SF_AOPD;  // Active Opto-electronic Protective Device

    bMutingActive : BOOL := FALSE;  // 뮤팅 (일시 무효화)
    bResetRequest : BOOL;
END_VAR

fbAOPD(
    Activate := TRUE,
    SAOPD1 := LightCurtainOSSD1,
    SAOPD2 := LightCurtainOSSD2,
    SMuting := bMutingActive,
    Reset := bResetRequest,
    DiagCode => ,
    SAOPDOut => SafeRobotEnable
);
```

### 🔗 표준 PLC ↔ Safety PLC 통신

```iecst
// ✅ 표준 PLC → Safety PLC 통신
PROGRAM StandardPLC
VAR
    {attribute 'TcSafety'}
    CommFromStandard : ST_SafetyComm;  // Safety로 전송

    {attribute 'TcSafety'}
    CommFromSafety : ST_SafetyComm;    // Safety로부터 수신
END_VAR

TYPE ST_SafetyComm :
STRUCT
    bStartRequest : BOOL;      // 시작 요청
    fTargetSpeed : REAL;       // 목표 속도 (위험 없음)
    nModeSelection : INT;      // 모드 선택
END_STRUCT
END_TYPE

// 표준 PLC에서 데이터 전송
CommFromStandard.bStartRequest := TRUE;
CommFromStandard.fTargetSpeed := 150.0;
```

```iecst
// ✅ Safety PLC (TwinSAFE)
PROGRAM SafetyPLC
VAR
    {attribute 'TcSafety'}
    CommFromStandard : ST_SafetyComm;

    {attribute 'TcSafety'}
    CommToStandard : ST_SafetyComm;

    fbSLS : SF_SafeLimitedSpeed;  // 안전 제한 속도
END_VAR

// Safety PLC에서 안전 검증
IF CommFromStandard.fTargetSpeed <= 200.0 THEN  // 안전 속도 한계
    // 안전 승인
    CommToStandard.bStartRequest := TRUE;
ELSE
    // 거부
    CommToStandard.bStartRequest := FALSE;
END_IF
```

---

## 1️⃣1️⃣ Analytics & Machine Learning API

### 📊 TwinCAT Analytics (TE3520)

**데이터 분석 및 머신러닝** 기능을 제공합니다.

```
TwinCAT Analytics
│
├── 📈 통계 알고리즘 (90+ 함수)
│   ├── 평균, 표준편차, RMS
│   ├── FFT, 상관관계
│   └── 필터링 (Low-pass, High-pass)
│
├── 🤖 Machine Learning
│   ├── SVM (Support Vector Machine)
│   ├── Decision Tree
│   ├── PCA (주성분 분석)
│   └── Neural Network Inference
│
└── 🔧 Condition Monitoring
    ├── 진동 분석
    ├── 이상 탐지
    └── 예측 정비
```

### 📐 통계 함수

```iecst
// ✅ 통계 분석
PROGRAM StatisticsExample
VAR
    aSignal : ARRAY[1..1000] OF LREAL;  // 측정 신호

    fbMean : FB_AnalyticsMean;          // 평균
    fbStdDev : FB_AnalyticsStdDev;      // 표준편차
    fbRMS : FB_AnalyticsRMS;            // RMS (Root Mean Square)

    fMean : LREAL;
    fStdDev : LREAL;
    fRMS : LREAL;
END_VAR

// 평균 계산
fbMean(
    pData := ADR(aSignal),
    nDataCount := 1000,
    bExecute := TRUE
);

IF fbMean.bDone THEN
    fMean := fbMean.fResult;
END_IF

// 표준편차 계산
fbStdDev(
    pData := ADR(aSignal),
    nDataCount := 1000,
    bExecute := TRUE
);

IF fbStdDev.bDone THEN
    fStdDev := fbStdDev.fResult;
END_IF

// RMS 계산 (진동 분석 등)
fbRMS(
    pData := ADR(aSignal),
    nDataCount := 1000,
    bExecute := TRUE
);

IF fbRMS.bDone THEN
    fRMS := fbRMS.fResult;

    // 진동 임계값 검사
    IF fRMS > 5.0 THEN
        ADSLOGSTR(ADSLOG_MSGTYPE_WARN, '진동 경고: RMS=%.2f', fRMS);
    END_IF
END_IF
```

### 🔬 FFT (고속 푸리에 변환)

```iecst
// ✅ FFT로 주파수 분석
PROGRAM FFTAnalysis
VAR
    aTimeDomainSignal : ARRAY[1..1024] OF LREAL;  // 시간 영역 신호
    aFrequencySpectrum : ARRAY[1..1024] OF LREAL;  // 주파수 스펙트럼

    fbFFT : FB_AnalyticsFFT;

    fSamplingRate : LREAL := 1000.0;  // [Hz]
    fDominantFreq : LREAL;            // 지배 주파수
    nPeakIndex : INT;
END_VAR

// FFT 실행
fbFFT(
    pDataIn := ADR(aTimeDomainSignal),
    pDataOut := ADR(aFrequencySpectrum),
    nDataCount := 1024,
    fSamplingRate := fSamplingRate,
    bExecute := TRUE
);

IF fbFFT.bDone THEN
    // 피크 주파수 찾기
    nPeakIndex := FindMaxIndex(aFrequencySpectrum);
    fDominantFreq := REAL_TO_LREAL(nPeakIndex) * fSamplingRate / 1024.0;

    ADSLOGSTR(ADSLOG_MSGTYPE_HINT, '지배 주파수: %.2f Hz', fDominantFreq);

    // 모터 불균형 검출 (회전 주파수의 배수에서 피크)
    IF fDominantFreq > 100.0 THEN
        // 이상 진동 감지
        TriggerMaintenance();
    END_IF
END_IF
```

### 🤖 머신러닝 추론 (Inference)

TwinCAT Analytics는 **ONNX, TensorFlow Lite** 모델을 실행할 수 있습니다.

```iecst
// ✅ Neural Network Inference
PROGRAM MLInference
VAR
    fbModelLoader : FB_AnalyticsMLModelLoad;
    fbInference : FB_AnalyticsMLInference;

    sModelPath : STRING := 'C:\Models\quality_classifier.onnx';

    // 입력 데이터 (10개 특징)
    aInputFeatures : ARRAY[1..10] OF REAL := [
        23.5,   // 온도
        65.2,   // 습도
        1500.0, // 속도
        4.2,    // 사이클 타임
        0.05,   // 진동
        // ... 나머지 특징
        85.0
    ];

    // 출력 (2개 클래스: 양품/불량품)
    aOutputProbabilities : ARRAY[1..2] OF REAL;

    bIsDefective : BOOL;
END_VAR

// 1단계: 모델 로드
fbModelLoader(
    sModelFilePath := sModelPath,
    bExecute := TRUE
);

// 2단계: 추론 실행
IF fbModelLoader.bDone THEN
    fbInference(
        hModel := fbModelLoader.hModel,
        pInputData := ADR(aInputFeatures),
        nInputSize := SIZEOF(aInputFeatures),
        pOutputData := ADR(aOutputProbabilities),
        nOutputSize := SIZEOF(aOutputProbabilities),
        bExecute := TRUE
    );

    IF fbInference.bDone THEN
        // 결과 해석
        IF aOutputProbabilities[2] > 0.8 THEN  // 불량 확률 > 80%
            bIsDefective := TRUE;
            ADSLOGSTR(ADSLOG_MSGTYPE_WARN, '불량품 검출: %.1f%%',
                      aOutputProbabilities[2] * 100.0);

            // 자동 배출
            RejectProduct();
        ELSE
            bIsDefective := FALSE;
        END_IF
    END_IF
END_IF
```

### 🛠️ Condition Monitoring (조건 모니터링)

```iecst
// ✅ 예측 정비 (Predictive Maintenance)
PROGRAM PredictiveMaintenance
VAR
    // 센서 데이터
    fVibration : REAL;       // 진동 [mm/s]
    fTemperature : REAL;     // 온도 [°C]
    fCurrent : REAL;         // 전류 [A]

    // 분석
    fbTrendAnalysis : FB_AnalyticsTrend;
    fbAnomalyDetection : FB_AnalyticsAnomalyDetection;

    // 건강 상태
    fHealthScore : REAL;  // 0.0 (고장) ~ 1.0 (정상)
    bMaintenanceRequired : BOOL;
    nDaysToFailure : INT;  // 예상 고장까지 남은 일수
END_VAR

// 진동 트렌드 분석
fbTrendAnalysis(
    fCurrentValue := fVibration,
    tSamplingInterval := T#1h,
    bExecute := TRUE
);

IF fbTrendAnalysis.bDone THEN
    // 상승 트렌드 검출
    IF fbTrendAnalysis.fTrendSlope > 0.1 THEN
        ADSLOGSTR(ADSLOG_MSGTYPE_WARN, '진동 증가 추세 감지', '');
    END_IF
END_IF

// 이상 탐지
fbAnomalyDetection(
    fVibration := fVibration,
    fTemperature := fTemperature,
    fCurrent := fCurrent,
    bExecute := TRUE
);

IF fbAnomalyDetection.bAnomaly THEN
    bMaintenanceRequired := TRUE;
    nDaysToFailure := fbAnomalyDetection.nEstimatedDaysToFailure;

    ADSLOGSTR(
        ADSLOG_MSGTYPE_ERROR,
        '이상 감지! 예상 고장: %d일 후',
        nDaysToFailure
    );

    // 정비 팀에 알림
    SendMaintenanceAlert();
END_IF
```

---

## 1️⃣2️⃣ Automation Interface (.NET API)

### 🤖 TwinCAT Automation Interface

**.NET (C#)에서 TwinCAT 프로젝트를 프로그래밍 방식으로 제어**합니다.

```
Automation Interface 용도
│
├── 🔧 프로젝트 자동 생성
│   └── PLC, I/O, Motion 설정
│
├── 📦 CI/CD 파이프라인
│   └── 자동 빌드, 배포, 테스트
│
├── 🔄 대량 설정
│   └── 100+ 축 자동 설정
│
└── 📊 프로젝트 분석
    └── 변수 추출, 문서 생성
```

### 🚀 프로젝트 자동 생성

```csharp
// ✅ C# - TwinCAT 프로젝트 자동 생성
using EnvDTE;
using EnvDTE80;
using TCatSysManagerLib;

// Visual Studio DTE 가져오기
Type t = System.Type.GetTypeFromProgID("TcXaeShell.DTE.15.0");
DTE2 dte = (DTE2)System.Activator.CreateInstance(t);
dte.SuppressUI = false;
dte.MainWindow.Visible = true;

// 새 솔루션 생성
Solution2 solution = (Solution2)dte.Solution;
string solutionPath = @"C:\TwinCAT\Projects\AutoGenerated";
string solutionName = "AutoProject";
solution.Create(solutionPath, solutionName);

// TwinCAT 프로젝트 추가
string templatePath = @"C:\TwinCAT\3.1\Components\Base\PrjTemplate\TwinCAT Project.tsproj";
Project project = solution.AddFromTemplate(templatePath, solutionPath, solutionName, false);

// System Manager 가져오기
ITcSysManager15 systemManager = (ITcSysManager15)project.Object;

// PLC 프로젝트 추가
ITcSmTreeItem plcItem = systemManager.LookupTreeItem("TIPC");
ITcSmTreeItem plcProject = plcItem.CreateChild("PlcProject1", 0, "", "Standard PLC Project");

// PLC 프로그램 자동 생성 (PLCopen XML)
ITcPlcProject plcProj = (ITcPlcProject)plcProject;
string plcOpenXml = @"<?xml version='1.0' encoding='UTF-8'?>
<project>
  <fileHeader companyName='Auto Generator' productVersion='3.1' />
  <contentHeader name='AutoProgram'>
    <pous>
      <pou name='MAIN' pouType='program'>
        <body>
          <ST>
            <xhtml xmlns='http://www.w3.org/1999/xhtml'>
              VAR
                counter : INT := 0;
              END_VAR

              counter := counter + 1;
            </xhtml>
          </ST>
        </body>
      </pou>
    </pous>
  </contentHeader>
</project>";

plcProj.ConsumeXml(plcOpenXml);

// I/O 설정 자동화
ITcSmTreeItem ioDevices = systemManager.LookupTreeItem("TIID");
ITcSmTreeItem ethercat = ioDevices.CreateChild("EtherCAT Master", 0, "", "");

// EtherCAT 슬레이브 추가 (예: EL2004 디지털 출력)
ITcSmTreeItem slave = ethercat.CreateChild("EL2004", 2004, "", "");

// 링크 생성 (I/O → PLC)
ITcSmTreeItem plcOutputs = plcProject.LookupChild("PlcTask Outputs");
ITcSmTreeItem terminal = slave.LookupChild("Channel 1");
plcOutputs.CreateLink(terminal);

// 빌드
solution.SolutionBuild.Build(true);

// 활성화
systemManager.ActivateConfiguration();
systemManager.StartRestartTwinCAT();

Console.WriteLine("프로젝트 자동 생성 완료!");

// 저장 및 종료
solution.SaveAs(Path.Combine(solutionPath, solutionName + ".sln"));
dte.Quit();
```

### 🔄 CI/CD 파이프라인

```yaml
# ✅ Azure DevOps Pipeline (YAML)
trigger:
- main

pool:
  vmImage: 'windows-latest'

steps:
- task: PowerShell@2
  displayName: 'Build TwinCAT Project'
  inputs:
    targetType: 'inline'
    script: |
      # TwinCAT Automation Interface로 빌드
      $dte = New-Object -ComObject "TcXaeShell.DTE.15.0"
      $solution = $dte.Solution
      $solution.Open("$(Build.SourcesDirectory)\MyProject.sln")

      # 빌드
      $solution.SolutionBuild.Build($true)

      if ($solution.SolutionBuild.LastBuildInfo -ne 0) {
        Write-Error "빌드 실패"
        exit 1
      }

      Write-Host "빌드 성공"
      $solution.Close($false)
      $dte.Quit()

- task: CopyFiles@2
  displayName: 'Copy Boot Project'
  inputs:
    SourceFolder: '$(Build.SourcesDirectory)\_Boot'
    Contents: '**'
    TargetFolder: '$(Build.ArtifactStagingDirectory)'

- task: PublishBuildArtifacts@1
  displayName: 'Publish Artifacts'
  inputs:
    PathtoPublish: '$(Build.ArtifactStagingDirectory)'
    ArtifactName: 'TwinCAT_Boot_Project'
```

### 📊 프로젝트 분석 및 문서 생성

```csharp
// ✅ PLC 변수 자동 추출 및 문서화
using TCatSysManagerLib;
using System.Xml;

ITcSysManager15 sysMan = /* ... */;
ITcSmTreeItem plcProject = sysMan.LookupTreeItem("TIPC^PlcProject1");
ITcPlcProject plcProj = (ITcPlcProject)plcProject;

// 심볼 정보 가져오기 (XML)
string symbolXml = plcProj.GenerateMappingInfo(false);

// XML 파싱
XmlDocument doc = new XmlDocument();
doc.LoadXml(symbolXml);

// Markdown 문서 생성
using (StreamWriter writer = new StreamWriter(@"C:\Docs\PLC_Variables.md"))
{
    writer.WriteLine("# PLC 변수 목록");
    writer.WriteLine();
    writer.WriteLine("| 변수명 | 데이터 타입 | 주소 | 설명 |");
    writer.WriteLine("|--------|-------------|------|------|");

    foreach (XmlNode node in doc.SelectNodes("//Symbol"))
    {
        string name = node.SelectSingleNode("Name")?.InnerText;
        string type = node.SelectSingleNode("BaseType")?.InnerText;
        string address = node.SelectSingleNode("BitOffs")?.InnerText;
        string comment = node.SelectSingleNode("Comment")?.InnerText ?? "";

        writer.WriteLine($"| `{name}` | {type} | {address} | {comment} |");
    }
}

Console.WriteLine("문서 생성 완료: PLC_Variables.md");
```

---

## 📚 부록

### 🔗 공식 리소스

| 리소스 | URL |
|--------|-----|
| 📖 **Beckhoff Infosys** | https://infosys.beckhoff.com/ |
| 💻 **GitHub - Beckhoff** | https://github.com/Beckhoff |
| 📦 **NuGet - TwinCAT.Ads** | https://www.nuget.org/packages/Beckhoff.TwinCAT.Ads |
| 🐍 **pyads (Python)** | https://github.com/stlehmann/pyads |
| 🌐 **AllTwinCAT 커뮤니티** | https://alltwincat.com/ |
| 🎓 **Contact & Coil** | https://www.contactandcoil.com/ |

### 🛠️ 개발 환경 설정

```bash
# ✅ TwinCAT 3 설치 (Windows)
# 1. Beckhoff 웹사이트에서 TwinCAT 3 다운로드
# 2. TwinCAT XAE (개발 환경) 설치
# 3. TwinCAT XAR (런타임) 설치

# ✅ .NET 라이브러리 설치
dotnet add package Beckhoff.TwinCAT.Ads --version 6.0.0

# ✅ Python 라이브러리 설치
pip install pyads

# ✅ Node.js 라이브러리 설치
npm install ads-client
```

### 📊 포트 번호 요약

| 포트 | 서비스 | 설명 |
|------|--------|------|
| **48898** | ADS TCP/IP | 표준 ADS 통신 |
| **8016** | Secure ADS | TLS 암호화 ADS |
| **4840** | OPC UA | OPC UA 서버 |
| **1883** | MQTT | 비보안 MQTT |
| **8883** | MQTT over TLS | 보안 MQTT |
| **851-854** | PLC Runtime | PLC 런타임 포트 |
| **501** | NC I | 첫 번째 NC 채널 |

### 🎯 학습 로드맵

```
TwinCAT 3 마스터하기
│
├── Week 1-2: 기초
│   ├── TwinCAT 설치 및 환경 구성
│   ├── ST 언어 기초
│   └── 간단한 PLC 프로그램 작성
│
├── Week 3-4: 통신
│   ├── ADS 프로토콜 이해
│   ├── C#/Python으로 PLC 제어
│   └── OPC UA 서버 설정
│
├── Week 5-6: Motion Control
│   ├── PLCopen Function Blocks
│   ├── 단일 축 제어
│   └── 다축 동기화
│
├── Week 7-8: HMI
│   ├── TwinCAT HMI 프로젝트 생성
│   ├── JavaScript API 활용
│   └── 서버 확장 개발
│
├── Week 9-10: IoT & Database
│   ├── MQTT 통신
│   ├── 클라우드 연동
│   └── SQL 데이터베이스 연동
│
└── Week 11-12: 고급 기능
    ├── Vision 시스템
    ├── Machine Learning 추론
    ├── Safety 프로그래밍
    └── Automation Interface
```

### 💡 베스트 프랙티스

1. **📝 코딩 표준**
   - 한글 주석 필수
   - 명명 규칙 준수 (FB_, g, etc.)
   - Function Block으로 모듈화

2. **🔐 보안**
   - Secure ADS 사용
   - 사용자 권한 관리
   - 민감한 데이터 암호화

3. **⚡ 성능**
   - Sum Command로 일괄 처리
   - 핸들 재사용
   - 비동기 I/O 활용

4. **🛡️ 안전**
   - TwinSAFE로 안전 로직 분리
   - 비상 정지 우선 처리
   - 안전 표준 준수 (IEC 61508)

5. **📊 유지보수**
   - 버전 관리 (Git)
   - 자동화된 빌드/배포
   - 포괄적인 문서화

---

## 🎉 결론

TwinCAT 3는 **산업 자동화의 모든 영역**을 커버하는 강력한 플랫폼입니다.

```
🚀 TwinCAT 3로 가능한 것들:
├── ⚙️  고속 PLC 제어 (마이크로초 사이클)
├── 🤖 정밀 모션 제어 (나노미터급)
├── 🌐 클라우드 IoT 연동
├── 📷 실시간 머신 비전
├── 🗄️  엔터프라이즈 데이터베이스 통합
├── 🛡️  SIL 3 안전 시스템
├── 🤖 AI/머신러닝 추론
└── 🖥️  웹 기반 HMI
```

이 문서가 여러분의 TwinCAT 3 개발 여정에 완벽한 가이드가 되기를 바랍니다! 🎓

---

**📧 피드백 및 기여**
이 문서에 대한 피드백이나 개선 사항이 있다면 언제든지 알려주세요!

**🔖 태그**: `#TwinCAT3` `#Beckhoff` `#산업자동화` `#PLC` `#IEC61131-3` `#ADS` `#MotionControl` `#IoT` `#머신비전` `#안전시스템`

---

> **© 2025 TwinCAT 3 Complete API Reference**
> 이 문서는 Beckhoff 공식 문서, GitHub 리포지토리, 커뮤니티 자료를 바탕으로 작성되었습니다.
> 최신 정보는 [Beckhoff Infosys](https://infosys.beckhoff.com/)를 참조하세요.
