$files = Get-childItem test -recurse -filter *.xml
$schemaFile = Get-ChildItem 'C:\Users\jschuster\source\linker\src\ILLink.Shared\ILLink.LinkAttributes.xsd'
$baseDir = Get-Location

function Test-XmlBySchema
{
    [CmdletBinding()]
    [OutputType([bool])]
    param
    (
        [Parameter(Mandatory)]
        [ValidateScript({ Test-Path -Path $_ })]
        [ValidatePattern('\.xml')]
        [string]$XmlFile,
        [Parameter(Mandatory)]
        [ValidateScript({ Test-Path -Path $_ })]
        [ValidatePattern('\.xsd')]
        [string]$SchemaPath
    )

    try
    {
        [xml]$xml = Get-Content $XmlFile
        $xml.Schemas.Add('', $SchemaPath) | Out-Null
        $xml.Validate($null)
        Write-Verbose "Successfully validated $XmlFile against schema ($SchemaPath)"
        $result = $true
    }
    catch
    {
        $err = $_.Exception.Message
        Write-Verbose "Failed to validate $XmlFile against schema ($SchemaPath)`nDetails: $err"
        $result = $false
    }
    finally
    {
        $result
    }
}

$allGood = $true
foreach ($source_file in $files)
{
    $x = Get-Content $source_file
    set-location $source_file.Directory
    $dest_file = $source_file.FullName
    $schemaLocation = Resolve-Path -Relative $schemaFile
    # echo $source_file + " analyzed, schema relative path: " + $schemaLocation
    $x[0] = $x[0] -replace "<linker>", ('<linker xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xsi:schemaLocation="https://www.w3schools.com ' + $schemaLocation + '">')
    $x[1] = $x[1] -replace "<linker>", ('<linker xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xsi:schemaLocation="https://www.w3schools.com ' + $schemaLocation + '">')
    $x[2] = $x[2] -replace "<linker>", ('<linker xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xsi:schemaLocation="https://www.w3schools.com ' + $schemaLocation + '">')
    $x | Out-File $dest_file
    if (($x[0] + $x[1] + $x[2]).contains("<linker xmlns:") ) {
        $passed = Test-XmlBySchema -XmlFile $dest_file -SchemaPath $schemaFile.FullName
        if ($passed -eq $false) { echo $dest_file + " failed"}
        $allGood = $allGood -and $passed
    }
    
}
set-location $baseDir
echo "All Tests: " + $allGood