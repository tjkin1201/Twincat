(* 
    Motor Control Function Block
    Description: Basic motor control with position feedback
    Author: Auto-generated template
*)

FUNCTION_BLOCK FB_MotorControl
VAR_INPUT
    bEnable         : BOOL;             // Enable motor
    fTargetPosition : LREAL;            // Target position
    fTargetVelocity : LREAL := 100.0;   // Target velocity
END_VAR

VAR_OUTPUT
    bReady          : BOOL;             // Motor ready
    bBusy           : BOOL;             // Motor moving
    bDone           : BOOL;             // Target reached
    bError          : BOOL;             // Error occurred
    uiErrorID       : UINT;             // Error code
END_VAR

VAR
    stAxisData      : ST_AxisData;      // Internal axis data
    eState          : E_MotorState;     // Internal state machine
    fPositionError  : LREAL;            // Position error
    fPositionTolerance : LREAL := 0.1;  // Position tolerance
END_VAR

// State machine
CASE eState OF
    E_MotorState.IDLE:
        bReady := TRUE;
        bBusy := FALSE;
        bDone := FALSE;
        
        IF bEnable THEN
            eState := E_MotorState.MOVING;
        END_IF
        
    E_MotorState.MOVING:
        bReady := FALSE;
        bBusy := TRUE;
        bDone := FALSE;
        
        // Calculate position error
        fPositionError := ABS(fTargetPosition - stAxisData.fActualPosition);
        
        // Simulate motion (in real application, this would interface with hardware)
        IF stAxisData.fActualPosition < fTargetPosition THEN
            stAxisData.fActualPosition := stAxisData.fActualPosition + 
                (fTargetVelocity * 0.001); // Assuming 1ms cycle time
        ELSIF stAxisData.fActualPosition > fTargetPosition THEN
            stAxisData.fActualPosition := stAxisData.fActualPosition - 
                (fTargetVelocity * 0.001);
        END_IF
        
        // Check if target reached
        IF fPositionError < fPositionTolerance THEN
            eState := E_MotorState.DONE;
        END_IF
        
        IF NOT bEnable THEN
            eState := E_MotorState.IDLE;
        END_IF
        
    E_MotorState.DONE:
        bReady := TRUE;
        bBusy := FALSE;
        bDone := TRUE;
        
        IF NOT bEnable THEN
            eState := E_MotorState.IDLE;
        END_IF
        
    E_MotorState.ERROR:
        bReady := FALSE;
        bBusy := FALSE;
        bDone := FALSE;
        bError := TRUE;
        
        IF NOT bEnable THEN
            bError := FALSE;
            uiErrorID := 0;
            eState := E_MotorState.IDLE;
        END_IF
END_CASE
