import { describe, expect, it } from 'vitest';

import {
    calculateMaxPosition,
    calculateProgressBarWidth,
    calculateProgressColor,
    calculateProgressPercent,
    calculateTextPosition,
    resolveProgressTextColorClass,
} from './dynamic-progress-bar.utils';

const MAX_PERCENT = 100;
const HALF_PERCENT = 50;
const BELOW_HALF_PERCENT = 49;
const LOW_PROGRESS = 20;
const LOW_TEXT_POSITION = '60%';
const NORMAL_PROGRESS = 80;
const NORMAL_TEXT_POSITION = '40%';
const OVERFLOW_PROGRESS = 150;
const WARNING_PROGRESS = 112;
const DANGER_PROGRESS = 175;
const OVERFLOW_MAX_POSITION = 66.666;
const OVERFLOW_TEXT_POSITION = 33.33;
const ONE_THIRD_CURRENT = 333;
const ONE_THIRD_MAX = 1000;
const ONE_THIRD_PROGRESS = 33;
const QUARTER_PROGRESS = 25;
const DOUBLE_CURRENT = 200;

describe('dynamic progress bar utils', () => {
    it('calculates rounded progress and handles invalid max', () => {
        expect(calculateProgressPercent(ONE_THIRD_CURRENT, ONE_THIRD_MAX)).toBe(ONE_THIRD_PROGRESS);
        expect(calculateProgressPercent(MAX_PERCENT, 0)).toBe(0);
    });

    it('clamps visual width but keeps overflow progress available', () => {
        expect(calculateProgressBarWidth(OVERFLOW_PROGRESS)).toBe('100%');
        expect(calculateProgressBarWidth(QUARTER_PROGRESS)).toBe('25%');
    });

    it('calculates max marker position for overflow', () => {
        expect(calculateMaxPosition(DOUBLE_CURRENT, MAX_PERCENT)).toBe(HALF_PERCENT);
        expect(calculateMaxPosition(HALF_PERCENT, MAX_PERCENT)).toBe(MAX_PERCENT);
    });

    it('calculates text position for low, normal, and overflow progress', () => {
        expect(calculateTextPosition(LOW_PROGRESS, LOW_PROGRESS, MAX_PERCENT, MAX_PERCENT)).toBe(LOW_TEXT_POSITION);
        expect(calculateTextPosition(NORMAL_PROGRESS, NORMAL_PROGRESS, MAX_PERCENT, MAX_PERCENT)).toBe(NORMAL_TEXT_POSITION);
        expect(parseFloat(calculateTextPosition(OVERFLOW_PROGRESS, OVERFLOW_PROGRESS, MAX_PERCENT, OVERFLOW_MAX_POSITION))).toBeCloseTo(
            OVERFLOW_TEXT_POSITION,
            1,
        );
    });

    it('resolves progress colors across normal, warning, and danger ranges', () => {
        expect(calculateProgressColor(0)).toBe('#509650');
        expect(calculateProgressColor(MAX_PERCENT)).toBe('#50fa50');
        expect(calculateProgressColor(WARNING_PROGRESS)).toBe('#ff9850');
        expect(calculateProgressColor(DANGER_PROGRESS)).toBe('#ff0000');
    });

    it('uses dark text before half progress', () => {
        expect(resolveProgressTextColorClass(BELOW_HALF_PERCENT)).toBe('text-black');
        expect(resolveProgressTextColorClass(HALF_PERCENT)).toBe('text-white');
    });
});
