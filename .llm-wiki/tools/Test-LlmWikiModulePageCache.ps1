[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiIndexCache.ps1')
$repositoryRoot = [string]@(& git -C $PSScriptRoot rev-parse --show-toplevel)[0]
if ($LASTEXITCODE -ne 0 -or -not $repositoryRoot) { throw 'Unable to resolve fixture source repository.' }
$fixtureParent = Get-LlmWikiSmokeSandboxRoot -RepositoryRoot $repositoryRoot -IndependentRepository
$fixture = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'module-page-cache' -IndependentRepository
$shell = (Get-Process -Id $PID).Path
$cases = 0
function Write-Fixture([string]$Path, [string]$Text) {
    $absolute = Join-Path $fixture $Path
    [void][IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($absolute))
    [IO.File]::WriteAllText($absolute, $Text, [Text.UTF8Encoding]::new($false))
}
function Run-Generator([string[]]$Argument, [int]$ExpectedExit = 0) {
    $start = [Diagnostics.ProcessStartInfo]::new()
    $start.FileName = $shell
    $start.WorkingDirectory = $fixture
    $start.UseShellExecute = $false
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $start.Arguments = '-NoLogo -NoProfile -File "' + (Join-Path $fixture '.llm-wiki/tools/Build-LlmWikiModulePages.ps1') + '" ' + ($Argument -join ' ')
    $process = [Diagnostics.Process]::Start($start)
    try {
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit(30000)) { $process.Kill($true); throw 'Module-page fixture timed out.' }
        $output = $stdout.GetAwaiter().GetResult() + $stderr.GetAwaiter().GetResult()
        if ($process.ExitCode -ne $ExpectedExit) { throw "Generator exit=$($process.ExitCode), expected=$ExpectedExit. $output" }
        return $output
    } finally { $process.Dispose() }
}
function Assert-Cache([bool]$Expected, [string[]]$Argument, [int]$Exit = 0) {
    $output = Run-Generator $Argument $Exit
    if ($output.Contains('pages cache hit:') -ne $Expected) { throw "Unexpected cache reuse ($Expected). $output" }
    $script:cases++
}
try {
    Write-Fixture '.gitignore' ".artifacts/`n"
    foreach ($tool in @('Build-LlmWikiModulePages.ps1', 'Get-LlmWikiContractConsumers.ps1', 'LlmWikiJson.ps1', 'LlmWikiGitPaths.ps1', 'LlmWikiIndexCache.ps1', 'LlmWikiSourceMatching.cs', 'LlmWikiNativeCompilation.ps1', 'LlmWikiQueryCache.ps1', 'LlmWikiChangeSetSnapshot.ps1')) {
        Write-Fixture ".llm-wiki/tools/$tool" ([IO.File]::ReadAllText((Join-Path $PSScriptRoot $tool)))
    }
    foreach ($path in @('.llm-wiki/generated/repository-catalog.json', 'docs/architecture/backend-modules.json', 'docs/architecture/module-dependencies.json')) {
        Write-Fixture $path ([IO.File]::ReadAllText((Join-Path $repositoryRoot $path)))
    }
    # These scenarios exercise cache invalidation and complete output sets. Two
    # modules preserve multiple pages and the Users contract evidence while
    # keeping every scenario independent of unrelated repository module growth.
    $fixtureCatalogPath = Join-Path $fixture '.llm-wiki/generated/repository-catalog.json'
    $fixtureCatalog = Get-Content -LiteralPath $fixtureCatalogPath -Raw | ConvertFrom-Json
    $fixtureCatalog.applicationModules = @($fixtureCatalog.applicationModules | Where-Object { $_.name -in @('Users','Billing') })
    $fixtureCatalog.extractedApplicationModules = @($fixtureCatalog.extractedApplicationModules | Where-Object { $_.name -in @('Users','Billing') })
    [IO.File]::WriteAllText($fixtureCatalogPath, ($fixtureCatalog | ConvertTo-Json -Depth 30))
    $manifestPath = Join-Path $fixture 'docs/architecture/backend-modules.json'
    $manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
    $manifest.modules = [pscustomobject]@{ Users=$manifest.modules.Users; Billing=$manifest.modules.Billing }
    $manifest.inventory.totalModules = 2
    $manifest.modules.Users.sourceMappings.abstractionAreas = @('Users')
    $manifest.modules.Users.sourceMappings | Add-Member -NotePropertyName infrastructureProjects -NotePropertyValue @('Modules/Users/Infrastructure') -Force
    [IO.File]::WriteAllText($manifestPath, ($manifest | ConvertTo-Json -Depth 20))
    $probePath = 'Modules/Users/Application/CacheProbe.cs'
    $probeText = 'namespace FoodDiary.Application.Users; public class CacheProbeHandler(IUserContextService context, IUserProfileReadService profile) { public string Execute() => context.Read() + profile.Read(); }'
    Write-Fixture $probePath $probeText
    Write-Fixture 'Modules/Users/Application.Abstractions/IUserContextService.cs' "public interface IUserContextService {`n string Read();`n}"
    Write-Fixture 'Modules/Users/Application.Abstractions/IUserProfileReadService.cs' "public interface IUserProfileReadService {`n string Read();`n}"
    $reuse = @('-ReuseUnchangedCheck')
    $reuseCheck = @('-Check', '-ReuseUnchangedCheck')
    Assert-Cache $false $reuse
    Assert-Cache $true $reuseCheck
    Assert-Cache $true $reuse
    Assert-Cache $false @('-Check')
    $outputs = @(Get-ChildItem (Join-Path $fixture '.llm-wiki/generated/modules') -Filter '*.md' -File | ForEach-Object { '.llm-wiki/generated/modules/' + $_.Name })
    $golden = Get-LlmWikiIndexOutputSetFingerprint $fixture $outputs
    Run-Generator @() | Out-Null
    if ((Get-LlmWikiIndexOutputSetFingerprint $fixture $outputs) -cne $golden) { throw 'Forced and cached generation differ.' }
    $cases++
    Write-Fixture $probePath ($probeText + ' // changed')
    Assert-Cache $false $reuse
    Write-Fixture 'Modules/Users/Application/Added.cs' 'public class AddedQuery { }'
    Assert-Cache $false $reuse
    Remove-Item -LiteralPath (Join-Path $fixture 'Modules/Users/Application/Added.cs')
    Assert-Cache $false $reuse
    Write-Fixture 'Modules/Users/Infrastructure/marker.md' 'source-area inventory marker'
    Assert-Cache $false $reuse
    Write-Fixture 'Modules/Users/Infrastructure/marker.md' 'content not consumed by the generator'
    Assert-Cache $true $reuseCheck
    Remove-Item -LiteralPath (Join-Path $fixture 'Modules/Users/Infrastructure/marker.md')
    Assert-Cache $false $reuse
    [void][IO.Directory]::CreateDirectory((Join-Path $fixture 'Users'))
    Assert-Cache $false $reuse
    Remove-Item -LiteralPath (Join-Path $fixture 'Users')
    Assert-Cache $false $reuse
    $helper = Join-Path $fixture '.llm-wiki/tools/Get-LlmWikiContractConsumers.ps1'
    [IO.File]::AppendAllText($helper, "`n# changed helper`n")
    Assert-Cache $false $reuse
    [IO.File]::AppendAllText($manifestPath, "`n ")
    Assert-Cache $false $reuse
    $pagePath = '.llm-wiki/generated/modules/users.md'
    $receipt = Join-Path $fixture '.artifacts/llm-wiki/index-cache/module-pages.json'
    $receiptBefore = Get-LlmWikiRequiredFileSha256 $receipt
    [IO.File]::AppendAllText((Join-Path $fixture $pagePath), 'corrupt output')
    Assert-Cache $false $reuseCheck 1
    if ((Get-LlmWikiRequiredFileSha256 $receipt) -cne $receiptBefore) { throw 'Failed output validation published a receipt.' }
    Assert-Cache $false $reuse
    Remove-Item -LiteralPath (Join-Path $fixture $pagePath)
    Assert-Cache $false $reuseCheck 1
    Assert-Cache $false $reuse
    Write-Fixture '.llm-wiki/generated/modules/obsolete.md' 'obsolete output'
    Assert-Cache $false $reuseCheck 1
    Assert-Cache $false $reuse
    if (Test-Path (Join-Path $fixture '.llm-wiki/generated/modules/obsolete.md')) { throw 'Obsolete page was not reconciled.' }
    [IO.File]::WriteAllText($receipt, '{')
    Assert-Cache $false $reuseCheck
    Assert-Cache $true $reuseCheck
    $legacy = Get-Content $receipt -Raw | ConvertFrom-Json
    $legacy.schemaVersion = 1
    [IO.File]::WriteAllText($receipt, ($legacy | ConvertTo-Json))
    Assert-Cache $false $reuseCheck
    Write-Host "Module-page cache regressions passed: $cases cases."
} finally {
    $cleanup = [IO.Path]::GetFullPath($fixture)
    if ([IO.Path]::GetDirectoryName($cleanup) -cne [IO.Path]::GetFullPath($fixtureParent).TrimEnd('\', '/')) { throw 'Fixture cleanup escaped its owned parent.' }
    Remove-Item -LiteralPath $cleanup -Recurse -Force
}
