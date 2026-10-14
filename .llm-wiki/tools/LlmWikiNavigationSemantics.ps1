. (Join-Path $PSScriptRoot 'LlmWikiJson.ps1')

function ConvertTo-LlmWikiNavigationSemantics([AllowNull()][object]$Value) {
    if ($null -eq $Value) { return $null }
    if ($Value -is [Collections.IDictionary]) {
        $result = [ordered]@{}
        foreach ($key in @($Value.Keys | Sort-Object)) {
            # Schema 1 navigation indexes use numeric line solely as a source
            # coordinate. Keep every other field, array position and path.
            if ($key -ceq 'line' -and ($Value[$key] -is [int] -or $Value[$key] -is [long])) { continue }
            $result[$key] = ConvertTo-LlmWikiNavigationSemantics $Value[$key]
        }
        return $result
    }
    if ($Value -is [Collections.IEnumerable] -and $Value -isnot [string]) {
        return ,@($Value | ForEach-Object { ConvertTo-LlmWikiNavigationSemantics $_ })
    }
    return $Value
}

function Test-LlmWikiNavigationCoordinatesOnly([string]$Path, [string]$Before, [string]$After) {
    $generators = @{
        '.llm-wiki/generated/frontend-index.json' = '.llm-wiki/tools/Build-LlmWikiFrontendIndex.ps1'
        '.llm-wiki/generated/csharp-symbol-index.json' = '.llm-wiki/tools/Build-LlmWikiSymbolIndex.ps1'
    }
    if (-not $generators.ContainsKey($Path)) { return $false }
    try {
        $old = ConvertFrom-Json -InputObject $Before -AsHashtable
        $current = ConvertFrom-Json -InputObject $After -AsHashtable
        if ($old.schemaVersion -ne 1 -or $current.schemaVersion -ne 1 -or $old.generator -cne $generators[$Path] -or $current.generator -cne $generators[$Path]) { return $false }
        $left = ConvertTo-LlmWikiNavigationSemantics $old | ConvertTo-Json -Depth 100 -Compress
        $right = ConvertTo-LlmWikiNavigationSemantics $current | ConvertTo-Json -Depth 100 -Compress
        return $left -ceq $right
    } catch { return $false }
}
