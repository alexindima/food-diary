[CmdletBinding()]
param(
    [string]$BaseRef = 'HEAD',
    [string]$SnapshotPath = 'tests/FoodDiary.Web.Api.IntegrationTests/Snapshots/openapi-full-contract.json',
    [string]$BaseSnapshotContent,
    [string]$CurrentSnapshotContent,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text',
    [switch]$FailOnBreaking
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$absoluteSnapshotPath = Join-Path $repositoryRoot $SnapshotPath

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
        $beforeShape = "$($property.Value.type)|$($property.Value.format)|$($property.Value.'$ref')"
        $afterShape = "$($afterProperty.Value.type)|$($afterProperty.Value.format)|$($afterProperty.Value.'$ref')"
        if ($beforeShape -ne $afterShape) {
            Add-Change $changes 'breaking' 'changed-schema-property' "$schemaName.$($property.Name)" "Property shape changed from '$beforeShape' to '$afterShape'."
        }
    }
    $beforeRequired = @($beforeSchema.required)
    foreach ($requiredProperty in @($afterSchema.required)) {
        if ($requiredProperty -notin $beforeRequired) {
            Add-Change $changes 'breaking' 'added-required-property' "$schemaName.$requiredProperty" 'Schema property became required.'
        }
    }
}

$result = [pscustomobject]@{
    baseRef = $BaseRef
    snapshotPath = $SnapshotPath
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
