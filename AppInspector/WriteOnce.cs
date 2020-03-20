// Copyright(C) Microsoft.All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using NLog;
using System;
using System.IO;

namespace Microsoft.ApplicationInspector.Commands
{
    /// <summary>
    /// Wraps Console, Output and Log Writes for convenience to write once from calling
    /// code to increase readability and for allowing replacement of console print
    /// functionality in conjunction logger which may have console configuration enabled
    /// Allows a Write once feature and protects code from console/log changes to standard out
    /// Use: call to write to console, file output and log in one call OR for log file only 
    /// output call logger directly
    /// 
    /// Note: NLOG is not consistent writing to standard out console making it impossible
    /// to predict when it will be visible thus it is largely used for file log output only
    /// </summary>
    public class WriteOnce
    {
        public enum ConsoleVerbosity { High, Medium, Low, None }

        public static ConsoleVerbosity Verbosity { get; set; }

        public static Logger Log { get; set; } //use SafeLog or check for null before use

        public static TextWriter Writer { get; set; }
        private static ConsoleColor _infoColor = ConsoleColor.Magenta;
        private static ConsoleColor _errorColor = ConsoleColor.Red;
        private static ConsoleColor _generalColor = ConsoleColor.Gray;
        private static ConsoleColor _resultColor = ConsoleColor.Yellow;
        private static ConsoleColor _opColor = ConsoleColor.Cyan;
        private static ConsoleColor _sysColor = ConsoleColor.Magenta;

        public ConsoleColor InfoForeColor { set { _infoColor = value; } }
        public ConsoleColor ErrorForeColor { set { _errorColor = value; } }
        public ConsoleColor OperationForeColor { set { _opColor = value; } }
        public ConsoleColor ResultForeColor { set { _resultColor = value; } }
        public ConsoleColor GeneralForeColor { set { _generalColor = value; } }


        public static void Operation(string msg, bool writeLine = true, ConsoleVerbosity verbosity = ConsoleVerbosity.Low)
        {
            Any(msg, writeLine, _opColor, verbosity);
        }


        public static void System(string msg, bool writeLine = true, ConsoleVerbosity verbosity = ConsoleVerbosity.Low)
        {
            Any(msg, writeLine, _sysColor, verbosity);
        }


        public static void General(string msg, bool writeLine = true, ConsoleVerbosity verbosity = ConsoleVerbosity.Medium)
        {
            Any(msg, writeLine, _generalColor, verbosity);
        }

        public static void Result(string msg, bool writeLine = true, ConsoleVerbosity verbosity = ConsoleVerbosity.Medium)
        {
            Any(msg, writeLine, _resultColor, verbosity);
        }


        public static void Info(string msg, bool writeLine = true, ConsoleVerbosity verbosity = ConsoleVerbosity.Medium)
        {
            if (Log != null && Log.Name != "Console")
                Log.Info(msg);

            if (Writer != null && Writer != Console.Out)
                Writer.WriteLine(msg);

            SafeConsoleWrite(msg, writeLine, _infoColor, verbosity);
        }


        public static void Any(string msg, bool writeLine = true, ConsoleColor foreColor = ConsoleColor.Gray, ConsoleVerbosity verbosity = ConsoleVerbosity.Medium)
        {
            if (Writer != null && Writer != Console.Out)
                Writer.WriteLine(msg);

            SafeConsoleWrite(msg, writeLine, foreColor, verbosity);
        }


        public static void Error(string msg, bool writeLine = true, ConsoleVerbosity verbosity = ConsoleVerbosity.Low)
        {
            if (Log != null && Log.Name != "Console")
                Log.Error(msg);

            if (Writer != null && Writer != Console.Out)
                Writer.WriteLine(msg);

            SafeConsoleWrite(msg, writeLine, _errorColor, verbosity);
        }


        public static void NewLine(ConsoleVerbosity verbosity = ConsoleVerbosity.Medium)
        {
            if (verbosity >= Verbosity)
                Console.WriteLine();
        }


        static void SafeConsoleWrite(string msg, bool writeLine, ConsoleColor foreground, ConsoleVerbosity verbosity)
        {
            if (verbosity >= Verbosity)
            {
                ConsoleColor lastForecolor = Console.ForegroundColor;
                Console.ForegroundColor = foreground;

                if (writeLine)
                    Console.WriteLine(msg);
                else
                    Console.Write(msg);

                Console.ForegroundColor = lastForecolor;
            }
        }


        static public void SafeLog(string message, NLog.LogLevel logLevel)
        {
            if (Log == null)
                Log = Utils.SetupLogging();

            if (Log != null && Log.Name != "Console")
            {
                int value = logLevel.Ordinal;
                switch (value)
                {
                    case 0:
                        Log.Trace(message);
                        break;
                    case 1:
                        Log.Debug(message);
                        break;
                    case 2:
                        Log.Info(message);
                        break;
                    case 3:
                        Log.Warn(message);
                        break;
                    case 4:
                        Log.Error(message);
                        break;
                }
            }
        }


        public static void FlushAll()
        {
            if (Writer != null)
            {
                //cleanup
                Writer.Flush();
                Writer.Close();
                Writer = null;
            }
        }

    }

}
