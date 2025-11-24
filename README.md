# TwinCAT Automation Project

This repository contains a TwinCAT 3 automation project for industrial control systems.

## Overview

TwinCAT (The Windows Control and Automation Technology) is Beckhoff's software system for PC-based automation. This project provides a structured template for developing PLC programs and automation solutions.

## Project Structure

```
├── PLC/                    # PLC project files
│   ├── POUs/              # Program Organization Units
│   ├── DUTs/              # Data Unit Types
│   ├── GVLs/              # Global Variable Lists
│   └── VISUs/             # Visualizations
├── Config/                 # System configuration files
├── docs/                   # Documentation
└── README.md              # This file
```

## Requirements

- Windows 10/11 (64-bit)
- TwinCAT 3.1 Build 4024 or higher
- Visual Studio 2019 or higher (optional, for integrated development)
- .NET Framework 4.8 or higher

## Getting Started

1. **Install TwinCAT 3**
   - Download from [Beckhoff website](https://www.beckhoff.com/en-us/products/automation/twincat/)
   - Follow the installation wizard
   - Restart your computer after installation

2. **Open the Project**
   - Launch TwinCAT XAE (eXtended Automation Engineering)
   - Open the solution file in the project root
   - Build the solution (F7)

3. **Activate TwinCAT Configuration**
   - Click "TwinCAT" → "Show Realtime Ethernet Compatible Devices"
   - Select your network adapter
   - Activate configuration

4. **Download to PLC**
   - Set TwinCAT to Config mode
   - Build and deploy the PLC project
   - Switch to Run mode

## Performance Configuration

This project is optimized for enhanced performance with the following settings:

- **Task Configuration**: Cycle times optimized for real-time requirements
- **Memory Management**: Efficient data structure organization
- **Network Settings**: EtherCAT performance tuning
- **Resource Allocation**: CPU core assignment for deterministic behavior

## Development Guidelines

### Code Organization

- Place reusable functions in the `POUs/Functions/` directory
- Define data structures in `DUTs/`
- Use meaningful variable names following IEC 61131-3 standards
- Document complex logic with inline comments

### Naming Conventions

- **Variables**: `camelCase` (e.g., `motorSpeed`)
- **Functions**: `PascalCase` (e.g., `CalculateVelocity`)
- **Constants**: `UPPER_SNAKE_CASE` (e.g., `MAX_SPEED`)
- **Prefixes**: 
  - `fb` for function blocks (e.g., `fbMotorControl`)
  - `st` for structures (e.g., `stAxisData`)
  - `e` for enumerations (e.g., `eMotorState`)

### Best Practices

- Keep functions small and focused (single responsibility)
- Use function blocks for stateful operations
- Implement error handling for all critical operations
- Test thoroughly in simulation before deploying to hardware
- Version control all project files except generated artifacts

## Testing

- Use the integrated TwinCAT simulator for initial testing
- Test in Config mode before switching to Run mode
- Monitor system performance using TwinCAT System Manager
- Log critical events and errors

## Troubleshooting

### Common Issues

1. **"No TwinCAT RT found"**
   - Ensure TwinCAT runtime is installed
   - Check if TwinCAT is in Config or Run mode
   - Verify system compatibility

2. **Build Errors**
   - Clean and rebuild the solution
   - Check for missing library references
   - Verify TwinCAT version compatibility

3. **Communication Issues**
   - Verify network adapter settings
   - Check EtherCAT device configuration
   - Ensure proper cable connections

## Contributing

1. Create a feature branch from `main`
2. Make your changes following the coding guidelines
3. Test thoroughly in simulation
4. Submit a pull request with a clear description

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Resources

- [TwinCAT 3 Documentation](https://infosys.beckhoff.com/)
- [IEC 61131-3 Standard](https://en.wikipedia.org/wiki/IEC_61131-3)
- [Beckhoff Information System](https://infosys.beckhoff.com/)

## Support

For issues and questions:
- Check the documentation in the `docs/` directory
- Review TwinCAT official documentation
- Open an issue in this repository
