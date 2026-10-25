export const platformBrowserChecks = new Set([
    'browser:page-structure',
    'browser:skip-link',
    'browser:text-resize',
    'browser:narrow-reflow',
    'browser:motion-preference',
    'browser:form-attributes',
    'browser:form-errors',
    'browser:resource-bounds',
    'browser:focus-visible',
]);

async function inspectBounds(page) {
    return page.evaluate(() => {
        const findings = [],
            exemptions = [];
        let observations = 1;
        if (document.documentElement.scrollWidth > innerWidth + 2)
            findings.push({ message: `Document requires horizontal scrolling at ${innerWidth}px.` });
        for (const element of document.querySelectorAll('button, input:not([type="hidden"]), select, textarea, a[href]')) {
            if (
                !element.checkVisibility({ checkOpacity: true, checkVisibilityCSS: true }) ||
                element.closest('[inert], [aria-hidden="true"]')
            )
                continue;
            const style = getComputedStyle(element),
                rect = element.getBoundingClientRect();
            if (
                style.clipPath !== 'none' ||
                !rect.width ||
                !rect.height ||
                element.closest('.fd-visually-hidden-focusable:not(:focus-within)')
            )
                continue;
            observations++;
            if (rect.left < -2 || rect.right > innerWidth + 2) {
                let region = element.parentElement,
                    scrollableTable = false;
                while (element.closest('table') && region && region !== document.body) {
                    const bounds = region.getBoundingClientRect();
                    if (
                        ['auto', 'scroll'].includes(getComputedStyle(region).overflowX) &&
                        region.scrollWidth > region.clientWidth + 2 &&
                        region.tabIndex >= 0 &&
                        (region.getAttribute('aria-label') || region.getAttribute('aria-labelledby')) &&
                        bounds.left >= -2 &&
                        bounds.right <= innerWidth + 2 &&
                        rect.width <= region.clientWidth
                    ) {
                        scrollableTable = true;
                        break;
                    }
                    region = region.parentElement;
                }
                if (scrollableTable) exemptions.push('Bounded, named, keyboard-scrollable data table (two-dimensional reflow exception).');
                else
                    findings.push({
                        message: `Control is clipped horizontally: ${element.tagName} ${element.getAttribute('aria-label') ?? element.textContent?.trim().slice(0, 60) ?? ''}`,
                    });
            }
        }
        return { observations, findings, exemptions };
    });
}

export async function inspectPlatformPage(page, check, scenario) {
    if (check === 'browser:focus-visible') {
        const root = scenario.openDialog ? page.getByRole('dialog') : page.locator('main');
        const candidates = root.locator('button, input:not([type="hidden"]), select, textarea, a[href]');
        const findings = [];
        let observations = 0;
        await page.keyboard.press('Tab');
        for (const element of (await candidates.elementHandles()).slice(0, 12)) {
            if (!(await element.isVisible()) || (await element.isDisabled())) continue;
            const before = await element.evaluate(node => {
                const owner = node.closest('.fd-ui-input__control-wrap, .fd-ui-select__control-wrap') ?? node;
                const style = getComputedStyle(owner);
                return { border: style.borderColor, shadow: style.boxShadow };
            });
            await element.focus();
            await page.evaluate(() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve))));
            const visible = await element.evaluate((node, before) => {
                const styles = [
                    getComputedStyle(node),
                    getComputedStyle(node.closest('.fd-ui-input__control-wrap, .fd-ui-select__control-wrap') ?? node),
                ];
                return styles.some(
                    style =>
                        (style.outlineStyle !== 'none' && Number.parseFloat(style.outlineWidth) >= 1) ||
                        (style.boxShadow !== 'none' && style.boxShadow !== before.shadow) ||
                        (style.borderColor !== before.border && Number.parseFloat(style.borderWidth) >= 1),
                );
            }, before);
            observations++;
            if (!visible)
                findings.push({
                    message: `No visible keyboard focus treatment on ${await element.evaluate(node => `${node.tagName}#${node.id}`)}`,
                });
        }
        return {
            observations,
            findings,
            detail: 'Keyboard modality and rendered outline/border/shadow, including forced-color scenarios.',
        };
    }
    if (check === 'browser:text-resize' || check === 'browser:narrow-reflow') {
        const previous = page.viewportSize();
        let style;
        try {
            if (check === 'browser:text-resize') style = await page.addStyleTag({ content: 'html {font-size: 200% !important;}' });
            else await page.setViewportSize({ width: 320, height: 900 });
            await page.evaluate(() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve))));
            const bounds = await inspectBounds(page);
            return {
                ...bounds,
                detail: `${check === 'browser:text-resize' ? '200% root text enlargement' : '320 CSS pixel reflow'} with actual control bounds; ${bounds.exemptions.length} controls in named keyboard-scrollable data tables.`,
            };
        } finally {
            if (style) await style.evaluate(element => element.remove());
            if (previous) await page.setViewportSize(previous);
        }
    }
    if (check === 'browser:skip-link') {
        const id = await page.locator('main').getAttribute('id');
        const links = page.locator('a[href]');
        const index = id ? await links.evaluateAll((anchors, id) => anchors.findIndex(anchor => anchor.hash === `#${id}`), id) : -1;
        if (index < 0) return { observations: 1, findings: [{ message: 'Provide a skip link to a focusable main landmark.' }] };
        const skip = links.nth(index);
        const target = new URL(
            await skip.getAttribute('href'),
            (await page.locator('base').count()) ? await page.locator('base').evaluate(element => element.href) : page.url(),
        );
        const current = new URL(page.url());
        if (target.origin !== current.origin || target.pathname !== current.pathname || target.search !== current.search)
            return { observations: 2, findings: [{ message: 'Skip navigation must preserve the current document path and query.' }] };
        await skip.focus();
        await page.keyboard.press('Enter');
        let moved = true;
        try {
            await page.waitForFunction(() => document.activeElement === document.querySelector('main'), undefined, { timeout: 1000 });
        } catch {
            moved = false;
        }
        return { observations: 2, findings: moved ? [] : [{ message: 'Activating the skip link did not move keyboard focus into main.' }] };
    }
    if (check === 'browser:form-errors') {
        const form = page.locator('[role="dialog"] form, dialog[open] form').first();
        const first = form.locator('input[required]:not([type="checkbox"])').first();
        const findings = [];
        if (!(await first.count())) return { observations: 1, findings: [{ message: 'Expected required credential controls.' }] };
        if ((await first.getAttribute('aria-invalid')) === 'true')
            findings.push({ message: 'Untouched credential field is already marked invalid.' });
        const submit = form.locator('button[type="submit"]').first();
        if (await submit.isDisabled())
            return { observations: 1, findings: [{ message: 'Validation must not disable the initial submit action.' }] };
        await submit.click();
        let announced = true;
        try {
            await page.waitForFunction(
                () => {
                    const field = document.querySelector(
                        '[role="dialog"] form input[required]:not([type="checkbox"]), dialog[open] form input[required]:not([type="checkbox"])',
                    );
                    return field?.getAttribute('aria-invalid') === 'true' && document.activeElement === field;
                },
                undefined,
                { timeout: 2000 },
            );
        } catch {
            announced = false;
        }
        if (!announced) findings.push({ message: 'Invalid submit must focus the first field and synchronize aria-invalid.' });
        const association = await first.evaluate(element =>
            (element.getAttribute('aria-describedby') ?? element.getAttribute('aria-errormessage') ?? '')
                .split(/\s+/u)
                .some(id => document.getElementById(id)?.textContent?.trim()),
        );
        if (!association) findings.push({ message: 'The invalid field has no associated readable error message.' });
        return { observations: 4, findings, detail: 'Untouched state, submit availability, invalid focus/ARIA and error association.' };
    }
    return page.evaluate(
        ({ check, kind }) => {
            const findings = [],
                fail = message => findings.push({ message });
            const visible = element => element.checkVisibility({ checkOpacity: true, checkVisibilityCSS: true });
            if (check === 'browser:page-structure') {
                const headings = [...document.querySelectorAll('h1,h2,h3,h4,h5,h6')].filter(visible);
                if (headings.filter(element => element.tagName === 'H1').length !== 1) fail('Expected one visible top-level page heading.');
                let level = 0;
                for (const heading of headings) {
                    const next = Number(heading.tagName[1]);
                    if (next > level + 1)
                        fail(`Heading order skips a level: ${heading.tagName} ${heading.textContent?.trim().slice(0, 60)}`);
                    level = next;
                }
                const ids = new Set();
                for (const element of document.querySelectorAll('[id]')) {
                    if (element.namespaceURI !== 'http://www.w3.org/1999/xhtml' || !element.id) continue;
                    if (ids.has(element.id)) fail(`Duplicate HTML ID: ${element.id}`);
                    ids.add(element.id);
                }
            }
            if (check === 'browser:motion-preference') {
                if (!matchMedia('(prefers-reduced-motion: reduce)').matches)
                    return { observations: 1, findings, detail: 'Scenario has no reduced-motion request.' };
                for (const animation of document.getAnimations()) {
                    if (animation.playState !== 'running' || !animation.effect?.getKeyframes) continue;
                    const moving = animation.effect
                        .getKeyframes()
                        .some(frame => ['transform', 'translate', 'rotate', 'scale', 'left', 'top'].some(key => key in frame));
                    const target = animation.effect.target;
                    if (moving && target?.checkVisibility() && !target.closest('[role="progressbar"], [role="status"]'))
                        fail(`Nonessential motion remains active under reduced motion: ${target.tagName}.${target.className}`);
                }
            }
            if (check === 'browser:form-attributes') {
                const form = document.querySelector('[role="dialog"] form, dialog[open] form');
                if (!form) return { observations: 1, findings: [{ message: 'Expected a rendered credential form.' }] };
                const email = form.querySelector('input[type="email"]');
                const purpose = kind === 'register' ? 'new-password' : 'current-password';
                const password = form.querySelector(`input[autocomplete="${purpose}"]`);
                if (form.method !== 'post') fail('Credential form transport must be POST.');
                if (!email || email.getAttribute('autocomplete')?.trim().toLowerCase() !== 'username')
                    fail('Credential email must identify the password-manager username.');
                if (!password || password.type !== 'password')
                    fail('Credential password must be masked and identify its current/new purpose.');
                for (const field of [email, password].filter(Boolean)) {
                    if (!field.required || !field.name || !field.id || !field.labels?.length)
                        fail('Credential controls require required, stable name/id and an associated visible label.');
                    if (field.type === 'email' && field.spellcheck !== false) fail('Account identifiers must disable spellchecking.');
                    if (!field.dispatchEvent(new Event('paste', { bubbles: true, cancelable: true })))
                        fail('Credential fields must allow pasting.');
                }
            }
            if (check === 'browser:resource-bounds') {
                for (const [selector, limit, label] of [
                    ['img[fetchpriority="high"]', 2, 'high-priority images'],
                    ['link[rel="preload"][as="image"]', 2, 'image preloads'],
                    ['link[rel="preload"][as="font"]', 3, 'font preloads'],
                    ['script[fetchpriority="high"]', 2, 'high-priority scripts'],
                ])
                    if (document.querySelectorAll(selector).length > limit) fail(`Too many ${label}; reviewed limit is ${limit}.`);
                for (const link of document.querySelectorAll('link[rel="preload"][as="font"]'))
                    if (!link.hasAttribute('crossorigin')) fail('Font preload is missing crossorigin.');
            }
            return { observations: 1, findings };
        },
        { check, kind: scenario.kind },
    );
}
