using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AppInspector
{
    public class OpException : Exception
    {
        public OpException(string msg) : base(msg)
        {

        }
    }
}
