Set-StrictMode -Version Latest
if (-not (Get-Command Invoke-LlmWikiGitPathList -ErrorAction SilentlyContinue)) {
    . (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1')
}

function Get-LlmWikiExtractionModule([string]$Objective) {
    if ($Objective -cmatch '(?i:\bModules/)(?<module>[A-Z][A-Za-z0-9_]+)\b') { return [string]$Matches.module }
    if ($Objective -cmatch '(?i:\bFoodDiary\.Application\.)(?<module>[A-Z][A-Za-z0-9_]+)\b') { return [string]$Matches.module }
    if ($Objective -cmatch '(?i:\bextract(?:ion)?\s+(?:of\s+)?)(?<module>[A-Z][A-Za-z0-9_]+)') { return [string]$Matches.module }
    if ($Objective -cmatch '\b(?<module>[A-Z][A-Za-z0-9_]+)\s+(?i:(?:modular-monolith\s+)?extract(?:ion)?)\b') { return [string]$Matches.module }
    if ($Objective -cmatch '\b(?<module>[A-Z][A-Za-z0-9_]+)\s+(?i:(?:into|as)\s+an?\s+isolated\s+application\s+module)') { return [string]$Matches.module }
    if ($Objective -cmatch '(?i:\u043f\u0435\u0440\u0435\u043d\u043e\u0441|\u043f\u0435\u0440\u0435\u043d\u0435\u0441\u0442\u0438).{0,40}\b(?<module>[A-Z][A-Za-z0-9_]+)\b') { return [string]$Matches.module }
    if ($Objective -cmatch '\b(?<module>[A-Z][A-Za-z0-9_]+)\b.{0,40}(?i:\u043f\u0435\u0440\u0435\u043d\u043e\u0441|\u043f\u0435\u0440\u0435\u043d\u0435\u0441\u0442\u0438)') { return [string]$Matches.module }
    return ''
}

function Get-LlmWikiExtractionPlan([string]$Objective, [string]$RepositoryRoot) {
    $module = Get-LlmWikiExtractionModule $Objective
    if ([string]::IsNullOrWhiteSpace($module)) { return $null }
    $explicitLogicalRootTarget = $Objective -cmatch "(?i:\bModules/$([regex]::Escape($module))\b)"
    $manifestPath = Join-Path $RepositoryRoot 'docs/architecture/backend-modules.json'
    $manifest = if (Test-Path -LiteralPath $manifestPath -PathType Leaf) { Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json } else { $null }
    $moduleEntry = if ($null -ne $manifest) { $manifest.modules.PSObject.Properties[$module] } else { $null }
    $sourceMappings = if ($null -ne $moduleEntry -and $moduleEntry.Value.PSObject.Properties['sourceMappings']) {
        $moduleEntry.Value.sourceMappings
    } else { $null }
    $applicationProjects = if ($explicitLogicalRootTarget) {
        @("Modules/$module/Application")
    } elseif ($null -ne $sourceMappings -and $sourceMappings.PSObject.Properties['applicationProjects']) {
        @($sourceMappings.applicationProjects)
    } else { @("FoodDiary.Application.$module") }
    $logicalRoots = if ($null -ne $sourceMappings -and $sourceMappings.PSObject.Properties['logicalRoot']) {
        @([string]$sourceMappings.logicalRoot)
    } else { @() }
    $mappedProjectPaths = @(
        if ($null -ne $sourceMappings) {
            foreach ($mappingProperty in @($sourceMappings.PSObject.Properties | Where-Object Name -like '*Projects')) {
                @($mappingProperty.Value)
            }
        }
    )
    $abstractionAreas = if ($null -ne $moduleEntry -and
        $moduleEntry.Value.PSObject.Properties['sourceMappings'] -and
        $moduleEntry.Value.sourceMappings.PSObject.Properties['abstractionAreas']) {
        @($moduleEntry.Value.sourceMappings.abstractionAreas)
    } else { @($module) }
    $candidates = @(
        "FoodDiary.Application/$module"
        "FoodDiary.Application.$module"
        $logicalRoots
        $mappedProjectPaths
        @($abstractionAreas | ForEach-Object {
            $normalizedArea = ((([string]$_).Replace('\', '/')) -replace '^\./', '').TrimEnd('/')
            if (Test-Path -LiteralPath (Join-Path $RepositoryRoot $normalizedArea)) {
                $normalizedArea
            } else {
                $moduleAbstractions = "Modules/$module/Application/Abstractions"
                if (Test-Path -LiteralPath (Join-Path $RepositoryRoot $moduleAbstractions)) { $moduleAbstractions }
            }
        })
        'FoodDiary.Application/DependencyInjection.cs'
        "FoodDiary.Application/DependencyInjection.$module.cs"
        'FoodDiary.Initializer/DependencyInjection.cs'
        'FoodDiary.JobManager/DependencyInjection.cs'
        'FoodDiary.Web.Api/Program.cs'
        'FoodDiary.slnx'
        'Dockerfile'
        'tests/FoodDiary.ArchitectureTests/ProjectDependencyMatrixTests.cs'
        'tests/FoodDiary.ArchitectureTests/BusinessModuleBoundaryTests.cs'
        'docs/architecture/backend-modules.json'
        'docs/architecture/module-dependencies.json'
        'docs/backend/BACKEND_MODULE_OWNERSHIP.md'
    )
    $referencePattern = "Add$([regex]::Escape($module))Module|FoodDiary\.Application\.$([regex]::Escape($module))(?:\.csproj)?"
    $mappedProjectFiles = @(
        foreach ($mappedPath in $mappedProjectPaths) {
            $absoluteMappedPath = Join-Path $RepositoryRoot $mappedPath
            if (Test-Path -LiteralPath $absoluteMappedPath -PathType Container) {
                Get-ChildItem -LiteralPath $absoluteMappedPath -Filter '*.csproj' -File
            } elseif ((Test-Path -LiteralPath $absoluteMappedPath -PathType Leaf) -and
                [IO.Path]::GetExtension($absoluteMappedPath) -eq '.csproj') {
                Get-Item -LiteralPath $absoluteMappedPath
            }
        }
    )
    foreach ($projectName in @($mappedProjectFiles | ForEach-Object { $_.Name } | Sort-Object -Unique)) {
        # Physical project identities are independent of legacy CLR assembly names.
        $referencePattern += '|(?:^|[/\\\s"''=])' + [regex]::Escape($projectName) + '(?=$|[\s"''<>),])'
    }
    $referencePaths = @(
        Invoke-LlmWikiGitPathList -RepositoryRoot $RepositoryRoot -Arguments @('ls-files', '--cached', '--others', '--exclude-standard', '--', '*.cs', '*.csproj', '*.slnx', 'Dockerfile', '**/Dockerfile') -FailureMessage 'Unable to enumerate module extraction reference candidates.' |
            Where-Object {
                $candidatePath = $_
                if ($candidatePath.StartsWith("FoodDiary.Application.$module/", [StringComparison]::OrdinalIgnoreCase)) { return $false }
                $absoluteCandidate = Join-Path $RepositoryRoot $candidatePath
                (Test-Path -LiteralPath $absoluteCandidate -PathType Leaf) -and [IO.File]::ReadAllText($absoluteCandidate) -match $referencePattern
            }
    )
    [pscustomobject][ordered]@{
        module = $module
        paths = @($candidates + $referencePaths | Where-Object { Test-Path -LiteralPath (Join-Path $RepositoryRoot $_) } | Sort-Object -Unique)
        criteria = @(
            "$module application source lives in $([string](@($applicationProjects)[0]))."
            "The extracted $module project uses only declared module dependencies."
            "Executable composition roots register Add${module}Module."
            "Existing $module application tests pass."
            "The legacy FoodDiary.Application.$module folder contains no source files."
        )
    }
}

function Get-LlmWikiSupplementalAcceptanceCriteria([string]$Objective, [string[]]$Scopes, [string[]]$Paths) {
    $criteria = [Collections.Generic.List[string]]::new()
    if ('Api' -in $Scopes -or 'Contracts' -in $Scopes) {
        $criteria.Add('HTTP routes match the intended behavior.')
        $criteria.Add('HTTP payloads match the intended behavior.')
        $criteria.Add('HTTP status codes match the intended behavior.')
        $criteria.Add('Existing API consumers remain compatible with the implemented contract change.')
        $criteria.Add('The OpenAPI snapshot matches the implemented HTTP contract.')
    }
    if ('Database' -in $Scopes) {
        $criteria.Add('Persistence mappings match the intended model.')
        $criteria.Add('The database schema matches the intended model.')
        $criteria.Add('Each added EF migration includes its matching Designer file.')
        $criteria.Add('The EF model snapshot includes applicable migration changes.')
    }
    if ($Objective -match '(?i)\b(notification|notify|email|mail|message delivery|push)\b' -and
        @($Paths | Where-Object { $_ -match '(?i)Notification|MailRelay|MailInbox|Email' }).Count -gt 0) {
        $criteria.Add('Notification delivery targets the intended recipient.')
        $criteria.Add('Notification delivery remains idempotent.')
        $criteria.Add('Notification delivery remains retry-safe.')
        $criteria.Add('Focused tests cover notification delivery.')
    }
    if (@($Paths | Where-Object {
                $_ -match '(?i)(?:^|/)FoodDiary\.JobManager/.+\.cs$' -or
                $_ -match '(?i)HostedService|Recurring'
            }).Count -gt 0) {
        $criteria.Add('The background job is registered.')
        $criteria.Add('The background job is configured.')
        $criteria.Add('The background job supports cancellation.')
        $criteria.Add('The background job remains retry-safe.')
        $criteria.Add('Direct background-job consumers compile.')
    }
    if ('Frontend' -in $Scopes) {
        foreach ($state in @('loading', 'success', 'empty', 'validation', 'error')) {
            $criteria.Add("The frontend $state state behaves correctly through the runtime owner.")
        }
    }
    if ('Localization' -in $Scopes) {
        $criteria.Add('English localization keys remain synchronized with Russian localization keys.')
        $criteria.Add('Russian text renders without corruption.')
    }
    if (@($Scopes | Where-Object { $_ -in @('Backend', 'Api', 'Database', 'Frontend') }).Count -gt 1) {
        $criteria.Add('Cross-module dependencies comply with the declared dependency policy.')
        $criteria.Add('The project dependency graph remains acyclic.')
        $criteria.Add('Architecture checks cover the dependency changes.')
    }
    return @($criteria)
}
