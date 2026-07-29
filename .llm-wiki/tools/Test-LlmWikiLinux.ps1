[CmdletBinding()]
param(
    [string]$Image = 'mcr.microsoft.com/powershell:7.5-ubuntu-24.04'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))

if ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform(
        [System.Runtime.InteropServices.OSPlatform]::Linux)) {
    & (Join-Path $PSScriptRoot 'Test-LlmWikiPortable.ps1')
    exit $LASTEXITCODE
}

if ($null -eq (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw 'Docker is required for the local Linux PowerShell smoke.'
}

& docker run --rm `
    --volume "${repositoryRoot}:/repo" `
    --workdir /repo `
    $Image `
    pwsh -NoLogo -NoProfile -File ./.llm-wiki/tools/Test-LlmWikiPortable.ps1
if ($LASTEXITCODE -ne 0) {
    throw "Linux PowerShell smoke failed with exit code $LASTEXITCODE."
}

Write-Host "LLM Wiki Linux smoke passed with image '$Image'."
