namespace Microsoft.ApplicationInspector.Commands
{
    using System;
    
    public record GitInformation
    {
        public Uri? RepositoryUri { get; set; }
        public string? CommitHash { get; set; }
        public string? Branch { get; set; }
    }
}
