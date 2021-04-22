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
        //public long Size { get; set; } = 0;
        public TimeSpan ScanTime { get; set; } = new TimeSpan();
    }
}
