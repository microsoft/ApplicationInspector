// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Microsoft.ApplicationInspector.RulesEngine;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.ApplicationInspector.Commands
{
    /// <summary>
    /// Used to combine validated rules into one json for ease in distribution of this
    /// application
    /// </summary>
    public class PackRulesCommand : Command
    {
        public enum ExitCode
        {
            NoIssues = 0,
            NotVerified = 1,
            CriticalError = 2
        }

        private string _path;
        private string _outputfile;
        private bool _indent;
        private string _arg_consoleVerbosityLevel;

        public PackRulesCommand(PackRulesCommandOptions opt)
        {
            _path = opt.RepackDefaultRules ? Utils.GetPath(Utils.AppPath.defaultRulesSrc) : opt.CustomRulesPath;
            _outputfile = opt.RepackDefaultRules && String.IsNullOrEmpty(opt.OutputFilePath) ? Utils.GetPath(Utils.AppPath.defaultRulesPackedFile) : opt.OutputFilePath;
            _arg_consoleVerbosityLevel = opt.ConsoleVerbosityLevel;

            if (!opt.RepackDefaultRules && string.IsNullOrEmpty(opt.CustomRulesPath) || string.IsNullOrEmpty(_outputfile))
                throw new OpException(ErrMsg.FormatString(ErrMsg.ID.PACK_MISSING_OUTPUT_ARG));

            _outputfile = Path.GetFullPath(_outputfile);
            _indent = !opt.NotIndented;

            WriteOnce.ConsoleVerbosity verbosity = WriteOnce.ConsoleVerbosity.Medium;
            if (!Enum.TryParse(_arg_consoleVerbosityLevel, true, out verbosity))
                throw new OpException(String.Format(ErrMsg.FormatString(ErrMsg.ID.CMD_INVALID_ARG_VALUE, "-x")));
            WriteOnce.Verbosity = verbosity;
        }

        /// <summary>
        /// After verifying rules are valid syntax and load; combines into a single .json file 
        /// for ease in distribution including this application's defaultset which are
        /// added to the manifest as an embedded resource (see AppInspector.Commands.csproj)
        /// </summary>
        /// <returns></returns>
        public override int Run()
        {
            WriteOnce.SafeLog("PackRules::Run", LogLevel.Trace);
            WriteOnce.Operation(ErrMsg.FormatString(ErrMsg.ID.CMD_RUNNING, "PackRules"));
            RulesVerifier verifier = new RulesVerifier(_path);

            if (!verifier.Verify())//throws anyway
                return (int)ExitCode.CriticalError;

            List<Rule> list = new List<Rule>(verifier.CompiledRuleset.AsEnumerable());

            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.Formatting = (_indent) ? Formatting.Indented : Formatting.None;

            using (FileStream fs = File.Open(_outputfile, FileMode.Create, FileAccess.Write))
            {
                StreamWriter sw = new StreamWriter(fs);
                sw.Write(JsonConvert.SerializeObject(list, settings));
                sw.Close();
                fs.Close();
            }

            WriteOnce.Operation(ErrMsg.FormatString(ErrMsg.ID.CMD_COMPLETED, "PackRules"));
            WriteOnce.Any(ErrMsg.FormatString(ErrMsg.ID.ANALYZE_OUTPUT_FILE, _outputfile), true, ConsoleColor.Gray, WriteOnce.ConsoleVerbosity.Medium);

            return (int)ExitCode.NoIssues;
        }

    }
}