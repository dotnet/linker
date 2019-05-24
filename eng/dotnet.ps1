. $PSScriptRoot/common/tools.ps1

$useInstalledDotNetCli = $false
$dotnetRoot = InitializeDotNetCli -install:$true
Join-Path $dotnetRoot "dotnet.exe"
$exitCode = Exec-Process (Join-Path $dotnetRoot "dotnet.exe") $args
exit $exitCode
