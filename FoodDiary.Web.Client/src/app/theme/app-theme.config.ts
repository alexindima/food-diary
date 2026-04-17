export type AppThemeName = 'ocean' | 'leaf';

export interface AppThemeDefinition {
    name: AppThemeName;
    labelKey: string;
    browserThemeColor: string;
}

export const APP_THEMES: readonly AppThemeDefinition[] = [
    {
        name: 'ocean',
        labelKey: 'SIDEBAR.THEME_OCEAN',
        browserThemeColor: 'var(--fd-color-primary-700)',
    },
    {
        name: 'leaf',
        labelKey: 'SIDEBAR.THEME_LEAF',
        browserThemeColor: 'var(--fd-color-emerald-700)',
    },
] as const;

export const DEFAULT_APP_THEME: AppThemeName = 'ocean';

export function isAppThemeName(value: string | null | undefined): value is AppThemeName {
    return APP_THEMES.some(theme => theme.name === value);
}
