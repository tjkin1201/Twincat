# TwinCAT 3 고급 API 가이드

> TwinCAT 3의 IoT, Vision, Database, Safety, Analytics 및 Automation Interface에 대한 종합 가이드

## 목차

1. [TwinCAT IoT - MQTT, OPC UA, Cloud 연동](#1-twincat-iot)
2. [TwinCAT Vision - 머신 비전 및 이미지 처리](#2-twincat-vision)
3. [TwinCAT Database Server - SQL 데이터베이스 연동](#3-twincat-database-server)
4. [TwinCAT Scope - 데이터 로깅 및 측정](#4-twincat-scope)
5. [TwinCAT Safety - 안전 기능 프로그래밍](#5-twincat-safety)
6. [TwinCAT Analytics - 데이터 분석 및 머신러닝](#6-twincat-analytics)
7. [TwinCAT Automation Interface - .NET 기반 프로젝트 자동화](#7-twincat-automation-interface)

---

## 1. TwinCAT IoT

### 1.1 개요

TwinCAT IoT는 산업 자동화 시스템과 IoT 생태계를 연결하는 핵심 기술입니다.

**주요 제품:**
- **TF6100**: TwinCAT 3 OPC UA (Server/Client)
- **TF6105**: OPC UA Pub/Sub (UDP/MQTT)
- **TF6701**: TwinCAT IoT Communication (MQTT)
- **TF6720**: TwinCAT IoT Data Agent

**지원 프로토콜:**
- MQTT v3.1.1, v5.0
- OPC UA (DA/HA/AC)
- AWS IoT
- Microsoft Azure IoT Hub

### 1.2 MQTT 통신 (TF6701)

#### 1.2.1 라이브러리 및 Function Block

**필수 라이브러리:**
```iecst
Tc3_IotBase  // MQTT 통신 기본 라이브러리
```

**주요 Function Blocks:**
- `FB_IotMqttClient`: MQTT 클라이언트 메인 FB
- `FB_IotMqttMessageQueue`: 메시지 큐 관리
- `FB_IotMqttMessage`: 개별 메시지 처리

#### 1.2.2 기본 MQTT 통신 예제

```iecst
PROGRAM PrgMqttCommunication
VAR
    // MQTT 클라이언트 인스턴스
    fbMqttClient        : FB_IotMqttClient;

    // 연결 및 제어 변수
    bSetParameter       : BOOL := TRUE;
    bConnect            : BOOL := FALSE;
    bPublish            : BOOL := FALSE;
    bSubscribe          : BOOL := FALSE;

    // 토픽 및 페이로드
    sTopicPub           : STRING(255) := 'factory/machine01/temperature';
    sTopicSub           : STRING(255) := 'factory/machine01/command';
    sPayload            : STRING(255);

    // 메시지 큐
    fbMessageQueue      : FB_IotMqttMessageQueue;
    fbMessage           : FB_IotMqttMessage;

    // 상태 및 에러
    bConnected          : BOOL;
    bError              : BOOL;
    nErrorId            : UDINT;

    // 타이머
    tonPublishTimer     : TON := (PT := T#1S);  // 1초마다 퍼블리시
END_VAR

// 파라미터 설정 (최초 1회)
IF bSetParameter THEN
    bSetParameter := FALSE;

    // 브로커 설정
    fbMqttClient.sHostName := '192.168.1.100';  // MQTT 브로커 IP
    fbMqttClient.nHostPort := 1883;             // MQTT 포트
    fbMqttClient.sClientId := 'TwinCAT_PLC_01'; // 클라이언트 ID

    // 선택적: 인증 정보
    fbMqttClient.sUserName := 'plc_user';
    fbMqttClient.sUserPassword := 'plc_password';

    // 선택적: Last Will and Testament
    fbMqttClient.sTopicLastWill := 'factory/machine01/status';
    fbMqttClient.sPayloadLastWill := 'offline';

    // QoS 설정 (0, 1, 2)
    fbMqttClient.eQoS := TcIotMqttQos.AtMostOnce;
END_IF

// MQTT 브로커 연결
IF bConnect AND NOT bConnected THEN
    fbMqttClient.Connect();
    bConnected := fbMqttClient.bConnected;

    IF bConnected THEN
        // 연결 성공 시 토픽 구독
        fbMqttClient.Subscribe(sTopic := sTopicSub, eQoS := TcIotMqttQos.AtLeastOnce);
    END_IF
END_IF

// 주기적 데이터 퍼블리시
tonPublishTimer(IN := bConnected);
IF tonPublishTimer.Q THEN
    tonPublishTimer(IN := FALSE);

    // 센서 데이터 읽기 (예시)
    sPayload := CONCAT('{"temperature":', REAL_TO_STRING(fTemperature));
    sPayload := CONCAT(sPayload, ',"humidity":');
    sPayload := CONCAT(sPayload, REAL_TO_STRING(fHumidity));
    sPayload := CONCAT(sPayload, '}');

    // 메시지 퍼블리시
    fbMqttClient.Publish(
        sTopic := sTopicPub,
        pPayload := ADR(sPayload),
        nPayloadSize := LEN(sPayload),
        eQoS := TcIotMqttQos.AtLeastOnce,
        bRetain := FALSE
    );
END_IF

// 구독 메시지 수신 및 처리
IF fbMessageQueue.nQueuedMessages > 0 THEN
    // 큐에서 메시지 가져오기
    IF fbMessageQueue.Dequeue(fbMessage := fbMessage) THEN
        // 토픽 확인
        IF fbMessage.sTopic = sTopicSub THEN
            // 페이로드 처리
            ProcessCommand(fbMessage.GetPayload());
        END_IF
    END_IF
END_IF

// 에러 처리
IF fbMqttClient.bError THEN
    bError := TRUE;
    nErrorId := fbMqttClient.nErrorId;

    // 에러 로깅 또는 재연결 로직
    // ...
END_IF
```

#### 1.2.3 MQTTv5 고급 기능

```iecst
PROGRAM PrgMqttV5Advanced
VAR
    fbMqttClient    : FB_IotMqttClient;

    // MQTTv5 전용 기능
    sResponseTopic  : STRING(255) := 'factory/machine01/response';
    sCorrelationData: STRING(255);

    // User Properties (MQTTv5)
    stUserProps     : ARRAY[0..4] OF ST_IotMqttUserProperty;
END_VAR

// MQTTv5 프로토콜 버전 설정
fbMqttClient.eProtocolVersion := TcIotMqttProtocolVersion.V5_0;

// User Properties 설정 (메타데이터)
stUserProps[0].sKey := 'deviceId';
stUserProps[0].sValue := 'PLC-001';
stUserProps[1].sKey := 'location';
stUserProps[1].sValue := 'Factory-A';

// Request/Response 패턴 (MQTTv5)
fbMqttClient.Publish(
    sTopic := 'factory/server/request',
    pPayload := ADR(sPayload),
    nPayloadSize := LEN(sPayload),
    eQoS := TcIotMqttQos.AtLeastOnce,
    sResponseTopic := sResponseTopic,  // 응답 받을 토픽
    pUserProperties := ADR(stUserProps),
    nUserPropertiesSize := 2
);
```

### 1.3 OPC UA 통신 (TF6100)

#### 1.3.1 OPC UA Server 구성

**특징:**
- IEC 61131-3 변수 자동 매핑
- Information Modeling 지원
- 보안 통신 (암호화, 인증)
- DA (Data Access), HA (Historical Access), AC (Alarms & Conditions)

**기본 설정 단계:**

1. **TwinCAT 프로젝트에 OPC UA Server 추가**
   - Solution Explorer → PLC Project → References → Add Library
   - `Tc3_OpcUa` 라이브러리 추가

2. **PLC 변수 OPC UA 노출**
```iecst
VAR_GLOBAL
    {attribute 'OPC.UA.DA' := '1'}  // OPC UA Data Access 노출
    gMachineStatus      : ST_MachineStatus;

    {attribute 'OPC.UA.DA.Access' := 'Read'}  // 읽기 전용
    gProductionCount    : UDINT;

    {attribute 'OPC.UA.DA.Access' := 'Write'}  // 쓰기 가능
    gSetpoint           : REAL;
END_VAR
```

3. **복잡한 데이터 타입 (구조체) 노출**
```iecst
TYPE ST_MachineStatus :
STRUCT
    {attribute 'OPC.UA.DA' := '1'}
    bRunning        : BOOL;         // 가동 상태
    fSpeed          : REAL;         // 현재 속도 [rpm]
    fTemperature    : REAL;         // 온도 [°C]
    nAlarmCode      : UINT;         // 알람 코드
    sOperatorName   : STRING(50);   // 작업자 이름
END_STRUCT
END_TYPE
```

#### 1.3.2 OPC UA Client (PLC에서 다른 서버 연결)

```iecst
VAR
    fbOpcUaClient       : FB_OpcUaClient;
    fbReadRequest       : FB_OpcUaReadRequest;
    fbWriteRequest      : FB_OpcUaWriteRequest;

    sServerUrl          : STRING(255) := 'opc.tcp://192.168.1.50:4840';
    sNodeId             : STRING(255) := 'ns=2;s=Temperature';

    bConnect            : BOOL;
    bRead               : BOOL;
    fReadValue          : REAL;
END_VAR

// OPC UA 서버 연결
fbOpcUaClient(
    sServerUrl := sServerUrl,
    bConnect := bConnect,
    bConnected => bConnected,
    bError => bError
);

// 데이터 읽기
IF bConnected AND bRead THEN
    bRead := FALSE;

    fbReadRequest(
        pClient := ADR(fbOpcUaClient),
        sNodeId := sNodeId,
        bExecute := TRUE,
        bDone => bReadDone,
        fValue => fReadValue
    );
END_IF
```

### 1.4 클라우드 연동

#### 1.4.1 AWS IoT Core 연동

**필수 제품:** TF6720 (TwinCAT IoT Data Agent)

**주요 기능:**
- AWS IoT Core MQTT 브로커 연결
- Device Shadow 동기화
- Thing 인증서 기반 보안

**설정 예시:**
```iecst
VAR
    fbAwsIotClient  : FB_IotAwsClient;
    sThingName      : STRING := 'Factory_PLC_01';
    sEndpoint       : STRING := 'xxxxx.iot.us-east-1.amazonaws.com';
    sCertPath       : STRING := 'C:\TwinCAT\Certificates\aws-cert.pem';
END_VAR

// AWS IoT 연결
fbAwsIotClient(
    sEndpoint := sEndpoint,
    sThingName := sThingName,
    sCertificatePath := sCertPath,
    bConnect := TRUE
);

// Device Shadow 업데이트
fbAwsIotClient.UpdateShadow(
    sPayload := '{"state":{"reported":{"temperature":25.5}}}'
);
```

#### 1.4.2 Azure IoT Hub 연동

```iecst
VAR
    fbAzureIotClient    : FB_IotAzureClient;
    sConnectionString   : STRING(512);
    sDeviceId           : STRING := 'PLC-Device-01';
END_VAR

// Azure IoT Hub 연결 문자열
sConnectionString := 'HostName=myhub.azure-devices.net;DeviceId=PLC-Device-01;SharedAccessKey=...';

// 연결 및 텔레메트리 전송
fbAzureIotClient(
    sConnectionString := sConnectionString,
    bConnect := TRUE
);

fbAzureIotClient.SendTelemetry(
    sJsonPayload := '{"temperature":25.5,"pressure":101.3}'
);
```

### 1.5 사용 사례

1. **예측 정비 (Predictive Maintenance)**
   - 센서 데이터를 MQTT로 클라우드 전송
   - 머신러닝 모델이 이상 감지
   - 알람을 OPC UA로 SCADA에 전달

2. **디지털 트윈**
   - 실시간 생산 데이터를 AWS/Azure에 전송
   - 클라우드에서 시뮬레이션 실행
   - 최적화된 파라미터를 PLC로 전송

3. **MES/ERP 연동**
   - OPC UA로 생산 데이터 공유
   - 품질 데이터를 데이터베이스에 저장
   - 실시간 대시보드 구축

---

## 2. TwinCAT Vision

### 2.1 개요

TwinCAT Vision은 이미지 처리 및 머신 비전 기능을 PLC 프로그램에 직접 통합하는 솔루션입니다.

**주요 제품:**
- **TF7000**: TwinCAT Vision Base (기본 이미지 처리)
- **TF7100**: TwinCAT Vision Matching 2D (패턴 매칭)
- **TF7200**: TwinCAT Vision Code Reading (바코드/QR 코드)
- **TF7250**: TwinCAT Vision OCR (광학 문자 인식)
- **TF7300**: TwinCAT Vision Metrology 2D (측정)
- **TF7400**: TwinCAT Vision Machine Learning (AI 기반 검사)

**지원 카메라:**
- GigE Vision 표준 카메라
- USB3 Vision 카메라
- File Source (이미지 파일)

### 2.2 기본 이미지 처리

#### 2.2.1 라이브러리 및 인터페이스

**필수 라이브러리:**
```iecst
Tc3_Vision  // TwinCAT Vision 핵심 라이브러리
```

**주요 인터페이스:**
- `ITcVnImage`: 이미지 데이터 인터페이스
- `ITcVnDisplayableImage`: 디스플레이 가능한 이미지
- `ITcVnContainer`: 비전 알고리즘 결과 컨테이너

#### 2.2.2 카메라 이미지 취득 예제

```iecst
PROGRAM PrgVisionAcquisition
VAR
    // Function Blocks
    fbCamera            : FB_VN_SimpleCameraControl;

    // 카메라 상태
    eState              : ETcVnCameraState;

    // 이미지 인터페이스
    ipImageIn           : ITcVnImage;           // 입력 이미지
    ipImageInDisp       : ITcVnDisplayableImage; // 디스플레이용
    ipImageProcessed    : ITcVnImage;           // 처리된 이미지

    // 카운터 및 상태
    nNewImageCounter    : UINT := 0;
    bTrigger            : BOOL;                 // 외부 트리거

    // 에러 처리
    hr                  : HRESULT;
    bError              : BOOL;
END_VAR

// 카메라 상태 확인
eState := fbCamera.GetState();

CASE eState OF
    TCVN_CS_ERROR:
        // 에러 발생 시 리셋
        hr := fbCamera.Reset();
        bError := TRUE;

    TCVN_CS_INITIAL, TCVN_CS_READY:
        // 카메라 준비 상태 → 이미지 취득 시작
        hr := fbCamera.StartAcquisition();

    TCVN_CS_ACQUIRING:
        // 이미지 취득 중
        hr := fbCamera.GetCurrentImage(ipImageIn);

        // 새 이미지 수신 확인
        IF SUCCEEDED(hr) AND ipImageIn <> 0 THEN
            nNewImageCounter := nNewImageCounter + 1;

            // 이미지 처리 함수 호출
            ProcessImage();

            // 디스플레이용 이미지 변환
            hr := F_VN_TransformIntoDisplayableImage(
                ipImageIn,
                ipImageInDisp,
                hr
            );
        END_IF
END_CASE
```

#### 2.2.3 이미지 처리 함수 예제

```iecst
METHOD ProcessImage : BOOL
VAR
    // 이미지 처리 변수
    ipImageGray     : ITcVnImage;       // 그레이스케일 이미지
    ipImageBlurred  : ITcVnImage;       // 블러 처리된 이미지
    ipImageEdges    : ITcVnImage;       // 엣지 검출 이미지

    // 필터 파라미터
    nKernelSize     : UDINT := 5;       // 커널 크기
    fThreshold1     : REAL := 50.0;     // Canny 엣지 하한
    fThreshold2     : REAL := 150.0;    // Canny 엣지 상한

    hr              : HRESULT;
END_VAR

// 1. 컬러 → 그레이스케일 변환
hr := F_VN_ConvertColorSpace(
    ipImageIn,          // 입력 이미지
    ipImageGray,        // 출력 이미지
    TCVN_CST_BGR_TO_GRAY, // 변환 타입
    hr
);

IF FAILED(hr) THEN
    ProcessImage := FALSE;
    RETURN;
END_IF

// 2. 가우시안 블러 (노이즈 제거)
hr := F_VN_GaussianBlur(
    ipImageGray,        // 입력 이미지
    ipImageBlurred,     // 출력 이미지
    nKernelSize,        // 커널 크기 (홀수)
    hr
);

// 3. Canny 엣지 검출
hr := F_VN_Canny(
    ipImageBlurred,     // 입력 이미지
    ipImageEdges,       // 출력 이미지
    fThreshold1,        // 하한 임계값
    fThreshold2,        // 상한 임계값
    nKernelSize,        // Sobel 커널 크기
    hr
);

// 4. 결과 저장
ipImageProcessed := ipImageEdges;

ProcessImage := SUCCEEDED(hr);
```

### 2.3 패턴 매칭 (TF7100)

```iecst
PROGRAM PrgPatternMatching
VAR
    // 템플릿 이미지
    ipTemplateImage     : ITcVnImage;

    // 매칭 결과
    ipMatchResults      : ITcVnContainer;
    stMatchResult       : TcVnMatchResult;

    // 매칭 파라미터
    fMinScore           : REAL := 0.8;  // 최소 매칭 점수 (0.0~1.0)
    nMaxMatches         : UDINT := 10;  // 최대 매칭 개수

    // 결과 변수
    nMatchCount         : UDINT;
    fMatchScore         : REAL;
    stPosition          : TcVnPoint2_REAL;  // 매칭 위치 (x, y)
    fAngle              : REAL;             // 회전 각도 [degree]

    hr                  : HRESULT;
END_VAR

// 템플릿 이미지 로드 (최초 1회)
IF ipTemplateImage = 0 THEN
    hr := F_VN_LoadImage(
        sFilePath := 'C:\Vision\template.bmp',
        ipImage := ipTemplateImage,
        hr := S_OK
    );
END_IF

// 패턴 매칭 수행
hr := F_VN_MatchTemplate(
    ipSrcImage := ipImageIn,            // 검색 대상 이미지
    ipTemplateImage := ipTemplateImage, // 템플릿 이미지
    ipMatchResults := ipMatchResults,   // 매칭 결과 컨테이너
    fMinScore := fMinScore,             // 최소 점수
    nMaxMatches := nMaxMatches,         // 최대 개수
    hr := hr
);

IF SUCCEEDED(hr) AND ipMatchResults <> 0 THEN
    // 매칭 결과 개수
    nMatchCount := F_VN_GetNumberOfElements(ipMatchResults, hr);

    // 첫 번째 매칭 결과 가져오기
    IF nMatchCount > 0 THEN
        hr := F_VN_GetAt_TcVnMatchResult(
            ipMatchResults,
            0,  // 인덱스
            stMatchResult,
            hr
        );

        IF SUCCEEDED(hr) THEN
            fMatchScore := stMatchResult.fScore;        // 매칭 점수
            stPosition := stMatchResult.stPosition;     // 위치 (x, y)
            fAngle := stMatchResult.fAngle;             // 각도

            // 결과 활용 (예: 로봇 좌표 전달)
            SendPositionToRobot(stPosition.fX, stPosition.fY, fAngle);
        END_IF
    END_IF
END_IF
```

### 2.4 바코드/QR 코드 읽기 (TF7200)

```iecst
PROGRAM PrgCodeReading
VAR
    // 코드 리더
    ipCodeResults       : ITcVnContainer;
    stCodeResult        : TcVnCodeResult;

    // 읽은 데이터
    sCodeData           : STRING(255);
    eCodeType           : ETcVnCodeType;  // QR, DataMatrix, EAN13, etc.

    nCodeCount          : UDINT;
    hr                  : HRESULT;
END_VAR

// 바코드/QR 코드 읽기
hr := F_VN_ReadCodes(
    ipSrcImage := ipImageIn,        // 입력 이미지
    ipCodeResults := ipCodeResults, // 결과 컨테이너
    eCodeTypes := TCVN_CT_ALL,      // 모든 코드 타입 읽기
    hr := hr
);

IF SUCCEEDED(hr) AND ipCodeResults <> 0 THEN
    nCodeCount := F_VN_GetNumberOfElements(ipCodeResults, hr);

    // 첫 번째 코드 결과
    IF nCodeCount > 0 THEN
        hr := F_VN_GetAt_TcVnCodeResult(ipCodeResults, 0, stCodeResult, hr);

        IF SUCCEEDED(hr) THEN
            sCodeData := stCodeResult.sData;        // 코드 데이터
            eCodeType := stCodeResult.eCodeType;    // 코드 타입

            // MES 시스템에 데이터 전송
            SendToMES(sCodeData);
        END_IF
    END_IF
END_IF
```

### 2.5 측정 (TF7300 Metrology)

```iecst
PROGRAM PrgMetrology
VAR
    // 엣지 검출 및 측정
    ipEdges             : ITcVnContainer;
    stLine              : TcVnLine2D_REAL;
    stCircle            : TcVnCircle2D_REAL;

    // 측정 결과
    fDistance           : REAL;     // 거리 [pixel]
    fDistanceMM         : REAL;     // 거리 [mm]
    fDiameter           : REAL;     // 직경 [mm]
    fCalibration        : REAL := 0.05;  // 캘리브레이션 [mm/pixel]

    // 품질 판정
    bQualityOK          : BOOL;
    fToleranceMin       : REAL := 9.8;   // 하한 [mm]
    fToleranceMax       : REAL := 10.2;  // 상한 [mm]

    hr                  : HRESULT;
END_VAR

// 원 검출
hr := F_VN_DetectCircles(
    ipSrcImage := ipImageIn,
    ipCircles := ipEdges,
    fMinRadius := 50.0,     // 최소 반지름 [pixel]
    fMaxRadius := 200.0,    // 최대 반지름 [pixel]
    hr := hr
);

IF SUCCEEDED(hr) AND ipEdges <> 0 THEN
    // 첫 번째 원 가져오기
    hr := F_VN_GetAt_TcVnCircle2D_REAL(ipEdges, 0, stCircle, hr);

    IF SUCCEEDED(hr) THEN
        // 직경 계산 (pixel → mm)
        fDiameter := stCircle.fRadius * 2.0 * fCalibration;

        // 품질 판정
        bQualityOK := (fDiameter >= fToleranceMin) AND (fDiameter <= fToleranceMax);

        IF NOT bQualityOK THEN
            // 불량품 처리
            TriggerReject();
        END_IF
    END_IF
END_IF
```

### 2.6 머신러닝 기반 검사 (TF7400)

```iecst
PROGRAM PrgMachineLearningInspection
VAR
    // 학습된 모델
    sModelPath          : STRING := 'C:\Vision\defect_model.xml';
    ipModel             : ITcVnMlModel;

    // 분류 결과
    ipClassResults      : ITcVnContainer;
    stClassResult       : TcVnClassificationResult;

    // 결과 데이터
    sClassName          : STRING(50);   // 클래스 이름 (예: "OK", "NG")
    fConfidence         : REAL;         // 신뢰도 (0.0~1.0)

    bDefectDetected     : BOOL;
    hr                  : HRESULT;
END_VAR

// 모델 로드 (최초 1회)
IF ipModel = 0 THEN
    hr := F_VN_LoadMlModel(sModelPath, ipModel, hr);
END_IF

// 이미지 분류 (양/불량 판정)
hr := F_VN_ClassifyImage(
    ipSrcImage := ipImageIn,
    ipModel := ipModel,
    ipResults := ipClassResults,
    hr := hr
);

IF SUCCEEDED(hr) AND ipClassResults <> 0 THEN
    hr := F_VN_GetAt_TcVnClassificationResult(ipClassResults, 0, stClassResult, hr);

    IF SUCCEEDED(hr) THEN
        sClassName := stClassResult.sClassName;     // "OK" or "Defect"
        fConfidence := stClassResult.fConfidence;   // 신뢰도

        // 불량 판정 (신뢰도 90% 이상)
        bDefectDetected := (sClassName = 'Defect') AND (fConfidence >= 0.9);

        IF bDefectDetected THEN
            // 불량품 처리 로직
            TriggerReject();
            LogDefect(sClassName, fConfidence);
        END_IF
    END_IF
END_IF
```

### 2.7 사용 사례

1. **품질 검사**
   - 부품 치수 측정 (Metrology)
   - 표면 결함 검출 (Machine Learning)
   - 바코드 읽기 및 추적 (Code Reading)

2. **로봇 비전**
   - 부품 위치 인식 (Pattern Matching)
   - 픽앤플레이스 좌표 계산
   - 다양한 각도/위치 대응

3. **조립 검증**
   - 부품 존재 확인
   - 올바른 방향 확인
   - 조립 완성도 검사

---

## 3. TwinCAT Database Server

### 3.1 개요

TwinCAT Database Server (TF6420)는 PLC에서 직접 SQL 데이터베이스와 통신할 수 있는 기능을 제공합니다.

**지원 데이터베이스:**
- Microsoft SQL Server
- MySQL / MariaDB
- PostgreSQL
- SQLite
- Oracle Database

**운영 모드:**
1. **Configure Mode**: 코드 없이 GUI로 설정
2. **PLC Expert Mode**: PLC FB로 자동 생성
3. **SQL Expert Mode**: FB 및 C++ 직접 제어

### 3.2 라이브러리 및 Function Block

**필수 라이브러리:**
```iecst
Tc3_Database  // Database Server 라이브러리
```

**주요 Function Blocks:**
- `FB_SQLDatabaseEvt`: 데이터베이스 연결 관리
- `FB_SQLCommandEvt`: SQL 명령 실행
- `FB_DBRecordInsert`: 레코드 삽입
- `FB_DBRecordSelect`: 레코드 조회
- `FB_DBRecordUpdate`: 레코드 업데이트

### 3.3 데이터베이스 연결 및 기본 사용

#### 3.3.1 연결 설정

```iecst
PROGRAM PrgDatabaseConnection
VAR
    // 데이터베이스 연결 FB
    fbDatabase      : FB_SQLDatabaseEvt;

    // 연결 상태
    bConnect        : BOOL := FALSE;
    bConnected      : BOOL;
    bDisconnect     : BOOL := FALSE;

    // 연결 정보
    hDBID           : UDINT := 1;  // 데이터베이스 핸들 ID

    // 에러 처리
    bError          : BOOL;
    nErrorId        : UDINT;
    sSQLState       : STRING(5);

    // 타임아웃
    tTimeout        : TIME := T#10S;
END_VAR

// 데이터베이스 연결
fbDatabase(
    sNetID := '',           // 로컬
    hDBID := hDBID,
    bConnect := bConnect,
    bDisconnect := bDisconnect,
    tTimeout := tTimeout,
    bConnected => bConnected,
    bError => bError,
    nErrID => nErrorId,
    sSQLState => sSQLState
);

// 에러 발생 시
IF bError THEN
    // 에러 로깅
    LogError(nErrorId, sSQLState);
END_IF
```

**Database Server 설정 (TwinCAT System Manager):**

1. Solution Explorer → SYSTEM → Database Server
2. Database Connections → Add Connection
3. 연결 정보 입력:
   - Name: `MyDatabase`
   - Database Type: `MS SQL Server`
   - Server: `192.168.1.100\SQLEXPRESS`
   - Database: `ProductionDB`
   - User: `plc_user`
   - Password: `plc_password`
   - Handle ID: `1`

#### 3.3.2 INSERT 작업

```iecst
PROGRAM PrgDatabaseInsert
VAR
    // SQL 명령 FB
    fbInsertCmd     : FB_SQLCommandEvt;

    // 삽입할 데이터
    stProductData   : ST_ProductData;

    // SQL 문자열
    sInsertQuery    : STRING(512);
    sDateTimeStr    : STRING(50);

    // 제어
    bInsert         : BOOL;
    bBusy           : BOOL;
    bDone           : BOOL;
    bError          : BOOL;
    nErrorId        : UDINT;
END_VAR

// 제품 데이터 구조체
TYPE ST_ProductData :
STRUCT
    sProductID      : STRING(20);   // 제품 ID
    nQuantity       : INT;          // 수량
    fWeight         : REAL;         // 중량 [kg]
    bQualityOK      : BOOL;         // 품질 합격 여부
    dtTimestamp     : DT;           // 타임스탬프
END_STRUCT
END_TYPE

// 현재 시간 문자열 생성
sDateTimeStr := DT_TO_STRING(stProductData.dtTimestamp);

// SQL INSERT 문 생성
sInsertQuery := CONCAT('INSERT INTO tbl_Production ',
                      '(ProductID, Quantity, Weight, QualityOK, Timestamp) VALUES (');
sInsertQuery := CONCAT(sInsertQuery, '$'');
sInsertQuery := CONCAT(sInsertQuery, stProductData.sProductID);
sInsertQuery := CONCAT(sInsertQuery, '$', ');
sInsertQuery := CONCAT(sInsertQuery, INT_TO_STRING(stProductData.nQuantity));
sInsertQuery := CONCAT(sInsertQuery, ', ');
sInsertQuery := CONCAT(sInsertQuery, REAL_TO_STRING(stProductData.fWeight));
sInsertQuery := CONCAT(sInsertQuery, ', ');
sInsertQuery := CONCAT(sInsertQuery, BOOL_TO_STRING(stProductData.bQualityOK));
sInsertQuery := CONCAT(sInsertQuery, ', $'');
sInsertQuery := CONCAT(sInsertQuery, sDateTimeStr);
sInsertQuery := CONCAT(sInsertQuery, '$')');

// INSERT 실행
fbInsertCmd(
    sNetID := '',
    hDBID := hDBID,
    sCmd := sInsertQuery,
    bExecute := bInsert,
    tTimeout := T#5S,
    bBusy => bBusy,
    bError => bError,
    nErrID => nErrorId
);

// 완료 처리
IF NOT bBusy AND NOT bError AND bInsert THEN
    bInsert := FALSE;  // 플래그 리셋
    // 성공 로깅
END_IF
```

#### 3.3.3 고급 INSERT (FB_DBRecordInsert)

```iecst
PROGRAM PrgDatabaseInsertAdvanced
VAR
    fbRecordInsert  : FB_DBRecordInsert;

    // 포맷 스트링 헬퍼
    fbFormatString  : FB_FormatString;

    // 데이터
    sProductID      : STRING(20) := 'PROD-12345';
    fTemperature    : REAL := 25.5;
    fPressure       : REAL := 101.3;
    fHumidity       : REAL := 45.0;
    sOperator       : STRING(50) := 'John Doe';

    // SQL 문자열
    sInsertCmd      : STRING(512);

    // 제어
    bExecute        : BOOL;
    bBusy           : BOOL;
    bError          : BOOL;
    nErrorId        : UDINT;
END_VAR

// FB_FormatString으로 SQL 문 생성
fbFormatString(
    sFormat := 'INSERT INTO tbl_SensorData VALUES($''%s$'',%f,%f,%f,$''%s$'')',
    arg1 := F_STRING(sProductID),
    arg2 := F_REAL(fTemperature),
    arg3 := F_REAL(fPressure),
    arg4 := F_REAL(fHumidity),
    arg5 := F_STRING(sOperator),
    sOut => sInsertCmd,
    bError => bError
);

// INSERT 실행
fbRecordInsert(
    sNetID := '',
    hDBID := hDBID,
    sInsertCmd := sInsertCmd,
    bExecute := bExecute,
    tTimeout := T#15S,
    bBusy => bBusy,
    bError => bError,
    nErrID => nErrorId
);
```

#### 3.3.4 SELECT 작업

```iecst
PROGRAM PrgDatabaseSelect
VAR
    fbRecordSelect  : FB_DBRecordSelect;

    // 조회할 레코드
    stRecord        : ST_ProductionRecord;
    nRecordIndex    : UDINT := 0;  // 조회할 레코드 인덱스

    // SQL SELECT 문
    sSelectCmd      : STRING(255) := 'SELECT * FROM tbl_Production WHERE QualityOK = 1';

    // 제어
    bExecute        : BOOL;
    bBusy           : BOOL;
    bError          : BOOL;
    nErrorId        : UDINT;

    // 결과
    nTotalRecords   : UDINT;
END_VAR

// 레코드 구조체 정의
TYPE ST_ProductionRecord :
STRUCT
    sProductID      : STRING(20);
    nQuantity       : INT;
    fWeight         : REAL;
    bQualityOK      : BOOL;
    sTimestamp      : STRING(50);
END_STRUCT
END_TYPE

// SELECT 실행
fbRecordSelect(
    sNetID := '',
    hDBID := hDBID,
    sSelectCmd := sSelectCmd,
    nRecordIndex := nRecordIndex,          // 읽을 레코드 인덱스
    cbRecordSize := SIZEOF(stRecord),      // 레코드 크기
    pDestAddr := ADR(stRecord),            // 결과 저장 주소
    bExecute := bExecute,
    tTimeout := T#15S,
    bBusy => bBusy,
    bError => bError,
    nErrID => nErrorId
);

// 결과 사용
IF NOT bBusy AND NOT bError AND bExecute THEN
    // 조회된 데이터 사용
    ProcessProductionData(stRecord);

    // 다음 레코드 조회
    nRecordIndex := nRecordIndex + 1;
END_IF
```

#### 3.3.5 배열로 여러 레코드 조회

```iecst
VAR
    fbRecordArraySelect : FB_DBRecordSelect;

    // 레코드 배열
    astRecords          : ARRAY[0..99] OF ST_ProductionRecord;
    nRecordsRetrieved   : UDINT;
    i                   : UDINT;
END_VAR

// 여러 레코드 조회
FOR i := 0 TO 99 DO
    fbRecordArraySelect(
        sNetID := '',
        hDBID := hDBID,
        sSelectCmd := sSelectCmd,
        nRecordIndex := i,
        cbRecordSize := SIZEOF(ST_ProductionRecord),
        pDestAddr := ADR(astRecords[i]),
        bExecute := TRUE,
        tTimeout := T#15S,
        bBusy => bBusy,
        bError => bError
    );

    // 대기
    WHILE bBusy DO
        ;  // 완료 대기
    END_WHILE

    IF bError THEN
        EXIT;  // 에러 시 중단
    END_IF

    nRecordsRetrieved := i + 1;
END_FOR

// 조회된 레코드 처리
FOR i := 0 TO nRecordsRetrieved - 1 DO
    ProcessRecord(astRecords[i]);
END_FOR
```

### 3.4 Stored Procedure 호출

```iecst
PROGRAM PrgStoredProcedure
VAR
    fbSqlCommand    : FB_SQLCommandEvt;

    // Stored Procedure 파라미터
    nMachineId      : INT := 5;
    dtStartDate     : DT;
    dtEndDate       : DT;

    // SQL 명령
    sStoredProcCmd  : STRING(255);

    // 제어
    bExecute        : BOOL;
    bBusy           : BOOL;
    bError          : BOOL;
END_VAR

// Stored Procedure 호출 문 생성
sStoredProcCmd := CONCAT('EXEC sp_GetProductionReport @MachineID=', INT_TO_STRING(nMachineId));
sStoredProcCmd := CONCAT(sStoredProcCmd, ', @StartDate=$'');
sStoredProcCmd := CONCAT(sStoredProcCmd, DT_TO_STRING(dtStartDate));
sStoredProcCmd := CONCAT(sStoredProcCmd, '$', @EndDate=$'');
sStoredProcCmd := CONCAT(sStoredProcCmd, DT_TO_STRING(dtEndDate));
sStoredProcCmd := CONCAT(sStoredProcCmd, '$'');

// 실행
fbSqlCommand(
    sNetID := '',
    hDBID := hDBID,
    sCmd := sStoredProcCmd,
    bExecute := bExecute,
    tTimeout := T#30S,
    bBusy => bBusy,
    bError => bError
);
```

### 3.5 트랜잭션 처리

```iecst
PROGRAM PrgDatabaseTransaction
VAR
    fbDatabase      : FB_SQLDatabaseEvt;
    fbCommand1      : FB_SQLCommandEvt;
    fbCommand2      : FB_SQLCommandEvt;

    eState          : INT := 0;

    bTransaction    : BOOL := FALSE;  // 트랜잭션 시작
    bCommit         : BOOL := FALSE;
    bRollback       : BOOL := FALSE;
END_VAR

CASE eState OF
    0:  // 대기
        IF bTransaction THEN
            eState := 10;
        END_IF

    10: // BEGIN TRANSACTION
        fbCommand1(
            sCmd := 'BEGIN TRANSACTION',
            bExecute := TRUE
        );
        IF NOT fbCommand1.bBusy THEN
            IF NOT fbCommand1.bError THEN
                eState := 20;
            ELSE
                eState := 999;  // 에러
            END_IF
        END_IF

    20: // 첫 번째 SQL 실행
        fbCommand1(
            sCmd := 'INSERT INTO tbl_Orders ...',
            bExecute := TRUE
        );
        IF NOT fbCommand1.bBusy THEN
            IF NOT fbCommand1.bError THEN
                eState := 30;
            ELSE
                eState := 900;  // Rollback
            END_IF
        END_IF

    30: // 두 번째 SQL 실행
        fbCommand2(
            sCmd := 'UPDATE tbl_Inventory ...',
            bExecute := TRUE
        );
        IF NOT fbCommand2.bBusy THEN
            IF NOT fbCommand2.bError THEN
                eState := 40;
            ELSE
                eState := 900;  // Rollback
            END_IF
        END_IF

    40: // COMMIT
        fbCommand1(
            sCmd := 'COMMIT TRANSACTION',
            bExecute := TRUE
        );
        IF NOT fbCommand1.bBusy THEN
            eState := 0;  // 완료
            bTransaction := FALSE;
        END_IF

    900: // ROLLBACK
        fbCommand1(
            sCmd := 'ROLLBACK TRANSACTION',
            bExecute := TRUE
        );
        IF NOT fbCommand1.bBusy THEN
            eState := 0;
            bTransaction := FALSE;
        END_IF

    999: // 에러 처리
        eState := 0;
        bTransaction := FALSE;
END_CASE
```

### 3.6 사용 사례

1. **생산 데이터 로깅**
   - 실시간 생산량, 품질 데이터 저장
   - Shift 별 통계 조회
   - MES/ERP 연동

2. **Traceability (추적성)**
   - 제품 시리얼 번호 추적
   - 원자재 배치 번호 기록
   - 공정 이력 저장

3. **알람/이벤트 로깅**
   - 기계 고장 이력
   - 작업자 활동 기록
   - 감사(Audit) 로그

---

## 4. TwinCAT Scope

### 4.1 개요

TwinCAT Scope는 실시간 데이터 수집, 시각화 및 분석 도구입니다.

**주요 제품:**
- **TE1300**: TwinCAT Scope View Professional
- **TE1400**: TwinCAT Scope Server

**주요 기능:**
- 실시간 데이터 시각화 (YT/XY 차트)
- 트리거 기반 데이터 수집
- 파형 분석 (FFT, 통계 등)
- 데이터 내보내기 (CSV, MATLAB)

### 4.2 Scope 프로젝트 생성 (GUI)

**단계:**

1. **Measurement Project 생성**
   - Solution Explorer → Add → New Item
   - TwinCAT Measurement Project 선택

2. **Chart 추가**
   - Scope Project → Add Chart
   - Chart Type: YT Chart (시간에 따른 변화)

3. **채널 추가**
   - Chart → Add Axis → Add Acquisition
   - PLC 변수 선택 (예: `MAIN.fTemperature`)

4. **트리거 설정**
   - Trigger Group 추가
   - Condition: `MAIN.bStartRecording = TRUE`
   - Actions: Start Record, Set Mark

5. **실행**
   - Start/Stop 버튼으로 데이터 수집

### 4.3 Automation Interface로 Scope 제어 (C#)

#### 4.3.1 C# 프로젝트 설정

**참조 추가:**
```csharp
// COM 참조
using Beckhoff.TwinCAT.Measurement.Scope;
using TwinCAT.Measurement.Scope.API.Model;

// 어셈블리 참조
using EnvDTE;
using EnvDTE80;
```

#### 4.3.2 Scope 프로젝트 생성 예제

```csharp
using System;
using Beckhoff.TwinCAT.Measurement.Scope;
using TwinCAT.Measurement.Scope.API.Model;

class ScopeAutomation
{
    static void Main()
    {
        // Scope Server 연결
        ScopeServer scopeServer = new ScopeServer();
        scopeServer.Connect("127.0.0.1", 8080);  // 로컬 Scope Server

        // Scope 프로젝트 생성
        ScopeProject project = scopeServer.CreateProject("ProductionMonitoring");

        // Chart 생성
        ScopeChart chart = project.CreateChart("TemperatureChart");
        chart.ChartType = ChartType.YT;

        // Y축 추가
        ScopeYAxis yAxis = chart.CreateYAxis("Temperature [°C]");
        yAxis.MinValue = 0.0;
        yAxis.MaxValue = 100.0;

        // 채널 추가 (PLC 변수)
        ScopeAcquisition acq = yAxis.CreateAcquisition();
        acq.TargetPorts.Add("851");  // ADS Port (PLC)
        acq.SymbolBased = true;
        acq.SymbolName = "MAIN.fTemperature";  // PLC 변수
        acq.Color = System.Drawing.Color.Red;

        // 샘플링 설정
        project.SampleTime = TimeSpan.FromMilliseconds(10);  // 10ms
        project.RecordTime = TimeSpan.FromSeconds(60);       // 60초

        // 트리거 설정
        ScopeTriggerGroup triggerGroup = project.CreateTriggerGroup("StartTrigger");
        ScopeTrigger trigger = triggerGroup.CreateTrigger();
        trigger.Condition = "MAIN.bStartRecording = TRUE";
        trigger.AddAction(TriggerAction.StartRecord);
        trigger.AddAction(TriggerAction.SetMark);

        // 프로젝트 저장
        project.Save(@"C:\Scope\ProductionMonitoring.tcscopex");

        // 시작
        project.Start();

        Console.WriteLine("Scope 프로젝트 시작됨. 종료하려면 Enter 키...");
        Console.ReadLine();

        // 정지 및 데이터 저장
        project.Stop();
        project.ExportData(@"C:\Scope\data.csv", ExportFormat.CSV);

        scopeServer.Disconnect();
    }
}
```

### 4.4 PLC에서 Scope 제어

#### 4.4.1 Scope Control 라이브러리

**라이브러리:**
```iecst
Tc2_Scope  // Scope 제어 라이브러리 (일부 버전)
```

**주요 변수 타입:**
- `SCOPE_TRIGGER`: 트리거 제어

#### 4.4.2 PLC에서 트리거 제어

```iecst
PROGRAM PrgScopeControl
VAR
    // Scope 트리거 (GUI에서 설정한 트리거와 연결)
    bStartRecording     : BOOL;     // Scope 트리거 변수
    bStopRecording      : BOOL;
    bSetMark            : BOOL;     // 마커 설정

    // 제어 로직
    bProductionStart    : BOOL;     // 생산 시작 신호
    bDefectDetected     : BOOL;     // 불량 감지

    // 데이터 (Scope에서 수집)
    fTemperature        : REAL;
    fPressure           : REAL;
    fVibration          : REAL;
END_VAR

// 생산 시작 시 Scope 기록 시작
IF bProductionStart THEN
    bStartRecording := TRUE;
ELSE
    bStartRecording := FALSE;
END_IF

// 불량 감지 시 마커 설정
IF bDefectDetected THEN
    bSetMark := TRUE;
    bDefectDetected := FALSE;  // 리셋
ELSE
    bSetMark := FALSE;
END_IF

// Scope가 수집할 데이터 업데이트
fTemperature := ReadTemperatureSensor();
fPressure := ReadPressureSensor();
fVibration := ReadVibrationSensor();
```

### 4.5 데이터 분석 (FFT, 통계)

Scope View에서 수집한 데이터를 분석할 수 있습니다.

**GUI 분석 도구:**
- **FFT (Fast Fourier Transform)**: 주파수 분석
- **통계**: 평균, 최대/최소, 표준편차
- **Cursor**: 특정 시간 값 읽기
- **Math Channels**: 수식 기반 가상 채널

**C# 코드로 FFT 분석:**
```csharp
// 데이터 로드
ScopeDataSet dataSet = project.LoadData(@"C:\Scope\data.tcscopex");

// FFT 분석
ScopeMathChannel fftChannel = chart.CreateMathChannel("FFT_Vibration");
fftChannel.Expression = "FFT(MAIN.fVibration)";
fftChannel.Calculate();

// 결과 내보내기
fftChannel.ExportData(@"C:\Scope\fft_result.csv");
```

### 4.6 사용 사례

1. **진동 분석**
   - 모터/베어링 진동 수집
   - FFT로 주파수 분석
   - 이상 주파수 감지 → 예측 정비

2. **프로세스 최적화**
   - 온도/압력/유량 데이터 수집
   - 파라미터 변경 전후 비교
   - 최적 조건 도출

3. **품질 분석**
   - 불량 발생 시 데이터 기록
   - 파형 분석으로 원인 규명
   - 재발 방지 대책 수립

---

## 5. TwinCAT Safety

### 5.1 개요

TwinCAT Safety는 기능 안전(Functional Safety) 표준을 준수하는 안전 PLC 기능입니다.

**주요 제품:**
- **TF6705**: TwinCAT Safety PLC

**인증:**
- **IEC 61508**: SIL 3 (Safety Integrity Level 3)
- **EN ISO 13849-1**: PLe, Cat 4
- **IEC 62061**: SIL CL 3

**안전 I/O:**
- EL19xx, EL29xx 시리즈 (TwinSAFE I/O)
- FSoE (Fail-Safe over EtherCAT) 프로토콜

### 5.2 안전 프로그래밍 언어

**지원 언어 (IEC 61508 준수):**
- **FBD (Function Block Diagram)**: 권장 ★★★
- **LD (Ladder Diagram)**: 권장 ★★★
- **ST (Structured Text)**: 권장 ★★ (제한된 서브셋)
- **Safety C**: 고급 응용 (Beckhoff 제공)

**제한 사항:**
- 동적 메모리 할당 금지
- 포인터 사용 제한
- 재귀 함수 금지
- 복잡한 연산 제한

### 5.3 PLCopen Safety Function Blocks

TwinSAFE는 PLCopen Safety 표준 FB를 제공합니다.

**주요 FB:**

| Function Block | 기능 | 용도 |
|----------------|------|------|
| `SF_EmergencyStop` | 비상 정지 | E-Stop 버튼 |
| `SF_TwoHandControlTypeII` | 양손 조작 | 프레스 작업 |
| `SF_TwoHandControlTypeIII` | 양손 조작 (고급) | 정밀 작업 |
| `SF_SafeStop1` | 안전 정지 1 | 제어 정지 후 전원 차단 |
| `SF_SafeStop2` | 안전 정지 2 | 전원 유지 정지 |
| `SF_ESPE` | 전기 민감 보호 장비 | 라이트 커튼, 레이저 스캐너 |
| `SF_ModeSelector` | 모드 선택 | 자동/수동 모드 |
| `SF_Muting` | 뮤팅 | 일시적 안전 기능 비활성화 |
| `SF_EDM` | 외부 장치 모니터링 | 접촉기 피드백 |

### 5.4 안전 프로그램 예제

#### 5.4.1 비상 정지 (Emergency Stop)

```iecst
PROGRAM SafeMain
VAR
    // 안전 입력 (TwinSAFE I/O)
    bEStopButton1       : BOOL;  // E-Stop 버튼 1
    bEStopButton2       : BOOL;  // E-Stop 버튼 2

    // 안전 출력
    bSafeMotorEnable    : BOOL;  // 모터 활성화

    // PLCopen Safety FB
    fbEmergencyStop     : SF_EmergencyStop;

    // 리셋 버튼
    bResetButton        : BOOL;

    // 상태
    bSafetyOK           : BOOL;
    bError              : BOOL;
    nErrorID            : UDINT;
END_VAR

// 비상 정지 FB 호출
fbEmergencyStop(
    Activate := TRUE,
    S_EStopIn := bEStopButton1 AND bEStopButton2,  // 2개 버튼 AND
    S_StartReset := bResetButton,
    S_AutoReset := FALSE,  // 수동 리셋
    Reset := bResetButton,

    // 출력
    Ready => bSafetyOK,
    S_EStopOut => bSafeMotorEnable,
    Error => bError,
    DiagCode => nErrorID
);

// 안전 출력을 물리 출력에 매핑
// (TwinSAFE Logic → TwinSAFE I/O)
```

#### 5.4.2 라이트 커튼 (Light Curtain)

```iecst
PROGRAM SafeLightCurtain
VAR
    // 라이트 커튼 입력
    bLightCurtainOSSD1  : BOOL;  // OSSD 1 (Output Signal Switching Device)
    bLightCurtainOSSD2  : BOOL;  // OSSD 2

    // 뮤팅 센서 (일시 비활성화)
    bMutingSensor1      : BOOL;
    bMutingSensor2      : BOOL;

    // 안전 FB
    fbESPE              : SF_ESPE;
    fbMuting            : SF_Muting;

    // 파라미터
    tMutingTime         : TIME := T#5S;  // 뮤팅 최대 시간

    // 출력
    bSafeZoneOK         : BOOL;
    bMutingActive       : BOOL;
END_VAR

// 뮤팅 FB (제품 통과 시 일시적으로 라이트 커튼 비활성화)
fbMuting(
    Activate := TRUE,
    S_MutingSensor1 := bMutingSensor1,
    S_MutingSensor2 := bMutingSensor2,
    MutingTime := tMutingTime,

    S_MutingActive => bMutingActive
);

// ESPE (라이트 커튼) FB
fbESPE(
    Activate := TRUE,
    S_ESPE_In := bLightCurtainOSSD1 AND bLightCurtainOSSD2,
    S_Muting := bMutingActive,

    S_ESPE_Out => bSafeZoneOK
);

// 안전 영역 OK일 때만 기계 가동
bSafeMotorEnable := bSafeZoneOK AND bSafetyOK;
```

#### 5.4.3 양손 조작 (Two-Hand Control)

```iecst
PROGRAM SafeTwoHandControl
VAR
    // 양손 버튼 입력
    bLeftHandButton     : BOOL;
    bRightHandButton    : BOOL;

    // 안전 FB
    fbTwoHandType3      : SF_TwoHandControlTypeIII;

    // 파라미터
    tSynchronousTime    : TIME := T#500MS;  // 동시 누름 시간 제한

    // 출력
    bPressSafeEnable    : BOOL;  // 프레스 활성화
    bError              : BOOL;
END_VAR

// 양손 조작 Type III (높은 안전 레벨)
fbTwoHandType3(
    Activate := TRUE,
    S_Button1 := bLeftHandButton,
    S_Button2 := bRightHandButton,
    DiscrepancyTime := tSynchronousTime,

    S_TwoHandOut => bPressSafeEnable,
    Error => bError
);

// 프레스는 양손 버튼이 동시에 눌려야만 작동
```

#### 5.4.4 안전 속도 모니터링 (Safe Speed Monitor)

```iecst
PROGRAM SafeSpeedMonitoring
VAR
    // 엔코더 입력
    nEncoderValue       : UDINT;
    fActualSpeed        : REAL;  // [rpm]

    // 안전 FB
    fbSafeSpeedMonitor  : SF_SafelyLimitedSpeed;

    // 파라미터
    fSafeSpeedLimit     : REAL := 10.0;  // 안전 속도 제한 [rpm]
    fTolerance          : REAL := 0.5;   // 허용 오차 [rpm]

    // 출력
    bSpeedOK            : BOOL;
    bOverspeed          : BOOL;
END_VAR

// 엔코더 값으로 속도 계산
fActualSpeed := CalculateSpeed(nEncoderValue);

// 안전 속도 모니터링
fbSafeSpeedMonitor(
    Activate := TRUE,
    S_Velocity := fActualSpeed,
    VelocityLimit := fSafeSpeedLimit,
    Tolerance := fTolerance,

    S_VelocityOK => bSpeedOK,
    S_Overspeed => bOverspeed
);

// 속도 초과 시 안전 정지
IF bOverspeed THEN
    bSafeMotorEnable := FALSE;
END_IF
```

### 5.5 안전 통신 (FSoE)

**FSoE (Fail-Safe over EtherCAT):**
- TwinSAFE I/O와 TwinSAFE Logic 간 안전 통신
- CRC, Sequence Number, Timeout으로 안전성 보장
- Black Channel 원리 (일반 통신 위에 안전 프로토콜)

**설정:**
1. System Manager → Safety → Safety Project
2. Safety Devices 추가 (EL19xx, EL29xx)
3. Safety Logic에서 안전 I/O 매핑
4. Connection 설정 (FSoE Address)

### 5.6 안전 프로젝트 검증

**필수 단계:**
1. **코드 리뷰**: 안전 전문가가 코드 검토
2. **시뮬레이션**: 모든 안전 시나리오 테스트
3. **Validation**: 실제 기계에서 검증
4. **Documentation**: 안전 매뉴얼 작성
5. **Certification**: 인증 기관 승인 (필요 시)

### 5.7 사용 사례

1. **로봇 협동 작업**
   - 라이트 커튼으로 작업자 진입 감지
   - 안전 속도 모니터링
   - 비상 정지 구현

2. **프레스/절단기**
   - 양손 조작 강제
   - 가드 도어 모니터링
   - 안전 거리 계산

3. **컨베이어 시스템**
   - 뮤팅 센서로 제품 통과 허용
   - 다중 비상 정지 버튼
   - 안전 영역 구분

---

## 6. TwinCAT Analytics

### 6.1 개요

TwinCAT Analytics는 데이터 분석, 패턴 인식, 예측 정비를 위한 고급 분석 도구입니다.

**주요 제품:**
- **TF3500**: TwinCAT Analytics (90+ 알고리즘)
- **TF3800**: TwinCAT Machine Learning (ML Inference Engine)
- **TF3810**: TwinCAT Neural Network Inference Engine

**주요 기능:**
- 통계 분석 (평균, 분산, FFT 등)
- 상태 모니터링 (Condition Monitoring)
- 패턴 인식
- 머신러닝 모델 실행 (추론)

### 6.2 Analytics 알고리즘

#### 6.2.1 통계 함수

**라이브러리:**
```iecst
Tc3_Analytics  // Analytics 라이브러리
```

**주요 FB:**

```iecst
PROGRAM PrgAnalyticsStatistics
VAR
    // 입력 데이터 (링 버퍼)
    afDataBuffer        : ARRAY[0..999] OF REAL;
    nBufferIndex        : UDINT := 0;
    fNewValue           : REAL;

    // Analytics FB
    fbMean              : FB_A_Mean;           // 평균
    fbStdDev            : FB_A_StdDev;         // 표준편차
    fbRMS               : FB_A_RMS;            // RMS (Root Mean Square)
    fbPeakToPeak        : FB_A_PeakToPeak;     // Peak-to-Peak
    fbFFT               : FB_A_FFT;            // Fast Fourier Transform

    // 결과
    fMean               : REAL;
    fStdDev             : REAL;
    fRMS                : REAL;
    fPeakToPeak         : REAL;

    // FFT 결과
    afFFTMagnitude      : ARRAY[0..511] OF REAL;
    fDominantFreq       : REAL;  // 지배 주파수 [Hz]

    // 제어
    bCalculate          : BOOL;
END_VAR

// 새 데이터 추가
afDataBuffer[nBufferIndex] := fNewValue;
nBufferIndex := (nBufferIndex + 1) MOD 1000;

// 통계 계산
IF bCalculate THEN
    bCalculate := FALSE;

    // 평균
    fbMean(
        pData := ADR(afDataBuffer),
        nSize := 1000,
        bExecute := TRUE,
        fResult => fMean
    );

    // 표준편차
    fbStdDev(
        pData := ADR(afDataBuffer),
        nSize := 1000,
        bExecute := TRUE,
        fResult => fStdDev
    );

    // RMS
    fbRMS(
        pData := ADR(afDataBuffer),
        nSize := 1000,
        bExecute := TRUE,
        fResult => fRMS
    );

    // Peak-to-Peak
    fbPeakToPeak(
        pData := ADR(afDataBuffer),
        nSize := 1000,
        bExecute := TRUE,
        fResult => fPeakToPeak
    );

    // FFT
    fbFFT(
        pDataIn := ADR(afDataBuffer),
        nSizeIn := 1000,
        pDataOut := ADR(afFFTMagnitude),
        nSizeOut := 512,
        fSamplingRate := 1000.0,  // 1 kHz
        bExecute := TRUE,
        fDominantFrequency => fDominantFreq
    );
END_IF
```

#### 6.2.2 조건 모니터링 (Condition Monitoring)

```iecst
PROGRAM PrgConditionMonitoring
VAR
    // 센서 데이터
    fVibration          : REAL;  // 진동 [mm/s]
    fTemperature        : REAL;  // 베어링 온도 [°C]
    fCurrent            : REAL;  // 모터 전류 [A]

    // Analytics FB
    fbTrendAnalysis     : FB_A_TrendDetection;
    fbAnomalyDetection  : FB_A_AnomalyDetection;

    // 임계값
    fVibrationNormal    : REAL := 2.0;   // 정상 진동 [mm/s]
    fVibrationWarning   : REAL := 5.0;   // 경고 진동 [mm/s]
    fVibrationAlarm     : REAL := 10.0;  // 알람 진동 [mm/s]

    // 상태
    eHealthStatus       : E_HealthStatus;
    bWarning            : BOOL;
    bAlarm              : BOOL;
    fRemainingLife      : REAL;  // 남은 수명 [%]
END_VAR

TYPE E_HealthStatus :
(
    HEALTH_GOOD := 0,
    HEALTH_WARNING := 1,
    HEALTH_CRITICAL := 2,
    HEALTH_FAILURE := 3
);
END_TYPE

// 진동 레벨 평가
IF fVibration < fVibrationWarning THEN
    eHealthStatus := HEALTH_GOOD;
ELSIF fVibration < fVibrationAlarm THEN
    eHealthStatus := HEALTH_WARNING;
    bWarning := TRUE;

    // 예측 정비 알림
    SendMaintenanceAlert('Vibration Warning', fVibration);
ELSE
    eHealthStatus := HEALTH_CRITICAL;
    bAlarm := TRUE;

    // 긴급 정지
    EmergencyStop();
END_IF

// 트렌드 분석 (점진적 악화 감지)
fbTrendAnalysis(
    fValue := fVibration,
    fThreshold := fVibrationNormal,
    tWindow := T#1H,  // 1시간 윈도우
    bExecute := TRUE,

    eTrend => eTrend,  // INCREASING, DECREASING, STABLE
    fSlope => fSlope   // 기울기
);

// 남은 수명 예측 (단순 선형 모델)
IF eTrend = TREND_INCREASING AND fSlope > 0.0 THEN
    fRemainingLife := (fVibrationAlarm - fVibration) / fSlope;  // [시간]
    fRemainingLife := fRemainingLife / 24.0;  // [일]
ELSE
    fRemainingLife := 999.0;  // 충분
END_IF
```

### 6.3 Machine Learning (TF3800)

TwinCAT Machine Learning은 학습된 ML 모델을 PLC에서 실행(Inference)합니다.

**지원 알고리즘:**
- **SVM (Support Vector Machine)**: 분류, 회귀
- **Decision Tree**: 규칙 기반 분류
- **Random Forest**: 앙상블 분류
- **PCA (Principal Component Analysis)**: 차원 축소, 이상 감지
- **K-Means**: 클러스터링

#### 6.3.1 ML 모델 학습 (외부 도구)

ML 모델은 일반적으로 Python (scikit-learn) 등으로 학습 후 ONNX 또는 TwinCAT 포맷으로 변환합니다.

**Python 예제 (scikit-learn):**
```python
from sklearn import svm
from sklearn.externals import joblib
import numpy as np

# 학습 데이터 (예: 정상/불량 분류)
X_train = np.array([
    [2.0, 25.0, 10.0],  # [진동, 온도, 전류]
    [2.1, 26.0, 10.5],
    # ... 정상 데이터
    [12.0, 80.0, 25.0],  # 불량 데이터
    [11.5, 78.0, 24.0],
    # ...
])

y_train = np.array([0, 0, 0, 1, 1, 1])  # 0: 정상, 1: 불량

# SVM 모델 학습
clf = svm.SVC(kernel='rbf', gamma=0.1, C=1.0)
clf.fit(X_train, y_train)

# 모델 저장
joblib.dump(clf, 'defect_detection_model.pkl')

# TwinCAT 포맷으로 변환 (Beckhoff 도구 사용)
# convert_to_twincat_format('defect_detection_model.pkl', 'model.xml')
```

#### 6.3.2 PLC에서 ML 모델 실행

```iecst
PROGRAM PrgMachineLearning
VAR
    // ML 모델
    fbMLModel           : FB_ML_SVM;
    sModelPath          : STRING := 'C:\TwinCAT\ML\defect_model.xml';

    // 입력 특징 (Features)
    afFeatures          : ARRAY[0..2] OF REAL;  // [진동, 온도, 전류]

    // 출력
    nPrediction         : INT;       // 0: 정상, 1: 불량
    fConfidence         : REAL;      // 신뢰도 (0.0~1.0)

    // 제어
    bPredict            : BOOL;
    bBusy               : BOOL;
    bError              : BOOL;
END_VAR

// 모델 로드 (최초 1회)
IF NOT fbMLModel.bModelLoaded THEN
    fbMLModel.LoadModel(sModelPath);
END_IF

// 센서 데이터를 특징 배열에 저장
afFeatures[0] := fVibration;
afFeatures[1] := fTemperature;
afFeatures[2] := fCurrent;

// 예측 수행
IF bPredict AND fbMLModel.bModelLoaded THEN
    bPredict := FALSE;

    fbMLModel(
        pFeatures := ADR(afFeatures),
        nNumFeatures := 3,
        bExecute := TRUE,

        bBusy => bBusy,
        nPrediction => nPrediction,
        fConfidence => fConfidence,
        bError => bError
    );
END_IF

// 예측 결과 활용
IF NOT bBusy AND NOT bError THEN
    IF nPrediction = 1 AND fConfidence > 0.9 THEN
        // 불량 예측 (90% 이상 신뢰도)
        TriggerReject();
        LogDefect(afFeatures, fConfidence);
    END_IF
END_IF
```

### 6.4 Neural Network Inference (TF3810)

딥러닝 모델(CNN, RNN 등)을 PLC에서 실행합니다.

**지원 포맷:**
- ONNX (Open Neural Network Exchange)
- TensorFlow Lite

#### 6.4.1 이미지 분류 (CNN 예제)

```iecst
PROGRAM PrgNeuralNetworkInference
VAR
    // Neural Network FB
    fbNeuralNet         : FB_NN_Inference;
    sONNXModelPath      : STRING := 'C:\TwinCAT\NN\cnn_defect.onnx';

    // 입력 이미지 (예: 64x64 그레이스케일)
    aImageData          : ARRAY[0..4095] OF BYTE;  // 64*64 = 4096

    // 출력 (클래스 확률)
    afClassProbabilities: ARRAY[0..1] OF REAL;  // [정상 확률, 불량 확률]

    // 결과
    nPredictedClass     : INT;
    fMaxProbability     : REAL;

    // 제어
    bInference          : BOOL;
    bBusy               : BOOL;
    bError              : BOOL;
END_VAR

// 모델 로드
IF NOT fbNeuralNet.bModelLoaded THEN
    fbNeuralNet.LoadONNXModel(sONNXModelPath);
END_IF

// TwinCAT Vision에서 이미지 가져오기
// (이미지 → BYTE 배열 변환)
ConvertImageToByteArray(ipImageIn, ADR(aImageData));

// 추론 실행
IF bInference AND fbNeuralNet.bModelLoaded THEN
    bInference := FALSE;

    fbNeuralNet(
        pInputData := ADR(aImageData),
        nInputSize := 4096,
        pOutputData := ADR(afClassProbabilities),
        nOutputSize := 2,
        bExecute := TRUE,

        bBusy => bBusy,
        bError => bError
    );
END_IF

// 결과 해석
IF NOT bBusy AND NOT bError THEN
    IF afClassProbabilities[0] > afClassProbabilities[1] THEN
        nPredictedClass := 0;  // 정상
        fMaxProbability := afClassProbabilities[0];
    ELSE
        nPredictedClass := 1;  // 불량
        fMaxProbability := afClassProbabilities[1];
    END_IF

    // 불량 판정
    IF nPredictedClass = 1 AND fMaxProbability > 0.95 THEN
        TriggerReject();
    END_IF
END_IF
```

### 6.5 사용 사례

1. **예측 정비 (Predictive Maintenance)**
   - 진동/온도 데이터 분석
   - ML 모델로 고장 예측
   - 정비 일정 최적화

2. **품질 예측**
   - 공정 파라미터로 품질 예측
   - 불량 발생 전 조치
   - 수율 향상

3. **에너지 최적화**
   - 전력 소비 패턴 분석
   - ML로 최적 운전 조건 학습
   - 에너지 절감

---

## 7. TwinCAT Automation Interface

### 7.1 개요

TwinCAT Automation Interface는 .NET/C# 코드로 TwinCAT 프로젝트를 자동으로 생성, 수정, 빌드, 배포하는 기능입니다.

**주요 용도:**
- 프로젝트 템플릿 자동 생성
- 대량의 I/O 자동 설정
- CI/CD 파이프라인 구축
- 프로젝트 일괄 업데이트

**필수 요소:**
- Visual Studio Shell (또는 Visual Studio)
- EnvDTE (Visual Studio Automation)
- TCatSysManagerLib (TwinCAT Automation Interface)

### 7.2 C# 프로젝트 설정

#### 7.2.1 참조 추가

**COM 참조:**
```
- EnvDTE (Visual Studio DTE)
- EnvDTE80
- Beckhoff TwinCAT XAE Base 3.x Type Library
```

**NuGet 패키지 (선택적):**
```
Beckhoff.TwinCAT.Ads  // ADS 통신
```

#### 7.2.2 기본 코드 구조

```csharp
using System;
using EnvDTE;
using EnvDTE80;
using TCatSysManagerLib;

namespace TwinCATAutomation
{
    class Program
    {
        static void Main(string[] args)
        {
            // 1. Visual Studio DTE 생성
            Type t = Type.GetTypeFromProgID("TcXaeShell.DTE.15.0");
            DTE2 dte = (DTE2)Activator.CreateInstance(t);
            dte.SuppressUI = false;
            dte.MainWindow.Visible = true;

            // 2. TwinCAT 프로젝트 생성
            Solution sln = dte.Solution;
            string projectPath = @"C:\Projects\AutomatedProject.tsproj";
            sln.Create(@"C:\Projects", "AutomatedProject");

            // 3. TwinCAT 프로젝트 항목 추가
            Project project = sln.AddFromTemplate(
                @"C:\TwinCAT\3.1\Components\Base\PrjTemplate\TwinCAT Project.tsproj",
                @"C:\Projects\AutomatedProject",
                "AutomatedProject.tsproj"
            );

            // 4. System Manager 액세스
            ITcSysManager sysManager = (ITcSysManager)project.Object;

            // 5. PLC 프로젝트 추가
            ITcSmTreeItem plc = sysManager.LookupTreeItem("TIPC");
            ITcPlcProject plcProject = (ITcPlcProject)plc.CreateChild(
                "PLC_Project",
                0,   // subtype
                "",  // additional info
                ""   // PLC template path
            );

            // 6. PLC 프로그램 추가
            CreatePlcProgram(plcProject);

            // 7. I/O 설정
            ConfigureIO(sysManager);

            // 8. 프로젝트 저장 및 활성화
            sln.SaveAs(projectPath);
            sysManager.ActivateConfiguration();

            // 9. 빌드
            sln.SolutionBuild.Build(true);

            Console.WriteLine("프로젝트 생성 및 빌드 완료!");

            // DTE 종료 (선택)
            // dte.Quit();
        }

        static void CreatePlcProgram(ITcPlcProject plcProject)
        {
            // POU (Program Organization Unit) 추가
            // 다음 섹션에서 설명
        }

        static void ConfigureIO(ITcSysManager sysManager)
        {
            // I/O 스캔 및 설정
            // 다음 섹션에서 설명
        }
    }
}
```

### 7.3 PLC 프로그램 자동 생성

#### 7.3.1 POU (Function Block) 추가

```csharp
using System.Xml.Linq;

static void CreatePlcProgram(ITcPlcProject plcProject)
{
    // POUs 폴더
    ITcSmTreeItem pousFolder = plcProject.LookupChild("POUs");

    // MAIN 프로그램 생성
    ITcSmTreeItem mainPou = pousFolder.CreateChild(
        "MAIN",
        610,  // Subtype for PROGRAM
        "",
        CreateMainProgramXml()
    );

    // Function Block 생성
    ITcSmTreeItem fbMotorControl = pousFolder.CreateChild(
        "FB_MotorControl",
        611,  // Subtype for FUNCTION_BLOCK
        "",
        CreateFunctionBlockXml("FB_MotorControl")
    );

    Console.WriteLine("PLC 프로그램 생성 완료");
}

static string CreateMainProgramXml()
{
    // PLCopen XML 포맷
    string xml = @"
    <Declaration><![CDATA[
    PROGRAM MAIN
    VAR
        fbMotor : FB_MotorControl;
        bStart : BOOL;
        fSpeed : REAL;
    END_VAR
    ]]></Declaration>
    <Implementation>
    <ST><![CDATA[
    // 메인 프로그램
    fbMotor(
        bEnable := bStart,
        fTargetSpeed := fSpeed
    );
    ]]></ST>
    </Implementation>";

    return xml;
}

static string CreateFunctionBlockXml(string fbName)
{
    string xml = $@"
    <Declaration><![CDATA[
    FUNCTION_BLOCK {fbName}
    VAR_INPUT
        bEnable : BOOL;         // 활성화
        fTargetSpeed : REAL;    // 목표 속도 [rpm]
    END_VAR
    VAR_OUTPUT
        fActualSpeed : REAL;    // 현재 속도 [rpm]
        bRunning : BOOL;        // 가동 상태
    END_VAR
    VAR
        fAcceleration : REAL := 10.0;  // 가속도 [rpm/s]
    END_VAR
    ]]></Declaration>
    <Implementation>
    <ST><![CDATA[
    // 모터 제어 로직
    IF bEnable THEN
        IF fActualSpeed < fTargetSpeed THEN
            fActualSpeed := fActualSpeed + fAcceleration * 0.001;  // 1ms 사이클
        END_IF
        bRunning := TRUE;
    ELSE
        fActualSpeed := 0.0;
        bRunning := FALSE;
    END_IF
    ]]></ST>
    </Implementation>";

    return xml;
}
```

#### 7.3.2 GVL (Global Variable List) 추가

```csharp
static void CreateGlobalVariables(ITcPlcProject plcProject)
{
    ITcSmTreeItem gvlsFolder = plcProject.LookupChild("GVLs");

    ITcSmTreeItem gvl = gvlsFolder.CreateChild(
        "GVL_System",
        615,  // Subtype for GVL
        "",
        CreateGvlXml()
    );
}

static string CreateGvlXml()
{
    string xml = @"
    <Declaration><![CDATA[
    VAR_GLOBAL
        gSystemStatus : INT := 0;       // 시스템 상태
        gProductionCount : UDINT := 0;  // 생산 카운트
        gAlarmCode : UINT := 0;         // 알람 코드
    END_VAR
    ]]></Declaration>";

    return xml;
}
```

### 7.4 I/O 자동 설정

#### 7.4.1 EtherCAT 디바이스 스캔

```csharp
static void ConfigureIO(ITcSysManager sysManager)
{
    // I/O Devices 노드
    ITcSmTreeItem ioDevices = sysManager.LookupTreeItem("TIID");

    // EtherCAT 마스터 추가
    ITcSmTreeItem etherCATMaster = ioDevices.CreateChild(
        "EtherCAT Master",
        111,  // Device type for EtherCAT
        "",
        ""
    );

    // 디바이스 스캔
    ITcSmTreeItem devices = etherCATMaster.LookupChild("Devices");
    devices.ConsumeXml("<TreeItem><DeviceGrpType>111</DeviceGrpType></TreeItem>");

    Console.WriteLine("EtherCAT 스캔 시작...");

    // 스캔된 디바이스 확인
    foreach (ITcSmTreeItem device in devices)
    {
        string deviceName = device.Name;
        Console.WriteLine($"발견된 디바이스: {deviceName}");

        // 디바이스 활성화
        device.ConsumeXml("<TreeItem><ItemState>2</ItemState></TreeItem>");  // 2 = Enabled
    }
}
```

#### 7.4.2 I/O 링크 자동 생성

```csharp
static void LinkIOToPLC(ITcSysManager sysManager, ITcPlcProject plcProject)
{
    // I/O 터미널 찾기 (예: EL1008 디지털 입력)
    ITcSmTreeItem el1008 = sysManager.LookupTreeItem("TIID^EtherCAT Master^Term 1 (EL1008)");

    // PLC 인스턴스
    ITcSmTreeItem plcTask = sysManager.LookupTreeItem("TIRT^PlcTask");

    // 입력 채널 링크
    for (int i = 1; i <= 8; i++)
    {
        ITcSmTreeItem channel = el1008.LookupChild($"Channel {i}");
        ITcSmTreeItem inputVar = channel.LookupChild("Input");

        // PLC 변수에 링크
        string plcVarPath = $"MAIN.bInput{i}";
        inputVar.CreateChild(plcVarPath, 0, "", $"<TreeItem><Link>{plcVarPath}</Link></TreeItem>");

        Console.WriteLine($"EL1008 Ch{i} → {plcVarPath} 링크 완료");
    }
}
```

### 7.5 프로젝트 빌드 및 배포

#### 7.5.1 빌드

```csharp
static void BuildProject(DTE2 dte)
{
    Console.WriteLine("프로젝트 빌드 중...");

    dte.Solution.SolutionBuild.Build(true);  // true = Wait for completion

    // 빌드 결과 확인
    if (dte.Solution.SolutionBuild.LastBuildInfo == 0)
    {
        Console.WriteLine("빌드 성공!");
    }
    else
    {
        Console.WriteLine($"빌드 실패! 에러 수: {dte.Solution.SolutionBuild.LastBuildInfo}");
    }
}
```

#### 7.5.2 활성화 (Activate Configuration)

```csharp
static void ActivateConfiguration(ITcSysManager sysManager)
{
    Console.WriteLine("Configuration 활성화 중...");

    sysManager.ActivateConfiguration();

    Console.WriteLine("활성화 완료!");
}
```

#### 7.5.3 원격 PLC에 배포

```csharp
using TwinCAT.Ads;

static void DeployToRemotePLC(string amsNetId, string projectPath)
{
    // ADS 연결
    using (AdsClient client = new AdsClient())
    {
        client.Connect(amsNetId, 10000);  // Port 10000 = System Service

        // 프로젝트 업로드
        // (이 부분은 TwinCAT XAE Shell을 통해 수행하는 것이 일반적)

        Console.WriteLine($"프로젝트를 {amsNetId}에 배포 완료");
    }
}
```

### 7.6 CI/CD 파이프라인 예제

#### 7.6.1 PowerShell 스크립트

```powershell
# build-and-deploy.ps1

param(
    [string]$ProjectPath = "C:\Projects\MyProject.sln",
    [string]$TargetAmsNetId = "192.168.1.100.1.1"
)

# TwinCAT Shell 실행
$dte = New-Object -ComObject "TcXaeShell.DTE.15.0"
$dte.SuppressUI = $true

# 프로젝트 열기
$dte.Solution.Open($ProjectPath)

# 빌드
$dte.Solution.SolutionBuild.Build($true)

if ($dte.Solution.SolutionBuild.LastBuildInfo -eq 0) {
    Write-Host "빌드 성공!"

    # 활성화
    $project = $dte.Solution.Projects.Item(1)
    $sysManager = $project.Object
    $sysManager.ActivateConfiguration()

    Write-Host "배포 완료!"
} else {
    Write-Host "빌드 실패!"
    exit 1
}

# DTE 종료
$dte.Quit()
```

#### 7.6.2 Azure DevOps / Jenkins 통합

**Azure Pipelines YAML:**
```yaml
trigger:
- main

pool:
  vmImage: 'windows-latest'

steps:
- task: PowerShell@2
  inputs:
    filePath: 'build-and-deploy.ps1'
    arguments: '-ProjectPath "$(Build.SourcesDirectory)\MyProject.sln"'
  displayName: 'Build TwinCAT Project'

- task: PublishBuildArtifacts@1
  inputs:
    PathtoPublish: '$(Build.SourcesDirectory)\MyProject\_CompileInfo'
    ArtifactName: 'twincat-build'
  displayName: 'Publish Build Artifacts'
```

### 7.7 사용 사례

1. **대규모 프로젝트 템플릿**
   - 수백 개의 I/O 자동 설정
   - 표준 Function Block 라이브러리 자동 추가
   - 일관된 프로젝트 구조 생성

2. **프로젝트 일괄 업데이트**
   - 100개 프로젝트의 라이브러리 버전 일괄 업그레이드
   - 공통 FB 코드 자동 패치

3. **CI/CD**
   - Git 커밋 시 자동 빌드
   - 테스트 환경 자동 배포
   - 버전 관리 및 릴리스 자동화

---

## 부록 A: 참고 자료

### 공식 문서
- **Beckhoff Information System**: https://infosys.beckhoff.com
- **TwinCAT 3 Documentation**: https://www.beckhoff.com/twincat3

### GitHub 샘플
- **TF7xxx Vision Samples**: https://github.com/Beckhoff/TF7xxx_Samples
- **TwinCAT Automation Interface Examples**: https://github.com/Beckhoff-USA-Community

### 커뮤니티
- **Beckhoff Community**: https://www.plctalk.net
- **AllTwinCAT Blog**: https://alltwincat.com
- **Contact and Coil**: https://www.contactandcoil.com

---

## 부록 B: 라이선스 정보

각 TwinCAT 기능은 별도의 라이선스가 필요합니다:

| 제품 코드 | 제품명 | 7일 체험판 |
|----------|--------|-----------|
| TF6100 | OPC UA | ✓ |
| TF6420 | Database Server | ✓ |
| TF6701 | IoT Communication (MQTT) | ✓ |
| TF7000 | Vision Base | ✓ |
| TF7100 | Vision Matching 2D | ✓ |
| TE1300 | Scope View Professional | ✓ |
| TF3500 | Analytics | ✓ |
| TF3800 | Machine Learning | ✓ |

**라이선스 활성화:**
1. Beckhoff 웹사이트에서 라이선스 구매
2. TwinCAT System Manager → License → Manage Licenses
3. 라이선스 파일 임포트 또는 온라인 활성화

---

## 부록 C: 문제 해결 (Troubleshooting)

### MQTT 연결 실패
```
증상: fbMqttClient.bError = TRUE, nErrorId = 0x00000001
원인: 브로커 연결 불가
해결:
  - 브로커 IP/Port 확인
  - 방화벽 설정 확인 (1883 포트)
  - 브로커 로그 확인
```

### Vision 이미지 취득 안 됨
```
증상: fbCamera.GetState() = TCVN_CS_ERROR
원인: 카메라 연결 문제
해결:
  - GigE Vision 카메라: IP 주소 확인
  - 네트워크 어댑터 Jumbo Frame 설정 (9000 bytes)
  - 카메라 전원 확인
```

### Database 연결 실패
```
증상: fbDatabase.bError = TRUE
원인: SQL Server 연결 불가
해결:
  - SQL Server 서비스 실행 확인
  - SQL Server Browser 활성화
  - TCP/IP 프로토콜 활성화
  - 방화벽 1433 포트 개방
```

### Automation Interface DTE 생성 실패
```
증상: Type.GetTypeFromProgID() returns null
원인: TwinCAT XAE Shell 미설치
해결:
  - TwinCAT XAE (또는 Visual Studio Shell) 설치
  - ProgID 버전 확인 ("TcXaeShell.DTE.15.0")
```

---

## 결론

TwinCAT 3의 고급 API들은 현대 산업 자동화의 요구사항을 충족하는 강력한 도구입니다:

- **IoT**: 클라우드와 실시간 연결
- **Vision**: 품질 검사 및 로봇 비전
- **Database**: 생산 데이터 관리
- **Scope**: 데이터 수집 및 분석
- **Safety**: 안전 시스템 구축
- **Analytics/ML**: 예측 정비 및 최적화
- **Automation Interface**: 프로젝트 자동화

이러한 기술들을 조합하여 스마트 팩토리와 Industry 4.0을 구현할 수 있습니다.

---

**문서 버전**: 1.0
**작성일**: 2025-11-24
**작성자**: Claude Code AI Assistant
