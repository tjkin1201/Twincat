# 📖 TwinCAT 3 용어집 (Glossary)

> **입문자를 위한 TwinCAT 3 핵심 용어 완벽 정리**
>
> 모든 용어는 실무 컨텍스트와 함께 설명됩니다.

---

## 📑 목차

1. [TwinCAT 시스템 & 아키텍처](#1-twincat-시스템--아키텍처)
2. [PLC 프로그래밍 (IEC 61131-3)](#2-plc-프로그래밍-iec-61131-3)
3. [ADS 통신](#3-ads-통신)
4. [데이터 타입 & 메모리](#4-데이터-타입--메모리)
5. [실시간 시스템](#5-실시간-시스템)
6. [Motion Control](#6-motion-control)
7. [TcCOM & C++ 모듈](#7-tccom--c-모듈)
8. [성능 & 최적화](#8-성능--최적화)
9. [상태 머신 & 제어 로직](#9-상태-머신--제어-로직)
10. [에러 처리 & 디버깅](#10-에러-처리--디버깅)
11. [메모리 영역 & 특수 타입](#11-메모리-영역--특수-타입)
12. [상수 & Define](#12-상수--define)
13. [속성 & 지시어](#13-속성--지시어)

---

## 1. 🖥️ TwinCAT 시스템 & 아키텍처

### TwinCAT (The Windows Control and Automation Technology)
- **정의**: Beckhoff의 PC 기반 실시간 제어 소프트웨어 플랫폼
- **특징**: Windows 운영체제에서 실시간(Real-time) 제어 가능
- **구성**: XAE (개발 환경) + XAR (런타임 환경)
- **컨텍스트**: "TwinCAT을 사용하면 일반 PC를 PLC로 사용할 수 있습니다"

### XAE (eXtended Automation Engineering)
- **정의**: TwinCAT 통합 개발 환경 (IDE)
- **기반**: Visual Studio Shell
- **용도**: PLC 프로그램 작성, I/O 설정, Motion 구성 등
- **실행**: Visual Studio에서 `.sln` 파일 열기
- **컨텍스트**: "XAE에서 코드를 작성하고 빌드합니다"

### XAR (eXtended Automation Runtime)
- **정의**: TwinCAT 실시간 런타임 환경
- **역할**: 컴파일된 PLC 프로그램을 실행하는 실시간 커널
- **모드**: Config Mode (설정) ↔ Run Mode (실행)
- **위치**: 타겟 시스템(PC 또는 IPC)에서 실행
- **컨텍스트**: "XAR이 Run Mode일 때 PLC 프로그램이 실행됩니다"

### Target System
- **정의**: TwinCAT 런타임이 실행되는 물리적 하드웨어
- **종류**:
  - 개발 PC (로컬 테스트)
  - IPC (Industrial PC, 현장 설치)
  - CX 임베디드 PC
- **연결**: AMS NetId로 식별
- **컨텍스트**: "개발 PC에서 원격 IPC의 Target System에 배포합니다"

### AMS (Automation Message Specification)
- **정의**: TwinCAT 시스템 간 통신을 위한 주소 체계
- **구성**: NetId + Port
- **NetId 형식**: `192.168.1.100.1.1` (IP주소.네트워크ID.호스트ID)
- **용도**: 네트워크상의 TwinCAT 시스템 고유 식별
- **컨텍스트**: "각 Target System은 고유한 AMS NetId를 가집니다"

### AMS Router
- **정의**: ADS 통신을 중계하는 라우팅 서비스
- **역할**:
  - 로컬/원격 ADS 요청 중계
  - 보안 및 접근 제어
  - 통신 경로 관리
- **비유**: "우체국의 우편 분류 시스템과 유사"
- **컨텍스트**: "모든 ADS 통신은 AMS Router를 거칩니다"

---

## 2. 🔧 PLC 프로그래밍 (IEC 61131-3)

### IEC 61131-3
- **정의**: PLC 프로그래밍 언어 국제 표준
- **포함 언어**: ST, LD, FBD, SFC, IL
- **중요성**: TwinCAT PLC는 이 표준을 준수
- **장점**: 다른 PLC 플랫폼과 개념 공유
- **컨텍스트**: "IEC 61131-3를 알면 다른 PLC 시스템도 쉽게 배울 수 있습니다"

### ST (Structured Text)
- **정의**: 고급 텍스트 기반 PLC 프로그래밍 언어
- **문법**: Pascal과 유사
- **장점**: 복잡한 알고리즘 구현에 적합
- **용도**: 수학 연산, 데이터 처리, 제어 로직
- **예시**:
```iecst
IF sensor THEN
    motor := TRUE;
END_IF
```

### LD (Ladder Diagram)
- **정의**: 계전기 회로도 기반 그래픽 언어
- **특징**: 전기 기술자에게 직관적
- **구성**: 접점(Contact), 코일(Coil)
- **용도**: 간단한 논리 회로
- **컨텍스트**: "LD는 릴레이 로직을 시각적으로 표현합니다"

### POU (Program Organization Unit)
- **정의**: PLC 프로그램의 기본 구성 단위
- **종류**:
  - PROGRAM (프로그램)
  - FUNCTION_BLOCK (함수 블록)
  - FUNCTION (함수)
- **비유**: "객체지향의 클래스/함수와 유사"
- **컨텍스트**: "각 POU는 독립적으로 테스트 가능합니다"

### FB (Function Block)
- **정의**: 상태를 유지하는 재사용 가능한 코드 블록
- **특징**:
  - 내부 변수 보유 (상태 저장)
  - 인스턴스마다 독립적인 메모리
  - VAR_INPUT, VAR_OUTPUT으로 인터페이스 정의
- **선언 예시**:
```iecst
FUNCTION_BLOCK FB_Motor
VAR_INPUT
    enable : BOOL;
END_VAR
VAR
    isRunning : BOOL;  // 내부 상태
END_VAR
```
- **컨텍스트**: "FB는 객체지향의 클래스 인스턴스처럼 동작합니다"

### FC (Function)
- **정의**: 상태를 유지하지 않는 함수
- **특징**:
  - 입력에 대해 항상 같은 출력 (순수 함수)
  - 내부 상태 없음
  - 빠른 실행
- **용도**: 수학 계산, 데이터 변환
- **예시**:
```iecst
FUNCTION CalcAverage : REAL
VAR_INPUT
    a, b : REAL;
END_VAR
CalcAverage := (a + b) / 2.0;
```

### DUT (Data Unit Type)
- **정의**: 사용자 정의 데이터 타입
- **종류**:
  - STRUCT (구조체)
  - ENUM (열거형)
  - UNION (공용체)
  - ALIAS (별칭)
- **예시**:
```iecst
TYPE ST_Motor :
STRUCT
    speed : REAL;
    status : BOOL;
END_STRUCT
END_TYPE
```
- **컨텍스트**: "DUT로 복잡한 데이터를 체계적으로 관리합니다"

### GVL (Global Variable List)
- **정의**: 전역 변수 목록
- **범위**: 프로젝트 전체에서 접근 가능
- **용도**: 시스템 상태, 공유 데이터
- **주의**: 과도한 사용은 유지보수 어려움
- **예시**:
```iecst
VAR_GLOBAL
    gSystemStatus : INT;
    gEmergencyStop : BOOL;
END_VAR
```

### Task
- **정의**: PLC 프로그램 실행 단위
- **종류**:
  - Cyclic Task (주기적 실행, 예: 1ms마다)
  - Event Task (이벤트 발생 시)
- **우선순위**: 1~31 (1이 최고 우선순위)
- **할당**: 각 PROGRAM을 특정 Task에 할당
- **컨텍스트**: "PlcTask는 1ms마다 MAIN 프로그램을 호출합니다"

---

## 3. 📡 ADS 통신

### ADS (Automation Device Specification)
- **정의**: TwinCAT의 디바이스 간 통신 프로토콜
- **특징**:
  - TCP/IP 기반
  - 클라이언트-서버 모델
  - 동기/비동기 지원
- **용도**: PLC ↔ HMI, PLC ↔ C++ 애플리케이션
- **컨텍스트**: "ADS로 외부 프로그램이 PLC 데이터를 읽고 씁니다"

### ADS Command
- **정의**: ADS 프로토콜의 명령 유형
- **주요 커맨드**:
  - `Read` - 데이터 읽기
  - `Write` - 데이터 쓰기
  - `ReadWrite` - 읽기+쓰기 (심볼 조회)
  - `ReadState` - 상태 확인
  - `AddNotification` - 변경 알림 등록
- **컨텍스트**: "ReadWrite로 심볼 핸들을 먼저 획득합니다"

### IndexGroup & IndexOffset
- **정의**: ADS 데이터 주소 체계
- **IndexGroup**: 데이터 영역 (예: I/O, PLC 메모리)
- **IndexOffset**: 해당 영역 내 오프셋
- **상수 예시**:
  - `ADSIGRP_SYM_HNDBYNAME` (0xF003) - 심볼 이름으로 핸들 조회
  - `ADSIGRP_SYM_VALBYHND` (0xF005) - 핸들로 값 읽기
- **컨텍스트**: "IndexGroup/Offset으로 PLC 메모리 특정 위치를 지정합니다"

### Handle (핸들)
- **정의**: PLC 변수에 대한 빠른 접근 식별자
- **획득**: 심볼 이름 → `AdsSyncReadWriteReq` → Handle
- **사용**: Handle로 직접 Read/Write (이름 조회 생략)
- **성능**: 이름 기반보다 **10배 이상 빠름**
- **해제**: 사용 후 `ReleaseSymHandle` 필수
- **예시 플로우**:
```
심볼 이름 "GVL.speed"
    ↓ (한 번만)
Handle = 0x12345678
    ↓ (반복 사용)
Read(Handle) → 100.5
Read(Handle) → 102.3
```

### ADS Notification
- **정의**: 변수 값이 변경되면 자동으로 알림받는 메커니즘
- **장점**: 폴링(Polling) 불필요 → CPU 부하 감소
- **설정**:
  - Cycle Time (확인 주기)
  - Max Delay (최대 지연)
- **콜백**: 변경 시 함수 자동 호출
- **컨텍스트**: "비상정지 신호는 Notification으로 실시간 감지합니다"

### Sum Command
- **정의**: 여러 ADS 명령을 하나로 묶어 전송
- **장점**:
  - 네트워크 왕복 횟수 감소
  - 지연 시간 단축
  - 10개 변수: 개별 10ms → Sum 1ms
- **용도**: 대량 데이터 읽기/쓰기
- **제약**: 모두 같은 타입(Read 또는 Write)
- **컨텍스트**: "100개 센서 값을 Sum Command로 한 번에 읽습니다"

### ADS Port
- **정의**: TwinCAT 애플리케이션의 논리적 포트 번호
- **범위**: 0 ~ 65535
- **예약 포트**:
  - `851` - PLC Runtime (1번째 PLC)
  - `852` - PLC Runtime (2번째 PLC)
  - `10000` - TwinCAT System Service
- **컨텍스트**: "PLC는 포트 851로 통신합니다"

---

## 4. 💾 데이터 타입 & 메모리

### BOOL
- **크기**: 1 bit (메모리상 1 byte 차지)
- **값**: `TRUE` (1) 또는 `FALSE` (0)
- **용도**: 센서 입력, 모터 ON/OFF
- **예시**: `bStartButton : BOOL;`

### INT / DINT / LINT
- **정의**: 정수형 데이터 타입
- **크기**:
  - `INT` - 16bit (-32,768 ~ 32,767)
  - `DINT` - 32bit (-2.1억 ~ 2.1억)
  - `LINT` - 64bit
- **용도**: 카운터, 인덱스, 상태 코드
- **컨텍스트**: "제품 개수는 DINT로 저장합니다"

### REAL / LREAL
- **정의**: 부동소수점 타입
- **크기**:
  - `REAL` - 32bit (단정밀도, 소수점 6~7자리)
  - `LREAL` - 64bit (배정밀도, 소수점 15자리)
- **용도**: 속도, 온도, 압력 등 아날로그 값
- **주의**: 정밀도 손실 주의
- **예시**: `rSpeed : REAL := 125.5;`

### STRING
- **정의**: 문자열 타입
- **크기**: 기본 80 bytes (255까지 가능)
- **선언**: `sMessage : STRING(50);` (50자)
- **용도**: 메시지, 로그, HMI 표시
- **주의**: 크기 고정 (동적 할당 불가)

### TIME / DT
- **정의**: 시간 관련 타입
- **TIME**: 시간 간격 (예: `T#1s500ms`)
- **DT**: 날짜와 시간 (예: `DT#2025-01-15-14:30:00`)
- **용도**: 타이머, 타임스탬프
- **예시**: `tTimeout : TIME := T#5s;`

### POINTER
- **정의**: 메모리 주소를 저장하는 타입
- **선언**: `pData : POINTER TO INT;`
- **용도**:
  - 대용량 데이터 전달 (복사 없이)
  - 동적 데이터 접근
- **위험**: 잘못된 주소 접근 시 시스템 크래시
- **예시**:
```iecst
VAR
    value : INT := 100;
    pValue : POINTER TO INT;
END_VAR
pValue := ADR(value);  // 주소 획득
pValue^ := 200;        // 간접 접근
```

### REFERENCE
- **정의**: C++의 참조와 유사 (안전한 포인터)
- **선언**: `refData : REFERENCE TO REAL;`
- **장점**:
  - NULL 체크 필요
  - 읽기 쉬움
- **용도**: FB 메서드의 VAR_IN_OUT
- **컨텍스트**: "REFERENCE는 TwinCAT 3.1부터 권장됩니다"

### VAR
- **정의**: 일반 변수 선언 (persistent)
- **메모리**: 힙 또는 정적 영역
- **생명주기**: FB/PROGRAM 생명주기 동안 유지
- **용도**: 상태 저장 (카운터, 누적값)
- **예시**:
```iecst
VAR
    counter : INT;  // 호출 간 유지됨
END_VAR
```

### VAR_TEMP
- **정의**: 임시 변수 (스택 메모리)
- **생명주기**: 함수 호출 중만 존재
- **장점**:
  - 빠른 할당/해제
  - 메모리 효율적
- **용도**: 루프 변수, 중간 계산
- **예시**:
```iecst
VAR_TEMP
    i : INT;  // 루프 후 사라짐
END_VAR
```

### VAR_STAT
- **정의**: 정적 변수 (static)
- **특징**:
  - 함수 내 선언
  - 모든 호출에서 공유
  - 초기화 1회만
- **용도**: 호출 횟수 카운터
- **예시**:
```iecst
FUNCTION_BLOCK FB_Test
VAR_STAT
    callCount : DINT;  // 모든 인스턴스 공유
END_VAR
```

### Alignment (메모리 정렬)
- **정의**: 데이터를 특정 주소 경계에 맞추는 것
- **규칙**:
  - `INT` (2byte) → 2의 배수 주소
  - `DINT` (4byte) → 4의 배수 주소
  - `LREAL` (8byte) → 8의 배수 주소
- **이유**: CPU 접근 효율
- **결과**: Padding (빈 공간) 발생
- **예시**:
```
STRUCT
    a : BYTE;   // 1 byte
    // [패딩 1 byte]
    b : INT;    // 2 byte (2의 배수 주소 필요)
END_STRUCT
→ 총 4 bytes (패딩 포함)
```

### Padding
- **정의**: 정렬을 위해 삽입되는 빈 바이트
- **영향**: 메모리 낭비
- **최적화**:
  - 큰 타입부터 선언
  - 같은 크기끼리 그룹화
- **컨텍스트**: "구조체 순서를 바꿔 패딩을 줄입니다"

---

## 5. ⏱️ 실시간 시스템

### Real-time (실시간)
- **정의**: 정해진 시간 내에 반드시 응답하는 시스템
- **Hard Real-time**: 데드라인 초과 시 시스템 실패 (예: 모션 제어)
- **Soft Real-time**: 데드라인 초과 허용 (예: HMI 업데이트)
- **TwinCAT**: Hard Real-time 지원
- **컨텍스트**: "1ms마다 정확히 실행되어야 합니다"

### Cycle Time (사이클 타임)
- **정의**: PLC Task가 실행되는 주기
- **일반값**: 1ms ~ 10ms
- **설정**: Task Configuration에서 지정
- **중요성**: 제어 성능 결정
- **트레이드오프**: 짧을수록 정밀, CPU 부하 증가
- **예시**: "모션 제어는 1ms, 온도 제어는 100ms"

### Jitter (지터)
- **정의**: 사이클 타임의 변동폭
- **측정**: 실제 사이클 타임의 표준편차
- **목표**: 1ms Task → Jitter < 10μs
- **원인**:
  - 인터럽트 간섭
  - CPU 캐시 미스
  - 메모리 할당
- **영향**: 모션 제어 떨림, 불안정성
- **컨텍스트**: "Jitter가 크면 속도 제어가 불안정합니다"

### Interrupt (인터럽트)
- **정의**: 하드웨어 이벤트 발생 시 즉시 처리
- **우선순위**: Task보다 높음
- **용도**:
  - 긴급 정지
  - 고속 카운터
  - 엔코더 신호
- **주의**: ISR(Interrupt Service Routine)은 짧게
- **컨텍스트**: "비상정지는 인터럽트로 즉시 처리합니다"

### Task Priority (우선순위)
- **범위**: 1 ~ 31 (1이 최고)
- **규칙**: 높은 우선순위가 먼저 실행
- **권장**:
  - Motion: 1~5
  - 빠른 제어: 6~10
  - 일반 로직: 11~20
  - HMI: 21~31
- **컨텍스트**: "모션 Task는 우선순위 1로 설정합니다"

### Overrun (오버런)
- **정의**: Task 실행 시간이 Cycle Time 초과
- **결과**:
  - 다음 사이클 지연
  - 시스템 경고
  - 제어 불안정
- **탐지**: TwinCAT 시스템 매니저에서 확인
- **해결**: 코드 최적화 또는 Cycle Time 증가
- **컨텍스트**: "오버런이 발생하면 빨간 경고가 표시됩니다"

---

## 6. 🎯 Motion Control

### Axis (축)
- **정의**: 단일 모션 제어 대상 (모터 1개)
- **구성**:
  - Drive (드라이브)
  - Encoder (엔코더)
  - 기구부
- **제어**: 위치, 속도, 토크
- **선언**: `mcAxis : AXIS_REF;`

### PLCopen
- **정의**: 모션 제어 함수 블록 표준
- **제공 FB**:
  - `MC_Power` - 축 활성화
  - `MC_MoveVelocity` - 속도 제어
  - `MC_MoveAbsolute` - 절대 위치 이동
  - `MC_MoveRelative` - 상대 위치 이동
  - `MC_Stop` - 정지
- **장점**: 다른 시스템과 호환
- **컨텍스트**: "PLCopen FB로 모션을 제어합니다"

### MC_MoveVelocity
- **정의**: 축을 지정 속도로 연속 이동
- **입력**:
  - `Axis` - 대상 축
  - `Execute` - 실행 트리거
  - `Velocity` - 목표 속도
  - `Acceleration` - 가속도
- **출력**:
  - `Done` - 목표 속도 도달
  - `Error` - 에러 발생
- **용도**: 컨베이어, 연속 회전
- **예시**:
```iecst
mcMoveVel(
    Axis := mcAxis,
    Execute := bStart,
    Velocity := 500.0,  // mm/s
    Acceleration := 1000.0
);
```

### Encoder (엔코더)
- **정의**: 모터 위치/속도를 측정하는 센서
- **종류**:
  - Incremental (증분형) - 상대 위치
  - Absolute (절대형) - 절대 위치
- **신호**: A, B상 (방향 감지), Z상 (원점)
- **해상도**: PPR (Pulses Per Revolution)
- **컨텍스트**: "엔코더로 정확한 위치 피드백을 받습니다"

### CamTable (캠 테이블)
- **정의**: Master 축과 Slave 축의 관계를 정의한 테이블
- **용도**: 전자 캠, 동기 제어
- **예시**:
  - Master 0°~360° → Slave 특정 궤적
  - 패키징: 컨베이어 속도에 따라 커터 동기
- **컨텍스트**: "CamTable로 복잡한 동기 모션을 구현합니다"

---

## 7. 🔌 TcCOM & C++ 모듈

### TcCOM (TwinCAT Component Object Model)
- **정의**: TwinCAT의 C++ 모듈 개발 프레임워크
- **기반**: Microsoft COM
- **특징**:
  - PLC와 실시간 통합
  - 복잡한 알고리즘 C++로 구현
  - 하드웨어 직접 접근
- **용도**:
  - Vision 처리
  - 고급 수학 연산
  - 커스텀 드라이버
- **컨텍스트**: "TcCOM으로 PLC에서 불가능한 기능을 추가합니다"

### ITcCyclic
- **정의**: 주기적 실행을 위한 TcCOM 인터페이스
- **메서드**: `CycleUpdate()` - PLC Task마다 호출
- **동기화**: PLC 사이클과 완벽 동기
- **용도**: 실시간 데이터 처리
- **예시**:
```cpp
class CMyModule : public ITcCyclic {
    HRESULT CycleUpdate() override {
        // 1ms마다 실행
        ProcessData();
        return S_OK;
    }
};
```

### CycleUpdate()
- **정의**: ITcCyclic의 핵심 메서드
- **호출 시점**: 각 PLC 사이클마다
- **실행 시간**: Cycle Time 내에 완료 필수
- **주의**:
  - 블로킹 함수 금지 (Sleep, Wait)
  - 동적 메모리 할당 최소화
- **컨텍스트**: "CycleUpdate에서 센서 데이터를 필터링합니다"

### TMI (TwinCAT Module Instance)
- **정의**: TcCOM 모듈의 실행 인스턴스
- **생성**: System Manager에서 추가
- **링크**: PLC 변수와 연결 (Input/Output)
- **설정**: XML 파라미터로 구성
- **컨텍스트**: "TMI를 생성하고 PLC와 링크합니다"

### Module Parameter
- **정의**: TcCOM 모듈의 설정값
- **형식**: XML
- **예시**:
```xml
<Parameters>
    <FilterOrder>3</FilterOrder>
    <Cutoff>10.0</Cutoff>
</Parameters>
```
- **로드**: `SetObjStatePS()` 시 파싱
- **컨텍스트**: "필터 차수를 파라미터로 설정합니다"

---

## 8. 🚀 성능 & 최적화

### Profiling (프로파일링)
- **정의**: 코드 실행 시간 측정 및 분석
- **도구**:
  - `QueryPerformanceCounter()` (C++)
  - TwinCAT Scope View
  - Call Stack Analysis
- **목적**: 병목 지점 찾기
- **컨텍스트**: "프로파일링으로 느린 함수를 발견했습니다"

### Bottleneck (병목)
- **정의**: 시스템 성능을 제한하는 구간
- **원인**:
  - 복잡한 연산
  - 대량 메모리 복사
  - 네트워크 지연
- **해결**:
  - 알고리즘 개선
  - 캐싱
  - 병렬화
- **컨텍스트**: "ADS 통신이 병목입니다 → Sum Command 사용"

### Caching (캐싱)
- **정의**: 자주 사용하는 데이터를 임시 저장
- **예시**:
  - ADS Handle 캐싱 → 심볼 조회 생략
  - 계산 결과 캐싱 → 재계산 방지
- **트레이드오프**: 메모리 vs 속도
- **컨텍스트**: "Handle을 캐싱해서 10배 빨라졌습니다"

### MEMCPY vs 루프
- **MEMCPY**:
  - 대량 메모리 복사 최적화 함수
  - CPU 명령어 레벨 최적화
  - 100개 이상 → MEMCPY 권장
- **FOR 루프**:
  - 소량 데이터 (< 10개)
  - 조건부 복사
  - 가독성 좋음
- **성능 차이**: 1000 elements → MEMCPY 5μs, 루프 50μs
- **컨텍스트**: "배열 전체 복사는 MEMCPY를 사용합니다"

### Inline Function
- **정의**: 함수 호출 대신 코드 직접 삽입
- **키워드**: `{attribute 'inline'}`
- **장점**: 함수 호출 오버헤드 제거
- **단점**: 코드 크기 증가
- **권장**: 작고 자주 호출되는 함수
- **예시**:
```iecst
{attribute 'inline'}
FUNCTION Add : INT
Add := a + b;
END_FUNCTION
```

---

## 9. 🔄 상태 머신 & 제어 로직

### State Machine (상태 머신)
- **정의**: 시스템을 유한개의 상태로 모델링
- **구성**:
  - State (상태): IDLE, RUNNING, ERROR 등
  - Transition (전이): 상태 전환 조건
  - Event (이벤트): 전환 트리거
- **구현**: CASE 문
- **장점**: 명확한 로직, 디버깅 쉬움
- **컨텍스트**: "복잡한 시퀀스는 상태 머신으로 구현합니다"

### State (상태)
- **정의**: 시스템의 현재 동작 모드
- **선언**: ENUM으로 정의
```iecst
TYPE E_State :
(
    IDLE,
    STARTING,
    RUNNING,
    STOPPING,
    ERROR
);
END_TYPE
```
- **특징**: 한 번에 하나의 상태만
- **컨텍스트**: "현재 상태는 RUNNING입니다"

### Transition (전이)
- **정의**: 상태 간 이동
- **조건**: IF/CASE 문으로 체크
- **예시**:
```iecst
CASE eState OF
    IDLE:
        IF bStart THEN
            eState := STARTING;
        END_IF
END_CASE
```
- **중요**: 모든 전이 조건 명확히
- **컨텍스트**: "START 버튼으로 IDLE → STARTING 전이"

### Event (이벤트)
- **정의**: 상태 전이를 일으키는 신호
- **종류**:
  - 외부: 버튼, 센서
  - 내부: 타이머 만료, 조건 만족
- **처리**: 우선순위 고려
- **컨텍스트**: "비상정지 이벤트가 최고 우선순위입니다"

### Sequence (시퀀스)
- **정의**: 순차적으로 실행되는 일련의 동작
- **구현**:
  - 상태 머신
  - STEP 변수 + CASE 문
- **예시**:
  1. 초기화
  2. 원점 복귀
  3. 작업 실행
  4. 종료
- **컨텍스트**: "시퀀스의 각 단계를 상태로 정의합니다"

---

## 10. 🐛 에러 처리 & 디버깅

### Error Code (에러 코드)
- **정의**: 에러 종류를 식별하는 숫자
- **ADS 에러**:
  - `0x0` - 성공
  - `0x4` - 잘못된 파라미터
  - `0x710` - 심볼을 찾을 수 없음
- **PLCopen 에러**:
  - `0x4260` - 축이 활성화되지 않음
- **처리**: CASE 문으로 분기
- **컨텍스트**: "에러 코드 0x710은 변수 이름 오타입니다"

### Exception Handling
- **정의**: 예외 상황 처리
- **ST 방식**:
  - `__TRY` ~ `__CATCH` ~ `__FINALLY`
  - TwinCAT 3.1.4024+
- **전통 방식**: IF 문으로 리턴값 체크
- **권장**: 모든 함수 호출 후 에러 확인
- **예시**:
```iecst
__TRY
    result := RiskyFunction();
__CATCH(e)
    LogError(e);
__FINALLY
    Cleanup();
__ENDTRY
```

### Watchdog (워치독)
- **정의**: 시스템 정지 감지 및 복구
- **원리**: 주기적 신호 체크, 없으면 리셋
- **TwinCAT**: Task Watchdog
- **설정**: Timeout 시간 (예: 10ms)
- **트리거**: Task Overrun, 무한 루프
- **컨텍스트**: "워치독이 발동해서 시스템이 재시작했습니다"

### Breakpoint (중단점)
- **정의**: 코드 실행을 일시 정지하는 지점
- **설정**: 코드 줄 클릭 (빨간 점)
- **효과**: 변수 값 실시간 확인
- **제약**: Run Mode에서는 제한적
- **컨텍스트**: "Breakpoint를 설정해서 변수를 확인합니다"

### Watch Window
- **정의**: 변수 값을 실시간 모니터링하는 창
- **사용**: 변수 이름 입력
- **기능**:
  - 값 읽기
  - 값 강제 변경 (Force)
  - 구조체 전개
- **컨텍스트**: "Watch Window로 센서 값을 모니터링합니다"

### Scope View
- **정의**: 변수의 시간에 따른 파형 표시 도구
- **용도**:
  - 신호 분석
  - 타이밍 검증
  - 노이즈 확인
- **설정**: Sampling Rate, Trigger
- **컨텍스트**: "Scope View로 지터를 시각화합니다"

### Online Change
- **정의**: 런타임 중 코드 수정 후 재배포
- **제약**:
  - 인터페이스 변경 불가
  - 데이터 타입 변경 불가
- **장점**: 시스템 정지 없이 버그 수정
- **위험**: 데이터 손실 가능
- **컨텍스트**: "Online Change로 운전 중 파라미터를 조정합니다"

---

## 11. 🗂️ 메모리 영역 & 특수 타입

### Process Image (프로세스 이미지)
- **정의**: 물리적 I/O와 PLC 간 데이터 교환 버퍼
- **구성**:
  - Input Process Image (입력)
  - Output Process Image (출력)
- **동작**:
  - Task 시작 시: 물리 I/O → Process Image 복사
  - Task 종료 시: Process Image → 물리 I/O 복사
- **장점**: 일관된 데이터 (사이클 중 값 변경 없음)
- **컨텍스트**: "Process Image 덕분에 사이클 내 센서 값이 고정됩니다"

### %I / %Q / %M (직접 주소 지정)
- **정의**: PLC 메모리 영역 직접 접근
- **종류**:
  - `%I` - Input (입력)
  - `%Q` - Output (출력)
  - `%M` - Memory (내부 메모리)
- **형식**: `%IX0.0` (Input, Byte 0, Bit 0)
- **예시**:
```iecst
VAR
    sensor AT %IX0.0 : BOOL;   // 입력 0번 비트
    motor AT %QX1.5 : BOOL;    // 출력 1번 바이트, 5번 비트
    counter AT %MW100 : INT;   // 메모리 워드 100
END_VAR
```
- **주의**: 하드 코딩 피하기 (심볼 사용 권장)
- **컨텍스트**: "레거시 코드에서 %IX0.0 같은 직접 주소를 볼 수 있습니다"

### RETAIN (리테인)
- **정의**: 전원이 꺼져도 값을 유지하는 변수
- **저장 위치**: 비휘발성 메모리 (NVRAM, 배터리 백업 RAM)
- **선언**:
```iecst
VAR RETAIN
    productionCount : DINT;  // 전원 꺼져도 유지
END_VAR
```
- **용도**:
  - 생산량 카운터
  - 총 가동 시간
  - 설정값
- **주의**: 과도한 사용 시 NVRAM 수명 단축
- **컨텍스트**: "총 생산량은 RETAIN 변수로 저장합니다"

### PERSISTENT (퍼시스턴트)
- **정의**: 다운로드 후에도 값을 유지하는 변수
- **차이점**: RETAIN은 전원, PERSISTENT는 프로그램 다운로드
- **선언**:
```iecst
VAR PERSISTENT
    calibrationValue : REAL := 1.234;
END_VAR
```
- **용도**: 교정 계수, 시리얼 넘버
- **저장**: 별도 파일 (BootData)
- **컨텍스트**: "센서 교정값은 PERSISTENT로 보존합니다"

### CONSTANT (상수)
- **정의**: 읽기 전용 값
- **선언**:
```iecst
VAR CONSTANT
    MAX_SPEED : REAL := 1000.0;
    PI : REAL := 3.14159;
END_VAR
```
- **장점**: 실수로 변경 방지, 최적화 가능
- **위치**: 프로그램 메모리 (ROM)
- **컨텍스트**: "최대 속도는 CONSTANT로 정의해서 안전합니다"

### AT 키워드 (주소 바인딩)
- **정의**: 변수를 특정 메모리 주소에 매핑
- **용도**:
  - 물리 I/O 연결
  - 다른 변수와 메모리 공유 (오버레이)
- **예시**:
```iecst
VAR
    temperature AT %IW100 : INT;  // I/O 주소
    speed : REAL;
    speedInt AT speed : DINT;     // 같은 메모리 공유
END_VAR
```
- **위험**: 타입 불일치 시 데이터 깨짐
- **컨텍스트**: "AT으로 변수를 I/O에 직접 매핑합니다"

### Symbolic Access vs Direct Access
- **Symbolic Access**: 변수 이름으로 접근 (`GVL.speed`)
  - 장점: 가독성, 유지보수
  - 단점: 이름 조회 오버헤드 (Handle 캐싱으로 해결)
- **Direct Access**: 주소로 접근 (`%MW100`)
  - 장점: 빠름
  - 단점: 가독성 낮음, 에러 위험
- **권장**: Symbolic Access + Handle 캐싱
- **컨텍스트**: "Symbolic Access로 코드를 작성하고, Handle로 성능을 확보합니다"

---

## 12. 🔢 상수 & Define

### ADS IndexGroup 상수
- **정의**: ADS 프로토콜에서 메모리 영역을 지정하는 상수
- **주요 상수**:

| 상수 이름 | 값 (Hex) | 용도 |
|-----------|----------|------|
| `ADSIGRP_SYM_HNDBYNAME` | `0xF003` | 심볼 이름 → Handle 변환 |
| `ADSIGRP_SYM_VALBYHND` | `0xF005` | Handle → 값 읽기/쓰기 |
| `ADSIGRP_SYM_RELEASEHND` | `0xF006` | Handle 해제 |
| `ADSIGRP_SYM_INFOBYNAMEEX` | `0xF007` | 심볼 정보 조회 (크기, 타입) |
| `ADSIGRP_SYM_UPLOAD` | `0xF00B` | 모든 심볼 업로드 |
| `ADSIGRP_IOIMAGE_RWIB` | `0xF020` | Input Process Image |
| `ADSIGRP_IOIMAGE_RWOB` | `0xF030` | Output Process Image |

- **사용 예시**:
```cpp
// C++ - 심볼 이름으로 Handle 획득
AdsSyncReadWriteReq(
    pAddr,
    ADSIGRP_SYM_HNDBYNAME,  // IndexGroup
    0,                       // IndexOffset
    sizeof(handle),          // 읽을 크기
    &handle,                 // 출력 버퍼
    strlen(symbolName),      // 쓸 크기
    symbolName,              // 심볼 이름
    &bytesRead
);
```
- **컨텍스트**: "ADSIGRP_SYM_HNDBYNAME으로 변수 Handle을 획득합니다"

### ADS Error Code 상수
- **정의**: ADS 통신 에러 코드
- **주요 코드**:

| 코드 (Hex) | 상수 이름 | 의미 |
|-----------|-----------|------|
| `0x0`   | `ADSERR_NOERR` | 성공 |
| `0x1`   | `ADSERR_DEVICE_ERROR` | 내부 에러 |
| `0x4`   | `ADSERR_DEVICE_INVALIDPARM` | 잘못된 파라미터 |
| `0x5`   | `ADSERR_DEVICE_NOTREADY` | 디바이스 준비 안 됨 |
| `0x6`   | `ADSERR_DEVICE_BUSY` | 디바이스 사용 중 |
| `0x7`   | `ADSERR_DEVICE_INVALIDCONTEXT` | 잘못된 컨텍스트 |
| `0x700` | `ADSERR_CLIENT_ERROR` | 클라이언트 에러 |
| `0x701` | `ADSERR_CLIENT_INVALIDPARM` | 잘못된 클라이언트 파라미터 |
| `0x702` | `ADSERR_CLIENT_LISTEMPTY` | 리스트 비어있음 |
| `0x703` | `ADSERR_CLIENT_VARUSED` | 변수 이미 사용 중 |
| `0x710` | `ADSERR_CLIENT_SYMBOLNOTFOUND` | 심볼을 찾을 수 없음 |
| `0x711` | `ADSERR_CLIENT_NOMORESYM` | 더 이상 심볼 없음 |

- **처리 예시**:
```cpp
long nErr = AdsSyncReadReq(...);
if (nErr == 0x710) {
    // 변수 이름 오타 확인
    printf("심볼을 찾을 수 없습니다!\n");
}
```
- **컨텍스트**: "에러 0x710은 변수 이름이 잘못되었습니다"

### PLCopen MC Error Code
- **정의**: Motion Control 함수 블록 에러 코드
- **주요 코드**:

| 코드 (Hex) | 의미 |
|-----------|------|
| `0x4260` | 축이 활성화되지 않음 (MC_Power 필요) |
| `0x4263` | 축이 이미 이동 중 |
| `0x4264` | 위치 범위 초과 |
| `0x4265` | 속도 범위 초과 |
| `0x4266` | 가속도 범위 초과 |
| `0x4267` | 드라이브 하드웨어 에러 |
| `0x4268` | 엔코더 에러 |
| `0x4269` | 소프트 리미트 도달 |

- **사용 예시**:
```iecst
IF mcMoveAbs.Error THEN
    CASE mcMoveAbs.ErrorID OF
        16#4260:
            // 축 활성화 필요
            LogError('MC_Power를 먼저 호출하세요');
        16#4269:
            // 리미트 스위치
            LogError('소프트 리미트 초과');
    END_CASE
END_IF
```
- **컨텍스트**: "0x4260 에러는 축을 먼저 활성화하라는 의미입니다"

### TwinCAT System Port 번호
- **정의**: TwinCAT 시스템에서 예약된 ADS 포트
- **주요 포트**:

| 포트 번호 | 서비스 |
|----------|--------|
| `851` | PLC Runtime (첫 번째 PLC) |
| `852` | PLC Runtime (두 번째 PLC) |
| `853` | PLC Runtime (세 번째 PLC) |
| `10000` | TwinCAT System Service |
| `14000` | TwinCAT 2 PLC (레거시) |
| `301` | NC (Motion Control) |
| `500` | CNC |

- **사용 예시**:
```cpp
AmsAddr addr;
addr.port = 851;  // 첫 번째 PLC에 연결
```
- **컨텍스트**: "포트 851로 첫 번째 PLC와 통신합니다"

### TcCOM HRESULT 코드
- **정의**: TcCOM 함수 반환 값 (COM 표준)
- **주요 코드**:

| 상수 | 값 | 의미 |
|------|-----|------|
| `S_OK` | `0x0` | 성공 |
| `S_FALSE` | `0x1` | 성공 (부분적) |
| `E_FAIL` | `0x80004005` | 일반 실패 |
| `E_INVALIDARG` | `0x80070057` | 잘못된 인수 |
| `E_OUTOFMEMORY` | `0x8007000E` | 메모리 부족 |
| `E_NOTIMPL` | `0x80004001` | 구현 안 됨 |
| `E_POINTER` | `0x80004003` | NULL 포인터 |

- **사용 예시**:
```cpp
HRESULT hr = CycleUpdate();
if (FAILED(hr)) {
    if (hr == E_INVALIDARG) {
        // 파라미터 에러
    }
}
```
- **매크로**: `SUCCEEDED(hr)`, `FAILED(hr)`
- **컨텍스트**: "HRESULT가 S_OK가 아니면 에러입니다"

### System Constants (시스템 상수)
- **정의**: TwinCAT에서 제공하는 기타 상수
- **예시**:
  - `T_PERF_FREERUN_MIN` - 최소 실행 시간 (프로파일링)
  - `T_PERF_FREERUN_MAX` - 최대 실행 시간
  - `SIZEOF()` - 타입/변수 크기
  - `ADR()` - 변수 주소
- **사용**:
```iecst
VAR
    dataSize : UDINT;
END_VAR
dataSize := SIZEOF(ST_MyStruct);  // 구조체 크기
```

---

## 13. 🏷️ 속성 & 지시어

### {attribute 'pack_mode'}
- **정의**: 구조체 메모리 정렬 방식 지정
- **값**:
  - `0` - 자동 정렬 (기본)
  - `1` - 1 byte 정렬 (패딩 없음)
  - `2` - 2 byte 정렬
  - `4` - 4 byte 정렬
  - `8` - 8 byte 정렬
- **사용 예시**:
```iecst
{attribute 'pack_mode' := '1'}
TYPE ST_Compact :
STRUCT
    a : BYTE;   // 오프셋 0
    b : INT;    // 오프셋 1 (패딩 없음!)
    c : DINT;   // 오프셋 3
END_STRUCT
END_TYPE
// 총 7 bytes (패딩 없음)
```
- **용도**:
  - 외부 프로토콜 통신 (정확한 바이트 순서)
  - 메모리 절약
- **주의**: `pack_mode := 1`은 성능 저하 가능 (정렬되지 않은 접근)
- **컨텍스트**: "Modbus 통신 구조체는 pack_mode := 1로 패딩을 제거합니다"

### {attribute 'inline'}
- **정의**: 함수를 인라인으로 전개 (함수 호출 제거)
- **효과**: 속도 향상 ↑, 코드 크기 증가 ↑
- **사용 예시**:
```iecst
{attribute 'inline'}
FUNCTION Add : INT
VAR_INPUT
    a, b : INT;
END_VAR
Add := a + b;
END_FUNCTION
```
- **권장 대상**:
  - 작은 함수 (< 10줄)
  - 자주 호출되는 함수
  - 루프 내부에서 호출
- **컨텍스트**: "간단한 getter 함수는 inline으로 최적화합니다"

### {attribute 'qualified_only'}
- **정의**: 전역 변수 접근 시 전체 경로 강제
- **효과**: 네임스페이스 충돌 방지
- **사용 예시**:
```iecst
{attribute 'qualified_only'}
VAR_GLOBAL
    gCounter : INT;
END_VAR

// 사용 시
GVL.gCounter := 10;  // ✅ OK
gCounter := 10;      // ❌ 에러! (GVL. 접두사 필수)
```
- **장점**: 명확성, 변수 출처 추적
- **컨텍스트**: "qualified_only로 전역 변수 출처를 명확히 합니다"

### {attribute 'hide'}
- **정의**: 인텔리센스/자동완성에서 숨김
- **용도**: 내부 구현, 레거시 코드
- **예시**:
```iecst
{attribute 'hide'}
FUNCTION_BLOCK FB_Internal
// 외부에서 보이지 않음
END_FUNCTION_BLOCK
```

### {warning disable}
- **정의**: 특정 경고 메시지 억제
- **형식**: `{warning disable C0195}` (경고 번호)
- **사용 예시**:
```iecst
{warning disable C0195}  // 사용하지 않는 변수 경고 무시
VAR
    _reserved : INT;
END_VAR
{warning restore C0195}
```
- **주의**: 남용 금지 (실제 문제 숨김 가능)
- **컨텍스트**: "프로토콜 예약 필드는 경고를 비활성화합니다"

### {region} ... {endregion}
- **정의**: 코드 블록을 접을 수 있는 영역 생성
- **용도**: 긴 코드 정리, 가독성
- **예시**:
```iecst
{region '초기화'}
bInitialized := FALSE;
counter := 0;
{endregion}

{region '메인 로직'}
IF bStart THEN
    // ...
END_IF
{endregion}
```
- **컨텍스트**: "region으로 코드를 논리적 섹션으로 나눕니다"

### PRAGMA
- **정의**: 컴파일러 지시어
- **예시**:
  - `{attribute 'linkalways'}` - 사용 안 해도 빌드에 포함
  - `{attribute 'no_assign'}` - 할당 금지 (읽기 전용)
  - `{attribute 'monitoring' := 'call'}` - 호출 시마다 모니터링
- **컨텍스트**: "linkalways로 라이브러리를 강제로 링크합니다"

### TcEncoding
- **정의**: 문자열 인코딩 지정
- **예시**:
```iecst
{attribute 'TcEncoding' := 'UTF-8'}
VAR
    sMessage : STRING := '안녕하세요';
END_VAR
```
- **용도**: 다국어 지원, HMI 통신
- **컨텍스트**: "UTF-8로 한글을 올바르게 표시합니다"

---

## 🔍 빠른 참조표

### 자주 혼동되는 용어

| 용어 A | 용어 B | 차이점 |
|--------|--------|--------|
| **XAE** | **XAR** | XAE는 개발 도구, XAR은 런타임 |
| **FB** | **FC** | FB는 상태 보유, FC는 순수 함수 |
| **VAR** | **VAR_TEMP** | VAR은 영구, VAR_TEMP는 임시 |
| **Handle** | **IndexGroup/Offset** | Handle은 심볼 ID, IndexGroup은 메모리 주소 |
| **Notification** | **Polling** | Notification은 변경 시 알림, Polling은 주기 확인 |
| **Cycle Time** | **Jitter** | Cycle Time은 평균 주기, Jitter는 변동폭 |
| **POINTER** | **REFERENCE** | POINTER는 위험, REFERENCE는 안전 |
| **Alignment** | **Padding** | Alignment는 규칙, Padding은 결과 |
| **RETAIN** | **PERSISTENT** | RETAIN은 전원, PERSISTENT는 다운로드 |
| **Process Image** | **Direct I/O** | Process Image는 버퍼, Direct는 실시간 |
| **Symbolic** | **Direct Access** | Symbolic은 이름, Direct는 주소 |
| **pack_mode := 0** | **pack_mode := 1** | 0은 정렬(패딩), 1은 압축(패딩 없음) |

---

## 📊 용어 사용 빈도 (문서 기준)

**최고 빈도 (입문 필수)**
1. ADS
2. PLC
3. Task
4. FB (Function Block)
5. Handle
6. Cycle Time
7. State Machine
8. VAR
9. REAL/INT
10. TwinCAT

**중간 빈도 (중급)**
1. Notification
2. TcCOM
3. Jitter
4. IndexGroup/Offset
5. PLCopen
6. DUT
7. Sum Command
8. POINTER
9. Alignment
10. Profiling

**낮은 빈도 (고급)**
1. REFERENCE
2. VAR_STAT
3. CamTable
4. Exception Handling
5. Inline Function

---

## 🎓 학습 팁

### 용어 암기 전략

1. **카테고리별 학습**
   - 하루 1개 섹션 집중
   - 연관 용어끼리 묶어서 암기

2. **실습과 연결**
   - 용어를 읽고 → 즉시 코드 작성
   - "ADS Handle"을 배우면 → C++ 예제 실행

3. **시각화 활용**
   - 용어 관계도 그리기
   - 예: ADS → Handle → Read/Write

4. **영문 원어 병행**
   - 공식 문서는 영어
   - 한글 설명 + 영문 용어 함께 암기

### 체크리스트

**1주차**: TwinCAT 시스템 & PLC 기초 용어
- [ ] XAE, XAR, ADS, Task, POU, FB, FC

**2주차**: 통신 & 데이터 타입
- [ ] Handle, IndexGroup/Offset, Notification
- [ ] VAR, VAR_TEMP, INT, REAL, POINTER

**3주차**: 실시간 & Motion
- [ ] Cycle Time, Jitter, Overrun
- [ ] PLCopen, MC_MoveVelocity, Axis

**4주차**: 고급 주제
- [ ] TcCOM, ITcCyclic, CycleUpdate
- [ ] State Machine, Profiling, Sum Command

---

## 🔗 관련 문서 링크

- **전체 API 개요**: `TwinCAT3_Complete_API_Reference_KR.md`
- **ADS & PLC 심화**: `TwinCAT3_ADS_PLC_Deep_Dive_KR.md`
- **시각적 가이드**: `TwinCAT3_Visual_Guide_KR.md`

---

## 📝 용어 추가 요청

이 용어집에 없는 용어가 있거나, 더 자세한 설명이 필요한 용어가 있다면:

1. GitHub Issue 등록
2. 문서 기여 (Pull Request)
3. 팀 내부 공유 및 업데이트

---

**📖 TwinCAT의 언어를 마스터하세요!**

> **용어를 이해하면, 문서가 보이고, 코드가 읽힙니다.**
>
> © 2025 TwinCAT 3 Glossary - Korean Edition
