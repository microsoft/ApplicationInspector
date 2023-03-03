using System.Collections.Generic;
using System.Linq;

namespace Microsoft.ApplicationInspector.RulesEngine;

public class RulesVerifierResult
{
    public RulesVerifierResult(IList<RuleStatus> ruleStatuses, AbstractRuleSet compiledRuleSets)
    {
        RuleStatuses = ruleStatuses;
        CompiledRuleSet = compiledRuleSets;
    }

    public IList<RuleStatus> RuleStatuses { get; }
    public AbstractRuleSet CompiledRuleSet { get; }
    public bool Verified => RuleStatuses.All(x => x.Verified);
}