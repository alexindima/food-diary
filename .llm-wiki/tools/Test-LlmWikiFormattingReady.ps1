[CmdletBinding()]
param([Parameter(Mandatory)][string[]]$ChangedPath)
$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$paths = @($ChangedPath | Where-Object { $_ } | ForEach-Object { $_.Replace('\', '/') } | Sort-Object -Unique)
$csharp = @($paths | Where-Object { $_ -match '\.cs$' -and (Test-Path -LiteralPath (Join-Path $repositoryRoot $_)) })
$frontend = @($paths | Where-Object { $_ -match '^FoodDiary\.Web\.Client/.+\.(ts|html)$' -and (Test-Path -LiteralPath (Join-Path $repositoryRoot $_)) })
if ($csharp.Count -gt 0) {
    Write-Host "Formatting preflight: checking $($csharp.Count) C# file(s) before index generation."
    $arguments = @('format', 'FoodDiary.slnx', '--verify-no-changes', '--include') + $csharp
    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) { throw 'C# formatting is not stable. Run dotnet format before regenerating Wiki indexes.' }
}
if ($frontend.Count -gt 0) {
    Write-Host "Formatting preflight: checking $($frontend.Count) frontend file(s) before index generation."
    Push-Location (Join-Path $repositoryRoot 'FoodDiary.Web.Client')
    try {
        $relative = @($frontend | ForEach-Object { $_.Substring('FoodDiary.Web.Client/'.Length) })
        & npx prettier --check @relative
        if ($LASTEXITCODE -ne 0) { throw 'Frontend formatting is not stable. Run Prettier before regenerating Wiki indexes.' }
    } finally { Pop-Location }
}
Write-Host 'Formatting preflight passed: source hashes are stable for index generation.'
