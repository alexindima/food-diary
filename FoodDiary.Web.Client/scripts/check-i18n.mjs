import fs from 'node:fs';
import path from 'node:path';

const rootDir = process.cwd();
const i18nDir = path.join(rootDir, 'assets', 'i18n');
const sourceDirs = [path.join(rootDir, 'src'), path.join(rootDir, 'projects', 'fooddiary-admin', 'src')];
const localeBundles = ['core', 'landing', 'seo', 'privacy', 'app'];
const bundleScopedEntryPoints = [
    {
        name: 'landing initial route',
        bundles: ['core', 'landing'],
        entries: [
            path.join(rootDir, 'src', 'app', 'features', 'public', 'components', 'landing-preview-tour', 'landing-preview-tour.ts'),
            path.join(
                rootDir,
                'src',
                'app',
                'features',
                'public',
                'components',
                'landing-preview-tour',
                'landing-preview-tour-data.mapper.ts',
            ),
            path.join(rootDir, 'src', 'app', 'components', 'shared', 'dashboard-summary-card', 'dashboard-summary-card.ts'),
            path.join(rootDir, 'src', 'app', 'components', 'shared', 'meals-preview', 'meals-preview.ts'),
            path.join(rootDir, 'src', 'app', 'components', 'shared', 'meals-preview', 'meals-preview-entry', 'meals-preview-entry.ts'),
            path.join(rootDir, 'src', 'app', 'components', 'shared', 'meal-details-fields', 'meal-details-fields.ts'),
            path.join(rootDir, 'src', 'app', 'components', 'shared', 'meal-satiety-fields', 'meal-satiety-fields.ts'),
            path.join(rootDir, 'src', 'app', 'components', 'shared', 'product-card', 'product-card.ts'),
            path.join(rootDir, 'src', 'app', 'components', 'shared', 'recipe-card', 'recipe-card.ts'),
            path.join(rootDir, 'src', 'app', 'features', 'meals', 'components', 'quick-consumption-drawer', 'quick-consumption-drawer.ts'),
        ],
    },
];

const mojibakePatterns = [/Ð[\u0080-\u00BF]/u, /Ñ[\u0080-\u00BF]/u, /â[\u0080-\u00BF]/u, /Â[\u0080-\u00BF]/u, /\uFFFD/u, /\?{3,}/u];

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
checkBundleScopedTranslationKeys();
checkUnsafeMealTypeTranslationKeys();
checkLegacyLocaleFiles();

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
        issues.push(`placeholder mismatch at ${pathLabel}: en=[${[...enPlaceholders].join(', ')}], ru=[${[...ruPlaceholders].join(', ')}]`);
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

function mergeSelectedBundles(bundles, selectedBundles) {
    return selectedBundles.reduce((result, bundle) => deepMerge(result, bundles[bundle]), {});
}

function checkRuntimeTranslationKeys() {
    const files = sourceDirs.flatMap(collectSourceFiles);

    for (const filePath of files) {
        const content = fs.readFileSync(filePath, 'utf8');
        const keys = new Set([...extractPipeKeys(content), ...extractTranslateServiceKeys(content)]);

        for (const key of keys) {
            if (!availableKeys.has(key)) {
                issues.push(`missing runtime key: ${key} referenced in ${path.relative(rootDir, filePath)}`);
            }
        }
    }
}

function checkBundleScopedTranslationKeys() {
    for (const group of bundleScopedEntryPoints) {
        const availableBundleKeys = new Set(flattenKeys(mergeSelectedBundles(locales.en, group.bundles)));
        const files = collectReachableSourceFiles(group.entries);

        for (const filePath of files) {
            const content = fs.readFileSync(filePath, 'utf8');
            const keys = new Set([
                ...extractPipeKeys(content),
                ...extractTranslateServiceKeys(content),
                ...extractTranslateFunctionKeys(content),
                ...extractStaticTranslationKeyLiterals(content),
            ]);

            for (const key of keys) {
                if (!availableBundleKeys.has(key)) {
                    issues.push(
                        `missing ${group.name} bundle key: ${key} referenced in ${path.relative(rootDir, filePath)}; loaded bundles=[${group.bundles.join(', ')}]`,
                    );
                }
            }
        }
    }
}

function checkUnsafeMealTypeTranslationKeys() {
    const files = sourceDirs.flatMap(collectSourceFiles);

    for (const filePath of files) {
        const content = fs.readFileSync(filePath, 'utf8');
        for (const match of extractUnsafeMealTypeTranslationKeys(content)) {
            issues.push(
                `unsafe meal type translation key: ${match} referenced in ${path.relative(rootDir, filePath)}; normalize mealType before building MEAL_TYPES keys`,
            );
        }
    }
}

function checkLegacyLocaleFiles() {
    for (const locale of Object.keys(locales)) {
        const expected = `${JSON.stringify(mergeBundles(locales[locale]), null, 4)}\n`;
        const filePath = path.join(i18nDir, `${locale}.json`);

        if (!fs.existsSync(filePath)) {
            issues.push(`missing legacy i18n file: ${path.relative(rootDir, filePath)}`);
            continue;
        }

        const actual = fs.readFileSync(filePath, 'utf8');
        if (actual !== expected) {
            issues.push(`stale legacy i18n file: ${path.relative(rootDir, filePath)}; run node scripts/generate-legacy-i18n.mjs`);
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

function extractTranslateFunctionKeys(content) {
    const keys = [];
    const pattern = /\btranslate\(\s*(['"`])([A-Za-z0-9][A-Za-z0-9_.-]*[A-Za-z0-9])\1\s*[,)]/gu;

    for (const match of content.matchAll(pattern)) {
        keys.push(match[2]);
    }

    return keys;
}

function extractStaticTranslationKeyLiterals(content) {
    const keys = [];
    const pattern = /(['"`])([A-Z][A-Z0-9_]*(?:\.[A-Z0-9_]+)+)\1/gu;

    for (const match of content.matchAll(pattern)) {
        keys.push(match[2]);
    }

    return keys;
}

function extractUnsafeMealTypeTranslationKeys(content) {
    const keys = [];
    const pattern = /`((?:MEAL_CARD\.)?MEAL_TYPES\.\$\{\s*[\w.]+\.mealType\s*\})`/gu;

    for (const match of content.matchAll(pattern)) {
        keys.push(match[1]);
    }

    return keys;
}

function collectReachableSourceFiles(entryPoints) {
    const files = new Set();

    for (const filePath of entryPoints.map(resolveSourceFile).filter(filePath => filePath !== null)) {
        files.add(filePath);

        if (!filePath.endsWith('.ts')) {
            continue;
        }

        const content = fs.readFileSync(filePath, 'utf8');
        const templatePath = resolveTemplatePath(filePath, content);
        if (templatePath !== null) {
            files.add(templatePath);
        }
    }

    return [...files];
}

function resolveTemplatePath(filePath, content) {
    const match = /templateUrl:\s*['"]([^'"]+)['"]/u.exec(content);
    if (match === null) {
        return null;
    }

    const templatePath = path.resolve(path.dirname(filePath), match[1]);
    return fs.existsSync(templatePath) ? templatePath : null;
}

function resolveSourceFile(filePath) {
    const candidates = filePath.endsWith('.ts') ? [filePath] : [`${filePath}.ts`, path.join(filePath, 'index.ts')];
    const sourceFile = candidates.find(candidate => fs.existsSync(candidate) && !candidate.endsWith('.spec.ts'));
    return sourceFile ?? null;
}
