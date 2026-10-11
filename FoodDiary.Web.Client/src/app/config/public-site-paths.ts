import { PUBLIC_SEO_PATHS } from './public-seo-landing-routes.config';

export const PUBLIC_INFORMATION_PAGES = [
    { path: 'pricing', baseKey: 'PUBLIC_PAGES.PRICING' },
    { path: 'ai-calorie-tracker', baseKey: 'PUBLIC_PAGES.AI' },
    { path: 'about', baseKey: 'PUBLIC_PAGES.ABOUT' },
    { path: 'contact', baseKey: 'PUBLIC_PAGES.CONTACT' },
    { path: 'guides/how-to-keep-a-food-diary', baseKey: 'PUBLIC_PAGES.DIARY_GUIDE' },
    { path: 'guides/calories-in-homemade-meals', baseKey: 'PUBLIC_PAGES.RECIPE_GUIDE' },
] as const;

export const PUBLIC_MARKETING_PATHS = new Set<string>([
    '/',
    '/privacy-policy',
    ...PUBLIC_SEO_PATHS,
    ...PUBLIC_INFORMATION_PAGES.map(page => `/${page.path}`),
]);

export type PublicSiteLocale = 'en' | 'ru';
const RUSSIAN_PATH_PREFIX = '/ru';

export function stripPublicLocale(path: string): string {
    const pathname = path.split(/[?#]/u, 1)[0].replace(/\/$/u, '');
    const normalized = pathname.length > 0 ? pathname : '/';
    return normalized === RUSSIAN_PATH_PREFIX
        ? '/'
        : normalized.startsWith(`${RUSSIAN_PATH_PREFIX}/`)
          ? normalized.slice(RUSSIAN_PATH_PREFIX.length)
          : normalized;
}

export function publicSiteLocale(path = '/'): PublicSiteLocale {
    const pathname = path.split(/[?#]/u, 1)[0];
    return pathname === '/ru' || pathname.startsWith('/ru/') ? 'ru' : 'en';
}

export function localizedPublicPath(path: string, locale: PublicSiteLocale): string {
    const basePath = stripPublicLocale(path);
    return locale === 'ru' ? (basePath === '/' ? '/ru' : `/ru${basePath}`) : basePath;
}

export function isPublicMarketingPath(path: string): boolean {
    return PUBLIC_MARKETING_PATHS.has(stripPublicLocale(path));
}
