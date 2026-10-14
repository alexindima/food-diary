. (Join-Path $PSScriptRoot 'LlmWikiNativeCompilation.ps1')
function Get-LlmWikiRankedPrecedentHistory {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$RepositoryRoot,
        [AllowEmptyCollection()][string[]]$Tokens,
        [AllowEmptyCollection()][object[]]$ScopeMatchers
    )
    $timer = [Diagnostics.Stopwatch]::StartNew()
    # Current Git observes replacements, shallow boundaries, independent refs and
    # config. Native parsing avoids a persistent history cache's invalidation cost.
    $raw = Invoke-LlmWikiGitCommand -RepositoryRoot $RepositoryRoot -Arguments @(
        'log', '--all', '-n', '300', '--date=iso-strict', '--pretty=format:@@@%H%x09%ad%x09%s', '--name-only'
    ) -FailureMessage 'Unable to read Git history for precedent analysis.'
    Write-Verbose ('Git precedent phase=history-read durationMs={0}' -f [Math]::Round($timer.Elapsed.TotalMilliseconds, 2))
    $scanner = Get-LlmWikiCompiledToolType -RepositoryRoot $RepositoryRoot -SourceFile 'LlmWikiGitHistoryScanner.cs' -ClassName HistoryScanner
    if ($null -ne $scanner) {
        $history = $scanner::Rank($raw.StandardOutput, [string[]]$Tokens, [string[]]@($ScopeMatchers | ForEach-Object path), [string[]]@($ScopeMatchers | ForEach-Object prefix))
        Write-Verbose ('Git precedent phase=native-history-ranking elapsedMs={0}' -f [Math]::Round($timer.Elapsed.TotalMilliseconds, 2))
        return $history
    }
    # Windows PowerShell fallback retains the prior parser and evidence ranking.
    $commits = [Collections.Generic.List[object]]::new()
    $current = $null
    foreach ($line in $raw.Lines) {
        if ($line.StartsWith('@@@')) {
            if ($null -ne $current) { $commits.Add([pscustomobject]$current) }
            $parts = $line.Substring(3).Split("`t", 3)
            $current = [ordered]@{ hash = $parts[0]; date = $parts[1]; subject = $parts[2]; paths = [Collections.Generic.List[string]]::new() }
        } elseif ($null -ne $current -and -not [string]::IsNullOrWhiteSpace($line)) { $current.paths.Add($line.Trim().Replace('\', '/')) }
    }
    if ($null -ne $current) { $commits.Add([pscustomobject]$current) }
    $ranked = @(foreach ($commit in $commits) {
        $subject = $commit.subject.ToLowerInvariant()
        $matchedTokens = @($Tokens | Where-Object { $subject.Contains($_) })
        $matchedPaths = [Collections.Generic.List[string]]::new()
        foreach ($candidate in $commit.paths) {
            foreach ($scope in $ScopeMatchers) {
                if ($candidate -eq $scope.path -or $candidate.StartsWith($scope.prefix, [StringComparison]::OrdinalIgnoreCase)) { $matchedPaths.Add($candidate); break }
            }
            if ($matchedPaths.Count -eq 12) { break }
        }
        $score = ($matchedTokens.Count * 4) + ([Math]::Min($matchedPaths.Count, 5) * 3)
        if ($score -le 0) { continue }
        [pscustomobject][ordered]@{
            hash = $commit.hash; shortHash = $commit.hash.Substring(0, 10); date = $commit.date; subject = $commit.subject
            score = $score; matchedIntentTokens = $matchedTokens; matchedPaths = @($matchedPaths)
        }
    })
    [pscustomobject]@{ CommitCount = $commits.Count; Ranked = $ranked }
}
