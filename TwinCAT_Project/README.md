# TwinCAT Project

This directory contains the TwinCAT 3 project files.

## Structure

- **POUs/** - Program Organization Units (Programs, Function Blocks, Functions)
- **DUTs/** - Data Unit Types (Structures, Enumerations, Unions)
- **GVLs/** - Global Variable Lists
- **ITFs/** - Interfaces

## Development

1. Open the TwinCAT solution in Visual Studio with TwinCAT XAE
2. Ensure "Enable Multiple Project Files" is enabled in TwinCAT settings
3. Organize code by functional modules
4. Follow naming conventions defined in CONTRIBUTING.md

## Building

Build the project using TwinCAT XAE:
- Build > Build Solution (or F7)
- Check for errors and warnings in the Error List window
- Resolve all warnings before committing

## Testing

Test your changes:
1. Activate TwinCAT configuration
2. Log in to the PLC
3. Verify functionality in online mode
4. Check variable values and program execution
