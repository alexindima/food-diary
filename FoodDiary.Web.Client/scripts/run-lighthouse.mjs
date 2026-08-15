import { createServer } from 'node:http';
import { mkdirSync, readFileSync, statSync, writeFileSync } from 'node:fs';
import path from 'node:path';

import { launch } from 'chrome-launcher';
import lighthouse from 'lighthouse';

const workspaceRoot = process.cwd();
const staticRoot = path.resolve(workspaceRoot, 'dist', 'browser');
const reportsRoot = path.resolve(workspaceRoot, 'reports', 'lighthouse');
const routes = ['/', '/food-diary', '/meal-planner', '/intermittent-fasting', '/privacy-policy'];
const contentTypes = new Map([
    ['.css', 'text/css; charset=utf-8'],
    ['.html', 'text/html; charset=utf-8'],
    ['.ico', 'image/x-icon'],
    ['.js', 'text/javascript; charset=utf-8'],
    ['.json', 'application/json; charset=utf-8'],
    ['.png', 'image/png'],
    ['.svg', 'image/svg+xml'],
    ['.webp', 'image/webp'],
    ['.woff2', 'font/woff2'],
]);

mkdirSync(reportsRoot, { recursive: true });

const server = createServer((request, response) => {
    const pathname = decodeURIComponent(new URL(request.url ?? '/', 'http://localhost').pathname);
    const relativePath = pathname.slice(1);
    const routePath =
        pathname === '/' ? 'index.html' : path.extname(relativePath) === '' ? path.join(relativePath, 'index.html') : relativePath;
    const requestedPath = path.resolve(staticRoot, routePath);
    const filePath = requestedPath.startsWith(`${staticRoot}${path.sep}`) ? requestedPath : path.join(staticRoot, 'index.html');

    try {
        if (!statSync(filePath).isFile()) {
            throw new Error('Not a file');
        }
        response.writeHead(200, { 'Content-Type': contentTypes.get(path.extname(filePath)) ?? 'application/octet-stream' });
        response.end(readFileSync(filePath));
    } catch {
        response.writeHead(404);
        response.end('Not found');
    }
});

await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
const address = server.address();
if (address === null || typeof address === 'string') {
    throw new Error('Unable to determine the Lighthouse server port.');
}

const chrome = await launch({ chromeFlags: ['--headless', '--no-sandbox'] });

try {
    for (const route of routes) {
        const result = await lighthouse(`http://127.0.0.1:${address.port}${route}`, {
            port: chrome.port,
            output: ['html', 'json'],
            logLevel: 'error',
        });
        if (result === undefined || !Array.isArray(result.report)) {
            throw new Error(`Lighthouse did not produce reports for ${route}.`);
        }

        const reportName = route === '/' ? 'index' : route.slice(1).replaceAll('/', '-');
        writeFileSync(path.join(reportsRoot, `${reportName}.html`), result.report[0]);
        writeFileSync(path.join(reportsRoot, `${reportName}.json`), result.report[1]);
    }
} finally {
    try {
        await chrome.kill();
    } catch (error) {
        if (!(error instanceof Error) || !('code' in error) || error.code !== 'EPERM') {
            throw error;
        }
    }
    await new Promise((resolve, reject) => server.close(error => (error === undefined ? resolve() : reject(error))));
}
