/* eslint-disable @typescript-eslint/no-magic-numbers -- Geometry fixtures verify fixed radii, interpolation and visual overflow bounds. */
import { describe, expect, it } from 'vitest';

import { buildDayNutrientContour, getDayNutrientAnchor } from './day-nutrition-contour.utils';

const ids = ['protein', 'fats', 'fiber', 'carbs'];
const progressPairs = [
    [100, 100],
    [100, 90],
    [100, 50],
    [100, 20],
];
const bars = (values: number[]): Array<{ id: string; percent: number }> => ids.map((id, index) => ({ id, percent: values[index] }));
function radii(path: string): number[] {
    return [...path.matchAll(/(-?\d+\.\d+) (-?\d+\.\d+)/g)].map(match => Math.hypot(Number(match[1]) - 160, Number(match[2]) - 160));
}

describe('day nutrient contour', () => {
    it('keeps marker positions on their contour anchors for empty, partial and overflow data', () => {
        const input = bars([0, 46, 125, 113]);
        const contour = radii(buildDayNutrientContour(input));
        input.forEach((bar, index) => {
            const anchor = getDayNutrientAnchor(bar.id, bar.percent);
            expect(anchor).not.toBeNull();
            if (anchor !== null) {
                expect(Math.hypot(anchor.x - 160, anchor.y - 160)).toBeCloseTo(contour[index * 40], 1);
            }
        });
        expect(getDayNutrientAnchor('unknown', 100)).toBeNull();
    });
    it('forms a circle at the goal when all nutrients reach 100%', () => {
        const result = radii(buildDayNutrientContour(bars([100, 100, 100, 100])));
        for (const index of [0, 40, 80, 120]) {
            expect(result[index]).toBeCloseTo(140, 1);
            expect(result[index + 20]).toBeCloseTo(140, 1);
        }
        expect(result.slice(0, 160).every(radius => radius > 94 && radius <= 140.01)).toBe(true);
        expect(result.slice(160).every(radius => Math.abs(radius - 94) < 0.01)).toBe(true);
    });

    it('deepens valleys as neighbouring progress diverges, including excess', () => {
        const valleys = progressPairs.map(([first, second]) => radii(buildDayNutrientContour(bars([first, second, second, first])))[20]);
        valleys.slice(1).forEach((radius, index) => {
            expect(radius).toBeLessThan(valleys[index]);
        });
        const moderate = radii(buildDayNutrientContour(bars([100, 50, 50, 100])))[20];
        const extreme = radii(buildDayNutrientContour(bars([300, 50, 50, 300])))[20];
        expect(extreme).toBeLessThan(moderate);
    });

    it('keeps a pronounced valley between 500% and 1000% despite compressed radii', () => {
        const result = radii(buildDayNutrientContour(bars([1000, 500, 1000, 300])));
        const smallerPeak = result[40];
        const valley = result[20];
        expect(smallerPeak - valley).toBeGreaterThan(30);
        expect(valley).toBeGreaterThan(94);
    });

    it('has no filled area for empty or invalid data', () => {
        const result = radii(buildDayNutrientContour([]));
        expect(result.slice(0, 160)).toEqual(result.slice(160));
        expect(buildDayNutrientContour(bars([NaN, -10, Infinity, 0]))).toBe(buildDayNutrientContour([]));
    });

    it('places each nonzero nutrient marker at a local peak even with unequal neighbours', () => {
        const result = radii(buildDayNutrientContour(bars([50, 1000, 300, 100]))).slice(0, 160);
        for (const index of [0, 40, 80, 120]) {
            expect(result[index]).toBeGreaterThan(result[(index + 159) % 160]);
            expect(result[index]).toBeGreaterThan(result[(index + 1) % 160]);
        }
    });

    it('preserves exact values at fixed directions and interpolates without overshooting', () => {
        const result = radii(buildDayNutrientContour(bars([0, 100, 50, 125])));
        expect(result[0]).toBeCloseTo(94, 1);
        expect(result[40]).toBeCloseTo(140, 1);
        expect(result[80]).toBeCloseTo(117, 1);
        expect(result[120]).toBeCloseTo(140 + 46 * Math.log10(1.25), 1);
        expect(result.slice(40, 81).every(radius => radius > 94 && radius <= 140.01)).toBe(true);
        expect(Math.abs(result[159] - result[0])).toBeLessThan(0.2);
    });

    it('keeps source order irrelevant and caps extreme overflow inside the canvas', () => {
        const input = bars([50, 60, 125, 110]);
        expect(buildDayNutrientContour([...input].reverse())).toBe(buildDayNutrientContour(input));
        expect(buildDayNutrientContour(bars([10000, 10000, 10000, 10000]))).toBe(buildDayNutrientContour(bars([1000, 1000, 1000, 1000])));
    });

    it('distinguishes moderate and extreme excess while bounding every contour sample', () => {
        const values = [100, 125, 150, 200, 300, 500, 1000];
        const distances = values.map(value => radii(buildDayNutrientContour(bars([value, 0, 50, 100])))[0]);
        distances.slice(1).forEach((radius, index) => {
            expect(radius).toBeGreaterThan(distances[index]);
        });
        for (const input of [
            [500, 0, 0, 0],
            [1000, 1000, 1000, 1000],
            [50, 300, 500, 1e9],
        ]) {
            expect(radii(buildDayNutrientContour(bars(input))).every(radius => radius >= 81.99 && radius <= 198.01)).toBe(true);
        }
    });
});
