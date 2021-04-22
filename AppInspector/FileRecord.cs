using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.ApplicationInspector.Commands
{
    public class FileRecord
    {
        public string FileName { get; set; } = string.Empty;
        public TimeSpan ScanTime { get; set; } = new TimeSpan();
        public ScanState Status { get; set; } = ScanState.None;
        public int NumFindings { get; set; } = 0;
    }

    public enum ScanState
    {
        None,
        Skipped,
        TimedOut,
        Analyzed
    }
}
