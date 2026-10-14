[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$result = & (Join-Path $PSScriptRoot 'Get-LlmWikiContractConsumers.ps1') -Contract IUserContextService -Format Json | ConvertFrom-Json
$sourceRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
. (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1')
$requestText = [Collections.Generic.Dictionary[string,string]]::new([StringComparer]::Ordinal)
foreach ($path in @(Invoke-LlmWikiGitPathList -RepositoryRoot $sourceRoot -Arguments @('ls-files','--cached','--others','--exclude-standard','--','*.cs'))) {
    if ([IO.File]::Exists((Join-Path $sourceRoot $path))) { $requestText[$path.Replace('\','/')] = [IO.File]::ReadAllText((Join-Path $sourceRoot $path)) }
}
$reused = & (Join-Path $PSScriptRoot 'Get-LlmWikiContractConsumers.ps1') -Contract IUserContextService -SourceTextByPath $requestText -Format Json | ConvertFrom-Json
if (($reused | ConvertTo-Json -Depth 20 -Compress) -cne ($result | ConvertTo-Json -Depth 20 -Compress)) { throw 'Request-local contract sources differ from the authoritative consumer scan.' }
if ($result.declarationPath -ne 'Modules/Users/Application/Common/IUserContextService.cs') {
    throw "Unexpected IUserContextService declaration: $($result.declarationPath)"
}
if ($result.readiness.abstractionOwned) { throw 'IUserContextService must currently be reported as implementation-owned.' }
if ($result.readiness.aggregateConsumers -ne 0) { throw 'IUserContextService aggregate readers should have been extracted.' }
if ($result.readiness.mutationConsumers -lt 1) { throw 'Expected remaining mutation consumers in the contract report.' }
if ($result.readiness.businessConsumers -ge $result.readiness.productionConsumers) { throw 'Composition and empty reference matches must not be counted as business consumers.' }
if ($result.readiness.compositionRegistrations -lt 1) { throw 'Expected DI registration evidence to be reported separately.' }
if ($result.readiness.externalModuleConsumers -ne 0) { throw 'IUserContextService must have no external module consumers before Users extraction.' }
if ($result.readiness.internalOwnerConsumers -lt 1) { throw 'Expected owner-internal consumers to remain visible.' }
$classificationCases = @(
    @('Modules/Users/Application/Commands/UpdateUser.cs', 'Users'),
    @('Modules/Meals/Application/Commands/UpdateUser.cs', 'Meals'),
    @('Modules\Meals\Application\Commands\UpdateUser.cs', 'Meals'),
    @('FoodDiary.Application/Users/Commands/UpdateUser.cs', 'Users'),
    @('FoodDiary.Application.Users/Commands/UpdateUser.cs', 'Users'),
    @('FoodDiary.Web.Api/Program.cs', 'FoodDiary.Web.Api')
)
# Load the real classifier definition without rerunning the repository scan.
$tokens = $null; $parseErrors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'Get-LlmWikiContractConsumers.ps1'), [ref]$tokens, [ref]$parseErrors)
$definition = $ast.Find({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Get-ModuleName' }, $true)
. (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1')
. ([scriptblock]::Create($definition.Extent.Text))
foreach ($case in $classificationCases) {
    if ((Get-ModuleName $case[0]) -ne $case[1]) { throw "Incorrect module classification for $($case[0])" }
}
foreach ($consumer in @($result.consumers | Where-Object access -eq 'mutation')) {
    if ($consumer.consumer -ne 'Users' -or 'UpdateUserAsync' -notin $consumer.methods) {
        throw 'Owner-internal mutations must remain visible with their actual operations.'
    }
}
Write-Host "LLM Wiki contract consumer tests passed: $($result.readiness.productionConsumers) production consumer(s)."

. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$fixtureParent = Get-LlmWikiSmokeSandboxRoot -RepositoryRoot $repositoryRoot -IndependentRepository
$fixture = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'contract-consumer-scan' -IndependentRepository
function Write-ScanFixture([string]$Path, [string]$Text) {
    $absolute = [IO.Path]::Combine($fixture, $Path)
    [void][IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($absolute))
    [IO.File]::WriteAllText($absolute, $Text, [Text.UTF8Encoding]::new($false))
}
try {
    foreach ($tool in @('Get-LlmWikiContractConsumers.ps1', 'LlmWikiGitPaths.ps1')) {
        Write-ScanFixture ".llm-wiki/tools/$tool" ([IO.File]::ReadAllText((Join-Path $PSScriptRoot $tool)))
    }
    Write-ScanFixture '.gitignore' ".artifacts/`n"
    Write-ScanFixture 'Modules/Probe/Application.Abstractions/IScanProbe.cs' "public interface IScanProbe {`n string Read();`n}"
    Write-ScanFixture 'Modules/Probe/Application/Consumer.cs' "public class Consumer(IScanProbe source) {`r`n public string Execute() => source.Read();`r`n}"
    Write-ScanFixture 'Shared/Папка с пробелом/Reference.cs' "// case-insensitive reference`r// iscanprobe`rpublic class Reference {}"
    Write-ScanFixture 'Modules/Probe/Application.Tests/ConsumerTests.cs' "public class ConsumerTests(IScanProbe source) {`n public string Execute() => source.Read();`n}"
    Write-ScanFixture 'Shared/WholeWord.cs' 'public class WholeWord(IScanProbeSuffix source, BeforeIScanProbe other) {}'
    Write-ScanFixture 'Shared/Deleted.cs' '// IScanProbe'
    & git -C $fixture init --quiet
    if ($LASTEXITCODE -ne 0) { throw 'Contract scan fixture Git initialization failed.' }
    & git -c core.autocrlf=false -C $fixture add -- .
    if ($LASTEXITCODE -ne 0) { throw 'Contract scan fixture Git staging failed.' }
    Remove-Item -LiteralPath ([IO.Path]::Combine($fixture, 'Shared/Deleted.cs'))
    $tool = Join-Path $fixture '.llm-wiki/tools/Get-LlmWikiContractConsumers.ps1'
    foreach ($includeTests in @($false, $true)) {
        $scan = & $tool -Contract IScanProbe -IncludeTests:$includeTests -Format Json | ConvertFrom-Json
        if ($scan.declarationPath -cne 'Modules/Probe/Application.Abstractions/IScanProbe.cs') { throw 'Contract declaration selection changed.' }
        $expectedPaths = @('Modules/Probe/Application/Consumer.cs', 'Shared/Папка с пробелом/Reference.cs')
        if ($includeTests) { $expectedPaths += 'Modules/Probe/Application.Tests/ConsumerTests.cs' }
        if (@(Compare-Object @($scan.consumers.path) $expectedPaths).Count -ne 0) { throw 'Contract scan lost newline/case/word/test/deleted-file semantics.' }
        $consumer = @($scan.consumers | Where-Object path -eq 'Modules/Probe/Application/Consumer.cs')[0]
        if ($consumer.methods.Count -ne 1 -or $consumer.methods[0] -cne 'Read' -or $consumer.returnedData[0] -cne 'string') { throw 'Captured contract method/return analysis changed.' }
    }
    Write-Host 'Contract consumer mixed-newline, case, whole-word, Unicode path, tests and deleted-file regressions passed.'
} finally {
    $cleanup = [IO.Path]::GetFullPath($fixture)
    if ([IO.Path]::GetDirectoryName($cleanup) -cne [IO.Path]::GetFullPath($fixtureParent).TrimEnd('\', '/')) { throw 'Contract scan fixture cleanup escaped its owned parent.' }
    Remove-Item -LiteralPath $cleanup -Recurse -Force
}
