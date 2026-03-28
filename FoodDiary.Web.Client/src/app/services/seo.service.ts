import { Injectable, inject } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';
import { DOCUMENT } from '@angular/common';
import { TranslateService } from '@ngx-translate/core';

const DEFAULT_TITLE = 'Food Diary';
const SITE_URL = 'https://fooddiary.club';
const OG_IMAGE = `${SITE_URL}/assets/pwa/icon-512x512.png`;

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
        const url = data.path ? `${SITE_URL}${data.path}` : SITE_URL;

        this.title.setTitle(pageTitle);

        this.meta.updateTag({ name: 'description', content: description });

        // Open Graph
        this.meta.updateTag({ property: 'og:title', content: pageTitle });
        this.meta.updateTag({ property: 'og:description', content: description });
        this.meta.updateTag({ property: 'og:url', content: url });
        this.meta.updateTag({ property: 'og:image', content: OG_IMAGE });
        this.meta.updateTag({ property: 'og:type', content: 'website' });
        this.meta.updateTag({ property: 'og:site_name', content: DEFAULT_TITLE });

        // Twitter Card
        this.meta.updateTag({ name: 'twitter:card', content: 'summary' });
        this.meta.updateTag({ name: 'twitter:title', content: pageTitle });
        this.meta.updateTag({ name: 'twitter:description', content: description });
        this.meta.updateTag({ name: 'twitter:image', content: OG_IMAGE });

        // Canonical URL
        this.updateCanonical(url);

        // Robots
        if (data.noIndex) {
            this.meta.updateTag({ name: 'robots', content: 'noindex, nofollow' });
        } else {
            this.meta.removeTag('name="robots"');
        }
    }

    public reset(): void {
        this.update({});
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
}
