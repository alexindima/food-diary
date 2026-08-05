export type FdUiLineChartXAxisLabelLayout = 'angled' | 'stacked';

export type FdUiLineChartReferenceLine = {
    value: number;
    label?: string;
    color?: string;
    lineStyle?: 'solid' | 'dashed';
    outOfRangeBehavior?: 'clamp' | 'hide';
    edgePaddingRatio?: number;
};
