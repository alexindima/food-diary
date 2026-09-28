import { describe, expect, it } from 'vitest';

import {
    detectRecipeLanguage,
    hasSubstantiallyChangedRecipeText,
    normalizeRecipeLanguage,
    recipeLanguageText,
} from './recipe-language.utils';

const ENGLISH =
    'Heat the oil in a large pan, then add the chopped vegetables and cook until soft. Stir with a wooden spoon and serve with fresh bread.';
const RUSSIAN =
    'Разогрейте масло на сковороде, затем добавьте нарезанные овощи и готовьте до мягкости. Перемешайте и оставьте на несколько минут, после подавайте с хлебом.';

describe('recipe language suggestions', () => {
    it('recognizes substantial recipe instructions in either supported language', () => {
        expect(detectRecipeLanguage(ENGLISH)).toBe('en');
        expect(detectRecipeLanguage(RUSSIAN)).toBe('ru');
    });
    it('abstains on short, mixed and unsupported text', () => {
        for (const text of [
            'Sandora',
            'Нарежьте овощи',
            `${ENGLISH} ${RUSSIAN}`,
            'Coupez les légumes puis faites revenir doucement dans une grande casserole avec les épices et servez immédiatement.',
        ]) {
            expect(detectRecipeLanguage(text)).toBeNull();
        }
    });
    it('uses only public instructional text', () => {
        const recipe = {
            name: 'Soup',
            description: 'Warm',
            comment: RUSSIAN,
            steps: [{ title: 'Cook', instruction: ENGLISH, ingredients: [{ textName: RUSSIAN }] }],
        };
        expect(recipeLanguageText(recipe)).toBe(`soup warm cook ${ENGLISH.toLowerCase()}`);
    });
    it('does not warn again for punctuation and small edits but detects substantial rewrites', () => {
        expect(hasSubstantiallyChangedRecipeText(ENGLISH, ENGLISH.toUpperCase())).toBe(false);
        expect(hasSubstantiallyChangedRecipeText(ENGLISH, ENGLISH.replace('wooden', 'large'))).toBe(false);
        expect(hasSubstantiallyChangedRecipeText(ENGLISH, RUSSIAN)).toBe(true);
    });
    it('normalizes account preferences with an English fallback', () => {
        expect(normalizeRecipeLanguage('ru-RU')).toBe('ru');
        expect(normalizeRecipeLanguage(null)).toBe('en');
    });
});
