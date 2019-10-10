# Introduction 

Microsoft Application Inspector is an analysis tool that can help identify "interesting" characteristics of source code, and in doing so, can describe **what a piece of software is** and **what it does**. Application Inspector is different from traditional static analysis tools in that it doesn't attempt to identify "good" or "bad" patterns; it will simply describe what it finds.

We created Application Inspector to help us identify risky open source software components based on their specific features, but the output of the tool can be used in other (non-security) contexts as well.

**Application Inspector is currently in PUBLIC PREVIEW.** Functionality may change without warning. Please do not rely on Application Inspector for important workloads, but your feedback is important to us. If you're interested in contributing, please review CONTRIBUTING.md.

# Getting Started

To use Application Inspector, download the relevant binary (either platform-specific or the multi-platform .NET Core release). If you use the .NET Core version, you will need to have .NET Core 3.0 or later installed.

## Usage

Application Inspector is a command-line tool. Simply run from a command line in Windows, Linux, or MacOS.

```
> dotnet ApplicationInspector.dll
Microsoft Application Inspector 1.0.0
ApplicationInspector 1.0.0
(c) Microsoft Corporation. All rights reserved

ERROR(S):
  No verb selected.

  analyze        Inspect source directory/file/compressed file (.tgz|zip) against defined characteristics
  tagdiff        Compares unique tag values between two source paths
  tagtest        Test presence of smaller set or custom tags in source (compare or verify modes)
  exporttags     Export unique rule tags
  verifyrules    Verify rules syntax is valid
  help           Display more information on a specific command.
  version        Display version information.
```

### Examples:

```
# Scan a project directory, with output going to "output.html"
dotnet ApplicationInspector.dll analyze -s /home/user/myproject 

# Add custom rules (can be specified multiple times)
dotnet ApplicationInspector.dll analyze -s /home/user/myproject -r /my/rules/directory -r /my/other/rules

# Write to JSON 
dotnet ApplicationInspector.dll analyze -s /home/user/myproject -f json
```

### Tagdiff Command

Use to analyze and report on differences in tag based matches between two projects e.g. v1, v2

    Usage: dotnet ApplicationInspector.dll tagdiff [arguments] [options]

    Arguments:
     --src1                        Required. Source 1 to compare (required)
     --src2                        Required. Source 2 to compare (required
     -t, --test-type               (Default: equality) Type of test to run [equality|inequality]
     -r, --custom-rules-path       Custom rules path
     -i, --ignore-default-rules    (Default: false) Ignore default rules bundled with application
     -o, --output-file-path        Path to output file
     -x, --console-verbosity       Console verbosity [high|medium|low
     -l, --log-file-path           Log file path
     -v, --log-file-level          Log file level

##### Simplest use

    dotnet ApplicationInspector.dll tagdiff /home/user/project1 /home/user/project2

##### Basic use

    dotnet ApplicationInspector.dll tagdiff /home/user/project1 /home/user/project2 -t equality

Output is a pass/fail result.

##### Basic use

    dotnet ApplicationInspector.dll tagdiff /home/user/project1 /home/user/project2 -t inequality

Output includes list of differences between each.

### TagTest Command

Used to verify (pass/fail) that a specified set of rule tags is present or not present in a project e.g.
user only wants to know true/false if crytography is present as expected or if personal data is not present
as expected and get a simple yes/no result rather than a full analyis report.

The user is expected to use the *custom-rules-path* option rather than the default ruleset because it is 
unlikely that any source package would contain all of the default rules.  Instead, create a custom path and rule set
as needed or specify a path using the custom-rules-path to point only to the rule(s) needed from the default set.  
Otherwise, testing for all default rules present in source will likely yield a false or fail result in most cases.

    Usage: dotnet ApplicationInspector.dll tagtest [arguments] [options

    Arguments:
    -s, --source-path             Required. Source to test (required)
    -t, --test-type               (Default: rulespresent) Test to perform [rulespresent|rulesnotpresent]
    -r, --custom-rules-path       Custom rules path 
    -i, --ignore-default-rules    (Default: true) Ignore default rules bundled with application
    -o, --output-file-path        Path to output file
    -x, --console-verbosity       Console verbosity [high|medium|low
    -l, --log-file-path           Log file path
    -v, --log-file-level          Log file level

#### Simplest way to test source for all default rules present in source

    dotnet ApplicationInspector.dll tagtest /home/user/project1 -r /home/user/myrules.json

#### Rules present test against custom rules only

    dotnet ApplicationInspector.dll tagtest /home/user/project1 -r /home/user/myrules.json -t rulespresent

#### Rules not present test against custom rules only

    dotnet ApplicationInspector.dll tagtest /home/user/project1 -r /home/user/myrules.json -t rulesnotpresent


### ExportTags Command

Simple export of the ruleset schema for tags

    Usage: dotnet ApplicationInspector.dll tags [arguments] [options]

    Arguments:
    -r, --custom-rules-path       Custom rules path
    -i, --ignore-default-rules    (Default: false) Ignore default rules bundled with application
    -o, --output-file-path        Path to output file

##### Using default rules to console

    dotnet ApplicationInspector.dll tags

##### Using output file

    dotnet ApplicationInspector.dll tags -o /home/user/myproject/exportags.txt

##### With custom rules and output file

    dotnet ApplicationInspector.dll tags /home/user/myproject/customrules -o /hom/user/myproject/output-rules.txt

### Verify Command

Verification that ruleset is compatible and error free for import and analysis

    Usage: dotnet ApplicationInspector.dll verify [arguments]

    Arguments:
    -r, --custom-rules-path       Custom rules path
    -i, --ignore-default-rules    (Default: false) Ignore default rules bundled with application

##### Using default rules

    dotnet ApplicationInspector.dll verify

##### Using custom rules

    dotnet ApplicationInspector.dll verify -r /home/user/myproject/customrules -i

# Build Instructions

Building from source requires .NET Core 3.0. Standard dotnet build commands can be run from the root source folder.

### Framework Dependent

    dotnet build -c Release

### Platform Targeted Portable

    dotnet publish -c Release -r win-x86
    dotnet publish -c Release -r linux-x64
    dotnet publish -c Release -r osx-x64
