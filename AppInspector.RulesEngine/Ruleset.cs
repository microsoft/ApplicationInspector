// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.


namespace Microsoft.ApplicationInspector.RulesEngine
{
    using Microsoft.Extensions.Logging;

    /// <summary>
    ///     Default class to use to store Application Inspector <see cref="Rule"/> objects.
    /// </summary>
    public class RuleSet : TypedRuleSet<Rule>
    {

        /// <summary>
        ///     Create a ruleset using the given (optional) logger.
        /// </summary>
        /// <param name="loggerFactory"></param>
        public RuleSet(ILoggerFactory? loggerFactory = null) : base(loggerFactory)
        {
        }
    }
}
