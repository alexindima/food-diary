[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'roslyn-extractor/LlmWiki.RoslynExtractor.csproj'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$artifactRoot = Join-Path $repositoryRoot ".artifacts/llm-wiki/roslyn-regression/$PID"
$fixtureRoot = Join-Path ([System.IO.Path]::GetTempPath()) "llm-wiki-roslyn-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $fixtureRoot -Force | Out-Null
try {
    Set-Content -LiteralPath (Join-Path $fixtureRoot 'Fixture.csproj') -Value '<Project Sdk="Microsoft.NET.Sdk" />' -Encoding utf8
    $fixtureDirectory = Join-Path $fixtureRoot 'Features/Coverage'
    New-Item -ItemType Directory -Path $fixtureDirectory -Force | Out-Null
    $fixture = Join-Path $fixtureDirectory 'Fixture.cs'
    @'
// public class FakeComment { }
namespace Fixture;
using System.Collections.Generic;
public sealed class RealHandler : IRequestHandler<RealCommand> {
    private const string FakeString = "public class FakeStringClass { }";
    public Task HandleAsync() => sender.Send(new RealCommand());
    public void Configure(IServiceCollection services) {
        services.AddScoped<IRealService, RealService>();
    }
    public void ResolveOverload() => Overloaded(42);
    public bool SelectsNamespace() => string.Equals(typeof(RealHandler).Namespace, "Fixture.Production.Controllers", StringComparison.Ordinal);
    public string UserText() => "This is ordinary user-facing text";
    private void Overloaded(int value) { }
    private void Overloaded(string value) { }
}
internal interface IRealService { }
internal sealed class RealService : IRealService { }
internal sealed class RealCommand { }
'@ | Set-Content -LiteralPath $fixture -Encoding utf8
    $build = & dotnet build $project -c Release --artifacts-path $artifactRoot --nologo --verbosity quiet
    if ($LASTEXITCODE -ne 0) { throw "Roslyn regression build failed: $build" }
    $dlls = @(Get-ChildItem -LiteralPath (Join-Path $artifactRoot 'bin') -Recurse -Filter LlmWiki.RoslynExtractor.dll -File)
    if ($dlls.Count -ne 1) { throw 'Roslyn regression did not publish exactly one extractor assembly.' }
    $json = & dotnet $dlls[0].FullName $fixture
    if ($LASTEXITCODE -ne 0) { throw "Roslyn extractor exited with $LASTEXITCODE." }
    $result = @($json | ConvertFrom-Json)[0]
    $symbolNames = @($result.symbols | ForEach-Object name)
    if ('RealHandler' -notin $symbolNames -or 'IRealService' -notin $symbolNames -or 'FakeComment' -in $symbolNames -or 'FakeStringClass' -in $symbolNames) {
        throw "Roslyn declarations are inaccurate: $($symbolNames -join ', ')."
    }
    $edgeKinds = @($result.edges | ForEach-Object kind)
    foreach ($kind in @('namespace-import', 'type-inheritance', 'mediator-handler', 'mediator-dispatch', 'di-service', 'di-implementation')) {
        if ($kind -notin $edgeKinds) { throw "Roslyn extractor omitted '$kind'." }
    }
    if (@($result.symbols | Where-Object { $_.name -eq 'RealHandler' -and $_.symbolId -match 'global::Fixture.RealHandler' }).Count -ne 1) {
        throw 'Roslyn extractor omitted the fully-qualified declaration identity.'
    }
    if (@($result.edges | Where-Object { $_.kind -eq 'resolved-method-call' -and $_.target -eq 'Overloaded' -and $_.targetId -match 'Overloaded\(int\)' }).Count -ne 1) {
        throw 'Roslyn semantic extraction did not resolve the selected overload.'
    }
    if (@($result.edges | Where-Object { $_.kind -eq 'resolved-type-inheritance' -and $_.target -eq 'IRealService' -and $_.targetId -match 'global::Fixture.IRealService' }).Count -ne 1) {
        throw 'Roslyn semantic extraction did not resolve the implemented interface.'
    }
    if (@($result.edges | Where-Object { $_.kind -eq 'namespace-filter' -and $_.target -eq 'Fixture.Production.Controllers' }).Count -ne 1) {
        throw 'Roslyn extractor omitted a namespace convention filter.'
    }
    if (@($result.edges | Where-Object { $_.kind -eq 'namespace-path-mismatch' -and $_.target -eq 'Fixture' -and $_.targetId -eq 'Fixture.Features.Coverage' }).Count -ne 1) {
        throw 'Roslyn extractor did not identify a namespace/folder mismatch.'
    }
    if (@($result.edges | Where-Object target -eq 'This is ordinary user-facing text').Count -ne 0) {
        throw 'Roslyn extractor classified ordinary text as a qualified code literal.'
    }
    $parallelPaths = @($fixture)
    foreach ($index in 1..12) {
        $path = Join-Path $fixtureDirectory "Concurrent$index.cs"
        [IO.File]::WriteAllText($path, "namespace Fixture; public sealed class Concurrent$index { public int Value => $index; }", [Text.UTF8Encoding]::new($false))
        $parallelPaths += $path
    }
    foreach ($semanticMode in @($false, $true)) {
        $serial = $null
        foreach ($concurrency in @(1,4)) {
            $payload = @{ paths=$parallelPaths; contextPaths=$parallelPaths; semantic=$semanticMode; maxConcurrency=$concurrency } | ConvertTo-Json -Compress
            $output = $payload | & dotnet $dlls[0].FullName --stdin
            if ($LASTEXITCODE -ne 0) { throw 'Parallel Roslyn extraction failed.' }
            if ($null -eq $serial) { $serial = $output } elseif (($serial -join "`n") -cne ($output -join "`n")) {
                throw 'Parallel parsing/extraction changed the complete symbols, typed edges, tokens or file order.'
            }
            $records = @($output | ConvertFrom-Json)
            if ($records.Count -ne $parallelPaths.Count -or (($records.path -join '|') -cne (($parallelPaths | ForEach-Object { $_.Replace('\','/') }) -join '|'))) {
                throw 'Parallel extraction lost a source or reordered its output.'
            }
        }
    }
    Write-Host 'LLM Wiki Roslyn extractor regression passed: syntax-aware declarations and typed relations are accurate.'
} finally {
    $expectedFixtureParent = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\','/')
    if ([IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($fixtureRoot)) -cne $expectedFixtureParent -or [IO.Path]::GetFileName($fixtureRoot) -notmatch '^llm-wiki-roslyn-[a-f0-9]{32}$') { throw 'Unowned Roslyn fixture cleanup.' }
    Remove-Item -LiteralPath $fixtureRoot -Recurse -Force -ErrorAction SilentlyContinue
    $expectedArtifacts = [IO.Path]::GetFullPath((Join-Path $repositoryRoot '.artifacts/llm-wiki/roslyn-regression')).TrimEnd('\','/')
    if ([IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($artifactRoot)) -cne $expectedArtifacts -or [IO.Path]::GetFileName($artifactRoot) -cne [string]$PID) { throw 'Unowned Roslyn artifact cleanup.' }
    if (Test-Path -LiteralPath $artifactRoot) { Remove-Item -LiteralPath $artifactRoot -Recurse -Force }
}
