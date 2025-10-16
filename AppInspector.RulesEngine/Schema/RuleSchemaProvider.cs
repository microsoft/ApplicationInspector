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
        private const string SCHEMA_RESOURCE_NAME = "Microsoft.ApplicationInspector.RulesEngine.rule-schema-v1.json";

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

        /// <summary>
        /// Private constructor for factory methods
        /// </summary>
        private RuleSchemaProvider(JsonSchema schema)
        {
            _schema = schema;
        }

        /// <summary>
        /// Create a schema provider from schema content string
        /// </summary>
        /// <param name="schemaContent">The JSON schema content as a string</param>
        /// <returns>A new RuleSchemaProvider instance</returns>
        public static RuleSchemaProvider FromSchemaContent(string schemaContent)
        {
            if (string.IsNullOrEmpty(schemaContent))
            {
                throw new ArgumentException("Schema content cannot be null or empty", nameof(schemaContent));
            }
            
            var schema = LoadSchemaFromContent(schemaContent);
            return new RuleSchemaProvider(schema);
        }

        private JsonSchema LoadEmbeddedSchema()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(SCHEMA_RESOURCE_NAME)
                ?? throw new InvalidOperationException($"Could not find embedded schema resource: {SCHEMA_RESOURCE_NAME}");
            using var reader = new StreamReader(stream);
            var schemaJson = reader.ReadToEnd();
            return LoadSchemaFromContent(schemaJson);
        }

        private JsonSchema LoadSchemaFromFile(string path)
        {
            var schemaJson = File.ReadAllText(path);
            return LoadSchemaFromContent(schemaJson);
        }

        private static JsonSchema LoadSchemaFromContent(string schemaContent)
        {
            return JsonSchema.FromText(schemaContent);
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
            // Use a stack to avoid deep recursion and potential stack overflow
            var processingStack = new Stack<(EvaluationResults result, string path)>();
            processingStack.Push((result, currentPath));
            
            while (processingStack.Count > 0)
            {
                var (currentResult, currentResultPath) = processingStack.Pop();
                
                // Check if there are any details with errors
                if (currentResult.Details != null)
                {
                    foreach (var detail in currentResult.Details)
                    {
                        if (!detail.IsValid)
                        {
                            var path = detail.InstanceLocation?.ToString() ?? currentResultPath;
                            var schemaPath = detail.SchemaLocation?.ToString() ?? "";
                            
                            errors.Add(new SchemaValidationError
                            {
                                Message = $"Validation failed at schema location '{schemaPath}'",
                                Path = path,
                                ErrorType = "SchemaViolation"
                            });
                            
                            // Push nested details onto the stack for processing
                            if (detail.Details != null && detail.Details.Any())
                            {
                                processingStack.Push((detail, path));
                            }
                        }
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
            var json = JsonSerializer.Serialize(rules);
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
