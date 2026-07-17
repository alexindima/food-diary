import { describe, expect, it } from 'vitest';

import type { FastingTimerCardState } from './fasting-timer-card.types';
import {
    buildFastingMainRingItems,
    buildFastingSummaryContentGroups,
    buildFastingSummaryRingItems,
    type FastingTimerCardDisplayContext,
} from './fasting-timer-card-display.mapper';

const translate = (key: string, params?: Record<string, unknown>): string =>
    params === undefined ? key : `${key}:${JSON.stringify(params)}`;

describe('fasting timer card display mapper', () => {
    it('builds an idle setup state without a duplicated header', () => {
        const items = buildFastingMainRingItems(createContext({ isSetupLayout: true }));

        expect(items).toEqual([{ className: 'fasting-timer-card__remaining fd-ui-body-sm', text: 'FASTING.SELECT_AND_START' }]);
    });

    it('builds active stage and next-stage presentation', () => {
        const context = createContext({
            state: createState({
                isActive: true,
                stageTitleKey: 'FASTING.STAGES.KETOSIS',
                stageDescriptionKey: 'FASTING.STAGES.KETOSIS_DESCRIPTION',
                stageIndex: 2,
                totalStages: 4,
                nextStageTitleKey: 'FASTING.STAGES.AUTOPHAGY',
                nextStageFormatted: '02:00',
            }),
            showStageProgress: true,
        });

        const items = buildFastingMainRingItems(context);
        const groups = buildFastingSummaryContentGroups(context);

        expect(items.map(item => item.text)).toContain('FASTING.STAGES.KETOSIS');
        expect(items.map(item => item.text)).toContain('FASTING.STAGES.NEXT_IN:{"time":"02:00"}');
        expect(groups.some(group => group.className.includes('summary-stage'))).toBe(true);
        expect(groups.some(group => group.className.includes('summary-next'))).toBe(true);
    });

    it('suppresses stage progression for an eating phase', () => {
        const context = createContext({
            state: createState({ isActive: true, occurrenceKind: 'EatingWindow', stageTitleKey: 'STAGE', stageIndex: 1, totalStages: 3 }),
            isEatingPhase: true,
            showStageProgress: false,
        });

        expect(buildFastingMainRingItems(context).map(item => item.text)).not.toContain('STAGE');
    });

    it('formats completed and progress summary states', () => {
        const context = createContext({
            state: createState({ currentSessionCompleted: true, progressPercent: 100 }),
            normalizedProgressPercent: 100,
        });

        expect(buildFastingMainRingItems(context).map(item => item.text)).toContain('FASTING.COMPLETED');
        expect(buildFastingSummaryRingItems(context).map(item => item.text)).toContain('100%');
    });
});

function createContext(overrides: Partial<FastingTimerCardDisplayContext> = {}): FastingTimerCardDisplayContext {
    return {
        state: createState(),
        isSetupLayout: false,
        isEatingPhase: false,
        showStageProgress: false,
        showStageDescriptionFallback: false,
        normalizedProgressPercent: 0,
        translate,
        ...overrides,
    };
}

function createState(overrides: Partial<FastingTimerCardState> = {}): FastingTimerCardState {
    return {
        isActive: false,
        isOvertime: false,
        currentSessionCompleted: false,
        progressPercent: 0,
        elapsedFormatted: '00:00',
        remainingFormatted: '16:00',
        remainingLabelKey: 'FASTING.REMAINING',
        labelKey: 'FASTING.WIDGET_LABEL',
        stateLabel: null,
        occurrenceKind: null,
        detailLabel: null,
        metaLabel: null,
        ringColor: null,
        glowColor: null,
        stageTitleKey: null,
        stageDescriptionKey: null,
        stageIndex: null,
        totalStages: 0,
        nextStageTitleKey: null,
        nextStageFormatted: null,
        showGlow: false,
        ...overrides,
    };
}
