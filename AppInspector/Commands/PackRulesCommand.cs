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
            CriticalError = Utils.ExitCode.CriticalError //ensure common value for final exit log mention
        }

        private string _rules_path;
        private string _arg_custom_rules_path;
        private string _arg_outputfile;
        private bool _arg_repack_default_rules;
        private bool _arg_indent;
        private string _arg_consoleVerbosityLevel;

        public PackRulesCommand(PackRulesCommandOptions opt)
        {
            _arg_repack_default_rules = opt.RepackDefaultRules;
            _arg_indent = !opt.NotIndented;
            _arg_custom_rules_path = opt.CustomRulesPath;
            _arg_outputfile = opt.OutputFilePath;
            _arg_consoleVerbosityLevel = opt.ConsoleVerbosityLevel ?? "medium";
            _arg_logger = opt.Log;
            _arg_log_file_path = opt.LogFilePath;
            _arg_log_level = opt.LogFileLevel;
            _arg_close_log_on_exit = Utils.CLIExecutionContext ? true : opt.CloseLogOnCommandExit;

            _rules_path = _arg_repack_default_rules ? Utils.GetPath(Utils.AppPath.defaultRulesSrc) : _arg_custom_rules_path;
            _arg_outputfile = _arg_repack_default_rules && String.IsNullOrEmpty(_arg_custom_rules_path) ?
                Utils.GetPath(Utils.AppPath.defaultRulesPackedFile) : _arg_outputfile;

            _arg_logger ??= Utils.SetupLogging(opt);
            WriteOnce.Log ??= _arg_logger;
            ConfigureConsoleOutput();

            try
            {
                ConfigFileOutput();
                ConfigRules();
            }
            catch (Exception e)
            {
                WriteOnce.Error(e.Message);
                if (_arg_close_log_on_exit)
                {
                    Utils.Logger = null;
                    WriteOnce.Log = null;
                }
                throw e;
            }
        }

        #region configure

        /// <summary>
        /// Establish console verbosity
        /// For NuGet DLL use, console is muted overriding any arguments sent
        /// </summary>
        void ConfigureConsoleOutput()
        {
            WriteOnce.SafeLog("PackRulesCommand::ConfigureConsoleOutput", LogLevel.Trace);

            //Set console verbosity based on run context (none for DLL use) and caller arguments
            if (!Utils.CLIExecutionContext)
                WriteOnce.Verbosity = WriteOnce.ConsoleVerbosity.None;
            else
            {
                WriteOnce.ConsoleVerbosity verbosity = WriteOnce.ConsoleVerbosity.Medium;
                if (!Enum.TryParse(_arg_consoleVerbosityLevel, true, out verbosity))
                {
                    WriteOnce.Error(ErrMsg.FormatString(ErrMsg.ID.CMD_INVALID_ARG_VALUE, "-x"));
                    throw new Exception(ErrMsg.FormatString(ErrMsg.ID.CMD_INVALID_ARG_VALUE, "-x"));
                }
                else
                    WriteOnce.Verbosity = verbosity;
            }
        }

        void ConfigFileOutput()
        {
            WriteOnce.SafeLog("PackRulesCommand::ConfigOutput", LogLevel.Trace);

            if (string.IsNullOrEmpty(_arg_outputfile))
                throw new Exception(ErrMsg.FormatString(ErrMsg.ID.PACK_MISSING_OUTPUT_ARG));
        }


        void ConfigRules()
        {
            WriteOnce.SafeLog("PackRulesCommand::ConfigRules", LogLevel.Trace);

            if (_arg_repack_default_rules && !Utils.CLIExecutionContext)
                throw new Exception(ErrMsg.GetString(ErrMsg.ID.VERIFY_RULES_NO_CLI_DEFAULT));

            if (!_arg_repack_default_rules && string.IsNullOrEmpty(_arg_custom_rules_path))
                throw new Exception(ErrMsg.GetString(ErrMsg.ID.CMD_NORULES_SPECIFIED));
        }

        #endregion

        /// <summary>
        /// Intentional as no identified value in calling from DLL at this time
        /// </summary>
        /// <returns></returns>
        public override string GetResult()
        {
            throw new NotImplementedException();
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

            try
            {
                RulesVerifier verifier = new RulesVerifier(_rules_path);
                verifier.Verify();

                List<Rule> list = new List<Rule>(verifier.CompiledRuleset.AsEnumerable());

                JsonSerializerSettings settings = new JsonSerializerSettings();
                settings.Formatting = (_arg_indent) ? Formatting.Indented : Formatting.None;

                using (FileStream fs = File.Open(_arg_outputfile, FileMode.Create, FileAccess.Write))
                {
                    StreamWriter sw = new StreamWriter(fs);
                    sw.Write(JsonConvert.SerializeObject(list, settings));
                    sw.Close();
                    fs.Close();
                }

                WriteOnce.Operation(ErrMsg.FormatString(ErrMsg.ID.CMD_COMPLETED, "PackRules"));
                WriteOnce.Any(ErrMsg.FormatString(ErrMsg.ID.ANALYZE_OUTPUT_FILE, _arg_outputfile), true, ConsoleColor.Gray, WriteOnce.ConsoleVerbosity.Medium);
                WriteOnce.FlushAll();
            }
            catch (Exception e)
            {
                WriteOnce.Error(e.Message);
                //exit normaly for CLI callers and throw for DLL callers
                if (Utils.CLIExecutionContext)
                    return (int)ExitCode.CriticalError;
                else
                    throw e;
            }
            finally
            {
                if (_arg_close_log_on_exit)
                {
                    Utils.Logger = null;
                    WriteOnce.Log = null;
                }
            }

            return (int)ExitCode.NoIssues;
        }

    }
}