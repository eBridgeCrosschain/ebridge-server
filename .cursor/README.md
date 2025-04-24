# Cursor Configuration

This directory contains Cursor-specific configurations and development rules for the E-Bridge Server project.

## 📁 Directory Structure

```
.cursor/
├── rules/                     # Development rules and guidelines
│   ├── auto-dev.mdc          # Automated development workflow
│   ├── code-practices.mdc    # Coding standards and practices
│   ├── development-guidelines.mdc # Development guidelines
│   └── test-guidelines.mdc   # Testing standards and procedures
└── README.md                 # This file
```

## 🔧 Rules Overview

### Automated Development (auto-dev.mdc)
- Structured development workflow
- Machine-specific task tracking
- Git operation safety measures
- Automated testing procedures
- Branch management and synchronization
- Code integration protocols

### Code Practices (code-practices.mdc)
- SOLID principles implementation
- Logging requirements
- Performance considerations
- Code verification steps
- Compilation error handling
- Code reference validation

### Development Guidelines (development-guidelines.mdc)
- File-by-file change management
- Information verification protocols
- Code structure maintenance
- Error handling standards
- No whitespace changes policy
- Implementation verification rules
- Security-first approach
- Variable naming conventions

### Test Guidelines (test-guidelines.mdc)
- Unit test implementation
- Test coverage requirements
- Test execution procedures
- Test documentation standards
- Test case design principles

## 🚀 Quick Start

1. **Environment Setup**
   ```bash
   # Get your machine ID
   ifconfig | grep ether | head -1 | awk '{print $2}'
   ```

2. **Git Configuration**
   ```bash
   # Set up non-interactive git environment
   export GIT_EDITOR=cat
   export GIT_PAGER=cat
   export GIT_ASKPASS=echo
   ```

3. **Development Process**
   - Follow the automated development workflow
   - Use machine-specific tracking
   - Maintain code quality standards
   - Update project documentation
   - Execute comprehensive tests

## ⚠️ Important Notes

1. **File Management**
   - Keep files under 400 lines
   - Split larger files into modules
   - Preserve existing code structure
   - No unnecessary whitespace changes

2. **Mandatory Processes**
   - Use MCP sequential thinking
   - Follow automated workflows
   - Verify all implementations
   - Document all changes

3. **Safety Measures**
   - Use non-interactive git commands
   - Implement timeout controls
   - Maintain backup procedures
   - Verify all code references

## 🔄 Development Workflow

The development process follows these steps:
1. Machine identification and registration
2. Branch selection and task assignment
3. Feature development and testing
4. Code quality assessment
5. Integration and deployment

## 🛠 Testing Requirements

All code changes must:
1. Include unit tests
2. Pass existing test suite
3. Maintain or improve coverage
4. Follow test guidelines
5. Include error handling tests

## 📝 Documentation Standards

When updating documentation:
1. Follow existing formats
2. Include practical examples
3. Verify all references
4. Keep information current

## 🤝 Contributing

1. Read all rule documents
2. Follow the automated workflow
3. Use provided templates
4. Maintain documentation
5. Submit comprehensive tests

## 🔍 Support

For development issues:
1. Check rule documentation
2. Review error logs
3. Follow debugging procedures
4. Update configurations as needed 