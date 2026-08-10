export type WeeklyCheckInTrendCardKey = 'calories' | 'protein' | 'weight' | 'waist' | 'hydration';

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

export type WeeklyCheckInSuggestionViewModel = {
    key: string;
    labelKey: string;
};

export type WeeklyReviewInsightTone = 'positive' | 'info' | 'attention';

export type WeeklyReviewInsightViewModel = {
    key: string;
    icon: string;
    tone: WeeklyReviewInsightTone;
    labelKey: string;
};

export type WeeklyReviewViewModel = {
    daysLogged: number;
    hasEnoughData: boolean;
    summaryKey: string;
    focusTitleKey: string;
    focusDescriptionKey: string;
    focusTarget: number;
    insights: WeeklyReviewInsightViewModel[];
};
