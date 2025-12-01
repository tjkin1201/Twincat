# TwinCAT Code QA Report

**분석 일시**: 2025-11-25T18:02:07.992131
**이전 버전**: `D:\00.Comapre\pollux_hcds_ald_mirror\Src_Diff\PLC\PM1\PM1`
**새 버전**: `D:\00.Comapre\pollux_hcds_ald_mirror_ffff\Src_Diff\PLC\PM1\PM1`

## 📊 요약

| 항목 | 값 |
|------|-----|
| 총 파일 변경 | 49개 |
| 파일 추가 | 3개 |
| 파일 삭제 | 2개 |
| 파일 수정 | 44개 |
| 변수 변경 | 41개 |
| **QA 이슈 총계** | **1830개** |
| 🔴 Critical | 7개 |
| 🟡 Warning | 1823개 |
| 🔵 Info | 0개 |

## 🔴 Critical Issues

| 파일 | 라인 | 규칙 | 메시지 |
|------|------|------|--------|
| SEQ_Temp_Control.TcPOU | 218 | QA002 | 위험한 타입 변환: REAL→INT |
| SEQ_Temp_Control.TcPOU | 222 | QA002 | 위험한 타입 변환: REAL→INT |
| SEQ_Temp_Control.TcPOU | 226 | QA002 | 위험한 타입 변환: REAL→INT |
| SEQ_Temp_Control.TcPOU | 945 | QA002 | 위험한 타입 변환: LREAL→REAL |
| SEQ_Temp_Control.TcPOU | 946 | QA002 | 위험한 타입 변환: LREAL→REAL |
| SEQ_Temp_Control.TcPOU | 947 | QA002 | 위험한 타입 변환: LREAL→REAL |
| SEQ_Driver_TCPIP.TcPOU | 1806 | QA002 | 위험한 타입 변환: REAL→INT |

## 🟡 Warning Issues

| 파일 | 라인 | 규칙 | 메시지 |
|------|------|------|--------|
| FB_Temp_DEV_TC_to_TC.TcPOU | 3 | QA007 | 매직 넘버 사용: 1000 |
| SEQ_Driver.TcPOU | 8 | QA007 | 매직 넘버 사용: 50 |
| SEQ_Driver.TcPOU | 21 | QA007 | 매직 넘버 사용: 10 |
| SEQ_Driver.TcPOU | 22 | QA007 | 매직 넘버 사용: 10 |
| SEQ_Driver.TcPOU | 31 | QA007 | 매직 넘버 사용: 1544 |
| SEQ_Driver.TcPOU | 64 | QA007 | 매직 넘버 사용: 894733 |
| SEQ_Driver.TcPOU | 71 | QA007 | 매직 넘버 사용: 1000 |
| SEQ_Driver.TcPOU | 106 | QA007 | 매직 넘버 사용: 1000 |
| SEQ_Driver.TcPOU | 116 | QA007 | 매직 넘버 사용: 1000 |
| SEQ_Driver.TcPOU | 135 | QA007 | 매직 넘버 사용: 1000 |
| SEQ_Driver.TcPOU | 145 | QA007 | 매직 넘버 사용: 1000 |
| SEQ_Driver.TcPOU | 164 | QA007 | 매직 넘버 사용: 1000 |
| SEQ_Driver.TcPOU | 174 | QA007 | 매직 넘버 사용: 1000 |
| SEQ_Driver.TcPOU | 193 | QA007 | 매직 넘버 사용: 1000 |
| SEQ_Driver.TcPOU | 203 | QA007 | 매직 넘버 사용: 1000 |
| SEQ_Driver.TcPOU | 222 | QA007 | 매직 넘버 사용: 1000 |
| SEQ_Driver.TcPOU | 232 | QA007 | 매직 넘버 사용: 1000 |
| SEQ_Driver.TcPOU | 251 | QA007 | 매직 넘버 사용: 1000 |
| SEQ_Driver.TcPOU | 261 | QA007 | 매직 넘버 사용: 1000 |
| SEQ_Driver.TcPOU | 280 | QA007 | 매직 넘버 사용: 1000 |
| SEQ_Driver.TcPOU | 290 | QA007 | 매직 넘버 사용: 1000 |
| SEQ_Driver.TcPOU | 309 | QA007 | 매직 넘버 사용: 1000 |
| SEQ_Driver.TcPOU | 319 | QA007 | 매직 넘버 사용: 1000 |
| SEQ_Driver.TcPOU | 338 | QA007 | 매직 넘버 사용: 1000 |
| SEQ_Driver.TcPOU | 348 | QA007 | 매직 넘버 사용: 1000 |
| SEQ_Driver.TcPOU | 367 | QA007 | 매직 넘버 사용: 1000 |
| SEQ_Driver.TcPOU | 377 | QA007 | 매직 넘버 사용: 1000 |
| SEQ_Driver.TcPOU | 396 | QA007 | 매직 넘버 사용: 1000 |
| SEQ_Driver.TcPOU | 406 | QA007 | 매직 넘버 사용: 1000 |
| SEQ_Driver.TcPOU | 425 | QA007 | 매직 넘버 사용: 1000 |
| ... | ... | ... | *외 1793개* |

## 📁 파일 변경 목록

- ➕ `POLY\POUs\Function\F_Get_NextJumpStep.TcPOU` (Added)
- ➕ `POLY\Data types\eDesc_TargetZone.TcDUT` (Added)
- ➕ `POLY\POUs\Function Block\FB_Temp_DEV_TC_to_TC.TcPOU` (Added)
- ➖ `POLY\POUs\Function\F_Cycle_StepCount_1.TcPOU` (Deleted)
- ➖ `POLY\POUs\Function\F_Cycle_StepCount.TcPOU` (Deleted)
- 📝 `POLY\Data types\tRecipe.TcDUT` (Modified)
- 📝 `POLY\POUs\Sequence\SEQ_Driver.TcPOU` (Modified)
- 📝 `POLY\Data types\tParameter_CAL_PID.TcDUT` (Modified)
- 📝 `POLY\Data types\tParameter_RDNPTune.TcDUT` (Modified)
- 📝 `POLY\POUs\Sequence\SEQ_Interface.TcPOU` (Modified)
- 📝 `POLY\Data types\tParameter_Heater_TC.TcDUT` (Modified)
- 📝 `POLY\Data types\tParameter_Heater_ALARM.TcDUT` (Modified)
- 📝 `POLY\Data types\tParameter_FRPTV.TcDUT` (Modified)
- 📝 `POLY\Global Variables\Global_Variables_Persistent.TcGVL` (Modified)
- 📝 `POLY\POUs\Sequence\SEQ_Interlock_Safety.TcPOU` (Modified)
- 📝 `POLY\Data types\tParameter_Gas.TcDUT` (Modified)
- 📝 `POLY\Data types\tParameter_Heater_AWC.TcDUT` (Modified)
- 📝 `POLY\POUs_10ms\Sequence\LOG_Dcoll.TcPOU` (Modified)
- 📝 `POLY\POUs\System\SYS_TrendData.TcPOU` (Modified)
- 📝 `POLY\Global Variables\Global_Variables_Memory.TcGVL` (Modified)
- 📝 `POLY\POUs\TempController\SEQ_Temp_Control.TcPOU` (Modified)
- 📝 `POLY\POUs\Sequence\SEQ_Physical_Heater_Shutter.TcPOU` (Modified)
- 📝 `POLY\Data types\tParameter_Heater_ITD.TcDUT` (Modified)
- 📝 `POLY\POUs\Function Block\FB_Sdo_Gas.TcPOU` (Modified)
- 📝 `POLY\Data types\tParameter_Heater_PID.TcDUT` (Modified)
- 📝 `POLY\Data types\tRecipeBody.TcDUT` (Modified)
- 📝 `POLY\POLY.plcproj` (Modified)
- 📝 `POLY\Data types\tParameter_TempTarget.TcDUT` (Modified)
- 📝 `POLY\POUs\Sequence\SEQ_Function_Process.TcPOU` (Modified)
- 📝 `POLY\Data types\tParameter_Pressure_Range.TcDUT` (Modified)
- 📝 `POLY\Data types\tParameter_LH_ALARM.TcDUT` (Modified)
- 📝 `POLY\Data types\tParameter_Intensity_Range.TcDUT` (Modified)
- 📝 `POLY\Data types\tParameter_Heater_Sub.TcDUT` (Modified)
- 📝 `POLY\Data types\tParameter_RSD.TcDUT` (Modified)
- 📝 `POLY\Data types\tParameter_LH_SET.TcDUT` (Modified)
- 📝 `POLY\Global Variables\Global_Variables_Constant.TcGVL` (Modified)
- 📝 `POLY\POUs\System\SYS_DataExchange.TcPOU` (Modified)
- 📝 `POLY\Data types\tParameterPartsPara.TcDUT` (Modified)
- 📝 `POLY\Data types\tParameter_Alarm.TcDUT` (Modified)
- 📝 `POLY\Data types\tParameter_FFU.TcDUT` (Modified)
- 📝 `POLY\POUs\TempController\SEQ_Driver_X20_ModBusTCP.TcPOU` (Modified)
- 📝 `POLY\Data types\eDesc_AutoProfile_Target.TcDUT` (Modified)
- 📝 `POLY\Data types\tParameter_Heater_POWER.TcDUT` (Modified)
- 📝 `POLY\POUs\Sequence\SEQ_Driver_TCPIP.TcPOU` (Modified)
- 📝 `POLY\Data types\tParameter_ProfileTune.TcDUT` (Modified)
- 📝 `POLY\Global Variables\Global_Variables_IO.TcGVL` (Modified)
- 📝 `POLY\Data types\tParameter_Heater_RDNP.TcDUT` (Modified)
- 📝 `POLY\POUs\MAIN.TcPOU` (Modified)
- 📝 `POLY\POUs\Sequence\SEQ_Driver_O2Analyzer.TcPOU` (Modified)

## ⚠️ 변수 타입 변경

| 파일 | 변수명 | 이전 타입 | 새 타입 |
|------|--------|-----------|---------|
| SEQ_Driver_TCPIP.TcPOU | Monitor_Step | BYTE | INT |
