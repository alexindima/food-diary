import { DOCUMENT } from '@angular/common';
import { inject, Injectable, signal } from '@angular/core';

import {
    APP_THEMES,
    type AppThemeDefinition,
    type AppThemeName,
    type AppUiStyleName,
    DEFAULT_APP_THEME,
    DEFAULT_APP_UI_STYLE,
    isAppThemeName,
    isAppUiStyleName,
} from '../theme/app-theme.config';

const PUBLIC_SEO_PATHS = new Set([
    '/food-diary',
    '/calorie-counter',
    '/meal-planner',
    '/macro-tracker',
    '/intermittent-fasting',
    '/meal-tracker',
    '/weight-loss-app',
    '/dietologist-collaboration',
    '/nutrition-planner',
    '/weight-tracker',
    '/body-progress-tracker',
    '/shopping-list-for-meal-planning',
    '/nutrition-tracker',
    '/food-log',
    '/protein-tracker',
    '/meal-prep-planner',
]);

@Injectable({ providedIn: 'root' })
export class ThemeService {
    private readonly document = inject(DOCUMENT);
    private readonly themeStorageKey = 'fd_theme';
    private readonly uiStyleStorageKey = 'fd_ui_style';
    private readonly localStorageRef = typeof localStorage === 'undefined' ? null : localStorage;
    private readonly themeState = signal<AppThemeName>(DEFAULT_APP_THEME);
    private readonly uiStyleState = signal<AppUiStyleName>(DEFAULT_APP_UI_STYLE);

    public readonly theme = this.themeState.asReadonly();
    public readonly uiStyle = this.uiStyleState.asReadonly();
    public readonly themes = APP_THEMES;

    public initializeTheme(): void {
        this.applyThemeForRoute(this.document.location.pathname);
    }

    public applyThemeForRoute(pathname: string): void {
        if (this.isPublicRoute(pathname)) {
            this.applyDefaultPublicTheme();
            return;
        }

        this.applyStoredTheme();
    }

    public setTheme(theme: string): void {
        if (!isAppThemeName(theme)) {
            return;
        }

        this.applyTheme(theme, true);
    }

    public setUiStyle(uiStyle: string): void {
        if (!isAppUiStyleName(uiStyle)) {
            return;
        }

        this.applyUiStyle(uiStyle, true);
    }

    public syncWithUserPreferences(theme: string | null | undefined, uiStyle: string | null | undefined): void {
        const resolvedTheme = this.resolveTheme(theme);
        const resolvedUiStyle = this.resolveUiStyle(uiStyle);

        if (this.isPublicRoute(this.document.location.pathname)) {
            this.persistThemePreference(resolvedTheme);
            this.persistUiStylePreference(resolvedUiStyle);
            return;
        }

        this.applyTheme(resolvedTheme, true);
        this.applyUiStyle(resolvedUiStyle, true);
    }

    private applyDefaultPublicTheme(): void {
        this.applyTheme(DEFAULT_APP_THEME, false);
        this.applyUiStyle(DEFAULT_APP_UI_STYLE, false);
    }

    private applyStoredTheme(): void {
        const storedTheme = this.getStoredTheme();
        const storedUiStyle = this.getStoredUiStyle();

        this.applyTheme(storedTheme ?? DEFAULT_APP_THEME, false);
        this.applyUiStyle(storedUiStyle ?? DEFAULT_APP_UI_STYLE, false);
    }

    private applyTheme(theme: AppThemeName, persist: boolean): void {
        this.themeState.set(theme);

        const root = this.document.documentElement;
        root.setAttribute('data-theme', theme);
        root.style.colorScheme = this.getThemeDefinition(theme).colorScheme;

        this.updateBrowserThemeColor(this.getThemeDefinition(theme).browserThemeColor);

        if (persist) {
            this.persistThemePreference(theme);
        }
    }

    private applyUiStyle(uiStyle: AppUiStyleName, persist: boolean): void {
        this.uiStyleState.set(uiStyle);
        this.document.documentElement.setAttribute('data-ui-style', uiStyle);

        if (persist) {
            this.persistUiStylePreference(uiStyle);
        }
    }

    private persistThemePreference(theme: AppThemeName): void {
        this.localStorageRef?.setItem(this.themeStorageKey, theme);
    }

    private persistUiStylePreference(uiStyle: AppUiStyleName): void {
        this.localStorageRef?.setItem(this.uiStyleStorageKey, uiStyle);
    }

    private getStoredTheme(): AppThemeName | null {
        const value = this.localStorageRef?.getItem(this.themeStorageKey) ?? null;
        if (value === null || value.length === 0 || value === 'undefined' || value === 'null' || !isAppThemeName(value)) {
            return null;
        }

        return value;
    }

    private getStoredUiStyle(): AppUiStyleName | null {
        const value = this.localStorageRef?.getItem(this.uiStyleStorageKey) ?? null;
        if (value === null || value.length === 0 || value === 'undefined' || value === 'null' || !isAppUiStyleName(value)) {
            return null;
        }

        return value;
    }

    private getThemeDefinition(themeName: AppThemeName): AppThemeDefinition {
        return APP_THEMES.find(theme => theme.name === themeName) ?? APP_THEMES[0];
    }

    private resolveTheme(theme: string | null | undefined): AppThemeName {
        return isAppThemeName(theme) ? theme : DEFAULT_APP_THEME;
    }

    private resolveUiStyle(uiStyle: string | null | undefined): AppUiStyleName {
        return isAppUiStyleName(uiStyle) ? uiStyle : DEFAULT_APP_UI_STYLE;
    }

    private isPublicRoute(pathname: string): boolean {
        const normalizedPath = pathname.split(/[?#]/u, 1)[0].toLowerCase();
        return (
            normalizedPath === '/' ||
            normalizedPath.startsWith('/auth') ||
            normalizedPath === '/privacy-policy' ||
            PUBLIC_SEO_PATHS.has(normalizedPath)
        );
    }

    private updateBrowserThemeColor(color: string): void {
        const metaThemeColor = this.document.querySelector<HTMLMetaElement>('meta[name="theme-color"]');
        if (metaThemeColor !== null) {
            metaThemeColor.content = color;
        }
    }
}
