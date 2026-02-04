# Application Inspector Sample Rules

This directory contains comprehensive sample rules that demonstrate all available fields and features for creating custom Application Inspector rules.

## Files

### comprehensive_sample_rule.json

A maximally specified sample rule file that demonstrates:

- **All top-level rule fields**: id, name, description, tags, severity, applies_to, does_not_apply_to, applies_to_file_regex, exclude_file_regex, depends_on_tags, overrides
- **Pattern matching types**: regex, regexword, string
- **Pattern features**: scopes, modifiers, confidence levels
- **Conditions**: same-file matching with both positive and negative conditions
- **Self-testing**: must-match and must-not-match arrays for validation
- **Structured data queries**: XPath for XML, JSONPath for JSON, YAMLPath for YAML
- **Comments**: Using _comment fields for documentation

## Usage

These sample rules are for reference and learning purposes. They demonstrate the full capabilities of the Application Inspector rule format.

To use these samples:

1. Copy and modify them for your specific detection needs
2. Adjust the patterns to match your target code constructs
3. Update the tags to fit your categorization scheme
4. Set appropriate severity levels
5. Add must-match/must-not-match test cases to validate your rules

## Field Descriptions

### Required Fields

- `id`: Unique identifier for the rule (e.g., "AI001234")
- `name`: Human-readable name describing what the rule detects
- `tags`: Array of categorization tags (at least one required)
- `patterns`: Array of search patterns (at least one required)

### Optional Fields

- `description`: Detailed description of what the rule detects
- `severity`: One of: critical, important, moderate, bestpractice, manualreview, unspecified (default: moderate)
- `applies_to`: Array of file extensions or language identifiers
- `does_not_apply_to`: Array of file extensions or language identifiers to exclude
- `applies_to_file_regex`: Array of regex patterns for files to include
- `exclude_file_regex`: Array of regex patterns for files to exclude
- `depends_on_tags`: Array of tags that must be present for this rule to apply
- `overrides`: Array of rule IDs that this rule supersedes
- `conditions`: Array of additional matching conditions
- `must-match`: Array of test strings that should match (for validation)
- `must-not-match`: Array of test strings that should not match (for validation)
- `_comment`: Optional comment for documentation

### Pattern Fields

- `pattern`: The regex/string pattern to search for (required)
- `type`: One of: regex (default), regexword, string, substring
- `scopes`: Array of: code, comment, all, html
- `confidence`: One of: high, medium, low, unspecified
- `modifiers`: Array of regex modifiers: i, m, s, x (or full names)
- `xpaths`: Array of XPath expressions for XML matching
- `jsonpaths`: Array of JSONPath expressions for JSON matching
- `ymlpaths`: Array of YAMLPath expressions for YAML matching
- `_comment`: Optional comment for documentation

### Condition Fields

- `pattern`: A pattern object (same structure as patterns array)
- `search_in`: Where to search - "file", "finding-region(-offset,length)", "finding-only", "same-line", "same-file"
- `negate_finding`: Boolean - if true, the finding is invalid if this condition matches
- `_comment`: Optional comment for documentation

## Best Practices

1. **Use lowercase for enums**: severity, type, confidence, and scopes should use lowercase values
2. **Populate optional fields**: Only include optional fields like `recommendation` or `_comment` if they contain meaningful content
3. **Test your rules**: Always include must-match and must-not-match examples
4. **Use appropriate severity**: Choose severity levels that reflect the actual impact
5. **Use specific patterns**: Make patterns as specific as possible to reduce false positives
6. **Document with comments**: Use `_comment` fields to explain complex patterns or conditions

## Related Documentation

- [Rule Schema](../../../rule-schema-v1.json)
- [Application Inspector Wiki](https://github.com/microsoft/ApplicationInspector/wiki)
