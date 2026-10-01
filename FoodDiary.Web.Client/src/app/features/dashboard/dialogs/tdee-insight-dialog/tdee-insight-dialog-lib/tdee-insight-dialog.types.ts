import type { TdeeInsight } from '../../../models/tdee-insight.data';

export type TdeeInsightDialogData = {
    insight: TdeeInsight | null;
    applyGoalAsync: (target: number) => Promise<boolean>;
};

export type TdeeInsightDialogAction = { type: 'profile' } | { type: 'meal' } | { type: 'weight' } | { type: 'goals' };

export type TdeeSetupItem = {
    readonly key: string;
    readonly icon: string;
    readonly complete: boolean;
    readonly titleKey: string;
    readonly textKey: string;
};
