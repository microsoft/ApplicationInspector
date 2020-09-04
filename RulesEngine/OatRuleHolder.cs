namespace Microsoft.ApplicationInspector.RulesEngine
{
    public class ConvertedOatRule : CST.OAT.Rule
    {
        public ConvertedOatRule(string name, Rule rule): base (name)
        {
            AppInspectorRule = rule;
        }

        public Rule AppInspectorRule { get; }
    }
}
