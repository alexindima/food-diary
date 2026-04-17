import '@angular/compiler';
import { DOCUMENT } from '@angular/common';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { ThemeService } from './theme.service';

describe('ThemeService', () => {
    let service: ThemeService;
    let documentRef: Document;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [ThemeService],
        });

        service = TestBed.inject(ThemeService);
        documentRef = TestBed.inject(DOCUMENT);
        localStorage.clear();
        documentRef.documentElement.removeAttribute('data-theme');
        documentRef.documentElement.style.colorScheme = '';

        let metaThemeColor = documentRef.querySelector('meta[name="theme-color"]');
        if (!metaThemeColor) {
            metaThemeColor = documentRef.createElement('meta');
            metaThemeColor.setAttribute('name', 'theme-color');
            documentRef.head.appendChild(metaThemeColor);
        }

        metaThemeColor.setAttribute('content', '#1d4ed8');
    });

    it('should initialize with the default theme', () => {
        service.initializeTheme();

        expect(service.theme()).toBe('ocean');
        expect(documentRef.documentElement.getAttribute('data-theme')).toBe('ocean');
        expect(documentRef.documentElement.style.colorScheme).toBe('light');
    });

    it('should restore a stored theme', () => {
        localStorage.setItem('fd_theme', 'leaf');

        service.initializeTheme();

        expect(service.theme()).toBe('leaf');
        expect(documentRef.documentElement.getAttribute('data-theme')).toBe('leaf');
        expect(documentRef.querySelector('meta[name="theme-color"]')?.getAttribute('content')).toBe(
            'var(--fd-color-emerald-700)',
        );
    });

    it('should persist a selected theme', () => {
        service.setTheme('leaf');

        expect(localStorage.getItem('fd_theme')).toBe('leaf');
        expect(documentRef.documentElement.getAttribute('data-theme')).toBe('leaf');
        expect(documentRef.querySelector('meta[name="theme-color"]')?.getAttribute('content')).toBe(
            'var(--fd-color-emerald-700)',
        );
    });
});
