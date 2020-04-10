using Microsoft.ApplicationInspector.Commands;
using Newtonsoft.Json;

namespace Microsoft.ApplicationInspector.CLI.Writers
{
    internal class JsonWriter : CommandResultsWriter
    {
        public override void WriteResults(Result result, CLICommandOptions commandOptions, bool autoClose = true)
        {
            JsonSerializer jsonSerializer = new JsonSerializer();
            jsonSerializer.Formatting = Formatting.Indented;

            //For console output, update write once for same results to console or file
            WriteOnce.TextWriter = TextWriter;

            if (string.IsNullOrEmpty(commandOptions.OutputFilePath))
            {
                WriteOnce.Result("Results");
            }

            if (result is TagTestResult)
            {
                jsonSerializer.Serialize(TextWriter, (TagTestResult)result);
            }
            else if (result is TagDiffResult)
            {
                jsonSerializer.Serialize(TextWriter, (TagDiffResult)result);
            }
            else if (result is VerifyRulesResult)
            {
                jsonSerializer.Serialize(TextWriter, (VerifyRulesResult)result);
            }
            else if (result is ExportTagsResult)
            {
                jsonSerializer.Serialize(TextWriter, (ExportTagsResult)result);
            }
            else if (result is PackRulesResult)
            {
                jsonSerializer.Serialize(TextWriter, (PackRulesResult)result);
            }
            else
            {
                throw new System.Exception("Unexpected object type for json writer");
            }

            WriteOnce.NewLine();

            if (autoClose)
            {
                FlushAndClose();
            }
        }

        public override void FlushAndClose()
        {
            TextWriter.Flush();
            TextWriter.Close();
            WriteOnce.TextWriter = null;
        }
    }
}