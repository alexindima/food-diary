import { describe, expect, it } from 'vitest';

import { buildSeoLandingViewModel, resolveSeoLandingPageData, SEO_PAGE_LABEL_KEYS } from './seo-landing.mapper';

const DEFAULT_AUDIENCE_ITEM_COUNT = 4;

describe('seo landing page data', () => {
    it('should resolve valid route data and ignore unsupported related paths', () => {
        const page = resolveSeoLandingPageData({
            baseKey: 'SEO_PAGES.TEST',
            featureKeys: ['FEATURE', 1],
            stepKeys: ['STEP'],
            faqKeys: ['FAQ'],
            relatedPaths: ['food-diary', 'unknown'],
        });

        expect(page).toEqual({
            baseKey: 'SEO_PAGES.TEST',
            featureKeys: ['FEATURE'],
            stepKeys: ['STEP'],
            faqKeys: ['FAQ'],
            relatedPaths: ['food-diary'],
        });
    });

    it('should fall back to food diary page data for invalid route data', () => {
        expect(resolveSeoLandingPageData(null)).toEqual({
            baseKey: 'SEO_PAGES.FOOD_DIARY',
            featureKeys: [],
            stepKeys: [],
            faqKeys: [],
            relatedPaths: [],
        });
    });
});

describe('seo landing view model', () => {
    it('should build translation keys for all public page sections', () => {
        const viewModel = buildSeoLandingViewModel({
            baseKey: 'SEO_PAGES.TEST',
            featureKeys: ['LOGGING'],
            stepKeys: ['START'],
            faqKeys: ['PRICE'],
            relatedPaths: ['calorie-counter'],
        });

        expect(viewModel.hero.titleKey).toBe('SEO_PAGES.TEST.TITLE');
        expect(viewModel.audience.items).toHaveLength(DEFAULT_AUDIENCE_ITEM_COUNT);
        expect(viewModel.features.items[0]).toEqual({
            key: 'LOGGING',
            titleKey: 'SEO_PAGES.TEST.FEATURES.ITEMS.LOGGING.TITLE',
            textKey: 'SEO_PAGES.TEST.FEATURES.ITEMS.LOGGING.TEXT',
        });
        expect(viewModel.faq.items[0]?.questionKey).toBe('SEO_PAGES.TEST.FAQ.ITEMS.PRICE.QUESTION');
        expect(viewModel.cta.primaryKey).toBe('SEO_PAGES.TEST.CTA.PRIMARY');
        expect(viewModel.relatedPages).toEqual([
            {
                path: '/calorie-counter',
                labelKey: SEO_PAGE_LABEL_KEYS['calorie-counter'],
            },
        ]);
    });
});
