# ILLink.Tasks

This library contains MSBuild tasks that run the ILLinker as part of .NET Core toolchain. It uses same code
as ILLinker but exposes the command line argument as MSBuild properties.

More details about how to use the task is in [doc](/doc/illink-tasks.md) folder.

## Building

To build ILLink.Tasks:

```sh
$ dotnet restore illink.sln
$ dotnet pack illink.sln
```

To produce a package:
```sh
$ ./eng/dotnet.{sh/ps1} pack illink.sln
```
