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

            if (TextWriter != null)
            {
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
                else if (result is PackRulesResult packRulesResult)
                {
                    jsonSerializer.Serialize(TextWriter, packRulesResult.Rules);//write rules array only to disk
                }
                else
                {
                    throw new System.Exception("Unexpected object type for json writer");
                }
            }
            else
            {
                WriteOnce.Log?.Error("Unexpected null TextWriter");
            }

            WriteOnce.NewLine();

            if (autoClose)
            {
                FlushAndClose();
            }
        }
    }
}