import { mkdirSync } from 'node:fs';
import { spawnSync } from 'node:child_process';
import path from 'node:path';

const workspaceRoot = process.cwd();
const tempDir = path.join(workspaceRoot, '.tmp', 'lighthouse');

mkdirSync(tempDir, { recursive: true });

const sharedEnv = {
    ...process.env,
    TMP: tempDir,
    TEMP: tempDir,
    TMPDIR: tempDir,
};

const npxCommand = process.platform === 'win32' ? 'npx' : 'npx';

function run(command) {
    const result = spawnSync(command, {
        cwd: workspaceRoot,
        env: sharedEnv,
        stdio: 'inherit',
        shell: true,
    });

    if (result.error) {
        throw result.error;
    }

    if (result.status !== 0) {
        process.exit(result.status ?? 1);
    }
}

run(`${npxCommand} lhci collect --config=.lighthouserc.json`);
run(`${npxCommand} lhci upload --config=.lighthouserc.json`);
