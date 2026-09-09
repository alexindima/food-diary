[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
$fixture = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'acceptance-packet-reuse'
try {
    $arguments = @{
        BaseRef = [string](git -C $repositoryRoot rev-parse HEAD)
        Objective = 'Preserve acceptance requirements when composing a task workspace.'
        ChangedPath = @('Modules/Users/Application/Commands/UpdateUser/UpdateUserCommandHandler.cs')
        Format = 'Json'
    }
    $packet = & (Join-Path $PSScriptRoot 'Get-LlmWikiChangePacket.ps1') @arguments | ConvertFrom-Json
    $matrixTool = Join-Path $PSScriptRoot 'Manage-LlmWikiAcceptanceMatrix.ps1'
    $arguments.Criterion = @('Acceptance requirements remain identical.')
    $arguments.Path = Join-Path $fixture 'acceptance.json'
    & $matrixTool init @arguments | Out-Null
    $baseline = Get-Content -LiteralPath $arguments.Path -Raw | ConvertFrom-Json
    & $matrixTool init @arguments -PacketInput $packet | Out-Null
    $reused = Get-Content -LiteralPath $arguments.Path -Raw | ConvertFrom-Json
    $baseline.createdAtUtc = $reused.createdAtUtc
    if (($baseline | ConvertTo-Json -Depth 30 -Compress) -cne ($reused | ConvertTo-Json -Depth 30 -Compress)) {
        throw 'Reused packet changed the acceptance matrix.'
    }
    $original = [IO.File]::ReadAllText($arguments.Path)
    foreach ($mutation in @('objective', 'baseRef', 'changedPaths', 'diffPaths', 'headRef', 'schemaVersion')) {
        $invalid = $packet | ConvertTo-Json -Depth 50 | ConvertFrom-Json
        switch ($mutation) {
            objective { $invalid.inputs.objective += ' other' }
            baseRef { $invalid.inputs.baseRef = 'HEAD' }
            changedPaths { $invalid.inputs.changedPaths = @('other.cs') }
            diffPaths { $invalid.diff.changedPaths = @('other.cs') }
            headRef { $invalid.inputs.headRef = $arguments.BaseRef }
            schemaVersion { $invalid.schemaVersion = 0 }
        }
        $rejected = $false
        try { & $matrixTool init @arguments -PacketInput $invalid | Out-Null }
        catch { $rejected = $_.Exception.Message -like 'PacketInput does not match*' }
        if (-not $rejected) { throw "Packet reuse accepted mismatched $mutation." }
        if ([IO.File]::ReadAllText($arguments.Path) -cne $original) { throw 'Rejected packet modified the matrix.' }
    }
    foreach ($action in @('show', 'validate', 'map', 'resolve')) {
        $rejected = $false
        try { & $matrixTool $action -Path $arguments.Path -PacketInput $packet | Out-Null }
        catch { $rejected = $_.Exception.Message -eq 'PacketInput is supported only for acceptance init.' }
        if (-not $rejected) { throw "Packet reuse accepted action $action." }
    }
    foreach ($mode in @('historical-head', 'implicit-paths', 'empty-paths')) {
        $invalidArguments = $arguments.Clone()
        if ($mode -eq 'historical-head') { $invalidArguments.HeadRef = $arguments.BaseRef }
        elseif ($mode -eq 'implicit-paths') { $invalidArguments.Remove('ChangedPath') }
        else { $invalidArguments.ChangedPath = @() }
        $rejected = $false
        try { & $matrixTool init @invalidArguments -PacketInput $packet | Out-Null }
        catch { $rejected = $_.Exception.Message -like 'PacketInput does not match*' }
        if (-not $rejected) { throw "Packet reuse accepted unsupported mode $mode." }
    }
    Write-Host 'Acceptance packet reuse regression passed: complete matrix parity and invalid-input rejection.'
} finally {
    $sandbox = [IO.Path]::GetFullPath((Get-LlmWikiSmokeSandboxRoot -RepositoryRoot $repositoryRoot)).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    if (-not [IO.Path]::GetFullPath($fixture).StartsWith($sandbox, [StringComparison]::OrdinalIgnoreCase)) { throw 'Fixture cleanup escaped sandbox.' }
    Remove-Item -LiteralPath $fixture -Recurse -Force
}
