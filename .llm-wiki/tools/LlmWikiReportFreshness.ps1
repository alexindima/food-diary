function Get-LlmWikiReportFreshness {
    param([string]$RepositoryRoot, [string]$ReportPath, [string]$PacketFingerprint)
    $absolute = Join-Path $RepositoryRoot $ReportPath
    if (-not [IO.File]::Exists($absolute)) { return [pscustomobject]@{ state = 'missing'; changedInputs = @() } }
    $text = [IO.File]::ReadAllText($absolute)
    if ($text -notmatch '(?m)^<!-- LLM_WIKI_REPORT_FRESHNESS: (.+) -->\r?$') { return [pscustomobject]@{ state = 'legacy-unclassified'; changedInputs = @() } }
    try {
        $metadata = ConvertFrom-Json -InputObject $Matches[1] -AsHashtable
        if ($metadata.state -cne 'assessment-snapshot' -or $metadata.packetFingerprint -notmatch '^[a-f0-9]{64}$' -or -not $metadata.artifactHashes) { throw 'Invalid report metadata' }
        $changed = [Collections.Generic.List[string]]::new()
        if ($PacketFingerprint -and $metadata.packetFingerprint -cne $PacketFingerprint) { $changed.Add('change packet') }
        $prefix = [IO.Path]::GetFullPath($RepositoryRoot).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
        foreach ($artifact in $metadata.artifactHashes) {
            $path = if ([IO.Path]::IsPathRooted($artifact.path)) { [IO.Path]::GetFullPath($artifact.path) } else { [IO.Path]::GetFullPath((Join-Path $RepositoryRoot $artifact.path)) }
            if (-not $path.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'Report input is outside the repository' }
            $actual = if ([IO.File]::Exists($path)) { (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() } else { '<missing>' }
            if ($actual -cne $artifact.sha256) { $changed.Add([string]$artifact.path) }
        }
        return [pscustomobject]@{ state = $(if ($changed.Count) { 'stale' } else { 'current' }); assessedAtUtc = $metadata.assessedAtUtc; changedInputs = @($changed) }
    } catch { return [pscustomobject]@{ state = 'invalid-metadata'; changedInputs = @() } }
}
