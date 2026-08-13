Set-StrictMode -Version Latest

function Get-LlmWikiCriterionCompoundConnectorCount([string]$Text) {
    @([regex]::Matches([string]$Text, '(?i)\b(and|or|but|while|unless)\b|[,;]')).Count
}

function Test-LlmWikiCriterionAtomic([string]$Text, [object]$RequirementPolicy) {
    (Get-LlmWikiCriterionCompoundConnectorCount $Text) -le [int]$RequirementPolicy.maximumCompoundConnectors
}
