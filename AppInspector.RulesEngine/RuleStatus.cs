using System.Collections.Generic;
using System.Linq;
using Microsoft.CST.OAT;

namespace Microsoft.ApplicationInspector.RulesEngine;

public class RuleStatus
{
    public string? RulesId { get; set; }
    public string? RulesName { get; set; }
    public bool Verified => !Errors.Any() && !OatIssues.Any();
    public IEnumerable<string> Errors { get; set; } = Enumerable.Empty<string>();
    public IEnumerable<Violation> OatIssues { get; set; } = Enumerable.Empty<Violation>();
    public bool HasPositiveSelfTests { get; set; }
    public bool HasNegativeSelfTests { get; set; }
    internal Rule Rule { get; set; }
}