function Invoke-LlmWikiCorpusEvaluation {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$RepositoryRoot,
        [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string[]]$CorpusPath,
        [ValidateRange(1, 4)][int]$MaxConcurrency = 2,
        [ValidateRange(1, 3600)][int]$TimeoutSeconds = 300,
        [string]$EvaluatorPath
    )

    . (Join-Path $RepositoryRoot '.llm-wiki/tools/LlmWikiProcess.ps1')
    if (-not $EvaluatorPath) { $EvaluatorPath = Join-Path $RepositoryRoot '.llm-wiki/tools/Measure-LlmWikiSqlContextEvaluation.ps1' }
    # A serial smoke run must remain serial even inside this nested pool.
    if ($env:LLM_WIKI_SMOKE_MAX_CONCURRENCY) {
        $outerConcurrency = 0
        if (-not [int]::TryParse($env:LLM_WIKI_SMOKE_MAX_CONCURRENCY, [ref]$outerConcurrency) -or $outerConcurrency -lt 1) {
            throw 'Invalid smoke concurrency limit.'
        }
        $MaxConcurrency = [Math]::Min($MaxConcurrency, $outerConcurrency)
    }
    $names = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $corpora = @($CorpusPath | ForEach-Object {
        $path = (Resolve-Path -LiteralPath $_).Path
        $name = [IO.Path]::GetFileName($path)
        if (-not $names.Add($name)) { throw "Duplicate corpus name: $name" }
        $source = [IO.File]::ReadAllText($path) | ConvertFrom-Json
        if (@($source.cases).Count -eq 0) { throw "Empty corpus: $name" }
        [pscustomobject]@{ Path = $path; Name = $name; CaseIds = @($source.cases.id); Cases = @($source.cases).Count }
    })
    $queue = [Collections.Generic.Queue[object]]::new()
    foreach ($corpus in $corpora | Sort-Object @{ Expression = 'Cases'; Descending = $true }, Name) { $queue.Enqueue($corpus) }
    $runId = "corpus-$PID-$([guid]::NewGuid().ToString('N'))"
    # Flat, uniquely named reports match the existing CI artifact upload pattern.
    $reportRoot = Join-Path $RepositoryRoot '.artifacts/llm-wiki/context-evaluation'
    $null = New-Item -ItemType Directory -Path $reportRoot -Force
    $running = [Collections.Generic.List[object]]::new()
    $evaluations = [Collections.Generic.Dictionary[string, string]]::new([StringComparer]::OrdinalIgnoreCase)
    $corpusTimings = [Collections.Generic.List[object]]::new()
    $timer = [Diagnostics.Stopwatch]::StartNew()
    $peak = 0
    $failure = $null
    try {
        while ($queue.Count -or $running.Count) {
            if ($env:LLM_WIKI_SMOKE_CANCEL_PATH -and (Test-Path -LiteralPath $env:LLM_WIKI_SMOKE_CANCEL_PATH)) {
                throw [OperationCanceledException]::new('Corpus evaluation cancelled by the smoke supervisor.')
            }
            while ($queue.Count -and $running.Count -lt $MaxConcurrency) {
                $corpus = $queue.Dequeue()
                $start = [Diagnostics.ProcessStartInfo]::new()
                $start.FileName = (Get-Process -Id $PID).Path
                $start.WorkingDirectory = $RepositoryRoot
                $start.UseShellExecute = $false
                $start.RedirectStandardOutput = $true
                $start.RedirectStandardError = $true
                $start.StandardOutputEncoding = [Text.UTF8Encoding]::new($false)
                $start.StandardErrorEncoding = [Text.UTF8Encoding]::new($false)
                # The caller builds once; each evaluator owns its corpus read lifecycle.
                foreach ($argument in @('-NoLogo', '-NoProfile', '-File', $EvaluatorPath, '-CorpusPath', $corpus.Path, '-SkipBuild', '-Format', 'Json')) {
                    $start.ArgumentList.Add($argument)
                }
                $process = [Diagnostics.Process]::new()
                $process.StartInfo = $start
                try {
                    if (-not $process.Start()) { throw "Cannot start corpus worker: $($corpus.Name)" }
                } catch {
                    $process.Dispose()
                    throw
                }
                $worker = [pscustomobject]@{
                    Corpus = $corpus; Process = $process; Timer = [Diagnostics.Stopwatch]::StartNew()
                    Output = $null; Error = $null
                }
                $running.Add($worker)
                $worker.Output = $process.StandardOutput.ReadToEndAsync()
                $worker.Error = $process.StandardError.ReadToEndAsync()
                $peak = [Math]::Max($peak, $running.Count)
                Write-Verbose "Evaluating corpus $($corpus.Name) in worker $($process.Id)."
            }
            foreach ($worker in @($running.ToArray())) {
                if (-not $worker.Process.HasExited -or -not $worker.Output.IsCompleted -or -not $worker.Error.IsCompleted) {
                    if ($worker.Timer.Elapsed.TotalSeconds -gt $TimeoutSeconds) { throw "Corpus worker timed out: $($worker.Corpus.Name)" }
                    continue
                }
                $output = $worker.Output.GetAwaiter().GetResult()
                $errors = $worker.Error.GetAwaiter().GetResult()
                $outputPath = Join-Path $reportRoot "$runId-$($worker.Corpus.Name)"
                [IO.File]::WriteAllText($outputPath, $output, [Text.UTF8Encoding]::new($false))
                [IO.File]::WriteAllText(($outputPath + '.log'), $errors, [Text.UTF8Encoding]::new($false))
                if ($worker.Process.ExitCode -ne 0) {
                    throw "Corpus worker failed with exit code $($worker.Process.ExitCode): $($worker.Corpus.Name). Diagnostics: $outputPath.log"
                }
                try { $evaluation = $output | ConvertFrom-Json }
                catch { throw "Corpus worker returned invalid JSON: $($worker.Corpus.Name). Diagnostics: $outputPath" }
                if ([string]$evaluation.corpusPath -ne $worker.Corpus.Path.Replace('\', '/') -or $evaluation.caseCount -ne $worker.Corpus.Cases -or
                    @($evaluation.results).Count -ne $worker.Corpus.Cases -or
                    (@($evaluation.results.id) -join "`0") -cne ($worker.Corpus.CaseIds -join "`0")) {
                    throw "Corpus worker changed corpus identity, case coverage or result order: $($worker.Corpus.Name)"
                }
                $evaluations.Add($worker.Corpus.Path, $output)
                $worker.Timer.Stop()
                $corpusTimings.Add([pscustomobject]@{
                    corpus = $worker.Corpus.Name
                    caseCount = $worker.Corpus.Cases
                    durationSeconds = [Math]::Round($worker.Timer.Elapsed.TotalSeconds, 3)
                })
                $running.Remove($worker) | Out-Null
                $worker.Process.Dispose()
            }
            if ($running.Count) { Start-Sleep -Milliseconds 100 }
        }
    } catch {
        $failure = $_
        throw
    } finally {
        $cleanupFailures = [Collections.Generic.List[string]]::new()
        foreach ($worker in $running) {
            try { Stop-LlmWikiProcessTree -Process $worker.Process }
            catch { $cleanupFailures.Add("$($worker.Corpus.Name): $_") }
            finally { $worker.Process.Dispose() }
        }
        $timer.Stop()
        if ($cleanupFailures.Count) {
            if ($null -eq $failure) { throw "Corpus worker cleanup failed: $($cleanupFailures -join '; ')" }
            Write-Warning "Corpus worker cleanup failed: $($cleanupFailures -join '; ')"
        }
    }
    if ($evaluations.Count -ne $corpora.Count) { throw 'Corpus pool did not evaluate every requested corpus.' }
    [pscustomobject]@{
        Evaluations = $evaluations; DurationSeconds = $timer.Elapsed.TotalSeconds
        CorpusCount = $evaluations.Count; PeakConcurrency = $peak; RunId = $runId
        CorpusTimings = @($corpusTimings | Sort-Object @{ Expression = 'durationSeconds'; Descending = $true }, corpus)
    }
}
