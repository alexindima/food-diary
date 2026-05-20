import fs from 'node:fs';
import path from 'node:path';

const rootDir = process.cwd();
const browserDistDir = path.join(rootDir, 'dist', 'browser');
const publicSeoRoutesConfigPath = path.join(rootDir, 'src', 'app', 'config', 'public-seo-landing-routes.config.ts');
const translationKeyPattern = /\b[A-Z][A-Z0-9_]*(?:\.[A-Z0-9_]+)+\b/gu;
const ignoredPathSegments = new Set(['assets']);
const requiredSeoPaths = readPublicSeoLandingPaths();

if (!fs.existsSync(browserDistDir)) {
    console.error('Prerender HTML check failed: dist/browser does not exist. Run npm run build first.');
    process.exit(1);
}

const htmlFiles = collectHtmlFiles(browserDistDir);
const issues = [];

if (htmlFiles.length === 0) {
    issues.push('dist/browser contains no HTML files to check. Run a prerendered production build before this check.');
}

for (const routePath of requiredSeoPaths) {
    const htmlFilePath = path.join(browserDistDir, routePath, 'index.html');
    if (!fs.existsSync(htmlFilePath)) {
        issues.push(`${toRouteLabel(routePath)}: missing prerendered index.html.`);
        continue;
    }

    assertSeoDocument(htmlFilePath, routePath, issues);
}

for (const filePath of htmlFiles) {
    const content = fs.readFileSync(filePath, 'utf8');
    const matches = new Set(content.match(translationKeyPattern) ?? []);

    if (matches.size > 0) {
        issues.push(`${path.relative(rootDir, filePath)}: ${[...matches].sort().join(', ')}`);
    }
}

if (issues.length > 0) {
    console.error('Prerender HTML check failed: untranslated i18n keys found.');
    for (const issue of issues) {
        console.error(`- ${issue}`);
    }
    process.exit(1);
}

console.log(`Prerender HTML check passed (${htmlFiles.length} HTML files).`);

function readPublicSeoLandingPaths() {
    const source = fs.readFileSync(publicSeoRoutesConfigPath, 'utf8');
    const matches = source.matchAll(/path:\s*'([^']+)'/gu);
    return [...matches].map(match => match[1]);
}

function assertSeoDocument(filePath, routePath, targetIssues) {
    const content = fs.readFileSync(filePath, 'utf8');
    const routeLabel = toRouteLabel(routePath);
    const canonicalUrl = routePath.length === 0 ? 'https://fooddiary.club/' : `https://fooddiary.club/${routePath}`;
    const ruAlternateUrl =
        routePath.length === 0 ? 'https://xn--b1adbcbrouc8l.xn--p1ai/' : `https://xn--b1adbcbrouc8l.xn--p1ai/${routePath}`;

    assertPattern(content, /<title>[^<]+<\/title>/iu, routeLabel, 'missing title tag.', targetIssues);
    assertPattern(content, /<meta\s+name="description"\s+content="[^"]+"\s*>/iu, routeLabel, 'missing meta description.', targetIssues);
    assertIncludes(content, `<link rel="canonical" href="${canonicalUrl}">`, routeLabel, 'missing canonical link.', targetIssues);
    assertIncludes(content, `hreflang="en" href="${canonicalUrl}"`, routeLabel, 'missing en alternate link.', targetIssues);
    assertIncludes(content, `hreflang="ru" href="${ruAlternateUrl}"`, routeLabel, 'missing ru alternate link.', targetIssues);
    assertIncludes(content, `hreflang="x-default" href="${canonicalUrl}"`, routeLabel, 'missing x-default alternate link.', targetIssues);
    assertPattern(
        content,
        /<script\s+type="application\/ld\+json"\s+data-seo-structured-data="app">.+?<\/script>/isu,
        routeLabel,
        'missing SEO structured data.',
        targetIssues,
    );
}

function assertPattern(content, pattern, routeLabel, message, targetIssues) {
    if (!pattern.test(content)) {
        targetIssues.push(`${routeLabel}: ${message}`);
    }
}

function assertIncludes(content, expected, routeLabel, message, targetIssues) {
    if (!content.includes(expected)) {
        targetIssues.push(`${routeLabel}: ${message}`);
    }
}

function toRouteLabel(routePath) {
    return routePath.length === 0 ? '/' : `/${routePath}`;
}

function collectHtmlFiles(directoryPath) {
    const entries = fs.readdirSync(directoryPath, { withFileTypes: true });
    const files = [];

    for (const entry of entries) {
        const fullPath = path.join(directoryPath, entry.name);
        if (entry.isDirectory()) {
            if (!ignoredPathSegments.has(entry.name)) {
                files.push(...collectHtmlFiles(fullPath));
            }
            continue;
        }

        if (entry.isFile() && entry.name.endsWith('.html')) {
            files.push(fullPath);
        }
    }

    return files;
}
