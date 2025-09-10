// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Json.Schema;

namespace Microsoft.ApplicationInspector.RulesEngine.Schema
{
    /// <summary>
    /// Provides JSON schema validation for Application Inspector rules
    /// </summary>
    public class RuleSchemaProvider
    {
        private readonly JsonSchema _schema;
        private const string SCHEMA_RESOURCE_NAME = "Microsoft.ApplicationInspector.RulesEngine.Schema.rule-schema.json";

        /// <summary>
        /// Initialize the schema provider
        /// </summary>
        /// <param name="customSchemaPath">Optional path to custom schema file</param>
        public RuleSchemaProvider(string? customSchemaPath = null)
        {
            if (!string.IsNullOrEmpty(customSchemaPath))
            {
                _schema = LoadSchemaFromFile(customSchemaPath);
            }
            else
            {
                _schema = LoadEmbeddedSchema();
            }
        }

        private JsonSchema LoadEmbeddedSchema()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(SCHEMA_RESOURCE_NAME)
                ?? throw new InvalidOperationException($"Could not find embedded schema resource: {SCHEMA_RESOURCE_NAME}");
            using var reader = new StreamReader(stream);
            var schemaJson = reader.ReadToEnd();
            return JsonSchema.FromText(schemaJson);
        }

        private JsonSchema LoadSchemaFromFile(string path)
        {
            var schemaJson = File.ReadAllText(path);
            return JsonSchema.FromText(schemaJson);
        }

        /// <summary>
        /// Get the loaded JSON schema
        /// </summary>
        /// <returns>The JsonSchema instance</returns>
        public JsonSchema GetSchema() => _schema;

        /// <summary>
        /// Validate JSON string against the schema
        /// </summary>
        /// <param name="json">JSON string to validate</param>
        /// <returns>Schema validation result</returns>
        public SchemaValidationResult ValidateJson(string json)
        {
            try
            {
                var schema = GetSchema();
                var document = JsonDocument.Parse(json);
                
                var evaluationOptions = new EvaluationOptions
                {
                    RequireFormatValidation = true,
                    OutputFormat = OutputFormat.Hierarchical
                };
                
                var result = schema.Evaluate(document, evaluationOptions);
                
                var errors = new List<SchemaValidationError>();
                if (!result.IsValid)
                {
                    // Try to collect errors from the result
                    CollectErrorsFromResult(result, errors, "");
                    
                    // If no errors were collected but validation failed, add a generic message
                    if (errors.Count == 0)
                    {
                        errors.Add(new SchemaValidationError
                        {
                            Message = "Schema validation failed but no specific errors were reported",
                            Path = "",
                            ErrorType = "Unknown"
                        });
                    }
                }
                
                return new SchemaValidationResult
                {
                    IsValid = result.IsValid,
                    Errors = errors
                };
            }
            catch (Exception ex)
            {
                return new SchemaValidationResult
                {
                    IsValid = false,
                    Errors = new List<SchemaValidationError> 
                    { 
                        new SchemaValidationError
                        {
                            Message = $"JSON parsing error: {ex.Message}",
                            Path = "",
                            ErrorType = "ParseError"
                        }
                    }
                };
            }
        }

        private void CollectErrorsFromResult(EvaluationResults result, List<SchemaValidationError> errors, string currentPath)
        {
            // Check if there are any details with errors
            if (result.Details != null)
            {
                foreach (var detail in result.Details)
                {
                    if (!detail.IsValid)
                    {
                        var path = detail.InstanceLocation?.ToString() ?? currentPath;
                        var schemaPath = detail.SchemaLocation?.ToString() ?? "";
                        
                        errors.Add(new SchemaValidationError
                        {
                            Message = $"Validation failed at schema location '{schemaPath}'",
                            Path = path,
                            ErrorType = "SchemaViolation"
                        });
                        
                        // Recursively collect errors from nested details
                        CollectErrorsFromResult(detail, errors, path);
                    }
                }
            }
        }

        /// <summary>
        /// Validate a collection of rules against the schema
        /// </summary>
        /// <param name="rules">Rules to validate</param>
        /// <returns>Schema validation result</returns>
        public SchemaValidationResult ValidateRules(IEnumerable<Rule> rules)
        {
            var json = JsonSerializer.Serialize(rules, new JsonSerializerOptions 
            { 
                WriteIndented = true
                // Do not use any naming policy - the Rule class already has JsonPropertyName attributes
            });
            return ValidateJson(json);
        }

        /// <summary>
        /// Validate a single rule against the schema (wrapped in an array)
        /// </summary>
        /// <param name="rule">Rule to validate</param>
        /// <returns>Schema validation result</returns>
        public SchemaValidationResult ValidateRule(Rule rule)
        {
            return ValidateRules(new[] { rule });
        }
    }

    /// <summary>
    /// Result of schema validation operation
    /// </summary>
    public class SchemaValidationResult
    {
        /// <summary>
        /// Whether the validation passed
        /// </summary>
        public bool IsValid { get; set; }
        
        /// <summary>
        /// List of validation errors
        /// </summary>
        public List<SchemaValidationError> Errors { get; set; } = new();
    }

    /// <summary>
    /// A schema validation error
    /// </summary>
    public class SchemaValidationError
    {
        /// <summary>
        /// Error message
        /// </summary>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// JSON path where the error occurred
        /// </summary>
        public string Path { get; set; } = string.Empty;
        
        /// <summary>
        /// Line number where the error occurred
        /// </summary>
        public int LineNumber { get; set; }
        
        /// <summary>
        /// Line position where the error occurred
        /// </summary>
        public int LinePosition { get; set; }
        
        /// <summary>
        /// Type of error
        /// </summary>
        public string ErrorType { get; set; } = string.Empty;
    }

    /// <summary>
    /// Defines how schema validation failures should be handled
    /// </summary>
    public enum SchemaValidationLevel
    {
        /// <summary>
        /// Schema validation errors are ignored
        /// </summary>
        Ignore,
        
        /// <summary>
        /// Schema validation errors are logged as warnings
        /// </summary>
        Warning,
        
        /// <summary>
        /// Schema validation errors are treated as errors
        /// </summary>
        Error
    }
}
