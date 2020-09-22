// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Microsoft.ApplicationInspector.Commands;
using System;

namespace Microsoft.ApplicationInspector.CLI
{
    internal class VerifyRulesTextWriter : CommandResultsWriter
    {
        public override void WriteResults(Result result, CLICommandOptions commandOptions, bool autoClose = true)
        {
            VerifyRulesResult verifyRulesResult = (VerifyRulesResult)result;

            //For console output, update write once for same results to console or file
            WriteOnce.TextWriter = TextWriter;

            if (string.IsNullOrEmpty(commandOptions.OutputFilePath))
            {
                WriteOnce.Result("Results");
            }

            if (verifyRulesResult.ResultCode != VerifyRulesResult.ExitCode.Verified)
            {
                WriteOnce.Any(MsgHelp.GetString(MsgHelp.ID.TAGTEST_RESULTS_FAIL), true, ConsoleColor.Red, WriteOnce.ConsoleVerbosity.Low);
            }
            else
            {
                WriteOnce.Any(MsgHelp.GetString(MsgHelp.ID.TAGTEST_RESULTS_SUCCESS), true, ConsoleColor.Green, WriteOnce.ConsoleVerbosity.Low);
            }

            if (verifyRulesResult.RuleStatusList.Count > 0)
            {
                WriteOnce.Result("Rule status");
                foreach (RuleStatus ruleStatus in verifyRulesResult.RuleStatusList)
                {
                    WriteOnce.General(string.Format("Ruleid: {0}, Rulename: {1}, Status: {2}", ruleStatus.RulesId, ruleStatus.RulesName, ruleStatus.Verified));
                }
            }

            if (autoClose)
            {
                FlushAndClose();
            }
        }
    }
}