import { Injectable, inject } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';
import { DOCUMENT } from '@angular/common';
import { TranslateService } from '@ngx-translate/core';

const DEFAULT_TITLE = 'Food Diary';
const ENGLISH_SITE_URL = 'https://fooddiary.club';
const RUSSIAN_SITE_URL = 'https://xn--b1adbcbrouc8l.xn--p1ai';
const RUSSIAN_HOSTS = new Set(['xn--b1adbcbrouc8l.xn--p1ai', 'www.xn--b1adbcbrouc8l.xn--p1ai']);

export interface SeoData {
    titleKey?: string | null;
    descriptionKey?: string;
    path?: string;
    noIndex?: boolean;
}

@Injectable({ providedIn: 'root' })
export class SeoService {
    private readonly title = inject(Title);
    private readonly meta = inject(Meta);
    private readonly document = inject(DOCUMENT);
    private readonly translate = inject(TranslateService);

    public update(data: SeoData): void {
        const translatedTitle = data.titleKey ? this.translate.instant(data.titleKey) : null;
        const pageTitle = translatedTitle ? `${translatedTitle} | ${DEFAULT_TITLE}` : DEFAULT_TITLE;
        const description = data.descriptionKey
            ? this.translate.instant(data.descriptionKey)
            : this.translate.instant('SEO.DEFAULT_DESCRIPTION');
        const currentUrl = this.buildSiteUrl(this.getCurrentSiteUrl(), data.path);

        this.title.setTitle(pageTitle);

        this.meta.updateTag({ name: 'description', content: description });

        this.meta.updateTag({ property: 'og:title', content: pageTitle });
        this.meta.updateTag({ property: 'og:description', content: description });
        this.meta.updateTag({ property: 'og:url', content: currentUrl });
        this.meta.updateTag({ property: 'og:image', content: `${this.getCurrentSiteUrl()}/assets/pwa/icon-512x512.png` });
        this.meta.updateTag({ property: 'og:type', content: 'website' });
        this.meta.updateTag({ property: 'og:site_name', content: DEFAULT_TITLE });

        this.meta.updateTag({ name: 'twitter:card', content: 'summary' });
        this.meta.updateTag({ name: 'twitter:title', content: pageTitle });
        this.meta.updateTag({ name: 'twitter:description', content: description });
        this.meta.updateTag({ name: 'twitter:image', content: `${this.getCurrentSiteUrl()}/assets/pwa/icon-512x512.png` });

        this.updateCanonical(currentUrl);
        this.updateAlternateLinks(data.path);

        if (data.noIndex) {
            this.meta.updateTag({ name: 'robots', content: 'noindex, nofollow' });
        } else {
            this.meta.removeTag('name="robots"');
        }
    }

    public reset(): void {
        this.update({});
    }

    private getCurrentSiteUrl(): string {
        const hostname = this.document.location.hostname?.toLowerCase();
        return hostname && RUSSIAN_HOSTS.has(hostname) ? RUSSIAN_SITE_URL : ENGLISH_SITE_URL;
    }

    private buildSiteUrl(baseUrl: string, path?: string): string {
        if (!path || path === '/') {
            return baseUrl;
        }

        return `${baseUrl}${path}`;
    }

    private updateCanonical(url: string): void {
        let link: HTMLLinkElement | null = this.document.querySelector('link[rel="canonical"]');
        if (!link) {
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
            let link = this.document.querySelector(`link[rel="alternate"][hreflang="${alternate.hreflang}"]`) as HTMLLinkElement | null;
            if (!link) {
                link = this.document.createElement('link');
                link.setAttribute('rel', 'alternate');
                link.setAttribute('hreflang', alternate.hreflang);
                this.document.head.appendChild(link);
            }

            link.setAttribute('href', alternate.href);
        }
    }
}
