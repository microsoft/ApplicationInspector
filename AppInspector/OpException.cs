using System;

namespace Microsoft.ApplicationInspector.Commands
{
    public class OpException : Exception
    {
        public OpException(string msg) : base(msg)
        {

        }
    }
}
