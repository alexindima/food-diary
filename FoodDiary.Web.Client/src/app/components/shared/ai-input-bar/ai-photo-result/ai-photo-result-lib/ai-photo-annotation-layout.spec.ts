import { describe, expect, it } from 'vitest';

import { evaluateAiPhotoAnnotationLayout, optimizeAiPhotoAnnotationLayout } from './ai-photo-annotation-layout';
import type { AiPhotoAnnotation } from './ai-photo-result.types';

/* eslint-disable @typescript-eslint/no-magic-numbers -- Coordinates intentionally describe geometry test fixtures. */
const SCENARIOS: ReadonlyArray<ReadonlyArray<readonly [number, number]>> = [
    [
        [34, 24],
        [66, 24],
        [31, 50],
        [69, 50],
        [37, 76],
        [63, 76],
    ],
    [
        [42, 42],
        [50, 38],
        [58, 43],
        [43, 56],
        [51, 62],
        [60, 55],
    ],
    [
        [16, 48],
        [30, 52],
        [43, 47],
        [57, 53],
        [70, 48],
        [84, 52],
    ],
    [
        [48, 13],
        [52, 27],
        [47, 41],
        [53, 58],
        [48, 73],
        [52, 87],
    ],
    [
        [15, 16],
        [50, 19],
        [84, 15],
        [18, 82],
        [51, 76],
        [83, 84],
    ],
];
/* eslint-enable @typescript-eslint/no-magic-numbers -- End of geometry fixture coordinates. */
const SCENARIO_CASES = SCENARIOS.map(points => [points] as const);
const FRAME_SIZE = 100;

function createAnnotations(points: ReadonlyArray<readonly [number, number]>): AiPhotoAnnotation[] {
    return points.map(([centerX, centerY], index) => ({
        id: `food-${index}`,
        name: `Food ${index + 1}`,
        amountLabel: '100 g',
        centerX,
        centerY,
        cardX: 0,
        cardY: 0,
        cardWidth: 0,
        cardHeight: 0,
        connectorPoints: [
            { x: centerX, y: centerY },
            { x: centerX, y: centerY },
        ],
        connectorPath: '',
        calories: 100,
        protein: 10,
        fat: 5,
        carbs: 15,
    }));
}

describe('AI photo annotation layout', () => {
    it.each(SCENARIO_CASES)('keeps cards within the image and avoids card overlaps', points => {
        const layout = optimizeAiPhotoAnnotationLayout(createAnnotations(points));
        const metrics = evaluateAiPhotoAnnotationLayout(layout);

        expect(metrics.cardOverlaps).toBe(0);
        expect(metrics.coveredProducts).toBe(0);
        for (const annotation of layout) {
            expect(annotation.cardX).toBeGreaterThanOrEqual(0);
            expect(annotation.cardY).toBeGreaterThanOrEqual(0);
            expect(annotation.cardX + annotation.cardWidth).toBeLessThanOrEqual(FRAME_SIZE);
            expect(annotation.cardY + annotation.cardHeight).toBeLessThanOrEqual(FRAME_SIZE);
        }
    });

    it.each(SCENARIO_CASES)('avoids connector crossings for representative food arrangements', points => {
        const metrics = evaluateAiPhotoAnnotationLayout(optimizeAiPhotoAnnotationLayout(createAnnotations(points)));

        expect(metrics.connectorCrossings).toBe(0);
        expect(metrics.connectorCardIntersections).toBe(0);
    });

    it('produces the same result for the same product coordinates', () => {
        const annotations = createAnnotations(SCENARIOS[0]);

        expect(optimizeAiPhotoAnnotationLayout(annotations)).toEqual(optimizeAiPhotoAnnotationLayout(annotations));
    });

    it('returns an empty layout without search work', () => {
        expect(optimizeAiPhotoAnnotationLayout([])).toEqual([]);
    });
});
