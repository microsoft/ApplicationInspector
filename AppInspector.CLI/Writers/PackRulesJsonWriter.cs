using Microsoft.ApplicationInspector.Commands;
using Newtonsoft.Json;
using System.IO;

namespace Microsoft.ApplicationInspector.CLI.Writers
{
    internal class PackRulesJsonWriter : CommandResultsWriter
    {
        public override void WriteResults(Result result, CLICommandOptions commandOptions, bool autoClose = true)
        {
            CLIPackRulesCmdOptions packRulesOptions = (CLIPackRulesCmdOptions)commandOptions;
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.Formatting = packRulesOptions.NotIndented ? Formatting.None : Formatting.Indented;

            PackRulesResult packRulesResult = (PackRulesResult)result;

            JsonSerializer jsonSerializer = new JsonSerializer();
            jsonSerializer.Formatting = packRulesOptions.NotIndented ? Formatting.None : Formatting.Indented;
            jsonSerializer.Serialize(TextWriter, packRulesResult.Rules);

            if (autoClose)
            {
                FlushAndClose();
            }
        }

        public override void FlushAndClose()
        {
            TextWriter.Flush();
            TextWriter.Close();
        }
    }
}

