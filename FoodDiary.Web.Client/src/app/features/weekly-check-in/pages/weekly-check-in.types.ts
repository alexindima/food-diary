export type WeeklyCheckInTrendCardKey = 'calories' | 'protein' | 'weight' | 'hydration';

export interface WeeklyCheckInTrendCardConfig {
    key: WeeklyCheckInTrendCardKey;
    labelKey: string;
    value: number;
    unitKey: string;
    numberFormat: string;
    invertPositive?: boolean;
    unitSeparator?: string;
}

export interface WeeklyCheckInTrendCardViewModel {
    key: WeeklyCheckInTrendCardKey;
    labelKey: string;
    value: number;
    unitKey: string;
    unitSeparator: string;
    numberFormat: string;
    valuePrefix: string;
    color: string;
    icon: string;
}
