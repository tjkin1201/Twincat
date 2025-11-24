(* 
    System State Enumeration
    Description: Defines the possible states of the system
*)

TYPE E_SystemState :
(
    INIT        := 0,   // System initialization
    IDLE        := 1,   // System ready, waiting
    RUNNING     := 2,   // Normal operation
    ERROR       := 3,   // Error state
    SHUTDOWN    := 4    // Controlled shutdown
);
END_TYPE
