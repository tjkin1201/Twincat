# Contributing to TwinCAT Project

Thank you for your interest in contributing to this TwinCAT project! This document provides guidelines and best practices for contributing.

## Development Workflow

### 1. Setting Up Your Environment

1. Fork the repository
2. Clone your fork locally
3. Install TwinCAT 3 XAE and Visual Studio
4. Configure TwinCAT for version control (see below)

### 2. TwinCAT Configuration for Version Control

Before making changes, ensure your TwinCAT XAE is properly configured:

1. **Enable Multiple Project Files**:
   - Go to Tools > Options > TwinCAT > XAE Environment > File Settings
   - Check "Enable Multiple Project Files"
   - This allows better diff/merge support in Git

2. **Disable User-Specific Settings from Version Control**:
   - User files (*.user, *.suo) are automatically excluded via .gitignore
   - These contain local paths and should never be committed

### 3. Making Changes

1. **Create a Feature Branch**:
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **Follow Naming Conventions**:
   - POUs: Use PascalCase (e.g., `FB_MotorController`)
   - Variables: Use camelCase with prefixes (e.g., `bStartMotor`, `nSpeed`)
   - Constants: Use UPPER_CASE (e.g., `MAX_SPEED`)
   - Function blocks: Prefix with `FB_`
   - Functions: Prefix with `F_`
   - Data types: Prefix with `E_` (enum), `ST_` (struct), `T_` (type)

3. **Code Organization**:
   - Group related functionality into modules
   - Keep POUs focused and single-purpose
   - Use interfaces for abstraction where appropriate
   - Document complex logic with comments

4. **Testing**:
   - Test your changes thoroughly on hardware or simulation
   - Verify no compilation errors or warnings
   - Check that existing functionality is not broken

### 4. Committing Changes

1. **Write Clear Commit Messages**:
   ```
   Short (50 chars or less) summary

   More detailed explanatory text, if necessary. Wrap it to about 72
   characters. The blank line separating the summary from the body is
   critical.

   - Bullet points are okay
   - Use present tense: "Add feature" not "Added feature"
   ```

2. **Keep Commits Atomic**:
   - Each commit should represent a single logical change
   - Don't mix unrelated changes in one commit

3. **Commit Often**:
   - Small, frequent commits are better than large, infrequent ones
   - Makes it easier to track changes and revert if needed

### 5. Submitting Changes

1. **Push to Your Fork**:
   ```bash
   git push origin feature/your-feature-name
   ```

2. **Create a Pull Request**:
   - Provide a clear description of the changes
   - Reference any related issues
   - Explain why the change is needed
   - Include screenshots or diagrams if helpful

3. **Code Review**:
   - Be responsive to feedback
   - Make requested changes promptly
   - Use TwinCAT Project Compare Tool to review differences

## Project Structure Guidelines

### Directory Organization

```
TwinCAT_Project/
├── 01_Core/              # Core functionality
│   ├── POUs/
│   ├── DUTs/
│   └── GVLs/
├── 02_Safety/            # Safety-related code
├── 03_Motion/            # Motion control
└── 99_Utilities/         # Utility functions
```

### File Naming

- Use descriptive names that indicate purpose
- Keep names concise but clear
- Use consistent prefixes based on type

## Code Style

### Structured Text (ST)

```iecst
PROGRAM MAIN
VAR
    bStart : BOOL;
    nCounter : INT := 0;
END_VAR

// Main program logic
IF bStart THEN
    nCounter := nCounter + 1;
END_IF
```

### Comments

- Use `//` for single-line comments
- Use `(* ... *)` for multi-line or block comments
- Document complex algorithms and non-obvious logic
- Keep comments up to date with code changes

## Best Practices

1. **Version Control**:
   - Never commit user files (*.user, *.suo)
   - Always pull before starting new work
   - Resolve conflicts carefully using TwinCAT Project Compare Tool

2. **Code Quality**:
   - Eliminate all warnings during compilation
   - Use strong typing
   - Initialize variables explicitly
   - Handle edge cases and errors

3. **Documentation**:
   - Update README.md if adding new features
   - Document interfaces and complex POUs
   - Keep docs/ folder updated with architecture diagrams

4. **Testing**:
   - Test on simulation before hardware
   - Verify safety-critical functionality thoroughly
   - Document test procedures for complex features

## Questions or Issues?

If you have questions or run into issues:
- Check existing issues for similar problems
- Open a new issue with detailed description
- Provide relevant code snippets and error messages

## License

By contributing, you agree that your contributions will be licensed under the MIT License.
