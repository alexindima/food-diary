import path from 'node:path';
const frontend = path.resolve(import.meta.dirname, '../..');
export default {
    testDir: path.join(frontend, 'e2e/web-guidance'),
    timeout: 45_000,
    fullyParallel: false,
    workers: 1,
    retries: 0,
    forbidOnly: Boolean(process.env.CI),
    failOnFlakyTests: true,
    use: {
        baseURL: 'http://127.0.0.1:4320',
        channel: process.env.FD_WEB_QUALITY_CHANNEL ?? 'chrome',
        headless: true,
        reducedMotion: 'reduce',
        trace: 'retain-on-failure',
        screenshot: 'only-on-failure',
    },
    outputDir: process.env.FD_WEB_QUALITY_RESULTS ?? path.join(frontend, '../.artifacts/web-quality/platform-results'),
    webServer: {
        command: 'node node_modules/@angular/cli/bin/ng.js serve --host 127.0.0.1 --port 4320',
        url: 'http://127.0.0.1:4320',
        cwd: frontend,
        reuseExistingServer: false,
        timeout: 180_000,
    },
};
