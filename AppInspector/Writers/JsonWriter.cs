// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using RulesEngine;

namespace Microsoft.AppInspector.Writers
{
    /// <summary>
    /// Subset of MatchRecord and Issue properties for json output
    /// TODO: look at possible consolidation with MatchRecord...do we need Issue.Rule etc. which are not
    /// wanted for json output in their entirety
    /// </summary>
    [Serializable]
    public class MatchItems
    {
        public MatchItems(MatchRecord matchRecord)
        {
            FileName = matchRecord.Filename;
            SourceType = matchRecord.Language;
            StartLocationLine = matchRecord.Issue.StartLocation.Line;
            StartLocationColumn = matchRecord.Issue.StartLocation.Column;
            EndLocationLine = matchRecord.Issue.EndLocation.Line;
            EndLocationColumn = matchRecord.Issue.EndLocation.Column;
            BoundaryIndex = matchRecord.Issue.Boundary.Index;
            BoundaryLength = matchRecord.Issue.Boundary.Length;
            RuleId = matchRecord.Issue.Rule.Id;
            Severity = matchRecord.Issue.Rule.Severity.ToString();
            RuleName = matchRecord.Issue.Rule.Name;
            RuleDescription = matchRecord.Issue.Rule.Description;
            PatternConfidence = matchRecord.Issue.Confidence.ToString();
            PatternType = matchRecord.Issue.PatternMatch.PatternType.ToString();
            MatchingPattern = matchRecord.Issue.PatternMatch.Pattern;
            Sample = matchRecord.TextSample;
            Excerpt = matchRecord.Excerpt;
            Tags = matchRecord.Issue.Rule.Tags;
        }

        [JsonProperty(PropertyName = "fileName")]
        public string FileName { get; set; }
        [JsonProperty(PropertyName = "ruleId")]
        public string RuleId { get; set; }
        [JsonProperty(PropertyName = "ruleName")]
        public string RuleName { get; set; }
        [JsonProperty(PropertyName = "ruleDescription")]
        public string RuleDescription { get; set; }
        [JsonProperty(PropertyName = "pattern")]
        public string MatchingPattern { get; set; }
        [JsonProperty(PropertyName = "type")]
        public string PatternType { get; set; }
        [JsonProperty(PropertyName = "confidence")]
        public string PatternConfidence { get; set; }
        [JsonProperty(PropertyName = "severity")]
        public string Severity { get; set; }
        [JsonProperty(PropertyName = "tags")]
        public string[] Tags { get; set; }
        [JsonProperty(PropertyName = "sourceType")]
        public string SourceType { get; set; }
        [JsonProperty(PropertyName = "sample")]
        public string Sample { get; set; }
        [JsonProperty(PropertyName = "excerpt")]
        public string Excerpt { get; set; }
        [JsonProperty(PropertyName = "startLocationLine")]
        public int StartLocationLine { get; set; }
        [JsonProperty(PropertyName = "startLocationColumn")]
        public int StartLocationColumn { get; set; }
        [JsonProperty(PropertyName = "endLocationLine")]
        public int EndLocationLine { get; set; }
        [JsonProperty(PropertyName = "endLocationColumn")]
        public int EndLocationColumn { get; set; }
        [JsonProperty(PropertyName = "boundaryIndex")]
        public int BoundaryIndex { get; set; }
        [JsonProperty(PropertyName = "boundaryLength")]
        public int BoundaryLength { get; set; }

 
    }



    /// <summary>
    /// Writes in json format
    /// Users can select arguments to filter output to 1. only simple tags 2. only matchlist without rollup metadata etc. 3. everything
    /// Lists of tagreportgroups are written as well as match list details so users have chose to present the same
    /// UI as shown in the HTML report to the level of detail desired...
    /// </summary>
    public class JsonWriter : Writer
    {
        List<Dictionary<string, object>> jsonResult = new List<Dictionary<string, object>>();

        public override void WriteApp(AppProfile app)
        {
            // Store the results here temporarily building up a list of items in the desired order etc.
            Dictionary<string, object> itemList = new Dictionary<string, object>(); ;
            
            if (app.SimpleTagsOnly)
            {
                List<string> keys = new List<string>(app.MetaData.UniqueTags);
                itemList.Add("tags", keys);
                keys.Sort();
            }
            else
            {
                if (!app.ExcludeRollup)
                    itemList.Add("AppProfile", app);

                //matches are added this way to avoid output of entire set of MatchItem properties which include full rule/patterns/cond.
                List<MatchItems> matches = new List<MatchItems>();
                
                foreach (MatchRecord match in app.MatchList)
                {
                    MatchItems matchItem = new MatchItems(match);
                    matches.Add(matchItem);
                }

                itemList.Add("matchDetails", matches);
            }

            jsonResult.Add(itemList);
        }

      
        public override void FlushAndClose()
        {
            TextWriter.Write(JsonConvert.SerializeObject(jsonResult, Formatting.Indented));
            TextWriter.Flush();
            TextWriter.Close();
        }

        
    }
}
