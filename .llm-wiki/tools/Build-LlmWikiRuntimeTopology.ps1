[CmdletBinding()]
param([switch]$Check)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiJson.ps1')
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$outputPath = Join-Path $wikiRoot 'generated/runtime-topology.json'

function ConvertTo-RepositoryPath {
    param([string]$Path)
    return [System.IO.Path]::GetFullPath($Path).Substring($repositoryRoot.Length + 1).Replace('\', '/')
}

$sourceFiles = @(
    Get-ChildItem -LiteralPath $repositoryRoot -Recurse -File -Filter '*.cs' |
        Where-Object {
            $_.FullName -notmatch '[\\/](tests|obj|bin|\.artifacts|TestResults|Migrations)[\\/]' -and
            $_.Name -notmatch '\.(Designer|g)\.cs$'
        } |
        Sort-Object { Get-LlmWikiOrdinalSortKey $_.FullName }
)
$hostedServices = [System.Collections.Generic.List[object]]::new()
$httpClients = [System.Collections.Generic.List[object]]::new()
$webhooks = [System.Collections.Generic.List[object]]::new()
$jobRegistrations = [System.Collections.Generic.List[object]]::new()

foreach ($file in $sourceFiles) {
    $content = [System.IO.File]::ReadAllText($file.FullName)
    $path = ConvertTo-RepositoryPath $file.FullName
    foreach ($match in [regex]::Matches(
        $content,
        '(?ms)(?:class|sealed\s+class)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*).{0,1500}?\)\s*:\s*(?<base>BackgroundService|IHostedService)\b|(?:class|sealed\s+class)\s+(?<name2>[A-Za-z_][A-Za-z0-9_]*)\s*:\s*(?<base2>BackgroundService|IHostedService)\b')) {
        $serviceName = if ($match.Groups['name'].Success) { $match.Groups['name'].Value } else { $match.Groups['name2'].Value }
        $baseType = if ($match.Groups['base'].Success) { $match.Groups['base'].Value } else { $match.Groups['base2'].Value }
        $hostedServices.Add([pscustomobject]@{
            name = $serviceName
            baseType = $baseType
            path = $path
        })
    }
    foreach ($match in [regex]::Matches(
        $content,
        'AddHttpClient\s*<\s*(?<contract>[A-Za-z_][A-Za-z0-9_]*)\s*,\s*(?<implementation>[A-Za-z_][A-Za-z0-9_]*)')) {
        $httpClients.Add([pscustomobject]@{
            contract = $match.Groups['contract'].Value
            implementation = $match.Groups['implementation'].Value
            registrationPath = $path
        })
    }
    foreach ($match in [regex]::Matches(
        $content,
        '(?m)(?:class|sealed\s+class)\s+(?<implementation>[A-Za-z_][A-Za-z0-9_]*(?:Client|Gateway|Provider|Transport))\s*\([^)]*\bHttpClient\s+')) {
        $implementation = $match.Groups['implementation'].Value
        if (@($httpClients | Where-Object implementation -eq $implementation).Count -eq 0) {
            $httpClients.Add([pscustomobject]@{
                contract = $null
                implementation = $implementation
                registrationPath = $path
            })
        }
    }
    if ($path -match 'Webhook' -or $content -match '\bWebhook(?:Controller|HttpRequest|Authorizer)\b') {
        foreach ($match in [regex]::Matches(
            $content,
            '(?m)(?:class|sealed\s+class)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*(?:Webhook|WebhookController|WebhookAuthorizer)[A-Za-z0-9_]*)')) {
            $webhooks.Add([pscustomobject]@{ name = $match.Groups['name'].Value; path = $path })
        }
    }
    foreach ($match in [regex]::Matches(
        $content,
        '(?ms)(?<api>(?:recurringJobManager|RecurringJob)\.(?:AddOrUpdate|RemoveIfExists))\s*\(\s*(?<detail>[^,\r\n\)]+)')) {
        $jobRegistrations.Add([pscustomobject]@{
            api = $match.Groups['api'].Value
            detail = $match.Groups['detail'].Value.Trim()
            path = $path
        })
    }
}

$composeServices = [System.Collections.Generic.List[object]]::new()
$composePath = Join-Path $repositoryRoot 'docker-compose.yml'
if (Test-Path -LiteralPath $composePath) {
    $compose = Get-Content -LiteralPath $composePath -Raw
    foreach ($match in [regex]::Matches($compose, '(?ms)^  (?<name>[a-zA-Z0-9_-]+):\r?\n(?<body>.*?)(?=^  [a-zA-Z0-9_-]+:|\z)')) {
        $body = $match.Groups['body'].Value
        $imageMatch = [regex]::Match($body, '(?m)^\s{4}image:\s*(?<value>.+)$')
        $dockerfileMatch = [regex]::Match($body, '(?m)^\s{6}dockerfile:\s*(?<value>.+)$')
        $dependencies = @(
            [regex]::Matches($body, '(?m)^\s{6}(?<name>[a-zA-Z0-9_-]+):\s*(?:\r?\n|$)') |
                ForEach-Object { $_.Groups['name'].Value } |
                Where-Object { $_ -notin @('condition', 'environment', 'healthcheck', 'profiles', 'build') } |
                Sort-Object { Get-LlmWikiOrdinalSortKey $_ } -Unique
        )
        $composeServices.Add([pscustomobject]@{
            name = $match.Groups['name'].Value
            image = if ($imageMatch.Success) { $imageMatch.Groups['value'].Value.Trim() } else { $null }
            dockerfile = if ($dockerfileMatch.Success) { $dockerfileMatch.Groups['value'].Value.Trim() } else { $null }
            dependsOn = $dependencies
        })
    }
}

$result = [ordered]@{
    schemaVersion = 1
    summary = [ordered]@{
        composeServices = $composeServices.Count
        hostedServices = @($hostedServices | Sort-Object { Get-LlmWikiOrdinalSortKey "$($_.name)`0$($_.path)" } -Unique).Count
        httpClients = @($httpClients | Sort-Object { Get-LlmWikiOrdinalSortKey "$($_.implementation)`0$($_.registrationPath)" } -Unique).Count
        webhooks = @($webhooks | Sort-Object { Get-LlmWikiOrdinalSortKey "$($_.name)`0$($_.path)" } -Unique).Count
        recurringJobRegistrations = $jobRegistrations.Count
    }
    composeServices = @($composeServices | Sort-Object { Get-LlmWikiOrdinalSortKey $_.name })
    hostedServices = @($hostedServices | Sort-Object { Get-LlmWikiOrdinalSortKey "$($_.name)`0$($_.path)" } -Unique)
    httpClients = @($httpClients | Sort-Object { Get-LlmWikiOrdinalSortKey "$($_.implementation)`0$($_.registrationPath)" } -Unique)
    webhooks = @($webhooks | Sort-Object { Get-LlmWikiOrdinalSortKey "$($_.name)`0$($_.path)" } -Unique)
    recurringJobRegistrations = @($jobRegistrations | Sort-Object { Get-LlmWikiOrdinalSortKey "$($_.path)`0$($_.detail)" })
}
$jsonText = ($result | ConvertTo-Json -Depth 10) + [Environment]::NewLine
if ($Check) {
    if (-not (Test-LlmWikiJsonEquivalent -ActualPath $outputPath -ExpectedJson $jsonText -Depth 10)) {
        Write-Host 'Runtime topology is stale. Run ./.llm-wiki/wiki.ps1 update.'
        exit 1
    }
    Write-Host "Runtime topology is current: $($result.summary.composeServices) services, $($result.summary.hostedServices) hosted workers, $($result.summary.httpClients) HTTP clients."
    exit 0
}
$utf8WithoutBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($outputPath, $jsonText, $utf8WithoutBom)
Write-Host "Generated .llm-wiki/generated/runtime-topology.json."
