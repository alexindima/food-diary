import { describe, expect, it } from 'vitest';

import { publicRecipeFixture } from '../../features/recipes/lib/public-recipe.test-helper';
import { publicRecipeSeo } from '../../features/recipes/lib/public-recipe.utils';
import { parseRouteSeoData } from './app-seo-data.utils';

describe('app seo data utils', () => {
    it('returns null for non-object route data', () => {
        expect(parseRouteSeoData(null)).toBeNull();
        expect(parseRouteSeoData('title')).toBeNull();
    });

    it('parses only supported seo fields', () => {
        expect(
            parseRouteSeoData({
                titleKey: 'SEO.TITLE',
                descriptionKey: 'SEO.DESCRIPTION',
                path: '/dashboard',
                noIndex: true,
                structuredDataBaseKey: 'SEO.STRUCTURED.BASE',
                structuredDataFeatureKeys: ['FEATURE.ONE', 1, 'FEATURE.TWO'],
                structuredDataFaqKeys: ['FAQ.ONE', false],
                ignored: 'value',
            }),
        ).toEqual({
            titleKey: 'SEO.TITLE',
            descriptionKey: 'SEO.DESCRIPTION',
            path: '/dashboard',
            noIndex: true,
            structuredDataBaseKey: 'SEO.STRUCTURED.BASE',
            structuredDataFeatureKeys: ['FEATURE.ONE', 'FEATURE.TWO'],
            structuredDataFaqKeys: ['FAQ.ONE'],
        });
    });

    it('keeps the published recipe title, description, cover and structured data from the resolver', () => {
        const recipe = publicRecipeFixture();
        const seo = parseRouteSeoData({ ...publicRecipeSeo(recipe), recipe, error: null });

        expect(seo).toMatchObject({
            title: 'Soup',
            description: 'A soup',
            imageUrl: 'cover.jpg',
        });
        expect(seo?.recipeStructuredData).toMatchObject({
            '@type': 'Recipe',
            name: 'Soup',
            image: ['cover.jpg', 'second.jpg'],
            recipeYield: '2',
        });
        expect(seo).not.toHaveProperty('recipe');
        expect(seo).not.toHaveProperty('error');
    });
});
