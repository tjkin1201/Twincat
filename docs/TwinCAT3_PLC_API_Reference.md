# TwinCAT 3 PLC 프로그래밍 API 및 인터페이스 상세 조사 보고서

## 목차
1. [IEC 61131-3 프로그래밍 인터페이스](#1-iec-61131-3-프로그래밍-인터페이스)
2. [Function Block (FB) 및 Function (FC) 라이브러리](#2-function-block-fb-및-function-fc-라이브러리)
3. [PLC 런타임 제어 API](#3-plc-런타임-제어-api)
4. [온라인 변경 및 다운로드 API](#4-온라인-변경-및-다운로드-api)
5. [심볼 액세스 및 데이터 타입 처리](#5-심볼-액세스-및-데이터-타입-처리)
6. [PLC 프로젝트 자동화 API](#6-plc-프로젝트-자동화-api)
7. [주요 라이브러리 상세](#7-주요-라이브러리-상세)
8. [참고 자료](#8-참고-자료)

---

## 1. IEC 61131-3 프로그래밍 인터페이스

### 1.1 개요
TwinCAT 3 PLC는 IEC 61131-3 3판 국제 표준을 준수하여 하나의 CPU에서 하나 이상의 PLC를 구현합니다. 표준에 명시된 모든 프로그래밍 언어를 사용할 수 있습니다.

### 1.2 지원 프로그래밍 언어

#### 텍스트 기반 언어
- **ST (Structured Text)**: 고급 텍스트 기반 언어
- **IL (Instruction List)**: 어셈블러와 유사한 명령어 목록

#### 그래픽 기반 언어
- **LD (Ladder Diagram)**: 전통적인 래더 로직
- **FBD (Function Block Diagram)**: 함수 블록 다이어그램
- **SFC (Sequential Function Chart)**: 순차 기능 차트
- **CFC (Continuous Function Chart)**: 연속 함수 차트 (확장)

### 1.3 프로그램 구성 단위 (POU - Program Organization Unit)

POU는 IEC 61131-3 표준에 정의된 프로그램 조직 단위입니다.

#### PROGRAM (PRG)
```iecst
PROGRAM MAIN
VAR
    counter : INT;
    status : BOOL;
END_VAR

// 구현부
counter := counter + 1;
```

**특징:**
- 인스턴스가 없음
- 프로그램 호출 시 값 변경이 다음 호출까지 유지됨
- PLC 프로젝트 트리에서 (PRG) 접미사로 표시

#### FUNCTION (FUN)
```iecst
FUNCTION CalculateAverage : REAL
VAR_INPUT
    values : ARRAY[1..10] OF REAL;
    count : INT;
END_VAR
VAR
    sum : REAL;
    i : INT;
END_VAR

// 구현부
sum := 0;
FOR i := 1 TO count DO
    sum := sum + values[i];
END_FOR
CalculateAverage := sum / count;
```

**특징:**
- 정확히 하나의 데이터 요소를 반환
- 상태를 유지할 수 없음 (stateless)
- 텍스트 언어에서 표현식의 연산자로 호출 가능

#### FUNCTION_BLOCK (FB)
```iecst
FUNCTION_BLOCK FB_MotorControl
VAR_INPUT
    enable : BOOL;          // 활성화 신호
    targetSpeed : REAL;     // 목표 속도 [rpm]
END_VAR
VAR_OUTPUT
    currentSpeed : REAL;    // 현재 속도 [rpm]
    fault : BOOL;           // 고장 상태
END_VAR
VAR
    internalState : INT;    // 내부 상태 변수
END_VAR

// 구현부
IF enable THEN
    currentSpeed := targetSpeed;
    fault := FALSE;
ELSE
    currentSpeed := 0;
END_IF
```

**특징:**
- 상태를 유지할 수 있음 (stateful)
- 항상 인스턴스를 통해 호출됨
- 객체 지향 프로그래밍 지원 (METHOD, PROPERTY)

#### METHOD (메서드)
```iecst
METHOD DoSomething : BOOL
VAR_INPUT
    param1 : INT;
END_VAR

// 메서드 구현
DoSomething := TRUE;
```

**특징:**
- Function Block에 속하는 함수
- 객체 지향 프로그래밍에 사용
- 인터페이스 구현 시 자동 추가됨

#### PROPERTY (속성)
```iecst
PROPERTY Speed : REAL
```

**특징:**
- Function Block의 속성
- GET/SET 접근자 제공
- 캡슐화 구현에 사용

### 1.4 데이터 타입 시스템

#### 기본 데이터 타입

**불리언 및 비트 타입:**
- `BOOL`: 불리언 값 (TRUE/FALSE)
- `BIT`: 개별 비트

**정수 타입 (부호 있음):**
- `SINT`: 8비트 부호 있는 정수 (-128 ~ 127)
- `INT`: 16비트 부호 있는 정수 (-32,768 ~ 32,767)
- `DINT`: 32비트 부호 있는 정수 (-2,147,483,648 ~ 2,147,483,647)
- `LINT`: 64비트 부호 있는 정수

**정수 타입 (부호 없음):**
- `USINT`: 8비트 부호 없는 정수 (0 ~ 255)
- `UINT`: 16비트 부호 없는 정수 (0 ~ 65,535)
- `UDINT`: 32비트 부호 없는 정수 (0 ~ 4,294,967,295)
- `ULINT`: 64비트 부호 없는 정수

**바이트 지향 타입:**
- `BYTE`: 8비트 (0 ~ 255)
- `WORD`: 16비트 (0 ~ 65,535)
- `DWORD`: 32비트
- `LWORD`: 64비트

**부동소수점 타입:**
- `REAL`: 32비트 IEEE 754 부동소수점
- `LREAL`: 64비트 IEEE 754 부동소수점

**문자열 타입:**
- `STRING`: ASCII 기반 문자열
- `WSTRING`: 와이드/유니코드 문자열

**시간 및 날짜 타입:**
- `TIME`, `LTIME`: 시간 간격
- `DATE`, `LDATE`: 날짜
- `TOD` (TIME_OF_DAY), `LTOD`: 하루 중 시간
- `DT` (DATE_AND_TIME), `LDT`: 날짜와 시간

#### 복합/파생 데이터 타입

**ARRAY (배열):**
```iecst
VAR
    // 1차원 배열
    values : ARRAY[1..10] OF REAL;

    // 다차원 배열
    matrix : ARRAY[1..3, 1..3] OF INT;

    // 초기화
    boolArray : ARRAY[1..2] OF BOOL := [TRUE, FALSE];
END_VAR
```

**STRUCT (구조체):**
```iecst
TYPE ST_MotorData :
STRUCT
    speed : REAL;           // 속도
    position : LREAL;       // 위치
    status : WORD;          // 상태 워드
    fault : BOOL;           // 고장 플래그
END_STRUCT
END_TYPE

VAR
    motor1 : ST_MotorData;
END_VAR
```

**POINTER (포인터):**
```iecst
VAR
    pValue : POINTER TO INT;
    value : INT := 100;
END_VAR

// 사용
pValue := ADR(value);
pValue^ := 200;  // 역참조 연산자 ^
```

**특징:**
- 간접 메모리 액세스 허용
- 역참조 연산자 `^` 필요

**REFERENCE (참조):**
```iecst
VAR_INPUT
    refValue : REFERENCE TO INT;
END_VAR

// 사용
refValue := 150;  // 암시적 역참조, ^ 연산자 불필요
```

**특징:**
- 다른 객체를 암시적으로 가리킴
- 액세스 시 자동으로 역참조됨
- 포인터와 달리 `^` 연산자 불필요

**ENUM (열거형):**
```iecst
TYPE E_State :
(
    IDLE := 0,
    RUNNING := 1,
    STOPPED := 2,
    ERROR := 99
);
END_TYPE
```

---

## 2. Function Block (FB) 및 Function (FC) 라이브러리

### 2.1 TwinCAT 라이브러리 구조

TwinCAT 3는 다양한 표준 라이브러리를 제공하며, 각 라이브러리는 특정 기능 영역을 담당합니다.

#### 주요 라이브러리 목록
- **Tc2_Standard**: IEC 61131-3 표준 함수
- **Tc2_System**: 시스템 관련 함수 블록
- **Tc2_Utilities**: 유틸리티 함수 (문자열, 시간, 데이터 변환)
- **Tc2_MC2**: 모션 컨트롤 (PLCopen V2.0 기반)
- **Tc3_Module**: 모듈 관련 함수
- **Tc3_MC3**: 차세대 모션 컨트롤 라이브러리

### 2.2 Function Block 사용 예시

```iecst
PROGRAM MAIN
VAR
    fbFileOpen : FB_FileOpen;      // 파일 열기 FB 인스턴스
    sFileName : STRING := 'C:\test.txt';
    hFile : UINT;                  // 파일 핸들
    bExecute : BOOL := FALSE;
END_VAR

// FB 호출
fbFileOpen(
    sPathName := sFileName,
    nMode := FOPEN_MODEREAD,
    ePath := PATH_GENERIC,
    bExecute := bExecute,
    tTimeout := T#5s,
    hFile => hFile
);

// 결과 확인
IF fbFileOpen.bBusy THEN
    // 파일 열기 진행 중
ELSIF fbFileOpen.bError THEN
    // 오류 발생
ELSIF hFile <> 0 THEN
    // 파일이 성공적으로 열림
END_IF
```

---

## 3. PLC 런타임 제어 API

### 3.1 ADS (Automation Device Specification) 프로토콜

ADS는 TwinCAT 시스템 내에서 데이터 교환을 위한 전송 계층으로, NC와 PLC 같은 다른 소프트웨어 모듈 간의 통신을 가능하게 합니다.

#### ADS 주요 개념

**AMS NetId (AMS Network Identifier):**
- TwinCAT 네트워크에서 로컬 컴퓨터의 주소
- 6바이트로 구성되며 점 표기법으로 표시 (예: "1.2.3.4.5.6")
- 기본적으로 TwinCAT은 시스템의 IP 주소에 ".1.1"을 추가하여 AMS Net ID 생성
- 예: IP "172.17.213.60" → AMS Net ID "172.17.213.60.1.1"

**포트 라우팅:**
- ADS 장치는 AMS NetId와 포트 번호로 식별됨
- 각 ADS 장치는 포트로 식별 가능
  - 포트 501: NC (수치 제어)
  - 포트 851: PLC Runtime 1
  - 포트 852: PLC Runtime 2
  - 포트 853: PLC Runtime 3
  - 포트 854: PLC Runtime 4

**IndexGroup 및 IndexOffset:**
- ADS 서비스는 두 매개변수로 지정됨
- **IndexGroup**: 액세스할 데이터의 카테고리 또는 유형 지정
- **IndexOffset**: 해당 카테고리 내의 특정 데이터 요소 지정

예시:
- IndexGroup 0x4020: %MB 범위의 주소로 변수 액세스
- IndexGroup 0x4040: 데이터 범위 액세스

### 3.2 ADS 통신 방식

#### 네트워크 통신
- TCP/IP 네트워크를 통한 안정적이고 실시간 통신
- TCP 및 UDP 지원
- Secure ADS: TLSv1.2를 사용한 보안 버전

#### 심볼 액세스 방법

**1. 경로 기반 액세스:**
```javascript
// 변수 경로로 액세스 (예: GVL_Test.ExampleStruct)
readSymbolByPath('GVL_Test.ExampleStruct')
```

**2. 핸들 기반 액세스:**
```csharp
// C# 예시
// 1단계: 변수 핸들 생성
uint handle = adsClient.CreateVariableHandle("GVL_Test.Counter");

// 2단계: 핸들로 데이터 읽기
int value = adsClient.ReadAny<int>(handle);

// 3단계: 핸들 해제
adsClient.DeleteVariableHandle(handle);
```

**3. IndexGroup/IndexOffset 액세스:**
```csharp
// ADS는 PLC 변수 읽기/쓰기에만 국한되지 않음
// AMSNetId, AMSPort, Group, Offset으로 식별되는 함수/데이터
adsClient.Read(indexGroup, indexOffset, dataStream);
```

### 3.3 TcAdsClient 클래스 (TwinCAT.Ads 라이브러리)

**주요 메서드:**

```csharp
using TwinCAT.Ads;

// ADS 클라이언트 생성 및 연결
TcAdsClient client = new TcAdsClient();
client.Connect("127.0.0.1.1.1", 851);  // 로컬 PLC Runtime 1

// 심볼 읽기
int value = client.ReadSymbol<int>("GVL.Variable1");

// 심볼 쓰기
client.WriteSymbol("GVL.Variable1", 100);

// 핸들 생성
uint handle = client.CreateVariableHandle("GVL.Variable2");

// 핸들로 읽기
byte[] data = client.Read(handle, 4);

// 핸들 삭제
client.DeleteVariableHandle(handle);

// 연결 종료
client.Disconnect();
client.Dispose();
```

### 3.4 공식 ADS 라이브러리

**Beckhoff 공식 GitHub 라이브러리:**
- **C++ ADS 라이브러리**: https://github.com/Beckhoff/ADS
- **Node.js ADS 클라이언트**: https://github.com/jisotalo/ads-client (비공식)

**TwinCAT.Ads.dll:**
- TcAdsDll.dll의 래퍼 클래스
- 동기/비동기 ADS 장치 데이터 액세스 가능
- NuGet에서 설치 가능

---

## 4. 온라인 변경 및 다운로드 API

### 4.1 온라인 변경 (Online Change)

온라인 변경은 실행 중인 PLC 프로그램을 중지하지 않고 수정된 부분만 컨트롤러에 다시 로드하는 기능입니다.

#### 온라인 변경 조건
- 컨트롤러에 이미 존재하는 PLC 프로젝트가 수정된 경우
- 수정된 부분만 다시 로드됨
- 실행 중인 프로그램이 중지되지 않음

#### 온라인 변경 실행 방법

**Visual Studio/TwinCAT XAE에서:**
```
PLC 메뉴 → Online Change 선택
```

**Automation Interface를 통한 프로그래밍 방식:**
```csharp
// XML 명령을 통한 온라인 변경
string xml = @"<TreeItem>
    <IECProjectDef>
        <OnlineSettings>
            <Commands>
                <OnlineChangeCmd>true</OnlineChangeCmd>
            </Commands>
        </OnlineSettings>
    </IECProjectDef>
</TreeItem>";

ITcSmTreeItem plcProject = systemManager.LookupTreeItem("TIPC^ProjectName^ProjectName Project");
plcProject.ConsumeXml(xml);
```

### 4.2 다운로드 API

#### PLC 로그인 (Login)
```csharp
string xml = @"<TreeItem>
    <IECProjectDef>
        <OnlineSettings>
            <Commands>
                <LoginCmd>true</LoginCmd>
            </Commands>
        </OnlineSettings>
    </IECProjectDef>
</TreeItem>";

plcProject.ConsumeXml(xml);
```

#### PLC 시작 (Start)
```csharp
string xml = @"<TreeItem>
    <IECProjectDef>
        <OnlineSettings>
            <Commands>
                <StartCmd>true</StartCmd>
            </Commands>
        </OnlineSettings>
    </IECProjectDef>
</TreeItem>";

plcProject.ConsumeXml(xml);
```

#### PLC 중지 (Stop)
```csharp
string xml = @"<TreeItem>
    <IECProjectDef>
        <OnlineSettings>
            <Commands>
                <StopCmd>true</StopCmd>
            </Commands>
        </OnlineSettings>
    </IECProjectDef>
</TreeItem>";

plcProject.ConsumeXml(xml);
```

---

## 5. 심볼 액세스 및 데이터 타입 처리

### 5.1 TMC 파일 (TwinCAT Module Class)

`.tmc` 파일은 TcCOM 모듈 자체를 설명하는 데 사용되며, 모듈 클래스 설명 및 사용되는 데이터 타입을 포함합니다.

#### TMC 에디터에서 데이터 타입 추가
1. TMC 에디터에서 "Data Types" 노드 선택
2. `+` 버튼 클릭하여 새 데이터 영역 추가
3. 사용자 정의 구조체, 인터페이스 생성 가능
4. 기본 TwinCAT 데이터 타입 또는 사용자 정의 데이터 타입 선택 가능

### 5.2 심볼 액세스 방법

#### ADS를 통한 심볼 액세스

**모든 변수는 ADS를 통해 심볼릭하게 사용 가능:**
- 적절한 클라이언트에서 읽기 및 쓰기 가능
- 실시간 모니터링 지원

#### 온라인 뷰에서 변수 모니터링
- 변수 값 실시간 관찰
- Watch Window 활용
- Breakpoint 설정
- Scope View로 신호 파형 관찰

### 5.3 데이터 타입 속성 및 처리

#### 타입 시스템 파일
TwinCAT 3는 타입 시스템을 위한 관리 시스템을 제공하며, 시스템 기본 타입으로 구성되고 고객 프로젝트를 통해 커스텀 데이터 타입으로 확장 가능합니다.

**주요 데이터 타입 카테고리:**
- **Struct**: 다른 데이터 타입의 구조체 (중첩 가능)
- **Enum**: 열거형
- **Array**: 정의된 차원 수를 가진 배열

---

## 6. PLC 프로젝트 자동화 API

### 6.1 TwinCAT Automation Interface 개요

TwinCAT Automation Interface는 프로그래밍/스크립팅 코드를 통해 TwinCAT XAE 구성을 자동으로 생성하고 조작할 수 있게 합니다.

**지원 언어:**
- C++ / .NET
- PowerShell
- Python
- 모든 COM 호환 프로그래밍 언어

**공식 문서:**
- URL: https://infosys.beckhoff.com/content/1033/tc3_automationinterface/
- PDF: https://download.beckhoff.com/download/document/automation/twincat3/Automation_Interface_EN.pdf

### 6.2 ITcSysManager 인터페이스

ITcSysManager는 Automation Interface의 핵심 인터페이스로, 모든 다른 작업에 필요한 참조를 제공합니다.

#### 주요 메서드

**ActivateConfiguration:**
```csharp
// 구성 활성화 (Activate Configuration 버튼 클릭과 동일)
ITcSysManager systemManager = // ... 초기화
systemManager.ActivateConfiguration();
```

**LookupTreeItem:**
```csharp
// 트리 항목 검색
ITcSmTreeItem plcProjectItem = systemManager.LookupTreeItem("TIPC^PlcGenerated");
```

### 6.3 ITcPlcProject 인터페이스

ITcPlcProject 클래스는 PLC 프로젝트의 속성을 설정할 수 있게 하며, 일반적으로 PLC 프로젝트의 루트 노드를 대상으로 합니다.

#### 주요 속성 및 메서드

**BootProjectAutostart:**
```csharp
// PLC 프로젝트 부팅 시 자동 시작 설정
ITcSmTreeItem plcProjectRootItem = systemManager.LookupTreeItem("TIPC^PlcGenerated");
ITcPlcProject iecProjectRoot = (ITcPlcProject)plcProjectRootItem;
iecProjectRoot.BootProjectAutostart = true;
```

**GenerateBootProject:**
```csharp
// 부트 프로젝트 생성
iecProjectRoot.GenerateBootProject(true);
```

### 6.4 실용적인 자동화 예제

#### 프로젝트 초기 설정

```csharp
using EnvDTE;
using TCatSysManagerLib;

// Visual Studio DTE 연결
EnvDTE.DTE dte = (EnvDTE.DTE)System.Runtime.InteropServices.Marshal.GetActiveObject("VisualStudio.DTE.17.0");

// TwinCAT XAE 솔루션 열기
dte.Solution.Open(@"C:\Projects\MyTwinCATProject\MyProject.sln");

// ITcSysManager 참조 얻기
EnvDTE.Project project = dte.Solution.Projects.Item(1);
ITcSysManager systemManager = (ITcSysManager)project.Object;
```

#### 새 PLC 프로젝트 생성

```csharp
// PLC 노드 찾기
ITcSmTreeItem plc = systemManager.LookupTreeItem("TIPC");

// 템플릿 파일 경로
string pathToTemplateFile = @"C:\TwinCAT\3.1\Components\Plc\PlcTemplates\Standard PLC Project.plcproj";

// 새 PLC 프로젝트 생성
ITcSmTreeItem newProject = plc.CreateChild("MyNewProject", 0, "", pathToTemplateFile);
```

#### 모든 PLC 프로젝트에 부트 프로젝트 설정

```csharp
// PLC 트리 항목 가져오기
ITcSmTreeItem plcTreeItem = systemManager.LookupTreeItem("TIPC");
int plcChildCount = plcTreeItem.ChildCount;

// 모든 PLC 프로젝트 순회
for (int i = 1; i <= plcChildCount; i++)
{
    ITcSmTreeItem plcProject = plcTreeItem.get_Child(i);
    ITcPlcProject iecProject = (ITcPlcProject)plcProject;

    // 자동 시작 활성화 및 부트 프로젝트 생성
    iecProject.BootProjectAutostart = true;
    iecProject.GenerateBootProject(true);
}

// 구성 저장 및 활성화
systemManager.ActivateConfiguration();
```

#### PLC 온라인 명령 실행

```csharp
// Login 명령
string loginXml = @"<TreeItem>
    <IECProjectDef>
        <OnlineSettings>
            <Commands>
                <LoginCmd>true</LoginCmd>
            </Commands>
        </OnlineSettings>
    </IECProjectDef>
</TreeItem>";

ITcSmTreeItem plcProject = systemManager.LookupTreeItem("TIPC^MyProject^MyProject Project");
plcProject.ConsumeXml(loginXml);

// Start 명령
string startXml = @"<TreeItem>
    <IECProjectDef>
        <OnlineSettings>
            <Commands>
                <StartCmd>true</StartCmd>
            </Commands>
        </OnlineSettings>
    </IECProjectDef>
</TreeItem>";

plcProject.ConsumeXml(startXml);
```

#### CI/CD 배포 예제

```csharp
public void DeployPLCProject(string targetAmsNetId, string projectPath)
{
    // DTE 연결
    Type dteType = Type.GetTypeFromProgID("VisualStudio.DTE.17.0");
    EnvDTE.DTE dte = (EnvDTE.DTE)Activator.CreateInstance(dteType);

    // 솔루션 열기
    dte.Solution.Open(projectPath);

    // ITcSysManager 얻기
    EnvDTE.Project project = dte.Solution.Projects.Item(1);
    ITcSysManager sysManager = (ITcSysManager)project.Object;

    // 대상 시스템 설정
    sysManager.SetTargetNetId(targetAmsNetId);

    // 구성 활성화
    sysManager.ActivateConfiguration();

    // PLC 프로젝트 찾기
    ITcSmTreeItem plcProject = sysManager.LookupTreeItem("TIPC^MyProject^MyProject Project");
    ITcPlcProject iecProject = (ITcPlcProject)plcProject;

    // 부트 프로젝트 생성
    iecProject.GenerateBootProject(true);
    iecProject.BootProjectAutostart = true;

    // Login 및 Start
    string loginStartXml = @"<TreeItem>
        <IECProjectDef>
            <OnlineSettings>
                <Commands>
                    <LoginCmd>true</LoginCmd>
                    <StartCmd>true</StartCmd>
                </Commands>
            </OnlineSettings>
        </IECProjectDef>
    </TreeItem>";

    plcProject.ConsumeXml(loginStartXml);

    // 정리
    dte.Quit();
}
```

### 6.5 필수 참조 라이브러리

C# 프로젝트에서 Automation Interface 사용 시 다음 COM 참조를 추가해야 합니다:

1. **TcatSysManagerLib**: TwinCAT System Manager 라이브러리
2. **EnvDTE**: Visual Studio Development Environment 라이브러리

**참조 추가 방법 (Visual Studio):**
```
프로젝트 → 참조 추가 → COM → TcatSysManagerLib 선택
프로젝트 → 참조 추가 → COM → EnvDTE 선택
```

---

## 7. 주요 라이브러리 상세

### 7.1 Tc2_System 라이브러리

Tc2_System 라이브러리는 IEC 61131-3 표준 범위에 속하지 않는 TwinCAT 시스템용 함수 및 함수 블록을 포함합니다.

#### 주요 Function Blocks 및 Functions

**시스템 시간 관련:**
- `NT_GetTime`: 현재 로컬 Windows 시스템 시간 읽기
- `NT_SetLocalTime`: 로컬 시스템 시간 설정

**로깅 함수:**
- `ADSLOGSTR`: TwinCAT "Error List" 창에 메시지 쓰기

**파일 작업 Function Blocks:**
- `FB_FileOpen`: 새 파일 생성 또는 기존 파일 열기
- `FB_FileClose`: 파일 닫기
- `FB_FileRead`: 파일에서 데이터 읽기
- `FB_FileWrite`: 파일에 데이터 쓰기
- `FB_FileSeek`: 파일 포인터 위치 설정
- `FB_FileTell`: 현재 파일 포인터 위치 얻기
- `FB_FileGets`: 파일에서 문자열 행 읽기
- `FB_FilePuts`: 파일에 문자열 행 쓰기
- `FB_FileDelete`: 파일 삭제
- `FB_FileRename`: 파일 이름 변경
- `FB_EOF`: 파일 끝 확인
- `FB_CreateDir`: 디렉토리 생성

**파일 작업 예시:**
```iecst
PROGRAM FileHandling
VAR
    fbFileOpen : FB_FileOpen;
    fbFileRead : FB_FileRead;
    fbFileClose : FB_FileClose;

    sFileName : STRING := 'C:\Data\config.txt';
    hFile : UINT;
    buffer : ARRAY[0..255] OF BYTE;
    step : INT := 0;
END_VAR

CASE step OF
    0: // 파일 열기
        fbFileOpen(
            sPathName := sFileName,
            nMode := FOPEN_MODEREAD,
            ePath := PATH_GENERIC,
            bExecute := TRUE,
            tTimeout := T#5s,
            hFile => hFile
        );
        IF NOT fbFileOpen.bBusy THEN
            IF NOT fbFileOpen.bError THEN
                step := 1;
            ELSE
                // 오류 처리
                step := 99;
            END_IF
        END_IF

    1: // 파일 읽기
        fbFileRead(
            hFile := hFile,
            pReadBuff := ADR(buffer),
            cbReadLen := SIZEOF(buffer),
            bExecute := TRUE,
            tTimeout := T#5s
        );
        IF NOT fbFileRead.bBusy THEN
            IF NOT fbFileRead.bError THEN
                step := 2;
            ELSE
                step := 99;
            END_IF
        END_IF

    2: // 파일 닫기
        fbFileClose(
            hFile := hFile,
            bExecute := TRUE,
            tTimeout := T#5s
        );
        IF NOT fbFileClose.bBusy THEN
            step := 0;
        END_IF

    99: // 오류 상태
        // 오류 처리 로직
END_CASE
```

**참고 사항:**
- 파일 함수 블록은 PC의 로컬에서 파일을 처리할 수 있음
- 실시간 요구 사항을 고려하여 파일 작업은 별도 사이클에서 처리 권장

### 7.2 Tc2_Utilities 라이브러리

Tc2_Utilities 라이브러리는 파일 조작, 문자열 처리, 시간 변환, 바이트 순서 변환 등 다양한 작업을 위한 함수 블록을 제공합니다.

#### 문자열 처리 함수

**FB_FormatString:**
최대 10개의 인수를 문자열로 변환하고 형식 사양에 따라 포맷팅합니다 (fprintf와 유사).

```iecst
VAR
    fbFormat : FB_FormatString;
    sFormat : STRING := 'Motor speed: %d rpm, Position: %.2f mm';
    sOutput : STRING(255);
    speed : INT := 1500;
    position : REAL := 123.456;
END_VAR

fbFormat(
    sFormat := sFormat,
    arg1 := F_INT(speed),
    arg2 := F_REAL(position),
    sOut => sOutput
);

// 결과: sOutput = 'Motor speed: 1500 rpm, Position: 123.46 mm'
```

**특징:**
- 동일 PLC 사이클에서 포맷팅 완료
- 출력 문자열이 즉시 사용 가능
- 최대 255자 제한

**FB_FormatString2:**
FB_FormatString과 유사하지만 문자열 크기 제한이 없습니다.

**특징:**
- 포맷 문자열 및 출력 문자열 크기 제한 없음
- 각 인수는 최대 250자로 제한

**형식 지정자:**
- `%d`, `%i`: 정수
- `%u`: 부호 없는 정수
- `%f`: 부동소수점
- `%.2f`: 소수점 2자리
- `%s`: 문자열
- `%x`, `%X`: 16진수

**확장 문자열 함수:**
- `CONCAT2`: 두 문자열 연결 (길이 제한 확인 포함)
- `FIND2`: 문자열 검색
- `DELETE2`: 문자열 삭제
- `INSERT2`: 문자열 삽입
- `REPLACE2`: 문자열 교체
- `FindAndReplace`: 검색 및 교체
- `FindAndSplit`: 검색 및 분할
- `STRING_TO_UTF8`: STRING을 UTF-8로 변환
- `UTF8_TO_STRING`: UTF-8을 STRING으로 변환
- WSTRING 변환 함수

**CONCAT2 예시:**
```iecst
VAR
    sStr1 : STRING := 'Hello';
    sStr2 : STRING := ' World';
    sResult : STRING(255);
END_VAR

sResult := CONCAT2(sStr1, sStr2);
// 결과: sResult = 'Hello World'
```

#### 시간 관련 함수

**FB_LocalSystemTime:**
로컬 시스템 시간을 읽습니다.

```iecst
VAR
    fbSysTime : FB_LocalSystemTime;
    systemTime : TIMESTRUCT;
END_VAR

fbSysTime(
    bEnable := TRUE,
    systemTime => systemTime
);

// systemTime.wYear, .wMonth, .wDay 등으로 접근
```

**시간 변환 함수:**
- 다양한 시간 형식 간 변환 제공
- TIMESTRUCT, FILETIME, DT 등 변환

#### 데이터 변환 함수

**바이트 순서 변환:**
- 빅 엔디안/리틀 엔디안 변환 함수
- 네트워크 통신 시 유용

### 7.3 Tc2_MC2 / Tc3_MC3 라이브러리 (모션 컨트롤)

TwinCAT 모션 컨트롤 라이브러리는 PLCopen 사양 V2.0을 기반으로 합니다.

#### Tc2_MC2 라이브러리
Beckhoff 서보 터미널 기술을 기반으로 한 간단한 기계 애플리케이션 프로그래밍용 함수 블록을 포함합니다.

#### Tc3_MC3 라이브러리
차세대 모션 컨트롤 라이브러리로, PLCopen 호환 함수 블록을 제공합니다.

#### 주요 모션 제어 Function Blocks

**MC_Power:**
```iecst
VAR
    fbPower : MC_Power;
    axis : AXIS_REF;
    bEnable : BOOL;
END_VAR

fbPower(
    Axis := axis,
    Enable := bEnable,
    Enable_Positive := TRUE,
    Enable_Negative := TRUE
);

IF fbPower.Status THEN
    // 축이 활성화됨
END_IF
```

**특징:**
- PLC에서 축 활성화
- 양/음 방향 활성화 제어

**MC_Home:**
```iecst
VAR
    fbHome : MC_Home;
    axis : AXIS_REF;
    bExecute : BOOL;
END_VAR

fbHome(
    Axis := axis,
    Execute := bExecute,
    Position := 0.0,
    HomingMode := MC_DefaultHoming
);

IF fbHome.Done THEN
    // 홈 복귀 완료
END_IF
```

**특징:**
- 축 참조 실행
- 홈 위치 설정

**MC_MoveAbsolute:**
```iecst
VAR
    fbMoveAbs : MC_MoveAbsolute;
    axis : AXIS_REF;
    bExecute : BOOL;
    targetPos : LREAL := 100.0;
END_VAR

fbMoveAbs(
    Axis := axis,
    Execute := bExecute,
    Position := targetPos,
    Velocity := 50.0,
    Acceleration := 100.0,
    Deceleration := 100.0
);

IF fbMoveAbs.Done THEN
    // 이동 완료
END_IF
```

**MC_MoveVelocity:**
```iecst
fbMoveVel(
    Axis := axis,
    Execute := bExecute,
    Velocity := 30.0,
    Acceleration := 50.0,
    Deceleration := 50.0
);
```

**MC_Stop:**
```iecst
fbStop(
    Axis := axis,
    Execute := bExecute,
    Deceleration := 200.0
);
```

**MC_Reset:**
```iecst
fbReset(
    Axis := axis,
    Execute := bExecute
);

IF fbReset.Done THEN
    // 축 리셋 완료
END_IF
```

**MC_Jog:**
```iecst
fbJog(
    Axis := axis,
    JogForward := bJogForward,
    JogBackwards := bJogBackward,
    Velocity := 20.0
);
```

**MC_SetPosition:**
```iecst
fbSetPos(
    Axis := axis,
    Execute := bExecute,
    Position := 0.0,  // 현재 위치를 0으로 설정
    Mode := TRUE
);
```

#### 모션 제어 전형적인 사용 패턴

```iecst
PROGRAM MotionControl
VAR
    axis1 : AXIS_REF;

    fbPower : MC_Power;
    fbHome : MC_Home;
    fbMoveAbs : MC_MoveAbsolute;
    fbStop : MC_Stop;
    fbReset : MC_Reset;

    state : INT := 0;
    bStart : BOOL;
    targetPosition : LREAL;
END_VAR

CASE state OF
    0: // 초기화
        fbPower(Axis := axis1, Enable := TRUE, Enable_Positive := TRUE, Enable_Negative := TRUE);
        IF fbPower.Status THEN
            state := 1;
        END_IF

    1: // 홈 복귀
        fbHome(Axis := axis1, Execute := TRUE, Position := 0.0);
        IF fbHome.Done THEN
            fbHome(Execute := FALSE);
            state := 2;
        ELSIF fbHome.Error THEN
            state := 99;  // 오류 상태
        END_IF

    2: // 대기
        IF bStart THEN
            state := 3;
        END_IF

    3: // 절대 이동
        fbMoveAbs(
            Axis := axis1,
            Execute := TRUE,
            Position := targetPosition,
            Velocity := 100.0,
            Acceleration := 200.0,
            Deceleration := 200.0
        );
        IF fbMoveAbs.Done THEN
            fbMoveAbs(Execute := FALSE);
            state := 2;
        ELSIF fbMoveAbs.Error THEN
            state := 99;
        END_IF

    99: // 오류 처리
        fbReset(Axis := axis1, Execute := TRUE);
        IF fbReset.Done THEN
            fbReset(Execute := FALSE);
            state := 0;
        END_IF
END_CASE
```

### 7.4 Tc2_Standard 라이브러리

IEC 61131-3 표준 함수들을 포함합니다.

#### 수학 함수
- `ABS`: 절대값
- `SQRT`: 제곱근
- `LN`: 자연 로그
- `LOG`: 상용 로그
- `EXP`: 지수 함수
- `SIN`, `COS`, `TAN`: 삼각 함수
- `ASIN`, `ACOS`, `ATAN`: 역삼각 함수

#### 비트 연산 함수
- `AND`, `OR`, `XOR`, `NOT`
- `SHL`, `SHR`: 비트 시프트
- `ROL`, `ROR`: 비트 회전

#### 선택 함수
- `SEL`: 2개 중 선택
- `MAX`, `MIN`: 최대/최소값
- `LIMIT`: 제한 값

#### 문자열 함수
- `LEN`: 문자열 길이
- `LEFT`, `RIGHT`, `MID`: 부분 문자열
- `CONCAT`: 문자열 연결
- `INSERT`, `DELETE`, `REPLACE`: 문자열 조작
- `FIND`: 문자열 검색

#### 타입 변환 함수
- `TO_STRING`, `TO_INT`, `TO_REAL` 등
- 다양한 데이터 타입 간 변환

---

## 8. 참고 자료

### 8.1 공식 Beckhoff 문서

**온라인 정보 시스템 (Infosys):**
- 메인 페이지: https://infosys.beckhoff.com/
- IEC 61131-3: https://infosys.beckhoff.com/content/1033/tcplccontrol/925276683.html
- TwinCAT PLC 소개: https://infosys.beckhoff.com/content/1033/tc3_plc_intro/
- Automation Interface: https://infosys.beckhoff.com/content/1033/tc3_automationinterface/

**다운로드 가능한 매뉴얼 (PDF):**
- Tc2_System: https://download.beckhoff.com/download/document/automation/twincat3/TwinCAT_3_PLC_Lib_Tc2_System_EN.pdf
- Tc2_Utilities: https://download.beckhoff.com/download/document/automation/twincat3/TwinCAT_3_PLC_Lib_Tc2_Utilities_EN.pdf
- Automation Interface: https://download.beckhoff.com/download/document/automation/twincat3/Automation_Interface_EN.pdf
- ADS PowerShell Module: https://download.beckhoff.com/download/document/automation/twincat3/TwinCAT_3_ADS_Powershell_Module_EN.pdf
- ADS-DLL C++: https://download.beckhoff.com/download/document/automation/twincat3/TwinCAT_ADS-DLL_C_EN.pdf
- ADS .NET Samples: https://download.beckhoff.com/download/document/automation/twincat3/TwinCAT_ADS_NET_Samples_EN.pdf

### 8.2 GitHub 리포지토리

**공식 Beckhoff:**
- ADS Protocol (C++): https://github.com/Beckhoff/ADS
- TcOpen (오픈소스 프레임워크): https://github.com/TcOpenGroup/TcOpen

**커뮤니티:**
- ads-client (Node.js): https://github.com/jisotalo/ads-client
- lcls-twincat-motion (모션 라이브러리): https://github.com/pcdshub/lcls-twincat-motion

### 8.3 커뮤니티 및 튜토리얼

**AllTwinCAT:**
- 웹사이트: https://alltwincat.com/
- TwinCAT 3 Tutorial 시리즈
- Automation Interface 튜토리얼

**Contact and Coil:**
- 웹사이트: https://www.contactandcoil.com/
- TwinCAT 3 Tutorial 시리즈
- Function Blocks 튜토리얼

**twinControls Forum:**
- 웹사이트: https://twincontrols.com/community/
- OOP (객체 지향 프로그래밍) 무료 강좌
- 기술 문제 해결

### 8.4 주요 개념 요약

#### ADS 통신
- **프로토콜**: TwinCAT 시스템 내 데이터 교환을 위한 전송 계층
- **AMSNetId**: TwinCAT 네트워크 주소 (6바이트, 점 표기법)
- **포트**: ADS 장치 식별 (PLC Runtime 1 = 851)
- **IndexGroup/IndexOffset**: 데이터 액세스 지정

#### POU 유형
- **PROGRAM**: 상태 유지, 인스턴스 없음
- **FUNCTION**: 단일 반환 값, 상태 비유지
- **FUNCTION_BLOCK**: 상태 유지, 인스턴스 필요
- **METHOD/PROPERTY**: 객체 지향 확장

#### 데이터 타입
- **기본**: BOOL, INT, REAL, STRING, TIME 등
- **복합**: ARRAY, STRUCT, POINTER, REFERENCE, ENUM
- **사용자 정의**: TYPE ... END_TYPE

#### Automation Interface
- **ITcSysManager**: 시스템 관리 인터페이스
- **ITcPlcProject**: PLC 프로젝트 관리 인터페이스
- **XML 명령**: ConsumeXml()을 통한 제어

#### 주요 라이브러리
- **Tc2_System**: 파일, 시간, 로깅
- **Tc2_Utilities**: 문자열, 변환, 유틸리티
- **Tc2_MC2/Tc3_MC3**: 모션 컨트롤 (PLCopen)
- **Tc2_Standard**: IEC 61131-3 표준 함수

---

## 부록 A: 빠른 참조

### A.1 일반적인 PLC 데이터 타입

| 타입 | 크기 | 범위 | 용도 |
|------|------|------|------|
| BOOL | 1 bit | TRUE/FALSE | 디지털 신호 |
| BYTE | 8 bits | 0..255 | 바이트 데이터 |
| WORD | 16 bits | 0..65535 | 상태 워드 |
| DWORD | 32 bits | 0..4,294,967,295 | 상태 더블 워드 |
| SINT | 8 bits | -128..127 | 작은 정수 |
| INT | 16 bits | -32,768..32,767 | 일반 정수 |
| DINT | 32 bits | -2,147,483,648..2,147,483,647 | 큰 정수 |
| REAL | 32 bits | ±3.4e38 | 부동소수점 |
| LREAL | 64 bits | ±1.7e308 | 높은 정밀도 부동소수점 |
| STRING | 변동 | 텍스트 | 문자열 데이터 |
| TIME | 32 bits | - | 시간 간격 |

### A.2 주요 ADS 포트 번호

| 포트 | 장치 |
|------|------|
| 10000 | TwinCAT System Service |
| 350 | TwinCAT I/O |
| 500 | NC |
| 501 | NC-I |
| 851 | PLC Runtime 1 |
| 852 | PLC Runtime 2 |
| 853 | PLC Runtime 3 |
| 854 | PLC Runtime 4 |

### A.3 PLCopen 모션 제어 FB 요약

| Function Block | 기능 |
|----------------|------|
| MC_Power | 축 활성화/비활성화 |
| MC_Home | 홈 복귀 |
| MC_Reset | 축 오류 리셋 |
| MC_MoveAbsolute | 절대 위치 이동 |
| MC_MoveRelative | 상대 위치 이동 |
| MC_MoveVelocity | 속도 모드 이동 |
| MC_Stop | 축 정지 |
| MC_Halt | 축 일시 정지 |
| MC_Jog | 조그 이동 |
| MC_SetPosition | 현재 위치 설정 |

### A.4 자주 사용하는 Tc2_Utilities 함수

| 함수/FB | 기능 |
|---------|------|
| FB_FormatString | 포맷된 문자열 생성 (printf 스타일) |
| CONCAT2 | 문자열 연결 |
| FB_LocalSystemTime | 로컬 시스템 시간 읽기 |
| FIND2 | 문자열 검색 |
| REPLACE2 | 문자열 교체 |

### A.5 자주 사용하는 Tc2_System 함수

| 함수/FB | 기능 |
|---------|------|
| ADSLOGSTR | 로그 메시지 출력 |
| NT_GetTime | Windows 시스템 시간 얻기 |
| FB_FileOpen | 파일 열기 |
| FB_FileRead | 파일 읽기 |
| FB_FileWrite | 파일 쓰기 |
| FB_FileClose | 파일 닫기 |

---

**문서 버전**: 1.0
**작성일**: 2025-11-24
**작성자**: Claude Code Research Agent
**라이선스**: 프로젝트 내부 사용

---

## 변경 이력

| 버전 | 날짜 | 변경 내용 |
|------|------|-----------|
| 1.0 | 2025-11-24 | 초기 문서 작성 |
