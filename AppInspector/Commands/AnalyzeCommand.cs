// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.Zip;
using MimeTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.SharpZipLib.Core;
using Microsoft.AppInspector.CLI.Writers;
using Microsoft.DevSkim;
using System.Text.RegularExpressions; 
using System.Text;
using Newtonsoft.Json;
using System.Diagnostics;


namespace Microsoft.AppInspector.CLI.Commands
{
    public class AnalyzeCommand : ICommand
    {
        readonly int MAX_FILESIZE = 1024 * 1000 * 5;  // Do not analyze files larger than 5 MB

        // Enable processing compressed files
        readonly string[] COMPRESSED_EXTENSIONS = "zip,gz,gzip,gem,tar,tgz,tar.gz,xz,7z".Split(",");
        Regex IgnoreMimeRegex;

        public enum ExitCode
        {
            NoMatches = 0,
            MatchesFound = 1,
            CriticalError = 2
        }

        AppProfile _appProfile;
        RuleProcessor _rulesProcessor;
        Writer _outputWriter;

        DateTime DateScanned { get; set; }
        DateTime _lastUpdated;

        public DateTime LastUpdated
        {
            get { return _lastUpdated;  }
            set
            {
                //find last updated file in solution
                if (_lastUpdated < value)
                    _lastUpdated = value;
            }
        }

        //cmdline arguments
        private string _arg_sourcePath;
        private string _arg_outputFile;
        private string _arg_fileFormat;
        private string _arg_outputTextFormat;
        private string _arg_customRulePath;
        private bool _arg_ignoreDefaultRules;
        private bool _arg_outputUniqueTagsOnly;
        private string _arg_confidenceFilters;
        private bool _arg_simpleTagsOnly;
        private Confidence _arg_confidence;
        private WriteOnce.ConsoleVerbosityLevel _arg_consoleVerbosityLevel;
        
        public AnalyzeCommand(AnalyzeCommandOptions opts)
        {
            _arg_sourcePath = opts.SourcePath;
            _arg_outputFile = opts.OutputFilePath;
            _arg_fileFormat = opts.OutputFileFormat;
            _arg_outputTextFormat = opts.TextOutputFormat;
            _arg_outputUniqueTagsOnly = opts.UniqueTagsOnly;
            _arg_customRulePath = opts.CustomRulesPath;
            _arg_confidenceFilters = opts.ConfidenceFilters;
            _arg_ignoreDefaultRules = opts.IgnoreDefaultRules;
            _arg_simpleTagsOnly = opts.SimpleTagsOnly;
            Enum.TryParse(opts.ConsoleVerbosityLevel, true, out _arg_consoleVerbosityLevel);
            WriteOnce.Verbosity = _arg_consoleVerbosityLevel;

            IgnoreMimeRegex = new Regex(@"^(audio|video)/.*$");

            //quick validations and setup
            if (!Directory.Exists(_arg_sourcePath) && !File.Exists(_arg_sourcePath))
            {
                string errorMsg = string.Format("Invalid source file or directory{0}", _arg_sourcePath);
                WriteOnce.Error(errorMsg);
                throw new Exception(errorMsg);
            }

            LastUpdated = DateTime.MinValue;
            DateScanned = DateTime.Now;

            ConfigureConfidenceFilters();
            ConfigRules();
            ConfigOutput();
        }


        #region configureMethods

        /// <summary>
        /// Expects user to supply all that apply
        /// </summary>
        void ConfigureConfidenceFilters()
        {
            //parse and verify confidence values
            if (String.IsNullOrEmpty(_arg_confidenceFilters))
                _arg_confidence = Confidence.High | Confidence.Medium; //excludes low by default
            else
            {
                string[] confidences = _arg_confidenceFilters.Split(',');
                foreach (string confidence in confidences)
                {
                    Confidence single;
                    if (Enum.TryParse(confidence, true, out single))
                        _arg_confidence |= single;
                    else
                        throw new Exception("Invalid run argument value for -x");
                }
            }
        }




        /// <summary>
        /// Add default and/or custom rules paths
        /// Iterate paths and add to ruleset
        /// </summary>
        void ConfigRules()
        {
            RuleSet rulesSet = new RuleSet(Program.Logger);
            List<string> rulePaths = new List<string>();
            if (!_arg_ignoreDefaultRules)
                rulePaths.Add(Helper.GetPath(Helper.AppPath.defaultRules));

            if (!string.IsNullOrEmpty(_arg_customRulePath))
                rulePaths.Add(_arg_customRulePath);

            foreach (string rulePath in rulePaths)
            {
                if (Directory.Exists(rulePath))
                    rulesSet.AddDirectory(rulePath);
                else
                    rulesSet.AddFile(rulePath);
            }

            //error check based on ruleset not path enumeration
            if (rulesSet.Count() == 0)
            {
                WriteOnce.Error("No rules specified");
                throw new Exception("No rules specified");
            }

            //instantiate a RuleProcessor with the added rules and exception for dependency
            _rulesProcessor = new RuleProcessor(rulesSet, _arg_confidence, _arg_outputUniqueTagsOnly, _arg_simpleTagsOnly, Program.Logger);
            
            if (_arg_outputUniqueTagsOnly)
            {
                List<TagException> tagExceptions = JsonConvert.DeserializeObject<List<TagException>>(File.ReadAllText(Helper.GetPath(Helper.AppPath.tagCounterPref)));
                string[] exceptions = new string[tagExceptions.Count];
                for (int i=0;i<tagExceptions.Count;i++)
                    exceptions[i] = tagExceptions[i].Tag;
                _rulesProcessor.UniqueTagExceptions = exceptions;
            }

            _appProfile = new AppProfile(_arg_sourcePath, rulePaths, false, _arg_simpleTagsOnly, _arg_outputUniqueTagsOnly);
            _appProfile.Args = "analyze -f " + _arg_fileFormat + " -u " + _arg_outputUniqueTagsOnly.ToString().ToLower() + " -v " +
                WriteOnce.Verbosity.ToString() + " -x " + _arg_confidence + " -i " + _arg_ignoreDefaultRules.ToString().ToLower();
    }

     
        void ConfigOutput()
        {
            //Set output type, format and outstream
            _outputWriter = WriterFactory.GetWriter(_arg_fileFormat ?? "text", (string.IsNullOrEmpty(_arg_outputFile)) ? null : "text", _arg_outputTextFormat);
            if (!string.IsNullOrEmpty(_arg_outputFile))
            {
                 if (_arg_fileFormat != "html")
                    _outputWriter.TextWriter = File.CreateText(_arg_outputFile);//not needed if html output since application controlled
            }
            else
                _outputWriter.TextWriter = Console.Out;
        }


        #endregion


        /// <summary>
        /// Main entry point to start analysis; handles setting up rules, directory enumeration
        /// file type detection and handoff
        /// </summary>
        /// <returns></returns>
        public int Run()
        {
            DateTime start = DateTime.Now;
            
            WriteOnce.Write("Analyze command running\n", ConsoleColor.Cyan, WriteOnce.ConsoleVerbosityLevel.Low);

            //if it's a file, make an IEnumerable out of it.
            IEnumerable<string> fileList;
            if (!Directory.Exists(_arg_sourcePath))
                fileList = new List<string>() { _arg_sourcePath };
            else
                fileList = Directory.EnumerateFiles(_arg_sourcePath, "*.*", SearchOption.AllDirectories);

            _appProfile.MetaData.TotalFiles = fileList.Count();

            // Iterate through all files and process against rules
            foreach (string filename in fileList)
            {
                var fileExtension = new FileInfo(filename).Extension;
                if (COMPRESSED_EXTENSIONS.Any(fileExtension.Contains))
                    ExpandAndProcess(filename);
                else
                    ProcessAsFile(filename);

                //progress report
                int totalFilesReviewed = _appProfile.MetaData.FilesAnalyzed + _appProfile.MetaData.FilesSkipped;
                int percentCompleted = (int)((float)totalFilesReviewed / (float)_appProfile.MetaData.TotalFiles * 100);
                WriteOnce.Write(string.Format("\r{0}% source files processed", percentCompleted), ConsoleColor.Gray, WriteOnce.ConsoleVerbosityLevel.Medium);
            }

            //prepare summary report
            WriteOnce.Write(string.Format("\r100% applicable files processed\t\t\t\t"), ConsoleColor.Gray, WriteOnce.ConsoleVerbosityLevel.Medium);
            WriteOnce.NewLine();
            WriteOnce.Info("Preparing report...");

            _appProfile.MetaData.LastUpdated = LastUpdated.ToString();
            _appProfile.DateScanned = DateScanned.ToString();
            _appProfile.PrepareReport();
            //close output files
            FlushAll();

            if (_appProfile.MetaData.TotalFiles == _appProfile.MetaData.FilesSkipped)
                WriteOnce.Error("No file types found in source path that are supported.");
            else if (_appProfile.MatchList.Count == 0)
                WriteOnce.Error("No pattern matches were detected for files in source path.");
            else          
                WriteOnce.Any("Report complete.", ConsoleColor.Cyan);

            TimeSpan timeSpan = start - DateTime.Now;
            Program.Logger.Trace(String.Format("Processing time: seconds:{0}", timeSpan.TotalSeconds*-1));

            return _appProfile.MatchList.Count() == 0 ? (int)ExitCode.NoMatches : 
                (int)ExitCode.MatchesFound;
        }



        /// <summary>
        /// Wrapper for files that are on disk and ready to read
        /// </summary>
        /// <param name="filename"></param>
        void ProcessAsFile(string filename)
        {
            _appProfile.MetaData.FileNames.Add(filename);
            _appProfile.MetaData.PackageTypes.Add("uncompressed");

            if (new System.IO.FileInfo(filename).Length > MAX_FILESIZE)
            {
                Program.Logger.Error(filename + " is too large.  File skipped");
                _appProfile.MetaData.FilesSkipped++;
                return;
            }

            string fileText = File.ReadAllText(filename);
            ProcessInMemory(filename, fileText);
        }


        /// <summary>
        /// Main WORKHORSE for analyzing file; called directly from decompression functions
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="fileText"></param>
        void ProcessInMemory(string filePath, string fileText)
        {
            if (fileText.Length > MAX_FILESIZE)
            {
                Program.Logger.Error(filePath + " is too large.  File skipped");
                _appProfile.MetaData.FilesSkipped++;
                return;
            }

            //determine if file is a compressed item to unpackage for processing
            string language = Language.FromFileName(filePath);

            // Skip files written in unknown language
            if (string.IsNullOrEmpty(language))
            {
                Program.Logger.Trace("Language not found for file: " + filePath);
                Program.Logger.Trace("But processing anyway: " + filePath);
                language = Path.GetFileName(filePath);
                _appProfile.MetaData.FilesSkipped++;
                return;
            }
            else
            {
                Program.Logger.Trace("Preparing to process file: " + filePath);
            }

            #region minorRollupTracking

            _appProfile.MetaData.FilesAnalyzed++;
            _appProfile.MetaData.AddLanguage(language);
            _appProfile.MetaData.FileExtensions.Add(Path.GetExtension(filePath).Replace('.', ' ').TrimStart());
            LastUpdated = File.GetLastWriteTime(filePath);

            #endregion

            //process file against rules
            Issue[] matches = _rulesProcessor.Analyze(fileText, language);

            //if any matches found for this file...
            if (matches.Count() > 0)
            {              
                _appProfile.MetaData.FilesAffected++;
                _appProfile.MetaData.TotalMatchesCount += matches.Count();

                HashSet<string> uniqueTagsControl = new HashSet<string>();

                // Iterate through each match issue 
                foreach (Issue match in matches)
                {
                    Program.Logger.Trace("Processing pattern matches for ruleId {0}, ruleName {1} file {2}", match.Rule.Id, match.Rule.Name, filePath);

                    //maintain a list of unique tags; multi-purpose but primarily for filtering -u option
                    bool dupTagFound = false;
                    foreach (string t in match.Rule.Tags)
                        dupTagFound = !uniqueTagsControl.Add(t);

                    //all unique dependendencies saved even if this tag pattern is not-unique
                    var tagPatternRegex = new Regex("Dependency.SourceInclude", RegexOptions.IgnoreCase);
                    String textMatch;
                    if (match.Rule.Tags.Any(v => tagPatternRegex.IsMatch(v)))
                        textMatch = ExtractDependency(fileText, match.Boundary.Index, match.PatternMatch, language);
                    else
                        textMatch = ExtractTextSample(fileText, match.Boundary.Index, match.Boundary.Length);

                    //TODO put all this in Issue class and avoid new wrapper type                      
                    MatchRecord record = new MatchRecord()
                    {
                        Filename = filePath,
                        Language = language,
                        Filesize = fileText.Length,
                        TextSample = textMatch,
                        Excerpt = ExtractExcerpt(fileText, match.StartLocation.Line),
                        Issue = match
                    };

                    //preserve issue level characteristics as rolled up meta data of interest
                    bool valid = _appProfile.MetaData.AddStandardProperties(record);

                    //bail after extracting any dependency unique items IF user requested
                    if (_arg_outputUniqueTagsOnly && dupTagFound)
                        continue;
                    else if (valid)
                        _appProfile.MatchList.Add(record);
                }
            }
            else
            {
                Program.Logger.Trace("No pattern matches detected for file: " + filePath);
            }

        }

        #region PostRulesMatchProcessingAssist

        /// <summary>
        /// Simple but keeps calling code consistent
        /// </summary>
        /// <param name="fileText"></param>
        /// <param name="index"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        string ExtractTextSample(string fileText, int index, int length)
        {
            string result = "";
            try
            {
                result = fileText.Substring(index, length).Trim();
            }
            catch (Exception)
            {
                //control the error description and continue; error in devskim possible
                Program.Logger.Error("Unexpected indexing issue in ExtractTextSample");
            }

            return result;
        }

        /// <summary>
        /// Helper to special case additional processing to just get the values without the import keywords etc.
        /// and encode for html output
        /// </summary>
        /// <param name="text"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        string ExtractDependency(string text, int startIndex, SearchPattern pattern, string language)
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
                                rawResult = parseValues[0].Trim();
                            else if (parseValues.Length > 1)
                                rawResult = parseValues[1].Trim(); //should be value; time will tell if fullproof
                        }
                        else if (match.Groups.Count > 1)//handles cases like include <stdio.h>
                            rawResult = match.Groups[1].Value.Trim();
                        //else if > 2 too hard to match; do nothing

                        break;//only designed to expect one match per line i.e. not include value include value
                    }
                }

                String finalResult = rawResult.Replace(";", "");
                _appProfile.MetaData.UniqueDependencies.Add(finalResult);

                return System.Net.WebUtility.HtmlEncode(finalResult);
            }

            return rawResult;
        }
        
        /// <summary>
        /// Located here to include during Match creation to avoid a call later or putting in constructor
        /// Needed in match ensuring value exists at time of report writing rather than expecting a callback
        /// from the template
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="startLineNumber"></param>
        /// <param name="length"></param>
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
            if (startLineNumber < 0) startLineNumber = 0;
            if (startLineNumber >= lines.Length) startLineNumber = lines.Length - 1;

            var excerptStartLine = Math.Max(0, startLineNumber - distance);
            var excerptEndLine = Math.Min(lines.Length - 1, startLineNumber + distance);
            
            /* This is a little wacky, but if the code snippet we're viewing is already
             * indented 16 characters minimum, we don't want to show all that extra white-
             * space, so we'll find the smallest number of spaces at the beginning of
             * each line and use that.
             */
            var n = (int)Math.Floor(Math.Log10(excerptEndLine) + 1);
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
                //string line = System.Net.WebUtility.HtmlEncode(lines[i].Substring(minSpaces).TrimEnd());
                //sb.AppendFormat("{0}  {1}\n", (i + 1).ToString().PadLeft(n, ' '), line);
            }

            return System.Convert.ToBase64String(Encoding.UTF8.GetBytes(sb.ToString()));
        }

    

        public void FlushAll()
        {
            if (_outputWriter != null)
            {
                _outputWriter.WriteApp(_appProfile);

                if (_outputWriter.TextWriter != null && _arg_fileFormat != "html")
                {
                    _outputWriter.FlushAndClose();//not required for html formal i.e. multiple files already closed
                    _outputWriter = null;
                    if (!String.IsNullOrEmpty(_arg_outputFile))
                        WriteOnce.WriteLine(String.Format("See {0}.", _arg_outputFile));
                    else
                        WriteOnce.WriteLine("\n");
                }
            }
        }

        #endregion


        #region ZipDecompression

        void ExpandAndProcess(string filename)
        {
            // Ignore images and other junk like that
            var fileExtension = new FileInfo(filename).Extension;
            var mimeType = MimeTypeMap.GetMimeType(fileExtension);
            bool mimeMatch = false;
            if (!IgnoreMimeRegex.IsMatch(mimeType))
            {
                var isValidExtension = COMPRESSED_EXTENSIONS.Any(fileExtension.Contains);
                if (isValidExtension || fileExtension == "ts")
                    mimeMatch = true;
                else if (mimeType.Contains("zip", StringComparison.CurrentCultureIgnoreCase) || // Should have been caught in file extensions above, but still OK.
                    mimeType.Contains("tar", StringComparison.CurrentCultureIgnoreCase) ||
                    mimeType.Contains("compressed", StringComparison.CurrentCultureIgnoreCase))
                    mimeMatch = true;

                if (mimeMatch)
                {
                    // Now process the file
                    switch (fileExtension)
                    {
                        case ".tgz":
                            ProcessTarGzFile(filename);
                            break;
                        case ".gz":
                            if (filename.Contains(".tar.gz"))
                            {
                                ProcessTarGzFile(filename);
                            }
                            break;
                        case ".jar":
                        case ".zip":
                            ProcessZipFile(filename);
                            break;
                        case ".gem":
                        case ".tar":
                        case ".nupkg":
                            Program.Logger.Warn($"Processing of {fileExtension} not implemented yet.");
                            break;
                        default:
                            Program.Logger.Warn("no support for compressed type: " + fileExtension);
                            break;
                    }

                    _appProfile.MetaData.PackageTypes.Add("compressed");

                }
                else
                    _appProfile.MetaData.PackageTypes.Add("compressed-unsupported");


            }
        }

        void ProcessZipFile(string filename)
        {
            Program.Logger.Trace("Analyzing .zip file: [{0}])", filename);

            ZipFile zipFile;
            int filesCount = 0;
            using (var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read))
            using (var memoryStream = new MemoryStream())
            {
                zipFile = new ZipFile(fileStream);
                _appProfile.MetaData.TotalFiles = (int)zipFile.Count;
                foreach (ZipEntry zipEntry in zipFile)
                {
                    if (zipEntry.IsDirectory)
                    {
                        continue;
                    }
                    filesCount++;
                    byte[] buffer = new byte[4096];
                    var zipStream = zipFile.GetInputStream(zipEntry);
                    if (zipEntry.Size > MAX_FILESIZE)
                    {
                        _appProfile.MetaData.FilesSkipped++;
                        Program.Logger.Error(string.Format("{0} in {1} is too large.  File skipped", zipEntry.Name, filename));
                        zipFile.Close();
                        continue;
                    }

                    StreamUtils.Copy(zipStream, memoryStream, buffer);
                    var mimeType = MimeTypeMap.GetMimeType(Path.GetExtension(zipEntry.Name));
                    if (IgnoreMimeRegex.IsMatch(mimeType) && new FileInfo(filename).Extension != "ts")
                    {
                        _appProfile.MetaData.FilesSkipped++;
                        Program.Logger.Error("Ignoring zip entry [{0}]", zipEntry.Name);
                    }
                    else
                    {
                        byte[] streamByteArray = memoryStream.ToArray();
                        ProcessInMemory(Path.GetFileName(zipEntry.Name), Encoding.UTF8.GetString(streamByteArray, 0, streamByteArray.Length));
                    }
                    memoryStream.SetLength(0);   // Clear out the stream
                }

                _appProfile.MetaData.TotalFiles += filesCount;
                zipFile.Close();
            }

        }

        void ProcessTarGzFile(string filename)
        {
            Program.Logger.Trace("Analyzing .tar.gz file: [{0}])", filename);

            using (var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read))
            using (var gzipStream = new GZipInputStream(fileStream))
            using (var memoryStream = new MemoryStream())
            {
                var tarStream = new TarInputStream(gzipStream);
                TarEntry tarEntry;
                int filesCount = 0;
                while ((tarEntry = tarStream.GetNextEntry()) != null)
                {
                    if (tarEntry.IsDirectory)
                    {
                        continue;
                    }
                    filesCount++;
                    tarStream.CopyEntryContents(memoryStream);
                    if (tarEntry.Size > MAX_FILESIZE)
                    {
                        _appProfile.MetaData.FilesSkipped++;
                        Program.Logger.Error(string.Format("{0} in {1} is too large.  File skipped", tarEntry.Name, filename));
                        tarStream.Close();
                        continue;
                    }

                    var mimeType = MimeTypeMap.GetMimeType(Path.GetExtension(tarEntry.Name));
                    if (IgnoreMimeRegex.IsMatch(mimeType) && new FileInfo(filename).Extension != "ts")
                    {
                        _appProfile.MetaData.FilesSkipped++;
                        Program.Logger.Error("Ignoring tar entry [{0}]", tarEntry.Name);
                    }
                    else
                    {
                        //file name may contain slashes; remove prior to call
                        byte[] streamByteArray = memoryStream.ToArray();
                        ProcessInMemory(Path.GetFileName(tarEntry.Name), Encoding.UTF8.GetString(streamByteArray, 0, streamByteArray.Length));
                    }

                    memoryStream.SetLength(0);   // Clear out the stream
                }
                _appProfile.MetaData.TotalFiles+= filesCount;
                tarStream.Close();
            }

        }

        #endregion


    }
  
}



