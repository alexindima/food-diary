export const DESIGN_TOKEN_VALUES = {
    colors: {
        primary: '#667c89',
        accent: '#f5a623',
        success: '#50e3c2',
        info: '#4a90e2',
        warn: '#ff6b6b',
        surface: '#ffffff',
        surfaceMuted: '#f5f5f5',
        text: '#333333',
        textMuted: '#757575',
        border: '#e0e0e0',
    },
    radius: {
        xs: '4px',
        sm: '6px',
        md: '8px',
        lg: '12px',
        pill: '999px',
    },
    spacing: {
        xxs: '4px',
        xs: '8px',
        sm: '12px',
        md: '16px',
        lg: '24px',
        xl: '32px',
        xxl: '40px',
    },
    typography: {
        fontFamily: '"Inter", "Segoe UI", system-ui, -apple-system, BlinkMacSystemFont, sans-serif',
        fontSizeBase: '1rem',
        lineHeightBase: '1.5',
        fontWeightRegular: '400',
        fontWeightMedium: '500',
        fontWeightBold: '600',
    },
    elevation: {
        xs: '0 1px 2px rgba(15, 23, 42, 0.08)',
        sm: '0 2px 6px rgba(15, 23, 42, 0.12)',
        md: '0 4px 12px rgba(15, 23, 42, 0.15)',
    },
} as const;

export const DESIGN_TOKEN_CSS_VARIABLES = {
    colors: {
        primary: '--fd-color-primary',
        accent: '--fd-color-accent',
        success: '--fd-color-success',
        info: '--fd-color-info',
        warn: '--fd-color-warn',
        surface: '--fd-color-surface',
        surfaceMuted: '--fd-color-surface-muted',
        text: '--fd-color-text',
        textMuted: '--fd-color-text-muted',
        border: '--fd-color-border',
    },
    radius: {
        xs: '--fd-radius-xs',
        sm: '--fd-radius-sm',
        md: '--fd-radius-md',
        lg: '--fd-radius-lg',
        pill: '--fd-radius-pill',
    },
    spacing: {
        xxs: '--fd-spacing-xxs',
        xs: '--fd-spacing-xs',
        sm: '--fd-spacing-sm',
        md: '--fd-spacing-md',
        lg: '--fd-spacing-lg',
        xl: '--fd-spacing-xl',
        xxl: '--fd-spacing-xxl',
    },
    typography: {
        fontFamily: '--fd-font-family-base',
        fontSizeBase: '--fd-font-size-base',
        lineHeightBase: '--fd-line-height-base',
        fontWeightRegular: '--fd-font-weight-regular',
        fontWeightMedium: '--fd-font-weight-medium',
        fontWeightBold: '--fd-font-weight-bold',
    },
    elevation: {
        xs: '--fd-shadow-xs',
        sm: '--fd-shadow-sm',
        md: '--fd-shadow-md',
    },
} as const;

export type DesignTokenValues = typeof DESIGN_TOKEN_VALUES;
export type DesignTokenGroup = keyof DesignTokenValues;
export type DesignTokenKeys<G extends DesignTokenGroup> = keyof DesignTokenValues[G];

export const getCssVariable = <G extends DesignTokenGroup>(
    group: G,
    token: DesignTokenKeys<G>,
): string => `var(${DESIGN_TOKEN_CSS_VARIABLES[group][token] as string})`;

