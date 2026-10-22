#requires -Version 7.0
[CmdletBinding()]
param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$Image = 'nginx:1.31.3-alpine'
)

$ErrorActionPreference = 'Stop'
$source = Get-Content -LiteralPath (Join-Path $RepositoryRoot 'nginx/sites-enabled/fooddiary.club') -Raw
# Exercise the real public server, including nested locations and inherited headers.
$server = [regex]::Match($source, '(?ms)^server \{\r?\n    listen 443 ssl;\r?\n    listen 443 quic;.*?^\}').Value
if (-not $server) { throw 'Unable to find the public FoodDiary server.' }
$server = [regex]::Replace($server, '(?m)^\s*(?:listen 443 (?:ssl|quic);|http2 on;|ssl_\w+ .+;)\r?\n', '')
$server = $server.Replace('server {', "server {`n    listen 8080;")
$id = [guid]::NewGuid().ToString('N')
$container = "fd-pwa-integrity-$id"
$temporaryRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$fixture = Join-Path $temporaryRoot $container
$started = $false
$client = [Net.Http.HttpClient]::new()
try {
    New-Item -ItemType Directory -Path (Join-Path $fixture 'assets') -Force | Out-Null
    $shells = @{
        '/index.html' = '<!doctype html><html><body>Public shell</body></html>'
        '/index.csr.html' = '<!doctype html><html><body>Client shell</body></html>'
    }
    foreach ($url in $shells.Keys) {
        [IO.File]::WriteAllText((Join-Path $fixture $url.TrimStart('/')), $shells[$url])
    }
    foreach ($path in @('ru', 'pricing', 'ru/pricing')) {
        New-Item -ItemType Directory -Path (Join-Path $fixture $path) -Force | Out-Null
        [IO.File]::WriteAllText((Join-Path $fixture "$path/index.html"), "Public page: $path")
    }
    [IO.File]::WriteAllText((Join-Path $fixture 'assets/control.css'), 'body { color: black; }')
    $configuration = @"
events {}
http {
    include /etc/nginx/mime.types;
    log_format fooddiary_privacy '`$status';
    map `$http_upgrade `$connection_upgrade { default upgrade; '' close; }
    $server
}
"@
    $configurationPath = Join-Path $fixture 'nginx.conf'
    [IO.File]::WriteAllText($configurationPath, $configuration)
    $arguments = @('run', '--detach', '--name', $container, '--publish', '127.0.0.1::8080',
        '--mount', "type=bind,source=$fixture,target=/var/www/fooddiary.club,readonly",
        '--mount', "type=bind,source=$configurationPath,target=/etc/nginx/nginx.conf,readonly", $Image)
    & docker @arguments | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Unable to start the owned Nginx fixture.' }
    $started = $true
    & docker exec $container nginx -t
    if ($LASTEXITCODE -ne 0) { throw 'Public server configuration is invalid.' }
    $binding = (& docker port $container '8080/tcp').Trim()
    if ($LASTEXITCODE -ne 0 -or $binding -notmatch '^127\.0\.0\.1:\d+$') { throw 'Unexpected fixture binding.' }
    $baseUri = "http://$binding"
    foreach ($url in $shells.Keys) {
        foreach ($suffix in @('', '?ngsw-cache-bust=regression')) {
            $response = $client.GetAsync("$baseUri$url$suffix").GetAwaiter().GetResult()
            try {
                if ([int]$response.StatusCode -ne 200) { throw "Shell request failed: $url$suffix" }
                $bytes = $response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult()
                $expected = [Text.Encoding]::UTF8.GetBytes($shells[$url])
                $actualHash = [Convert]::ToHexString([Security.Cryptography.SHA1]::HashData($bytes))
                $expectedHash = [Convert]::ToHexString([Security.Cryptography.SHA1]::HashData($expected))
                if ($actualHash -cne $expectedHash) { throw "Versioned shell bytes changed: $url$suffix" }
                $cache = ($response.Headers.GetValues('Cache-Control') -join ',')
                foreach ($directive in @('no-transform', 'no-store', 'no-cache', 'must-revalidate')) {
                    if ($cache -notmatch "(?:^|[,\s])$directive(?:$|[,\s])") { throw "Missing $directive on $url$suffix" }
                }
                if (-not $response.Headers.Contains('Content-Security-Policy') -or
                    -not $response.Headers.Contains('Referrer-Policy') -or
                    -not $response.Headers.Contains('X-Content-Type-Options')) {
                    throw "Shell security headers were lost: $url$suffix"
                }
            } finally { $response.Dispose() }
        }
    }
    $navigationShells = @{
        '/' = $shells['/index.html']
        '/ru' = 'Public page: ru'
        '/pricing' = 'Public page: pricing'
        '/ru/pricing' = 'Public page: ru/pricing'
        '/dashboard' = $shells['/index.csr.html']
        '/dashboard?date=2026-10-22' = $shells['/index.csr.html']
        '/meals' = $shells['/index.csr.html']
        '/products' = $shells['/index.csr.html']
        '/meals/create' = $shells['/index.csr.html']
    }
    foreach ($path in $navigationShells.Keys) {
        $response = $client.GetAsync("$baseUri$path").GetAwaiter().GetResult()
        try {
            $body = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
            if ([int]$response.StatusCode -ne 200 -or $body -cne $navigationShells[$path]) {
                throw "Navigation served the wrong shell: $path"
            }
        } finally { $response.Dispose() }
    }
    $control = $client.GetAsync("$baseUri/assets/control.css").GetAwaiter().GetResult()
    try {
        if ([int]$control.StatusCode -ne 200 -or ($control.Headers.GetValues('Cache-Control') -join ',') -notmatch 'immutable') {
            throw 'Unrelated static asset caching changed.'
        }
    } finally { $control.Dispose() }
    Remove-Item -LiteralPath (Join-Path $fixture 'index.csr.html')
    $missing = $client.GetAsync("$baseUri/index.csr.html").GetAwaiter().GetResult()
    try {
        if ([int]$missing.StatusCode -ne 404) { throw 'Missing versioned shell must return 404 instead of SPA fallback.' }
    } finally { $missing.Dispose() }
    Write-Host 'PWA shell integrity passed: 4 shell requests, 9 public/workspace navigations, security/cache headers, static control, missing-shell 404.'
} finally {
    $client.Dispose()
    if ($started) { & docker rm --force $container | Out-Null }
    $resolvedFixture = [IO.Path]::GetFullPath($fixture)
    if ($resolvedFixture.StartsWith($temporaryRoot, [StringComparison]::OrdinalIgnoreCase) -and
        [IO.Path]::GetFileName($resolvedFixture) -ceq $container -and (Test-Path -LiteralPath $resolvedFixture)) {
        Remove-Item -LiteralPath $resolvedFixture -Recurse -Force
    }
}
