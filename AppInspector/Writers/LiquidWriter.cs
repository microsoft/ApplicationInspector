// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DotLiquid;
using DotLiquid.FileSystems;
using Newtonsoft.Json;
using System.Linq;

namespace Microsoft.AppInspector
{
    public class LiquidWriter : Writer
    {
        /// <summary>
        /// Registers datatypes with html framework liquid and sets up data for use within it and used
        /// with html partial.liquid files that are embedded as resources
        /// </summary>
        /// <param name="app"></param>
        public override void WriteApp(AppProfile app)
        {
            var htmlTemplateText = File.ReadAllText(Path.Combine(Utils.GetPath(Utils.AppPath.basePath), "html/index.html"));
            Assembly test = Assembly.GetEntryAssembly();
            Template.FileSystem = new EmbeddedFileSystem(Assembly.GetEntryAssembly(), "ApplicationInspector.html.partials");
            
            RegisterSafeType(typeof(AppProfile));
            RegisterSafeType(typeof(AppMetaData));
           
            var htmlTemplate = Template.Parse(htmlTemplateText);
            var data = new Dictionary<string, object>();
            data["AppProfile"] = app;

            //matchitems rather than records created to exclude full rule/patterns/cond.
            List<MatchItems> matches = new List<MatchItems>();
            foreach (MatchRecord match in app.MatchList)
            {
                MatchItems matchItem = new MatchItems(match);
                matches.Add(matchItem);
            }

            data["matchDetails"] = matches;
            
            var hashData = new Hash();
            hashData["json"] = JsonConvert.SerializeObject(data, Formatting.Indented);
            hashData["application_version"] = Program.GetVersionString();

            //add dynamic sets of groups of taginfo read from preferences for Profile page
            List<TagGroup> tagGroupList = app.GetCategoryTagGroups("profile");
            hashData["groups"] = tagGroupList;

            //add summary values for sorted tags lists of taginfo
            foreach (string outerKey in app.KeyedSortedTagInfoLists.Keys)
                hashData.Add(outerKey, app.KeyedSortedTagInfoLists[outerKey]);

            //add summary metadata lists
            hashData["cputargets"] = app.MetaData.CPUTargets;
            hashData["apptypes"] = app.MetaData.AppTypes;
            hashData["packagetypes"] = app.MetaData.PackageTypes;
            hashData["ostargets"] = app.MetaData.OSTargets;
            hashData["outputs"] = app.MetaData.Outputs;
            hashData["filetypes"] = app.MetaData.FileExtensions;
            hashData["tagcounters"] = app.MetaData.TagCountersUI;

            var htmlResult = htmlTemplate.Render(hashData);
            File.WriteAllText("output.html", htmlResult);
           
            //writes out json report for convenience and linking to from report page(s)
            String jsonReportPath = Path.Combine("output.json");
            Writer jsonWriter = WriterFactory.GetWriter("json", jsonReportPath);
            jsonWriter.TextWriter = File.CreateText(jsonReportPath);
            jsonWriter.WriteApp(app);
            jsonWriter.FlushAndClose();

            Utils.OpenBrowser("output.html");
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
