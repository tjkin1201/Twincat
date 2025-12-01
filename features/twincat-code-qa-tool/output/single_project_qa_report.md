# TwinCAT 프로젝트 QA 분석 리포트

**분석 일시**: 2025-11-25T18:12:51.974324
**프로젝트 경로**: `D:\00.Comapre\pollux_hcds_ald_mirror_ffff\Src_Diff\PLC\PM1\PM1`

## 📊 프로젝트 요약

| 항목 | 값 |
|------|-----|
| 총 파일 수 | 301개 |
| POU (프로그램/FB/함수) | 124개 |
| GVL (전역 변수) | 7개 |
| DUT (데이터 타입) | 170개 |
| 총 코드 라인 | 54,630줄 |
| **총 QA 이슈** | **8296개** |
| 🔴 Critical | 439개 |
| 🟡 Warning | 4646개 |
| 🔵 Info | 3211개 |

## 📈 카테고리별 이슈

| 카테고리 | 건수 | 설명 |
|----------|------|------|
| Safety | 445개 | 안전 - 잠재적 버그, 런타임 오류 |
| Performance | 1개 | 성능 - 메모리, 실행 속도 |
| Maintainability | 6293개 | 유지보수 - 가독성, 복잡도 |
| Style | 1557개 | 스타일 - 명명 규칙, 코딩 표준 |

## 🔴 Critical Issues (즉시 검토 필요)

| 파일 | 라인 | 규칙 | 메시지 |
|------|------|------|--------|
| FindValue_Inverse_TempParaTable.TcPOU | 11 | QA001 | 초기화되지 않은 중요 변수 (REAL/LREAL/포인터) |
| FindValue_Inverse_TempParaTable.TcPOU | 12 | QA001 | 초기화되지 않은 중요 변수 (REAL/LREAL/포인터) |
| FindValue_Inverse_TempParaTable.TcPOU | 14 | QA001 | 초기화되지 않은 중요 변수 (REAL/LREAL/포인터) |
| FindValue_Inverse_TempParaTable.TcPOU | 15 | QA001 | 초기화되지 않은 중요 변수 (REAL/LREAL/포인터) |
| FindValue_TempParaTable.TcPOU | 11 | QA001 | 초기화되지 않은 중요 변수 (REAL/LREAL/포인터) |
| FindValue_TempParaTable.TcPOU | 12 | QA001 | 초기화되지 않은 중요 변수 (REAL/LREAL/포인터) |
| FindValue_TempParaTable.TcPOU | 14 | QA001 | 초기화되지 않은 중요 변수 (REAL/LREAL/포인터) |
| FindValue_TempParaTable.TcPOU | 15 | QA001 | 초기화되지 않은 중요 변수 (REAL/LREAL/포인터) |
| F_Driver_APC_PID.TcPOU | 4 | QA001 | 초기화되지 않은 중요 변수 (REAL/LREAL/포인터) |
| F_Driver_Heater_Write_Array.TcPOU | 12 | QA001 | 초기화되지 않은 중요 변수 (REAL/LREAL/포인터) |
| F_EventLog_Shift.TcPOU | 11 | QA001 | 초기화되지 않은 중요 변수 (REAL/LREAL/포인터) |
| F_Function_LV_Log.TcPOU | 3 | QA001 | 초기화되지 않은 중요 변수 (REAL/LREAL/포인터) |
| F_Function_Process_Log.TcPOU | 3 | QA001 | 초기화되지 않은 중요 변수 (REAL/LREAL/포인터) |
| F_Get_NextJumpStep.TcPOU | 5 | QA001 | 초기화되지 않은 중요 변수 (REAL/LREAL/포인터) |
| F_Get_Parameter.TcPOU | 4 | QA006 | 0으로 나누기 가능성 |
| F_Get_Parameter.TcPOU | 8 | QA006 | 0으로 나누기 가능성 |
| F_Parameter_Check.TcPOU | 4 | QA001 | 초기화되지 않은 중요 변수 (REAL/LREAL/포인터) |
| F_Parameter_Check.TcPOU | 11 | QA001 | 초기화되지 않은 중요 변수 (REAL/LREAL/포인터) |
| F_Parameter_Check.TcPOU | 1 | QA006 | 0으로 나누기 가능성 |
| F_RealRound.TcPOU | 3 | QA001 | 초기화되지 않은 중요 변수 (REAL/LREAL/포인터) |
| F_Set_Parameter.TcPOU | 3 | QA001 | 초기화되지 않은 중요 변수 (REAL/LREAL/포인터) |
| F_Set_Parameter.TcPOU | 11 | QA001 | 초기화되지 않은 중요 변수 (REAL/LREAL/포인터) |
| F_Set_Parameter.TcPOU | 1 | QA006 | 0으로 나누기 가능성 |
| Get_IEEE754.TcPOU | 7 | QA001 | 초기화되지 않은 중요 변수 (REAL/LREAL/포인터) |
| Put_IEEE754.TcPOU | 3 | QA001 | 초기화되지 않은 중요 변수 (REAL/LREAL/포인터) |
| REAL_ROUND.TcPOU | 3 | QA001 | 초기화되지 않은 중요 변수 (REAL/LREAL/포인터) |
| REAL_TO_STR.TcPOU | 3 | QA001 | 초기화되지 않은 중요 변수 (REAL/LREAL/포인터) |
| REAL_TO_STR.TcPOU | 7 | QA001 | 초기화되지 않은 중요 변수 (REAL/LREAL/포인터) |
| REAL_TO_STR.TcPOU | 6 | QA002 | 위험한 타입 변환: REAL→DINT |
| FB_BasicPID_RSD.TcPOU | 4 | QA001 | 초기화되지 않은 중요 변수 (REAL/LREAL/포인터) |
| FB_BasicPID_RSD.TcPOU | 5 | QA001 | 초기화되지 않은 중요 변수 (REAL/LREAL/포인터) |
| FB_BasicPID_RSD.TcPOU | 9 | QA001 | 초기화되지 않은 중요 변수 (REAL/LREAL/포인터) |
| FB_BasicPID_RSD.TcPOU | 15 | QA001 | 초기화되지 않은 중요 변수 (REAL/LREAL/포인터) |
| FB_BasicPID_RSD.TcPOU | 16 | QA001 | 초기화되지 않은 중요 변수 (REAL/LREAL/포인터) |
| FB_BasicPID_RSD.TcPOU | 17 | QA001 | 초기화되지 않은 중요 변수 (REAL/LREAL/포인터) |
| FB_BasicPID_RSD.TcPOU | 18 | QA001 | 초기화되지 않은 중요 변수 (REAL/LREAL/포인터) |
| FB_BasicPID_RSD.TcPOU | 23 | QA001 | 초기화되지 않은 중요 변수 (REAL/LREAL/포인터) |
| FB_Collection.TcPOU | 19 | QA006 | 0으로 나누기 가능성 |
| FB_Control_Gas.TcPOU | 15 | QA001 | 초기화되지 않은 중요 변수 (REAL/LREAL/포인터) |
| FB_Control_Gas.TcPOU | 16 | QA001 | 초기화되지 않은 중요 변수 (REAL/LREAL/포인터) |
| FB_Control_Gas.TcPOU | 17 | QA001 | 초기화되지 않은 중요 변수 (REAL/LREAL/포인터) |
| FB_Control_Gas.TcPOU | 18 | QA001 | 초기화되지 않은 중요 변수 (REAL/LREAL/포인터) |
| FB_Control_Gas.TcPOU | 19 | QA001 | 초기화되지 않은 중요 변수 (REAL/LREAL/포인터) |
| FB_Control_Gas.TcPOU | 20 | QA001 | 초기화되지 않은 중요 변수 (REAL/LREAL/포인터) |
| FB_Control_Gas.TcPOU | 21 | QA001 | 초기화되지 않은 중요 변수 (REAL/LREAL/포인터) |
| FB_Control_Gas.TcPOU | 22 | QA001 | 초기화되지 않은 중요 변수 (REAL/LREAL/포인터) |
| FB_Control_Gas.TcPOU | 23 | QA001 | 초기화되지 않은 중요 변수 (REAL/LREAL/포인터) |
| FB_Control_Gas.TcPOU | 24 | QA001 | 초기화되지 않은 중요 변수 (REAL/LREAL/포인터) |
| FB_Control_Gas.TcPOU | 42 | QA001 | 초기화되지 않은 중요 변수 (REAL/LREAL/포인터) |
| FB_Control_Gas.TcPOU | 43 | QA001 | 초기화되지 않은 중요 변수 (REAL/LREAL/포인터) |
| ... | | | *외 389개* |

## 📋 규칙별 이슈 통계

| 규칙 ID | 심각도 | 카테고리 | 건수 |
|---------|--------|----------|------|
| QA001 | 🔴 Critical | Safety | 371개 |
| QA002 | 🔴 Critical | Safety | 23개 |
| QA003 | 🟡 Warning | Performance | 1개 |
| QA004 | 🟡 Warning | Safety | 6개 |
| QA005 | 🔴 Critical | Safety | 1개 |
| QA006 | 🔴 Critical | Safety | 44개 |
| QA007 | 🟡 Warning | Maintainability | 4242개 |
| QA008 | 🟡 Warning | Maintainability | 14개 |
| QA009 | 🟡 Warning | Maintainability | 21개 |
| QA010 | 🟡 Warning | Maintainability | 316개 |
| QA013 | 🔵 Info | Maintainability | 1590개 |
| QA014 | 🟡 Warning | Maintainability | 46개 |
| QA015 | 🔵 Info | Maintainability | 64개 |
| QA016 | 🔵 Info | Style | 1557개 |

## ⚠️ 복잡도 높은 파일 (Top 10)

| 파일 | 타입 | 라인수 | 복잡도 | 이슈수 |
|------|------|--------|--------|--------|
| SEQ_Function_Process | PROGRAM | 7285 | 1254 | 477 |
| SEQ_Physical_LoadLock | PROGRAM | 1925 | 386 | 18 |
| SEQ_Physical_Boat_Elevator | PROGRAM | 1855 | 339 | 109 |
| SEQ_Interlock_Valve | PROGRAM | 5060 | 286 | 4174 |
| SEQ_Driver_TCPIP | PROGRAM | 1893 | 260 | 162 |
| SEQ_Interface | PROGRAM | 990 | 176 | 110 |
| SEQ_Temp_Control | PROGRAM | 1311 | 169 | 145 |
| SEQ_Function_Auto_PID | PROGRAM | 925 | 153 | 21 |
| SEQ_Physical_APC | PROGRAM | 832 | 135 | 105 |
| SEQ_Interlock_Safety | PROGRAM | 1385 | 119 | 356 |
