$llmWikiInProcessSqliteCacheKey = 'FoodDiary.LlmWiki.InProcessSqlite.State'

function Initialize-LlmWikiInProcessSqlite {
    [CmdletBinding()]
    param(
        [ValidateSet('', 'architecture-health', 'domain', 'runtime')]
        [string]$Projection = ''
    )

    if (-not [string]::IsNullOrWhiteSpace($Projection)) {
        . (Join-Path $PSScriptRoot 'Ensure-LlmWikiSqliteProjection.ps1')
        Ensure-LlmWikiSqliteProjection -Category $Projection
    }

    if ($PSVersionTable.PSVersion.Major -lt 7) {
        throw 'In-process SQLite Wiki queries require PowerShell 7 (pwsh).'
    }

    $cachedState = [AppDomain]::CurrentDomain.GetData($llmWikiInProcessSqliteCacheKey)
    if ($null -ne $cachedState -and
        $null -ne ('LlmWiki.SqliteReader.DomainDataReader' -as [type])) {
        return $cachedState
    }
    $stopwatch = [Diagnostics.Stopwatch]::StartNew()
    $build = & (Join-Path $PSScriptRoot 'Build-LlmWikiInProcessSqliteReader.ps1') -Format Json | ConvertFrom-Json
    if (-not [bool]$build.ready) { throw 'In-process SQLite reader build did not become ready.' }
    $outputPath = [string]$build.outputPath
    foreach ($assemblyName in @(
        'SQLitePCLRaw.core.dll'
        'SQLitePCLRaw.provider.e_sqlite3.dll'
        'SQLitePCLRaw.batteries_v2.dll'
        'Microsoft.Data.Sqlite.dll'
        'LlmWiki.SqliteReader.dll'
    )) {
        $assemblyPath = Join-Path $outputPath $assemblyName
        if (-not (Test-Path -LiteralPath $assemblyPath -PathType Leaf)) {
            throw "In-process SQLite reader dependency is missing: $assemblyPath"
        }
        $assemblySimpleName = [IO.Path]::GetFileNameWithoutExtension($assemblyName)
        $loadedAssembly = [AppDomain]::CurrentDomain.GetAssemblies() |
            Where-Object { $_.GetName().Name -eq $assemblySimpleName } |
            Select-Object -First 1
        if ($null -eq $loadedAssembly) {
            Add-Type -LiteralPath $assemblyPath -ErrorAction Stop
        } else {
            $expectedVersion = [Reflection.AssemblyName]::GetAssemblyName($assemblyPath).Version
            if ($loadedAssembly.GetName().Version -ne $expectedVersion) {
                throw "In-process SQLite reader dependency version conflict: $assemblySimpleName is already loaded as $($loadedAssembly.GetName().Version), expected $expectedVersion."
            }
        }
    }
    $stopwatch.Stop()
    $state = [pscustomobject][ordered]@{
        ready = $true
        reusedBuild = [bool]$build.reused
        fingerprint = [string]$build.fingerprint
        runtimeIdentifier = [string]$build.runtimeIdentifier
        outputPath = $outputPath
        loadDurationMs = [Math]::Round($stopwatch.Elapsed.TotalMilliseconds, 2)
    }
    [AppDomain]::CurrentDomain.SetData($llmWikiInProcessSqliteCacheKey, $state)
    return $state
}
