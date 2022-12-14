# XInstall2 -- a DotNet console application that use xml to drive installation

XInstall2 is a dotnet console application that allow flxible configuration via xml
to handle day-to-day console tasks

The application also provide a plugin system to allow users develop their own actions
for their particular environment

## Building XInstall2 Console App
To build XInstall2, cd `into ./XInstall2` directory, and execute the following command

    dotnet publish

This will generate necessary binary files under ./XInstall2/XInstall2/bin/Debug/net7.0/publish,

        [5.2M]  .
    ├── [ 76K]  Actions.dll
    ├── [ 38K]  Actions.pdb
    ├── [ 71K]  Core.dll
    ├── [ 42K]  Core.pdb
    ├── [ 27K]  Microsoft.Win32.SystemEvents.dll
    ├── [179K]  System.CodeDom.dll
    ├── [261K]  System.Data.SqlClient.dll
    ├── [123K]  System.DirectoryServices.dll
    ├── [169K]  System.Drawing.Common.dll
    ├── [ 69K]  System.Management.dll
    ├── [ 96K]  System.Security.Permissions.dll
    ├── [ 25K]  System.Windows.Extensions.dll
    ├── [ 12K]  Util.dll
    ├── [ 12K]  Util.pdb
    ├── [130K]  XInstall2
    ├── [ 19K]  XInstall2.deps.json
    ├── [ 12K]  XInstall2.dll
    ├── [ 15K]  XInstall2.pdb
    ├── [ 139]  XInstall2.runtimeconfig.json
    └── [3.9M]  runtimes
        ├── [1.3M]  unix
        │   └── [1.3M]  lib
        │       ├── [932K]  netcoreapp2.1
        │       │   └── [932K]  System.Data.SqlClient.dll
        │       └── [409K]  netcoreapp3.0
        │           └── [409K]  System.Drawing.Common.dll
        ├── [2.1M]  win
        │   └── [2.1M]  lib
        │       ├── [649K]  netcoreapp2.0
        │       │   ├── [361K]  System.DirectoryServices.dll
        │       │   └── [288K]  System.Management.dll
        │       ├── [999K]  netcoreapp2.1
        │       │   └── [999K]  System.Data.SqlClient.dll
        │       └── [519K]  netcoreapp3.0
        │           ├── [ 51K]  Microsoft.Win32.SystemEvents.dll
        │           ├── [427K]  System.Drawing.Common.dll
        │           └── [ 41K]  System.Windows.Extensions.dll
        ├── [170K]  win-arm64
        │   └── [170K]  native
        │       └── [170K]  sni.dll
        ├── [156K]  win-x64
        │   └── [156K]  native
        │       └── [156K]  sni.dll
        └── [133K]  win-x86
            └── [133K]  native
                └── [133K]  sni.dll

## Executing XInstall2 Console App
To run the Install2 console app, execute the following command:

   dotnet run bin/Debug/net7.0/XInstall2 -loadxml:../conf/Config.err.xml 

Note: if -loadxml: ... does not specify from the command line, XInstall2 will look for
Config.xml from the same directory

## XInstall2 Code Structure
XInstall2 is composed by three parts,

* Core - locate at ./Core directory. The part is taking charge of loading, parsing, and executing XML document
* Actions - locate at ./Actions directory. This is a builitin support functions (action is XInstall2's term).
  This is the power of XInstall2. One can also define a custom actions.
* Util - localte at ./Util is a collection of classes that support error handling and logging

