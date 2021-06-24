#!/usr/bin/env bash

# Stop script if unbound variable found (use ${var:-} if intentional)
set -u

# Stop script if subcommand fails
set -e

bootstrap=false

args=""
while [[ $# > 0 ]]; do
  opt="$(echo "$1" | awk '{print tolower($0)}')"
  case "$opt" in
    --bootstrap)
      bootstrap=true
      shift
      ;;
    *)
    args="$args $1"
    shift
    ;;
  esac
done

source="${BASH_SOURCE[0]}"

# resolve $SOURCE until the file is no longer a symlink
while [[ -h $source ]]; do
  scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"
  source="$(readlink "$source")"

  # if $source was a relative symlink, we need to resolve it relative to the path where the
  # symlink file was located
  [[ $source != /* ]] && source="$scriptroot/$source"
done
scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"

# Import Arcade functions
. "$scriptroot/eng/common/tools.sh"

function BuildBootstrapCompiler {
  InitializeDotNetCli true
  # Build linker
  echo "Building illink"

  local dir="$artifacts_dir/bootstrap"
  local illink_dir="$dir/illink"

  rm -rf $dir
  mkdir -p $dir

  local illink_proj="ILLink.Tasks"
  local illink_proj_path="src/$illink_proj/$illink_proj.csproj"
  dotnet pack -nologo "$illink_proj_path" -p:PackageOutputPath="$dir"
  unzip "$dir/Microsoft.NET.$illink_proj.*.nupkg" -d "$illink_dir"
  chmod -R 755 "$dir"

  # Build csc with trimming
  echo "Building csc"
  local csc_proj_path="test/TestCsc/csc.csproj"
  dotnet publish -nologo --use-current-runtime -o "$dir/csc" "$csc_proj_path" -p:BootstrapLinkerPath="$illink_dir" -bl:"$log_dir/BootstrapCsc.binlog"

  echo "Cleaning bootstrap artifacts"
  dotnet clean -nologo -v:m "$illink_proj_path"
  dotnet clean -nologo -v:m "$csc_proj_path"

  _BootstrapCompilerPath="$dir/csc"
}

if [[ "$bootstrap" == true ]]; then
  BuildBootstrapCompiler
fi

"$scriptroot/eng/common/build.sh" --build --restore -p:BootstrapCscPath="${_BootstrapCompilerPath:-}" $args
