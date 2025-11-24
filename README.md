# TwinCAT Project

A TwinCAT 3 automation project repository with best practices for version control and project structure.

## Overview

This repository contains a TwinCAT 3 project following industry best practices for version control, modularity, and maintainability.

## Repository Structure

```
/Twincat
├── README.md              # Project documentation
├── LICENSE                # License information
├── .gitignore            # Git ignore rules for TwinCAT projects
├── .gitattributes        # Git line ending configuration
├── CONTRIBUTING.md       # Contribution guidelines
├── docs/                 # Documentation and images
├── src/                  # Supplementary scripts (Python, Node.js, etc.)
└── TwinCAT_Project/      # TwinCAT project files
    ├── POUs/             # Program Organization Units
    ├── DUTs/             # Data Unit Types
    ├── GVLs/             # Global Variable Lists
    └── ITFs/             # Interfaces
```

## Getting Started

### Prerequisites

- TwinCAT 3 XAE (eXtended Automation Engineering)
- Visual Studio 2013 or later (integrated with TwinCAT)
- Git for version control

### Setup

1. Clone the repository:
   ```bash
   git clone https://github.com/tjkin1201/Twincat.git
   cd Twincat
   ```

2. Open the TwinCAT project in Visual Studio with TwinCAT XAE

3. Build and deploy to your target system

## Project Organization

### Modular Structure

The project follows a modular organization approach:
- Each functional module has its own folder
- POUs, DUTs, GVLs, and ITFs are organized by module
- Use numeric prefixes (e.g., `01_Module`, `02_Module`) for custom ordering

### Version Control Best Practices

- **Enable Multiple Project Files**: In TwinCAT XAE, go to Tools > Options > TwinCAT > XAE Environment > File Settings and enable "Enable Multiple Project Files" for better diff/merge support
- **Commit Often**: Make small, focused commits with clear messages
- **Use Branches**: Create feature branches for new development
- **Review Changes**: Use TwinCAT Project Compare Tool for reviewing changes

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines on contributing to this project.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Resources

- [Beckhoff TwinCAT 3 Documentation](https://infosys.beckhoff.com/)
- [TwinCAT 3 Version Control Guide](https://infosys.beckhoff.com/content/1033/tc3_sourcecontrol/14604064523.html)
- [TwinCAT Project Structure Best Practices](https://infosys.beckhoff.com/content/1033/tc3_plc_intro/12049759115.html)

## Support

For questions or issues, please open an issue in this repository.
