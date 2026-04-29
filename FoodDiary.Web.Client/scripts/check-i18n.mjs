import fs from 'node:fs';
import path from 'node:path';

const rootDir = process.cwd();
const i18nDir = path.join(rootDir, 'assets', 'i18n');
const sourceDirs = [path.join(rootDir, 'src'), path.join(rootDir, 'projects', 'fooddiary-admin', 'src')];
const localeBundles = ['core', 'landing', 'seo', 'privacy', 'app'];

const mojibakePatterns = [
    /Ð[\u0080-\u00BF]/u,
    /Ñ[\u0080-\u00BF]/u,
    /â[\u0080-\u00BF]/u,
    /Â[\u0080-\u00BF]/u,
];

const issues = [];

const locales = {
    en: readLocaleBundles('en'),
    ru: readLocaleBundles('ru'),
};
const availableKeys = new Set(flattenKeys(mergeBundles(locales.en)));

for (const bundle of localeBundles) {
    compareNodes(bundle, locales.en[bundle], locales.ru[bundle]);
}
checkRuntimeTranslationKeys();

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

function readLocaleBundles(locale) {
    return Object.fromEntries(
        localeBundles.map(bundle => {
            const filePath = path.join(i18nDir, locale, `${bundle}.json`);
            return [bundle, JSON.parse(fs.readFileSync(filePath, 'utf8'))];
        }),
    );
}

function mergeBundles(bundles) {
    return localeBundles.reduce((result, bundle) => deepMerge(result, bundles[bundle]), {});
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

function flattenKeys(node, currentPath = '') {
    if (!isPlainObject(node)) {
        return currentPath ? [currentPath] : [];
    }

    return Object.entries(node).flatMap(([key, value]) => flattenKeys(value, joinPath(currentPath, key)));
}

function deepMerge(target, source) {
    const output = { ...target };

    for (const [key, value] of Object.entries(source)) {
        const targetValue = output[key];
        output[key] = isPlainObject(targetValue) && isPlainObject(value) ? deepMerge(targetValue, value) : value;
    }

    return output;
}

function checkRuntimeTranslationKeys() {
    const files = sourceDirs.flatMap(collectSourceFiles);

    for (const filePath of files) {
        const content = fs.readFileSync(filePath, 'utf8');
        const keys = new Set([
            ...extractPipeKeys(content),
            ...extractTranslateServiceKeys(content),
        ]);

        for (const key of keys) {
            if (!availableKeys.has(key)) {
                issues.push(`missing runtime key: ${key} referenced in ${path.relative(rootDir, filePath)}`);
            }
        }
    }
}

function collectSourceFiles(directoryPath) {
    if (!fs.existsSync(directoryPath)) {
        return [];
    }

    const entries = fs.readdirSync(directoryPath, { withFileTypes: true });
    const files = [];

    for (const entry of entries) {
        const fullPath = path.join(directoryPath, entry.name);
        if (entry.isDirectory()) {
            files.push(...collectSourceFiles(fullPath));
            continue;
        }

        if (entry.isFile() && (fullPath.endsWith('.ts') || fullPath.endsWith('.html')) && !fullPath.endsWith('.spec.ts')) {
            files.push(fullPath);
        }
    }

    return files;
}

function extractPipeKeys(content) {
    const keys = [];
    const pattern = /(['"`])([A-Za-z0-9][A-Za-z0-9_.-]*[A-Za-z0-9])\1\s*\|\s*translate\b/gu;

    for (const match of content.matchAll(pattern)) {
        keys.push(match[2]);
    }

    return keys;
}

function extractTranslateServiceKeys(content) {
    const keys = [];
    const pattern =
        /\b(?:this\.)?[A-Za-z_$]*translate[A-Za-z0-9_$]*\.(?:instant|get|stream)\(\s*(['"`])([A-Za-z0-9][A-Za-z0-9_.-]*[A-Za-z0-9])\1\s*[,)]/giu;

    for (const match of content.matchAll(pattern)) {
        keys.push(match[2]);
    }

    return keys;
}
