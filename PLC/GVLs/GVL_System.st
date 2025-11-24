(* 
    Global Variable List
    Description: System-wide global variables
*)

VAR_GLOBAL
    // System information
    sSystemVersion      : STRING(50) := '1.0.0';
    dtSystemStartTime   : DATE_AND_TIME;
    
    // System status
    bSystemInitialized  : BOOL := FALSE;
    bSystemRunning      : BOOL := FALSE;
    
    // Performance monitoring
    udiTotalCycles      : UDINT := 0;
    fCycleTime          : LREAL := 0.0;     // Current cycle time in ms
    fMaxCycleTime       : LREAL := 0.0;     // Maximum cycle time in ms
    
    // System limits
    MAX_AXES            : INT := 8;
    MAX_SPEED           : LREAL := 1000.0;
END_VAR
