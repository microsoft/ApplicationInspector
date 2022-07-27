using System.Collections.Generic;
using System.Linq;

namespace Microsoft.ApplicationInspector.RulesEngine;

public class RulesVerifierResult
{
    public RulesVerifierResult(List<RuleStatus> ruleStatuses, AbstractRuleSet compiledRuleSets)
    {
        RuleStatuses = ruleStatuses;
        CompiledRuleSet = compiledRuleSets;
    }
    public List<RuleStatus> RuleStatuses { get; }
    public AbstractRuleSet CompiledRuleSet { get; }
    public  bool Verified => RuleStatuses.All(x => x.Verified);
}