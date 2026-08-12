[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$workspaceName = ".authentication-start-$([guid]::NewGuid().ToString('N'))"
$workspacePath = ".artifacts/llm-wiki/tasks/$workspaceName"
$workspaceAbsolute = Join-Path $repositoryRoot $workspacePath
$paths = @(
    'FoodDiary.Application/Authentication/Commands/LinkGoogle/LinkGoogleCommandHandler.cs'
    'FoodDiary.Application/Authentication/Commands/LinkTelegram/LinkTelegramCommandHandler.cs'
    'FoodDiary.Application/Authentication/Commands/ResendEmailVerification/ResendEmailVerificationCommandHandler.cs'
)
$criteria = @(
    'Google linking preserves email mismatch, same identity, different identity, already-owned identity, and successful link outcomes without exposing User to Authentication.'
    'Telegram linking preserves replay protection, already-linked identity, already-owned identity, and successful link outcomes without exposing User to Authentication.'
    'Email verification resend preserves confirmed-user no-op, cooldown failure, token persistence, and email delivery fields without exposing User to Authentication.'
    'Architecture tests reject direct User aggregate access from the migrated Authentication handlers.'
)
try {
    $design = & (Join-Path $repositoryRoot '.llm-wiki/wiki.ps1') design `
        -Objective 'Replace direct User aggregate mutation in critical Authentication handlers with narrow Users-owned capabilities' `
        -PlannedPath $paths `
        -Format Json | ConvertFrom-Json
    if ($null -eq $design) { throw 'Design did not return a checkpoint result.' }

    & (Join-Path $repositoryRoot '.llm-wiki/wiki.ps1') task-start `
        -Objective 'Replace direct User aggregate mutation in critical Authentication handlers with narrow Users-owned capabilities' `
        -Criterion $criteria `
        -PlannedPath $paths `
        -AllowedPath '^FoodDiary\.Application/(?:Authentication|Users)/|^FoodDiary\.Application\.Abstractions/Users/|^tests/FoodDiary\.(?:Application|Architecture)Tests/' `
        -WorkspacePath $workspacePath
    if (-not (Test-Path -LiteralPath (Join-Path $workspaceAbsolute 'workspace.json') -PathType Leaf)) { throw 'Governed task-start did not create a workspace.' }

    $initialAssessment = & (Join-Path $repositoryRoot '.llm-wiki/wiki.ps1') task-requirements-assess `
        -WorkspacePath $workspacePath `
        -Format Json | ConvertFrom-Json
    if ($initialAssessment.valid -or @($initialAssessment.model.findings | Where-Object id -eq 'criterion-compound').Count -lt 3) {
        throw 'Requirement assessment did not reject the compound Authentication criteria.'
    }

    $expansion = & (Join-Path $repositoryRoot '.llm-wiki/wiki.ps1') task-requirements-expand `
        -WorkspacePath $workspacePath `
        -Reason 'Split compound outcomes and add atomic elevated-risk requirements.' `
        -Format Json | ConvertFrom-Json
    if (-not $expansion.valid -or $expansion.addedCount -le 0) {
        throw "Requirement expansion did not produce a valid atomic model: $(@($expansion.model.findings | ForEach-Object { "$($_.criterionId):$($_.id)" }) -join ', ')"
    }

    $assessment = & (Join-Path $repositoryRoot '.llm-wiki/wiki.ps1') task-requirements-assess `
        -WorkspacePath $workspacePath `
        -FailOnInvalid `
        -Format Json | ConvertFrom-Json
    if (-not $assessment.valid) { throw "Governed requirements assessment failed: $(@($assessment.model.findings | ForEach-Object { "$($_.criterionId):$($_.id)" }) -join ', ')" }
    if (@($assessment.model.classification.criteria | Where-Object { -not $_.atomic }).Count -ne 0) { throw 'Requirement expansion retained compound criteria.' }

    $deliveryStatus = & (Join-Path $repositoryRoot '.llm-wiki/wiki.ps1') delivery-status `
        -WorkspacePath $workspacePath `
        -Format Json | ConvertFrom-Json
    if ($deliveryStatus.assessment.objective -cne 'Replace direct User aggregate mutation in critical Authentication handlers with narrow Users-owned capabilities') {
        throw 'Delivery workflow did not read the objective from the current change-packet schema.'
    }
} finally {
    Remove-Item -LiteralPath $workspaceAbsolute -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host 'LLM Wiki governed Authentication task-start tests passed.'
