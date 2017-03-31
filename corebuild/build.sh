#!/usr/bin/env bash

# build.sh will bootstrap the cli and ultimately call "dotnet build"..

working_tree_root="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
$working_tree_root/dotnet.sh build ../linker/Mono.Linker.csproj -c netcore_Debug $@
exit $?
