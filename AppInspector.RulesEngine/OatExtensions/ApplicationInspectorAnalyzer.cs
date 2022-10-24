using Microsoft.CST.OAT;
using Microsoft.Extensions.Logging;

namespace Microsoft.ApplicationInspector.RulesEngine.OatExtensions;

public class ApplicationInspectorAnalyzer : Analyzer
{
    public ApplicationInspectorAnalyzer(ILoggerFactory? loggerFactory = null, AnalyzerOptions? analyzerOptions = null) : base(analyzerOptions)
    {
        SetOperation(new WithinOperation(this, loggerFactory));
        SetOperation(new OatRegexWithIndexOperation(this, loggerFactory));
        SetOperation(new OatSubstringIndexOperation(this, loggerFactory));
    }
}