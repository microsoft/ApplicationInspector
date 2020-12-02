# Introduction 
![CodeQL](https://github.com/microsoft/ApplicationInspector/workflows/CodeQL/badge.svg) ![Nuget](https://img.shields.io/nuget/v/Microsoft.CST.ApplicationInspector.Cli?link=https://www.nuget.org/packages/Microsoft.CST.ApplicationInspector.CLI/&link=https://www.nuget.org/packages/Microsoft.CST.ApplicationInspector.CLI/) ![Nuget](https://img.shields.io/nuget/dt/Microsoft.CST.ApplicationInspector.Cli?link=https://www.nuget.org/packages/Microsoft.CST.ApplicationInspector.CLI/&link=https://www.nuget.org/packages/Microsoft.CST.ApplicationInspector.CLI/)

Microsoft Application Inspector is a software source code characterization tool that helps **identify coding features of first or third party software components** based on well-known library/API calls and is helpful in security and non-security use cases. It uses hundreds of rules and regex patterns to surface interesting characteristics of source code to aid in determining **what the software is** or **what it does** from what file operations it uses, encryption, shell operations, cloud API's, frameworks and more and has received industry attention as a new and valuable contribution to OSS on [ZDNet](https://www.zdnet.com/article/microsoft-application-inspector-is-now-open-source-so-use-it-to-test-code-security/
), [SecurityWeek](https://www.securityweek.com/microsoft-introduces-free-source-code-analyzer), [CSOOnline](https://www.csoonline.com/article/3514732/microsoft-s-offers-application-inspector-to-probe-untrusted-open-source-code.html), [Linux.com/news](https://www.linux.com/news/microsoft-application-inspector-is-now-open-source-so-use-it-to-test-code-security/), [HelpNetSecurity](https://www.helpnetsecurity.com/2020/01/17/microsoft-application-inspector/
), Twitter and more and was first featured on [Microsoft.com](https://www.microsoft.com/security/blog/2020/01/16/introducing-microsoft-application-inspector/).

Application Inspector is different from traditional static analysis tools in that it doesn't attempt to identify "good" or "bad" patterns; it simply reports what it finds against a set of over 400 rule patterns for feature detection including features that impact security such as the use of cryptography and more.  This can be extremely helpful in reducing the time needed to determine what Open Source or other components do by examining the source directly rather than trusting to limited documentation or recommendations.  

The tool supports scanning various programming languages including C, C++, C#, Java, JavaScript, HTML, Python, Objective-C, Go, Ruby, PowerShell and [more](https://github.com/microsoft/ApplicationInspector/wiki/3.4-Applies_to-(languages)) and can scan projects with mixed language files.  It supports generating results in HTML, JSON and text output formats with the **default being an HTML report** similar to the one shown here.

![AppInspector-Features](https://user-images.githubusercontent.com/47648296/72893326-9c82c700-3ccd-11ea-8944-9831ea17f3e0.png)

Be sure to see our complete project wiki page https://Github.com/Microsoft/ApplicationInspector/wiki for additional information and help.

# Goals

Microsoft Application Inspector helps you in securing your applications from start to deployment.

**Design Choices** - Enables you to choose which components meet your needs with a smaller footprint of unnecessary or unknowns features for keeping your application attack surface smaller as well as help to verify expected ones i.e. industry standard crypto only. 

**Identifying Feature Deltas** - Detects changes between component versions which can be critical for detecting injection of backdoors.

**Automating Security Compliance Checks** - Use to identify components with features that require additional security scrutiny, approval or SDL compliance as part of your build pipeline or create a repository of metadata regarding all of your enterprise application.

# Contribute

We have a strong default starting base of Rules for feature detection.  But there are many feature identification patterns yet to be defined and we invite you to **submit ideas** on what you want to see or take a crack at defining a few.  This is a chance to literally impact the open source ecosystem helping provide a tool that everyone can use.  See the [Rules](https://github.com/microsoft/ApplicationInspector/wiki/3.-Understanding-Rules) section of the wiki for more.  

# Official Releases

Application Inspector is in GENERAL AUDIENCE release status. Your feedback is important to us. If you're interested in contributing, please review the CONTRIBUTING.md.

Application Inspector is availble as a command line tool or NuGet package and is supported on Windows, Linux, or MacOS. 

Platform specific binaries of the ApplicationInspector CLI are available on our GitHub [releases page](https://github.com/microsoft/ApplicationInspector/releases).

The C# library is available on NuGet as [Microsoft.CST.ApplicationInspector.Commands](https://www.nuget.org/packages/Microsoft.CST.ApplicationInspector.Commands/).

The .NET Global Tool is available on NuGet as [Microsoft.CST.ApplicationInspector.CLI](https://www.nuget.org/packages/Microsoft.CST.ApplicationInspector.CLI/).

If you use the .NET Core version, you will need to have .NET Core 3.1 or later installed.  See the [JustRunIt.md](https://github.com/microsoft/ApplicationInspector/blob/master/JustRunIt.md) or [Build.md](https://github.com/microsoft/ApplicationInspector/blob/master/BUILD.md) files for more.

# Basic CLI Usage

```
> dotnet ApplicationInspector.CLI.dll or on *Windows* simply ApplicationInspector.CLI.exe <command> <options>

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

For additional help on use of the console interface see [CLI Usage](https://github.com/microsoft/ApplicationInspector/wiki/1.-CLI-Usage).  

For help using the NuGet package see [NuGet Support](https://github.com/microsoft/ApplicationInspector/wiki/2.-NuGet-Support)

```
# Build Instructions

See [build.md](https://github.com/microsoft/ApplicationInspector/blob/main/BUILD.md)

