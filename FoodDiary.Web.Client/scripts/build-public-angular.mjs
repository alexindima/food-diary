import { spawnSync } from 'node:child_process';

// Angular's second bundling pass can change browser chunk bytes under an existing URL.
// Keep the original esbuild output so retained releases can share immutable assets safely.
const result = spawnSync(process.execPath, ['node_modules/@angular/cli/bin/ng.js', 'build', ...process.argv.slice(2)], {
    env: { ...process.env, NG_BUILD_OPTIMIZE_CHUNKS: 'false' },
    stdio: 'inherit',
});
if (result.error) throw result.error;
process.exitCode = result.status ?? 1;
