import { copyFileSync, readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

import { chromium } from '@playwright/test';

// Use the same vector mark and default ocean color as the sidebar. Keep the
// complete mark inside Android's central maskable safe zone, with opaque edges.
const mark = readFileSync(new URL('../assets/images/header/logo-mask.svg', import.meta.url), 'utf8');
const paddedMark = mark.replace('<svg ', '<svg x="16%" y="16%" width="68%" height="68%" ').replaceAll('fill="#000"', 'fill="#2563eb"');
const icon = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512"><rect width="512" height="512" fill="#ffffff"/>${paddedMark}</svg>`;
const browser = await chromium.launch({ channel: 'chrome', headless: true });

try {
    const page = await browser.newPage({ deviceScaleFactor: 1 });
    for (const size of [48, 72, 96, 144, 180, 192, 512]) {
        await page.setViewportSize({ width: size, height: size });
        await page.setContent(
            `<style>html,body{margin:0;width:100%;height:100%;overflow:hidden}svg{display:block;width:100%;height:100%}</style>${icon}`,
        );
        await page.screenshot({ path: fileURLToPath(new URL(`../assets/pwa/app-icon-${size}x${size}.png`, import.meta.url)) });
    }
    // Existing push payloads and social previews keep these public URLs.
    for (const size of [192, 512]) {
        copyFileSync(
            new URL(`../assets/pwa/app-icon-${size}x${size}.png`, import.meta.url),
            new URL(`../assets/pwa/icon-${size}x${size}.png`, import.meta.url),
        );
    }
    // Android uses the alpha silhouette for its monochrome notification badge.
    await page.setViewportSize({ width: 96, height: 96 });
    await page.setContent(
        `<style>html,body{margin:0;background:transparent}svg{display:block;width:100%;height:100%}</style><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512">${paddedMark.replaceAll('#2563eb', '#ffffff')}</svg>`,
    );
    await page.screenshot({ path: fileURLToPath(new URL('../assets/pwa/icon-96x96.png', import.meta.url)), omitBackground: true });
} finally {
    await browser.close();
}
