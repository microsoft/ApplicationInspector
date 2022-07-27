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
