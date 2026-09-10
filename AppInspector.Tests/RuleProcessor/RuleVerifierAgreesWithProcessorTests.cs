// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.CST.RecursiveExtractor;
using Xunit;

namespace AppInspector.Tests.RuleProcessor;

/// <summary>
///     A self-test is only worth anything if it asserts what a scan will actually report, so the verifier and
///     the analyzer have to reach the same verdict for the same content.
///     They did not for rules with an authored expression: such a rule hands the engine a plain disjunction of
///     its clauses, because the authored expression is applied per finding afterwards, so asking the engine
///     whether the rule matched returned true as soon as any single pattern or condition matched. That failed
///     correct rules on their `must-not-match` samples and passed rules that report nothing on their
///     `must-match` samples.
/// </summary>
[ExcludeFromCodeCoverage]
public class RuleVerifierAgreesWithProcessorTests
{
    /// <summary>
    ///     The shape reported from the field: a rule level condition ANDed with a pattern. The negative sample
    ///     contains the pattern but not the condition, so the expression is false and nothing is reported.
    /// </summary>
    private const string sameFileCondition = @"[
    {
        ""id"": ""SA600001"",
        ""name"": ""Testing.Verifier.SameFileCondition"",
        ""tags"": [ ""Testing.Verifier.SameFileCondition"" ],
        ""severity"": ""Critical"",
        ""description"": ""readObject on an ObjectInputStream"",
        ""applies_to"": [ ""c"" ],
        ""expression"": ""p AND ois"",
        ""must-match"": [],
        ""must-not-match"": [],
        ""patterns"": [
            { ""pattern"": ""readObject"", ""type"": ""substring"", ""label"": ""p"", ""scopes"": [ ""code"" ] }
        ],
        ""conditions"": [
            { ""pattern"": { ""pattern"": ""ObjectInputStream"", ""type"": ""substring"", ""scopes"": [ ""code"" ] },
              ""search_in"": ""same-file"", ""label"": ""ois"" }
        ]
    }
]";

    /// <summary>
    ///     The documented worked example, whose behaviour is pinned by
    ///     <see cref="RuleExpressionBehaviourTests.PerPatternCondition_GuardedPatternIsExcused" />.
    /// </summary>
    private const string perPatternCondition = @"[
    {
        ""id"": ""SA600002"",
        ""name"": ""Testing.Verifier.PerPatternCondition"",
        ""tags"": [ ""Testing.Verifier.PerPatternCondition"" ],
        ""severity"": ""Critical"",
        ""description"": ""curl is excused by an explicit tls1.3 flag, wget is not"",
        ""applies_to"": [ ""c"" ],
        ""expression"": ""(curl AND NOT tls13) OR wget"",
        ""must-match"": [],
        ""must-not-match"": [],
        ""patterns"": [
            { ""pattern"": ""curl"", ""type"": ""substring"", ""label"": ""curl"", ""scopes"": [ ""code"" ] },
            { ""pattern"": ""wget"", ""type"": ""substring"", ""label"": ""wget"", ""scopes"": [ ""code"" ] }
        ],
        ""conditions"": [
            { ""pattern"": { ""pattern"": ""--tlsv1.3"", ""type"": ""substring"", ""scopes"": [ ""code"" ] },
              ""search_in"": ""same-line"", ""label"": ""tls13"" }
        ]
    }
]";

    /// <summary>
    ///     Two conditions under an OR, so neither on its own is required.
    /// </summary>
    private const string conditionDisjunction = @"[
    {
        ""id"": ""SA600003"",
        ""name"": ""Testing.Verifier.ConditionDisjunction"",
        ""tags"": [ ""Testing.Verifier.ConditionDisjunction"" ],
        ""severity"": ""Critical"",
        ""description"": ""either context is enough"",
        ""applies_to"": [ ""c"" ],
        ""expression"": ""p AND (near OR sameline)"",
        ""must-match"": [],
        ""must-not-match"": [],
        ""patterns"": [
            { ""pattern"": ""deserialize"", ""type"": ""substring"", ""label"": ""p"", ""scopes"": [ ""code"" ] }
        ],
        ""conditions"": [
            { ""pattern"": { ""pattern"": ""TypeNameHandling"", ""type"": ""substring"", ""scopes"": [ ""code"" ] },
              ""search_in"": ""finding-region(-3, 0)"", ""label"": ""near"" },
            { ""pattern"": { ""pattern"": ""JsonSerializerSettings"", ""type"": ""substring"", ""scopes"": [ ""code"" ] },
              ""search_in"": ""same-line"", ""label"": ""sameline"" }
        ]
    }
]";

    /// <summary>
    ///     A negated conjunction of conditions, which fires when either requirement is missing.
    /// </summary>
    private const string negatedConjunction = @"[
    {
        ""id"": ""SA600004"",
        ""name"": ""Testing.Verifier.NegatedConjunction"",
        ""tags"": [ ""Testing.Verifier.NegatedConjunction"" ],
        ""severity"": ""Critical"",
        ""description"": ""a cookie missing either hardening flag"",
        ""applies_to"": [ ""c"" ],
        ""expression"": ""cookie AND NOT (secure AND httponly)"",
        ""must-match"": [],
        ""must-not-match"": [],
        ""patterns"": [
            { ""pattern"": ""setcookie"", ""type"": ""substring"", ""label"": ""cookie"", ""scopes"": [ ""code"" ] }
        ],
        ""conditions"": [
            { ""pattern"": { ""pattern"": ""Secure"", ""type"": ""substring"", ""scopes"": [ ""code"" ] },
              ""search_in"": ""same-line"", ""label"": ""secure"" },
            { ""pattern"": { ""pattern"": ""HttpOnly"", ""type"": ""substring"", ""scopes"": [ ""code"" ] },
              ""search_in"": ""same-line"", ""label"": ""httponly"" }
        ]
    }
]";

    /// <summary>
    ///     Exactly one of two conditions.
    /// </summary>
    private const string exclusiveOr = @"[
    {
        ""id"": ""SA600005"",
        ""name"": ""Testing.Verifier.ExclusiveOr"",
        ""tags"": [ ""Testing.Verifier.ExclusiveOr"" ],
        ""severity"": ""Critical"",
        ""description"": ""exactly one mode may be set"",
        ""applies_to"": [ ""c"" ],
        ""expression"": ""p AND (a XOR b)"",
        ""must-match"": [],
        ""must-not-match"": [],
        ""patterns"": [
            { ""pattern"": ""configure"", ""type"": ""substring"", ""label"": ""p"", ""scopes"": [ ""code"" ] }
        ],
        ""conditions"": [
            { ""pattern"": { ""pattern"": ""modeA"", ""type"": ""substring"", ""scopes"": [ ""code"" ] },
              ""search_in"": ""same-line"", ""label"": ""a"" },
            { ""pattern"": { ""pattern"": ""modeB"", ""type"": ""substring"", ""scopes"": [ ""code"" ] },
              ""search_in"": ""same-line"", ""label"": ""b"" }
        ]
    }
]";

    /// <summary>
    ///     An expression over pattern labels alone, with no conditions. A finding comes from a single pattern, so
    ///     a sibling pattern label is false at every finding and this reports exactly what `alpha` alone would.
    ///     The engine still sees a disjunction of both patterns, so this diverged too.
    /// </summary>
    private const string patternsOnlyNegation = @"[
    {
        ""id"": ""SA600006"",
        ""name"": ""Testing.Verifier.PatternsOnlyNegation"",
        ""tags"": [ ""Testing.Verifier.PatternsOnlyNegation"" ],
        ""severity"": ""Critical"",
        ""description"": ""alpha, expressed with a sibling pattern label"",
        ""applies_to"": [ ""c"" ],
        ""expression"": ""a AND NOT b"",
        ""must-match"": [],
        ""must-not-match"": [],
        ""patterns"": [
            { ""pattern"": ""alpha"", ""type"": ""substring"", ""label"": ""a"", ""scopes"": [ ""code"" ] },
            { ""pattern"": ""beta"", ""type"": ""substring"", ""label"": ""b"", ""scopes"": [ ""code"" ] }
        ]
    }
]";

    /// <summary>
    ///     The legacy shape, which never diverged and must not start doing so.
    /// </summary>
    private const string legacyNegateFinding = @"[
    {
        ""id"": ""SA600007"",
        ""name"": ""Testing.Verifier.LegacyNegateFinding"",
        ""tags"": [ ""Testing.Verifier.LegacyNegateFinding"" ],
        ""severity"": ""Critical"",
        ""description"": ""curl without an explicit tls1.3 flag"",
        ""applies_to"": [ ""c"" ],
        ""must-match"": [],
        ""must-not-match"": [],
        ""patterns"": [
            { ""pattern"": ""curl"", ""type"": ""substring"", ""scopes"": [ ""code"" ] }
        ],
        ""conditions"": [
            { ""pattern"": { ""pattern"": ""--tlsv1.3"", ""type"": ""substring"", ""scopes"": [ ""code"" ] },
              ""search_in"": ""same-line"", ""negate_finding"": true }
        ]
    }
]";

    private static readonly Dictionary<string, string> Rules = new()
    {
        { nameof(sameFileCondition), sameFileCondition },
        { nameof(perPatternCondition), perPatternCondition },
        { nameof(conditionDisjunction), conditionDisjunction },
        { nameof(negatedConjunction), negatedConjunction },
        { nameof(exclusiveOr), exclusiveOr },
        { nameof(patternsOnlyNegation), patternsOnlyNegation },
        { nameof(legacyNegateFinding), legacyNegateFinding }
    };

    private readonly Microsoft.ApplicationInspector.RulesEngine.Languages _languages = new();

    /// <summary>
    ///     Every case is stated as the verdict a scan produces, so the agreement assertions below cannot be
    ///     satisfied by the verifier and the analyzer regressing together.
    /// </summary>
    public static IEnumerable<object[]> Cases()
    {
        // The reported repro. The pattern is present but the condition is not.
        yield return new object[] { nameof(sameFileCondition), "ObjectInputStream i;\ni.readObject();\n", true };
        yield return new object[] { nameof(sameFileCondition), "cache.readObject();\n", false };

        // The documented worked example.
        yield return new object[] { nameof(perPatternCondition), "curl http://x\n", true };
        yield return new object[] { nameof(perPatternCondition), "curl --tlsv1.3 http://x\n", false };
        yield return new object[] { nameof(perPatternCondition), "curl --tlsv1.3 http://x\nwget http://y\n", true };

        yield return new object[] { nameof(conditionDisjunction), "TypeNameHandling.All\nvar x = deserialize(y)\n", true };
        yield return new object[]
            { nameof(conditionDisjunction), "var x = deserialize(new JsonSerializerSettings())\n", true };
        yield return new object[] { nameof(conditionDisjunction), "var x = deserialize(y)\n", false };

        yield return new object[] { nameof(negatedConjunction), "setcookie a Secure\n", true };
        yield return new object[] { nameof(negatedConjunction), "setcookie a Secure HttpOnly\n", false };
        yield return new object[] { nameof(negatedConjunction), "setcookie a Secure HttpOnly\nsetcookie b Secure\n", true };

        yield return new object[] { nameof(exclusiveOr), "configure modeA\n", true };
        yield return new object[] { nameof(exclusiveOr), "configure modeA modeB\n", false };
        yield return new object[] { nameof(exclusiveOr), "configure\n", false };

        yield return new object[] { nameof(patternsOnlyNegation), "alpha x\n", true };
        yield return new object[] { nameof(patternsOnlyNegation), "beta x\n", false };

        yield return new object[] { nameof(legacyNegateFinding), "curl http://x\n", true };
        yield return new object[] { nameof(legacyNegateFinding), "curl --tlsv1.3 http://x\n", false };
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void VerifierReachesTheSameVerdictAsTheAnalyzer(string ruleKey, string content, bool expectedToReport)
    {
        var ruleJson = Rules[ruleKey];

        Assert.Equal(expectedToReport, AnalyzerReportsAFinding(ruleJson, content));

        // A must-match self-test asserts that a scan reports the sample, so it must pass exactly when it does.
        Assert.Equal(expectedToReport, VerifierAccepts(WithSelfTest(ruleJson, "must-match", content)));

        // A must-not-match self-test asserts the opposite.
        Assert.Equal(!expectedToReport, VerifierAccepts(WithSelfTest(ruleJson, "must-not-match", content)));
    }

    [Theory]
    [InlineData("ois AND p", "ObjectInputStream i;\ni.readObject();\n", true)]
    [InlineData("ois AND p", "cache.readObject();\n", false)]
    [InlineData("NOT ois AND p", "ObjectInputStream i;\ni.readObject();\n", false)]
    [InlineData("NOT ois AND p", "cache.readObject();\n", true)]
    public void ConditionBeforePattern_VerifierReachesTheSameVerdictAsTheAnalyzer(
        string expression, string content, bool expectedToReport)
    {
        var rules = JsonNode.Parse(sameFileCondition)!;
        rules[0]!["expression"] = expression;
        var ruleJson = rules.ToJsonString(new JsonSerializerOptions { WriteIndented = true });

        Assert.Equal(expectedToReport, AnalyzerReportsAFinding(ruleJson, content));
        Assert.Equal(expectedToReport, VerifierAccepts(WithSelfTest(ruleJson, "must-match", content)));
        Assert.Equal(!expectedToReport, VerifierAccepts(WithSelfTest(ruleJson, "must-not-match", content)));
    }

    /// <summary>
    ///     The reported failure, stated on its own so a regression names itself.
    /// </summary>
    [Fact]
    public void ExpressionWithConditions_DoesNotFailACorrectMustNotMatch()
    {
        var rule = WithSelfTest(sameFileCondition, "must-not-match", "cache.readObject();\n");

        Assert.False(AnalyzerReportsAFinding(sameFileCondition, "cache.readObject();\n"));
        Assert.True(VerifierAccepts(rule));
    }

    /// <summary>
    ///     The same defect in the other direction, which is quieter and worse: a rule that reports nothing for a
    ///     sample used to pass a must-match self-test naming it, because any one clause matching satisfied the
    ///     engine.
    /// </summary>
    [Fact]
    public void ExpressionWithConditions_DoesNotPassAMustMatchThatReportsNothing()
    {
        var rule = WithSelfTest(sameFileCondition, "must-match", "cache.readObject();\n");

        Assert.False(VerifierAccepts(rule));
    }

    private bool AnalyzerReportsAFinding(string ruleJson, string content)
    {
        RuleSet rules = new();
        rules.AddString(ruleJson, "TestRules");
        Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor =
            new(rules, new RuleProcessorOptions { Parallel = false });

        Assert.True(_languages.FromFileNameOut("test.c", out var info));

        return processor.AnalyzeFile(content, new FileEntry("test.c", new MemoryStream()), info).Any();
    }

    private static bool VerifierAccepts(string ruleJson)
    {
        RuleSet rules = new();
        rules.AddString(ruleJson, "TestRules");

        return new RulesVerifier(new RulesVerifierOptions()).Verify(rules).Verified;
    }

    /// <summary>
    ///     Fills in one of the empty self-test arrays the rule declares.
    /// </summary>
    private static string WithSelfTest(string ruleJson, string field, string sample)
    {
        var token = $"\"{field}\": []";
        Assert.Contains(token, ruleJson);

        return ruleJson.Replace(token, $"\"{field}\": [ {JsonSerializer.Serialize(sample)} ]");
    }
}
