#requires -Version 7.0
[CmdletBinding()]
param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$Image = 'nginx:1.31.3-alpine',
    [ValidateSet('missing', 'never')]
    [string]$PullPolicy = 'missing'
)

$ErrorActionPreference = 'Stop'
$source = Get-Content -LiteralPath (Join-Path $RepositoryRoot 'nginx/sites-enabled/fooddiary.club') -Raw
# Exercise the real public server, including nested locations and inherited headers.
$server = [regex]::Match($source, '(?ms)^server \{\r?\n    listen 443 ssl;\r?\n    listen 443 quic;.*?^\}').Value
if (-not $server) { throw 'Unable to find the public FoodDiary server.' }
$server = [regex]::Replace($server, '(?m)^\s*(?:listen 443 (?:ssl|quic);|http2 on;|ssl_\w+ .+;)\r?\n', '')
$server = $server.Replace('server {', "server {`n    listen 8080;")
$adminServer = [regex]::Match($source, '(?ms)^server \{\r?\n    listen 443 ssl;\r?\n    http2 on;\r?\n    server_name admin\.fooddiary\.club;.*?^\}').Value
if (-not $adminServer) { throw 'Unable to find the admin FoodDiary server.' }
$adminServer = [regex]::Replace($adminServer, '(?m)^\s*(?:listen 443 ssl;|http2 on;|ssl_\w+ .+;)\r?\n', '')
$adminServer = $adminServer.Replace('server {', "server {`n    listen 8081;")
$id = [guid]::NewGuid().ToString('N')
$container = "fd-pwa-integrity-$id"
$temporaryRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$fixture = Join-Path $temporaryRoot $container
$releaseRoot = Join-Path $fixture 'current'
$adminRoot = Join-Path $fixture 'admin'
$started = $false
$client = [Net.Http.HttpClient]::new()
try {
    New-Item -ItemType Directory -Path (Join-Path $releaseRoot 'assets') -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $adminRoot 'current') -Force | Out-Null
    $shells = @{
        '/index.html' = '<!doctype html><html><body>Public shell</body></html>'
        '/index.csr.html' = '<!doctype html><html><body>Client shell</body></html>'
    }
    foreach ($url in $shells.Keys) {
        [IO.File]::WriteAllText((Join-Path $releaseRoot $url.TrimStart('/')), $shells[$url])
    }
    foreach ($path in @('ru', 'pricing', 'ru/pricing')) {
        New-Item -ItemType Directory -Path (Join-Path $releaseRoot $path) -Force | Out-Null
        [IO.File]::WriteAllText((Join-Path $releaseRoot "$path/index.html"), "Public page: $path")
    }
    [IO.File]::WriteAllText((Join-Path $releaseRoot 'assets/control.css'), 'body { color: black; }')
    [IO.File]::WriteAllText((Join-Path $adminRoot 'current/index.html'), 'Admin shell')
    foreach ($siteRoot in @($fixture, $adminRoot)) {
        New-Item -ItemType Directory -Path (Join-Path $siteRoot 'retained-assets/media') -Force | Out-Null
        New-Item -ItemType Directory -Path (Join-Path $siteRoot '.well-known/acme-challenge') -Force | Out-Null
        [IO.File]::WriteAllText((Join-Path $siteRoot 'retained-assets/chunk-OLD12345.js'), 'old lazy chunk')
        [IO.File]::WriteAllText((Join-Path $siteRoot 'retained-assets/media/font-OLD12345.woff2'), 'old font')
        [IO.File]::WriteAllText((Join-Path $siteRoot '.well-known/acme-challenge/control'), 'acme parent root')
        # These files must never be served through the retained-asset fallback.
        foreach ($name in @('index.html', 'ngsw.json', 'ngsw-worker.js', 'config-OLD12345.json')) {
            [IO.File]::WriteAllText((Join-Path $siteRoot "retained-assets/$name"), 'retained poison')
        }
    }
    $configuration = @"
events {}
http {
    include /etc/nginx/mime.types;
    log_format fooddiary_privacy '`$status';
    map `$http_upgrade `$connection_upgrade { default upgrade; '' close; }
    $server
    $adminServer
}
"@
    $configurationPath = Join-Path $fixture 'nginx.conf'
    [IO.File]::WriteAllText($configurationPath, $configuration)
    $arguments = @('run', '--detach', "--pull=$PullPolicy", '--memory', '128m', '--cpus', '1', '--name', $container, '--publish', '127.0.0.1::8080', '--publish', '127.0.0.1::8081',
        '--mount', "type=bind,source=$fixture,target=/var/www/fooddiary.club,readonly",
        '--mount', "type=bind,source=$adminRoot,target=/var/www/admin.fooddiary.club,readonly",
        '--mount', "type=bind,source=$configurationPath,target=/etc/nginx/nginx.conf,readonly", $Image)
    & docker @arguments | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Unable to start the owned Nginx fixture.' }
    $started = $true
    & docker exec $container nginx -t
    if ($LASTEXITCODE -ne 0) { throw 'Public server configuration is invalid.' }
    $binding = (& docker port $container '8080/tcp').Trim()
    if ($LASTEXITCODE -ne 0 -or $binding -notmatch '^127\.0\.0\.1:\d+$') { throw 'Unexpected fixture binding.' }
    $baseUri = "http://$binding"
    $adminBinding = (& docker port $container '8081/tcp').Trim()
    if ($LASTEXITCODE -ne 0 -or $adminBinding -notmatch '^127\.0\.0\.1:\d+$') { throw 'Unexpected admin fixture binding.' }
    foreach ($siteUri in @($baseUri, "http://$adminBinding")) {
        foreach ($url in @('/chunk-OLD12345.js', '/media/font-OLD12345.woff2')) {
            $response = $client.GetAsync("$siteUri$url").GetAwaiter().GetResult()
            try {
                if ([int]$response.StatusCode -ne 200 -or ($response.Headers.GetValues('Cache-Control') -join ',') -notmatch 'immutable') {
                    throw "Old immutable asset is unavailable: $siteUri$url"
                }
            } finally { $response.Dispose() }
        }
        foreach ($url in @('/ngsw.json', '/ngsw-worker.js', '/chunk-MISSING1.js')) {
            $response = $client.GetAsync("$siteUri$url").GetAwaiter().GetResult()
            try {
                if ([int]$response.StatusCode -ne 404) { throw "Missing asset/manifest/worker must return 404: $siteUri$url" }
            } finally { $response.Dispose() }
        }
        foreach ($url in @('/index.html', '/config-OLD12345.json')) {
            $response = $client.GetAsync("$siteUri$url").GetAwaiter().GetResult()
            try {
                if ($response.Content.ReadAsStringAsync().GetAwaiter().GetResult() -eq 'retained poison') {
                    throw "Unversioned document fell back to retained assets: $siteUri$url"
                }
            } finally { $response.Dispose() }
        }
        $challenge = $client.GetStringAsync("$siteUri/.well-known/acme-challenge/control").GetAwaiter().GetResult()
        if ($challenge -cne 'acme parent root') { throw 'ACME challenge no longer uses the stable parent root.' }
    }
    $adminShell = $client.GetAsync("http://$adminBinding/index.html").GetAwaiter().GetResult()
    try {
        if ([int]$adminShell.StatusCode -ne 200) { throw 'Admin shell request failed.' }
        $adminPolicy = $adminShell.Headers.GetValues('Content-Security-Policy') -join ';'
        $imageSources = [regex]::Match($adminPolicy, '(?:^|;)\s*img-src\s+([^;]+)').Groups[1].Value -split '\s+'
        foreach ($requiredSource in @("'self'", 'data:', 'blob:', 'https:')) {
            if ($requiredSource -notin $imageSources) { throw "Admin CSP blocks a supported profile-image source: $requiredSource" }
        }
    } finally { $adminShell.Dispose() }
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
    Remove-Item -LiteralPath (Join-Path $releaseRoot 'index.csr.html')
    $missing = $client.GetAsync("$baseUri/index.csr.html").GetAwaiter().GetResult()
    try {
        if ([int]$missing.StatusCode -ne 404) { throw 'Missing versioned shell must return 404 instead of SPA fallback.' }
    } finally { $missing.Dispose() }
    Write-Host 'PWA shell integrity passed: exact production public/admin servers, shell/navigation/security/cache checks, retained lazy chunks/fonts, missing-asset/manifest/worker 404, stable ACME roots.'
} finally {
    $client.Dispose()
    if ($started) { & docker rm --force $container | Out-Null }
    $resolvedFixture = [IO.Path]::GetFullPath($fixture)
    if ($resolvedFixture.StartsWith($temporaryRoot, [StringComparison]::OrdinalIgnoreCase) -and
        [IO.Path]::GetFileName($resolvedFixture) -ceq $container -and (Test-Path -LiteralPath $resolvedFixture)) {
        Remove-Item -LiteralPath $resolvedFixture -Recurse -Force
    }
}
