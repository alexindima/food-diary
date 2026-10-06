[CmdletBinding()]
param([string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path)

$ErrorActionPreference = 'Stop'
$sourceRoots = @(
    '.llm-wiki/tools', '.llm-wiki/wiki.ps1', 'FoodDiary.Development.Mcp',
    'scripts/Start-FoodDiaryDevelopmentMcp.ps1', 'scripts/Start-FoodDiaryDevelopmentMcp.cmd',
    'scripts/ci/Assert-WikiCiResult.ps1', 'scripts/ci/Test-WikiCiResult.ps1',
    '.github/workflows/ci-tests.yml'
)
$extensions = @('.ps1', '.cs', '.csproj', '.mjs', '.cmd', '.yml')
$paths = @(& git -C $RepositoryRoot ls-files --cached --others --exclude-standard -- $sourceRoots | Where-Object {
    [IO.Path]::GetExtension($_) -in $extensions
} | Sort-Object -Unique)
if ($LASTEXITCODE -ne 0) { throw 'Unable to enumerate tracked Wiki audit sources.' }
$baseline = (& git -C $RepositoryRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Unable to resolve the audit baseline.' }
$worktreeStatus = @(& git -C $RepositoryRoot status --porcelain=v1 --untracked-files=all)
if ($LASTEXITCODE -ne 0) { throw 'Unable to capture the audit worktree state.' }
$entries = foreach ($path in $paths) {
    $content = [IO.File]::ReadAllText((Join-Path $RepositoryRoot $path)).Replace("`r`n", "`n").Replace("`r", "`n")
    $sha = [Security.Cryptography.SHA256]::Create()
    try { $hash = ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($content)))).Replace('-', '').ToLowerInvariant() }
    finally { $sha.Dispose() }
    $lines = $content.Split([char]10)
    $lineCount = $lines.Count
    if ($content.EndsWith("`n")) { $lineCount-- }
    $extension = [IO.Path]::GetExtension($path)
    $leaf = [IO.Path]::GetFileName($path)
    $role = if ($path -eq '.llm-wiki/wiki.ps1') { 'facade' }
        elseif ($path.StartsWith('FoodDiary.Development.Mcp/')) { 'mcp-runtime' }
        elseif ($path.StartsWith('scripts/') -or $path.StartsWith('.github/')) { 'launcher-ci' }
        elseif ($leaf.StartsWith('Test-') -or $leaf.EndsWith('.test.mjs')) { 'regression-tool' }
        elseif ($leaf.StartsWith('Build-')) { 'index-generator' }
        elseif ($leaf.StartsWith('Manage-') -or $leaf.StartsWith('Initialize-') -or $leaf.StartsWith('Update-') -or $leaf.StartsWith('Complete-') -or $leaf.StartsWith('Start-')) { 'state-workflow' }
        elseif ($leaf.StartsWith('LlmWiki') -or $extension -in @('.cs', '.csproj')) { 'shared-library' }
        elseif ($leaf.StartsWith('code-graph')) { 'graph-runtime' }
        else { 'query-or-orchestration' }
    $functions = @()
    $parameters = @()
    $parseErrors = @()
    if ($extension -eq '.ps1') {
        $tokens = $null; $errors = $null
        $ast = [Management.Automation.Language.Parser]::ParseInput($content, [ref]$tokens, [ref]$errors)
        $parseErrors = @($errors | ForEach-Object { [pscustomobject]@{line=$_.Extent.StartLineNumber; message=$_.Message} })
        $functions = @($ast.FindAll({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] }, $true) | ForEach-Object {
            [pscustomobject]@{name=$_.Name; startLine=$_.Extent.StartLineNumber; endLine=$_.Extent.EndLineNumber}
        })
        if ($ast.ParamBlock) { $parameters = @($ast.ParamBlock.Parameters | ForEach-Object { $_.Name.VariablePath.UserPath }) }
    }
    $operations = @()
    for ($index = 0; $index -lt $lineCount; $index++) {
        $line = $lines[$index]
        if ($line -match 'Get-ChildItem|EnumerateFiles|EnumerateDirectories|ReadAllText|ReadToEnd|Get-Content|Get-FileHash|Start-Process|WaitForExit|ProcessStartInfo|DatabaseSync|SqliteConnection|\.Kill\(|taskkill|ConvertFrom-Json|ConvertTo-Json') {
            $operations += [pscustomobject]@{line=$index + 1; text=$line.Trim()}
        }
    }
    [pscustomobject][ordered]@{
        path=$path; role=$role; extension=$extension; lines=$lineCount
        sourceSha256=$hash; parameters=$parameters; functions=$functions
        parseErrors=$parseErrors; operationAnchors=$operations
    }
}
[pscustomobject][ordered]@{
    schemaVersion=1; auditDate='2026-10-06'; baseline=$baseline
    worktreeStatus=$worktreeStatus
    snapshotContract='Hashes identify current worktree content; baseline identifies HEAD and is not a claim that the worktree is clean.'
    hashContract='sha256 UTF-8 source text with LF line endings; a hash proves source identity, not review'
    scopeRoots=$sourceRoots
    classificationContract='Automated navigation only. All source review and performance/reliability conclusions are recorded separately.'
    sourceFileCount=@($entries).Count; sourceLineCount=($entries.lines | Measure-Object -Sum).Sum
    files=@($entries)
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'inventory.json') -Encoding utf8
Write-Host "Inventoried $(@($entries).Count) files; no file is marked reviewed by this script."
