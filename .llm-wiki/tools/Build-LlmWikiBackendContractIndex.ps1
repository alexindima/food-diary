[CmdletBinding()]
param(
    [switch]$Check,
    [switch]$ReuseUnchangedCheck,
    [switch]$RequireCompiledScanner,
    [switch]$DisableCompiledScanner
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiJson.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiIndexCache.ps1')
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$symbolIndex = Get-Content -LiteralPath (Join-Path $wikiRoot 'generated/csharp-symbol-index.json') -Raw | ConvertFrom-Json
$outputPath = Join-Path $wikiRoot 'generated/backend-contract-index.json'
$cachePath = Join-Path $repositoryRoot '.artifacts/llm-wiki/index-cache/backend-contract-index.json'
$cacheInputs = @(Invoke-LlmWikiGitPathList -RepositoryRoot $repositoryRoot -Arguments @('ls-files', '--cached', '--others', '--exclude-standard', '--', '*.cs') -FailureMessage 'Unable to enumerate backend-contract cache inputs.')
$cacheInputs = @($cacheInputs | Where-Object { $_ -notmatch '(^|[\/])\.llm-wiki[\/]tools[\/]' -and $_ -notmatch '[\/](node_modules|bin|obj|Migrations|\.artifacts|TestResults)[\/]' -and $_ -notmatch '\.(Designer|g)\.cs$' }) + @(
    '.llm-wiki/generated/csharp-symbol-index.json'
    '.llm-wiki/tools/Build-LlmWikiBackendContractIndex.ps1'
    '.llm-wiki/tools/Invoke-LlmWikiContractReferenceExtractor.ps1'
    '.llm-wiki/tools/contract-reference-extractor/LlmWiki.ContractReferenceExtractor.csproj'
    '.llm-wiki/tools/contract-reference-extractor/Program.cs'
    '.llm-wiki/tools/LlmWikiJson.ps1'
    '.llm-wiki/tools/LlmWikiIndexCache.ps1'
)
$inputFingerprint = Get-LlmWikiIndexInputFingerprint $repositoryRoot $cacheInputs
if ($ReuseUnchangedCheck -and (Test-LlmWikiIndexCache $cachePath $outputPath $inputFingerprint)) { Write-Host 'Backend contract index cache hit: inputs, generator, and output are unchanged.'; exit 0 }

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
function Get-CollisionClassification([string]$Name, [object[]]$Definitions) {
    $definitionPaths = @($Definitions.path | Sort-Object -Unique)
    if ($Definitions.Count -le 1) { return 'unique' }
    if ($definitionPaths.Count -eq 1) { return 'same-file-overload-or-factory' }
    if ($Name -eq 'InitializerCommand' -and @($definitionPaths | Where-Object { $_ -notmatch '(?:^|/)FoodDiary\.(?:MailInbox\.|MailRelay\.)?Initializer/InitializerCommand\.cs$' }).Count -eq 0) {
        return 'service-local-initializer-contract'
    }
    return 'ambiguous'
}
$contracts = @(
    $contractSymbols |
        Group-Object name |
        ForEach-Object {
            $definitions = @($_.Group)
            $collisionClassification = Get-CollisionClassification $_.Name $definitions
            [pscustomobject]@{
                name = $_.Name
                roles = @($definitions.role | Sort-Object -Unique)
                kinds = @($definitions.kind | Sort-Object -Unique)
                areas = @($definitions.path | ForEach-Object { Get-Area $_ } | Sort-Object -Unique)
                definitionPaths = @($definitions.path | Sort-Object -Unique)
                ambiguous = $collisionClassification -eq 'ambiguous'
                collisionClassification = $collisionClassification
            }
        } |
        Sort-Object name
)
$contractsByName = @{}
foreach ($contract in $contracts) { $contractsByName[$contract.name] = $contract }

$consumerEdges = [System.Collections.Generic.List[object]]::new()
$sourcePaths = @(
    $cacheInputs |
        Where-Object {
            $_ -match '\.cs$' -and
            (Test-Path -LiteralPath (Join-Path $repositoryRoot $_) -PathType Leaf)
        } |
        Sort-Object -Unique
)

function Add-ConsumerEdges([string]$Path, [object[]]$Reference) {
    foreach ($item in $Reference) {
        $contract = $contractsByName[[string]$item.name]
        if ($path -in @($contract.definitionPaths)) { continue }
        $consumerEdges.Add([pscustomobject]@{
            contract = $contract.name
            roles = $contract.roles
            definitionPaths = $contract.definitionPaths
            consumerArea = Get-Area $Path
            consumerPath = $Path
            isTest = $Path -match '(^|/)(tests|[^/]+\.Tests)/' -or $Path -match 'Tests?\.cs$'
            referenceCount = [int]$item.count
        })
    }
}

$scannerMode = 'powershell-regex-fallback'
$compiledIndex = $null
if (-not $DisableCompiledScanner) {
    try {
        $compiledIndex = & (Join-Path $PSScriptRoot 'Invoke-LlmWikiContractReferenceExtractor.ps1') `
            -RepositoryRoot $repositoryRoot `
            -Path $sourcePaths `
            -Contract $contracts `
            -BuildBackendIndex
        $scannerMode = 'compiled-aho-corasick'
    } catch {
        if ($RequireCompiledScanner) { throw }
        Write-Warning "Compiled contract-reference scanner failed; using the exact PowerShell regex fallback. $($_.Exception.Message)"
    }
}

if ($null -ne $compiledIndex) {
    $jsonText = [string]$compiledIndex.indexJson
    $contractCount = [int]$compiledIndex.contracts
    $consumerEdgeCount = [int]$compiledIndex.consumerEdges
} else {
    $nameAlternation = @($contracts.name | Sort-Object Length -Descending | ForEach-Object { [regex]::Escape($_) }) -join '|'
    $contractReferenceRegex = [regex]::new(
        "(?<![A-Za-z0-9_])(?<name>$nameAlternation)(?![A-Za-z0-9_])",
        [Text.RegularExpressions.RegexOptions]::CultureInvariant,
        [TimeSpan]::FromSeconds(10))
    foreach ($path in $sourcePaths) {
        $content = [System.IO.File]::ReadAllText([IO.Path]::Combine($repositoryRoot, $path))
        $references = @(
            $contractReferenceRegex.Matches($content) |
                Group-Object { $_.Groups['name'].Value } |
                ForEach-Object { [pscustomobject]@{ name = $_.Name; count = $_.Count } }
        )
        Add-ConsumerEdges $path $references
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
    $jsonText = ConvertTo-LlmWikiCanonicalJson $result -Depth 10
    $contractCount = $result.summary.contracts
    $consumerEdgeCount = $result.summary.consumerEdges
}
if ($Check) {
    if (-not (Test-LlmWikiTextEquivalent -ActualPath $outputPath -ExpectedText $jsonText) -and
        -not (Test-LlmWikiJsonEquivalent -ActualPath $outputPath -ExpectedJson $jsonText -Depth 10)) {
        Write-Host 'Backend contract index is stale. Run ./.llm-wiki/wiki.ps1 update.'
        exit 1
    }
    Write-LlmWikiIndexCache $cachePath $outputPath $inputFingerprint
    Write-Host "Backend contract index is current: $contractCount contracts, $consumerEdgeCount consumer edges; scanner=$scannerMode."
    exit 0
}
$utf8WithoutBom = New-Object System.Text.UTF8Encoding($false)
if (Test-LlmWikiTextEquivalent -ActualPath $outputPath -ExpectedText $jsonText) {
    Write-LlmWikiIndexCache $cachePath $outputPath $inputFingerprint
    Write-Host 'Backend contract index unchanged; preserved byte-identical output.'
    exit 0
}
[System.IO.File]::WriteAllText($outputPath, $jsonText, $utf8WithoutBom)
Write-LlmWikiIndexCache $cachePath $outputPath $inputFingerprint
Write-Host 'Generated .llm-wiki/generated/backend-contract-index.json.'
