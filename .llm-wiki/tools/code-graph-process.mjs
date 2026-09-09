import { spawnSync } from 'node:child_process';

export function runGraphProcess(command, args, options = {}) {
  const result = spawnSync(command, args, {
    encoding: 'utf8', maxBuffer: 64 * 1024 * 1024, ...options,
    stdio: ['pipe', 'pipe', 'pipe'],
  });
  if (result.error || result.status !== 0) {
    // MSBuild writes compiler diagnostics to stdout. Keep both streams in the
    // exception so outer PowerShell transcripts retain the original failure.
    throw new Error(`${command} failed (exit=${result.status}, signal=${result.signal ?? 'none'}):\n`
      + `${result.error?.message ?? ''}\nstdout:\n${result.stdout ?? ''}\nstderr:\n${result.stderr ?? ''}`,
    { cause: result.error });
  }
  return result.stdout;
}
