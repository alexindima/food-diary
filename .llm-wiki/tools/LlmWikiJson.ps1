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

function Get-LlmWikiJsonFingerprint {
    [CmdletBinding()]
    param(
        [AllowNull()]
        [object]$Value,
        [ValidateRange(1, 100)]
        [int]$Depth = 30
    )

    if ($null -eq $Value) { $Value = @() }
    $json = ConvertTo-Json -InputObject $Value -Depth $Depth -Compress
    # Windows PowerShell and PowerShell 7 use different JSON serializers. Normalize
    # HTML-sensitive characters and escape casing before hashing persisted payloads.
    $json = $json.Replace('&', '\u0026').Replace('<', '\u003c').Replace('>', '\u003e')
    # Retain existing PowerShell 7 fingerprints when Windows PowerShell escapes
    # apostrophes. An even number of preceding slashes denotes a real Unicode
    # escape; odd counts belong to a literal backslash-u sequence and stay intact.
    # The replacement delegates are expensive even when neither regex can match.
    # Both patterns require this exact marker, including literal escaped text.
    if ($json.IndexOf('\u', [StringComparison]::Ordinal) -ge 0) {
        $json = [regex]::Replace($json, '(\\*)\\u0027', {
            param($match)
            $prefix = $match.Groups[1].Value
            if ($prefix.Length % 2 -eq 0) { return $prefix + "'" }
            return $match.Value
        })
        $json = [regex]::Replace($json, '(\\*)\\u[0-9A-Fa-f]{4}', {
            param($match)
            if ($match.Groups[1].Value.Length % 2 -eq 0) { return $match.Value.ToLowerInvariant() }
            return $match.Value
        })
    }

    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        return ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($json))) -replace '-', '').ToLowerInvariant()
    } finally {
        $sha.Dispose()
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

function ConvertTo-LlmWikiCanonicalJson {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipeline)]
        [AllowNull()]
        [object]$Value,
        [ValidateRange(1, 100)]
        [int]$Depth = 30
    )

    process {
        $compact = ConvertTo-Json -InputObject $Value -Depth $Depth -Compress
        $builder = New-Object System.Text.StringBuilder
        $indent = 0
        $inString = $false
        $escaped = $false

        foreach ($character in $compact.ToCharArray()) {
            if ($inString) {
                $null = $builder.Append($character)
                if ($escaped) { $escaped = $false }
                elseif ($character -eq '\') { $escaped = $true }
                elseif ($character -eq '"') { $inString = $false }
                continue
            }

            if ($character -eq '"') {
                $inString = $true
                $null = $builder.Append($character)
                continue
            }

            switch ($character) {
                { $_ -eq '{' -or $_ -eq '[' } {
                    $null = $builder.Append($character)
                    $indent++
                    $null = $builder.Append("`n").Append(' ' * ($indent * 2))
                }
                { $_ -eq '}' -or $_ -eq ']' } {
                    $indent--
                    $null = $builder.Append("`n").Append(' ' * ($indent * 2)).Append($character)
                }
                ',' { $null = $builder.Append(",`n").Append(' ' * ($indent * 2)) }
                ':' { $null = $builder.Append(': ') }
                default { $null = $builder.Append($character) }
            }
        }

        return $builder.ToString() + "`n"
    }
}
