import { describe, expect, it } from 'vitest';

import {
    buildBmiPointerPosition,
    buildBmiSegments,
    buildBmiViewModel,
    calculateBmiValue,
    getBmiStatusInfo,
} from './weight-history-bmi.mapper';

const HEIGHT_CM = 180;
const WEIGHT_KG = 74.2;
const EXPECTED_BMI = 22.9;
const UNDERWEIGHT_BMI = 18;
const NORMAL_BMI = 22;
const OVERWEIGHT_BMI = 27;
const OBESE_BMI = 31;
const MID_SCALE_BMI = 20;
const ABOVE_SCALE_BMI = 100;
const BMI_SEGMENT_COUNT = 4;

describe('weight history BMI mapper', () => {
    it('calculates rounded BMI from height and latest weight', () => {
        expect(calculateBmiValue(HEIGHT_CM, WEIGHT_KG)).toBe(EXPECTED_BMI);
    });

    it('returns null when BMI data is missing or invalid', () => {
        expect(calculateBmiValue(null, WEIGHT_KG)).toBeNull();
        expect(calculateBmiValue(HEIGHT_CM, null)).toBeNull();
        expect(calculateBmiValue(0, WEIGHT_KG)).toBeNull();
        expect(calculateBmiValue(HEIGHT_CM, 0)).toBeNull();
    });

    it('builds status info by BMI range', () => {
        expect(getBmiStatusInfo(UNDERWEIGHT_BMI).labelKey).toBe('WEIGHT_HISTORY.BMI_STATUS.UNDERWEIGHT');
        expect(getBmiStatusInfo(NORMAL_BMI).labelKey).toBe('WEIGHT_HISTORY.BMI_STATUS.NORMAL');
        expect(getBmiStatusInfo(OVERWEIGHT_BMI).labelKey).toBe('WEIGHT_HISTORY.BMI_STATUS.OVER');
        expect(getBmiStatusInfo(OBESE_BMI).labelKey).toBe('WEIGHT_HISTORY.BMI_STATUS.OBESE');
    });

    it('builds clamped pointer position and segment widths', () => {
        expect(buildBmiPointerPosition(0)).toBe('1%');
        expect(buildBmiPointerPosition(MID_SCALE_BMI)).toBe('50%');
        expect(buildBmiPointerPosition(ABOVE_SCALE_BMI)).toBe('99%');
        expect(buildBmiSegments().map(segment => segment.width)).toEqual(['46.25%', '16.25%', '12.5%', '25%']);
    });

    it('builds a complete BMI view model', () => {
        const viewModel = buildBmiViewModel(HEIGHT_CM, WEIGHT_KG);

        expect(viewModel?.value).toBe(EXPECTED_BMI);
        expect(viewModel?.status.labelKey).toBe('WEIGHT_HISTORY.BMI_STATUS.NORMAL');
        expect(viewModel?.segments).toHaveLength(BMI_SEGMENT_COUNT);
        expect(viewModel?.pointerPosition).toBe('57.25%');
    });
});
