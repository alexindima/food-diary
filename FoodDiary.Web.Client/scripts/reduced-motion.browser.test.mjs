import assert from 'node:assert/strict';
import { after, before, test } from 'node:test';
import { fileURLToPath } from 'node:url';

import { encapsulateStyle } from '@angular/compiler';
import { chromium } from '@playwright/test';
import { compile } from 'sass';

import { inspectPlatformPage } from '../quality/modern-web-guidance/browser-platform.mjs';

const frontend = fileURLToPath(new URL('../', import.meta.url));
const fixtures = [
    [
        'toast',
        'projects/fd-ui-kit/src/lib/toast/fd-ui-toast-host.scss',
        '<section class="fd-ui-toast-host__item" role="status">Saved</section>',
        '.fd-ui-toast-host__item',
    ],
    ['hint', 'projects/fd-ui-kit/src/lib/hint/fd-ui-hint-overlay.scss', '<div class="fd-ui-hint">Helpful hint</div>', '.fd-ui-hint'],
    [
        'switch',
        'projects/fd-ui-kit/src/lib/switch/fd-ui-switch.scss',
        '<button class="fd-ui-switch" aria-label="Toggle"><span class="fd-ui-switch__track"><span class="fd-ui-switch__thumb"></span></span></button>',
        '.fd-ui-switch__thumb',
    ],
    [
        'bar',
        'projects/fd-ui-kit/src/lib/bar-chart/fd-ui-bar-chart.scss',
        '<svg><rect class="fd-ui-bar-chart__bar" width="30" height="40" y="20" /></svg>',
        '.fd-ui-bar-chart__bar',
    ],
    [
        'line',
        'projects/fd-ui-kit/src/lib/line-chart/fd-ui-line-chart.scss',
        '<svg><path class="fd-ui-line-chart__line" d="M 0 0 L 30 30" /></svg>',
        '.fd-ui-line-chart__line',
    ],
    [
        'pie',
        'projects/fd-ui-kit/src/lib/pie-chart/fd-ui-pie-chart.scss',
        '<svg><circle class="fd-ui-pie-chart__segment" r="10" cx="20" cy="20" /></svg>',
        '.fd-ui-pie-chart__segment',
    ],
    ['button', 'projects/fd-ui-kit/src/lib/button/fd-ui-button.scss', '<button class="fd-ui-button">Save</button>', '.fd-ui-button'],
    [
        'card',
        'projects/fd-ui-kit/src/lib/entity-card/fd-ui-entity-card.scss',
        '<div class="fd-ui-entity-card">Recipe</div>',
        '.fd-ui-entity-card',
    ],
    [
        'loader',
        'projects/fd-ui-kit/src/lib/loader/fd-ui-loader.scss',
        '<div class="fd-ui-loader__spinner" role="status">Loading</div>',
        '.fd-ui-loader__spinner',
    ],
    ['skeleton', 'src/app/components/shared/skeleton/skeleton.scss', '<div class="fd-skeleton">Loading preview</div>', '.fd-skeleton'],
    [
        'consent',
        'src/app/components/shared/ai-consent-dialog/ai-consent-dialog.scss',
        '<div class="ai-consent-dialog__body"><div style="height:1000px">Consent</div></div>',
        '.ai-consent-dialog__body',
    ],
    [
        'input',
        'projects/fd-ui-kit/src/lib/input/fd-ui-input.scss',
        '<input class="fd-ui-input__control" aria-label="Email" />',
        '.fd-ui-input__control',
    ],
];
const animated = ['toast', 'hint', 'loader', 'skeleton'];
const transitioning = ['switch', 'bar', 'line', 'pie', 'button', 'card'];
let browser;
let styles;

before(async () => {
    styles = fixtures
        .map(([id, file]) => encapsulateStyle(compile(frontend + file, { loadPaths: [frontend + 'src/styles'] }).css, id))
        .join('\n');
    styles += compile(frontend + 'src/styles/overlays.scss', { loadPaths: [frontend + 'src/styles'] }).css;
    browser = await chromium.launch({ channel: process.env.FD_WEB_QUALITY_CHANNEL ?? 'chrome', headless: true });
});

after(async () => browser?.close());

async function render(page, reducedMotion) {
    await page.emulateMedia({ reducedMotion });
    await page.setContent(`<style>${styles}</style><style>
        body { --fd-space-xs:8px; --fd-size-switch-thumb:20px; --fd-size-switch-track-width:48px; --fd-size-switch-track-height:24px; }
        [data-fixture] { display:block; min-height:40px; margin:12px; }
        .fd-ui-toast-host__item,.fd-ui-hint,.fd-ui-entity-card { min-height:30px; }
        svg { width:160px; height:80px; }
        .ai-consent-dialog__body { height:60px; }
    </style><h1>FoodDiary motion contracts</h1>`);
    await page.evaluate(fixtures => {
        for (const [id, , markup] of fixtures) {
            const host = document.createElement('section');
            host.dataset.fixture = id;
            host.setAttribute(`_nghost-${id}`, '');
            if (id === 'card') host.className = 'fd-ui-entity-card--hoverable';
            host.innerHTML = markup;
            for (const child of host.querySelectorAll('*')) child.setAttribute(`_ngcontent-${id}`, '');
            document.body.append(host);
        }
        document
            .querySelector('.fd-ui-switch')
            .addEventListener('click', event => event.currentTarget.classList.toggle('fd-ui-switch--checked'));
    }, fixtures);
}

async function state(page, id) {
    const selector = fixtures.find(([key]) => key === id)[3];
    return page.locator(`[data-fixture="${id}"] ${selector}`).evaluate(element => {
        const style = getComputedStyle(element);
        return {
            animation: style.animationName,
            durations: style.transitionDuration.split(',').map(value => Number.parseFloat(value)),
            scroll: style.scrollBehavior,
        };
    });
}

for (const width of [390, 1440]) {
    test(`source component styles respect reduced motion at ${width}px, including preference changes`, async () => {
        const page = await browser.newPage({ viewport: { width, height: 900 } });
        try {
            await render(page, 'no-preference');
            for (const id of animated)
                assert.notEqual((await state(page, id)).animation, 'none', `${id}: normal animation remains available`);
            for (const id of transitioning)
                assert.ok(
                    (await state(page, id)).durations.some(value => value > 0),
                    `${id}: normal transition remains available`,
                );
            assert.equal((await state(page, 'consent')).scroll, 'smooth');

            await page.emulateMedia({ reducedMotion: 'reduce' });
            for (const id of animated)
                assert.equal((await state(page, id)).animation, 'none', `${id}: reduced motion must disable animation`);
            for (const id of transitioning)
                assert.ok(
                    (await state(page, id)).durations.every(value => value === 0),
                    `${id}: reduced motion must disable geometric transitions`,
                );
            assert.equal((await state(page, 'consent')).scroll, 'auto');

            await page.getByRole('button', { name: 'Toggle' }).click();
            const thumb = page.locator('.fd-ui-switch__thumb');
            assert.notEqual(await thumb.evaluate(el => getComputedStyle(el).transform), 'none', 'Switch state still updates');
            assert.equal(await thumb.evaluate(el => el.getAnimations().length), 0, 'Switch updates without movement animation');
            const bar = page.locator('.fd-ui-bar-chart__bar');
            assert.equal(
                await bar.evaluate(el => {
                    getComputedStyle(el).height;
                    el.style.height = '70px';
                    el.style.y = '0px';
                    getComputedStyle(el).height;
                    return el.getAnimations().length;
                }),
                0,
                'Updated chart geometry must be immediate',
            );
            await page.locator('.ai-consent-dialog__body').evaluate(el => el.scrollTo({ top: 100 }));
            assert.equal(await page.locator('.ai-consent-dialog__body').evaluate(el => el.scrollTop), 100);

            await page.emulateMedia({ reducedMotion: 'no-preference' });
            assert.notEqual((await state(page, 'toast')).animation, 'none', 'Animations return when the preference changes');
        } finally {
            await page.close();
        }
    });
}

test('autofill still emits its nonvisual animationstart event with reduced motion', async () => {
    const page = await browser.newPage();
    try {
        await render(page, 'reduce');
        const input = page.locator('[data-fixture="input"] input');
        await input.evaluate(el => el.addEventListener('animationstart', event => (el.dataset.animationStarted = event.animationName)));
        const cdp = await page.context().newCDPSession(page);
        await cdp.send('DOM.enable');
        await cdp.send('CSS.enable');
        const { root } = await cdp.send('DOM.getDocument');
        const { nodeId } = await cdp.send('DOM.querySelector', { nodeId: root.nodeId, selector: '[data-fixture="input"] input' });
        await cdp.send('CSS.forcePseudoState', { nodeId, forcedPseudoClasses: ['autofill'] });
        await page.waitForFunction(() =>
            document.querySelector('[data-fixture="input"] input').dataset.animationStarted?.endsWith('fd-ui-input-autofill-start'),
        );
        assert.match(await input.evaluate(el => getComputedStyle(el).animationName), /fd-ui-input-autofill-start$/u);
        assert.deepEqual(
            await page.evaluate(() =>
                [...document.styleSheets]
                    .flatMap(sheet => [...sheet.cssRules])
                    .filter(rule => rule instanceof CSSKeyframesRule && rule.name.endsWith('fd-ui-input-autofill-start'))
                    .flatMap(rule => [...rule.cssRules].map(frame => frame.style.opacity)),
            ),
            ['1', '1'],
        );
        await cdp.detach();
    } finally {
        await page.close();
    }
});

test('detail dialogs and backdrops keep a visible static default', async () => {
    const page = await browser.newPage({ reducedMotion: 'reduce' });
    try {
        for (const [id, file, markup] of [
            [
                'detail',
                'projects/fd-ui-kit/src/lib/dialog/fd-ui-dialog.scss',
                '<div class="fd-ui-dialog-panel--detail"><div class="fd-ui-dialog">Dialog</div></div>',
            ],
            [
                'shell',
                'projects/fd-ui-kit/src/lib/dialog-shell/fd-ui-dialog-shell.scss',
                '<div class="fd-ui-dialog-shell"><div class="fd-ui-dialog">Dialog shell</div></div>',
            ],
        ]) {
            const css = encapsulateStyle(compile(frontend + file, { loadPaths: [frontend + 'src/styles'] }).css, id);
            await page.setContent(`<style>${css}</style>${markup}`);
            await page
                .locator('div')
                .evaluateAll((elements, scope) => elements.forEach(el => el.setAttribute(`_ngcontent-${scope}`, '')), id);
            assert.equal(
                await page.locator('.fd-ui-dialog').evaluate(el => getComputedStyle(el).opacity),
                '1',
                `${id}: static dialog must stay visible`,
            );
            assert.equal(await page.locator('.fd-ui-dialog').evaluate(el => getComputedStyle(el).transform), 'none');
        }
        await page.setContent(`<style>${styles}</style><div class="fd-ui-dialog-backdrop--detail">Backdrop</div>`);
        assert.equal(await page.locator('div').evaluate(el => getComputedStyle(el).opacity), '1', 'Static backdrop must stay visible');
    } finally {
        await page.close();
    }
});

test('WebQuality detects moving status notifications and SVG geometry under reduced motion', async () => {
    const page = await browser.newPage({ reducedMotion: 'reduce' });
    try {
        await page.setContent('<div role="status">Saved</div><svg><rect width="30" height="30" /></svg>');
        await page.evaluate(() => {
            document
                .querySelector('[role="status"]')
                .animate([{ transform: 'translateY(10px)' }, { transform: 'none' }], { duration: 10_000 });
            document.querySelector('rect').animate([{ height: '30px' }, { height: '70px' }], { duration: 10_000 });
        });
        const result = await inspectPlatformPage(page, 'browser:motion-preference', {});
        assert.ok(
            result.findings.some(item => item.message.includes('DIV')),
            'Moving status notification must be reported',
        );
        assert.ok(
            result.findings.some(item => item.message.includes('rect')),
            'SVG geometry motion must be reported',
        );
    } finally {
        await page.close();
    }
});
