// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using DotLiquid;
using DotLiquid.FileSystems;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Microsoft.ApplicationInspector.Commands
{
    public class LiquidWriter : Writer
    {
        readonly int MAX_HTML_REPORT_FILE_SIZE = 1024 * 1000 * 3;  //warn about potential slow rendering

        /// <summary>
        /// Registers datatypes with html framework liquid and sets up data for use within it and used
        /// with html partial.liquid files that are embedded as resources
        /// Liquid processing within partial html files only requires data ref to object while JS requires json objects
        /// </summary>
        /// <param name="app"></param>
        public override void WriteApp(AppProfile app)
        {
            var htmlTemplateText = File.ReadAllText(Path.Combine(Utils.GetPath(Utils.AppPath.basePath), "html/index.html"));
            Template.FileSystem = new EmbeddedFileSystem(Assembly.GetEntryAssembly(), "Microsoft.ApplicationInspector.CLI.html.partials");

            RegisterSafeType(typeof(AppProfile));
            RegisterSafeType(typeof(AppMetaData));

            var htmlTemplate = Template.Parse(htmlTemplateText);
            var data = new Dictionary<string, object>();
            data["AppProfile"] = app;

            var hashData = new Hash();
            hashData["json"] = Newtonsoft.Json.JsonConvert.SerializeObject(data);//json serialization required for [js] access to objects
            hashData["application_version"] = Utils.GetVersionString();

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
            string htmlOutputFilePath = Path.Combine(Utils.GetPath(Utils.AppPath.basePath), "output.html");
            File.WriteAllText(htmlOutputFilePath, htmlResult);

            //writes out json report for convenience and linking to from report page(s)
            String jsonReportPath = Path.Combine(Utils.GetPath(Utils.AppPath.basePath), "output.json");
            Writer jsonWriter = WriterFactory.GetWriter("json", jsonReportPath);
            jsonWriter.TextWriter = File.CreateText(jsonReportPath);
            jsonWriter.WriteApp(app);
            jsonWriter.FlushAndClose();

            //html report size warning
            string outputHTMLPath = Path.Combine(Utils.GetPath(Utils.AppPath.basePath), "output.html");
            if (File.Exists(outputHTMLPath) && new FileInfo(outputHTMLPath).Length > MAX_HTML_REPORT_FILE_SIZE)
            {
                WriteOnce.Info(ErrMsg.GetString(ErrMsg.ID.ANALYZE_REPORTSIZE_WARN));
            }

            if (!app.SuppressBrowserOpen)
                Utils.OpenBrowser(htmlOutputFilePath);
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
