[CmdletBinding()]
param(
    [string]$BaseRef = 'HEAD',
    [string]$HeadRef,
    [string[]]$ChangedPath,
    [object]$DiffInput,
    [object]$PolicyInput,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$toolsRoot = $PSScriptRoot
$common = @{ BaseRef = $BaseRef; Format = 'Json' }
if ($PSBoundParameters.ContainsKey('HeadRef')) { $common.HeadRef = $HeadRef }
if ($PSBoundParameters.ContainsKey('ChangedPath')) { $common.ChangedPath = $ChangedPath }
$diffArguments = @{} + $common
$diffArguments.Limit = 20
$diff = if ($null -ne $DiffInput) { $DiffInput } else {
    & (Join-Path $toolsRoot 'Get-LlmWikiDiffContext.ps1') @diffArguments | ConvertFrom-Json
}
$policy = if ($null -ne $PolicyInput) { $PolicyInput } else {
    & (Join-Path $toolsRoot 'Test-LlmWikiChangePolicy.ps1') @common | ConvertFrom-Json
}
$paths = @($diff.changedPaths)
$rules = @($policy.matchedRules.id)

$flags = [ordered]@{
    databaseMigration = @($paths | Where-Object { $_ -match '/Migrations/' -or $_ -match 'ModelSnapshot\.cs$' }).Count -gt 0
    configuration = @($paths | Where-Object { $_ -match 'appsettings|\.env\.example$|Options\.cs$|docker-compose|deploy\.yml$' }).Count -gt 0
    dependencies = 'dependency-nuget' -in $rules -or 'dependency-npm' -in $rules
    backgroundJobs = @($paths | Where-Object { $_ -match 'FoodDiary\.JobManager|HostedService|Jobs?/' }).Count -gt 0
    externalIntegrations = @($paths | Where-Object { $_ -match 'FoodDiary\.Integrations|MailRelay|MailInbox|Telegram|Billing|Webhooks?' }).Count -gt 0
    publicApi = @($diff.scopes) -contains 'Api'
    frontend = @($diff.scopes) -contains 'Frontend'
}

$preDeploy = [System.Collections.Generic.List[string]]::new()
$deploy = [System.Collections.Generic.List[string]]::new()
$postDeploy = [System.Collections.Generic.List[string]]::new()
$rollback = [System.Collections.Generic.List[string]]::new()
$preDeploy.Add('Resolve every required check and review obligation in the evidence bundle.')
$postDeploy.Add('Monitor error rate, latency, saturation, and changed business-flow outcomes.')
$rollback.Add('Identify the last known-good application version and preserve forward-compatible data.')

if ($flags.databaseMigration) {
    $preDeploy.Add('Review migration lock duration, table rewrite/backfill cost, defaults/nullability, and compatibility with the currently deployed application.')
    $deploy.Add('Apply migrations with the repository initializer/deployment sequence before enabling code that requires the new schema.')
    $postDeploy.Add('Verify migration history, expected schema objects/indexes, and representative reads/writes.')
    $rollback.Add('Prefer roll-forward for shared/production data; document whether old application code remains schema-compatible.')
}
if ($flags.configuration) {
    $preDeploy.Add('Diff configuration keys across appsettings templates, environment examples, deployment secrets/variables, and option validation.')
    $deploy.Add('Provision non-secret configuration and secrets before starting consumers; do not print secret values.')
    $postDeploy.Add('Verify startup option validation, readiness, and provider connectivity in the target environment.')
    $rollback.Add('Retain the previous configuration contract and secret versions until rollback is no longer needed.')
}
if ($flags.dependencies) {
    $preDeploy.Add('Review dependency changelog, transitive graph, vulnerability audit, license, lockfile, and runtime/platform compatibility.')
    $postDeploy.Add('Monitor startup/runtime errors and provider/client behavior affected by upgraded packages.')
}
if ($flags.backgroundJobs) {
    $preDeploy.Add('Review duplicate execution, leases/locks, retry policy, idempotency, schedule changes, and mixed-version workers.')
    $postDeploy.Add('Verify job registration, last-success age, failure streak, duration, and processed-item counts.')
}
if ($flags.externalIntegrations) {
    $preDeploy.Add('Confirm provider credentials, sandbox/staging behavior, timeouts, retries, idempotency, webhook verification, and rate limits.')
    $postDeploy.Add('Verify provider success/failure/fallback telemetry without exposing payload secrets or personal data.')
}
if ($flags.publicApi) {
    $preDeploy.Add('Review OpenAPI compatibility and coordinate consumers for intentional breaking changes.')
    $postDeploy.Add('Smoke-test changed routes with representative authorization and error cases.')
}
if ($flags.frontend) {
    $deploy.Add('Deploy frontend assets with cache/version compatibility for the currently deployed API.')
    $postDeploy.Add('Verify the target interaction, console health, desktop/mobile rendering, and localized copy.')
}
if ($deploy.Count -eq 0) { $deploy.Add('Use the standard deployment path; no specialized ordering was inferred.') }

$result = [pscustomobject]@{
    flags = [pscustomobject]$flags
    preDeploy = @($preDeploy)
    deploy = @($deploy)
    postDeploy = @($postDeploy)
    rollback = @($rollback)
    reviewObligations = @($policy.reviewObligations)
}
if ($Format -eq 'Json') {
    $result | ConvertTo-Json -Depth 8
    exit 0
}

Write-Host 'Rollout impact:'
foreach ($property in $result.flags.PSObject.Properties | Where-Object Value) { Write-Host " - $($property.Name)" }
foreach ($section in @(
    @{ Name = 'Pre-deploy'; Values = $result.preDeploy },
    @{ Name = 'Deploy'; Values = $result.deploy },
    @{ Name = 'Post-deploy'; Values = $result.postDeploy },
    @{ Name = 'Rollback'; Values = $result.rollback }
)) {
    Write-Host ''
    Write-Host "$($section.Name):"
    foreach ($item in $section.Values) { Write-Host " - $item" }
}
