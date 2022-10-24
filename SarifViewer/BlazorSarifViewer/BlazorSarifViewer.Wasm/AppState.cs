using System.Collections.Generic;
using System.Net.Http;

namespace BlazorSarifViewer.Wasm
{
    public class AppState
    {
        public HttpClient Http { get; set; } = new HttpClient();
        public List<SarifFile> Files { get; set; } = new List<SarifFile>();
        public int LinesContext { get; set; } = 5;
    }
}
