#requires -Version 7.0
<#
.SYNOPSIS
Formats project reference lists. Use -Check in CI or -Path for selected projects.
.DESCRIPTION
Orders project references within uninterrupted runs by ascending leading ../ count,
then project file name and full Include as a tie-breaker (ordinal comparison).
Package references are ordered by package id. Conditions, comments, duplicate
includes, expressions, Update/Remove items and other item types are sort barriers.
No references are moved between ItemGroups. Metadata moves with its reference.
#>
[CmdletBinding()]
param(
    [switch]$Check,
    [string[]]$Path
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent

function Sort-ReferenceRun([System.Collections.Generic.List[System.Xml.XmlElement]]$Run) {
    if ($Run.Count -lt 2) { return }
    $keys = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($element in $Run) {
        if (-not $keys.Add($element.GetAttribute('Include'))) { return }
    }
    $sorted = [Collections.Generic.List[System.Xml.XmlElement]]::new($Run)
    $sorted.Sort([Comparison[System.Xml.XmlElement]]{
        param($left, $right)
        $leftInclude = $left.GetAttribute('Include')
        $rightInclude = $right.GetAttribute('Include')
        if ($left.LocalName -eq 'ProjectReference') {
            $leftDepth = [regex]::Match($leftInclude, '^(?:\./)?(?<parent>\.\./)*').Groups['parent'].Captures.Count
            $rightDepth = [regex]::Match($rightInclude, '^(?:\./)?(?<parent>\.\./)*').Groups['parent'].Captures.Count
            $comparison = $leftDepth.CompareTo($rightDepth)
            if ($comparison -ne 0) { return $comparison }
            $comparison = [StringComparer]::Ordinal.Compare(
                [IO.Path]::GetFileName($leftInclude), [IO.Path]::GetFileName($rightInclude))
            if ($comparison -ne 0) { return $comparison }
        }
        return [StringComparer]::Ordinal.Compare($leftInclude, $rightInclude)
    })
    for ($i = 0; $i -lt $Run.Count; $i++) {
        [void]$Run[$i].ParentNode.ReplaceChild($sorted[$i].CloneNode($true), $Run[$i])
    }
}

if (-not $Path) {
    $Path = @(& git -C $root ls-files --cached --others --exclude-standard -- '*.csproj')
    if ($LASTEXITCODE -ne 0) { throw 'Cannot list repository projects.' }
    $Path = @($Path | Sort-Object -Unique | Where-Object { [IO.File]::Exists((Join-Path $root $_)) })
}
$changes = 0
foreach ($entry in $Path) {
    $full = if ([IO.Path]::IsPathRooted($entry)) { [IO.Path]::GetFullPath($entry) } else { [IO.Path]::GetFullPath((Join-Path $root $entry)) }
    $source = [IO.File]::ReadAllText($full)
    $document = [Xml.XmlDocument]::new()
    $document.PreserveWhitespace = $true
    $document.LoadXml($source)
    foreach ($group in $document.SelectNodes('//*[local-name()="ItemGroup"]')) {
        $run = [Collections.Generic.List[System.Xml.XmlElement]]::new()
        $kind = ''
        foreach ($node in @($group.ChildNodes)) {
            if ($node -is [Xml.XmlWhitespace] -or $node -is [Xml.XmlSignificantWhitespace]) { continue }
            $sortable = $node -is [Xml.XmlElement] -and $node.LocalName -in @('ProjectReference', 'PackageReference') -and
                $node.HasAttribute('Include') -and -not $node.HasAttribute('Condition') -and
                $node.GetAttribute('Include') -notmatch '[$@%*?;]'
            if (-not $sortable -or ($kind -and $kind -ne $node.LocalName)) {
                Sort-ReferenceRun $run
                $run.Clear()
                $kind = ''
            }
            if ($sortable) {
                if ($node.LocalName -eq 'ProjectReference') {
                    $node.SetAttribute('Include', $node.GetAttribute('Include').Replace('\', '/'))
                }
                $kind = $node.LocalName
                $run.Add($node)
            }
        }
        Sort-ReferenceRun $run
    }
    $newline = if ($source.Contains("`r`n")) { "`r`n" } else { "`n" }
    foreach ($group in $document.SelectNodes('//*[local-name()="ItemGroup" or local-name()="PropertyGroup"]')) {
        $depth = 0
        $parent = $group.ParentNode
        while ($parent -is [Xml.XmlElement]) { $depth++; $parent = $parent.ParentNode }
        $indent = ' ' * ($depth * 2)
        foreach ($node in @($group.ChildNodes)) {
            if ($node -is [Xml.XmlWhitespace] -or $node -is [Xml.XmlSignificantWhitespace]) {
                [void]$group.RemoveChild($node)
            }
        }
        foreach ($node in @($group.ChildNodes)) {
            [void]$group.InsertBefore($document.CreateWhitespace($newline + $indent + '  '), $node)
        }
        if ($group.HasChildNodes) { [void]$group.AppendChild($document.CreateWhitespace($newline + $indent)) }
        $previous = $group.PreviousSibling
        if ($previous -is [Xml.XmlWhitespace]) { $previous = $previous.PreviousSibling }
        if ($previous -is [Xml.XmlElement] -and $previous.LocalName -in @('ItemGroup', 'PropertyGroup')) {
            if ($group.PreviousSibling -is [Xml.XmlWhitespace]) {
                $group.PreviousSibling.Value = $newline + $newline + $indent
            } else {
                [void]$group.ParentNode.InsertBefore($document.CreateWhitespace($newline + $newline + $indent), $group)
            }
        }
    }
    $formatted = $document.OuterXml
    if ($formatted -cne $source) {
        $changes++
        Write-Output ([IO.Path]::GetRelativePath($root, $full))
        if (-not $Check) {
            $bytes = [IO.File]::ReadAllBytes($full)
            $bom = $bytes.Length -ge 3 -and $bytes[0] -eq 239 -and $bytes[1] -eq 187 -and $bytes[2] -eq 191
            [IO.File]::WriteAllText($full, $formatted, [Text.UTF8Encoding]::new($bom))
        }
    }
}
if ($Check -and $changes -gt 0) {
    Write-Output "$changes project(s) need formatting. Run ./scripts/Format-ProjectFiles.ps1"
    exit 1
}
Write-Output "Project formatting complete: $changes changed, $($Path.Count) checked."
