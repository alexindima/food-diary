import fs from 'node:fs';
import path from 'node:path';

const rootDir = process.cwd();
const i18nDir = path.join(rootDir, 'assets', 'i18n');
const localeFiles = {
    en: path.join(i18nDir, 'en.json'),
    ru: path.join(i18nDir, 'ru.json'),
};

const mojibakePatterns = [
    /Ð[\u0080-\u00BF]/u,
    /Ñ[\u0080-\u00BF]/u,
    /â[\u0080-\u00BF]/u,
    /Â[\u0080-\u00BF]/u,
];

const issues = [];

const locales = Object.fromEntries(
    Object.entries(localeFiles).map(([locale, filePath]) => [locale, JSON.parse(fs.readFileSync(filePath, 'utf8'))]),
);

compareNodes('', locales.en, locales.ru);

if (issues.length > 0) {
    console.error('i18n check failed:');
    for (const issue of issues) {
        console.error(`- ${issue}`);
    }
    process.exit(1);
}

console.log('i18n check passed');

function compareNodes(currentPath, enNode, ruNode) {
    const pathLabel = currentPath || '<root>';

    if (isPlainObject(enNode) !== isPlainObject(ruNode)) {
        issues.push(`shape mismatch at ${pathLabel}`);
        return;
    }

    if (isPlainObject(enNode) && isPlainObject(ruNode)) {
        const enKeys = Object.keys(enNode);
        const ruKeys = Object.keys(ruNode);

        for (const key of enKeys) {
            if (!(key in ruNode)) {
                issues.push(`missing key in ru: ${joinPath(currentPath, key)}`);
            }
        }

        for (const key of ruKeys) {
            if (!(key in enNode)) {
                issues.push(`extra key in ru: ${joinPath(currentPath, key)}`);
            }
        }

        const sharedKeys = enKeys.filter(key => key in ruNode);
        for (const key of sharedKeys) {
            compareNodes(joinPath(currentPath, key), enNode[key], ruNode[key]);
        }
        return;
    }

    if (typeof enNode !== 'string' || typeof ruNode !== 'string') {
        issues.push(`non-string leaf at ${pathLabel}`);
        return;
    }

    if (enNode.trim().length === 0) {
        issues.push(`empty string in en: ${pathLabel}`);
    }

    if (ruNode.trim().length === 0) {
        issues.push(`empty string in ru: ${pathLabel}`);
    }

    if (containsMojibake(enNode)) {
        issues.push(`possible mojibake in en: ${pathLabel}`);
    }

    if (containsMojibake(ruNode)) {
        issues.push(`possible mojibake in ru: ${pathLabel}`);
    }

    const enPlaceholders = extractPlaceholders(enNode);
    const ruPlaceholders = extractPlaceholders(ruNode);
    if (!sameStringSet(enPlaceholders, ruPlaceholders)) {
        issues.push(
            `placeholder mismatch at ${pathLabel}: en=[${[...enPlaceholders].join(', ')}], ru=[${[...ruPlaceholders].join(', ')}]`,
        );
    }
}

function isPlainObject(value) {
    return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function joinPath(currentPath, key) {
    return currentPath ? `${currentPath}.${key}` : key;
}

function containsMojibake(value) {
    return mojibakePatterns.some(pattern => pattern.test(value));
}

function extractPlaceholders(value) {
    const matches = value.match(/\{\{\s*[\w.]+\s*\}\}/gu) ?? [];
    return new Set(matches.map(match => match.replace(/\s+/gu, '')));
}

function sameStringSet(left, right) {
    if (left.size !== right.size) {
        return false;
    }

    for (const value of left) {
        if (!right.has(value)) {
            return false;
        }
    }

    return true;
}
