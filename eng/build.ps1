[CmdletBinding(PositionalBinding=$false)]
Param(
  [switch] $integrationTest,
  [Parameter(ValueFromRemainingArguments=$true)][String[]]$remaining
)

. (Join-Path $PSScriptRoot "common/tools.ps1")

echo $ArtifactsDir

$args = $remaining.Clone()

if ($integrationTest) {
    dotnet build (Join-Path $PSScriptRoot bootstrap.proj)
    $args += "-integrationTest"
    $args += "/p:BootstrapBuildPath=$ArtifactsDir/bootstrap"
}

powershell -ExecutionPolicy ByPass -NoProfile (Join-Path $PSScriptRoot "common/build.ps1") @args