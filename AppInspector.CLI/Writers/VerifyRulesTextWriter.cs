// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.IO;
using Microsoft.ApplicationInspector.Commands;
using Microsoft.ApplicationInspector.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.ApplicationInspector.CLI;

internal class VerifyRulesTextWriter : CommandResultsWriter
{
    private readonly ILogger<VerifyRulesTextWriter> _logger;

    public VerifyRulesTextWriter(TextWriter textWriter, ILoggerFactory? loggerFactory = null) : base(textWriter)
    {
        _logger = loggerFactory?.CreateLogger<VerifyRulesTextWriter>() ?? NullLogger<VerifyRulesTextWriter>.Instance;
    }

    public override void WriteResults(Result result, CLICommandOptions commandOptions, bool autoClose = true)
    {
        var verifyRulesResult = (VerifyRulesResult)result;

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
            foreach (var ruleStatus in verifyRulesResult.RuleStatusList)
            {
                TextWriter.WriteLine("Ruleid: {0}, Rulename: {1}, Status: {2}", ruleStatus.RulesId,
                    ruleStatus.RulesName, ruleStatus.Verified);

                if (ruleStatus.Verified)
                {
                    continue;
                }

                foreach (var error in ruleStatus.Errors)
                {
                    TextWriter.WriteLine("    Error: {0}", error);
                }

                foreach (var oatIssue in ruleStatus.OatIssues)
                {
                    TextWriter.WriteLine("    OAT issue: {0}", oatIssue.Description);
                }

                foreach (var schemaError in ruleStatus.SchemaValidationErrors)
                {
                    TextWriter.WriteLine("    Schema error: {0}", schemaError.Message);
                }
            }
        }

        if (autoClose)
        {
            FlushAndClose();
        }
    }
}