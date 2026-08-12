function Test-LlmWikiSameSet([object[]]$Left, [object[]]$Right) {
    $leftValues = @($Left | ForEach-Object { [string]$_ } | Sort-Object -Unique)
    $rightValues = @($Right | ForEach-Object { [string]$_ } | Sort-Object -Unique)
    return ($leftValues.Count -eq $rightValues.Count -and @(Compare-Object $leftValues $rightValues).Count -eq 0)
}
