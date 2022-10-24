using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Sarif;
using System.Linq;
using System.Text;
using ColorCode;

namespace WinSarifViewerCompanionLibrary
{
    public class SarifLoader
    {
        string _sarifFilePath;
        SarifLog _sarifLog;
        CodeColorizer _colorizer;
        Dictionary<string, string> tagsToRulesMapping;

        public SarifLoader(string sarifFilePath)
        {
            _sarifFilePath = sarifFilePath;
            _sarifLog = LoadFileDefferedInternal();
            _colorizer = new CodeColorizer();
            tagsToRulesMapping = new Dictionary<string, string>();
        }

        SarifLog LoadFileDefferedInternal()
        {
            var sarifLog = SarifLog.LoadDeferred(_sarifFilePath);
            return sarifLog;
        }


        public string[] GetRuns()
        {
            return _sarifLog.Runs.Select((run) => run.Tool.Driver.Name).ToArray();
        }

        public string[] GetRules(string tool)
        {
            var run = SelectRun(tool);
            if (run != default)
            {
                return run.Tool.Driver.Rules.Select((rule) => rule.Name).ToArray();
            }

            return default;
        }

        public string[] GetResultKinds()
        {
            return Enum.GetNames(typeof(ResultKind));
        }

        public string[] GetFailureLevels()
        {
            return Enum.GetNames(typeof(FailureLevel));
        }

        public int GetResultCount(string tool)
        {
            var run = SelectRun(tool);
            return run.Results.Count;
        }

        public IList<Result> GetResults(string tool)
        {
            var run = SelectRun(tool);
            return run.Results;
        }

        public List<string> GetRuleIdsFromNames(string tool, List<string> ruleNames)
        {
            var run = SelectRun(tool);

            if (run != default)
            {
                var ruleIds = run.Tool.Driver.Rules.Where((ruleObject) => ruleNames.Contains(ruleObject.Name)).Select((ruleObject) => ruleObject.Id).ToList();
                return ruleIds;
            }
            return default;
        }

        public List<Result> GetFilteredResults(string tool, List<string> rules, List<string> types, List<string> severities, List<string> tags)
        {
            IEnumerable<Result> filteredResults = default;

            var run = SelectRun(tool);
            run.SetRunOnResults();

            var results = run.Results;
            var ruleIds = GetRuleIdsFromNames(tool, rules);

            filteredResults = results;
            if(rules.Count > 0)
            {
                filteredResults = filteredResults.Where((result) => ruleIds.Contains(result.Rule.Id));
            }

            if(types.Count > 0)
            {
                filteredResults = filteredResults.Where((result) => types.Contains(result.Kind.ToString()));
            }

            if(severities.Count > 0)
            {
                filteredResults = filteredResults.Where((result) => severities.Contains(result.Level.ToString()));
            }

            if (tags.Count > 0)
            {
                var allRules = run.Tool.Driver.Rules;
                List<string> filteredRuleIds = new List<string>();
                foreach (var tag in tags)
                {
                    // find all the rules with these tags first
                    filteredRuleIds.AddRange(tagsToRulesMapping.Where((entry) => entry.Key == tag).Select(x => x.Value));
                }
                filteredResults = filteredResults.Where((result) => filteredRuleIds.Contains(result.Rule.Id));
            }

            return filteredResults.ToList();
        }

        public StringBuilder GenerateHTMLFromResults(string tool, List<Result> results)
        {
            StringBuilder html = new StringBuilder(@"<!DOCTYPE html>
    <html lang = \'en\'>
    <head>
        <title>Style Injection Example</title>
        <meta charset='utf-8'>
        <meta name='viewport' content='width=device-width, initial-scale=1'>    
<link rel='stylesheet' href='https://stackpath.bootstrapcdn.com/bootstrap/4.3.1/css/bootstrap.min.css' integrity='sha384-ggOyR0iXCbMQv3Xipma34MD+dH/1fQ784/j6cY/iJTQUOhcWr7x9JvoRxT2MZw1T' crossorigin='anonymous'>
    </head>
    <body>");

            var run = SelectRun(tool);
            if (results.Count > 0)
            {
                foreach (var result in results)
                {
                    var rule = run.Tool.Driver.Rules.Where((item) => item.Id.ToString() == result.Rule.Id).FirstOrDefault();
                    result.Run = run;
                    var language = result.Locations[0].PhysicalLocation?.Region?.SourceLanguage;// ?? run.try("semmle.sourceLanguage");
                    var fileName = result.Locations[0].PhysicalLocation?.ArtifactLocation.Uri.OriginalString;
                    int? startLine = result.Locations[0].PhysicalLocation?.Region?.StartLine;
                    int? endLine = result.Locations[0].PhysicalLocation?.Region?.EndLine;
                    var codeSnippet = result.Locations[0].PhysicalLocation?.ContextRegion?.Snippet?.Text ?? result.Locations[0].PhysicalLocation?.Region?.Snippet?.Text;
                    int? snippetStartLine = result.Locations[0].PhysicalLocation?.ContextRegion?.StartLine;

                    html.Append("<div class='card'><div class='card-body'>");
                    html.Append($"<div class='alert alert-info' role='alert'><b>{fileName}</b> at line number <b>{startLine}</b></div>");
                    html.Append($"<div class='alert alert-primary' role='alert'><h3>Rule: {result.RuleId}: {rule.Name}</h3>{rule.FullDescription?.Text}</div>");
                    html.Append($"<div class='alert alert-secondary' role='alert'>Tags: <b>{string.Join(",", rule.Tags)}</b></div>");

                    var codeSnippetLines = codeSnippet.Split('\n');
                    string numerizedSnippet = string.Join("\n",  codeSnippetLines.Select((line) =>  $"{snippetStartLine++}: {line}"));
                    html.Append(_colorizer.Colorize(numerizedSnippet, Languages.CSharp).Trim());

                    html.Append("</div></div>");
                }
            }

            return html.Append("</body></html>");
        }

        public string[] GetTags(string tool)
        {
            var run = SelectRun(tool);

            List<string> tagsFromRules = new List<string>();
            // if not, parse the results
            foreach (var rule in run.Tool.Driver.Rules)
            {
                tagsFromRules.AddRange(rule.Tags);
                foreach (var tag in rule.Tags)
                {
                    if (!tagsToRulesMapping.Keys.Contains(tag))
                    {
                        tagsToRulesMapping.Add(tag, rule.Id);
                    }
                }
            }

            return tagsFromRules.Distinct().ToArray();
        }

        Run SelectRun(string tool)
        {
            return _sarifLog.Runs.Where((run) => run.Tool.Driver.Name == tool).FirstOrDefault(); // may fail if both runs by the same tool
        }
    }
}
