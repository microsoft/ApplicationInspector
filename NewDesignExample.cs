///////////////////////// COMMANDS CORE

//New model for refactoring will be similar to Octokit, others with a result object and will split out the core processing for each command so that they can be called by either the Run() CLI command or the NuGet GetResult style command like this below.It’s not all bad as today we do separate the analysis data from the command classes that wrap use of the same but I do see how I could have created another level of decoupling building the results object from writing it out which this will accomplish.

class AnalyzeResult
{
    //internal representation of data
    //methods to access data
}

class AnalzyeResultOptions
{
    //only internal processing options; no output
    bool DuplicateTagsAllowed;
}

class AnalyzeCommand
{
    AnalyzeResult GetResult(AnalzyeResultOptions options) //defined in each command; callable by NuGet or CLI
    {
        //core processing now isolated with no output actions 
        AnalyzeResult result = new AnalyzeResult()
        {
            //add applicable data 
        };

        return result;
    }
}

// CLI

/// <summary>
/// 
/// </summary>
class AnalyzeCommandOptions
{
    //full set of options including processing, output and logging options
    bool DuplcateTagsAllowed { get; set; }
    string OutputFormat { get; set; }
    NLogLevel LogLevel;
}


class Programm
{
    void RunAnalyzeCmd(AnalyzeCommandOptions opts)
    {
        AnalyzeCommand command = new AnalyzeCommand();
        AnalyzeResult result = command.GetResult(new AnalzyeResultOptions
        {
            //processing only properties are initialized
            DuplicateTagsAllowed = opts.DuplicateTagsAllowed
        });

        WriteOutput(result, opts); //overloaded by result type
    }
}

