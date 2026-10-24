import { spawn } from 'node:child_process';
import { createWriteStream } from 'node:fs';
import { mkdir } from 'node:fs/promises';
import path from 'node:path';

export async function runNode(args, { cwd, log, env = {}, timeout = 600_000 }) {
    await mkdir(path.dirname(log), { recursive: true });
    const output = createWriteStream(log);
    const child = spawn(process.execPath, args, {
        cwd,
        env: { ...process.env, ...env },
        windowsHide: true,
        stdio: ['ignore', 'pipe', 'pipe'],
    });
    child.stdout.pipe(output, { end: false });
    child.stderr.pipe(output, { end: false });
    let timedOut = false;
    const timer = setTimeout(() => {
        timedOut = true;
        if (process.platform === 'win32')
            spawn('taskkill', ['/PID', String(child.pid), '/T', '/F'], { windowsHide: true, stdio: 'ignore' });
        else child.kill('SIGTERM');
    }, timeout);
    try {
        const code = await new Promise((resolve, reject) => {
            child.once('error', reject);
            child.once('close', resolve);
        });
        if (timedOut) throw new Error(`Owned process timed out; see ${log}`);
        return code;
    } finally {
        clearTimeout(timer);
        output.end();
    }
}
