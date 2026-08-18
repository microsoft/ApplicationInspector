using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.CST.RecursiveExtractor;
using Xunit;

namespace AppInspector.Tests.RuleProcessor;

/// <summary>
///     The detections that the fixed pattern-OR-conditions-AND shape could not express.
/// </summary>
[ExcludeFromCodeCoverage]
public class RuleExpressionBehaviourTests
{
    /// <summary>
    ///     One condition guards one pattern: `(curl AND NOT tls13) OR wget`.
    /// </summary>
    private const string perPatternCondition = @"[
    {
        ""id"": ""SA500001"",
        ""name"": ""Testing.Rules.PerPatternCondition"",
        ""tags"": [ ""Testing.Rules.PerPatternCondition"" ],
        ""severity"": ""Critical"",
        ""description"": ""curl is excused by an explicit tls1.3 flag, wget is not"",
        ""expression"": ""(curl AND NOT tls13) OR wget"",
        ""patterns"": [
            { ""pattern"": ""curl"", ""type"": ""substring"", ""label"": ""curl"", ""scopes"": [ ""code"" ] },
            { ""pattern"": ""wget"", ""type"": ""substring"", ""label"": ""wget"", ""scopes"": [ ""code"" ] }
        ],
        ""conditions"": [
            {
                ""pattern"": { ""pattern"": ""--tlsv1.3"", ""type"": ""substring"", ""scopes"": [ ""code"" ] },
                ""search_in"": ""same-line"",
                ""label"": ""tls13""
            }
        ]
    }
]";

    /// <summary>
    ///     Condition disjunction: `p AND (near OR sameline)`.
    /// </summary>
    private const string conditionDisjunction = @"[
    {
        ""id"": ""SA500002"",
        ""name"": ""Testing.Rules.ConditionDisjunction"",
        ""tags"": [ ""Testing.Rules.ConditionDisjunction"" ],
        ""severity"": ""Critical"",
        ""description"": ""either context is enough"",
        ""expression"": ""p AND (near OR sameline)"",
        ""patterns"": [
            { ""pattern"": ""deserialize"", ""type"": ""substring"", ""label"": ""p"", ""scopes"": [ ""code"" ] }
        ],
        ""conditions"": [
            {
                ""pattern"": { ""pattern"": ""TypeNameHandling"", ""type"": ""substring"", ""scopes"": [ ""code"" ] },
                ""search_in"": ""finding-region(-3, 0)"",
                ""label"": ""near""
            },
            {
                ""pattern"": { ""pattern"": ""JsonSerializerSettings"", ""type"": ""substring"", ""scopes"": [ ""code"" ] },
                ""search_in"": ""same-line"",
                ""label"": ""sameline""
            }
        ]
    }
]";

    /// <summary>
    ///     Negated conjunction: `p AND NOT (secure AND httponly)` fires when any required flag is missing.
    /// </summary>
    private const string negatedConjunction = @"[
    {
        ""id"": ""SA500003"",
        ""name"": ""Testing.Rules.NegatedConjunction"",
        ""tags"": [ ""Testing.Rules.NegatedConjunction"" ],
        ""severity"": ""Critical"",
        ""description"": ""cookie must set both flags"",
        ""expression"": ""p AND NOT (secure AND httponly)"",
        ""patterns"": [
            { ""pattern"": ""Set-Cookie"", ""type"": ""substring"", ""label"": ""p"", ""scopes"": [ ""code"" ] }
        ],
        ""conditions"": [
            {
                ""pattern"": { ""pattern"": ""Secure"", ""type"": ""substring"", ""scopes"": [ ""code"" ] },
                ""search_in"": ""same-line"",
                ""label"": ""secure""
            },
            {
                ""pattern"": { ""pattern"": ""HttpOnly"", ""type"": ""substring"", ""scopes"": [ ""code"" ] },
                ""search_in"": ""same-line"",
                ""label"": ""httponly""
            }
        ]
    }
]";

    /// <summary>
    ///     Exactly one of two mutually exclusive settings: `p AND (a XOR b)`.
    /// </summary>
    private const string exclusiveOr = @"[
    {
        ""id"": ""SA500004"",
        ""name"": ""Testing.Rules.ExclusiveOr"",
        ""tags"": [ ""Testing.Rules.ExclusiveOr"" ],
        ""severity"": ""Critical"",
        ""description"": ""exactly one mode may be set"",
        ""expression"": ""p AND (a XOR b)"",
        ""patterns"": [
            { ""pattern"": ""configure"", ""type"": ""substring"", ""label"": ""p"", ""scopes"": [ ""code"" ] }
        ],
        ""conditions"": [
            {
                ""pattern"": { ""pattern"": ""modeA"", ""type"": ""substring"", ""scopes"": [ ""code"" ] },
                ""search_in"": ""same-line"",
                ""label"": ""a""
            },
            {
                ""pattern"": { ""pattern"": ""modeB"", ""type"": ""substring"", ""scopes"": [ ""code"" ] },
                ""search_in"": ""same-line"",
                ""label"": ""b""
            }
        ]
    }
]";

    /// <summary>
    ///     The same per-pattern scoping without an expression, by declaring the condition on the pattern it
    ///     guards.
    /// </summary>
    private const string scopedConditionNoExpression = @"[
    {
        ""id"": ""SA500005"",
        ""name"": ""Testing.Rules.ScopedCondition"",
        ""tags"": [ ""Testing.Rules.ScopedCondition"" ],
        ""severity"": ""Critical"",
        ""description"": ""condition guards only the curl pattern"",
        ""patterns"": [
            {
                ""pattern"": ""curl"", ""type"": ""substring"", ""label"": ""curl"", ""scopes"": [ ""code"" ],
                ""conditions"": [
                    {
                        ""pattern"": { ""pattern"": ""--tlsv1.3"", ""type"": ""substring"", ""scopes"": [ ""code"" ] },
                        ""search_in"": ""same-line"",
                        ""negate_finding"": true
                    }
                ]
            },
            { ""pattern"": ""wget"", ""type"": ""substring"", ""label"": ""wget"", ""scopes"": [ ""code"" ] }
        ]
    }
]";

    /// <summary>
    ///     Two conditions guarded by an OR, each satisfied by a different finding. Both sub-expressions
    ///     succeed, so the engine propagates both condition captures, and intersecting them would demand a
    ///     finding that passed both and report nothing. This is the case that per-finding evaluation of the
    ///     expression exists for.
    /// </summary>
    private const string conditionsSatisfiedByDifferentFindings = @"[
    {
        ""id"": ""SA500006"",
        ""name"": ""Testing.Rules.SplitDisjunction"",
        ""tags"": [ ""Testing.Rules.SplitDisjunction"" ],
        ""severity"": ""Critical"",
        ""description"": ""either guard is enough, and each is met by a different finding"",
        ""expression"": ""p AND (g1 OR g2)"",
        ""patterns"": [
            { ""pattern"": ""deserialize"", ""type"": ""substring"", ""label"": ""p"", ""scopes"": [ ""code"" ] }
        ],
        ""conditions"": [
            { ""pattern"": { ""pattern"": ""GUARD1"", ""type"": ""substring"", ""scopes"": [ ""code"" ] },
              ""search_in"": ""same-line"", ""label"": ""g1"" },
            { ""pattern"": { ""pattern"": ""GUARD2"", ""type"": ""substring"", ""scopes"": [ ""code"" ] },
              ""search_in"": ""same-line"", ""label"": ""g2"" }
        ]
    }
]";

    private readonly Microsoft.ApplicationInspector.RulesEngine.Languages _languages = new();

    private string[] MatchedPatterns(string ruleJson, string content)
    {
        RuleSet rules = new();
        rules.AddString(ruleJson, "TestRules");
        Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor =
            new(rules, new RuleProcessorOptions { Parallel = false });

        Assert.True(_languages.FromFileNameOut("test.c", out var info));

        return processor.AnalyzeFile(content, new FileEntry("test.c", new MemoryStream()), info)
            .Select(x => x.MatchingPattern?.Pattern ?? string.Empty).OrderBy(x => x).ToArray();
    }

    [Fact]
    public void PerPatternCondition_GuardedPatternIsExcused()
    {
        Assert.Equal(new[] { "curl" }, MatchedPatterns(perPatternCondition, "curl http://x\n"));
        Assert.Empty(MatchedPatterns(perPatternCondition, "curl --tlsv1.3 http://x\n"));
    }

    /// <summary>
    ///     The case the historical shape could not express: the unguarded pattern must still report even
    ///     though the guarded pattern was excused on the same file.
    /// </summary>
    [Fact]
    public void PerPatternCondition_UnguardedPatternStillReports()
    {
        Assert.Equal(new[] { "wget" }, MatchedPatterns(perPatternCondition, "curl --tlsv1.3 http://x\nwget http://y\n"));
    }

    [Fact]
    public void ConditionDisjunction_EitherContextSuffices()
    {
        Assert.Equal(new[] { "deserialize" },
            MatchedPatterns(conditionDisjunction, "TypeNameHandling.All\nvar x = deserialize(y)\n"));
        Assert.Equal(new[] { "deserialize" },
            MatchedPatterns(conditionDisjunction, "var x = deserialize(new JsonSerializerSettings())\n"));
        Assert.Empty(MatchedPatterns(conditionDisjunction, "var x = deserialize(y)\n"));
    }

    /// <summary>
    ///     Partially hardened code is the common real world defect and is exactly what a conjunction of
    ///     negated conditions cannot detect.
    /// </summary>
    [Fact]
    public void NegatedConjunction_FiresWhenOnlyOneRequirementIsMet()
    {
        Assert.Equal(new[] { "Set-Cookie" }, MatchedPatterns(negatedConjunction, "Set-Cookie: a=b; Secure\n"));
        Assert.Equal(new[] { "Set-Cookie" }, MatchedPatterns(negatedConjunction, "Set-Cookie: a=b; HttpOnly\n"));
        Assert.Equal(new[] { "Set-Cookie" }, MatchedPatterns(negatedConjunction, "Set-Cookie: a=b\n"));
        Assert.Empty(MatchedPatterns(negatedConjunction, "Set-Cookie: a=b; Secure; HttpOnly\n"));
    }

    [Fact]
    public void ExclusiveOr_RequiresExactlyOne()
    {
        Assert.Equal(new[] { "configure" }, MatchedPatterns(exclusiveOr, "configure modeA\n"));
        Assert.Equal(new[] { "configure" }, MatchedPatterns(exclusiveOr, "configure modeB\n"));
        Assert.Empty(MatchedPatterns(exclusiveOr, "configure modeA modeB\n"));
        Assert.Empty(MatchedPatterns(exclusiveOr, "configure\n"));
    }

    [Fact]
    public void PatternLevelCondition_ScopesConditionWithoutAnExpression()
    {
        Assert.Equal(new[] { "curl" }, MatchedPatterns(scopedConditionNoExpression, "curl http://x\n"));
        Assert.Empty(MatchedPatterns(scopedConditionNoExpression, "curl --tlsv1.3 http://x\n"));

        // wget is not guarded by the condition, so it reports even on a line the condition would exclude.
        Assert.Equal(new[] { "wget" },
            MatchedPatterns(scopedConditionNoExpression, "curl --tlsv1.3 http://x\nwget http://y\n"));
    }

    /// <summary>
    ///     Both findings satisfy the expression, each through a different half of the disjunction, so both
    ///     must be reported. Intersecting the condition captures instead reports neither.
    /// </summary>
    [Fact]
    public void ConditionsSatisfiedByDifferentFindings_ReportBoth()
    {
        RuleSet rules = new();
        rules.AddString(conditionsSatisfiedByDifferentFindings, "TestRules");
        Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor =
            new(rules, new RuleProcessorOptions { Parallel = false });

        Assert.True(_languages.FromFileNameOut("test.c", out var info));

        var matches = processor.AnalyzeFile("deserialize GUARD1\ndeserialize GUARD2\n",
            new FileEntry("test.c", new MemoryStream()), info);

        Assert.Equal(new[] { 1, 2 }, matches.Select(x => x.StartLocationLine).OrderBy(x => x));
    }

    /// <summary>
    ///     A NOT judged across the whole file is satisfied by any one compliant finding, which would let a single
    ///     hardened call site hide every vulnerable one beside it. The negation has to be judged per finding.
    /// </summary>
    private const string negatedCondition = @"[
    {
        ""id"": ""SA500005"",
        ""name"": ""Testing.Rules.NegatedCondition"",
        ""tags"": [ ""Testing.Rules.NegatedCondition"" ],
        ""severity"": ""Critical"",
        ""description"": ""a cookie without the Secure flag"",
        ""expression"": ""cookie AND NOT secure"",
        ""patterns"": [
            { ""pattern"": ""setcookie"", ""type"": ""substring"", ""label"": ""cookie"", ""scopes"": [ ""code"" ] }
        ],
        ""conditions"": [
            { ""pattern"": { ""pattern"": ""Secure"", ""type"": ""substring"", ""scopes"": [ ""code"" ] },
              ""search_in"": ""same-line"", ""label"": ""secure"" }
        ]
    }
]";

    [Theory]
    [InlineData("setcookie a\n", 1)]
    [InlineData("setcookie a Secure\n", 0)]
    [InlineData("setcookie a Secure\nsetcookie b\n", 1)]
    [InlineData("setcookie a\nsetcookie b\n", 2)]
    public void NegationIsJudgedPerFinding_NotAcrossTheFile(string content, int expected)
    {
        Assert.Equal(expected, MatchedPatterns(negatedCondition, content).Length);
    }

    /// <summary>
    ///     The worked example from the rule authoring documentation: fire when either flag is missing, including
    ///     on a file that also contains a fully hardened cookie.
    /// </summary>
    private const string partiallyHardened = @"[
    {
        ""id"": ""SA500008"",
        ""name"": ""Testing.Rules.PartiallyHardened"",
        ""tags"": [ ""Testing.Rules.PartiallyHardened"" ],
        ""severity"": ""Critical"",
        ""description"": ""a cookie missing either hardening flag"",
        ""expression"": ""cookie AND NOT (secure AND httponly)"",
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

    [Theory]
    [InlineData("setcookie a Secure HttpOnly\n", 0)]
    [InlineData("setcookie a Secure\n", 1)]
    [InlineData("setcookie a HttpOnly\n", 1)]
    [InlineData("setcookie a\n", 1)]
    [InlineData("setcookie a Secure HttpOnly\nsetcookie b Secure\n", 1)]
    public void PartiallyHardenedFinding_IsReportedBesideAHardenedOne(string content, int expected)
    {
        Assert.Equal(expected, MatchedPatterns(partiallyHardened, content).Length);
    }

    /// <summary>
    ///     Regex patterns carry the pattern they belong to on the clause rather than in the label, so an author
    ///     supplied label must survive on them exactly as it does on string patterns.
    /// </summary>
    [Theory]
    [InlineData("regex")]
    [InlineData("regexword")]
    public void AuthorSuppliedLabel_IsHonouredOnRegexPatterns(string patternType)
    {
        var ruleJson = $@"[
    {{
        ""id"": ""SA500006"",
        ""name"": ""Testing.Rules.RegexLabel"",
        ""tags"": [ ""Testing.Rules.RegexLabel"" ],
        ""severity"": ""Critical"",
        ""description"": ""labelled regex patterns"",
        ""expression"": ""alpha OR beta"",
        ""patterns"": [
            {{ ""pattern"": ""alpha"", ""type"": ""{patternType}"", ""label"": ""alpha"", ""scopes"": [ ""code"" ] }},
            {{ ""pattern"": ""beta"", ""type"": ""{patternType}"", ""label"": ""beta"", ""scopes"": [ ""code"" ] }}
        ]
    }}
]";

        Assert.Equal(new[] { "alpha", "beta" }, MatchedPatterns(ruleJson, "alpha here\nbeta there\n"));
    }

    /// <summary>
    ///     A condition declared on a pattern is labelled in the same namespace as a rule level one, so naming it
    ///     must work the same way in an expression.
    /// </summary>
    [Fact]
    public void AuthorSuppliedLabel_IsHonouredOnPatternLevelConditions()
    {
        const string ruleJson = @"[
    {
        ""id"": ""SA500007"",
        ""name"": ""Testing.Rules.PatternConditionLabel"",
        ""tags"": [ ""Testing.Rules.PatternConditionLabel"" ],
        ""severity"": ""Critical"",
        ""description"": ""labelled pattern level condition"",
        ""expression"": ""p AND NOT g"",
        ""patterns"": [
            { ""pattern"": ""curl"", ""type"": ""substring"", ""label"": ""p"", ""scopes"": [ ""code"" ],
              ""conditions"": [
                { ""pattern"": { ""pattern"": ""--tlsv1.3"", ""type"": ""substring"", ""scopes"": [ ""code"" ] },
                  ""search_in"": ""same-line"", ""label"": ""g"" }
              ] }
        ]
    }
]";

        Assert.Equal(new[] { "curl" }, MatchedPatterns(ruleJson, "curl http://x\n"));
        Assert.Empty(MatchedPatterns(ruleJson, "curl --tlsv1.3 http://x\n"));
    }
}
