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
        browserThemeColor: '#1d4ed8',
    },
    {
        name: 'leaf',
        labelKey: 'SIDEBAR.THEME_LEAF',
        browserThemeColor: '#047857',
    },
] as const;

export const DEFAULT_APP_THEME: AppThemeName = 'ocean';

export function isAppThemeName(value: string | null | undefined): value is AppThemeName {
    return APP_THEMES.some(theme => theme.name === value);
}
