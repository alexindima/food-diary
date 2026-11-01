function Get-LlmWikiFrontendBrowserCheck {
    param([string]$RepositoryRoot, [string]$Path, [object]$Package)
    $normalized = $Path.Replace('\', '/')
    if ($normalized -notmatch '^FoodDiary\.Web\.Client/e2e/(?<area>[^/]+)/.+\.spec\.ts$') { return $null }
    $area = $Matches['area']
    $relative = $normalized.Substring('FoodDiary.Web.Client/'.Length)
    $script = switch ($area) {
        'client-smoke' { 'test:e2e:client:smoke' }
        'admin-smoke' { 'test:e2e:admin:smoke' }
        'web-guidance' {
            $specific = 'test:guidance:' + ([IO.Path]::GetFileName($normalized) -replace '\.spec\.ts$', '')
            if ($Package.scripts.PSObject.Properties[$specific]) { $specific } else { 'test:guidance:platform' }
        }
        default { $null }
    }
    if (-not [IO.File]::Exists((Join-Path $RepositoryRoot $normalized))) { throw "Browser test does not exist: $normalized" }
    if (-not $script -or -not $Package.scripts.PSObject.Properties[$script]) { throw "No verified package.json Playwright runner owns '$normalized'; use its explicit browser configuration." }
    [pscustomobject]@{
        id = 'focused-playwright-' + ($relative -replace '[^A-Za-z0-9]', '-').ToLowerInvariant()
        command = "cd FoodDiary.Web.Client && npm run $script -- $relative"
        source = 'focused-browser-test'; priority = 'required'; reason = 'explicit-playwright-scenario'
        commandEvidence = "FoodDiary.Web.Client/package.json:$script; $normalized"
    }
}
