# TwinCAT QA 상세 분석 리포트

**프로젝트**: `D:\00.Comapre\pollux_hcds_ald_mirror_ffff\Src_Diff\PLC\PM1\PM1`
**분석 일시**: 2025-11-25T18:12:51.974324

## 📊 분석 요약

- **총 파일**: 301개
- **총 코드**: 54,630줄
- **총 이슈**: 8,296개
  - 🔴 Critical: 439개
  - 🟡 Warning: 4646개
  - 🔵 Info: 3211개

## 🔴 Critical Issues 상세

### QA001 (371건)

#### 📍 FindValue_Inverse_TempParaTable.TcPOU:11

**메시지**: 초기화되지 않은 중요 변수 (REAL/LREAL/포인터)

```iecst
Range_1_Value						: REAL;
```

**💡 수정 제안**: 선언 시 초기값을 명시하세요: var : TYPE := 초기값;

---

#### 📍 FindValue_Inverse_TempParaTable.TcPOU:12

**메시지**: 초기화되지 않은 중요 변수 (REAL/LREAL/포인터)

```iecst
Range_2_Value						: REAL;
```

**💡 수정 제안**: 선언 시 초기값을 명시하세요: var : TYPE := 초기값;

---

#### 📍 FindValue_Inverse_TempParaTable.TcPOU:14

**메시지**: 초기화되지 않은 중요 변수 (REAL/LREAL/포인터)

```iecst
Para_1_Value						: REAL;
```

**💡 수정 제안**: 선언 시 초기값을 명시하세요: var : TYPE := 초기값;

---

#### 📍 FindValue_Inverse_TempParaTable.TcPOU:15

**메시지**: 초기화되지 않은 중요 변수 (REAL/LREAL/포인터)

```iecst
Para_2_Value						: REAL;
```

**💡 수정 제안**: 선언 시 초기값을 명시하세요: var : TYPE := 초기값;

---

#### 📍 FindValue_TempParaTable.TcPOU:11

**메시지**: 초기화되지 않은 중요 변수 (REAL/LREAL/포인터)

```iecst
Range_1_Value						: REAL;
```

**💡 수정 제안**: 선언 시 초기값을 명시하세요: var : TYPE := 초기값;

---

#### 📍 FindValue_TempParaTable.TcPOU:12

**메시지**: 초기화되지 않은 중요 변수 (REAL/LREAL/포인터)

```iecst
Range_2_Value						: REAL;
```

**💡 수정 제안**: 선언 시 초기값을 명시하세요: var : TYPE := 초기값;

---

#### 📍 FindValue_TempParaTable.TcPOU:14

**메시지**: 초기화되지 않은 중요 변수 (REAL/LREAL/포인터)

```iecst
Para_1_Value						: REAL;
```

**💡 수정 제안**: 선언 시 초기값을 명시하세요: var : TYPE := 초기값;

---

#### 📍 FindValue_TempParaTable.TcPOU:15

**메시지**: 초기화되지 않은 중요 변수 (REAL/LREAL/포인터)

```iecst
Para_2_Value						: REAL;
```

**💡 수정 제안**: 선언 시 초기값을 명시하세요: var : TYPE := 초기값;

---

#### 📍 F_Driver_APC_PID.TcPOU:4

**메시지**: 초기화되지 않은 중요 변수 (REAL/LREAL/포인터)

```iecst
iPID_Value							: REAL;
```

**💡 수정 제안**: 선언 시 초기값을 명시하세요: var : TYPE := 초기값;

---

#### 📍 F_Driver_Heater_Write_Array.TcPOU:12

**메시지**: 초기화되지 않은 중요 변수 (REAL/LREAL/포인터)

```iecst
rData								: REAL;
```

**💡 수정 제안**: 선언 시 초기값을 명시하세요: var : TYPE := 초기값;

---

#### 📍 F_EventLog_Shift.TcPOU:11

**메시지**: 초기화되지 않은 중요 변수 (REAL/LREAL/포인터)

```iecst
f2									: REAL;
```

**💡 수정 제안**: 선언 시 초기값을 명시하세요: var : TYPE := 초기값;

---

#### 📍 F_Function_LV_Log.TcPOU:3

**메시지**: 초기화되지 않은 중요 변수 (REAL/LREAL/포인터)

```iecst
iValue								: REAL;
```

**💡 수정 제안**: 선언 시 초기값을 명시하세요: var : TYPE := 초기값;

---

#### 📍 F_Function_Process_Log.TcPOU:3

**메시지**: 초기화되지 않은 중요 변수 (REAL/LREAL/포인터)

```iecst
iValue								: REAL;
```

**💡 수정 제안**: 선언 시 초기값을 명시하세요: var : TYPE := 초기값;

---

#### 📍 F_Get_NextJumpStep.TcPOU:5

**메시지**: 초기화되지 않은 중요 변수 (REAL/LREAL/포인터)

```iecst
recipe								: POINTER TO tRecipe;
```

**💡 수정 제안**: 선언 시 초기값을 명시하세요: var : TYPE := 초기값;

---

#### 📍 F_Parameter_Check.TcPOU:4

**메시지**: 초기화되지 않은 중요 변수 (REAL/LREAL/포인터)

```iecst
srcAddr								: POINTER TO DWORD;;
```

**💡 수정 제안**: 선언 시 초기값을 명시하세요: var : TYPE := 초기값;

---

#### 📍 F_Parameter_Check.TcPOU:11

**메시지**: 초기화되지 않은 중요 변수 (REAL/LREAL/포인터)

```iecst
currIndex							: POINTER TO UDINT;
```

**💡 수정 제안**: 선언 시 초기값을 명시하세요: var : TYPE := 초기값;

---

#### 📍 F_RealRound.TcPOU:3

**메시지**: 초기화되지 않은 중요 변수 (REAL/LREAL/포인터)

```iecst
lValue								: LREAL;
```

**💡 수정 제안**: 선언 시 초기값을 명시하세요: var : TYPE := 초기값;

---

#### 📍 F_Set_Parameter.TcPOU:3

**메시지**: 초기화되지 않은 중요 변수 (REAL/LREAL/포인터)

```iecst
paramAddr							: POINTER TO DWORD;
```

**💡 수정 제안**: 선언 시 초기값을 명시하세요: var : TYPE := 초기값;

---

#### 📍 F_Set_Parameter.TcPOU:11

**메시지**: 초기화되지 않은 중요 변수 (REAL/LREAL/포인터)

```iecst
currIndex							: POINTER TO UDINT;
```

**💡 수정 제안**: 선언 시 초기값을 명시하세요: var : TYPE := 초기값;

---

#### 📍 Get_IEEE754.TcPOU:7

**메시지**: 초기화되지 않은 중요 변수 (REAL/LREAL/포인터)

```iecst
f2									: REAL;
```

**💡 수정 제안**: 선언 시 초기값을 명시하세요: var : TYPE := 초기값;

---

*... 외 351건*

### QA002 (23건)

#### 📍 REAL_TO_STR.TcPOU:6

**메시지**: 위험한 타입 변환: REAL→DINT

```iecst
REAL_TO_STR := DINT_TO_STRING(REAL_TO_DINT(fConvertValue));
```

**💡 수정 제안**: LIMIT 함수로 범위 검증 후 변환하세요

---

#### 📍 SEQ_Driver_APC.TcPOU:337

**메시지**: 위험한 타입 변환: REAL→DINT

```iecst
iSet_Value := REAL_TO_DINT(Buff_Set_Value);
```

**💡 수정 제안**: LIMIT 함수로 범위 검증 후 변환하세요

---

#### 📍 SEQ_Driver_SdoMfc.TcPOU:23

**메시지**: 위험한 타입 변환: REAL→INT

```iecst
IF DX_MFC_Mode[REAL_TO_INT(AO_G_LIFE_Target)] = edG_LIFE_EXECUTE THEN
```

**💡 수정 제안**: LIMIT 함수로 범위 검증 후 변환하세요

---

#### 📍 SEQ_Driver_SdoMfc.TcPOU:31

**메시지**: 위험한 타입 변환: REAL→INT

```iecst
DX_MFC_Mode[REAL_TO_INT(AO_G_LIFE_Target)] := edG_LIFE_INIT;
```

**💡 수정 제안**: LIMIT 함수로 범위 검증 후 변환하세요

---

#### 📍 SEQ_Driver_SdoMfc.TcPOU:35

**메시지**: 위험한 타입 변환: REAL→INT

```iecst
IF DX_MFC_Mode[REAL_TO_INT(AO_G_LIFE_Target)] = edSet THEN
```

**💡 수정 제안**: LIMIT 함수로 범위 검증 후 변환하세요

---

#### 📍 SEQ_Driver_SdoMfc.TcPOU:41

**메시지**: 위험한 타입 변환: REAL→INT

```iecst
DX_MFC_Mode[REAL_TO_INT(AO_G_LIFE_Target)] := edSet;
```

**💡 수정 제안**: LIMIT 함수로 범위 검증 후 변환하세요

---

#### 📍 SEQ_Driver_SdoMfc.TcPOU:43

**메시지**: 위험한 타입 변환: REAL→INT

```iecst
AO_MFC_SET[REAL_TO_INT(AO_G_LIFE_Target)] := 0;
```

**💡 수정 제안**: LIMIT 함수로 범위 검증 후 변환하세요

---

#### 📍 SEQ_Driver_TCPIP.TcPOU:1806

**메시지**: 위험한 타입 변환: REAL→INT

```iecst
oSpeed_Value := REAL_TO_INT((oRotate_Speed * cRotate_Swap_Unit));
```

**💡 수정 제안**: LIMIT 함수로 범위 검증 후 변환하세요

---

#### 📍 SEQ_Driver_TV_VAT.TcPOU:29

**메시지**: 위험한 타입 변환: LREAL→REAL

```iecst
ioAO_ECAT_TV_VAT_PRESSURE_SETPOINT := LREAL_TO_REAL(Buff_iPressure);
```

**💡 수정 제안**: LIMIT 함수로 범위 검증 후 변환하세요

---

#### 📍 SEQ_Physical_Boat_Elevator.TcPOU:2065

**메시지**: 위험한 타입 변환: REAL→INT

```iecst
iBoatSpeed := REAL_TO_INT(rBoatSpeed);
```

**💡 수정 제안**: LIMIT 함수로 범위 검증 후 변환하세요

---

#### 📍 SEQ_Physical_Boat_Elevator.TcPOU:2070

**메시지**: 위험한 타입 변환: REAL→INT

```iecst
iBoatSpeed := REAL_TO_INT(rBoatSpeed);
```

**💡 수정 제안**: LIMIT 함수로 범위 검증 후 변환하세요

---

#### 📍 SEQ_Physical_Boat_Elevator.TcPOU:2093

**메시지**: 위험한 타입 변환: REAL→INT

```iecst
iBoatSpeed := REAL_TO_INT(rBoatSpeed);
```

**💡 수정 제안**: LIMIT 함수로 범위 검증 후 변환하세요

---

#### 📍 SEQ_Physical_Boat_Elevator.TcPOU:2096

**메시지**: 위험한 타입 변환: REAL→INT

```iecst
iBoatSpeed := REAL_TO_INT(rBoatSpeed);
```

**💡 수정 제안**: LIMIT 함수로 범위 검증 후 변환하세요

---

#### 📍 SEQ_Physical_Boat_Elevator.TcPOU:2099

**메시지**: 위험한 타입 변환: REAL→INT

```iecst
iBoatSpeed := REAL_TO_INT(rBoatSpeed);
```

**💡 수정 제안**: LIMIT 함수로 범위 검증 후 변환하세요

---

#### 📍 SEQ_Physical_Boat_Elevator.TcPOU:2102

**메시지**: 위험한 타입 변환: REAL→INT

```iecst
iBoatSpeed := REAL_TO_INT(rBoatSpeed);
```

**💡 수정 제안**: LIMIT 함수로 범위 검증 후 변환하세요

---

#### 📍 SEQ_Physical_Boat_Elevator.TcPOU:2105

**메시지**: 위험한 타입 변환: REAL→INT

```iecst
iBoatSpeed := REAL_TO_INT(rBoatSpeed);
```

**💡 수정 제안**: LIMIT 함수로 범위 검증 후 변환하세요

---

#### 📍 SEQ_Physical_Boat_Elevator.TcPOU:2108

**메시지**: 위험한 타입 변환: REAL→INT

```iecst
iBoatSpeed := REAL_TO_INT(rBoatSpeed);
```

**💡 수정 제안**: LIMIT 함수로 범위 검증 후 변환하세요

---

#### 📍 SEQ_Temp_Control.TcPOU:218

**메시지**: 위험한 타입 변환: REAL→INT

```iecst
stTempCtrlParameter[i].nInSpikeMovingAverageCnt		:= REAL_TO_INT(AX_CFG_Heater_Spike_MovAvg_Cnt);
```

**💡 수정 제안**: LIMIT 함수로 범위 검증 후 변환하세요

---

#### 📍 SEQ_Temp_Control.TcPOU:222

**메시지**: 위험한 타입 변환: REAL→INT

```iecst
stTempCtrlParameter[i].nInProfileMovingAverageCnt	:= REAL_TO_INT(AX_CFG_Heater_Profile_MovAvg_Cnt);
```

**💡 수정 제안**: LIMIT 함수로 범위 검증 후 변환하세요

---

#### 📍 SEQ_Temp_Control.TcPOU:226

**메시지**: 위험한 타입 변환: REAL→INT

```iecst
stTempCtrlParameter[i].nOutPowerMovingAverageCnt	:= REAL_TO_INT(AX_CFG_Heater_Power_MovAvg_Cnt);
```

**💡 수정 제안**: LIMIT 함수로 범위 검증 후 변환하세요

---

*... 외 3건*

### QA005 (1건)

#### 📍 SEQ_Interlock_Valve.TcPOU:6870

**메시지**: 실수형(REAL/LREAL) 직접 등호 비교

```iecst
IF REAR_FFU_Interlock_Check = FALSE THEN
```

**💡 수정 제안**: ABS(a - b) < epsilon 형태로 비교하세요

---

### QA006 (44건)

#### 📍 F_Get_Parameter.TcPOU:4

**메시지**: 0으로 나누기 가능성

```iecst
//	RETURN;
```

**💡 수정 제안**: 나누기 전 분모가 0이 아닌지 확인하세요

---

#### 📍 F_Get_Parameter.TcPOU:8

**메시지**: 0으로 나누기 가능성

```iecst
size := srcSize / cMAX_Table;
```

**💡 수정 제안**: 나누기 전 분모가 0이 아닌지 확인하세요

---

#### 📍 F_Parameter_Check.TcPOU:1

**메시지**: 0으로 나누기 가능성

```iecst
size := srcSize / cMAX_Table;
```

**💡 수정 제안**: 나누기 전 분모가 0이 아닌지 확인하세요

---

#### 📍 F_Set_Parameter.TcPOU:1

**메시지**: 0으로 나누기 가능성

```iecst
size := paramSize / cMAX_Table;
```

**💡 수정 제안**: 나누기 전 분모가 0이 아닌지 확인하세요

---

#### 📍 FB_Collection.TcPOU:19

**메시지**: 0으로 나누기 가능성

```iecst
oAvg := Total / iCount;
```

**💡 수정 제안**: 나누기 전 분모가 0이 아닌지 확인하세요

---

#### 📍 FB_Control_Gas.TcPOU:138

**메시지**: 0으로 나누기 가능성

```iecst
Deviation := (iSet - oFlow) * 100 / iScale;	 							(* Gas Alarm은 Full Scale 대비 발생시킨다. *)
```

**💡 수정 제안**: 나누기 전 분모가 0이 아닌지 확인하세요

---

#### 📍 FB_Control_Gas.TcPOU:140

**메시지**: 0으로 나누기 가능성

```iecst
Deviation_Set := (iSet - oFlow) * 100 / iSet;
```

**💡 수정 제안**: 나누기 전 분모가 0이 아닌지 확인하세요

---

#### 📍 FB_Control_Gas.TcPOU:319

**메시지**: 0으로 나누기 가능성

```iecst
Deviation := (iSet-oFlow) * 100.0 / iScale;
```

**💡 수정 제안**: 나누기 전 분모가 0이 아닌지 확인하세요

---

#### 📍 FB_Control_Gas.TcPOU:322

**메시지**: 0으로 나누기 가능성

```iecst
Deviation := (oFlow-iSet) * 100.0 / iScale;
```

**💡 수정 제안**: 나누기 전 분모가 0이 아닌지 확인하세요

---

#### 📍 FB_Sdo_Gas_PartsPara.TcPOU:298

**메시지**: 0으로 나누기 가능성

```iecst
oBuffPartsPara.Item[12] := rGasSysmbol;																		//ex) H2, N2
```

**💡 수정 제안**: 나누기 전 분모가 0이 아닌지 확인하세요

---

#### 📍 FB_Sdo_Gas_PartsPara.TcPOU:299

**메시지**: 0으로 나누기 가능성

```iecst
oBuffPartsPara.Item[13] := rGasName;																		//ex) H2, N2
```

**💡 수정 제안**: 나누기 전 분모가 0이 아닌지 확인하세요

---

#### 📍 SEQ_Driver_APC.TcPOU:394

**메시지**: 0으로 나누기 가능성

```iecst
AI_APC_Pressure1								:= ioAI_ECAT_APC_P1_SENSOR_PRESSURE;//AI_APC_Pressure1										:= ioAI_ECAT_APC_P1_SENSOR_PRESSURE/cGauge_Unit;
```

**💡 수정 제안**: 나누기 전 분모가 0이 아닌지 확인하세요

---

#### 📍 SEQ_Driver_APC.TcPOU:395

**메시지**: 0으로 나누기 가능성

```iecst
AI_APC_Pressure2								:= ioAI_ECAT_APC_P2_SENSOR_PRESSURE;//AI_APC_Pressure2										:= ioAI_ECAT_APC_P2_SENSOR_PRESSURE/cGauge_Unit;
```

**💡 수정 제안**: 나누기 전 분모가 0이 아닌지 확인하세요

---

#### 📍 SEQ_Driver_TCPIP.TcPOU:224

**메시지**: 0으로 나누기 가능성

```iecst
fbAlarm[14414].iPost  := TRUE;	(*EMG from PC(S/W)*)
```

**💡 수정 제안**: 나누기 전 분모가 0이 아닌지 확인하세요

---

#### 📍 SEQ_Driver_TCPIP.TcPOU:226

**메시지**: 0으로 나누기 가능성

```iecst
fbAlarm[14415].iPost  := TRUE;	(*EMG from Junjang(H/W)*)
```

**💡 수정 제안**: 나누기 전 분모가 0이 아닌지 확인하세요

---

#### 📍 SEQ_Driver_TCPIP.TcPOU:262

**메시지**: 0으로 나누기 가능성

```iecst
fbAlarm[14817].iPost  := TRUE;	(*EMG from PC(S/W)*)
```

**💡 수정 제안**: 나누기 전 분모가 0이 아닌지 확인하세요

---

#### 📍 SEQ_Driver_TCPIP.TcPOU:264

**메시지**: 0으로 나누기 가능성

```iecst
fbAlarm[14818].iPost  := TRUE;	(*EMG from Junjang(H/W)*)
```

**💡 수정 제안**: 나누기 전 분모가 0이 아닌지 확인하세요

---

#### 📍 SEQ_Driver_TCPIP.TcPOU:979

**메시지**: 0으로 나누기 가능성

```iecst
iBoatHomePosition := iBoatHomePosition / iBoat_ConversionRatio;		(* mm 단위 환산 *)
```

**💡 수정 제안**: 나누기 전 분모가 0이 아닌지 확인하세요

---

#### 📍 SEQ_Driver_TCPIP.TcPOU:983

**메시지**: 0으로 나누기 가능성

```iecst
iBoatUpPosition := iBoatUpPosition / iBoat_ConversionRatio;		(* mm 단위 환산 *)
```

**💡 수정 제안**: 나누기 전 분모가 0이 아닌지 확인하세요

---

#### 📍 SEQ_Driver_TCPIP.TcPOU:987

**메시지**: 0으로 나누기 가능성

```iecst
iBoatInitPosition := iBoatInitPosition / iBoat_ConversionRatio;		(* mm 단위 환산 *)
```

**💡 수정 제안**: 나누기 전 분모가 0이 아닌지 확인하세요

---

*... 외 24건*
