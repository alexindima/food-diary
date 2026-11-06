. (Join-Path $PSScriptRoot 'LlmWikiVerificationReceipts.ps1')

function Get-LlmWikiEvidenceExecutionLocation {
    param([string]$RepositoryRoot, [string]$EvidencePath, [switch]$Prepare)
    $root = [IO.Path]::GetFullPath($RepositoryRoot).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    $absolute = [IO.Path]::GetFullPath($(if ([IO.Path]::IsPathRooted($EvidencePath)) { $EvidencePath } else { Join-Path $root $EvidencePath }))
    $comparison = if ([IO.Path]::DirectorySeparatorChar -eq '\') { [StringComparison]::OrdinalIgnoreCase } else { [StringComparison]::Ordinal }
    if (-not $absolute.StartsWith($root, $comparison)) { throw 'Evidence execution destination must remain inside the repository.' }
    $relative = $absolute.Substring($root.Length).Replace('\', '/')
    if ($relative -notmatch '^\.artifacts/llm-wiki/(?:[^/]+/)*[^/]+\.json$') { throw 'Evidence execution destination must be JSON under .artifacts/llm-wiki.' }
    $logs = Join-Path ([IO.Path]::GetDirectoryName($absolute)) 'logs'
    if ($Prepare) {
        # Fail before spending time on the check, including read-only evidence
        # files and unwritable/malformed log directories.
        $handle = [IO.File]::Open($absolute, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::Read)
        $handle.Dispose()
        $null = New-Item -ItemType Directory -Path $logs -Force
        $probe = Join-Path $logs ('.preflight-' + [guid]::NewGuid().ToString('N'))
        try { $handle = [IO.File]::Open($probe, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None); $handle.Dispose() }
        finally { if ([IO.File]::Exists($probe)) { [IO.File]::Delete($probe) } }
    }
    [pscustomobject]@{ evidencePath = $absolute; relativePath = $relative; logsDirectory = $logs; logPrefix = $logs + [IO.Path]::DirectorySeparatorChar }
}

function Test-LlmWikiFrontendExecutionCommand([string]$Command) {
    if ($Command -notmatch '^(?:npm --prefix FoodDiary\.Web\.Client run |cd FoodDiary\.Web\.Client && npm run )(?<script>test:ci:(?:app|admin|ui-kit|tour|webmcp)|test:e2e:(?:client|admin):smoke|test:guidance:[a-z0-9:-]+)(?<arguments>(?: -- [A-Za-z0-9_./\\:=,* -]+)?)$') { return $false }
    $scriptName = $Matches['script']
    $arguments = $Matches['arguments']
    if ($arguments -match '(?:^|[=,/\\ ])\.\.(?:[/\\]|$)') { return $false }
    if (-not $arguments) { return $true }
    if ($arguments.Trim().Length -le 3) { return $false }
    $tokens = @($arguments.Trim().Substring(3) -split '\s+')
    for ($index = 0; $index -lt $tokens.Count; $index++) {
        $token = $tokens[$index]
        if ($scriptName.StartsWith('test:ci:')) {
            if ($token -eq '--include') {
                $index++
                if ($index -ge $tokens.Count -or $tokens[$index] -notmatch '^(?:src|projects)/[A-Za-z0-9_./*,-]+\.spec\.ts$') { return $false }
            } elseif ($token -notmatch '^--include=(?:src|projects)/[A-Za-z0-9_./*,-]+\.spec\.ts$|^--watch=false$|^--coverage(?:=(?:true|false))?$') { return $false }
        } elseif ($token -notmatch '^e2e/[A-Za-z0-9_./*-]+\.spec\.ts$|^--project=[A-Za-z0-9_-]+$|^--(?:workers|timeout|repeat-each)=[1-9][0-9]*$|^--retries=[0-9]+$|^--headed$') { return $false }
    }
    return $true
}

function Test-LlmWikiSupplementalExecutionCommand([string]$Command) {
    # These are test/validation commands, not a general shell escape hatch.
    return (Test-LlmWikiFrontendExecutionCommand $Command) -or
        $Command -match '^node (?:--test(?: [A-Za-z0-9_./\\:=,-]+)*|[A-Za-z0-9_./\\-]+\.(?:mjs|cjs|js)(?: [A-Za-z0-9_./\\:=,-]+)*)$' -or
        $Command -match '^pwsh -NoProfile -File (?:\.llm-wiki/tools/|scripts/)Test-[A-Za-z0-9_.-]+\.ps1(?: -[A-Za-z0-9_-]+ [A-Za-z0-9_./\\:,-]+)*$' -or
        $Command -match '^python -m unittest discover -s \.agents/skills/[a-z0-9-]+/scripts -p test_[A-Za-z0-9_*]+\.py$' -or
        $Command -match '^docker compose(?: -f [A-Za-z0-9_./\\-]+)? config(?: (?:--quiet|-q))?$' -or
        $Command -match '^docker build --check(?: -f [A-Za-z0-9_./\\-]+)? [A-Za-z0-9_./\\-]+$'
}

function Test-LlmWikiCanonicalExecutionCommand([string]$Command) {
    # Callers first verify exact equality with the current policy command.
    return $Command -match '^dotnet (test|format|list) [A-Za-z0-9_./\\-]+(?: [A-Za-z0-9_./\\:=~,-]+)*$' -or
        $Command -match '^cd FoodDiary\.Web\.Client && npm (run [A-Za-z0-9_:-]+|audit)$' -or
        $Command -match '^\./\.llm-wiki/wiki\.ps1 [A-Za-z0-9_./\\:-]+(?: [A-Za-z0-9_./\\:-]+)*$'
}

function Get-LlmWikiRegressionTestFiles([string]$RepositoryRoot, [string[]]$TestPath) {
    if (@($TestPath).Count -eq 0) { throw 'Regression evidence requires -TestPath for the focused test files.' }
    foreach ($path in @($TestPath | Sort-Object -Unique)) {
        $relative = $path.Replace('\', '/')
        if ([string]::IsNullOrWhiteSpace($relative) -or [IO.Path]::IsPathRooted($relative) -or $relative -match '(^|/)\.\.(/|$)') {
            throw 'Regression test files must be repository-relative paths.'
        }
        $absolute = Join-Path $RepositoryRoot $relative
        if (-not (Test-Path -LiteralPath $absolute -PathType Leaf)) { throw "Regression test file does not exist: $relative" }
        [pscustomobject][ordered]@{ path = $relative; sha256 = Get-LlmWikiFileSha256 $absolute }
    }
}

function Test-LlmWikiRegressionEvidence([object]$Entry, [string]$RepositoryRoot, [string]$EvidencePath, [switch]$BeforeOnly) {
    $issues = [Collections.Generic.List[string]]::new()
    $present = $null -ne $Entry -and $Entry.PSObject.Properties['regression'] -and $null -ne $Entry.regression
    if (-not $present) { return [pscustomobject]@{ id = $Entry.id; present = $false; valid = $true; state = 'not-required'; issues = @() } }
    $proof = $Entry.regression
    try {
        $evidenceAbsolute = [IO.Path]::GetFullPath($(if ([IO.Path]::IsPathRooted($EvidencePath)) { $EvidencePath } else { Join-Path $RepositoryRoot $EvidencePath }))
        $logPrefix = (Join-Path ([IO.Path]::GetDirectoryName($evidenceAbsolute)) 'logs') + [IO.Path]::DirectorySeparatorChar
        $records = @{}
        foreach ($phase in @('before', 'after')) {
            if ($BeforeOnly -and $phase -eq 'after') { continue }
            if (-not $proof.PSObject.Properties[$phase] -or $null -eq $proof.$phase) { $issues.Add("Regression $phase execution is missing."); continue }
            $run = $proof.$phase
            foreach ($file in @(@{ path = $run.logPath; hash = $run.logSha256 }, @{ path = $run.recordPath; hash = $run.recordSha256 })) {
                $absolute = [IO.Path]::GetFullPath((Join-Path $RepositoryRoot ([string]$file.path)))
                if (-not $absolute.StartsWith($logPrefix, [StringComparison]::OrdinalIgnoreCase) -or
                    -not (Test-Path -LiteralPath $absolute -PathType Leaf) -or
                    (Get-LlmWikiFileSha256 $absolute) -cne [string]$file.hash) { throw "Regression $phase log or execution record is missing, modified or outside its evidence boundary." }
            }
            $record = Get-Content -LiteralPath (Join-Path $RepositoryRoot $run.recordPath) -Raw | ConvertFrom-Json
            $records[$phase] = $record
            if ($record.regressionPhase -cne $phase -or [string]$record.command -cne [string]$Entry.command -or
                [int]$record.exitCode -ne [int]$run.exitCode -or $null -eq $record.exitCode -or
                [string]$record.fingerprintBefore -cne [string]$record.fingerprintAfter -or
                [string]::IsNullOrWhiteSpace([string]$record.fingerprintBefore) -or
                [IO.Path]::GetFullPath([string]$record.checkout) -cne [IO.Path]::GetFullPath($RepositoryRoot) -or
                [IO.Path]::GetFullPath([string]$record.logPath) -cne [IO.Path]::GetFullPath((Join-Path $RepositoryRoot $run.logPath)) -or
                [string]$record.logSha256 -cne [string]$run.logSha256) { throw "Regression $phase execution does not match its recorded command, source or result." }
        }
        if ($records.ContainsKey('before')) {
            $before = $records.before
            $failure = [string]$before.expectedFailure
            $log = [IO.File]::ReadAllText((Join-Path $RepositoryRoot $proof.before.logPath))
            if ([int]$before.exitCode -eq 0 -or [string]::IsNullOrWhiteSpace($failure) -or -not $log.Contains($failure)) { $issues.Add('Regression before run did not observe the expected failure.') }
            $currentTests = @(Get-LlmWikiRegressionTestFiles $RepositoryRoot @($before.testFiles | ForEach-Object path))
            if (($currentTests | ConvertTo-Json -Compress) -cne (@($before.testFiles) | ConvertTo-Json -Compress)) { $issues.Add('Regression test files changed; repeat the before and after runs with the same tests.') }
        }
        if (-not $BeforeOnly -and $records.ContainsKey('after')) {
            $after = $records.after
            if ([int]$after.exitCode -ne 0 -or $Entry.status -cne 'passed' -or $Entry.lineage.kind -cne 'executed-check' -or
                [int]$Entry.lineage.execution.exitCode -ne 0 -or $Entry.lineage.artifact.sha256 -cne $proof.after.logSha256 -or
                $Entry.lineage.execution.verification.recordSha256 -cne $proof.after.recordSha256) { $issues.Add('Regression after run requires current passing executed evidence, not a manual pass.') }
            if ($Entry.lineage.kind -ceq 'executed-check') {
                $currentSource = & (Join-Path $PSScriptRoot 'Get-LlmWikiContentFingerprint.ps1') -Path @($Entry.lineage.dependencies.paths) -Format Json | ConvertFrom-Json
                if ([string]$currentSource.fingerprint -cne [string]$Entry.lineage.dependencies.contentFingerprint) { $issues.Add('Regression after evidence is stale: source content changed after the passing run.') }
            }
            if ($records.ContainsKey('before') -and
                ((@($before.testFiles) | ConvertTo-Json -Compress) -cne (@($after.testFiles) | ConvertTo-Json -Compress) -or
                [string]$before.fingerprintBefore -ceq [string]$after.fingerprintBefore -or
                [DateTimeOffset]::Parse([string]$before.completedAtUtc) -gt [DateTimeOffset]::Parse([string]$after.startedAtUtc))) { $issues.Add('Regression runs must use the same test files, with the after run following a source change.') }
        }
    } catch { $issues.Add($_.Exception.Message) }
    [pscustomobject][ordered]@{
        id = [string]$Entry.id; present = $true; valid = $issues.Count -eq 0
        state = $(if ($issues.Count -eq 0) { if ($BeforeOnly) { 'before-observed' } else { 'confirmed' } } else { 'unconfirmed' })
        issues = @($issues)
    }
}

function Test-LlmWikiEvidenceExecutionCommand {
    param([string]$Canonical, [string]$Executed, [string]$RepositoryRoot)
    if ($Canonical.Trim() -ceq $Executed.Trim()) { return $true }
    if ($Canonical -notmatch '^dotnet test ' -or $Executed -notmatch '^dotnet test ' -or $Executed -match '[;&|<>`$]') { return $false }
    function Get-ExecutionTokens([string]$Text, [bool]$StripOutputArguments) {
        $tokens = @([regex]::Matches($Text, '"[^"\r\n]*"|''[^''\r\n]*''|[^\s]+') | ForEach-Object { $_.Value.Trim('"', "'") })
        $result = [Collections.Generic.List[string]]::new()
        for ($index = 0; $index -lt $tokens.Count; $index++) {
            $token = $tokens[$index]
            if ($token -eq '--no-restore') { continue }
            if ($StripOutputArguments -and $token -eq '--no-build') { continue }
            if ($StripOutputArguments -and $token -in @('--artifacts-path', '--results-directory', '--logger')) {
                if ($index + 1 -ge $tokens.Count -or $tokens[$index + 1].StartsWith('-')) { return $null }
                $index++; continue
            }
            if ($StripOutputArguments -and $token -match '^--maxcpucount:[1-9][0-9]*$') { continue }
            $result.Add($token)
        }
        return ,@($result)
    }
    $expected = Get-ExecutionTokens $Canonical $false
    $actual = Get-ExecutionTokens $Executed $true
    if ($null -eq $actual -or ($expected -join "`n") -cne ($actual -join "`n")) { return $false }
    if ($Executed -notmatch '(?:^|\s)--no-build(?:\s|$)') { return $true }
    # Skipping compilation requires a successful build receipt for the current
    # exact repository contents. A supplemental/manual test cannot replace it.
    $project = [string]$expected[2]
    function Get-BuildOption([string]$Text, [string]$Option, [string]$Default) {
        if ($Text -match ('(?:^|\s)(?:' + $Option + ')(?:=|\s+)("[^"\r\n]+"|''[^''\r\n]+''|\S+)')) { return $Matches[1].Trim('"', "'").Replace('\', '/') }
        return $Default
    }
    foreach ($receipt in @(Get-LlmWikiVerificationReceipts $RepositoryRoot | Where-Object validForCurrentState)) {
        if ([string]$receipt.command -notmatch '^dotnet build ("[^"\r\n]+"|''[^''\r\n]+''|\S+)(?:\s|$)') { continue }
        $builtProject = $Matches[1].Trim('"', "'").Replace('\', '/')
        if ((Get-BuildOption $receipt.command '--configuration|-c' 'Debug') -cne (Get-BuildOption $Executed '--configuration|-c' 'Debug')) { continue }
        if ((Get-BuildOption $receipt.command '--artifacts-path' '<default>') -cne (Get-BuildOption $Executed '--artifacts-path' '<default>')) { continue }
        if ((Get-BuildOption $receipt.command '--framework|-f' '<default>') -cne (Get-BuildOption $Executed '--framework|-f' '<default>')) { continue }
        if ($builtProject -ceq $project.Replace('\', '/') -or $builtProject -in @('FoodDiary.slnx', 'FoodDiary.sln')) { return $true }
    }
    return $false
}
