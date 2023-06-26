// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.ApplicationInspector.Commands;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.CST.OAT.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Location = Microsoft.CodeAnalysis.Sarif.Location;
using Result = Microsoft.ApplicationInspector.Commands.Result;

namespace Microsoft.ApplicationInspector.CLI.Writers;

internal static class AnalyzeSarifWriterExtensions
{
    internal static void AddRange(this TagsCollection tc, IEnumerable<string>? tagsToAdd)
    {
        if (tagsToAdd is null)
        {
            return;
        }

        foreach (var tag in tagsToAdd) tc.Add(tag);
    }
}

/// <summary>
///     Writes in sarif format
/// </summary>
public class AnalyzeSarifWriter : CommandResultsWriter
{
    private readonly ILogger<AnalyzeSarifWriter> _logger;

    public AnalyzeSarifWriter(StreamWriter streamWriter, ILoggerFactory? loggerFactory = null) : base(streamWriter)
    {
        _logger = loggerFactory?.CreateLogger<AnalyzeSarifWriter>() ?? NullLogger<AnalyzeSarifWriter>.Instance;
    }

    public override void WriteResults(Result result, CLICommandOptions commandOptions, bool autoClose = true)
    {
        if (StreamWriter is null)
        {
            throw new ArgumentNullException(nameof(StreamWriter));
        }

        if (commandOptions is CLIAnalyzeCmdOptions cliAnalyzeCmdOptions)
        {
            string? basePath = cliAnalyzeCmdOptions.BasePath;

            if (result is AnalyzeResult analyzeResult)
            {
                SarifLog log = new()
                {
                    Version = SarifVersion.Current
                };

                log.Runs = new List<Run>();
                // Convert Base Path to Forward Slashes to be a valid URI
                
                var run = new Run()
                {
                    Tool = new Tool
                    {
                        Driver = new ToolComponent
                        {
                            Name = "Application Inspector",
                            InformationUri = new Uri("https://github.com/microsoft/ApplicationInspector/"),
                            Organization = "Microsoft",
                            Version = Helpers.GetVersionString()
                        }
                    }
                };
                if (!string.IsNullOrEmpty(basePath))
                {
                    if (Path.DirectorySeparatorChar == '\\')
                    {
                        basePath = basePath.Replace("\\","/");
                        if (!basePath.EndsWith("/"))
                        {
                            basePath = $"{basePath}/";
                        }

                    }

                    run.OriginalUriBaseIds = new Dictionary<string, ArtifactLocation>()
                    {

                        { "ROOT", new ArtifactLocation() { Uri = new Uri($"file://{basePath}") } }
                    };
                }

                if (Uri.TryCreate(cliAnalyzeCmdOptions.RepositoryUri, UriKind.RelativeOrAbsolute, out var uri))
                {
                    run.VersionControlProvenance = new List<VersionControlDetails>
                    {
                        new()
                        {
                            RepositoryUri = uri,
                            RevisionId = cliAnalyzeCmdOptions.CommitHash,
                            MappedTo = new ArtifactLocation()
                            {
                                UriBaseId = "ROOT"
                            }
                        }
                    };
                }
                else if (analyzeResult.Metadata.RepositoryUri is { })
                {
                    run.VersionControlProvenance = new List<VersionControlDetails>
                    {
                        new()
                        {
                            RepositoryUri = analyzeResult.Metadata.RepositoryUri,
                            RevisionId = analyzeResult.Metadata.CommitHash ?? string.Empty,
                            Branch = analyzeResult.Metadata.Branch ?? string.Empty,
                            MappedTo = new ArtifactLocation()
                            {
                                UriBaseId = "ROOT"
                            }
                        }
                    };
                }

                var artifacts = new List<Artifact>();

                var reportingDescriptors = new List<ReportingDescriptor>();
                run.Results = new List<CodeAnalysis.Sarif.Result>();
                foreach (var match in analyzeResult.Metadata.Matches)
                {
                    var sarifResult = new CodeAnalysis.Sarif.Result();

                    if (match.Rule is not null)
                    {
                        if (reportingDescriptors.All(r => r.Id != match.Rule.Id))
                        {
                            ReportingDescriptor reportingDescriptor = new()
                            {
                                FullDescription = new MultiformatMessageString { Text = match.Rule.Description },
                                Id = match.Rule.Id,
                                Name = match.Rule.Name,
                                DefaultConfiguration = new ReportingConfiguration
                                {
                                    Level = FailureLevel.Note
                                }
                            };
                            reportingDescriptor.Tags.AddRange(match.Rule.Tags);
                            reportingDescriptor.SetProperty("AppInspector:Severity", match.Rule.Severity.ToString());
                            reportingDescriptors.Add(reportingDescriptor);
                        }

                        sarifResult.Level = FailureLevel.Note;
                        sarifResult.RuleId = match.Rule.Id;
                        sarifResult.Tags.AddRange(match.Rule.Tags);
                        sarifResult.Message = new Message
                        {
                            Text = match.Rule.Description
                        };

                        if (match.FileName is not null)
                        {
                            var fileName = match.FileName;
                            if (basePath is not null)
                            {
                                fileName = Path.GetRelativePath(basePath, fileName).Replace("\\","/");
                            }

                            if (Uri.TryCreate(fileName, UriKind.RelativeOrAbsolute, out var outUri))
                            {
                                var artifactIndex = artifacts.FindIndex(a => a.Location.Uri.Equals(outUri));
                                if (artifactIndex == -1)
                                {
                                    Artifact artifact = new()
                                    {
                                        Location = new ArtifactLocation
                                        {
                                            Index = artifacts.Count,
                                            Uri = outUri
                                        }
                                    };
                                    if (basePath != null)
                                    {
                                        artifact.Location.UriBaseId = "ROOT";
                                    }
                                    artifactIndex = artifact.Location.Index;
                                    artifact.Tags.AddRange(match.Rule.Tags);
                                    if (match.LanguageInfo is { } languageInfo)
                                    {
                                        artifact.SourceLanguage = languageInfo.Name;
                                    }

                                    artifacts.Add(artifact);
                                }
                                else
                                {
                                    artifacts[artifactIndex].Tags.AddRange(match.Rule.Tags);
                                }

                                Location location = new()
                                {
                                    PhysicalLocation = new PhysicalLocation
                                    {
                                        ArtifactLocation = new ArtifactLocation
                                        {
                                            Index = artifactIndex,
                                            Uri = outUri
                                        },
                                        Region = new Region
                                        {
                                            StartLine = match.StartLocationLine,
                                            StartColumn = match.StartLocationColumn,
                                            EndLine = match.EndLocationLine,
                                            EndColumn = match.EndLocationColumn,
                                            Snippet = new ArtifactContent
                                            {
                                                Text = match.Sample
                                            }
                                        }
                                    }
                                };
                                if (basePath != null)
                                {
                                    location.PhysicalLocation.ArtifactLocation.UriBaseId = "ROOT";
                                }
                                sarifResult.SetProperty("AppInspector:Severity", match.Rule.Severity.ToString());
                                sarifResult.Locations = new List<Location>
                                {
                                    location
                                };
                            }
                        }
                    }

                    run.Artifacts = artifacts;
                    run.Tool.Driver.Rules = reportingDescriptors;
                    run.Results.Add(sarifResult);
                }

                log.Runs.Add(run);
                
                /* Sarif SDK workarounds */
                // Begin Workaround for https://github.com/microsoft/sarif-sdk/issues/2024 - results with level warning are not serialized
                // Save the sarif log to a stream
                var stream = new MemoryStream();
                log.Save(stream);
                stream.Position = 0;
                // Read the saved log back in
                var reReadLog = JObject.Parse(new StreamReader(stream).ReadToEnd());
                // Find results with levels that are not set
                var resultsWithoutLevels =
                    reReadLog.SelectTokens("$.runs[*].results[*]").Where(t => t["level"] == null).ToList();
                // For each result with no level set its level to warning
                foreach (var resultWithoutLevel in resultsWithoutLevels)
                {
                    resultWithoutLevel["level"] = "warning";
                }

                // Rules which had a default configuration of Warning will also not have the field populated
                var rulesWithoutDefaultConfiguration = reReadLog.SelectTokens("$.runs[*].tool.driver.rules[*]")
                    .Where(t => t["defaultConfiguration"] == null).ToList();
                // For each result with no default configuration option, add one with the level warning
                foreach (var rule in rulesWithoutDefaultConfiguration)
                {
                    rule["defaultConfiguration"] = new JObject { { "level", "warning" } };
                }

                // Rules with a DefaultConfiguration object, but where that object has no level also should be set
                //  ApplicationInspector should always populate this object with a level, but potentially
                var rulesWithoutDefaultConfigurationLevel = reReadLog.SelectTokens("$.runs[*].tool.driver.rules[*].defaultConfiguration")
                    .Where(t => t["level"] == null).ToList();
                // For each result with a default configuration object that has no level
                //  add a level property equal to warning
                foreach (var rule in rulesWithoutDefaultConfigurationLevel)
                {
                    rule["level"] = "warning";
                }

                // Begin Workaround for https://github.com/microsoft/sarif-sdk/issues/2662
                // The provided schema (rtm.6) is 404, so replace it with a 2.1.0 that is available.
                reReadLog["$schema"] = "https://www.schemastore.org/schemas/json/sarif-2.1.0-rtm.5.json";
                using var jsonWriter = new JsonTextWriter(TextWriter);
                reReadLog.WriteTo(jsonWriter);
                // Add a newline at the end to make logging messages cleaner
                TextWriter.WriteLine();

                // End Workarounds
                TextWriter.Flush();
                if (autoClose)
                {
                    FlushAndClose();
                }
            }
            else
            {
                throw new ArgumentException("This writer can only write Analyze results.", nameof(result));
            }
        }
        else
        {
            throw new ArgumentException("This writer requires a CLIAnalyzeCmdOptions options argument.",
                nameof(commandOptions));
        }
    }

    private static FailureLevel GetSarifFailureLevel(Severity severity)
    {
        return severity switch
        {
            Severity.BestPractice => FailureLevel.Note,
            Severity.Critical => FailureLevel.Error,
            Severity.Important => FailureLevel.Warning,
            Severity.ManualReview => FailureLevel.Note,
            Severity.Moderate => FailureLevel.Warning,
            _ => FailureLevel.Note
        };
    }
}