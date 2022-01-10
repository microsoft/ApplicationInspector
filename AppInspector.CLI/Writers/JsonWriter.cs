namespace Microsoft.ApplicationInspector.CLI.Writers
{
    using Microsoft.ApplicationInspector.Commands;
    using Microsoft.ApplicationInspector.Common;
    using Newtonsoft.Json;

    internal class JsonWriter : CommandResultsWriter
    {
        public override void WriteResults(Result result, CLICommandOptions commandOptions, bool autoClose = true)
        {
            JsonSerializer jsonSerializer = new();
            jsonSerializer.Formatting = Formatting.Indented;
            jsonSerializer.NullValueHandling = NullValueHandling.Ignore;
            jsonSerializer.DefaultValueHandling = DefaultValueHandling.Ignore;

            //For console output, update write once for same results to console or file
            WriteOnce.TextWriter = TextWriter;

            if (string.IsNullOrEmpty(commandOptions.OutputFilePath))
            {
                WriteOnce.Result("Results");
            }

            if (TextWriter != null)
            {
                switch (result) 
                {
                    case TagDiffResult:
                    case ExportTagsResult:
                    case VerifyRulesResult:
                        jsonSerializer.Serialize(TextWriter, result);
                        break;
                    case PackRulesResult prr:
                        jsonSerializer.Serialize(TextWriter, prr.Rules);
                        break;
                    default:
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