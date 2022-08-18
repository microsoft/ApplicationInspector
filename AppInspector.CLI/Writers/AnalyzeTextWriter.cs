// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Microsoft.ApplicationInspector.CLI
{
    using System.Linq;
    using Microsoft.ApplicationInspector.Commands;
    using Microsoft.ApplicationInspector.RulesEngine;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    public class AnalyzeTextWriter : CommandResultsWriter
    {
        private readonly int COLUMN_MAX = 80;
        public override void WriteResults(Result result, CLICommandOptions commandOptions, bool autoClose = true)
        {
            CLIAnalyzeCmdOptions cLIAnalyzeCmdOptions = (CLIAnalyzeCmdOptions)commandOptions;
            AnalyzeResult analyzeResult = (AnalyzeResult)result;
            if (TextWriter is null)
            {
                throw new ArgumentNullException(nameof(TextWriter));
            }
            TextWriter.WriteLine("Results");

            WriteAppMeta(analyzeResult.Metadata);
            WriteDependencies(analyzeResult.Metadata);
            TextWriter.WriteLine(MakeHeading("Match Details"));

            foreach (MatchRecord match in analyzeResult.Metadata.Matches ?? new List<MatchRecord>())
            {
                WriteMatch(match);
            }

            if (autoClose)
            {
                FlushAndClose();
            }
        }

        public AnalyzeTextWriter(TextWriter textWriter, string formatString, ILoggerFactory? loggerFactory = null) : base(textWriter)
        {
            _logger = loggerFactory?.CreateLogger<AnalyzeJsonWriter>() ?? NullLogger<AnalyzeJsonWriter>.Instance;
            if (string.IsNullOrEmpty(formatString))
            {
                _formatString = "Tag:%T,Rule:%N,Ruleid:%R,Confidence:%X,File:%F,Language:%l,SourceType:%tLine:%L,%C,Sample:%m";
            }
            else
            {
                _formatString = formatString;
            }
        }

        
        private string StringList(IEnumerable<string> data)
        {
            return string.Join(' ', data);
        }

        private string StringList(IDictionary<string, int> data)
        {
            return string.Join(' ', data.Keys);
        }

        private string StringList(IDictionary<string, string> data)
        {
            StringBuilder build = new();

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
            StringBuilder build = new();
            build.Append(string.Format("[{0}]", header));
            for (int i = header.Length; i < COLUMN_MAX; i++)
            {
                build.Append("-");
            }

            return build.ToString();
        }

        
        public void WriteAppMeta(MetaData metaData)
        {
            if (TextWriter is null)
            {
                throw new ArgumentNullException(nameof(TextWriter));
            }
            //write predefined characteristics
            TextWriter.WriteLine(string.Format(MakeHeading("Project Info")));
            TextWriter.WriteLine(string.Format("Name: {0}", metaData.ApplicationName + " " + metaData.SourceVersion));
            TextWriter.WriteLine(string.Format("Description: {0}", metaData.Description));
            TextWriter.WriteLine(string.Format("Source path: {0}", metaData.SourcePath));
            TextWriter.WriteLine(string.Format("Authors: {0}", metaData.Authors));
            TextWriter.WriteLine(string.Format("Last Updated: {0}", metaData.LastUpdated));
            TextWriter.WriteLine(string.Format("Languages: {0}", StringList(metaData.Languages?.Select(x => x.Key) ?? Array.Empty<string>())));
            TextWriter.WriteLine(string.Format(MakeHeading("Scan Settings")));
            TextWriter.WriteLine(string.Format("Date scanned: {0}", metaData.DateScanned));
            TextWriter.WriteLine(string.Format(MakeHeading("Source Info")));
            TextWriter.WriteLine(string.Format("Application type: {0}", StringList(metaData.AppTypes ?? new List<string>())));
            TextWriter.WriteLine(string.Format("Package types: {0}", StringList(metaData.PackageTypes ?? new List<string>())));
            TextWriter.WriteLine(string.Format("File extensions: {0}", StringList(metaData.FileExtensions ?? new List<string>())));
            TextWriter.WriteLine(string.Format(MakeHeading("Detetected Targets")));
            TextWriter.WriteLine(string.Format("Output types: {0}", StringList(metaData.Outputs ?? new List<string>())));
            TextWriter.WriteLine(string.Format("OS Targets: {0}", StringList(metaData.OSTargets ?? new List<string>())));
            TextWriter.WriteLine(string.Format("CPU Targets: {0}", StringList(metaData.CPUTargets ?? new List<string>())));
            TextWriter.WriteLine(string.Format("Cloud targets: {0}", StringList(metaData.CloudTargets ?? new List<string>())));
            TextWriter.WriteLine(string.Format(MakeHeading("Stats")));
            TextWriter.WriteLine(string.Format("Files analyzed: {0}", metaData.FilesAnalyzed));
            TextWriter.WriteLine(string.Format("Files skipped: {0}", metaData.FilesSkipped));
            TextWriter.WriteLine(string.Format("Total files: {0}", metaData.TotalFiles));
            TextWriter.WriteLine(string.Format("Total matches: {0} in {1} file(s)", metaData.TotalMatchesCount, metaData.FilesAffected));
            TextWriter.WriteLine(string.Format("Unique matches: {0}", metaData.UniqueMatchesCount));

            TextWriter.WriteLine(MakeHeading("UniqueTags"));
            foreach (string tag in metaData.UniqueTags ?? new List<string>())
            {
                TextWriter.WriteLine(tag);
            }

            TextWriter.WriteLine(MakeHeading("Select Counters"));
            foreach (MetricTagCounter tagCounter in metaData.TagCounters ?? new List<MetricTagCounter>())
            {
                TextWriter.WriteLine(string.Format("Tagname: {0}, Count: {1}", tagCounter.Tag, tagCounter.Count));
            }
        }

        public void WriteMatch(MatchRecord match)
        {
            if (TextWriter is null)
            {
                throw new ArgumentNullException(nameof(TextWriter));
            }
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
            output = output.Replace("%T", string.Join(',', match.Tags ?? System.Array.Empty<string>()));

            TextWriter.WriteLine(output);
        }

        private void WriteDependencies(MetaData metaData)
        {
            if (TextWriter is null)
            {
                throw new ArgumentNullException(nameof(TextWriter));
            }
            TextWriter.WriteLine(MakeHeading("Dependencies"));

            foreach (string s in metaData.UniqueDependencies ?? new List<string>())
            {
                TextWriter.WriteLine(s);
            }
        }

        private readonly string _formatString;
        private readonly ILogger<AnalyzeJsonWriter> _logger;
    }
}