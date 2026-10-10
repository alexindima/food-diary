param([Parameter(Mandatory)][string]$ArchivePath, [Parameter(Mandatory)][string]$DestinationPath)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$cache = [IO.Path]::GetFullPath((Join-Path $root '.artifacts/tool-cache/k6'))
$archive = [IO.Path]::GetFullPath($ArchivePath)
$destination = [IO.Path]::GetFullPath($DestinationPath)
if (-not $archive.StartsWith($cache + [IO.Path]::DirectorySeparatorChar) -or -not $destination.StartsWith($cache + [IO.Path]::DirectorySeparatorChar)) { throw 'Extraction must stay in the owned k6 cache' }
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::OpenRead($archive)
try {
    $entry = @($zip.Entries | Where-Object Name -eq 'k6.exe')
    if ($entry.Count -ne 1 -or $entry[0].Length -gt 200MB) { throw 'Unexpected k6 release archive' }
    [IO.Directory]::CreateDirectory($destination) | Out-Null
    $inputStream = $entry[0].Open()
    $outputStream = [IO.File]::Create((Join-Path $destination 'k6.exe'))
    try { $inputStream.CopyTo($outputStream) } finally { $outputStream.Dispose(); $inputStream.Dispose() }
} finally { $zip.Dispose() }
