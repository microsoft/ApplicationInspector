namespace Microsoft.ApplicationInspector.RulesEngine
{
    /// <summary>
    /// Wrapper for OAT based processing
    /// </summary>
    public class ConvertedOatRule : CST.OAT.Rule
    {
        public ConvertedOatRule(string name, Rule rule): base (name)
        {
            AppInspectorRule = rule;
        }

        /// <summary>
        /// Native Application Inspector Rule to preserve format and instance data
        /// </summary>
        public Rule AppInspectorRule { get; }
    }
}
