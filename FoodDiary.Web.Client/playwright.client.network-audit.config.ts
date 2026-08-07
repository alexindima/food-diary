import { tmpdir } from 'node:os';
import { join } from 'node:path';

import { defineConfig } from '@playwright/test';

export default defineConfig({
    testDir: './e2e/client-smoke',
    timeout: 180_000,
    fullyParallel: false,
    retries: 0,
    reporter: 'list',
    grep: /@network-audit/u,
    outputDir: join(tmpdir(), 'food-diary-playwright', 'network-audit'),
    use: {
        baseURL: 'http://127.0.0.1:4201',
        trace: 'retain-on-failure',
        screenshot: 'off',
        video: 'off',
        headless: true,
        reducedMotion: 'reduce',
    },
    webServer: {
        command: 'npx ng serve --host 127.0.0.1 --port 4201',
        url: 'http://127.0.0.1:4201',
        reuseExistingServer: true,
        timeout: 120_000,
        cwd: '.',
    },
});
