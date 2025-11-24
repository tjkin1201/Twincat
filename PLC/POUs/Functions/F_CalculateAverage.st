(* 
    Calculate Average Function
    Description: Calculates the average of an array of values
    Returns: Average value
*)

FUNCTION F_CalculateAverage : LREAL
VAR_INPUT
    aValues     : ARRAY[0..99] OF LREAL;    // Input array
    iCount      : INT;                       // Number of valid values (max 100)
END_VAR

VAR
    i           : INT;
    fSum        : LREAL := 0.0;
    iValidCount : INT;
END_VAR

// Validate count to prevent array overflow
IF iCount > 100 THEN
    iValidCount := 100;
ELSIF iCount < 0 THEN
    iValidCount := 0;
ELSE
    iValidCount := iCount;
END_IF

// Calculate sum
FOR i := 0 TO iValidCount - 1 DO
    fSum := fSum + aValues[i];
END_FOR

// Calculate average
IF iValidCount > 0 THEN
    F_CalculateAverage := fSum / INT_TO_LREAL(iValidCount);
ELSE
    F_CalculateAverage := 0.0;
END_IF
