# Contributing to TwinCAT Project

Thank you for your interest in contributing to this TwinCAT automation project! This document provides guidelines for contributing.

## Code of Conduct

- Be respectful and inclusive
- Welcome newcomers
- Focus on constructive feedback
- Help others learn

## How to Contribute

### Reporting Issues

If you find a bug or have a suggestion:

1. Check if the issue already exists
2. Create a new issue with:
   - Clear title and description
   - Steps to reproduce (for bugs)
   - Expected vs actual behavior
   - TwinCAT version information
   - System configuration details

### Submitting Changes

1. **Fork the Repository**
   - Click the Fork button on GitHub
   - Clone your fork locally

2. **Create a Branch**
   ```bash
   git checkout -b feature/your-feature-name
   # or
   git checkout -b fix/bug-description
   ```

3. **Make Your Changes**
   - Follow the coding standards (see below)
   - Test your changes thoroughly
   - Update documentation if needed
   - Add comments for complex logic

4. **Commit Your Changes**
   ```bash
   git add .
   git commit -m "Add feature: description"
   ```
   
   Commit message format:
   - `feat:` for new features
   - `fix:` for bug fixes
   - `docs:` for documentation
   - `refactor:` for code refactoring
   - `test:` for test changes
   - `perf:` for performance improvements

5. **Push to Your Fork**
   ```bash
   git push origin feature/your-feature-name
   ```

6. **Create a Pull Request**
   - Go to the original repository
   - Click "New Pull Request"
   - Select your branch
   - Fill in the PR template
   - Wait for review

## Coding Standards

### IEC 61131-3 Standards

Follow IEC 61131-3 programming standards for PLC code:

#### Variable Naming

```
// Local variables
camelCase:          motorSpeed, sensorValue
```

```
// Function Blocks
PascalCase with fb prefix:    fbMotorControl, fbPIDController
```

```
// Structures
PascalCase with st prefix:    stAxisData, stConfiguration
```

```
// Enumerations
PascalCase with e prefix:     eSystemState, eMotorState
```

```
// Constants
UPPER_SNAKE_CASE:   MAX_SPEED, DEFAULT_TIMEOUT
```

```
// Global Variables
PascalCase:         SystemReady, EmergencyStop
```

### Code Structure

1. **File Headers**
   ```
   (* 
       Component Name
       Description: Brief description of purpose
       Author: Your Name
       Date: YYYY-MM-DD
       Version: 1.0.0
   *)
   ```

2. **Variable Declarations**
   - Group related variables
   - Add comments for non-obvious variables
   - Initialize with sensible defaults

3. **Code Body**
   - Keep functions small and focused
   - One function = one responsibility
   - Max ~50 lines per function (guideline)
   - Use white space for readability

4. **Comments**
   - Explain WHY, not WHAT
   - Comment complex algorithms
   - Update comments when code changes
   - Use English for comments

### Example Function

```
(* 
    Calculate Motor Torque
    Description: Calculates required motor torque based on load and acceleration
    Returns: Required torque in Nm
*)
FUNCTION F_CalculateTorque : LREAL
VAR_INPUT
    fLoad           : LREAL;    // Load in kg
    fAcceleration   : LREAL;    // Acceleration in m/s²
    fRadius         : LREAL;    // Radius in m
END_VAR

VAR
    fInertia        : LREAL;
    fTorque         : LREAL;
END_VAR

// Calculate moment of inertia
fInertia := fLoad * fRadius * fRadius;

// Calculate required torque: T = I * α
fTorque := fInertia * fAcceleration;

F_CalculateTorque := fTorque;
```

## Documentation

### Code Documentation

- Document all public interfaces
- Explain function parameters
- Describe return values
- Note any side effects
- List preconditions and postconditions

### User Documentation

When adding features that affect users:

1. Update README.md
2. Update relevant docs/ files
3. Add examples if appropriate
4. Update CHANGELOG.md

### Comments in Code

```
// Good: Explains why
// Reverse direction because encoder is mounted backwards
bDirection := NOT bDirection;

// Bad: Explains what (obvious from code)
// Set direction to not direction
bDirection := NOT bDirection;
```

## Testing

### Before Submitting

1. **Syntax Check**
   - Code compiles without errors
   - No warnings (or justified warnings)

2. **Functionality Test**
   - Test in TwinCAT simulator
   - Verify all code paths
   - Test edge cases
   - Check error handling

3. **Performance Test**
   - Verify cycle time impact
   - Check memory usage
   - Monitor CPU load
   - Test under load

### Test Checklist

- [ ] Code compiles successfully
- [ ] No compiler warnings
- [ ] Tested in simulation
- [ ] Documentation updated
- [ ] CHANGELOG.md updated
- [ ] Follows coding standards
- [ ] No breaking changes (or documented)

## Project Structure

When adding files, follow this structure:

```
PLC/
├── POUs/
│   ├── Programs/       # Main programs
│   ├── FunctionBlocks/ # Reusable FBs
│   └── Functions/      # Utility functions
├── DUTs/              # Data types
├── GVLs/              # Global variables
└── VISUs/             # Visualizations

Config/                # Configuration docs
docs/                  # User documentation
```

## Performance Guidelines

When contributing code:

- Minimize cycle time impact
- Avoid dynamic memory allocation in real-time tasks
- Use appropriate data types (INT vs LREAL)
- Optimize loops
- Cache calculated values
- Consider task priority

## Hardware Considerations

If code interacts with hardware:

- Document hardware requirements
- Handle communication errors
- Provide simulation alternatives
- Include safety considerations
- Test with actual hardware when possible

## Questions?

If you have questions:

1. Check existing documentation
2. Search closed issues
3. Open a new issue with your question
4. Tag with "question" label

## Recognition

Contributors will be:
- Listed in CHANGELOG.md
- Credited in commit history
- Acknowledged in release notes

Thank you for contributing to making this project better!
