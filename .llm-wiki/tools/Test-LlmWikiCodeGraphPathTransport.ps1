[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$manager = Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1'
$graph = Join-Path $PSScriptRoot 'code-graph.mjs'
$scope = @('AGENTS.md', 'FoodDiary.slnx', '.llm-wiki/index.md', ('missing path/' + ('проверка ' * 60) + '.cs'))
$sourcePath = 'FoodDiary.Domain/Entities/Users/User.cs'
$longScope = @($scope * 100) + @($sourcePath)
$scope += $sourcePath
if (($longScope -join ';').Length -le 32767) { throw 'The regression must exceed the Windows command-line limit.' }
$ordinary = & $manager fingerprint -ChangedPath $scope -SkipRefresh -Format Json | ConvertFrom-Json
$large = & $manager fingerprint -ChangedPath $longScope -SkipRefresh -Format Json | ConvertFrom-Json
if ($ordinary.fingerprint -cne $large.fingerprint) { throw 'Large scope transport changed the fingerprint.' }
$ordinaryProjection = & $manager compiled-context -CompiledMode ChangedPaths -ChangedPath $scope -SkipRefresh -Format Json | ConvertFrom-Json
$largeProjection = & $manager compiled-context -CompiledMode ChangedPaths -ChangedPath $longScope -SkipRefresh -Format Json | ConvertFrom-Json
if (-not $ordinaryProjection.ready -or [int]$ordinaryProjection.returnedRecords -eq 0) {
    throw 'Projection comparison requires a ready graph and a real source record at the tail of the oversized scope.'
}
foreach ($field in @('ready', 'source', 'selectionMode', 'catalog', 'symbols', 'frontendSymbols', 'sourceHashes', 'returnedRecords')) {
    if ((ConvertTo-Json -InputObject $ordinaryProjection.$field -Depth 30 -Compress) -cne
        (ConvertTo-Json -InputObject $largeProjection.$field -Depth 30 -Compress)) {
        throw "Large scope transport changed compiled-context field '$field'."
    }
}
$priorEncoding = $OutputEncoding
try {
    $OutputEncoding = [Text.UTF8Encoding]::new($false)
    $unicode = '["missing path/проверка.cs"]' | & node $graph fingerprint --path-stdin=true | ConvertFrom-Json
    $unicodeArgv = & $manager fingerprint -ChangedPath 'missing path/проверка.cs' -SkipRefresh -Format Json | ConvertFrom-Json
    if ($unicode.fingerprint -cne $unicodeArgv.fingerprint -or
        @($unicode.requestedPaths)[0] -cne 'missing path/проверка.cs') { throw 'UTF-8 scope transport changed Unicode paths.' }
    foreach ($invalid in @('{"path":"AGENTS.md"}', '[42]', 'not-json')) {
        $failure = @($invalid | & node $graph fingerprint --path-stdin=true 2>&1)
        if ($LASTEXITCODE -eq 0) { throw 'Invalid scope input was silently accepted.' }
    }
    $conflicting = @('["AGENTS.md"]' | & node $graph fingerprint --path=FoodDiary.slnx --path-stdin=true 2>&1)
    if ($LASTEXITCODE -eq 0) { throw 'Conflicting scope transports were silently accepted.' }
} finally {
    $OutputEncoding = $priorEncoding
}
Write-Host 'Code graph path transport passed: oversized scope, equivalent projection, UTF-8 and invalid-input propagation.'
exit 0
