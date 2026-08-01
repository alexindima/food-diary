import fs from 'node:fs';
import path from 'node:path';
import { createRequire } from 'node:module';

const repositoryRoot = path.resolve(import.meta.dirname, '../..');
const require = createRequire(path.join(repositoryRoot, 'FoodDiary.Web.Client/package.json'));
const { chromium } = require('@playwright/test');
const options = JSON.parse(fs.readFileSync(process.argv[2], 'utf8'));
const browser = await chromium.launch({ headless: true });
const errors = [];
try {
    const contextOptions = { viewport: options.viewport };
    if (options.storageStatePath) contextOptions.storageState = options.storageStatePath;
    const context = await browser.newContext(contextOptions);
    const page = await context.newPage();
    page.on('console', message => { if (message.type() === 'error') errors.push(`console: ${message.text()}`); });
    page.on('pageerror', error => errors.push(`page: ${error.message}`));
    await page.goto(options.url, { waitUntil: 'networkidle' });
    if (options.triggerSelector) await page.locator(options.triggerSelector).click();
    await page.locator(options.fileSelector).setInputFiles(options.fixturePath);
    await page.locator(options.resultSelector).waitFor({ state: 'visible', timeout: options.timeoutMs });
    await page.screenshot({ path: options.screenshotPath, fullPage: true });
    if (errors.length > 0) throw new Error(errors.join('\n'));
    process.stdout.write(JSON.stringify({ passed: true, screenshotPath: options.screenshotPath, errors: [] }));
    await context.close();
} finally { await browser.close(); }
