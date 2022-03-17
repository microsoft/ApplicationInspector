// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Microsoft.ApplicationInspector.CLI
{
    using Microsoft.ApplicationInspector.Commands;
    using System.IO;

    /// <summary>
    /// Common class for command specific writers
    /// </summary>
    public abstract class CommandResultsWriter
    {
        protected CommandResultsWriter(TextWriter writer)
        {
            TextWriter = writer;
        }
        public TextWriter TextWriter { get; }
        public abstract void WriteResults(Result result, CLICommandOptions commandOptions, bool autoClose = true);
        public void FlushAndClose()
        {
            TextWriter?.Flush();
            TextWriter?.Close();
        }
    }
}