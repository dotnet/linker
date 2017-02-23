#!/usr/bin/env bash

working_tree_root="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
$working_tree_root/run.sh restore -Project=linker/Mono.Linker.csproj -Configuration=netcore_Debug $@
$working_tree_root/run.sh restore -Project=cecil/Mono.Cecil.csproj -Configuration=netstandard_Debug $@
 
