# Build How-to

## Pre-requisites

### CLI + GUI:
- .NET Core 3.0 or better (https://dotnet.microsoft.com/download/dotnet-core/3.0)

## Building

Run these commands in the ```ApplicationInspector``` directory.

### Building a Debug version

Windows
```
dotnet build
```

Linux/Mac
```
make
```

### Building a Release version

Windows
```
dotnet publish -c Release -r win10-x64
```

Linux/Mac
```
make release
```
