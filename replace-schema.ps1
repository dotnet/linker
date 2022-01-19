$xmlfiles = Get-childItem test -recurse -filter *.xml
$schemaFile = Get-ChildItem 'C:\Users\jschuster\source\linker\src\ILLink.Shared\ILLink.LinkAttributes.xsd'
$baseDir = Get-Location
$allFiles = Get-ChildItem test -recurse -filter *.cs 
$allGood = $true

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

foreach ($csfile in $allFiles) 
{
    $x = select-string '\[SetupLinkAttributesFile \("(.*.xml)"\)\]' -path $csfile
    if($x) 
    {
        $xmlfilename = $x.matches.groups[1].value
        set-location $csfile.Directory
        $x = Get-Content $xmlfilename
        $dest_file = $xmlfilename
        $schemaLocation = Resolve-Path -Relative $schemaFile
        # echo $source_file + " analyzed, schema relative path: " + $schemaLocation
        $x[0] = $x[0] -replace "(?<line>.*)src\\ILLink.Shared\\.*.xsd", '${line}src\ILLink.Shared\ILLink.LinkAttributes.xsd'
        $x[1] = $x[1] -replace "(?<line>.*)src\\ILLink.Shared\\.*.xsd", '${line}src\ILLink.Shared\ILLink.LinkAttributes.xsd'
        $x[2] = $x[2] -replace "(?<line>.*)src\\ILLink.Shared\\.*.xsd", '${line}src\ILLink.Shared\ILLink.LinkAttributes.xsd'
        $x | Out-File $dest_file
        $passed = Test-XmlBySchema -XmlFile $dest_file -SchemaPath $schemaFile.FullName
        echo $xmlfilename + " processed"
        if ($passed -eq $false) { echo $dest_file + " failed"}
        $allGood = $allGood -and $passed
    }
}

cd $baseDir