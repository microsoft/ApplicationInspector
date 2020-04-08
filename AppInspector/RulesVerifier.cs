// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Microsoft.ApplicationInspector.RulesEngine;
using NLog;
using System;
using System.Diagnostics;
using System.IO;

namespace Microsoft.ApplicationInspector.Commands
{
    /// <summary>
    /// Common helper used by VerifyRulesCommand and PackRulesCommand classes to reduce duplication
    /// </summary>
    internal class RulesVerifier
    {
        private bool fail_fast;
        private readonly RuleSet _rules;
        private readonly string _rulesPath;
        private readonly Logger _logger;

        public RulesVerifier(string rulePath, Logger logger)
        {
            _logger = logger;
            _rules = new RuleSet(logger);
            _rulesPath = rulePath;
        }


        public void Verify(bool failFast = true)
        {
            fail_fast = failFast;

            if (Directory.Exists(_rulesPath))
            {
                LoadDirectory(_rulesPath);
            }
            else if (File.Exists(_rulesPath))
            {
                LoadFile(_rulesPath);
            }
            else
            {
                throw new OpException(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_RULE_PATH, _rulesPath));
            }
        }



        private void LoadDirectory(string path)
        {
            foreach (string filename in Directory.EnumerateFileSystemEntries(path, "*.json", SearchOption.AllDirectories))
            {
                LoadFile(filename);
            }
        }


        private void LoadFile(string file)
        {
            try
            {
                _rules.AddFile(file, null);
            }
            catch (Exception e)
            {
                Debug.Write(e.Message);//Ensure console message indicates problem for Build process
                WriteOnce.SafeLog(e.Message, NLog.LogLevel.Error);

                //allow caller to specify whether to continue
                if (fail_fast)
                {
                    throw new OpException(MsgHelp.FormatString(MsgHelp.ID.VERIFY_RULE_FAILED, file));
                }
            }

        }



        public RuleSet CompiledRuleset => _rules;

    }
}
