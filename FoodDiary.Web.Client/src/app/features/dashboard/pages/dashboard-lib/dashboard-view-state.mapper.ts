import type {
    DashboardBlockId,
    DashboardBlockState,
    DashboardBlockStateOptions,
    DashboardHeaderState,
    DashboardMealsPreviewState,
} from './dashboard-view.types';

const EMPTY_INDEX = -1;

export function buildDashboardHeaderState(isToday: boolean, selectedDateLabel: string): DashboardHeaderState {
    return {
        fullTitleKey: isToday ? 'DASHBOARD.TITLE' : 'DASHBOARD.TITLE_FOR_DATE',
        compactTitleKey: isToday ? 'DASHBOARD.TITLE_SHORT' : 'DASHBOARD.TITLE_FOR_DATE_SHORT',
        titleParams: isToday ? null : { date: selectedDateLabel },
        selectedDateLabel,
    };
}

export function buildDashboardMealsPreviewState(isToday: boolean, titleForDate: string | null): DashboardMealsPreviewState {
    return {
        titleText: isToday ? null : titleForDate,
        emptyKey: isToday ? 'DASHBOARD.MEALS_EMPTY' : 'DASHBOARD.MEALS_EMPTY_FOR_DATE',
        showDateActions: isToday,
        showEmptyState: !isToday,
    };
}

export function buildDashboardBlockState(options: {
    blockId: DashboardBlockId;
    editing: boolean;
    isVisible: boolean;
    canToggle: boolean;
    ariaLabel: string | null;
    stateOptions?: DashboardBlockStateOptions;
}): DashboardBlockState {
    const stateOptions = options.stateOptions ?? {};
    const isInteractive = options.editing || stateOptions.alwaysInteractive === true;

    return {
        hidden: options.editing && !options.isVisible,
        role: isInteractive ? 'button' : null,
        tabIndex: isInteractive ? 0 : EMPTY_INDEX,
        ariaPressed: options.editing ? options.isVisible : null,
        ariaDisabled: options.editing && stateOptions.locked === true ? !options.canToggle : null,
        ariaLabel: options.ariaLabel,
        inert: options.editing ? '' : null,
    };
}

export function isDashboardAsideBlock(blockId: string): boolean {
    return (
        blockId === 'hydration' ||
        blockId === 'cycle' ||
        blockId === 'weight' ||
        blockId === 'waist' ||
        blockId === 'tdee' ||
        blockId === 'advice'
    );
}
