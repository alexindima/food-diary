import { describe, expect, it } from 'vitest';

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
});
