export type NutrientBar = {
    id: string;
    label: string;
    labelKey?: string;
    current: number;
    target: number;
    unit: string;
    unitKey?: string;
    colorStart: string;
    colorEnd: string;
};

export type NutrientBarViewModel = {
    labelText: string;
    unitText: string;
    valueColor: string;
    fillBackground: string;
    fillWidth: number;
} & NutrientBar;
