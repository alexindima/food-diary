[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$workspacePath = ".artifacts/llm-wiki/tasks/task-scope-$([Guid]::NewGuid().ToString('N'))"
$absoluteWorkspacePath = Join-Path $repositoryRoot $workspacePath
$plannedPaths = @('FoodDiary.Application/Users', 'tests/FoodDiary.Application.Tests/Users')
$changedPaths = @(
    'FoodDiary.Application\Users\Common\UserContextService.cs',
    'tests\FoodDiary.Application.Tests\Users\UserContextServiceTests.cs'
)

try {
    $null = New-Item -ItemType Directory -Path $absoluteWorkspacePath -Force
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiChangeManifest.ps1') init `
        -Path "$workspacePath/change-manifest.json" `
        -Objective 'Verify task-local scope and evidence references.' `
        -ChangedPath $changedPaths `
        -PlannedPath $plannedPaths `
        -AllowedPath @('^FoodDiary\.Application/Users(?:/.*)?$', '^tests/FoodDiary\.Application\.Tests/Users(?:/.*)?$') `
        -EvidencePath "$workspacePath/evidence.json" | Out-Null
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiAcceptanceMatrix.ps1') init `
        -Path "$workspacePath/acceptance-matrix.json" `
        -Objective 'Verify task-local scope and evidence references.' `
        -ChangedPath $changedPaths `
        -Criterion 'Task paths remain normalized and workspace-local.' `
        -EvidencePath "$workspacePath/evidence.json" | Out-Null

    $manifest = Get-Content -LiteralPath (Join-Path $absoluteWorkspacePath 'change-manifest.json') -Raw | ConvertFrom-Json
    $acceptance = Get-Content -LiteralPath (Join-Path $absoluteWorkspacePath 'acceptance-matrix.json') -Raw | ConvertFrom-Json
    $expectedEvidencePath = "$workspacePath/evidence.json"
    $actualPlannedPaths = @($manifest.scope.plannedPaths)
    if (@($actualPlannedPaths).Count -ne 2 -or @($plannedPaths | Where-Object { $_ -notin $actualPlannedPaths }).Count -gt 0) {
        throw "Task manifest did not preserve planned paths: $($actualPlannedPaths -join ', ')."
    }
    if ([string]$manifest.evidencePath -cne $expectedEvidencePath -or [string]$acceptance.evidencePath -cne $expectedEvidencePath) {
        throw 'Task artifacts do not reference the workspace-local evidence bundle.'
    }

    foreach ($changedPath in $changedPaths) {
        $normalizedPath = $changedPath.Replace('\', '/')
        if (-not (@($manifest.scope.allowedPathPatterns | Where-Object { $normalizedPath -match [string]$_ }).Count)) {
            throw "Normalized Windows path was not recognized as in scope: $changedPath."
        }
    }

    $initializerSource = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Initialize-LlmWikiTaskWorkspace.ps1') -Raw
    foreach ($requiredWiring in @('-PlannedPath $scopeRoots', '-EvidencePath "$normalizedWorkspacePath/evidence.json"')) {
        if (-not $initializerSource.Contains($requiredWiring)) { throw "Task initializer wiring is absent: $requiredWiring" }
    }
} finally {
    if (Test-Path -LiteralPath $absoluteWorkspacePath) {
        Remove-Item -LiteralPath $absoluteWorkspacePath -Recurse -Force -ErrorAction SilentlyContinue
    }
}

Write-Host 'LLM Wiki task scope regression passed.'
