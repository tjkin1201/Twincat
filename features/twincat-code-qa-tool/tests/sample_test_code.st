PROGRAM PLC_Main
VAR
    counter : INT := 0;
    enable : BOOL;
    output : REAL;
END_VAR

IF enable THEN
    counter := counter + 1;
    output := counter * 1.5;
END_IF

END_PROGRAM
