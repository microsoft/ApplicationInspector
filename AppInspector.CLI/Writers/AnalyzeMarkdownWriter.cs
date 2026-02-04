// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.ApplicationInspector.Commands;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.ApplicationInspector.CLI;

/// <summary>
///     Writes analysis results in Markdown format, suitable for CI environments.
///     Provides a concise summary of key features and findings.
/// </summary>
public class AnalyzeMarkdownWriter : CommandResultsWriter
{
    private readonly ILogger<AnalyzeMarkdownWriter> _logger;

    public AnalyzeMarkdownWriter(StreamWriter streamWriter, ILoggerFactory? loggerFactory = null) : base(streamWriter)
    {
        _logger = loggerFactory?.CreateLogger<AnalyzeMarkdownWriter>() ?? NullLogger<AnalyzeMarkdownWriter>.Instance;
    }

    public override void WriteResults(Result result, CLICommandOptions commandOptions, bool autoClose = true)
    {
        var analyzeResult = (AnalyzeResult)result;
        if (TextWriter is null)
        {
            throw new ArgumentNullException(nameof(TextWriter));
        }

        WriteMarkdownReport(analyzeResult);

        if (autoClose)
        {
            FlushAndClose();
        }
    }

    private void WriteMarkdownReport(AnalyzeResult analyzeResult)
    {
        var metadata = analyzeResult.Metadata;
        
        // Title
        TextWriter.WriteLine("# Application Inspector Analysis Report");
        TextWriter.WriteLine();

        // Summary Section
        TextWriter.WriteLine("## Summary");
        TextWriter.WriteLine();
        WriteProjectInfo(metadata);
        TextWriter.WriteLine();

        // Key Statistics
        TextWriter.WriteLine("## Key Statistics");
        TextWriter.WriteLine();
        WriteStatistics(metadata);
        TextWriter.WriteLine();

        // Key Features Detected
        TextWriter.WriteLine("## Key Features Detected");
        TextWriter.WriteLine();
        WriteKeyFeatures(metadata);
        TextWriter.WriteLine();

        // Detected Technologies
        if (metadata.Languages?.Any() == true || metadata.AppTypes?.Any() == true)
        {
            TextWriter.WriteLine("## Detected Technologies");
            TextWriter.WriteLine();
            WriteDetectedTechnologies(metadata);
            TextWriter.WriteLine();
        }

        // Target Platforms
        if (HasTargetPlatforms(metadata))
        {
            TextWriter.WriteLine("## Target Platforms");
            TextWriter.WriteLine();
            WriteTargetPlatforms(metadata);
            TextWriter.WriteLine();
        }

        // Dependencies
        if (metadata.UniqueDependencies?.Any() == true)
        {
            TextWriter.WriteLine("## Dependencies");
            TextWriter.WriteLine();
            WriteDependencies(metadata);
            TextWriter.WriteLine();
        }

        // Tag Counters
        if (metadata.TagCounters?.Any() == true)
        {
            TextWriter.WriteLine("## Detailed Tag Counters");
            TextWriter.WriteLine();
            WriteTagCounters(metadata);
        }
    }

    private void WriteProjectInfo(MetaData metadata)
    {
        TextWriter.WriteLine($"- **Application Name**: {metadata.ApplicationName ?? "N/A"}");
        if (!string.IsNullOrEmpty(metadata.SourceVersion))
        {
            TextWriter.WriteLine($"- **Version**: {metadata.SourceVersion}");
        }
        TextWriter.WriteLine($"- **Source Path**: `{metadata.SourcePath ?? "N/A"}`");
        if (!string.IsNullOrEmpty(metadata.Description))
        {
            TextWriter.WriteLine($"- **Description**: {metadata.Description}");
        }
        if (!string.IsNullOrEmpty(metadata.Authors))
        {
            TextWriter.WriteLine($"- **Authors**: {metadata.Authors}");
        }
        TextWriter.WriteLine($"- **Date Scanned**: {metadata.DateScanned ?? "N/A"}");
        if (!string.IsNullOrEmpty(metadata.LastUpdated) && metadata.LastUpdated != DateTime.MinValue.ToString())
        {
            TextWriter.WriteLine($"- **Last Updated**: {metadata.LastUpdated}");
        }
    }

    private void WriteStatistics(MetaData metadata)
    {
        TextWriter.WriteLine("| Metric | Count |");
        TextWriter.WriteLine("|--------|-------|");
        TextWriter.WriteLine($"| Total Files | {metadata.TotalFiles} |");
        TextWriter.WriteLine($"| Files Analyzed | {metadata.FilesAnalyzed} |");
        TextWriter.WriteLine($"| Files Skipped | {metadata.FilesSkipped} |");
        if (metadata.FilesTimedOut > 0)
        {
            TextWriter.WriteLine($"| Files Timed Out | {metadata.FilesTimedOut} |");
        }
        TextWriter.WriteLine($"| Files with Matches | {metadata.FilesAffected} |");
        TextWriter.WriteLine($"| Total Matches | {metadata.TotalMatchesCount} |");
        TextWriter.WriteLine($"| Unique Matches | {metadata.UniqueMatchesCount} |");
        TextWriter.WriteLine($"| Unique Tags | {metadata.UniqueTags.Count} |");
    }

    private void WriteKeyFeatures(MetaData metadata)
    {
        if (metadata.UniqueTags?.Any() != true)
        {
            TextWriter.WriteLine("_No unique features detected._");
            return;
        }

        // Group tags by category for better organization
        var tagsByCategory = metadata.UniqueTags
            .GroupBy(tag => tag.Split('.').FirstOrDefault() ?? "Other")
            .OrderBy(g => g.Key);

        foreach (var category in tagsByCategory)
        {
            TextWriter.WriteLine($"### {category.Key}");
            TextWriter.WriteLine();
            foreach (var tag in category.OrderBy(t => t))
            {
                TextWriter.WriteLine($"- `{tag}`");
            }
            TextWriter.WriteLine();
        }
    }

    private void WriteDetectedTechnologies(MetaData metadata)
    {
        if (metadata.Languages?.Any() == true)
        {
            TextWriter.WriteLine("### Languages");
            TextWriter.WriteLine();
            foreach (var lang in metadata.Languages.OrderByDescending(l => l.Value))
            {
                TextWriter.WriteLine($"- **{lang.Key}**: {lang.Value} file(s)");
            }
            TextWriter.WriteLine();
        }

        if (metadata.AppTypes?.Any() == true)
        {
            TextWriter.WriteLine("### Application Types");
            TextWriter.WriteLine();
            foreach (var appType in metadata.AppTypes.OrderBy(a => a))
            {
                TextWriter.WriteLine($"- {appType}");
            }
            TextWriter.WriteLine();
        }

        if (metadata.PackageTypes?.Any() == true)
        {
            TextWriter.WriteLine("### Package Types");
            TextWriter.WriteLine();
            foreach (var packageType in metadata.PackageTypes.OrderBy(p => p))
            {
                TextWriter.WriteLine($"- {packageType}");
            }
            TextWriter.WriteLine();
        }

        if (metadata.FileExtensions?.Any() == true)
        {
            TextWriter.WriteLine("### File Extensions");
            TextWriter.WriteLine();
            var extensions = string.Join(", ", metadata.FileExtensions.OrderBy(e => e).Select(e => $"`{e}`"));
            TextWriter.WriteLine(extensions);
        }
    }

    private bool HasTargetPlatforms(MetaData metadata)
    {
        return (metadata.OSTargets?.Any() == true) ||
               (metadata.CPUTargets?.Any() == true) ||
               (metadata.CloudTargets?.Any() == true) ||
               (metadata.Outputs?.Any() == true);
    }

    private void WriteTargetPlatforms(MetaData metadata)
    {
        if (metadata.Outputs?.Any() == true)
        {
            TextWriter.WriteLine("### Output Types");
            TextWriter.WriteLine();
            foreach (var output in metadata.Outputs.OrderBy(o => o))
            {
                TextWriter.WriteLine($"- {output}");
            }
            TextWriter.WriteLine();
        }

        if (metadata.OSTargets?.Any() == true)
        {
            TextWriter.WriteLine("### Operating Systems");
            TextWriter.WriteLine();
            foreach (var os in metadata.OSTargets.OrderBy(o => o))
            {
                TextWriter.WriteLine($"- {os}");
            }
            TextWriter.WriteLine();
        }

        if (metadata.CPUTargets?.Any() == true)
        {
            TextWriter.WriteLine("### CPU Architectures");
            TextWriter.WriteLine();
            foreach (var cpu in metadata.CPUTargets.OrderBy(c => c))
            {
                TextWriter.WriteLine($"- {cpu}");
            }
            TextWriter.WriteLine();
        }

        if (metadata.CloudTargets?.Any() == true)
        {
            TextWriter.WriteLine("### Cloud Platforms");
            TextWriter.WriteLine();
            foreach (var cloud in metadata.CloudTargets.OrderBy(c => c))
            {
                TextWriter.WriteLine($"- {cloud}");
            }
        }
    }

    private void WriteDependencies(MetaData metadata)
    {
        if (metadata.UniqueDependencies?.Any() != true)
        {
            return;
        }

        var deps = metadata.UniqueDependencies.OrderBy(d => d).ToList();
        
        if (deps.Count <= 20)
        {
            // Show all dependencies if 20 or fewer
            foreach (var dep in deps)
            {
                TextWriter.WriteLine($"- `{dep}`");
            }
        }
        else
        {
            // Show first 20 and indicate there are more
            foreach (var dep in deps.Take(20))
            {
                TextWriter.WriteLine($"- `{dep}`");
            }
            TextWriter.WriteLine();
            TextWriter.WriteLine($"_... and {deps.Count - 20} more_");
        }
    }

    private void WriteTagCounters(MetaData metadata)
    {
        if (metadata.TagCounters?.Any() != true)
        {
            return;
        }

        TextWriter.WriteLine("| Tag | Count |");
        TextWriter.WriteLine("|-----|-------|");
        
        foreach (var counter in metadata.TagCounters.OrderByDescending(c => c.Count).ThenBy(c => c.Tag))
        {
            TextWriter.WriteLine($"| `{counter.Tag}` | {counter.Count} |");
        }
    }
}
