using System.Collections.Generic;
using System.Linq;
using Microsoft.ApplicationInspector.RulesEngine.Schema;
using Microsoft.CST.OAT;

namespace Microsoft.ApplicationInspector.RulesEngine;

public class RuleStatus
{
    public string? RulesId { get; set; }
    public string? RulesName { get; set; }
    public bool Verified => !Errors.Any() && !OatIssues.Any() && !SchemaValidationErrors.Any();
    public IEnumerable<string> Errors { get; set; } = Enumerable.Empty<string>();
    public IEnumerable<Violation> OatIssues { get; set; } = Enumerable.Empty<Violation>();
    public bool HasPositiveSelfTests { get; set; }
    public bool HasNegativeSelfTests { get; set; }
    internal Rule? Rule { get; set; }
    
    /// <summary>
    /// Schema validation errors for this rule
    /// </summary>
    public IEnumerable<SchemaValidationError> SchemaValidationErrors { get; set; } = Enumerable.Empty<SchemaValidationError>();
    
    /// <summary>
    /// Whether the rule passed schema validation
    /// </summary>
    public bool PassedSchemaValidation { get; set; } = true;
}