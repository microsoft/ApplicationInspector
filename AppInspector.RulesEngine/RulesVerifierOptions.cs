using Microsoft.CST.OAT;
using Microsoft.Extensions.Logging;

namespace Microsoft.ApplicationInspector.RulesEngine;

public class RulesVerifierOptions
{
    /// <summary>
    /// If desired you may provide the analyzer to use. An analyzer with AI defaults will be created to use for validation.
    /// </summary>
    public Analyzer? Analyzer { get; set; }
    /// <summary>
    /// To receive log messages, provide a LoggerFactory with your preferred configuration.
    /// </summary>
    public ILoggerFactory? LoggerFactory { get; set; }
    /// <summary>
    /// If true, the verifier will stop on the first issue and will not continue reporting issues.
    /// </summary>
    public bool FailFast { get; set; }
    public Languages LanguageSpecs { get; set; } = new Languages();
}