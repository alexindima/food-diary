[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiRuntimeTopologyFingerprint.ps1')
$fixtureRoot = [IO.Path]::GetFullPath((Join-Path ([IO.Path]::GetTempPath()) "wiki-source-inventory-$([guid]::NewGuid().ToString('N'))"))
$fixtureRepository = Join-Path $fixtureRoot 'repository'
$linkedDirectory = Join-Path $fixtureRepository 'linked'

function Write-InventoryFixture([string]$Path, [string]$Content) {
    $null = New-Item -ItemType Directory -Path (Split-Path -Parent $Path) -Force
    [IO.File]::WriteAllText($Path, $Content, [Text.UTF8Encoding]::new($false))
}

function Get-CanonicalReferenceFiles([string]$Filter) {
    # Legacy Get-ChildItem can follow junctions and hide system entries. The
    # canonical modern inventory follows neither links nor hidden entries,
    # and retains the fixture's non-hidden system file on both runtimes.
    $reference = @(Get-ChildItem -LiteralPath $fixtureRepository -Recurse -File -Filter $Filter |
        Where-Object { -not $_.FullName.StartsWith($linkedDirectory + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) })
    $systemPath = Join-Path $fixtureRepository 'system.cs'
    if ([IO.File]::Exists($systemPath) -and 'system.cs' -like $Filter -and $systemPath -notin $reference.FullName) {
        $reference += Get-Item -LiteralPath $systemPath -Force
    }
    return $reference
}

function Get-ReferenceRuntimeFingerprint {
    $paths = @(
        @(Join-Path $fixtureRepository 'docker-compose.yml') +
        @(Get-CanonicalReferenceFiles '*.cs' |
            Where-Object {
                $_.FullName -notmatch '[\\/](tests|obj|bin|\.artifacts|\.llm-wiki|TestResults|Migrations)[\\/]' -and
                $_.Name -notmatch '\.(Designer|g)\.cs$'
            } | ForEach-Object FullName) |
        Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } |
        Sort-Object { $_.ToLowerInvariant() } -Unique
    )
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        $entries = foreach ($path in $paths) {
            $content = [IO.File]::ReadAllText($path).Replace("`r`n", "`n").Replace("`r", "`n")
            $hash = ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($content))) -replace '-', '').ToLowerInvariant()
            $relative = $path.Substring($fixtureRepository.Length + 1).Replace('\', '/')
            "$relative=$hash`n"
        }
        return ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($entries -join ''))) -replace '-', '').ToLowerInvariant()
    } finally { $sha.Dispose() }
}

try {
    foreach ($path in @('Root.cs', 'Modules/файл с пробелом.cs', 'node_modules/Included.cs', 'Modules/Widget.CS', 'Modules/Widget.Designer.cs', 'Modules/Widget.g.cs', 'tests/IncludedConfiguration.cs', 'Modules/MainConfiguration.cs')) {
        Write-InventoryFixture (Join-Path $fixtureRepository $path) "public class Alpha {}`r`n"
    }
    foreach ($name in @('tests', 'Obj', 'bin', '.artifacts', '.llm-wiki', 'TestResults', 'Migrations')) {
        Write-InventoryFixture (Join-Path $fixtureRepository "$name/nested/ExcludedConfiguration.cs") 'excluded'
    }
    Write-InventoryFixture (Join-Path $fixtureRepository 'node_modules/ExcludedConfiguration.cs') 'runtime includes this tree'
    Write-InventoryFixture (Join-Path $fixtureRepository 'docker-compose.yml') "services:`r`n"
    Write-InventoryFixture (Join-Path $fixtureRepository 'Modules/файл с пробелом.cs') "// Кириллица 😀`rclass Unicode {}`r`n"
    Write-InventoryFixture (Join-Path $fixtureRepository 'Empty.cs') ''
    $hiddenFile = Join-Path $fixtureRepository '.hidden.cs'
    Write-InventoryFixture $hiddenFile 'hidden'
    if ([IO.Path]::DirectorySeparatorChar -eq '\') {
        [IO.File]::SetAttributes($hiddenFile, [IO.FileAttributes]::Hidden)
        $systemFile = Join-Path $fixtureRepository 'system.cs'
        Write-InventoryFixture $systemFile 'system'
        [IO.File]::SetAttributes($systemFile, [IO.FileAttributes]::System)
    }
    Write-InventoryFixture (Join-Path $fixtureRepository '.hidden-directory/Hidden.cs') 'hidden directory'
    if ([IO.Path]::DirectorySeparatorChar -eq '\') {
        [IO.File]::SetAttributes((Join-Path $fixtureRepository '.hidden-directory'), [IO.FileAttributes]::Hidden)
    }
    $linkTarget = Join-Path $fixtureRoot 'outside-repository'
    Write-InventoryFixture (Join-Path $linkTarget 'OutsideConfiguration.cs') 'outside'
    $linkType = if ([IO.Path]::DirectorySeparatorChar -eq '\') { 'Junction' } else { 'SymbolicLink' }
    $null = New-Item -ItemType $linkType -Path $linkedDirectory -Target $linkTarget

    foreach ($case in @(
        @{ filter = '*.cs'; excluded = @('tests','obj','bin','.artifacts','.llm-wiki','TestResults','Migrations'); regex = '[\\/](tests|obj|bin|\.artifacts|\.llm-wiki|TestResults|Migrations)[\\/]'; runtime = $true },
        @{ filter = '*Configuration.cs'; excluded = @('Migrations','node_modules','bin','obj','.artifacts','TestResults'); regex = '[\\/](Migrations|node_modules|bin|obj|\.artifacts|TestResults)[\\/]'; runtime = $false }
    )) {
        $expected = @(Get-CanonicalReferenceFiles $case.filter |
            Where-Object { $_.FullName -notmatch $case.regex -and (-not $case.runtime -or $_.Name -notmatch '\.(Designer|g)\.cs$') } |
            ForEach-Object FullName | Sort-Object)
        $actual = @(Get-LlmWikiSourceFiles -RepositoryRoot $fixtureRepository -Filter $case.filter -ExcludedDirectory $case.excluded |
            Where-Object { $_.FullName -notmatch $case.regex -and (-not $case.runtime -or $_.Name -notmatch '\.(Designer|g)\.cs$') } |
            ForEach-Object FullName | Sort-Object)
        if (($actual -join "`0") -cne ($expected -join "`0") -or $actual.Count -eq 0) {
            throw "Source inventory changed for $($case.filter): $(Compare-Object $expected $actual | ConvertTo-Json -Compress)"
        }
        if (@($actual | Where-Object { $_.StartsWith($linkedDirectory + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) }).Count -gt 0) {
            throw 'Source inventory followed a linked directory outside the repository.'
        }
    }
    $unicodeName = (-join @([char]0x0444, [char]0x0430, [char]0x0439, [char]0x043B)) + '.cs'
    $unicodeFile = Join-Path $fixtureRepository $unicodeName
    Write-InventoryFixture $unicodeFile 'canonical Unicode source'
    if ($unicodeFile -notin @(Get-LlmWikiSourceFiles -RepositoryRoot $fixtureRepository -Filter '*.cs' | ForEach-Object FullName)) {
        throw 'Source inventory lost a Unicode filename on this runtime.'
    }
    $forcedExpected = @(Get-ChildItem -LiteralPath $fixtureRepository -Recurse -File -Force -Filter '*.cs' |
        Where-Object {
            -not $_.FullName.StartsWith($linkedDirectory + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -and
            $_.FullName -notmatch '[\\/](tests|obj|bin|\.artifacts|TestResults|Migrations)[\\/]' -and
            $_.Name -notmatch '\.(Designer|g)\.cs$'
        } | ForEach-Object FullName | Sort-Object)
    $forcedActual = @(Get-LlmWikiSourceFiles -RepositoryRoot $fixtureRepository -Filter '*.cs' -Force -ExcludedDirectory @('tests','obj','bin','.artifacts','TestResults','Migrations') |
        Where-Object { $_.FullName -notmatch '[\\/](tests|obj|bin|\.artifacts|TestResults|Migrations)[\\/]' -and $_.Name -notmatch '\.(Designer|g)\.cs$' } |
        ForEach-Object FullName | Sort-Object)
    if (($forcedActual -join [char]0) -cne ($forcedExpected -join [char]0) -or $forcedActual.Count -eq 0) {
        throw 'Forced source enumeration changed the sensitive-data candidate source set.'
    }
    if ($hiddenFile -notin $forcedActual -or (Join-Path $fixtureRepository '.hidden-directory/Hidden.cs') -notin $forcedActual) {
        throw 'Force mode omitted hidden files or a hidden directory.'
    }
    if (@($forcedActual | Where-Object { $_.StartsWith($linkedDirectory + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) }).Count -gt 0) {
        throw 'Forced source inventory followed an external directory link.'
    }
    $before = Get-LlmWikiRuntimeTopologyFingerprint -RepositoryRoot $fixtureRepository
    if ($before.sourceFingerprint -cne (Get-ReferenceRuntimeFingerprint)) { throw 'Runtime fingerprint changed from the original normalized-content contract.' }
    $hasher = [Security.Cryptography.SHA256]::Create()
    try {
        foreach ($content in @('', "Кириллица 😀`r`nline`rnext", 'second hash')) {
            $expectedHash = Get-LlmWikiNormalizedContentHash -Content $content
            $reusedHash = Get-LlmWikiNormalizedContentHash -Content $content -Hasher $hasher
            if ($reusedHash -cne $expectedHash) { throw 'Shared SHA256 changed the normalized-content hash.' }
        }
        # The caller owns a supplied hasher; the helper must leave it usable.
        $null = $hasher.ComputeHash([byte[]]@())
    } finally { $hasher.Dispose() }
    $source = Join-Path $fixtureRepository 'Root.cs'
    $timestamp = [IO.File]::GetLastWriteTimeUtc($source)
    Write-InventoryFixture $source "public class Bravo {}`r`n"
    [IO.File]::SetLastWriteTimeUtc($source, $timestamp)
    $edited = Get-LlmWikiRuntimeTopologyFingerprint -RepositoryRoot $fixtureRepository
    if ($before.sourceFingerprint -ceq $edited.sourceFingerprint) { throw 'Equal-size, equal-time content change was hidden.' }
    Write-InventoryFixture $source "public class Bravo {}`n"
    $normalized = Get-LlmWikiRuntimeTopologyFingerprint -RepositoryRoot $fixtureRepository
    if ($edited.sourceFingerprint -cne $normalized.sourceFingerprint) { throw 'Line-ending normalization changed.' }
    $hashes = [Collections.Generic.Dictionary[string, string]]::new([StringComparer]::Ordinal)
    foreach ($path in @(@(Join-Path $fixtureRepository 'docker-compose.yml') + @(Get-LlmWikiRuntimeTopologySourceFiles -RepositoryRoot $fixtureRepository | ForEach-Object FullName))) {
        $hashes[$path] = Get-LlmWikiNormalizedContentHash -Content ([IO.File]::ReadAllText($path))
    }
    $reused = Get-LlmWikiRuntimeTopologyFingerprint -RepositoryRoot $fixtureRepository -SourceHashes $hashes
    if ($reused.sourceFingerprint -cne $normalized.sourceFingerprint -or $reused.sourceFileCount -ne $normalized.sourceFileCount) { throw 'Generator hash reuse changed the fingerprint.' }
    $untracked = Join-Path $fixtureRepository 'NewSource.cs'
    Write-InventoryFixture $untracked 'new source'
    $added = Get-LlmWikiRuntimeTopologyFingerprint -RepositoryRoot $fixtureRepository
    if ($added.sourceFileCount -ne $normalized.sourceFileCount + 1 -or $added.sourceFingerprint -ceq $normalized.sourceFingerprint) { throw 'New untracked source was omitted.' }
    Remove-Item -LiteralPath $untracked
    if ((Get-LlmWikiRuntimeTopologyFingerprint -RepositoryRoot $fixtureRepository).sourceFingerprint -cne $normalized.sourceFingerprint) { throw 'Deleting a source did not restore the fingerprint.' }
    Remove-Item -LiteralPath (Join-Path $fixtureRepository 'docker-compose.yml')
    if ((Get-LlmWikiRuntimeTopologyFingerprint -RepositoryRoot $fixtureRepository).sourceFileCount -ne $normalized.sourceFileCount - 1) { throw 'Missing Compose file was retained.' }
    Write-Host 'Source inventory passed: original paths and fingerprints, exclusions, links, Unicode, untracked sources and equal-time edits.'
} finally {
    if ([IO.Path]::GetDirectoryName($fixtureRoot) -cne [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\', '/') -or
        [IO.Path]::GetFileName($fixtureRoot) -notmatch '^wiki-source-inventory-[a-f0-9]{32}$') { throw 'Unsafe fixture cleanup path.' }
    # Directory.Delete without recursion unlinks the junction/symlink itself;
    # legacy Remove-Item can throw a NullReferenceException for a junction.
    if (Test-Path -LiteralPath $linkedDirectory) { [IO.Directory]::Delete($linkedDirectory) }
    if (Test-Path -LiteralPath $fixtureRoot) { Remove-Item -LiteralPath $fixtureRoot -Recurse -Force }
}
