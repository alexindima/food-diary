import { mkdir, writeFile } from 'node:fs/promises';
import { isAbsolute, relative, resolve } from 'node:path';

const repositoryRoot = resolve(import.meta.dirname, '../..');
const safeId = value => typeof value === 'string' && /^[a-z0-9][a-z0-9-]{0,59}$/.test(value);

export async function readWebmcpDiagnostics(page) {
  const text = await page.locator('meta[name="fooddiary-webmcp"]').getAttribute('content', { timeout: 1000 }).catch(() => null);
  if (!text) return { status: 'not-published' };
  try {
    const value = JSON.parse(text);
    const reasons = ['ready', 'production-build', 'non-local-host', 'browser-api-unavailable'];
    const actions = ['use-supported-browser', 'sign-in', 'reload-and-inspect-registration', 'fetch-tool-catalog', 'wait-for-registration'];
    return {
      status: 'published', reason: reasons.includes(value.reason) ? value.reason : 'unknown',
      nextAction: actions.includes(value.nextAction) ? value.nextAction : 'unknown',
      authenticated: value.authenticated === true,
      registeredToolCount: Number.isSafeInteger(value.registeredToolCount) && value.registeredToolCount >= 0 ? value.registeredToolCount : null,
    };
  } catch { return { status: 'invalid-diagnostics' }; }
}

/** The caller supplies an existing Playwright Browser and real owner-specific scenarios.
 * Each matrix cell owns a fresh context; this helper never closes the caller's browser.
 */
export async function runControlledScenarios({ browser, baseURL, outputDirectory, scenarios, locales = ['ru', 'en'],
  viewports = [{ width: 390, height: 844 }, { width: 1440, height: 1000 }] }) {
  const url = new URL(baseURL);
  if (!['http:', 'https:'].includes(url.protocol) || !['localhost', '127.0.0.1', '[::1]'].includes(url.hostname)
      || url.username || url.password) throw new Error('Controlled scenarios require a local application URL without credentials.');
  const output = resolve(outputDirectory);
  const outputRelative = relative(resolve(repositoryRoot, '.artifacts'), output);
  if (!outputRelative || outputRelative.startsWith('..') || isAbsolute(outputRelative)) throw new Error('Choose a task-owned directory below repository .artifacts.');
  if (!Array.isArray(scenarios) || scenarios.length === 0 || scenarios.some(item => !safeId(item.id) || typeof item.exercise !== 'function')
      || new Set(scenarios.map(item => item.id)).size !== scenarios.length
      || locales.length === 0 || new Set(locales).size !== locales.length || locales.some(locale => !['ru', 'en'].includes(locale))
      || viewports.length === 0 || new Set(viewports.map(view => `${view.width}x${view.height}`)).size !== viewports.length
      || viewports.some(view => !Number.isInteger(view.width) || !Number.isInteger(view.height)
        || view.width < 320 || view.width > 3840 || view.height < 240 || view.height > 2160)
      || scenarios.length * locales.length * viewports.length > 64) throw new Error('Invalid or oversized controlled scenario matrix.');
  await mkdir(output, { recursive: true });
  const report = { schemaVersion: 1, origin: url.origin, cases: [] };
  await writeFile(resolve(output, 'report.json'), `${JSON.stringify(report, null, 2)}\n`, { flag: 'wx' });
  for (const scenario of scenarios) for (const locale of locales) for (const viewport of viewports) {
    const id = `${scenario.id}-${locale}-${viewport.width}x${viewport.height}`;
    const context = await browser.newContext({ locale, viewport, reducedMotion: 'reduce' });
    const result = { id, scenario: scenario.id, locale, viewport, status: 'failed' };
    try {
      const page = await context.newPage();
      const inputs = { locale, viewport, baseURL: url.href };
      await scenario.prepare?.(page, inputs);
      await page.goto(url.href);
      await scenario.exercise(page, inputs);
      result.webmcp = await readWebmcpDiagnostics(page);
      await page.screenshot({ path: resolve(output, `${id}.png`), fullPage: true });
      result.status = 'passed';
    } catch (error) {
      // Assertion text and page errors can contain private response or form data.
      result.failure = { kind: ['Error', 'AssertionError', 'TimeoutError', 'TypeError'].includes(error?.name) ? error.name : 'Error' };
    } finally {
      await context.close();
      report.cases.push(result);
      await writeFile(resolve(output, 'report.json'), `${JSON.stringify(report, null, 2)}\n`);
    }
  }
  if (report.cases.some(item => item.status !== 'passed')) throw new Error(`Controlled scenarios failed; inspect ${resolve(output, 'report.json')}`);
  return report;
}
