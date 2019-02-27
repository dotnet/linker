#!/usr/bin/env bash

source="${BASH_SOURCE[0]}"
scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"
. "$scriptroot/../eng/common/tools.sh"

InitializeDotNetCli true
echo "$_InitializeDotNetCli/dotnet" "$@"
"$_InitializeDotNetCli/dotnet" "$@"
exit $?
