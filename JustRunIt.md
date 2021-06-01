# Overview

If you're not looking to build the application and simply want to download it and start using it.  All you really need for setup is
the .NET Core pre-requisites.

## Get Application Inspector
### .NET Tool (recommended)
#### Pre-Requisite
- Download and install the .NET Core 5.0 [SDK](https://dotnet.microsoft.com/download/dotnet-core/5.0)

#### Install
- Use the dotnet command `dotnet tool install --global Microsoft.CST.ApplicationInspector.CLI` See more in the [wiki](https://github.com/microsoft/ApplicationInspector/wiki/7.-NuGet-Support)

### Platform Dependent Binary
- Download Application Inspector by selecting the pre-built package for the operating system of choice shown under the Assets section
of the [Releases](https://github.com/microsoft/ApplicationInspector/releases) page and expand the files to your local system drive
using the decompression utility of choice e.g. WinZip, 7zip, Gzip etc. 

## Start Using It

Nuget Tool: Run Application Inspector against your source code: `appinspector analyze -s path/to/src`.

## Getting Help

You can also get help directly from the application by simply typing in the application name without a command
or with a command to get the required and optional arguments for a given command.

* `appinspector --help` - to get a list of available top level commands
* `appinspector [command] --help` - to get a list of required and optional arguments to supply with the selected command
