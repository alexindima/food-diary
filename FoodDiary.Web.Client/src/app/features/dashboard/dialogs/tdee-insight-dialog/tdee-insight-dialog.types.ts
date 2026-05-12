import type { TdeeInsight } from '../../models/tdee-insight.data';

export interface TdeeInsightDialogData {
    insight: TdeeInsight | null;
}

export type TdeeInsightDialogAction =
    | { type: 'profile' }
    | { type: 'meal' }
    | { type: 'weight' }
    | { type: 'goals' }
    | { type: 'applyGoal'; target: number };

export interface TdeeSetupItem {
    readonly key: string;
    readonly icon: string;
    readonly complete: boolean;
    readonly titleKey: string;
    readonly textKey: string;
}
