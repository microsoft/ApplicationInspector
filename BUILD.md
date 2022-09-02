# Build How-to

## Pre-requisites

### .NET:
- .NET 6.0 (https://dotnet.microsoft.com/download/)

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

 
