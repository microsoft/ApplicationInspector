// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using NLog;

namespace Microsoft.ApplicationInspector.Commands
{
    abstract public class Command
    {
        protected Logger _arg_logger;

        public abstract int Run();
    }
}
