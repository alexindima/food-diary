[CmdletBinding(SupportsShouldProcess)]
param([string]$SettingsPath)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
. (Join-Path $PSScriptRoot 'coverage/CoverageAttributeFilters.ps1')
if (-not $SettingsPath) { $SettingsPath = Join-Path $repositoryRoot 'FoodDiary.sln.DotSettings.user' }
$SettingsPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($SettingsPath)
$exists = Test-Path -LiteralPath $SettingsPath -PathType Leaf
if (-not $exists -and -not $IsWindows) {
    Write-Output 'No personal coverage settings to update; Windows-only methods remain included.'
    return
}

$document = [Xml.XmlDocument]::new()
$document.PreserveWhitespace = $true
$document.XmlResolver = $null
if ($exists) {
    $originalBytes = [IO.File]::ReadAllBytes($SettingsPath)
    $inputStream = [IO.MemoryStream]::new($originalBytes)
    try { $document.Load($inputStream) } finally { $inputStream.Dispose() }
} else {
    $document.LoadXml(@'
<wpf:ResourceDictionary xml:space="preserve" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" xmlns:s="clr-namespace:System;assembly=mscorlib" xmlns:wpf="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
</wpf:ResourceDictionary>
'@)
}
$namespaces = [Xml.XmlNamespaceManager]::new($document.NameTable)
$namespaces.AddNamespace('x', 'http://schemas.microsoft.com/winfx/2006/xaml')
$key = '/Default/dotCover/Filtering/ExcludedAttributesString/@EntryValue'
$node = $document.SelectSingleNode("/*/*[@x:Key='$key']", $namespaces)
if (-not $node -and -not $IsWindows) {
    Write-Output 'No personal Windows marker to remove; existing Rider settings are unchanged.'
    return
}
$previous = if ($node) { $node.InnerText } else { $null }
$filters = if ($node) { @($previous -split '\|\|') } else {
    @('System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute', 'System.CodeDom.Compiler.GeneratedCodeAttribute')
}
$updated = @(Get-FoodDiaryCoverageAttributeFilters -WindowsPlatform $IsWindows -ExcludeAttribute $filters) -join '||'
if ($null -ne $previous -and $updated -ceq $previous) {
    Write-Output 'Personal coverage filters already match the current operating system.'
    return
}
if ($PSCmdlet.ShouldProcess($SettingsPath, 'Update only the platform coverage attribute filter in the personal Rider layer')) {
    if (-not $node) {
        $node = $document.CreateElement('s', 'String', 'clr-namespace:System;assembly=mscorlib')
        $attribute = $document.CreateAttribute('x', 'Key', 'http://schemas.microsoft.com/winfx/2006/xaml')
        $attribute.Value = $key
        [void]$node.Attributes.Append($attribute)
        [void]$document.DocumentElement.AppendChild($node)
    }
    $node.InnerText = $updated
    # Preserve CR characters in setting values, but never emit character entities outside the root.
    foreach ($container in @($document, $document.DocumentElement)) {
        foreach ($child in $container.ChildNodes) {
            if ($child.NodeType -in @([Xml.XmlNodeType]::Whitespace, [Xml.XmlNodeType]::SignificantWhitespace)) {
                $child.Value = $child.Value.Replace("`r`n", "`n").Replace("`r", "`n")
            }
        }
    }
    $writerSettings = [Xml.XmlWriterSettings]::new()
    $writerSettings.Encoding = [Text.UTF8Encoding]::new($false)
    $writerSettings.NewLineHandling = [Xml.NewLineHandling]::Entitize
    $writerSettings.OmitXmlDeclaration = $document.FirstChild -isnot [Xml.XmlDeclaration]
    $outputStream = [IO.MemoryStream]::new()
    $writer = [Xml.XmlWriter]::Create($outputStream, $writerSettings)
    try { $document.Save($writer) } finally { $writer.Dispose() }
    $outputBytes = $outputStream.ToArray()
    $outputStream.Dispose()
    $validation = [Xml.XmlDocument]::new()
    $validation.XmlResolver = $null
    $validation.LoadXml([Text.Encoding]::UTF8.GetString($outputBytes))
    if ($exists -and [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([IO.File]::ReadAllBytes($SettingsPath))) -cne
        [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($originalBytes))) {
        throw 'Personal settings changed during configuration; rerun to preserve the newer values.'
    }
    $temporaryPath = $SettingsPath + '.' + [guid]::NewGuid().ToString('N') + '.tmp'
    try {
        [IO.File]::WriteAllBytes($temporaryPath, $outputBytes)
        [IO.File]::Move($temporaryPath, $SettingsPath, $true)
    } finally {
        if ([IO.File]::Exists($temporaryPath)) { [IO.File]::Delete($temporaryPath) }
    }
    Write-Output "Updated personal coverage filters for $([Runtime.InteropServices.RuntimeInformation]::OSDescription)."
}
