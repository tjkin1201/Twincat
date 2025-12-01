# TwinCAT ADS API 빠른 참조 가이드

## 기본 정보

### ADS 통신 포트
```
TCP: 48898
UDP: 48899
Secure ADS (TLS): 8016
```

### AmsPort 번호
| 서비스 | 포트 |
|--------|------|
| System Service | 10000 |
| NC Runtime | 501 |
| PLC Runtime 1 | 851 |
| PLC Runtime 2 | 852 |
| PLC Runtime 3 | 853 |
| PLC Runtime 4 | 854 |

---

## C# 코드 예제

### 기본 연결 및 읽기/쓰기
```csharp
using TwinCAT.Ads;

// 연결
using (AdsClient client = new AdsClient())
{
    client.Connect("192.168.1.100.1.1", 851);

    // 읽기 (심볼릭)
    int value = (int)client.ReadSymbol("MAIN.counter", typeof(int));

    // 쓰기 (심볼릭)
    client.WriteSymbol("MAIN.setpoint", 100.0);
}
```

### 핸들 사용
```csharp
// 핸들 생성
uint handle = client.CreateVariableHandle("MAIN.temperature");

// 읽기
double temp = (double)client.ReadAny(handle, typeof(double));

// 쓰기
client.WriteAny(handle, 25.5);

// 핸들 삭제
client.DeleteVariableHandle(handle);
```

### 알림 등록
```csharp
// 이벤트 핸들러
client.AdsNotificationEx += (sender, e) =>
{
    Console.WriteLine($"{e.SymbolName} = {e.Value}");
};

// 알림 등록
uint handle = client.AddDeviceNotificationEx(
    "MAIN.counter",
    AdsTransMode.OnChange,
    TimeSpan.FromMilliseconds(100),
    TimeSpan.Zero,
    null,
    typeof(int)
);

// 알림 해제
client.DeleteDeviceNotification(handle);
```

### Sum Command (여러 변수 읽기)
```csharp
using TwinCAT.Ads.SumCommand;

// Sum Read 생성
SumSymbolRead sumRead = new SumSymbolRead(client);

// 변수 추가
sumRead.AddSymbol("MAIN.var1");
sumRead.AddSymbol("MAIN.var2");
sumRead.AddSymbol("MAIN.var3");

// 일괄 읽기
object[] values = sumRead.Read();

// 결과 처리
int var1 = (int)values[0];
double var2 = (double)values[1];
bool var3 = (bool)values[2];
```

---

## Python 코드 예제

### 기본 연결 및 읽기/쓰기
```python
import pyads

# 연결
plc = pyads.Connection('192.168.1.100.1.1', 851)
plc.open()

# 읽기
value = plc.read_by_name('MAIN.counter', pyads.PLCTYPE_INT)

# 쓰기
plc.write_by_name('MAIN.setpoint', 100, pyads.PLCTYPE_INT)

# 연결 종료
plc.close()
```

### 구조체 읽기
```python
import ctypes

class SensorData(ctypes.Structure):
    _fields_ = [
        ('temperature', ctypes.c_double),
        ('pressure', ctypes.c_double),
        ('status', ctypes.c_int)
    ]

data = plc.read_by_name('MAIN.sensorData', SensorData)
print(f"Temp: {data.temperature}")
```

### 알림 등록
```python
def callback(notification, data):
    print(f"Value: {notification.value}")

# 알림 속성
attr = pyads.NotificationAttrib(
    length=4,
    trans_mode=pyads.ADSTRANS_SERVERONCHA,
    cycle_time=100,
    max_delay=0
)

# 알림 등록
handle = plc.add_device_notification('MAIN.counter', attr, callback)

# 알림 해제
plc.del_device_notification(handle)
```

### 라우트 추가 (Linux/macOS)
```python
pyads.add_route("192.168.1.100.1.1", "192.168.1.100")
```

---

## C++ 코드 예제

### 기본 읽기/쓰기
```cpp
#include "AdsLib.h"

// AMS 주소 설정
AmsAddr addr;
addr.netId = {192, 168, 1, 100, 1, 1};
addr.port = 851;

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

// 변수 쓰기
int32_t writeValue = 100;
result = AdsSyncWriteReqEx(
    amsPort,
    &addr,
    ADSIGRP_SYM_VALBYHND,
    handle,
    sizeof(writeValue),
    &writeValue
);
```

---

## 중요 IndexGroup

### Sum Command
```
읽기: 0xF080
쓰기: 0xF081
```

### 심볼 핸들
```
이름으로 핸들 요청: 0xF003 (ADSIGRP_SYM_HNDBYNAME)
핸들로 값 읽기: 0xF005 (ADSIGRP_SYM_VALBYHND)
핸들로 값 쓰기: 0xF006 (ADSIGRP_SYM_VALBYNAME)
```

---

## Notification TransMode

| 모드 | C# | Python | 설명 |
|------|-----|--------|------|
| OnChange | `AdsTransMode.OnChange` | `pyads.ADSTRANS_SERVERONCHA` | 값 변경 시만 전송 |
| Cyclic | `AdsTransMode.Cyclic` | `pyads.ADSTRANS_SERVERCYCLE` | 주기적으로 전송 |

---

## 주요 에러 코드

| 에러 코드 | 16진수 | 설명 | 해결 방법 |
|-----------|--------|------|----------|
| 1792 | 0x700 | Driver Signature | OEM 인증서 설치 및 재부팅 |
| 1796 | 0x704 | Read/Write Permission | VAR_IN_OUT 값 확인, 함수 블록 호출 확인 |
| 1817 | 0x719 | Device Timeout | 네트워크 어댑터 설정 확인 |
| 1861 | 0x745 | Network/Route | 방화벽 설정, 네트워크 연결 확인 |
| 4115 | 0x1013 | System Clock | win8settick.bat 실행 및 재부팅 |

---

## 데이터 타입 매핑

### C# → PLC
| C# | PLC (IEC 61131-3) | 크기 |
|----|-------------------|------|
| `bool` | `BOOL` | 1 byte |
| `byte` | `BYTE` | 1 byte |
| `short` | `INT` | 2 bytes |
| `int` | `DINT` | 4 bytes |
| `long` | `LINT` | 8 bytes |
| `float` | `REAL` | 4 bytes |
| `double` | `LREAL` | 8 bytes |
| `string` | `STRING` | variable |

### Python → PLC
| Python | pyads 상수 | PLC |
|--------|------------|-----|
| `bool` | `PLCTYPE_BOOL` | `BOOL` |
| `int` | `PLCTYPE_INT` | `INT` |
| `int` | `PLCTYPE_DINT` | `DINT` |
| `float` | `PLCTYPE_REAL` | `REAL` |
| `float` | `PLCTYPE_LREAL` | `LREAL` |
| `str` | `PLCTYPE_STRING` | `STRING` |

---

## 방화벽 설정 (Windows PowerShell)

```powershell
# ADS TCP 포트
New-NetFirewallRule -DisplayName "ADS TCP" -Direction Inbound -LocalPort 48898 -Protocol TCP -Action Allow

# ADS UDP 포트
New-NetFirewallRule -DisplayName "ADS UDP" -Direction Inbound -LocalPort 48899 -Protocol UDP -Action Allow

# Secure ADS
New-NetFirewallRule -DisplayName "Secure ADS" -Direction Inbound -LocalPort 8016 -Protocol TCP -Action Allow
```

---

## NuGet 패키지 설치

```bash
# .NET ADS 클라이언트
dotnet add package Beckhoff.TwinCAT.Ads

# TCP Router (크로스 플랫폼)
dotnet add package Beckhoff.TwinCAT.Ads.TcpRouter
```

---

## Python 패키지 설치

```bash
pip install pyads
```

---

## ADS 상태 코드

| 상태 | 값 | 설명 |
|------|-----|------|
| Invalid | 0 | 유효하지 않음 |
| Idle | 1 | 대기 |
| Reset | 2 | 리셋 |
| Init | 3 | 초기화 |
| Start | 4 | 시작 |
| Run | 5 | 실행 중 |
| Stop | 6 | 정지 |

---

## 성능 최적화 팁

1. **Sum Command 사용**: 여러 변수를 한 번에 읽기/쓰기 (최대 500개 권장)
2. **핸들 재사용**: 반복적 접근 시 핸들 생성/삭제 최소화
3. **알림 활용**: 폴링 대신 Push 모델 사용
4. **비동기 작업**: C#에서 async/await 활용
5. **MaxDelay 설정**: 알림 일괄 처리로 네트워크 트래픽 감소

---

## 유용한 링크

- [Beckhoff Infosys 문서](https://infosys.beckhoff.com/)
- [Beckhoff ADS GitHub](https://github.com/Beckhoff/ADS)
- [pyads 문서](https://pyads.readthedocs.io/)
- [Stack Overflow - twincat-ads](https://stackoverflow.com/questions/tagged/twincat-ads)

---

**작성일**: 2025-11-24
