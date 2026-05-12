import { DOCUMENT } from '@angular/common';
import { TestBed } from '@angular/core/testing';
import { Meta, Title } from '@angular/platform-browser';
import { TranslateService } from '@ngx-translate/core';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { SeoService } from './seo.service';

type SeoServiceInternals = {
    getCurrentSiteUrl: () => string;
};

let service: SeoService;
let titleService: Title;
let metaService: Meta;
let translateService: { getCurrentLang: ReturnType<typeof vi.fn>; instant: ReturnType<typeof vi.fn> };
let document: Document;

beforeEach(() => {
    translateService = {
        instant: vi.fn((key: string) => key),
        getCurrentLang: vi.fn(() => 'en'),
    };

    TestBed.configureTestingModule({
        providers: [SeoService, { provide: TranslateService, useValue: translateService }],
    });

    service = TestBed.inject(SeoService);
    titleService = TestBed.inject(Title);
    metaService = TestBed.inject(Meta);
    document = TestBed.inject(DOCUMENT);
});

describe('SeoService title and description', () => {
    it('should set page title with suffix when titleKey provided', () => {
        translateService.instant.mockImplementation((key: string) => (key === 'SEO.PRODUCTS' ? 'Products' : key));

        service.update({ titleKey: 'SEO.PRODUCTS' });
        expect(titleService.getTitle()).toBe('Products | Food Diary');
    });

    it('should set default title when titleKey is null', () => {
        service.update({ titleKey: null });
        expect(titleService.getTitle()).toBe('Food Diary');
    });

    it('should set default title when titleKey is undefined', () => {
        service.update({});
        expect(titleService.getTitle()).toBe('Food Diary');
    });

    it('should set meta description from descriptionKey', () => {
        translateService.instant.mockImplementation((key: string) => (key === 'SEO.LANDING_DESCRIPTION' ? 'Track your meals' : key));

        service.update({ descriptionKey: 'SEO.LANDING_DESCRIPTION' });

        const tag = metaService.getTag('name="description"');
        expect(tag?.content).toBe('Track your meals');
    });

    it('should use default description when descriptionKey is absent', () => {
        translateService.instant.mockImplementation((key: string) => (key === 'SEO.DEFAULT_DESCRIPTION' ? 'Default description' : key));

        service.update({});

        const tag = metaService.getTag('name="description"');
        expect(tag?.content).toBe('Default description');
    });
});

describe('SeoService social tags', () => {
    it('should update OG tags', () => {
        translateService.instant.mockImplementation((key: string) => (key === 'SEO.MEALS' ? 'Meals' : 'Default'));

        service.update({ titleKey: 'SEO.MEALS', path: '/meals' });

        expect(metaService.getTag('property="og:title"')?.content).toBe('Meals | Food Diary');
        expect(metaService.getTag('property="og:url"')?.content).toBe('https://fooddiary.club/meals');
        expect(metaService.getTag('property="og:type"')?.content).toBe('website');
        expect(metaService.getTag('property="og:site_name"')?.content).toBe('Food Diary');
        expect(metaService.getTag('property="og:image"')?.content).toContain('icon-512x512.png');
        expect(metaService.getTag('property="og:locale"')?.content).toBe('en_US');
    });

    it('should update Twitter card tags', () => {
        service.update({ titleKey: null, path: '/' });

        expect(metaService.getTag('name="twitter:card"')?.content).toBe('summary');
        expect(metaService.getTag('name="twitter:title"')?.content).toBe('Food Diary');
    });
});

describe('SeoService canonical urls', () => {
    it('should use current russian domain for canonical and og:url', () => {
        vi.spyOn(service as unknown as SeoServiceInternals, 'getCurrentSiteUrl').mockReturnValue('https://xn--b1adbcbrouc8l.xn--p1ai');

        service.update({ path: '/meals' });

        expect(metaService.getTag('property="og:url"')?.content).toBe('https://xn--b1adbcbrouc8l.xn--p1ai/meals');
        expect(document.querySelector('link[rel="canonical"]')?.getAttribute('href')).toBe('https://xn--b1adbcbrouc8l.xn--p1ai/meals');
    });

    it('should create or update canonical link', () => {
        service.update({ path: '/products' });

        const link = document.querySelector('link[rel="canonical"]');
        expect(link?.getAttribute('href')).toBe('https://fooddiary.club/products');
    });

    it('should update existing canonical link', () => {
        service.update({ path: '/first' });
        service.update({ path: '/second' });

        const links = document.querySelectorAll('link[rel="canonical"]');
        expect(links.length).toBe(1);
        expect(links[0].getAttribute('href')).toBe('https://fooddiary.club/second');
    });

    it('should strip query params and fragments from canonical urls', () => {
        service.update({ path: '/products?utm_source=google#top' });

        expect(document.querySelector('link[rel="canonical"]')?.getAttribute('href')).toBe('https://fooddiary.club/products');
        expect(metaService.getTag('property="og:url"')?.content).toBe('https://fooddiary.club/products');
    });

    it('should use site URL as default when path is not provided', () => {
        service.update({});

        expect(metaService.getTag('property="og:url"')?.content).toBe('https://fooddiary.club');
    });
});

describe('SeoService alternate links', () => {
    it('should create alternate hreflang links', () => {
        service.update({ path: '/products' });

        expect(document.querySelector('link[rel="alternate"][hreflang="en"]')?.getAttribute('href')).toBe(
            'https://fooddiary.club/products',
        );
        expect(document.querySelector('link[rel="alternate"][hreflang="ru"]')?.getAttribute('href')).toBe(
            'https://xn--b1adbcbrouc8l.xn--p1ai/products',
        );
        expect(document.querySelector('link[rel="alternate"][hreflang="x-default"]')?.getAttribute('href')).toBe(
            'https://fooddiary.club/products',
        );
    });
});

describe('SeoService structured data and robots', () => {
    it('should render structured data for the current page', () => {
        translateService.instant.mockImplementation((key: string) => (key === 'SEO.DEFAULT_DESCRIPTION' ? 'Default description' : key));

        service.update({ path: '/privacy-policy' });

        const script = document.querySelector('script[data-seo-structured-data="app"]');
        expect(script).not.toBeNull();

        const payload = JSON.parse(script?.textContent ?? '{}') as { ['@graph']: Array<Record<string, string>> };
        expect(payload['@graph'].some(item => item['@type'] === 'SoftwareApplication')).toBe(true);
        expect(payload['@graph'].some(item => item['url'] === 'https://fooddiary.club/privacy-policy')).toBe(true);
    });

    it('should add robots noindex when noIndex is true', () => {
        service.update({ noIndex: true });

        const tag = metaService.getTag('name="robots"');
        expect(tag?.content).toBe('noindex, nofollow');
    });

    it('should remove robots tag when noIndex is false', () => {
        service.update({ noIndex: true });
        service.update({ noIndex: false });

        const tag = metaService.getTag('name="robots"');
        expect(tag).toBeNull();
    });
});

describe('SeoService reset', () => {
    it('should reset to defaults', () => {
        service.update({ titleKey: 'SEO.PRODUCTS', noIndex: true });
        service.reset();

        expect(titleService.getTitle()).toBe('Food Diary');
        expect(metaService.getTag('name="robots"')).toBeNull();
    });
});
