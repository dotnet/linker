# ILLink.Tasks

ILLink.Tasks is a package containing MSBuild tasks and targets that
will run the linker to run during publish of a .NET Core app.

ILLink.Tasks provides an MSBuild task called ILLink that makes it easy
to run the linker from an MSBuild project file:

```xml
<ILLink AssemblyPaths="@(AssemblyFilesToLink)"
        RootAssemblyNames="@(LinkerRootAssemblies)"
        RootDescriptorFiles="@(LinkerRootDescriptors)"
        OutputDirectory="output"
        ExtraArgs="-t -c link -l none" />
```

For a description of the options that this task supports, see the
comments in [LinkTask.cs](integration/ILLink.Tasks/LinkTask.cs).


In addition, ILLink.Tasks contains MSBuild logic that makes the linker
run automatically during `dotnet publish` for .NET Core apps. This
will:

- Determine the assemblies and options to pass to illink.
- Remove unused native files from the publish output.
- Run crossgen on the linked assemblies to improve startup performance.

The full set of options is described below.

## Building

```
linker> ./corebuild/dotnet.{sh/ps1} restore ./corebuild/integration/linker.sln
linker> ./corebuild/dotnet.{sh/ps1} pack ./corebuild/integration/ILLink.Tasks/ILLink.Tasks.csproj
```

The output package will be placed in
`corebuild/integration/bin/nupkgs`. If you are building on unix, you
will need to adjust
[ILLink.Tasks.nuspec](integration/ILLink.Tasks/ILLink.Tasks.nuspec). Replace
the dll file includes with the following:

`<file src="netcoreapp2.0/**/*.dll" target="tools/netcoreapp2.0" />`

## Using ILLink.Tasks

Add a package reference to the linker. Ensure that either the
[dotnet-core](https://dotnet.myget.org/gallery/dotnet-core) myget feed
or the path to the locally-built linker package path exists in the
project's nuget.config.

After adding the package, linking will be turned on during `dotnet
publish`. The publish output will contain the linked assemblies.

You should make sure to test the publish output before deploying your
code, because the linker can potentially break apps that use
reflection.

## Default behavior

By default, the linker will operate in a conservative mode that keeps
all managed assemblies that aren't part of the framework (they are
kept intact, and the linker simply copies them). It also analyzes all
non-framework assemblies to find and keep code used by them (they are
roots for the analysis). This means that unanalyzed reflection calls
within the app should continue to work after linking. Reflection calls
to code in the framework can potentially break when using the linker,
if the target of the call is removed.

For portable publish, framework assemblies usually do not get
published with the app. In this case they will not be analyzed or
linked.

For self-contained publish, framework assemblies are part of the
publish output, and are analyzed by the linker. Any framework
assemblies that aren't predicted to be used at runtime based on the
linker analysis will be removed from the publish output. Used
framework assemblies will be kept, and any used code within these
assemblies will be compiled to native code. Unused parts of used
framework assemblies are kept as IL, so that reflection calls will
continue to work, with runtime JIT compilation.

Native dependencies that aren't referenced by any of the kept managed
assemblies will be removed from the publish output as well.

## Options

The following MSBuild properties can be used to control the behavior
of the linker, from the command-line (via `dotnet publish
/p:PropertyName=PropertyValue`), or from the .csproj file (via
`<PropertyName>PropertyValue</PropertyName>`). They are defined and
used in
[ILLink.Tasks.targets](integration/ILLink.Tasks/ILLink.Tasks.targets)
and
[ILLink.CrossGen.targets](integration/ILLink.Tasks/ILLink.CrossGen.targets)

- `LinkDuringPublish` (default `true`) - Set to `false` to disable
  linking.

- `ShowLinkerSizeComparison` (default `false`) - Set to `true` to
  print out a table showing the size impact of the linker.

- `RootAllApplicationAssemblies` (default `true`) - If `true`, all
  application assemblies are rooted by the linker. This means they are
  kept in their entirety, and analyzed for dependencies. If `false`,
  only the app dll's entry point is rooted.

- `LinkerRootAssemblies` - The set of assemblies to root. The default
  depends on the value of `RootAllApplicationAssemblies`. Additional
  assemblies can be rooted by adding them to this ItemGroup.

- `LinkerRootDescripotrs` - The set of [xml descriptors](../linker#syntax-of-xml-descriptor)
  specifying additional roots within assemblies. The default is to
  include a generated descriptor that roots everything in the
  application assembly if `RootAllApplicationAssemblies` is
  `true`. Additional roots from descriptors can be included by adding
  the descriptor files to this ItemGroup.

- `ExtraLinkerArgs` - Extra arguments to pass to the linker. The
  default sets some flags that output symbols, tolerate resolution
  errors, log warnings, skip mono-specific localization assemblies,
  and keep type-forwarder assemblies. See
  [ILLink.Tasks.targets](integration/ILLink.Tasks/ILLink.Tasks.targets).
  Setting this will override the defaults.

- Assembly actions: illink has the ability to specify an [action](../linker#actions-on-the-assemblies) to
  take per-assembly. ILLink.Tasks provides high-level switches that
  control the action to take for a set of assemblies. The set of
  managed files that make up the application are split into
  "application" and "platform" assemblies. The "platform" represents
  the .NET framework, while the "application" represents the rest of
  the application and its other dependencies. The assembly action can
  be set for each of these groups independently, for assemblies that
  are analyzed as used and as unused, with the following switches:

  - `UsedApplicationAssemblyAction` - The default is to copy any used
    application assemblies to the output, leaving them as-is.
  - `UnusedApplicationAssemblyAction` - The default is to delete (not
    publish) unused application assemblies.
  - `UsedPlatformAssemblyAction` - For self-contained publish, the
    default is to add the BypassNGenAttribute to unused code in used
    platform assemblies. This causes the native compilation step to
    compile only parts of these assemblies that are used. For portable
    publish, the default is to skip these, because the platform
    assemblies are generally not published with the app.
  - `UnusedPlatformAssemblyAction` - For self-contained publish, the
    default is to delete (not publish) unused platform assemblies. For
    portable publish, the default is to skip.

  The full list of assembly actions is described in
  [AssemblyAction.cs](../linker/Linker/AssemblyAction.cs) Some
  combinations of actions may be disallowed if they do not make
  sense. For more details, see
  [SetAssemblyActions.cs](integration/ILLink.Tasks/SetAssemblyActions.cs).

- `LinkerTrimNativeDeps` (default `true`) - If `true`, enable
  detection and removal of unused native dependencies. If `false`, all
  native dependencies are kept.

- `CrossGenDuringPublish` (default `true`) - If `true`, run crossgen
  on the set of assemblies modified by the linker that were crossgen'd
  before linking. If `false`, just output IL for the linked
  assemblies, even if they were crossgen'd before linking.