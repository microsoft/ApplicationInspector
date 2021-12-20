// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Microsoft.ApplicationInspector.RulesEngine
{
    /// <summary>
    /// Helper class for language based commenting
    /// </summary>
    public sealed class Language
    {
        private static Language? _instance;
        private readonly List<Comment> Comments;
        private readonly List<LanguageInfo> Languages;

        private Language()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            // Load comments
            Stream? resource = assembly.GetManifestResourceStream("Microsoft.ApplicationInspector.RulesEngine.Resources.comments.json");
            using (StreamReader file = new(resource ?? new MemoryStream()))
            {
                Comments = JsonConvert.DeserializeObject<List<Comment>>(file.ReadToEnd()) ?? new List<Comment>(); ;
            }

            // Load languages
            resource = assembly.GetManifestResourceStream("Microsoft.ApplicationInspector.RulesEngine.Resources.languages.json");
            using (StreamReader file = new(resource ?? new MemoryStream()))
            {
                Languages = JsonConvert.DeserializeObject<List<LanguageInfo>>(file.ReadToEnd()) ?? new List<LanguageInfo>();
            }
        }

        /// <summary>
        /// Returns the language for a given file name if detected.
        /// </summary>
        /// <param name="fileName">The FileName to check.</param>
        /// <param name="info">If this returns true, a valid LanguageInfo object. If false, undefined.</param>
        /// <returns>True if the language could be detected based on the filename.</returns>
        public static bool FromFileNameOut(string fileName, out LanguageInfo info)
        {
            info = new LanguageInfo();

            return FromFileName(fileName, ref info);
        }

        /// <summary>
        /// Returns language for given file name
        /// </summary>
        /// <param name="fileName">File name</param>
        /// <returns>Language</returns>
        public static bool FromFileName(string fileName, ref LanguageInfo info)
        {
            if (fileName == null)
            {
                return false;
            }

            string file = Path.GetFileName(fileName).ToLower(CultureInfo.CurrentCulture);
            string ext = Path.GetExtension(file);

            // Look for whole filename first
            foreach (LanguageInfo item in Instance.Languages)
            {
                if (item.Name == file)
                {
                    info = item;
                    return true;
                }
            }

            // Look for extension only ext is defined
            if (!string.IsNullOrEmpty(ext))
            {
                foreach (LanguageInfo item in Instance.Languages)
                {
                    if (item.Name == "typescript-config")//special case where extension used for exact match to a single type
                    {
                        if (item.Extensions?.Any(x => x.ToLower().Equals(file)) ?? false)
                        {
                            info = item;
                            return true;
                        }
                    }
                    else if (Array.Exists(item.Extensions ?? Array.Empty<string>(), x => x.EndsWith(ext, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        info = item;
                        return true;
                    }
                }
            }

            return false;
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
                    if (Array.Exists(comment.Languages ?? new string[] { "" }, x => x.Equals(language, StringComparison.InvariantCultureIgnoreCase)) && comment.Inline is { })
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
                    if ((comment.Languages?.Contains(language.ToLower(CultureInfo.InvariantCulture)) ?? false) && comment.Prefix is { })
                        return comment.Prefix;
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
                    if (Array.Exists(comment.Languages ?? new string[] { "" }, x => x.Equals(language, StringComparison.InvariantCultureIgnoreCase)) && comment.Suffix is { })
                        return comment.Suffix;
                }
            }

            return result;
        }

        /// <summary>
        /// Get names of all known languages
        /// </summary>
        /// <returns>Returns list of names</returns>
        public static string[] GetNames()
        {
            var names = from x in Instance.Languages
                        select x.Name;

            return names.ToArray();
        }
        private static Language Instance
        {
            get
            {
                return _instance ??= new Language();
            }
        }
    }
}