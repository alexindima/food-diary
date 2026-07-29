function Enable-LlmWikiStringDateJsonParsing {
    [CmdletBinding()]
    param()

    $convertFromJson = Get-Command ConvertFrom-Json
    if (-not $convertFromJson.Parameters.ContainsKey('DateKind')) {
        return
    }
    if ($null -eq $global:PSDefaultParameterValues) {
        $global:PSDefaultParameterValues = @{}
    }
    $global:PSDefaultParameterValues['ConvertFrom-Json:DateKind'] = 'String'
}

function ConvertFrom-LlmWikiJson {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipeline)]
        [AllowEmptyString()]
        [string]$Json
    )

    process {
        $convertFromJson = Get-Command ConvertFrom-Json
        if ($convertFromJson.Parameters.ContainsKey('DateKind')) {
            return ConvertFrom-Json -InputObject $Json -DateKind String
        }
        return ConvertFrom-Json -InputObject $Json
    }
}

function Test-LlmWikiJsonEquivalent {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ActualPath,
        [Parameter(Mandatory)]
        [string]$ExpectedJson,
        [ValidateRange(1, 100)]
        [int]$Depth = 100
    )

    if (-not (Test-Path -LiteralPath $ActualPath -PathType Leaf)) {
        return $false
    }

    try {
        $actualObject = ConvertFrom-LlmWikiJson ([System.IO.File]::ReadAllText($ActualPath))
        $expectedObject = ConvertFrom-LlmWikiJson $ExpectedJson
        $actualCanonical = $actualObject | ConvertTo-Json -Depth $Depth -Compress
        $expectedCanonical = $expectedObject | ConvertTo-Json -Depth $Depth -Compress
        return $actualCanonical -ceq $expectedCanonical
    }
    catch {
        return $false
    }
}

function Get-LlmWikiOrdinalSortKey {
    [CmdletBinding()]
    param(
        [AllowEmptyString()]
        [string]$Value
    )

    $bytes = [System.Text.Encoding]::UTF8.GetBytes($Value)
    return [System.BitConverter]::ToString($bytes).Replace('-', '')
}

function Test-LlmWikiTextEquivalent {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ActualPath,
        [Parameter(Mandatory)]
        [string]$ExpectedText
    )

    if (-not (Test-Path -LiteralPath $ActualPath -PathType Leaf)) {
        return $false
    }

    $actualText = [System.IO.File]::ReadAllText($ActualPath)
    $normalizedActual = $actualText.Replace("`r`n", "`n").Replace("`r", "`n")
    $normalizedExpected = $ExpectedText.Replace("`r`n", "`n").Replace("`r", "`n")
    return $normalizedActual -ceq $normalizedExpected
}
