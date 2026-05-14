import { describe, expect, it } from 'vitest';

import {
    buildWhtPointerPosition,
    buildWhtSegments,
    buildWhtViewModel,
    calculateWhtValue,
    getWhtStatusInfo,
} from './waist-history-wht.mapper';

const HEIGHT_CM = 180;
const WAIST_CM = 82;
const EXPECTED_WHT = 0.46;
const UNDER_WHT = 0.35;
const NORMAL_WHT = 0.46;
const ELEVATED_WHT = 0.55;
const HIGH_WHT = 0.65;
const MID_SCALE_WHT = 0.4;
const ABOVE_SCALE_WHT = 10;
const WHT_SEGMENT_COUNT = 4;

describe('waist history WHT mapper', () => {
    it('calculates rounded waist-to-height ratio from height and latest waist', () => {
        expect(calculateWhtValue(HEIGHT_CM, WAIST_CM)).toBe(EXPECTED_WHT);
    });

    it('returns null when WHT data is missing or invalid', () => {
        expect(calculateWhtValue(null, WAIST_CM)).toBeNull();
        expect(calculateWhtValue(HEIGHT_CM, null)).toBeNull();
        expect(calculateWhtValue(0, WAIST_CM)).toBeNull();
        expect(calculateWhtValue(HEIGHT_CM, 0)).toBeNull();
    });

    it('builds status info by WHT range', () => {
        expect(getWhtStatusInfo(UNDER_WHT).labelKey).toBe('WAIST_HISTORY.WHT_STATUS.UNDER');
        expect(getWhtStatusInfo(NORMAL_WHT).labelKey).toBe('WAIST_HISTORY.WHT_STATUS.NORMAL');
        expect(getWhtStatusInfo(ELEVATED_WHT).labelKey).toBe('WAIST_HISTORY.WHT_STATUS.ELEVATED');
        expect(getWhtStatusInfo(HIGH_WHT).labelKey).toBe('WAIST_HISTORY.WHT_STATUS.HIGH');
    });

    it('builds clamped pointer position and segment widths', () => {
        expect(buildWhtPointerPosition(0)).toBe('1%');
        expect(buildWhtPointerPosition(MID_SCALE_WHT)).toBe('50%');
        expect(buildWhtPointerPosition(ABOVE_SCALE_WHT)).toBe('99%');
        expect(buildWhtSegments().map(segment => segment.width)).toEqual(['50%', '12.5%', '12.5%', '25%']);
    });

    it('builds a complete WHT view model', () => {
        const viewModel = buildWhtViewModel(HEIGHT_CM, WAIST_CM);

        expect(viewModel?.value).toBe(EXPECTED_WHT);
        expect(viewModel?.status.labelKey).toBe('WAIST_HISTORY.WHT_STATUS.NORMAL');
        expect(viewModel?.segments).toHaveLength(WHT_SEGMENT_COUNT);
        expect(viewModel?.pointerPosition).toBe('57.5%');
    });
});
