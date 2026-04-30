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
        documentRef.documentElement.removeAttribute('data-ui-style');
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
        expect(service.uiStyle()).toBe('classic');
        expect(documentRef.documentElement.getAttribute('data-theme')).toBe('ocean');
        expect(documentRef.documentElement.getAttribute('data-ui-style')).toBe('classic');
        expect(documentRef.documentElement.style.colorScheme).toBe('light');
    });

    it('should restore stored theme and ui style', () => {
        localStorage.setItem('fd_theme', 'leaf');
        localStorage.setItem('fd_ui_style', 'modern');

        service.applyThemeForRoute('/dashboard');

        expect(service.theme()).toBe('leaf');
        expect(service.uiStyle()).toBe('modern');
        expect(documentRef.documentElement.getAttribute('data-theme')).toBe('leaf');
        expect(documentRef.documentElement.getAttribute('data-ui-style')).toBe('modern');
        expect(documentRef.querySelector('meta[name="theme-color"]')?.getAttribute('content')).toBe('var(--fd-color-emerald-700)');
    });

    it('should use default theme on public routes without overwriting stored preferences', () => {
        localStorage.setItem('fd_theme', 'leaf');
        localStorage.setItem('fd_ui_style', 'modern');

        service.applyThemeForRoute('/auth/login');

        expect(service.theme()).toBe('ocean');
        expect(service.uiStyle()).toBe('classic');
        expect(documentRef.documentElement.getAttribute('data-theme')).toBe('ocean');
        expect(documentRef.documentElement.getAttribute('data-ui-style')).toBe('classic');
        expect(localStorage.getItem('fd_theme')).toBe('leaf');
        expect(localStorage.getItem('fd_ui_style')).toBe('modern');
    });

    it('should restore stored theme after leaving public routes', () => {
        localStorage.setItem('fd_theme', 'leaf');
        localStorage.setItem('fd_ui_style', 'modern');

        service.applyThemeForRoute('/auth/login');

        service.applyThemeForRoute('/dashboard');

        expect(service.theme()).toBe('leaf');
        expect(service.uiStyle()).toBe('modern');
        expect(documentRef.documentElement.getAttribute('data-theme')).toBe('leaf');
        expect(documentRef.documentElement.getAttribute('data-ui-style')).toBe('modern');
    });

    it('should persist user preferences without applying them while current route is public', () => {
        service.initializeTheme();

        service.syncWithUserPreferences('leaf', 'modern');

        expect(localStorage.getItem('fd_theme')).toBe('leaf');
        expect(localStorage.getItem('fd_ui_style')).toBe('modern');
        expect(service.theme()).toBe('ocean');
        expect(service.uiStyle()).toBe('classic');
        expect(documentRef.documentElement.getAttribute('data-theme')).toBe('ocean');
        expect(documentRef.documentElement.getAttribute('data-ui-style')).toBe('classic');
    });

    it('should apply dark color scheme for the dark theme', () => {
        service.setTheme('dark');

        expect(localStorage.getItem('fd_theme')).toBe('dark');
        expect(documentRef.documentElement.getAttribute('data-theme')).toBe('dark');
        expect(documentRef.documentElement.style.colorScheme).toBe('dark');
        expect(documentRef.querySelector('meta[name="theme-color"]')?.getAttribute('content')).toBe('#191c21');
    });

    it('should persist a selected theme', () => {
        service.setTheme('leaf');

        expect(localStorage.getItem('fd_theme')).toBe('leaf');
        expect(documentRef.documentElement.getAttribute('data-theme')).toBe('leaf');
        expect(documentRef.querySelector('meta[name="theme-color"]')?.getAttribute('content')).toBe('var(--fd-color-emerald-700)');
    });

    it('should persist a selected ui style', () => {
        service.setUiStyle('modern');

        expect(localStorage.getItem('fd_ui_style')).toBe('modern');
        expect(documentRef.documentElement.getAttribute('data-ui-style')).toBe('modern');
    });
});
