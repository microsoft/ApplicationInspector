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
- `severity`: One of: critical, important, moderate, bestpractice, manualreview, unspecified (lowercase preferred; default: moderate)
- `applies_to`: Array of file extensions or language identifiers
- `does_not_apply_to`: Array of file extensions or language identifiers to exclude
- `applies_to_file_regex`: Array of regex patterns for files to include
- `exclude_file_regex`: Array of regex patterns for files to exclude
- `depends_on_tags`: Array of tags that must be present for this rule to apply
- `overrides`: Array of rule IDs that this rule supersedes
- `conditions`: Array of additional matching conditions
- `expression`: Boolean expression combining pattern and condition labels (see [Expressions](#expressions))
- `must-match`: Array of test strings that should match (for validation)
- `must-not-match`: Array of test strings that should not match (for validation)
- `_comment`: Optional comment for documentation

### Pattern Fields

- `pattern`: The regex/string pattern to search for (required)
- `type`: One of: regex (default), regexword, string, substring
- `label`: Name for this pattern, for use in `expression` (default: the pattern's index)
- `conditions`: Array of conditions that gate only this pattern's findings
- `scopes`: Array of: code, comment, all, html
- `confidence`: One of: high, medium, low, unspecified
- `modifiers`: Array of regex modifiers: i, m, s, x (or full names)
- `xpaths`: Array of XPath expressions for XML matching
- `jsonpaths`: Array of JSONPath expressions for JSON matching
- `ymlpaths`: Array of YAMLPath expressions for YAML matching
- `_comment`: Optional comment for documentation

### Condition Fields

- `pattern`: A pattern object (same structure as patterns array)
- `search_in`: Where to search - "finding-region(-offset,length)", "finding-only", "same-line", "same-file", "only-before", "only-after"
- `negate_finding`: Boolean - if true, the finding is invalid if this condition matches
- `label`: Name for this condition, for use in `expression` (default: the condition's clause index)
- `applies_to`: Array of languages this condition applies to (default: all)
- `does_not_apply_to`: Array of languages this condition does not apply to
- `_comment`: Optional comment for documentation

## Expressions

By default a rule matches when **any** pattern matches and **every** rule level condition holds:

```text
(pattern0 OR pattern1 OR ...) AND condition0 AND condition1 ...
```

Two optional features let you go beyond that shape.

### Scoping a condition to one pattern

Declare the condition on the pattern it guards rather than at rule level. Other patterns are unaffected,
so they still report even when the condition excludes this pattern's finding.

```json
"patterns": [
  {
    "pattern": "curl", "type": "substring", "label": "curl",
    "conditions": [
      {
        "pattern": { "pattern": "--tlsv1.3", "type": "substring" },
        "search_in": "same-line",
        "negate_finding": true
      }
    ]
  },
  { "pattern": "wget", "type": "substring", "label": "wget" }
]
```

### Writing the expression yourself

Set `expression` to combine labels with `AND`, `OR`, `XOR`, `NAND`, `NOR` and `NOT`. This makes
otherwise inexpressible rules possible, such as a disjunction of conditions or a negated conjunction:

```json
"expression": "cookie AND NOT (secure AND httponly)"
```

That rule fires when *either* required flag is missing. Writing it as two negated conditions would
instead mean "neither flag is present", which stays silent on partially hardened code.

Every finding is judged against the expression on its own. A file containing one correctly hardened
cookie and one missing a flag reports the second, because the first satisfying `secure AND httponly`
says nothing about the second.

> **Expressions have no operator precedence.** They are evaluated strictly left to right, so
> `a OR b AND c` means `(a OR b) AND c`, **not** `a OR (b AND c)`. Always use parentheses to make
> grouping explicit; rule verification rejects an expression that mixes operators at the same level
> without them.

Other rules for expressions:

- Labels may not contain spaces or parentheses, and must be unique within the rule.
- Every label named in the expression must belong to a pattern or condition in the same rule.
- Attach parentheses to labels: `(a OR b)` is valid, `( a OR b )` is not.
- Parentheses must be balanced.
- A rule that sets `expression` must express negation with `NOT` rather than `negate_finding`.
- A condition can only test findings produced by patterns evaluated before it, so place at least one
  pattern label ahead of any condition label.

## Best Practices

1. **Use lowercase for enums**: severity, type, confidence, and scopes should use lowercase values
2. **Populate optional fields**: Only include optional fields like `recommendation` or `_comment` if they contain meaningful content
3. **Test your rules**: Always include must-match and must-not-match examples
4. **Use appropriate severity**: Choose severity levels that reflect the actual impact
5. **Use specific patterns**: Make patterns as specific as possible to reduce false positives
6. **Document with comments**: Use `_comment` fields to explain complex patterns or conditions
7. **Parenthesise expressions**: Expressions fold left to right with no precedence, so group explicitly

## Related Documentation

- [Rule Schema](../../../rule-schema-v1.json)
- [Application Inspector Wiki](https://github.com/microsoft/ApplicationInspector/wiki)
