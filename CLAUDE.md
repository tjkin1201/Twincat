# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 프로젝트 개요

이 프로젝트는 TwinCAT (The Windows Control and Automation Technology) 기반 자동화 시스템 프로젝트입니다.
TwinCAT은 Beckhoff의 PC 기반 제어 소프트웨어로, 실시간 제어와 자동화를 위한 통합 개발 환경입니다.

## 언어 설정

**필수 규칙:**
- 모든 대화는 한글로 진행
- 모든 코드 주석은 한글로 작성
- 모든 커밋 메시지는 한글로 작성
- 모든 문서는 한글로 작성
- 변수/함수명은 영어 사용 (코드 호환성)

## TwinCAT 개발 환경

### 필수 도구
- **TwinCAT XAE (eXtended Automation Engineering)**: Visual Studio 기반 통합 개발 환경
- **TwinCAT XAR (eXtended Automation Runtime)**: 실시간 런타임 환경
- **Visual Studio**: TwinCAT XAE Shell 또는 통합 버전

### 주요 프로그래밍 언어 (IEC 61131-3 표준)
- **ST (Structured Text)**: 고급 텍스트 기반 언어
- **LD (Ladder Diagram)**: 래더 로직
- **FBD (Function Block Diagram)**: 함수 블록 다이어그램
- **SFC (Sequential Function Chart)**: 순차 기능 차트
- **IL (Instruction List)**: 명령어 목록

## 일반적인 명령어

### TwinCAT 프로젝트 작업
```bash
# TwinCAT XAE Shell 실행 (Visual Studio가 설치된 경우)
# Visual Studio에서 .sln 파일 열기

# TwinCAT 시스템 모드 전환 (관리자 권한 필요)
# TwinCAT System Manager에서 Config -> Run-Mode 설정

# 빌드
# Visual Studio: Build -> Build Solution (Ctrl+Shift+B)

# PLC 프로젝트 활성화
# Solution Explorer -> Activate Configuration
```

### Git 작업
```bash
# 상태 확인
git status

# 변경사항 스테이징
git add .

# 커밋 (한글 메시지)
git commit -m "기능: PLC 프로그램 추가"
git commit -m "수정: 모션 제어 로직 버그 수정"

# 푸시
git push origin main
```

## 프로젝트 구조

TwinCAT 프로젝트의 일반적인 구조:

```
TwinCAT/
├── ProjectName.sln                 # Visual Studio 솔루션 파일
├── ProjectName/
│   ├── ProjectName.tsproj         # TwinCAT 프로젝트 파일
│   ├── _Config/                   # 설정 파일
│   ├── PLC/                       # PLC 프로그램
│   │   ├── POUs/                  # Program Organization Units
│   │   │   ├── MAIN.TcPOU        # 메인 프로그램
│   │   │   └── FBs/              # Function Blocks
│   │   ├── DUTs/                  # Data Unit Types (구조체, 열거형)
│   │   ├── GVLs/                  # Global Variable Lists
│   │   └── VISUs/                 # Visualizations
│   ├── IO/                        # I/O 설정
│   ├── NC/                        # Motion Control (해당되는 경우)
│   └── System/                    # 시스템 설정
└── docs/                          # 문서 (프로젝트별로 생성)
```

## 아키텍처 및 설계 원칙

### PLC 프로그래밍 원칙
- **모듈화**: 재사용 가능한 Function Block (FB) 설계
- **캡슐화**: FB 내부 로직을 VAR_INPUT/VAR_OUTPUT으로 명확히 정의
- **상태 머신**: 복잡한 시퀀스는 CASE 문을 사용한 상태 머신으로 구현
- **안전성**: 비상 정지 및 안전 로직 우선 처리

### 명명 규칙
```iecst
// Function Blocks: FB_ 접두사
FUNCTION_BLOCK FB_ConveyorControl

// 전역 변수: g 접두사
VAR_GLOBAL
    gSystemStatus : INT;
END_VAR

// 로컬 변수: 소문자로 시작
VAR
    currentSpeed : REAL;
    isRunning : BOOL;
END_VAR

// 상수: 대문자
VAR CONSTANT
    MAX_SPEED : REAL := 100.0;
END_VAR
```

### 주석 작성 (한글)
```iecst
// 좋은 예시
FUNCTION_BLOCK FB_MotorControl
    // 모터 속도 제어 함수 블록
    // 입력: targetSpeed (목표 속도), enable (활성화)
    // 출력: currentSpeed (현재 속도), fault (고장 상태)
VAR_INPUT
    targetSpeed : REAL;  // 목표 속도 [rpm]
    enable : BOOL;       // 모터 활성화
END_VAR
```

## 파일 조직

### 작업 파일 저장 위치
- `/src` - 소스 코드 파일 (ST 언어 외부 파일)
- `/tests` - 테스트 파일
- `/docs` - 문서 및 마크다운 파일
- `/config` - 설정 파일
- `/scripts` - 유틸리티 스크립트

**중요**: 루트 폴더에 작업 파일, 텍스트/마크다운, 테스트 파일을 저장하지 마세요.

## TwinCAT 특수 고려사항

### 실시간 제어
- PLC 사이클 타임 고려 (일반적으로 1-10ms)
- 복잡한 연산은 여러 사이클에 분산
- 시간 지연이 중요한 로직은 인터럽트 사용

### 데이터 타입
- `BOOL`, `BYTE`, `WORD`, `DWORD`, `LWORD`
- `INT`, `DINT`, `LINT` (정수)
- `REAL`, `LREAL` (부동소수점)
- `TIME`, `DT` (시간)
- `STRING` (문자열)

### 디버깅
- Online View에서 변수 값 실시간 모니터링
- Watch Window 활용
- Breakpoint 설정 가능
- Scope View로 신호 파형 관찰

## Git 관리

### 제외할 파일 (.gitignore 권장)
```
# TwinCAT 생성 파일
*.tmc
*.tpy
*.compiled-library
_Boot/
_CompileInfo/
_Libraries/
.vs/
bin/
obj/

# Visual Studio
*.suo
*.user
*.userosscache
*.sln.docstates
```

### 커밋 메시지 형식
```
기능: [새 기능 설명]
수정: [버그 수정 설명]
문서: [문서 업데이트 설명]
리팩토링: [코드 개선 설명]
```

## 협업 가이드

### 코드 리뷰 체크리스트
- [ ] 한글 주석 작성 여부
- [ ] 안전 로직 포함 여부
- [ ] 상태 머신 명확성
- [ ] Function Block 재사용성
- [ ] 변수 명명 규칙 준수

### 병합 전 확인사항
- 빌드 오류 없음
- 시뮬레이션 테스트 통과
- 주석 및 문서 업데이트
- 코드 리뷰 승인

---

**참고**: 이 문서는 TwinCAT 프로젝트의 기본 구조를 설명합니다. 프로젝트가 진행되면서 구체적인 아키텍처와 모듈 정보로 업데이트하세요.
