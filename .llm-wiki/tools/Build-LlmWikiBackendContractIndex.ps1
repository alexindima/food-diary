[CmdletBinding()]
param([switch]$Check, [switch]$ReuseUnchangedCheck)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiJson.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiIndexCache.ps1')
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$symbolIndex = Get-Content -LiteralPath (Join-Path $wikiRoot 'generated/csharp-symbol-index.json') -Raw | ConvertFrom-Json
$outputPath = Join-Path $wikiRoot 'generated/backend-contract-index.json'
$cachePath = Join-Path $repositoryRoot '.artifacts/llm-wiki/index-cache/backend-contract-index.json'
$cacheInputs = @(& git -C $repositoryRoot ls-files --cached --others --exclude-standard -- '*.cs')
if ($LASTEXITCODE -ne 0) { throw 'Unable to enumerate backend-contract cache inputs.' }
$cacheInputs = @($cacheInputs | Where-Object { $_ -notmatch '[\/](node_modules|bin|obj|Migrations|\.artifacts|TestResults)[\/]' -and $_ -notmatch '\.(Designer|g)\.cs$' }) + @('.llm-wiki/generated/csharp-symbol-index.json', '.llm-wiki/tools/Build-LlmWikiBackendContractIndex.ps1', '.llm-wiki/tools/LlmWikiJson.ps1', '.llm-wiki/tools/LlmWikiIndexCache.ps1')
$inputFingerprint = Get-LlmWikiIndexInputFingerprint $repositoryRoot $cacheInputs
if ($ReuseUnchangedCheck -and (Test-LlmWikiIndexCache $cachePath $outputPath $inputFingerprint)) { Write-Host 'Backend contract index cache hit: inputs, generator, and output are unchanged.'; exit 0 }

function ConvertTo-RepositoryPath([string]$Path) {
    [System.IO.Path]::GetFullPath($Path).Substring($repositoryRoot.Length + 1).Replace('\', '/')
}

function Get-Area([string]$Path) {
    if ($Path -match '^MailInbox/') { return 'MailInbox' }
    if ($Path -match '^MailRelay/') { return 'MailRelay' }
    if ($Path -match '^Shared/') { return 'Shared' }
    return 'FoodDiary'
}

$contractSymbols = @(
    $symbolIndex.symbols |
        Where-Object {
            $_.role -in @('Command', 'Query', 'Event', 'Request', 'Response') -or
            $_.kind -eq 'interface'
        } |
        Sort-Object name, path
)
$contracts = @(
    $contractSymbols |
        Group-Object name |
        ForEach-Object {
            $definitions = @($_.Group)
            [pscustomobject]@{
                name = $_.Name
                roles = @($definitions.role | Sort-Object -Unique)
                kinds = @($definitions.kind | Sort-Object -Unique)
                areas = @($definitions.path | ForEach-Object { Get-Area $_ } | Sort-Object -Unique)
                definitionPaths = @($definitions.path | Sort-Object -Unique)
                ambiguous = $definitions.Count -gt 1
            }
        } |
        Sort-Object name
)
$contractsByName = @{}
foreach ($contract in $contracts) { $contractsByName[$contract.name] = $contract }
$nameAlternation = @($contracts.name | Sort-Object Length -Descending | ForEach-Object { [regex]::Escape($_) }) -join '|'

$consumerEdges = [System.Collections.Generic.List[object]]::new()
$sourceFiles = @(
    Get-ChildItem -LiteralPath $repositoryRoot -Recurse -File -Filter '*.cs' |
        Where-Object {
            $_.FullName -notmatch '[\\/](node_modules|bin|obj|Migrations|\.artifacts|TestResults)[\\/]' -and
            $_.Name -notmatch '\.(Designer|g)\.cs$'
        } |
        Sort-Object FullName
)
foreach ($file in $sourceFiles) {
    $content = [System.IO.File]::ReadAllText($file.FullName)
    $path = ConvertTo-RepositoryPath $file.FullName
    $matchesByName = @(
        [regex]::Matches($content, "(?<![A-Za-z0-9_])(?<name>$nameAlternation)(?![A-Za-z0-9_])") |
            Group-Object { $_.Groups['name'].Value }
    )
    foreach ($matchGroup in $matchesByName) {
        $contract = $contractsByName[$matchGroup.Name]
        if ($path -in @($contract.definitionPaths)) { continue }
        $consumerEdges.Add([pscustomobject]@{
            contract = $contract.name
            roles = $contract.roles
            definitionPaths = $contract.definitionPaths
            consumerArea = Get-Area $path
            consumerPath = $path
            isTest = $path -match '(^|/)(tests|[^/]+\.Tests)/' -or $path -match 'Tests?\.cs$'
            referenceCount = $matchGroup.Count
        })
    }
}

$result = [ordered]@{
    schemaVersion = 1
    summary = [ordered]@{
        contracts = $contracts.Count
        ambiguousContracts = @($contracts | Where-Object ambiguous).Count
        consumerEdges = $consumerEdges.Count
        productionConsumerEdges = @($consumerEdges | Where-Object { -not $_.isTest }).Count
        testConsumerEdges = @($consumerEdges | Where-Object isTest).Count
        consumedContracts = @($consumerEdges.contract | Sort-Object -Unique).Count
        unconsumedContracts = @($contracts | Where-Object { $_.name -notin @($consumerEdges.contract) }).Count
    }
    contracts = $contracts
    consumerEdges = @($consumerEdges | Sort-Object contract, isTest, consumerPath)
}
$jsonText = ($result | ConvertTo-Json -Depth 10) + [Environment]::NewLine
if ($Check) {
    if (-not (Test-LlmWikiJsonEquivalent -ActualPath $outputPath -ExpectedJson $jsonText -Depth 10)) {
        Write-Host 'Backend contract index is stale. Run ./.llm-wiki/wiki.ps1 update.'
        exit 1
    }
    Write-Host "Backend contract index is current: $($result.summary.contracts) contracts, $($result.summary.consumerEdges) consumer edges."
    exit 0
}
$utf8WithoutBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($outputPath, $jsonText, $utf8WithoutBom)
Write-LlmWikiIndexCache $cachePath $outputPath $inputFingerprint
Write-Host 'Generated .llm-wiki/generated/backend-contract-index.json.'
