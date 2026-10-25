import { JSDOM } from 'jsdom';

export function inspectPrerenderTranslationState(html, locale, bundle) {
    const document = new JSDOM(html).window.document;
    const script = [...document.querySelectorAll('script[type="application/json"]')].find(element => element.id.endsWith('-state'));
    if (!script) return ['missing Angular translation transfer state'];
    let state;
    try {
        state = JSON.parse(script.textContent);
    } catch {
        return ['invalid Angular transfer JSON'];
    }
    if (state === null || Array.isArray(state) || typeof state !== 'object') return ['invalid Angular transfer object'];
    const issues = [];
    for (const name of ['core', bundle]) {
        const value = state[`fd-i18n:${locale}:${name}`];
        if (value === null || Array.isArray(value) || typeof value !== 'object')
            issues.push(`missing transferred ${locale}/${name} bundle`);
    }
    if (bundle !== 'landing' && Object.hasOwn(state, `fd-i18n:${locale}:landing`))
        issues.push('unrelated landing translations in startup state');
    if (Object.keys(state).some(key => /^fd-i18n:[^:]+:app$/u.test(key)))
        issues.push('private application translations in public startup state');
    return issues;
}
