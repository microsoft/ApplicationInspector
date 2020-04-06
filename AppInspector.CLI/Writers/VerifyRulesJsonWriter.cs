using Microsoft.ApplicationInspector.Commands;
using Newtonsoft.Json;

namespace Microsoft.ApplicationInspector.CLI.Writers
{
    class VerifyRulesJsonWriter : CommandResultsWriter
    {
        public override void WriteResults(Result result, CLICommandOptions commandOptions, bool autoClose = true)
        {
            WriteOnce.Result("Result");

            JsonSerializer jsonSerializer = new JsonSerializer();
            jsonSerializer.Formatting = Formatting.Indented;
            jsonSerializer.Serialize(TextWriter, (VerifyRulesResult)result);

            WriteOnce.NewLine();

            if (autoClose)
                FlushAndClose();
        }

        public override void FlushAndClose()
        {
            TextWriter.Flush();
            TextWriter.Close();
        }
    }
}
