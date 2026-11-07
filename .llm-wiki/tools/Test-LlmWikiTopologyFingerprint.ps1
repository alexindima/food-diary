[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiRuntimeTopologyFingerprint.ps1')
$fixture = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'topology-fingerprint' -IndependentRepository
function Assert-Fingerprint([bool]$Value, [string]$Message) { if (-not $Value) { throw "Topology fingerprint: $Message" } }
try {
    foreach ($relative in @('Modules/One/Domain/Ёж.cs','Modules/One/Domain/ÅValue.cs','Modules/One/tests/Test.cs','Modules/One/Domain/Value.Designer.cs','obj/Generated.cs')) {
        $path=Join-Path $fixture $relative; $null=[IO.Directory]::CreateDirectory((Split-Path $path)); [IO.File]::WriteAllText($path,"class Value { }`r`n")
    }
    [IO.File]::WriteAllText((Join-Path $fixture 'docker-compose.yml'),"services: {}`r`n")
    $hashes=[Collections.Generic.Dictionary[string,string]]::new([StringComparer]::Ordinal)
    foreach ($file in @(Get-LlmWikiRuntimeTopologySourceFiles $fixture)) { $hashes[$file.FullName]=Get-LlmWikiNormalizedContentHash ([IO.File]::ReadAllText($file.FullName)) }
    $compose=Join-Path $fixture 'docker-compose.yml'; $hashes[$compose]=Get-LlmWikiNormalizedContentHash ([IO.File]::ReadAllText($compose))
    $expected=Get-LlmWikiRuntimeTopologyFingerprint -RepositoryRoot $fixture -SourceHashes $hashes
    $actual=Get-LlmWikiRuntimeTopologyFingerprint -RepositoryRoot $fixture
    Assert-Fingerprint ($actual.sourceFingerprint -ceq $expected.sourceFingerprint -and $actual.sourceFileCount -eq 3) 'native and supplied-source fingerprints must retain exact legacy bytes, Unicode ordering and exclusions.'
    $changed=Join-Path $fixture 'Modules/One/Domain/Ёж.cs'; $time=[IO.File]::GetLastWriteTimeUtc($changed)
    [IO.File]::WriteAllText($changed,"class Other { }`r`n"); [IO.File]::SetLastWriteTimeUtc($changed,$time)
    $edited=Get-LlmWikiRuntimeTopologyFingerprint -RepositoryRoot $fixture
    Assert-Fingerprint ($edited.sourceFingerprint -cne $actual.sourceFingerprint) 'same-size same-time edits must invalidate freshness.'
    [IO.File]::WriteAllText($changed,"class Other { }`n")
    Assert-Fingerprint ((Get-LlmWikiRuntimeTopologyFingerprint -RepositoryRoot $fixture).sourceFingerprint -ceq $edited.sourceFingerprint) 'line endings must remain normalized.'
    [IO.File]::WriteAllText((Join-Path $fixture 'Added.cs'),'class Added {}')
    Assert-Fingerprint ((Get-LlmWikiRuntimeTopologyFingerprint -RepositoryRoot $fixture).sourceFileCount -eq 4) 'new files must be included.'
    [IO.File]::Delete($changed)
    $deleted=Get-LlmWikiRuntimeTopologyFingerprint -RepositoryRoot $fixture
    Assert-Fingerprint ($deleted.sourceFileCount -eq 3 -and $deleted.sourceFingerprint -cne $edited.sourceFingerprint) 'deleted files must invalidate freshness.'
    Write-Host 'Topology fingerprint regression passed: independent legacy parity, Unicode, exclusions, normalized newlines, same-time content, additions and deletions.'
} finally {
    if (-not [IO.Path]::GetFileName($fixture).StartsWith('topology-fingerprint-', [StringComparison]::Ordinal)) { throw 'Unowned fingerprint fixture.' }
    Remove-Item -LiteralPath $fixture -Recurse -Force
}
