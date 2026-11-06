[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'coverage/CoverageAttributeFilters.ps1')
$marker = 'FoodDiary.Modules.Ai.Infrastructure.Diagnostics.ExcludeFromWindowsCoverageAttribute'
$standard = 'System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute'
function Assert-Filters([bool]$WindowsPlatform, [string[]]$InputFilters, [string[]]$Expected) {
    $actual = @(Get-FoodDiaryCoverageAttributeFilters -WindowsPlatform $WindowsPlatform -ExcludeAttribute $InputFilters)
    if (($actual -join '|') -cne ($Expected -join '|')) { throw "Unexpected attribute policy for Windows=$WindowsPlatform." }
}
Assert-Filters $true @($standard, 'Example.CustomAttribute') @($standard, 'Example.CustomAttribute', $marker)
Assert-Filters $false @($standard, $marker, 'Example.CustomAttribute') @($standard, 'Example.CustomAttribute')
Assert-Filters $true @($marker, $standard, $marker.ToUpperInvariant()) @($standard, $marker)
Assert-Filters $false @() @()
Assert-Filters $true @() @($marker)

$fixtureRoot = Join-Path (Split-Path -Parent $PSScriptRoot) ('.artifacts/coverage-platform-tests/' + [guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($fixtureRoot)
$settings = Join-Path $fixtureRoot 'Fixture.sln.DotSettings.user'
@'
<wpf:ResourceDictionary xml:space="preserve" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" xmlns:s="clr-namespace:System;assembly=mscorlib" xmlns:wpf="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
  <!-- Preserve unrelated personal settings and existing coverage exclusions. -->
  <s:String x:Key="/Unrelated/@EntryValue">keep me</s:String>
  <s:String x:Key="/Multiline/@EntryValue">Привет&#xD;&#xA;мир</s:String>
  <s:String x:Key="/Default/dotCover/Filtering/ExcludedAssembliesString/@EntryValue">Existing.*</s:String>
  <s:String x:Key="/Default/dotCover/Filtering/ExcludedAttributesString/@EntryValue">System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute||Example.CustomAttribute</s:String>
</wpf:ResourceDictionary>
'@ | Set-Content -LiteralPath $settings -Encoding utf8
$configure = Join-Path $PSScriptRoot 'Configure-CoveragePlatform.ps1'
$newSettings = Join-Path $fixtureRoot 'New.sln.DotSettings.user'
& $configure -SettingsPath $newSettings -WhatIf | Out-Null
if (Test-Path -LiteralPath $newSettings) { throw 'WhatIf created a personal settings file.' }
& $configure -SettingsPath $newSettings | Out-Null
if ((Test-Path -LiteralPath $newSettings) -ne $IsWindows) { throw 'New personal settings were not limited to Windows.' }
$beforeWhatIf = (Get-FileHash -LiteralPath $settings).Hash
& $configure -SettingsPath $settings -WhatIf | Out-Null
if ((Get-FileHash -LiteralPath $settings).Hash -cne $beforeWhatIf) { throw 'WhatIf changed personal settings.' }
& $configure -SettingsPath $settings | Out-Null
$document = [xml](Get-Content -LiteralPath $settings -Raw)
$values = @{}
foreach ($node in $document.DocumentElement.ChildNodes | Where-Object NodeType -eq Element) {
    $values[$node.GetAttribute('Key', 'http://schemas.microsoft.com/winfx/2006/xaml')] = $node.InnerText
}
$expected = @(Get-FoodDiaryCoverageAttributeFilters -WindowsPlatform $IsWindows -ExcludeAttribute @($standard, 'Example.CustomAttribute')) -join '||'
if ($values['/Multiline/@EntryValue'] -cne "Привет`r`nмир") { throw 'Embedded personal setting text changed during XML serialization.' }
if ($values['/Default/dotCover/Filtering/ExcludedAttributesString/@EntryValue'] -cne $expected -or
    $values['/Unrelated/@EntryValue'] -cne 'keep me' -or
    $values['/Default/dotCover/Filtering/ExcludedAssembliesString/@EntryValue'] -cne 'Existing.*') {
    throw 'Personal settings reconciliation changed unrelated settings or lost an existing filter.'
}
$configuredHash = (Get-FileHash -LiteralPath $settings).Hash
& $configure -SettingsPath $settings | Out-Null
if ((Get-FileHash -LiteralPath $settings).Hash -cne $configuredHash) { throw 'Configuration was not idempotent.' }
$invalid = Join-Path $fixtureRoot 'Invalid.sln.DotSettings.user'
Set-Content -LiteralPath $invalid -Value '<broken' -Encoding utf8
$invalidHash = (Get-FileHash -LiteralPath $invalid).Hash
$rejected = $false
try { & $configure -SettingsPath $invalid | Out-Null } catch { $rejected = $true }
if (-not $rejected -or (Get-FileHash -LiteralPath $invalid).Hash -cne $invalidHash) { throw 'Malformed settings were not preserved and rejected.' }
Write-Output 'Coverage policy passed: Windows/Linux filters, preservation, WhatIf, idempotence and malformed settings.'
