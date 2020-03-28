// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Microsoft.ApplicationInspector.RulesEngine;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Microsoft.ApplicationInspector.Commands
{
    /// <summary>
    /// Common helper used by VerifyRulesCommand and PackRulesCommand classes to reduce duplication
    /// </summary>
    class RulesVerifier
    {
        private RuleSet _rules;
        private string _rulesPath;

        public RulesVerifier(string rulePath)
        {
            _rules = new RuleSet();
            _rulesPath = rulePath;
        }


        public void Verify()
        {
            if (Directory.Exists(_rulesPath))
                LoadDirectory(_rulesPath);
            else if (File.Exists(_rulesPath))
                LoadFile(_rulesPath);
            else
            {
                throw new Exception(ErrMsg.FormatString(ErrMsg.ID.CMD_INVALID_RULE_PATH, _rulesPath));
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
            RuleSet rules = new RuleSet();

            try
            {
                rules.AddFile(file, null);
            }
            catch (Exception e)
            {
                Debug.Write(e.Message);//Ensure console message indicates problem for Build process
                WriteOnce.SafeLog(e.Message, NLog.LogLevel.Error);
                throw new Exception(ErrMsg.FormatString(ErrMsg.ID.VERIFY_RULE_FAILED, file));
            }

            _rules.AddRange(rules.AsEnumerable());
        }



        public RuleSet CompiledRuleset
        {
            get { return _rules; }
        }

    }
}
