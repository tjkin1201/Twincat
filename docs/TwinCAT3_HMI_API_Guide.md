# TwinCAT 3 HMI API 가이드

## 목차
1. [개요](#개요)
2. [TwinCAT HMI Framework](#twincat-hmi-framework)
3. [JavaScript API 및 확장 기능](#javascript-api-및-확장-기능)
4. [서버 확장 및 커스텀 컨트롤](#서버-확장-및-커스텀-컨트롤)
5. [심볼 바인딩 및 데이터 통신](#심볼-바인딩-및-데이터-통신)
6. [이벤트 처리 및 사용자 관리](#이벤트-처리-및-사용자-관리)
7. [실전 예제](#실전-예제)

---

## 개요

TwinCAT HMI는 HTML5 기반의 현대적인 산업용 HMI 솔루션으로, 웹 기술(HTML5, JavaScript, CSS)을 활용하여 플랫폼 독립적인 사용자 인터페이스를 제공합니다.

### 주요 구성 요소

- **TE2000**: TwinCAT 3 HMI Engineering (개발 환경)
- **TF2000**: TwinCAT 3 HMI Server (런타임 서버)
- **TF2200**: TwinCAT 3 HMI Extension SDK (확장 개발 도구)

### 아키텍처

```
┌─────────────────────────────────────┐
│   HMI Client (Browser)              │
│   - HTML5/CSS3                      │
│   - JavaScript/TypeScript           │
│   - Framework API                   │
└──────────────┬──────────────────────┘
               │ WebSocket (JSON)
┌──────────────▼──────────────────────┐
│   HMI Server                        │
│   - Server Extensions (.NET/Python) │
│   - Symbol Management               │
│   - User Management                 │
│   - Event System                    │
└──────────────┬──────────────────────┘
               │ ADS Protocol
┌──────────────▼──────────────────────┐
│   TwinCAT PLC Runtime               │
│   - PLC Variables                   │
│   - Functions                       │
└─────────────────────────────────────┘
```

### 최신 버전 정보

- **Framework Version**: 14.3.277 (NuGet)
- **TypeScript 지원**: Version 1.12부터
- **통신 프로토콜**: ADS, OPC UA, Server-to-Server

---

## TwinCAT HMI Framework

### Framework API 기본

TwinCAT HMI Framework는 클라이언트 측에서 사용할 수 있는 포괄적인 JavaScript/TypeScript API를 제공합니다.

#### Framework 네임스페이스 구조

```javascript
TcHmi
├── Controls              // 컨트롤 관리
├── Symbol                // 심볼 처리
├── Server                // 서버 통신
├── EventProvider         // 이벤트 시스템
├── Functions             // 함수 등록 및 실행
├── Locale                // 다국어 지원
└── System                // 시스템 유틸리티
```

### 컨트롤 참조 가져오기

```javascript
// ID로 컨트롤 참조 가져오기
var button = TcHmi.Controls.get('TcHmiButton_1');

if (button) {
    // 컨트롤이 존재하는 경우
    console.log('버튼 컨트롤을 찾았습니다.');
}
```

### 컨트롤 속성 제어

```javascript
// 컨트롤 속성 설정
var textBox = TcHmi.Controls.get('TcHmiTextbox_1');

// 텍스트 설정
textBox.setText('새로운 텍스트');

// 텍스트 가져오기
var currentText = textBox.getText();

// 가시성 제어
textBox.setVisibility(TcHmi.Visibility.Visible);
textBox.setVisibility(TcHmi.Visibility.Hidden);
textBox.setVisibility(TcHmi.Visibility.Collapsed);

// 활성화/비활성화
textBox.setIsEnabled(false);

// 색상 설정
textBox.setTextColor({
    color: 'rgba(255, 0, 0, 1)'  // 빨간색
});

textBox.setBackgroundColor({
    color: '#00FF00'  // 녹색
});
```

### 주요 Framework 컨트롤

#### TcHmiButton

```javascript
var button = TcHmi.Controls.get('TcHmiButton_1');

// 버튼 텍스트 설정
button.setText('시작');

// 아이콘 설정
button.setIcon('Images/Icons/Start.png');

// 텍스트 정렬
button.setTextHorizontalAlignment(TcHmi.HorizontalAlignment.Center);
button.setTextVerticalAlignment(TcHmi.VerticalAlignment.Center);

// 클릭 이벤트 핸들러 (별도 섹션 참조)
```

#### TcHmiTextbox

```javascript
var textbox = TcHmi.Controls.get('TcHmiTextbox_1');

// 텍스트 설정/가져오기
textbox.setText('입력값');
var value = textbox.getText();

// 타입 설정
textbox.setContentPadding({
    left: 5,
    right: 5,
    top: 3,
    bottom: 3
});

// 최대 길이 제한
textbox.setMaxLength(50);

// 읽기 전용 설정
textbox.setIsReadOnly(true);

// 플레이스홀더 설정
textbox.setPlaceholder('여기에 입력하세요...');
```

#### TcHmiNumericInput

```javascript
var numericInput = TcHmi.Controls.get('TcHmiNumericInput_1');

// 값 설정
numericInput.setValue(123.45);

// 값 가져오기
var value = numericInput.getValue();

// 범위 설정
numericInput.setMinValue(0.0);
numericInput.setMaxValue(1000.0);

// 소수점 자릿수
numericInput.setDecimalDigits(2);

// 단위 표시
numericInput.setUnit('°C');
```

#### TcHmiLinearGauge

```javascript
var gauge = TcHmi.Controls.get('TcHmiLinearGauge_1');

// 현재 값 설정
gauge.setValue(75.0);

// 범위 설정
gauge.setMinValue(0.0);
gauge.setMaxValue(100.0);

// 눈금 설정
gauge.setMainTickRange(20);
gauge.setSubTickRange(5);

// 색상 범위 설정 (예: 정상/경고/위험)
gauge.setRange([
    {
        start: 0,
        end: 60,
        color: { color: '#00FF00' }  // 녹색 (정상)
    },
    {
        start: 60,
        end: 80,
        color: { color: '#FFFF00' }  // 노란색 (경고)
    },
    {
        start: 80,
        end: 100,
        color: { color: '#FF0000' }  // 빨간색 (위험)
    }
]);
```

#### TcHmiTrendLineChart

```javascript
var chart = TcHmi.Controls.get('TcHmiTrendLineChart_1');

// Y축 설정
chart.setYAxis([
    {
        id: 'yAxis1',
        position: 'Left',
        mainTickRange: 20,
        autoScaling: true,
        decimalPlaces: 1
    }
]);

// 트렌드 라인 추가
chart.setLines([
    {
        symbol: '%s%PLC1.Temperature%/s%',
        yAxisId: 'yAxis1',
        lineColor: { color: 'rgba(255, 0, 0, 1)' },
        lineWidth: 2,
        pointDot: false
    },
    {
        symbol: '%s%PLC1.Pressure%/s%',
        yAxisId: 'yAxis1',
        lineColor: { color: 'rgba(0, 0, 255, 1)' },
        lineWidth: 2,
        pointDot: false
    }
]);

// 시간 범위 설정
chart.setInterval(60000);  // 60초
```

---

## JavaScript API 및 확장 기능

### TypeScript 지원

TwinCAT HMI는 버전 1.12부터 TypeScript를 지원합니다. TypeScript 코드는 자동으로 JavaScript로 변환됩니다.

#### TypeScript 함수 예시

```typescript
// TypeScript로 작성된 함수
module TcHmi {
    export module Functions {
        export module UserFunctions {
            export function calculateTotal(
                price: number,
                quantity: number,
                taxRate: number
            ): number {
                // 가격 계산 로직
                const subtotal = price * quantity;
                const tax = subtotal * (taxRate / 100);
                return subtotal + tax;
            }
        }
    }
}
```

### 사용자 정의 함수 등록

#### registerFunctionEx (v1.12 이상)

```javascript
(function () {
    // 함수 정의
    function myCustomFunction(param1, param2) {
        // 함수 로직
        console.log('파라미터 1:', param1);
        console.log('파라미터 2:', param2);

        var result = param1 + param2;
        return result;
    }

    // Framework에 함수 등록
    TcHmi.Functions.registerFunctionEx(
        'MyCustomFunction',           // 함수 이름
        'TcHmi.Functions.UserFunctions', // 네임스페이스
        myCustomFunction              // 함수 참조
    );
})();
```

#### 함수 호출

```javascript
// 등록된 함수 호출
TcHmi.Functions.UserFunctions.MyCustomFunction(10, 20);

// 또는 동적 호출
var fnName = 'TcHmi.Functions.UserFunctions.MyCustomFunction';
var result = TcHmi.System.callFunction(fnName, [10, 20]);
```

### Browser API 활용

TwinCAT HMI는 표준 웹 브라우저 환경에서 실행되므로, 모든 브라우저 API를 사용할 수 있습니다.

```javascript
// Local Storage 사용
function saveUserPreference(key, value) {
    localStorage.setItem(key, JSON.stringify(value));
}

function loadUserPreference(key) {
    var data = localStorage.getItem(key);
    return data ? JSON.parse(data) : null;
}

// Web Workers (백그라운드 작업)
var worker = new Worker('worker.js');
worker.postMessage({ cmd: 'calculate', data: [1, 2, 3, 4, 5] });

worker.onmessage = function(e) {
    console.log('계산 결과:', e.data);
};

// Fetch API (외부 데이터 가져오기)
fetch('https://api.example.com/data')
    .then(response => response.json())
    .then(data => {
        console.log('데이터:', data);
    })
    .catch(error => {
        console.error('오류:', error);
    });

// Canvas API (그래픽 그리기)
var canvas = document.getElementById('myCanvas');
var ctx = canvas.getContext('2d');
ctx.fillStyle = 'red';
ctx.fillRect(10, 10, 100, 100);
```

### 다국어 지원 (Localization)

```javascript
// 로케일 설정
TcHmi.Locale.setCurrentLocale('ko-KR');

// 현재 로케일 가져오기
var currentLocale = TcHmi.Locale.getCurrentLocale();

// 번역 문자열 사용 (Engineering에서 정의)
// %lt%KeyName%/lt% 형식으로 사용
```

**localization.json 예시:**

```json
{
  "locale": "ko-KR",
  "localizedTexts": {
    "StartButton": "시작",
    "StopButton": "정지",
    "Temperature": "온도",
    "Pressure": "압력",
    "ErrorMessage": "오류가 발생했습니다."
  }
}
```

---

## 서버 확장 및 커스텀 컨트롤

### 서버 확장 개요

TwinCAT HMI Server는 모듈식 구조로 설계되어 서버 확장을 통해 기능을 확장할 수 있습니다.

### 서버 확장 개발 (.NET)

#### 요구사항

- **TF2200**: TwinCAT 3 HMI Extension SDK 라이선스
- **Visual Studio**: Full Version (Community Edition 가능)
- **개발 언어**: C# (.NET Framework 또는 .NET Core)

#### 기본 서버 확장 구조

```csharp
using System;
using TcHmiSrv.Core;
using TcHmiSrv.Core.General;
using TcHmiSrv.Core.Listeners;
using TcHmiSrv.Core.Tools.Management;

namespace MyCustomExtension
{
    // TcHmiSrv.Core.ServerExtension 상속
    public class ServerExtension : TcHmiSrv.Core.ServerExtension
    {
        // 생성자
        public ServerExtension(string name, IServerContext context)
            : base(name, context)
        {
        }

        // 초기화
        protected override ErrorValue Init()
        {
            // 초기화 로직
            TcHmiSrv.Core.Tools.Management.Log.Info(
                "MyCustomExtension이 초기화되었습니다."
            );

            return ErrorValue.HMI_SUCCESS;
        }

        // 커맨드 등록
        protected override void RegisterCommands()
        {
            // 커스텀 커맨드 등록
            this.Commands.Add("GetSystemInfo", GetSystemInfo);
            this.Commands.Add("ProcessData", ProcessData);
        }

        // 커맨드 핸들러 예시
        private ErrorValue GetSystemInfo(Context context, Command command)
        {
            try
            {
                // 시스템 정보 수집
                var systemInfo = new
                {
                    MachineName = Environment.MachineName,
                    OSVersion = Environment.OSVersion.ToString(),
                    ProcessorCount = Environment.ProcessorCount,
                    UpTime = Environment.TickCount / 1000 / 60  // 분 단위
                };

                // 결과 반환
                command.ExtensionResult = systemInfo;
                return ErrorValue.HMI_SUCCESS;
            }
            catch (Exception ex)
            {
                TcHmiSrv.Core.Tools.Management.Log.Error(
                    "GetSystemInfo 오류: " + ex.Message
                );
                return ErrorValue.HMI_E_FAIL;
            }
        }

        private ErrorValue ProcessData(Context context, Command command)
        {
            try
            {
                // 파라미터 읽기
                var inputData = command.ReadValue<string>("inputData");

                // 데이터 처리 로직
                var processedData = inputData.ToUpper();

                // 결과 반환
                command.WriteValue("result", processedData);
                return ErrorValue.HMI_SUCCESS;
            }
            catch (Exception ex)
            {
                TcHmiSrv.Core.Tools.Management.Log.Error(
                    "ProcessData 오류: " + ex.Message
                );
                return ErrorValue.HMI_E_FAIL;
            }
        }
    }
}
```

#### 클라이언트에서 서버 확장 호출

```javascript
// 서버 확장 커맨드 호출
(function () {
    var requestOptions = {
        requestType: "GET",
        commands: [
            {
                commandOptions: ["SendErrorMessage"],
                symbol: "MyCustomExtension.GetSystemInfo"
            }
        ]
    };

    TcHmi.Server.Request(requestOptions, function (data) {
        if (data.error === TcHmi.Errors.NONE) {
            // 성공
            var systemInfo = data.response.commands[0];
            console.log('시스템 정보:', systemInfo);
        } else {
            // 오류 처리
            console.error('오류:', data.error);
        }
    });
})();
```

### 서버 확장 고급 기능

#### 동적 심볼 관리

```csharp
// 동적 심볼 생성
protected override ErrorValue Init()
{
    // 심볼 경로 정의
    string symbolPath = "MyExtension.DynamicData.Temperature";

    // 심볼 추가
    this.Symbols.Add(
        new Symbol(symbolPath, TcHmiSrv.Core.ValueType.Double)
        {
            Value = 25.5,
            Attribute = TcHmiSrv.Core.SymbolAttribute.Read |
                       TcHmiSrv.Core.SymbolAttribute.Write
        }
    );

    return ErrorValue.HMI_SUCCESS;
}

// 심볼 값 업데이트
private void UpdateTemperature(double newValue)
{
    string symbolPath = "MyExtension.DynamicData.Temperature";

    if (this.Symbols.TryGetValue(symbolPath, out var symbol))
    {
        symbol.Value = newValue;
    }
}
```

#### 이벤트 발생

```csharp
// 커스텀 이벤트 정의
public class TemperatureAlarmEventArgs : EventArgs
{
    public double Temperature { get; set; }
    public DateTime Timestamp { get; set; }
}

// 이벤트 발생
private void RaiseTemperatureAlarm(double temperature)
{
    var eventArgs = new TemperatureAlarmEventArgs
    {
        Temperature = temperature,
        Timestamp = DateTime.Now
    };

    // 이벤트 전송
    this.OnEvent(new Event("TemperatureAlarm", eventArgs));
}
```

#### ADS 통신

```csharp
using TcHmiSrv.Core.Listeners.RequestListenerEventArgs.AdsExtensions;

// ADS 읽기
private ErrorValue ReadPLCVariable()
{
    var adsClient = this.Server.AdsClient;

    try
    {
        // PLC 변수 읽기
        var handle = adsClient.CreateVariableHandle("MAIN.Temperature");
        var value = adsClient.ReadSymbol<double>(handle);

        adsClient.DeleteVariableHandle(handle);

        Log.Info($"PLC 온도: {value}");
        return ErrorValue.HMI_SUCCESS;
    }
    catch (Exception ex)
    {
        Log.Error("ADS 읽기 오류: " + ex.Message);
        return ErrorValue.HMI_E_FAIL;
    }
}
```

### 커스텀 Framework 컨트롤 개발

#### 기본 User Control 구조

**MyCustomControl.ts (TypeScript):**

```typescript
module TcHmi.Controls.MyNamespace {
    export class MyCustomControl extends TcHmi.Controls.System.TcHmiControl {

        constructor(element: JQuery, pcElement: JQuery, attrs: TcHmi.Controls.ControlAttributeList) {
            super(element, pcElement, attrs);
        }

        // 내부 변수
        protected __myValue: number = 0;

        // 속성 Setter
        public setMyValue(valueNew: number) {
            let convertedValue = TcHmi.ValueConverter.toNumber(valueNew);

            if (convertedValue === null) {
                convertedValue = this.getAttributeDefaultValueInternal('MyValue') as number;
            }

            if (this.__myValue === convertedValue) {
                return;
            }

            this.__myValue = convertedValue;

            // DOM 업데이트
            this.__elementTemplateRoot.text(this.__myValue.toString());

            // 변경 이벤트 발생
            TcHmi.EventProvider.raise(
                this.__id + '.onMyValueChanged',
                { value: this.__myValue }
            );
        }

        // 속성 Getter
        public getMyValue(): number {
            return this.__myValue;
        }

        // Destroy 처리
        public destroy() {
            super.destroy();
        }
    }

    // Framework에 컨트롤 등록
    TcHmi.Controls.registerEx(
        'MyCustomControl',
        'TcHmi.Controls.MyNamespace',
        MyCustomControl
    );
}
```

**MyCustomControl.html (템플릿):**

```html
<div id="MyCustomControl-%id%" class="my-custom-control">
    <div data-tchmi-template-name="MyCustomControl">
        <div class="my-custom-control-content">
            <!-- 컨트롤 내용 -->
        </div>
    </div>
</div>
```

**MyCustomControl.css:**

```css
.my-custom-control {
    border: 1px solid #ccc;
    padding: 10px;
    background-color: #f0f0f0;
}

.my-custom-control-content {
    font-size: 16px;
    color: #333;
}
```

---

## 심볼 바인딩 및 데이터 통신

### ADS 프로토콜을 통한 심볼 바인딩

#### 심볼 매핑 과정

1. **HMI Server 설정**: PLC와의 ADS 연결 구성
2. **심볼 검색**: PLC의 전역 변수 목록 가져오기
3. **바인딩 생성**: HMI 컨트롤 속성과 PLC 심볼 연결

#### Engineering에서 바인딩 설정

**방법 1: UI를 통한 바인딩**

1. 컨트롤 선택
2. 속성 창에서 바인딩할 속성 우클릭
3. "Create Binding..." 선택
4. "Server symbols" 탭에서 PLC 변수 선택
5. ADS 경로: `ADS::PLC1.MAIN.Temperature`

**방법 2: 바인딩 표현식 직접 입력**

```
%s%PLC1.MAIN.Temperature%/s%
```

**방법 3: JavaScript에서 동적 바인딩**

```javascript
// 심볼 읽기
var symbolExpression = '%s%PLC1.MAIN.Temperature%/s%';

TcHmi.Symbol.readEx(symbolExpression, function (data) {
    if (data.error === TcHmi.Errors.NONE) {
        var temperature = data.value;
        console.log('온도:', temperature);
    } else {
        console.error('심볼 읽기 오류:', data.error);
    }
});

// 심볼 쓰기
var newValue = 30.5;

TcHmi.Symbol.writeEx(symbolExpression, newValue, function (data) {
    if (data.error === TcHmi.Errors.NONE) {
        console.log('값이 성공적으로 작성되었습니다.');
    } else {
        console.error('심볼 쓰기 오류:', data.error);
    }
});
```

### 심볼 구독 (실시간 업데이트)

```javascript
// 심볼 변경 감시
var watchId = TcHmi.Symbol.watchEx(
    '%s%PLC1.MAIN.Temperature%/s%',
    function (data) {
        if (data.error === TcHmi.Errors.NONE) {
            console.log('온도가 변경되었습니다:', data.value);

            // UI 업데이트
            var gauge = TcHmi.Controls.get('TcHmiLinearGauge_1');
            if (gauge) {
                gauge.setValue(data.value);
            }
        }
    }
);

// 구독 취소 (정리 시)
TcHmi.Symbol.unwatch(watchId);
```

### 구조체 심볼 처리

#### PLC 구조체 정의

```iecst
TYPE ST_MotorData :
STRUCT
    Speed : REAL;
    Current : REAL;
    Temperature : REAL;
    IsRunning : BOOL;
    ErrorCode : UINT;
END_STRUCT
END_TYPE

VAR_GLOBAL
    Motor1 : ST_MotorData;
END_VAR
```

#### HMI에서 구조체 필드 접근

```javascript
// 전체 구조체 읽기
TcHmi.Symbol.readEx('%s%PLC1.Motor1%/s%', function (data) {
    if (data.error === TcHmi.Errors.NONE) {
        var motorData = data.value;
        console.log('속도:', motorData.Speed);
        console.log('전류:', motorData.Current);
        console.log('온도:', motorData.Temperature);
        console.log('실행 중:', motorData.IsRunning);
        console.log('에러 코드:', motorData.ErrorCode);
    }
});

// 개별 필드 읽기
TcHmi.Symbol.readEx('%s%PLC1.Motor1.Speed%/s%', function (data) {
    if (data.error === TcHmi.Errors.NONE) {
        console.log('모터 속도:', data.value);
    }
});

// 필드 쓰기
var newSpeed = 1500.0;
TcHmi.Symbol.writeEx(
    '%s%PLC1.Motor1.Speed%/s%',
    newSpeed,
    function (data) {
        if (data.error === TcHmi.Errors.NONE) {
            console.log('속도 설정 완료');
        }
    }
);
```

### 배열 심볼

```iecst
// PLC 배열 정의
VAR_GLOBAL
    Temperatures : ARRAY[1..10] OF REAL;
END_VAR
```

```javascript
// 배열 전체 읽기
TcHmi.Symbol.readEx('%s%PLC1.Temperatures%/s%', function (data) {
    if (data.error === TcHmi.Errors.NONE) {
        var tempArray = data.value;
        for (var i = 0; i < tempArray.length; i++) {
            console.log(`온도 ${i + 1}:`, tempArray[i]);
        }
    }
});

// 배열 요소 개별 접근
TcHmi.Symbol.readEx('%s%PLC1.Temperatures[5]%/s%', function (data) {
    if (data.error === TcHmi.Errors.NONE) {
        console.log('5번 온도:', data.value);
    }
});
```

### Server-to-Server 통신

여러 HMI 서버 간 데이터 교환이 가능합니다.

```javascript
// 다른 HMI 서버의 심볼 읽기
var remoteSymbol = '%s%RemoteServer::MyExtension.Data%/s%';

TcHmi.Symbol.readEx(remoteSymbol, function (data) {
    if (data.error === TcHmi.Errors.NONE) {
        console.log('원격 데이터:', data.value);
    }
});
```

---

## 이벤트 처리 및 사용자 관리

### 이벤트 시스템

#### 컨트롤 이벤트

TwinCAT HMI 컨트롤은 다양한 이벤트를 발생시킵니다.

```javascript
// 버튼 클릭 이벤트
var button = TcHmi.Controls.get('TcHmiButton_1');

button.registerEvent(
    'onPressed',
    function (evt, data) {
        console.log('버튼이 눌렸습니다.');
        // 이벤트 처리 로직
    }
);

// 텍스트박스 텍스트 변경 이벤트
var textbox = TcHmi.Controls.get('TcHmiTextbox_1');

textbox.registerEvent(
    'onTextChanged',
    function (evt, data) {
        console.log('새 텍스트:', data.newValue);
    }
);

// 이벤트 해제
var destroyEvent = function () {
    button.destroyEvent('onPressed', eventHandler);
};
```

#### 주요 이벤트 목록

| 이벤트 | 발생 시점 | 적용 컨트롤 |
|--------|-----------|-------------|
| onPressed | 클릭/터치 시 | Button, Image |
| onTextChanged | 텍스트 변경 시 | Textbox, NumericInput |
| onValueChanged | 값 변경 시 | NumericInput, Slider |
| onToggleStateChanged | 토글 상태 변경 | ToggleButton, CheckBox |
| onVisibilityChanged | 가시성 변경 | 모든 컨트롤 |
| onAttached | DOM에 추가됨 | 모든 컨트롤 |
| onDetached | DOM에서 제거됨 | 모든 컨트롤 |

### JavaScript 액션

Engineering에서 이벤트에 JavaScript 액션을 직접 연결할 수 있습니다.

**예시: 버튼 클릭 시 모터 시작**

```javascript
// 버튼의 onPressed 이벤트 액션
(function (evt, data) {
    // PLC 변수에 TRUE 쓰기
    TcHmi.Symbol.writeEx(
        '%s%PLC1.MAIN.bStartMotor%/s%',
        true,
        function (writeData) {
            if (writeData.error === TcHmi.Errors.NONE) {
                console.log('모터 시작 명령 전송 완료');
            } else {
                console.error('명령 전송 실패:', writeData.error);
            }
        }
    );
})(evt, data);
```

### Alarm 및 Event Grid

TwinCAT HMI는 내장 알람 및 이벤트 시스템을 제공합니다.

#### Event Grid 컨트롤

```javascript
var eventGrid = TcHmi.Controls.get('TcHmiEventGrid_1');

// 이벤트 필터 설정
eventGrid.setFilter({
    severity: ['Error', 'Warning']  // 오류 및 경고만 표시
});

// 이벤트 확인 (Acknowledge)
var selectedEvents = eventGrid.getSelectedEvents();
selectedEvents.forEach(function (event) {
    event.acknowledge();
});
```

#### 서버 확장에서 알람 생성

```csharp
// C# 서버 확장에서 알람 발생
public class AlarmExtension : ServerExtension
{
    private void RaiseAlarm(string message, TcHmiSrv.Core.AlarmSeverity severity)
    {
        var alarm = new TcHmiSrv.Core.Alarm
        {
            Id = Guid.NewGuid().ToString(),
            Severity = severity,
            Message = message,
            Timestamp = DateTime.Now,
            SourceName = this.Name
        };

        // 알람 발생
        this.OnAlarm(alarm);
    }

    // 사용 예시
    private void CheckTemperature(double temperature)
    {
        if (temperature > 80.0)
        {
            RaiseAlarm(
                $"온도 경고: {temperature}°C",
                TcHmiSrv.Core.AlarmSeverity.Warning
            );
        }

        if (temperature > 100.0)
        {
            RaiseAlarm(
                $"온도 위험: {temperature}°C - 즉시 조치 필요!",
                TcHmiSrv.Core.AlarmSeverity.Error
            );
        }
    }
}
```

### 사용자 관리 (User Management)

TwinCAT HMI는 강력한 사용자 인증 및 권한 관리 시스템을 제공합니다.

#### 인증 모드 설정

**HMI Server 구성:**

- **None**: 인증 없음
- **Always authenticate**: 항상 인증 필요

#### 사용자 및 그룹 관리

**Server Configuration에서 설정:**

1. TwinCAT HMI Configuration 열기
2. "User Management" 섹션
3. 사용자 추가/편집
4. 그룹 할당

**사용자 그룹 예시:**
- **Administrator**: 모든 권한
- **Operator**: 운전 권한
- **Viewer**: 읽기 전용

#### 로그인/로그아웃

```javascript
// 프로그래밍 방식 로그인
TcHmi.Server.Request({
    requestType: 'Login',
    username: 'operator1',
    password: 'mypassword'
}, function (data) {
    if (data.error === TcHmi.Errors.NONE) {
        console.log('로그인 성공');
    } else {
        console.error('로그인 실패');
    }
});

// 로그아웃
TcHmi.Server.Request({
    requestType: 'Logout'
}, function (data) {
    if (data.error === TcHmi.Errors.NONE) {
        console.log('로그아웃 성공');
    }
});

// 현재 사용자 정보 가져오기
TcHmi.Server.Request({
    requestType: 'GetCurrentUser'
}, function (data) {
    if (data.error === TcHmi.Errors.NONE) {
        var user = data.response;
        console.log('현재 사용자:', user.name);
        console.log('그룹:', user.groups);
    }
});
```

#### 컨트롤 수준 권한

```javascript
// 특정 그룹만 버튼 조작 가능하도록 설정
var button = TcHmi.Controls.get('TcHmiButton_1');

// Access 설정 (Engineering에서도 설정 가능)
button.setAccess([
    {
        group: 'Administrator',
        permission: 'operate'
    },
    {
        group: 'Operator',
        permission: 'operate'
    },
    {
        group: 'Viewer',
        permission: 'observe'  // 보기만 가능
    }
]);
```

#### 심볼 수준 권한

서버 확장에서 심볼 읽기/쓰기 권한을 그룹별로 제어할 수 있습니다.

```csharp
// 심볼 권한 설정
protected override ErrorValue Init()
{
    var symbol = new Symbol("MyExtension.CriticalData", ValueType.Int32)
    {
        Value = 100,
        Attribute = SymbolAttribute.Read | SymbolAttribute.Write
    };

    // 권한 설정: Administrator만 쓰기 가능
    symbol.SetAccess(new[]
    {
        new Access { Group = "Administrator", Permission = Permission.ReadWrite },
        new Access { Group = "Operator", Permission = Permission.Read },
        new Access { Group = "Viewer", Permission = Permission.Read }
    });

    this.Symbols.Add(symbol);
    return ErrorValue.HMI_SUCCESS;
}
```

#### 런타임 사용자 관리

클라이언트에서 동적으로 사용자를 생성/편집/삭제할 수 있습니다.

```javascript
// 새 사용자 추가
var newUser = {
    name: 'newoperator',
    password: 'securepassword',
    groups: ['Operator']
};

TcHmi.Server.Request({
    requestType: 'AddUser',
    data: newUser
}, function (data) {
    if (data.error === TcHmi.Errors.NONE) {
        console.log('사용자가 추가되었습니다.');
    }
});

// 사용자 비밀번호 변경
TcHmi.Server.Request({
    requestType: 'ChangeUserPassword',
    username: 'newoperator',
    oldPassword: 'securepassword',
    newPassword: 'newsecurepassword'
}, function (data) {
    if (data.error === TcHmi.Errors.NONE) {
        console.log('비밀번호가 변경되었습니다.');
    }
});

// 사용자 삭제
TcHmi.Server.Request({
    requestType: 'DeleteUser',
    username: 'newoperator'
}, function (data) {
    if (data.error === TcHmi.Errors.NONE) {
        console.log('사용자가 삭제되었습니다.');
    }
});
```

---

## 실전 예제

### 예제 1: 실시간 모니터링 대시보드

#### PLC 변수

```iecst
VAR_GLOBAL
    Temperature : REAL := 25.0;
    Pressure : REAL := 1.0;
    FlowRate : REAL := 100.0;
    bAlarmActive : BOOL := FALSE;
END_VAR
```

#### HMI JavaScript

```javascript
(function () {
    // 초기화 함수
    function initializeDashboard() {
        // 심볼 구독
        watchSymbols();

        // UI 업데이트 타이머 시작
        setInterval(updateUI, 1000);
    }

    // 심볼 감시
    function watchSymbols() {
        // 온도 감시
        TcHmi.Symbol.watchEx('%s%PLC1.Temperature%/s%', function (data) {
            if (data.error === TcHmi.Errors.NONE) {
                updateTemperature(data.value);
            }
        });

        // 압력 감시
        TcHmi.Symbol.watchEx('%s%PLC1.Pressure%/s%', function (data) {
            if (data.error === TcHmi.Errors.NONE) {
                updatePressure(data.value);
            }
        });

        // 유량 감시
        TcHmi.Symbol.watchEx('%s%PLC1.FlowRate%/s%', function (data) {
            if (data.error === TcHmi.Errors.NONE) {
                updateFlowRate(data.value);
            }
        });

        // 알람 상태 감시
        TcHmi.Symbol.watchEx('%s%PLC1.bAlarmActive%/s%', function (data) {
            if (data.error === TcHmi.Errors.NONE) {
                handleAlarmState(data.value);
            }
        });
    }

    // UI 업데이트 함수들
    function updateTemperature(value) {
        var gauge = TcHmi.Controls.get('TemperatureGauge');
        if (gauge) {
            gauge.setValue(value);
        }

        var textbox = TcHmi.Controls.get('TemperatureValue');
        if (textbox) {
            textbox.setText(value.toFixed(1) + ' °C');
        }

        // 임계값 체크
        if (value > 80.0) {
            gauge.setBackgroundColor({ color: '#FF0000' });
        } else if (value > 60.0) {
            gauge.setBackgroundColor({ color: '#FFFF00' });
        } else {
            gauge.setBackgroundColor({ color: '#00FF00' });
        }
    }

    function updatePressure(value) {
        var gauge = TcHmi.Controls.get('PressureGauge');
        if (gauge) {
            gauge.setValue(value);
        }
    }

    function updateFlowRate(value) {
        var gauge = TcHmi.Controls.get('FlowRateGauge');
        if (gauge) {
            gauge.setValue(value);
        }
    }

    function handleAlarmState(isActive) {
        var alarmIndicator = TcHmi.Controls.get('AlarmIndicator');
        if (alarmIndicator) {
            if (isActive) {
                alarmIndicator.setBackgroundColor({ color: '#FF0000' });
                alarmIndicator.setText('알람 발생!');

                // 알람 사운드 재생 (선택적)
                playAlarmSound();
            } else {
                alarmIndicator.setBackgroundColor({ color: '#00FF00' });
                alarmIndicator.setText('정상');
            }
        }
    }

    function updateUI() {
        // 주기적 UI 업데이트 로직
        var now = new Date();
        var timeText = TcHmi.Controls.get('CurrentTime');
        if (timeText) {
            timeText.setText(now.toLocaleTimeString('ko-KR'));
        }
    }

    function playAlarmSound() {
        var audio = new Audio('Sounds/alarm.mp3');
        audio.play();
    }

    // 페이지 로드 시 초기화
    initializeDashboard();
})();
```

### 예제 2: 모터 제어 패널

#### PLC Function Block

```iecst
FUNCTION_BLOCK FB_MotorControl
VAR_INPUT
    bStart : BOOL;
    bStop : BOOL;
    rSetSpeed : REAL;  // [rpm]
END_VAR
VAR_OUTPUT
    bRunning : BOOL;
    bFault : BOOL;
    rActualSpeed : REAL;
    nFaultCode : UINT;
END_VAR
VAR
    fbAxisPower : MC_Power;
    fbMoveVelocity : MC_MoveVelocity;
    fbHalt : MC_Halt;
    Axis : AXIS_REF;
END_VAR

// 모터 활성화
fbAxisPower(
    Axis := Axis,
    Enable := TRUE
);

// 시작 명령
IF bStart AND NOT bRunning THEN
    fbMoveVelocity(
        Axis := Axis,
        Execute := TRUE,
        Velocity := rSetSpeed,
        Acceleration := 1000.0,
        Deceleration := 1000.0
    );

    IF fbMoveVelocity.InVelocity THEN
        bRunning := TRUE;
        fbMoveVelocity(Execute := FALSE);
    END_IF
END_IF

// 정지 명령
IF bStop AND bRunning THEN
    fbHalt(
        Axis := Axis,
        Execute := TRUE,
        Deceleration := 2000.0
    );

    IF fbHalt.Done THEN
        bRunning := FALSE;
        fbHalt(Execute := FALSE);
    END_IF
END_IF

// 실제 속도 읽기
rActualSpeed := Axis.NcToPlc.ActVelo;

// 오류 처리
bFault := Axis.Status.Error;
nFaultCode := UDINT_TO_UINT(Axis.Status.ErrorID);
```

#### HMI 제어 스크립트

```javascript
(function () {
    var motorControl = {
        // 모터 시작
        start: function () {
            var setSpeed = TcHmi.Controls.get('SpeedInput').getValue();

            if (setSpeed <= 0 || setSpeed > 3000) {
                alert('속도는 0-3000 rpm 범위여야 합니다.');
                return;
            }

            // 속도 설정
            TcHmi.Symbol.writeEx(
                '%s%PLC1.MotorFB.rSetSpeed%/s%',
                setSpeed,
                function (data) {
                    if (data.error === TcHmi.Errors.NONE) {
                        // 시작 명령
                        TcHmi.Symbol.writeEx(
                            '%s%PLC1.MotorFB.bStart%/s%',
                            true
                        );

                        console.log('모터 시작 명령 전송');
                    }
                }
            );
        },

        // 모터 정지
        stop: function () {
            TcHmi.Symbol.writeEx(
                '%s%PLC1.MotorFB.bStop%/s%',
                true,
                function (data) {
                    if (data.error === TcHmi.Errors.NONE) {
                        console.log('모터 정지 명령 전송');
                    }
                }
            );
        },

        // 상태 모니터링
        monitorStatus: function () {
            // 실행 상태
            TcHmi.Symbol.watchEx(
                '%s%PLC1.MotorFB.bRunning%/s%',
                function (data) {
                    if (data.error === TcHmi.Errors.NONE) {
                        var statusText = TcHmi.Controls.get('MotorStatus');
                        if (statusText) {
                            statusText.setText(
                                data.value ? '실행 중' : '정지'
                            );
                            statusText.setTextColor({
                                color: data.value ? '#00FF00' : '#808080'
                            });
                        }
                    }
                }
            );

            // 실제 속도
            TcHmi.Symbol.watchEx(
                '%s%PLC1.MotorFB.rActualSpeed%/s%',
                function (data) {
                    if (data.error === TcHmi.Errors.NONE) {
                        var speedGauge = TcHmi.Controls.get('SpeedGauge');
                        if (speedGauge) {
                            speedGauge.setValue(data.value);
                        }

                        var speedValue = TcHmi.Controls.get('ActualSpeedValue');
                        if (speedValue) {
                            speedValue.setText(data.value.toFixed(0) + ' rpm');
                        }
                    }
                }
            );

            // 고장 상태
            TcHmi.Symbol.watchEx(
                '%s%PLC1.MotorFB.bFault%/s%',
                function (data) {
                    if (data.error === TcHmi.Errors.NONE && data.value) {
                        // 고장 코드 읽기
                        TcHmi.Symbol.readEx(
                            '%s%PLC1.MotorFB.nFaultCode%/s%',
                            function (codeData) {
                                if (codeData.error === TcHmi.Errors.NONE) {
                                    alert('모터 고장 발생! 코드: 0x' +
                                          codeData.value.toString(16));
                                }
                            }
                        );
                    }
                }
            );
        },

        // 초기화
        init: function () {
            // 시작 버튼 이벤트
            var startButton = TcHmi.Controls.get('StartButton');
            if (startButton) {
                startButton.registerEvent('onPressed', function () {
                    motorControl.start();
                });
            }

            // 정지 버튼 이벤트
            var stopButton = TcHmi.Controls.get('StopButton');
            if (stopButton) {
                stopButton.registerEvent('onPressed', function () {
                    motorControl.stop();
                });
            }

            // 상태 모니터링 시작
            this.monitorStatus();
        }
    };

    // 페이지 로드 시 초기화
    motorControl.init();
})();
```

### 예제 3: 데이터 로깅 및 트렌드 차트

```javascript
(function () {
    var dataLogger = {
        dataPoints: [],
        maxDataPoints: 100,

        // 데이터 수집
        collectData: function () {
            TcHmi.Symbol.readEx('%s%PLC1.Temperature%/s%', function (data) {
                if (data.error === TcHmi.Errors.NONE) {
                    var dataPoint = {
                        timestamp: new Date(),
                        temperature: data.value
                    };

                    dataLogger.dataPoints.push(dataPoint);

                    // 최대 개수 유지
                    if (dataLogger.dataPoints.length > dataLogger.maxDataPoints) {
                        dataLogger.dataPoints.shift();
                    }

                    // 차트 업데이트
                    dataLogger.updateChart();

                    // 로컬 스토리지에 저장
                    dataLogger.saveToLocalStorage();
                }
            });
        },

        // 차트 업데이트
        updateChart: function () {
            var chart = TcHmi.Controls.get('TrendChart');
            if (!chart) return;

            // 차트 데이터 포맷 변환
            var chartData = this.dataPoints.map(function (point) {
                return {
                    x: point.timestamp.getTime(),
                    y: point.temperature
                };
            });

            // 차트에 데이터 설정
            chart.setChartData([
                {
                    name: '온도',
                    data: chartData,
                    color: { color: 'rgba(255, 0, 0, 1)' }
                }
            ]);
        },

        // 로컬 스토리지에 저장
        saveToLocalStorage: function () {
            try {
                localStorage.setItem(
                    'temperatureLog',
                    JSON.stringify(this.dataPoints)
                );
            } catch (e) {
                console.error('로컬 스토리지 저장 실패:', e);
            }
        },

        // 로컬 스토리지에서 로드
        loadFromLocalStorage: function () {
            try {
                var storedData = localStorage.getItem('temperatureLog');
                if (storedData) {
                    this.dataPoints = JSON.parse(storedData);
                    // 날짜 객체로 복원
                    this.dataPoints.forEach(function (point) {
                        point.timestamp = new Date(point.timestamp);
                    });
                    this.updateChart();
                }
            } catch (e) {
                console.error('로컬 스토리지 로드 실패:', e);
            }
        },

        // 데이터 내보내기 (CSV)
        exportToCSV: function () {
            var csv = 'Timestamp,Temperature\n';
            this.dataPoints.forEach(function (point) {
                csv += point.timestamp.toISOString() + ',' +
                       point.temperature + '\n';
            });

            // Blob 생성 및 다운로드
            var blob = new Blob([csv], { type: 'text/csv' });
            var url = URL.createObjectURL(blob);
            var a = document.createElement('a');
            a.href = url;
            a.download = 'temperature_log.csv';
            a.click();
            URL.revokeObjectURL(url);
        },

        // 초기화
        init: function () {
            // 저장된 데이터 로드
            this.loadFromLocalStorage();

            // 1초마다 데이터 수집
            setInterval(function () {
                dataLogger.collectData();
            }, 1000);

            // 내보내기 버튼 이벤트
            var exportButton = TcHmi.Controls.get('ExportButton');
            if (exportButton) {
                exportButton.registerEvent('onPressed', function () {
                    dataLogger.exportToCSV();
                });
            }
        }
    };

    // 초기화
    dataLogger.init();
})();
```

---

## 참고 자료

### 공식 문서

- [TwinCAT HMI Documentation](https://infosys.beckhoff.com/content/1033/te2000_tc3_hmi_engineering/)
- [TwinCAT HMI API Reference](https://infosys.beckhoff.com/content/1033/te2000_tc3_hmi_engineering/3730606987.html)
- [Beckhoff GitHub - Server Samples](https://github.com/Beckhoff/TF2000_Server_Samples)

### NuGet 패키지

- [Beckhoff.TwinCAT.HMI.Framework](https://www.nuget.org/packages/Beckhoff.TwinCAT.HMI.Framework/)
- [Beckhoff.TwinCAT.HMI.Controls](https://www.nuget.org/packages/Beckhoff.TwinCAT.HMI.Controls/)

### 커뮤니티 리소스

- [Hello TwinCAT - HMI Tutorials](https://hellotwincat.dev/)
- [HEMELIX - TwinCAT HMI Guides](https://www.hemelix.com/scada-hmi/twincat-hmi/)

### 제품 정보

- [TwinCAT HMI 제품 페이지](https://www.beckhoff.com/en-en/products/automation/twincat-3-hmi/)
- [TE2000 - TwinCAT 3 HMI Engineering](https://www.beckhoff.com/en-en/products/automation/twincat/texxxx-twincat-3-engineering/te2000.html)
- [TF2200 - TwinCAT 3 HMI Extension SDK](https://www.beckhoff.com/en-en/products/automation/twincat/tfxxxx-twincat-3-functions/tf2xxx-hmi/tf2200.html)

---

**마지막 업데이트**: 2025-11-24
**TwinCAT HMI Framework 버전**: 14.3.277
**문서 버전**: 1.0
