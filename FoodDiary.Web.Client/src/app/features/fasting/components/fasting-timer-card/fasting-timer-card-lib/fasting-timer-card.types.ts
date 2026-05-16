import type { FastingOccurrenceKind } from '../../../models/fasting.data';

export type FastingTimerCardLayout = 'dashboard' | 'page';

export type FastingTimerCardDisplayItem = {
    className: string;
    text: string;
};

export type FastingTimerCardDisplayGroup = {
    className: string;
    items: FastingTimerCardDisplayItem[];
};

export type FastingTimerCardState = {
    isActive: boolean;
    isOvertime: boolean;
    currentSessionCompleted: boolean;
    progressPercent: number;
    elapsedFormatted: string;
    remainingFormatted: string;
    remainingLabelKey: string;
    labelKey: string;
    stateLabel: string | null;
    occurrenceKind: FastingOccurrenceKind | null;
    detailLabel: string | null;
    metaLabel: string | null;
    ringColor: string | null;
    glowColor: string | null;
    stageTitleKey: string | null;
    stageDescriptionKey: string | null;
    stageIndex: number | null;
    totalStages: number;
    nextStageTitleKey: string | null;
    nextStageFormatted: string | null;
    showGlow: boolean;
};

export type FastingTimerCardChrome = {
    density: 'compact' | 'default';
    title: string;
    showPageControls: boolean;
};
