import path from 'node:path';

const frontend = path.resolve(import.meta.dirname, '../..');
export default {
    testDir: path.join(frontend, 'e2e/client-smoke'),
    grep: /authenticated accessibility/u,
    timeout: 30_000,
    fullyParallel: false,
    workers: 1,
    retries: 0,
    outputDir: process.env.FD_WEB_QUALITY_RESULTS,
    use: {
        baseURL: 'http://127.0.0.1:4318',
        headless: true,
        channel: process.env.FD_WEB_QUALITY_CHANNEL ?? 'chrome',
        reducedMotion: 'reduce',
        trace: 'retain-on-failure',
        screenshot: 'only-on-failure',
    },
    webServer: {
        command: 'node node_modules/@angular/cli/bin/ng.js serve --host 127.0.0.1 --port 4318',
        url: 'http://127.0.0.1:4318',
        cwd: frontend,
        reuseExistingServer: false,
        timeout: 180_000,
    },
};
