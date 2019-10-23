// Copyright(C) Microsoft.All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace RulesEngine
{
    /// <summary>
    /// Helper class for language based commenting
    /// </summary>
    public class Language
    {
        private Language()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            // Load comments
            Stream resource = assembly.GetManifestResourceStream("RulesEngine.Resources.comments.json");
            using (StreamReader file = new StreamReader(resource))
            {
                Comments = JsonConvert.DeserializeObject<List<Comment>>(file.ReadToEnd());
            }

            // Load languages
            resource = assembly.GetManifestResourceStream("RulesEngine.Resources.languages.json");
            using (StreamReader file = new StreamReader(resource))
            {
                Languages = JsonConvert.DeserializeObject<List<LanguageInfo>>(file.ReadToEnd());
            }
        }

        /// <summary>
        /// Returns language for given file name
        /// </summary>
        /// <param name="fileName">File name</param>
        /// <returns>Language</returns>
        public static string FromFileName(string fileName)
        {
            if (fileName == null)
                return string.Empty;

            string file = Path.GetFileName(fileName).ToLower(CultureInfo.CurrentCulture);
            string ext = Path.GetExtension(file);

            // Look for whole filename first
            foreach (LanguageInfo item in Instance.Languages)
            {
                if (item.Name == file)
                    return item.Name;
            }

            // Look for extension only ext is defined
            if (!string.IsNullOrEmpty(ext))
            {
                foreach (LanguageInfo item in Instance.Languages)
                {
                    if (Array.Exists(item.Extensions, x => x.EndsWith(ext)))
                        return item.Name;
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Gets comment inline for given language
        /// </summary>        
        /// <param name="language">Language</param>
        /// <returns>Commented string</returns>
        public static string GetCommentInline(string language)
        {
            string result = string.Empty;

            if (language != null)
            {
                foreach (Comment comment in Instance.Comments)
                {
                    if (comment.Languages.Contains(language))
                        return comment.Inline;
                }
            }

            return result;
        }

        /// <summary>
        /// Gets comment preffix for given language
        /// </summary>        
        /// <param name="language">Language</param>
        /// <returns>Commented string</returns>
        public static string GetCommentPrefix(string language)
        {
            string result = string.Empty;

            if (language != null)
            {
                foreach (Comment comment in Instance.Comments)
                {
                    if (comment.Languages.Contains(language))
                        return comment.Preffix;
                }
            }

            return result;
        }

        /// <summary>
        /// Gets comment suffix for given language
        /// </summary>        
        /// <param name="language">Language</param>
        /// <returns>Commented string</returns>
        public static string GetCommentSuffix(string language)
        {
            string result = string.Empty;

            if (language != null)
            {
                foreach (Comment comment in Instance.Comments)
                {
                    if (comment.Languages.Contains(language))
                        return comment.Suffix;
                }
            }

            return result;
        }

        /// <summary>
        /// Get names of all known lannguages
        /// </summary>
        /// <returns>Returns list of names</returns>
        public static string[] GetNames()
        {
            var names = from x in Instance.Languages
                        select x.Name;

            return names.ToArray();
        }

        private static Language _instance;
        private static Language Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new Language();

                return _instance;
            }        
        }

        private List<Comment> Comments;
        private List<LanguageInfo> Languages;      
    }
}

