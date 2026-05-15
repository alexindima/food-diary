import type { FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select.component';

import { PERCENT_MULTIPLIER } from '../../../../shared/lib/nutrition.constants';
import type { BodyTargetKey, MacroPreset, MacroPresetKey } from '../../lib/goals.facade';
import type { DayCalorieKey } from '../../models/goals.data';
import type { BodyTarget, CyclingDayControl, PointerCoordinates, RingRect } from './goals-page.models';

export const GOALS_RING_THICKNESS_PX = 30;

const HALF_CIRCLE_DEGREES = 180;
const RING_START_ANGLE_OFFSET_DEGREES = 450;
const FULL_CIRCLE_DEGREES = 360;

type BodyTargetDefinition = Omit<BodyTarget, 'value'>;

const GOALS_BODY_TARGET_DEFINITIONS: BodyTargetDefinition[] = [
    {
        key: 'weight',
        titleKey: 'GOALS_PAGE.BODY_TARGET_WEIGHT',
        unit: 'kg',
        current: null,
        delta: null,
    },
    {
        key: 'waist',
        titleKey: 'GOALS_PAGE.BODY_TARGET_WAIST',
        unit: 'cm',
        current: null,
        delta: null,
    },
];

export function buildBodyTargets(values: Record<BodyTargetKey, number>): BodyTarget[] {
    return GOALS_BODY_TARGET_DEFINITIONS.map(target => ({
        ...target,
        value: values[target.key],
    }));
}

export function buildCyclingDayControls(days: ReadonlyArray<{ key: DayCalorieKey; labelKey: string }>): CyclingDayControl[] {
    return days.map(day => ({
        ...day,
        inputId: `cycling-day-${day.key}`,
    }));
}

export function buildMacroPresetOptions(
    presets: MacroPreset[],
    translate: (key: string) => string,
): Array<FdUiSelectOption<MacroPresetKey>> {
    return presets.map(preset => ({
        value: preset.key,
        label: translate(preset.labelKey),
    }));
}

export function withMacroProgressStyles<T extends { percent: number }>(state: T): T & { progressOffset: string; progressRatio: number } {
    return {
        ...state,
        progressOffset: `${state.percent}%`,
        progressRatio: state.percent / PERCENT_MULTIPLIER,
    };
}

export function calculateGoalRingDistances(
    point: PointerCoordinates,
    rect: RingRect,
    ringThicknessPx = GOALS_RING_THICKNESS_PX,
): {
    centerX: number;
    centerY: number;
    distanceFromCenter: number;
    innerRadius: number;
    outerRadius: number;
} {
    const centerX = rect.left + rect.width / 2;
    const centerY = rect.top + rect.height / 2;
    const distanceFromCenter = Math.hypot(point.clientX - centerX, point.clientY - centerY);
    const outerRadius = Math.min(rect.width, rect.height) / 2;
    const innerRadius = outerRadius - ringThicknessPx;
    return { centerX, centerY, distanceFromCenter, innerRadius, outerRadius };
}

export function isPointInsideGoalRing(point: PointerCoordinates, rect: RingRect, ringThicknessPx = GOALS_RING_THICKNESS_PX): boolean {
    const { distanceFromCenter, innerRadius, outerRadius } = calculateGoalRingDistances(point, rect, ringThicknessPx);
    return distanceFromCenter >= innerRadius && distanceFromCenter <= outerRadius;
}

export function calculateCaloriesFromRingPointer(
    point: PointerCoordinates,
    rect: RingRect,
    minCalories: number,
    maxCalories: number,
): number {
    const { centerX, centerY } = calculateGoalRingDistances(point, rect);
    const dx = point.clientX - centerX;
    const dy = point.clientY - centerY;
    const radians = Math.atan2(dy, dx);
    const degrees = (radians * HALF_CIRCLE_DEGREES) / Math.PI;
    const normalized = (degrees + RING_START_ANGLE_OFFSET_DEGREES) % FULL_CIRCLE_DEGREES;
    const ratio = normalized / FULL_CIRCLE_DEGREES;
    return Math.round(minCalories + ratio * (maxCalories - minCalories));
}
