[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$evaluator = Join-Path $PSScriptRoot 'Test-LlmWikiChangePolicy.ps1'
$cases = @(
    @{ path = 'Modules/Products/Infrastructure/Persistence/Products/ProductRepository.cs'; expected = $true; exists = $true }
    @{ path = 'Modules/Recipes/Infrastructure/Persistence/Recipes/RecipeRepository.cs'; expected = $true; exists = $true }
    @{ path = 'Modules/Dashboard/Infrastructure/Persistence/Dashboard/DashboardReadService.cs'; expected = $true; exists = $true }
    @{ path = 'Modules/Products/Infrastructure/Services/ProductLookupReadService.cs'; expected = $true; exists = $false }
    @{ path = 'Modules/Recipes/Infrastructure/Repositories/RecipeRows.cs'; expected = $true; exists = $false }
    @{ path = 'FoodDiary.Infrastructure/Persistence/Users/UserRepository.cs'; expected = $true; exists = $true }
    @{ path = 'MailInbox/FoodDiary.MailInbox.Infrastructure/Services/NpgsqlInboundMailStore.cs'; expected = $true; exists = $true }
    @{ path = 'Modules/Products/Domain/ProductRepository.cs'; expected = $false; exists = $false }
    @{ path = 'Modules/Products/Application/ProductReadService.cs'; expected = $false; exists = $false }
    @{ path = 'Modules/Products/Contracts/ProductStore.cs'; expected = $false; exists = $false }
    @{ path = 'Modules/Products/InfrastructureExtra/Persistence/ProductRepository.cs'; expected = $false; exists = $false }
    @{ path = 'Modules/Products/tests/Infrastructure/Persistence/ProductRepository.cs'; expected = $false; exists = $false }
    @{ path = 'Modules/Products/Infrastructure/tests/Persistence/ProductRepository.cs'; expected = $false; exists = $false }
    @{ path = 'Modules/Products/Infrastructure/Migrations/ProductRepository.cs'; expected = $false; exists = $false }
    @{ path = 'Modules/Products/Infrastructure/Persistence/Migrations/ProductRows.cs'; expected = $false; exists = $false }
    @{ path = 'Modules/Products/Nested/Infrastructure/Persistence/ProductRepository.cs'; expected = $false; exists = $false }
    @{ path = 'FoodDiary.Infrastructure/Migrations/ProductRepository.cs'; expected = $false; exists = $false }
    @{ path = 'MailInbox/FoodDiary.MailInbox.Infrastructure/Migrations/InboundMailStore.cs'; expected = $false; exists = $false }
)

foreach ($case in $cases) {
    if ($case.exists -and -not (Test-Path -LiteralPath (Join-Path $repositoryRoot $case.path) -PathType Leaf)) {
        throw "Real policy fixture is absent: $($case.path)"
    }
    $result = & $evaluator -ChangedPath @($case.path) -Format Json | ConvertFrom-Json
    $matched = @($result.matchedRules | Where-Object id -eq 'performance-data-access').Count -eq 1
    $required = @($result.requiredChecks | Where-Object id -eq 'data-access-integration-tests').Count -eq 1
    if ($matched -ne $case.expected -or $required -ne $case.expected) {
        throw "Persistence policy mismatch for $($case.path): rule=$matched, check=$required, expected=$($case.expected)"
    }
    if ($required) {
        $command = [string]($result.requiredChecks | Where-Object id -eq 'data-access-integration-tests').command
        if ($command -cne 'dotnet test tests/FoodDiary.Infrastructure.IntegrationTests/FoodDiary.Infrastructure.IntegrationTests.csproj') {
            throw "Persistence policy changed its provider-backed check: $command"
        }
    }
}
Write-Host "Module persistence policy regression passed: $($cases.Count) cases; matched rule and required integration check verified."
