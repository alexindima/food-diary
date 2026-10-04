[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$tool = Join-Path $PSScriptRoot 'Test-LlmWikiApiCompatibility.ps1'
. (Join-Path $PSScriptRoot 'LlmWikiApiAcceptance.ps1')

function Assert-ApiCompatibility([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

$baseContract = @{
    OpenApi = '3.0.4'
    Endpoints = @(
        @{ Path = '/api/v{version}/example'; Operations = @(
            @{ Method = 'post'; HasRequestBody = $true; ResponseCodes = @('200') }
        ) }
    )
} | ConvertTo-Json -Depth 8
$restrictedContract = @{
    OpenApi = '3.0.4'
    Endpoints = @(
        @{ Path = '/api/v{version}/example'; Operations = @(
            @{ Method = 'post'; HasRequestBody = $true; ResponseCodes = @('200', '413') }
        ) }
    )
} | ConvertTo-Json -Depth 8
$restriction = & $tool -BaseSnapshotContent $baseContract -CurrentSnapshotContent $restrictedContract -Format Json | ConvertFrom-Json
Assert-ApiCompatibility ($restriction.breakingCount -eq 0) 'A request-size limit was incorrectly classified as schema-breaking.'
Assert-ApiCompatibility ($restriction.behavioralRestrictionCount -eq 1) 'A new 413 response was not classified as a behavioral restriction.'
Assert-ApiCompatibility (@($restriction.behavioralRestrictions.kind) -contains 'added-request-size-restriction') 'The behavioral restriction detail is missing.'

$detailedBaseContract = @{
    OpenApi = '3.0.4'
    Endpoints = @(
        @{ Path = '/api/v{version}/example'; Operations = @(
            @{
                Method = 'post'
                HasRequestBody = $true
                RequestBody = @{ Required = $true; Content = @(
                    @{ MediaType = 'application/json'; Reference = '#/components/schemas/CreateRequest'; Nullable = $false }
                ) }
                ResponseCodes = @('200')
                SuccessResponses = @(
                    @{ StatusCode = '200'; MediaType = 'application/json'; Type = 'array'; ItemReference = '#/components/schemas/Item'; Nullable = $false }
                )
                QueryParameters = @(
                    @{ Name = 'limit'; Location = 'query'; Required = $false; Type = 'integer'; Format = 'int32'; Default = '20'; Minimum = 1; Maximum = 100 }
                )
            }
        ) }
    )
} | ConvertTo-Json -Depth 14
$detailedChangedContract = @{
    OpenApi = '3.0.4'
    Endpoints = @(
        @{ Path = '/api/v{version}/example'; Operations = @(
            @{
                Method = 'post'
                HasRequestBody = $true
                RequestBody = @{ Required = $true; Content = @(
                    @{ MediaType = 'application/json'; Reference = '#/components/schemas/ReplacementRequest'; Nullable = $false }
                ) }
                ResponseCodes = @('200')
                SuccessResponses = @(
                    @{ StatusCode = '200'; MediaType = 'application/json'; Type = 'array'; ItemReference = '#/components/schemas/ReplacementItem'; Nullable = $false }
                    @{ StatusCode = '200'; MediaType = 'text/csv'; Type = 'string'; Format = 'binary'; Nullable = $false; Headers = @(
                        @{ Name = 'Content-Disposition'; Type = 'string' }
                    ) }
                )
                QueryParameters = @(
                    @{ Name = 'limit'; Location = 'query'; Required = $false; Type = 'integer'; Format = 'int32'; Default = '20'; Minimum = 1; Maximum = 50 }
                )
            }
        ) }
    )
} | ConvertTo-Json -Depth 14
$detailed = & $tool -BaseSnapshotContent $detailedBaseContract -CurrentSnapshotContent $detailedChangedContract -Format Json | ConvertFrom-Json
Assert-ApiCompatibility (@($detailed.changes.kind) -contains 'changed-parameter') 'Parameter validation constraints were omitted from compatibility comparison.'
Assert-ApiCompatibility (@($detailed.changes.kind) -contains 'changed-request-schema') 'Request body schema changes were omitted from compatibility comparison.'
Assert-ApiCompatibility (@($detailed.changes.kind) -contains 'changed-response-schema') 'Successful response item schema changes were omitted from compatibility comparison.'
Assert-ApiCompatibility (@($detailed.changes.kind) -contains 'added-response-media-type') 'Additional response media types were omitted from compatibility comparison.'
Assert-ApiCompatibility (@($detailed.changes.kind) -contains 'added-response-header') 'Additional response headers were omitted from compatibility comparison.'

foreach ($case in @(
    @{ Minimum = 0; Maximum = 100; Breaking = $false }
    @{ Minimum = 1; Maximum = 200; Breaking = $false }
    @{ Minimum = $null; Maximum = $null; Breaking = $false }
    @{ Minimum = 2; Maximum = 100; Breaking = $true }
    @{ Minimum = 0; Maximum = 50; Breaking = $true }
    @{ Minimum = 0; Maximum = 100; Type = 'string'; Breaking = $true }
    @{ Minimum = 0; Maximum = 100; Default = '30'; Breaking = $true }
)) {
    $rangeContract = $detailedBaseContract | ConvertFrom-Json
    $parameter = $rangeContract.Endpoints[0].Operations[0].QueryParameters[0]
    $parameter.Minimum = $case.Minimum
    $parameter.Maximum = $case.Maximum
    if ($case.ContainsKey('Type')) { $parameter.Type = $case.Type }
    if ($case.ContainsKey('Default')) { $parameter.Default = $case.Default }
    $rangeResult = & $tool -BaseSnapshotContent $detailedBaseContract -CurrentSnapshotContent ($rangeContract | ConvertTo-Json -Depth 14) -Format Json | ConvertFrom-Json
    Assert-ApiCompatibility (($rangeResult.breakingCount -gt 0) -eq $case.Breaking) "Incorrect numeric parameter compatibility: $($case | ConvertTo-Json -Compress)"
    if (-not $case.Breaking) {
        Assert-ApiCompatibility (@($rangeResult.changes.kind) -contains 'widened-parameter-range') 'Widened numeric range must remain visible as an additive change.'
    }
}

$emptyEndpointBase = @{
    OpenApi = '3.0.4'
    Endpoints = @()
    Schemas = @(
        @{ Name = 'ExampleHttpRequest'; Properties = @(
            @{ Name = 'stable'; Required = $true; Type = 'string'; Nullable = $false }
        ) }
    )
} | ConvertTo-Json -Depth 10
$emptyEndpointChanged = @{
    OpenApi = '3.0.4'
    Endpoints = @()
    Schemas = @(
        @{ Name = 'ExampleHttpRequest'; Properties = @(
            @{ Name = 'stable'; Required = $true; Type = 'string'; Nullable = $false }
            @{ Name = 'optionalAdded'; Required = $false; Type = 'boolean'; Nullable = $false }
        ) }
    )
} | ConvertTo-Json -Depth 10
$emptyEndpointSchema = & $tool `
    -BaseSnapshotContent $emptyEndpointBase `
    -CurrentSnapshotContent $emptyEndpointChanged `
    -Format Json | ConvertFrom-Json
Assert-ApiCompatibility ($emptyEndpointSchema.snapshotFormat -eq 'endpoint-contract') 'An empty endpoint array was misclassified as raw OpenAPI.'
Assert-ApiCompatibility (@($emptyEndpointSchema.changes.kind) -contains 'added-schema-property') 'An empty endpoint array suppressed compact schema comparison.'

$beforeDto = @'
public sealed record ExampleHttpRequest(
    string Name,
    string? Details = null) : IValidatableObject {
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
        List<ValidationResult> failures = [];
        return failures;
    }
}
'@
$afterDto = @'
public sealed record ExampleHttpRequest(
    string Name,
    string? Details = null,
    [property: JsonPropertyName("center_x")] decimal? CenterX = null) : IValidatableObject {
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => [];
}
'@
$dto = & $tool `
    -BaseSnapshotContent $baseContract `
    -CurrentSnapshotContent $baseContract `
    -BaseHttpDtoContent $beforeDto `
    -CurrentHttpDtoContent $afterDto `
    -HttpDtoPath 'Synthetic/ExampleHttpRequest.cs' `
    -Format Json | ConvertFrom-Json
Assert-ApiCompatibility (@($dto.changes.kind) -contains 'added-http-dto-property') 'Roslyn DTO comparison omitted an added optional property.'
Assert-ApiCompatibility (@($dto.changes.location) -contains 'Synthetic/ExampleHttpRequest.cs::ExampleHttpRequest.center_x') 'Roslyn DTO comparison ignored JsonPropertyName.'
Assert-ApiCompatibility (@($dto.changes.location | Where-Object { $_ -match '\.failures$' }).Count -eq 0) 'A method-local variable was treated as an HTTP DTO property.'

# Exercise actual Git selection and the real Roslyn parser in an isolated repository.
# Synthetic snapshot arguments bypass file discovery and cannot catch unchanged DTOs.
. (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1')
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$fixtureParent = Join-Path $repositoryRoot '.artifacts/llm-wiki/tests/api-dto-selection'
$fixtureRoot = Join-Path $fixtureParent ([guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $fixtureRoot -Force
function Invoke-DtoFixtureGit([string[]]$Arguments) {
    $null = Invoke-LlmWikiGitCommand -RepositoryRoot $fixtureRoot -Arguments $Arguments -FailureMessage 'API DTO fixture Git operation failed.'
}
function Write-DtoFixtureFile([string]$Path, [string]$Content) {
    $absolutePath = Join-Path $fixtureRoot $Path
    $null = New-Item -ItemType Directory -Path (Split-Path -Parent $absolutePath) -Force
    [IO.File]::WriteAllText($absolutePath, $Content, [Text.UTF8Encoding]::new($false))
}
function Get-DtoFixtureReport {
    Push-Location $fixtureRoot
    try {
        & $fixtureTool -BaseRef HEAD -SnapshotPath 'openapi.json' -PayloadSnapshotPath 'payload.json' -Format Json | ConvertFrom-Json
    } finally { Pop-Location }
}
try {
    $fixtureTools = Join-Path $fixtureRoot '.llm-wiki/tools'
    $null = New-Item -ItemType Directory -Path $fixtureTools -Force
    foreach ($name in @('Test-LlmWikiApiCompatibility.ps1', 'LlmWikiApiAcceptance.ps1', 'LlmWikiGitPaths.ps1')) {
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination (Join-Path $fixtureTools $name)
    }
    $extractorRoot = Join-Path $fixtureTools 'roslyn-extractor'
    $null = New-Item -ItemType Directory -Path (Join-Path $extractorRoot 'bin/Release') -Force
    foreach ($name in @('Program.cs', 'LlmWiki.RoslynExtractor.csproj')) {
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot "roslyn-extractor/$name") -Destination (Join-Path $extractorRoot $name)
    }
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'roslyn-extractor/bin/Release/net10.0') -Destination (Join-Path $extractorRoot 'bin/Release/net10.0') -Recurse
    $fixtureTool = Join-Path $fixtureTools 'Test-LlmWikiApiCompatibility.ps1'
    $moduleDto = 'Modules/Example/Presentation.Contracts/ExampleHttpRequest.cs'
    $legacyDto = 'FoodDiary.Presentation.Api/LegacyHttpResponse.cs'
    $unicodeDto = 'Modules/Example/Presentation.Contracts/Папка с пробелом/UnicodeHttpRequest.cs'
    $originalDto = 'public sealed record ExampleHttpRequest(string Name);'
    Write-DtoFixtureFile $moduleDto $originalDto
    Write-DtoFixtureFile $legacyDto 'public sealed record LegacyHttpResponse(string Name);'
    Write-DtoFixtureFile $unicodeDto 'public sealed record UnicodeHttpRequest(string Name);'
    Write-DtoFixtureFile 'openapi.json' $baseContract
    Write-DtoFixtureFile 'payload.json' '{}'
    Write-DtoFixtureFile '.gitignore' "Logs/`n.llm-wiki/`nbin/`nobj/`n"
    Invoke-DtoFixtureGit @('init', '--quiet')
    Invoke-DtoFixtureGit @('config', 'core.autocrlf', 'false')
    Invoke-DtoFixtureGit @('config', 'core.quotepath', 'true')
    Invoke-DtoFixtureGit @('add', '--', '.gitignore', 'openapi.json', 'payload.json', $moduleDto, $legacyDto, $unicodeDto)
    Invoke-DtoFixtureGit @('-c', 'user.name=Wiki Regression', '-c', 'user.email=wiki-regression@example.invalid', 'commit', '--quiet', '-m', 'HTTP DTO selection baseline')

    Write-DtoFixtureFile 'Modules/Example/Presentation/bin/GeneratedHttpRequest.cs' 'public sealed record GeneratedHttpRequest(string Name);'
    Write-DtoFixtureFile 'Modules/Example/Presentation/obj/GeneratedHttpResponse.cs' 'public sealed record GeneratedHttpResponse(string Name);'
    $unchanged = Get-DtoFixtureReport
    Assert-ApiCompatibility (@($unchanged.httpDtoPaths).Count -eq 0 -and @($unchanged.changes).Count -eq 0) 'Unchanged baseline DTOs or generated sources were reported as new API.'

    Write-DtoFixtureFile $moduleDto 'public sealed record ExampleHttpRequest(string Name, string? Details = null);'
    $modified = Get-DtoFixtureReport
    Assert-ApiCompatibility (@($modified.httpDtoPaths).Count -eq 1 -and $modified.httpDtoPaths[0] -ceq $moduleDto -and $modified.additiveCount -eq 1 -and $modified.breakingCount -eq 0) "A modified tracked DTO was skipped or unchanged DTOs were reintroduced: $($modified | ConvertTo-Json -Depth 5 -Compress)"
    Assert-ApiCompatibility ($modified.changes[0].kind -eq 'added-http-dto-property') 'Modified DTO comparison lost property-level evidence.'
    Write-DtoFixtureFile $moduleDto 'public sealed record ExampleHttpRequest(int Name);'
    $incompatible = Get-DtoFixtureReport
    Assert-ApiCompatibility ($incompatible.breakingCount -eq 1 -and $incompatible.changes[0].kind -eq 'changed-http-dto-property') 'Selection optimization hid a breaking tracked DTO type change.'
    Write-DtoFixtureFile $moduleDto $originalDto

    Write-DtoFixtureFile $legacyDto 'public sealed record LegacyHttpResponse(int Name);'
    $legacyModification = Get-DtoFixtureReport
    Assert-ApiCompatibility (@($legacyModification.httpDtoPaths).Count -eq 1 -and $legacyModification.httpDtoPaths[0] -ceq $legacyDto -and $legacyModification.breakingCount -eq 1) 'Tracked DTOs directly in the legacy presentation root were skipped.'
    Write-DtoFixtureFile $legacyDto 'public sealed record LegacyHttpResponse(string Name);'

    Write-DtoFixtureFile $unicodeDto 'public sealed record UnicodeHttpRequest(int Name);'
    $unicodeModification = Get-DtoFixtureReport
    Assert-ApiCompatibility (@($unicodeModification.httpDtoPaths).Count -eq 1 -and $unicodeModification.httpDtoPaths[0] -ceq $unicodeDto -and $unicodeModification.breakingCount -eq 1) 'Quoted Unicode or spaced Git paths hid a breaking tracked DTO change.'
    Write-DtoFixtureFile $unicodeDto 'public sealed record UnicodeHttpRequest(string Name);'

    $newModuleDto = 'Modules/Example/Presentation/NewHttpRequest.cs'
    $newLegacyDto = 'FoodDiary.Presentation.Api/NewHttpResponse.cs'
    Write-DtoFixtureFile $newModuleDto 'public sealed record NewHttpRequest(string Name);'
    Write-DtoFixtureFile $newLegacyDto 'public sealed record NewHttpResponse(string Name);'
    $new = Get-DtoFixtureReport
    Assert-ApiCompatibility (@($new.httpDtoPaths).Count -eq 2 -and $new.httpDtoPaths -contains $newModuleDto -and $new.httpDtoPaths -contains $newLegacyDto -and $new.additiveCount -eq 2) 'Untracked module or legacy DTOs were lost during baseline filtering.'
    Remove-Item -LiteralPath (Join-Path $fixtureRoot $newModuleDto), (Join-Path $fixtureRoot $newLegacyDto)

    $stagedDestination = 'Modules/Example/Presentation.Contracts/Moved/ExampleHttpRequest.cs'
    $null = New-Item -ItemType Directory -Path (Split-Path -Parent (Join-Path $fixtureRoot $stagedDestination)) -Force
    Invoke-DtoFixtureGit @('mv', '--', $moduleDto, $stagedDestination)
    $stagedRename = Get-DtoFixtureReport
    Assert-ApiCompatibility (@($stagedRename.httpDtoPaths).Count -eq 1 -and $stagedRename.httpDtoPaths[0] -ceq $stagedDestination -and @($stagedRename.changes).Count -eq 0) 'A staged rename lost identity or was classified as a new DTO.'
    Invoke-DtoFixtureGit @('mv', '--', $stagedDestination, $moduleDto)

    $ignoredDestination = 'Modules/Example/Presentation.Contracts/Logs/ExampleHttpRequest.cs'
    Write-DtoFixtureFile $ignoredDestination $originalDto
    Remove-Item -LiteralPath (Join-Path $fixtureRoot $moduleDto)
    $ignoredRename = Get-DtoFixtureReport
    Assert-ApiCompatibility (@($ignoredRename.httpDtoPaths).Count -eq 1 -and $ignoredRename.httpDtoPaths[0] -ceq $ignoredDestination -and @($ignoredRename.changes).Count -eq 0) 'An exact unstaged move into an ignored directory lost identity.'

    Write-DtoFixtureFile $ignoredDestination 'public sealed record ExampleHttpRequest(int Name);'
    $editedMove = Get-DtoFixtureReport
    Assert-ApiCompatibility ($editedMove.breakingCount -eq 1 -and @($editedMove.changes.kind) -contains 'removed-http-dto' -and @($editedMove.changes.kind) -contains 'added-http-dto') 'An edited unstaged move was paired without conservative removal/addition review.'
    Write-DtoFixtureFile $ignoredDestination $originalDto
    $ambiguousDestination = 'Modules/Example/Presentation.Contracts/Other/ExampleHttpRequest.cs'
    Write-DtoFixtureFile $ambiguousDestination $originalDto
    $ambiguousMove = Get-DtoFixtureReport
    Assert-ApiCompatibility ($ambiguousMove.breakingCount -eq 1 -and @($ambiguousMove.httpDtoPaths).Count -eq 3 -and $ambiguousMove.additiveCount -eq 2) 'Ambiguous unstaged move candidates silently suppressed the removed DTO.'
} finally {
    $resolvedFixture = [IO.Path]::GetFullPath($fixtureRoot)
    $resolvedParent = [IO.Path]::GetFullPath($fixtureParent) + [IO.Path]::DirectorySeparatorChar
    if (-not $resolvedFixture.StartsWith($resolvedParent, [StringComparison]::OrdinalIgnoreCase)) { throw 'API DTO fixture cleanup escaped its owned parent.' }
    Remove-Item -LiteralPath $resolvedFixture -Recurse -Force
}

$toolText = Get-Content -LiteralPath $tool -Raw
Assert-ApiCompatibility ($toolText -notmatch '\[regex\]::Matches') 'API compatibility still parses C# DTO declarations with regular expressions.'
Assert-ApiCompatibility ($toolText.Contains('--name-status') -and $toolText.Contains('--find-renames')) 'API compatibility does not preserve DTO identity across physical file moves.'
Assert-ApiCompatibility ($toolText.Contains('Modules/*/Presentation/**/*.cs')) 'API compatibility does not discover module-owned Presentation DTOs.'
Assert-ApiCompatibility ($toolText -match "@\('Presentation', 'Presentation\.Contracts'\)") 'API compatibility does not discover unstaged module-owned Presentation DTOs from physical module roots.'
Assert-ApiCompatibility ($toolText -match "notmatch '\[\\\\/\]\(\?:bin\|obj\)\[\\\\/\]'") 'API compatibility does not exclude generated module Presentation sources.'
Assert-ApiCompatibility ($toolText -match 'Pair only exact-content delete/add candidates') 'API compatibility does not conservatively pair exact unstaged DTO moves.'
$approvalBase = '0123456789012345678901234567890123456789'
$approvalChange = [pscustomobject]@{
    kind = 'removed-operation'; location = 'GET /example'; description = 'Public API operation was removed.'; dimension = 'structural'
}
$approvalSnapshots = @(
    [pscustomobject]@{ path = 'openapi.json'; beforeContent = '{"old":true}'; afterContent = '{"new":true}' }
    [pscustomobject]@{ path = 'payload.json'; beforeContent = '{}'; afterContent = '{}' }
)
$approval = [pscustomobject]@{
    schemaVersion = 1; id = 'synthetic-coordinated-release'; baseCommit = $approvalBase
    decision = 'All clients may be updated together.'; clientRollout = 'Deploy every supported client with the API.'
    snapshots = @($approvalSnapshots | ForEach-Object {
        [pscustomobject]@{
            path = $_.path
            beforeSha256 = Get-LlmWikiApiContractFingerprint $_.beforeContent
            afterSha256 = Get-LlmWikiApiContractFingerprint $_.afterContent
        }
    })
    acceptedChanges = @($approvalChange)
}
$approvalArguments = @{ Manifest = $approval; BaseCommit = $approvalBase; Snapshots = $approvalSnapshots; BreakingChanges = @($approvalChange) }
$accepted = Get-LlmWikiApiAcceptance @approvalArguments
Assert-ApiCompatibility ($accepted.status -eq 'applied' -and @($accepted.acceptedChanges).Count -eq 1 -and @($accepted.unacceptedChanges).Count -eq 0) 'The exact approved transition was not accepted.'
$noApproval = Get-LlmWikiApiAcceptance -BreakingChanges @($approvalChange)
Assert-ApiCompatibility (@($noApproval.unacceptedChanges).Count -eq 1) 'A missing release decision accepted a breaking change.'
$approvalArguments.BaseCommit = 'ffffffffffffffffffffffffffffffffffffffff'
$wrongBase = Get-LlmWikiApiAcceptance @approvalArguments
Assert-ApiCompatibility (@($wrongBase.acceptedChanges).Count -eq 0) 'A release decision accepted another baseline.'
$approvalArguments.BaseCommit = $approvalBase
$changedSnapshots = @($approvalSnapshots | ForEach-Object {
    [pscustomobject]@{ path = $_.path; beforeContent = $_.beforeContent; afterContent = $_.afterContent }
})
$changedSnapshots[0].afterContent = '{"other":true}'
$approvalArguments.Snapshots = $changedSnapshots
$wrongFingerprint = Get-LlmWikiApiAcceptance @approvalArguments
Assert-ApiCompatibility ($wrongFingerprint.status -eq 'fingerprint-mismatch' -and @($wrongFingerprint.acceptedChanges).Count -eq 0) 'Unreviewed snapshot bytes inherited approval.'
$approvalArguments.Snapshots = @($approvalSnapshots[0])
$missingPayload = Get-LlmWikiApiAcceptance @approvalArguments
Assert-ApiCompatibility (@($missingPayload.acceptedChanges).Count -eq 0) 'Omitting the payload snapshot inherited approval.'
$approvalArguments.Snapshots = $approvalSnapshots
$approvalArguments.Snapshots = @($approvalSnapshots[0], $approvalSnapshots[0])
$duplicateSnapshot = Get-LlmWikiApiAcceptance @approvalArguments
Assert-ApiCompatibility (@($duplicateSnapshot.acceptedChanges).Count -eq 0) 'Duplicate OpenAPI snapshots bypassed the payload fingerprint.'
$approvalArguments.Snapshots = $approvalSnapshots
$extraChange = [pscustomobject]@{
    kind = 'removed-operation'; location = 'GET /other'; description = 'Public API operation was removed.'; dimension = 'structural'
}
$approvalArguments.BreakingChanges = @($approvalChange, $extraChange)
$additionalBreak = Get-LlmWikiApiAcceptance @approvalArguments
Assert-ApiCompatibility (@($additionalBreak.acceptedChanges).Count -eq 1 -and @($additionalBreak.unacceptedChanges).Count -eq 1) 'An additional break inherited the release decision.'
$modifiedChange = [pscustomobject]@{
    kind = $approvalChange.kind; location = $approvalChange.location; description = 'A different contract change.'; dimension = 'structural'
}
$approvalArguments.BreakingChanges = @($modifiedChange)
$modifiedBreak = Get-LlmWikiApiAcceptance @approvalArguments
Assert-ApiCompatibility (@($modifiedBreak.acceptedChanges).Count -eq 0) 'Approval ignored the exact change description.'
$invalidApproval = $approval | ConvertTo-Json -Depth 8 | ConvertFrom-Json
$invalidApproval.acceptedChanges = @($approvalChange, $approvalChange)
$invalidRejected = $false
try { Get-LlmWikiApiAcceptance -Manifest $invalidApproval -BaseCommit $approvalBase -Snapshots $approvalSnapshots -BreakingChanges @($approvalChange) | Out-Null } catch { $invalidRejected = $true }
Assert-ApiCompatibility $invalidRejected 'Duplicate approval identities were accepted.'
Assert-ApiCompatibility ((Get-LlmWikiApiContractFingerprint "{}`r`n") -eq (Get-LlmWikiApiContractFingerprint "{}`n")) 'Windows and Linux line endings produced different fingerprints.'
Assert-ApiCompatibility ($restriction.acceptedBreakingCount -eq 0) 'Synthetic comparisons inherited a repository release decision.'
Write-Host 'LLM Wiki API compatibility regression passed: contract comparisons and exact release acceptance reject unapproved changes.'
