[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Contract,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text',
    [switch]$IncludeTests
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$escapedContract = [regex]::Escape($Contract)
$searchMatches = @(& git -C $repositoryRoot grep -n -E "\b$escapedContract\b" -- '*.cs')
if ($LASTEXITCODE -notin @(0, 1)) { throw "Unable to search consumers of $Contract." }

$declaration = @($searchMatches | Where-Object { $_ -match "\b(interface|class|record)\s+$escapedContract\b" } | Select-Object -First 1)
$declarationPath = if ($declaration) { ($declaration -split ':', 3)[0].Replace('\', '/') } else { $null }
$owningAssembly = if ($declarationPath) { ($declarationPath -split '/')[0] } else { 'not-found' }
$declarationText = if ($declarationPath) { [IO.File]::ReadAllText((Join-Path $repositoryRoot $declarationPath)) } else { '' }
$methodDefinitions = @([regex]::Matches($declarationText, '(?m)^\s*(?<return>[^\r\n;()]+?)\s+(?<method>[A-Za-z_]\w*)\s*\(') | ForEach-Object {
    [pscustomobject]@{ name = $_.Groups['method'].Value; returns = $_.Groups['return'].Value.Trim() }
})

function Get-ModuleName([string]$Path) {
    if ($Path -match '^FoodDiary\.Application/([^/]+)/') { return $Matches[1] }
    if ($Path -match '^([^/]+)/') { return $Matches[1] }
    return 'root'
}
function Get-Replacement([string]$Module, [string[]]$Methods) {
    if ('UpdateUserAsync' -in $Methods) { return "$Module-specific user mutation capability (do not expose the User aggregate)" }
    return "$Module-specific profile/read projection containing only consumed fields"
}

$consumerPaths = @($searchMatches | ForEach-Object { ($_ -split ':', 3)[0].Replace('\', '/') } | Where-Object {
    $_ -ne $declarationPath -and ($IncludeTests -or $_ -notmatch '(^|/)tests?/|\.Tests?/')
} | Sort-Object -Unique)
$consumers = foreach ($path in $consumerPaths) {
    $text = [IO.File]::ReadAllText((Join-Path $repositoryRoot $path))
    $variableNames = @([regex]::Matches($text, "\b$escapedContract\s+(?<name>[A-Za-z_]\w*)") | ForEach-Object { $_.Groups['name'].Value } | Sort-Object -Unique)
    $methods = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($variable in $variableNames) {
        foreach ($methodMatch in [regex]::Matches($text, "\b$([regex]::Escape($variable))\.(?<method>[A-Za-z_]\w*)\s*\(")) {
            [void]$methods.Add($methodMatch.Groups['method'].Value)
        }
    }
    $methodList = @($methods | Sort-Object)
    $returnedData = @($methodDefinitions | Where-Object name -in $methodList | ForEach-Object returns | Sort-Object -Unique)
    $module = Get-ModuleName $path
    $usesAggregate = $returnedData -match '(^|[<, ])User([>, ]|$)' -or $text -match '\bResult<User>\b'
    $mutation = 'UpdateUserAsync' -in $methodList
    $composition = $path -match 'DependencyInjection|Initializer|Program\.cs$'
    [pscustomobject][ordered]@{
        consumer = $module
        path = $path
        methods = $methodList
        returnedData = $returnedData
        owningAssembly = $owningAssembly
        access = if ($mutation) { 'mutation' } elseif ($usesAggregate) { 'aggregate-read' } else { 'narrow-read-or-access' }
        extractionSafe = $owningAssembly -match '\.Abstractions$' -and -not $usesAggregate -and -not $mutation
        replacementContract = Get-Replacement $module $methodList
        compositionRegistration = $composition
    }
}

$result = [pscustomobject][ordered]@{
    schemaVersion = 1
    contract = $Contract
    declarationPath = $declarationPath
    owningAssembly = $owningAssembly
    methods = $methodDefinitions
    consumers = @($consumers)
    readiness = [pscustomobject][ordered]@{
        productionConsumers = @($consumers).Count
        businessConsumers = @($consumers | Where-Object { -not $_.compositionRegistration -and @($_.methods).Count -gt 0 }).Count
        compositionRegistrations = @($consumers | Where-Object compositionRegistration).Count
        internalOwnerConsumers = @($consumers | Where-Object { $_.consumer -eq 'Users' -and -not $_.compositionRegistration }).Count
        externalModuleConsumers = @($consumers | Where-Object { $_.consumer -ne 'Users' -and -not $_.compositionRegistration -and @($_.methods).Count -gt 0 }).Count
        emptyReferenceMatches = @($consumers | Where-Object { -not $_.compositionRegistration -and @($_.methods).Count -eq 0 }).Count
        abstractionOwned = $owningAssembly -match '\.Abstractions$'
        aggregateConsumers = @($consumers | Where-Object access -eq 'aggregate-read').Count
        mutationConsumers = @($consumers | Where-Object access -eq 'mutation').Count
        extractionSafeConsumers = @($consumers | Where-Object extractionSafe).Count
        blockers = @(
            if ($owningAssembly -notmatch '\.Abstractions$') { "Contract is owned by implementation assembly $owningAssembly." }
            if (@($consumers | Where-Object { $_.access -in @('aggregate-read', 'mutation') }).Count) { 'Consumers depend on the User aggregate or aggregate mutation.' }
        )
    }
}

if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 8; exit 0 }
Write-Host "Contract extraction readiness: $Contract"
Write-Host "Declaration: $declarationPath ($owningAssembly)"
Write-Host "Consumers: $($result.readiness.productionConsumers); business=$($result.readiness.businessConsumers); external=$($result.readiness.externalModuleConsumers); owner-internal=$($result.readiness.internalOwnerConsumers); composition=$($result.readiness.compositionRegistrations); empty=$($result.readiness.emptyReferenceMatches)"
Write-Host "Extraction blockers: aggregate=$($result.readiness.aggregateConsumers); mutation=$($result.readiness.mutationConsumers); extraction-safe=$($result.readiness.extractionSafeConsumers)"
foreach ($blocker in $result.readiness.blockers) { Write-Host "BLOCKER: $blocker" }
foreach ($consumer in $result.consumers) {
    Write-Host "- $($consumer.consumer): $($consumer.methods -join ', ') -> $($consumer.returnedData -join ', ') [$($consumer.access)]"
    Write-Host "  $($consumer.path)"
    Write-Host "  Replace with: $($consumer.replacementContract)"
}
