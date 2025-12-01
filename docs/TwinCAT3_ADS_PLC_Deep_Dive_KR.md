# 🔥 TwinCAT 3 ADS & PLC 프로그래밍 심화 가이드

> **📘 ST & C++ 중심 실전 개발 가이드**
> 최종 업데이트: 2025년 1월
> 대상: 중급 ~ 고급 TwinCAT 개발자
> 버전: 2.0

---

## 📑 목차

- [Part 1: ADS API 완전 정복](#part-1-ads-api-완전-정복)
  - [1.1 ADS 기본 아키텍처](#11-ads-기본-아키텍처)
  - [1.2 ST에서 ADS 사용하기](#12-st에서-ads-사용하기)
  - [1.3 C++ ADS 라이브러리](#13-c-ads-라이브러리)
  - [1.4 IndexGroup/IndexOffset 완전 가이드](#14-indexgroupindexoffset-완전-가이드)
  - [1.5 고급 패턴: 비동기 & 멀티스레딩](#15-고급-패턴-비동기--멀티스레딩)
  - [1.6 성능 최적화](#16-성능-최적화)
  - [1.7 에러 처리 및 디버깅](#17-에러-처리-및-디버깅)

- [Part 2: PLC 프로그래밍 마스터](#part-2-plc-프로그래밍-마스터)
  - [2.1 ST 고급 문법](#21-st-고급-문법)
  - [2.2 Function Block 설계 패턴](#22-function-block-설계-패턴)
  - [2.3 포인터 및 레퍼런스](#23-포인터-및-레퍼런스)
  - [2.4 TcCOM - C++ 모듈 개발](#24-tccom---c-모듈-개발)
  - [2.5 메모리 관리 및 최적화](#25-메모리-관리-및-최적화)
  - [2.6 실시간 성능 고려사항](#26-실시간-성능-고려사항)
  - [2.7 고급 라이브러리 활용](#27-고급-라이브러리-활용)

---

# Part 1: ADS API 완전 정복

## 1.1 ADS 기본 아키텍처

### 📐 ADS 계층 구조

```
┌──────────────────────────────────────────────────┐
│           Application Layer                       │
│  (PLC, HMI, C++/C# Application)                  │
└──────────────┬───────────────────────────────────┘
               │
┌──────────────▼───────────────────────────────────┐
│           ADS Protocol Layer                      │
│  ┌────────────┐  ┌────────────┐  ┌────────────┐ │
│  │ Read/Write │  │ ReadWrite  │  │Notification│ │
│  └────────────┘  └────────────┘  └────────────┘ │
└──────────────┬───────────────────────────────────┘
               │
┌──────────────▼───────────────────────────────────┐
│           AMS Router                              │
│  (Port Routing & Message Distribution)           │
└──────────────┬───────────────────────────────────┘
               │
┌──────────────▼───────────────────────────────────┐
│           Transport Layer                         │
│  TCP/IP (Port 48898) or Local Shared Memory      │
└──────────────────────────────────────────────────┘
```

### 🔑 ADS 주소 지정 체계

```
ADS Address = AmsNetId + AmsPort

┌─────────────────────────────────────────┐
│ AmsNetId: 192.168.1.100.1.1             │
│           └─┬─┘ └─┬─┘ └┬┘ └┬┘ └─┬─┘     │
│             │     │    │   │    │       │
│          Network  Host TC TC Runtime    │
│                        ID ID  Index     │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│ AmsPort: 851 (PLC Runtime Port)         │
│                                         │
│  Common Ports:                          │
│  - 10000: System Service                │
│  - 350:   Real-time (TC2 compat)        │
│  - 851:   First PLC Runtime             │
│  - 852:   Second PLC Runtime            │
│  - 501:   NC I Interpreter              │
│  - 500:   NC Safety                     │
└─────────────────────────────────────────┘
```

### 📊 ADS 명령어 종류

| 명령어 | ID | 기능 | 사용 사례 |
|--------|-------|------|-----------|
| **ADS_Read** | 0x02 | 데이터 읽기 | 변수 값 조회 |
| **ADS_Write** | 0x03 | 데이터 쓰기 | 변수 값 설정 |
| **ADS_ReadWrite** | 0x09 | 읽기+쓰기 동시 | RPC, 심볼 핸들 생성 |
| **ADS_ReadState** | 0x04 | 상태 읽기 | PLC Run/Stop 확인 |
| **ADS_WriteControl** | 0x05 | 제어 명령 | PLC 시작/중지 |
| **ADS_AddNotification** | 0x06 | 알림 등록 | 변수 변경 감지 |
| **ADS_DelNotification** | 0x07 | 알림 해제 | 알림 구독 취소 |

---

## 1.2 ST에서 ADS 사용하기

### 📝 기본 ADSREAD / ADSWRITE

```iecst
// ✅ ST - ADS를 이용한 다른 PLC 변수 읽기
PROGRAM ReadFromAnotherPLC
VAR
    // ADS 클라이언트 설정
    sNetId : T_AmsNetId := '192.168.1.100.1.1';  // 대상 PLC NetId
    nPort : T_AmsPort := 852;                     // 두 번째 PLC Runtime

    // 읽을 변수 정보
    sVarName : STRING := 'MAIN.ProductionCount';
    nProductionCount : DINT;

    // ADS 통신 Function Blocks
    fbGetSymHandleByName : ADSREAD;
    fbReadByHandle : ADSREAD;
    fbReleaseHandle : ADSWRITE;

    nSymHandle : UDINT;  // 심볼 핸들
    eState : (INIT, GET_HANDLE, READ_VALUE, RELEASE_HANDLE, DONE, ERROR);
    nErrId : UDINT;
END_VAR

CASE eState OF
    INIT:
        // 1단계: 심볼 핸들 얻기
        fbGetSymHandleByName(
            NETID := sNetId,
            PORT := nPort,
            IDXGRP := ADSIGRP_SYM_HNDBYNAME,
            IDXOFFS := 0,
            LEN := SIZEOF(nSymHandle),
            SRCADDR := ADR(sVarName),
            DESTADDR := ADR(nSymHandle),
            READ := TRUE
        );

        IF NOT fbGetSymHandleByName.BUSY THEN
            IF fbGetSymHandleByName.ERR THEN
                nErrId := fbGetSymHandleByName.ERRID;
                eState := ERROR;
            ELSE
                eState := READ_VALUE;
            END_IF
            fbGetSymHandleByName(READ := FALSE);
        END_IF

    READ_VALUE:
        // 2단계: 핸들로 변수 값 읽기
        fbReadByHandle(
            NETID := sNetId,
            PORT := nPort,
            IDXGRP := ADSIGRP_SYM_VALBYHND,
            IDXOFFS := nSymHandle,
            LEN := SIZEOF(nProductionCount),
            DESTADDR := ADR(nProductionCount),
            READ := TRUE
        );

        IF NOT fbReadByHandle.BUSY THEN
            IF fbReadByHandle.ERR THEN
                nErrId := fbReadByHandle.ERRID;
                eState := ERROR;
            ELSE
                // 성공: nProductionCount에 값이 저장됨
                ADSLOGDINT(msgCtrlMask := ADSLOG_MSGTYPE_HINT,
                          msgFmtStr := '생산 개수: %d',
                          dintArg := nProductionCount);
                eState := RELEASE_HANDLE;
            END_IF
            fbReadByHandle(READ := FALSE);
        END_IF

    RELEASE_HANDLE:
        // 3단계: 핸들 해제
        fbReleaseHandle(
            NETID := sNetId,
            PORT := nPort,
            IDXGRP := ADSIGRP_SYM_RELEASEHND,
            IDXOFFS := nSymHandle,
            LEN := 0,
            SRCADDR := 0,
            WRITE := TRUE
        );

        IF NOT fbReleaseHandle.BUSY THEN
            eState := DONE;
            fbReleaseHandle(WRITE := FALSE);
        END_IF

    ERROR:
        // 에러 처리
        ADSLOGSTR(msgCtrlMask := ADSLOG_MSGTYPE_ERROR,
                 msgFmtStr := 'ADS 에러: 0x%x',
                 strArg := UDINT_TO_HEXSTR(nErrId, 8, FALSE));
END_CASE
```

### 🔄 ADSRDWRT (Read-Write) - RPC 패턴

```iecst
// ✅ ST - ADS ReadWrite로 함수 호출 (Remote Procedure Call)
PROGRAM RPC_Example
VAR
    sNetId : T_AmsNetId := '192.168.1.100.1.1';
    nPort : T_AmsPort := 851;

    // RPC 파라미터
    stInputData : ST_CalculateParams := (
        nValue1 := 100,
        nValue2 := 50,
        eOperation := eOP_ADD
    );
    stOutputData : ST_CalculateResult;

    fbRPC : ADSRDWRT;
    bExecute : BOOL;
END_VAR

TYPE ST_CalculateParams :
STRUCT
    nValue1 : INT;
    nValue2 : INT;
    eOperation : (eOP_ADD, eOP_SUB, eOP_MUL, eOP_DIV);
END_STRUCT
END_TYPE

TYPE ST_CalculateResult :
STRUCT
    nResult : INT;
    bSuccess : BOOL;
END_STRUCT
END_TYPE

// RPC 실행
IF bExecute THEN
    fbRPC(
        NETID := sNetId,
        PORT := nPort,
        IDXGRP := 16#12345,  // 사용자 정의 IndexGroup
        IDXOFFS := 0,
        WRITELEN := SIZEOF(stInputData),
        READLEN := SIZEOF(stOutputData),
        SRCADDR := ADR(stInputData),
        DESTADDR := ADR(stOutputData),
        WRTRD := TRUE
    );

    IF NOT fbRPC.BUSY THEN
        IF NOT fbRPC.ERR THEN
            // 결과 확인
            IF stOutputData.bSuccess THEN
                ADSLOGDINT(msgCtrlMask := ADSLOG_MSGTYPE_HINT,
                          msgFmtStr := '계산 결과: %d',
                          dintArg := stOutputData.nResult);
            END_IF
        END_IF
        bExecute := FALSE;
        fbRPC(WRTRD := FALSE);
    END_IF
END_IF
```

### 🔔 ADS Notification (ST)

```iecst
// ✅ ST - ADS 알림으로 변수 변경 감지
PROGRAM ADS_Notification_Example
VAR
    sNetId : T_AmsNetId := '192.168.1.100.1.1';
    nPort : T_AmsPort := 851;

    fbAddNotification : ADSTRANSMODE;
    fbNotificationData : ADSNOTIFICATION;

    sVarName : STRING := 'MAIN.EmergencyStop';
    nNotificationHandle : UDINT;

    bEmergencyStop : BOOL;
    bAlarmTriggered : BOOL;

    eState : (INIT, SUBSCRIBE, MONITORING);
END_VAR

CASE eState OF
    INIT:
        // 알림 등록
        fbAddNotification(
            NETID := sNetId,
            PORT := nPort,
            IDXGRP := ADSIGRP_SYM_HNDBYNAME,
            IDXOFFS := 0,
            ATTRIB := (
                cbLength := SIZEOF(bEmergencyStop),
                nTransMode := ADSTRANS_SERVERONCHA,  // 값 변경 시
                nMaxDelay := 0,  // 즉시
                nCycleTime := 0  // OnChange 모드에서는 무시됨
            ),
            PDATA := ADR(sVarName),
            TMOUT := T#5s,
            ENABLE := TRUE
        );

        IF fbAddNotification.VALID THEN
            nNotificationHandle := fbAddNotification.HNOTIFICATION;
            eState := MONITORING;
        ELSIF fbAddNotification.ERR THEN
            ADSLOGSTR(msgCtrlMask := ADSLOG_MSGTYPE_ERROR,
                     msgFmtStr := '알림 등록 실패: 0x%x',
                     strArg := UDINT_TO_HEXSTR(fbAddNotification.ERRID, 8, FALSE));
        END_IF

    MONITORING:
        // 알림 데이터 확인
        fbNotificationData(
            HNOTIFICATION := nNotificationHandle
        );

        IF fbNotificationData.VALID THEN
            // 새 데이터가 도착했을 때
            MEMCPY(destAddr := ADR(bEmergencyStop),
                   srcAddr := fbNotificationData.PDATA,
                   n := fbNotificationData.CBDATA);

            IF bEmergencyStop AND NOT bAlarmTriggered THEN
                // 비상 정지 감지!
                ADSLOGSTR(msgCtrlMask := ADSLOG_MSGTYPE_ERROR,
                         msgFmtStr := '!!! 비상 정지 감지 !!!',
                         strArg := '');

                // 긴급 조치 실행
                EmergencyShutdown();
                bAlarmTriggered := TRUE;
            ELSIF NOT bEmergencyStop THEN
                bAlarmTriggered := FALSE;
            END_IF
        END_IF
END_CASE
```

### 📦 구조체 배열 일괄 읽기/쓰기

```iecst
// ✅ ST - 구조체 배열 한 번에 읽기
PROGRAM BulkStructRead
VAR
    sNetId : T_AmsNetId := '192.168.1.100.1.1';
    nPort : T_AmsPort := 851;

    // 센서 데이터 구조체
    aSensorData : ARRAY[1..100] OF ST_SensorData;

    fbReadBulk : ADSREAD;
    bTrigger : BOOL;
END_VAR

TYPE ST_SensorData :
STRUCT
    fTemperature : REAL;      // 온도 [°C]
    fPressure : REAL;         // 압력 [bar]
    nTimestamp : UDINT;       // 타임스탬프 [ms]
    bValid : BOOL;            // 유효 플래그
END_STRUCT
END_TYPE

// 대량 데이터 읽기
IF bTrigger THEN
    fbReadBulk(
        NETID := sNetId,
        PORT := nPort,
        IDXGRP := ADSIGRP_SYM_VALBYHND,
        IDXOFFS := 16#12340000,  // 구조체 배열 핸들 (사전에 획득)
        LEN := SIZEOF(aSensorData),  // 100개 구조체 전체 크기
        DESTADDR := ADR(aSensorData),
        READ := TRUE
    );

    IF NOT fbReadBulk.BUSY THEN
        IF NOT fbReadBulk.ERR THEN
            // 성공: 100개 센서 데이터 모두 읽음
            ProcessSensorData(aSensorData);
        ELSE
            ADSLOGSTR(msgCtrlMask := ADSLOG_MSGTYPE_ERROR,
                     msgFmtStr := '대량 읽기 실패: 0x%x',
                     strArg := UDINT_TO_HEXSTR(fbReadBulk.ERRID, 8, FALSE));
        END_IF
        bTrigger := FALSE;
        fbReadBulk(READ := FALSE);
    END_IF
END_IF
```

---

## 1.3 C++ ADS 라이브러리

### 🔧 기본 설정 (Windows)

```cpp
// ✅ C++ - ADS 라이브러리 초기화
#include <Windows.h>
#include <TcAdsDef.h>
#include <TcAdsAPI.h>

#pragma comment(lib, "TcAdsDll.lib")

// ADS 포트 열기
long AdsPortOpen()
{
    long nPort = AdsPortOpenEx();
    if (nPort == 0) {
        std::cerr << "ADS 포트 열기 실패" << std::endl;
        return -1;
    }
    std::cout << "ADS 포트 열림: " << nPort << std::endl;
    return nPort;
}

// ADS 포트 닫기
void AdsPortClose(long nPort)
{
    long nErr = AdsPortCloseEx(nPort);
    if (nErr) {
        std::cerr << "포트 닫기 실패: 0x" << std::hex << nErr << std::endl;
    }
}
```

### 📖 변수 읽기 (심볼릭 방식)

```cpp
// ✅ C++ - 심볼릭 변수 읽기
#include <iostream>
#include <string>
#include <TcAdsDef.h>
#include <TcAdsAPI.h>

class AdsClient {
private:
    long m_nPort;
    AmsAddr m_Addr;

public:
    AdsClient(const std::string& netId, uint16_t port) {
        // ADS 포트 열기
        m_nPort = AdsPortOpenEx();
        if (m_nPort == 0) {
            throw std::runtime_error("ADS 포트 열기 실패");
        }

        // AMS 주소 설정
        if (!AmsNetIdFromString(netId.c_str(), &m_Addr.netId)) {
            throw std::runtime_error("잘못된 NetId 형식");
        }
        m_Addr.port = port;
    }

    ~AdsClient() {
        AdsPortCloseEx(m_nPort);
    }

    // 템플릿 함수: 변수 읽기
    template<typename T>
    T ReadSymbol(const std::string& symbolName) {
        // 1단계: 심볼 핸들 얻기
        uint32_t hSymbol = 0;
        uint32_t bytesRead = 0;

        long nErr = AdsSyncReadWriteReqEx2(
            m_nPort,
            &m_Addr,
            ADSIGRP_SYM_HNDBYNAME,  // IndexGroup
            0,                       // IndexOffset
            sizeof(hSymbol),         // 읽을 크기
            &hSymbol,                // 출력 버퍼
            symbolName.size(),       // 쓸 크기
            symbolName.c_str(),      // 심볼 이름
            &bytesRead
        );

        if (nErr) {
            throw std::runtime_error("심볼 핸들 얻기 실패: 0x" +
                                     std::to_string(nErr));
        }

        // 2단계: 핸들로 값 읽기
        T value;
        nErr = AdsSyncReadReqEx2(
            m_nPort,
            &m_Addr,
            ADSIGRP_SYM_VALBYHND,   // IndexGroup
            hSymbol,                 // IndexOffset (핸들)
            sizeof(T),
            &value,
            &bytesRead
        );

        if (nErr) {
            // 핸들 해제
            AdsSyncWriteReqEx(m_nPort, &m_Addr,
                             ADSIGRP_SYM_RELEASEHND, hSymbol, 0, nullptr);
            throw std::runtime_error("값 읽기 실패: 0x" +
                                     std::to_string(nErr));
        }

        // 3단계: 핸들 해제
        AdsSyncWriteReqEx(m_nPort, &m_Addr,
                         ADSIGRP_SYM_RELEASEHND, hSymbol, 0, nullptr);

        return value;
    }

    // 템플릿 함수: 변수 쓰기
    template<typename T>
    void WriteSymbol(const std::string& symbolName, const T& value) {
        // 심볼 핸들 얻기
        uint32_t hSymbol = 0;
        uint32_t bytesRead = 0;

        long nErr = AdsSyncReadWriteReqEx2(
            m_nPort, &m_Addr,
            ADSIGRP_SYM_HNDBYNAME, 0,
            sizeof(hSymbol), &hSymbol,
            symbolName.size(), symbolName.c_str(),
            &bytesRead
        );

        if (nErr) {
            throw std::runtime_error("심볼 핸들 얻기 실패");
        }

        // 값 쓰기
        nErr = AdsSyncWriteReqEx(
            m_nPort,
            &m_Addr,
            ADSIGRP_SYM_VALBYHND,
            hSymbol,
            sizeof(T),
            &value
        );

        // 핸들 해제
        AdsSyncWriteReqEx(m_nPort, &m_Addr,
                         ADSIGRP_SYM_RELEASEHND, hSymbol, 0, nullptr);

        if (nErr) {
            throw std::runtime_error("값 쓰기 실패: 0x" +
                                     std::to_string(nErr));
        }
    }
};

// 사용 예제
int main() {
    try {
        // ADS 클라이언트 생성
        AdsClient client("192.168.1.100.1.1", 851);

        // INT 변수 읽기
        int counter = client.ReadSymbol<int>("MAIN.counter");
        std::cout << "카운터: " << counter << std::endl;

        // REAL 변수 쓰기
        float temperature = 23.5f;
        client.WriteSymbol<float>("MAIN.temperature", temperature);
        std::cout << "온도 설정: " << temperature << "°C" << std::endl;

        // BOOL 변수
        bool motorRunning = client.ReadSymbol<bool>("MAIN.bMotorRunning");
        std::cout << "모터 상태: " << (motorRunning ? "실행 중" : "정지") << std::endl;

    } catch (const std::exception& e) {
        std::cerr << "에러: " << e.what() << std::endl;
        return 1;
    }

    return 0;
}
```

### 🔔 Notification (C++)

```cpp
// ✅ C++ - ADS 알림 (콜백 방식)
#include <iostream>
#include <thread>
#include <chrono>
#include <TcAdsDef.h>
#include <TcAdsAPI.h>

// 알림 콜백 함수
void __stdcall NotificationCallback(
    const AmsAddr* pAddr,
    const AdsNotificationHeader* pNotification,
    uint32_t hUser
)
{
    // 데이터 추출
    void* pData = (void*)(pNotification + 1);  // 헤더 다음에 데이터

    // 사용자 정의 처리 (예: float 값)
    float* pValue = (float*)pData;

    std::cout << "=== 알림 수신 ===" << std::endl;
    std::cout << "타임스탬프: " << pNotification->nTimeStamp << std::endl;
    std::cout << "값: " << *pValue << std::endl;
    std::cout << "샘플 수: " << pNotification->nSamples << std::endl;
}

int main() {
    long nPort = AdsPortOpenEx();
    if (nPort == 0) {
        std::cerr << "ADS 포트 열기 실패" << std::endl;
        return 1;
    }

    // AMS 주소
    AmsAddr addr;
    AmsNetIdFromString("192.168.1.100.1.1", &addr.netId);
    addr.port = 851;

    // 심볼 핸들 얻기
    std::string symbolName = "MAIN.temperature";
    uint32_t hSymbol = 0;
    uint32_t bytesRead = 0;

    long nErr = AdsSyncReadWriteReqEx2(
        nPort, &addr,
        ADSIGRP_SYM_HNDBYNAME, 0,
        sizeof(hSymbol), &hSymbol,
        symbolName.size(), symbolName.c_str(),
        &bytesRead
    );

    if (nErr) {
        std::cerr << "심볼 핸들 실패: 0x" << std::hex << nErr << std::endl;
        AdsPortCloseEx(nPort);
        return 1;
    }

    // 알림 속성 설정
    AdsNotificationAttrib attrib = {
        sizeof(float),           // cbLength: 데이터 크기
        ADSTRANS_SERVERONCHA,    // nTransMode: 값 변경 시
        0,                       // nMaxDelay: 최대 지연 (0 = 즉시)
        0                        // nCycleTime: 사이클 (OnChange에서는 무시)
    };

    // 알림 등록
    uint32_t hNotification = 0;
    nErr = AdsSyncAddDeviceNotificationReqEx(
        nPort,
        &addr,
        ADSIGRP_SYM_VALBYHND,
        hSymbol,
        &attrib,
        NotificationCallback,
        12345,  // hUser: 사용자 정의 핸들
        &hNotification
    );

    if (nErr) {
        std::cerr << "알림 등록 실패: 0x" << std::hex << nErr << std::endl;
        // 핸들 해제
        AdsSyncWriteReqEx(nPort, &addr, ADSIGRP_SYM_RELEASEHND, hSymbol, 0, nullptr);
        AdsPortCloseEx(nPort);
        return 1;
    }

    std::cout << "알림 등록됨. 변수 변경을 기다리는 중..." << std::endl;
    std::cout << "종료하려면 Ctrl+C를 누르세요." << std::endl;

    // 60초 대기
    std::this_thread::sleep_for(std::chrono::seconds(60));

    // 알림 해제
    AdsSyncDelDeviceNotificationReqEx(nPort, &addr, hNotification);

    // 심볼 핸들 해제
    AdsSyncWriteReqEx(nPort, &addr, ADSIGRP_SYM_RELEASEHND, hSymbol, 0, nullptr);

    // 포트 닫기
    AdsPortCloseEx(nPort);

    std::cout << "종료됨." << std::endl;
    return 0;
}
```

---

## 1.4 IndexGroup/IndexOffset 완전 가이드

### 📚 표준 IndexGroup 목록

| IndexGroup (16진수) | 상수 이름 | 용도 |
|---------------------|-----------|------|
| **0xF000** | ADSIGRP_SYMTAB | 심볼 테이블 |
| **0xF001** | ADSIGRP_SYMNAME | 이름으로 심볼 정보 |
| **0xF002** | ADSIGRP_SYMVAL | 이름으로 값 액세스 |
| **0xF003** | ADSIGRP_SYM_HNDBYNAME | 이름으로 핸들 얻기 |
| **0xF004** | ADSIGRP_SYM_VALBYNAME | 이름으로 값 읽기/쓰기 |
| **0xF005** | ADSIGRP_SYM_VALBYHND | 핸들로 값 읽기/쓰기 |
| **0xF006** | ADSIGRP_SYM_RELEASEHND | 핸들 해제 |
| **0xF007** | ADSIGRP_SYM_INFOBYNAME | 이름으로 심볼 정보 |
| **0xF008** | ADSIGRP_SYM_VERSION | 심볼 버전 |
| **0xF009** | ADSIGRP_SYM_INFOBYNAMEEX | 확장 심볼 정보 |
| **0xF00A** | ADSIGRP_SYM_DOWNLOAD | 심볼 다운로드 |
| **0xF00B** | ADSIGRP_SYM_UPLOAD | 심볼 업로드 |
| **0xF00C** | ADSIGRP_SYM_UPLOADINFO | 업로드 정보 |
| **0xF080** | ADSIGRP_SYMNOTE | 심볼 알림 |
| **0xF020** | ADSIGRP_IOIMAGE_RWIB | I/O 이미지 입력 읽기 |
| **0xF021** | ADSIGRP_IOIMAGE_RWOB | I/O 이미지 출력 쓰기 |
| **0xF030** | ADSIGRP_MULTIPLE_READ | 다중 읽기 (Sum Command) |
| **0xF031** | ADSIGRP_MULTIPLE_WRITE | 다중 쓰기 (Sum Command) |

### 🔍 심볼 정보 조회

```cpp
// ✅ C++ - 심볼 정보 상세 조회
#include <iostream>
#include <string>
#include <TcAdsDef.h>
#include <TcAdsAPI.h>

#pragma pack(push, 1)
struct AdsSymbolEntry {
    uint32_t entryLength;      // 전체 엔트리 길이
    uint32_t iGroup;           // IndexGroup
    uint32_t iOffs;            // IndexOffset
    uint32_t size;             // 데이터 크기
    uint32_t dataType;         // 데이터 타입
    uint32_t flags;            // 플래그
    uint16_t nameLength;       // 이름 길이
    uint16_t typeLength;       // 타입 이름 길이
    uint16_t commentLength;    // 주석 길이
    // char name[nameLength + 1]
    // char type[typeLength + 1]
    // char comment[commentLength + 1]
};
#pragma pack(pop)

void PrintSymbolInfo(const std::string& netId, uint16_t port,
                     const std::string& symbolName) {
    long nPort = AdsPortOpenEx();
    AmsAddr addr;
    AmsNetIdFromString(netId.c_str(), &addr.netId);
    addr.port = port;

    // 심볼 정보를 위한 버퍼
    uint8_t buffer[1024];
    uint32_t bytesRead = 0;

    long nErr = AdsSyncReadWriteReqEx2(
        nPort, &addr,
        ADSIGRP_SYM_INFOBYNAME,
        0,
        sizeof(buffer),
        buffer,
        symbolName.size(),
        symbolName.c_str(),
        &bytesRead
    );

    if (nErr == 0) {
        AdsSymbolEntry* pEntry = (AdsSymbolEntry*)buffer;

        // 이름, 타입, 주석 추출
        char* pName = (char*)(buffer + sizeof(AdsSymbolEntry));
        char* pType = pName + pEntry->nameLength + 1;
        char* pComment = pType + pEntry->typeLength + 1;

        std::cout << "=== 심볼 정보 ===" << std::endl;
        std::cout << "이름: " << pName << std::endl;
        std::cout << "타입: " << pType << std::endl;
        std::cout << "주석: " << pComment << std::endl;
        std::cout << "크기: " << pEntry->size << " 바이트" << std::endl;
        std::cout << "IndexGroup: 0x" << std::hex << pEntry->iGroup << std::endl;
        std::cout << "IndexOffset: 0x" << std::hex << pEntry->iOffs << std::endl;
        std::cout << "데이터 타입: 0x" << std::hex << pEntry->dataType << std::endl;
        std::cout << "플래그: 0x" << std::hex << pEntry->flags << std::endl;
    } else {
        std::cerr << "심볼 정보 조회 실패: 0x" << std::hex << nErr << std::endl;
    }

    AdsPortCloseEx(nPort);
}

int main() {
    PrintSymbolInfo("192.168.1.100.1.1", 851, "MAIN.temperature");
    return 0;
}
```

### ⚡ Sum Command (다중 읽기/쓰기)

```cpp
// ✅ C++ - Sum Command로 100개 변수 한 번에 읽기
#include <iostream>
#include <vector>
#include <TcAdsDef.h>
#include <TcAdsAPI.h>

#pragma pack(push, 1)
struct AdsSumReadRequest {
    uint32_t indexGroup;
    uint32_t indexOffset;
    uint32_t length;
};

struct AdsSumReadResponse {
    uint32_t errorCode;
    uint32_t length;
    // uint8_t data[length]
};
#pragma pack(pop)

int main() {
    long nPort = AdsPortOpenEx();
    AmsAddr addr;
    AmsNetIdFromString("192.168.1.100.1.1", &addr.netId);
    addr.port = 851;

    const int NUM_VARS = 100;
    std::vector<uint32_t> handles(NUM_VARS);

    // 1단계: 모든 변수 핸들 획득 (사전 작업)
    for (int i = 0; i < NUM_VARS; ++i) {
        std::string varName = "MAIN.aSensors[" + std::to_string(i + 1) + "].fValue";
        uint32_t bytesRead = 0;

        AdsSyncReadWriteReqEx2(
            nPort, &addr,
            ADSIGRP_SYM_HNDBYNAME, 0,
            sizeof(handles[i]), &handles[i],
            varName.size(), varName.c_str(),
            &bytesRead
        );
    }

    // 2단계: Sum Command 요청 데이터 생성
    std::vector<uint8_t> requestBuffer(NUM_VARS * sizeof(AdsSumReadRequest));
    AdsSumReadRequest* pRequests = (AdsSumReadRequest*)requestBuffer.data();

    for (int i = 0; i < NUM_VARS; ++i) {
        pRequests[i].indexGroup = ADSIGRP_SYM_VALBYHND;
        pRequests[i].indexOffset = handles[i];
        pRequests[i].length = sizeof(float);  // REAL 타입
    }

    // 3단계: Sum Command 실행
    std::vector<uint8_t> responseBuffer(NUM_VARS * (sizeof(AdsSumReadResponse) + sizeof(float)));
    uint32_t bytesRead = 0;

    auto start = std::chrono::high_resolution_clock::now();

    long nErr = AdsSyncReadWriteReqEx2(
        nPort, &addr,
        ADSIGRP_MULTIPLE_READ,  // Sum Read
        NUM_VARS,               // 변수 개수
        responseBuffer.size(),
        responseBuffer.data(),
        requestBuffer.size(),
        requestBuffer.data(),
        &bytesRead
    );

    auto end = std::chrono::high_resolution_clock::now();
    auto duration = std::chrono::duration_cast<std::chrono::microseconds>(end - start);

    if (nErr == 0) {
        std::cout << "Sum Command 성공! (소요 시간: " << duration.count() << " μs)" << std::endl;

        // 결과 파싱
        uint8_t* pResponse = responseBuffer.data();
        for (int i = 0; i < NUM_VARS; ++i) {
            AdsSumReadResponse* pResp = (AdsSumReadResponse*)pResponse;

            if (pResp->errorCode == 0) {
                float value = *(float*)(pResponse + sizeof(AdsSumReadResponse));
                std::cout << "센서[" << i + 1 << "]: " << value << std::endl;
            } else {
                std::cerr << "센서[" << i + 1 << "] 읽기 실패: 0x"
                         << std::hex << pResp->errorCode << std::endl;
            }

            pResponse += sizeof(AdsSumReadResponse) + pResp->length;
        }
    } else {
        std::cerr << "Sum Command 실패: 0x" << std::hex << nErr << std::endl;
    }

    // 4단계: 핸들 해제
    for (int i = 0; i < NUM_VARS; ++i) {
        AdsSyncWriteReqEx(nPort, &addr,
                         ADSIGRP_SYM_RELEASEHND, handles[i], 0, nullptr);
    }

    AdsPortCloseEx(nPort);

    std::cout << std::endl;
    std::cout << "성능 비교:" << std::endl;
    std::cout << "- Sum Command (100개): " << duration.count() << " μs" << std::endl;
    std::cout << "- 개별 읽기 예상 (100개 × 500μs): ~50,000 μs" << std::endl;
    std::cout << "- 성능 향상: " << (50000.0 / duration.count()) << "배" << std::endl;

    return 0;
}
```

---

## 1.5 고급 패턴: 비동기 & 멀티스레딩

### 🚀 비동기 ADS (C++)

```cpp
// ✅ C++ - 비동기 ADS 읽기/쓰기
#include <iostream>
#include <future>
#include <functional>
#include <TcAdsDef.h>
#include <TcAdsAPI.h>

class AsyncAdsClient {
private:
    long m_nPort;
    AmsAddr m_Addr;

public:
    AsyncAdsClient(const std::string& netId, uint16_t port) {
        m_nPort = AdsPortOpenEx();
        AmsNetIdFromString(netId.c_str(), &m_Addr.netId);
        m_Addr.port = port;
    }

    ~AsyncAdsClient() {
        AdsPortCloseEx(m_nPort);
    }

    // 비동기 읽기
    template<typename T>
    std::future<T> ReadSymbolAsync(const std::string& symbolName) {
        return std::async(std::launch::async, [this, symbolName]() {
            // 심볼 핸들 얻기
            uint32_t hSymbol = 0;
            uint32_t bytesRead = 0;

            long nErr = AdsSyncReadWriteReqEx2(
                m_nPort, &m_Addr,
                ADSIGRP_SYM_HNDBYNAME, 0,
                sizeof(hSymbol), &hSymbol,
                symbolName.size(), symbolName.c_str(),
                &bytesRead
            );

            if (nErr) {
                throw std::runtime_error("핸들 얻기 실패: 0x" + std::to_string(nErr));
            }

            // 값 읽기
            T value;
            nErr = AdsSyncReadReqEx2(
                m_nPort, &m_Addr,
                ADSIGRP_SYM_VALBYHND, hSymbol,
                sizeof(T), &value, &bytesRead
            );

            // 핸들 해제
            AdsSyncWriteReqEx(m_nPort, &m_Addr,
                             ADSIGRP_SYM_RELEASEHND, hSymbol, 0, nullptr);

            if (nErr) {
                throw std::runtime_error("값 읽기 실패: 0x" + std::to_string(nErr));
            }

            return value;
        });
    }

    // 비동기 쓰기
    template<typename T>
    std::future<void> WriteSymbolAsync(const std::string& symbolName, const T& value) {
        return std::async(std::launch::async, [this, symbolName, value]() {
            uint32_t hSymbol = 0;
            uint32_t bytesRead = 0;

            long nErr = AdsSyncReadWriteReqEx2(
                m_nPort, &m_Addr,
                ADSIGRP_SYM_HNDBYNAME, 0,
                sizeof(hSymbol), &hSymbol,
                symbolName.size(), symbolName.c_str(),
                &bytesRead
            );

            if (nErr) {
                throw std::runtime_error("핸들 얻기 실패");
            }

            nErr = AdsSyncWriteReqEx(
                m_nPort, &m_Addr,
                ADSIGRP_SYM_VALBYHND, hSymbol,
                sizeof(T), &value
            );

            AdsSyncWriteReqEx(m_nPort, &m_Addr,
                             ADSIGRP_SYM_RELEASEHND, hSymbol, 0, nullptr);

            if (nErr) {
                throw std::runtime_error("값 쓰기 실패");
            }
        });
    }
};

// 사용 예제
int main() {
    AsyncAdsClient client("192.168.1.100.1.1", 851);

    // 여러 변수를 병렬로 읽기
    auto future1 = client.ReadSymbolAsync<int>("MAIN.counter");
    auto future2 = client.ReadSymbolAsync<float>("MAIN.temperature");
    auto future3 = client.ReadSymbolAsync<bool>("MAIN.bRunning");

    // 결과 대기
    try {
        int counter = future1.get();
        float temperature = future2.get();
        bool running = future3.get();

        std::cout << "카운터: " << counter << std::endl;
        std::cout << "온도: " << temperature << "°C" << std::endl;
        std::cout << "실행 중: " << (running ? "예" : "아니오") << std::endl;

    } catch (const std::exception& e) {
        std::cerr << "에러: " << e.what() << std::endl;
    }

    // 비동기 쓰기
    auto writeFuture = client.WriteSymbolAsync<float>("MAIN.setpoint", 25.5f);
    writeFuture.wait();  // 완료 대기

    std::cout << "설정값 쓰기 완료" << std::endl;

    return 0;
}
```

### 🔄 멀티스레드 ADS 통신

```cpp
// ✅ C++ - 멀티스레드로 여러 PLC 동시 통신
#include <iostream>
#include <thread>
#include <vector>
#include <mutex>
#include <TcAdsDef.h>
#include <TcAdsAPI.h>

std::mutex g_cout_mutex;  // 출력 동기화

// 각 PLC와 통신하는 워커 스레드
void PlcWorker(const std::string& netId, uint16_t port, int workerId) {
    long nPort = AdsPortOpenEx();
    if (nPort == 0) {
        std::lock_guard<std::mutex> lock(g_cout_mutex);
        std::cerr << "워커 " << workerId << ": 포트 열기 실패" << std::endl;
        return;
    }

    AmsAddr addr;
    AmsNetIdFromString(netId.c_str(), &addr.netId);
    addr.port = port;

    // 주기적으로 데이터 읽기 (10초 동안 1초마다)
    for (int i = 0; i < 10; ++i) {
        // 심볼 읽기
        std::string varName = "MAIN.temperature";
        uint32_t hSymbol = 0;
        uint32_t bytesRead = 0;

        long nErr = AdsSyncReadWriteReqEx2(
            nPort, &addr,
            ADSIGRP_SYM_HNDBYNAME, 0,
            sizeof(hSymbol), &hSymbol,
            varName.size(), varName.c_str(),
            &bytesRead
        );

        if (nErr == 0) {
            float temperature = 0.0f;
            nErr = AdsSyncReadReqEx2(
                nPort, &addr,
                ADSIGRP_SYM_VALBYHND, hSymbol,
                sizeof(temperature), &temperature, &bytesRead
            );

            if (nErr == 0) {
                std::lock_guard<std::mutex> lock(g_cout_mutex);
                std::cout << "워커 " << workerId << " [" << netId << "]: "
                         << "온도 = " << temperature << "°C" << std::endl;
            }

            // 핸들 해제
            AdsSyncWriteReqEx(nPort, &addr,
                             ADSIGRP_SYM_RELEASEHND, hSymbol, 0, nullptr);
        }

        std::this_thread::sleep_for(std::chrono::seconds(1));
    }

    AdsPortCloseEx(nPort);

    {
        std::lock_guard<std::mutex> lock(g_cout_mutex);
        std::cout << "워커 " << workerId << " 종료" << std::endl;
    }
}

int main() {
    // 3개의 PLC와 동시 통신
    std::vector<std::thread> workers;

    workers.emplace_back(PlcWorker, "192.168.1.100.1.1", 851, 1);
    workers.emplace_back(PlcWorker, "192.168.1.101.1.1", 851, 2);
    workers.emplace_back(PlcWorker, "192.168.1.102.1.1", 851, 3);

    std::cout << "3개 PLC와 동시 통신 시작..." << std::endl;

    // 모든 워커 완료 대기
    for (auto& worker : workers) {
        worker.join();
    }

    std::cout << "모든 통신 완료" << std::endl;
    return 0;
}
```

---

## 1.6 성능 최적화

### ⚡ 핸들 재사용 패턴

```cpp
// ✅ C++ - 핸들 캐싱으로 성능 향상
#include <iostream>
#include <unordered_map>
#include <string>
#include <TcAdsDef.h>
#include <TcAdsAPI.h>

class OptimizedAdsClient {
private:
    long m_nPort;
    AmsAddr m_Addr;
    std::unordered_map<std::string, uint32_t> m_HandleCache;  // 핸들 캐시

public:
    OptimizedAdsClient(const std::string& netId, uint16_t port) {
        m_nPort = AdsPortOpenEx();
        AmsNetIdFromString(netId.c_str(), &m_Addr.netId);
        m_Addr.port = port;
    }

    ~OptimizedAdsClient() {
        // 모든 핸들 해제
        for (auto& pair : m_HandleCache) {
            AdsSyncWriteReqEx(m_nPort, &m_Addr,
                             ADSIGRP_SYM_RELEASEHND, pair.second, 0, nullptr);
        }
        AdsPortCloseEx(m_nPort);
    }

    // 핸들 획득 (캐시 사용)
    uint32_t GetHandle(const std::string& symbolName) {
        // 캐시 확인
        auto it = m_HandleCache.find(symbolName);
        if (it != m_HandleCache.end()) {
            return it->second;  // 캐시된 핸들 반환
        }

        // 새 핸들 획득
        uint32_t hSymbol = 0;
        uint32_t bytesRead = 0;

        long nErr = AdsSyncReadWriteReqEx2(
            m_nPort, &m_Addr,
            ADSIGRP_SYM_HNDBYNAME, 0,
            sizeof(hSymbol), &hSymbol,
            symbolName.size(), symbolName.c_str(),
            &bytesRead
        );

        if (nErr) {
            throw std::runtime_error("핸들 얻기 실패: 0x" + std::to_string(nErr));
        }

        // 캐시에 저장
        m_HandleCache[symbolName] = hSymbol;
        return hSymbol;
    }

    // 빠른 읽기 (핸들 재사용)
    template<typename T>
    T ReadSymbolFast(const std::string& symbolName) {
        uint32_t hSymbol = GetHandle(symbolName);

        T value;
        uint32_t bytesRead = 0;

        long nErr = AdsSyncReadReqEx2(
            m_nPort, &m_Addr,
            ADSIGRP_SYM_VALBYHND, hSymbol,
            sizeof(T), &value, &bytesRead
        );

        if (nErr) {
            throw std::runtime_error("값 읽기 실패: 0x" + std::to_string(nErr));
        }

        return value;
    }

    // 빠른 쓰기 (핸들 재사용)
    template<typename T>
    void WriteSymbolFast(const std::string& symbolName, const T& value) {
        uint32_t hSymbol = GetHandle(symbolName);

        long nErr = AdsSyncWriteReqEx(
            m_nPort, &m_Addr,
            ADSIGRP_SYM_VALBYHND, hSymbol,
            sizeof(T), &value
        );

        if (nErr) {
            throw std::runtime_error("값 쓰기 실패: 0x" + std::to_string(nErr));
        }
    }
};

// 성능 비교
int main() {
    OptimizedAdsClient client("192.168.1.100.1.1", 851);

    const int ITERATIONS = 1000;

    auto start = std::chrono::high_resolution_clock::now();

    // 핸들 재사용으로 1000번 읽기
    for (int i = 0; i < ITERATIONS; ++i) {
        float temp = client.ReadSymbolFast<float>("MAIN.temperature");
        (void)temp;  // 경고 방지
    }

    auto end = std::chrono::high_resolution_clock::now();
    auto duration = std::chrono::duration_cast<std::chrono::milliseconds>(end - start);

    std::cout << "1000번 읽기 소요 시간: " << duration.count() << " ms" << std::endl;
    std::cout << "평균 읽기 시간: " << (duration.count() / 1000.0) << " ms/read" << std::endl;

    // 예상 성능:
    // - 핸들 재사용: ~0.5 ms/read
    // - 매번 핸들 생성/해제: ~1.5 ms/read (3배 느림)

    return 0;
}
```

### 📊 I/O 이미지 직접 액세스

```iecst
// ✅ ST - I/O 이미지로 빠른 액세스
PROGRAM FastIOAccess
VAR
    // 전통적인 방법 (느림)
    bInputTraditional AT %IX0.0 : BOOL;
    bOutputTraditional AT %QX0.0 : BOOL;

    // I/O 이미지 포인터 (빠름)
    pInputImage : POINTER TO BYTE;
    pOutputImage : POINTER TO BYTE;

    // 직접 비트 조작
    nInputByte : BYTE;
    nOutputByte : BYTE;
END_VAR

// 초기화: I/O 이미지 주소 얻기
pInputImage := ADR(bInputTraditional);  // 입력 이미지 시작 주소
pOutputImage := ADR(bOutputTraditional);  // 출력 이미지 시작 주소

// 빠른 읽기 (전체 바이트 한 번에)
nInputByte := pInputImage^;

// 특정 비트 확인
IF (nInputByte AND 16#01) <> 0 THEN
    // 비트 0이 SET됨
END_IF

IF (nInputByte AND 16#02) <> 0 THEN
    // 비트 1이 SET됨
END_IF

// 빠른 쓰기 (전체 바이트 한 번에)
nOutputByte := 16#FF;  // 모든 비트 SET
pOutputImage^ := nOutputByte;

// 또는 특정 비트만 설정
nOutputByte := nOutputByte OR 16#04;  // 비트 2 SET
nOutputByte := nOutputByte AND 16#FB;  // 비트 2 CLEAR
pOutputImage^ := nOutputByte;
```

---

## 1.7 에러 처리 및 디버깅

### 🐛 ADS 에러 코드 상세

```cpp
// ✅ C++ - ADS 에러 코드 해석 함수
#include <iostream>
#include <string>
#include <TcAdsDef.h>

std::string GetAdsErrorString(long errorCode) {
    switch (errorCode) {
        // 일반 에러
        case 0x0000: return "ERR_NOERROR: 성공";
        case 0x0001: return "ERR_INTERNAL: 내부 에러";
        case 0x0002: return "ERR_NORTIME: 실시간 시스템 없음";
        case 0x0003: return "ERR_ALLOCLOCKEDMEM: 메모리 할당 실패";
        case 0x0004: return "ERR_INSERTMAILBOX: Mailbox 삽입 실패";
        case 0x0005: return "ERR_WRONGRECEIVEHMSG: 잘못된 HMSG 수신";
        case 0x0006: return "ERR_TARGETPORTNOTFOUND: 대상 포트를 찾을 수 없음";
        case 0x0007: return "ERR_TARGETMACHINENOTFOUND: 대상 머신을 찾을 수 없음";

        // 라우터 에러
        case 0x0700: return "ROUTERERR_NOLOCKEDMEMORY: 라우터 메모리 부족";
        case 0x0701: return "ROUTERERR_RESYNCNOTALLOWED: 재동기화 불가";
        case 0x0702: return "ROUTERERR_NOMOREQUEUES: 큐 부족";
        case 0x0703: return "ROUTERERR_SYNCQUEUEFULL: 동기화 큐 가득 찼음";
        case 0x0704: return "ROUTERERR_ASYNCQUEUEFULL: 비동기 큐 가득 찼음";
        case 0x0705: return "ROUTERERR_ADDRNOTPRESENT: 주소가 없음";
        case 0x0706: return "ROUTERERR_NOTINITIALIZED: 라우터 초기화 안 됨";
        case 0x0707: return "ROUTERERR_NOMOREMEMORY: 라우터 메모리 부족";

        // ADS 에러
        case 0x0710: return "ERR_NOERROR: 성공";
        case 0x0711: return "ERR_INVALIDPARAMETER: 잘못된 파라미터";
        case 0x0712: return "ERR_NOTIMPL: 구현되지 않음";
        case 0x0713: return "ERR_OUTOFRANGE: 범위 초과";
        case 0x0714: return "ERR_INVALIDSIZE: 잘못된 크기";
        case 0x0715: return "ERR_DEVICEINVALIDOFFSET: 잘못된 오프셋";
        case 0x0716: return "ERR_DEVICEINVALIDACCESS: 잘못된 액세스";
        case 0x0717: return "ERR_DEVICEINVALIDCONTEXT: 잘못된 컨텍스트";
        case 0x0718: return "ERR_DEVICENOTSUPPORTED: 지원하지 않는 장치";
        case 0x0719: return "ERR_DEVICEINVALIDDATA: 잘못된 데이터";

        // PLC 런타임 에러
        case 0x1000: return "RTERR_INTERNAL: PLC 내부 에러";
        case 0x1001: return "RTERR_BADTIMERPERIODS: 잘못된 타이머 주기";
        case 0x1002: return "RTERR_INVALIDTASKPTR: 잘못된 태스크 포인터";
        case 0x1003: return "RTERR_INVALIDSTACKPTR: 잘못된 스택 포인터";
        case 0x1004: return "RTERR_PRIOEXISTS: 우선순위 이미 존재";
        case 0x1005: return "RTERR_NOMORETCB: TCB 부족";
        case 0x1006: return "RTERR_NOMORESEMAS: 세마포어 부족";
        case 0x1007: return "RTERR_NOMOREQUEUES: 큐 부족";

        // 심볼 에러
        case 0x1861: return "ERR_SYMBOLNOTFOUND: 심볼을 찾을 수 없음";
        case 0x1862: return "ERR_SYMBOLVERSIONINVALID: 심볼 버전 불일치";
        case 0x1863: return "ERR_INVALIDSTATE: 잘못된 상태";

        default:
            return "UNKNOWN_ERROR: 알 수 없는 에러 (0x" +
                   std::to_string(errorCode) + ")";
    }
}

// 에러 핸들러
void HandleAdsError(long errorCode, const std::string& operation) {
    if (errorCode != 0) {
        std::cerr << "=== ADS 에러 ===" << std::endl;
        std::cerr << "작업: " << operation << std::endl;
        std::cerr << "에러 코드: 0x" << std::hex << errorCode << std::endl;
        std::cerr << "설명: " << GetAdsErrorString(errorCode) << std::endl;

        // 에러별 권장 조치
        if (errorCode == 0x0006 || errorCode == 0x0706) {
            std::cerr << "권장 조치: PLC가 실행 중인지 확인하세요." << std::endl;
        } else if (errorCode == 0x0007 || errorCode == 0x0705) {
            std::cerr << "권장 조치: AmsNetId와 라우터 설정을 확인하세요." << std::endl;
        } else if (errorCode == 0x1861) {
            std::cerr << "권장 조치: 변수 이름을 확인하세요." << std::endl;
        } else if (errorCode == 0x1862) {
            std::cerr << "권장 조치: PLC 프로그램을 다시 빌드하세요." << std::endl;
        }
    }
}
```

### 🔍 디버깅 도구: TcAdsLogger

```iecst
// ✅ ST - 커스텀 ADS 로거
FUNCTION_BLOCK FB_AdsLogger
VAR_INPUT
    bEnable : BOOL;           // 로깅 활성화
    sOperation : STRING(50);  // 작업 설명
    nErrorCode : UDINT;       // 에러 코드
END_VAR

VAR
    fbFileOpen : FB_FileOpen;
    fbFileWrite : FB_FileWrite;
    fbFileClose : FB_FileClose;

    sLogFile : STRING := 'C:\TwinCAT\Logs\AdsLog.txt';
    hFile : UINT;
    sLogEntry : STRING(512);
    dtTimestamp : DT;
END_VAR

IF bEnable THEN
    // 타임스탬프
    dtTimestamp := NT_GetTime();

    // 로그 엔트리 생성
    sLogEntry := DT_TO_STRING(dtTimestamp);
    sLogEntry := CONCAT(sLogEntry, ' | ');
    sLogEntry := CONCAT(sLogEntry, sOperation);
    sLogEntry := CONCAT(sLogEntry, ' | 에러: 0x');
    sLogEntry := CONCAT(sLogEntry, UDINT_TO_HEXSTR(nErrorCode, 8, FALSE));
    sLogEntry := CONCAT(sLogEntry, '$N');  // 줄바꿈

    // 파일 열기 (추가 모드)
    fbFileOpen(
        sPathName := sLogFile,
        nMode := FOPEN_MODEWRITE OR FOPEN_MODEAPPEND OR FOPEN_MODETEXT,
        ePath := PATH_GENERIC,
        bExecute := TRUE
    );

    IF fbFileOpen.bError THEN
        bEnable := FALSE;
        fbFileOpen(bExecute := FALSE);
    ELSIF NOT fbFileOpen.bBusy AND fbFileOpen.hFile <> 0 THEN
        hFile := fbFileOpen.hFile;

        // 로그 쓰기
        fbFileWrite(
            hFile := hFile,
            pWriteBuff := ADR(sLogEntry),
            cbWriteLen := LEN(sLogEntry),
            bExecute := TRUE
        );

        IF NOT fbFileWrite.bBusy THEN
            // 파일 닫기
            fbFileClose(
                hFile := hFile,
                bExecute := TRUE
            );

            IF NOT fbFileClose.bBusy THEN
                bEnable := FALSE;
                fbFileOpen(bExecute := FALSE);
                fbFileWrite(bExecute := FALSE);
                fbFileClose(bExecute := FALSE);
            END_IF
        END_IF
    END_IF
END_IF
```

---

# Part 2: PLC 프로그래밍 마스터

## 2.1 ST 고급 문법

### 🔄 고급 루프 및 제어 구조

```iecst
// ✅ ST - CONTINUE, EXIT, RETURN
FUNCTION_BLOCK FB_AdvancedLoops
VAR
    aData : ARRAY[1..100] OF INT;
    nSum : INT := 0;
    nValidCount : INT := 0;
    bFoundTarget : BOOL := FALSE;
END_VAR

VAR_INPUT
    nTargetValue : INT := 50;
END_VAR

// CONTINUE 예제: 조건에 맞지 않으면 건너뛰기
METHOD ProcessWithContinue
FOR i := 1 TO 100 DO
    IF aData[i] < 0 THEN
        CONTINUE;  // 음수는 건너뛰기
    END_IF;

    nSum := nSum + aData[i];
    nValidCount := nValidCount + 1;
END_FOR;
END_METHOD

// EXIT 예제: 목표값 찾으면 루프 종료
METHOD FindValueWithExit
FOR i := 1 TO 100 DO
    IF aData[i] = nTargetValue THEN
        bFoundTarget := TRUE;
        EXIT;  // 찾았으니 루프 종료
    END_IF;
END_FOR;
END_METHOD

// RETURN 예제: 메소드 즉시 종료
METHOD ValidateData : BOOL
VAR
    i : INT;
END_VAR

FOR i := 1 TO 100 DO
    IF aData[i] < -1000 OR aData[i] > 1000 THEN
        ValidateData := FALSE;
        RETURN;  // 유효하지 않으면 즉시 FALSE 반환
    END_IF;
END_FOR;

ValidateData := TRUE;  // 모두 유효
END_METHOD
```

### 🎯 CASE OF 고급 패턴

```iecst
// ✅ ST - CASE OF 여러 값 및 범위
PROGRAM CaseAdvanced
VAR
    nErrorCode : INT := 1005;
    sSeverity : STRING(20);
    sMessage : STRING(100);
END_VAR

// 여러 값을 한 번에 처리
CASE nErrorCode OF
    0:
        sSeverity := '정상';
        sMessage := '에러 없음';

    1..99:  // 범위 지정
        sSeverity := '경고';
        sMessage := '경미한 경고';

    100, 101, 102:  // 여러 값
        sSeverity := '에러';
        sMessage := '통신 에러';

    1000..1999:
        sSeverity := '치명적';
        sMessage := '시스템 에러';

    ELSE
        sSeverity := '알 수 없음';
        sMessage := '정의되지 않은 에러 코드';
END_CASE;

ADSLOGSTR(msgCtrlMask := ADSLOG_MSGTYPE_HINT,
         msgFmtStr := '[%s] %s (코드: %d)',
         strArg := CONCAT(sSeverity, CONCAT('|', sMessage)));
```

### 📐 비트 연산 마스터

```iecst
// ✅ ST - 비트 조작 고급 기법
FUNCTION_BLOCK FB_BitManipulation
VAR
    nFlags : DWORD := 16#00000000;
END_VAR

// 개별 플래그 정의 (비트 위치)
VAR CONSTANT
    FLAG_MOTOR_ENABLE    : DWORD := 16#00000001;  // 비트 0
    FLAG_ALARM_ACTIVE    : DWORD := 16#00000002;  // 비트 1
    FLAG_AUTO_MODE       : DWORD := 16#00000004;  // 비트 2
    FLAG_DOOR_OPEN       : DWORD := 16#00000008;  // 비트 3
    FLAG_MAINTENANCE     : DWORD := 16#00000010;  // 비트 4
    FLAG_EMERGENCY_STOP  : DWORD := 16#00000020;  // 비트 5
END_VAR

// 플래그 설정
METHOD SetFlag
VAR_INPUT
    nFlag : DWORD;
END_VAR

nFlags := nFlags OR nFlag;
END_METHOD

// 플래그 클리어
METHOD ClearFlag
VAR_INPUT
    nFlag : DWORD;
END_VAR

nFlags := nFlags AND NOT nFlag;
END_METHOD

// 플래그 토글
METHOD ToggleFlag
VAR_INPUT
    nFlag : DWORD;
END_VAR

nFlags := nFlags XOR nFlag;
END_METHOD

// 플래그 확인
METHOD IsFlagSet : BOOL
VAR_INPUT
    nFlag : DWORD;
END_VAR

IsFlagSet := (nFlags AND nFlag) <> 0;
END_METHOD

// 여러 플래그 한 번에 설정
METHOD SetMultipleFlags
VAR_INPUT
    nFlagMask : DWORD;
END_VAR

nFlags := nFlags OR nFlagMask;
END_METHOD

// 비트 카운트 (SET된 비트 개수)
METHOD CountSetBits : INT
VAR
    i : INT;
    nTemp : DWORD;
    nCount : INT := 0;
END_VAR

nTemp := nFlags;
FOR i := 0 TO 31 DO
    IF (nTemp AND 1) <> 0 THEN
        nCount := nCount + 1;
    END_IF;
    nTemp := SHR(nTemp, 1);
END_FOR;

CountSetBits := nCount;
END_METHOD
```

---

## 2.2 Function Block 설계 패턴

### 🏗️ 싱글톤 패턴 (Global FB)

```iecst
// ✅ ST - 싱글톤 Function Block
FUNCTION_BLOCK FB_SystemManager EXTENDS FB_init
VAR
    bInitialized : BOOL := FALSE;

    // 시스템 상태
    eSystemState : E_SystemState := E_SystemState.INIT;
    nErrorCount : UDINT := 0;
    dtLastError : DT;

    // 통계
    nCycleCount : UDINT := 0;
    tCycleTimeMax : TIME := T#0ms;
    tCycleTimeAvg : TIME := T#0ms;
END_VAR

TYPE E_SystemState :
(
    INIT := 0,
    IDLE := 10,
    RUNNING := 20,
    ERROR := 99
);
END_TYPE

// FB_init 메소드 (생성자)
METHOD FB_init : BOOL
VAR_INPUT
    bInitRetains : BOOL;
    bInCopyCode : BOOL;
END_VAR

IF NOT bInitRetains THEN
    // 최초 생성 시에만 실행
    bInitialized := TRUE;
    eSystemState := E_SystemState.INIT;

    ADSLOGSTR(msgCtrlMask := ADSLOG_MSGTYPE_HINT,
             msgFmtStr := 'SystemManager 초기화',
             strArg := '');
END_IF;

FB_init := TRUE;
END_METHOD

// 싱글톤 인스턴스 얻기 (Static 메소드처럼 사용)
METHOD GetInstance : POINTER TO FB_SystemManager
GetInstance := ADR(GVL.SystemManager);  // 전역 인스턴스 반환
END_METHOD
```

```iecst
// ✅ 전역 변수 리스트에서 싱글톤 선언
{attribute 'qualified_only'}
VAR_GLOBAL
    SystemManager : FB_SystemManager;  // 싱글톤 인스턴스
END_VAR
```

```iecst
// ✅ 사용 예제
PROGRAM UseSingleton
VAR
    pSysMan : POINTER TO FB_SystemManager;
END_VAR

// 어디서든 같은 인스턴스 접근
pSysMan := GVL.SystemManager.GetInstance();

// 또는 직접 접근
GVL.SystemManager.nCycleCount := GVL.SystemManager.nCycleCount + 1;
```

### 🎭 상태 머신 패턴

```iecst
// ✅ ST - 고급 상태 머신 패턴
FUNCTION_BLOCK FB_ConveyorStateMachine
VAR_INPUT
    bStart : BOOL;
    bStop : BOOL;
    bEmergencyStop : BOOL;
    bReset : BOOL;
END_VAR

VAR_OUTPUT
    bRunning : BOOL;
    bError : BOOL;
    eCurrentState : E_ConveyorState;
END_VAR

VAR
    ePreviousState : E_ConveyorState := E_ConveyorState.IDLE;
    fbTimer : TON;
    tStateTimeout : TIME := T#10s;

    // 전환 카운터 (디버깅용)
    nStateTransitions : UDINT := 0;
END_VAR

TYPE E_ConveyorState :
(
    IDLE := 0,
    STARTING := 10,
    RUNNING := 20,
    STOPPING := 30,
    ERROR := 99,
    EMERGENCY := 100
);
END_TYPE

// 메인 로직
IF bEmergencyStop THEN
    // 비상 정지는 언제나 최우선
    ChangeState(E_ConveyorState.EMERGENCY);
END_IF;

CASE eCurrentState OF
    E_ConveyorState.IDLE:
        bRunning := FALSE;
        bError := FALSE;

        IF bStart THEN
            ChangeState(E_ConveyorState.STARTING);
        END_IF;

    E_ConveyorState.STARTING:
        // 모터 기동 시퀀스
        fbTimer(IN := TRUE, PT := T#3s);

        IF fbTimer.Q THEN
            // 기동 완료
            ChangeState(E_ConveyorState.RUNNING);
            fbTimer(IN := FALSE);
        ELSIF bStop OR CheckStartupError() THEN
            // 기동 중단 또는 에러
            ChangeState(E_ConveyorState.ERROR);
            fbTimer(IN := FALSE);
        END_IF;

    E_ConveyorState.RUNNING:
        bRunning := TRUE;

        IF bStop THEN
            ChangeState(E_ConveyorState.STOPPING);
        ELSIF CheckRuntimeError() THEN
            ChangeState(E_ConveyorState.ERROR);
        END_IF;

    E_ConveyorState.STOPPING:
        // 안전 정지 시퀀스
        fbTimer(IN := TRUE, PT := T#2s);

        IF fbTimer.Q THEN
            // 정지 완료
            ChangeState(E_ConveyorState.IDLE);
            fbTimer(IN := FALSE);
        END_IF;

    E_ConveyorState.ERROR:
        bRunning := FALSE;
        bError := TRUE;

        IF bReset THEN
            // 에러 리셋
            ClearErrors();
            ChangeState(E_ConveyorState.IDLE);
        END_IF;

    E_ConveyorState.EMERGENCY:
        bRunning := FALSE;
        bError := TRUE;

        // 비상 정지 해제 후에만 리셋 가능
        IF NOT bEmergencyStop AND bReset THEN
            ChangeState(E_ConveyorState.IDLE);
        END_IF;
END_CASE;

// 상태 변경 메소드
METHOD ChangeState
VAR_INPUT
    eNewState : E_ConveyorState;
END_VAR

IF eCurrentState <> eNewState THEN
    // 상태 전환 로그
    ADSLOGSTR(
        msgCtrlMask := ADSLOG_MSGTYPE_HINT,
        msgFmtStr := '상태 전환: %d -> %d',
        strArg := CONCAT(
            TO_STRING(eCurrentState),
            CONCAT(' -> ', TO_STRING(eNewState))
        )
    );

    ePreviousState := eCurrentState;
    eCurrentState := eNewState;
    nStateTransitions := nStateTransitions + 1;

    // 상태 진입 시 초기화
    OnStateEntry(eNewState);
END_IF;
END_METHOD

// 상태 진입 시 초기화
METHOD OnStateEntry
VAR_INPUT
    eState : E_ConveyorState;
END_VAR

CASE eState OF
    E_ConveyorState.IDLE:
        // IDLE 진입 시 모든 출력 OFF
        ResetOutputs();

    E_ConveyorState.STARTING:
        // 기동 준비
        PrepareStartup();

    E_ConveyorState.RUNNING:
        // 정상 운전 시작
        EnableNormalOperation();
END_CASE;
END_METHOD
```

### 🔌 인터페이스 패턴

```iecst
// ✅ ST - 인터페이스 정의
INTERFACE I_Motor
// 모터 제어 인터페이스

METHOD Start : BOOL
// 모터 시작
END_METHOD

METHOD Stop : BOOL
// 모터 정지
END_METHOD

METHOD GetSpeed : REAL
// 현재 속도 조회 [rpm]
END_METHOD

METHOD SetSpeed : BOOL
VAR_INPUT
    fSpeed : REAL;  // 목표 속도 [rpm]
END_VAR
// 속도 설정
END_METHOD

PROPERTY IsRunning : BOOL
// 실행 중 여부
END_PROPERTY
```

```iecst
// ✅ ST - 인터페이스 구현 (AC 서보)
FUNCTION_BLOCK FB_ACServoMotor IMPLEMENTS I_Motor
VAR
    mcPower : MC_Power;
    mcMoveVelocity : MC_MoveVelocity;
    mcStop : MC_Stop;

    axis : AXIS_REF;
    fCurrentSpeed : REAL;
    bIsRunning : BOOL;
END_VAR

METHOD Start : BOOL
    mcPower(Axis := axis, Enable := TRUE);

    IF mcPower.Status THEN
        Start := TRUE;
    ELSE
        Start := FALSE;
    END_IF;
END_METHOD

METHOD Stop : BOOL
    mcStop(Axis := axis, Execute := TRUE, Deceleration := 500.0);

    IF mcStop.Done THEN
        mcStop(Execute := FALSE);
        Stop := TRUE;
    ELSE
        Stop := FALSE;
    END_IF;
END_METHOD

METHOD GetSpeed : REAL
    GetSpeed := axis.NcToPlc.ActVelo;
END_METHOD

METHOD SetSpeed : BOOL
VAR_INPUT
    fSpeed : REAL;
END_VAR

    mcMoveVelocity(
        Axis := axis,
        Execute := TRUE,
        Velocity := fSpeed,
        Acceleration := 1000.0,
        Deceleration := 1000.0
    );

    SetSpeed := NOT mcMoveVelocity.Busy;
END_METHOD

PROPERTY IsRunning : BOOL
    IsRunning := axis.NcToPlc.StateDWord.0;  // Enabled 비트
END_PROPERTY
```

```iecst
// ✅ ST - 다형성 활용
PROGRAM PolymorphismExample
VAR
    // 인터페이스 포인터
    pMotor : POINTER TO I_Motor;

    // 구체적 구현체
    acServo : FB_ACServoMotor;
    stepperMotor : FB_StepperMotor;

    bUseServo : BOOL := TRUE;
END_VAR

// 런타임에 모터 선택
IF bUseServo THEN
    pMotor := ADR(acServo);
ELSE
    pMotor := ADR(stepperMotor);
END_IF;

// 공통 인터페이스로 제어 (구현체와 무관)
IF pMotor <> 0 THEN
    pMotor^.Start();
    pMotor^.SetSpeed(1500.0);

    IF pMotor^.IsRunning THEN
        ADSLOGSTR(msgCtrlMask := ADSLOG_MSGTYPE_HINT,
                 msgFmtStr := '모터 실행 중, 속도: %.1f rpm',
                 strArg := REAL_TO_STRING(pMotor^.GetSpeed()));
    END_IF;
END_IF;
```

---

## 2.3 포인터 및 레퍼런스

### 📍 포인터 기본

```iecst
// ✅ ST - 포인터 기초
PROGRAM PointerBasics
VAR
    nValue : INT := 100;
    pValue : POINTER TO INT;  // INT형 포인터

    // 배열과 포인터
    aData : ARRAY[1..10] OF REAL;
    pData : POINTER TO REAL;

    i : INT;
END_VAR

// 포인터에 주소 할당
pValue := ADR(nValue);

// 포인터로 값 읽기
IF pValue <> 0 THEN  // NULL 체크
    ADSLOGDINT(msgCtrlMask := ADSLOG_MSGTYPE_HINT,
              msgFmtStr := '포인터를 통한 값: %d',
              dintArg := pValue^);  // 역참조
END_IF;

// 포인터로 값 쓰기
pValue^ := 200;
// nValue는 이제 200

// 배열 순회 (포인터 산술)
pData := ADR(aData[1]);  // 첫 요소 주소

FOR i := 1 TO 10 DO
    pData^ := REAL_TO_REAL(i) * 1.5;
    pData := pData + SIZEOF(REAL);  // 다음 요소로 이동
END_FOR;
```

### 🔗 레퍼런스 (Reference)

```iecst
// ✅ ST - 레퍼런스 사용
FUNCTION_BLOCK FB_DataProcessor
VAR_INPUT
    refInputData : REFERENCE TO ST_SensorData;  // 레퍼런스 입력
END_VAR

VAR_OUTPUT
    fProcessedValue : REAL;
END_VAR

// 레퍼런스 유효성 검사
IF __ISVALIDREF(refInputData) THEN
    // 레퍼런스로 데이터 읽기 (자동 역참조)
    fProcessedValue := refInputData.fTemperature * 1.8 + 32.0;  // °C -> °F

    // 레퍼런스로 데이터 쓰기
    refInputData.bProcessed := TRUE;
ELSE
    ADSLOGSTR(msgCtrlMask := ADSLOG_MSGTYPE_ERROR,
             msgFmtStr := '잘못된 레퍼런스!',
             strArg := '');
END_IF;
```

```iecst
// ✅ 사용 예제
PROGRAM UseReference
VAR
    sensor1 : ST_SensorData;
    processor : FB_DataProcessor;
END_VAR

// 레퍼런스 전달 (포인터처럼 동작하지만 문법이 간결)
processor(refInputData := sensor1);

// sensor1.bProcessed가 TRUE로 설정됨
```

### 🧮 포인터를 활용한 동적 데이터 처리

```iecst
// ✅ ST - 가변 길이 데이터 처리
FUNCTION_BLOCK FB_DynamicArray
VAR_INPUT
    pData : POINTER TO BYTE;   // 데이터 시작 주소
    nDataSize : UDINT;         // 데이터 크기 [바이트]
END_VAR

VAR
    nSum : UDINT := 0;
    i : UDINT;
    pCurrent : POINTER TO BYTE;
END_VAR

// 모든 바이트 합계 계산
METHOD CalculateChecksum : UDINT
VAR
    i : UDINT;
    pTemp : POINTER TO BYTE;
END_VAR

nSum := 0;
pTemp := pData;

IF pTemp <> 0 THEN
    FOR i := 0 TO (nDataSize - 1) DO
        nSum := nSum + pTemp^;
        pTemp := pTemp + 1;  // 다음 바이트
    END_FOR;
END_IF;

CalculateChecksum := nSum;
END_METHOD

// 데이터 복사
METHOD CopyData : BOOL
VAR_INPUT
    pDest : POINTER TO BYTE;
    nDestSize : UDINT;
END_VAR
VAR
    i : UDINT;
    pSrc : POINTER TO BYTE;
    pDst : POINTER TO BYTE;
END_VAR

IF pData = 0 OR pDest = 0 OR nDestSize < nDataSize THEN
    CopyData := FALSE;
    RETURN;
END_IF;

pSrc := pData;
pDst := pDest;

FOR i := 0 TO (nDataSize - 1) DO
    pDst^ := pSrc^;
    pSrc := pSrc + 1;
    pDst := pDst + 1;
END_FOR;

CopyData := TRUE;
END_METHOD
```

---

## 2.4 TcCOM - C++ 모듈 개발

### 🔧 TcCOM 개요

**TcCOM (TwinCAT Component Object Model)**은 C++로 고성능 실시간 모듈을 개발하는 프레임워크입니다.

```
TcCOM 아키텍처
│
├── TwinCAT Runtime
│   └── 실시간 스케줄러
│
├── TcCOM Module (C++)
│   ├── ITComObject 인터페이스
│   ├── CycleUpdate() - 사이클릭 실행
│   └── Parameters & DataAreas
│
└── PLC Program (ST)
    └── TcCOM 모듈 호출
```

### 🏗️ TcCOM 모듈 생성

```cpp
// ✅ C++ - TcCOM 모듈 헤더 (MyModule.h)
#pragma once
#include <TcPch.h>
#include <TcBase.h>
#include <TcModule.h>

// 모듈 클래스 정의
class CMyModule : public ITcModule
{
public:
    // 생성자/소멸자
    CMyModule();
    virtual ~CMyModule();

    // ITcModule 인터페이스 구현
    virtual HRESULT TCOMAPI SetObjState(TMC_OBJSTATE nNewState, PTcInitDataHdr pData);
    virtual HRESULT TCOMAPI CycleUpdate(ITcTask* ipTask, ITcUnknown* ipCaller, ULONG_PTR context);

private:
    // 파라미터 (TMC 파일에서 정의)
    struct Parameters {
        REAL fSampleRate;        // 샘플링 레이트 [Hz]
        INT nFilterOrder;        // 필터 차수
        BOOL bEnableLogging;     // 로깅 활성화
    } m_Parameters;

    // 입력 데이터
    struct Inputs {
        REAL fInputSignal;       // 입력 신호
        BOOL bReset;             // 리셋 신호
    } m_Inputs;

    // 출력 데이터
    struct Outputs {
        REAL fFilteredSignal;    // 필터링된 신호
        BOOL bReady;             // 준비 상태
        UDINT nCycleCount;       // 사이클 카운터
    } m_Outputs;

    // 내부 상태
    TMC_OBJSTATE m_nObjState;
    std::vector<REAL> m_FilterBuffer;  // 필터 버퍼

    // 내부 메소드
    void InitializeFilter();
    REAL ApplyFilter(REAL fInput);
};
```

```cpp
// ✅ C++ - TcCOM 모듈 구현 (MyModule.cpp)
#include "MyModule.h"

CMyModule::CMyModule()
    : m_nObjState(TMC_OBJ_NONE)
{
    // 생성자: 초기화
    memset(&m_Parameters, 0, sizeof(m_Parameters));
    memset(&m_Inputs, 0, sizeof(m_Inputs));
    memset(&m_Outputs, 0, sizeof(m_Outputs));
}

CMyModule::~CMyModule()
{
    // 소멸자: 리소스 해제
    m_FilterBuffer.clear();
}

// 상태 변경 핸들러
HRESULT CMyModule::SetObjState(TMC_OBJSTATE nNewState, PTcInitDataHdr pData)
{
    switch (nNewState) {
        case TMC_OBJ_PREOP:
            // Pre-Operational: 초기화 단계
            TcTrace(tlInfo, "CMyModule: PREOP 상태");

            // 파라미터 읽기 (TMC에서 매핑됨)
            if (m_Parameters.nFilterOrder < 1 || m_Parameters.nFilterOrder > 10) {
                m_Parameters.nFilterOrder = 5;  // 기본값
            }

            // 필터 초기화
            InitializeFilter();
            break;

        case TMC_OBJ_SAFEOP:
            // Safe-Operational: 준비 완료
            TcTrace(tlInfo, "CMyModule: SAFEOP 상태");
            m_Outputs.bReady = TRUE;
            break;

        case TMC_OBJ_OP:
            // Operational: 정상 동작
            TcTrace(tlInfo, "CMyModule: OP 상태 (실행 중)");
            break;

        case TMC_OBJ_STOP:
            // Stop: 정지
            TcTrace(tlInfo, "CMyModule: STOP 상태");
            m_Outputs.bReady = FALSE;
            break;

        default:
            break;
    }

    m_nObjState = nNewState;
    return S_OK;
}

// 사이클릭 업데이트 (실시간 실행)
HRESULT CMyModule::CycleUpdate(ITcTask* ipTask, ITcUnknown* ipCaller, ULONG_PTR context)
{
    if (m_nObjState != TMC_OBJ_OP) {
        return S_OK;  // OP 상태가 아니면 아무것도 안 함
    }

    // 리셋 처리
    if (m_Inputs.bReset) {
        InitializeFilter();
        m_Outputs.nCycleCount = 0;
    }

    // 필터 적용
    m_Outputs.fFilteredSignal = ApplyFilter(m_Inputs.fInputSignal);

    // 사이클 카운터 증가
    m_Outputs.nCycleCount++;

    // 선택적 로깅 (주의: 실시간 성능에 영향)
    if (m_Parameters.bEnableLogging && (m_Outputs.nCycleCount % 1000 == 0)) {
        TcTrace(tlVerbose, "CMyModule: 사이클 %u, 출력: %.3f",
                m_Outputs.nCycleCount, m_Outputs.fFilteredSignal);
    }

    return S_OK;
}

// 필터 초기화
void CMyModule::InitializeFilter()
{
    m_FilterBuffer.clear();
    m_FilterBuffer.resize(m_Parameters.nFilterOrder, 0.0f);
}

// 이동 평균 필터 적용
REAL CMyModule::ApplyFilter(REAL fInput)
{
    // 버퍼에 새 값 추가
    m_FilterBuffer.erase(m_FilterBuffer.begin());
    m_FilterBuffer.push_back(fInput);

    // 평균 계산
    REAL fSum = 0.0f;
    for (size_t i = 0; i < m_FilterBuffer.size(); ++i) {
        fSum += m_FilterBuffer[i];
    }

    return fSum / static_cast<REAL>(m_FilterBuffer.size());
}
```

### 📋 TMC 파일 (모듈 설명)

```xml
<!-- ✅ MyModule.tmc - TcCOM 모듈 메타데이터 -->
<?xml version="1.0" encoding="UTF-8"?>
<TcModuleClass>
  <Name>MyModule</Name>
  <CLSID>{12345678-1234-1234-1234-123456789ABC}</CLSID>
  <Version>1.0.0</Version>
  <Description>고성능 신호 필터링 모듈</Description>

  <!-- 파라미터 -->
  <Parameters>
    <Parameter>
      <Name>SampleRate</Name>
      <Type>REAL</Type>
      <Default>1000.0</Default>
      <Description>샘플링 레이트 [Hz]</Description>
    </Parameter>
    <Parameter>
      <Name>FilterOrder</Name>
      <Type>INT</Type>
      <Default>5</Default>
      <Description>필터 차수 (1~10)</Description>
    </Parameter>
    <Parameter>
      <Name>EnableLogging</Name>
      <Type>BOOL</Type>
      <Default>FALSE</Default>
      <Description>로깅 활성화</Description>
    </Parameter>
  </Parameters>

  <!-- 입력 -->
  <DataAreas>
    <DataArea>
      <Name>Inputs</Name>
      <Type>Input</Type>
      <Symbol>
        <Name>InputSignal</Name>
        <Type>REAL</Type>
        <Comment>입력 신호</Comment>
      </Symbol>
      <Symbol>
        <Name>Reset</Name>
        <Type>BOOL</Type>
        <Comment>리셋</Comment>
      </Symbol>
    </DataArea>

    <!-- 출력 -->
    <DataArea>
      <Name>Outputs</Name>
      <Type>Output</Type>
      <Symbol>
        <Name>FilteredSignal</Name>
        <Type>REAL</Type>
        <Comment>필터링된 신호</Comment>
      </Symbol>
      <Symbol>
        <Name>Ready</Name>
        <Type>BOOL</Type>
        <Comment>준비 상태</Comment>
      </Symbol>
      <Symbol>
        <Name>CycleCount</Name>
        <Type>UDINT</Type>
        <Comment>사이클 카운터</Comment>
      </Symbol>
    </DataArea>
  </DataAreas>
</TcModuleClass>
```

### 🔗 PLC에서 TcCOM 모듈 사용

```iecst
// ✅ ST - TcCOM 모듈 호출
PROGRAM UseTcCOMModule
VAR
    // TcCOM 모듈 인스턴스 (System Manager에서 추가)
    MyFilterModule : FB_MyModule;  // 자동 생성된 Function Block

    // 입력 신호 생성 (예: 사인파 + 노이즈)
    fTime : REAL := 0.0;
    fCleanSignal : REAL;
    fNoisySignal : REAL;
    fFilteredSignal : REAL;

    bReset : BOOL := FALSE;
END_VAR

// 사인파 생성
fTime := fTime + 0.001;  // 1ms 사이클 가정
fCleanSignal := 10.0 * SIN(2.0 * 3.14159 * 5.0 * fTime);  // 5Hz 사인파

// 노이즈 추가 (랜덤)
fNoisySignal := fCleanSignal + (RAND() MOD 100 - 50) / 50.0;

// TcCOM 모듈로 필터링
MyFilterModule.InputSignal := fNoisySignal;
MyFilterModule.Reset := bReset;

fFilteredSignal := MyFilterModule.FilteredSignal;

// 결과 확인
IF MyFilterModule.Ready THEN
    // 매 1000 사이클마다 로그
    IF MyFilterModule.CycleCount MOD 1000 = 0 THEN
        ADSLOGSTR(
            msgCtrlMask := ADSLOG_MSGTYPE_HINT,
            msgFmtStr := '사이클 %d: 원본=%.2f, 노이즈=%.2f, 필터=%.2f',
            strArg := CONCAT(
                UDINT_TO_STRING(MyFilterModule.CycleCount),
                CONCAT('|', REAL_TO_STRING(fCleanSignal))
            )
        );
    END_IF;
END_IF;
```

### ⚡ TcCOM 고급 기능

#### 1. 멀티 태스크 동기화

```cpp
// ✅ C++ - 멀티 태스크에서 TcCOM 사용
HRESULT CMyModule::CycleUpdate(ITcTask* ipTask, ITcUnknown* ipCaller, ULONG_PTR context)
{
    // 태스크 우선순위 확인
    ULONG nPriority = 0;
    ipTask->GetPriority(&nPriority);

    // 고우선순위 태스크에서만 실행
    if (nPriority < 10) {
        return S_OK;
    }

    // 크리티컬 섹션 (다른 태스크와 동기화)
    TcLock lock(m_CriticalSection);

    // 공유 데이터 접근
    m_SharedData.nValue++;

    return S_OK;
}
```

#### 2. 동적 메모리 할당

```cpp
// ✅ C++ - 실시간 안전 메모리 할당
class CMyModule : public ITcModule
{
private:
    // 고정 크기 메모리 풀 (컴파일 타임 할당)
    static const size_t BUFFER_SIZE = 1024;
    BYTE m_StaticBuffer[BUFFER_SIZE];
    size_t m_BufferUsed;

public:
    // 메모리 풀에서 할당 (실시간 안전)
    void* AllocateFromPool(size_t size) {
        if (m_BufferUsed + size > BUFFER_SIZE) {
            TcTrace(tlError, "메모리 풀 부족!");
            return nullptr;
        }

        void* pMem = &m_StaticBuffer[m_BufferUsed];
        m_BufferUsed += size;
        return pMem;
    }

    // 메모리 풀 리셋
    void ResetPool() {
        m_BufferUsed = 0;
    }
};
```

---

## 2.5 메모리 관리 및 최적화

### 🧠 메모리 레이아웃 이해

```
TwinCAT PLC 메모리 구조
│
├── 📦 Static Memory (정적 메모리)
│   ├── PRG 변수 (PROGRAM)
│   ├── GVL 변수 (Global Variable List)
│   ├── FB 인스턴스 (FUNCTION_BLOCK)
│   └── 상수 (CONSTANT)
│
├── 📚 Stack Memory (스택)
│   ├── 지역 변수 (VAR)
│   ├── 임시 변수 (VAR_TEMP)
│   └── 함수 호출 스택
│
└── 💾 Dynamic Memory (동적 메모리) - PLC에서는 제한적
```

### 🎯 메모리 정렬 (Alignment)

```iecst
// ✅ ST - 메모리 정렬 최적화
TYPE ST_OptimizedStruct :
STRUCT
    // ❌ 나쁜 예: 메모리 낭비 (패딩 발생)
    (*
    bFlag1 : BOOL;       // 1바이트
    nValue1 : DINT;      // 4바이트, 3바이트 패딩 추가됨
    bFlag2 : BOOL;       // 1바이트
    nValue2 : DINT;      // 4바이트, 3바이트 패딩 추가됨
    // 총: 16바이트 (실제 데이터 10바이트 + 패딩 6바이트)
    *)

    // ✅ 좋은 예: 큰 타입부터 배치
    nValue1 : DINT;      // 4바이트
    nValue2 : DINT;      // 4바이트
    bFlag1 : BOOL;       // 1바이트
    bFlag2 : BOOL;       // 1바이트
    // 총: 10바이트 (패딩 최소화)
END_STRUCT
END_TYPE
```

```cpp
// ✅ C++ - 메모리 정렬 확인
#include <iostream>

#pragma pack(push, 1)  // 1바이트 정렬 강제 (패딩 제거)
struct PackedStruct {
    bool flag1;
    int value1;
    bool flag2;
    int value2;
};
#pragma pack(pop)

struct UnpackedStruct {
    bool flag1;
    int value1;
    bool flag2;
    int value2;
};

int main() {
    std::cout << "PackedStruct 크기: " << sizeof(PackedStruct) << " 바이트" << std::endl;
    // 출력: 10 바이트

    std::cout << "UnpackedStruct 크기: " << sizeof(UnpackedStruct) << " 바이트" << std::endl;
    // 출력: 16 바이트 (패딩 포함)

    return 0;
}
```

### 🔄 VAR_TEMP 활용 (스택 최적화)

```iecst
// ✅ ST - VAR_TEMP로 스택 메모리 사용
FUNCTION_BLOCK FB_OptimizedProcessing
VAR_INPUT
    pInputData : POINTER TO ARRAY[1..1000] OF REAL;
END_VAR

VAR_OUTPUT
    fResult : REAL;
END_VAR

VAR_TEMP
    // 임시 버퍼 (스택에 할당, 함수 종료 시 자동 해제)
    aTempBuffer : ARRAY[1..1000] OF REAL;
    i : INT;
    fSum : REAL;
END_VAR

// VAR_TEMP는 매 호출마다 스택에 할당됨 (빠름)
fSum := 0.0;

FOR i := 1 TO 1000 DO
    aTempBuffer[i] := pInputData^[i] * 2.0;
    fSum := fSum + aTempBuffer[i];
END_FOR;

fResult := fSum / 1000.0;
```

### 💾 RETAIN 변수 (비휘발성 메모리)

```iecst
// ✅ ST - RETAIN 변수 (전원 꺼져도 유지)
VAR RETAIN
    nProductionTotal : UDINT := 0;      // 총 생산량
    tOperatingHours : TIME := T#0ms;   // 총 가동 시간
    dtLastMaintenance : DT;             // 마지막 정비 일시
END_VAR

VAR PERSISTENT
    // PERSISTENT: RETAIN보다 더 강력 (온라인 변경 시에도 유지)
    stCalibrationData : ST_CalibrationData;
END_VAR
```

### 🚀 메모리 복사 최적화

```iecst
// ✅ ST - MEMCPY vs 루프 비교
FUNCTION_BLOCK FB_MemoryCopyBenchmark
VAR
    aSrc : ARRAY[1..10000] OF BYTE;
    aDest1 : ARRAY[1..10000] OF BYTE;
    aDest2 : ARRAY[1..10000] OF BYTE;

    tStartTime : ULINT;
    tEndTime : ULINT;
    tLoopTime : ULINT;
    tMemcpyTime : ULINT;

    i : INT;
END_VAR

// 방법 1: FOR 루프 (느림)
tStartTime := F_GetSystemTime();

FOR i := 1 TO 10000 DO
    aDest1[i] := aSrc[i];
END_FOR;

tEndTime := F_GetSystemTime();
tLoopTime := tEndTime - tStartTime;

// 방법 2: MEMCPY (빠름)
tStartTime := F_GetSystemTime();

MEMCPY(
    destAddr := ADR(aDest2),
    srcAddr := ADR(aSrc),
    n := SIZEOF(aSrc)
);

tEndTime := F_GetSystemTime();
tMemcpyTime := tEndTime - tStartTime;

// 결과 출력
ADSLOGSTR(
    msgCtrlMask := ADSLOG_MSGTYPE_HINT,
    msgFmtStr := '루프: %d us, MEMCPY: %d us (%.1f배 빠름)',
    strArg := CONCAT(
        ULINT_TO_STRING(tLoopTime),
        CONCAT('|', ULINT_TO_STRING(tMemcpyTime))
    )
);
// 예상 결과: 루프: 500 us, MEMCPY: 50 us (10배 빠름)
```

---

## 2.6 실시간 성능 고려사항

### ⏱️ 사이클 타임 측정

```iecst
// ✅ ST - 정밀한 사이클 타임 측정
PROGRAM CycleTimeMeasurement
VAR
    tStartCycle : ULINT;
    tEndCycle : ULINT;
    tCurrentCycle : ULINT;
    tMaxCycle : ULINT := 0;
    tMinCycle : ULINT := 16#FFFFFFFFFFFFFFFF;
    tAvgCycle : ULINT := 0;
    tSumCycle : ULINT := 0;
    nCycleCount : UDINT := 0;

    // 경고 임계값
    tWarningThreshold : ULINT := 1000000;  // 1ms = 1,000,000ns
END_VAR

// 사이클 시작
tStartCycle := F_GetSystemTime();  // 나노초 단위

// === 여기에 실제 로직 ===
DoProcessing();
// ======================

// 사이클 종료
tEndCycle := F_GetSystemTime();
tCurrentCycle := tEndCycle - tStartCycle;

// 통계 업데이트
IF tCurrentCycle > tMaxCycle THEN
    tMaxCycle := tCurrentCycle;

    // 최대 사이클 타임 경고
    IF tMaxCycle > tWarningThreshold THEN
        ADSLOGSTR(
            msgCtrlMask := ADSLOG_MSGTYPE_WARN,
            msgFmtStr := '!!! 사이클 타임 초과: %d us !!!',
            strArg := ULINT_TO_STRING(tMaxCycle / 1000)
        );
    END_IF;
END_IF;

IF tCurrentCycle < tMinCycle THEN
    tMinCycle := tCurrentCycle;
END_IF;

tSumCycle := tSumCycle + tCurrentCycle;
nCycleCount := nCycleCount + 1;

// 평균 계산 (1000 사이클마다)
IF nCycleCount MOD 1000 = 0 THEN
    tAvgCycle := tSumCycle / 1000;

    ADSLOGSTR(
        msgCtrlMask := ADSLOG_MSGTYPE_HINT,
        msgFmtStr := '사이클 통계 (us): 평균=%d, 최소=%d, 최대=%d',
        strArg := CONCAT(
            ULINT_TO_STRING(tAvgCycle / 1000),
            CONCAT('|', CONCAT(
                ULINT_TO_STRING(tMinCycle / 1000),
                CONCAT('|', ULINT_TO_STRING(tMaxCycle / 1000))
            ))
        )
    );

    // 통계 리셋
    tSumCycle := 0;
    tMaxCycle := 0;
    tMinCycle := 16#FFFFFFFFFFFFFFFF;
END_IF;
```

### 🎯 Jitter 최소화 기법

```iecst
// ✅ ST - 지터 최소화를 위한 패턴
FUNCTION_BLOCK FB_LowJitterControl
VAR
    // 타이머 대신 카운터 사용 (결정적)
    nCycleCounter : UDINT := 0;
    nActionInterval : UDINT := 100;  // 100 사이클마다 실행

    // 조건부 실행 최소화
    bAlwaysExecute : BOOL := TRUE;
END_VAR

// ❌ 나쁜 예: 타이머 사용 (비결정적)
(*
VAR
    fbTimer : TON;
END_VAR

fbTimer(IN := TRUE, PT := T#100ms);
IF fbTimer.Q THEN
    // 100ms마다 실행 (하지만 정확하지 않음)
    DoPeriodicTask();
    fbTimer(IN := FALSE);
END_IF;
*)

// ✅ 좋은 예: 카운터 사용 (결정적)
nCycleCounter := nCycleCounter + 1;

IF nCycleCounter >= nActionInterval THEN
    nCycleCounter := 0;
    DoPeriodicTask();  // 정확히 100 사이클마다 실행
END_IF;

// ✅ 조건 분기 최소화
// 나쁜 예:
(*
IF bCondition1 THEN
    DoTask1();
ELSIF bCondition2 THEN
    DoTask2();
ELSIF bCondition3 THEN
    DoTask3();
ELSE
    DoTask4();
END_IF;
*)

// 좋은 예: 점프 테이블 패턴
CASE nTaskIndex OF
    1: DoTask1();
    2: DoTask2();
    3: DoTask3();
    ELSE DoTask4();
END_CASE;
```

### 🔧 캐시 최적화

```cpp
// ✅ C++ - 캐시 친화적 데이터 구조
#include <vector>

// ❌ 나쁜 예: AoS (Array of Structures) - 캐시 미스 많음
struct Particle_AoS {
    float x, y, z;       // 위치
    float vx, vy, vz;    // 속도
    float mass;          // 질량
    int id;              // ID
};

std::vector<Particle_AoS> particles(10000);

// 위치만 업데이트할 때 불필요한 데이터도 캐시로 로드됨
for (auto& p : particles) {
    p.x += p.vx;
    p.y += p.vy;
    p.z += p.vz;
}

// ✅ 좋은 예: SoA (Structure of Arrays) - 캐시 효율적
struct Particles_SoA {
    std::vector<float> x, y, z;      // 위치
    std::vector<float> vx, vy, vz;   // 속도
    std::vector<float> mass;         // 질량
    std::vector<int> id;             // ID

    Particles_SoA(size_t n) {
        x.resize(n); y.resize(n); z.resize(n);
        vx.resize(n); vy.resize(n); vz.resize(n);
        mass.resize(n);
        id.resize(n);
    }
};

Particles_SoA particles(10000);

// 위치 업데이트 시 위치 데이터만 캐시로 로드됨 (빠름!)
for (size_t i = 0; i < 10000; ++i) {
    particles.x[i] += particles.vx[i];
    particles.y[i] += particles.vy[i];
    particles.z[i] += particles.vz[i];
}
```

---

## 2.7 고급 라이브러리 활용

### 📚 Tc2_Utilities - 고급 기능

#### 문자열 빌더 패턴

```iecst
// ✅ ST - 효율적인 문자열 조합
FUNCTION_BLOCK FB_StringBuilder
VAR
    sBuffer : STRING(4095) := '';
    nCurrentLength : UDINT := 0;
END_VAR

METHOD Append : BOOL
VAR_INPUT
    sText : STRING(255);
END_VAR
VAR
    nTextLen : UDINT;
END_VAR

nTextLen := LEN(sText);

IF (nCurrentLength + nTextLen) > 4095 THEN
    Append := FALSE;  // 버퍼 오버플로우
    RETURN;
END_IF;

// 문자열 추가
sBuffer := CONCAT(sBuffer, sText);
nCurrentLength := nCurrentLength + nTextLen;
Append := TRUE;
END_METHOD

METHOD Clear
sBuffer := '';
nCurrentLength := 0;
END_METHOD

METHOD ToString : STRING(4095)
ToString := sBuffer;
END_METHOD

// 사용 예제
VAR
    sb : FB_StringBuilder;
    sResult : STRING(4095);
END_VAR

sb.Clear();
sb.Append('온도: ');
sb.Append(REAL_TO_STRING(fTemperature));
sb.Append('°C, 압력: ');
sb.Append(REAL_TO_STRING(fPressure));
sb.Append('bar');

sResult := sb.ToString();
// 결과: "온도: 23.5°C, 압력: 1.2bar"
```

#### JSON 파싱 (수동 구현)

```iecst
// ✅ ST - 간단한 JSON 파서
FUNCTION ParseJsonValue : STRING(255)
VAR_INPUT
    sJson : STRING(4095);
    sKey : STRING(50);
END_VAR
VAR
    nKeyPos : INT;
    nValueStart : INT;
    nValueEnd : INT;
    sSearchPattern : STRING(60);
    sValue : STRING(255);
    i : INT;
END_VAR

// 키 검색 패턴: "key":"value"
sSearchPattern := CONCAT('"', CONCAT(sKey, '":"'));
nKeyPos := FIND(sJson, sSearchPattern);

IF nKeyPos > 0 THEN
    nValueStart := nKeyPos + LEN(sSearchPattern);

    // 값 종료 위치 찾기 (다음 " 또는 ,)
    FOR i := nValueStart TO LEN(sJson) DO
        IF MID(sJson, 1, i) = '"' OR MID(sJson, 1, i) = ',' THEN
            nValueEnd := i - 1;
            EXIT;
        END_IF;
    END_FOR;

    // 값 추출
    sValue := MID(sJson, nValueEnd - nValueStart + 1, nValueStart);
    ParseJsonValue := sValue;
ELSE
    ParseJsonValue := '';  // 키를 찾지 못함
END_IF;
```

### 🔢 Tc2_Math - 수학 라이브러리

```iecst
// ✅ ST - 고급 수학 함수
PROGRAM AdvancedMath
VAR
    fAngle : LREAL := 0.0;
    fResult : LREAL;

    aVector1 : ARRAY[1..3] OF LREAL := [1.0, 2.0, 3.0];
    aVector2 : ARRAY[1..3] OF LREAL := [4.0, 5.0, 6.0];
    fDotProduct : LREAL;

    // 통계
    aData : ARRAY[1..100] OF LREAL;
    fMean : LREAL;
    fStdDev : LREAL;
END_VAR

// 삼각 함수 (라디안)
fAngle := 0.5236;  // 30도 = π/6 rad
fResult := SIN(fAngle);  // 0.5

// 쌍곡선 함수
fResult := SINH(1.0);  // 1.175

// 지수 및 로그
fResult := EXP(2.0);   // e^2 = 7.389
fResult := LN(10.0);   // ln(10) = 2.303
fResult := LOG(100.0); // log10(100) = 2.0

// 거듭제곱 및 루트
fResult := EXPT(2.0, 10.0);  // 2^10 = 1024
fResult := SQRT(16.0);       // 4.0

// 벡터 내적 (Dot Product)
fDotProduct := aVector1[1] * aVector2[1] +
               aVector1[2] * aVector2[2] +
               aVector1[3] * aVector2[3];
// 결과: 1*4 + 2*5 + 3*6 = 32

// 평균 및 표준편차
// (Tc3_Math 라이브러리 사용 가정)
(*
fbMean(pData := ADR(aData), nDataCount := 100);
fMean := fbMean.fResult;

fbStdDev(pData := ADR(aData), nDataCount := 100, fMean := fMean);
fStdDev := fbStdDev.fResult;
*)
```

### 🕐 시간 처리 고급

```iecst
// ✅ ST - 시간 연산 및 변환
FUNCTION_BLOCK FB_TimeUtilities
VAR
    dtCurrent : DT;
    stTime : TIMESTRUCT;

    dtStart : DT;
    dtEnd : DT;
    tElapsed : TIME;

    sTimeString : STRING(50);
END_VAR

METHOD GetCurrentTime
// 현재 시간 가져오기
dtCurrent := DT_TO_DT(NT_GetTime());

// TIMESTRUCT로 변환
SYSTEMTIME_TO_DT(dtCurrent, stTime);

ADSLOGSTR(
    msgCtrlMask := ADSLOG_MSGTYPE_HINT,
    msgFmtStr := '현재 시간: %04d-%02d-%02d %02d:%02d:%02d',
    strArg := CONCAT(
        WORD_TO_STRING(stTime.wYear),
        CONCAT('-', CONCAT(
            WORD_TO_STRING(stTime.wMonth),
            CONCAT('-', WORD_TO_STRING(stTime.wDay))
        ))
    )
);
END_METHOD

METHOD CalculateElapsedTime : TIME
// 경과 시간 계산
dtStart := DT#2025-01-15-08:00:00;
dtEnd := DT#2025-01-15-17:30:00;

tElapsed := DT_TO_TIME(dtEnd) - DT_TO_TIME(dtStart);
// 결과: T#9h30m

CalculateElapsedTime := tElapsed;
END_METHOD

METHOD FormatTime : STRING(50)
VAR_INPUT
    tTime : TIME;
END_VAR
VAR
    nHours : DINT;
    nMinutes : DINT;
    nSeconds : DINT;
END_VAR

// TIME을 시:분:초로 변환
nHours := TIME_TO_DINT(tTime) / 3600000;
nMinutes := (TIME_TO_DINT(tTime) MOD 3600000) / 60000;
nSeconds := (TIME_TO_DINT(tTime) MOD 60000) / 1000;

FormatTime := CONCAT(
    DINT_TO_STRING(nHours),
    CONCAT(':', CONCAT(
        DINT_TO_STRING(nMinutes),
        CONCAT(':', DINT_TO_STRING(nSeconds))
    ))
);
// 결과: "9:30:0"
END_METHOD
```

---

## 🎓 실전 예제: 완전한 시스템 구현

### 🏭 프로젝트: 스마트 컨베이어 제어 시스템

```iecst
// ✅ ST - 통합 컨베이어 시스템
PROGRAM SmartConveyorSystem
VAR
    // ============ 하드웨어 I/O ============
    // 센서
    bProductDetected AT %IX0.0 : BOOL;       // 제품 감지 센서
    bEmergencyStop AT %IX0.1 : BOOL;         // 비상 정지 버튼
    fEncoderSpeed AT %IW2 : REAL;            // 엔코더 속도 [mm/s]

    // 액추에이터
    bMotorEnable AT %QX0.0 : BOOL;           // 모터 활성화
    fMotorSpeed AT %QW2 : REAL;              // 모터 속도 설정 [mm/s]

    // ============ Function Blocks ============
    fbConveyorSM : FB_ConveyorStateMachine;  // 상태 머신
    fbAdsComm : FB_AdsDataExchange;          // ADS 통신
    fbDataLogger : FB_DatabaseLogger;        // 데이터베이스 로깅
    fbVisionInspection : FB_VisionQualityCheck;  // 비전 검사

    // ============ 제어 변수 ============
    fTargetSpeed : REAL := 150.0;            // 목표 속도 [mm/s]
    nProductCount : UDINT := 0;              // 생산 개수
    nRejectCount : UDINT := 0;               // 불량 개수

    // ============ 통계 ============
    stStatistics : ST_ProductionStatistics;

    // ============ HMI 통신 ============
    bStartButton : BOOL;                     // HMI 시작 버튼
    bStopButton : BOOL;                      // HMI 정지 버튼
END_VAR

TYPE ST_ProductionStatistics :
STRUCT
    nTotalProduction : UDINT;                // 총 생산량
    nTotalRejects : UDINT;                   // 총 불량수
    fRejectRate : REAL;                      // 불량률 [%]
    tAverageCycleTime : TIME;                // 평균 사이클 타임
    dtLastProduction : DT;                   // 마지막 생산 시간
END_STRUCT
END_TYPE

// ============ 메인 로직 ============

// 1. 상태 머신 업데이트
fbConveyorSM(
    bStart := bStartButton,
    bStop := bStopButton,
    bEmergencyStop := bEmergencyStop,
    fTargetSpeed := fTargetSpeed
);

// 2. 모터 제어 출력
bMotorEnable := fbConveyorSM.bMotorEnable;
fMotorSpeed := fbConveyorSM.fCurrentSpeed;

// 3. 제품 감지 시 처리
IF bProductDetected AND fbConveyorSM.eCurrentState = E_ConveyorState.RUNNING THEN
    // 비전 검사
    fbVisionInspection(bTrigger := TRUE);

    IF fbVisionInspection.bInspectionComplete THEN
        IF fbVisionInspection.bQualityOK THEN
            // 양품
            nProductCount := nProductCount + 1;
            stStatistics.nTotalProduction := stStatistics.nTotalProduction + 1;
        ELSE
            // 불량품
            nRejectCount := nRejectCount + 1;
            stStatistics.nTotalRejects := stStatistics.nTotalRejects + 1;

            // 불량품 배출 로직
            TriggerRejectMechanism();
        END_IF;

        // 데이터베이스 로깅
        fbDataLogger.LogProduction(
            nProductId := stStatistics.nTotalProduction,
            bQualityOK := fbVisionInspection.bQualityOK,
            fSpeed := fEncoderSpeed,
            dtTimestamp := NT_GetTime()
        );

        stStatistics.dtLastProduction := NT_GetTime();
        fbVisionInspection(bTrigger := FALSE);
    END_IF;
END_IF;

// 4. 통계 계산
IF stStatistics.nTotalProduction > 0 THEN
    stStatistics.fRejectRate :=
        (UDINT_TO_REAL(stStatistics.nTotalRejects) /
         UDINT_TO_REAL(stStatistics.nTotalProduction)) * 100.0;
END_IF;

// 5. ADS 통신 (SCADA/HMI로 데이터 전송)
fbAdsComm.UpdateRemoteData(stStatistics);

// 6. 주기적 리포트 (10초마다)
IF fbConveyorSM.nCycleCount MOD 10000 = 0 THEN
    ADSLOGSTR(
        msgCtrlMask := ADSLOG_MSGTYPE_HINT,
        msgFmtStr := '생산 통계: 총=%d, 불량=%d (%.2f%%), 속도=%.1f mm/s',
        strArg := CONCAT(
            UDINT_TO_STRING(stStatistics.nTotalProduction),
            CONCAT('|', CONCAT(
                UDINT_TO_STRING(stStatistics.nTotalRejects),
                CONCAT('|', REAL_TO_STRING(stStatistics.fRejectRate))
            ))
        )
    );
END_IF;
```

---

## 📖 참고 자료 및 더 나아가기

### 🔗 공식 문서

| 리소스 | URL |
|--------|-----|
| **Beckhoff Infosys** | https://infosys.beckhoff.com/ |
| **TwinCAT 3 매뉴얼** | https://download.beckhoff.com/download/document/automation/twincat3/ |
| **ADS 스펙** | Infosys > TwinCAT 3 > ADS |
| **TcCOM 개발 가이드** | Infosys > TwinCAT 3 > C++ |

### 📚 추천 학습 경로

```
1주차: ADS 기초
  ├── ADS 프로토콜 이해
  ├── ST에서 ADSREAD/ADSWRITE
  └── C++로 기본 통신

2주차: ADS 고급
  ├── Notification 활용
  ├── Sum Command 최적화
  └── 멀티스레드 통신

3주차: ST 심화
  ├── 고급 Function Block 설계
  ├── 포인터 및 레퍼런스
  └── 메모리 최적화

4주차: TcCOM 개발
  ├── 간단한 TcCOM 모듈 생성
  ├── PLC 연동
  └── 실시간 성능 측정

5주차: 통합 프로젝트
  └── 완전한 자동화 시스템 구현
```

### 💡 베스트 프랙티스 요약

1. **ADS 통신**
   - ✅ 핸들 재사용
   - ✅ Sum Command 활용
   - ✅ 비동기 I/O 사용
   - ❌ 매번 심볼 이름으로 액세스

2. **PLC 프로그래밍**
   - ✅ Function Block 모듈화
   - ✅ VAR_TEMP로 스택 활용
   - ✅ MEMCPY로 대량 복사
   - ❌ 전역 변수 남용

3. **실시간 성능**
   - ✅ 사이클 타임 모니터링
   - ✅ 결정적 알고리즘
   - ✅ 캐시 친화적 데이터 구조
   - ❌ 동적 메모리 할당

4. **TcCOM 개발**
   - ✅ 고정 크기 메모리 풀
   - ✅ 에러 처리 철저히
   - ✅ 로깅 최소화
   - ❌ 실시간 태스크에서 I/O

---

## 🎉 결론

이 가이드에서 다룬 내용:

✅ **ADS API 완전 정복**
  - ST, C++에서의 ADS 사용법
  - IndexGroup/IndexOffset 상세
  - 성능 최적화 (Sum Command, 핸들 재사용)
  - 비동기 & 멀티스레딩

✅ **PLC 프로그래밍 마스터**
  - ST 고급 문법 및 패턴
  - Function Block 설계 (싱글톤, 상태 머신, 인터페이스)
  - 포인터 및 레퍼런스 활용
  - TcCOM C++ 모듈 개발

✅ **메모리 & 성능 최적화**
  - 메모리 레이아웃 및 정렬
  - 실시간 성능 측정
  - Jitter 최소화 기법
  - 캐시 최적화

✅ **고급 라이브러리 활용**
  - Tc2_Utilities 고급 기능
  - Tc2_Math 수학 함수
  - 시간 처리 및 문자열 조작

이제 여러분은 TwinCAT 3 ADS API와 PLC 프로그래밍의 **진정한 전문가**가 되었습니다! 🚀

---

**📧 피드백 환영**
이 문서에 대한 의견이나 추가 요청 사항이 있다면 언제든지 알려주세요!

**🏷️ 태그**: `#TwinCAT3` `#ADS` `#PLC` `#StructuredText` `#C++` `#TcCOM` `#실시간제어` `#성능최적화` `#산업자동화`

---

> **© 2025 TwinCAT 3 ADS & PLC Programming Deep Dive**
> ST & C++ 중심 실전 가이드