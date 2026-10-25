import fs from 'node:fs';
import path from 'node:path';
import { inspectPrerenderTranslationState } from './prerender-translations.mjs';

const browserDir = path.resolve('dist/browser');
const siteUrl = 'https://fooddiary.club';
const routes = fs
    .readFileSync('src/prerender-routes.txt', 'utf8')
    .split(/\r?\n/u)
    .map(value => value.trim())
    .filter(Boolean);
const issues = [];
const retiredHost = 'xn--b1adbcbrouc8l.xn--p1ai';

for (const route of routes) {
    const file = path.join(browserDir, route.slice(1), 'index.html');
    if (!fs.existsSync(file)) {
        issues.push(`${route}: missing prerender HTML`);
        continue;
    }
    const html = fs.readFileSync(file, 'utf8');
    const isRussian = route === '/ru' || route.startsWith('/ru/');
    const locale = isRussian ? 'ru' : 'en';
    const basePath = route === '/ru' ? '/' : isRussian ? route.slice(3) : route;
    const bundle = basePath === '/' ? 'landing' : basePath === '/privacy-policy' ? 'privacy' : 'seo';
    issues.push(...inspectPrerenderTranslationState(html, locale, bundle).map(issue => `${route}: ${issue}`));
    if (basePath === '/') {
        for (const selector of ['fd-features', 'fd-dietologist-promo', 'fd-landing-steps', 'fd-landing-faq'])
            if (!html.includes(`<${selector} `) && !html.includes(`<${selector}>`))
                issues.push(`${route}: public ${selector} content is missing from SSR`);
    }
    if (/<fd-seo-landing-page(?:\s|>)/u.test(html)) {
        if (!/<details\b/u.test(html) || !/<summary\b/u.test(html)) issues.push(`${route}: native FAQ content is missing from SSR`);
        for (const content of ['seo-landing__related-card', 'seo-landing__section--cta'])
            if (!html.includes(content)) issues.push(`${route}: public ${content} content is missing from SSR`);
    }
    const canonical = siteUrl + (route === '/' ? '' : route);
    const enUrl = siteUrl + (basePath === '/' ? '' : basePath);
    const ruUrl = siteUrl + (basePath === '/' ? '/ru' : `/ru${basePath}`);
    const required = [
        [`<html lang="${locale}"`, 'document language'],
        [`<link rel="canonical" href="${canonical}">`, 'canonical'],
        [`hreflang="en" href="${enUrl}"`, 'English alternate'],
        [`hreflang="ru" href="${ruUrl}"`, 'Russian alternate'],
        [`hreflang="x-default" href="${enUrl}"`, 'default alternate'],
        [`property="og:locale" content="${isRussian ? 'ru_RU' : 'en_US'}"`, 'Open Graph locale'],
    ];
    for (const [value, label] of required) if (!html.includes(value)) issues.push(`${route}: missing ${label}`);
    if (!/<title>[^<]+<\/title>/iu.test(html)) issues.push(`${route}: missing title`);
    if (!/<meta\s+name="description"\s+content="[^"]+"/iu.test(html)) issues.push(`${route}: missing description`);
    const headings = [...html.matchAll(/<h1\b[^>]*>([\s\S]*?)<\/h1>/giu)];
    if (headings.length !== 1) issues.push(`${route}: expected one H1, found ${headings.length}`);
    if (isRussian && !/[А-Яа-яЁё]/u.test(headings[0]?.[1] ?? '')) issues.push(`${route}: H1 is not Russian`);
    if (!isRussian && /[А-Яа-яЁё]/u.test(headings[0]?.[1] ?? '')) issues.push(`${route}: H1 is not English`);
    if (/\b(?:PUBLIC_PAGES|PUBLIC_SITE|LANDING_HERO|SEO_PAGE|CALORIE_COUNTER_PAGE)\.[A-Z_]+/u.test(html))
        issues.push(`${route}: untranslated public copy`);
    if (html.includes(retiredHost)) issues.push(`${route}: retired Russian domain is still present`);
    if (html.includes('\uFFFD')) issues.push(`${route}: replacement character in rendered text`);
    if (!/<script\s+type="application\/ld\+json"\s+data-seo-structured-data="app">/iu.test(html))
        issues.push(`${route}: missing structured data`);
}
for (const asset of ['sitemap.xml', 'sitemap-ru.xml', 'robots.txt', 'robots-ru.txt']) {
    const file = path.join(browserDir, asset);
    if (fs.existsSync(file) && fs.readFileSync(file, 'utf8').includes(retiredHost))
        issues.push(`${asset}: retired Russian domain is still present`);
}
if (issues.length) {
    console.error('Public prerender verification failed:');
    for (const issue of issues) console.error(`- ${issue}`);
    process.exit(1);
}
console.log(`Public prerender verification passed (${routes.length} localized documents).`);
