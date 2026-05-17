import { defineConfig } from '@playwright/test';

export default defineConfig({
    testDir: './e2e/admin-smoke',
    timeout: 30_000,
    fullyParallel: false,
    retries: 0,
    reporter: 'list',
    use: {
        baseURL: 'http://127.0.0.1:4301',
        trace: 'retain-on-failure',
        screenshot: 'only-on-failure',
        video: 'retain-on-failure',
        headless: true,
    },
    webServer: {
        command: 'npx ng serve fooddiary-admin --host 127.0.0.1 --port 4301',
        url: 'http://127.0.0.1:4301',
        reuseExistingServer: true,
        timeout: 120_000,
        cwd: '.',
    },
});
