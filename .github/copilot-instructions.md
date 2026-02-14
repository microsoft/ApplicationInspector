# GitHub Copilot Instructions for ApplicationInspector

## Project Overview

Microsoft Application Inspector is a source code characterization tool that identifies coding features based on well-known library/API calls using regex patterns and rules. It supports multiple programming languages and generates reports in HTML, JSON, SARIF, and text formats.

## Tech Stack

- **Language**: C# (.NET 6.0+)
- **Build System**: dotnet CLI
- **Test Framework**: xUnit
- **Solution Structure**: Multi-project .NET solution with 7 projects
- **Rule Format**: JSON files following rule-schema-v1.json

## Project Structure

- `AppInspector.CLI/` - Command-line interface application
- `AppInspector/` - Core commands library (AppInspector.Commands.csproj)
- `AppInspector.RulesEngine/` - Rule processing and pattern matching engine
- `AppInspector.Common/` - Common utilities and types
- `AppInspector.Logging/` - Logging infrastructure
- `AppInspector.Tests/` - Test suite using xUnit
- `AppInspector.Benchmarks/` - Performance benchmarks
- `AppInspector/rules/default/` - Built-in detection rules organized by category

## Build and Test Commands

### Building
```bash
# Build debug version
dotnet build

# Build release version
dotnet build -c Release

# Platform-specific publish
dotnet publish -c Release -r win10-x64
dotnet publish -c Release -r linux-x64
dotnet publish -c Release -r osx-x64
```

### Testing
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test AppInspector.Tests/AppInspector.Tests.csproj

# Run tests with verbose output
dotnet test --verbosity normal
```

### Running the CLI
```bash
# Analyze code
dotnet run --project AppInspector.CLI -- analyze -s path/to/source

# Verify rules
dotnet run --project AppInspector.CLI -- verifyrules -r path/to/rules

# Export tags
dotnet run --project AppInspector.CLI -- exporttags
```

## Coding Standards and Conventions

### General C# Guidelines

- Follow standard C# naming conventions (PascalCase for public members, camelCase for private fields)
- Use meaningful, descriptive names for variables, methods, and classes
- Keep methods focused and single-purpose
- Add XML documentation comments for public APIs
- Use nullable reference types where appropriate
- Prefer `var` for local variables when the type is obvious
- Use LINQ for collection operations when it improves readability

### Specific Project Conventions

- **Namespace Structure**: Follow the project name structure (e.g., `Microsoft.ApplicationInspector.Commands`)
- **Logging**: Use the logging infrastructure from `AppInspector.Logging` namespace
- **Error Handling**: Return structured result types rather than throwing exceptions for expected failures
- **Async/Await**: Use async methods for I/O operations; follow the Task-based Asynchronous Pattern (TAP)

## Rule Development Guidelines

### Rule File Structure

Rules are JSON files located in `AppInspector/rules/default/` organized by category:
- `cryptography/` - Cryptographic operations
- `security_feature/` - Security-related features (authentication, authorization, etc.)
- `data_handling/` - Database, serialization, deserialization
- `os/` - Operating system operations (file I/O, process execution, reflection)
- `cloud_services/` - Cloud platform APIs
- `frameworks/` - Framework detection
- And more...

### Rule Schema Requirements

Each rule must include:
- `id` - Unique identifier (format: AI######, e.g., AI038900)
- `name` - Human-readable name
- `tags` - Array of tags describing the feature (e.g., "Authentication.Microsoft.Online")
- `patterns` - Array of pattern objects for detection
- `description` - Optional detailed description
- `severity` - Optional: Critical, Important, Moderate, BestPractice, ManualReview, Unspecified
- `applies_to` - Optional: array of language identifiers (e.g., ["csharp", "java"])

### Pattern Types

- `string` - Literal string match
- `regex` - Regular expression
- `regexword` - Regex with word boundaries automatically added
- `substring` - Substring match

### Pattern Best Practices

1. **Use specific patterns**: Prefer specific API/class names over generic words to reduce false positives
2. **Word boundaries**: Use `\b` in regex patterns or `regexword` type to prevent matching substrings
3. **Avoid catastrophic backtracking**: Don't use unlimited repetition patterns like `.*` in regex
4. **Test cases**: Include `must-match` and `must-not-match` test cases in rules for validation
5. **Confidence levels**: Set appropriate confidence (high, medium, low) based on pattern specificity
6. **Scopes**: Specify appropriate scopes (code, comment) to limit false positives
7. **Conditions**: Use conditions with `same-line` search to ensure API methods are invoked, not just referenced
8. **Pattern for invocation**: Use `.Method(` patterns instead of `Class.Method` to catch actual usage via variables

### Example Rule Pattern

```json
{
  "name": "Authentication: Microsoft (Identity)",
  "id": "AI038900",
  "description": "Authentication using Microsoft Identity Platform (ADAL/MSAL)",
  "tags": ["Authentication.Microsoft.Online"],
  "severity": "critical",
  "patterns": [
    {
      "pattern": "microsoft\\.msal",
      "type": "regexword",
      "scopes": ["code"],
      "confidence": "high"
    },
    {
      "pattern": "AcquireTokenForClient",
      "type": "regexword",
      "scopes": ["code"],
      "confidence": "high"
    }
  ]
}
```

Note: This is a simplified example. Real rules may use alternation (e.g., `pattern1|pattern2`) to match multiple alternatives in a single pattern.

### Rule ID Conventions

- Rule IDs follow consistent patterns within categories
- Authentication rules: AI038900 - AI041999 range
- Ensure new rule IDs don't conflict with existing ones
- Sequential numbering within a category

## Testing Conventions

### Test Organization

- Tests use xUnit framework
- Test classes organized under `AppInspector.Tests/Commands/`
- Test data located in `AppInspector.Tests/TestData/`
- Use descriptive test method names that explain what is being tested
- Use test fixtures (`IClassFixture<>`) for shared setup/teardown

### Test Patterns

```csharp
[Fact]
public void TestMethodName_Scenario_ExpectedBehavior()
{
    // Arrange
    var options = new AnalyzeOptions { /* ... */ };
    
    // Act
    var result = command.GetResult();
    
    // Assert
    Assert.True(result.ResultCode == AnalyzeResult.ExitCode.Success);
}
```

## Security Considerations

- **No hardcoded secrets**: Never commit credentials, API keys, or sensitive data
- **Input validation**: Validate all user inputs, especially file paths and patterns
- **Safe regex**: Avoid regex patterns that can cause catastrophic backtracking
- **Secure defaults**: Use secure defaults for cryptographic operations
- **Sanitization**: Use appropriate sanitizers (e.g., `html_escape`) to prevent injection vulnerabilities

## Performance Considerations

- **Regex compilation**: Consider caching compiled regex patterns for frequently used rules
- **File I/O**: Use async I/O for file operations
- **Memory usage**: Be mindful of memory when processing large codebases
- **Parallel processing**: Leverage parallel processing where appropriate for scanning multiple files

## Common Pitfalls to Avoid

1. **Rule regex performance**: Avoid complex regex patterns that can cause performance issues on large files
2. **False positives**: Test rules thoroughly to minimize false positive detections
3. **Breaking changes**: Maintain backward compatibility in rule schema and command-line interface
4. **Platform dependencies**: Ensure code works across Windows, Linux, and macOS
5. **Resource cleanup**: Properly dispose of file handles and other resources

## Code Review Checklist

Before submitting changes:
- [ ] Code builds without errors (`dotnet build`)
- [ ] All tests pass (`dotnet test`)
- [ ] New rules include test cases (must-match and must-not-match)
- [ ] New rules validate successfully (`dotnet run --project AppInspector.CLI -- verifyrules`)
- [ ] XML documentation added for public APIs
- [ ] No hardcoded secrets or sensitive data
- [ ] Changes are backward compatible
- [ ] Performance impact considered for rule changes

## Useful Commands

```bash
# Verify rule syntax
dotnet run --project AppInspector.CLI -- verifyrules -r AppInspector/rules/default/

# Pack rules into single file
dotnet run --project AppInspector.CLI -- packrules -r AppInspector/rules/default/ -o packed-rules.json

# Run benchmarks
dotnet run --project AppInspector.Benchmarks -c Release

# Clean build artifacts
dotnet clean
```

## Resources

- [Project Wiki](https://github.com/microsoft/ApplicationInspector/wiki)
- [Understanding Rules](https://github.com/microsoft/ApplicationInspector/wiki/3.-Understanding-Rules)
- [CLI Usage](https://github.com/microsoft/ApplicationInspector/wiki/1.-CLI-Usage)
- [NuGet Support](https://github.com/microsoft/ApplicationInspector/wiki/2.-NuGet-Support)
- [Rule Schema](https://github.com/microsoft/ApplicationInspector/blob/main/rule-schema-v1.json)

## Getting Help

- Review the [CONTRIBUTING.md](https://github.com/microsoft/ApplicationInspector/blob/main/CONTRIBUTING.md) for contribution guidelines
- Check [BUILD.md](https://github.com/microsoft/ApplicationInspector/blob/main/BUILD.md) for detailed build instructions
- See [SECURITY.md](https://github.com/microsoft/ApplicationInspector/blob/main/SECURITY.md) for security reporting procedures
