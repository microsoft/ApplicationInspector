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
    /// The language specifications to use
    /// </summary>
    public Languages LanguageSpecs { get; set; } = new Languages();

    /// <summary>
    /// By default rules must have unique IDs, this disables that validation check
    /// </summary>
    public bool DisableRequireUniqueIds { get; set; }
}