Set-StrictMode -Version Latest

function Get-LlmWikiPackageReferences {
    param([Parameter(Mandatory)][string]$XmlText, [switch]$IncludeCentralVersions)

    $project = ([xml]$XmlText).Project
    if ($null -eq $project -or -not $project.PSObject.Properties['ItemGroup']) { return @() }
    @($project.ItemGroup | ForEach-Object {
        foreach ($elementName in @('PackageReference') + $(if ($IncludeCentralVersions) { @('PackageVersion') } else { @() })) {
            if ($null -ne $_ -and $_.PSObject.Properties[$elementName]) {
                @($_.PSObject.Properties[$elementName].Value)
            }
        }
    } | Where-Object { $null -ne $_ } | ForEach-Object {
        [pscustomobject]@{
            Include = $(if ($_.PSObject.Properties['Include']) { [string]$_.Include } elseif ($_.PSObject.Properties['Update']) { [string]$_.Update } else { '' })
            Version = $(if ($_.PSObject.Properties['Version']) { [string]$_.Version } else { '' })
        }
    })
}
