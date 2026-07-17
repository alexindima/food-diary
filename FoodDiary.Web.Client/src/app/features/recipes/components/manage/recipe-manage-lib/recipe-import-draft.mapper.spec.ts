import { describe, expect, it } from 'vitest';

import { parseRecipeImportDraft } from './recipe-import-draft.mapper';
import { createRecipeIngredientValue } from './recipe-manage-form.mapper';

const labels = {
    ingredientsLabel: 'Ingredients',
    sourceLabel: 'Source',
};

describe('parseRecipeImportDraft', () => {
    it('returns null when text and source URL are empty', () => {
        expect(parseRecipeImportDraft({ ...labels, text: '  ', sourceUrl: ' ', existingIngredients: [] })).toBeNull();
    });

    it('parses English sections and removes ordered-list prefixes', () => {
        const draft = getDraft({
            ...labels,
            text: 'Pancakes\nIngredients:\nFlour\nMilk\nSteps:\n1. Mix batter\n2) Cook',
            sourceUrl: 'https://example.test/recipe',
            existingIngredients: [],
        });

        expect(draft).toMatchObject({
            name: 'Pancakes',
            description: 'Ingredients\nFlour\nMilk',
            comment: 'Source: https://example.test/recipe',
        });
        expect(draft.steps.map(step => step.description)).toEqual(['Mix batter', 'Cook']);
    });

    it('recognizes Russian headings', () => {
        const draft = getDraft({
            ...labels,
            text: 'Суп\nИнгредиенты:\nВода\nПриготовление:\n- Смешать',
            sourceUrl: '',
            existingIngredients: [],
        });

        expect(draft.description).toBe('Ingredients\nВода');
        expect(draft.steps[0]?.description).toBe('Смешать');
    });

    it('clones existing ingredients into every imported step', () => {
        const ingredient = { ...createRecipeIngredientValue(), foodName: 'Flour' };
        const draft = getDraft({
            ...labels,
            text: 'Pancakes\nSteps:\nMix\nCook',
            sourceUrl: '',
            existingIngredients: [ingredient],
        });

        expect(draft.steps[0]?.ingredients).toEqual([ingredient]);
        expect(draft.steps[1]?.ingredients).toEqual([ingredient]);
        expect(draft.steps[0]?.ingredients[0]).not.toBe(ingredient);
        expect(draft.steps[0]?.ingredients[0]).not.toBe(draft.steps[1]?.ingredients[0]);
    });

    it('uses the URL as title and creates a fallback step when text is empty', () => {
        const draft = getDraft({
            ...labels,
            text: '',
            sourceUrl: 'https://example.test/recipe',
            existingIngredients: [],
        });

        expect(draft.name).toBe('https://example.test/recipe');
        expect(draft.steps[0]?.description).toBe('https://example.test/recipe');
    });
});

function getDraft(input: Parameters<typeof parseRecipeImportDraft>[0]): NonNullable<ReturnType<typeof parseRecipeImportDraft>> {
    const draft = parseRecipeImportDraft(input);
    if (draft === null) {
        throw new Error('Expected a recipe import draft.');
    }

    return draft;
}
