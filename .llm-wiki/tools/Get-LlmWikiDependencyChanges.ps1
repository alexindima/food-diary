[CmdletBinding()]
param(
    [string]$BaseRef = 'HEAD',
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$changes = [System.Collections.Generic.List[object]]::new()

function Get-BaseText {
    param([string]$Path)
    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = 'SilentlyContinue'
    $text = git show "${BaseRef}:$Path" 2>$null
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

$manifestPaths = @(
    @(
        git ls-files '*.csproj' 'Directory.Build.props' 'package.json'
        git ls-files --others --exclude-standard -- '*.csproj' 'Directory.Build.props' 'package.json'
    ) | Where-Object { $_ } | Sort-Object -Unique
)
foreach ($path in $manifestPaths) {
    $file = Get-Item -LiteralPath (Join-Path $repositoryRoot $path)
    $beforeText = Get-BaseText $path
    if ($null -eq $beforeText) {
        $beforeText = if ($file.Extension -in @('.csproj', '.props')) { '<Project />' } else { '{}' }
    }
    $afterText = Get-Content -LiteralPath $file.FullName -Raw
    if ($beforeText.Trim() -eq $afterText.Trim()) { continue }

    $beforePackages = @{}
    $afterPackages = @{}
    if ($file.Extension -in @('.csproj', '.props')) {
        foreach ($item in ([xml]$beforeText).Project.ItemGroup.PackageReference) {
            if ($item.Include) { $beforePackages[[string]$item.Include] = [string]$item.Version }
        }
        foreach ($item in ([xml]$afterText).Project.ItemGroup.PackageReference) {
            if ($item.Include) { $afterPackages[[string]$item.Include] = [string]$item.Version }
        }
        $ecosystem = 'nuget'
    } else {
        $beforeJson = $beforeText | ConvertFrom-Json
        $afterJson = $afterText | ConvertFrom-Json
        foreach ($group in @('dependencies', 'devDependencies', 'peerDependencies', 'optionalDependencies')) {
            $beforeGroup = $beforeJson.$group
            $afterGroup = $afterJson.$group
            if ($null -ne $beforeGroup) {
                foreach ($property in @($beforeGroup.PSObject.Properties)) { $beforePackages[$property.Name] = [string]$property.Value }
            }
            if ($null -ne $afterGroup) {
                foreach ($property in @($afterGroup.PSObject.Properties)) { $afterPackages[$property.Name] = [string]$property.Value }
            }
        }
        $ecosystem = 'npm'
    }

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

$lockfilePaths = @(git ls-files 'package-lock.json' | Where-Object { $_ } | Sort-Object -Unique)
foreach ($path in $lockfilePaths) {
    $beforeText = Get-BaseText $path
    $absolutePath = Join-Path $repositoryRoot $path
    if ($null -eq $beforeText -or -not (Test-Path -LiteralPath $absolutePath)) { continue }
    $afterText = Get-Content -LiteralPath $absolutePath -Raw
    if ($beforeText.Trim() -ne $afterText.Trim()) {
        Add-ManifestChange 'npm' $path '(lockfile graph)' $null $null 'lockfile-changed'
    }
}

$result = [pscustomobject]@{
    baseRef = $BaseRef
    changeCount = $changes.Count
    changes = @($changes | Sort-Object ecosystem, manifest, package)
}
if ($Format -eq 'Json') {
    $result | ConvertTo-Json -Depth 7
} else {
    Write-Host "Dependency changes: $($result.changeCount)"
    foreach ($change in $result.changes) {
        Write-Host " - [$($change.ecosystem)] $($change.kind) $($change.package): '$($change.before)' -> '$($change.after)' ($($change.manifest))"
    }
}
