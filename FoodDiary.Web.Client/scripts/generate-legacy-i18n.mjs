import fs from 'node:fs';
import path from 'node:path';

const rootDir = process.cwd();
const i18nDir = path.join(rootDir, 'assets', 'i18n');
const localeBundles = ['core', 'landing', 'seo', 'privacy', 'app'];
const locales = ['en', 'ru'];

for (const locale of locales) {
    const merged = localeBundles.reduce((result, bundle) => {
        const filePath = path.join(i18nDir, locale, `${bundle}.json`);
        const bundleTranslations = JSON.parse(fs.readFileSync(filePath, 'utf8'));
        return deepMerge(result, bundleTranslations);
    }, {});

    const targetPath = path.join(i18nDir, `${locale}.json`);
    fs.writeFileSync(targetPath, `${JSON.stringify(merged, null, 4)}\n`, 'utf8');
}

console.log('Legacy i18n files generated');

function deepMerge(target, source) {
    const output = { ...target };

    for (const [key, value] of Object.entries(source)) {
        const targetValue = output[key];
        output[key] = isPlainObject(targetValue) && isPlainObject(value) ? deepMerge(targetValue, value) : value;
    }

    return output;
}

function isPlainObject(value) {
    return typeof value === 'object' && value !== null && !Array.isArray(value);
}
