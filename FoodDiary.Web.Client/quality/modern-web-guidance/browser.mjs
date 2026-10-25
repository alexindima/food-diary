import { mkdir } from 'node:fs/promises';
import { createRequire } from 'node:module';
import path from 'node:path';
import { pathToFileURL } from 'node:url';
import { platformBrowserChecks, inspectPlatformPage } from './browser-platform.mjs';

const require = createRequire(new URL('../../package.json', import.meta.url));
const dependency = async name => import(pathToFileURL(require.resolve(name)).href);
export const browserChecks = new Set([
    ...platformBrowserChecks,
    'browser:axe',
    'browser:document-language',
    'browser:page-title',
    'browser:main-landmark',
    'browser:horizontal-overflow',
    'browser:zoom-allowed',
    'browser:login-autocomplete',
    'browser:dialog-focus',
]);

export async function inspectPage(page, check, scenario) {
    if (platformBrowserChecks.has(check)) return inspectPlatformPage(page, check, scenario);
    if (check === 'browser:axe') {
        const module = await dependency('@axe-core/playwright');
        const AxeBuilder = module.default;
        const forcedColors = await page.evaluate(() => matchMedia('(forced-colors: active)').matches);
        const builder = new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21aa', 'wcag22aa']);
        // axe mixes unforced -webkit-text-fill-color with the forced background:
        // https://github.com/dequelabs/axe-core/issues/3978. Keep semantic checks
        // in the requested mode and measure author contrast in normal colors.
        const result = await (forcedColors ? builder.disableRules(['color-contrast']) : builder).analyze();
        if (forcedColors) {
            await page.emulateMedia({ forcedColors: 'none' });
            try {
                const contrast = await new AxeBuilder({ page }).withRules(['color-contrast']).analyze();
                for (const key of ['passes', 'violations', 'incomplete']) result[key].push(...contrast[key]);
            } finally {
                await page.emulateMedia({ forcedColors: 'active' });
            }
        }
        return {
            observations: result.passes.length + result.violations.length + result.incomplete.length,
            findings: result.violations.map(violation => ({
                message: `${violation.id}: ${violation.help}`,
                targets: violation.nodes.map(node => node.target.join(' ')),
                url: violation.helpUrl,
            })),
            detail: `axe ${result.testEngine.version}: ${result.incomplete.length} checks also need manual review.${forcedColors ? ' Author contrast measured in normal colors (axe #3978); system-palette contrast remains manual.' : ''}`,
            manual: result.incomplete.map(item => ({ id: item.id, reason: item.help })),
        };
    }
    if (check === 'browser:dialog-focus') {
        const dialog = page.getByRole('dialog').filter({ visible: true });
        if ((await dialog.count()) !== 1) return { observations: 1, findings: [{ message: 'Expected exactly one visible dialog.' }] };
        const findings = [];
        if (!(await dialog.evaluate(element => element.contains(document.activeElement))))
            findings.push({ message: 'Opening the dialog did not move focus inside it.' });
        const candidates = await dialog.locator('button, a[href], input, select, textarea, iframe, [tabindex]').elementHandles();
        const tabbable = [];
        for (const element of candidates)
            if (
                (await element.isVisible()) &&
                (await element.evaluate(
                    element => element.tabIndex >= 0 && !element.disabled && element.getAttribute('aria-hidden') !== 'true',
                ))
            )
                tabbable.push(element);
        if (!tabbable.length) return { observations: 1, findings: [{ message: 'Dialog has no visible keyboard controls.' }] };
        await tabbable[0].focus();
        await page.keyboard.press('Shift+Tab');
        // A native modal may send Tab to browser chrome (activeElement becomes body).
        // That is valid UA navigation; focusing background page controls is not.
        const nativeBrowserNavigation = () =>
            dialog.evaluate(element => element.matches('dialog:modal') && document.activeElement === document.body);
        if (!(await dialog.evaluate(element => element.contains(document.activeElement))) && !(await nativeBrowserNavigation()))
            findings.push({ message: 'Reverse Tab escaped the modal control boundary.' });
        // Provider widgets can add controls after opening. Start from the actual
        // reverse-Tab destination instead of a stale snapshot of the last control.
        let wrapped = false;
        for (let index = 0; index < tabbable.length + 4; index++) {
            await page.keyboard.press('Tab');
            if ((await tabbable[0].evaluate(element => element === document.activeElement)) || (await nativeBrowserNavigation())) {
                wrapped = true;
                break;
            }
            if (!(await dialog.evaluate(element => element.contains(document.activeElement)))) break;
        }
        if (!wrapped) findings.push({ message: 'Tab escaped or failed to cycle within the modal control boundary.' });
        await page.keyboard.press('Escape');
        await dialog.waitFor({ state: 'hidden' });
        const opener = page.getByRole('button', { name: scenario.openDialog, exact: true }).first();
        if (!(await opener.evaluate(element => element === document.activeElement || element.contains(document.activeElement))))
            findings.push({ message: 'Closing the dialog did not restore focus to its opener.' });
        return { observations: 5, findings, detail: 'Checked initial focus, forward/reverse tab wrap, Escape and focus restoration.' };
    }
    return page.evaluate(
        ({ check, locale }) => {
            const findings = [];
            let observations = 1;
            const fail = message => findings.push({ message });
            if (check === 'browser:document-language' && !document.documentElement.lang.toLowerCase().startsWith(locale))
                fail(`Document language does not match ${locale}.`);
            if (check === 'browser:page-title' && (!document.title.trim() || /^(?:PUBLIC_SITE|LANDING)[A-Z_.]+$/u.test(document.title)))
                fail('Page title is empty or appears to be an untranslated key.');
            if (check === 'browser:main-landmark' && document.querySelectorAll('main').length !== 1)
                fail('Expected one native main landmark.');
            if (check === 'browser:horizontal-overflow' && document.documentElement.scrollWidth > innerWidth + 2)
                fail(`Page overflows horizontally: ${document.documentElement.scrollWidth}px at ${innerWidth}px.`);
            if (check === 'browser:zoom-allowed') {
                const viewport = document.querySelector('meta[name="viewport"]')?.content ?? '';
                if (/user-scalable\s*=\s*(?:no|0)|maximum-scale\s*=\s*(?:0(?:\.\d+)?|1(?:\.0+)?)\s*(?:,|$)/iu.test(viewport))
                    fail('Viewport metadata restricts user zoom.');
            }
            if (check === 'browser:login-autocomplete') {
                const root = document.querySelector('[role="dialog"], dialog[open]');
                const email = root?.querySelector('input[type="email"]');
                const password = root?.querySelector('input[autocomplete="current-password"], input[type="password"]');
                observations = 2;
                if (!email || email.getAttribute('autocomplete')?.trim().toLowerCase() !== 'username')
                    fail('Rendered login email must use autocomplete="username".');
                if (!password || password.getAttribute('autocomplete')?.trim().toLowerCase() !== 'current-password')
                    fail('Rendered login password must use autocomplete="current-password".');
            }
            return { observations, findings };
        },
        { check, locale: scenario.locale },
    );
}

export async function runBrowser({
    policy,
    baseURL,
    scenarios,
    artifactDirectory,
    browserName = 'chromium',
    channel = process.env.FD_WEB_QUALITY_CHANNEL ?? 'chrome',
}) {
    const url = new URL(baseURL);
    if (!['http:', 'https:'].includes(url.protocol) || url.username || url.password || url.search || url.hash || url.pathname !== '/')
        throw new Error('Browser audit requires an HTTP(S) origin without credentials.');
    if (!['chromium', 'firefox', 'webkit'].includes(browserName)) throw new Error('Unknown browser engine.');
    if (
        !Array.isArray(scenarios) ||
        !scenarios.length ||
        new Set(scenarios.map(item => item.id)).size !== scenarios.length ||
        scenarios.some(
            item =>
                !/^[a-z0-9-]+$/u.test(item.id) ||
                !item.path?.startsWith('/') ||
                item.path.startsWith('//') ||
                new URL(item.path, url).origin !== url.origin ||
                !policy.scopes.some(scope => scope.id === item.scope) ||
                !['en', 'ru'].includes(item.locale) ||
                !Number.isInteger(item.viewport?.width) ||
                !Number.isInteger(item.viewport?.height),
        )
    )
        throw new Error('Invalid browser scenario matrix.');
    const requirements = policy.requirements.filter(requirement => requirement.engine === 'browser');
    for (const requirement of requirements)
        if (!browserChecks.has(requirement.check)) throw new Error(`Unknown browser check: ${requirement.check}`);
    const playwright = await dependency('@playwright/test');
    const browserType = (playwright.default ?? playwright)[browserName];
    const browser = await browserType.launch({ headless: true, ...(browserName === 'chromium' ? { channel } : {}) });
    const records = [];
    await mkdir(path.join(artifactDirectory, 'screenshots'), { recursive: true });
    try {
        for (const scenario of scenarios) {
            const selected = requirements.filter(
                requirement =>
                    requirement.scopes.includes(scenario.scope) &&
                    (!requirement.scenarios || requirement.scenarios.includes(scenario.kind)),
            );
            const context = await browser.newContext({
                viewport: scenario.viewport,
                locale: scenario.locale,
                colorScheme: scenario.colorScheme ?? 'light',
                reducedMotion: scenario.reducedMotion ?? 'no-preference',
                forcedColors: scenario.forcedColors ?? 'none',
                serviceWorkers: 'block',
            });
            const page = await context.newPage();
            page.setDefaultTimeout(15_000);
            const runtimeErrors = [];
            page.on('pageerror', error => runtimeErrors.push(error.message));
            try {
                const response = await page.goto(new URL(scenario.path, url).href, { waitUntil: 'domcontentloaded' });
                if (!response || response.status() >= 400) throw new Error(`Page returned HTTP ${response?.status() ?? 'no response'}.`);
                await page
                    .locator(scenario.readySelector ?? 'html[data-app-ready][data-i18n-ready] fd-root main')
                    .waitFor({ state: 'visible' });
                if (new URL(page.url()).pathname !== scenario.path)
                    throw new Error(`Unexpected route after readiness: ${new URL(page.url()).pathname}`);
                if ((await page.locator('main').innerText()).trim().length < 30) throw new Error('Expected meaningful page content.');
                if (scenario.openDialog) {
                    const opener = page.getByRole('button', { name: scenario.openDialog, exact: true }).first();
                    await opener.focus();
                    await page.keyboard.press('Enter');
                    await page.getByRole('dialog').waitFor({ state: 'visible' });
                    await page.waitForFunction(() => {
                        const dialog = document.querySelector('[role="dialog"], dialog[open]');
                        return !document.getAnimations().some(animation => {
                            const timing = animation.effect?.getComputedTiming();
                            return (
                                dialog?.contains(animation.effect?.target) &&
                                animation.playState === 'running' &&
                                Number.isFinite(timing?.endTime)
                            );
                        });
                    });
                }
                // Focus restoration closes the dialog, so inspect it last.
                for (const requirement of [...selected].sort(
                    (a, b) => Number(a.check === 'browser:dialog-focus') - Number(b.check === 'browser:dialog-focus'),
                )) {
                    try {
                        const observation = await inspectPage(page, requirement.check, scenario);
                        records.push({ requirement: requirement.id, case: scenario.id, ...observation });
                    } catch (error) {
                        records.push({
                            requirement: requirement.id,
                            case: scenario.id,
                            error: error.message,
                            observations: 0,
                            findings: [],
                        });
                    }
                }
                if (new URL(page.url()).pathname !== scenario.path)
                    throw new Error(`Route changed during audit: ${new URL(page.url()).pathname}`);
                if (runtimeErrors.length) throw new Error(`Page runtime errors: ${runtimeErrors.join('; ')}`);
                await page.screenshot({ path: path.join(artifactDirectory, 'screenshots', `${scenario.id}.png`), fullPage: true });
            } catch (error) {
                await page
                    .screenshot({
                        path: path.join(artifactDirectory, 'screenshots', `${scenario.id}-error.png`),
                        fullPage: true,
                        animations: 'disabled',
                    })
                    .catch(() => {});
                const diagnostic = runtimeErrors.length ? `${error.message}\nRuntime: ${runtimeErrors.join('; ')}` : error.message;
                for (const requirement of selected) {
                    const existing = records.find(record => record.case === scenario.id && record.requirement === requirement.id);
                    if (existing) existing.error = diagnostic;
                    else
                        records.push({
                            requirement: requirement.id,
                            case: scenario.id,
                            error: diagnostic,
                            observations: 0,
                            findings: [],
                        });
                }
            } finally {
                await context.close();
            }
        }
    } finally {
        await browser.close();
    }
    return {
        metadata: {
            browser: browserName,
            channel: browserName === 'chromium' ? channel : undefined,
            version: browser.version(),
            origin: url.origin,
            scenarios,
        },
        results: requirements.map(requirement => {
            const matching = records.filter(record => record.requirement === requirement.id);
            const failures = matching.flatMap(record => record.findings.map(finding => ({ ...finding, case: record.case })));
            const errors = matching.filter(record => record.error);
            return {
                requirement: requirement.id,
                engine: 'browser',
                status: errors.length || !matching.length ? 'error' : failures.length ? 'fail' : 'pass',
                observations: matching.reduce((count, record) => count + record.observations, 0),
                findings: failures,
                detail: errors.length
                    ? errors.map(record => `${record.case}: ${record.error}`).join('\n')
                    : `Inspected ${matching.length} browser scenarios.`,
                cases: matching.map(record => ({
                    id: record.case,
                    error: record.error,
                    observations: record.observations,
                    detail: record.detail,
                    manual: record.manual,
                })),
            };
        }),
    };
}
