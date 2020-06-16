# Introduction 

Microsoft Application Inspector is a software source code analysis tool that helps identify and surface well-known features and other interesting characteristics of source code to aid in determining **what the software is** or **what it does**.  It has received attention on [ZDNet](https://www.zdnet.com/article/microsoft-application-inspector-is-now-open-source-so-use-it-to-test-code-security/
), [SecurityWeek](https://www.securityweek.com/microsoft-introduces-free-source-code-analyzer), [CSOOnline](https://www.csoonline.com/article/3514732/microsoft-s-offers-application-inspector-to-probe-untrusted-open-source-code.html), [Linux.com/news](https://www.linux.com/news/microsoft-application-inspector-is-now-open-source-so-use-it-to-test-code-security/), [HelpNetSecurity](https://www.helpnetsecurity.com/2020/01/17/microsoft-application-inspector/
), Twitter and more and was first featured on [Microsoft.com](https://www.microsoft.com/security/blog/2020/01/16/introducing-microsoft-application-inspector/).

Application Inspector is different from traditional static analysis tools in that it doesn't attempt to identify "good" or "bad" patterns; it simply reports what it finds against a set of over 400 rule patterns for feature detection including features that impact security such as the use of cryptography and more.  This can be extremely helpful in reducing the time needed to determine what Open Source or other components do by examining the source directly rather than trusting to limited documentation or recommendations.  

The tool supports scanning various programming languages including C, C++, C#, Java, JavaScript, HTML, Python, Objective-C, Go, Ruby, PowerShell and [more](https://github.com/microsoft/ApplicationInspector/wiki/2.1-Field:-applies_to-(languages-support)) and can scan projects with mixed language files.  It also includes HTML, JSON and text output formats with the default being an HTML report similar to the one shown here.

![AppInspector-Features](https://user-images.githubusercontent.com/47648296/72893326-9c82c700-3ccd-11ea-8944-9831ea17f3e0.png)

It includes a filterable confidence indicator to help minimize false positives matches as well as customizable default rules and conditional match logic.  

Be sure to see our project wiki page for more help https://Github.com/Microsoft/ApplicationInspector/wiki for **illustrations** and additional information and help.

# Goals

Application Inspector helps **inform you better** for choosing the best components to meet your needs with a smaller footprint of unknowns for keeping your application attack surface smaller.  It helps you to avoid inclusion of components with unexpected features you don't want.  

Application Inspector can help **identify feature deltas** or changes between component versions which can be critical for detecting injection of backdoors.

It can be used to **automate detection of features** of interest to identify components that require additional scrutiny as part of your build pipeline or create a repository of metadata regarding all of your enterprise application.

Basically, we created Application Inspector to help us **identify risky third party software components** based on their specific features, but the tool is helpful in many non-security contexts as well. 

Application Inspector v1.0 is now in GENERAL AUDIENCE release status. Your feedback is important to us. If you're interested in contributing, please review the CONTRIBUTING.md.

# Contribute

We have a strong default starting base of Rules for feature detection.  But there are many feature identification patterns yet to be defined and we invite you to **submit ideas** on what you want to see or take a crack at defining a few.  This is a chance to literally impact the open source ecosystem helping provide a tool that everyone can use.  See the [Rules](https://github.com/microsoft/applicationinspector/wiki) section of the wiki for more.  

# Getting Application Inspector

To use Application Inspector, download the relevant binary (either platform-specific or the multi-platform .NET Core release) from the Releases page or see the NuGet Support page in our wiki. If you use the .NET Core version, you will need to have .NET Core 3.1 or later installed.  See the [JustRunIt.md](https://github.com/microsoft/ApplicationInspector/blob/main/JustRunIt.md) or [Build.md](https://github.com/microsoft/ApplicationInspector/blob/main/BUILD.md) files for help.

# Developers 

It might be valuable to consult the project wiki for additional background on Rules, Tags and more used to identify features.  Tags are used as a systematic hierarchical nomenclature e.g. Cryptography.Protocol.TLS to more easily represent features.  The commands may be used programmatically using just the Microsoft.CST.ApplicationInspector.Commands package.

## Usage

Application Inspector is availble as a command line tool or NuGet package and is supported on Windows, Linux, or MacOS.  

```
> dotnet ApplicationInspector.CLI.dll or on *Windows* simply ApplicationInspector.exe <command> <options>

Microsoft Application Inspector

(c) Microsoft Corporation. All rights reserved

ERROR(S):
  No verb selected.

  analyze        Inspect source directory/file/compressed file (.tgz|zip) against defined characteristics

  tagdiff        Compares unique tag values between two source paths

  tagtest        Test (T/F) for presence of custom rule set in source

  exporttags     Export unique rule tags to view what code features may be detected

  verifyrules    Verify custom rules syntax is valid

  packrules      Combine multiple rule files into one file for ease in distribution

  help           Display more information on a specific command.

  version        Display version information.
```

## Examples:

### Command Help
```
  Usage: dotnet ApplicationInspector.CLI.dll [arguments] [options]

  dotnet ApplicationInspector.CLI.dll <no args> -description of available commands
  dotnet ApplicationInspector.CLI.dll <command> <no args> -arg options description for a given command
```

### Analyze Command
```
  Usage: dotnet ApplicationInspector.CLI.dll analyze [arguments] [options]

  Arguments:
  -s, --source-path              Source file or directory to inspect
  
  -m, --match-depth              First match or best match based on confidence level (first|best)

  -o, --output-file-path         Output file path

  -f, --output-file-format       Output format [html|json|text]

  -e, --text-format              Match text format specifiers

  -r, --custom-rules-path        Custom rules file or directory path

  -t, --tag-output-only          Output only identified tags

  -i, --ignore-default-rules     Exclude default rules bundled with application

  -d, --allow-dup-tags           Output contains unique and non-unique tag matches

  -b, --suppress-browser-open    Suppress automatically opening HTML output using default browser

  -c, --confidence-filters       Output only matches with specified confidence <value>,<value> [high|medium|low]

  -k, --file-path-exclusions     Exclude source files (none|default: sample,example,test,docs,.vs,.git)

  -x, --console-verbosity        Console verbosity [high|medium|low|none]

  -l, --log-file-path            Log file path

  -v, --log-file-level           Log file level [Debug|Info|Warn|Error|Trace|Fatal|Off]

  --help                         Display this help screen.

```
##### Scan a project directory, with output sent to "output.html" (default behavior includes launching default browser to this file)
```
  dotnet ApplicationInspector.CLI.dll analyze -s /home/user/myproject 
```
##### Scan using custom rules 
```
  dotnet ApplicationInspector.CLI.dll analyze -s /home/user/myproject -r /home/user/myrules 
```
##### Write to JSON format
```
  dotnet ApplicationInspector.CLI.dll analyze -s /home/user/myproject -f json
```
##### Write just unique tags found to file
```
  dotnet ApplicationInspector.CLI.dll analyze -s /home/user/myproject -f json -t
```
### Tag Diff Command

Use to analyze and report on differences in tags (features) between two project or project versions e.g. v1, v2 to see what changed
```
  Usage: dotnet ApplicationInspector.CLI.dll tagdiff [arguments] [options]

  Arguments:
  --src1                        Source 1 to compare

  --src2                        Source 2 to compare

  -t, --test-type               Type of test to run [equality|inequality]

  -r, --custom-rules-path       Custom rules file or directory path

  -i, --ignore-default-rules    Exclude default rules bundled with application

  -o, --output-file-path        Output file path

  -k, --file-path-exclusions     Exclude source files (none|default: sample,example,test,docs,.vs,.git)

  -x, --console-verbosity       Console verbosity [high|medium|low|none]

  -l, --log-file-path           Log file path

  -v, --log-file-level          Log file level [Debug|Info|Warn|Error|Trace|Fatal|Off]

  --help                        Display this help screen.

```
##### Simplist way to see the delta of tag features between two projects
```
  dotnet ApplicationInspector.CLI.dll tagdiff --src1 /home/user/project1 --src2 /home/user/project2
```
##### Basic use
```
  dotnet ApplicationInspector.CLI.dll tagdiff --src1 /home/user/project1 --src2 /home/user/project2 -t equality
```
##### Basic use
```
  dotnet ApplicationInspector.CLI.dll tagdiff --src1 /home/user/project1 --src2 /home/user/project2 -t inequality
```
### Tag Test Command

Used to verify (pass/fail) that a specified set of rule tags is present or not present in a project e.g.
user only wants to know true/false if cryptography is present as expected or if personal data is not present
as expected and get a simple yes/no result rather than a full analysis report.

Note: The user is expected to use the *custom-rules-path* option rather than the default ruleset because it is 
unlikely that any source package would contain all of the default rules and testing for all default rules present in source
will likely yield a false or fail result in most cases.
```
  Usage: dotnet ApplicationInspector.CLI.dll tagtest [arguments] [options

  Arguments:
  -s, --source-path          Source file or directory to inspect

  -t, --test-type            Test to perform [rulespresent|rulesnotpresent]

  -r, --custom-rules-path    Custom rules file or directory path

  -o, --output-file-path     Output file path

  -k, --file-path-exclusions     Exclude source files (none|default: sample,example,test,docs,.vs,.git)

  -x, --console-verbosity    Console verbosity [high|medium|low|none]

  -l, --log-file-path        Log file path

  -v, --log-file-level       Log file level [Debug|Info|Warn|Error|Trace|Fatal|Off]

  --help                     Display this help screen.

 ```
#### Simplest use to see if a set of rules are all present in a project
```
  dotnet ApplicationInspector.CLI.dll tagtest -s /home/user/project1 -r /home/user/myrules.json
```
#### Basic use
```
  dotnet ApplicationInspector.CLI.dll tagtest -s /home/user/project1 -r /home/user/myrules.json -t rulespresent
```
#### Basic use
```
  dotnet ApplicationInspector.CLI.dll tagtest -s /home/user/project1 -r /home/user/myrules.json -t rulesnotpresent
```
### Export Tags Command

  Simple export of the ruleset tags representing what features are supported for detection
```
  Usage: dotnet ApplicationInspector.CLI.dll exporttags [arguments] [options]

  Arguments:
  -r, --custom-rules-path       Custom rules path

  -i, --ignore-default-rules    Ignore default rules bundled with application. 

  -o, --output-file-path        Path to output file

  -x, --console-verbosity       Console verbosity [high|medium|low]

  --help                        Display this help screen.

```
##### Export default rule tags to console
```
  dotnet ApplicationInspector.CLI.dll exporttags
```
##### Using output file
```
  dotnet ApplicationInspector.CLI.dll exporttags -o /home/user/myproject/exportags.txt
```
##### With custom rules and output file
```
  dotnet ApplicationInspector.CLI.dll exporttags -r /home/user/myproject/customrules -o /hom/user/myproject/exportags.txt
```
### Verify Rules Command

Verify a custom ruleset is compatible and error free for use with Application Inspector.  Note the default ruleset is already
verified as part of the Build process and does not normally require a separate verification.
```
  Usage: dotnet ApplicationInspector.CLI.dll verifyrules [arguments]

  Arguments:

  -d, --verify-default-rules Verify default rules

  -r, --custom-rules-path    Custom rules file or directory path

  -o, --output-file-path     Output file path

  -x, --console-verbosity    Console verbosity [high|medium|low|none]

  -l, --log-file-path        Log file path

  -v, --log-file-level       Log file level [Debug|Info|Warn|Error|Trace|Fatal|Off]

  --help                     Display this help screen.

```
##### Simpliest case
```
  dotnet ApplicationInspector.CLI.dll verifyrules -r /home/user/mycustomrules
```
### Pack Rules Command

Condense multiple rule files into one for ease in distribution with Application Inspector
```
  Usage: dotnet ApplicationInspector.CLI.dll packrules [arguments]

  Arguments:

    -d, --pack-default-rules    Repack default rules.  Automatic on Application Inspector build.

    -r, --custom-rules-path     Custom rules file or directory path

    -o, --output-file-path      Output file path

    -i, --not-indented          Remove indentation from json output

    -x, --console-verbosity     Console verbosity [high|medium|low|none]

    -l, --log-file-path         Log file path

    -v, --log-file-level        Log file level [Debug|Info|Warn|Error|Trace|Fatal|Off]

    --help                      Display this help screen.

```
##### Simplist case to repack default rules into default or alternate location
```
  dotnet ApplicationInspector.CLI.dll packrules -d -o /home/user/myproject/defaultrules.json
```
##### Using custom rules only
```
  dotnet ApplicationInspector.CLI.dll packrules -r /home/user/myproject/customrules -o /home/user/mypackedcustomrules.json
```
# Build Instructions

Building from source requires .NET Core 3.0. Standard dotnet build commands can be run from the root source folder.

### Framework Dependent
```
  dotnet build -c Release
```
### Platform Targeted Portable
```
  dotnet publish -c Release -r win-x86
  dotnet publish -c Release -r linux-x64
  dotnet publish -c Release -r osx-x64
```
