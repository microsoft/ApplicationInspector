// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using DotLiquid;
using DotLiquid.FileSystems;
using Newtonsoft.Json;
using System.Linq;

namespace Microsoft.AppInspector.CLI.Writers
{
    public class Random : DotLiquid.Tag
    {
        private int _max;

        public override void Initialize(string tagName, string markup, List<string> tokens)
        {
            base.Initialize(tagName, markup, tokens);
            _max = Convert.ToInt32(markup);
        }
    }



    public class LiquidWriter : Writer
    {
        /**
         * Writes data (defined in `app`) to the HTML template.
         */
        public override void WriteApp(AppProfile app)
        {
            var htmlTemplateText = File.ReadAllText("html/index.html");
            Template.FileSystem = new EmbeddedFileSystem(Assembly.GetEntryAssembly(), "AppInspector.CLI.html.partials");
            Template.RegisterTag<Random>("random");

            RegisterSafeType(typeof(AppProfile));
            RegisterSafeType(typeof(AppMetaData));
           
            var htmlTemplate = Template.Parse(htmlTemplateText);

            var data = new Dictionary<string, object>();
            data["AppProfile"] = app;

            //matches are added this way to avoid output of entire set of MatchItem properties which include full rule/patterns/cond.
            List<MatchItems> matches = new List<MatchItems>();

            foreach (MatchRecord match in app.MatchList)
            {
                MatchItems matchItem = new MatchItems(match);
                matches.Add(matchItem);
            }
            data["matchDetails"] = matches;
            var hashData = new Hash();
            hashData["profile"] = app;
            hashData["match_details"] = matches;
            hashData["json"] = JsonConvert.SerializeObject(data, Formatting.Indented);
            hashData["application_version"] = Program.GetVersionString();

            List<TagGroup> tagGroupList = app.GetCategoryTagGroups("profile");
            hashData["groups"] = tagGroupList;

            foreach (string outerKey in app.KeyedSortedTagInfoLists.Keys)
                hashData.Add(outerKey, app.KeyedSortedTagInfoLists[outerKey]);

            hashData.Add("cputargets", app.MetaData.CPUTargets);
            hashData.Add("apptypes", app.MetaData.AppTypes);
            hashData.Add("packagetypes", app.MetaData.PackageTypes);
            hashData.Add("ostargets", app.MetaData.OSTargets);

            hashData["tagcounters"] = app.MetaData.TagCounters;
            //foreach (TagCounter counter in app.MetaData.TagCounters)
              //  hashData.Add(counter.Tag, counter.Count);

            var htmlResult = htmlTemplate.Render(hashData);
            File.WriteAllText("output.html", htmlResult);
           
            //writes out json report for convenience and linking to from report page(s)
            String jsonReportPath = Path.Combine("output.json");
            Writer jsonWriter = WriterFactory.GetWriter("json", jsonReportPath);
            jsonWriter.TextWriter = File.CreateText(jsonReportPath);
            jsonWriter.WriteApp(app);
            jsonWriter.FlushAndClose();

            Helper.OpenBrowser("output.html");
        }

       
        public override void FlushAndClose()
        {
            TextWriter.Flush();
            TextWriter.Close();
        }

        private void RegisterSafeType(Type type)
        {
            Template.RegisterSafeType(type, (t) => t.ToString());
            Template.RegisterSafeType(type, type.GetMembers(BindingFlags.Instance).Select((e) => e.Name).ToArray());
        }
    }
}
