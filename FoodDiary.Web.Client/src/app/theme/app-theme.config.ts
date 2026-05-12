export type AppThemeName = 'ocean' | 'leaf' | 'dark';
export type AppUiStyleName = 'classic' | 'modern';

export type AppThemeDefinition = {
    name: AppThemeName;
    labelKey: string;
    browserThemeColor: string;
    colorScheme: 'light' | 'dark';
};

export type AppUiStyleDefinition = {
    name: AppUiStyleName;
    labelKey: string;
};

export const APP_THEMES: readonly AppThemeDefinition[] = [
    {
        name: 'ocean',
        labelKey: 'SIDEBAR.THEME_OCEAN',
        browserThemeColor: 'var(--fd-color-primary-700)',
        colorScheme: 'light',
    },
    {
        name: 'leaf',
        labelKey: 'SIDEBAR.THEME_LEAF',
        browserThemeColor: 'var(--fd-color-emerald-700)',
        colorScheme: 'light',
    },
    {
        name: 'dark',
        labelKey: 'SIDEBAR.THEME_DARK',
        browserThemeColor: '#191c21',
        colorScheme: 'dark',
    },
] as const;

export const DEFAULT_APP_THEME: AppThemeName = 'ocean';
export const DEFAULT_APP_UI_STYLE: AppUiStyleName = 'classic';

export const APP_UI_STYLES: readonly AppUiStyleDefinition[] = [
    {
        name: 'classic',
        labelKey: 'USER_MANAGE.UI_STYLE_OPTIONS.CLASSIC',
    },
    {
        name: 'modern',
        labelKey: 'USER_MANAGE.UI_STYLE_OPTIONS.MODERN',
    },
] as const;

export function isAppThemeName(value: string | null | undefined): value is AppThemeName {
    return APP_THEMES.some(theme => theme.name === value);
}

export function isAppUiStyleName(value: string | null | undefined): value is AppUiStyleName {
    return APP_UI_STYLES.some(style => style.name === value);
}
