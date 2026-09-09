[CmdletBinding()]
param([string]$ToolsRoot = $PSScriptRoot)
$ErrorActionPreference = 'Stop'
$root = Join-Path ([IO.Path]::GetTempPath()) "wiki-full-resume-$([guid]::NewGuid().ToString('N'))"
$fixtureTools = Join-Path $root '.llm-wiki/tools'
$null = New-Item -ItemType Directory -Path $fixtureTools -Force
$null = New-Item -ItemType Directory -Path (Join-Path $root '.artifacts') -Force
try {
    foreach ($name in @('Invoke-LlmWikiFullVerification.ps1', 'Get-LlmWikiVerificationStageFingerprint.ps1', 'LlmWikiGitPaths.ps1', 'LlmWikiProcess.ps1', 'Write-LlmWikiWorkflowMetric.ps1')) {
        Copy-Item -LiteralPath (Join-Path $ToolsRoot $name) -Destination $fixtureTools
    }
    $stub = 'param([switch]$Check, [int]$MaxConcurrency, [switch]$AllGroups); Add-Content -LiteralPath (Join-Path $PSScriptRoot "../../.artifacts/count.txt") -Value run'
    foreach ($name in @('Test-LlmWikiSessionResolution.ps1', 'Test-LlmWikiProcess.ps1', 'Invoke-LlmWikiIndexPipeline.ps1', 'Invoke-LlmWikiParallelSmoke.ps1')) {
        [IO.File]::WriteAllText((Join-Path $fixtureTools $name), $stub)
    }
    [IO.File]::WriteAllText((Join-Path $root '.gitignore'), '.artifacts/')
    & git -C $root init --quiet
    & git -C $root config core.autocrlf false
    & git -C $root add .
    & git -C $root -c user.name=Wiki -c user.email=wiki@example.invalid commit --quiet -m baseline
    if ($LASTEXITCODE -ne 0) { throw 'Unable to initialize full resume fixture.' }
    $unicodePath = Join-Path $root 'исходник.cs'
    [IO.File]::WriteAllText($unicodePath, 'first')
    $runner = Join-Path $fixtureTools 'Invoke-LlmWikiFullVerification.ps1'
    $shellPath = (Get-Process -Id $PID).Path
    $log = Join-Path $root '.artifacts/full.log'
    $counts = @()
    for ($round = 0; $round -lt 4; $round++) {
        if ($round -eq 2) { [IO.File]::WriteAllText($unicodePath, 'other') }
        if ($round -eq 3) {
            $null = New-Item -ItemType Directory -Path (Join-Path $root '.llm-wiki/reviews') -Force
            [IO.File]::WriteAllText((Join-Path $root '.llm-wiki/reviews/receipt.json'), '{}')
        }
        & $shellPath -NoLogo -NoProfile -File $runner -ResumePassedStages *> $log
        if ($LASTEXITCODE -ne 0) { throw "Full resume fixture failed: $([IO.File]::ReadAllText($log))" }
        $counts += [IO.File]::ReadAllLines((Join-Path $root '.artifacts/count.txt')).Length
    }
    if (($counts -join ',') -ne '4,4,8,8') { throw "Full resume expected 4,4,8,8 runs; got $($counts -join ',')." }
    Write-Host 'Full resume passed: four stages run once, unchanged input resumes, changed Unicode content reruns all four.'
} finally {
    $resolved = [IO.Path]::GetFullPath($root)
    if (-not $resolved.StartsWith([IO.Path]::GetFullPath([IO.Path]::GetTempPath()), [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe fixture cleanup.' }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
