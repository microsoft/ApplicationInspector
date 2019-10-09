// Copyright(C) Microsoft.All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System;
using System.IO;
using NLog;


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
    public enum ConsoleVerbosityLevel { High, Medium, Low, None }

    private static ConsoleColor _infoForeColor = ConsoleColor.Magenta;
    private static ConsoleColor _errorForeColor = ConsoleColor.Red;
    private static ConsoleColor _generalForeColor = ConsoleColor.Gray;
    public static ConsoleVerbosityLevel Verbosity { get; set; }

    public static Logger Log {get; set;}
    public static TextWriter Writer { get; set; }
    public ConsoleColor InfoForeColor { set { _infoForeColor = value; } }
    public ConsoleColor ErrorForeColor { set { _errorForeColor = value; } }
    
    public static void Info(string s, ConsoleVerbosityLevel verbosityLevel=ConsoleVerbosityLevel.Medium)
    {
        if (Log != null && Log.Name != "Console")
            Log.Info(s);

        if (Writer != null && Writer != Console.Out)
            Writer.WriteLine(s);

        WriteLine(s, _infoForeColor, verbosityLevel);
    }


    public static void Any(string s, ConsoleVerbosityLevel verbosityLevel = ConsoleVerbosityLevel.Medium)
    {
        Any(s, _generalForeColor, verbosityLevel);
    }


    public static void Any(string s, ConsoleColor foreColor, ConsoleVerbosityLevel verbosityLevel = ConsoleVerbosityLevel.Medium)
    {
        if (Log != null && Log.Name != "Console")
            Log.Info(s);

        if (Writer != null && Writer != Console.Out)
            Writer.WriteLine(s);

        WriteLine(s, foreColor, verbosityLevel);
    }

    public static void Error(string s, ConsoleVerbosityLevel verbosityLevel = ConsoleVerbosityLevel.Medium)
    {
        if (Log != null && Log.Name != "Console")
            Log.Error(s);

        WriteLine(s, _errorForeColor, verbosityLevel);
    }


    public static void NewLine(ConsoleVerbosityLevel verbosityLevel = ConsoleVerbosityLevel.Medium)
    {
        if (verbosityLevel >= Verbosity)
            Console.WriteLine();
    }

    public static void Write(string s, ConsoleVerbosityLevel verbosityLevel = ConsoleVerbosityLevel.Medium)
    {
        Write(s, _infoForeColor,verbosityLevel);
    }


    public static void WriteLine(string s, ConsoleVerbosityLevel verbosityLevel = ConsoleVerbosityLevel.Medium)
    {
        WriteLine(s, _infoForeColor, verbosityLevel);
    }


    public static void Write(string s, ConsoleColor foreground, ConsoleVerbosityLevel verbosityLevel = ConsoleVerbosityLevel.Medium)
    {
        if (verbosityLevel >= Verbosity)
        {
            ConsoleColor lastForecolor = Console.ForegroundColor;
            Console.ForegroundColor = foreground;
            Console.Write(s);
            Console.ForegroundColor = lastForecolor;
        }
    }

    public static void WriteLine(string s, ConsoleColor foreground, ConsoleVerbosityLevel verbosityLevel = ConsoleVerbosityLevel.Medium)
    {
        if (verbosityLevel >= Verbosity)
        {
            ConsoleColor lastForecolor = Console.ForegroundColor;
            Console.ForegroundColor = foreground;
            Console.WriteLine(s);
            Console.ForegroundColor = lastForecolor;
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

            WriteOnce.WriteLine("See output file if specified.  For HTML format option, please see the HTMLOutput folder created.");
        }
    }



}