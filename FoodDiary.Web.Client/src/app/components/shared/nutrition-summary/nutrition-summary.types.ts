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
