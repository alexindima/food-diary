[CmdletBinding()]
param(
    [string]$BaseRef = 'HEAD',
    [string]$SnapshotPath = 'tests/FoodDiary.Web.Api.IntegrationTests/Snapshots/openapi-full-contract.json',
    [string]$PayloadSnapshotPath = 'tests/FoodDiary.Web.Api.IntegrationTests/Snapshots/payload-contract-snapshots.json',
    [string]$BaseSnapshotContent,
    [string]$CurrentSnapshotContent,
    [string]$BasePayloadSnapshotContent,
    [string]$CurrentPayloadSnapshotContent,
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

function Add-Change {
    param(
        [System.Collections.Generic.List[object]]$List,
        [string]$Severity,
        [string]$Kind,
        [string]$Location,
        [string]$Description
    )
    $List.Add([pscustomobject]@{
        severity = $Severity
        kind = $Kind
        location = $Location
        description = $Description
    })
}

function ConvertTo-ComparableOpenApi {
    param($Snapshot)

    if ($null -ne $Snapshot.Endpoints) {
        $paths = [ordered]@{}
        foreach ($endpoint in @($Snapshot.Endpoints)) {
            $operations = [ordered]@{}
            foreach ($operation in @($endpoint.Operations)) {
                $responses = [ordered]@{}
                foreach ($responseCode in @($operation.ResponseCodes)) {
                    $responses[[string]$responseCode] = [ordered]@{}
                }
                $operations[[string]$operation.Method.ToLowerInvariant()] = [ordered]@{
                    parameters = @()
                    responses = $responses
                }
            }
            $paths[[string]$endpoint.Path] = $operations
        }

        return ([ordered]@{
            paths = $paths
            components = [ordered]@{ schemas = [ordered]@{} }
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
$snapshotFormat = if ($null -ne $beforeSource.Endpoints -or $null -ne $afterSource.Endpoints) {
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
            if (-not $afterParameter.required) { continue }
            $wasPresent = @($beforeParameters | Where-Object {
                $_.name -eq $afterParameter.name -and $_.in -eq $afterParameter.in
            }).Count -gt 0
            if (-not $wasPresent) {
                Add-Change $changes 'breaking' 'added-required-parameter' "$($method.ToUpperInvariant()) $path" "Required $($afterParameter.in) parameter '$($afterParameter.name)' was added."
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
                Add-Change $changes 'additive' 'added-response' "$($method.ToUpperInvariant()) $path" "Documented response '$($responseProperty.Name)' was added."
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
        $beforeShape = "$($property.Value.type)|$($property.Value.format)|$($property.Value.'$ref')|nullable=$($property.Value.nullable)|items=$($property.Value.items.type):$($property.Value.items.'$ref')"
        $afterShape = "$($afterProperty.Value.type)|$($afterProperty.Value.format)|$($afterProperty.Value.'$ref')|nullable=$($afterProperty.Value.nullable)|items=$($afterProperty.Value.items.type):$($afterProperty.Value.items.'$ref')"
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

$result = [pscustomobject]@{
    baseRef = $BaseRef
    snapshotPath = $SnapshotPath
    payloadSnapshotPath = $PayloadSnapshotPath
    snapshotFormat = $snapshotFormat
    breakingCount = @($changes | Where-Object severity -eq 'breaking').Count
    additiveCount = @($changes | Where-Object severity -eq 'additive').Count
    changes = @($changes)
}

if ($Format -eq 'Json') {
    $result | ConvertTo-Json -Depth 8
} else {
    Write-Host "API compatibility: $($result.breakingCount) breaking, $($result.additiveCount) additive change(s)."
    foreach ($change in $changes) {
        Write-Host " - [$($change.severity)] $($change.kind): $($change.location) - $($change.description)"
    }
}

if ($FailOnBreaking -and $result.breakingCount -gt 0) {
    exit 1
}
