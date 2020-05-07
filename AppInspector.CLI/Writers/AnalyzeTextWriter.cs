// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Microsoft.ApplicationInspector.Commands;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
                List<string> keys = new List<string>(analyzeResult.Metadata.UniqueTags);
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

                foreach (MatchRecord match in analyzeResult.Metadata.Matches)
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

        private string StringList(HashSet<string> data)
        {
            StringBuilder build = new StringBuilder();

            foreach (string s in data)
            {
                build.Append(s);
                build.Append(" ");
            }

            return build.ToString();
        }

        private string StringList(Dictionary<string, int> data)
        {
            StringBuilder build = new StringBuilder();

            foreach (string s in data.Keys)
            {
                build.Append(s);
                build.Append(" ");
            }

            return build.ToString();
        }

        private string StringList(ConcurrentDictionary<string, int> data)
        {
            StringBuilder build = new StringBuilder();

            foreach (string s in data.Keys)
            {
                build.Append(s);
                build.Append(" ");
            }

            return build.ToString();
        }

        private string StringList(SortedDictionary<string, string> data)
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
            WriteOnce.General(String.Format(MakeHeading("Project Info")));
            WriteOnce.General(String.Format("Name: {0}", metaData.ApplicationName + " " + metaData.SourceVersion));
            WriteOnce.General(String.Format("Description: {0}", metaData.Description));
            WriteOnce.General(String.Format("Source path: {0}", metaData.SourcePath));
            WriteOnce.General(String.Format("Authors: {0}", metaData.Authors));
            WriteOnce.General(String.Format("Last Updated: {0}", metaData.LastUpdated));
            WriteOnce.General(String.Format("Languages: {0}", StringList(metaData.Languages)));
            WriteOnce.General(String.Format(MakeHeading("Scan Settings")));
            WriteOnce.General(String.Format("Date scanned: {0}", metaData.DateScanned));
            WriteOnce.General(String.Format(MakeHeading("Source Info")));
            WriteOnce.General(String.Format("Application type: {0}", StringList(metaData.AppTypes)));
            WriteOnce.General(String.Format("Package types: {0}", StringList(metaData.PackageTypes)));
            WriteOnce.General(String.Format("File extensions: {0}", StringList(metaData.FileExtensions)));
            WriteOnce.General(String.Format(MakeHeading("Detetected Targets")));
            WriteOnce.General(String.Format("Output types: {0}", StringList(metaData.Outputs)));
            WriteOnce.General(String.Format("OS Targets: {0}", StringList(metaData.OSTargets)));
            WriteOnce.General(String.Format("CPU Targets: {0}", StringList(metaData.CPUTargets)));
            WriteOnce.General(String.Format("Cloud targets: {0}", StringList(metaData.CloudTargets)));
            WriteOnce.General(String.Format(MakeHeading("Stats")));
            WriteOnce.General(String.Format("Files analyzed: {0}", metaData.FilesAnalyzed));
            WriteOnce.General(String.Format("Files skipped: {0}", metaData.FilesSkipped));
            WriteOnce.General(String.Format("Total files: {0}", metaData.TotalFiles));
            WriteOnce.General(String.Format("Total matches: {0} in {1} file(s)", metaData.TotalMatchesCount, metaData.FilesAffected));
            WriteOnce.General(String.Format("Unique matches: {0}", metaData.UniqueMatchesCount));

            WriteOnce.General(MakeHeading("UniqueTags"));
            List<string> orderedTags = metaData.UniqueTags.ToList<string>();
            orderedTags.Sort();

            foreach (string tag in orderedTags)
            {
                WriteOnce.General(tag);
            }

            WriteOnce.General(MakeHeading("Select Counters"));
            foreach (MetricTagCounter tagCounter in metaData.TagCounters)
            {
                WriteOnce.General(String.Format("Tagname: {0}, Count: {1}", tagCounter.Tag, tagCounter.Count));
            }
        }

        public void WriteMatch(MatchRecord match)
        {
            string output = _formatString.Replace("%F", match.FileName);
            output = output.Replace("%l", match.Language.Name);
            output = output.Replace("%t", match.Language.Type.ToString());
            output = output.Replace("%L", match.StartLocationLine.ToString());
            output = output.Replace("%C", match.StartLocationColumn.ToString());
            output = output.Replace("%l", match.EndLocationLine.ToString());
            output = output.Replace("%c", match.EndLocationColumn.ToString());
            output = output.Replace("%R", match.RuleId);
            output = output.Replace("%N", match.RuleName);
            output = output.Replace("%S", match.Severity);
            output = output.Replace("%X", match.PatternConfidence);
            output = output.Replace("%D", match.RuleDescription);
            output = output.Replace("%m", match.Sample);
            output = output.Replace("%T", string.Join(',', match.Tags));

            WriteOnce.General(output);
        }

        private void WriteDependencies(MetaData metaData)
        {
            WriteOnce.General(MakeHeading("Dependencies"));

            foreach (string s in metaData.UniqueDependencies)
            {
                WriteOnce.General(s);
            }
        }

        public override void FlushAndClose()
        {
            TextWriter.Flush();
            TextWriter.Close();
            WriteOnce.TextWriter = null;
        }

        private readonly string _formatString;
    }
}