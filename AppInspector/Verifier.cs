// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Microsoft.ApplicationInspector.RulesEngine;
using System;
using System.IO;
using System.Linq;

namespace Microsoft.ApplicationInspector.Commands
{
    class Verifier
    {
        public Verifier(string[] paths)
        {
            //_messages = new List<string>();
            _rules = new RuleSet();
            _paths = paths;
        }

        public Verifier(string path)
            : this(new string[] { path })
        {
        }

        public bool Verify()
        {
            bool isCompiled = true;

            foreach (string rulesPath in _paths)
            {
                if (Directory.Exists(rulesPath))
                    isCompiled = LoadDirectory(rulesPath);
                else if (File.Exists(rulesPath))
                    isCompiled = LoadFile(rulesPath);
                else
                {
                    Console.Error.WriteLine("Error: Not a valid file or directory {0}", rulesPath);
                    isCompiled = false;
                    break;
                }
            }

            if (isCompiled)
            {
                CheckIntegrity();
            }


            return isCompiled;
        }

        private void CheckIntegrity()
        {
            //call verifycommand and throw exception on error
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
            rules.OnDeserializationError += delegate (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs e)
            {
                throw new Exception(String.Format("Error packing rule file: {0}", file));
            };

            rules.AddFile(file, null);

            if (noProblem)
                _rules.AddRange(rules.AsEnumerable());

            return noProblem;
        }



        public RuleSet CompiledRuleset
        {
            get { return _rules; }
        }

        private RuleSet _rules;
        private string[] _paths;
    }
}
