export type WeeklyCheckInTrendCardKey = 'calories' | 'protein' | 'weight' | 'hydration';

export type WeeklyCheckInTrendCardConfig = {
    key: WeeklyCheckInTrendCardKey;
    labelKey: string;
    value: number;
    unitKey: string;
    numberFormat: string;
    invertPositive?: boolean;
    unitSeparator?: string;
};

export type WeeklyCheckInTrendCardViewModel = {
    key: WeeklyCheckInTrendCardKey;
    labelKey: string;
    value: number;
    unitKey: string;
    unitSeparator: string;
    numberFormat: string;
    valuePrefix: string;
    color: string;
    icon: string;
};
