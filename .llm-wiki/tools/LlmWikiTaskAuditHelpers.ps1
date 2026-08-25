function Read-Json([string]$Path) {
    try { return Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json } catch { return $null }
}

function Get-PropertyValue([object]$Object, [string]$Name) {
    if ($null -eq $Object) { return $null }
    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property) { return $null }
    $property.Value
}

function Get-AgeDays([DateTime]$Timestamp) {
    [Math]::Round([Math]::Max(0, ($script:auditTime - $Timestamp.ToUniversalTime()).TotalDays), 2)
}

function Convert-ToUtc([object]$Value, [DateTime]$Fallback) {
    $parsed = [DateTime]::MinValue
    if ($null -ne $Value -and [DateTime]::TryParse(
        [string]$Value,
        [Globalization.CultureInfo]::InvariantCulture,
        [Globalization.DateTimeStyles]::AssumeUniversal,
        [ref]$parsed)) {
        return $parsed.ToUniversalTime()
    }
    $Fallback.ToUniversalTime()
}
