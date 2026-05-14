import { CENTIMETERS_PER_METER } from '../../../shared/lib/body-measurement.constants';
import { PERCENT_MULTIPLIER } from '../../../shared/lib/nutrition.constants';
import { WEIGHT_HISTORY_VALUE_ROUNDING_FACTOR } from './weight-history.constants';
import type { BmiSegmentViewModel, BmiStatusInfo, BmiViewModel } from './weight-history.types';

const BMI_SCALE_MAX = 40;
const BMI_POINTER_PADDING_PERCENT = 1;
const BMI_UNDERWEIGHT_MAX = 18.5;
const BMI_NORMAL_MAX = 25;
const BMI_OVERWEIGHT_MAX = 30;

const BMI_SEGMENT_DEFINITIONS: Array<Omit<BmiSegmentViewModel, 'width'>> = [
    { labelKey: 'WEIGHT_HISTORY.BMI_SEGMENTS.UNDER', from: 0, to: 18.5, class: 'weight-history-page__bmi-segment--under' },
    { labelKey: 'WEIGHT_HISTORY.BMI_SEGMENTS.NORMAL', from: 18.5, to: 25, class: 'weight-history-page__bmi-segment--normal' },
    { labelKey: 'WEIGHT_HISTORY.BMI_SEGMENTS.OVER', from: 25, to: 30, class: 'weight-history-page__bmi-segment--over' },
    { labelKey: 'WEIGHT_HISTORY.BMI_SEGMENTS.OBESE', from: 30, to: BMI_SCALE_MAX, class: 'weight-history-page__bmi-segment--obese' },
];

export function buildBmiViewModel(heightCm: number | null, latestWeight: number | null): BmiViewModel | null {
    const value = calculateBmiValue(heightCm, latestWeight);
    if (value === null) {
        return null;
    }

    return {
        value,
        status: getBmiStatusInfo(value),
        segments: buildBmiSegments(),
        pointerPosition: buildBmiPointerPosition(value),
    };
}

export function calculateBmiValue(heightCm: number | null, latestWeight: number | null): number | null {
    if (heightCm === null || latestWeight === null || heightCm <= 0 || latestWeight <= 0) {
        return null;
    }

    const heightMeters = heightCm / CENTIMETERS_PER_METER;
    const bmi = latestWeight / (heightMeters * heightMeters);
    return Math.round(bmi * WEIGHT_HISTORY_VALUE_ROUNDING_FACTOR) / WEIGHT_HISTORY_VALUE_ROUNDING_FACTOR;
}

export function buildBmiSegments(): BmiSegmentViewModel[] {
    return BMI_SEGMENT_DEFINITIONS.map(segment => ({
        ...segment,
        width: `${((segment.to - segment.from) / BMI_SCALE_MAX) * PERCENT_MULTIPLIER}%`,
    }));
}

export function buildBmiPointerPosition(bmi: number): string {
    const rawPercent = (bmi / BMI_SCALE_MAX) * PERCENT_MULTIPLIER;
    const percent = Math.max(BMI_POINTER_PADDING_PERCENT, Math.min(PERCENT_MULTIPLIER - BMI_POINTER_PADDING_PERCENT, rawPercent));
    return `${percent}%`;
}

export function getBmiStatusInfo(bmi: number): BmiStatusInfo {
    if (bmi < BMI_UNDERWEIGHT_MAX) {
        return {
            labelKey: 'WEIGHT_HISTORY.BMI_STATUS.UNDERWEIGHT',
            descriptionKey: 'WEIGHT_HISTORY.BMI_STATUS_DESC.UNDERWEIGHT',
            class: 'weight-history-page__bmi-status--under',
        };
    }

    if (bmi < BMI_NORMAL_MAX) {
        return {
            labelKey: 'WEIGHT_HISTORY.BMI_STATUS.NORMAL',
            descriptionKey: 'WEIGHT_HISTORY.BMI_STATUS_DESC.NORMAL',
            class: 'weight-history-page__bmi-status--normal',
        };
    }

    if (bmi < BMI_OVERWEIGHT_MAX) {
        return {
            labelKey: 'WEIGHT_HISTORY.BMI_STATUS.OVER',
            descriptionKey: 'WEIGHT_HISTORY.BMI_STATUS_DESC.OVER',
            class: 'weight-history-page__bmi-status--over',
        };
    }

    return {
        labelKey: 'WEIGHT_HISTORY.BMI_STATUS.OBESE',
        descriptionKey: 'WEIGHT_HISTORY.BMI_STATUS_DESC.OBESE',
        class: 'weight-history-page__bmi-status--obese',
    };
}
