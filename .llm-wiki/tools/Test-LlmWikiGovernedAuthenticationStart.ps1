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
    'Google linking rejects a credential email that differs from the current user email.'
    'Telegram linking rejects a replayed linking token.'
    'Email verification resend enforces cooldown before issuing a replacement token.'
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

    $assessment = & (Join-Path $repositoryRoot '.llm-wiki/wiki.ps1') task-requirements-assess `
        -WorkspacePath $workspacePath `
        -Format Json | ConvertFrom-Json
    if (-not $assessment.valid) { throw "Governed requirements assessment failed: $(@($assessment.model.findings | ForEach-Object { "$($_.criterionId):$($_.id)" }) -join ', ')" }
    if ([int]$assessment.model.classification.criteriaCount -ne $criteria.Count) { throw 'Governed requirements assessment lost acceptance criteria.' }
} finally {
    Remove-Item -LiteralPath $workspaceAbsolute -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host 'LLM Wiki governed Authentication task-start tests passed.'
