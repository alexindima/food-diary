import { describe, expect, it } from 'vitest';

import { isPublicMarketingPath, localizedPublicPath, publicSiteLocale, stripPublicLocale } from './public-site-paths';

describe('public URL language contract', () => {
    it('pairs a deep guide across locales without duplicating the prefix', () => {
        expect(localizedPublicPath('/ru/guides/how-to-keep-a-food-diary?source=menu#start', 'en')).toBe('/guides/how-to-keep-a-food-diary');
        expect(localizedPublicPath('/ru/guides/how-to-keep-a-food-diary/', 'ru')).toBe('/ru/guides/how-to-keep-a-food-diary');
    });

    it('recognizes root locale with query parameters and a fragment', () => {
        expect(publicSiteLocale('/ru?auth=register#form')).toBe('ru');
        expect(stripPublicLocale('/ru/?auth=register#form')).toBe('/');
        expect(localizedPublicPath('/', 'ru')).toBe('/ru');
    });

    it('does not create public alternate paths for account routes or unknown pages', () => {
        expect(isPublicMarketingPath('/products')).toBe(false);
        expect(isPublicMarketingPath('/ru/products')).toBe(false);
        expect(isPublicMarketingPath('/ru/page-that-does-not-exist')).toBe(false);
        expect(publicSiteLocale('/russian-page')).toBe('en');
    });
});
