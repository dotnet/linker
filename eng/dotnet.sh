#!/usr/bin/env bash

source="${BASH_SOURCE[0]}"
scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"
. "$scriptroot/common/tools.sh"

use_installed_dotnet_cli=false
InitializeDotNetCli true
echo "$_InitializeDotNetCli/dotnet" "$@"
"$_InitializeDotNetCli/dotnet" "$@"
exit $?
