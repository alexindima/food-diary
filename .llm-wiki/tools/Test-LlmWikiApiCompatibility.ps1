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
    if ($null -eq $Object -or -not $Object.PSObject.Properties[$Name]) { return $null }
    $Object.PSObject.Properties[$Name].Value
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

function ConvertTo-ComparableOpenApi {
    param($Snapshot)

    if ($null -ne $Snapshot -and $Snapshot.PSObject.Properties['Endpoints'] -and $null -ne $Snapshot.Endpoints) {
        $paths = [ordered]@{}
        foreach ($endpoint in @($Snapshot.Endpoints)) {
            $operations = [ordered]@{}
            foreach ($operation in @($endpoint.Operations)) {
                $responses = [ordered]@{}
                foreach ($responseCode in @($operation.ResponseCodes)) {
                    $responses[[string]$responseCode] = [ordered]@{}
                }
                $queryParameters = if ($operation.PSObject.Properties['QueryParameters']) { @($operation.QueryParameters) } else { @() }
                $parameters = @($queryParameters | Where-Object { $null -ne $_ } | ForEach-Object {
                    [ordered]@{
                        name = [string]$_.Name
                        in = [string]$_.Location
                        required = [bool]$_.Required
                        schema = [ordered]@{
                            type = [string]$_.Type
                            format = $(if ($_.PSObject.Properties['Format']) { [string]$_.Format } else { '' })
                            default = $(if ($_.PSObject.Properties['Default']) { $_.Default } else { $null })
                        }
                    }
                })
                $operations[[string]$operation.Method.ToLowerInvariant()] = [ordered]@{
                    parameters = $parameters
                    responses = $responses
                }
            }
            $paths[[string]$endpoint.Path] = $operations
        }

        $schemas = [ordered]@{}
        $snapshotSchemas = if ($Snapshot.PSObject.Properties['Schemas']) { @($Snapshot.Schemas) } else { @() }
        foreach ($schema in @($snapshotSchemas | Where-Object { $null -ne $_ -and -not [string]::IsNullOrWhiteSpace([string]$_.Name) })) {
            $properties = [ordered]@{}
            $required = [System.Collections.Generic.List[string]]::new()
            $schemaProperties = if ($schema.PSObject.Properties['Properties']) { @($schema.Properties) } else { @() }
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
$beforeHasEndpoints = $null -ne $beforeSource -and $beforeSource.PSObject.Properties['Endpoints'] -and $null -ne $beforeSource.Endpoints
$afterHasEndpoints = $null -ne $afterSource -and $afterSource.PSObject.Properties['Endpoints'] -and $null -ne $afterSource.Endpoints
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
            $beforeShape = "$(Get-PropertyValue $beforeParameter.schema 'type')|$(Get-PropertyValue $beforeParameter.schema 'format')|default=$(Get-PropertyValue $beforeParameter.schema 'default')"
            $afterShape = "$(Get-PropertyValue $afterParameter.schema 'type')|$(Get-PropertyValue $afterParameter.schema 'format')|default=$(Get-PropertyValue $afterParameter.schema 'default')"
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

        foreach ($responseProperty in Get-Properties $methodProperty.Value.responses) {
            $stillPresent = @(
                Get-Properties $afterMethodProperty.Value.responses |
                    Where-Object Name -eq $responseProperty.Name
            ).Count -gt 0
            if (-not $stillPresent) {
                Add-Change $changes 'breaking' 'removed-response' "$($method.ToUpperInvariant()) $path" "Documented response '$($responseProperty.Name)' was removed."
            }
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
