function Get-ObjectPropertyValues([object[]]$InputObject, [string]$Name) {
    @($InputObject | ForEach-Object {
        if ($null -ne $_ -and $_.PSObject.Properties[$Name]) { $_.$Name }
    } | Where-Object { $null -ne $_ -and -not [string]::IsNullOrWhiteSpace([string]$_) })
}

function Get-NormalizedResearchPaths([object[]]$InputObject) {
    @($InputObject |
        Where-Object { $null -ne $_ -and -not [string]::IsNullOrWhiteSpace([string]$_) } |
        ForEach-Object { ([string]$_).Replace('\', '/').TrimEnd('/') } |
        Where-Object { $_ } |
        Sort-Object -Unique)
}

function Test-RepositoryReadPath([string]$Path) {
    if ([string]::IsNullOrWhiteSpace($Path) -or [IO.Path]::IsPathRooted($Path)) { return $false }
    Test-Path -LiteralPath (Join-Path $repositoryRoot $Path) -PathType Leaf
}

function Get-SharedPathCount([object[]]$Left, [object[]]$Right) {
    @($Left | Where-Object { $_ -in $Right }).Count
}

function ConvertFrom-UnicodeEscape([string]$Value) {
    ('"' + $Value + '"') | ConvertFrom-Json
}

function New-GroundedQuestion(
    [string]$Id,
    [bool]$Blocking,
    [string]$Question,
    [string]$EvidenceNeeded,
    [string]$WhyUserInputIsRequired,
    [object]$Anchor,
    [string]$ResolutionCommand = ''
) {
    [pscustomobject][ordered]@{
        id = $Id
        blocking = $Blocking
        question = $Question
        evidenceNeeded = $EvidenceNeeded
        whyUserInputIsRequired = $WhyUserInputIsRequired
        anchorStatus = $(if ($null -ne $Anchor) { [string]$Anchor.status } else { 'missing' })
        anchor = $(if ($null -ne $Anchor) {
            [pscustomobject][ordered]@{ path = $Anchor.path; line = $Anchor.line; symbol = $Anchor.symbol }
        } else { $null })
        resolutionCommand = $ResolutionCommand
    }
}
