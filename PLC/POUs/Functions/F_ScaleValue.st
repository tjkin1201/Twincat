(* 
    Scale Value Function
    Description: Scales a value from one range to another
    Returns: Scaled value
*)

FUNCTION F_ScaleValue : LREAL
VAR_INPUT
    fInputValue     : LREAL;    // Value to scale
    fInputMin       : LREAL;    // Input minimum
    fInputMax       : LREAL;    // Input maximum
    fOutputMin      : LREAL;    // Output minimum
    fOutputMax      : LREAL;    // Output maximum
END_VAR

VAR
    fInputRange     : LREAL;
    fOutputRange    : LREAL;
    fNormalizedValue : LREAL;
END_VAR

// Calculate ranges
fInputRange := fInputMax - fInputMin;
fOutputRange := fOutputMax - fOutputMin;

// Avoid division by zero
IF ABS(fInputRange) < 0.0001 THEN
    F_ScaleValue := fOutputMin;
    RETURN;
END_IF

// Normalize input value to 0-1 range
fNormalizedValue := (fInputValue - fInputMin) / fInputRange;

// Scale to output range
F_ScaleValue := fOutputMin + (fNormalizedValue * fOutputRange);

// Clamp to output range
IF F_ScaleValue < fOutputMin THEN
    F_ScaleValue := fOutputMin;
ELSIF F_ScaleValue > fOutputMax THEN
    F_ScaleValue := fOutputMax;
END_IF
