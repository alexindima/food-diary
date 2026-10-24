import { spawn, execFileSync } from 'node:child_process';

function stopOwnedTree(child, startedAt) {
  if (!child.pid) return;
  if (process.platform === 'win32') {
    // Enumerate descendants even if their parent has already exited but left
    // inherited pipes open. Recheck creation time before stopping a captured PID.
    const script = `$minimum=[DateTimeOffset]::FromUnixTimeMilliseconds(${startedAt}).UtcDateTime;`
      + `$pending=[Collections.Generic.Queue[int]]::new();$pending.Enqueue(${child.pid});$owned=@();`
      + 'while($pending.Count){$parent=$pending.Dequeue();foreach($item in @(Get-CimInstance Win32_Process -Filter "ParentProcessId = $parent")){'
      + 'if($item.CreationDate.ToUniversalTime() -ge $minimum){$owned+= $item;$pending.Enqueue([int]$item.ProcessId)}}};'
      + 'foreach($item in @($owned | Sort-Object CreationDate -Descending)){$current=Get-CimInstance Win32_Process -Filter "ProcessId = $($item.ProcessId)";'
      + 'if($current -and $current.CreationDate -eq $item.CreationDate){Stop-Process -Id $item.ProcessId -Force -ErrorAction SilentlyContinue}}';
    try { execFileSync('powershell.exe', ['-NoLogo', '-NoProfile', '-NonInteractive', '-Command', script], { windowsHide: true, timeout: 10000, stdio: 'ignore' }); }
    finally { child.kill('SIGKILL'); }
  } else {
    try { process.kill(-child.pid, 'SIGKILL'); } catch (error) { if (error.code !== 'ESRCH') throw error; }
  }
}

export async function runGraphProcess(command, args, options = {}) {
  const timeout = options.timeout ?? 600000;
  const maxBuffer = options.maxBuffer ?? 64 * 1024 * 1024;
  if (!Number.isSafeInteger(timeout) || timeout < 1 || timeout > 3600000) throw new Error('Graph process timeout must be 1..3600000ms.');
  if (!Number.isSafeInteger(maxBuffer) || maxBuffer < 1) throw new Error('Graph process output limit must be positive.');
  if (options.signal?.aborted) throw options.signal.reason ?? new Error('Graph process cancelled.');
  return new Promise((resolve, reject) => {
    const startedAt = Date.now();
    const child = spawn(command, args, { cwd: options.cwd, env: options.env,
      windowsHide: true, detached: process.platform !== 'win32', stdio: ['pipe', 'pipe', 'pipe'] });
    const stdout = [], stderr = [];
    let bytes = 0, failure, timer, cleanupTimer, settled = false;
    const finish = (code, signal) => {
      if (settled) return;
      settled = true;
      clearTimeout(timer);
      clearTimeout(cleanupTimer);
      options.signal?.removeEventListener('abort', abort);
      const out = Buffer.concat(stdout).toString('utf8'), err = Buffer.concat(stderr).toString('utf8');
      if (failure || code !== 0) reject(new Error(`${command} failed (exit=${code}, signal=${signal ?? 'none'}):\n`
        + `${failure?.message ?? ''}\nstdout:\n${out}\nstderr:\n${err}`, { cause: failure }));
      else resolve(out);
    };
    const stop = error => {
      if (settled || failure) return;
      failure = error;
      try { stopOwnedTree(child, startedAt); } catch (cleanupError) { failure = new AggregateError([error, cleanupError], `${error.message}; process cleanup failed`); }
      // An exited parent can leave pipes open in descendants. Cleanup has run;
      // close our handles as well so a failed cleanup cannot wait indefinitely.
      child.stdin.destroy(); child.stdout.destroy(); child.stderr.destroy();
      cleanupTimer = setTimeout(() => finish(child.exitCode, child.signalCode), 10000);
    };
    const abort = () => stop(options.signal.reason ?? new Error('Graph process cancelled.'));
    const read = target => chunk => {
      const remaining = Math.max(0, maxBuffer - bytes);
      if (remaining) target.push(chunk.subarray(0, remaining));
      bytes += chunk.length;
      if (bytes > maxBuffer) stop(new Error(`Graph process output exceeded ${maxBuffer} bytes.`));
    };
    child.stdout.on('data', read(stdout)); child.stderr.on('data', read(stderr));
    child.on('error', error => { failure ??= error; });
    child.on('close', finish);
    child.stdin.on('error', error => { if (error.code !== 'EPIPE' && error.code !== 'ERR_STREAM_DESTROYED') stop(error); });
    timer = setTimeout(() => stop(new Error(`Graph process timed out after ${timeout}ms.`)), timeout);
    options.signal?.addEventListener('abort', abort, { once: true });
    if (options.signal?.aborted) abort();
    else child.stdin.end(options.input ?? '');
  });
}
