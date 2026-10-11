import { describe, expect, it } from 'vitest';

import prerenderRoutesContent from '../../prerender-routes.txt?raw';
import { PUBLIC_SEO_LANDING_ROUTES, SEO_PAGE_LABEL_KEYS } from './public-seo-landing-routes.config';
import { isPublicMarketingPath, localizedPublicPath, PUBLIC_MARKETING_PATHS } from './public-site-paths';

describe('public SEO landing routes config', () => {
    it('keeps route paths, labels, and related links in sync', () => {
        const routePaths = new Set(PUBLIC_SEO_LANDING_ROUTES.map(route => route.path));

        expect(routePaths.size).toBe(PUBLIC_SEO_LANDING_ROUTES.length);

        for (const route of PUBLIC_SEO_LANDING_ROUTES) {
            expect(SEO_PAGE_LABEL_KEYS[route.path]).toBe(route.titleKey);

            for (const relatedPath of route.relatedPaths) {
                expect(routePaths.has(relatedPath)).toBe(true);
            }
        }
    });

    it('keeps prerendered SEO routes covered by route translation bundles', () => {
        const prerenderRoutes = readPrerenderRoutes();
        for (const route of prerenderRoutes) {
            expect(isPublicMarketingPath(route)).toBe(true);
        }

        for (const route of PUBLIC_MARKETING_PATHS) {
            expect(prerenderRoutes.has(localizedPublicPath(route, 'en'))).toBe(true);
            expect(prerenderRoutes.has(localizedPublicPath(route, 'ru'))).toBe(true);
        }
    });
});

function readPrerenderRoutes(): Set<string> {
    const routes = prerenderRoutesContent
        .split(/\r?\n/u)
        .map(route => route.trim())
        .filter(route => route.length > 0);

    return new Set(routes);
}
