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

            // Debug: Check what JSON is generated
            var json = System.Text.Json.JsonSerializer.Serialize(new[] { rule }, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true
            });
            
            var result = provider.ValidateRule(rule);
            
            // Debug: Show what errors we're getting
            if (!result.IsValid)
            {
                var errorMessages = string.Join("; ", result.Errors.Select(e => $"'{e.Message}' at '{e.Path}' (Type: {e.ErrorType})"));
                throw new Exception($"Schema validation failed. JSON: {json}\n\nErrors: {errorMessages}");
            }
            Assert.Empty(result.Errors);
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
