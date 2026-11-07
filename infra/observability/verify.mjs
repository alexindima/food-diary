#!/usr/bin/env node
// Cross-platform entry point for the repository's executed-evidence runner.
// Python owns the behavioral fixtures; this wrapper uses no shell interpolation.
import { existsSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const directory = dirname(fileURLToPath(import.meta.url));
const root = resolve(directory, '../..');
const args = new Set(process.argv.slice(2));
for (const argument of args) {
    if (!['--frontend-only', '--prometheus', '--pricing-only', '--slo-only'].includes(argument)) {
        throw new Error(`Unknown verification option: ${argument}`);
    }
}
const localPython = join(root, '.artifacts/observability-venv', process.platform === 'win32' ? 'Scripts/python.exe' : 'bin/python');
const python = existsSync(localPython) ? localPython : process.platform === 'win32' ? 'python' : 'python3';

function run(executable, arguments_, cwd = directory) {
    const result = spawnSync(executable, arguments_, { cwd, stdio: 'inherit', shell: false });
    if (result.error) throw result.error;
    if (result.status !== 0) process.exit(result.status ?? 1);
}

if (args.has('--frontend-only')) {
    run(python, ['-m', 'unittest', 'test_observability.FrontendPipelineTests']);
} else {
    run(python, ['-m', 'unittest', 'discover', '-s', '.', '-p', 'test_*.py']);
}
if (args.has('--prometheus') || args.has('--pricing-only') || args.has('--slo-only')) {
    const files = args.has('--slo-only') ? ['fooddiary-coverage-tests.yml'] : args.has('--pricing-only') ? ['fooddiary-pricing-tests.yml'] : [
        'fooddiary-alert-tests.yml', 'fooddiary-coverage-tests.yml', 'fooddiary-pricing-tests.yml',
    ];
    run('docker', ['run', '--rm', '--mount', `type=bind,source=${join(directory, 'prometheus')},target=/rules,readonly`,
        '--entrypoint', '/bin/promtool',
        'prom/prometheus@sha256:339ce86a59413be18d0e445472891d022725b4803fab609069110205e79fb2f1',
        'test', 'rules', ...(args.has('--slo-only') ? ['--run=internal polling must not qualify low-volume user errors'] : []),
        ...files.map(file => `/rules/${file}`)], root);
}
