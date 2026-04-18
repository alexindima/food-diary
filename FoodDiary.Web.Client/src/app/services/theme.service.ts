import { DOCUMENT } from '@angular/common';
import { computed, inject, Injectable, signal } from '@angular/core';

import { APP_THEMES, AppThemeDefinition, AppThemeName, DEFAULT_APP_THEME, isAppThemeName } from '../theme/app-theme.config';

@Injectable({
    providedIn: 'root',
})
export class ThemeService {
    private readonly document = inject(DOCUMENT);
    private readonly storageKey = 'fd_theme';
    private readonly localStorageRef = typeof localStorage === 'undefined' ? null : localStorage;
    private readonly themeState = signal<AppThemeName>(DEFAULT_APP_THEME);

    public readonly theme = this.themeState.asReadonly();
    public readonly themes = APP_THEMES;
    public readonly activeThemeDefinition = computed(() => this.getThemeDefinition(this.themeState()));

    public initializeTheme(): void {
        const storedTheme = this.getStoredTheme();
        this.applyTheme(storedTheme ?? DEFAULT_APP_THEME, false);
    }

    public setTheme(theme: string): void {
        if (!isAppThemeName(theme)) {
            return;
        }

        this.applyTheme(theme, true);
    }

    public syncWithUserTheme(theme: string | null | undefined): void {
        if (!theme || !isAppThemeName(theme)) {
            this.applyTheme(DEFAULT_APP_THEME, true);
            return;
        }

        this.applyTheme(theme, true);
    }

    private applyTheme(theme: AppThemeName, persist: boolean): void {
        this.themeState.set(theme);

        const root = this.document.documentElement;
        root.setAttribute('data-theme', theme);
        root.style.colorScheme = this.getThemeDefinition(theme).colorScheme;

        this.updateBrowserThemeColor(this.getThemeDefinition(theme).browserThemeColor);

        if (persist) {
            this.localStorageRef?.setItem(this.storageKey, theme);
        }
    }

    private getStoredTheme(): AppThemeName | null {
        const value = this.localStorageRef?.getItem(this.storageKey) ?? null;
        if (!value || value === 'undefined' || value === 'null' || !isAppThemeName(value)) {
            return null;
        }

        return value;
    }

    private getThemeDefinition(themeName: AppThemeName): AppThemeDefinition {
        return APP_THEMES.find(theme => theme.name === themeName) ?? APP_THEMES[0];
    }

    private updateBrowserThemeColor(color: string): void {
        const metaThemeColor = this.document.querySelector<HTMLMetaElement>('meta[name="theme-color"]');
        if (metaThemeColor) {
            metaThemeColor.content = color;
        }
    }
}
