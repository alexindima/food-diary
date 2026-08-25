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
$unregisteredStableCommands = @($registeredCommands | Where-Object { $_ -notin $declaredCommands })
$tierIds = @($registry.tiers.id)
$compactCommandLines = @($compactHelp | Where-Object { $_ -match '^\s+\.\/\.llm-wiki\/wiki\.ps1 ' })
$detailedCommandLines = @($detailedHelp | Where-Object { $_ -match '^\s+\.\/\.llm-wiki\/wiki\.ps1 ' })
if ($compactCommandLines.Count -lt 8 -or $compactCommandLines.Count -gt 15 -or
    $compactHelp -notcontains 'Administrative and compatibility commands:' -or
    $compactHelp -notcontains '  ./.llm-wiki/wiki.ps1 help -Detailed' -or
    $detailedHelp -notcontains 'Detailed command catalog:' -or
    $compactHelp -notcontains 'Command stability tiers: core, governed, experimental.' -or
    @($tierIds | Sort-Object -Unique).Count -ne 3 -or
    $unregisteredStableCommands.Count -gt 0 -or
    $detailedCommandLines.Count -le $compactCommandLines.Count) {
    throw @"
Wiki facade help tiers are inconsistent.
Compact command lines: $($compactCommandLines.Count)
Detailed command lines: $($detailedCommandLines.Count)
Unregistered stable commands: $($unregisteredStableCommands -join ', ')
"@
}

Write-Host "LLM Wiki facade command catalog passed: $($declaredCommands.Count) declared command(s), one route each, $($compactCommandLines.Count) primary help entries, and detailed compatibility help."
