import { PERCENT_MULTIPLIER } from '../../../shared/lib/nutrition.constants';
import { WAIST_HISTORY_RATIO_ROUNDING_FACTOR } from './waist-history.constants';
import type { WhtSegmentViewModel, WhtStatusInfo, WhtViewModel } from './waist-history.types';

const WHT_SCALE_MAX = 0.8;
const POINTER_PADDING_PERCENT = 1;
const WHT_UNDER_MAX = 0.4;
const WHT_NORMAL_MAX = 0.5;
const WHT_ELEVATED_MAX = 0.6;
const WHT_PERCENT_ROUNDING_FACTOR = 1000;

const WHT_SEGMENT_DEFINITIONS: Array<Omit<WhtSegmentViewModel, 'width'>> = [
    { labelKey: 'WAIST_HISTORY.WHT_SEGMENTS.UNDER', from: 0, to: WHT_UNDER_MAX, class: 'waist-history-page__wht-segment--under' },
    {
        labelKey: 'WAIST_HISTORY.WHT_SEGMENTS.NORMAL',
        from: WHT_UNDER_MAX,
        to: WHT_NORMAL_MAX,
        class: 'waist-history-page__wht-segment--normal',
    },
    {
        labelKey: 'WAIST_HISTORY.WHT_SEGMENTS.ELEVATED',
        from: WHT_NORMAL_MAX,
        to: WHT_ELEVATED_MAX,
        class: 'waist-history-page__wht-segment--elevated',
    },
    {
        labelKey: 'WAIST_HISTORY.WHT_SEGMENTS.HIGH',
        from: WHT_ELEVATED_MAX,
        to: WHT_SCALE_MAX,
        class: 'waist-history-page__wht-segment--high',
    },
];

export function buildWhtViewModel(heightCm: number | null, waistCm: number | null): WhtViewModel | null {
    const value = calculateWhtValue(heightCm, waistCm);
    if (value === null) {
        return null;
    }

    return {
        value,
        status: getWhtStatusInfo(value),
        segments: buildWhtSegments(),
        pointerPosition: buildWhtPointerPosition(value),
    };
}

export function calculateWhtValue(heightCm: number | null, waistCm: number | null): number | null {
    if (heightCm === null || waistCm === null || heightCm <= 0 || waistCm <= 0) {
        return null;
    }

    const ratio = waistCm / heightCm;
    return Math.round(ratio * WAIST_HISTORY_RATIO_ROUNDING_FACTOR) / WAIST_HISTORY_RATIO_ROUNDING_FACTOR;
}

export function buildWhtSegments(): WhtSegmentViewModel[] {
    return WHT_SEGMENT_DEFINITIONS.map(segment => ({
        ...segment,
        width: formatWhtPercent(((segment.to - segment.from) / WHT_SCALE_MAX) * PERCENT_MULTIPLIER),
    }));
}

export function buildWhtPointerPosition(value: number): string {
    const rawPercent = (value / WHT_SCALE_MAX) * PERCENT_MULTIPLIER;
    const clamped = Math.max(POINTER_PADDING_PERCENT, Math.min(PERCENT_MULTIPLIER - POINTER_PADDING_PERCENT, rawPercent));
    return formatWhtPercent(clamped);
}

export function getWhtStatusInfo(value: number): WhtStatusInfo {
    if (value < WHT_UNDER_MAX) {
        return {
            labelKey: 'WAIST_HISTORY.WHT_STATUS.UNDER',
            descriptionKey: 'WAIST_HISTORY.WHT_STATUS_DESC.UNDER',
            class: 'waist-history-page__wht-status--under',
        };
    }

    if (value < WHT_NORMAL_MAX) {
        return {
            labelKey: 'WAIST_HISTORY.WHT_STATUS.NORMAL',
            descriptionKey: 'WAIST_HISTORY.WHT_STATUS_DESC.NORMAL',
            class: 'waist-history-page__wht-status--normal',
        };
    }

    if (value < WHT_ELEVATED_MAX) {
        return {
            labelKey: 'WAIST_HISTORY.WHT_STATUS.ELEVATED',
            descriptionKey: 'WAIST_HISTORY.WHT_STATUS_DESC.ELEVATED',
            class: 'waist-history-page__wht-status--elevated',
        };
    }

    return {
        labelKey: 'WAIST_HISTORY.WHT_STATUS.HIGH',
        descriptionKey: 'WAIST_HISTORY.WHT_STATUS_DESC.HIGH',
        class: 'waist-history-page__wht-status--high',
    };
}

function formatWhtPercent(value: number): string {
    return `${Math.round(value * WHT_PERCENT_ROUNDING_FACTOR) / WHT_PERCENT_ROUNDING_FACTOR}%`;
}
