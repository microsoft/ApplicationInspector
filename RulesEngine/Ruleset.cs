// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.


using Microsoft.ApplicationInspector.RulesEngine.OatExtensions;
using Newtonsoft.Json;

namespace Microsoft.ApplicationInspector.RulesEngine
{
    using Microsoft.CST.OAT;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    ///     Storage for rules
    /// </summary>
    public class RuleSet : TypedRuleSet<Rule>
    {
        /// <summary>
        ///     Creates instance of Ruleset
        /// </summary>
        public RuleSet(ILoggerFactory? loggerFactory = null) : base(loggerFactory)
        {
        }
    }
}
