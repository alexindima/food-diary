[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$toolsRoot = $PSScriptRoot
$wikiRoot = Split-Path -Parent $toolsRoot
$errors = [System.Collections.Generic.List[string]]::new()

function Assert-Wiki {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        $errors.Add($Message)
    }
}

$billingJson = & (Join-Path $toolsRoot 'Find-LlmWikiContext.ps1') `
    -Module Billing `
    -ChangeType Api `
    -Format Json `
    -Limit 8
$billing = $billingJson | ConvertFrom-Json
Assert-Wiki ($billing.module.name -eq 'Billing') 'Billing context did not resolve the extracted application module.'
Assert-Wiki (@($billing.wikiPages.path) -contains '.llm-wiki/generated/modules/billing.md') 'Billing module page is missing from context.'
Assert-Wiki (@($billing.controllers.name) -contains 'BillingController') 'BillingController is missing from API context.'
Assert-Wiki (@($billing.projects.name) -contains 'FoodDiary.Application.Billing') 'Billing application project is missing from context.'
Assert-Wiki (@($billing.symbols.path | Where-Object { $_ -match '/Billing/' }).Count -gt 0) 'Billing symbols are not ranked into context.'
Assert-Wiki (@($billing.tests.path | Where-Object { $_ -match '/Billing/' }).Count -gt 0) 'Billing focused tests are missing from context.'

$diffJson = & (Join-Path $toolsRoot 'Get-LlmWikiDiffContext.ps1') `
    -ChangedPath @(
        'FoodDiary.Presentation.Api/Features/Fasting/FastingController.cs'
        'FoodDiary.Application/Fasting/Commands/StartFastingCommandHandler.cs'
        'FoodDiary.Web.Client/assets/i18n/en/common.json'
        'FoodDiary.Infrastructure/Persistence/Migrations/Example.cs'
    ) `
    -Format Json `
    -Limit 6
$diff = $diffJson | ConvertFrom-Json
Assert-Wiki (@($diff.modules.name) -contains 'Fasting') 'Diff context did not infer the Fasting module.'
Assert-Wiki (@($diff.scopes) -contains 'Api') 'Diff context did not infer API scope.'
Assert-Wiki (@($diff.scopes) -contains 'Frontend') 'Diff context did not infer frontend scope.'
Assert-Wiki (@($diff.scopes) -contains 'Database') 'Diff context did not infer database scope.'
Assert-Wiki (@($diff.scopes) -contains 'Localization') 'Diff context did not infer localization scope.'
Assert-Wiki (@($diff.generatedActions) -contains './.llm-wiki/tools/Build-LlmWikiSymbolIndex.ps1') 'Diff context did not request symbol-index regeneration.'
Assert-Wiki (@($diff.generatedActions) -contains './.llm-wiki/tools/Build-LlmWikiFrontendIndex.ps1') 'Diff context did not request frontend-index regeneration.'
Assert-Wiki (@($diff.warnings | Where-Object { $_ -match 'snapshot' }).Count -gt 0) 'API contract warning is missing.'
Assert-Wiki (@($diff.warnings | Where-Object { $_ -match 'locale' }).Count -gt 0) 'Localization warning is missing.'
Assert-Wiki (@($diff.warnings | Where-Object { $_ -match 'migration' }).Count -gt 0) 'Migration warning is missing.'

$catalog = Get-Content -LiteralPath (Join-Path $wikiRoot 'generated/repository-catalog.json') -Raw | ConvertFrom-Json
$modulePages = Get-ChildItem -LiteralPath (Join-Path $wikiRoot 'generated/modules') -File -Filter '*.md'
Assert-Wiki ($catalog.extractedApplicationModules.Count -eq 2) 'Expected Billing and Marketing extracted application modules.'
Assert-Wiki (@($catalog.extractedApplicationModules.name) -contains 'Billing') 'Billing is missing from extracted application modules.'
Assert-Wiki ($modulePages.Count -eq ($catalog.applicationModules.Count + $catalog.extractedApplicationModules.Count + 1)) 'Generated module-page count does not match catalog modules plus index.'

$symbols = Get-Content -LiteralPath (Join-Path $wikiRoot 'generated/csharp-symbol-index.json') -Raw | ConvertFrom-Json
Assert-Wiki ($symbols.summary.symbols -gt 0) 'C# symbol index is empty.'
Assert-Wiki ($symbols.summary.roles.CommandHandler -gt 0) 'C# symbol index did not classify command handlers.'
Assert-Wiki ($symbols.summary.dependencyInjectionRegistrations -gt 0) 'C# symbol index did not extract DI registrations.'

$frontend = Get-Content -LiteralPath (Join-Path $wikiRoot 'generated/frontend-index.json') -Raw | ConvertFrom-Json
Assert-Wiki ($frontend.summary.features -gt 0) 'Frontend index did not extract features.'
Assert-Wiki ($frontend.summary.routes -gt 0) 'Frontend index did not extract routes.'
Assert-Wiki ($frontend.summary.specs -gt 0) 'Frontend index did not extract specs.'
Assert-Wiki (@($frontend.localization | Where-Object { -not $_.englishExists -or -not $_.russianExists }).Count -eq 0) 'Frontend locale file pairs are incomplete.'

if ($errors.Count -gt 0) {
    Write-Host "LLM Wiki tool smoke tests failed with $($errors.Count) error(s):"
    foreach ($testError in $errors) {
        Write-Host " - $testError"
    }
    exit 1
}

Write-Host 'LLM Wiki tool smoke tests passed: context, diff, module pages, catalog, and symbols.'
