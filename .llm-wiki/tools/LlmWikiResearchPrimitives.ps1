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

function ConvertTo-LlmWikiJsonSafeObject([object]$InputObject) {
    if ($null -eq $InputObject -or $InputObject -is [string] -or $InputObject -is [ValueType]) {
        return $InputObject
    }
    if ($InputObject -is [Collections.IDictionary]) {
        $safeDictionary = [ordered]@{}
        foreach ($key in $InputObject.Keys) {
            $safeDictionary[[string]$key] = ConvertTo-LlmWikiJsonSafeObject $InputObject[$key]
        }
        return [pscustomobject]$safeDictionary
    }
    if ($InputObject -is [Collections.IEnumerable]) {
        $safeItems = @($InputObject | ForEach-Object { ConvertTo-LlmWikiJsonSafeObject $_ })
        Write-Output -NoEnumerate $safeItems
        return
    }
    if ($InputObject -is [psobject]) {
        $safeObject = [ordered]@{}
        foreach ($property in $InputObject.PSObject.Properties) {
            if (-not $property.IsGettable) { continue }
            $safeObject[$property.Name] = ConvertTo-LlmWikiJsonSafeObject $property.Value
        }
        return [pscustomobject]$safeObject
    }
    return $InputObject
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
