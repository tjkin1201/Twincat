(* 
    Motor State Enumeration
    Description: Defines the possible states of a motor
*)

TYPE E_MotorState :
(
    IDLE        := 0,   // Motor idle
    MOVING      := 1,   // Motor moving to target
    DONE        := 2,   // Target reached
    ERROR       := 3    // Error state
);
END_TYPE
