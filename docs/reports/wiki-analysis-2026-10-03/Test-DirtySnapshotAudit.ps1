$ErrorActionPreference = 'Stop'
$root = (& git -C $PSScriptRoot rev-parse --show-toplevel).Trim()
$auditOutput = Join-Path $root '.artifacts/wiki-analysis-replay'
New-Item -ItemType Directory -Force $auditOutput | Out-Null
$fixture = Join-Path $auditOutput ('guard-fixture-' + [guid]::NewGuid().ToString('N'))
$tools = Join-Path $fixture '.llm-wiki/tools'
New-Item -ItemType Directory -Path $tools -Force | Out-Null
foreach ($name in @('Invoke-LlmWikiReadOnlyTool.ps1','LlmWikiGitPaths.ps1','Manage-LlmWikiCodeGraph.ps1')) {
    Copy-Item -LiteralPath (Join-Path $root ".llm-wiki/tools/$name") -Destination (Join-Path $tools $name)
}
@'
$path = Join-Path (Get-Location) 'source.txt'
[IO.File]::WriteAllText($path, 'overlay-mutated')
Write-Output 'mutation-tool-returned-success'
'@ | Set-Content (Join-Path $tools 'Mutate-Dirty.ps1') -Encoding utf8
@'
Get-Content (Join-Path (Get-Location) 'source.txt') -Raw
'@ | Set-Content (Join-Path $tools 'Read-Dirty.ps1') -Encoding utf8
[IO.File]::WriteAllText((Join-Path $fixture 'source.txt'), 'committed-content')
& git -C $fixture init --quiet
& git -C $fixture add .
& git -C $fixture -c user.name='Wiki audit fixture' -c user.email='wiki-audit@example.invalid' commit --quiet -m 'Initialize isolated audit fixture'
if ($LASTEXITCODE -ne 0) { throw 'Unable to initialize fixture' }
[IO.File]::WriteAllText((Join-Path $fixture 'source.txt'), 'overlay-original')
$rejected = $false
$failure = $null
try {
    $mutationOutput = & (Join-Path $tools 'Invoke-LlmWikiReadOnlyTool.ps1') -ToolPath (Join-Path $tools 'Mutate-Dirty.ps1')
} catch { $rejected=$true; $failure=$_.Exception.Message }
$readerOutput = & (Join-Path $tools 'Invoke-LlmWikiReadOnlyTool.ps1') -ToolPath (Join-Path $tools 'Read-Dirty.ps1')
$result = [pscustomobject]@{
    fixture=$fixture
    guardSha256=(Get-FileHash (Join-Path $tools 'Invoke-LlmWikiReadOnlyTool.ps1')).Hash
    rejected=$rejected
    failure=$failure
    mutationOutput=$mutationOutput
    subsequentSnapshotRead=$readerOutput
    originalSource=[IO.File]::ReadAllText((Join-Path $fixture 'source.txt'))
}
$result | ConvertTo-Json | Set-Content (Join-Path $auditOutput 'dirty-snapshot-proof.json') -Encoding utf8
$result | ConvertTo-Json
