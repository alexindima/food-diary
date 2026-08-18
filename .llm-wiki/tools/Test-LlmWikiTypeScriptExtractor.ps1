[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
$fixtureRoot = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'typescript-extractor'
$fixturePath = Join-Path $fixtureRoot 'sample.component.ts'
try {
    @'
import { Component, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

export interface Summary { value: number; }
export const routes = [{ path: 'summary', loadComponent: () => import('./summary.component') }];

@Component({ selector: 'app-summary', templateUrl: './summary.component.html' })
export class SummaryComponent {
  private readonly http = inject(HttpClient);
  load() { return this.http.get('/api/summary'); }
}
'@ | Set-Content -LiteralPath $fixturePath -Encoding UTF8
    $relativePath = $fixturePath.Substring($repositoryRoot.Length + 1).Replace('\', '/')
    $result = '{"paths":["' + $relativePath + '"]}' | node (Join-Path $PSScriptRoot 'typescript-extractor.mjs') | ConvertFrom-Json
    $symbolNames = @($result.symbols | ForEach-Object { [string]$_.name })
    foreach ($expected in @('Summary', 'routes', 'SummaryComponent', 'load')) {
        if ($expected -notin $symbolNames) { throw "TypeScript extractor omitted declaration '$expected'." }
    }
    $edgeKinds = @($result.edges | ForEach-Object { [string]$_.kind })
    foreach ($expected in @('module-import', 'angular-route', 'angular-lazy-route', 'component-selector', 'component-resource', 'di-service', 'http-client')) {
        if ($expected -notin $edgeKinds) { throw "TypeScript extractor omitted relation '$expected'." }
    }
    Write-Host 'LLM Wiki TypeScript extractor regression passed: compiler-API declarations and Angular relations are accurate.'
} finally {
    if (Test-Path -LiteralPath $fixtureRoot) { Remove-Item -LiteralPath $fixtureRoot -Recurse -Force }
}
