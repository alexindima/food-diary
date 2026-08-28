[CmdletBinding()]
param(
    [string]$BaseRef = 'HEAD',
    [switch]$RepositoryWide,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$changes = [System.Collections.Generic.List[object]]::new()
. (Join-Path $PSScriptRoot 'LlmWikiDependencyManifest.ps1')

function Get-BaseText {
    param([string]$Path)
    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = 'SilentlyContinue'
    $text = git -C $repositoryRoot show "${BaseRef}:$Path" 2>$null
    $exitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorActionPreference
    if ($exitCode -ne 0) { return $null }
    return ($text -join [Environment]::NewLine)
}

function Add-ManifestChange {
    param([string]$Ecosystem, [string]$Manifest, [string]$Name, [string]$Before, [string]$After, [string]$Kind)
    $changes.Add([pscustomobject]@{
        ecosystem = $Ecosystem
        manifest = $Manifest
        package = $Name
        before = $Before
        after = $After
        kind = $Kind
    })
}

function Convert-ToComparableText([string]$Value) {
    if ($null -eq $Value) { return $null }
    return $Value.Replace("`r`n", "`n").Replace("`r", "`n").Trim()
}

$manifestPaths = @(
    @(
        git -C $repositoryRoot ls-files -- '*.csproj' 'Directory.Build.props' ':(glob)**/package.json'
        git -C $repositoryRoot ls-files --others --exclude-standard -- '*.csproj' 'Directory.Build.props' ':(glob)**/package.json'
    ) | Where-Object { $_ } | Sort-Object -Unique
)
$inventory = [System.Collections.Generic.List[object]]::new()
foreach ($path in $manifestPaths) {
    $file = Get-Item -LiteralPath (Join-Path $repositoryRoot $path)
    $beforeText = Get-BaseText $path
    if ($null -eq $beforeText) {
        $beforeText = if ($file.Extension -in @('.csproj', '.props')) { '<Project />' } else { '{}' }
    }
    $afterText = Get-Content -LiteralPath $file.FullName -Raw

    $beforePackages = @{}
    $afterPackages = @{}
    if ($file.Extension -in @('.csproj', '.props')) {
        foreach ($item in @(Get-LlmWikiPackageReferences -XmlText $beforeText)) {
            if ($item.Include) { $beforePackages[[string]$item.Include] = [string]$item.Version }
        }
        foreach ($item in @(Get-LlmWikiPackageReferences -XmlText $afterText)) {
            if ($item.Include) { $afterPackages[[string]$item.Include] = [string]$item.Version }
        }
        $ecosystem = 'nuget'
    } else {
        $beforeJson = $beforeText | ConvertFrom-Json
        $afterJson = $afterText | ConvertFrom-Json
        foreach ($group in @('dependencies', 'devDependencies', 'peerDependencies', 'optionalDependencies')) {
            $beforeGroup = if ($beforeJson.PSObject.Properties[$group]) { $beforeJson.PSObject.Properties[$group].Value } else { $null }
            $afterGroup = if ($afterJson.PSObject.Properties[$group]) { $afterJson.PSObject.Properties[$group].Value } else { $null }
            if ($null -ne $beforeGroup) {
                foreach ($property in @($beforeGroup.PSObject.Properties)) { $beforePackages[$property.Name] = [string]$property.Value }
            }
            if ($null -ne $afterGroup) {
                foreach ($property in @($afterGroup.PSObject.Properties)) { $afterPackages[$property.Name] = [string]$property.Value }
            }
        }
        $ecosystem = 'npm'
    }

    if ($RepositoryWide) {
        $inventory.Add([pscustomobject][ordered]@{
            ecosystem = $ecosystem
            manifest = $path
            packageCount = $afterPackages.Count
            packages = @($afterPackages.GetEnumerator() | Sort-Object Name | ForEach-Object {
                [pscustomobject]@{ name = [string]$_.Name; version = [string]$_.Value }
            })
        })
    }
    if ((Convert-ToComparableText $beforeText) -ceq (Convert-ToComparableText $afterText)) { continue }

    foreach ($name in @($beforePackages.Keys + $afterPackages.Keys | Sort-Object -Unique)) {
        $hasBefore = $beforePackages.ContainsKey($name)
        $hasAfter = $afterPackages.ContainsKey($name)
        if (-not $hasBefore) { Add-ManifestChange $ecosystem $path $name $null $afterPackages[$name] 'added' }
        elseif (-not $hasAfter) { Add-ManifestChange $ecosystem $path $name $beforePackages[$name] $null 'removed' }
        elseif ($beforePackages[$name] -ne $afterPackages[$name]) {
            Add-ManifestChange $ecosystem $path $name $beforePackages[$name] $afterPackages[$name] 'version-changed'
        }
    }
}

$lockfilePaths = @(git -C $repositoryRoot ls-files -- 'package-lock.json' ':(glob)**/package-lock.json' | Where-Object { $_ } | Sort-Object -Unique)
foreach ($path in $lockfilePaths) {
    $beforeText = Get-BaseText $path
    $absolutePath = Join-Path $repositoryRoot $path
    if ($null -eq $beforeText -or -not (Test-Path -LiteralPath $absolutePath)) { continue }
    $afterText = Get-Content -LiteralPath $absolutePath -Raw
    if ((Convert-ToComparableText $beforeText) -cne (Convert-ToComparableText $afterText)) {
        Add-ManifestChange 'npm' $path '(lockfile graph)' $null $null 'lockfile-changed'
    }
}

$uniqueInventoryPackages = @($inventory | ForEach-Object packages | ForEach-Object name | Sort-Object -Unique)
$result = [pscustomobject][ordered]@{
    selectionMode = $(if ($RepositoryWide) { 'repository-inventory' } else { 'change-diff' })
    baseRef = $BaseRef
    changeCount = $changes.Count
    changes = @($changes | Sort-Object ecosystem, manifest, package)
    inventory = $(if ($RepositoryWide) {
        [pscustomobject][ordered]@{
            manifestCount = $inventory.Count
            nugetManifestCount = @($inventory | Where-Object ecosystem -eq 'nuget').Count
            npmManifestCount = @($inventory | Where-Object ecosystem -eq 'npm').Count
            packageReferenceCount = [int](($inventory | Measure-Object packageCount -Sum).Sum)
            uniquePackageCount = $uniqueInventoryPackages.Count
            lockfileCount = $lockfilePaths.Count
            manifests = @($inventory)
        }
    } else { $null })
    evidenceBoundary = $(if ($RepositoryWide) { 'Repository manifests and lockfiles are inventoried locally. Versions are not vulnerability verdicts; use an ecosystem audit with current advisory data for vulnerability conclusions.' } else { 'Only manifest changes relative to BaseRef are reported.' })
}
if ($Format -eq 'Json') {
    $result | ConvertTo-Json -Depth 7
} else {
    if ($RepositoryWide) {
        Write-Host "Dependency inventory: $($result.inventory.manifestCount) manifest(s), $($result.inventory.packageReferenceCount) reference(s), $($result.inventory.uniquePackageCount) unique package(s), $($result.inventory.lockfileCount) lockfile(s)."
    }
    Write-Host "Dependency changes relative to $BaseRef`: $($result.changeCount)"
    foreach ($change in $result.changes) {
        Write-Host " - [$($change.ecosystem)] $($change.kind) $($change.package): '$($change.before)' -> '$($change.after)' ($($change.manifest))"
    }
}
