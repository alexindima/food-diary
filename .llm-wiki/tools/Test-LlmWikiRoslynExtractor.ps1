[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'roslyn-extractor/LlmWiki.RoslynExtractor.csproj'
$fixtureRoot = Join-Path ([System.IO.Path]::GetTempPath()) "llm-wiki-roslyn-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $fixtureRoot -Force | Out-Null
try {
    $fixture = Join-Path $fixtureRoot 'Fixture.cs'
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
    private void Overloaded(int value) { }
    private void Overloaded(string value) { }
}
internal interface IRealService { }
internal sealed class RealService : IRealService { }
internal sealed class RealCommand { }
'@ | Set-Content -LiteralPath $fixture -Encoding utf8
    $json = & dotnet run --project $project --no-launch-profile -- $fixture
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
    Write-Host 'LLM Wiki Roslyn extractor regression passed: syntax-aware declarations and typed relations are accurate.'
} finally {
    Remove-Item -LiteralPath $fixtureRoot -Recurse -Force -ErrorAction SilentlyContinue
}
