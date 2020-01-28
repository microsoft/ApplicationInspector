# Introduction 

Microsoft Application Inspector is a software source code analysis tool that helps identify and surface well-known features and other interesting characteristics of source code to aid in determining **what the software is** or **what it does**.  It has received attention on [ZDNet](https://www.zdnet.com/article/microsoft-application-inspector-is-now-open-source-so-use-it-to-test-code-security/
), [SecurityWeek](https://www.securityweek.com/microsoft-introduces-free-source-code-analyzer), [CSOOnline](https://www.csoonline.com/article/3514732/microsoft-s-offers-application-inspector-to-probe-untrusted-open-source-code.html), [Linux.com/news](https://www.linux.com/news/microsoft-application-inspector-is-now-open-source-so-use-it-to-test-code-security/), [HelpNetSecurity](https://www.helpnetsecurity.com/2020/01/17/microsoft-application-inspector/
), Twitter and more and was first featured on [Microsoft.com](https://www.microsoft.com/security/blog/2020/01/16/introducing-microsoft-application-inspector/).

Application Inspector is different from traditional static analysis tools in that it doesn't attempt to identify "good" or "bad" patterns; it simply reports what it finds against a set of over 400 rule patterns for feature detection including features that impact security such as the use of cryptography and more.  This can be extremely helpful in reducing the time needed to determine what Open Source or other components do by examining the source directly rather than trusting to limited documentation or recommendations.  

The tool supports scanning various programming languages including C, C++, C#, Java, JavaScript, HTML, Python, Objective-C, Go, Ruby, Powershell and [more](https://github.com/microsoft/ApplicationInspector/wiki/2.1-Field:-applies_to-(languages-support)) and includes html, json and text output formats with the default being an html report similar to the one shown here.

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

To use Application Inspector, download the relevant binary (either platform-specific or the multi-platform .NET Core release). If you use the .NET Core version, you will need to have .NET Core 3.0 or later installed.

It might be valuable to consult the project wiki for additional background on Rules, Tags and more used to identify features.  Tags are used as a systematic heirarchal nomenclature e.g. Cryptography.Protocol.TLS to more easily represent features.

## Usage

Application Inspector is a command-line tool. Run it from a command line in Windows, Linux, or MacOS.  

```
> dotnet AppInspector.dll or on *Windows* simply AppInspector.exe <command> <options>

Microsoft Application Inspector 1.0.17
ApplicationInspector 1.0.17

(c) Microsoft Corporation. All rights reserved

ERROR(S):
  No verb selected.

  analyze        Inspect source directory/file/compressed file (.tgz|zip) against defined characteristics
  tagdiff        Compares unique tag values between two source paths
  tagtest        Test presence of smaller set or custom tags in source (compare or verify modes)
  exporttags     Export default unique rule tags to view what features may be detected
  verifyrules    Verify rules syntax is valid
  help           Display more information on a specific command
  version        Display version information
```

## Examples:

### Command Help
```
  Usage: dotnet AppInspector.dll [arguments] [options]

  dotnet AppInspector.dll -description of available commands
  dotnet AppInspector.dll <command> -options description for a given command
```

### Analyze Command
```
  Usage: dotnet AppInspector.dll analyze [arguments] [options]

  Arguments:
  -s, --source-path             Required. Path to source code to inspect (required)
  -o, --output-file-path        Path to output file.  Ignored with -f html option which auto creates output.html
  -f, --output-file-format      (Default: html) Output format [html|json|text]
  -e, --text-format             (Default: Tag:%T,Rule:%N,Ruleid:%R,Confidence:%X,File:%F,Sourcetype:%t,Line:%L,Sample:%m) 
  -r, --custom-rules-path       Custom rules path
  -t, --tag-output-only         (Default: false) Output only contains identified tags
  -i, --ignore-default-rules    (Default: false) Ignore default rules bundled with application
  -d, --allow-dup-tags          (Default: false) Output only contains non-unique tag matches
  -c, --confidence-filters      (Default: high,medium) Output only if matches rule pattern confidence [<value>,] [high|medium|low]
  -k, --include-sample-paths    (Default: false) Include source files with (sample,example,test,.vs,.git) in pathname in analysis
  -x, --console-verbosity       (Default: medium) Console verbosity [high|medium|low|none]
  -l, --log-file-path           Log file path
  -v, --log-file-level          (Default: Error) Log file level [Debug|Info|Warn|Error|Fatal|Off]
```
##### Scan a project directory, with output sent to "output.html" (default behavior includes launching default browser to this file)
```
  dotnet AppInspector.dll analyze -s /home/user/myproject 
```
##### Add custom rules (can be specified multiple times)
```
  dotnet AppInspector.dll analyze -s /home/user/myproject -r /my/rules/directory -r /my/other/rules
```
##### Write to JSON format
```
  dotnet AppInspector.dll analyze -s /home/user/myproject -f json
```
### Tagdiff Command

Use to analyze and report on differences in tags (features) between two project or project versions e.g. v1, v2 to see what changed
```
  Usage: dotnet AppInspector.dll tagdiff [arguments] [options]

  Arguments:
  --src1                        Required. Source 1 to compare (required)
  --src2                        Required. Source 2 to compare (required
  -t, --test-type               (Default: equality) Type of test to run [equality|inequality]
  -r, --custom-rules-path       Custom rules path
  -i, --ignore-default-rules    (Default: false) Ignore default rules bundled with application
  -o, --output-file-path        Path to output file
  -x, --console-verbosity       Console verbosity [high|medium|low
  -l, --log-file-path           Log file path
  -v, --log-file-level          Log file level [error|trace|debug|info]
```
##### Simplist way to see the delta in tag features between two projects
```
  dotnet AppInspector.dll tagdiff --src1 /home/user/project1 --src2 /home/user/project2
```
##### Basic use
```
  dotnet AppInspector.dll tagdiff --src1 /home/user/project1 --src2 /home/user/project2 -t equality
```
##### Basic use
```
  dotnet AppInspector.dll tagdiff --src1 /home/user/project1 --src2 /home/user/project2 -t inequality
```
### TagTest Command

Used to verify (pass/fail) that a specified set of rule tags is present or not present in a project e.g.
user only wants to know true/false if crytography is present as expected or if personal data is not present
as expected and get a simple yes/no result rather than a full analyis report.

Note: The user is expected to use the *custom-rules-path* option rather than the default ruleset because it is 
unlikely that any source package would contain all of the default rules.  Instead, create a custom path and rule set
as needed or specify a path using the custom-rules-path to point only to the rule(s) needed from the default set.  
Otherwise, testing for all default rules present in source will likely yield a false or fail result in most cases.
```
  Usage: dotnet AppInspector.dll tagtest [arguments] [options

  Arguments:
  -s, --source-path             Required. Source to test (required)
  -t, --test-type               (Default: rulespresent) Test to perform [rulespresent|rulesnotpresent]
  -r, --custom-rules-path       Custom rules path 
  -i, --ignore-default-rules    (Default: true) Ignore default rules bundled with application
  -o, --output-file-path        Path to output file
  -x, --console-verbosity       Console verbosity [high|medium|low
  -l, --log-file-path           Log file path
  -v, --log-file-level          Log file level
```
#### Simplest use to see if a set of rules are all present in a project
```
  dotnet AppInspector.dll tagtest -s /home/user/project1 -r /home/user/myrules.json
```
#### Basic use
```
  dotnet AppInspector.dll tagtest -s /home/user/project1 -r /home/user/myrules.json -t rulespresent
```
#### Basic use
```
  dotnet AppInspector.dll tagtest -s /home/user/project1 -r /home/user/myrules.json -t rulesnotpresent
```
### ExportTags Command

  Simple export of the ruleset schema for tags representing what features are supported for detection
```
  Usage: dotnet AppInspector.dll exporttags [arguments] [options]

  Arguments:
  -r, --custom-rules-path       Custom rules path
  -i, --ignore-default-rules    (Default: false) Ignore default rules bundled with application
  -o, --output-file-path        Path to output file
  -x, --console-verbosity       Console verbosity [high|medium|low
```
##### Export default rule tags to console
```
  dotnet AppInspector.dll exporttags
```
##### Using output file
```
  dotnet AppInspector.dll exporttags -o /home/user/myproject/exportags.txt
```
##### With custom rules and output file
```
  dotnet AppInspector.dll exporttags -r /home/user/myproject/customrules -o /hom/user/myproject/exportags.txt
```
### Verify Command

Verification that ruleset is compatible and error free for import and analysis
```
  Usage: dotnet AppInspector.dll verifyrules [arguments]

  Arguments:
  -r, --custom-rules-path       Custom rules path
  -i, --ignore-default-rules    (Default: false) Ignore default rules bundled with application
  -o, --output-file-path        Path to output file
  -x, --console-verbosity       Console verbosity [high|medium|low
```
##### Simplist case to verify default rules
```
  dotnet AppInspector.dll verifyrules
```
##### Using custom rules only
```
  dotnet AppInspector.dll verifyrules -r /home/user/myproject/customrules -i
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
