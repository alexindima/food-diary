const COLOR_PRIMARY = {
    '50': '#EEF2FF',
    '100': '#E0EAFF',
    '200': '#C2D4FF',
    '300': '#9BB8FF',
    '400': '#7094FF',
    '500': '#4A6CF7',
    '600': '#3451D6',
    '700': '#2439B0',
    '800': '#1B2A80',
    '900': '#121C54',
} as const;

const COLOR_SECONDARY = {
    '50': '#F3EEFF',
    '100': '#E6DDFF',
    '200': '#CFC1FF',
    '300': '#B49EFF',
    '400': '#977AFF',
    '500': '#7B61FF',
    '600': '#644CDB',
    '700': '#4C39B7',
    '800': '#362787',
    '900': '#241856',
} as const;

const COLOR_GRAY = {
    '50': '#F9FAFB',
    '100': '#F3F4F6',
    '200': '#E5E7EB',
    '300': '#D1D5DB',
    '400': '#9CA3AF',
    '500': '#6B7280',
    '600': '#4B5563',
    '700': '#374151',
    '800': '#1F2933',
    '900': '#111827',
} as const;

const COLOR_SEMANTIC = {
    success: '#4BB543',
    warning: '#FFCC00',
    danger: '#FF3B3B',
    info: '#2F80ED',
} as const;

const COLOR_BACKGROUND = {
    page: '#F5F7FB',
    card: '#FFFFFF',
    muted: '#F0F4FA',
} as const;

const COLOR_CHART = {
    calories: '#F2994A',
    proteins: '#2D9CDB',
    fats: '#F2C94C',
    carbs: '#27AE60',
    fiber: '#9B51E0',
} as const;

const COLOR_DIALOG = {
    surface: '#FFFFFF',
    surfaceMuted: '#F8FAFF',
    border: 'rgba(17, 24, 39, 0.08)',
    overlay: 'rgba(15, 23, 42, 0.6)',
} as const;

const LAYOUT_PAGE = {
    background: COLOR_BACKGROUND.page,
    horizontalPadding: '32px',
    verticalPadding: '48px',
    contentMaxWidth: '1200px',
    sectionSpacing: '40px',
} as const;

const LAYOUT_HEADER = {
    height: '72px',
    background: COLOR_PRIMARY['800'],
    textColor: '#FFFFFF',
    shadow: '0 12px 34px rgba(15, 23, 42, 0.12)',
    horizontalPaddingLeft: '32px',
    horizontalPaddingRight: '16px',
} as const;

export const DESIGN_TOKEN_VALUES = {
    color: {
        primary: COLOR_PRIMARY,
        secondary: COLOR_SECONDARY,
        gray: COLOR_GRAY,
        semantic: COLOR_SEMANTIC,
        background: COLOR_BACKGROUND,
        chart: COLOR_CHART,
        dialog: COLOR_DIALOG,
    },
    layout: {
        page: LAYOUT_PAGE,
        header: LAYOUT_HEADER,
    },
    button: {
        primary: {
            background: {
                default: COLOR_PRIMARY['500'],
                hover: COLOR_PRIMARY['600'],
                active: COLOR_PRIMARY['700'],
                disabled: COLOR_PRIMARY['300'],
            },
            border: {
                default: 'transparent',
                hover: 'transparent',
                active: 'transparent',
                disabled: 'transparent',
            },
            text: {
                default: '#FFFFFF',
                disabled: '#FFFFFFA0',
            },
        },
        secondary: {
            background: {
                default: '#FFFFFF',
                hover: COLOR_GRAY['100'],
                active: COLOR_GRAY['200'],
                disabled: COLOR_GRAY['200'],
            },
            border: {
                default: COLOR_GRAY['300'],
                hover: COLOR_GRAY['400'],
                active: COLOR_GRAY['400'],
                disabled: COLOR_GRAY['200'],
            },
            text: {
                default: COLOR_GRAY['900'],
                disabled: COLOR_GRAY['500'],
            },
        },
        outline: {
            background: {
                default: 'transparent',
                hover: COLOR_PRIMARY['50'],
                active: COLOR_PRIMARY['100'],
                disabled: 'transparent',
            },
            border: {
                default: COLOR_PRIMARY['500'],
                hover: COLOR_PRIMARY['600'],
                active: COLOR_PRIMARY['600'],
                disabled: COLOR_GRAY['300'],
            },
            text: {
                default: COLOR_PRIMARY['500'],
                disabled: COLOR_GRAY['400'],
            },
        },
        ghost: {
            background: {
                default: 'transparent',
                hover: COLOR_PRIMARY['50'],
                active: COLOR_PRIMARY['100'],
                disabled: 'transparent',
            },
            border: {
                default: 'transparent',
                hover: 'transparent',
                active: 'transparent',
                disabled: 'transparent',
            },
            text: {
                default: COLOR_PRIMARY['500'],
                disabled: COLOR_GRAY['400'],
            },
        },
        danger: {
            background: {
                default: COLOR_SEMANTIC.danger,
                hover: '#E03535',
                active: '#C42F2F',
                disabled: '#FFB3B3',
            },
            border: {
                default: 'transparent',
                hover: 'transparent',
                active: 'transparent',
                disabled: 'transparent',
            },
            text: {
                default: '#FFFFFF',
                disabled: '#FFFFFFA0',
            },
        },
    },
} as const;

export type DesignTokenValues = typeof DESIGN_TOKEN_VALUES;

export const DESIGN_TOKEN_CSS_VARIABLES = {
    color: {
        primary: {
            50: '--fd-color-primary-50',
            100: '--fd-color-primary-100',
            200: '--fd-color-primary-200',
            300: '--fd-color-primary-300',
            400: '--fd-color-primary-400',
            500: '--fd-color-primary-500',
            600: '--fd-color-primary-600',
            700: '--fd-color-primary-700',
            800: '--fd-color-primary-800',
            900: '--fd-color-primary-900',
        },
        secondary: {
            50: '--fd-color-secondary-50',
            100: '--fd-color-secondary-100',
            200: '--fd-color-secondary-200',
            300: '--fd-color-secondary-300',
            400: '--fd-color-secondary-400',
            500: '--fd-color-secondary-500',
            600: '--fd-color-secondary-600',
            700: '--fd-color-secondary-700',
            800: '--fd-color-secondary-800',
            900: '--fd-color-secondary-900',
        },
        gray: {
            50: '--fd-color-gray-50',
            100: '--fd-color-gray-100',
            200: '--fd-color-gray-200',
            300: '--fd-color-gray-300',
            400: '--fd-color-gray-400',
            500: '--fd-color-gray-500',
            600: '--fd-color-gray-600',
            700: '--fd-color-gray-700',
            800: '--fd-color-gray-800',
            900: '--fd-color-gray-900',
        },
        semantic: {
            success: '--fd-color-success',
            warning: '--fd-color-warning',
            danger: '--fd-color-danger',
            info: '--fd-color-info',
        },
        background: {
            page: '--fd-color-bg-page',
            card: '--fd-color-bg-card',
            muted: '--fd-color-bg-muted',
        },
        chart: {
            calories: '--fd-color-chart-calories',
            proteins: '--fd-color-chart-proteins',
            fats: '--fd-color-chart-fats',
            carbs: '--fd-color-chart-carbs',
            fiber: '--fd-color-chart-fiber',
        },
        dialog: {
            surface: '--fd-color-dialog-surface',
            surfaceMuted: '--fd-color-dialog-surface-muted',
            border: '--fd-color-dialog-border',
            overlay: '--fd-color-dialog-overlay',
        },
    },
    layout: {
        page: {
            background: '--fd-layout-page-background',
            horizontalPadding: '--fd-layout-page-horizontal-padding',
            verticalPadding: '--fd-layout-page-vertical-padding',
            contentMaxWidth: '--fd-layout-page-content-max-width',
            sectionSpacing: '--fd-layout-page-section-spacing',
        },
        header: {
            height: '--fd-layout-header-height',
            background: '--fd-layout-header-background',
            textColor: '--fd-layout-header-text-color',
            shadow: '--fd-layout-header-shadow',
            horizontalPaddingLeft: '--fd-layout-header-horizontal-padding-left',
            horizontalPaddingRight: '--fd-layout-header-horizontal-padding-right',
        },
    },
    button: {
        primary: {
            background: {
                default: '--fd-button-primary-background-default',
                hover: '--fd-button-primary-background-hover',
                active: '--fd-button-primary-background-active',
                disabled: '--fd-button-primary-background-disabled',
            },
            border: {
                default: '--fd-button-primary-border-default',
                hover: '--fd-button-primary-border-hover',
                active: '--fd-button-primary-border-active',
                disabled: '--fd-button-primary-border-disabled',
            },
            text: {
                default: '--fd-button-primary-text-default',
                disabled: '--fd-button-primary-text-disabled',
            },
        },
        secondary: {
            background: {
                default: '--fd-button-secondary-background-default',
                hover: '--fd-button-secondary-background-hover',
                active: '--fd-button-secondary-background-active',
                disabled: '--fd-button-secondary-background-disabled',
            },
            border: {
                default: '--fd-button-secondary-border-default',
                hover: '--fd-button-secondary-border-hover',
                active: '--fd-button-secondary-border-active',
                disabled: '--fd-button-secondary-border-disabled',
            },
            text: {
                default: '--fd-button-secondary-text-default',
                disabled: '--fd-button-secondary-text-disabled',
            },
        },
        outline: {
            background: {
                default: '--fd-button-outline-background-default',
                hover: '--fd-button-outline-background-hover',
                active: '--fd-button-outline-background-active',
                disabled: '--fd-button-outline-background-disabled',
            },
            border: {
                default: '--fd-button-outline-border-default',
                hover: '--fd-button-outline-border-hover',
                active: '--fd-button-outline-border-active',
                disabled: '--fd-button-outline-border-disabled',
            },
            text: {
                default: '--fd-button-outline-text-default',
                disabled: '--fd-button-outline-text-disabled',
            },
        },
        ghost: {
            background: {
                default: '--fd-button-ghost-background-default',
                hover: '--fd-button-ghost-background-hover',
                active: '--fd-button-ghost-background-active',
                disabled: '--fd-button-ghost-background-disabled',
            },
            border: {
                default: '--fd-button-ghost-border-default',
                hover: '--fd-button-ghost-border-hover',
                active: '--fd-button-ghost-border-active',
                disabled: '--fd-button-ghost-border-disabled',
            },
            text: {
                default: '--fd-button-ghost-text-default',
                disabled: '--fd-button-ghost-text-disabled',
            },
        },
        danger: {
            background: {
                default: '--fd-button-danger-background-default',
                hover: '--fd-button-danger-background-hover',
                active: '--fd-button-danger-background-active',
                disabled: '--fd-button-danger-background-disabled',
            },
            border: {
                default: '--fd-button-danger-border-default',
                hover: '--fd-button-danger-border-hover',
                active: '--fd-button-danger-border-active',
                disabled: '--fd-button-danger-border-disabled',
            },
            text: {
                default: '--fd-button-danger-text-default',
                disabled: '--fd-button-danger-text-disabled',
            },
        },
        'secondary-text': {
            background: {
                default: '--fd-button-secondary-text-background-default',
                hover: '--fd-button-secondary-text-background-hover',
                active: '--fd-button-secondary-text-background-active',
                disabled: '--fd-button-secondary-text-background-disabled',
            },
            border: {
                default: '--fd-button-secondary-text-border-default',
                hover: '--fd-button-secondary-text-border-hover',
                active: '--fd-button-secondary-text-border-active',
                disabled: '--fd-button-secondary-text-border-disabled',
            },
            text: {
                default: '--fd-button-secondary-text-text-default',
                disabled: '--fd-button-secondary-text-text-disabled',
            },
        },
        'danger-ghost': {
            background: {
                default: '--fd-button-danger-ghost-background-default',
                hover: '--fd-button-danger-ghost-background-hover',
                active: '--fd-button-danger-ghost-background-active',
                disabled: '--fd-button-danger-ghost-background-disabled',
            },
            border: {
                default: '--fd-button-danger-ghost-border-default',
                hover: '--fd-button-danger-ghost-border-hover',
                active: '--fd-button-danger-ghost-border-active',
                disabled: '--fd-button-danger-ghost-border-disabled',
            },
            text: {
                default: '--fd-button-danger-ghost-text-default',
                disabled: '--fd-button-danger-ghost-text-disabled',
            },
        },
    },
} as const;

export type DesignTokenCssVariables = typeof DESIGN_TOKEN_CSS_VARIABLES;

export const getColorCssVariable = <
    Palette extends keyof DesignTokenCssVariables['color'],
    Shade extends keyof DesignTokenCssVariables['color'][Palette],
>(palette: Palette, shade: Shade): string =>
    `var(${DESIGN_TOKEN_CSS_VARIABLES.color[palette][shade]})`;

export const getButtonCssVariable = <
    Variant extends keyof DesignTokenCssVariables['button'],
    Property extends keyof DesignTokenCssVariables['button'][Variant],
    State extends keyof DesignTokenCssVariables['button'][Variant][Property],
>(variant: Variant, property: Property, state: State): string =>
    `var(${DESIGN_TOKEN_CSS_VARIABLES.button[variant][property][state]})`;
