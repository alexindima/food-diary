import type { RecursivePartial } from '../../../../shared/lib/common.data';

const TOOLTIP_DECIMAL_PLACES = 2;

export type NutrientsSummaryConfigInternal = {
    styles: {
        common: {
            gap: number;
            infoBreakpoints: {
                columnLayout: number;
                chartBlockSize: number;
                gap: number;
            };
        };
        charts: {
            chartBlockSize: number;
            gap: number;
            breakpoints: {
                columnLayout: number;
                chartBlockSize: number;
                gap: number;
            };
        };
        info: {
            lineStyles: {
                nutrients: {
                    fontSize: number;
                    lineHeight: number;
                    colorWidthMultiplier: number;
                };
            };
        };
    };
    content: {
        hidePieChart: boolean;
    };
};

export type NutrientsSummaryConfig = RecursivePartial<NutrientsSummaryConfigInternal>;

export const DEFAULT_NUTRIENTS_SUMMARY_CONFIG: NutrientsSummaryConfigInternal = {
    styles: {
        common: {
            gap: 16,
            infoBreakpoints: {
                columnLayout: 600,
                chartBlockSize: 256,
                gap: 12,
            },
        },
        charts: {
            chartBlockSize: 192,
            gap: 16,
            breakpoints: {
                columnLayout: 768,
                chartBlockSize: 192,
                gap: 12,
            },
        },
        info: {
            lineStyles: {
                nutrients: {
                    fontSize: 16,
                    lineHeight: 20,
                    colorWidthMultiplier: 2,
                },
            },
        },
    },
    content: {
        hidePieChart: false,
    },
};

export function mergeNutrientsSummaryConfig(userConfig: NutrientsSummaryConfig): NutrientsSummaryConfigInternal {
    const styles = userConfig.styles;
    const commonStyles = styles?.common;
    const infoBreakpoints = commonStyles?.infoBreakpoints;
    const chartStyles = styles?.charts;
    const chartBreakpoints = chartStyles?.breakpoints;
    const infoStyles = styles?.info;
    const nutrientLineStyles = infoStyles?.lineStyles?.nutrients;

    return {
        ...DEFAULT_NUTRIENTS_SUMMARY_CONFIG,
        ...userConfig,
        styles: {
            ...DEFAULT_NUTRIENTS_SUMMARY_CONFIG.styles,
            ...styles,
            common: {
                ...DEFAULT_NUTRIENTS_SUMMARY_CONFIG.styles.common,
                ...commonStyles,
                infoBreakpoints: {
                    ...DEFAULT_NUTRIENTS_SUMMARY_CONFIG.styles.common.infoBreakpoints,
                    ...infoBreakpoints,
                },
            },
            charts: {
                ...DEFAULT_NUTRIENTS_SUMMARY_CONFIG.styles.charts,
                ...chartStyles,
                breakpoints: {
                    ...DEFAULT_NUTRIENTS_SUMMARY_CONFIG.styles.charts.breakpoints,
                    ...chartBreakpoints,
                },
            },
            info: {
                ...DEFAULT_NUTRIENTS_SUMMARY_CONFIG.styles.info,
                ...infoStyles,
                lineStyles: {
                    nutrients: {
                        ...DEFAULT_NUTRIENTS_SUMMARY_CONFIG.styles.info.lineStyles.nutrients,
                        ...nutrientLineStyles,
                    },
                },
            },
        },
        content: {
            ...DEFAULT_NUTRIENTS_SUMMARY_CONFIG.content,
            ...userConfig.content,
        },
    };
}

export type NutrientsSummaryTooltipContext = {
    label: string;
    raw: unknown;
};

export function formatNutrientsSummaryTooltip(context: NutrientsSummaryTooltipContext, gramsLabel: string): string {
    const label = context.label.length > 0 ? context.label : '';
    const rawValue = Number(context.raw);
    const value = Number.isNaN(rawValue) ? 0 : rawValue;
    const formattedValue = parseFloat(value.toFixed(TOOLTIP_DECIMAL_PLACES));

    return `${label}: ${formattedValue} ${gramsLabel}`;
}
