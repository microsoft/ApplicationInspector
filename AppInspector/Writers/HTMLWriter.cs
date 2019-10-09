// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using HandlebarsDotNet;


namespace Microsoft.AppInspector.CLI.Writers
{
   
    public class HTMLWriter : Writer
    {

        /// <summary>
        /// WriteApp dynamically pulls from the template folder and copies all files to the target report folder
        /// making sure to process any "template" named files as needed before compilation by the templating framework
        /// </summary>
        /// <param name="app"></param>
        public override void WriteApp(AppProfile app)
        {
            string templatesPath = Helper.GetPath(Helper.AppPath.htmlTemplates);
            string htmlReportsPath = Helper.GetPath(Helper.AppPath.htmlReport);
            IEnumerable<string> files = Directory.EnumerateFiles(templatesPath, "*.*");

            //report too unwieldy for single file so place in named directory; create if new
            if (!Directory.Exists(htmlReportsPath))
                Directory.CreateDirectory(htmlReportsPath);

            foreach (string filePath in files)
            {
                if (!filePath.Contains("template-"))
                    File.Copy(filePath, Path.Combine(htmlReportsPath, Path.GetFileName(filePath)), true);
                else
                {
                    string finalPath = filePath.Replace("template-", "");
                    ProcessTemplate(filePath, Path.Combine(htmlReportsPath, Path.GetFileName(finalPath)), app);
                }
            }

            //writes out json report for convenience and linking to from report page(s)
            String jsonReportPath = Path.Combine(htmlReportsPath, "jsonreport.json");
            Writer jsonWriter = WriterFactory.GetWriter("json", jsonReportPath);
            jsonWriter.TextWriter = File.CreateText(jsonReportPath);
            jsonWriter.WriteApp(app);
            jsonWriter.FlushAndClose();

            OpenBrowser(Path.Combine(htmlReportsPath, "index.html"));
        }


        /// <summary>
        /// ProcessTemplate will match files with tagcategories to locate the right set of data to replace before 
        /// final template compilation by the templating framework used
        /// </summary>
        /// <param name="templateFilePath"></param>
        /// <param name="finalFilePath"></param>
        /// <param name="app"></param>
        private void ProcessTemplate(string templateFilePath, string finalFilePath, AppProfile app)
        {
            string categoryName = null;
            foreach (TagCategory tagCategory in app.TagGroupPreferences)
            {
                if (templateFilePath.Contains(tagCategory.Name))
                {
                    categoryName = tagCategory.Name;                  
                    break;
                }
            }

            string processedTemplate = GetTemplate(templateFilePath, categoryName, app);
            CompileTemplate(processedTemplate, finalFilePath, app);
        }



        /// <summary>
        /// Provides feature to allow dynamic creation of tag groups in template before final template compilation
        /// Used in profile and composition html pages.  If no special processing is required, returns the raw
        /// template which is assumed to contain simple template framework compatible keywords or none at all.
        /// </summary>
        /// <param name="templateFileName"></param>
        /// <param name="category"></param>
        /// <param name="app"></param>
        /// <returns></returns>
        string GetTemplate(string templateFileName, string category, AppProfile app)
        {
            string result = "";

            string templateText = File.ReadAllText(templateFileName);
            List<TagGroup> tagGroupList = string.IsNullOrEmpty(category) ? null : app.GetCategoryTagGroups(category);

            string subSectionStartPattern = "[[subSectionStart]]";
            string subSectionEndPattern = "[[subSectionEnd]]";
            int istartSection = templateText.IndexOf(subSectionStartPattern);
            int iendSection = templateText.IndexOf(subSectionEndPattern);

            if (tagGroupList == null || istartSection == -1 || iendSection == -1)
                return templateText;

            string subSectionTemplate = templateText.Substring(istartSection, iendSection-istartSection+subSectionEndPattern.Length+1);
            subSectionTemplate = subSectionTemplate.Replace(subSectionStartPattern, "");
            subSectionTemplate = subSectionTemplate.Replace(subSectionEndPattern, "");

            //loop through optional sections for group to replace with actual variable names, titles etc.
            foreach (TagGroup tagGroup in tagGroupList)
            {
                result += subSectionTemplate;
                int isectionName= result.IndexOf("[[subSectionName]]");
                if (-1 != isectionName)
                {
                    result = result.Replace("[[subSectionName]]", tagGroup.Title);
                }

                int isectionVarRef = result.IndexOf("[[subSectionVarRef]]");
                if (-1 != isectionVarRef)
                {
                    result = result.Replace("[[subSectionVarRef]]", "tagGrp" + tagGroup.DataRef);
                    result = result.Replace("tagGrptagGrp", "tagGrp"); //in case someone thinks to add in profile.json etc.
                }
            }

            //create dup sub template section with start/end and use as revised template the continue compilation
            string leftSide = templateText.Substring(0, istartSection);
            string rightSide = templateText.Substring(iendSection + subSectionEndPattern.Length);
            string finalResult = leftSide + result + rightSide;
            return finalResult;
        }



        private void CompileTemplateSimple(string templateFileName, string outputFileName, AppProfile app)
        {
            string templateText = File.ReadAllText(templateFileName);
            CompileTemplate(templateText, outputFileName, app);
        }


        private void CompileTemplate(string templateText, string outputFileName, AppProfile app)
        {
            //add app object for access to various items (TBD if needed in future)
            var data = new Dictionary<string, object>()
            {
                { "app", app }
            };

            //now add all list groups
            foreach (string outerKey in app.KeyedTagInfoLists.Keys)
                data.Add(outerKey, app.KeyedTagInfoLists[outerKey]);

            foreach (string outerKey in app.KeyedSortedTagInfoLists.Keys)
                data.Add(outerKey, app.KeyedSortedTagInfoLists[outerKey]);

            data.Add("selectTagCounters", app.MetaData.TagCounters);

            foreach (string outerKey in app.MetaData.KeyedPropertyLists.Keys)
                data.Add(outerKey, app.MetaData.KeyedPropertyLists[outerKey]);

            TextWriter textWriter = null;

            try
            {
                var template = Handlebars.Compile(templateText);
                textWriter = File.CreateText(outputFileName);
                textWriter.Write(template(data));
            }
            catch (Exception ex)
            {
                WriteOnce.Error("Error creating HTML report");
                throw new Exception(ex.Message);
            }
            finally
            {
                if (textWriter != null)
                {
                    textWriter.Flush();
                    textWriter.Close();
                }
            }
        }


        public static void OpenBrowser(string url)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}"));
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
        }


        public override void FlushAndClose()
        {
            TextWriter.Flush();
            TextWriter.Close();
        }

    }
}
