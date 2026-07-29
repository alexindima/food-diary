[CmdletBinding()]
param([switch]$Check)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiJson.ps1')
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$outputPath = Join-Path $wikiRoot 'generated/sensitive-data-index.json'

function ConvertTo-RepositoryPath {
    param([string]$Path)
    return [System.IO.Path]::GetFullPath($Path).Substring($repositoryRoot.Length + 1).Replace('\', '/')
}

$categories = [ordered]@{
    credential = '(?i)(Password|RefreshToken|AccessToken|TokenHash|Token$|Secret|ApiKey|SigningKey|Credential|ConnectionString)'
    identity = '(?i)(Email|Phone|Telegram(UserId|Username)?|GoogleSubject|IpAddress|UserAgent|ExternalIdentity)'
    health = '(?i)(Weight|Waist|Height|Birth(Date)?|DateOfBirth|Gender|ActivityLevel|Cycle|Fasting|Nutrition|Calories|Hydration|Exercise|Satiety|Tdee)'
    financial = '(?i)(Payment|Subscription|CustomerId|Invoice|Checkout|Billing|Money|Amount|Currency)'
    privateContent = '(?i)(MessageBody|MessageContent|Comment|Notes|Prompt|ImageUrl|ImageAsset|ReportReason)'
}

$fields = [System.Collections.Generic.List[object]]::new()
$potentialLogging = [System.Collections.Generic.List[object]]::new()
$boundaryFiles = [System.Collections.Generic.List[object]]::new()
$sourceFiles = @(
    Get-ChildItem -LiteralPath $repositoryRoot -Recurse -File -Filter '*.cs' |
        Where-Object {
            $_.FullName -notmatch '[\\/](tests|obj|bin|\.artifacts|TestResults|Migrations)[\\/]' -and
            $_.Name -notmatch '\.(Designer|g)\.cs$'
        } |
        Sort-Object FullName
)

foreach ($file in $sourceFiles) {
    $content = [System.IO.File]::ReadAllText($file.FullName)
    $path = ConvertTo-RepositoryPath $file.FullName
    $fileFields = [System.Collections.Generic.List[object]]::new()
    foreach ($match in [regex]::Matches(
        $content,
        '(?m)(?:public\s+)?(?:required\s+)?(?<type>[A-Za-z_][A-Za-z0-9_?<>,.\[\]]*)\s+(?<name>[A-Z][A-Za-z0-9_]*)\s*(?:\{|[,;)])')) {
        $name = $match.Groups['name'].Value
        $declaredType = $match.Groups['type'].Value
        if ($declaredType -in @('class', 'interface', 'record', 'struct', 'enum') -or
            $name -match '^(I[A-Z].*(Repository|Service|Provider|Client|Gateway))$' -or
            $name -match '(Handler|Controller|Validator|Repository|Service|Provider|Client|Gateway|Options|Async)$') {
            continue
        }
        $category = $null
        foreach ($entry in $categories.GetEnumerator()) {
            if ($name -match $entry.Value) { $category = $entry.Key; break }
        }
        if ($null -eq $category) { continue }
        $line = 1 + [regex]::Matches($content.Substring(0, $match.Index), "`n").Count
        $item = [pscustomobject]@{
            category = $category
            name = $name
            type = $declaredType
            path = $path
            line = $line
        }
        $fileFields.Add($item)
        $fields.Add($item)
    }

    foreach ($logMatch in [regex]::Matches(
        $content,
        '(?ms)\bLog(?:Trace|Debug|Information|Warning|Error|Critical)\s*\((?<call>.{0,1000}?)\);')) {
        $call = $logMatch.Groups['call'].Value
        $matchedNames = @(
            $fileFields |
                Where-Object {
                    $call -match "\b$([regex]::Escape($_.name))\b|\{$([regex]::Escape($_.name))\}"
                } |
                Select-Object -ExpandProperty name -Unique
        )
        if ($matchedNames.Count -gt 0) {
            $potentialLogging.Add([pscustomobject]@{
                path = $path
                line = 1 + [regex]::Matches($content.Substring(0, $logMatch.Index), "`n").Count
                fieldNames = $matchedNames
            })
        }
    }

    if ($fileFields.Count -gt 0 -and $path -match '(Presentation|Integrations|Export|MailRelay|MailInbox|Telegram|Responses|Requests)') {
        $boundaryFiles.Add([pscustomobject]@{
            path = $path
            categories = @($fileFields.category | Sort-Object -Unique)
            fieldCount = $fileFields.Count
        })
    }
}

$uniqueFields = @($fields | Sort-Object category, path, line, name -Unique)
$result = [ordered]@{
    schemaVersion = 1
    semantics = [ordered]@{
        inventory = 'Name-based candidate sensitive fields. Confirm semantics in source before making privacy claims.'
        potentialLogging = 'A logging call near a candidate field name. This is a review lead, not proof that a runtime value is logged.'
    }
    summary = [ordered]@{
        candidateFields = $uniqueFields.Count
        credential = @($uniqueFields | Where-Object category -eq 'credential').Count
        identity = @($uniqueFields | Where-Object category -eq 'identity').Count
        health = @($uniqueFields | Where-Object category -eq 'health').Count
        financial = @($uniqueFields | Where-Object category -eq 'financial').Count
        privateContent = @($uniqueFields | Where-Object category -eq 'privateContent').Count
        boundaryFiles = @($boundaryFiles | Sort-Object path -Unique).Count
        potentialLoggingLeads = @($potentialLogging | Sort-Object path, line -Unique).Count
    }
    fields = $uniqueFields
    boundaryFiles = @($boundaryFiles | Sort-Object path -Unique)
    potentialLogging = @($potentialLogging | Sort-Object path, line -Unique)
}
$jsonText = ($result | ConvertTo-Json -Depth 10) + [Environment]::NewLine
if ($Check) {
    if (-not (Test-LlmWikiJsonEquivalent -ActualPath $outputPath -ExpectedJson $jsonText -Depth 10)) {
        Write-Host 'Sensitive data index is stale. Run ./.llm-wiki/wiki.ps1 update.'
        exit 1
    }
    Write-Host "Sensitive data index is current: $($result.summary.candidateFields) candidates, $($result.summary.boundaryFiles) boundary files."
    exit 0
}
$utf8WithoutBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($outputPath, $jsonText, $utf8WithoutBom)
Write-Host "Generated .llm-wiki/generated/sensitive-data-index.json."
