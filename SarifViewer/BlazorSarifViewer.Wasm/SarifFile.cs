using Microsoft.CodeAnalysis.Sarif;
using Microsoft.CodeAnalysis.Sarif.Readers;
using Newtonsoft.Json;
using System;
using System.IO;

namespace BlazorSarifViewer.Wasm
{
    public class SarifFile : IDisposable
    {
        public SarifLog SarifLog { get; set; }
        public string FileName { get; set; }
        private Stream? Backing { get; set; }
        public SarifFile(Stream sarifLog, string name)
        {
            FileName = name;
            Backing = sarifLog;
            JsonSerializer serializer = new JsonSerializer();
            serializer.ContractResolver = new SarifDeferredContractResolver();

            using JsonPositionedTextReader jptr = new JsonPositionedTextReader(() => new DelegatingStream(Backing));
            SarifLog = serializer.Deserialize<SarifLog>(jptr);
        }

        public SarifFile(SarifLog log, string name)
		{
            FileName = name;
            SarifLog = log;
        }
        public void Dispose()
        {
            Backing?.Dispose();
        } 
    }
}