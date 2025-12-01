# TE1200 SA 규칙 개선 사항

## 개요

TE1200 폴더의 SA 규칙들을 실제 TwinCAT/IEC 61131-3 코드에서 문제를 검출할 수 있도록 개선했습니다.

## 개선 일자
2025-11-28

## 개선된 파일

### 1. SA0001_UnreachableCode.cs
**목적:** 도달 불가능한 코드 감지

**개선 사항:**
- ✅ RETURN/EXIT 후 코드 검사 로직 강화
  - 중첩 레벨(nesting level) 추적 개선
  - ELSE/ELSIF 처리 추가
  - 주석과 빈 줄 필터링 강화
  - 세미콜론 직후 종료자 감지

- ✅ 항상 false인 조건문 감지
  - `IF FALSE THEN`, `IF 0=1 THEN` 등

- ✅ 항상 true인 조건의 ELSE 블록 감지
  - `IF TRUE THEN ... ELSE` 패턴

**검출 예시:**
```iecst
// 도달 불가능한 코드
RETURN;
nValue := 10;  // ❌ 검출됨!

// 항상 FALSE 조건
IF 0 = 1 THEN
    DoSomething();  // ❌ 검출됨!
END_IF
```

---

### 2. SA0002_EmptyObjects.cs
**목적:** 빈 객체(POU) 감지

**개선 사항:**
- ✅ 정규식 패턴 개선
  - EXTENDS/IMPLEMENTS 지원
  - PUBLIC/PRIVATE 접근자 지원
  - VAR 블록은 제외하고 실행 코드만 검사

- ✅ 실행 코드 유무 정확히 판단
  - VAR...END_VAR 블록 제거 후 검사
  - 주석 제거 후 검사
  - 빈 공백만 있으면 빈 객체로 판단

**검출 예시:**
```iecst
// 빈 Function Block
FUNCTION_BLOCK FB_Empty
VAR
    nCounter : INT;
END_VAR
// 변수만 있고 코드 없음
END_FUNCTION_BLOCK  // ❌ 검출됨!

// 빈 Method
METHOD DoSomething
VAR_INPUT
    nValue : INT;
END_VAR
// 파라미터만 있고 본문 없음
END_METHOD  // ❌ 검출됨!
```

---

### 3. SA0003_SA0015.cs
**여러 규칙 개선:**

#### SA0003: 빈 문장
**개선 사항:**
- ✅ 빈 WHILE, FOR 루프 추가 감지
- ✅ 주석만 있는 블록도 빈 블록으로 처리

**검출 예시:**
```iecst
;  // ❌ 단독 세미콜론

IF bCondition THEN
END_IF  // ❌ 빈 IF 블록

WHILE bRun DO
END_WHILE  // ❌ 빈 WHILE - 무한 루프 위험!
```

#### SA0004: 출력에 대한 다중 쓰기
**개선 사항:**
- ✅ 3회 이상 할당 시에만 경고 (2회는 허용)
- ✅ FB 인스턴스와 THIS 변수 제외
- ✅ 배열 인덱스 할당 지원
- ✅ 주석과 빈 줄 필터링

**검출 예시:**
```iecst
nResult := 10;
nResult := 20;
nResult := 30;  // ❌ 3회 할당 검출!
```

#### SA0007: 상수에 대한 주소 연산자
**개선 사항:**
- ✅ 숫자 리터럴 감지 추가
- ✅ ADR() 안의 대상 추출 및 표시
- ✅ 구체적인 해결 방법 제시

**검출 예시:**
```iecst
pData := ADR(MAX_VALUE);  // ❌ 상수에 ADR
pData := ADR(100);        // ❌ 리터럴에 ADR
```

---

### 4. SA0016_SA0030.cs
**여러 규칙 개선:**

#### SA0016: 구조체의 갭
**개선 사항:**
- ✅ 실제 멤버 타입 크기 분석
- ✅ 비최적 레이아웃 감지 (작은 타입 뒤 큰 타입)
- ✅ 타입별 크기 매핑 (LREAL=8, REAL=4, INT=2, BOOL=1)

**검출 예시:**
```iecst
TYPE ST_Data :
STRUCT
    bFlag : BOOL;      // 1바이트
    rValue : LREAL;    // 8바이트 - ❌ 7바이트 패딩 발생!
END_STRUCT
END_TYPE

// ✅ 권장: 큰 타입부터 배치
TYPE ST_DataOptimized :
STRUCT
    rValue : LREAL;    // 8바이트
    bFlag : BOOL;      // 1바이트 - 패딩 최소화
END_STRUCT
END_TYPE
```

#### SA0017: 포인터 변수에 비정상 할당
**개선 사항:**
- ✅ 정규식 패턴 강화 (POINTER TO 타입 지원)
- ✅ ADR, NULL, 0, 변수 할당은 허용
- ✅ 숫자 리터럴 직접 할당만 검출
- ✅ 할당된 값 추출 및 표시

**검출 예시:**
```iecst
pData := ADR(variable);  // ✅ 허용
pData := 0;              // ✅ 허용 (NULL 초기화)
pData := 16#12345678;    // ❌ 검출! 위험한 주소 할당
```

#### SA0018: 비정상적인 비트 접근
**개선 사항:**
- ✅ 부호 있는 타입 명명 패턴 감지 강화
- ✅ INT/DINT 리터럴 비트 접근 감지
- ✅ 비트 마스킹 대안 제시

**검출 예시:**
```iecst
nStatus.0  // ❌ 부호 있는 INT에 비트 접근
INT#100.5  // ❌ 리터럴에 비트 접근

// ✅ 권장: 비트 마스킹
IF (nStatus AND 16#0001) <> 0 THEN
```

#### SA0030: 에러 처리 누락
**개선 사항:**
- ✅ 0으로 나누기 검사 강화
  - 주변 5줄 내 0 검사 확인
  - 숫자 리터럴 제외
  - 구체적인 변수명 표시

- ✅ **배열 인덱스 범위 검사 추가** (새 기능!)
  - 변수 인덱스에 대한 범위 검사 권장
  - 숫자 리터럴 인덱스는 제외

**검출 예시:**
```iecst
// 0으로 나누기
result := nValue / nDivisor;  // ❌ nDivisor 0 검사 없음!

// ✅ 권장
IF nDivisor <> 0 THEN
    result := nValue / nDivisor;
ELSE
    result := 0;
END_IF

// 배열 범위 검사
arrData[nIndex] := 10;  // ❌ nIndex 범위 검사 권장!

// ✅ 권장
IF nIndex >= 0 AND nIndex <= UPPER_BOUND(arrData, 1) THEN
    arrData[nIndex] := 10;
END_IF
```

---

## 주요 개선 포인트 요약

### 1. 더 일반적인 패턴 검사
- ❌ 이전: `_`로 시작하거나 "Unused" 포함된 변수만 검사
- ✅ 개선: 실제 코드 구조와 패턴 분석

### 2. 실행 코드와 선언부 구분
- VAR 블록, 주석, 빈 줄 제외
- 실제 실행 코드만 검사

### 3. 맥락 기반 검사
- 주변 코드 분석 (예: 0 검사 주변 5줄 확인)
- 중첩 레벨 추적
- 타입별 크기 고려

### 4. 오탐 감소
- FB 인스턴스, THIS, 시스템 변수 제외
- 숫자 리터럴과 변수 구분
- 2회 할당은 허용 (3회부터 경고)

### 5. 구체적인 피드백
- 문제가 되는 변수명/값 표시
- 발생 위치(라인 번호) 정확히 표시
- 실용적인 해결 방법 제시

---

## 실제 검출 가능한 패턴

### PM1 프로젝트에서 검출 가능한 예시

```iecst
// 1. 도달 불가능한 코드
METHOD ProcessData
IF bError THEN
    RETURN;
    nCounter := nCounter + 1;  // ❌ SA0001
END_IF
END_METHOD

// 2. 빈 Function Block
FUNCTION_BLOCK FB_Placeholder
VAR
    bReady : BOOL;
END_VAR
// TODO: 구현 예정
END_FUNCTION_BLOCK  // ❌ SA0002

// 3. 다중 쓰기
nOutput := nInput1;
nOutput := nInput2;
nOutput := nInput3;  // ❌ SA0004

// 4. 포인터 비정상 할당
pBuffer := 16#1000;  // ❌ SA0017

// 5. 0으로 나누기 미검사
rSpeed := rDistance / rTime;  // ❌ SA0030

// 6. 배열 범위 미검사
FOR i := 0 TO nCount DO
    arrValues[i] := i * 10;  // ❌ SA0030 (nCount 범위 검사 필요)
END_FOR

// 7. 구조체 메모리 비효율
TYPE ST_SensorData :
STRUCT
    bActive : BOOL;        // 1 바이트
    rTemperature : LREAL;  // 8 바이트 - ❌ SA0016 (7바이트 낭비)
    nID : INT;             // 2 바이트
END_STRUCT
END_TYPE
```

---

## 테스트 권장 사항

### 1. PM1 프로젝트 전체 스캔
```bash
dotnet run -- scan -p "D:\01. Vscode\Twincat\PM1" -r SA0001,SA0002,SA0004,SA0017,SA0030
```

### 2. 개별 규칙 테스트
각 규칙별로 테스트 케이스 작성 및 검증

### 3. False Positive 확인
실제 프로젝트에서 오탐 비율 측정 및 추가 개선

---

## 향후 개선 계획

1. **심볼 테이블 통합**
   - 전역/로컬 변수 구분
   - 타입 정보 정확히 파악

2. **제어 흐름 분석 강화**
   - 복잡한 중첩 구조 처리
   - 루프와 분기 조합

3. **데이터 흐름 분석**
   - 변수 사용 추적
   - 초기화 여부 확인

4. **통계 기반 임계값 조정**
   - 프로젝트별 특성 반영
   - 사용자 설정 가능

---

## 결론

이번 개선으로 TE1200 SA 규칙들이 실제 TwinCAT 프로젝트에서 의미 있는 이슈를 검출할 수 있게 되었습니다. 특히 안전성(Safety)과 관련된 규칙들(0으로 나누기, 포인터 할당, 배열 범위)이 강화되어 런타임 오류를 사전에 방지할 수 있습니다.
