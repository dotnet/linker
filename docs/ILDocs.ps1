# Tool for updating Microsoft docs to match the warnings produced by the linker and analyzers

Param (
    # Path to SharedStrings.resx
    [string] $SharedStringsPath,
    # Path to the dotnet/docs folder containing "ilXXXX.md" 
    [string] $ILDocsPath,
    # Path to DiagnosticIds.cs
    [string] $DiagnosticIdsPath,
    # Flag to update the files in place. Otherwise only shows the git diff
    [switch] $Update)

$SharedStringsFile = [XML](Get-Content $SharedStringsPath)
$xmlRoot = $SharedStringsFile.root
$ILFiles = Get-ChildItem $ILDocsPath | Where-Object {$_.Name -match "il[0-9][-0-9][0-9][0-9].md" }



$titles = $xmlRoot.ChildNodes | Where-Object { $_.name -match ".*Title$"}
$messages = $xmlRoot.ChildNodes | Where-Object { $_.name -match ".*MessageDocs$"}

$map = @{}
foreach ($title in $titles)
{
    $title.name -match "(.*)Title" | out-null
    $codeName = $Matches[1]

    # Find code number in DiagnosticIds.cs
    $pattern = "^\s*" + $codeName + "\s*=\s*([0-9][-0-9][0-9][0-9])"
    $match = Select-String -Path $DiagnosticIdsPath -Pattern $pattern
    # Skip if no match
    try { $code = $match.Matches.Groups[1].Value }
    catch{
        echo "Could not find diagnostic code for $codeName"
        continue
    }

    $pattern = '(.*)(<!--IL.*-->)*(^\s*<data name=\"' + $codeName + ".*)"
    # $replace = '$1<!-- IL' + $code + ' -->
# $3'
    $titleval = $title.value
    $docsMessage = $xmlRoot.ChildNodes | Where-Object {$_.name -eq "${codeName}MessageDocs"} 
    $map.Add($code, @($titleval, $docsMessage.value))
}

$tmpFolder = Join-Path $env:TEMP "trim-warnings"
mkdir $tmpFolder -ErrorAction SilentlyContinue
foreach ($file in $ILFiles)
{
    $file.Name -match "il([0-9][0-9][0-9][0-9])" | out-null
    $ilNumber = $Matches[1]
    $pair = $map[$ilNumber]

    if ($null -eq $pair) # Didn't find the code in DiagnosticIds.cs
    {
        if ($Update) { continue }
        # Still copy folder for git diff
        $oldContents = (Get-Content $file)
        $outfile =  Join-Path $tmpFolder ("il" + $ilNumber + ".md")
        Out-File $outfile -InputObject $oldContents
        continue
    }

    $title = $pair[0]
    $message = $pair[1]
    # Replace heading and error message
    $DocTitleFindPattern = '(?smi)# Trim Warning (IL[0-9][0-9][0-9][0-9]: )[^#]*(##)'
    $DocTitleReplacePattern = '# Trim Warning $1' + $title + '

'+$message+'

$2'
    $updated = (Get-Content $file -Raw) -replace $DocTitleFindPattern, $DocTitleReplacePattern

    # Replace Metadata title and description
    $titleFindPattern = '(^title:.*IL[0-9][0-9][0-9][0-9]:).*'
    $titleReplacePattern = '$1 ' + $title + '"'
    $updated = $updated -replace $titleFindPattern, $titleReplacePattern

    $descriptionFindPattern = '^(description: "Learn .*IL[0-9][0-9][0-9][0-9]).*$'
    $descriptionReplacePattern = '$1"'
    $updated = $updated -replace $descriptionFindPattern, $descriptionReplacePattern

    # Output to temp file to compare, or replace the file in place
    $outfile = Join-Path $tmpFolder ("il" + $ilNumber + ".md")
    if ($Update) {$outfile = $file}
    Out-File $outfile -InputObject $updated
}
# Show diff if not replacing in place
if (-not ($Update))
{
    $originalFiles = Join-Path $ILDocsPath ""
    $newfiles = Join-Path $tmpFolder ""
    git diff --no-index $originalFiles $newfiles 
}