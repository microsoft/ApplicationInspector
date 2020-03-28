// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using NLog;

namespace Microsoft.ApplicationInspector.Commands
{
    abstract public class Command
    {
        protected Logger _arg_logger;
        protected string _arg_log_file_path;
        protected string _arg_log_level;
        protected bool _arg_close_log_on_exit;

        public abstract int Run();
        public abstract string GetResult();
    }
}
