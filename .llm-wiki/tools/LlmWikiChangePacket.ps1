function Get-LlmWikiPacketObjective([object]$Packet) {
    if ($null -eq $Packet) { throw 'Change packet is absent.' }
    if ($Packet.PSObject.Properties['inputs'] -and $null -ne $Packet.inputs -and
        $Packet.inputs.PSObject.Properties['objective'] -and
        -not [string]::IsNullOrWhiteSpace([string]$Packet.inputs.objective)) {
        return [string]$Packet.inputs.objective
    }
    if ($Packet.PSObject.Properties['objective'] -and
        -not [string]::IsNullOrWhiteSpace([string]$Packet.objective)) {
        return [string]$Packet.objective
    }
    throw 'Change packet does not contain inputs.objective or legacy objective.'
}

function Get-LlmWikiPacketContentFingerprint([object]$Packet) {
    . (Join-Path $PSScriptRoot 'LlmWikiJson.ps1')
    # The public packet fingerprint identifies the task inputs, not compiled
    # index contents. Bind derived artifacts to the contents as well. Only SQL
    # provider timing diagnostics are excluded; source hashes and records stay.
    function Convert-PacketContent([object]$Value, [bool]$ProviderDiagnostics = $false) {
        if ($null -eq $Value) { return $null }
        if ($Value -is [Collections.IDictionary] -or $Value -is [pscustomobject]) {
            $properties = if ($Value -is [Collections.IDictionary]) { @($Value.Keys) } else { @($Value.PSObject.Properties.Name) }
            $result = [ordered]@{}
            foreach ($key in @($properties | Sort-Object)) {
                if ($ProviderDiagnostics -and $key -in @('sqlDurationMs', 'roundTripDurationMs')) { continue }
                $child = if ($Value -is [Collections.IDictionary]) { $Value[$key] } else { $Value.$key }
                $result[$key] = Convert-PacketContent $child ($key -in @('compiledIndex', 'impactIndex'))
            }
            return $result
        }
        if ($Value -is [Collections.IEnumerable] -and $Value -isnot [string]) {
            return ,@($Value | ForEach-Object { Convert-PacketContent $_ })
        }
        return $Value
    }
    Get-LlmWikiJsonFingerprint (Convert-PacketContent $Packet) -Depth 100
}
