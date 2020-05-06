// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Microsoft.ApplicationInspector.RulesEngine;
using MultiExtractor;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.ApplicationInspector.Commands
{
    /// <summary>
    /// Options specific to analyze operation not to be confused with CLIAnalyzeCmdOptions which include CLI only args
    /// </summary>
    public class AnalyzeOptions : CommandOptions
    {
        public string SourcePath { get; set; }
        public string CustomRulesPath { get; set; }
        public bool IgnoreDefaultRules { get; set; }
        public bool AllowDupTags { get; set; }
        public string MatchDepth { get; set; } = "best";
        public string ConfidenceFilters { get; set; } = "high,medium";
        public string FilePathExclusions { get; set; } = "sample,example,test,docs,.vs,.git";
    }

    /// <summary>
    /// Result of Analyze command GetResult() operation
    /// </summary>
    public class AnalyzeResult : Result
    {
        public enum ExitCode
        {
            Success = 0,
            NoMatches = 1,
            CriticalError = Utils.ExitCode.CriticalError //ensure common value for final exit log mention
        }

        [JsonProperty(Order = 2, PropertyName = "resultCode")]
        public ExitCode ResultCode { get; set; }

        /// <summary>
        /// Analyze command result object containing scan properties
        /// </summary>
        [JsonProperty(Order = 3, PropertyName = "metaData")]
        public MetaData Metadata { get; set; }

        public AnalyzeResult()
        {
            Metadata = new MetaData("", "");//needed for serialization; replaced later
        }
    }

    /// <summary>
    /// Analyze operation for setup and processing of results from Rulesengine
    /// </summary>
    public class AnalyzeCommand
    {
        private readonly int WARN_ZIP_FILE_SIZE = 1024 * 1000 * 10;  // warning for large zip files
        private readonly int MAX_FILESIZE = 1024 * 1000 * 5;  // Skip source files larger than 5 MB and log
        private readonly int MAX_TEXT_SAMPLE_LENGTH = 200;//char bytes

        private IEnumerable<string> _srcfileList;
        private MetaDataHelper _metaDataHelper; //wrapper containing MetaData object to be assigned to result
        private RuleProcessor _rulesProcessor;

        private DateTime DateScanned { get; set; }

        private DateTime _lastUpdated;

        /// <summary>
        /// Updated dynamically to more recent file in source
        /// </summary>
        public DateTime LastUpdated
        {
            get => _lastUpdated;
            set
            {
                //find last updated file in solution
                if (_lastUpdated < value)
                {
                    _lastUpdated = value;
                }
            }
        }

        private readonly List<string> _fileExclusionList;
        private Confidence _confidence;
        private readonly AnalyzeOptions _options; //copy of incoming caller options

        public AnalyzeCommand(AnalyzeOptions opt)
        {
            _options = opt;
            _options.MatchDepth ??= "best";

            if (!string.IsNullOrEmpty(opt.FilePathExclusions))
            {
                _fileExclusionList = opt.FilePathExclusions.ToLower().Split(",").ToList<string>();
                if (_fileExclusionList != null && (_fileExclusionList.Contains("none") || _fileExclusionList.Contains("None")))
                {
                    _fileExclusionList.Clear();
                }
            }

            LastUpdated = DateTime.MinValue;
            DateScanned = DateTime.Now;

            try
            {
                _options.Log ??= Utils.SetupLogging(_options);
                WriteOnce.Log ??= _options.Log;

                ConfigureConsoleOutput();
                ConfigSourcetoScan();
                ConfigConfidenceFilters();
                ConfigRules();
            }
            catch (OpException e) //group error handling
            {
                WriteOnce.Error(e.Message);
                throw;
            }
        }

        #region configureMethods

        /// <summary>
        /// Establish console verbosity
        /// For NuGet DLL use, console is automatically muted overriding any arguments sent
        /// </summary>
        private void ConfigureConsoleOutput()
        {
            WriteOnce.SafeLog("AnalyzeCommand::ConfigureConsoleOutput", LogLevel.Trace);

            //Set console verbosity based on run context (none for DLL use) and caller arguments
            if (!Utils.CLIExecutionContext)
            {
                WriteOnce.Verbosity = WriteOnce.ConsoleVerbosity.None;
            }
            else
            {
                WriteOnce.ConsoleVerbosity verbosity = WriteOnce.ConsoleVerbosity.Medium;
                if (!Enum.TryParse(_options.ConsoleVerbosityLevel, true, out verbosity))
                {
                    throw new OpException(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_ARG_VALUE, "-x"));
                }
                else
                {
                    WriteOnce.Verbosity = verbosity;
                }
            }
        }

        /// <summary>
        /// Expects user to supply all that apply impacting which rule pattern matches are returned
        /// </summary>
        private void ConfigConfidenceFilters()
        {
            WriteOnce.SafeLog("AnalyzeCommand::ConfigConfidenceFilters", LogLevel.Trace);
            //parse and verify confidence values
            if (String.IsNullOrEmpty(_options.ConfidenceFilters))
            {
                _confidence = Confidence.High | Confidence.Medium; //excludes low by default
            }
            else
            {
                string[] confidences = _options.ConfidenceFilters.Split(',');
                foreach (string confidence in confidences)
                {
                    Confidence single;
                    if (Enum.TryParse(confidence, true, out single))
                    {
                        _confidence |= single;
                    }
                    else
                    {
                        throw new OpException(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_ARG_VALUE, "x"));
                    }
                }
            }
        }

        /// <summary>
        /// Simple validation on source path provided for scanning and preparation
        /// </summary>
        private void ConfigSourcetoScan()
        {
            WriteOnce.SafeLog("AnalyzeCommand::ConfigSourcetoScan", LogLevel.Trace);

            if (String.IsNullOrEmpty(_options.SourcePath))
            {
                throw new OpException(MsgHelp.FormatString(MsgHelp.ID.CMD_REQUIRED_ARG_MISSING, "SourcePath"));
            }

            if (Directory.Exists(_options.SourcePath))
            {
                try
                {
                    _srcfileList = Directory.EnumerateFiles(_options.SourcePath, "*.*", SearchOption.AllDirectories);
                    if (_srcfileList.Count() == 0)
                    {
                        throw new OpException(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_FILE_OR_DIR, _options.SourcePath));
                    }
                }
                catch (Exception)
                {
                    throw new OpException(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_FILE_OR_DIR, _options.SourcePath));
                }
            }
            else if (File.Exists(_options.SourcePath)) //not a directory but make one for single flow
            {
                _srcfileList = new List<string>() { _options.SourcePath };
            }
            else
            {
                throw new OpException(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_FILE_OR_DIR, _options.SourcePath));
            }
        }

        /// <summary>
        /// Add default and/or custom rules paths
        /// Iterate paths and add to ruleset
        /// </summary>
        private void ConfigRules()
        {
            WriteOnce.SafeLog("AnalyzeCommand::ConfigRules", LogLevel.Trace);

            RuleSet rulesSet = null;
            List<string> rulePaths = new List<string>();

            if (!_options.IgnoreDefaultRules)
            {
                rulePaths.Add(Utils.GetPath(Utils.AppPath.defaultRulesPackedFile));
                rulesSet = Utils.GetDefaultRuleSet(_options.Log);
            }

            if (!string.IsNullOrEmpty(_options.CustomRulesPath))
            {
                if (rulesSet == null)
                {
                    rulesSet = new RuleSet(_options.Log);
                }

                rulePaths.Add(_options.CustomRulesPath);

                if (Directory.Exists(_options.CustomRulesPath))
                {
                    rulesSet.AddDirectory(_options.CustomRulesPath);
                }
                else if (File.Exists(_options.CustomRulesPath))
                {
                    rulesSet.AddFile(_options.CustomRulesPath);
                }
                else
                {
                    throw new OpException(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_RULE_PATH, _options.CustomRulesPath));
                }
            }

            //error check based on ruleset not path enumeration
            if (rulesSet == null || rulesSet.Count() == 0)
            {
                throw new OpException(MsgHelp.GetString(MsgHelp.ID.CMD_NORULES_SPECIFIED));
            }

            //instantiate a RuleProcessor with the added rules and exception for dependency
            _rulesProcessor = new RuleProcessor(rulesSet, _confidence, !_options.AllowDupTags, _options.MatchDepth == "first", _options.Log);

            //create metadata helper to wrap and help populate metadata from scan
            _metaDataHelper = new MetaDataHelper(_options.SourcePath, !_options.AllowDupTags);
        }

        #endregion configureMethods

        /// <summary>
        /// Main entry point to start analysis from CLI; handles setting up rules, directory enumeration
        /// file type detection and handoff
        /// Pre: All Configure Methods have been called already and we are ready to SCAN
        /// </summary>
        /// <returns></returns>
        public AnalyzeResult GetResult()
        {
            WriteOnce.SafeLog("AnalyzeCommand::Run", LogLevel.Trace);

            WriteOnce.Operation(MsgHelp.FormatString(MsgHelp.ID.CMD_RUNNING, "Analyze"));
            AnalyzeResult analyzeResult = new AnalyzeResult()
            {
                AppVersion = Utils.GetVersionString()
            };

            try
            {
                _metaDataHelper.Metadata.TotalFiles = _srcfileList.Count();//updated for zipped files later

                // Iterate through all files and process against rules
                foreach (string filename in _srcfileList)
                {
                    ArchiveFileType archiveFileType = ArchiveFileType.UNKNOWN;
                    try //fix for #146
                    {
                        archiveFileType = MiniMagic.DetectFileType(filename);
                    }
                    catch (Exception e)
                    {
                        WriteOnce.SafeLog(e.Message + "\n" + e.StackTrace, LogLevel.Error);//log details
                        Exception f = new Exception(MsgHelp.FormatString(MsgHelp.ID.ANALYZE_FILE_TYPE_OPEN, filename));//report friendly version
                        throw f;
                    }

                    if (archiveFileType == ArchiveFileType.UNKNOWN)//not a known zipped file type
                    {
                        ProcessAsFile(filename);
                    }
                    else
                    {
                        UnZipAndProcess(filename, archiveFileType, _srcfileList.Count() == 1);
                    }
                }

                WriteOnce.General("\r" + MsgHelp.FormatString(MsgHelp.ID.ANALYZE_FILES_PROCESSED_PCNT, 100));

                //wrapup result status
                if (_metaDataHelper.Metadata.TotalFiles == _metaDataHelper.Metadata.FilesSkipped)
                {
                    WriteOnce.Error(MsgHelp.GetString(MsgHelp.ID.ANALYZE_NOSUPPORTED_FILETYPES));
                    analyzeResult.ResultCode = AnalyzeResult.ExitCode.NoMatches;
                }
                else if (_metaDataHelper.Metadata.Matches.Count == 0)
                {
                    WriteOnce.Error(MsgHelp.GetString(MsgHelp.ID.ANALYZE_NOPATTERNS));
                    analyzeResult.ResultCode = AnalyzeResult.ExitCode.NoMatches;
                }
                else
                {
                    _metaDataHelper.Metadata.LastUpdated = LastUpdated.ToString();
                    _metaDataHelper.Metadata.DateScanned = DateScanned.ToString();
                    analyzeResult.Metadata = _metaDataHelper.Metadata; //replace instance with metadatahelper processed one
                    analyzeResult.ResultCode = AnalyzeResult.ExitCode.Success;
                }
            }
            catch (OpException e)
            {
                WriteOnce.Error(e.Message);
                //caught for CLI callers with final exit msg about checking log or throws for DLL callers
                throw;
            }

            return analyzeResult;
        }

        /// <summary>
        /// Wrapper for files that are on disk and ready to read vs unzipped files which are not to allow separation of core
        /// scan evaluation for use by decompression methods as well
        /// </summary>
        /// <param name="filename"></param>
        private void ProcessAsFile(string filename)
        {
            //check for supported language
            LanguageInfo languageInfo = new LanguageInfo();
            if (FileChecksPassed(filename, ref languageInfo))
            {
                LastUpdated = File.GetLastWriteTime(filename);
                _metaDataHelper.Metadata.PackageTypes.Add(MsgHelp.GetString(MsgHelp.ID.ANALYZE_UNCOMPRESSED_FILETYPE));

                string fileText = File.ReadAllText(filename);
                ProcessInMemory(filename, fileText, languageInfo);
            }
        }

        /// <summary>
        /// Main WORKHORSE for analyzing file; called from file based or decompression functions
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="fileText"></param>
        private void ProcessInMemory(string filePath, string fileText, LanguageInfo languageInfo)
        {
            #region minorRollupTrackingAndProgress

            WriteOnce.SafeLog("Preparing to process file: " + filePath, LogLevel.Trace);

            _metaDataHelper.Metadata.FilesAnalyzed++;

            int totalFilesReviewed = _metaDataHelper.Metadata.FilesAnalyzed + _metaDataHelper.Metadata.FilesSkipped;
            int percentCompleted = (int)((float)totalFilesReviewed / (float)_metaDataHelper.Metadata.TotalFiles * 100);
            //earlier issue now resolved so app handles mixed zipped/zipped and unzipped/zipped directories but catch all for non-critical UI
            if (percentCompleted > 100)
            {
                percentCompleted = 100;
            }

            if (percentCompleted < 100) //caller already reports @100% so avoid 2x for file output
            {
                WriteOnce.General("\r" + MsgHelp.FormatString(MsgHelp.ID.ANALYZE_FILES_PROCESSED_PCNT, percentCompleted), false);
            }

            #endregion minorRollupTrackingAndProgress

            //process file against rules returning unique or duplicate matches as configured
            ScanResult[] scanResults = _rulesProcessor.Analyze(fileText, languageInfo);

            //if any matches found for this file...
            if (scanResults.Count() > 0)
            {
                _metaDataHelper.Metadata.FilesAffected++;
                _metaDataHelper.Metadata.TotalMatchesCount += scanResults.Count();

                // Iterate through each match issue
                foreach (ScanResult scanResult in scanResults)
                {
                    WriteOnce.SafeLog(string.Format("Processing pattern matches for ruleId {0}, ruleName {1} file {2}", scanResult.Rule.Id, scanResult.Rule.Name, filePath), LogLevel.Trace);

                    //save all unique dependencies even if Dependency tag pattern is not-unique
                    var tagPatternRegex = new Regex("Dependency.SourceInclude", RegexOptions.IgnoreCase);
                    String textMatch = string.Empty;

                    if (scanResult.Rule.Tags.Any(v => tagPatternRegex.IsMatch(v)))
                    {
                        textMatch = ExtractDependency(fileText, scanResult.Boundary.Index, scanResult.PatternMatch, languageInfo.Name);
                    }
                    else
                    {
                        textMatch = ExtractTextSample(fileText, scanResult.Boundary.Index, scanResult.Boundary.Length);
                    }

                    MatchRecord matchRecord = new MatchRecord()
                    {
                        FileName = filePath,
                        Language = languageInfo,
                        StartLocationLine = scanResult.StartLocation.Line,
                        StartLocationColumn = scanResult.StartLocation.Column,
                        EndLocationLine = scanResult.EndLocation.Line,
                        EndLocationColumn = scanResult.EndLocation.Column,
                        RuleId = scanResult.Rule.Id,
                        Severity = scanResult.Rule.Severity.ToString(),
                        RuleName = scanResult.Rule.Name,
                        RuleDescription = scanResult.Rule.Description,
                        PatternConfidence = scanResult.Confidence.ToString(),
                        PatternType = scanResult.PatternMatch.PatternType.ToString(),
                        MatchingPattern = scanResult.PatternMatch.Pattern,
                        Sample = textMatch,
                        Excerpt = ExtractExcerpt(fileText, scanResult.StartLocation.Line),
                        Tags = scanResult.Rule.Tags
                    };

                    //preserve issue level characteristics as rolled up meta data of interest
                    _metaDataHelper.AddMatchRecord(matchRecord);
                }
            }
            else
            {
                WriteOnce.SafeLog("No pattern matches detected for file: " + filePath, LogLevel.Trace);
            }
        }

        #region ProcessingAssist

        /// <summary>
        /// Simple wrapper but keeps calling code consistent
        /// Do not html code result which is accomplished later before out put to report
        /// </summary>
        private string ExtractTextSample(string fileText, int index, int length)
        {
            string result = "";
            try
            {
                //some js file results may be too long for practical display
                if (length > MAX_TEXT_SAMPLE_LENGTH)
                {
                    length = MAX_TEXT_SAMPLE_LENGTH;
                }

                result = fileText.Substring(index, length).Trim();
            }
            catch (Exception)
            {
                //control the error description and continue; error in rules engine possible
                WriteOnce.SafeLog("Unexpected indexing issue in ExtractTextSample.  Process continued", LogLevel.Error);
            }

            return result;
        }

        /// <summary>
        /// Located here to include during Match creation to avoid a call later or putting in constructor
        /// Needed in match ensuring value exists at time of report writing rather than expecting a callback
        /// from the template
        /// </summary>
        /// <returns></returns>
        private string ExtractExcerpt(string text, int startLineNumber, int length = 10)
        {
            if (String.IsNullOrEmpty(text))
            {
                return "";
            }

            var lines = text.Split('\n');
            var distance = (int)((length - 1.0) / 2.0);

            // Sanity check
            if (startLineNumber < 0)
            {
                startLineNumber = 0;
            }

            if (startLineNumber >= lines.Length)
            {
                startLineNumber = lines.Length - 1;
            }

            var excerptStartLine = Math.Max(0, startLineNumber - distance);
            var excerptEndLine = Math.Min(lines.Length - 1, startLineNumber + distance);

            /* If the code snippet we're viewing is already indented 16 characters minimum,
             * we don't want to show all that extra white-space, so we'll find the smallest
             * number of spaces at the beginning of each line and use that.
             */

            var minSpaces = -1;
            for (var i = excerptStartLine; i <= excerptEndLine; i++)
            {
                var numPrefixSpaces = lines[i].TakeWhile(c => c == ' ').Count();
                minSpaces = (minSpaces == -1 || numPrefixSpaces < minSpaces) ? numPrefixSpaces : minSpaces;
            }

            var sb = new StringBuilder();
            // We want to go from (start - 5) to (start + 5) (off by one?)
            // LINE=10, len=5, we want 8..12, so N-(L-1)/2 to N+(L-1)/2
            // But cap those values at 0/end
            for (var i = excerptStartLine; i <= excerptEndLine; i++)
            {
                string line = lines[i].Substring(minSpaces).TrimEnd();
                sb.AppendLine(line);

            }

            return System.Net.WebUtility.HtmlEncode(sb.ToString());
        }

        /// <summary>
        /// Helper to special case additional processing to just get the values without the import keywords etc.
        /// and encode for html output
        /// </summary>
        private string ExtractDependency(string text, int startIndex, SearchPattern pattern, string language)
        {
            // import value; load value; include value;
            string rawResult = "";
            int endIndex = text.IndexOf('\n', startIndex);
            if (-1 != startIndex && -1 != endIndex)
            {
                rawResult = text.Substring(startIndex, endIndex - startIndex).Trim();

                //recreate regex used to find entire value
                Regex regex = new Regex(pattern.Pattern);
                MatchCollection matches = regex.Matches(rawResult);

                //remove surrounding import or trailing comments
                if (matches.Count > 0)
                {
                    foreach (Match match in matches)
                    {
                        if (match.Groups.Count == 1)//handles cases like "using Newtonsoft.Json"
                        {
                            string[] parseValues = match.Groups[0].Value.Split(' ');
                            if (parseValues.Length == 1)
                            {
                                rawResult = parseValues[0].Trim();
                            }
                            else if (parseValues.Length > 1)
                            {
                                rawResult = parseValues[1].Trim(); //should be value; time will tell if fullproof
                            }
                        }
                        else if (match.Groups.Count > 1)//handles cases like include <stdio.h>
                        {
                            rawResult = match.Groups[1].Value.Trim();
                        }
                        //else if > 2 too hard to match; do nothing

                        break;//only designed to expect one match per line i.e. not include value include value
                    }
                }

                String finalResult = rawResult.Replace(";", "");
                _metaDataHelper.Metadata.UniqueDependencies.Add(finalResult);

                return System.Net.WebUtility.HtmlEncode(finalResult);
            }

            return rawResult;
        }

        #endregion ProcessingAssist

        private void UnZipAndProcess(string filename, ArchiveFileType archiveFileType, bool topLevel = true)
        {
            // zip itself may be in excluded list i.e. sample, test or similar unless ignore filter requested
            if (_fileExclusionList != null && _fileExclusionList.Any(v => filename.ToLower().Contains(v)))
            {
                WriteOnce.SafeLog(MsgHelp.FormatString(MsgHelp.ID.ANALYZE_EXCLUDED_TYPE_SKIPPED, filename), LogLevel.Warn);
                _metaDataHelper.Metadata.FilesSkipped++;
                return;
            }

            //zip itself may be too huge for timely processing
            if (new FileInfo(filename).Length > WARN_ZIP_FILE_SIZE)
            {
                if (topLevel)
                {
                    WriteOnce.General(MsgHelp.GetString(MsgHelp.ID.ANALYZE_COMPRESSED_FILESIZE_WARN));
                }
                else
                {
                    WriteOnce.SafeLog("Decompressing large file " + filename, LogLevel.Warn);
                }
            }
            else
            {
                if (topLevel)
                {
                    WriteOnce.General(MsgHelp.GetString(MsgHelp.ID.ANALYZE_COMPRESSED_PROCESSING));
                }
                else
                {
                    WriteOnce.SafeLog("Decompressing file " + filename, LogLevel.Warn);
                }
            }

            LastUpdated = File.GetLastWriteTime(filename);
            _metaDataHelper.Metadata.PackageTypes.Add(MsgHelp.GetString(MsgHelp.ID.ANALYZE_COMPRESSED_FILETYPE));

            try
            {
                IEnumerable<FileEntry> files = Extractor.ExtractFile(filename);

                if (files.Count() > 0)
                {
                    _metaDataHelper.Metadata.TotalFiles += files.Count();//additive in case additional child zip files processed

                    foreach (FileEntry file in files)
                    {
                        //check uncompressed file passes standard checks
                        LanguageInfo languageInfo = new LanguageInfo();
                        if (FileChecksPassed(file.FullPath, ref languageInfo, file.Content.Length))
                        {
                            byte[] streamByteArray = file.Content.ToArray();
                            ProcessInMemory(file.FullPath, Encoding.UTF8.GetString(streamByteArray, 0, streamByteArray.Length), languageInfo);
                        }
                    }
                }
                else
                {
                    WriteOnce.SafeLog(string.Format("Decompression found no files in {0}", filename), LogLevel.Warn);//zero results can be valid
                }
            }
            catch (Exception)
            {
                string errmsg = MsgHelp.FormatString(MsgHelp.ID.ANALYZE_COMPRESSED_ERROR, filename);
                WriteOnce.Error(errmsg);
                throw;
            }
        }

        /// <summary>
        /// Common validation called by ProcessAsFile and UnzipAndProcess to ensure same order and checks made
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="languageInfo"></param>
        /// <param name="fileLength">should be > zero if called from unzip method</param>
        /// <returns></returns>
        private bool FileChecksPassed(string filePath, ref LanguageInfo languageInfo, long fileLength = 0)
        {
            _metaDataHelper.Metadata.FileExtensions.Add(Path.GetExtension(filePath).Replace('.', ' ').TrimStart());

            // 1. Skip files written in unknown language
            if (!Language.FromFileName(filePath, ref languageInfo))
            {
                WriteOnce.SafeLog(MsgHelp.FormatString(MsgHelp.ID.ANALYZE_LANGUAGE_NOTFOUND, filePath), LogLevel.Warn);
                _metaDataHelper.Metadata.FilesSkipped++;
                return false;
            }

            _metaDataHelper.AddLanguage(languageInfo.Name);

            // 2. Skip excluded files i.e. sample, test or similar unless ignore filter requested
            if (_fileExclusionList != null && _fileExclusionList.Any(v => filePath.ToLower().Contains(v)))
            {
                WriteOnce.SafeLog(MsgHelp.FormatString(MsgHelp.ID.ANALYZE_EXCLUDED_TYPE_SKIPPED, filePath), LogLevel.Warn);
                _metaDataHelper.Metadata.FilesSkipped++;
                return false;
            }

            // 3. Skip if exceeds file size limits
            try
            {
                fileLength = fileLength <= 0 ? new FileInfo(filePath).Length : fileLength;
                if (fileLength > MAX_FILESIZE)
                {
                    WriteOnce.SafeLog(MsgHelp.FormatString(MsgHelp.ID.ANALYZE_FILESIZE_SKIPPED, filePath), LogLevel.Warn);
                    _metaDataHelper.Metadata.FilesSkipped++;
                    return false;
                }
            }
            catch (Exception)
            {
                WriteOnce.Error(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_FILE_OR_DIR, filePath));
                throw;
            }

            return true;
        }
    }
}