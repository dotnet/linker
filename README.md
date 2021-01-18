# Trimming Tools

This repository hosts various tools and msbuild tasks which are used when trimming managed applications with .NET 5 and newer.

## IL Linker

The [IL Linker](src/linker/README.md) is the developer's tool that can be used to produce apps that contain only code and assembly dependencies which are necessary to run the app. It's fully integrated into
.NET SDKs via [ILLink.Tasks](src/ILLink.Tasks/README.md) build task. and exposed via `dotnet publish` trimming [settings](https://docs.microsoft.com/en-us/dotnet/core/deploying/trim-self-contained#trim-your-app---cli).

The linker is always enabled for all size sensitive .NET workloads like Blazor WebAssembly, Xamarin or .NET mobile and can manually enabled for other project types. The default apps trimming setting can be further customized by using range set of [msbuild properties](https://docs.microsoft.com/en-us/dotnet/core/deploying/trimming-options).

## Dependencies Analyzer

The [analyzer](src/analyzer/README.md) is a tool to analyze dependencies which were recorded during linker processing. It tracks details about reasons and connection between elements to keep it in the resulting linked assembly. It can be used to better understand the dependencies between different types and members to help further reduce the linked output.

## Trimming Lens

The [tlens](src/tlens/README.md) is another tool for developers which can be used to explore ways to reduce the size of trimmed apps or exploring libraries readiness for trimming. The tool produces a recommendation where the compiled source could be improved to produce even smaller outputs when trimmed using the linker.

## Source Code Analyzer

Another tool available for developers is implemented as [Roslyn Analyzer](src/ILLink.RoslynAnalyzer) which runs on source code and warns developers about code patterns and APIs which are problematic when building code which should could be used with trimmed apps.

# Contributing

We welcome contributions! Many developers have helped make this project better by reporting [issues](issues) or contributing [pull requests](linker/pulls).

## How to build all projects

There is a shell script available in the root folder which can build the whole project and much more (build.cmd on Windows).

```sh
./build.sh
```

## Running tests from CLI

The same script can be used to run all tests from the terminal. We also have integration into Visual Studio and individual tests or whole test suites can be run within IDE as well.

```sh
./build.sh -test
```


## CI Build & Test Status

[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/mono/linker-ci?branchName=master)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=364&branchName=master)
