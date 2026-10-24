[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$fixtureParent = Join-Path $repositoryRoot '.artifacts/cleanup-tests'
$fixtureRoot = Join-Path $fixtureParent ([guid]::NewGuid().ToString('N'))
$fixtureRepository = Join-Path $fixtureRoot 'repository'
$fixtureScript = Join-Path $fixtureRepository 'scripts/Clean-NestedDotnetArtifacts.ps1'
$powershellPath = (Get-Process -Id $PID).Path
$fixtureLinks = [Collections.Generic.List[string]]::new()

function Write-Fixture([string]$RelativePath) {
    $path = Join-Path $fixtureRepository $RelativePath
    [void][IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($path))
    [IO.File]::WriteAllText($path, 'preserve unless explicitly targeted')
    return $path
}

function Invoke-Cleanup([string[]]$Arguments) {
    $output = & $powershellPath -NoLogo -NoProfile -File $fixtureScript @Arguments 2>&1 | Out-String
    return [pscustomobject]@{ exitCode = $LASTEXITCODE; output = $output }
}

try {
    [void][IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($fixtureScript))
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Clean-NestedDotnetArtifacts.ps1') -Destination $fixtureScript
    $cache = Write-Fixture '.artifacts/llm-wiki/read-only-snapshots/checkout/.artifacts/marker.txt'
    $owned = Write-Fixture '.artifacts/pre-push/owned/output.dll'
    $sibling = Write-Fixture '.artifacts/pre-push/other/output.dll'
    $package = Write-Fixture 'FoodDiary.Web.Client/node_modules/package/.artifacts/marker.txt'
    $result = Invoke-Cleanup @('-RootArtifactPath', '.artifacts/pre-push/owned')
    if ($result.exitCode -ne 0) { throw "Scoped cleanup failed: $($result.output)" }
    if (-not [IO.File]::Exists($cache)) { throw 'Cleanup removed shared root cache artifacts.' }
    if (-not [IO.File]::Exists($sibling) -or -not [IO.File]::Exists($package)) { throw 'Cleanup removed an unrelated scope or dependency artifact.' }
    if ([IO.File]::Exists($owned)) { throw 'Cleanup retained its explicitly owned scope.' }

    $nested = Write-Fixture 'Modules/Example/.artifacts/child/.artifacts/output.dll'
    $result = Invoke-Cleanup @('-RootArtifactPath', '.artifacts/pre-push/already-absent')
    if ($result.exitCode -ne 0 -or [IO.File]::Exists($nested)) { throw "Nested cleanup double-deleted a descendant or retained outputs: $($result.output)" }
    if (-not [IO.File]::Exists($cache)) { throw 'Nested cleanup changed the shared cache.' }

    $nested = Write-Fixture 'Modules/Example/.artifacts/output.dll'
    foreach ($arguments in @(
        @('-RootArtifactPath', '../outside'),
        @('-RootArtifactPath', '.artifacts'),
        @('-RootArtifactPath', '.artifacts/'),
        @('-IncludeRoot', '-RootArtifactPath', '.artifacts/pre-push/owned')
    )) {
        $result = Invoke-Cleanup $arguments
        if ($result.exitCode -eq 0) { throw 'Cleanup accepted an unsafe or conflicting target.' }
        if (-not [IO.File]::Exists($nested) -or -not [IO.File]::Exists($cache)) { throw 'Cleanup mutated outputs before rejecting an invalid target.' }
    }

    $external = Join-Path $fixtureRoot 'external'
    [void][IO.Directory]::CreateDirectory((Join-Path $external '.artifacts'))
    $externalMarker = Join-Path $external '.artifacts/marker.txt'
    [IO.File]::WriteAllText($externalMarker, 'outside the fixture repository')
    foreach ($relativeLink in @('linked-project', '.artifacts/linked-scope')) {
        $link = Join-Path $fixtureRepository $relativeLink
        $fixtureLinks.Add($link)
        $linkType = if ([OperatingSystem]::IsWindows()) { 'Junction' } else { 'SymbolicLink' }
        New-Item -ItemType $linkType -Path $link -Target $external | Out-Null
    }
    $result = Invoke-Cleanup @('-RootArtifactPath', '.artifacts/linked-scope/.artifacts')
    if ($result.exitCode -eq 0 -or -not [IO.File]::Exists($externalMarker)) { throw 'Cleanup followed a linked scope outside its repository.' }
    $result = Invoke-Cleanup @()
    if ($result.exitCode -ne 0 -or -not [IO.File]::Exists($externalMarker)) { throw 'Nested discovery followed a directory link.' }
    foreach ($link in $fixtureLinks) { [IO.Directory]::Delete($link) }

    $result = Invoke-Cleanup @('-IncludeRoot')
    if ($result.exitCode -ne 0 -or [IO.Directory]::Exists((Join-Path $fixtureRepository '.artifacts'))) { throw "Explicit root cleanup failed: $($result.output)" }
    if ([IO.File]::Exists($nested) -or -not [IO.File]::Exists($package)) { throw 'Explicit root cleanup changed the nested/dependency contract.' }
    Write-Output 'Nested .NET artifact cleanup regressions passed: shared caches, owned scopes, descendants, dependencies, directory links and pre-mutation target validation.'
} finally {
    $resolvedFixture = [IO.Path]::GetFullPath($fixtureRoot)
    if (-not $resolvedFixture.StartsWith([IO.Path]::GetFullPath($fixtureParent) + [IO.Path]::DirectorySeparatorChar, [StringComparison]::Ordinal)) {
        throw "Refusing unsafe fixture cleanup: $resolvedFixture"
    }
    foreach ($link in $fixtureLinks) {
        if ([IO.Directory]::Exists($link)) { [IO.Directory]::Delete($link) }
    }
    if ([IO.Directory]::Exists($resolvedFixture)) { [IO.Directory]::Delete($resolvedFixture, $true) }
}
