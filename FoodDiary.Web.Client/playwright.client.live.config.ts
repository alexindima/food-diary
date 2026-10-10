import { resolve } from 'node:path';

import { defineConfig } from '@playwright/test';

const runtimeName = process.env['FOODDIARY_E2E_RUNTIME'];
const runId = process.env['FOODDIARY_E2E_RUN_ID'];
if (!/^[a-z][a-z0-9-]{0,39}$/u.test(runtimeName ?? '') || !/^[a-f0-9-]{36}$/u.test(runId ?? '')) {
    throw new Error('Use the owned journey driver with a runtime slug and unique run ID');
}
const evidenceDirectory = resolve('..', '.artifacts/reliability', runtimeName ?? '', `journeys-${runId ?? ''}`);

export default defineConfig({
    testDir: './e2e/client-live',
    timeout: 90_000,
    expect: { timeout: 20_000 },
    workers: 1,
    retries: 0,
    reporter: [['list'], ['json', { outputFile: resolve(evidenceDirectory, 'results.json') }]],
    outputDir: resolve(evidenceDirectory, 'browser'),
    use: { browserName: 'chromium', viewport: { width: 1280, height: 900 }, timezoneId: 'UTC', trace: 'off', screenshot: 'off' },
});
