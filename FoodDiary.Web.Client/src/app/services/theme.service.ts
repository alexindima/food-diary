import { DOCUMENT } from '@angular/common';
import { computed, inject, Injectable, signal } from '@angular/core';

import {
    APP_THEMES,
    APP_UI_STYLES,
    AppThemeDefinition,
    AppThemeName,
    AppUiStyleDefinition,
    AppUiStyleName,
    DEFAULT_APP_THEME,
    DEFAULT_APP_UI_STYLE,
    isAppThemeName,
    isAppUiStyleName,
} from '../theme/app-theme.config';

@Injectable({
    providedIn: 'root',
})
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
    public readonly uiStyles = APP_UI_STYLES;
    public readonly activeThemeDefinition = computed(() => this.getThemeDefinition(this.themeState()));
    public readonly activeUiStyleDefinition = computed(() => this.getUiStyleDefinition(this.uiStyleState()));

    public initializeTheme(): void {
        const storedTheme = this.getStoredTheme();
        const storedUiStyle = this.getStoredUiStyle();

        this.applyTheme(storedTheme ?? DEFAULT_APP_THEME, false);
        this.applyUiStyle(storedUiStyle ?? DEFAULT_APP_UI_STYLE, false);
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
        this.applyTheme(this.resolveTheme(theme), true);
        this.applyUiStyle(this.resolveUiStyle(uiStyle), true);
    }

    private applyTheme(theme: AppThemeName, persist: boolean): void {
        this.themeState.set(theme);

        const root = this.document.documentElement;
        root.setAttribute('data-theme', theme);
        root.style.colorScheme = this.getThemeDefinition(theme).colorScheme;

        this.updateBrowserThemeColor(this.getThemeDefinition(theme).browserThemeColor);

        if (persist) {
            this.localStorageRef?.setItem(this.themeStorageKey, theme);
        }
    }

    private applyUiStyle(uiStyle: AppUiStyleName, persist: boolean): void {
        this.uiStyleState.set(uiStyle);
        this.document.documentElement.setAttribute('data-ui-style', uiStyle);

        if (persist) {
            this.localStorageRef?.setItem(this.uiStyleStorageKey, uiStyle);
        }
    }

    private getStoredTheme(): AppThemeName | null {
        const value = this.localStorageRef?.getItem(this.themeStorageKey) ?? null;
        if (!value || value === 'undefined' || value === 'null' || !isAppThemeName(value)) {
            return null;
        }

        return value;
    }

    private getStoredUiStyle(): AppUiStyleName | null {
        const value = this.localStorageRef?.getItem(this.uiStyleStorageKey) ?? null;
        if (!value || value === 'undefined' || value === 'null' || !isAppUiStyleName(value)) {
            return null;
        }

        return value;
    }

    private getThemeDefinition(themeName: AppThemeName): AppThemeDefinition {
        return APP_THEMES.find(theme => theme.name === themeName) ?? APP_THEMES[0];
    }

    private getUiStyleDefinition(styleName: AppUiStyleName): AppUiStyleDefinition {
        return APP_UI_STYLES.find(style => style.name === styleName) ?? APP_UI_STYLES[0];
    }

    private resolveTheme(theme: string | null | undefined): AppThemeName {
        return isAppThemeName(theme) ? theme : DEFAULT_APP_THEME;
    }

    private resolveUiStyle(uiStyle: string | null | undefined): AppUiStyleName {
        return isAppUiStyleName(uiStyle) ? uiStyle : DEFAULT_APP_UI_STYLE;
    }

    private updateBrowserThemeColor(color: string): void {
        const metaThemeColor = this.document.querySelector<HTMLMetaElement>('meta[name="theme-color"]');
        if (metaThemeColor) {
            metaThemeColor.content = color;
        }
    }
}
