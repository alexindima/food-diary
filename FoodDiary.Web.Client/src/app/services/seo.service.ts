import { DOCUMENT } from '@angular/common';
import { inject, Injectable } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';
import { TranslateService } from '@ngx-translate/core';

const DEFAULT_TITLE = 'Food Diary';
const ENGLISH_SITE_URL = 'https://fooddiary.club';
const RUSSIAN_SITE_URL = 'https://xn--b1adbcbrouc8l.xn--p1ai';
const RUSSIAN_HOSTS = new Set(['xn--b1adbcbrouc8l.xn--p1ai', 'www.xn--b1adbcbrouc8l.xn--p1ai']);
const STRUCTURED_DATA_SELECTOR = 'script[data-seo-structured-data="app"]';
const ROOT_PATHS = new Set(['', '/']);

export type SeoData = {
    titleKey?: string | null;
    descriptionKey?: string;
    path?: string;
    noIndex?: boolean;
    structuredDataBaseKey?: string;
    structuredDataFeatureKeys?: readonly string[];
    structuredDataFaqKeys?: readonly string[];
};

@Injectable({ providedIn: 'root' })
export class SeoService {
    private readonly title = inject(Title);
    private readonly meta = inject(Meta);
    private readonly document = inject(DOCUMENT);
    private readonly translate = inject(TranslateService);

    public update(data: SeoData): void {
        const currentSiteUrl = this.getCurrentSiteUrl();
        const translatedTitle = data.titleKey !== null && data.titleKey !== undefined ? this.translate.instant(data.titleKey) : null;
        const pageTitle = translatedTitle !== null && translatedTitle.length > 0 ? `${translatedTitle} | ${DEFAULT_TITLE}` : DEFAULT_TITLE;
        const description =
            data.descriptionKey !== undefined && data.descriptionKey.length > 0
                ? this.translate.instant(data.descriptionKey)
                : this.translate.instant('SEO.DEFAULT_DESCRIPTION');
        const currentUrl = this.buildSiteUrl(currentSiteUrl, data.path);

        this.title.setTitle(pageTitle);

        this.meta.updateTag({ name: 'description', content: description });
        this.meta.updateTag({ property: 'og:title', content: pageTitle });
        this.meta.updateTag({ property: 'og:description', content: description });
        this.meta.updateTag({ property: 'og:url', content: currentUrl });
        this.meta.updateTag({ property: 'og:image', content: `${currentSiteUrl}/assets/pwa/icon-512x512.png` });
        this.meta.updateTag({ property: 'og:type', content: 'website' });
        this.meta.updateTag({ property: 'og:site_name', content: DEFAULT_TITLE });
        this.meta.updateTag({ property: 'og:locale', content: this.getOpenGraphLocale() });

        this.meta.updateTag({ name: 'twitter:card', content: 'summary' });
        this.meta.updateTag({ name: 'twitter:title', content: pageTitle });
        this.meta.updateTag({ name: 'twitter:description', content: description });
        this.meta.updateTag({ name: 'twitter:image', content: `${currentSiteUrl}/assets/pwa/icon-512x512.png` });

        this.updateCanonical(currentUrl);
        this.updateAlternateLinks(data.path);
        this.updateStructuredData(pageTitle, description, currentUrl, data);

        if (data.noIndex === true) {
            this.meta.updateTag({ name: 'robots', content: 'noindex, nofollow' });
        } else {
            this.meta.removeTag('name="robots"');
        }
    }

    public reset(): void {
        this.update({});
    }

    private getCurrentSiteUrl(): string {
        const hostname = this.document.location.hostname.toLowerCase();
        return hostname.length > 0 && RUSSIAN_HOSTS.has(hostname) ? RUSSIAN_SITE_URL : ENGLISH_SITE_URL;
    }

    private buildSiteUrl(baseUrl: string, path?: string): string {
        const normalizedPath = this.normalizePath(path);
        if (normalizedPath.length === 0 || normalizedPath === '/') {
            return baseUrl;
        }

        return `${baseUrl}${normalizedPath}`;
    }

    private normalizePath(path?: string): string {
        if (path === undefined || path.length === 0) {
            return '/';
        }

        const withoutHash = path.split('#', 1)[0] ?? '/';
        const withoutQuery = withoutHash.split('?', 1)[0] ?? '/';

        if (withoutQuery.length === 0 || withoutQuery === '/') {
            return '/';
        }

        return withoutQuery.endsWith('/') ? withoutQuery.slice(0, -1) : withoutQuery;
    }

    private getOpenGraphLocale(): string {
        const currentLang = this.translate.getCurrentLang().toLowerCase();
        return currentLang === 'ru' ? 'ru_RU' : 'en_US';
    }

    private updateCanonical(url: string): void {
        let link: HTMLLinkElement | null = this.document.querySelector('link[rel="canonical"]') ?? null;
        if (link === null) {
            link = this.document.createElement('link');
            link.setAttribute('rel', 'canonical');
            this.document.head.appendChild(link);
        }
        link.setAttribute('href', url);
    }

    private updateAlternateLinks(path?: string): void {
        const alternates = [
            { hreflang: 'en', href: this.buildSiteUrl(ENGLISH_SITE_URL, path) },
            { hreflang: 'ru', href: this.buildSiteUrl(RUSSIAN_SITE_URL, path) },
            { hreflang: 'x-default', href: this.buildSiteUrl(ENGLISH_SITE_URL, path) },
        ];

        for (const alternate of alternates) {
            let link = this.document.querySelector(`link[rel="alternate"][hreflang="${alternate.hreflang}"]`) ?? null;
            if (link === null) {
                link = this.document.createElement('link');
                link.setAttribute('rel', 'alternate');
                link.setAttribute('hreflang', alternate.hreflang);
                this.document.head.appendChild(link);
            }

            link.setAttribute('href', alternate.href);
        }
    }

    private updateStructuredData(pageTitle: string, description: string, currentUrl: string, data: SeoData): void {
        let script = this.document.querySelector<HTMLScriptElement>(STRUCTURED_DATA_SELECTOR) ?? null;
        if (script === null) {
            script = this.document.createElement('script');
            script.type = 'application/ld+json';
            script.setAttribute('data-seo-structured-data', 'app');
            this.document.head.appendChild(script);
        }

        const currentSiteUrl = this.getCurrentSiteUrl();
        const inLanguage = this.translate.getCurrentLang() === 'ru' ? 'ru' : 'en';
        const normalizedPath = this.normalizePath(this.document.location.pathname);
        const isLandingPage = ROOT_PATHS.has(normalizedPath);
        const featureList = isLandingPage
            ? this.getLandingFeatureList()
            : this.getStructuredFeatureList(data.structuredDataBaseKey, data.structuredDataFeatureKeys);
        const faqEntity = isLandingPage
            ? this.getLandingFaqEntity(currentUrl)
            : this.getStructuredFaqEntity(data.structuredDataBaseKey, data.structuredDataFaqKeys, currentUrl);
        const structuredData = {
            '@context': 'https://schema.org',
            '@graph': [
                {
                    '@type': 'WebSite',
                    name: DEFAULT_TITLE,
                    url: currentSiteUrl,
                    inLanguage,
                },
                {
                    '@type': 'SoftwareApplication',
                    name: DEFAULT_TITLE,
                    applicationCategory: 'HealthApplication',
                    operatingSystem: 'Web',
                    url: currentUrl,
                    description,
                    inLanguage,
                    ...(featureList !== undefined ? { featureList } : {}),
                    offers: {
                        '@type': 'Offer',
                        price: '0',
                        priceCurrency: 'USD',
                        availability: 'https://schema.org/InStock',
                    },
                },
                {
                    '@type': 'WebPage',
                    name: pageTitle,
                    url: currentUrl,
                    description,
                    isPartOf: {
                        '@type': 'WebSite',
                        name: DEFAULT_TITLE,
                        url: currentSiteUrl,
                    },
                    inLanguage,
                },
                ...(faqEntity !== null ? [faqEntity] : []),
            ],
        };

        script.textContent = JSON.stringify(structuredData);
    }

    private getLandingFeatureList(): string[] {
        return [
            this.translate.instant('FEATURES.ITEMS.LOG_MEALS.TITLE'),
            this.translate.instant('FEATURES.ITEMS.MEAL_PLANS.TITLE'),
            this.translate.instant('FEATURES.ITEMS.STATISTICS.TITLE'),
            this.translate.instant('FEATURES.ITEMS.BODY_HISTORY.TITLE'),
            this.translate.instant('FEATURES.ITEMS.FASTING.TITLE'),
            this.translate.instant('LANDING_DIETOLOGIST.CARD.TITLE'),
        ];
    }

    private getLandingFaqEntity(currentUrl: string): Record<string, unknown> {
        const faqItems = ['APP_SCOPE', 'PLANNING', 'PROGRESS', 'DIETOLOGIST', 'TRACKING', 'SAFETY'] as const;

        return {
            '@type': 'FAQPage',
            url: currentUrl,
            inLanguage: this.translate.getCurrentLang() === 'ru' ? 'ru' : 'en',
            mainEntity: faqItems.map(item => ({
                '@type': 'Question',
                name: this.translate.instant(`LANDING_FAQ.ITEMS.${item}.QUESTION`),
                acceptedAnswer: {
                    '@type': 'Answer',
                    text: this.translate.instant(`LANDING_FAQ.ITEMS.${item}.ANSWER`),
                },
            })),
        };
    }

    private getStructuredFeatureList(baseKey?: string, featureKeys?: readonly string[]): string[] | undefined {
        if (baseKey === undefined || baseKey.length === 0 || featureKeys === undefined || featureKeys.length === 0) {
            return undefined;
        }

        return featureKeys.map(key => this.translate.instant(`${baseKey}.FEATURES.ITEMS.${key}.TITLE`));
    }

    private getStructuredFaqEntity(
        baseKey: string | undefined,
        faqKeys: readonly string[] | undefined,
        currentUrl: string,
    ): Record<string, unknown> | null {
        if (baseKey === undefined || baseKey.length === 0 || faqKeys === undefined || faqKeys.length === 0) {
            return null;
        }

        return {
            '@type': 'FAQPage',
            url: currentUrl,
            inLanguage: this.translate.getCurrentLang() === 'ru' ? 'ru' : 'en',
            mainEntity: faqKeys.map(item => ({
                '@type': 'Question',
                name: this.translate.instant(`${baseKey}.FAQ.ITEMS.${item}.QUESTION`),
                acceptedAnswer: {
                    '@type': 'Answer',
                    text: this.translate.instant(`${baseKey}.FAQ.ITEMS.${item}.ANSWER`),
                },
            })),
        };
    }
}
