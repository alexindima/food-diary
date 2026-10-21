Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'LlmWikiJson.ps1')

function Get-LlmWikiCriterionFingerprint([object]$Criterion) {
    Get-LlmWikiJsonFingerprint ([pscustomobject][ordered]@{ id = [string]$Criterion.id; text = [string]$Criterion.text })
}

function Get-LlmWikiCriterionCompoundConnectorCount([string]$Text) {
    @([regex]::Matches([string]$Text, '(?i)\b(and|or|but|while|unless)\b|[,;]')).Count
}

function Test-LlmWikiCriterionAtomic([string]$Text, [object]$RequirementPolicy) {
    (Get-LlmWikiCriterionCompoundConnectorCount $Text) -le [int]$RequirementPolicy.maximumCompoundConnectors
}
