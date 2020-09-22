// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Microsoft.ApplicationInspector.Commands;

namespace Microsoft.ApplicationInspector.CLI
{
    public class AnalyzeDummyWriter : CommandResultsWriter
    {
        public override void WriteResults(Result result, CLICommandOptions commandOptions, bool autoClose = true)
        {
            throw new System.NotImplementedException();
        }
    }

    public class ExportDummyWriter : CommandResultsWriter
    {

        public override void WriteResults(Result result, CLICommandOptions commandOptions, bool autoClose = true)
        {
            throw new System.NotImplementedException();
        }
    }

    public class TagTestDummyWriter : CommandResultsWriter
    {
        public override void WriteResults(Result result, CLICommandOptions commandOptions, bool autoClose = true)
        {
            throw new System.NotImplementedException();
        }
    }

    public class TagDiffDummyWriter : CommandResultsWriter
    {
        public override void WriteResults(Result result, CLICommandOptions commandOptions, bool autoClose = true)
        {
            throw new System.NotImplementedException();
        }
    }

    public class VerifyRulesDummyWriter : CommandResultsWriter
    {
        public override void WriteResults(Result result, CLICommandOptions commandOptions, bool autoClose = true)
        {
            throw new System.NotImplementedException();
        }
    }

    public class PackRulesDummyWriter : CommandResultsWriter
    {
        public override void WriteResults(Result result, CLICommandOptions commandOptions, bool autoClose = true)
        {
            throw new System.NotImplementedException();
        }
    }
}