// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

namespace Microsoft.ApplicationInspector.RulesEngine
{
    using Microsoft.ApplicationInspector.Common;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Helper class for language based commenting
    /// </summary>
    public sealed class Languages
    {
        private readonly List<Comment> Comments;
        private readonly List<LanguageInfo> LanguageInfos;
        private readonly ILogger _logger;
        public Languages(ILoggerFactory? loggerFactory = null, string? commentPath = null, string? languagePath = null)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            _logger = loggerFactory?.CreateLogger<Languages>() ?? new NullLogger<Languages>();
            // Load comments
            if (string.IsNullOrEmpty(commentPath))
            {
                Stream? resource = assembly.GetManifestResourceStream("Microsoft.ApplicationInspector.RulesEngine.Resources.comments.json");
                using StreamReader file = new(resource ?? new MemoryStream());
                Comments = JsonConvert.DeserializeObject<List<Comment>>(file.ReadToEnd()) ?? new List<Comment>();
            }
            else
            {
                if (JsonConvert.DeserializeObject<List<Comment>>(File.ReadAllText(commentPath)) is List<Comment> comments)
                {
                    Comments = comments;
                }
                else
                {
                    Comments = new List<Comment>();
                    _logger.LogWarning("Provided file {commentPath} could not be parsed as a valid List of Comment objects.", commentPath);
                }
            }

            // Load Languages
            if (string.IsNullOrEmpty(languagePath))
            {
                Stream? resource = assembly.GetManifestResourceStream("Microsoft.ApplicationInspector.RulesEngine.Resources.languages.json");
                using StreamReader file = new(resource ?? new MemoryStream());
                LanguageInfos = JsonConvert.DeserializeObject<List<LanguageInfo>>(file.ReadToEnd()) ?? new List<LanguageInfo>(); ;
            }
            else
            {
                if (JsonConvert.DeserializeObject<List<LanguageInfo>>(File.ReadAllText(languagePath)) is List<LanguageInfo> languages)
                {
                    LanguageInfos = languages;
                }
                else
                {
                    LanguageInfos = new List<LanguageInfo>();
                    _logger.LogWarning("Provided file {langPath} could not be parsed as a valid List of LanguageInfo objects.", languagePath);
                }
            }
        }

        /// <summary>
        /// Returns the language for a given file name if detected.
        /// </summary>
        /// <param name="fileName">The FileName to check.</param>
        /// <param name="info">If this returns true, a valid LanguageInfo object. If false, undefined.</param>
        /// <returns>True if the language could be detected based on the filename.</returns>
        public bool FromFileNameOut(string fileName, out LanguageInfo info)
        {
            info = new LanguageInfo();

            return FromFileName(fileName, ref info);
        }

        /// <summary>
        /// Returns <see cref="LanguageInfo"/> for a given filename. It is recommended to use <see cref="FromFileNameOut"/> instead.
        /// </summary>
        /// <param name="fileName">File name</param>
        /// <param name="info">Ref to an existing LanguageInfo object to assign the result to, if true is returned.</param>
        /// <returns>True if the language could be detected based on the filename</returns>
        public bool FromFileName(string fileName, ref LanguageInfo info)
        {
            if (fileName == null)
            {
                return false;
            }

            string file = Path.GetFileName(fileName).ToLower(CultureInfo.CurrentCulture);
            string ext = Path.GetExtension(file);

            // Look for whole filename first
            if (LanguageInfos.FirstOrDefault(item => item.FileNames?.Contains(file,StringComparer.InvariantCultureIgnoreCase) ?? false) is LanguageInfo langInfo)
            {
                info = langInfo;
                return true;
            }
            
            // Look for extension only ext is defined
            if (!string.IsNullOrEmpty(ext))
            {
                if (LanguageInfos.FirstOrDefault(item => item.Extensions?.Contains(ext,StringComparer.InvariantCultureIgnoreCase) ?? false) is LanguageInfo extLangInfo)
                {
                    info = extLangInfo;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets comment inline for given language
        /// </summary>
        /// <param name="language">Language</param>
        /// <returns>Commented string</returns>
        public string GetCommentInline(string language)
        {
            string result = string.Empty;

            if (language != null)
            {
                foreach (Comment comment in Comments)
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
        public string GetCommentPrefix(string language)
        {
            string result = string.Empty;

            if (language != null)
            {
                foreach (Comment comment in Comments)
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
        public string GetCommentSuffix(string language)
        {
            string result = string.Empty;

            if (language != null)
            {
                foreach (Comment comment in Comments)
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
        public string[] GetNames()
        {
            var names = from x in LanguageInfos
                        select x.Name;

            return names.ToArray();
        }
    }
}