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


        public bool Verify()
        {
            bool isCompiled = true;

            if (Directory.Exists(_rulesPath))
                isCompiled = LoadDirectory(_rulesPath);
            else if (File.Exists(_rulesPath))
                isCompiled = LoadFile(_rulesPath);
            else
            {
                throw new OpException(ErrMsg.FormatString(ErrMsg.ID.CMD_INVALID_RULE_PATH, _rulesPath));
            }

            return isCompiled;
        }



        private bool LoadDirectory(string path)
        {
            bool result = true;
            foreach (string filename in Directory.EnumerateFileSystemEntries(path, "*.json", SearchOption.AllDirectories))
            {
                if (!LoadFile(filename))
                    result = false;
            }

            return result;
        }

        private bool LoadFile(string file)
        {
            RuleSet rules = new RuleSet();
            bool noProblem = true;
            try
            {
                rules.AddFile(file, null);
            }
            catch (Exception e)
            {
                Debug.Write(e.Message);//Ensure console message indicates problem for Build process
                WriteOnce.SafeLog(e.Message, NLog.LogLevel.Error);
                throw new OpException(ErrMsg.FormatString(ErrMsg.ID.VERIFY_RULE_FAILED, file));
            }

            if (noProblem)
                _rules.AddRange(rules.AsEnumerable());

            return noProblem;
        }



        public RuleSet CompiledRuleset
        {
            get { return _rules; }
        }

    }
}
