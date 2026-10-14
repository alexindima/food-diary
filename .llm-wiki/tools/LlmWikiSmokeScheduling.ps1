function Get-LlmWikiSmokeDurationEstimates {
    param([string]$HistoryRoot, [int]$MaxConcurrency)
    $samples = @{}
    foreach ($file in @(Get-ChildItem -LiteralPath $HistoryRoot -File -Filter '*.timings.json' -ErrorAction SilentlyContinue | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 20)) {
        try {
            $run = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json
            if (-not $run.completed -or [int]$run.maxConcurrency -ne $MaxConcurrency) { continue }
            foreach ($group in $run.groups) {
                if ([int]$group.exitCode -ne 0 -or ($group.PSObject.Properties['executed'] -and -not $group.executed)) { continue }
                $duration = [double]$group.durationSeconds
                if ([double]::IsNaN($duration) -or [double]::IsInfinity($duration) -or $duration -le 0 -or $duration -gt 3600) { continue }
                $name = [string]$group.group
                if (-not $samples.ContainsKey($name)) { $samples[$name] = [Collections.Generic.List[double]]::new() }
                if ($samples[$name].Count -lt 5) { $samples[$name].Add($duration) }
            }
        } catch { }
    }
    $estimates = @{}
    foreach ($name in $samples.Keys) {
        $values = @($samples[$name] | Sort-Object)
        $middle = [int][Math]::Floor($values.Count / 2)
        $median = if ($values.Count % 2) { $values[$middle] } else { ($values[$middle-1] + $values[$middle]) / 2 }
        $estimates[$name] = [double]$median
    }
    return $estimates
}

function Get-LlmWikiSmokeSchedule {
    param([string[]]$Groups, [hashtable]$Priority, [hashtable]$Duration)
    # Partial history cannot predict new groups reliably. Keep the catalog order
    # until this entire independent lane has usable measurements.
    $useHistory = $Groups.Count -gt 0 -and @($Groups | Where-Object { -not $Duration.ContainsKey($_) }).Count -eq 0
    return @($Groups | Sort-Object @{ Expression = { if ($useHistory) { $Duration[$_] } elseif ($Priority.ContainsKey($_)) { $Priority[$_] } else { 0 } }; Descending = $true }, @{ Expression = { $_ } })
}
