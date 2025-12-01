# TwinCAT 3 ADS API 심층 조사 보고서

## 목차
1. [ADS 프로토콜 개요](#1-ads-프로토콜-개요)
2. [ADS 아키텍처](#2-ads-아키텍처)
3. [주요 API 함수](#3-주요-api-함수)
4. [다양한 언어에서의 ADS 사용](#4-다양한-언어에서의-ads-사용)
5. [ADS Router 및 통신 설정](#5-ads-router-및-통신-설정)
6. [실시간 데이터 교환 메커니즘](#6-실시간-데이터-교환-메커니즘)
7. [에러 코드 및 디버깅](#7-에러-코드-및-디버깅)
8. [고급 기능](#8-고급-기능)
9. [참고 자료](#9-참고-자료)

---

## 1. ADS 프로토콜 개요

### 1.1 ADS란?

**ADS (Automation Device Specification)**는 TwinCAT의 통신 프로토콜로, TwinCAT 시스템 간 데이터 교환 및 제어를 가능하게 합니다.

#### 주요 특징:
- **미디어 독립성**: 직렬 또는 네트워크 연결을 통해 통신 가능
- **TCP/IP 기반**: 안정적이고 실시간 통신 보장
- **표준 포트**:
  - TCP: 48898
  - UDP: 48899
  - Secure ADS: 8016 (TLS 암호화)

### 1.2 ADS의 역할

ADS는 다음과 같은 기능을 수행합니다:
- TwinCAT 장치 간 데이터 교환
- 명령 실행 및 제어
- 실시간 변수 모니터링
- PLC, NC, CNC 등 다양한 런타임 시스템 통합

### 1.3 AMS (Automation Message Specification)

**AMS**는 ADS 데이터의 교환을 명시하는 프로토콜입니다.

#### AmsNetId
- **형식**: 6바이트 점 표기법 (예: "172.1.2.16.1.1")
- **기본값**: IP 주소 + ".1.1" (예: 192.168.1.100 → 192.168.1.100.1.1)
- **용도**: TwinCAT 메시지 라우터 식별

#### AmsPort
각 ADS 장치를 식별하는 포트 번호:

| 서비스/런타임 | AmsPort |
|--------------|---------|
| TwinCAT System Service | 10000 |
| NC (Numerical Control) | 501 |
| PLC Runtime 1 | 851 |
| PLC Runtime 2 | 852 |
| PLC Runtime 3 | 853 |
| PLC Runtime 4 | 854 |
| TwinCAT 3 PLC 범위 | 800-899 |

---

## 2. ADS 아키텍처

### 2.1 메시지 라우터 아키텍처

```
┌─────────────────────────────────────────────────────┐
│            TwinCAT Message Router                    │
│  ┌──────────────┐  ┌──────────────┐  ┌───────────┐ │
│  │  PLC Runtime │  │ NC Runtime   │  │  System   │ │
│  │  Port: 851   │  │ Port: 501    │  │Port: 10000│ │
│  └──────────────┘  └──────────────┘  └───────────┘ │
└─────────────────────────────────────────────────────┘
                       │
                       │ ADS over TCP/IP
                       │ Port 48898/48899
                       │
┌─────────────────────────────────────────────────────┐
│              ADS Client Application                  │
│  (C++, C#, Python, Java, LabVIEW, etc.)             │
└─────────────────────────────────────────────────────┘
```

### 2.2 통신 계층

1. **Application Layer**: 사용자 애플리케이션 (ADS Client)
2. **ADS Layer**: ADS 프로토콜 처리
3. **AMS Layer**: AmsNetId 기반 라우팅
4. **Transport Layer**: TCP/IP 또는 UDP
5. **Physical Layer**: Ethernet, Wi-Fi 등

### 2.3 주소 지정 체계

ADS 장치를 명시적으로 지정하기 위해 필요한 정보:
- **AmsNetId**: 대상 장치 식별 (6바이트)
- **AmsPort**: 대상 서비스/런타임 식별 (2바이트)
- **IndexGroup**: 데이터 그룹 식별 (4바이트)
- **IndexOffset**: 그룹 내 데이터 오프셋 (4바이트)

---

## 3. 주요 API 함수

### 3.1 기본 읽기/쓰기 함수

#### AdsSyncReadReq / AdsSyncReadReqEx2
**기능**: ADS 장치로부터 동기적으로 데이터 읽기

**파라미터**:
- `AmsNetId`: 대상 장치의 AMS Net ID
- `AmsPort`: 대상 포트
- `IndexGroup`: 읽을 데이터의 인덱스 그룹
- `IndexOffset`: 읽을 데이터의 인덱스 오프셋
- `Length`: 읽을 데이터 길이 (바이트)
- `DataBuffer`: 읽은 데이터를 저장할 버퍼

**사용 시나리오**: 단일 변수 또는 메모리 영역 읽기

#### AdsSyncWriteReq / AdsSyncWriteReqEx
**기능**: ADS 장치에 동기적으로 데이터 쓰기

**파라미터**:
- `AmsNetId`: 대상 장치의 AMS Net ID
- `AmsPort`: 대상 포트
- `IndexGroup`: 쓸 데이터의 인덱스 그룹
- `IndexOffset`: 쓸 데이터의 인덱스 오프셋
- `Length`: 쓸 데이터 길이 (바이트)
- `DataBuffer`: 쓸 데이터가 포함된 버퍼

**사용 시나리오**: 단일 변수 또는 메모리 영역 쓰기

#### AdsSyncReadWriteReqEx2
**기능**: ADS 장치에 동기적으로 데이터 읽기 및 쓰기

**파라미터**:
- 읽기 및 쓰기 파라미터 조합
- `WriteDataBuffer`: 쓸 데이터
- `ReadDataBuffer`: 읽은 데이터 저장 버퍼

**사용 시나리오**: 함수 호출, 명령 실행 등 입출력이 모두 필요한 작업

### 3.2 데이터 접근 방법

#### 3.2.1 IndexGroup/IndexOffset 직접 접근
**장점**: 가장 빠른 성능
**단점**: 주소가 동적으로 변경되면 무효화될 수 있음

```c
// 예시: 직접 메모리 접근
uint32_t indexGroup = 0x4020;  // 메모리 영역
uint32_t indexOffset = 0x1000; // 오프셋
```

#### 3.2.2 심볼릭 핸들 접근
**장점**: 안전성, PLC 재컴파일 시에도 유효
**단점**: 핸들 생성/삭제에 따른 추가 ADS 통신 필요

**프로세스**:
1. `CreateVariableHandle()` - 변수 이름으로 핸들 생성
2. 핸들을 사용하여 읽기/쓰기
3. `DeleteVariableHandle()` - 핸들 삭제

**핸들 생성을 위한 IndexGroup**:
- `ADSIGRP_SYM_HNDBYNAME (0xF003)`: 이름으로 핸들 요청

#### 3.2.3 심볼릭 경로 직접 접근
**장점**: 핸들 관리 불필요, 코드 가독성 높음
**단점**: 약간의 성능 오버헤드

```csharp
// C# 예시
string variableName = "MAIN.counter";
client.ReadSymbol(variableName, typeof(int));
```

### 3.3 ADS Sum Command (일괄 처리)

#### 개요
여러 변수를 하나의 ADS 명령으로 읽기/쓰기하여 통신 오버헤드를 크게 감소시킵니다.

#### 읽기 Sum Command
- **IndexGroup**: `0xF080`
- **IndexOffset**: 하위 명령 수 (읽을 변수 개수)

#### 쓰기 Sum Command
- **IndexGroup**: `0xF081`
- **IndexOffset**: 하위 명령 수 (쓸 변수 개수)

#### 성능 최적화
- **권장 최대 하위 명령**: 500개
- **이유**: PLC가 하나의 ADS 요청을 완전히 처리한 후 다음 사이클 시작
- **데이터 크기 제한**: AMS 라우터 기본 제한 2048KB

#### 사용 시나리오
- 대량의 변수를 주기적으로 읽어야 할 때
- HMI 화면 업데이트
- 데이터 로깅
- 시스템 모니터링

---

## 4. 다양한 언어에서의 ADS 사용

### 4.1 C++ (공식 Beckhoff ADS 라이브러리)

#### 설치 및 빌드
```bash
# GitHub에서 클론
git clone https://github.com/Beckhoff/ADS.git
cd ADS

# Meson과 Ninja를 사용하여 빌드
meson build
ninja -C build
```

#### 헤더 파일
```cpp
#include "TcAdsApi.h"
#include "TcAdsDef.h"
```

#### 기본 사용 예제
```cpp
#include "AdsLib.h"

// ADS 포트 열기
AmsAddr addr;
addr.netId = {192, 168, 1, 100, 1, 1};
addr.port = 851; // PLC Runtime 1

// 변수 읽기
int32_t value;
uint32_t bytesRead;
long result = AdsSyncReadReqEx2(
    amsPort,
    &addr,
    ADSIGRP_SYM_VALBYHND,
    handle,
    sizeof(value),
    &value,
    &bytesRead
);
```

#### 지원 플랫폼
- Windows (Visual Studio)
- Linux (GCC)
- FreeBSD
- macOS

### 4.2 C# / .NET (Beckhoff.TwinCAT.Ads)

#### 설치
```bash
# NuGet 패키지 설치
dotnet add package Beckhoff.TwinCAT.Ads
```

#### 기본 사용 예제
```csharp
using TwinCAT.Ads;

// AdsClient 인스턴스 생성
using (AdsClient client = new AdsClient())
{
    // 연결
    client.Connect("192.168.1.100.1.1", 851);

    // 변수 읽기 (심볼릭)
    int value = (int)client.ReadSymbol("MAIN.counter", typeof(int));

    // 변수 쓰기 (심볼릭)
    client.WriteSymbol("MAIN.setpoint", 100.0);

    // 핸들 사용
    uint handle = client.CreateVariableHandle("MAIN.temperature");
    double temp = (double)client.ReadAny(handle, typeof(double));
    client.DeleteVariableHandle(handle);
}
```

#### 비동기 읽기 예제
```csharp
// 비동기 상태 읽기
AdsState state = await client.ReadStateAsync();
Console.WriteLine($"ADS State: {state.State}");
```

#### 주요 클래스 및 메서드
- **AdsClient**: ADS 통신의 루트 객체
  - `Connect()`: 대상 장치 연결
  - `ReadSymbol()`: 심볼릭 변수 읽기
  - `WriteSymbol()`: 심볼릭 변수 쓰기
  - `CreateVariableHandle()`: 변수 핸들 생성
  - `DeleteVariableHandle()`: 변수 핸들 삭제
  - `AddDeviceNotification()`: 알림 등록
  - `DeleteDeviceNotification()`: 알림 삭제

- **SumSymbolRead**: 여러 심볼을 한 번에 읽기
- **SumSymbolWrite**: 여러 심볼을 한 번에 쓰기

#### 라이선스
무료 사용 가능 (TC1000 포함)

### 4.3 Python (pyads)

#### 설치
```bash
pip install pyads
```

#### 기본 사용 예제
```python
import pyads

# PLC 연결
plc = pyads.Connection('192.168.1.100.1.1', 851)
plc.open()

# 정수 변수 읽기
value = plc.read_by_name('MAIN.counter', pyads.PLCTYPE_INT)
print(f"Counter: {value}")

# 정수 변수 쓰기
plc.write_by_name('MAIN.setpoint', 100, pyads.PLCTYPE_INT)

# 연결 종료
plc.close()
```

#### 구조체 읽기 예제
```python
# 구조체 정의
import ctypes

class MyStruct(ctypes.Structure):
    _fields_ = [
        ('temperature', ctypes.c_double),
        ('pressure', ctypes.c_double),
        ('status', ctypes.c_int)
    ]

# 구조체 읽기
data = plc.read_by_name('MAIN.sensorData', MyStruct)
print(f"Temperature: {data.temperature}")
```

#### 알림 등록 예제
```python
# 콜백 함수 정의
def callback(notification, data):
    print(f"Value changed: {data}")

# 알림 등록
attr = pyads.NotificationAttrib(ctypes.sizeof(ctypes.c_int))
handles = plc.add_device_notification(
    'MAIN.counter',
    attr,
    callback
)

# 메인 루프
try:
    while True:
        time.sleep(1)
except KeyboardInterrupt:
    plc.del_device_notification(handles)
    plc.close()
```

#### 주요 기능
- **Connection 클래스**: ADS 연결 관리
  - `read_by_name()`: 변수 이름으로 읽기
  - `write_by_name()`: 변수 이름으로 쓰기
  - `read()`: IndexGroup/Offset으로 읽기
  - `write()`: IndexGroup/Offset으로 쓰기
  - `add_device_notification()`: 알림 등록
  - `del_device_notification()`: 알림 삭제

- **지원 데이터 타입**:
  - `PLCTYPE_BOOL`, `PLCTYPE_BYTE`, `PLCTYPE_WORD`
  - `PLCTYPE_INT`, `PLCTYPE_DINT`, `PLCTYPE_REAL`
  - `PLCTYPE_STRING`, 커스텀 구조체 등

#### 라우팅 설정
```python
# 로컬 라우트 추가 (Linux/macOS)
pyads.add_route("192.168.1.100.1.1", "192.168.1.100")
```

### 4.4 기타 언어

#### Java
- **TcJavaToAds 라이브러리**: Beckhoff 공식 Java 라이브러리
- **사용 사례**: Android 앱, 엔터프라이즈 시스템

#### LabVIEW
- **TC3 Interface for LabVIEW**: Beckhoff 공식 LabVIEW 툴킷
- **사용 사례**: 테스트 자동화, 데이터 수집

#### Delphi
- **TcAdsDll for Delphi**: Beckhoff 공식 Delphi 라이브러리
- **사용 사례**: 레거시 시스템 통합

#### MATLAB
- **TE1410 TC3 Interface for MATLAB**: Beckhoff 공식 MATLAB 툴킷
- **함수**:
  - `TC_ADS_SyncRead`
  - `TC_ADS_SyncWrite`
  - `TC_ADS_SyncReadWrite`

---

## 5. ADS Router 및 통신 설정

### 5.1 ADS Router 개요

**ADS Router (AMS Router)**는 ADS 클라이언트와 PLC/TwinCAT 런타임을 연결하는 중개자 역할을 수행합니다.

#### 역할
- ADS 메시지 라우팅 및 배포
- 네트워크 연결 관리
- 보안 및 인증

### 5.2 정적 라우트 설정

#### Windows (TwinCAT 설치된 경우)

1. **TwinCAT 아이콘** 클릭 (시스템 트레이)
2. **Router → Edit Routes** 선택
3. **Add** 버튼 클릭
4. 라우트 정보 입력:
   - **Route Name**: 연결 이름 (예: "Remote PLC")
   - **AmsNetId**: 대상 장치의 AMS Net ID
   - **Address**: 대상 IP 주소 또는 호스트 이름
   - **Transport Type**: TCP/IP
5. **Add Route** 또는 **Add Route (IP)** 클릭

#### TwinCAT Engineering (XAE)

1. **Solution Explorer**에서 **SYSTEM → Routes** 확장
2. **우클릭 → Add New Route**
3. 대상 장치 정보 입력
4. **Activate Configuration** 실행

#### Linux/macOS (pyads 사용)

```python
import pyads

# 라우트 추가
pyads.add_route("192.168.1.100.1.1", "192.168.1.100")

# 라우트 삭제
pyads.delete_route("192.168.1.100.1.1")
```

### 5.3 라우트 설정 확인

#### 명령줄 (Windows)
```bash
# TcAdsMonitor 사용
C:\TwinCAT\Ads Api\TcAdsMonitor.exe

# AmsNetId 확인
# HKEY_LOCAL_MACHINE\SOFTWARE\Beckhoff\TwinCAT3\System
```

#### 프로그래밍 방식
```csharp
// C# - 로컬 AmsNetId 확인
string localNetId = AmsNetId.Local.ToString();
Console.WriteLine($"Local AMS Net ID: {localNetId}");
```

### 5.4 ADS Router 구성 요소

#### Local Net ID
- 시스템의 고유 식별자
- 설치 시 IP 주소 기반으로 자동 생성 (예: 192.168.1.100 → 192.168.1.100.1.1)

#### 기본 포트
- TwinCAT 설치 시 자동 설정
- 기본 디렉토리: `C:\TwinCAT\AdsApi`

### 5.5 Config Mode와 Run Mode

#### Config Mode
- 구성 변경 가능
- PLC 런타임 중지 상태
- 라우트, I/O 매핑 수정

#### Run Mode
- PLC 런타임 실행
- 실시간 제어 활성화
- 읽기/쓰기 가능

#### 모드 전환
```bash
# TwinCAT System Manager에서
# Config → Run-Mode 선택
```

### 5.6 방화벽 설정

ADS 통신을 위해 다음 포트를 열어야 합니다:

| 포트 | 프로토콜 | 용도 |
|------|---------|------|
| 48898 | TCP | ADS 통신 |
| 48899 | UDP | ADS 브로드캐스트 |
| 8016 | TCP | Secure ADS (TLS) |

#### Windows Firewall 규칙 추가
```powershell
# ADS TCP 포트
New-NetFirewallRule -DisplayName "ADS TCP" -Direction Inbound -LocalPort 48898 -Protocol TCP -Action Allow

# ADS UDP 포트
New-NetFirewallRule -DisplayName "ADS UDP" -Direction Inbound -LocalPort 48899 -Protocol UDP -Action Allow
```

---

## 6. 실시간 데이터 교환 메커니즘

### 6.1 Push vs. Pull 모델

#### Pull 모델 (폴링)
- **방식**: 타이머 기반 주기적 읽기 호출
- **장점**: 구현 간단
- **단점**: 불필요한 네트워크 트래픽, 지연 발생

```csharp
// 폴링 예제 (비효율적)
while (true)
{
    int value = client.ReadSymbol("MAIN.counter", typeof(int));
    // 값 처리
    Thread.Sleep(100); // 100ms 대기
}
```

#### Push 모델 (알림)
- **방식**: 값 변경 시 또는 주기적으로 서버가 클라이언트에 자동 전송
- **장점**: 효율적, 낮은 지연, 네트워크 트래픽 감소
- **단점**: 설정 복잡도 증가

### 6.2 ADS Notification (알림)

#### 개요
ADS Notification은 TwinCAT 서버가 클라이언트에 자동으로 값을 전송하는 메커니즘입니다.

#### 알림 등록 프로세스
1. `AddDeviceNotification()` - 알림 등록
2. 이벤트 자동 발생 (TwinCAT에서)
3. 콜백 함수 호출 (클라이언트에서)
4. `DeleteDeviceNotification()` - 알림 해제

### 6.3 Notification 파라미터

#### TransMode (전송 모드)

| 모드 | 설명 | 사용 사례 |
|------|------|----------|
| `OnChange` | 값이 변경될 때만 전송 | 이벤트 기반 모니터링 |
| `Cyclic` | 주기적으로 전송 (변경 여부 무관) | 정기적 데이터 수집 |

#### CycleTime (사이클 타임)
- **설명**: PLC가 변수 변경을 확인하는 주기
- **단위**: 밀리초 (ms) 또는 100나노초 (hecto-nanosecond, hns)
- **예시**: 100ms → 100밀리초마다 변경 확인

#### MaxDelay (최대 지연)
- **설명**: 알림을 수집하여 일괄 전송하는 최대 대기 시간
- **단위**: 밀리초 (ms) 또는 100나노초
- **예시**:
  - 0 → 즉시 전송
  - 1000ms → 최대 1초 동안 알림 수집 후 일괄 전송

### 6.4 C# 알림 예제

```csharp
using TwinCAT.Ads;

using (AdsClient client = new AdsClient())
{
    client.Connect("192.168.1.100.1.1", 851);

    // 알림 이벤트 핸들러 등록
    client.AdsNotificationEx += (sender, e) =>
    {
        // 변수 이름
        string symbolName = e.SymbolName;

        // 변경된 값
        object value = e.Value;

        Console.WriteLine($"{symbolName} changed to {value}");
    };

    // 알림 등록
    uint notificationHandle = client.AddDeviceNotificationEx(
        "MAIN.counter",                     // 변수 이름
        AdsTransMode.OnChange,              // 변경 시에만
        TimeSpan.FromMilliseconds(100),     // 100ms 마다 확인
        TimeSpan.Zero,                      // 즉시 전송
        null,                               // 사용자 데이터
        typeof(int)                         // 데이터 타입
    );

    // 메인 루프
    Console.WriteLine("Press any key to stop...");
    Console.ReadKey();

    // 알림 해제
    client.DeleteDeviceNotification(notificationHandle);
}
```

### 6.5 Python 알림 예제

```python
import pyads
import time

# 콜백 함수
def on_value_changed(notification, data):
    value = notification.value
    timestamp = notification.timestamp
    print(f"Value: {value}, Time: {timestamp}")

# PLC 연결
plc = pyads.Connection('192.168.1.100.1.1', 851)
plc.open()

# 알림 속성 설정
attr = pyads.NotificationAttrib(
    length=4,  # 데이터 크기 (bytes)
    trans_mode=pyads.ADSTRANS_SERVERONCHA,  # OnChange 모드
    cycle_time=100,  # 100ms
    max_delay=0  # 즉시 전송
)

# 알림 등록
notification = plc.add_device_notification(
    'MAIN.counter',
    attr,
    on_value_changed
)

# 메인 루프
try:
    while True:
        time.sleep(1)
except KeyboardInterrupt:
    # 알림 해제
    plc.del_device_notification(notification)
    plc.close()
```

### 6.6 성능 특성

#### 통신 효율성
- **한 사이클 당**: 1개의 ADS 읽기 명령 + 1개의 ADS 쓰기 명령
- **예시**: 약 700개의 입출력 신호를 약 30ms 사이클에서 교환 가능

#### 알림 vs. 폴링 비교

| 항목 | 폴링 | 알림 |
|------|------|------|
| 네트워크 트래픽 | 높음 (지속적 요청) | 낮음 (변경 시만) |
| CPU 부하 | 높음 | 낮음 |
| 응답 시간 | 폴링 주기에 의존 | 변경 즉시 (TransMode에 따라) |
| 구현 복잡도 | 낮음 | 중간 |

---

## 7. 에러 코드 및 디버깅

### 7.1 ADS 에러 코드 분류

Beckhoff 공식 문서는 ADS 에러 코드를 다음과 같이 그룹화합니다:

| 범위 | 설명 |
|------|------|
| `0x0000...` | 글로벌 에러 코드 |
| `0x0500...` | 라우터 에러 코드 |
| `0x0700...` | 일반 ADS 에러 |
| `0x1000...` | RTime 에러 코드 |

### 7.2 주요 에러 코드 및 해결 방법

#### ADS Error 1792 (0x700) - Driver Signature Error
**원인**: 드라이버 서명 오류

**해결 방법**:
```bash
# OEM 인증서 설치 (.reg 파일 실행)
C:\TwinCAT\3.1\Target\OemCertificates\*.reg
```
시스템 재부팅 후 해결

#### ADS Error 1796 (0x704) - Read/Write Permission
**원인**:
- 읽기/쓰기 권한 부족
- `VAR_IN_OUT`이 유효한 값이 없을 때 (함수 블록 미호출)

**해결 방법**:
```iecst
// 함수 블록 호출 확인
fbMyBlock(
    input := someValue,
    output => result
);
```

#### ADS Error 1817 (0x719) - Device Timeout
**원인**:
- 네트워크 카드 설정 오류
- IP 스택 TcCOM 객체 시작 실패

**해결 방법**:
1. TwinCAT 프로젝트의 "Adapter" 설정 확인
2. 올바른 네트워크 어댑터 선택
3. IP 주소 및 서브넷 마스크 확인

#### ADS Error 1861 (0x745) - Network/Route Issues
**원인**:
- 네트워크 라우트 물리적 단절
- 방화벽 차단

**해결 방법**:
```powershell
# Windows Firewall 예외 추가
New-NetFirewallRule -DisplayName "ADS TCP" -Direction Inbound -LocalPort 48898 -Protocol TCP -Action Allow
New-NetFirewallRule -DisplayName "ADS UDP" -Direction Inbound -LocalPort 48899 -Protocol UDP -Action Allow
```

네트워크 케이블 및 스위치 연결 확인

#### ADS Error 4115 (0x1013) - System Clock Setup Fails
**원인**: RTime 시스템 클럭 설정 실패

**해결 방법**:
```bash
# 관리자 권한으로 실행
C:\TwinCAT\3.1\System\win8settick.bat

# 시스템 재부팅
```

### 7.3 디버깅 도구 및 기법

#### TcAdsMonitor
- **위치**: `C:\TwinCAT\Ads Api\TcAdsMonitor.exe`
- **기능**: 실시간 ADS 통신 모니터링

#### Visual Studio 디버깅
```csharp
// 예외 처리
try
{
    int value = (int)client.ReadSymbol("MAIN.counter", typeof(int));
}
catch (AdsErrorException ex)
{
    Console.WriteLine($"ADS Error {ex.ErrorCode}: {ex.Message}");
}
```

#### Python 디버깅
```python
import pyads

try:
    value = plc.read_by_name('MAIN.counter', pyads.PLCTYPE_INT)
except pyads.ADSError as e:
    print(f"ADS Error {e.err_code}: {e}")
```

#### 로깅
```csharp
// C# - ADS 상태 읽기
StateInfo state = client.ReadState();
Console.WriteLine($"ADS State: {state.AdsState}, Device State: {state.DeviceState}");
```

### 7.4 일반적인 트러블슈팅 체크리스트

1. **연결 확인**
   - [ ] AmsNetId가 올바른가?
   - [ ] AmsPort가 올바른가? (PLC Runtime 1 = 851)
   - [ ] 네트워크 연결이 정상인가?

2. **라우트 확인**
   - [ ] 정적 라우트가 추가되었는가?
   - [ ] 양방향 라우트가 설정되었는가?

3. **방화벽 확인**
   - [ ] 포트 48898 (TCP) 열려 있는가?
   - [ ] 포트 48899 (UDP) 열려 있는가?

4. **PLC 상태 확인**
   - [ ] PLC가 Run 모드인가?
   - [ ] 변수가 존재하는가?
   - [ ] 변수 이름이 정확한가?

5. **권한 확인**
   - [ ] TwinCAT이 관리자 권한으로 실행되고 있는가?
   - [ ] 읽기/쓰기 권한이 있는가?

---

## 8. 고급 기능

### 8.1 Secure ADS (TLS 암호화)

#### 개요
- **도입 버전**: TwinCAT 3.1.4024.0 이상
- **프로토콜**: TLSv1.2
- **포트**: TCP 8016
- **라이선스**: TC1000에 포함 (무료)

#### 인증 방법

##### 1. Self-Signed Certificates (자체 서명 인증서)
- TwinCAT 첫 시작 시 자동 생성
- **유효 기간**: 2000-01-01 ~ 2061-01-01 (보안상 너무 긺)
- **사용 사례**: 테스트 환경

##### 2. Pre-Shared Keys (PSK)
- TwinCAT 시스템에 저장된 사전 공유 키
- 들어오는 ADS 라우트 인증
- **사용 사례**: 유지보수 직원 접근 권한 부여

##### 3. Custom Certificates with CA
- 고객이 자체 인증서 생성 및 관리
- 공통 Certificate Authority (CA)를 통한 동적 구성
- 추가 구성 없이 암호화 통신 가능
- **사용 사례**: 엔터프라이즈 환경

#### 보안 고려사항
- **전송 암호화**: 네트워크 전송 중 암호화
- **종단 간 암호화 아님**: 컴포넌트 간 종단 간 암호화는 아님
- **권장**: 중요한 데이터 전송 시 Secure ADS 사용

#### 참고 문서
- [Secure ADS 공식 매뉴얼](https://download.beckhoff.com/download/document/automation/twincat3/Secure_ADS_EN.pdf)

### 8.2 TCP Router (독립 실행형 라우터)

#### Beckhoff.TwinCAT.Ads.TcpRouter 패키지
- **기능**: Windows가 아닌 시스템에서 ADS 통신 지원
- **플랫폼**: Linux, macOS, FreeBSD 등
- **사용 사례**:
  - Docker 컨테이너에서 ADS 클라이언트 실행
  - 크로스 플랫폼 애플리케이션

#### 설치 (NuGet)
```bash
dotnet add package Beckhoff.TwinCAT.Ads.TcpRouter
```

### 8.3 성능 최적화 기법

#### 1. Sum Command 사용
- 여러 변수를 한 번에 읽기/쓰기
- 통신 오버헤드 최소화
- **권장 최대**: 500개 변수

#### 2. 핸들 재사용
```csharp
// 핸들 생성 (한 번만)
uint handle = client.CreateVariableHandle("MAIN.counter");

// 여러 번 사용
for (int i = 0; i < 1000; i++)
{
    int value = (int)client.ReadAny(handle, typeof(int));
    // 처리
}

// 핸들 삭제
client.DeleteVariableHandle(handle);
```

#### 3. 비동기 작업 (C#)
```csharp
// 비동기 읽기
int value = await client.ReadAsync<int>("MAIN.counter", CancellationToken.None);

// 여러 작업 병렬 처리
var tasks = new List<Task<int>>();
tasks.Add(client.ReadAsync<int>("MAIN.var1", CancellationToken.None));
tasks.Add(client.ReadAsync<int>("MAIN.var2", CancellationToken.None));
await Task.WhenAll(tasks);
```

#### 4. 알림 최적화
- `MaxDelay` 설정으로 알림 일괄 처리
- 불필요한 알림 등록 방지
- 사용하지 않는 알림 즉시 삭제

### 8.4 ADS 상태 관리

#### ADS State

| 상태 | 값 | 설명 |
|------|-----|------|
| Invalid | 0 | 유효하지 않음 |
| Idle | 1 | 대기 |
| Reset | 2 | 리셋 |
| Init | 3 | 초기화 |
| Start | 4 | 시작 |
| Run | 5 | 실행 중 |
| Stop | 6 | 정지 |

#### 상태 읽기
```csharp
StateInfo state = client.ReadState();
Console.WriteLine($"ADS State: {state.AdsState}");
Console.WriteLine($"Device State: {state.DeviceState}");
```

### 8.5 Symbol Information (심볼 정보)

#### Symbol Loader
```csharp
// Symbol Loader 사용
TcAdsSymbolInfoLoader loader = client.CreateSymbolInfoLoader();

// 모든 심볼 로드
IEnumerable<ITcAdsSymbol> symbols = loader.GetSymbols();

foreach (var symbol in symbols)
{
    Console.WriteLine($"Name: {symbol.Name}");
    Console.WriteLine($"Type: {symbol.Type}");
    Console.WriteLine($"Size: {symbol.Size}");
    Console.WriteLine($"IndexGroup: 0x{symbol.IndexGroup:X}");
    Console.WriteLine($"IndexOffset: 0x{symbol.IndexOffset:X}");
}
```

#### 데이터 타입 정보
```csharp
// 특정 심볼 정보
ITcAdsSymbol symbol = loader.GetSymbol("MAIN.counter");
ITcAdsDataType dataType = symbol.DataType;

Console.WriteLine($"Type Name: {dataType.Name}");
Console.WriteLine($"Base Type: {dataType.BaseType}");
```

### 8.6 다중 PLC 통신

#### 동일 시스템의 여러 런타임
```csharp
// PLC Runtime 1 (Port 851)
AdsClient client1 = new AdsClient();
client1.Connect("192.168.1.100.1.1", 851);

// PLC Runtime 2 (Port 852)
AdsClient client2 = new AdsClient();
client2.Connect("192.168.1.100.1.1", 852);

// 각각 독립적으로 통신
int value1 = client1.ReadSymbol("MAIN.counter", typeof(int));
int value2 = client2.ReadSymbol("MAIN.counter", typeof(int));
```

#### 여러 원격 시스템
```csharp
// 원격 PLC 1
AdsClient plc1 = new AdsClient();
plc1.Connect("192.168.1.100.1.1", 851);

// 원격 PLC 2
AdsClient plc2 = new AdsClient();
plc2.Connect("192.168.1.101.1.1", 851);
```

---

## 9. 참고 자료

### 9.1 공식 Beckhoff 문서

#### Infosys 문서
- [ADS Introduction](https://infosys.beckhoff.com/content/1033/tcadscommon/12439470475.html)
- [TwinCAT ADS .NET API Documentation](https://infosys.beckhoff.com/content/1033/tc3_adsnetref/7312567947.html)
- [ADS Port Numbers](https://infosys.beckhoff.com/content/1033/tcplclib_tc2_system/31084171.html)
- [ADS Return Codes](https://infosys.beckhoff.com/content/1033/tf6701_tc3_iot_communication_mqtt/374277003.html)

#### 다운로드 센터
- [TwinCAT ADS-DLL C++ Manual](https://download.beckhoff.com/download/document/automation/twincat3/TwinCAT_ADS-DLL_C_EN.pdf)
- [Secure ADS Manual](https://download.beckhoff.com/download/document/automation/twincat3/Secure_ADS_EN.pdf)
- [ADS .NET Samples Manual](https://download.beckhoff.com/download/document/automation/twincat3/TwinCAT_ADS_NET_Samples_EN.pdf)

#### 제품 페이지
- [TC1000 TwinCAT 3 ADS](https://www.beckhoff.com/en-us/products/automation/twincat/tc1xxx-twincat-3-base/tc1000.html)

### 9.2 GitHub 저장소

#### 공식 Beckhoff 저장소
- [Beckhoff/ADS](https://github.com/Beckhoff/ADS) - C/C++ 라이브러리 (크로스 플랫폼)
- 지원 플랫폼: Linux, FreeBSD, macOS, Windows

#### 커뮤니티 프로젝트
- [stlehmann/pyads](https://github.com/stlehmann/pyads) - Python ADS 래퍼
- [densogiaichned/dsian.TwinCAT.Ads.Server.Mock](https://github.com/densogiaichned/dsian.TwinCAT.Ads.Server.Mock) - ADS 서버 모킹 (단위 테스트용)

### 9.3 NuGet 패키지

- **Beckhoff.TwinCAT.Ads** (최신: 6.2.521)
  - .NET ADS 클라이언트 라이브러리
  - [NuGet 페이지](https://www.nuget.org/packages/Beckhoff.TwinCAT.Ads)

- **Beckhoff.TwinCAT.Ads.TcpRouter** (최신: 6.2.521)
  - 크로스 플랫폼 TCP 라우터
  - [NuGet 페이지](https://www.nuget.org/packages/Beckhoff.TwinCAT.Ads.TcpRouter)

### 9.4 Python 라이브러리

- **pyads** (최신: 3.5.0)
  - Python ADS 래퍼
  - [PyPI](https://pypi.org/project/pyads/)
  - [공식 문서](https://pyads.readthedocs.io/en/latest/)

### 9.5 커뮤니티 및 포럼

- **Stack Overflow**: [twincat-ads 태그](https://stackoverflow.com/questions/tagged/twincat-ads)
- **PLCCoder.com**: [ADS 튜토리얼](https://www.plccoder.com/communicating-between-beckhoff-controllers-part-2-ads/)
- **twinControls Forum**: TwinCAT 관련 질문 및 답변

### 9.6 블로그 및 기술 문서

- [How runZero speaks to the TwinCAT 3 ADS Protocol](https://www.runzero.com/blog/twincat-3-ads-protocol/) - ADS 프로토콜 심층 분석
- [TwinCAT Protocol: Evolution and Architecture](https://emqx.medium.com/twincat-protocol-evolution-and-architecture-7adb528ed9ae) - TwinCAT 아키텍처 설명
- [Using Python to Communicate with TwinCAT By ADS](http://soup01.com/en/2022/06/02/beckhoffusing-python-to-communicate-with-twincat-by-ads/) - Python ADS 튜토리얼

### 9.7 웨비나 및 교육 자료

- [TwinCAT 3.1 | Transport encryption for ADS – Secure ADS](https://www.beckhoff.com/en-en/support/webinars/twincat-3-1-transport-encryption-for-ads-secure-ads.html) - Beckhoff 공식 웨비나

---

## 요약

TwinCAT 3의 ADS API는 강력하고 유연한 통신 프로토콜로, 다음과 같은 특징을 갖습니다:

### 주요 장점
1. **플랫폼 독립성**: Windows, Linux, macOS, FreeBSD 등 다양한 플랫폼 지원
2. **언어 다양성**: C++, C#, Python, Java, LabVIEW, Delphi 등 다양한 언어 지원
3. **실시간 성능**: 낮은 지연, 높은 처리량
4. **확장성**: Sum Command, 알림 등 최적화 기능
5. **보안**: Secure ADS를 통한 TLS 암호화

### 적용 분야
- **산업 자동화**: PLC 제어 및 모니터링
- **HMI/SCADA**: 실시간 데이터 시각화
- **데이터 로깅**: 센서 데이터 수집 및 저장
- **테스트 자동화**: 장비 테스트 및 검증
- **엔터프라이즈 통합**: MES, ERP 시스템 연동

### 개발 권장사항
1. **심볼릭 접근 사용**: 코드 유지보수성 향상
2. **알림 활용**: 폴링 대신 Push 모델 사용
3. **Sum Command 활용**: 대량 데이터 처리 시 성능 최적화
4. **Secure ADS 적용**: 중요 데이터 전송 시 보안 강화
5. **에러 처리**: 적절한 예외 처리 및 로깅

---

**작성일**: 2025-11-24
**버전**: 1.0
**기반**: TwinCAT 3.1, ADS API 6.x

