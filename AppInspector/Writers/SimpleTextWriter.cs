// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AppInspector.CLI.Writers
{
    public class SimpleTextWriter : Writer
    {
        readonly int COLUMN_MAX = 80;

        public SimpleTextWriter(string formatString)
        {
            if (string.IsNullOrEmpty(formatString))
                _formatString = "Tag:%T,Rule:%N,Ruleid:%R,Confidence:%X,File:%F,Sourcetype:%t,Line:%L,Sample:%m";
            else
                _formatString = formatString;
        }

        public override void WriteApp(AppProfile app)
        {
            //option for tags only vs full match record structure; used tagdiff and tagtest commands
            if (app.SimpleTagsOnly)
            {
                List<string> tags = new List<string>();
                foreach (MatchRecord match in app.MatchList)
                    foreach (string tag in match.Issue.Rule.Tags)
                        tags.Add(tag);

                tags.Sort();
                foreach (string tag in tags)
                    TextWriter.WriteLine(tag);
            }
            else
            {
                if (!app.ExcludeRollup)
                    WriteAppMeta(app);

                TextWriter.WriteLine(MakeHeading("Dependencies"));
                WriteDependencies(app);

                TextWriter.WriteLine(MakeHeading("Match Details"));
                foreach (MatchRecord match in app.MatchList)
                    WriteMatch(match);
            }

            TextWriter.WriteLine("\nEnd of report");
        }


        private string StringList (HashSet<string> data)
        {
            string results="";

            foreach (string s in data)
                results += s + " ";

            return results;
        }


        private string StringList(SortedDictionary<string,string> data)
        {
            string results = "";

            foreach (string s in data.Values)
                results += s + " ";

            return results;
        }


        /// <summary>
        /// even out delineator for headings
        /// </summary>
        /// <param name="header"></param>
        /// <returns></returns>
        private string MakeHeading(string header)
        {
            string result = header;
            for (int i = header.Length; i < COLUMN_MAX; i++)
                result += "-";

            return result;
        }


        public void WriteAppMeta(AppProfile app)
        {
            //write predefined characteristics
            TextWriter.WriteLine(MakeHeading("Project Info"));
            TextWriter.WriteLine("Name: {0}", app.MetaData.ApplicationName + " " + app.MetaData.SourceVersion);
            TextWriter.WriteLine("Description: {0}", app.MetaData.Description);
            TextWriter.WriteLine("Source path: {0}", app.SourcePath);
            TextWriter.WriteLine("Authors: {0}", app.MetaData.Authors);
            TextWriter.WriteLine("Last Updated: {0}", app.MetaData.LastUpdated);
            TextWriter.WriteLine(MakeHeading("Scan Settings"));
            TextWriter.WriteLine("AppInspector version: {0}", app.Version);
            TextWriter.WriteLine("Rules path: {0}", StringList(app.MetaData.RulePaths));
            TextWriter.WriteLine("Run Arguments: {0}", app.Args);
            TextWriter.WriteLine("Date scanned: {0}", app.DateScanned);
            TextWriter.WriteLine(MakeHeading("Source Info"));
            TextWriter.WriteLine("Application type: {0}", StringList(app.MetaData.AppTypes));
            TextWriter.WriteLine("Package types: {0}", StringList(app.MetaData.PackageTypes));
            TextWriter.WriteLine("File extensions: {0}", StringList(app.MetaData.FileExtensions));
            TextWriter.WriteLine(MakeHeading("Detetected Targets"));
            TextWriter.WriteLine("Output types: {0}", StringList(app.MetaData.Outputs));
            TextWriter.WriteLine("OS Targets: {0}", StringList(app.MetaData.OSTargets));
            TextWriter.WriteLine("CPU Targets: {0}", StringList(app.MetaData.CPUTargets));
            TextWriter.WriteLine("Cloud targets: {0}", StringList(app.MetaData.CloudTargets));
            TextWriter.WriteLine(MakeHeading("Stats"));
            TextWriter.WriteLine("Files analyzed: {0}", app.MetaData.FilesAnalyzed);
            TextWriter.WriteLine("Files skipped: {0}", app.MetaData.FilesSkipped);
            TextWriter.WriteLine("Total files: {0}", app.MetaData.TotalFiles);
            TextWriter.WriteLine("Detections: {0} in {1} file(s)", app.MetaData.TotalMatchesCount, app.MetaData.FilesAffected);
            TextWriter.WriteLine("Unique detections: {0}", app.MetaData.UniqueMatchesCount);
            TextWriter.WriteLine(MakeHeading("Select Counters"));
            foreach (TagCounter counter in app.MetaData.TagCounters)
                TextWriter.WriteLine("{0} count: {1}", counter.ShortTag, counter.Count);

            TextWriter.WriteLine(MakeHeading("Customizable tag groups"));
            //iterate through customizable tag group lists
            foreach (string tagGroupCategory in app.KeyedTagInfoLists.Keys)
            {
                List<TagInfo> list = app.KeyedTagInfoLists[tagGroupCategory];
                TextWriter.WriteLine(string.Format("[{0}]", tagGroupCategory));
                foreach (TagInfo tagInfo in list)
                    TextWriter.WriteLine("Tag:{0},Detected:{1},Confidence:{2}", tagInfo.Tag, tagInfo.Detected, tagInfo.Confidence);             
            }
        }

        public void WriteMatch(MatchRecord match)
        {
            string output = _formatString.Replace("%F", match.Filename);
            output = output.Replace("%t", match.Language);
            output = output.Replace("%L", match.Issue.StartLocation.Line.ToString());
            output = output.Replace("%C", match.Issue.StartLocation.Column.ToString());
            output = output.Replace("%l", match.Issue.EndLocation.Line.ToString());
            output = output.Replace("%c", match.Issue.EndLocation.Column.ToString());
            output = output.Replace("%I", match.Issue.Boundary.Index.ToString());
            output = output.Replace("%i", match.Issue.Boundary.Length.ToString());
            output = output.Replace("%R", match.Issue.Rule.Id);
            output = output.Replace("%N", match.Issue.Rule.Name);
            output = output.Replace("%S", match.Issue.Rule.Severity.ToString());
            output = output.Replace("%X", match.Issue.Confidence.ToString());//override rule confidence because of unstructured text vs source
            output = output.Replace("%D", match.Issue.Rule.Description);
            output = output.Replace("%m", match.TextSample);
            output = output.Replace("%T", string.Join(',',match.Issue.Rule.Tags));

            TextWriter.WriteLine(output);

        }


        private void WriteDependencies(AppProfile app)
        {
            foreach (string s in app.MetaData.UniqueDependencies)
                TextWriter.WriteLine(s);
        }



        public override void FlushAndClose()
        {
            TextWriter.Flush();
            TextWriter.Close();
        }

        string _formatString;
    }
}
