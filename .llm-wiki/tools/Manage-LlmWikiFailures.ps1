[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('search', 'add', 'validate')]
    [string]$Action = 'search',
    [string]$Query,
    [string]$Id,
    [string]$Symptom,
    [string]$Cause,
    [string]$Fix,
    [string[]]$PathPattern = @(),
    [string[]]$Verification = @()
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$knowledgePath = Join-Path $wikiRoot 'knowledge/failures.json'

function Read-Knowledge {
    $data = Get-Content -LiteralPath $knowledgePath -Raw | ConvertFrom-Json
    if ($data.schemaVersion -ne 1 -or $null -eq $data.entries) {
        throw 'Unsupported or invalid failure knowledge schema.'
    }
    return $data
}

function Write-Knowledge {
    param($Data)
    $json = $Data | ConvertTo-Json -Depth 8
    $utf8WithoutBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($knowledgePath, ($json + [Environment]::NewLine), $utf8WithoutBom)
}

switch ($Action) {
    'validate' {
        $data = Read-Knowledge
        $duplicateIds = @($data.entries | Group-Object id | Where-Object Count -gt 1)
        if ($duplicateIds.Count -gt 0) {
            throw "Duplicate failure IDs: $($duplicateIds.Name -join ', ')"
        }
        foreach ($entry in $data.entries) {
            foreach ($field in @('id', 'symptom', 'cause', 'fix')) {
                if ([string]::IsNullOrWhiteSpace([string]$entry.$field)) {
                    throw "Failure entry is missing '$field'."
                }
            }
            if (@($entry.verification).Count -eq 0) {
                throw "Failure entry '$($entry.id)' must include verification evidence."
            }
            foreach ($pattern in @($entry.pathPatterns)) {
                try { $null = [regex]::new($pattern) } catch {
                    throw "Failure entry '$($entry.id)' has invalid path regex '$pattern'."
                }
            }
        }
        Write-Host "Failure knowledge is valid: $(@($data.entries).Count) entries."
    }
    'add' {
        foreach ($required in @('Id', 'Symptom', 'Cause', 'Fix')) {
            if ([string]::IsNullOrWhiteSpace([string](Get-Variable $required -ValueOnly))) {
                throw "add requires -$required."
            }
        }
        $data = Read-Knowledge
        if (@($data.entries | Where-Object id -eq $Id).Count -gt 0) {
            throw "Failure ID already exists: $Id"
        }
        $data.entries = @($data.entries) + [pscustomobject]@{
            id = $Id
            symptom = $Symptom
            cause = $Cause
            fix = $Fix
            pathPatterns = @($PathPattern)
            verification = @($Verification)
        }
        $data.entries = @($data.entries | Sort-Object id)
        Write-Knowledge $data
        Write-Host "Added failure knowledge: $Id"
    }
    default {
        $data = Read-Knowledge
        $matches = @($(if ([string]::IsNullOrWhiteSpace($Query)) {
            @($data.entries)
        } else {
            @($data.entries | Where-Object {
                (($_ | ConvertTo-Json -Depth 5 -Compress) -match [regex]::Escape($Query))
            })
        }))
        if ($matches.Count -eq 0) {
            Write-Host "No known failures matched '$Query'."
            exit 0
        }
        foreach ($entry in $matches) {
            Write-Host "$($entry.id): $($entry.symptom)"
            Write-Host "  Cause: $($entry.cause)"
            Write-Host "  Fix: $($entry.fix)"
            if (@($entry.verification).Count -gt 0) {
                Write-Host "  Verify: $($entry.verification -join '; ')"
            }
        }
    }
}
