[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$Url,
    [Parameter(Mandatory)] [string]$FixturePath,
    [Parameter(Mandatory)] [string]$ResultSelector,
    [string]$TriggerSelector,
    [string]$FileSelector = 'input[type=file]',
    [string]$ScreenshotPath = '.artifacts/browser/visual-qa.png',
    [string]$StorageStatePath,
    [ValidateRange(320, 3840)] [int]$ViewportWidth = 1440,
    [ValidateRange(320, 2160)] [int]$ViewportHeight = 1000,
    [ValidateRange(1000, 120000)] [int]$TimeoutMs = 30000,
    [switch]$Run,
    [ValidateSet('Text', 'Json')] [string]$Format = 'Text'
)
$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
function Resolve-RepoFile([string]$Value, [string]$Label) {
    $candidate = if ([System.IO.Path]::IsPathRooted($Value)) { $Value } else { Join-Path $repositoryRoot $Value }
    if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) { throw "$Label does not exist: $Value" }
    return (Resolve-Path -LiteralPath $candidate).Path
}
$fixture = Resolve-RepoFile $FixturePath 'Fixture'
$storage = if ($StorageStatePath) { Resolve-RepoFile $StorageStatePath 'Storage state' } else { $null }
$screenshot = if ([System.IO.Path]::IsPathRooted($ScreenshotPath)) { $ScreenshotPath } else { Join-Path $repositoryRoot $ScreenshotPath }
$result = [ordered]@{
    mode = $(if ($Run) { 'run' } else { 'plan' }); url = $Url; fixturePath = $fixture
    triggerSelector = $TriggerSelector; fileSelector = $FileSelector; resultSelector = $ResultSelector
    viewport = [ordered]@{ width = $ViewportWidth; height = $ViewportHeight }
    screenshotPath = [System.IO.Path]::GetFullPath($screenshot)
    checks = @('target result becomes visible', 'no console errors', 'no uncaught page errors', 'screenshot captured')
}
if ($Run) {
    $playwrightPackage = Join-Path $repositoryRoot 'FoodDiary.Web.Client/node_modules/@playwright/test/package.json'
    if (-not (Test-Path -LiteralPath $playwrightPackage -PathType Leaf)) {
        throw 'Visual QA run requires frontend dependencies. Run npm ci in FoodDiary.Web.Client first.'
    }
    $null = New-Item -ItemType Directory -Path (Split-Path -Parent $screenshot) -Force
    $optionsPath = Join-Path ([System.IO.Path]::GetTempPath()) "llm-wiki-visual-qa-$([guid]::NewGuid().ToString('N')).json"
    try {
        $options = [ordered]@{}
        foreach ($entry in $result.GetEnumerator()) { $options[$entry.Key] = $entry.Value }
        $options['storageStatePath'] = $storage; $options['timeoutMs'] = $TimeoutMs
        [System.IO.File]::WriteAllText($optionsPath, ($options | ConvertTo-Json -Depth 5), [System.Text.UTF8Encoding]::new($false))
        $execution = & node (Join-Path $PSScriptRoot 'Invoke-LlmWikiVisualQa.mjs') $optionsPath
        if ($LASTEXITCODE -ne 0) { throw 'Visual QA browser execution failed.' }
        $result['execution'] = $execution | ConvertFrom-Json
    } finally { if (Test-Path -LiteralPath $optionsPath) { Remove-Item -LiteralPath $optionsPath -Force } }
}
if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 6 } else {
    Write-Host "Visual QA $($result.mode): $Url"
    Write-Host "Upload: $fixture -> $FileSelector"
    if ($TriggerSelector) { Write-Host "Trigger: $TriggerSelector" }
    Write-Host "Expect: $ResultSelector; viewport: ${ViewportWidth}x${ViewportHeight}"
    Write-Host "Screenshot: $($result.screenshotPath)"
    if (-not $Run) { Write-Host 'Plan only. Repeat with -Run when the target app and authentication state are ready.' }
}
