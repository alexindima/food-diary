[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$tool = Join-Path $PSScriptRoot 'Test-LlmWikiApiCompatibility.ps1'

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
Write-Host 'LLM Wiki API compatibility regression passed: request, response, parameter, DTO, and behavioral contracts are compared.'
