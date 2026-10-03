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

$toolText = Get-Content -LiteralPath $tool -Raw
Assert-ApiCompatibility ($toolText -notmatch '\[regex\]::Matches') 'API compatibility still parses C# DTO declarations with regular expressions.'
Assert-ApiCompatibility ($toolText -match '--name-status --find-renames') 'API compatibility does not preserve DTO identity across physical file moves.'
Assert-ApiCompatibility ($toolText -match "'Modules/\*/Presentation/\*\*/\*\.cs'") 'API compatibility does not discover module-owned Presentation DTOs.'
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
