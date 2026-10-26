import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { test } from 'node:test';
import { runInNewContext } from 'node:vm';

import { JSDOM } from 'jsdom';

const source = readFileSync(new URL('../src/index.html', import.meta.url), 'utf8');
const script = /<script>([\s\S]*?)<\/script>/u.exec(source)[1];

function loadStartup(path, storage = {}, blockedStorage = false) {
    const { window } = new JSDOM(source, { url: `https://fooddiary.club${path}` });
    // Match the public document emitted by prerendering before the head script runs.
    window.document.documentElement.setAttribute('data-theme', 'ocean');
    window.document.documentElement.style.colorScheme = 'light';
    for (const [key, value] of Object.entries(storage)) window.localStorage.setItem(key, value);
    const context = {
        document: window.document,
        navigator: window.navigator,
        location: window.location,
        localStorage: window.localStorage,
        sessionStorage: window.sessionStorage,
    };
    if (blockedStorage) {
        context.localStorage = {
            getItem: () => {
                throw new Error('Storage unavailable');
            },
        };
    }
    runInNewContext(script, context);
    return window;
}

test('conceals a published cookie-session hint until the API proves authentication', () => {
    const window = loadStartup('/', { accountAccess: JSON.stringify(['00000000-0000-4000-8000-000000000001', 'User']) });
    assert.ok(
        window.document.documentElement.classList.contains('fd-session-route-pending'),
        'Published account metadata must conceal the document while the cookie is proved.',
    );
    window.close();
});

for (const path of ['/', '/ru', '/ru/', '/dashboard', '/dashboard?date=2026-10-22', '/meals', '/meals/create', '/explore']) {
    test(`conceals the initial document and restores dark preferences before startup at ${path}`, () => {
        const window = loadStartup(path, {
            accountAccess: '["owner","User"]',
            fd_theme: 'dark',
            fd_ui_style: 'modern',
            fd_surface_style: 'matte',
        });
        const root = window.document.documentElement;
        assert.ok(root.classList.contains('fd-session-route-pending'));
        assert.equal(root.dataset.theme, 'dark');
        assert.equal(root.dataset.uiStyle, 'modern');
        assert.equal(root.dataset.surfaceStyle, 'matte');
        assert.equal(root.style.colorScheme, 'dark');
        window.close();
    });
}

test('also conceals a session stored only in the current tab', () => {
    const window = loadStartup('/meals');
    window.sessionStorage.setItem('userId', 'tab-owner');
    runInNewContext(script, window);
    assert.ok(window.document.documentElement.classList.contains('fd-session-route-pending'));
    window.close();
});

test('keeps the guest landing visible even when an old theme preference remains', () => {
    const window = loadStartup('/ru', { fd_theme: 'dark' });
    assert.equal(window.document.documentElement.dataset.theme, 'ocean');
    assert.equal(window.document.documentElement.classList.contains('fd-session-route-pending'), false);
    window.close();
});

test('uses a neutral system color for guests and restores the saved theme before the first frame', () => {
    for (const [theme, color] of [
        ['ocean', '#ffffff'],
        ['leaf', '#ffffff'],
        ['dark', '#191c21'],
    ]) {
        const window = loadStartup('/dashboard', { accountAccess: '["owner","User"]', fd_theme: theme });
        assert.equal(
            window.document.querySelector('meta[name="theme-color"]').content,
            color,
            `Startup system color must follow the ${theme} theme before Angular starts.`,
        );
        window.close();
    }
    const guest = loadStartup('/ru', { fd_theme: 'dark' });
    assert.equal(guest.document.querySelector('meta[name="theme-color"]').content, '#ffffff');
    guest.close();
});

test('provides a neutral installed PWA fallback instead of a fixed brand color', () => {
    const manifest = JSON.parse(readFileSync(new URL('../assets/manifest.webmanifest', import.meta.url), 'utf8'));
    assert.equal(manifest.theme_color, '#ffffff', 'Installed PWA fallback must use the light surface color.');
});

test('publishes the notification icon and badge referenced by the push payload', () => {
    const sender = readFileSync(
        new URL('../../Modules/Notifications/Infrastructure/Services/WebPushNotificationSender.cs', import.meta.url),
        'utf8',
    );
    const paths = [...sender.matchAll(/(?:icon|badge) = "(\/assets\/[^"]+)"/gu)].map(match => match[1]);
    assert.ok(paths.length > 0, 'The push sender must declare its icon assets.');
    for (const path of paths) {
        assert.doesNotThrow(() => readFileSync(new URL(`..${path}`, import.meta.url)), `Push payload asset must exist: ${path}`);
    }
});

test('ignores invalid preferences and tolerates unavailable browser storage', () => {
    for (const blocked of [false, true]) {
        const window = loadStartup('/dashboard', { accountAccess: '["owner","User"]', fd_theme: 'invalid' }, blocked);
        assert.equal(window.document.documentElement.dataset.theme, 'ocean');
        if (blocked) assert.equal(window.document.documentElement.classList.contains('fd-session-route-pending'), false);
        window.close();
    }
});
