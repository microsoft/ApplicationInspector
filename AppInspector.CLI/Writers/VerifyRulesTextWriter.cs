// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Microsoft.ApplicationInspector.RulesEngine;

namespace Microsoft.ApplicationInspector.CLI
{
    using Microsoft.ApplicationInspector.Commands;
    using Microsoft.ApplicationInspector.Common;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using System.IO;

    internal class VerifyRulesTextWriter : CommandResultsWriter
    {
        private readonly ILogger<VerifyRulesTextWriter> _logger;

        public VerifyRulesTextWriter(TextWriter textWriter, ILoggerFactory? loggerFactory = null) : base(textWriter)
        {
            _logger = loggerFactory?.CreateLogger<VerifyRulesTextWriter>() ?? NullLogger<VerifyRulesTextWriter>.Instance;
        }
        public override void WriteResults(Result result, CLICommandOptions commandOptions, bool autoClose = true)
        {
            VerifyRulesResult verifyRulesResult = (VerifyRulesResult)result;

            if (string.IsNullOrEmpty(commandOptions.OutputFilePath))
            {
                TextWriter.WriteLine("Results");
            }

            if (verifyRulesResult.ResultCode != VerifyRulesResult.ExitCode.Verified)
            {
                TextWriter.WriteLine(MsgHelp.ID.TAGTEST_RESULTS_FAIL);
            }
            else
            {
                TextWriter.WriteLine(MsgHelp.ID.TAGTEST_RESULTS_SUCCESS);
            }

            if (verifyRulesResult.RuleStatusList.Count > 0)
            {
                TextWriter.WriteLine("Rule status");
                foreach (RuleStatus ruleStatus in verifyRulesResult.RuleStatusList)
                {
                    TextWriter.WriteLine("Ruleid: {0}, Rulename: {1}, Status: {2}", ruleStatus.RulesId, ruleStatus.RulesName, ruleStatus.Verified);
                }
            }

            if (autoClose)
            {
                FlushAndClose();
            }
        }
    }
}