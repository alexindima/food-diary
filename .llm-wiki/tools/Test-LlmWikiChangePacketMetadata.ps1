[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiChangePacket.ps1')

$current = [pscustomobject]@{ inputs = [pscustomobject]@{ objective = 'Current objective' } }
$legacy = [pscustomobject]@{ objective = 'Legacy objective' }
if ((Get-LlmWikiPacketObjective $current) -cne 'Current objective') { throw 'Current packet objective was not read.' }
if ((Get-LlmWikiPacketObjective $legacy) -cne 'Legacy objective') { throw 'Legacy packet objective was not read.' }

$message = $null
try { $null = Get-LlmWikiPacketObjective ([pscustomobject]@{}) } catch { $message = $_.Exception.Message }
if ($message -cne 'Change packet does not contain inputs.objective or legacy objective.') {
    throw "Missing packet objective did not produce the expected diagnostic: $message"
}

Write-Host 'LLM Wiki change-packet metadata compatibility tests passed.'
