using System;
using System.Text.Json.Serialization;

namespace Microsoft.ApplicationInspector.Commands;

public class FileRecord
{
    public string FileName { get; set; } = string.Empty;
    public TimeSpan ScanTime { get; set; } = new();
    public ScanState Status { get; set; } = ScanState.None;
    public int NumFindings { get; set; } = 0;
    public DateTime ModifyTime { get; set; } = DateTime.MinValue;
    public DateTime CreateTime { get; set; } = DateTime.MinValue;
    public DateTime AccessTime { get; set; } = DateTime.MinValue;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ScanState
{
    None,
    Skipped,
    TimedOut,
    Analyzed,
    Affected,
    TimeOutSkipped,
    Error
}