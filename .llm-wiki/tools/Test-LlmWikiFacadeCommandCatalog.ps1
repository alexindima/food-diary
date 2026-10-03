[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$wikiPath = Join-Path (Split-Path -Parent $PSScriptRoot) 'wiki.ps1'
$tokens = $null
$parseErrors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile(
    $wikiPath,
    [ref]$tokens,
    [ref]$parseErrors)
if (@($parseErrors).Count -gt 0) {
    throw "Wiki facade has $(@($parseErrors).Count) parser error(s)."
}

$commandParameter = $ast.ParamBlock.Parameters | Where-Object {
    $_.Name.VariablePath.UserPath -eq 'Command'
} | Select-Object -First 1
$validateSet = $commandParameter.Attributes | Where-Object {
    $_.TypeName.Name -eq 'ValidateSet'
} | Select-Object -First 1
if ($null -eq $validateSet) {
    throw 'Wiki facade Command parameter must declare a ValidateSet command catalog.'
}
$declaredCommands = @($validateSet.PositionalArguments | ForEach-Object {
    [string]$_.SafeGetValue()
})

$commandSwitches = @($ast.FindAll({
    param($node)
    $node -is [Management.Automation.Language.SwitchStatementAst] -and
        $node.Condition.Extent.Text -eq '$Command'
}, $true))
if ($commandSwitches.Count -ne 1) {
    throw "Wiki facade must have exactly one command router switch; found $($commandSwitches.Count)."
}

$routedCommands = @(
    foreach ($clause in $commandSwitches[0].Clauses) {
        if ($clause.Item1 -is [Management.Automation.Language.StringConstantExpressionAst]) {
            [string]$clause.Item1.SafeGetValue()
            continue
        }
        $clause.Item1.FindAll({
            param($node)
            $node -is [Management.Automation.Language.StringConstantExpressionAst]
        }, $true) | ForEach-Object { [string]$_.SafeGetValue() }
    }
)

$duplicateDeclarations = @($declaredCommands | Group-Object | Where-Object Count -gt 1 | ForEach-Object Name)
$duplicateRoutes = @($routedCommands | Group-Object | Where-Object Count -gt 1 | ForEach-Object Name)
$missingRoutes = @($declaredCommands | Where-Object {
    $_ -ne 'help' -and $_ -notin $routedCommands
})
$undeclaredRoutes = @($routedCommands | Where-Object { $_ -notin $declaredCommands })
$indexCommandAssignment = $ast.FindAll({
    param($node)
    $node -is [Management.Automation.Language.AssignmentStatementAst] -and
        $node.Left.Extent.Text -eq '$indexCommandTools'
}, $true) | Select-Object -First 1
$indexCommandMap = if ($null -eq $indexCommandAssignment) {
    $null
} else {
    $indexCommandAssignment.Right.FindAll({
        param($node)
        $node -is [Management.Automation.Language.HashtableAst]
    }, $true) | Select-Object -First 1
}
$invalidIndexMappings = @()
if ($null -eq $indexCommandMap) {
    $invalidIndexMappings = @('<missing index command map>')
} else {
    $invalidIndexMappings = @($indexCommandMap.KeyValuePairs | ForEach-Object {
        $commandName = [string]$_.Item1.SafeGetValue()
        $toolName = [string]$_.Item2.SafeGetValue()
        if ($commandName -notin $declaredCommands -or $commandName -notin $routedCommands -or
            -not (Test-Path -LiteralPath (Join-Path $PSScriptRoot $toolName) -PathType Leaf)) {
            "$commandName=$toolName"
        }
    })
}
if ($duplicateDeclarations.Count -gt 0 -or $duplicateRoutes.Count -gt 0 -or
    $missingRoutes.Count -gt 0 -or $undeclaredRoutes.Count -gt 0 -or
    $invalidIndexMappings.Count -gt 0) {
    throw @"
Wiki facade command catalog is inconsistent.
Duplicate declarations: $($duplicateDeclarations -join ', ')
Duplicate routes: $($duplicateRoutes -join ', ')
Missing routes: $($missingRoutes -join ', ')
Undeclared routes: $($undeclaredRoutes -join ', ')
Invalid index mappings: $($invalidIndexMappings -join ', ')
"@
}

$compactHelp = @(& $wikiPath help 6>&1 | ForEach-Object { [string]$_ })
$detailedHelp = @(& $wikiPath help -Detailed 6>&1 | ForEach-Object { [string]$_ })
$registryPath = Join-Path (Split-Path -Parent $PSScriptRoot) 'policies/command-registry.json'
$registry = Get-Content -LiteralPath $registryPath -Raw | ConvertFrom-Json
$registeredCommands = @($registry.tiers | ForEach-Object {
    if ($_.PSObject.Properties['commands']) { @($_.commands) }
} | Where-Object { $_ } | Sort-Object -Unique)
$registryCommandOccurrences = @($registry.tiers | ForEach-Object { @($_.commands) } | Where-Object { $_ })
$duplicateRegistryCommands = @($registryCommandOccurrences | Group-Object | Where-Object Count -ne 1 | ForEach-Object Name)
$missingRegistryCommands = @($declaredCommands | Where-Object { $_ -notin $registeredCommands })
$unknownRegistryCommands = @($registeredCommands | Where-Object { $_ -notin $declaredCommands })
$tierIds = @($registry.tiers.id)
$compactCommandLines = @($compactHelp | Where-Object { $_ -match '^\s+\.\/\.llm-wiki\/wiki\.ps1 ' })
$detailedCommandLines = @($detailedHelp | Where-Object { $_ -match '^\s+\.\/\.llm-wiki\/wiki\.ps1 ' })
if ($compactCommandLines.Count -lt 8 -or $compactCommandLines.Count -gt 15 -or
    $compactHelp -notcontains 'Administrative and compatibility commands:' -or
    $compactHelp -notcontains '  ./.llm-wiki/wiki.ps1 help -Detailed' -or
    $detailedHelp -notcontains 'Detailed command catalog:' -or
    $compactHelp -notcontains 'Command stability tiers: core, governed, experimental.' -or
    [int]$registry.schemaVersion -ne 2 -or
    @($tierIds | Sort-Object -Unique).Count -ne 3 -or
    $duplicateRegistryCommands.Count -gt 0 -or
    $missingRegistryCommands.Count -gt 0 -or
    $unknownRegistryCommands.Count -gt 0 -or
    $detailedCommandLines.Count -le $compactCommandLines.Count) {
    throw @"
Wiki facade help tiers are inconsistent.
Compact command lines: $($compactCommandLines.Count)
Detailed command lines: $($detailedCommandLines.Count)
Duplicate registry commands: $($duplicateRegistryCommands -join ', ')
Missing registry commands: $($missingRegistryCommands -join ', ')
Unknown registry commands: $($unknownRegistryCommands -join ', ')
"@
}

$healthClause = $commandSwitches[0].Clauses | Where-Object { $_.Item1.Extent.Text -eq "'health'" } | Select-Object -First 1
$fullVerificationClause = $commandSwitches[0].Clauses | Where-Object { $_.Item1.Extent.Text -eq "'verify-full'" } | Select-Object -First 1
$fullVerificationTry = $fullVerificationClause.Item2.FindAll({
    param($node)
    $node -is [Management.Automation.Language.TryStatementAst]
}, $true) | Select-Object -First 1
if ($null -eq $fullVerificationTry) { throw 'Full verification must preserve its guarded failure contract.' }
$verificationBodyText = $fullVerificationTry.Body.Extent.Text
$verificationBody = [scriptblock]::Create($verificationBodyText.Substring(1, $verificationBodyText.Length - 2))
foreach ($scenario in @('Focused', 'Core', 'Full', 'default-concurrency', 'policy-failed', 'impact-failed')) {
    & {
        $calls = [Collections.Generic.List[object]]::new()
        function Invoke-WikiTool([string]$Name, [hashtable]$Arguments) {
            $calls.Add([pscustomobject]@{ Name = $Name; Arguments = $Arguments })
            if (($scenario -eq 'policy-failed' -and $Name -eq 'Test-LlmWikiChangePolicy.ps1') -or
                ($scenario -eq 'impact-failed' -and $Name -eq 'Get-LlmWikiImpact.ps1')) {
                throw 'Expected verification gate failure.'
            }
        }
        $MaxConcurrency = if ($scenario -eq 'default-concurrency') { $null } else { 2 }
        $ResumePassedStages = $true
        $VerificationProfile = if ($scenario -in @('Core', 'Full')) { $scenario } else { 'Focused' }
        $PSBoundParameters = @{}
        $metricOutcome = 'failed'
        $failure = $null
        try { . $verificationBody } catch { $failure = $_ }
        $runnerCall = $calls | Where-Object Name -eq 'Invoke-LlmWikiFullVerification.ps1' | Select-Object -First 1
        if ($null -eq $runnerCall -or -not $runnerCall.Arguments.ResumePassedStages) {
            throw 'Full verification lost its runner or stage-resume argument.'
        }
        if ($null -eq $MaxConcurrency) {
            if ($runnerCall.Arguments.ContainsKey('IndexConcurrency')) { throw 'Full verification overrode default concurrency without a request.' }
        } elseif ($runnerCall.Arguments.IndexConcurrency -ne $MaxConcurrency) {
            throw 'Full verification lost the requested concurrency bound.'
        }
        if (($VerificationProfile -eq 'Full' -and -not $runnerCall.Arguments.FullTools) -or
            ($VerificationProfile -eq 'Core' -and -not $runnerCall.Arguments.CoreTools)) {
            throw 'Full verification lost the requested tool profile.'
        }
        $publicationCalls = @($calls | Where-Object Name -eq 'Write-LlmWikiIndexVerificationReceipt.ps1')
        if ($scenario -in @('policy-failed', 'impact-failed')) {
            if ($null -eq $failure -or $publicationCalls.Count -ne 0 -or $metricOutcome -ne 'failed') {
                throw 'A failed verification gate published a successful full verification receipt.'
            }
        } elseif ($null -ne $failure -or $publicationCalls.Count -ne 1 -or
            -not $publicationCalls[0].Arguments.CompletedFullVerification -or
            $calls[$calls.Count - 1].Name -ne 'Write-LlmWikiIndexVerificationReceipt.ps1' -or
            $metricOutcome -ne 'passed') {
            throw "Full verification did not publish exactly once after every gate: $scenario. $failure"
        }
    }
}
$runnerAst = [Management.Automation.Language.Parser]::ParseFile(
    (Join-Path $PSScriptRoot 'Invoke-LlmWikiFullVerification.ps1'), [ref]$tokens, [ref]$parseErrors)
if (@($parseErrors).Count -gt 0 -or @($runnerAst.FindAll({
    param($node)
    $node -is [Management.Automation.Language.CommandAst] -and
        $node.Extent.Text -match 'Write-LlmWikiIndexVerificationReceipt'
}, $true)).Count -gt 0) {
    throw 'The inner regression runner must not publish before the facade finishes its policy and source-impact gates.'
}
& {
    function Invoke-WikiTool([string]$Name, [hashtable]$Arguments) {
        if ($Name -ne 'Invoke-LlmWikiSelfMaintenance.ps1') { throw 'Wiki health routed to the wrong checker.' }
        return $Arguments
    }
    $QualityArea = 'Wiki'; $Format = 'Json'; $BaseRef = 'HEAD'
    $route = [scriptblock]::Create("switch ('health') { 'health' $($healthClause.Item2.Extent.Text) }")
    foreach ($FailOnInvalid in @($false, $true)) {
        $forwarded = & $route
        if (-not $forwarded.ContainsKey('FailOnInvalid') -or [bool]$forwarded.FailOnInvalid -ne $FailOnInvalid) {
            throw 'Wiki health lost the requested failure-exit contract.'
        }
    }
}
$maintenanceText = Get-Content (Join-Path $PSScriptRoot 'Invoke-LlmWikiSelfMaintenance.ps1') -Raw
$start = $maintenanceText.IndexOf('$freshnessReason =')
$end = $maintenanceText.IndexOf('if ($Format -eq')
$diagnostic = [scriptblock]::Create($maintenanceText.Substring($start, $end - $start))
foreach ($scenario in @('current', 'head-changed', 'working-tree-changed', 'projection-unavailable')) {
    & {
        Set-StrictMode -Version Latest
        $projectionFresh = $scenario -eq 'current'
        $graphStatus = if ($scenario -eq 'projection-unavailable') { $null } else {
            [pscustomobject]@{ changeSetGitHead = 'old'; currentChangeSetGitHead = $(if ($scenario -eq 'head-changed') { 'new' } else { 'old' }); changeSetFingerprint = 'a'; currentChangeSetFingerprint = 'b' }
        }
        $ownership = [pscustomobject]@{ unresolvedCount = 0 }
        $sources = [pscustomobject]@{ changedPages = @() }
        $remainingSources = [pscustomobject]@{ findings = @() }
        $Repair = $false
        . $diagnostic
        if ($result.projectionStatus.reason -ne $scenario -or $result.valid -ne $projectionFresh) { throw "Wrong freshness diagnosis: $scenario" }
        if ($projectionFresh -and $result.nextAction -ne 'No action required.') { throw 'Healthy wiki should not request a rebuild.' }
        if (-not $projectionFresh -and $result.nextAction -notmatch 'graph-build') { throw 'Stale projection must recommend graph-build.' }
    }
}
Write-Host "LLM Wiki facade command catalog passed: $($declaredCommands.Count) declared command(s), one route each, $($compactCommandLines.Count) primary help entries, and detailed compatibility help."
