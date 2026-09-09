[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
$fixture = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'workspace-policy-cache'
try {
    $fixtureTools = Join-Path $fixture '.llm-wiki/tools'
    $fixturePolicies = Join-Path $fixture '.llm-wiki/policies'
    $null = New-Item -ItemType Directory -Path $fixtureTools, $fixturePolicies -Force
    $tool = Join-Path $fixtureTools 'Get-LlmWikiWorkspacePolicy.ps1'
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Get-LlmWikiWorkspacePolicy.ps1') -Destination $tool
    $policyPath = Join-Path $fixturePolicies 'workspace-policies.json'
    $original = [IO.File]::ReadAllText((Join-Path $PSScriptRoot '../policies/workspace-policies.json'))
    [IO.File]::WriteAllText($policyPath, $original)

    function Assert-Rejected([scriptblock]$Action, [string]$Name) {
        $rejected = $false
        try { & $Action | Out-Null } catch { $rejected = $true }
        if (-not $rejected) { throw "Policy cache accepted $Name." }
    }

    $baseline = & $tool get -Format Json
    $snapshot = & $tool get -WithFingerprint -Format Json
    $validation = & $tool validate -Format Json | ConvertFrom-Json
    if (-not $validation.valid -or ($snapshot | ConvertFrom-Json).fingerprint -cne $validation.fingerprint) {
        throw 'Cached policy fingerprint differs from full validation.'
    }
    if ((& $tool get -Format Json) -cne $baseline -or (& $tool get -WithFingerprint -Format Json) -cne $snapshot) {
        throw 'Policy cache changed serialized output across modes.'
    }
    $mutable = & $tool get
    $mutable.workspace.latestSchemaVersion = 0
    if ((& $tool get -Format Json) -cne $baseline) { throw 'Caller mutation contaminated cached policy.' }

    # Culture-sensitive PowerShell equality ignores NUL, but JSON keys must not.
    $nulEdit = $original.Replace('"workspace"', ('"work' + [char]0 + 'space"'))
    if ($nulEdit.Length -ne $original.Length + 1) { throw 'NUL policy fixture was not constructed.' }
    [IO.File]::WriteAllText($policyPath, $nulEdit)
    Assert-Rejected { & $tool get -Format Json } 'culture-equivalent text with an inserted NUL'
    [IO.File]::WriteAllText($policyPath, $original)
    if ((& $tool get -Format Json) -cne $baseline) { throw 'Policy did not recover after NUL edit.' }

    $timestamp = [IO.File]::GetLastWriteTimeUtc($policyPath)
    $invalid = $original -replace '("latestSchemaVersion"\s*:\s*)4', '${1}0'
    if ($invalid -ceq $original -or $invalid.Length -ne $original.Length) { throw 'Same-size policy fixture was not constructed.' }
    [IO.File]::WriteAllText($policyPath, $invalid)
    [IO.File]::SetLastWriteTimeUtc($policyPath, $timestamp)
    Assert-Rejected { & $tool get -Format Json } 'same-size invalid edit with restored timestamp'
    if ((& $tool validate -Format Json | ConvertFrom-Json).valid) { throw 'Full validation accepted invalid policy.' }
    [IO.File]::WriteAllText($policyPath, '{broken')
    Assert-Rejected { & $tool get -Format Json } 'malformed JSON'
    Remove-Item -LiteralPath $policyPath
    Assert-Rejected { & $tool get -Format Json } 'deleted policy'
    [IO.File]::WriteAllText($policyPath, $original)
    if ((& $tool get -Format Json) -cne $baseline) { throw 'Restored policy did not recover.' }

    $alternatePath = Join-Path $fixturePolicies 'alternate.json'
    [IO.File]::WriteAllText($alternatePath, $invalid)
    Assert-Rejected { & $tool get -Path '.llm-wiki/policies/alternate.json' -Format Json } 'alternate policy path'

    $changedPolicy = $original | ConvertFrom-Json
    $changedPolicy.workspace.format += '-fixture'
    [IO.File]::WriteAllText($policyPath, ($changedPolicy | ConvertTo-Json -Depth 30))
    $changedSnapshot = & $tool get -WithFingerprint -Format Json | ConvertFrom-Json
    $changedValidation = & $tool validate -Format Json | ConvertFrom-Json
    if (-not $changedValidation.valid -or $changedSnapshot.fingerprint -ceq $validation.fingerprint -or
        $changedSnapshot.fingerprint -cne $changedValidation.fingerprint -or
        $changedSnapshot.policy.workspace.format -cne $changedPolicy.workspace.format) {
        throw 'Valid policy edit did not publish a fresh policy and fingerprint.'
    }
    [IO.File]::WriteAllText($policyPath, $original)
    if ((& $tool get -Format Json) -cne $baseline) { throw 'Policy restoration did not replace changed cached content.' }

    # Alter the executing validator while keeping the policy and its path intact.
    $source = [IO.File]::ReadAllText($tool)
    $marker = '$result = [pscustomobject][ordered]@{'
    if (-not $source.Contains($marker)) { throw 'Validator fixture marker is missing.' }
    $source = $source.Replace($marker, '$issues.Add(''Changed validator rejects this policy.'')' + [Environment]::NewLine + $marker)
    [IO.File]::WriteAllText($tool, $source)
    Assert-Rejected { & $tool get -Format Json } 'changed validator implementation'
    Write-Host 'Workspace policy cache passed: output parity, fingerprint, caller isolation, content freshness, missing/malformed files, path isolation and validator invalidation.'
} finally {
    $resolvedFixture = [IO.Path]::GetFullPath($fixture)
    $sandbox = [IO.Path]::GetFullPath((Get-LlmWikiSmokeSandboxRoot -RepositoryRoot $repositoryRoot)).TrimEnd('\', '/')
    if (-not $resolvedFixture.StartsWith($sandbox + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe policy fixture cleanup path.' }
    Remove-Item -LiteralPath $resolvedFixture -Recurse -Force
}
