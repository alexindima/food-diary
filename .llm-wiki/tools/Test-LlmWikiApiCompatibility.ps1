[CmdletBinding()]
param(
    [string]$BaseRef = 'HEAD',
    [string]$SnapshotPath = 'tests/FoodDiary.Web.Api.IntegrationTests/Snapshots/openapi-full-contract.json',
    [string]$PayloadSnapshotPath = 'tests/FoodDiary.Web.Api.IntegrationTests/Snapshots/payload-contract-snapshots.json',
    [string]$BaseSnapshotContent,
    [string]$CurrentSnapshotContent,
    [string]$BasePayloadSnapshotContent,
    [string]$CurrentPayloadSnapshotContent,
    [string]$BaseHttpDtoContent,
    [string]$CurrentHttpDtoContent,
    [string]$HttpDtoPath = 'SyntheticHttpModel.cs',
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text',
    [switch]$FailOnBreaking
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$absoluteSnapshotPath = Join-Path $repositoryRoot $SnapshotPath
$absolutePayloadSnapshotPath = Join-Path $repositoryRoot $PayloadSnapshotPath

function Get-Properties {
    param($Object)
    if ($null -eq $Object) { return @() }
    return @($Object.PSObject.Properties)
}
function Get-PropertyValue {
    param($Object, [string]$Name)
    if ($null -eq $Object) { return $null }
    $property = $Object.PSObject.Properties | Where-Object Name -eq $Name | Select-Object -First 1
    if ($null -eq $property) { return $null }
    $property.Value
}

function Add-Change {
    param(
        [System.Collections.Generic.List[object]]$List,
        [string]$Severity,
        [string]$Kind,
        [string]$Location,
        [string]$Description,
        [ValidateSet('structural', 'behavioral')]
        [string]$Dimension = 'structural'
    )
    $List.Add([pscustomobject]@{
        severity = $Severity
        kind = $Kind
        location = $Location
        description = $Description
        dimension = $Dimension
    })
}

function Convert-CompactSchemaShape {
    param($Source)

    $shape = [ordered]@{}
    $scalarMappings = [ordered]@{
        Type = 'type'
        Format = 'format'
        Reference = '$ref'
        Nullable = 'nullable'
        Default = 'default'
        MinLength = 'minLength'
        MaxLength = 'maxLength'
        Minimum = 'minimum'
        Maximum = 'maximum'
        MinItems = 'minItems'
        MaxItems = 'maxItems'
        Pattern = 'pattern'
        Enum = 'enum'
    }
    foreach ($mapping in $scalarMappings.GetEnumerator()) {
        $value = Get-PropertyValue $Source $mapping.Key
        if ($null -ne $value -and -not ($value -is [string] -and [string]::IsNullOrWhiteSpace($value))) {
            $shape[$mapping.Value] = $value
        }
    }

    $itemType = Get-PropertyValue $Source 'ItemType'
    $itemFormat = Get-PropertyValue $Source 'ItemFormat'
    $itemReference = Get-PropertyValue $Source 'ItemReference'
    if ($null -ne $itemType -or $null -ne $itemFormat -or $null -ne $itemReference) {
        $shape['items'] = [ordered]@{}
        if ($null -ne $itemType) { $shape['items']['type'] = $itemType }
        if ($null -ne $itemFormat) { $shape['items']['format'] = $itemFormat }
        if ($null -ne $itemReference) { $shape['items']['$ref'] = $itemReference }
    }

    $additionalType = Get-PropertyValue $Source 'AdditionalPropertiesType'
    $additionalReference = Get-PropertyValue $Source 'AdditionalPropertiesReference'
    if ($null -ne $additionalType -or $null -ne $additionalReference) {
        $shape['additionalProperties'] = [ordered]@{}
        if ($null -ne $additionalType) { $shape['additionalProperties']['type'] = $additionalType }
        if ($null -ne $additionalReference) { $shape['additionalProperties']['$ref'] = $additionalReference }
    }

    return $shape
}

function ConvertTo-ComparableOpenApi {
    param($Snapshot)

    $snapshotEndpoints = Get-PropertyValue $Snapshot 'Endpoints'
    if ($null -ne $snapshotEndpoints) {
        $paths = [ordered]@{}
        foreach ($endpoint in @($snapshotEndpoints)) {
            $operations = [ordered]@{}
            foreach ($operation in @($endpoint.Operations)) {
                $responses = [ordered]@{}
                foreach ($responseCode in @($operation.ResponseCodes)) {
                    $responses[[string]$responseCode] = [ordered]@{}
                }
                $successResponses = @(Get-PropertyValue $operation 'SuccessResponses')
                foreach ($successResponse in @($successResponses | Where-Object { $null -ne $_ })) {
                    $statusCode = [string]$successResponse.StatusCode
                    if (-not $responses.Contains($statusCode)) { $responses[$statusCode] = [ordered]@{} }
                    if (-not $responses[$statusCode].Contains('content')) { $responses[$statusCode]['content'] = [ordered]@{} }
                    $responses[$statusCode]['content'][[string]$successResponse.MediaType] = [ordered]@{
                        schema = Convert-CompactSchemaShape $successResponse
                    }
                    $headers = @((Get-PropertyValue $successResponse 'Headers') | Where-Object { $null -ne $_ })
                    if ($headers.Count -gt 0) {
                        $responses[$statusCode]['headers'] = [ordered]@{}
                        foreach ($header in $headers) {
                            $responses[$statusCode]['headers'][[string]$header.Name] = [ordered]@{
                                schema = Convert-CompactSchemaShape $header
                            }
                        }
                    }
                }
                $queryParameters = @(Get-PropertyValue $operation 'QueryParameters')
                $pathParameters = @(Get-PropertyValue $operation 'PathParameters')
                $parameters = @(@($queryParameters) + @($pathParameters) | Where-Object { $null -ne $_ } | ForEach-Object {
                    [ordered]@{
                        name = [string]$_.Name
                        in = [string]$_.Location
                        required = [bool]$_.Required
                        schema = Convert-CompactSchemaShape $_
                    }
                })
                $comparableOperation = [ordered]@{
                    parameters = $parameters
                    responses = $responses
                }
                $requestBody = Get-PropertyValue $operation 'RequestBody'
                if ($null -ne $requestBody) {
                    $requestContent = [ordered]@{}
                    $requestMediaTypes = Get-PropertyValue $requestBody 'Content'
                    foreach ($mediaType in @($requestMediaTypes | Where-Object { $null -ne $_ })) {
                        $requestContent[[string]$mediaType.MediaType] = [ordered]@{
                            schema = Convert-CompactSchemaShape $mediaType
                        }
                    }
                    $comparableOperation['requestBody'] = [ordered]@{
                        required = [bool](Get-PropertyValue $requestBody 'Required')
                        content = $requestContent
                    }
                } elseif ([bool](Get-PropertyValue $operation 'HasRequestBody')) {
                    $comparableOperation['requestBody'] = [ordered]@{ required = $false; content = [ordered]@{} }
                }
                $operations[[string]$operation.Method.ToLowerInvariant()] = $comparableOperation
            }
            $paths[[string]$endpoint.Path] = $operations
        }

        $schemas = [ordered]@{}
        $snapshotSchemas = @(Get-PropertyValue $Snapshot 'Schemas')
        foreach ($schema in @($snapshotSchemas | Where-Object { $null -ne $_ -and -not [string]::IsNullOrWhiteSpace([string]$_.Name) })) {
            $properties = [ordered]@{}
            $required = [System.Collections.Generic.List[string]]::new()
            $schemaProperties = @(Get-PropertyValue $schema 'Properties')
            foreach ($property in @($schemaProperties | Where-Object { $null -ne $_ -and -not [string]::IsNullOrWhiteSpace([string]$_.Name) })) {
                $shape = [ordered]@{}
                $propertyType = Get-PropertyValue $property 'Type'
                $propertyFormat = Get-PropertyValue $property 'Format'
                $propertyReference = Get-PropertyValue $property 'Reference'
                $propertyItemType = Get-PropertyValue $property 'ItemType'
                $propertyItemReference = Get-PropertyValue $property 'ItemReference'
                if (-not [string]::IsNullOrWhiteSpace([string]$propertyType)) { $shape['type'] = [string]$propertyType }
                if (-not [string]::IsNullOrWhiteSpace([string]$propertyFormat)) { $shape['format'] = [string]$propertyFormat }
                if (-not [string]::IsNullOrWhiteSpace([string]$propertyReference)) { $shape['$ref'] = [string]$propertyReference }
                $shape['nullable'] = [bool](Get-PropertyValue $property 'Nullable')
                if (-not [string]::IsNullOrWhiteSpace([string]$propertyItemType) -or
                    -not [string]::IsNullOrWhiteSpace([string]$propertyItemReference)) {
                    $shape['items'] = [ordered]@{}
                    if (-not [string]::IsNullOrWhiteSpace([string]$propertyItemType)) { $shape['items']['type'] = [string]$propertyItemType }
                    if (-not [string]::IsNullOrWhiteSpace([string]$propertyItemReference)) { $shape['items']['$ref'] = [string]$propertyItemReference }
                }
                $properties[[string]$property.Name] = $shape
                if ([bool](Get-PropertyValue $property 'Required')) { $required.Add([string]$property.Name) }
            }
            $schemas[[string]$schema.Name] = [ordered]@{
                type = 'object'
                properties = $properties
                required = @($required)
            }
        }

        return ([ordered]@{
            paths = $paths
            components = [ordered]@{ schemas = $schemas }
        } | ConvertTo-Json -Depth 12 | ConvertFrom-Json)
    }

    return $Snapshot
}

function Get-SchemaShapeText {
    param($Schema)

    if ($null -eq $Schema) { return '' }
    $shape = [ordered]@{}
    foreach ($name in @(
        'type', 'format', '$ref', 'nullable', 'default',
        'minLength', 'maxLength', 'minimum', 'maximum',
        'exclusiveMinimum', 'exclusiveMaximum', 'minItems', 'maxItems',
        'pattern', 'enum')) {
        $value = Get-PropertyValue $Schema $name
        if ($null -ne $value) { $shape[$name] = $value }
    }
    $items = Get-PropertyValue $Schema 'items'
    if ($null -ne $items) { $shape['items'] = Get-SchemaShapeText $items }
    $additionalProperties = Get-PropertyValue $Schema 'additionalProperties'
    if ($null -ne $additionalProperties) {
        $shape['additionalProperties'] = if ($additionalProperties -is [bool]) {
            $additionalProperties
        } else {
            Get-SchemaShapeText $additionalProperties
        }
    }
    foreach ($composition in @('allOf', 'anyOf', 'oneOf')) {
        $nodes = Get-PropertyValue $Schema $composition
        if ($null -ne $nodes) { $shape[$composition] = @($nodes | ForEach-Object { Get-SchemaShapeText $_ }) }
    }
    return ($shape | ConvertTo-Json -Depth 12 -Compress)
}

function Compare-ContentContracts {
    param(
        $BeforeContent,
        $AfterContent,
        [string]$Location,
        [string]$ContractKind,
        [System.Collections.Generic.List[object]]$Changes
    )

    foreach ($beforeMedia in Get-Properties $BeforeContent) {
        $afterMedia = Get-Properties $AfterContent | Where-Object Name -eq $beforeMedia.Name | Select-Object -First 1
        $mediaLocation = "${Location}::$($beforeMedia.Name)"
        if ($null -eq $afterMedia) {
            Add-Change $Changes 'breaking' "removed-$ContractKind-media-type" $mediaLocation 'Documented media type was removed.'
            continue
        }
        $beforeShape = Get-SchemaShapeText (Get-PropertyValue $beforeMedia.Value 'schema')
        $afterShape = Get-SchemaShapeText (Get-PropertyValue $afterMedia.Value 'schema')
        if ($beforeShape -ne $afterShape) {
            Add-Change $Changes 'breaking' "changed-$ContractKind-schema" $mediaLocation "Schema shape changed from '$beforeShape' to '$afterShape'."
        }
    }
    foreach ($afterMedia in Get-Properties $AfterContent) {
        if (@(Get-Properties $BeforeContent | Where-Object Name -eq $afterMedia.Name).Count -eq 0) {
            Add-Change $Changes 'additive' "added-$ContractKind-media-type" "${Location}::$($afterMedia.Name)" 'Documented media type was added.'
        }
    }
}

function Compare-HeaderContracts {
    param(
        $BeforeHeaders,
        $AfterHeaders,
        [string]$Location,
        [System.Collections.Generic.List[object]]$Changes
    )

    foreach ($beforeHeader in Get-Properties $BeforeHeaders) {
        $afterHeader = Get-Properties $AfterHeaders | Where-Object Name -eq $beforeHeader.Name | Select-Object -First 1
        $headerLocation = "${Location}::header.$($beforeHeader.Name)"
        if ($null -eq $afterHeader) {
            Add-Change $Changes 'breaking' 'removed-response-header' $headerLocation 'Documented response header was removed.'
            continue
        }
        $beforeShape = Get-SchemaShapeText (Get-PropertyValue $beforeHeader.Value 'schema')
        $afterShape = Get-SchemaShapeText (Get-PropertyValue $afterHeader.Value 'schema')
        if ($beforeShape -ne $afterShape) {
            Add-Change $Changes 'breaking' 'changed-response-header' $headerLocation "Header schema changed from '$beforeShape' to '$afterShape'."
        }
    }
    foreach ($afterHeader in Get-Properties $AfterHeaders) {
        if (@(Get-Properties $BeforeHeaders | Where-Object Name -eq $afterHeader.Name).Count -eq 0) {
            Add-Change $Changes 'additive' 'added-response-header' "${Location}::header.$($afterHeader.Name)" 'Documented response header was added.'
        }
    }
}

function Compare-PayloadKeySets {
    param(
        $BeforeNode,
        $AfterNode,
        [string]$Location,
        [System.Collections.Generic.List[object]]$Changes
    )

    if ($null -eq $BeforeNode -or $null -eq $AfterNode) { return }
    foreach ($beforeProperty in Get-Properties $BeforeNode) {
        $afterProperty = Get-Properties $AfterNode |
            Where-Object Name -eq $beforeProperty.Name |
            Select-Object -First 1
        if ($null -eq $afterProperty) { continue }
        $propertyLocation = if ([string]::IsNullOrWhiteSpace($Location)) {
            $beforeProperty.Name
        } else {
            "$Location.$($beforeProperty.Name)"
        }
        if ($beforeProperty.Name -match '(^keys$|Keys$)') {
            $beforeKeys = @($beforeProperty.Value | Where-Object { $_ -is [string] })
            $afterKeys = @($afterProperty.Value | Where-Object { $_ -is [string] })
            foreach ($key in $beforeKeys) {
                if ($key -notin $afterKeys) {
                    Add-Change $Changes 'breaking' 'removed-payload-property' "$propertyLocation.$key" 'Serialized response property was removed.'
                }
            }
            foreach ($key in $afterKeys) {
                if ($key -notin $beforeKeys) {
                    Add-Change $Changes 'additive' 'added-payload-property' "$propertyLocation.$key" 'Serialized response property was added.'
                }
            }
            continue
        }
        if ($beforeProperty.Value -is [System.Management.Automation.PSCustomObject]) {
            Compare-PayloadKeySets $beforeProperty.Value $afterProperty.Value $propertyLocation $Changes
        }
    }
}

function Get-HttpDtoProperties {
    param([string]$Content, [string]$Path)

    if ([string]::IsNullOrWhiteSpace($Content)) { return [pscustomobject]@{} }
    $project = Join-Path $PSScriptRoot 'roslyn-extractor/LlmWiki.RoslynExtractor.csproj'
    $program = Join-Path $PSScriptRoot 'roslyn-extractor/Program.cs'
    $extractorDll = Join-Path $PSScriptRoot 'roslyn-extractor/bin/Release/net10.0/LlmWiki.RoslynExtractor.dll'
    if (-not (Test-Path -LiteralPath $extractorDll -PathType Leaf) -or
        (Get-Item -LiteralPath $project).LastWriteTimeUtc -gt (Get-Item -LiteralPath $extractorDll).LastWriteTimeUtc -or
        (Get-Item -LiteralPath $program).LastWriteTimeUtc -gt (Get-Item -LiteralPath $extractorDll).LastWriteTimeUtc) {
        & dotnet build $project -c Release --nologo --verbosity quiet | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "Unable to build the Roslyn HTTP DTO extractor (exit $LASTEXITCODE)." }
    }
    $inputJson = [pscustomobject]@{
        sources = @([pscustomobject]@{ id = 'source'; path = $Path; content = $Content })
    } | ConvertTo-Json -Depth 5 -Compress
    $extractorOutput = $inputJson | & dotnet $extractorDll --http-dto-stdin
    if ($LASTEXITCODE -ne 0) { throw "Roslyn HTTP DTO extraction failed for '$Path' with exit code $LASTEXITCODE." }
    $result = ($extractorOutput -join [Environment]::NewLine) | ConvertFrom-Json
    return $result.source
}

function Compare-HttpDtoContent {
    param(
        [string]$BeforeContent,
        [string]$AfterContent,
        [string]$Path,
        [System.Collections.Generic.List[object]]$Changes
    )

    $beforeRecords = Get-HttpDtoProperties $BeforeContent $Path
    $afterRecords = Get-HttpDtoProperties $AfterContent $Path
    foreach ($beforeRecord in Get-Properties $beforeRecords) {
        $afterRecord = Get-Properties $afterRecords | Where-Object Name -eq $beforeRecord.Name | Select-Object -First 1
        if ($null -eq $afterRecord) {
            Add-Change $Changes 'breaking' 'removed-http-dto' "${Path}::$($beforeRecord.Name)" 'Public HTTP DTO was removed.'
            continue
        }
        foreach ($beforeProperty in Get-Properties $beforeRecord.Value) {
            $afterProperty = Get-Properties $afterRecord.Value | Where-Object Name -eq $beforeProperty.Name | Select-Object -First 1
            $location = "${Path}::$($beforeRecord.Name).$($beforeProperty.Name)"
            if ($null -eq $afterProperty) {
                Add-Change $Changes 'breaking' 'removed-http-dto-property' $location 'Serialized HTTP DTO property was removed.'
            } elseif ($beforeProperty.Value.type -ne $afterProperty.Value.type) {
                Add-Change $Changes 'breaking' 'changed-http-dto-property' $location "HTTP DTO property type changed from '$($beforeProperty.Value.type)' to '$($afterProperty.Value.type)'."
            } elseif ($beforeProperty.Value.optional -and -not $afterProperty.Value.optional) {
                Add-Change $Changes 'breaking' 'required-http-dto-property' $location 'HTTP DTO property became required.'
            }
        }
        foreach ($afterProperty in Get-Properties $afterRecord.Value) {
            if (@(Get-Properties $beforeRecord.Value | Where-Object Name -eq $afterProperty.Name).Count -gt 0) { continue }
            $location = "${Path}::$($beforeRecord.Name).$($afterProperty.Name)"
            if ($afterProperty.Value.optional) {
                Add-Change $Changes 'additive' 'added-http-dto-property' $location 'Optional serialized HTTP DTO property was added.'
            } else {
                Add-Change $Changes 'breaking' 'added-required-http-dto-property' $location 'Required serialized HTTP DTO property was added.'
            }
        }
    }
    foreach ($afterRecord in Get-Properties $afterRecords) {
        if (@(Get-Properties $beforeRecords | Where-Object Name -eq $afterRecord.Name).Count -eq 0) {
            Add-Change $Changes 'additive' 'added-http-dto' "${Path}::$($afterRecord.Name)" 'Public HTTP DTO was added.'
        }
    }
}

if (-not $PSBoundParameters.ContainsKey('CurrentSnapshotContent') -and -not (Test-Path -LiteralPath $absoluteSnapshotPath)) {
    throw "OpenAPI snapshot not found: $SnapshotPath"
}

$currentText = if ($PSBoundParameters.ContainsKey('CurrentSnapshotContent')) {
    $CurrentSnapshotContent
} else {
    Get-Content -LiteralPath $absoluteSnapshotPath -Raw
}
$baseText = if ($PSBoundParameters.ContainsKey('BaseSnapshotContent')) {
    $BaseSnapshotContent
} else {
    $gitText = git show "${BaseRef}:$SnapshotPath" 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw "Unable to read '$SnapshotPath' from '$BaseRef'."
    }
    $gitText -join [Environment]::NewLine
}

$beforeSource = $baseText | ConvertFrom-Json
$afterSource = $currentText | ConvertFrom-Json
$beforeHasEndpoints = $null -ne (Get-PropertyValue $beforeSource 'Endpoints')
$afterHasEndpoints = $null -ne (Get-PropertyValue $afterSource 'Endpoints')
$snapshotFormat = if ($beforeHasEndpoints -or $afterHasEndpoints) {
    'endpoint-contract'
} else {
    'openapi'
}
$before = ConvertTo-ComparableOpenApi $beforeSource
$after = ConvertTo-ComparableOpenApi $afterSource
$changes = [System.Collections.Generic.List[object]]::new()
$httpMethods = @('get', 'put', 'post', 'delete', 'options', 'head', 'patch', 'trace')

foreach ($pathProperty in Get-Properties $before.paths) {
    $path = $pathProperty.Name
    $afterPathProperty = Get-Properties $after.paths | Where-Object Name -eq $path | Select-Object -First 1
    if ($null -eq $afterPathProperty) {
        Add-Change $changes 'breaking' 'removed-path' $path 'Public API path was removed.'
        continue
    }
    foreach ($methodProperty in Get-Properties $pathProperty.Value) {
        if ($methodProperty.Name -notin $httpMethods) { continue }
        $method = $methodProperty.Name
        $afterMethodProperty = Get-Properties $afterPathProperty.Value |
            Where-Object Name -eq $method |
            Select-Object -First 1
        if ($null -eq $afterMethodProperty) {
            Add-Change $changes 'breaking' 'removed-operation' "$($method.ToUpperInvariant()) $path" 'Public API operation was removed.'
            continue
        }

        $beforeParameters = @($methodProperty.Value.parameters)
        $afterParameters = @($afterMethodProperty.Value.parameters)
        foreach ($afterParameter in $afterParameters) {
            $beforeParameter = @($beforeParameters | Where-Object {
                $_.name -eq $afterParameter.name -and $_.in -eq $afterParameter.in
            } | Select-Object -First 1)
            $location = "$($method.ToUpperInvariant()) ${path}::$($afterParameter.in).$($afterParameter.name)"
            if ($beforeParameter.Count -eq 0) {
                if ($afterParameter.required) {
                    Add-Change $changes 'breaking' 'added-required-parameter' $location 'Required parameter was added.'
                } else {
                    Add-Change $changes 'additive' 'added-optional-parameter' $location 'Optional parameter was added.'
                }
                continue
            }
            $beforeParameter = $beforeParameter[0]
            if (-not $beforeParameter.required -and $afterParameter.required) {
                Add-Change $changes 'breaking' 'required-parameter' $location 'Parameter became required.'
            }
            $beforeShape = Get-SchemaShapeText $beforeParameter.schema
            $afterShape = Get-SchemaShapeText $afterParameter.schema
            if ($beforeShape -ne $afterShape) {
                Add-Change $changes 'breaking' 'changed-parameter' $location "Parameter shape changed from '$beforeShape' to '$afterShape'."
            }
        }
        foreach ($beforeParameter in $beforeParameters) {
            $stillPresent = @($afterParameters | Where-Object {
                $_.name -eq $beforeParameter.name -and $_.in -eq $beforeParameter.in
            }).Count -gt 0
            if (-not $stillPresent) {
                $location = "$($method.ToUpperInvariant()) ${path}::$($beforeParameter.in).$($beforeParameter.name)"
                Add-Change $changes 'breaking' 'removed-parameter' $location 'Public API parameter was removed.'
            }
        }

        $beforeRequestBody = Get-PropertyValue $methodProperty.Value 'requestBody'
        $afterRequestBody = Get-PropertyValue $afterMethodProperty.Value 'requestBody'
        $operationLocation = "$($method.ToUpperInvariant()) $path"
        if ($null -ne $beforeRequestBody -and $null -eq $afterRequestBody) {
            Add-Change $changes 'breaking' 'removed-request-body' $operationLocation 'Documented request body was removed.'
        } elseif ($null -eq $beforeRequestBody -and $null -ne $afterRequestBody) {
            if ([bool](Get-PropertyValue $afterRequestBody 'required')) {
                Add-Change $changes 'breaking' 'added-required-request-body' $operationLocation 'A required request body was added.'
            } else {
                Add-Change $changes 'additive' 'added-optional-request-body' $operationLocation 'An optional request body was added.'
            }
        } elseif ($null -ne $beforeRequestBody -and $null -ne $afterRequestBody) {
            if (-not [bool](Get-PropertyValue $beforeRequestBody 'required') -and
                [bool](Get-PropertyValue $afterRequestBody 'required')) {
                Add-Change $changes 'breaking' 'required-request-body' $operationLocation 'Request body became required.'
            }
            Compare-ContentContracts `
                (Get-PropertyValue $beforeRequestBody 'content') `
                (Get-PropertyValue $afterRequestBody 'content') `
                "${operationLocation}::request" `
                'request' `
                $changes
        }

        foreach ($responseProperty in Get-Properties $methodProperty.Value.responses) {
            $afterResponseProperty = Get-Properties $afterMethodProperty.Value.responses |
                Where-Object Name -eq $responseProperty.Name |
                Select-Object -First 1
            $stillPresent = @(
                $afterResponseProperty
            ).Count -gt 0
            if (-not $stillPresent) {
                Add-Change $changes 'breaking' 'removed-response' "$($method.ToUpperInvariant()) $path" "Documented response '$($responseProperty.Name)' was removed."
                continue
            }
            $responseLocation = "${operationLocation}::response.$($responseProperty.Name)"
            Compare-ContentContracts `
                (Get-PropertyValue $responseProperty.Value 'content') `
                (Get-PropertyValue $afterResponseProperty.Value 'content') `
                $responseLocation `
                'response' `
                $changes
            Compare-HeaderContracts `
                (Get-PropertyValue $responseProperty.Value 'headers') `
                (Get-PropertyValue $afterResponseProperty.Value 'headers') `
                $responseLocation `
                $changes
        }
        foreach ($responseProperty in Get-Properties $afterMethodProperty.Value.responses) {
            $wasPresent = @(
                Get-Properties $methodProperty.Value.responses |
                    Where-Object Name -eq $responseProperty.Name
            ).Count -gt 0
            if (-not $wasPresent) {
                if ([string]$responseProperty.Name -eq '413') {
                    Add-Change $changes 'behavioral-restriction' 'added-request-size-restriction' "$($method.ToUpperInvariant()) $path" "Documented response '413' was added; the schema is additive, but previously accepted request sizes may now be rejected." 'behavioral'
                } else {
                    Add-Change $changes 'additive' 'added-response' "$($method.ToUpperInvariant()) $path" "Documented response '$($responseProperty.Name)' was added."
                }
            }
        }
    }
}

foreach ($pathProperty in Get-Properties $after.paths) {
    $path = $pathProperty.Name
    $existed = @(Get-Properties $before.paths | Where-Object Name -eq $path).Count -gt 0
    if (-not $existed) {
        Add-Change $changes 'additive' 'added-path' $path 'Public API path was added.'
        continue
    }
    $beforePath = (Get-Properties $before.paths | Where-Object Name -eq $path | Select-Object -First 1).Value
    foreach ($methodProperty in Get-Properties $pathProperty.Value) {
        if ($methodProperty.Name -notin $httpMethods) { continue }
        if (@(Get-Properties $beforePath | Where-Object Name -eq $methodProperty.Name).Count -eq 0) {
            Add-Change $changes 'additive' 'added-operation' "$($methodProperty.Name.ToUpperInvariant()) $path" 'Public API operation was added.'
        }
    }
}

$beforeSchemas = $before.components.schemas
$afterSchemas = $after.components.schemas
foreach ($schemaProperty in Get-Properties $beforeSchemas) {
    $schemaName = $schemaProperty.Name
    $afterSchemaProperty = Get-Properties $afterSchemas | Where-Object Name -eq $schemaName | Select-Object -First 1
    if ($null -eq $afterSchemaProperty) {
        Add-Change $changes 'breaking' 'removed-schema' $schemaName 'Public component schema was removed.'
        continue
    }
    $beforeSchema = $schemaProperty.Value
    $afterSchema = $afterSchemaProperty.Value
    foreach ($property in Get-Properties $beforeSchema.properties) {
        $afterProperty = Get-Properties $afterSchema.properties | Where-Object Name -eq $property.Name | Select-Object -First 1
        if ($null -eq $afterProperty) {
            Add-Change $changes 'breaking' 'removed-schema-property' "$schemaName.$($property.Name)" 'Schema property was removed.'
            continue
        }
        $beforeItems = Get-PropertyValue $property.Value 'items'
        $afterItems = Get-PropertyValue $afterProperty.Value 'items'
        $beforeShape = "$(Get-PropertyValue $property.Value 'type')|$(Get-PropertyValue $property.Value 'format')|$(Get-PropertyValue $property.Value '$ref')|nullable=$(Get-PropertyValue $property.Value 'nullable')|items=$(Get-PropertyValue $beforeItems 'type'):$(Get-PropertyValue $beforeItems '$ref')"
        $afterShape = "$(Get-PropertyValue $afterProperty.Value 'type')|$(Get-PropertyValue $afterProperty.Value 'format')|$(Get-PropertyValue $afterProperty.Value '$ref')|nullable=$(Get-PropertyValue $afterProperty.Value 'nullable')|items=$(Get-PropertyValue $afterItems 'type'):$(Get-PropertyValue $afterItems '$ref')"
        if ($beforeShape -ne $afterShape) {
            Add-Change $changes 'breaking' 'changed-schema-property' "$schemaName.$($property.Name)" "Property shape changed from '$beforeShape' to '$afterShape'."
        }
    }
    $beforeRequired = @($beforeSchema.required)
    $afterRequired = @($afterSchema.required)
    foreach ($property in Get-Properties $afterSchema.properties) {
        $existed = @(Get-Properties $beforeSchema.properties | Where-Object Name -eq $property.Name).Count -gt 0
        if (-not $existed -and $property.Name -notin $afterRequired) {
            Add-Change $changes 'additive' 'added-schema-property' "$schemaName.$($property.Name)" 'Optional schema property was added.'
        }
    }
    foreach ($requiredProperty in $afterRequired) {
        if ($requiredProperty -notin $beforeRequired) {
            Add-Change $changes 'breaking' 'added-required-property' "$schemaName.$requiredProperty" 'Schema property became required.'
        }
    }
}
foreach ($schemaProperty in Get-Properties $afterSchemas) {
    if (@(Get-Properties $beforeSchemas | Where-Object Name -eq $schemaProperty.Name).Count -eq 0) {
        Add-Change $changes 'additive' 'added-schema' $schemaProperty.Name 'Public component schema was added.'
    }
}

$comparePayloadSnapshots = (
    (-not $PSBoundParameters.ContainsKey('BaseSnapshotContent') -and -not $PSBoundParameters.ContainsKey('CurrentSnapshotContent')) -or
    $PSBoundParameters.ContainsKey('BasePayloadSnapshotContent') -or
    $PSBoundParameters.ContainsKey('CurrentPayloadSnapshotContent')
)
if ($comparePayloadSnapshots) {
    if (-not $PSBoundParameters.ContainsKey('CurrentPayloadSnapshotContent') -and -not (Test-Path -LiteralPath $absolutePayloadSnapshotPath)) {
        throw "Payload contract snapshot not found: $PayloadSnapshotPath"
    }
    $currentPayloadText = if ($PSBoundParameters.ContainsKey('CurrentPayloadSnapshotContent')) {
        $CurrentPayloadSnapshotContent
    } else {
        Get-Content -LiteralPath $absolutePayloadSnapshotPath -Raw
    }
    $basePayloadText = if ($PSBoundParameters.ContainsKey('BasePayloadSnapshotContent')) {
        $BasePayloadSnapshotContent
    } else {
        $gitPayloadText = git show "${BaseRef}:$PayloadSnapshotPath" 2>$null
        if ($LASTEXITCODE -ne 0) {
            throw "Unable to read '$PayloadSnapshotPath' from '$BaseRef'."
        }
        $gitPayloadText -join [Environment]::NewLine
    }
    Compare-PayloadKeySets `
        ($basePayloadText | ConvertFrom-Json) `
        ($currentPayloadText | ConvertFrom-Json) `
        'payload' `
        $changes
}

$httpDtoPaths = [System.Collections.Generic.List[string]]::new()
$compareHttpDtos = (
    (-not $PSBoundParameters.ContainsKey('BaseSnapshotContent') -and -not $PSBoundParameters.ContainsKey('CurrentSnapshotContent')) -or
    $PSBoundParameters.ContainsKey('BaseHttpDtoContent') -or
    $PSBoundParameters.ContainsKey('CurrentHttpDtoContent')
)
if ($compareHttpDtos) {
    if ($PSBoundParameters.ContainsKey('BaseHttpDtoContent') -or $PSBoundParameters.ContainsKey('CurrentHttpDtoContent')) {
        Compare-HttpDtoContent $BaseHttpDtoContent $CurrentHttpDtoContent $HttpDtoPath $changes
        $httpDtoPaths.Add($HttpDtoPath)
    } else {
        $changedDtoPaths = @(
            git -C $repositoryRoot diff --name-only --diff-filter=ACMRD $BaseRef -- 'FoodDiary.Presentation.Api/**/*.cs' |
                Where-Object { $_ -match 'Http(?:Model|Request|Response)\.cs$' } |
                Sort-Object -Unique
        )
        if ($LASTEXITCODE -ne 0) { throw "Unable to collect changed HTTP DTO paths from '$BaseRef'." }
        foreach ($path in $changedDtoPaths) {
            $baseDtoLines = @(git -C $repositoryRoot show "${BaseRef}:$path" 2>$null)
            $baseDtoText = if ($LASTEXITCODE -eq 0) { $baseDtoLines -join [Environment]::NewLine } else { '' }
            $absoluteDtoPath = Join-Path $repositoryRoot $path
            $currentDtoText = if (Test-Path -LiteralPath $absoluteDtoPath) {
                Get-Content -LiteralPath $absoluteDtoPath -Raw
            } else {
                ''
            }
            Compare-HttpDtoContent $baseDtoText $currentDtoText $path $changes
            $httpDtoPaths.Add($path)
        }
    }
}

$breakingChanges = @($changes | Where-Object severity -eq 'breaking')
$additiveChanges = @($changes | Where-Object severity -eq 'additive')
$behavioralRestrictions = @($changes | Where-Object severity -eq 'behavioral-restriction')
$result = [pscustomobject]@{
    baseRef = $BaseRef
    snapshotPath = $SnapshotPath
    payloadSnapshotPath = $PayloadSnapshotPath
    snapshotFormat = $snapshotFormat
    httpDtoPaths = @($httpDtoPaths)
    breakingCount = $breakingChanges.Count
    additiveCount = $additiveChanges.Count
    behavioralRestrictionCount = $behavioralRestrictions.Count
    breakingChanges = $breakingChanges
    additiveChanges = $additiveChanges
    behavioralRestrictions = $behavioralRestrictions
    structuralCompatibility = [pscustomobject]@{
        breakingCount = $breakingChanges.Count
        additiveCount = $additiveChanges.Count
    }
    behavioralCompatibility = [pscustomobject]@{
        restrictionCount = $behavioralRestrictions.Count
        status = $(if ($behavioralRestrictions.Count -gt 0) { 'review-required' } else { 'compatible' })
    }
    changes = @($changes)
}

if ($Format -eq 'Json') {
    $result | ConvertTo-Json -Depth 8
} else {
    Write-Host "API compatibility: $($result.breakingCount) structural breaking, $($result.additiveCount) structural additive, $($result.behavioralRestrictionCount) behavioral restriction(s)."
    foreach ($change in $changes) {
        Write-Host " - [$($change.severity)] $($change.kind): $($change.location) - $($change.description)"
    }
}

if ($FailOnBreaking -and $result.breakingCount -gt 0) {
    exit 1
}
