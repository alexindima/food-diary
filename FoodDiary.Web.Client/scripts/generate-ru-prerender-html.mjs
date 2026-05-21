import fs from 'node:fs/promises';
import path from 'node:path';

const rootDir = process.cwd();
const browserDistDir = path.join(rootDir, 'dist', 'browser');
const ruOutputDir = path.join(browserDistDir, '__ru');
const ruSiteUrl = 'https://xn--b1adbcbrouc8l.xn--p1ai';
const enSiteUrl = 'https://fooddiary.club';
const defaultTitle = 'Food Diary';
const publicSeoRoutesConfigPath = path.join(rootDir, 'src', 'app', 'config', 'public-seo-landing-routes.config.ts');
const routeSeoData = await readRouteSeoData();
const routes = [...routeSeoData.keys()];

await fs.rm(ruOutputDir, { recursive: true, force: true });

for (const routePath of routes) {
    const sourceFilePath = toEnglishSsgFilePath(routePath);
    const outputFilePath = toRuOutputFilePath(routePath);
    const sourceHtml = await fs.readFile(sourceFilePath, 'utf8');
    const html = rewriteSeoHtml(sourceHtml, routePath, routeSeoData.get(routePath));

    await fs.mkdir(path.dirname(outputFilePath), { recursive: true });
    await fs.writeFile(outputFilePath, html);
}

console.log(`Generated ${routes.length} Russian prerender HTML files in dist/browser/__ru.`);

async function readRouteSeoData() {
    const translations = await readRuTranslations();
    const routeData = new Map();

    routeData.set('/', {
        title: buildPageTitle(resolveTranslation(translations, 'SEO.LANDING_TITLE')),
        description: resolveTranslation(translations, 'SEO.LANDING_DESCRIPTION'),
    });

    for (const route of await readPublicSeoLandingRoutes()) {
        routeData.set(`/${route.path}`, {
            title: buildPageTitle(resolveTranslation(translations, route.titleKey)),
            description: resolveTranslation(translations, route.descriptionKey),
        });
    }

    routeData.set('/privacy-policy', {
        title: buildPageTitle(resolveTranslation(translations, 'SEO.PRIVACY_POLICY')),
        description: resolveTranslation(translations, 'SEO.PRIVACY_POLICY_DESCRIPTION'),
    });

    return routeData;
}

async function readRuTranslations() {
    const bundleNames = ['core', 'landing', 'seo', 'privacy'];
    const bundles = await Promise.all(
        bundleNames.map(async bundle => JSON.parse(await fs.readFile(path.join(rootDir, 'assets', 'i18n', 'ru', `${bundle}.json`), 'utf8'))),
    );

    return bundles.reduce((result, bundle) => deepMerge(result, bundle), {});
}

async function readPublicSeoLandingRoutes() {
    const source = await fs.readFile(publicSeoRoutesConfigPath, 'utf8');
    const routeBlocks = source.matchAll(/\{\s*path:\s*'(?<path>[^']+)'[\s\S]*?titleKey:\s*'(?<titleKey>[^']+)'[\s\S]*?descriptionKey:\s*'(?<descriptionKey>[^']+)'/gu);

    return [...routeBlocks].map(match => ({
        path: match.groups.path,
        titleKey: match.groups.titleKey,
        descriptionKey: match.groups.descriptionKey,
    }));
}

function rewriteSeoHtml(sourceHtml, routePath, seoData) {
    const canonicalUrl = buildSiteUrl(ruSiteUrl, routePath);
    const enAlternateUrl = buildSiteUrl(enSiteUrl, routePath);
    const ruAlternateUrl = canonicalUrl;
    const title = seoData?.title ?? defaultTitle;
    const description = seoData?.description ?? '';

    let html = sourceHtml
        .replace(/<html\s+lang="[^"]+"/iu, '<html lang="ru"')
        .replace(/<title>[\s\S]*?<\/title>/iu, `<title>${escapeHtml(title)}</title>`);

    html = upsertMeta(html, 'name', 'description', description);
    html = upsertMeta(html, 'property', 'og:title', title);
    html = upsertMeta(html, 'property', 'og:description', description);
    html = upsertMeta(html, 'property', 'og:url', canonicalUrl);
    html = upsertMeta(html, 'property', 'og:image', `${ruSiteUrl}/assets/pwa/icon-512x512.png`);
    html = upsertMeta(html, 'property', 'og:locale', 'ru_RU');
    html = upsertMeta(html, 'name', 'twitter:title', title);
    html = upsertMeta(html, 'name', 'twitter:description', description);
    html = upsertMeta(html, 'name', 'twitter:image', `${ruSiteUrl}/assets/pwa/icon-512x512.png`);
    html = upsertLink(html, 'canonical', null, canonicalUrl);
    html = upsertLink(html, 'alternate', 'en', enAlternateUrl);
    html = upsertLink(html, 'alternate', 'ru', ruAlternateUrl);
    html = upsertLink(html, 'alternate', 'x-default', enAlternateUrl);
    html = rewriteStructuredData(html, canonicalUrl, title, description);

    return html;
}

function rewriteStructuredData(html, canonicalUrl, title, description) {
    return html.replace(
        /<script\s+type="application\/ld\+json"\s+data-seo-structured-data="app">(?<json>[\s\S]*?)<\/script>/iu,
        (_match, json) => {
            const payload = JSON.parse(json);
            rewriteStructuredValue(payload, canonicalUrl, title, description);
            return `<script type="application/ld+json" data-seo-structured-data="app">${JSON.stringify(payload)}</script>`;
        },
    );
}

function rewriteStructuredValue(value, canonicalUrl, title, description) {
    if (Array.isArray(value)) {
        for (const item of value) {
            rewriteStructuredValue(item, canonicalUrl, title, description);
        }
        return;
    }

    if (typeof value !== 'object' || value === null) {
        return;
    }

    for (const [key, item] of Object.entries(value)) {
        if (typeof item === 'string') {
            if (item.startsWith(enSiteUrl)) {
                value[key] = item.replace(enSiteUrl, ruSiteUrl);
            }
            if (key === 'inLanguage') {
                value[key] = 'ru';
            }
            continue;
        }

        rewriteStructuredValue(item, canonicalUrl, title, description);
    }

    if (value['@type'] === 'SoftwareApplication' || value['@type'] === 'WebPage') {
        value.url = canonicalUrl;
        value.description = description;
    }

    if (value['@type'] === 'WebPage') {
        value.name = title;
    }
}

function upsertMeta(html, attributeName, attributeValue, content) {
    const pattern = new RegExp(`<meta\\s+${attributeName}="${escapeRegExp(attributeValue)}"\\s+content="[^"]*"\\s*>`, 'iu');
    const replacement = `<meta ${attributeName}="${attributeValue}" content="${escapeHtmlAttribute(content)}">`;

    return pattern.test(html) ? html.replace(pattern, replacement) : html.replace('</head>', `        ${replacement}\n</head>`);
}

function upsertLink(html, rel, hreflang, href) {
    const selector = hreflang === null ? `<link\\s+rel="${rel}"\\s+href="[^"]*"\\s*>` : `<link\\s+rel="${rel}"\\s+hreflang="${hreflang}"\\s+href="[^"]*"\\s*>`;
    const pattern = new RegExp(selector, 'iu');
    const hreflangPart = hreflang === null ? '' : ` hreflang="${hreflang}"`;
    const replacement = `<link rel="${rel}"${hreflangPart} href="${href}">`;

    return pattern.test(html) ? html.replace(pattern, replacement) : html.replace('</head>', `        ${replacement}\n</head>`);
}

function buildPageTitle(title) {
    return title.length > 0 ? `${title} | ${defaultTitle}` : defaultTitle;
}

function buildSiteUrl(baseUrl, routePath) {
    return routePath === '/' ? baseUrl : `${baseUrl}${routePath}`;
}

function toEnglishSsgFilePath(routePath) {
    return routePath === '/' ? path.join(browserDistDir, 'index.html') : path.join(browserDistDir, routePath.slice(1), 'index.html');
}

function toRuOutputFilePath(routePath) {
    return routePath === '/' ? path.join(ruOutputDir, 'index.html') : path.join(ruOutputDir, routePath.slice(1), 'index.html');
}

function resolveTranslation(translations, key) {
    return key.split('.').reduce((value, part) => (typeof value === 'object' && value !== null ? value[part] : undefined), translations) ?? key;
}

function deepMerge(target, source) {
    const output = { ...target };
    for (const [key, value] of Object.entries(source)) {
        output[key] = isDictionary(output[key]) && isDictionary(value) ? deepMerge(output[key], value) : value;
    }

    return output;
}

function isDictionary(value) {
    return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function escapeHtml(value) {
    return value.replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;');
}

function escapeHtmlAttribute(value) {
    return escapeHtml(value).replaceAll('"', '&quot;');
}

function escapeRegExp(value) {
    return value.replace(/[.*+?^${}()|[\]\\]/gu, '\\$&');
}
