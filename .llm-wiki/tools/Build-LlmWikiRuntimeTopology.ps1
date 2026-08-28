[CmdletBinding()]
param([switch]$Check)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiJson.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiRuntimeTopologyFingerprint.ps1')
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$outputPath = Join-Path $wikiRoot 'generated/runtime-topology.json'

function ConvertTo-RepositoryPath {
    param([string]$Path)
    return [System.IO.Path]::GetFullPath($Path).Substring($repositoryRoot.Length + 1).Replace('\', '/')
}

function Get-ComposePropertyBlock {
    param([string]$Body, [string]$Name)
    $escapedName = [regex]::Escape($Name)
    $match = [regex]::Match($Body, "(?ms)^    ${escapedName}:\s*\r?\n(?<block>.*?)(?=^    [a-zA-Z0-9_-]+:\s*|\z)")
    return $(if ($match.Success) { $match.Groups['block'].Value } else { '' })
}

function Get-ComposeListValues {
    param([string]$Block)
    return @(
        [regex]::Matches($Block, '(?m)^\s{6}-\s*["'']?(?<value>[^\r\n"'']+)["'']?\s*$') |
            ForEach-Object { $_.Groups['value'].Value.Trim() }
    )
}

function Get-BehaviorSignals {
    param([string]$Content)
    return @(
        if ($Content -match '(?i)retry|resilien(?:ce|cy)|Polly|AutomaticRetry') { 'retry-or-resilience' }
        if ($Content -match '(?i)timeout|TimeSpan\.From(?:Milliseconds|Seconds|Minutes)') { 'timeout-or-delay' }
        if ($Content -match '(?i)CancellationToken\.None') { 'cancellation-suppressed' }
        if ($Content -match '(?i)\b(?:cancellationToken|stoppingToken)\b' -and $Content -notmatch '(?i)CancellationToken\.None') { 'cancellation-propagation-candidate' }
        if ($Content -match '(?i)idempoten') { 'idempotency' }
        if ($Content -match '(?i)deduplic|duplicate|replay|alreadyprocessed') { 'duplicate-or-replay-control' }
        if ($Content -match '(?i)outbox') { 'outbox-delivery' }
        if ($Content -match '(?i)concurren|SemaphoreSlim|Interlocked|lock\s*\(') { 'concurrency-control' }
    ) | Sort-Object -Unique
}

function Get-SourceWindow {
    param([string]$Content, [int]$Index, [int]$Length, [int]$Radius = 1600)
    $start = [Math]::Max(0, $Index - $Radius)
    $end = [Math]::Min($Content.Length, $Index + $Length + $Radius)
    return $Content.Substring($start, $end - $start)
}

$sourceFiles = @(
    Get-ChildItem -LiteralPath $repositoryRoot -Recurse -File -Filter '*.cs' |
        Where-Object {
            $_.FullName -notmatch '[\\/](tests|obj|bin|\.artifacts|\.llm-wiki|TestResults|Migrations)[\\/]' -and
            $_.Name -notmatch '\.(Designer|g)\.cs$'
        } |
        Sort-Object { Get-LlmWikiOrdinalSortKey $_.FullName }
)
$hostedServices = [System.Collections.Generic.List[object]]::new()
$httpClients = [System.Collections.Generic.List[object]]::new()
$webhooks = [System.Collections.Generic.List[object]]::new()
$jobRegistrations = [System.Collections.Generic.List[object]]::new()
$networkPolicies = [System.Collections.Generic.List[object]]::new()

foreach ($file in $sourceFiles) {
    $content = [System.IO.File]::ReadAllText($file.FullName)
    $path = ConvertTo-RepositoryPath $file.FullName
    foreach ($match in [regex]::Matches(
        $content,
        '(?ms)(?:class|sealed\s+class)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*).{0,1500}?\)\s*:\s*(?<base>BackgroundService|IHostedService)\b|(?:class|sealed\s+class)\s+(?<name2>[A-Za-z_][A-Za-z0-9_]*)\s*:\s*(?<base2>BackgroundService|IHostedService)\b')) {
        $serviceName = if ($match.Groups['name'].Success) { $match.Groups['name'].Value } else { $match.Groups['name2'].Value }
        $baseType = if ($match.Groups['base'].Success) { $match.Groups['base'].Value } else { $match.Groups['base2'].Value }
        $behaviorSignals = @(Get-BehaviorSignals -Content (Get-SourceWindow -Content $content -Index $match.Index -Length $match.Length))
        $hostedServices.Add([pscustomobject]@{
            name = $serviceName
            baseType = $baseType
            path = $path
            behaviorSignals = $behaviorSignals
            behaviorSignalScope = 'class-window'
        })
    }
    foreach ($match in [regex]::Matches(
        $content,
        'AddHttpClient\s*<\s*(?<contract>[A-Za-z_][A-Za-z0-9_]*)\s*,\s*(?<implementation>[A-Za-z_][A-Za-z0-9_]*)')) {
        $behaviorSignals = @(Get-BehaviorSignals -Content (Get-SourceWindow -Content $content -Index $match.Index -Length $match.Length -Radius 500))
        $httpClients.Add([pscustomobject]@{
            contract = $match.Groups['contract'].Value
            implementation = $match.Groups['implementation'].Value
            registrationPath = $path
            behaviorSignals = $behaviorSignals
            behaviorSignalScope = 'registration-window'
        })
    }
    foreach ($match in [regex]::Matches(
        $content,
        '(?m)(?:class|sealed\s+class)\s+(?<implementation>[A-Za-z_][A-Za-z0-9_]*(?:Client|Gateway|Provider|Transport))\s*\([^)]*\bHttpClient\s+')) {
        $implementation = $match.Groups['implementation'].Value
        if (@($httpClients | Where-Object implementation -eq $implementation).Count -eq 0) {
            $behaviorSignals = @(Get-BehaviorSignals -Content (Get-SourceWindow -Content $content -Index $match.Index -Length $match.Length))
            $httpClients.Add([pscustomobject]@{
                contract = $null
                implementation = $implementation
                registrationPath = $path
                behaviorSignals = $behaviorSignals
                behaviorSignalScope = 'class-window'
            })
        }
    }
    if ($path -match 'Webhook' -or $content -match '\bWebhook(?:Controller|HttpRequest|Authorizer)\b') {
        foreach ($match in [regex]::Matches(
            $content,
            '(?m)(?:class|sealed\s+class)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*(?:Webhook|WebhookController|WebhookAuthorizer)[A-Za-z0-9_]*)')) {
            $classWindow = Get-SourceWindow -Content $content -Index $match.Index -Length $match.Length
            $signals = @(
                if ($classWindow -match '(?i)HMAC|signature|CryptographicOperations') { 'signature-validation' }
                if ($classWindow -match '(?i)timestamp|tolerance|freshness') { 'freshness-validation' }
                if ($classWindow -match '(?i)ApiKey|Authorization') { 'request-authentication' }
                if ($classWindow -match '(?i)idempoten|deduplicat|alreadyprocessed|unique') { 'replay-or-duplicate-control' }
            ) | Sort-Object -Unique
            $webhooks.Add([pscustomobject]@{
                name = $match.Groups['name'].Value
                path = $path
                securitySignals = @($signals)
                behaviorSignals = @(
                    @(Get-BehaviorSignals -Content $classWindow)
                    if ('replay-or-duplicate-control' -in $signals) { 'idempotency-review-candidate' }
                ) | Sort-Object -Unique
                behaviorSignalScope = 'class-window'
                evidenceStatus = 'inferred-from-code'
            })
        }
    }
    if ($content -match '\b(HttpClient|HttpMessageHandler|SocketsHttpHandler|ConnectCallback|SendAsync)\b' -and
        $content -match '(?i)(ConnectCallback|AllowAutoRedirect|UseProxy|Dns\.|GetHostAddresses|IPAddress|CheckHostName|MaxResponseContentBufferSize|Timeout|private|loopback|linklocal)') {
        $signals = @(
            if ($content -match '(?i)ConnectCallback') { 'connect-time-address-control' }
            if ($content -match '(?i)AllowAutoRedirect\s*=\s*false') { 'redirects-disabled' }
            if ($content -match '(?i)UseProxy\s*=\s*false') { 'proxy-disabled' }
            if ($content -match '(?i)Dns\.|GetHostAddresses') { 'dns-resolution' }
            if ($content -match '(?i)IPAddress|loopback|private|linklocal') { 'ip-address-policy' }
            if ($content -match '(?i)Timeout') { 'timeout-policy' }
            if ($content -match '(?i)MaxResponseContentBufferSize|maximum.*(?:bytes|size)|bounded') { 'response-size-policy' }
            if ($content -match '(?i)https|CheckHostName|Uri') { 'uri-policy' }
        ) | Sort-Object -Unique
        $networkPolicies.Add([pscustomobject]@{
            name = [IO.Path]::GetFileNameWithoutExtension($path)
            path = $path
            securitySignals = @($signals)
            behaviorSignals = @(Get-BehaviorSignals -Content $content)
            behaviorSignalScope = 'policy-file'
            evidenceStatus = 'inferred-from-code'
            runtimeEvidenceRequired = @('effective DNS result at connection time', 'effective proxy and redirect behavior')
        })
    }
    foreach ($match in [regex]::Matches(
        $content,
        '(?ms)(?<api>(?:recurringJobManager|RecurringJob)\.(?:AddOrUpdate|RemoveIfExists))\s*\(\s*(?<detail>[^,\r\n\)]+)')) {
        $jobRegistrations.Add([pscustomobject]@{
            api = $match.Groups['api'].Value
            detail = $match.Groups['detail'].Value.Trim()
            path = $path
            behaviorSignals = @(Get-BehaviorSignals -Content $match.Value)
            behaviorSignalScope = 'registration-expression'
            targetBehaviorEvidence = 'not-expanded; inspect the registered job implementation before making retry, cancellation, or idempotency claims'
        })
    }
}

$composeServices = [System.Collections.Generic.List[object]]::new()
$composePath = Join-Path $repositoryRoot 'docker-compose.yml'
if (Test-Path -LiteralPath $composePath) {
    $compose = Get-Content -LiteralPath $composePath -Raw
    $servicesMatch = [regex]::Match($compose, '(?ms)^services:\s*\r?\n(?<body>.*?)(?=^[a-zA-Z0-9_-]+:\s*(?:\r?\n|$)|\z)')
    $servicesBody = if ($servicesMatch.Success) { $servicesMatch.Groups['body'].Value } else { '' }
    foreach ($match in [regex]::Matches($servicesBody, '(?ms)^  (?<name>[a-zA-Z0-9_-]+):\r?\n(?<body>.*?)(?=^  [a-zA-Z0-9_-]+:|\z)')) {
        $body = $match.Groups['body'].Value
        $imageMatch = [regex]::Match($body, '(?m)^\s{4}image:\s*(?<value>.+)$')
        $dockerfileMatch = [regex]::Match($body, '(?m)^\s{6}dockerfile:\s*(?<value>.+)$')
        $dependsOnBlock = Get-ComposePropertyBlock -Body $body -Name 'depends_on'
        $environmentBlock = Get-ComposePropertyBlock -Body $body -Name 'environment'
        $portsBlock = Get-ComposePropertyBlock -Body $body -Name 'ports'
        $profilesBlock = Get-ComposePropertyBlock -Body $body -Name 'profiles'
        $networksBlock = Get-ComposePropertyBlock -Body $body -Name 'networks'
        $volumesBlock = Get-ComposePropertyBlock -Body $body -Name 'volumes'
        $dependencies = @(
            [regex]::Matches($dependsOnBlock, '(?m)^\s{6}(?<name>[a-zA-Z0-9_-]+):\s*(?:\r?\n|$)') |
                ForEach-Object { $_.Groups['name'].Value } |
                Sort-Object { Get-LlmWikiOrdinalSortKey $_ } -Unique
        )
        $environmentKeys = @(
            @([regex]::Matches($environmentBlock, '(?m)^\s{6}(?<name>[A-Za-z_][A-Za-z0-9_]*):') | ForEach-Object { $_.Groups['name'].Value }) +
            @([regex]::Matches($environmentBlock, '(?m)^\s{6}-\s*(?<name>[A-Za-z_][A-Za-z0-9_]*)=') | ForEach-Object { $_.Groups['name'].Value }) |
                Sort-Object { Get-LlmWikiOrdinalSortKey $_ } -Unique
        )
        $networkNames = @(
            [regex]::Matches($networksBlock, '(?m)^\s{6}(?<name>[a-zA-Z0-9_-]+):\s*(?:\r?\n|$)') |
                ForEach-Object { $_.Groups['name'].Value } |
                Sort-Object { Get-LlmWikiOrdinalSortKey $_ } -Unique
        )
        $composeServices.Add([pscustomobject]@{
            name = $match.Groups['name'].Value
            image = if ($imageMatch.Success) { $imageMatch.Groups['value'].Value.Trim() } else { $null }
            dockerfile = if ($dockerfileMatch.Success) { $dockerfileMatch.Groups['value'].Value.Trim() } else { $null }
            dependsOn = $dependencies
            ports = @(Get-ComposeListValues $portsBlock)
            profiles = @(Get-ComposeListValues $profilesBlock)
            networks = $networkNames
            environmentKeys = $environmentKeys
            volumeMounts = @(Get-ComposeListValues $volumesBlock)
            readOnlyRootFilesystem = [bool]($body -match '(?m)^    read_only:\s*true\s*$')
            dropsAllCapabilities = [bool]($body -match '(?ms)^    cap_drop:\s*\r?\n(?:\s{6}-\s*)?ALL\s*$')
            noNewPrivileges = [bool]($body -match '(?m)^\s{6}-\s*no-new-privileges:true\s*$')
            evidenceStatus = 'declared-in-repository'
        })
    }
}

$result = [ordered]@{
    schemaVersion = 1
    freshness = Get-LlmWikiRuntimeTopologyFingerprint -RepositoryRoot $repositoryRoot
    summary = [ordered]@{
        composeServices = $composeServices.Count
        hostedServices = @($hostedServices | Sort-Object { Get-LlmWikiOrdinalSortKey "$($_.name)`0$($_.path)" } -Unique).Count
        httpClients = @($httpClients | Sort-Object { Get-LlmWikiOrdinalSortKey "$($_.implementation)`0$($_.registrationPath)" } -Unique).Count
        webhooks = @($webhooks | Sort-Object { Get-LlmWikiOrdinalSortKey "$($_.name)`0$($_.path)" } -Unique).Count
        recurringJobRegistrations = $jobRegistrations.Count
        networkPolicies = @($networkPolicies | Sort-Object { Get-LlmWikiOrdinalSortKey "$($_.name)`0$($_.path)" } -Unique).Count
    }
    semantics = [ordered]@{
        declared = 'Compose fields describe repository declarations, not effective production exposure or IAM.'
        inferred = 'Code security signals are navigation evidence and require source and runtime validation.'
        externalEvidence = @('effective firewall and reverse-proxy exposure', 'cloud IAM and database grants', 'runtime DNS, redirect, proxy, and certificate behavior')
    }
    composeServices = @($composeServices | Sort-Object { Get-LlmWikiOrdinalSortKey $_.name })
    hostedServices = @($hostedServices | Sort-Object { Get-LlmWikiOrdinalSortKey "$($_.name)`0$($_.path)" } -Unique)
    httpClients = @($httpClients | Sort-Object { Get-LlmWikiOrdinalSortKey "$($_.implementation)`0$($_.registrationPath)" } -Unique)
    webhooks = @($webhooks | Sort-Object { Get-LlmWikiOrdinalSortKey "$($_.name)`0$($_.path)" } -Unique)
    recurringJobRegistrations = @($jobRegistrations | Sort-Object { Get-LlmWikiOrdinalSortKey "$($_.path)`0$($_.detail)" })
    networkPolicies = @($networkPolicies | Sort-Object { Get-LlmWikiOrdinalSortKey "$($_.name)`0$($_.path)" } -Unique)
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
