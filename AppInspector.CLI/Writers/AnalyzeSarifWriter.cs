// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Microsoft.ApplicationInspector.CLI
{
    using Microsoft.ApplicationInspector.Commands;
    using Microsoft.ApplicationInspector.RulesEngine;
    using Microsoft.CodeAnalysis.Sarif;
    using Microsoft.CST.OAT.Utils;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Location = Microsoft.CodeAnalysis.Sarif.Location;
    using Result = Microsoft.ApplicationInspector.Commands.Result;

    internal static class AnalyzeSarifWriterExtensions
    {
        internal static void AddRange(this TagsCollection tc, IEnumerable<string>? tagsToAdd)
        {
            if (tagsToAdd is null) return;
            foreach (string tag in tagsToAdd)
            {
                tc.Add(tag);
            }
        }
    }
    /// <summary>
    /// Writes in sarif format
    /// </summary>
    public class AnalyzeSarifWriter : CommandResultsWriter
    {
        public AnalyzeSarifWriter()
        {

        }

        public override void WriteResults(Result result, CLICommandOptions commandOptions, bool autoClose = true)
        {
            if (TextWriter is null)
            {
                throw new NullReferenceException(nameof(TextWriter));
            }
            string? basePath = null;
            if (commandOptions is CLIAnalyzeCmdOptions cLIAnalyzeCmdOptions)
            {
                basePath = cLIAnalyzeCmdOptions.BasePath;

                if (result is AnalyzeResult analyzeResult)
                {
                    SarifLog log = new();
                    SarifVersion sarifVersion = SarifVersion.Current;
                    log.SchemaUri = sarifVersion.ConvertToSchemaUri();
                    log.Version = sarifVersion;
                    log.Runs = new List<Run>();
                    var run = new Run();

                    if (Uri.TryCreate(cLIAnalyzeCmdOptions.RepositoryUri, UriKind.RelativeOrAbsolute, out Uri? uri))
                    {
                        run.VersionControlProvenance = new List<VersionControlDetails>()
                        {
                            new VersionControlDetails()
                            {
                                RepositoryUri = uri,
                                RevisionId = cLIAnalyzeCmdOptions.CommitHash
                            }
                        };
                    }
                    
                    var artifacts = new List<Artifact>();
                    run.Tool = new Tool
                    {
                        Driver = new ToolComponent
                        {
                            Name = $"Application Inspector",
                            InformationUri = new Uri("https://github.com/microsoft/ApplicationInspector/"),
                            Organization = "Microsoft",
                            Version = Helpers.GetVersionString(),
                        }
                    };
                    var reportingDescriptors = new List<ReportingDescriptor>();
                    run.Results = new List<CodeAnalysis.Sarif.Result>();
                    foreach (var match in analyzeResult.Metadata.Matches)
                    {
                        var sarifResult = new CodeAnalysis.Sarif.Result();

                        if (match.Rule is not null)
                        {
                            if (!reportingDescriptors.Any(r => r.Id == match.Rule.Id))
                            {
                                ReportingDescriptor reportingDescriptor = new()
                                {
                                    FullDescription = new MultiformatMessageString() { Text = match.Rule.Description },
                                    Id = match.Rule.Id,
                                    Name = match.Rule.Name,
                                    DefaultConfiguration = new ReportingConfiguration()
                                    {
                                        Level = GetSarifFailureLevel(match.Rule.Severity)
                                    }
                                };
                                reportingDescriptor.Tags.AddRange(match.Rule.Tags);
                                reportingDescriptors.Add(reportingDescriptor);
                            }

                            sarifResult.Level = GetSarifFailureLevel(match.Rule.Severity);
                            sarifResult.RuleId = match.Rule.Id;
                            sarifResult.Tags.AddRange(match.Rule.Tags);
                            sarifResult.Message = new Message()
                            {
                                Text = match.Rule.Description
                            };

                            if (match.FileName is not null)
                            {
                                string fileName = match.FileName;
                                if (basePath is not null)
                                {
                                    fileName = Path.GetRelativePath(basePath, fileName);
                                }
                                if (Uri.TryCreate(fileName, UriKind.RelativeOrAbsolute, out Uri? outUri))
                                {
                                    int artifactIndex = artifacts.FindIndex(a => a.Location.Uri.Equals(outUri));
                                    if (artifactIndex == -1)
                                    {
                                        Artifact artifact = new()
                                        {
                                            Location = new ArtifactLocation()
                                            {
                                                Index = artifacts.Count,
                                                Uri = outUri
                                            },
                                        };
                                        artifactIndex = artifact.Location.Index;
                                        artifact.Tags.AddRange(match.Rule.Tags);
                                        if (Language.FromFileNameOut(fileName, out LanguageInfo languageInfo))
                                        {
                                            artifact.SourceLanguage = languageInfo.Name;
                                        }
                                        artifacts.Add(artifact);
                                    }
                                    else
                                    {
                                        artifacts[artifactIndex].Tags.AddRange(match.Rule.Tags);
                                    }
                                    sarifResult.Locations = new List<Location>()
                                {
                                    new Location()
                                    {
                                        PhysicalLocation = new PhysicalLocation()
                                        {
                                            ArtifactLocation = new ArtifactLocation()
                                            {
                                                Index = artifactIndex
                                            },
                                            Region = new Region()
                                            {
                                                StartLine = match.StartLocationLine,
                                                StartColumn = match.StartLocationColumn,
                                                EndLine = match.EndLocationLine,
                                                EndColumn = match.EndLocationColumn,
                                                Snippet = new ArtifactContent()
                                                {
                                                    Text = match.Sample
                                                }
                                            }
                                        }
                                    }
                                };
                                }
                            }
                        }

                        run.Artifacts = artifacts;
                        run.Tool.Driver.Rules = reportingDescriptors;
                        run.Results.Add(sarifResult);
                    }

                    log.Runs.Add(run);
                    JsonSerializerSettings serializerSettings = new();
                    var serializer = new JsonSerializer();
                    serializer.Serialize(TextWriter, log);
                    FlushAndClose();
                }
                else
                {
                    throw new ArgumentException("This writer can only write Analyze results.", nameof(result));
                }
            }
            else
            {
                throw new ArgumentException("This writer requires a CLIAnalyzeCmdOptions options argument.", nameof(commandOptions));
            }
        }

        private static FailureLevel GetSarifFailureLevel(RulesEngine.Severity severity) => severity switch
        {
            RulesEngine.Severity.BestPractice => FailureLevel.Note,
            RulesEngine.Severity.Critical => FailureLevel.Error,
            RulesEngine.Severity.Important => FailureLevel.Warning,
            RulesEngine.Severity.ManualReview => FailureLevel.Note,
            RulesEngine.Severity.Moderate => FailureLevel.Warning,
            _ => FailureLevel.Note
        };
    }
}