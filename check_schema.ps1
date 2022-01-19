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
    if (($x[0] + $x[1] + $x[2]).contains("ILLink.LinkAttributes.xsd") ) {
        $passed = Test-XmlBySchema -XmlFile $source_file.FullName -SchemaPath $schemaFile.FullName -Verbose
        if ($passed -eq $false) { echo $source_file.FullName + " failed"}
        $allGood = $allGood -and $passed
    }
    
}
set-location $baseDir
echo "All Tests: " + $allGood