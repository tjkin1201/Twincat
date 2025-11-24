(* 
    Axis Data Structure
    Description: Data structure for axis control and status
*)

TYPE ST_AxisData :
STRUCT
    // Position data
    fActualPosition     : LREAL;        // Actual position in mm or degrees
    fTargetPosition     : LREAL;        // Target position
    fActualVelocity     : LREAL;        // Actual velocity
    
    // Status flags
    bEnabled            : BOOL;         // Axis enabled
    bMoving             : BOOL;         // Axis in motion
    bInPosition         : BOOL;         // Axis at target position
    bError              : BOOL;         // Error state
    
    // Parameters
    fMaxVelocity        : LREAL := 100.0;   // Maximum velocity
    fAcceleration       : LREAL := 50.0;    // Acceleration
    fDeceleration       : LREAL := 50.0;    // Deceleration
    
    // Error information
    uiErrorID           : UINT;         // Error code
    sErrorMessage       : STRING(255);  // Error description
END_STRUCT
END_TYPE
