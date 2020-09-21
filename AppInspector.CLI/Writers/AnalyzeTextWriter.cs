// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Microsoft.ApplicationInspector.Commands;
using Microsoft.ApplicationInspector.RulesEngine;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Microsoft.ApplicationInspector.CLI
{
    public class AnalyzeTextWriter : CommandResultsWriter
    {
        private readonly int COLUMN_MAX = 80;

        public override void WriteResults(Result result, CLICommandOptions commandOptions, bool autoClose = true)
        {
            CLIAnalyzeCmdOptions cLIAnalyzeCmdOptions = (CLIAnalyzeCmdOptions)commandOptions;
            AnalyzeResult analyzeResult = (AnalyzeResult)result;

            //For console output, update write once for same results to console or file
            WriteOnce.TextWriter = TextWriter;

            WriteOnce.Result("Results");

            if (cLIAnalyzeCmdOptions.SimpleTagsOnly)
            {
                List<string> keys = analyzeResult.Metadata.UniqueTags ?? new List<string>();
                keys.Sort();

                foreach (string tag in keys)
                {
                    WriteOnce.General(tag);
                }
            }
            else
            {
                WriteAppMeta(analyzeResult.Metadata);
                WriteDependencies(analyzeResult.Metadata);
                WriteOnce.General(MakeHeading("Match Details"));

                foreach (MatchRecord match in analyzeResult.Metadata.Matches ?? new List<MatchRecord>())
                {
                    WriteMatch(match);
                }
            }

            if (autoClose)
            {
                FlushAndClose();
            }
        }

        public AnalyzeTextWriter(string formatString)
        {
            if (string.IsNullOrEmpty(formatString))
            {
                _formatString = "Tag:%T,Rule:%N,Ruleid:%R,Confidence:%X,File:%F,Language:%l,SourceType:%tLine:%L,%C,Sample:%m";
            }
            else
            {
                _formatString = formatString;
            }
        }

        #region helpers

        private string StringList(List<string> data)
        {
            return string.Join(' ', data);
        }

        private string StringList(ImmutableSortedDictionary<string,int> data)
        { 
            return string.Join(' ', data.Keys);
        }

        private string StringList(ImmutableSortedDictionary<string, string> data)
        {
            StringBuilder build = new StringBuilder();

            foreach (string s in data.Values)
            {
                build.Append(s);
                build.Append(" ");
            }

            return build.ToString();
        }

        /// <summary>
        /// even out delineator for headings
        /// </summary>
        /// <param name="header"></param>
        /// <returns></returns>
        private string MakeHeading(string header)
        {
            StringBuilder build = new StringBuilder();
            build.Append(string.Format("[{0}]", header));
            for (int i = header.Length; i < COLUMN_MAX; i++)
            {
                build.Append("-");
            }

            return build.ToString();
        }

        #endregion helpers

        public void WriteAppMeta(MetaData metaData)
        {
            //write predefined characteristics
            WriteOnce.General(string.Format(MakeHeading("Project Info")));
            WriteOnce.General(string.Format("Name: {0}", metaData.ApplicationName + " " + metaData.SourceVersion));
            WriteOnce.General(string.Format("Description: {0}", metaData.Description));
            WriteOnce.General(string.Format("Source path: {0}", metaData.SourcePath));
            WriteOnce.General(string.Format("Authors: {0}", metaData.Authors));
            WriteOnce.General(string.Format("Last Updated: {0}", metaData.LastUpdated));
            WriteOnce.General(string.Format("Languages: {0}", StringList(metaData.Languages.ToImmutableSortedDictionary())));
            WriteOnce.General(string.Format(MakeHeading("Scan Settings")));
            WriteOnce.General(string.Format("Date scanned: {0}", metaData.DateScanned));
            WriteOnce.General(string.Format(MakeHeading("Source Info")));
            WriteOnce.General(string.Format("Application type: {0}", StringList(metaData.AppTypes??new List<string>())));
            WriteOnce.General(string.Format("Package types: {0}", StringList(metaData.PackageTypes ?? new List<string>())));
            WriteOnce.General(string.Format("File extensions: {0}", StringList(metaData.FileExtensions ?? new List<string>())));
            WriteOnce.General(string.Format(MakeHeading("Detetected Targets")));
            WriteOnce.General(string.Format("Output types: {0}", StringList(metaData.Outputs ?? new List<string>())));
            WriteOnce.General(string.Format("OS Targets: {0}", StringList(metaData.OSTargets ?? new List<string>())));
            WriteOnce.General(string.Format("CPU Targets: {0}", StringList(metaData.CPUTargets ?? new List<string>())));
            WriteOnce.General(string.Format("Cloud targets: {0}", StringList(metaData.CloudTargets ?? new List<string>())));
            WriteOnce.General(string.Format(MakeHeading("Stats")));
            WriteOnce.General(string.Format("Files analyzed: {0}", metaData.FilesAnalyzed));
            WriteOnce.General(string.Format("Files skipped: {0}", metaData.FilesSkipped));
            WriteOnce.General(string.Format("Total files: {0}", metaData.TotalFiles));
            WriteOnce.General(string.Format("Total matches: {0} in {1} file(s)", metaData.TotalMatchesCount, metaData.FilesAffected));
            WriteOnce.General(string.Format("Unique matches: {0}", metaData.UniqueMatchesCount));

            WriteOnce.General(MakeHeading("UniqueTags"));
            foreach (string tag in metaData.UniqueTags ?? new List<string>())
            {
                WriteOnce.General(tag);
            }

            WriteOnce.General(MakeHeading("Select Counters"));
            foreach (MetricTagCounter tagCounter in metaData.TagCounters ?? new List<MetricTagCounter>())
            {
                WriteOnce.General(string.Format("Tagname: {0}, Count: {1}", tagCounter.Tag, tagCounter.Count));
            }
        }

        public void WriteMatch(MatchRecord match)
        {
            string output = _formatString.Replace("%F", match.FileName);
            output = output.Replace("%l", match.LanguageInfo.Name);
            output = output.Replace("%t", match.LanguageInfo.Type.ToString());
            output = output.Replace("%L", match.StartLocationLine.ToString());
            output = output.Replace("%C", match.StartLocationColumn.ToString());
            output = output.Replace("%l", match.EndLocationLine.ToString());
            output = output.Replace("%c", match.EndLocationColumn.ToString());
            output = output.Replace("%R", match.RuleId);
            output = output.Replace("%N", match.RuleName);
            output = output.Replace("%S", match.Severity.ToString());
            output = output.Replace("%X", match.Confidence.ToString());
            output = output.Replace("%D", match.RuleDescription);
            output = output.Replace("%m", match.Sample);
            output = output.Replace("%T", string.Join(',', match.Tags ?? new string[] { }));

            WriteOnce.General(output);
        }

        private void WriteDependencies(MetaData metaData)
        {
            WriteOnce.General(MakeHeading("Dependencies"));

            foreach (string s in metaData.UniqueDependencies ?? new List<string>())
            {
                WriteOnce.General(s);
            }
        }

        private readonly string _formatString;
    }
}