function Test-LlmWikiPresentationOnlyTemplateDiff([string]$DiffText) {
    if ([string]::IsNullOrWhiteSpace($DiffText)) { return $false }

    $added = [System.Collections.Generic.List[string]]::new()
    $removed = [System.Collections.Generic.List[string]]::new()
    foreach ($line in @($DiffText -split "`r?`n")) {
        if ($line -match '^\+\+\+|^---|^@@') { continue }
        if ($line.StartsWith('+')) { $added.Add($line.Substring(1)) }
        elseif ($line.StartsWith('-')) { $removed.Add($line.Substring(1)) }
    }
    if ($added.Count -eq 0 -and $removed.Count -eq 0) { return $false }

    function ConvertTo-TemplateSemanticLine([string]$Line) {
        $normalized = $Line
        $normalized = [regex]::Replace($normalized, '\s+class\s*=\s*("[^"]*"|''[^'']*'')', '', 'IgnoreCase')
        $normalized = [regex]::Replace($normalized, '\s+style\s*=\s*("[^"]*"|''[^'']*'')', '', 'IgnoreCase')
        $normalized = [regex]::Replace($normalized, '\s+\[(?:class|style)(?:\.[^\]]+)?\]\s*=\s*("[^"]*"|''[^'']*'')', '', 'IgnoreCase')
        return ($normalized -replace '\s+', ' ').Trim()
    }

    $addedSemantic = @($added | ForEach-Object { ConvertTo-TemplateSemanticLine $_ } | Where-Object { $_ })
    $removedSemantic = @($removed | ForEach-Object { ConvertTo-TemplateSemanticLine $_ } | Where-Object { $_ })
    if ($addedSemantic.Count -ne $removedSemantic.Count) { return $false }

    $addedSorted = @($addedSemantic | Sort-Object)
    $removedSorted = @($removedSemantic | Sort-Object)
    for ($index = 0; $index -lt $addedSorted.Count; $index++) {
        if ($addedSorted[$index] -cne $removedSorted[$index]) { return $false }
    }
    return $true
}

function Get-LlmWikiPathDiff([string]$RepositoryRoot, [string]$Path) {
    $parts = [System.Collections.Generic.List[string]]::new()
    $diffArgumentSets = @(
        , @('diff', '--cached', '--unified=0', '--', $Path)
        , @('diff', '--unified=0', '--', $Path)
    )
    foreach ($arguments in $diffArgumentSets) {
        $text = (& git -C $RepositoryRoot @arguments) -join [Environment]::NewLine
        if ($LASTEXITCODE -ne 0) { throw "Unable to inspect semantic diff for '$Path'." }
        if (-not [string]::IsNullOrWhiteSpace($text)) { $parts.Add($text) }
    }
    return $parts -join [Environment]::NewLine
}

function Test-LlmWikiBookkeepingPath([string]$Path) {
    $normalized = $Path.Replace('\\', '/')
    return $normalized -match '^\.llm-wiki/generated/' -or
        $normalized -eq '.llm-wiki/reviews/source-impact-reviews.json'
}
