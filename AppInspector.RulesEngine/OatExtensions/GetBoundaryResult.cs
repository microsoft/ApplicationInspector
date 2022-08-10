namespace Microsoft.ApplicationInspector.RulesEngine.OatExtensions;

internal class GetBoundaryResult
{
    internal GetBoundaryResult(bool validBoundary, int start, int end)
    {
        ValidBoundary = validBoundary;
        Start = start;
        End = end;
    }

    internal int End { get; }

    internal int Start { get; }

    internal bool ValidBoundary { get; }
}