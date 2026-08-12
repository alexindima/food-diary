function Test-LlmWikiSameSet([object[]]$Left, [object[]]$Right) {
    $leftValues = @($Left | ForEach-Object { [string]$_ } | Sort-Object -Unique)
    $rightValues = @($Right | ForEach-Object { [string]$_ } | Sort-Object -Unique)
    return ($leftValues.Count -eq $rightValues.Count -and @(Compare-Object $leftValues $rightValues).Count -eq 0)
}

function Get-LlmWikiPropertyValues([object[]]$InputObject, [string]$PropertyName) {
    foreach ($item in @($InputObject)) {
        if ($null -eq $item) { continue }
        $property = $item.PSObject.Properties[$PropertyName]
        if ($null -eq $property -or $null -eq $property.Value) { continue }
        foreach ($value in @($property.Value)) { $value }
    }
}
