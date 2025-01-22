using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.ApplicationInspector.Commands;
using Microsoft.ApplicationInspector.Common;
using Microsoft.ApplicationInspector.Logging;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AppInspector.Tests.Commands;

// TODO: This does not intentionally try to make the OAT rule maker fail
// The OAT rules are being validated but there aren't test cases that intentionally try to break it.
[TestClass]
[ExcludeFromCodeCoverage]
public class TestVerifyRulesCmd
{
    private ILoggerFactory _factory = new NullLoggerFactory();
    private readonly LogOptions _logOptions = new();
    private string _validRulesPath = string.Empty;
    private string _ruleWithoutDescriptionPath = string.Empty;
    private string _invalidJsonValidRulePath = string.Empty;
    private string _validJsonInvalidRuleNoIdPath = string.Empty;
    private string _sameIdPath = string.Empty;
    private string _overriddenDependsOnTagMissingRulePath = string.Empty;
    private string _dependsOnTagMissingRulePath = string.Empty;
    private string _invalidFileRegexesPath = string.Empty;
    private string _knownLanguagesPath = string.Empty;
    private string _mustMatchRulePath = string.Empty;
    private string _mustMatchRuleFailPath = string.Empty;
    private string _mustNotMatchRulePath = string.Empty;
    private string _mustNotMatchRuleFailPath = string.Empty;

    [TestInitialize]
    public void InitOutput()
    {
        _factory = _logOptions.GetLoggerFactory();
        _validRulesPath = Path.Combine("TestData","TestVerifyRulesCmd","Rules", "ValidRules.json");
        _ruleWithoutDescriptionPath = Path.Combine("TestData","TestVerifyRulesCmd","Rules", "RuleWithoutDescription.json");
        _invalidJsonValidRulePath = Path.Combine("TestData","TestVerifyRulesCmd","Rules", "InvalidJsonValidRule.json");
        _validJsonInvalidRuleNoIdPath = Path.Combine("TestData","TestVerifyRulesCmd","Rules", "ValidJsonInvalidRuleNoId.json");
        _sameIdPath = Path.Combine("TestData","TestVerifyRulesCmd","Rules", "SameId.json");
        _overriddenDependsOnTagMissingRulePath = Path.Combine("TestData","TestVerifyRulesCmd","Rules", "OverriddenDependsOnTagMissingRule.json");
        _dependsOnTagMissingRulePath = Path.Combine("TestData","TestVerifyRulesCmd","Rules", "DependsOnTagMissingRule.json");
        _invalidFileRegexesPath = Path.Combine("TestData","TestVerifyRulesCmd","Rules", "InvalidFileRegexesRule.json");
        _knownLanguagesPath = Path.Combine("TestData","TestVerifyRulesCmd","Rules", "KnownLanguages.json");
        _mustMatchRulePath = Path.Combine("TestData","TestVerifyRulesCmd","Rules", "MustMatchRule.json");
        _mustMatchRuleFailPath = Path.Combine("TestData","TestVerifyRulesCmd","Rules", "MustMatchRuleFail.json");
        _mustNotMatchRulePath = Path.Combine("TestData","TestVerifyRulesCmd","Rules", "MustNotMatchRule.json");
        _mustNotMatchRuleFailPath = Path.Combine("TestData","TestVerifyRulesCmd","Rules", "MustNotMatchRuleFail.json");

    }

    /// <summary>
    ///     Ensure an exception is thrown if you don't specify any rules to verify
    /// </summary>
    [TestMethod]
    public void NoDefaultNoCustomRules()
    {
        Assert.ThrowsException<OpException>(() => new VerifyRulesCommand(new VerifyRulesOptions()));
    }
    
    [TestMethod]
    public void NoDescription()
    {
        VerifyRulesOptions options = new()
        {
            CustomRulesPath = _ruleWithoutDescriptionPath
        };

        VerifyRulesCommand command = new(options, _factory);
        var result = command.GetResult();
        Assert.AreEqual(1, result.Unverified.Count());
        Assert.AreEqual(VerifyRulesResult.ExitCode.NotVerified, result.ResultCode);
    }

    [TestMethod]
    public void CustomRules()
    {
        VerifyRulesOptions options = new()
        {
            CustomRulesPath = _validRulesPath
        };

        VerifyRulesCommand command = new(options, _factory);
        var result = command.GetResult();

        Assert.AreEqual(VerifyRulesResult.ExitCode.Verified, result.ResultCode);
    }

    [TestMethod]
    public void UnclosedJson()
    {
        VerifyRulesOptions options = new()
        {
            CustomRulesPath = _invalidJsonValidRulePath
        };

        VerifyRulesCommand command = new(options, _factory);
        var result = command.GetResult();
        Assert.AreEqual(VerifyRulesResult.ExitCode.CriticalError, result.ResultCode);
    }

    [TestMethod]
    public void NullId()
    {
        var set = new RuleSet();
        set.AddString(File.ReadAllText(_validJsonInvalidRuleNoIdPath), "NoIdTest");
        RulesVerifierOptions options = new()
        {
            LoggerFactory = _factory
        };
        var rulesVerifier = new RulesVerifier(options);
        Assert.IsFalse(rulesVerifier.Verify(set).Verified);
    }

    [TestMethod]
    public void DuplicateId()
    {
        VerifyRulesOptions options = new()
        {
            CustomRulesPath = _sameIdPath
        };

        VerifyRulesCommand command = new(options, _factory);
        var result = command.GetResult();
        Assert.AreEqual(VerifyRulesResult.ExitCode.NotVerified, result.ResultCode);
    }

    [TestMethod]
    public void OverriddenRuleMissingDependsOnTag()
    {
        VerifyRulesOptions options = new()
        {
            CustomRulesPath = _overriddenDependsOnTagMissingRulePath
        };

        VerifyRulesCommand command = new(options, _factory);
        var result = command.GetResult();
        Assert.AreEqual(VerifyRulesResult.ExitCode.NotVerified, result.ResultCode);
    }

    [TestMethod]
    public void MissingDependsOnTag()
    {
        VerifyRulesOptions options = new()
        {
            CustomRulesPath = _dependsOnTagMissingRulePath
        };

        VerifyRulesCommand command = new(options, _factory);
        var result = command.GetResult();
        Assert.AreEqual(VerifyRulesResult.ExitCode.NotVerified, result.ResultCode);
    }

    [TestMethod]
    public void DuplicateIdCheckDisabled()
    {
        VerifyRulesOptions options = new()
        {
            CustomRulesPath = _sameIdPath,
            DisableRequireUniqueIds = true
        };

        VerifyRulesCommand command = new(options, _factory);
        var result = command.GetResult();
        Assert.AreEqual(VerifyRulesResult.ExitCode.Verified, result.ResultCode);
    }

    [TestMethod]
    public void InvalidRegex()
    {
        VerifyRulesOptions options = new()
        {
            CustomRulesPath = _invalidFileRegexesPath
        };

        VerifyRulesCommand command = new(options, _factory);
        var result = command.GetResult();
        Assert.AreEqual(VerifyRulesResult.ExitCode.NotVerified, result.ResultCode);
    }

    [TestMethod]
    public void UnknownLanguage()
    {
        VerifyRulesOptions options = new()
        {
            CustomRulesPath = _knownLanguagesPath
        };

        VerifyRulesCommand command = new(options, _factory);
        var result = command.GetResult();
        Assert.AreEqual(VerifyRulesResult.ExitCode.NotVerified, result.ResultCode);
    }

    [TestMethod]
    public void MustMatch()
    {
        VerifyRulesOptions options = new()
        {
            CustomRulesPath = _mustMatchRulePath
        };

        VerifyRulesCommand command = new(options, _factory);
        var result = command.GetResult();
        Assert.AreEqual(VerifyRulesResult.ExitCode.Verified, result.ResultCode);
    }

    [TestMethod]
    public void MustMatchDetectIncorrect()
    {
        VerifyRulesOptions options = new()
        {
            CustomRulesPath = _mustMatchRuleFailPath
        };

        VerifyRulesCommand command = new(options, _factory);
        var result = command.GetResult();
        Assert.AreEqual(VerifyRulesResult.ExitCode.NotVerified, result.ResultCode);
    }

    [TestMethod]
    public void MustNotMatch()
    {
        VerifyRulesOptions options = new()
        {
            CustomRulesPath = _mustNotMatchRulePath
        };

        VerifyRulesCommand command = new(options, _factory);
        var result = command.GetResult();
        Assert.AreEqual(VerifyRulesResult.ExitCode.Verified, result.ResultCode);
    }

    [TestMethod]
    public void MustNotMatchDetectIncorrect()
    {
        VerifyRulesOptions options = new()
        {
            CustomRulesPath = _mustNotMatchRuleFailPath
        };

        VerifyRulesCommand command = new(options, _factory);
        var result = command.GetResult();
        Assert.AreEqual(VerifyRulesResult.ExitCode.NotVerified, result.ResultCode);
    }
}