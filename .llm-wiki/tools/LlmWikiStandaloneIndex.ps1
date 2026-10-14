. (Join-Path $PSScriptRoot 'LlmWikiInProcessSqlite.ps1')

function Get-LlmWikiStandaloneReadState([string]$Json) {
    if ($PSVersionTable.PSVersion.Major -lt 7) { return $Json | ConvertFrom-Json }
    $document = [System.Text.Json.JsonDocument]::Parse($Json)
    try {
        $root = $document.RootElement
        $ready = $root.GetProperty('ready').GetBoolean()
        $reason = if ($ready) { '' } else { $root.GetProperty('unavailableReason').GetString() }
        $cacheProperty = [System.Text.Json.JsonElement]::new()
        $cachePath = if ($root.TryGetProperty('cacheDatabasePath', [ref]$cacheProperty)) { $cacheProperty.GetString() } else { '' }
        return [pscustomobject]@{ ready=$ready; unavailableReason=$reason; cacheDatabasePath=$cachePath }
    } finally { $document.Dispose() }
}

function Invoke-LlmWikiStandaloneIndexRead {
    [CmdletBinding()]
    param([string]$RepositoryRoot, [string]$Index, [string]$Query, [int]$Limit)

    try {
        $cacheStateKey = 'FoodDiary.LlmWiki.StandaloneIndex.Databases'
        $cacheState = [AppDomain]::CurrentDomain.GetData($cacheStateKey)
        if ($null -eq $cacheState) {
            $cacheState = @{}
            [AppDomain]::CurrentDomain.SetData($cacheStateKey, $cacheState)
        }
        $requestKey = $RepositoryRoot + [char]0 + $Index
        # Node owns Unicode query folding and exact module-inventory hashing.
        if ($PSVersionTable.PSVersion.Major -ge 7 -and $Index -ne 'modules' -and $Query -cnotmatch '[^\x00-\x7f]') {
            try {
                $null = Initialize-LlmWikiInProcessSqlite -RequireExistingBuild
                $cachedPath = if ($cacheState.ContainsKey($requestKey)) { [string]$cacheState[$requestKey] } else { $null }
                $result = [LlmWiki.SqliteReader.StandaloneIndexReader]::Query($RepositoryRoot, $Index, $Query, $Limit, $cachedPath)
                $status = Get-LlmWikiStandaloneReadState $result
                if ($status.ready -or $status.unavailableReason -eq 'compiled-source-missing') { return $result }
                if ($null -ne $cachedPath) { $cacheState.Remove($requestKey) }
            } catch {
                Write-Verbose "In-process standalone reader unavailable; trying the Node projection owner. $($_.Exception.Message)"
            }
        }
        $json = & node (Join-Path $PSScriptRoot 'read-standalone-index.mjs') $RepositoryRoot "--index=$Index" "--query=$Query" "--limit=$Limit" '--prepare-cache=true'
        if ($LASTEXITCODE -ne 0) { throw "Read-only standalone SQLite query failed with exit code $LASTEXITCODE." }
        $raw = $json -join [Environment]::NewLine
        $result = Get-LlmWikiStandaloneReadState $raw
        if ($result.ready -and -not [string]::IsNullOrWhiteSpace([string]$result.cacheDatabasePath)) {
            $cacheState[$requestKey] = [string]$result.cacheDatabasePath
        }
        return $raw
    } catch {
        Write-Verbose "Standalone read could not use its direct projection; trying isolated recovery. $($_.Exception.Message)"
        return '{"ready":false,"unavailableReason":"standalone-index-reader-unavailable"}'
    }
}
