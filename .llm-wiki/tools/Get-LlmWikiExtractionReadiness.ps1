[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Module,
    [ValidateSet('Text', 'Json')][string]$Format = 'Text',
    [switch]$IncludeTests,
    [switch]$CompileProbe,
    [string[]]$DependencyFixturePath
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
. (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1')
$folderModulePath = Join-Path $repositoryRoot "FoodDiary.Application/$Module"
$extractedModulePath = Join-Path $repositoryRoot "FoodDiary.Application.$Module"
if (-not (Test-Path -LiteralPath $folderModulePath -PathType Container) -and
    -not (Test-Path -LiteralPath $extractedModulePath -PathType Container)) {
    throw "Application module not found: $Module"
}
$aggregateName = if ($Module -eq 'Users') { 'User' } else { $Module.TrimEnd('s') }
$sourcePaths = @(Invoke-LlmWikiGitPathList -RepositoryRoot $repositoryRoot -Arguments @('ls-files', '--cached', '--others', '--exclude-standard', '--', '*.cs') -FailureMessage 'Unable to enumerate C# sources for extraction readiness.')
$sourcePaths = @($sourcePaths |
    Where-Object { Test-Path -LiteralPath (Join-Path $repositoryRoot $_) -PathType Leaf } |
    Where-Object { $IncludeTests -or $_ -notmatch '(^|/)tests?/|\.Tests?/' } |
    Sort-Object -Unique)
$moduleSourcePrefixes = @("FoodDiary.Application/$Module/", "FoodDiary.Application.$Module/")
$moduleSourcePaths = @($sourcePaths | Where-Object {
    $candidate = $_
    @($moduleSourcePrefixes | Where-Object { $candidate.StartsWith($_, [StringComparison]::OrdinalIgnoreCase) }).Count -gt 0
})
$moduleSourcePaths += @($DependencyFixturePath | Where-Object { $_ } | ForEach-Object { ConvertTo-LlmWikiRepositoryPath $_ })
$moduleSourcePaths = @($moduleSourcePaths | Sort-Object -Unique)

$dependencyConfigPath = Join-Path $repositoryRoot 'docs/architecture/module-dependencies.json'
$dependencyConfig = if (Test-Path -LiteralPath $dependencyConfigPath -PathType Leaf) {
    Get-Content -LiteralPath $dependencyConfigPath -Raw | ConvertFrom-Json
} else { $null }
$declaredDependencies = @(
    if ($null -ne $dependencyConfig -and $dependencyConfig.modules.PSObject.Properties[$Module]) {
        @($dependencyConfig.modules.$Module)
    }
)
$internalFeatureNamespaces = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($path in $moduleSourcePaths) {
    $text = [IO.File]::ReadAllText((Join-Path $repositoryRoot $path))
    $scanText = [regex]::Replace($text, '(?s)"(?:\\.|[^"\\])*"|//[^\r\n]*|/\*.*?\*/', { param($match) ' ' * $match.Length })
    foreach ($match in [regex]::Matches($scanText, '(?m)^\s*namespace\s+FoodDiary\.Application\.(?<feature>[A-Z][A-Za-z0-9_]+)(?:\.|\s*[;{])')) {
        $feature = $match.Groups['feature'].Value
        if ($feature -ne 'Abstractions') { $null = $internalFeatureNamespaces.Add($feature) }
    }
}
$sourceDependencies = [Collections.Generic.List[object]]::new()
foreach ($path in $moduleSourcePaths) {
    $text = [IO.File]::ReadAllText((Join-Path $repositoryRoot $path))
    $scanText = [regex]::Replace($text, '(?s)"(?:\\.|[^"\\])*"|//[^\r\n]*|/\*.*?\*/', { param($match) ' ' * $match.Length })
    foreach ($match in [regex]::Matches($scanText, '\bFoodDiary\.Application\.(?<module>[A-Z][A-Za-z0-9_]+)(?:\.[A-Za-z0-9_]+)*')) {
        $dependencyModule = $match.Groups['module'].Value
        if ($dependencyModule -in @($Module, 'Abstractions') -or $internalFeatureNamespaces.Contains($dependencyModule)) { continue }
        $line = 1 + ($text.Substring(0, $match.Index) -split "`n").Count - 1
        $kind = if ($text.Substring([Math]::Max(0, $match.Index - [Math]::Min(20, $match.Index)), [Math]::Min($match.Length + [Math]::Min(20, $match.Index), $text.Length - [Math]::Max(0, $match.Index - [Math]::Min(20, $match.Index)))) -match '(?i)using\s+static') {
            'static-helper'
        } elseif ($text -match "(?m)^\s*using\s+FoodDiary\.Application\.$([regex]::Escape($dependencyModule))") {
            'namespace-import'
        } else { 'public-type-reference' }
        $sourceDependencies.Add([pscustomobject]@{ module = $dependencyModule; path = $path; line = $line; kind = $kind; reference = $match.Value })
    }
}
$sourceDependencies = @($sourceDependencies | Sort-Object module, path, line, kind -Unique)
$actualDependencies = @($sourceDependencies | ForEach-Object { [string]$_.module } | Where-Object { $_ } | Sort-Object -Unique)
$undeclaredDependencies = @($actualDependencies | Where-Object { $_ -notin $declaredDependencies })
$staleDeclaredDependencies = @($declaredDependencies | Where-Object { $_ -notin $actualDependencies })
$projectDependencies = @($actualDependencies | Where-Object { Test-Path -LiteralPath (Join-Path $repositoryRoot "FoodDiary.Application.$_/FoodDiary.Application.$_.csproj") -PathType Leaf })
$unresolvedCoreDependencies = @($actualDependencies | Where-Object { $_ -notin $projectDependencies })
$diRegistrations = @(
    $sourcePaths |
        Where-Object { $_ -match '^FoodDiary\.Application/DependencyInjection(?:\.[^/]+)?\.cs$' } |
        ForEach-Object {
            $path = $_
            $text = [IO.File]::ReadAllText((Join-Path $repositoryRoot $path))
            if ($text -match "FoodDiary\.Application\.$([regex]::Escape($Module))\.|\b$([regex]::Escape($Module))[A-Za-z0-9_]*(?:Service|Handler|Processor|Validator)\b") {
                [pscustomobject]@{ path = $path; kind = 'composition-registration' }
            }
        }
)

$contracts = [Collections.Generic.List[object]]::new()
foreach ($path in $sourcePaths) {
    $absolutePath = Join-Path $repositoryRoot $path
    if (-not (Test-Path -LiteralPath $absolutePath -PathType Leaf)) { continue }
    $text = [IO.File]::ReadAllText($absolutePath)
    foreach ($match in [regex]::Matches($text, '(?ms)public\s+interface\s+(?<name>I[A-Za-z0-9_]+)(?:\s*:\s*(?<base>[^\{]+))?\s*\{(?<body>.*?)\}')) {
        $body = $match.Groups['body'].Value
        $aggregateMethods = @([regex]::Matches($body, "(?m)^\s*(?<return>[^;\r\n]*\b$([regex]::Escape($aggregateName))\??(?:[>, ])[^;\r\n]*)\s+(?<method>[A-Za-z_]\w*)\s*\(") | ForEach-Object { [pscustomobject]@{ name = $_.Groups['method'].Value; returns = $_.Groups['return'].Value.Trim() } })
        $mutationMethods = @([regex]::Matches($body, '(?m)^\s*(?<return>[^;\r\n]+)\s+(?<method>(?:Update|Create|Delete|Remove|Restore|Set)[A-Za-z0-9_]*)Async\s*\(') | ForEach-Object { [pscustomobject]@{ name = $_.Groups['method'].Value; returns = $_.Groups['return'].Value.Trim() } })
        $baseContracts = @([regex]::Matches($match.Groups['base'].Value, '\bI[A-Z][A-Za-z0-9_]+\b') | ForEach-Object Value | Sort-Object -Unique)
        if ($aggregateMethods.Count -gt 0 -or $mutationMethods.Count -gt 0 -or $baseContracts.Count -gt 0) {
            $contracts.Add([pscustomobject]@{ name = $match.Groups['name'].Value; path = $path; aggregateMethods = $aggregateMethods; mutationMethods = $mutationMethods; baseContracts = $baseContracts })
        }
    }
    foreach ($match in [regex]::Matches($text, '(?m)public\s+interface\s+(?<name>I[A-Za-z0-9_]+)\s*:\s*(?<base>[^;\r\n]+)\s*;')) {
        $name = $match.Groups['name'].Value
        if (@($contracts | Where-Object name -eq $name).Count -gt 0) { continue }
        $baseContracts = @([regex]::Matches($match.Groups['base'].Value, '\bI[A-Z][A-Za-z0-9_]+\b') | ForEach-Object Value | Sort-Object -Unique)
        $contracts.Add([pscustomobject]@{ name = $name; path = $path; aggregateMethods = @(); mutationMethods = @(); baseContracts = $baseContracts })
    }
}

$leakingNames = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($contract in $contracts | Where-Object { @($_.aggregateMethods).Count -gt 0 }) { $null = $leakingNames.Add($contract.name) }
do {
    $added = $false
    foreach ($contract in $contracts) {
        if (-not $leakingNames.Contains($contract.name) -and @($contract.baseContracts | Where-Object { $leakingNames.Contains($_) }).Count -gt 0) { $added = $leakingNames.Add($contract.name) -or $added }
    }
} while ($added)

function Get-ConsumerModule([string]$Path) {
    if ($Path -match '^FoodDiary\.Application(?:\.Abstractions)?/([^/]+)/') { return $Matches[1] }
    if ($Path -match '^FoodDiary\.Application\.([^/]+)/') { return $Matches[1] }
    return ($Path -split '/')[0]
}
$leaks = [Collections.Generic.List[object]]::new()
foreach ($contract in $contracts | Where-Object { $leakingNames.Contains($_.name) }) {
    $escaped = [regex]::Escape($contract.name)
    foreach ($consumerPath in $sourcePaths) {
        if ($consumerPath -eq $contract.path) { continue }
        $consumerText = [IO.File]::ReadAllText((Join-Path $repositoryRoot $consumerPath))
        if ($consumerText -notmatch "\b$escaped\b") { continue }
        $composition = $consumerPath -match 'DependencyInjection|Initializer|Program\.cs$'
        $properties = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        $operations = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        foreach ($variableMatch in [regex]::Matches($consumerText, '(?m)\b(?:User|var)\??\s+(?<name>[a-zA-Z_]\w*)\s*=\s*await\s+[^;]+;')) {
            $variable = [regex]::Escape($variableMatch.Groups['name'].Value)
            foreach ($memberMatch in [regex]::Matches($consumerText, "\b$variable(?:\?\.)?\.(?<member>[A-Z][A-Za-z0-9_]*)(?<call>\s*\()?") ) {
                if ($memberMatch.Groups['call'].Success) { $null = $operations.Add($memberMatch.Groups['member'].Value) }
                else { $null = $properties.Add($memberMatch.Groups['member'].Value) }
            }
        }
        $inheritsLeak = @($contract.baseContracts | Where-Object { $leakingNames.Contains($_) }).Count -gt 0
        $kind = if ($composition) { 'composition-only' } elseif ($inheritsLeak -and @($contract.aggregateMethods).Count -eq 0) { 'transitive-wrapper' } elseif ($contract.name -match 'Directory|Repository') { 'repository-or-directory' } elseif (@($contract.aggregateMethods).Count -gt 0) { 'direct-or-wrapper-aggregate' } else { 'transitive-wrapper' }
        $leaks.Add([pscustomobject]@{ contract = $contract.name; declarationPath = $contract.path; consumerModule = Get-ConsumerModule $consumerPath; consumerPath = $consumerPath; kind = $kind; usedProperties = @($properties | Sort-Object); usedOperations = @($operations | Sort-Object); compositionOnly = $composition })
    }
}

$context = & (Join-Path $PSScriptRoot 'Get-LlmWikiContractConsumers.ps1') -Contract IUserContextService -Format Json | ConvertFrom-Json
$productionLeaks = @($leaks | Where-Object {
    -not $_.compositionOnly -and
    $_.consumerPath -notmatch '(^|/)tests?/|\.Tests?/' -and
    $_.consumerModule -ne $Module -and
    $_.consumerPath -notmatch "^FoodDiary\.Infrastructure/Persistence/$Module/"
})
$mutationConsumers = @(if ($Module -eq 'Users') { @($context.consumers | Where-Object {
    $_.access -eq 'mutation' -and
    -not $_.compositionRegistration -and
    $_.consumer -ne $Module
}) } else { @() })
$blockers = [Collections.Generic.List[string]]::new()
if ($productionLeaks.Count -gt 0) { $blockers.Add("$($productionLeaks.Count) production path(s) expose the $aggregateName aggregate through direct or transitive contracts.") }
if ($mutationConsumers.Count -gt 0) { $blockers.Add("$($mutationConsumers.Count) IUserContextService mutation consumer(s) still require a narrow mutation capability.") }
if ($unresolvedCoreDependencies.Count -gt 0) { $blockers.Add("$($unresolvedCoreDependencies.Count) core Application module dependency(ies) must be removed, moved behind Application.Abstractions, or extracted as project references: $($unresolvedCoreDependencies -join ', ').") }
if ($undeclaredDependencies.Count -gt 0) { $blockers.Add("module-dependencies.json is missing: $($undeclaredDependencies -join ', ').") }
$projections = @($productionLeaks | Where-Object { (@($_.usedProperties).Count -gt 0 -or @($_.usedOperations).Count -gt 0) -and $_.consumerPath -match '^FoodDiary\.Application(?:\.Billing)?/' -and $_.consumerModule -ne 'Users' } | Group-Object consumerModule | ForEach-Object { [pscustomobject]@{
    module = $_.Name
    suggestedName = "$(($_.Name -replace '^FoodDiary\.Application\.', ''))UserProjection"
    fields = @($_.Group | ForEach-Object { @($_.usedProperties) } | Sort-Object -Unique)
    operations = @($_.Group | ForEach-Object { @($_.usedOperations) } | Sort-Object -Unique)
    consumers = @($_.Group | ForEach-Object consumerPath | Sort-Object -Unique)
} })
$compileProbeResult = [pscustomobject]@{ requested = [bool]$CompileProbe; passed = $null; exitCode = $null; projectPath = $null; diagnostics = @() }
if ($CompileProbe) {
    $probeRoot = Join-Path $repositoryRoot ".artifacts/llm-wiki/extraction-probe/$($Module.ToLowerInvariant())-$PID-$([Guid]::NewGuid().ToString('N'))"
    $probeProject = Join-Path $probeRoot "FoodDiary.Application.$Module.Probe.csproj"
    $null = New-Item -ItemType Directory -Path $probeRoot -Force
    try {
        $compileItems = @($moduleSourcePaths | ForEach-Object {
            $absolute = (Join-Path $repositoryRoot $_).Replace('&', '&amp;').Replace('"', '&quot;')
            "    <Compile Include=`"$absolute`" Link=`"$($_.Substring($moduleSourcePrefixes[0].Length).Replace('&', '&amp;').Replace('"', '&quot;'))`" />"
        }) -join [Environment]::NewLine
        $projectReferences = @(
            'FoodDiary.Application.Abstractions/FoodDiary.Application.Abstractions.csproj'
            'FoodDiary.Domain/FoodDiary.Domain.csproj'
            'Shared/FoodDiary.Mediator/FoodDiary.Mediator.csproj'
            $projectDependencies | ForEach-Object { "FoodDiary.Application.$_/FoodDiary.Application.$_.csproj" }
        ) | ForEach-Object { "    <ProjectReference Include=`"$((Join-Path $repositoryRoot $_).Replace('&', '&amp;').Replace('"', '&quot;'))`" />" }
        $projectText = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <AssemblyName>FoodDiary.Application.$Module.Probe</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
$compileItems
  </ItemGroup>
  <ItemGroup>
$($projectReferences -join [Environment]::NewLine)
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="FluentValidation" />
    <PackageReference Include="FluentValidation.DependencyInjectionExtensions" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />
  </ItemGroup>
</Project>
"@
        [IO.File]::WriteAllText($probeProject, $projectText, [Text.UTF8Encoding]::new($false))
        $probeOutput = @(& dotnet build $probeProject --nologo --artifacts-path (Join-Path $probeRoot 'artifacts') -m:1 2>&1 | ForEach-Object { [string]$_ })
        $probeExitCode = $LASTEXITCODE
        $compileProbeResult = [pscustomobject]@{
            requested = $true
            passed = $probeExitCode -eq 0
            exitCode = $probeExitCode
            projectPath = $probeProject.Substring($repositoryRoot.Length + 1).Replace('\', '/')
            diagnostics = @($probeOutput | Where-Object { $_ -match '(?i)\berror\s+(?:CS|NU|MSB)\d+' } | Select-Object -First 30)
        }
        if ($probeExitCode -ne 0) { $blockers.Add("Compile probe failed with exit code $probeExitCode; inspect compileProbe.diagnostics.") }
    } finally {
        Remove-Item -LiteralPath $probeRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}

$result = [pscustomobject]@{
    schemaVersion = 1; module = $Module; ownedAggregate = $aggregateName
    contractReadiness = [pscustomobject]@{ contract = 'IUserContextService'; aggregateBlockers = [int]$context.readiness.aggregateConsumers; mutationBlockers = $mutationConsumers.Count; aggregateReady = [int]$context.readiness.aggregateConsumers -eq 0 }
    moduleReadiness = [pscustomobject]@{ ready = $blockers.Count -eq 0; blockers = @($blockers); aggregateLeakPaths = $productionLeaks.Count; leakingContracts = @($leakingNames | Sort-Object) }
    dependencyReadiness = [pscustomobject]@{
        sourceFileCount = $moduleSourcePaths.Count
        internalFeatureNamespaces = @($internalFeatureNamespaces | Sort-Object)
        actualModules = $actualDependencies
        declaredModules = $declaredDependencies
        undeclaredModules = $undeclaredDependencies
        staleDeclaredModules = $staleDeclaredDependencies
        availableProjectModules = $projectDependencies
        unresolvedCoreModules = $unresolvedCoreDependencies
        sourceReferences = $sourceDependencies
        diRegistrations = $diRegistrations
        ready = $unresolvedCoreDependencies.Count -eq 0 -and $undeclaredDependencies.Count -eq 0
    }
    compileProbe = $compileProbeResult
    leaks = @($leaks)
    categories = [pscustomobject]@{ directOrWrapper = @($leaks | Where-Object kind -eq 'direct-or-wrapper-aggregate').Count; repositoryOrDirectory = @($leaks | Where-Object kind -eq 'repository-or-directory').Count; transitiveWrapper = @($leaks | Where-Object kind -eq 'transitive-wrapper').Count; test = @($leaks | Where-Object { $_.consumerPath -match '(^|/)tests?/|\.Tests?/' }).Count; compositionOnly = @($leaks | Where-Object compositionOnly).Count }
    suggestedProjections = $projections
}
if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 10; exit 0 }
Write-Host "Extraction readiness: $Module module (owned aggregate: $aggregateName)"
Write-Host "Contract readiness: IUserContextService aggregate blockers=$($result.contractReadiness.aggregateBlockers), mutation blockers=$($result.contractReadiness.mutationBlockers), aggregate ready=$($result.contractReadiness.aggregateReady)"
Write-Host "Module readiness: $(if ($result.moduleReadiness.ready) { 'ready' } else { 'not ready' })"
Write-Host "Dependency readiness: actual=$($actualDependencies.Count), undeclared=$($undeclaredDependencies.Count), DI registrations=$($diRegistrations.Count)"
if ($CompileProbe) { Write-Host "Compile probe: $(if ($result.compileProbe.passed) { 'passed' } else { 'failed' })" }
foreach ($blocker in $result.moduleReadiness.blockers) { Write-Host "BLOCKER: $blocker" }
Write-Host "Leaks: direct/wrapper=$($result.categories.directOrWrapper), repository/directory=$($result.categories.repositoryOrDirectory), transitive=$($result.categories.transitiveWrapper), tests=$($result.categories.test), composition=$($result.categories.compositionOnly)"
foreach ($contractGroup in @($productionLeaks | Group-Object contract | Sort-Object @{ Expression = 'Count'; Descending = $true }, Name)) {
    $modules = @($contractGroup.Group.consumerModule | Sort-Object -Unique)
    Write-Host "- $($contractGroup.Name): $($contractGroup.Count) production path(s), modules=$($modules -join ', ')"
}
foreach ($projection in $result.suggestedProjections) {
    Write-Host "Projection: $($projection.suggestedName) { $($projection.fields -join ', ') }"
    if (@($projection.operations).Count -gt 0) { Write-Host "  Separate mutation/domain operations: $($projection.operations -join ', ')" }
}
