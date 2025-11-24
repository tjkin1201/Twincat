(* 
    Calculate Average Function
    Description: Calculates the average of an array of values
    Returns: Average value
*)

FUNCTION F_CalculateAverage : LREAL
VAR_INPUT
    aValues     : ARRAY[0..99] OF LREAL;    // Input array
    iCount      : INT;                       // Number of valid values
END_VAR

VAR
    i           : INT;
    fSum        : LREAL := 0.0;
END_VAR

// Calculate sum
FOR i := 0 TO iCount - 1 DO
    IF i <= 99 THEN
        fSum := fSum + aValues[i];
    END_IF
END_FOR

// Calculate average
IF iCount > 0 THEN
    F_CalculateAverage := fSum / INT_TO_LREAL(iCount);
ELSE
    F_CalculateAverage := 0.0;
END_IF
