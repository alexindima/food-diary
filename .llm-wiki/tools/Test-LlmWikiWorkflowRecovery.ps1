[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$workspaceName = "workflow-recovery-$([guid]::NewGuid().ToString('N'))"
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
$workspacePath = New-LlmWikiSmokeFixtureRepositoryPath -RepositoryRoot $repositoryRoot -Name $workspaceName
$absoluteWorkspace = Join-Path $repositoryRoot $workspacePath
$receiptPath = Join-Path $absoluteWorkspace 'context-security.json'

try {
    $null = New-Item -ItemType Directory -Path $absoluteWorkspace -Force
    $packet = [pscustomobject][ordered]@{
        fingerprint = 'b' * 64
        brief = [pscustomobject][ordered]@{ instructions = $null; contextPages = $null }
        diff = [pscustomobject][ordered]@{ changedPaths = $null }
    }
    [IO.File]::WriteAllText(
        (Join-Path $absoluteWorkspace 'change-packet.json'),
        (($packet | ConvertTo-Json -Depth 5) + [Environment]::NewLine),
        [Text.UTF8Encoding]::new($false))

    $missingPathError = ''
    try {
        & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextSecurity.ps1') create `
            -WorkspacePath $workspacePath `
            -Format Json | Out-Null
    } catch { $missingPathError = $_.Exception.Message }
    if ($missingPathError -notmatch 'No context-security paths were supplied or discovered' -or $missingPathError -notmatch 'ChangedPath') {
        throw "Context security did not return an actionable missing-path diagnostic: $missingPathError"
    }
    $assessment = & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextSecurity.ps1') create `
        -WorkspacePath $workspacePath `
        -Path '.llm-wiki/index.md' `
        -Format Json | ConvertFrom-Json
    if (-not $assessment.valid -or @($assessment.assessment.sources).Count -ne 1) {
        throw 'Context security did not recover after the actionable missing-path diagnostic.'
    }
    $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
    $receipt.sources = $null
    [IO.File]::WriteAllText(
        $receiptPath,
        (($receipt | ConvertTo-Json -Depth 30) + [Environment]::NewLine),
        [Text.UTF8Encoding]::new($false))
    $validation = & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextSecurity.ps1') verify `
        -WorkspacePath $workspacePath `
        -Format Json | ConvertFrom-Json
    if ($null -eq $validation -or -not $validation.PSObject.Properties['valid']) {
        throw 'Context security crashed instead of normalizing a null stored source collection.'
    }

    $plannerText = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Manage-LlmWikiVerificationPlan.ps1') -Raw
    foreach ($contract in @('plan = $plan; issues = @()', "PSObject.Properties['plan']", 'BLOCKED:', 'Repair:')) {
        if (-not $plannerText.Contains($contract)) { throw "Verification runner recovery contract is missing '$contract'." }
    }
    Write-Host 'LLM Wiki workflow recovery regression passed: null context diagnostics and resumable plan output are safe.'
} finally {
    if (Test-Path -LiteralPath $absoluteWorkspace) { Remove-Item -LiteralPath $absoluteWorkspace -Recurse -Force }
}
