// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Microsoft.ApplicationInspector.Commands;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.ApplicationInspector.RulesEngine.Schema;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace Microsoft.ApplicationInspector.Tests.RuleProcessor
{
    [ExcludeFromCodeCoverage]
    public class SchemaValidationTests
    {
        [Fact]
        public void SchemaProvider_LoadsEmbeddedSchema()
        {
            var provider = new RuleSchemaProvider();
            var schema = provider.GetSchema();
            
            Assert.NotNull(schema);
        }

        [Fact]
        public void ValidRule_PassesSchemaValidation()
        {
            var provider = new RuleSchemaProvider();
            var rule = new Rule
            {
                Id = "TEST001",
                Name = "Test Rule",
                Description = "A test rule",
                Tags = new[] { "Test.Tag" },
                Patterns = new[]
                {
                    new SearchPattern
                    {
                        Pattern = "test",
                        PatternType = PatternType.String,
                        Scopes = new[] { PatternScope.Code },
                        Confidence = Confidence.High
                    }
                }
            };

            
            var result = provider.ValidateRule(rule);
            
            if (!result.IsValid)
            {
                var errorMessages = string.Join("; ", result.Errors.Select(e => $"'{e.Message}' at '{e.Path}' (Type: {e.ErrorType})"));
                Assert.Fail($"Schema validation failed: {errorMessages}");
            }
        }

        [Fact]
        public void RuleWithoutRequiredFields_FailsSchemaValidation()
        {
            var provider = new RuleSchemaProvider();
            var rule = new Rule
            {
                // Missing required fields: id, name, tags, patterns
                Description = "A test rule"
            };

            var result = provider.ValidateRule(rule);
            
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Errors);
        }

        [Fact]
        public void RulesVerifier_WithSchemaValidation_ValidatesRules()
        {
            var options = new RulesVerifierOptions
            {
                EnableSchemaValidation = true,
                SchemaValidationLevel = SchemaValidationLevel.Error
            };

            var verifier = new RulesVerifier(options);
            var ruleset = new RuleSet();
            
            // Add a valid rule
            var validRule = new Rule
            {
                Id = "TEST001",
                Name = "Valid Test Rule",
                Description = "A valid test rule",
                Tags = new[] { "Test.Valid" },
                Patterns = new[]
                {
                    new SearchPattern
                    {
                        Pattern = "validtest",
                        PatternType = PatternType.String,
                        Scopes = new[] { PatternScope.Code },
                        Confidence = Confidence.High
                    }
                }
            };
            ruleset.AddRule(validRule);

            var result = verifier.Verify(ruleset);
            
            Assert.NotNull(result);
            Assert.NotEmpty(result.RuleStatuses);
            
            var ruleStatus = result.RuleStatuses.First();
            Assert.True(ruleStatus.PassedSchemaValidation);
            Assert.Empty(ruleStatus.SchemaValidationErrors);
        }

        [Fact]
        public void RulesVerifier_WithInvalidRule_ReportsSchemaErrors()
        {
            var options = new RulesVerifierOptions
            {
                EnableSchemaValidation = true,
                SchemaValidationLevel = SchemaValidationLevel.Warning
            };

            var verifier = new RulesVerifier(options);
            var ruleset = new RuleSet();
            
            // Add an invalid rule (missing required fields)
            var invalidRule = new Rule
            {
                Description = "An invalid rule missing required fields"
                // Missing: id, name, tags, patterns
            };
            ruleset.AddRule(invalidRule);

            var result = verifier.Verify(ruleset);
            
            Assert.NotNull(result);
            Assert.NotEmpty(result.RuleStatuses);
            
            var ruleStatus = result.RuleStatuses.First();
            Assert.False(ruleStatus.PassedSchemaValidation);
            Assert.NotEmpty(ruleStatus.SchemaValidationErrors);
        }

        [Fact]
        public void SchemaValidationDisabled_SkipsValidation()
        {
            var options = new RulesVerifierOptions
            {
                EnableSchemaValidation = false // Disabled
            };

            var verifier = new RulesVerifier(options);
            var ruleset = new RuleSet();
            
            // Add an invalid rule
            var invalidRule = new Rule
            {
                Description = "An invalid rule"
                // Missing required fields
            };
            ruleset.AddRule(invalidRule);

            var result = verifier.Verify(ruleset);
            
            Assert.NotNull(result);
            Assert.NotEmpty(result.RuleStatuses);
            
            var ruleStatus = result.RuleStatuses.First();
            // Since schema validation is disabled, it should pass schema validation by default
            Assert.True(ruleStatus.PassedSchemaValidation);
            Assert.Empty(ruleStatus.SchemaValidationErrors);
        }

        [Fact]
        public void RuleWithInvalidPatternType_FailsSchemaValidation()
        {
            var provider = new RuleSchemaProvider();
            
            // Create a rule with an invalid pattern type by serializing a custom JSON string
            var invalidJson = @"[
                {
                    ""id"": ""TEST_INVALID_PATTERN"",
                    ""name"": ""Test Rule with Invalid Pattern Type"",
                    ""description"": ""A test rule with invalid pattern type"",
                    ""tags"": [""Test.Invalid""],
                    ""patterns"": [
                        {
                            ""pattern"": ""test"",
                            ""type"": ""InvalidPatternType"",
                            ""scopes"": [""Code""],
                            ""confidence"": ""High""
                        }
                    ]
                }
            ]";

            var result = provider.ValidateJson(invalidJson);
            
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Errors);
        }

        [Fact]
        public void RuleWithInvalidConfidenceLevel_FailsSchemaValidation()
        {
            var provider = new RuleSchemaProvider();
            
            var invalidJson = @"[
                {
                    ""id"": ""TEST_INVALID_CONFIDENCE"",
                    ""name"": ""Test Rule with Invalid Confidence"",
                    ""description"": ""A test rule with invalid confidence level"",
                    ""tags"": [""Test.Invalid""],
                    ""patterns"": [
                        {
                            ""pattern"": ""test"",
                            ""type"": ""String"",
                            ""scopes"": [""Code""],
                            ""confidence"": ""VeryHigh""
                        }
                    ]
                }
            ]";

            var result = provider.ValidateJson(invalidJson);
            
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Errors);
        }

        [Fact]
        public void RuleWithInvalidSeverity_FailsSchemaValidation()
        {
            var provider = new RuleSchemaProvider();
            
            var invalidJson = @"[
                {
                    ""id"": ""TEST_INVALID_SEVERITY"",
                    ""name"": ""Test Rule with Invalid Severity"",
                    ""description"": ""A test rule with invalid severity level"",
                    ""tags"": [""Test.Invalid""],
                    ""severity"": ""SuperCritical"",
                    ""patterns"": [
                        {
                            ""pattern"": ""test"",
                            ""type"": ""String"",
                            ""scopes"": [""Code""],
                            ""confidence"": ""High""
                        }
                    ]
                }
            ]";

            var result = provider.ValidateJson(invalidJson);
            
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Errors);
        }

        [Fact]
        public void RuleWithInvalidScope_FailsSchemaValidation()
        {
            var provider = new RuleSchemaProvider();
            
            var invalidJson = @"[
                {
                    ""id"": ""TEST_INVALID_SCOPE"",
                    ""name"": ""Test Rule with Invalid Scope"",
                    ""description"": ""A test rule with invalid scope"",
                    ""tags"": [""Test.Invalid""],
                    ""patterns"": [
                        {
                            ""pattern"": ""test"",
                            ""type"": ""String"",
                            ""scopes"": [""InvalidScope""],
                            ""confidence"": ""High""
                        }
                    ]
                }
            ]";

            var result = provider.ValidateJson(invalidJson);
            
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Errors);
        }

        [Fact]
        public void RuleWithEmptyTagsArray_FailsSchemaValidation()
        {
            var provider = new RuleSchemaProvider();
            
            var invalidJson = @"[
                {
                    ""id"": ""TEST_EMPTY_TAGS"",
                    ""name"": ""Test Rule with Empty Tags"",
                    ""description"": ""A test rule with empty tags array"",
                    ""tags"": [],
                    ""patterns"": [
                        {
                            ""pattern"": ""test"",
                            ""type"": ""String"",
                            ""scopes"": [""Code""],
                            ""confidence"": ""High""
                        }
                    ]
                }
            ]";

            var result = provider.ValidateJson(invalidJson);
            
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Errors);
        }

        [Fact]
        public void RuleWithEmptyPatternsArray_FailsSchemaValidation()
        {
            var provider = new RuleSchemaProvider();
            
            var invalidJson = @"[
                {
                    ""id"": ""TEST_EMPTY_PATTERNS"",
                    ""name"": ""Test Rule with Empty Patterns"",
                    ""description"": ""A test rule with empty patterns array"",
                    ""tags"": [""Test.Invalid""],
                    ""patterns"": []
                }
            ]";

            var result = provider.ValidateJson(invalidJson);
            
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Errors);
        }

        [Fact]
        public void RuleWithInvalidRegexModifier_FailsSchemaValidation()
        {
            var provider = new RuleSchemaProvider();
            
            var invalidJson = @"[
                {
                    ""id"": ""TEST_INVALID_MODIFIER"",
                    ""name"": ""Test Rule with Invalid Modifier"",
                    ""description"": ""A test rule with invalid regex modifier"",
                    ""tags"": [""Test.Invalid""],
                    ""patterns"": [
                        {
                            ""pattern"": ""test"",
                            ""type"": ""Regex"",
                            ""modifiers"": [""invalidmodifier""],
                            ""scopes"": [""Code""],
                            ""confidence"": ""High""
                        }
                    ]
                }
            ]";

            var result = provider.ValidateJson(invalidJson);
            
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Errors);
        }

        [Fact]
        public void RuleWithInvalidSearchCondition_FailsSchemaValidation()
        {
            var provider = new RuleSchemaProvider();
            
            var invalidJson = @"[
                {
                    ""id"": ""TEST_INVALID_CONDITION"",
                    ""name"": ""Test Rule with Invalid Search Condition"",
                    ""description"": ""A test rule with invalid search condition"",
                    ""tags"": [""Test.Invalid""],
                    ""patterns"": [
                        {
                            ""pattern"": ""test"",
                            ""type"": ""String"",
                            ""scopes"": [""Code""],
                            ""confidence"": ""High""
                        }
                    ],
                    ""conditions"": [
                        {
                            ""pattern"": {
                                ""pattern"": ""condition"",
                                ""type"": ""String""
                            },
                            ""search_in"": ""invalid-search-location""
                        }
                    ]
                }
            ]";

            var result = provider.ValidateJson(invalidJson);
            
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Errors);
        }

        [Fact]
        public void RuleWithMissingRequiredPatternField_FailsSchemaValidation()
        {
            var provider = new RuleSchemaProvider();
            
            var invalidJson = @"[
                {
                    ""id"": ""TEST_MISSING_PATTERN"",
                    ""name"": ""Test Rule with Missing Pattern Field"",
                    ""description"": ""A test rule with missing pattern field"",
                    ""tags"": [""Test.Invalid""],
                    ""patterns"": [
                        {
                            ""type"": ""String"",
                            ""scopes"": [""Code""],
                            ""confidence"": ""High""
                        }
                    ]
                }
            ]";

            var result = provider.ValidateJson(invalidJson);
            
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Errors);
        }

        [Fact]
        public void RuleWithEmptyId_FailsSchemaValidation()
        {
            var provider = new RuleSchemaProvider();
            
            var invalidJson = @"[
                {
                    ""id"": """",
                    ""name"": ""Test Rule with Empty ID"",
                    ""description"": ""A test rule with empty ID"",
                    ""tags"": [""Test.Invalid""],
                    ""patterns"": [
                        {
                            ""pattern"": ""test"",
                            ""type"": ""String"",
                            ""scopes"": [""Code""],
                            ""confidence"": ""High""
                        }
                    ]
                }
            ]";

            var result = provider.ValidateJson(invalidJson);
            
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Errors);
        }

        [Fact]
        public void RulesVerifier_WithSchemaValidationError_HandlesErrorLevel()
        {
            var options = new RulesVerifierOptions
            {
                EnableSchemaValidation = true,
                SchemaValidationLevel = SchemaValidationLevel.Error
            };

            var verifier = new RulesVerifier(options);
            var ruleset = new RuleSet();
            
            // Add a rule with invalid pattern type using reflection to bypass normal validation
            var invalidRule = new Rule
            {
                Id = "TEST_SCHEMA_ERROR",
                Name = "Invalid Rule for Schema Error Test",
                Description = "A rule that will fail schema validation",
                Tags = new[] { "Test.SchemaError" },
                Patterns = new[]
                {
                    new SearchPattern
                    {
                        Pattern = "test",
                        PatternType = PatternType.String,
                        Scopes = new[] { PatternScope.Code },
                        Confidence = Confidence.High
                    }
                }
            };

            // Simulate a schema validation failure by setting the result directly
            invalidRule.SchemaValidationResult = new SchemaValidationResult
            {
                IsValid = false,
                Errors = new List<SchemaValidationError>
                {
                    new SchemaValidationError
                    {
                        Message = "Test schema validation error",
                        Path = "/0/patterns/0/type",
                        ErrorType = "SchemaViolation"
                    }
                }
            };

            ruleset.AddRule(invalidRule);

            var result = verifier.Verify(ruleset);
            
            Assert.NotNull(result);
            Assert.NotEmpty(result.RuleStatuses);
            
            var ruleStatus = result.RuleStatuses.First();
            Assert.False(ruleStatus.PassedSchemaValidation);
            Assert.NotEmpty(ruleStatus.SchemaValidationErrors);
            Assert.NotEmpty(ruleStatus.Errors); // Should have errors when SchemaValidationLevel is Error
        }

        [Fact]
        public void RulesVerifier_WithSchemaValidationWarning_HandlesWarningLevel()
        {
            var options = new RulesVerifierOptions
            {
                EnableSchemaValidation = true,
                SchemaValidationLevel = SchemaValidationLevel.Warning
            };

            var verifier = new RulesVerifier(options);
            var ruleset = new RuleSet();
            
            // Add a rule with schema validation failure
            var invalidRule = new Rule
            {
                Id = "TEST_SCHEMA_WARNING",
                Name = "Invalid Rule for Schema Warning Test",
                Description = "A rule that will fail schema validation but only warn",
                Tags = new[] { "Test.SchemaWarning" },
                Patterns = new[]
                {
                    new SearchPattern
                    {
                        Pattern = "test",
                        PatternType = PatternType.String,
                        Scopes = new[] { PatternScope.Code },
                        Confidence = Confidence.High
                    }
                }
            };

            // Simulate a schema validation failure by setting the result directly
            invalidRule.SchemaValidationResult = new SchemaValidationResult
            {
                IsValid = false,
                Errors = new List<SchemaValidationError>
                {
                    new SchemaValidationError
                    {
                        Message = "Test schema validation warning",
                        Path = "/0/patterns/0/type",
                        ErrorType = "SchemaViolation"
                    }
                }
            };

            ruleset.AddRule(invalidRule);

            var result = verifier.Verify(ruleset);
            
            Assert.NotNull(result);
            Assert.NotEmpty(result.RuleStatuses);
            
            var ruleStatus = result.RuleStatuses.First();
            Assert.False(ruleStatus.PassedSchemaValidation);
            Assert.NotEmpty(ruleStatus.SchemaValidationErrors);
            // Errors collection should not contain schema validation errors since this is only a warning level
            Assert.DoesNotContain(ruleStatus.Errors, e => e.Contains("Schema validation error"));
        }

        [Fact]
        public void DefaultRules_PassSchemaValidation()
        {
            var provider = new RuleSchemaProvider();
            
            // Load the embedded default rules using RuleSetUtils
            var defaultRuleSet = RuleSetUtils.GetDefaultRuleSet();
            var rules = defaultRuleSet.GetAppInspectorRules().ToArray();
            
            Assert.NotNull(rules);
            Assert.NotEmpty(rules);
            
            var failedRules = new List<string>();
            
            foreach (var rule in rules)
            {
                var result = provider.ValidateRule(rule);
                
                if (!result.IsValid)
                {
                    var errors = string.Join("; ", result.Errors.Select(e => $"{e.ErrorType}: {e.Message} at {e.Path}"));
                    failedRules.Add($"Rule {rule.Id} ({rule.Name}): {errors}");
                }
            }
            
            Assert.True(failedRules.Count == 0, 
                $"All built-in rules should pass schema validation. Failed rules:\n{string.Join("\n", failedRules)}");
        }
    }
}
