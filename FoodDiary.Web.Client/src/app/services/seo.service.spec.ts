import { describe, it, expect, beforeEach, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { Meta, Title } from '@angular/platform-browser';
import { DOCUMENT } from '@angular/common';
import { TranslateService } from '@ngx-translate/core';
import { SeoService } from './seo.service';

describe('SeoService', () => {
    let service: SeoService;
    let titleService: Title;
    let metaService: Meta;
    let translateService: { instant: ReturnType<typeof vi.fn> };
    let document: Document;

    beforeEach(() => {
        translateService = {
            instant: vi.fn((key: string) => key),
        };

        TestBed.configureTestingModule({
            providers: [SeoService, { provide: TranslateService, useValue: translateService }],
        });

        service = TestBed.inject(SeoService);
        titleService = TestBed.inject(Title);
        metaService = TestBed.inject(Meta);
        document = TestBed.inject(DOCUMENT);
    });

    describe('update', () => {
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

        it('should update OG tags', () => {
            translateService.instant.mockImplementation((key: string) => (key === 'SEO.MEALS' ? 'Meals' : 'Default'));

            service.update({ titleKey: 'SEO.MEALS', path: '/meals' });

            expect(metaService.getTag('property="og:title"')?.content).toBe('Meals | Food Diary');
            expect(metaService.getTag('property="og:url"')?.content).toBe('https://fooddiary.club/meals');
            expect(metaService.getTag('property="og:type"')?.content).toBe('website');
            expect(metaService.getTag('property="og:site_name"')?.content).toBe('Food Diary');
            expect(metaService.getTag('property="og:image"')?.content).toContain('icon-512x512.png');
        });

        it('should update Twitter card tags', () => {
            service.update({ titleKey: null, path: '/' });

            expect(metaService.getTag('name="twitter:card"')?.content).toBe('summary');
            expect(metaService.getTag('name="twitter:title"')?.content).toBe('Food Diary');
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

        it('should use site URL as default when path is not provided', () => {
            service.update({});

            expect(metaService.getTag('property="og:url"')?.content).toBe('https://fooddiary.club');
        });
    });

    describe('reset', () => {
        it('should reset to defaults', () => {
            service.update({ titleKey: 'SEO.PRODUCTS', noIndex: true });
            service.reset();

            expect(titleService.getTitle()).toBe('Food Diary');
            expect(metaService.getTag('name="robots"')).toBeNull();
        });
    });
});
