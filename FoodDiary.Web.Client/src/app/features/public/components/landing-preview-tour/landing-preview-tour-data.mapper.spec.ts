import { describe, expect, it } from 'vitest';

import { buildLandingPreviewContent } from './landing-preview-tour-data.mapper';

const NOW = new Date('2026-05-15T12:00:00Z');
const LUNCH_CALORIES = 430;
const YOGURT_DRAFT_AMOUNT = 150;

describe('landing preview content', () => {
    it('should build localized preview products, recipes and meals', () => {
        const content = buildLandingPreviewContent(key => `t:${key}`, NOW);

        expect(content.heroSummaryCard.nutrientBars.map(bar => bar.id)).toEqual(['protein', 'carbs', 'fats', 'fiber']);
        expect(content.previewProducts.map(product => product.name)).toEqual([
            't:LANDING_PREVIEW_TOUR.PREVIEW_DATA.PRODUCTS.YOGURT.NAME',
            't:LANDING_PREVIEW_TOUR.PREVIEW_DATA.PRODUCTS.GRANOLA.NAME',
        ]);
        expect(content.previewProducts[0]?.createdAt).toBe(NOW);
        expect(content.previewRecipes.map(recipe => recipe.createdAt)).toEqual([NOW.toISOString(), NOW.toISOString()]);
        expect(content.guestMealEntries[1]?.meal?.totalCalories).toBe(LUNCH_CALORIES);
    });

    it('should build quick draft items from preview products and recipes', () => {
        const content = buildLandingPreviewContent(key => key, NOW);

        expect(content.previewQuickItems.map(item => item.key)).toEqual([
            'product-preview-yogurt',
            'product-preview-granola',
            'recipe-preview-bowl',
        ]);
        expect(content.previewQuickItems[0]?.amount).toBe(YOGURT_DRAFT_AMOUNT);
        expect(content.previewQuickItems[2]?.type).toBe('recipe');
    });
});
