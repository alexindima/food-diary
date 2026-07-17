import type { FastingTimerCardDisplayGroup, FastingTimerCardDisplayItem, FastingTimerCardState } from './fasting-timer-card.types';

export type FastingTimerCardDisplayContext = {
    state: FastingTimerCardState;
    isSetupLayout: boolean;
    isEatingPhase: boolean;
    showStageProgress: boolean;
    showStageDescriptionFallback: boolean;
    normalizedProgressPercent: number;
    translate: (key: string, params?: Record<string, unknown>) => string;
};

export function buildFastingMainRingItems(context: FastingTimerCardDisplayContext): FastingTimerCardDisplayItem[] {
    const { state, translate } = context;
    const items: FastingTimerCardDisplayItem[] = [];
    const showHeader = !(context.isSetupLayout && !state.isActive && !state.currentSessionCompleted);
    if (showHeader) {
        items.push({ className: 'fasting-timer-card__label fd-ui-meta-text', text: translate(state.labelKey) });
        pushOptionalItem(items, 'fasting-timer-card__state fd-ui-meta-text', state.stateLabel);
    }

    if (state.isActive) {
        addActiveItems(items, context);
    } else if (state.currentSessionCompleted) {
        addCompletedItems(items, context);
    } else if (context.isSetupLayout) {
        items.push({ className: 'fasting-timer-card__remaining fd-ui-body-sm', text: translate('FASTING.SELECT_AND_START') });
    } else {
        items.push({
            className: 'fasting-timer-card__elapsed fasting-timer-card__elapsed--idle fd-ui-metric-lg',
            text: translate('FASTING.READY'),
        });
        items.push({ className: 'fasting-timer-card__remaining fd-ui-body-sm', text: translate('FASTING.SELECT_AND_START') });
    }

    if (showHeader) {
        pushOptionalItem(items, 'fasting-timer-card__detail fd-ui-caption', state.detailLabel);
    }
    return items;
}

export function buildFastingSummaryRingItems(context: FastingTimerCardDisplayContext): FastingTimerCardDisplayItem[] {
    const { state, translate } = context;
    if (!state.isActive && !state.currentSessionCompleted) {
        return [
            {
                className: 'fasting-timer-card__elapsed fasting-timer-card__elapsed--idle fd-ui-metric-lg',
                text: translate('FASTING.READY'),
            },
            { className: 'fasting-timer-card__remaining fd-ui-body-sm', text: translate('FASTING.SELECT_AND_START') },
        ];
    }

    const items: FastingTimerCardDisplayItem[] = [];
    pushOptionalItem(items, 'fasting-timer-card__state fasting-timer-card__state--summary fd-ui-body-sm', state.stateLabel);
    items.push({
        className: 'fasting-timer-card__elapsed fasting-timer-card__elapsed--summary fd-ui-metric-lg',
        text: state.elapsedFormatted,
    });
    items.push({
        className: 'fasting-timer-card__percent fasting-timer-card__percent--summary-secondary fd-ui-stat-value',
        text: `${context.normalizedProgressPercent.toFixed(0)}%`,
    });
    return items;
}

export function buildFastingSummaryContentGroups(context: FastingTimerCardDisplayContext): FastingTimerCardDisplayGroup[] {
    if (context.state.isActive) {
        return buildActiveSummaryGroups(context);
    }
    if (context.state.currentSessionCompleted) {
        return buildCompletedSummaryGroups(context);
    }
    return [
        {
            className: 'fasting-timer-card__summary-time fasting-timer-card__summary-time--idle fd-stack fd-gap-card-header',
            items: [
                { className: 'fd-ui-metric-lg', text: context.translate('FASTING.READY') },
                { className: 'fasting-timer-card__remaining fd-ui-body-sm', text: context.translate('FASTING.SELECT_AND_START') },
            ],
        },
    ];
}

function addActiveItems(items: FastingTimerCardDisplayItem[], context: FastingTimerCardDisplayContext): void {
    const { state, translate } = context;
    items.push({ className: 'fasting-timer-card__elapsed fd-ui-metric-lg', text: state.elapsedFormatted });
    addStageItems(items, context);
    items.push({ className: 'fasting-timer-card__remaining fd-ui-body-sm', text: getRemainingText(context) });
    if (!context.isEatingPhase && !state.isOvertime && state.nextStageTitleKey !== null && state.nextStageFormatted !== null) {
        items.push({
            className: 'fasting-timer-card__next-stage-time fd-ui-body-sm',
            text: translate('FASTING.STAGES.NEXT_IN', { time: state.nextStageFormatted }),
        });
        items.push({
            className: 'fasting-timer-card__next-stage-label',
            text: `${translate('FASTING.STAGES.NEXT_STAGE')}: ${translate(state.nextStageTitleKey)}`,
        });
    } else if (context.showStageDescriptionFallback) {
        pushOptionalTranslatedItem(items, 'fasting-timer-card__next-stage-label', state.stageDescriptionKey, translate);
    }
}

function addCompletedItems(items: FastingTimerCardDisplayItem[], context: FastingTimerCardDisplayContext): void {
    items.push({ className: 'fasting-timer-card__elapsed fd-ui-metric-lg', text: context.state.elapsedFormatted });
    addStageItems(items, context);
    items.push({
        className: 'fasting-timer-card__remaining fasting-timer-card__remaining--done fd-ui-body-sm',
        text: context.translate('FASTING.COMPLETED'),
    });
    pushOptionalTranslatedItem(items, 'fasting-timer-card__next-stage-label', context.state.stageDescriptionKey, context.translate);
}

function buildActiveSummaryGroups(context: FastingTimerCardDisplayContext): FastingTimerCardDisplayGroup[] {
    const { state, translate } = context;
    const groups: FastingTimerCardDisplayGroup[] = [];
    if (context.showStageProgress) {
        const stageItems: FastingTimerCardDisplayItem[] = [];
        if (state.metaLabel === null) {
            stageItems.push({ className: 'fasting-timer-card__stage-progress fd-ui-overline', text: getStageProgressText(context) });
        }
        pushOptionalTranslatedItem(stageItems, 'fasting-timer-card__stage-title fd-ui-card-title', state.stageTitleKey, translate);
        pushOptionalTranslatedItem(stageItems, 'fasting-timer-card__next-stage-label', state.stageDescriptionKey, translate);
        groups.push({ className: 'fasting-timer-card__summary-stage fd-stack fd-gap-card-header', items: stageItems });
    }

    if (!context.isEatingPhase && !state.isOvertime && state.nextStageTitleKey !== null && state.nextStageFormatted !== null) {
        groups.push({
            className: 'fasting-timer-card__summary-next fd-stack fd-gap-card-header',
            items: [
                {
                    className: 'fasting-timer-card__next-stage-time fd-ui-body-sm',
                    text: translate('FASTING.STAGES.NEXT_IN', { time: state.nextStageFormatted }),
                },
            ],
        });
    }
    groups.push({
        className: 'fasting-timer-card__summary-meta fd-row fd-gap-card-header',
        items: [
            {
                className: state.isOvertime
                    ? 'fasting-timer-card__remaining fasting-timer-card__remaining--done fd-ui-body-sm'
                    : 'fasting-timer-card__remaining fd-ui-body-sm',
                text: getRemainingText(context),
            },
        ],
    });
    if (context.showStageDescriptionFallback && state.stageDescriptionKey !== null) {
        groups.push({
            className: 'fasting-timer-card__summary-description fd-stack fd-gap-card-header',
            items: [{ className: 'fasting-timer-card__next-stage-label', text: translate(state.stageDescriptionKey) }],
        });
    }
    return groups;
}

function buildCompletedSummaryGroups(context: FastingTimerCardDisplayContext): FastingTimerCardDisplayGroup[] {
    const groups: FastingTimerCardDisplayGroup[] = [];
    if (context.showStageProgress) {
        const stageItems: FastingTimerCardDisplayItem[] = [];
        addStageItems(stageItems, context);
        groups.push({ className: 'fasting-timer-card__summary-stage fd-stack fd-gap-card-header', items: stageItems });
    }
    groups.push({
        className: 'fasting-timer-card__summary-completed fd-stack fd-gap-card-header',
        items: [
            {
                className: 'fasting-timer-card__remaining fasting-timer-card__remaining--done fd-ui-body-sm',
                text: context.translate('FASTING.COMPLETED'),
            },
            ...buildOptionalTranslatedItems('fasting-timer-card__next-stage-label', context.state.stageDescriptionKey, context.translate),
        ],
    });
    return groups;
}

function addStageItems(items: FastingTimerCardDisplayItem[], context: FastingTimerCardDisplayContext): void {
    if (!context.showStageProgress) {
        return;
    }
    items.push({ className: 'fasting-timer-card__stage-progress fd-ui-overline', text: getStageProgressText(context) });
    pushOptionalTranslatedItem(items, 'fasting-timer-card__stage-title fd-ui-card-title', context.state.stageTitleKey, context.translate);
}

function getRemainingText(context: FastingTimerCardDisplayContext): string {
    return context.state.isOvertime
        ? context.translate('FASTING.GOAL_REACHED')
        : `${context.translate(context.state.remainingLabelKey)}: ${context.state.remainingFormatted}`;
}

function getStageProgressText(context: FastingTimerCardDisplayContext): string {
    return context.translate('FASTING.STAGES.PROGRESS', {
        current: context.state.stageIndex,
        total: context.state.totalStages,
    });
}

function pushOptionalItem(items: FastingTimerCardDisplayItem[], className: string, text: string | null): void {
    if (text !== null) {
        items.push({ className, text });
    }
}

function pushOptionalTranslatedItem(
    items: FastingTimerCardDisplayItem[],
    className: string,
    translationKey: string | null,
    translate: FastingTimerCardDisplayContext['translate'],
): void {
    items.push(...buildOptionalTranslatedItems(className, translationKey, translate));
}

function buildOptionalTranslatedItems(
    className: string,
    translationKey: string | null,
    translate: FastingTimerCardDisplayContext['translate'],
): FastingTimerCardDisplayItem[] {
    return translationKey !== null ? [{ className, text: translate(translationKey) }] : [];
}
