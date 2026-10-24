[CmdletBinding()]
param([switch]$SkipRefresh)

$ErrorActionPreference = 'Stop'
$planner = Join-Path $PSScriptRoot 'Get-LlmWikiGraphTestPlan.ps1'
# The mixed Meals flow used by development-context has more downstream tests
# than the bounded response. Its service test must survive that boundary.
$scope = @(
    'Modules/Meals/Application/Services/MealNutritionCalculator.cs'
    'Modules/Meals/Application/Services/MealNutritionService.cs'
    'Modules/Meals/Infrastructure/Persistence/Meals/MealNutritionStatisticsReadService.cs'
    'FoodDiary.Web.Client/src/app/features/meals/components/manage/meal-manage-form.ts'
    'Modules/Meals/Application/Queries/ReadMealNutritionStatistics/ReadMealNutritionStatisticsQueryHandler.cs'
    'Hosts/tests/FoodDiary.Web.Api.IntegrationTests/PresentationBoundaryIntegrationTests.cs'
    'Modules/Meals/Application/Commands/CreateMeal/CreateMealCommandHandler.cs'
    'Modules/Meals/Application/Commands/RepeatMeal/RepeatMealCommandHandler.cs'
    'Modules/Meals/Application/Commands/UpdateMeal/UpdateMealCommandHandler.cs'
    'Modules/Meals/Application/Services/MealProductNutritionReadService.cs'
    'Modules/Recipes/Application/Services/RecipeNutritionUpdater.cs'
    'Modules/Recipes/Application/Services/RecipeNutritionCalculator.cs'
)
$plan = & $planner -ProposedPath $scope -Limit 12 -SkipRefresh:$SkipRefresh -Format Json | ConvertFrom-Json
$nutritionTest = 'Modules/Meals/tests/FoodDiary.Modules.Meals.Application.Tests/MealNutritionServiceTests.cs'
if (@($plan.recommended) -notcontains $nutritionTest) {
    throw 'Bounded Meals test plan omitted MealNutritionServiceTests in favor of broad downstream consumers.'
}
if (@($plan.required) -notcontains 'FoodDiary.Web.Client/src/app/features/meals/components/manage/meal-manage-form.spec.ts' -or
    @($plan.required) -notcontains 'Hosts/tests/FoodDiary.Web.Api.IntegrationTests/PresentationBoundaryIntegrationTests.cs' -or
    @($plan.required) -contains $nutritionTest -or @($plan.recommended).Count -gt 12) {
    throw 'Owning-test priority changed direct-test classification or exceeded the response bound.'
}
$recipePlan = & $planner -ProposedPath 'Modules/Recipes/Application/Services/RecipeNutritionCalculator.cs' -Limit 1 -SkipRefresh -Format Json | ConvertFrom-Json
if (@($recipePlan.recommended) -notcontains 'Modules/Recipes/tests/FoodDiary.Modules.Recipes.Application.Tests/RecipeNutritionCalculatorTests.cs') {
    throw 'Owning-test priority did not generalize to the Recipes calculator at Limit=1.'
}
$recipeDirectoryPlan = & $planner -ProposedPath 'Modules/Recipes/Application' -Limit 100 -SkipRefresh -Format Json | ConvertFrom-Json
if (@($recipeDirectoryPlan.recommended | Where-Object { $_ -match 'RecipesFeatureTests\.cs$' }).Count -ne 1 -or
    @($recipeDirectoryPlan.required | Where-Object { $_ -match 'RecipesFeatureTests\.cs$' }).Count -ne 0) {
    throw 'Directory-scoped Recipes plan dropped its existing feature test while prioritizing file-scoped owners.'
}
Write-Host 'Fast graph test-plan regressions passed (Meals flow, Recipes file ownership and directory scope).'
