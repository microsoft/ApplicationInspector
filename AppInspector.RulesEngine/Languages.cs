// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.ApplicationInspector.RulesEngine;

/// <summary>
///     Helper class for language based commenting
/// </summary>
public sealed class Languages
{
    private const string CommentResourcePath = "Microsoft.ApplicationInspector.RulesEngine.Resources.comments.json";
    private const string LanguagesResourcePath = "Microsoft.ApplicationInspector.RulesEngine.Resources.languages.json";
    private readonly List<Comment> _comments = new();
    private readonly List<LanguageInfo> _languageInfos = new();
    private readonly ILogger _logger;

    public Languages(ILoggerFactory? loggerFactory = null, Stream? commentsStream = null,
        Stream? languagesStream = null)
    {
        _logger = loggerFactory?.CreateLogger<Languages>() ?? new NullLogger<Languages>();
        var assembly = typeof(Languages).Assembly;
        var commentResource = commentsStream ?? assembly.GetManifestResourceStream(CommentResourcePath);

        if (commentResource is null)
        {
            _logger.LogError("Failed to load embedded comments configuration from {CommentResourcePath}",
                CommentResourcePath);
        }
        else
        {
            var result = JsonSerializer.DeserializeAsync<List<Comment>>(commentResource);
            if (result.Result != null)
            {
                _comments = result.Result.ToList();
            }
        }

        var languagesResource = languagesStream ?? assembly.GetManifestResourceStream(LanguagesResourcePath);
        if (languagesResource is null)
        {
            _logger.LogError("Failed to load embedded languages configuration from {LanguagesResourcePath}",
                LanguagesResourcePath);
        }
        else
        {
            var result = JsonSerializer.DeserializeAsync<List<LanguageInfo>>(languagesResource);
            if (result.Result != null)
            {
                _languageInfos = result.Result.ToList();
            }
        }
    }

    public static Languages FromConfigurationFiles(ILoggerFactory? loggerFactory = null, string? commentsPath = null,
        string? languagesPath = null)
    {
        Stream? commentsStream = commentsPath is null ? null : File.OpenRead(commentsPath);
        Stream? languagesStream = languagesPath is null ? null : File.OpenRead(languagesPath);
        try
        {
            return new Languages(loggerFactory, commentsStream, languagesStream);
        }
        finally
        {
            commentsStream?.Dispose();
            languagesStream?.Dispose();
        }
    }

    /// <summary>
    ///     Returns the language for a given file name if detected.
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
    ///     Returns <see cref="LanguageInfo" /> for a given filename. It is recommended to use <see cref="FromFileNameOut" />
    ///     instead.
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

        var file = Path.GetFileName(fileName).ToLower(CultureInfo.CurrentCulture);
        var ext = Path.GetExtension(file);

        // Look for whole filename first
        if (_languageInfos.FirstOrDefault(item =>
                item.FileNames?.Contains(file, StringComparer.InvariantCultureIgnoreCase) ?? false) is LanguageInfo
            langInfo)
        {
            info = langInfo;
            return true;
        }

        // Look for extension only ext is defined
        if (!string.IsNullOrEmpty(ext))
        {
            if (_languageInfos.FirstOrDefault(item =>
                    item.Extensions?.Contains(ext, StringComparer.InvariantCultureIgnoreCase) ?? false) is LanguageInfo
                extLangInfo)
            {
                info = extLangInfo;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Gets comment inline for given language
    /// </summary>
    /// <param name="language">Language</param>
    /// <returns>Commented string</returns>
    public string GetCommentInline(string language)
    {
        var result = string.Empty;

        if (language != null)
        {
            foreach (var comment in _comments)
            {
                if (Array.Exists(comment.Languages ?? new[] { "" },
                        x => x.Equals(language, StringComparison.InvariantCultureIgnoreCase)) && comment.Inline is { })
                {
                    return comment.Inline;
                }
            }
        }

        return result;
    }

    /// <summary>
    ///     Gets comment preffix for given language
    /// </summary>
    /// <param name="language">Language</param>
    /// <returns>Commented string</returns>
    public string GetCommentPrefix(string language)
    {
        var result = string.Empty;

        if (language != null)
        {
            foreach (var comment in _comments)
            {
                if ((comment.Languages?.Contains(language.ToLower(CultureInfo.InvariantCulture)) ?? false) &&
                    comment.Prefix is { })
                {
                    return comment.Prefix;
                }
            }
        }

        return result;
    }

    /// <summary>
    ///     Gets comment suffix for given language
    /// </summary>
    /// <param name="language">Language</param>
    /// <returns>Commented string</returns>
    public string GetCommentSuffix(string language)
    {
        var result = string.Empty;

        if (language != null)
        {
            foreach (var comment in _comments)
            {
                if (Array.Exists(comment.Languages ?? new[] { "" },
                        x => x.Equals(language, StringComparison.InvariantCultureIgnoreCase)) && comment.Suffix is { })
                {
                    return comment.Suffix;
                }
            }
        }

        return result;
    }

    /// <summary>
    ///     Get names of all known languages
    /// </summary>
    /// <returns>Returns list of names</returns>
    public string[] GetNames()
    {
        var names = from x in _languageInfos
            select x.Name;

        return names.ToArray();
    }
}