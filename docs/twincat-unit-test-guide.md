# TwinCAT 단위 테스트 프레임워크 입문 가이드

> 이 문서는 TwinCAT PLC 개발에서 단위 테스트를 처음 시작하는 분들을 위한 종합 가이드입니다.

---

## 목차

1. [단위 테스트란 무엇인가?](#1-단위-테스트란-무엇인가)
2. [왜 PLC에서 단위 테스트가 필요한가?](#2-왜-plc에서-단위-테스트가-필요한가)
3. [TwinCAT 단위 테스트 프레임워크 비교](#3-twincat-단위-테스트-프레임워크-비교)
4. [TcUnit - 가장 추천하는 프레임워크](#4-tcunit---가장-추천하는-프레임워크)
5. [TcUnit-Runner - CI/CD 자동화 도구](#5-tcunit-runner---cicd-자동화-도구)
6. [Tc3_UnitTest - 간단한 대안](#6-tc3_unittest---간단한-대안)
7. [TcOpen - 산업용 애플리케이션 프레임워크](#7-tcopen---산업용-애플리케이션-프레임워크)
8. [어떤 프레임워크를 선택해야 하는가?](#8-어떤-프레임워크를-선택해야-하는가)
9. [실전 적용 로드맵](#9-실전-적용-로드맵)

---

## 1. 단위 테스트란 무엇인가?

### 기본 개념

**단위 테스트(Unit Test)**는 소프트웨어의 가장 작은 단위(함수, 메서드, Function Block 등)가 의도한 대로 동작하는지 검증하는 테스트입니다.

```
┌─────────────────────────────────────────────────────────────┐
│                     테스트 피라미드                          │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│                    ┌─────────┐                              │
│                   /  E2E     \        ← 적음, 느림, 비쌈     │
│                  /   테스트    \                             │
│                 ┌─────────────┐                             │
│                /   통합 테스트   \      ← 중간               │
│               ┌─────────────────┐                           │
│              /     단위 테스트     \    ← 많음, 빠름, 저렴   │
│             └─────────────────────┘                         │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### PLC 단위 테스트의 특징

| 일반 소프트웨어 | PLC 소프트웨어 |
|----------------|---------------|
| 함수/클래스 테스트 | Function Block 테스트 |
| 즉시 실행 | PLC 사이클 기반 실행 |
| 메모리에서 실행 | 실시간 런타임에서 실행 |
| 다양한 프레임워크 | 제한된 프레임워크 선택지 |

### 왜 xUnit 스타일인가?

TcUnit은 **xUnit** 패턴을 따릅니다. xUnit은 JUnit(Java), NUnit(.NET), pytest(Python) 등 전 세계적으로 사용되는 테스트 프레임워크의 표준 패턴입니다.

```
xUnit 핵심 구조:
┌─────────────────┐
│   Test Suite    │  ← 테스트들을 그룹화한 집합
├─────────────────┤
│   Test Case     │  ← 개별 테스트
├─────────────────┤
│   Assertion     │  ← 결과 검증 (Assert)
├─────────────────┤
│   Test Runner   │  ← 테스트 실행기
└─────────────────┘
```

---

## 2. 왜 PLC에서 단위 테스트가 필요한가?

### 현실의 문제점

```
기존 PLC 개발 방식의 문제:

개발자 A: "내가 만든 FB 잘 돌아가는지 어떻게 확인하지?"
         → 실제 장비에 올려서 수동 테스트
         → 시간 오래 걸림, 위험할 수 있음

개발자 B: "다른 사람이 수정한 코드 때문에 내 코드가 깨졌어!"
         → 어디서 문제인지 찾기 어려움
         → 수정 후 전체 다시 테스트 필요

개발자 C: "레거시 코드 수정해야 하는데 무서워..."
         → 수정하면 뭐가 깨질지 모름
         → 결국 수정 안 하고 복사해서 새로 만듦
```

### 단위 테스트의 장점

| 장점 | 설명 |
|-----|------|
| **빠른 피드백** | 실제 장비 없이 몇 초 내에 테스트 결과 확인 |
| **회귀 방지** | 기존 기능이 새 코드로 인해 깨지는 것을 자동 감지 |
| **문서화 효과** | 테스트 코드가 사용 예시를 보여줌 |
| **리팩토링 자신감** | 테스트가 있으면 코드 수정이 두렵지 않음 |
| **디버깅 시간 단축** | 문제 범위를 빠르게 좁힐 수 있음 |

### ROI (투자 대비 효과)

```
단위 테스트 도입 전후 비교:

┌─────────────────┬─────────────┬─────────────┐
│      항목       │   도입 전    │   도입 후    │
├─────────────────┼─────────────┼─────────────┤
│ 버그 발견 시점   │  현장 설치   │  개발 단계   │
│ 버그 수정 비용   │    10x      │     1x      │
│ 코드 수정 자신감 │     낮음     │    높음     │
│ 레거시 유지보수  │    어려움    │   가능함    │
│ 신규 인원 온보딩 │    오래걸림  │   빠름      │
└─────────────────┴─────────────┴─────────────┘
```

---

## 3. TwinCAT 단위 테스트 프레임워크 비교

### 한눈에 보는 비교표

| 항목 | TcUnit | Tc3_UnitTest | TcOpen/Tc.Prober |
|------|--------|--------------|------------------|
| **난이도** | ⭐⭐ 중간 | ⭐ 쉬움 | ⭐⭐⭐ 어려움 |
| **기능** | ⭐⭐⭐⭐⭐ 풍부 | ⭐⭐ 기본 | ⭐⭐⭐⭐ 풍부 |
| **문서화** | ⭐⭐⭐⭐⭐ 우수 | ⭐⭐ 보통 | ⭐⭐⭐ 중간 |
| **CI/CD 통합** | ⭐⭐⭐⭐⭐ 우수 | ⭐⭐ 가능 | ⭐⭐⭐⭐ 우수 |
| **커뮤니티** | 활발 | 소규모 | 중간 |
| **유지보수** | 활발 | 제한적 | 보관됨(Archived) |
| **라이선스** | MIT (무료) | MIT (무료) | MIT (무료) |

### 선택 가이드 플로우차트

```
시작
  │
  ▼
┌─────────────────────────────┐
│ .NET 통합이 필요한가?        │
└─────────────────────────────┘
        │
   예   │   아니오
   ▼    │    ▼
TcOpen  │  ┌─────────────────────────────┐
        │  │ CI/CD 자동화가 필요한가?     │
        │  └─────────────────────────────┘
        │          │
        │     예   │   아니오
        │     ▼    │    ▼
        │  TcUnit  │  ┌─────────────────────────────┐
        │  +       │  │ 가장 간단한 것을 원하는가?   │
        │  Runner  │  └─────────────────────────────┘
        │          │          │
        │          │     예   │   아니오
        │          │     ▼    │    ▼
        │          │  Tc3_    │  TcUnit
        │          │  UnitTest│  (추천)
```

---

## 4. TcUnit - 가장 추천하는 프레임워크

### 4.1 TcUnit이란?

[TcUnit](https://tcunit.org/)은 Beckhoff TwinCAT 3 전용으로 만들어진 **xUnit 스타일 단위 테스트 프레임워크**입니다.

**핵심 특징:**
- ✅ MIT 라이선스 (완전 무료, 상업적 사용 가능)
- ✅ 단일 라이브러리로 쉬운 설치
- ✅ 100개 이상의 Assert 메서드 제공
- ✅ CI/CD 통합 지원 (TcUnit-Runner)
- ✅ xUnit XML 형식 결과 출력
- ✅ 활발한 커뮤니티와 문서화

**GitHub**: https://github.com/tcunit/TcUnit

### 4.2 시스템 요구사항

```
필수 환경:
┌─────────────────────────────────────────────────┐
│ • TwinCAT XAE (Visual Studio 통합 또는 Shell)    │
│ • TwinCAT 3.1 Build 4022 이상                   │
│ • Windows 7/10/11                               │
└─────────────────────────────────────────────────┘
```

### 4.3 설치 방법

#### 방법 1: 미리 컴파일된 라이브러리 설치 (권장)

```
설치 순서:

1. GitHub Releases 페이지에서 TcUnit.library 다운로드
   https://github.com/tcunit/TcUnit/releases

2. TwinCAT XAE (Visual Studio) 실행

3. 메뉴에서 PLC > Library Repository... 선택

4. Install... 버튼 클릭

5. 다운로드한 TcUnit.library 파일 선택

6. 설치 완료!
   설치 경로: C:\TwinCAT\3.1\Components\Plc\Managed Libraries\
              www.tcunit.org\TcUnit\
```

#### 방법 2: 소스 코드에서 설치

```
설치 순서:

1. GitHub에서 소스 코드 클론
   git clone https://github.com/tcunit/TcUnit.git

2. TcUnit.sln 솔루션 파일 열기

3. Solution Explorer에서 TcUnit Project 우클릭

4. "Save as library and install..." 선택

5. 설치 완료!
```

### 4.4 프로젝트에 TcUnit 추가하기

```
프로젝트 참조 추가:

1. Solution Explorer에서 PLC 프로젝트의 References 우클릭

2. "Add library..." 선택

3. "TcUnit" 검색

4. TcUnit 선택 후 OK

5. 이제 테스트 코드 작성 가능!
```

### 4.5 프로젝트 구조 권장 사항

```
MyTwinCATProject/
├── MyProject.sln
├── MyProject/
│   ├── MyProject.tsproj
│   └── PLC/
│       ├── POUs/                    ← 실제 코드
│       │   ├── FB_MotorControl.TcPOU
│       │   ├── FB_ConveyorControl.TcPOU
│       │   └── MAIN.TcPOU
│       │
│       ├── Test/                    ← 테스트 코드 (별도 폴더!)
│       │   ├── FB_MotorControl_Test.TcPOU
│       │   ├── FB_ConveyorControl_Test.TcPOU
│       │   └── PRG_TEST.TcPOU       ← 테스트 실행 프로그램
│       │
│       ├── DUTs/
│       └── GVLs/
```

### 4.6 첫 번째 테스트 작성하기

#### Step 1: 테스트할 Function Block 준비

```iecst
// 파일: POUs/FB_Calculator.TcPOU
// 설명: 간단한 계산기 Function Block

FUNCTION_BLOCK FB_Calculator
VAR_INPUT
    nValue1 : INT;      // 첫 번째 값
    nValue2 : INT;      // 두 번째 값
END_VAR
VAR_OUTPUT
    nResult : INT;      // 계산 결과
END_VAR
```

```iecst
// FB_Calculator의 Add 메서드
METHOD Add : INT
    // 두 값을 더해서 반환
    nResult := nValue1 + nValue2;
    Add := nResult;
END_METHOD
```

```iecst
// FB_Calculator의 Multiply 메서드
METHOD Multiply : INT
    // 두 값을 곱해서 반환
    nResult := nValue1 * nValue2;
    Multiply := nResult;
END_METHOD
```

#### Step 2: 테스트 Suite 작성

```iecst
// 파일: Test/FB_Calculator_Test.TcPOU
// 설명: FB_Calculator를 테스트하는 테스트 Suite

FUNCTION_BLOCK FB_Calculator_Test EXTENDS TcUnit.FB_TestSuite
VAR
    fbCalculator : FB_Calculator;   // 테스트 대상
END_VAR
```

```iecst
// FB_Calculator_Test의 본문
// 여기서 모든 테스트 메서드를 호출

// 덧셈 테스트 실행
Test_Add_PositiveNumbers();
Test_Add_NegativeNumbers();
Test_Add_Zero();

// 곱셈 테스트 실행
Test_Multiply_PositiveNumbers();
Test_Multiply_ByZero();
```

#### Step 3: 개별 테스트 메서드 작성

```iecst
// 테스트 메서드 1: 양수 덧셈
METHOD Test_Add_PositiveNumbers
VAR
    nResult : INT;
END_VAR

// ========================================
// 테스트 시작 선언
// ========================================
TEST('양수 덧셈 테스트: 5 + 3 = 8');

// ========================================
// 준비 (Arrange)
// ========================================
fbCalculator.nValue1 := 5;
fbCalculator.nValue2 := 3;

// ========================================
// 실행 (Act)
// ========================================
nResult := fbCalculator.Add();

// ========================================
// 검증 (Assert)
// ========================================
AssertEquals_INT(
    Expected := 8,
    Actual := nResult,
    Message := '5 + 3은 8이어야 합니다'
);

// ========================================
// 테스트 종료 선언
// ========================================
TEST_FINISHED();
END_METHOD
```

```iecst
// 테스트 메서드 2: 음수 덧셈
METHOD Test_Add_NegativeNumbers
VAR
    nResult : INT;
END_VAR

TEST('음수 덧셈 테스트: -5 + (-3) = -8');

fbCalculator.nValue1 := -5;
fbCalculator.nValue2 := -3;

nResult := fbCalculator.Add();

AssertEquals_INT(
    Expected := -8,
    Actual := nResult,
    Message := '-5 + (-3)은 -8이어야 합니다'
);

TEST_FINISHED();
END_METHOD
```

```iecst
// 테스트 메서드 3: 0과의 덧셈
METHOD Test_Add_Zero
VAR
    nResult : INT;
END_VAR

TEST('0 덧셈 테스트: 10 + 0 = 10');

fbCalculator.nValue1 := 10;
fbCalculator.nValue2 := 0;

nResult := fbCalculator.Add();

AssertEquals_INT(
    Expected := 10,
    Actual := nResult,
    Message := '10 + 0은 10이어야 합니다'
);

TEST_FINISHED();
END_METHOD
```

```iecst
// 테스트 메서드 4: 양수 곱셈
METHOD Test_Multiply_PositiveNumbers
VAR
    nResult : INT;
END_VAR

TEST('양수 곱셈 테스트: 4 x 5 = 20');

fbCalculator.nValue1 := 4;
fbCalculator.nValue2 := 5;

nResult := fbCalculator.Multiply();

AssertEquals_INT(
    Expected := 20,
    Actual := nResult,
    Message := '4 x 5는 20이어야 합니다'
);

TEST_FINISHED();
END_METHOD
```

```iecst
// 테스트 메서드 5: 0과의 곱셈
METHOD Test_Multiply_ByZero
VAR
    nResult : INT;
END_VAR

TEST('0 곱셈 테스트: 100 x 0 = 0');

fbCalculator.nValue1 := 100;
fbCalculator.nValue2 := 0;

nResult := fbCalculator.Multiply();

AssertEquals_INT(
    Expected := 0,
    Actual := nResult,
    Message := '어떤 수에 0을 곱하면 0이어야 합니다'
);

TEST_FINISHED();
END_METHOD
```

#### Step 4: 테스트 실행 프로그램 작성

```iecst
// 파일: Test/PRG_TEST.TcPOU
// 설명: 모든 테스트를 실행하는 프로그램

PROGRAM PRG_TEST
VAR
    // 테스트 Suite 인스턴스들
    fbCalculatorTest : FB_Calculator_Test;

    // 추가 테스트 Suite가 있으면 여기에 선언
    // fbMotorTest : FB_Motor_Test;
    // fbConveyorTest : FB_Conveyor_Test;
END_VAR
```

```iecst
// PRG_TEST 본문
// TcUnit 실행 - 모든 테스트 Suite를 자동으로 실행

TcUnit.RUN();
```

### 4.7 테스트 실행하기

```
테스트 실행 순서:

1. 프로젝트 빌드
   Build > Build Solution (Ctrl+Shift+B)

2. PLC에 활성화
   Solution Explorer > TwinCAT Project 우클릭 > Activate Configuration

3. 로그인 및 실행
   PLC > Login (F8)
   PLC > Start (F5)

4. 결과 확인
   View > Error List
   Description 열로 정렬하면 테스트 결과 확인 가능
```

### 4.8 테스트 결과 해석

```
Visual Studio Error List 창에서 결과 확인:

┌────────┬─────────────────────────────────────────────────┐
│ 아이콘  │ 의미                                            │
├────────┼─────────────────────────────────────────────────┤
│   ✓    │ 테스트 통과 (PASS)                              │
│   ✗    │ 테스트 실패 (FAIL)                              │
│   ⚠    │ 경고 또는 스킵된 테스트                          │
└────────┴─────────────────────────────────────────────────┘

실패 시 출력 예시:
| FAILED | Test_Add_PositiveNumbers |
         Expected: 8, Actual: 9 |
         5 + 3은 8이어야 합니다
```

### 4.9 자주 사용하는 Assert 메서드

```
┌────────────────────────────┬─────────────────────────────────┐
│       Assert 메서드         │            용도                 │
├────────────────────────────┼─────────────────────────────────┤
│ AssertEquals_BOOL          │ BOOL 값 비교                    │
│ AssertEquals_INT           │ INT 값 비교                     │
│ AssertEquals_DINT          │ DINT 값 비교                    │
│ AssertEquals_REAL          │ REAL 값 비교 (Delta 파라미터)   │
│ AssertEquals_LREAL         │ LREAL 값 비교 (Delta 파라미터)  │
│ AssertEquals_STRING        │ STRING 값 비교                  │
│ AssertEquals_TIME          │ TIME 값 비교                    │
│ AssertTrue                 │ 조건이 TRUE인지 확인            │
│ AssertFalse                │ 조건이 FALSE인지 확인           │
│ AssertArrayEquals_INT      │ INT 배열 비교                   │
└────────────────────────────┴─────────────────────────────────┘
```

### 4.10 실수(REAL) 값 테스트 시 주의사항

```iecst
// ❌ 잘못된 방법 - 부동소수점 비교 문제 발생 가능
METHOD Test_Division_Wrong
    TEST('나눗셈 테스트 (잘못된 방법)');

    // 10 / 3 = 3.333333...
    // 부동소수점 오차로 인해 정확히 일치하지 않을 수 있음
    AssertEquals_REAL(
        Expected := 3.333333,
        Actual := 10.0 / 3.0,
        Message := '정확히 일치하지 않을 수 있음'
    );

    TEST_FINISHED();
END_METHOD
```

```iecst
// ✅ 올바른 방법 - Delta(허용 오차) 사용
METHOD Test_Division_Correct
    TEST('나눗셈 테스트 (올바른 방법)');

    // Delta 파라미터로 허용 오차 지정
    AssertEquals_REAL(
        Expected := 3.333333,
        Actual := 10.0 / 3.0,
        Delta := 0.0001,        // 0.0001까지 오차 허용
        Message := 'Delta 범위 내에서 일치'
    );

    TEST_FINISHED();
END_METHOD
```

### 4.11 여러 PLC 사이클에 걸친 테스트

PLC 테스트의 특징 중 하나는 여러 사이클에 걸쳐 동작을 확인해야 할 때가 있다는 것입니다.

```iecst
// 여러 사이클에 걸친 테스트 예시
METHOD Test_MotorStartSequence
VAR
    nCycleCount : UINT;
END_VAR

TEST('모터 시작 시퀀스 테스트 (5 사이클)');

// 사이클 카운터 증가
nCycleCount := nCycleCount + 1;

CASE nCycleCount OF
    1:
        // 1사이클: 시작 명령
        fbMotor.bStart := TRUE;

    2:
        // 2사이클: 시작 명령 해제
        fbMotor.bStart := FALSE;

    3, 4:
        // 3-4사이클: 대기 (모터 가속 중)

    5:
        // 5사이클: 동작 상태 확인
        AssertTrue(
            Condition := fbMotor.bRunning,
            Message := '모터가 동작 중이어야 합니다'
        );

        // 모든 검증 완료 후 테스트 종료
        TEST_FINISHED();
END_CASE
END_METHOD
```

---

## 5. TcUnit-Runner - CI/CD 자동화 도구

### 5.1 TcUnit-Runner란?

[TcUnit-Runner](https://github.com/tcunit/TcUnit-Runner)는 TcUnit 테스트를 **명령줄에서 자동으로 실행**할 수 있게 해주는 도구입니다.

```
TcUnit-Runner의 역할:

┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│   Git 저장소     │────▶│  CI/CD 서버     │────▶│  TcUnit-Runner  │
│   (코드 변경)    │     │  (Jenkins 등)   │     │  (테스트 실행)   │
└─────────────────┘     └─────────────────┘     └─────────────────┘
                                                        │
                                                        ▼
                                               ┌─────────────────┐
                                               │  테스트 결과     │
                                               │  (xUnit XML)    │
                                               └─────────────────┘
```

### 5.2 왜 TcUnit-Runner가 필요한가?

| 수동 테스트 | TcUnit-Runner 사용 |
|-----------|-------------------|
| Visual Studio 필요 | 명령줄만으로 실행 |
| 사람이 직접 클릭 | 스크립트로 자동화 |
| 결과를 눈으로 확인 | XML 파일로 결과 저장 |
| CI/CD 불가능 | Jenkins/Azure DevOps 통합 |

### 5.3 설치 방법

```
설치 순서:

1. GitHub Releases에서 최신 버전 다운로드
   https://github.com/tcunit/TcUnit-Runner/releases

2. 압축 해제

3. 원하는 경로에 배치
   예: C:\Tools\TcUnit-Runner\

4. (선택) PATH 환경 변수에 추가
```

### 5.4 기본 사용법

```batch
:: 기본 실행
TcUnit-Runner.exe -p "C:\Projects\MyProject\MyProject.sln"

:: 결과 파일 지정
TcUnit-Runner.exe -p "C:\Projects\MyProject\MyProject.sln" ^
                  -r "C:\TestResults\results.xml"

:: 타임아웃 설정 (초)
TcUnit-Runner.exe -p "C:\Projects\MyProject\MyProject.sln" ^
                  -t 300
```

### 5.5 주요 명령줄 옵션

```
┌────────────┬────────────────────────────────────────┐
│   옵션      │                설명                    │
├────────────┼────────────────────────────────────────┤
│ -p, --path │ TwinCAT 솔루션 파일(.sln) 경로         │
│ -r, --result│ 결과 XML 파일 저장 경로               │
│ -t, --timeout│ 테스트 실행 타임아웃 (초)            │
│ -a, --amsnetid│ 대상 PLC의 AMS Net ID              │
│ -v, --verbose│ 상세 출력 모드                       │
└────────────┴────────────────────────────────────────┘
```

### 5.6 Jenkins 연동 예시

```groovy
// Jenkinsfile 예시
pipeline {
    agent any

    stages {
        stage('Checkout') {
            steps {
                // 소스 코드 체크아웃
                checkout scm
            }
        }

        stage('Unit Test') {
            steps {
                // TcUnit-Runner로 테스트 실행
                bat '''
                    C:\\Tools\\TcUnit-Runner\\TcUnit-Runner.exe ^
                    -p "%WORKSPACE%\\MyProject.sln" ^
                    -r "%WORKSPACE%\\test-results.xml" ^
                    -t 300
                '''
            }
        }

        stage('Publish Results') {
            steps {
                // JUnit 형식으로 결과 발행
                junit '**/test-results.xml'
            }
        }
    }

    post {
        failure {
            // 테스트 실패 시 알림
            emailext subject: 'TwinCAT 테스트 실패',
                     body: '단위 테스트가 실패했습니다. 확인 바랍니다.',
                     to: 'team@company.com'
        }
    }
}
```

### 5.7 Azure DevOps 연동

```yaml
# azure-pipelines.yml 예시
trigger:
  - main
  - develop

pool:
  vmImage: 'windows-latest'

steps:
- task: PowerShell@2
  displayName: 'TcUnit 테스트 실행'
  inputs:
    targetType: 'inline'
    script: |
      C:\Tools\TcUnit-Runner\TcUnit-Runner.exe `
        -p "$(Build.SourcesDirectory)\MyProject.sln" `
        -r "$(Build.SourcesDirectory)\test-results.xml"

- task: PublishTestResults@2
  displayName: '테스트 결과 발행'
  inputs:
    testResultsFormat: 'JUnit'
    testResultsFiles: '**/test-results.xml'
    failTaskOnFailedTests: true
```

---

## 6. Tc3_UnitTest - 간단한 대안

### 6.1 Tc3_UnitTest란?

[Tc3_UnitTest](https://github.com/PeterZerlauth/Tc3_UnitTest)는 Peter Zerlauth가 만든 **간단하고 가벼운** TwinCAT 3 단위 테스트 프레임워크입니다.

**특징:**
- ✅ 사용하기 매우 쉬움
- ✅ 설정이 간단함
- ✅ JUnit XML 형식 결과 출력
- ⚠️ TcUnit보다 기능이 제한적
- ⚠️ 문서화가 적음
- ⚠️ 커뮤니티 지원 제한적

### 6.2 언제 사용하는가?

```
Tc3_UnitTest 추천 상황:

✅ 소규모 프로젝트
✅ 빠르게 테스트를 시작하고 싶을 때
✅ 복잡한 기능이 필요 없을 때
✅ TcUnit이 너무 무겁게 느껴질 때

❌ 비추천 상황:
❌ 대규모 프로젝트
❌ CI/CD 자동화가 중요할 때
❌ 풍부한 Assert 메서드가 필요할 때
❌ 커뮤니티 지원이 필요할 때
```

### 6.3 TcUnit vs Tc3_UnitTest 비교

| 항목 | TcUnit | Tc3_UnitTest |
|------|--------|--------------|
| Assert 메서드 수 | 100개 이상 | 기본적인 것만 |
| CI/CD 도구 | TcUnit-Runner 제공 | 직접 구현 필요 |
| 문서화 | 공식 사이트 + 예제 | GitHub README만 |
| 멀티 사이클 테스트 | 지원 | 제한적 |
| 커뮤니티 | 활발 | 소규모 |

---

## 7. TcOpen - 산업용 애플리케이션 프레임워크

### 7.1 TcOpen이란?

[TcOpen](https://github.com/TcOpenGroup/TcOpen)은 TwinCAT 3와 .NET 기반의 **산업용 애플리케이션 프레임워크**입니다.

```
TcOpen의 범위:

┌─────────────────────────────────────────────────────────────┐
│                      TcOpen Framework                        │
├─────────────────────────────────────────────────────────────┤
│  ┌───────────┐  ┌───────────┐  ┌───────────┐  ┌───────────┐ │
│  │ 표준화된   │  │   HMI     │  │  테스팅    │  │  데이터    │ │
│  │ 컴포넌트   │  │   연동     │  │  (Prober) │  │  로깅     │ │
│  └───────────┘  └───────────┘  └───────────┘  └───────────┘ │
│                                                             │
│  ┌───────────────────────────────────────────────────────┐  │
│  │              .NET / C# 통합                            │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

### 7.2 Tc.Prober - TcOpen의 테스트 도구

Tc.Prober는 TcOpen 프로젝트의 일부로, **.NET 테스트 프레임워크**(NUnit, xUnit.NET)에서 TwinCAT PLC 코드를 테스트할 수 있게 해줍니다.

**작동 원리:**
```
C# 테스트 코드 → ADS 통신 → PLC 메서드 호출 → 결과 검증
```

### 7.3 시스템 요구사항

```
필수 환경:
┌─────────────────────────────────────────────────┐
│ • TwinCAT 3.1 Build 4024.17 이상                │
│ • Visual Studio 2019/2022                       │
│ • .NET Framework 4.8 또는 .NET 6+               │
│ • Inxton Vortex Tools (Visual Studio 확장)      │
└─────────────────────────────────────────────────┘

환경 변수 설정:
Tc3Target = [테스트 대상 PLC의 AMS Net ID]
```

### 7.4 장단점

**장점:**
- ✅ C# 개발자에게 친숙한 환경
- ✅ 풍부한 .NET 테스트 생태계 활용
- ✅ 복잡한 시나리오 테스트 가능
- ✅ UI 테스트와 통합 가능

**단점:**
- ❌ 설정이 복잡함
- ❌ .NET 지식 필요
- ❌ 순수 PLC 개발자에게 진입 장벽
- ⚠️ 프로젝트가 보관됨(Archived) 상태
- ⚠️ Inxton으로 개발 이전됨

### 7.5 추천 대상

```
TcOpen/Tc.Prober 추천 대상:

✅ .NET/C# 개발 경험이 있는 팀
✅ HMI와 PLC를 통합 테스트해야 하는 경우
✅ 기존에 .NET 테스트 인프라가 있는 경우
✅ 대규모 산업 프로젝트

❌ 비추천 대상:
❌ PLC만 단독으로 테스트하는 경우
❌ .NET 경험이 없는 팀
❌ 빠른 도입이 필요한 경우
```

---

## 8. 어떤 프레임워크를 선택해야 하는가?

### 최종 선택 가이드

```
                    당신의 상황은?
                         │
         ┌───────────────┼───────────────┐
         │               │               │
         ▼               ▼               ▼
    순수 PLC만        .NET 통합       빠르고 간단하게
    테스트 필요       필요            시작하고 싶음
         │               │               │
         ▼               ▼               ▼
    ┌─────────┐    ┌──────────┐    ┌─────────────┐
    │ TcUnit  │    │  TcOpen  │    │ Tc3_UnitTest│
    │   ⭐⭐⭐  │    │ /Prober  │    │     또는     │
    │  최추천  │    │   ⭐⭐    │    │   TcUnit    │
    └─────────┘    └──────────┘    └─────────────┘
         │
         ▼
    CI/CD 필요?
    ──────┬──────
     예   │   아니오
     ▼    │    ▼
  TcUnit  │  TcUnit
    +     │   만
  Runner  │  사용
```

### 프레임워크별 적합도

| 상황 | TcUnit | Tc3_UnitTest | TcOpen |
|------|--------|--------------|--------|
| 개인/소규모 프로젝트 | ⭐⭐⭐ | ⭐⭐⭐ | ⭐ |
| 팀 프로젝트 | ⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐ |
| CI/CD 필요 | ⭐⭐⭐ | ⭐ | ⭐⭐ |
| .NET 통합 | ⭐ | ⭐ | ⭐⭐⭐ |
| 문서/지원 | ⭐⭐⭐ | ⭐ | ⭐⭐ |
| 학습 곡선 (낮을수록 좋음) | ⭐⭐ | ⭐⭐⭐ | ⭐ |

### 결론: 대부분의 경우 TcUnit 추천

```
TcUnit을 추천하는 이유:

1. 가장 활발한 개발/지원
2. 풍부한 문서화
3. CI/CD 도구 제공 (TcUnit-Runner)
4. 글로벌 표준 xUnit 패턴
5. MIT 라이선스 (무료)
6. 검증된 산업 현장 사용 사례
```

---

## 9. 실전 적용 로드맵

### Phase 1: 기초 다지기 (1-2주)

```
목표: TcUnit 설치 및 첫 테스트 작성

할 일:
□ TcUnit 라이브러리 설치
□ 예제 프로젝트 다운로드 및 실행
□ 간단한 FB에 테스트 작성
□ Visual Studio에서 결과 확인 방법 익히기

참고 자료:
• TcUnit 공식 사이트: https://tcunit.org/
• 예제 프로젝트: https://github.com/tcunit/ExampleProjects
```

### Phase 2: 실전 적용 (2-4주)

```
목표: 실제 프로젝트에 테스트 적용

할 일:
□ 프로젝트 구조에 Test 폴더 추가
□ 핵심 Function Block 5-10개에 테스트 작성
□ 테스트 명명 규칙 수립
□ 팀원 교육 (1-2시간 세션)

팁:
• 새로 작성하는 코드부터 테스트 적용
• 버그 수정 시 해당 버그를 검증하는 테스트 추가
• 완벽하지 않아도 됨, 시작이 중요!
```

### Phase 3: 자동화 구축 (2-3주)

```
목표: CI/CD 파이프라인에 테스트 통합

할 일:
□ TcUnit-Runner 설치 및 설정
□ Jenkins/Azure DevOps 파이프라인 구성
□ 코드 커밋 시 자동 테스트 실행
□ 테스트 결과 대시보드 구성

성과 지표:
• 코드 변경마다 자동 테스트 실행
• 테스트 실패 시 즉시 알림
• 테스트 커버리지 추적
```

### Phase 4: 성숙화 (지속)

```
목표: 테스트 문화 정착

할 일:
□ 테스트 커버리지 목표 설정 (예: 60% → 80%)
□ 코드 리뷰 시 테스트 코드 필수화
□ 테스트 작성 가이드라인 문서화
□ 정기적인 테스트 코드 리팩토링

장기 목표:
• TDD (테스트 주도 개발) 도입
• 통합 테스트로 범위 확장
• 테스트 자동화 범위 확대
```

---

## 참고 자료

### 공식 문서 및 저장소

| 프레임워크 | 공식 사이트 | GitHub |
|-----------|------------|--------|
| TcUnit | [tcunit.org](https://tcunit.org/) | [tcunit/TcUnit](https://github.com/tcunit/TcUnit) |
| TcUnit-Runner | - | [tcunit/TcUnit-Runner](https://github.com/tcunit/TcUnit-Runner) |
| Tc3_UnitTest | - | [PeterZerlauth/Tc3_UnitTest](https://github.com/PeterZerlauth/Tc3_UnitTest) |
| TcOpen | - | [TcOpenGroup/TcOpen](https://github.com/TcOpenGroup/TcOpen) |

### 추가 학습 자료

- [AllTwinCAT 블로그 - TcUnit 시리즈](https://alltwincat.com/category/tcunit/)
- [Unit Testing in Industrial Automation](https://alltwincat.com/2021/02/16/unit-testing-in-the-world-of-industrial-automation/)
- [TcUnit DeepWiki 가이드](https://deepwiki.com/tcunit/TcUnit/3-user-guide)

### 커뮤니티

- GitHub Issues를 통한 질문/버그 리포트
- LinkedIn TwinCAT 그룹
- Beckhoff 공식 포럼

---

## 용어 정리

| 용어 | 설명 |
|------|------|
| **단위 테스트 (Unit Test)** | 소프트웨어의 가장 작은 단위를 독립적으로 테스트 |
| **테스트 Suite** | 관련된 테스트들의 집합 |
| **Assert** | 예상 결과와 실제 결과를 비교하여 검증 |
| **xUnit** | 단위 테스트 프레임워크의 표준 아키텍처 패턴 |
| **CI/CD** | 지속적 통합/지속적 배포 (Continuous Integration/Deployment) |
| **TDD** | 테스트 주도 개발 (Test-Driven Development) |
| **회귀 테스트** | 기존 기능이 새 변경으로 깨지지 않았는지 확인 |
| **테스트 커버리지** | 코드 중 테스트로 검증된 비율 |

---

> 📝 **문서 버전**: 1.0
> 📅 **작성일**: 2025-11-27
> 🔗 **참고**: 이 문서는 TwinCAT 단위 테스트 입문자를 위해 작성되었습니다.
