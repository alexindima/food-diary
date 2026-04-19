import { defineConfig } from '@playwright/test';

export default defineConfig({
    testDir: './e2e/client-smoke',
    timeout: 30_000,
    fullyParallel: false,
    retries: 0,
    reporter: 'list',
    use: {
        baseURL: 'http://127.0.0.1:4201',
        trace: 'retain-on-failure',
        screenshot: 'only-on-failure',
        video: 'retain-on-failure',
        headless: true,
    },
    webServer: {
        command: 'npx ng serve --host 127.0.0.1 --port 4201',
        url: 'http://127.0.0.1:4201',
        reuseExistingServer: true,
        timeout: 120_000,
        cwd: '.',
    },
});
