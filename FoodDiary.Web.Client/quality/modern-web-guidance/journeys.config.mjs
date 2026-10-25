import path from 'node:path';

const frontend = path.resolve(import.meta.dirname, '../..');
export default {
    testDir: path.join(frontend, 'e2e'),
    timeout: 30_000,
    fullyParallel: false,
    workers: 1,
    retries: 0,
    forbidOnly: Boolean(process.env.CI),
    failOnFlakyTests: true,
    outputDir: process.env.FD_WEB_QUALITY_RESULTS,
    use: {
        baseURL: 'http://127.0.0.1:4318',
        headless: true,
        channel: process.env.FD_WEB_QUALITY_CHANNEL ?? 'chrome',
        reducedMotion: 'reduce',
        trace: 'retain-on-failure',
        screenshot: 'only-on-failure',
    },
    projects: [
        {
            name: 'client-guidance',
            testMatch: 'client-smoke/client-smoke.spec.ts',
            grep: /authenticated accessibility|client auth smoke|mobile navigation keyboard regression|meal editing regression|product creation behavior/u,
        },
        {
            name: 'admin-guidance',
            testMatch: ['admin-smoke/admin-smoke.spec.ts', 'admin-smoke/moderation-workflow.spec.ts'],
            use: { baseURL: 'http://127.0.0.1:4319' },
        },
        { name: 'platform-guidance', testMatch: 'web-guidance/*.spec.ts' },
    ],
    webServer: [
        {
            command: 'node node_modules/@angular/cli/bin/ng.js serve --host 127.0.0.1 --port 4318',
            url: 'http://127.0.0.1:4318',
            cwd: frontend,
            reuseExistingServer: false,
            timeout: 180_000,
        },
        {
            command: 'node node_modules/@angular/cli/bin/ng.js serve fooddiary-admin --host 127.0.0.1 --port 4319',
            url: 'http://127.0.0.1:4319',
            cwd: frontend,
            reuseExistingServer: false,
            timeout: 180_000,
        },
    ],
};
