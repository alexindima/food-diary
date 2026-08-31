[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiApplicationModulePaths.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$fixture = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'application-module-paths'
try {
    foreach ($path in @('Modules/Example/Application/Abstractions', 'FoodDiary.Application.Legacy', 'FoodDiary.Application/Folder', 'FoodDiary.Application.Empty/bin', 'Custom/Owned', 'docs/architecture')) {
        $null = New-Item -ItemType Directory -Path (Join-Path $fixture $path) -Force
    }
    foreach ($path in @('Modules/Example/Application/New.Identity.csproj', 'Modules/Example/Application/Abstractions/Ports.csproj', 'FoodDiary.Application.Legacy/FoodDiary.Application.Legacy.csproj', 'Custom/Owned/Actual.csproj')) {
        [IO.File]::WriteAllText((Join-Path $fixture $path), '<Project Sdk="Microsoft.NET.Sdk" />')
    }
    $logical = Get-LlmWikiApplicationModuleLayout $fixture Example
    if ($logical.projects.Count -ne 1 -or $logical.projects[0] -ne 'Modules/Example/Application/New.Identity.csproj') { throw 'Logical project discovery included nested ports or depended on its legacy identity.' }
    if ($logical.sourceRoots[0] -ne 'Modules/Example/Application') { throw 'Logical source root missing.' }
    if ((Get-LlmWikiApplicationModuleLayout $fixture Legacy).projects.Count -ne 1) { throw 'Legacy project missing.' }
    $folder = Get-LlmWikiApplicationModuleLayout $fixture Folder
    if ($folder.projects.Count -ne 0 -or $folder.sourceRoots[0] -ne 'FoodDiary.Application/Folder') { throw 'Legacy folder mode lost.' }
    foreach ($name in @('Empty', 'ExampleExtra', 'Missing')) {
        if ((Get-LlmWikiApplicationModuleLayout $fixture $name).sourceRoots.Count -ne 0) { throw "False module match: $name" }
    }
    foreach ($mapping in @('Custom/Owned', 'Custom/Owned/Actual.csproj', 'Custom\Owned\Actual.csproj')) {
        $manifest = @{modules=@{Mapped=@{sourceMappings=@{applicationProjects=@($mapping)}}}} | ConvertTo-Json -Depth 6
        [IO.File]::WriteAllText((Join-Path $fixture 'docs/architecture/backend-modules.json'), $manifest)
        if ((Get-LlmWikiApplicationModuleLayout $fixture Mapped).projects[0] -ne 'Custom/Owned/Actual.csproj') { throw "Mapped application project not resolved: $mapping" }
    }
    $rejected = $false
    try { Get-LlmWikiApplicationModuleLayout $fixture '../Escape' | Out-Null } catch { $rejected = $true }
    if (-not $rejected) { throw 'Unsafe module identifier accepted.' }
} finally {
    Remove-Item -LiteralPath $fixture -Recurse -Force
}
Write-Host 'Application module layout regression passed: logical, mapped, legacy, empty and nested boundaries.'
