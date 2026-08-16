Set-StrictMode -Version Latest

function ConvertFrom-LlmWikiGitNameStatus {
    param([string[]]$Lines)

    @(
        foreach ($line in @($Lines)) {
            if ([string]::IsNullOrWhiteSpace($line)) { continue }
            $parts = @($line -split "`t")
            if ($parts.Count -lt 3 -or $parts[0] -notmatch '^R\d{0,3}$') { continue }
            [pscustomobject][ordered]@{
                status = [string]$parts[0]
                from = ([string]$parts[1]).Replace('\', '/')
                to = ([string]$parts[2]).Replace('\', '/')
            }
        }
    )
}

function Get-LlmWikiGitRenames {
    param(
        [Parameter(Mandatory)][string]$RepositoryRoot,
        [Parameter(Mandatory)][string]$BaseRef,
        [string]$HeadRef
    )

    $arguments = @('-c', 'core.quotepath=false', '-C', $RepositoryRoot, 'diff', '--name-status', '--find-renames', '--diff-filter=R', $BaseRef)
    if (-not [string]::IsNullOrWhiteSpace($HeadRef) -and $HeadRef -ine 'HEAD') { $arguments += $HeadRef }
    $arguments += '--'
    $lines = @(& git @arguments)
    if ($LASTEXITCODE -ne 0) { throw "git rename discovery failed for base '$BaseRef' and head '$HeadRef'." }
    @(ConvertFrom-LlmWikiGitNameStatus $lines)
}

function Test-LlmWikiRenameDestination {
    param(
        [Parameter(Mandatory)][string]$Path,
        [object[]]$Renames,
        [string[]]$KnownPaths
    )

    $normalized = $Path.Replace('\', '/')
    foreach ($rename in @($Renames)) {
        if ($null -eq $rename -or -not $rename.PSObject.Properties['from'] -or -not $rename.PSObject.Properties['to']) { continue }
        $from = ([string]$rename.from).Replace('\', '/')
        $to = ([string]$rename.to).Replace('\', '/')
        $fromDirectory = if ($from.Contains('/')) { $from.Substring(0, $from.LastIndexOf('/')) } else { '' }
        $toDirectory = if ($to.Contains('/')) { $to.Substring(0, $to.LastIndexOf('/')) } else { '' }
        $isExactDestination = $normalized -ceq $to
        $isInsideDestination = -not [string]::IsNullOrWhiteSpace($toDirectory) -and
            $normalized.StartsWith("$toDirectory/", [StringComparison]::Ordinal)
        if (-not $isExactDestination -and -not $isInsideDestination) { continue }
        if ($from -in @($KnownPaths) -or @($KnownPaths | Where-Object {
            $known = ([string]$_).Replace('\', '/').TrimEnd('/')
            $from -eq $known -or
                $from.StartsWith("$known/", [StringComparison]::Ordinal) -or
                (-not [string]::IsNullOrWhiteSpace($fromDirectory) -and
                    ($known.StartsWith("$fromDirectory/", [StringComparison]::Ordinal) -or $known -ceq $fromDirectory))
        }).Count -gt 0) { return $true }
    }
    return $false
}
