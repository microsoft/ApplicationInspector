# Build How-to

## Pre-requisites

### .NET Core:
- .NET Core 3.1 or better (https://dotnet.microsoft.com/download/dotnet-core/3.1)
- Download the application code as a zip and expand or clone in Visual Studio 2019 and simply build in IDE

## Commandline 

Run these commands in the ```ApplicationInspector``` directory.

### Building a Debug version

Windows
```
dotnet build
```

Linux/Mac
```
dotnet build
```

### Building a Release version

#### Platform Targeted Portable
```
dotnet publish -c Release -r win10-x64
dotnet publish -c Release -r win-x86
dotnet publish -c Release -r linux-x64
dotnet publish -c Release -r osx-x64
```

#### Framework Dependent
```
  dotnet build -c Release
```

 
