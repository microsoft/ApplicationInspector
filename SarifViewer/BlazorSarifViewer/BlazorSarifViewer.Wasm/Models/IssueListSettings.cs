using Microsoft.CodeAnalysis.Sarif;
using System.Collections.Generic;

namespace BlazorSarifViewer.Wasm.Models
{
    public class IssueListSettings
    {
        public Run Run { get; set; }

        public List<ResultKind> ResultKinds { get; set; } = new();

        public List<FailureLevel> ResultLevels { get; set; } = new();

        public List<string> Tags { get; set; } = new();

        public List<ReportingDescriptor> Rules { get; set; } = new();
    }
}
