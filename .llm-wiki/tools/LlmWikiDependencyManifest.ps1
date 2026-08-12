Set-StrictMode -Version Latest

function Get-LlmWikiPackageReferences {
    param([Parameter(Mandatory)][string]$XmlText)

    $project = ([xml]$XmlText).Project
    if ($null -eq $project -or -not $project.PSObject.Properties['ItemGroup']) { return @() }
    @($project.ItemGroup | ForEach-Object {
        if ($null -ne $_ -and $_.PSObject.Properties['PackageReference']) {
            @($_.PackageReference)
        }
    } | Where-Object { $null -ne $_ } | ForEach-Object {
        [pscustomobject]@{
            Include = $(if ($_.PSObject.Properties['Include']) { [string]$_.Include } else { '' })
            Version = $(if ($_.PSObject.Properties['Version']) { [string]$_.Version } else { '' })
        }
    })
}
