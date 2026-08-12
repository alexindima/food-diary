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
