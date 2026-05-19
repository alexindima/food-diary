import fs from 'node:fs';
import path from 'node:path';

const rootDir = process.cwd();
const browserDistDir = path.join(rootDir, 'dist', 'browser');
const translationKeyPattern = /\b[A-Z][A-Z0-9_]*(?:\.[A-Z0-9_]+)+\b/gu;
const ignoredPathSegments = new Set(['assets']);

if (!fs.existsSync(browserDistDir)) {
    console.error('Prerender HTML check failed: dist/browser does not exist. Run npm run build first.');
    process.exit(1);
}

const htmlFiles = collectHtmlFiles(browserDistDir);
const issues = [];

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
