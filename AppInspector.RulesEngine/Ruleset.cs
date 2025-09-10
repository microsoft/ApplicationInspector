// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.


using Microsoft.ApplicationInspector.RulesEngine.Schema;
using Microsoft.Extensions.Logging;

namespace Microsoft.ApplicationInspector.RulesEngine;

/// <summary>
///     Default class to use to store Application Inspector <see cref="Rule" /> objects.
/// </summary>
public class RuleSet : TypedRuleSet<Rule>
{
    /// <summary>
    ///     Create a ruleset using the given (optional) logger and schema provider.
    /// </summary>
    /// <param name="loggerFactory"></param>
    /// <param name="schemaProvider"></param>
    public RuleSet(ILoggerFactory? loggerFactory = null, RuleSchemaProvider? schemaProvider = null) 
        : base(loggerFactory, schemaProvider)
    {    
    }
}