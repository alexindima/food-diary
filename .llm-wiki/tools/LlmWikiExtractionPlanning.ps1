Set-StrictMode -Version Latest
if (-not (Get-Command Invoke-LlmWikiGitPathList -ErrorAction SilentlyContinue)) {
    . (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1')
}

function Get-LlmWikiExtractionModule([string]$Objective) {
    if ($Objective -match '(?i)\bextract(?:ion)?\s+(?:of\s+)?(?<module>[A-Z][A-Za-z0-9_]+)') { return [string]$Matches.module }
    if ($Objective -match '(?i)\b(?<module>[A-Z][A-Za-z0-9_]+)\s+(?:into|as)\s+an?\s+isolated\s+application\s+module') { return [string]$Matches.module }
    return ''
}

function Get-LlmWikiExtractionPlan([string]$Objective, [string]$RepositoryRoot) {
    $module = Get-LlmWikiExtractionModule $Objective
    if ([string]::IsNullOrWhiteSpace($module)) { return $null }
    $manifestPath = Join-Path $RepositoryRoot 'docs/architecture/backend-modules.json'
    $manifest = if (Test-Path -LiteralPath $manifestPath -PathType Leaf) { Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json } else { $null }
    $moduleEntry = if ($null -ne $manifest) { $manifest.modules.PSObject.Properties[$module] } else { $null }
    $abstractionAreas = if ($null -ne $moduleEntry -and
        $moduleEntry.Value.PSObject.Properties['sourceMappings'] -and
        $moduleEntry.Value.sourceMappings.PSObject.Properties['abstractionAreas']) {
        @($moduleEntry.Value.sourceMappings.abstractionAreas)
    } else { @($module) }
    $candidates = @(
        "FoodDiary.Application/$module"
        "FoodDiary.Application.$module"
        @($abstractionAreas | ForEach-Object { "FoodDiary.Application.Abstractions/$_" })
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
            "$module source lives in FoodDiary.Application.$module."
            "The extracted $module project uses only declared module dependencies."
            "Executable composition roots register Add${module}Module."
            "Existing $module application tests pass."
            "The legacy FoodDiary.Application/$module folder contains no source files."
        )
    }
}
