import { describe, expect, it } from 'vitest';

import { findOverlappingXAxisLabels } from './fd-ui-line-chart-x-axis-layout';

const CLEARANCE = 4;
const CROWDED_LABEL_INDEX = 12;

describe('line chart x-axis label collisions', () => {
    it('keeps the final date readable when the preceding label overlaps it', () => {
        const hidden = findOverlappingXAxisLabels(
            [
                { index: 0, left: 0, right: 36 },
                { index: CROWDED_LABEL_INDEX, left: 410, right: 447 },
                { index: 13, left: 430, right: 465 },
            ],
            CLEARANCE,
        );

        expect([...hidden]).toEqual([CROWDED_LABEL_INDEX]);
    });

    it('restores a label when the resized plot has enough room', () => {
        const hidden = findOverlappingXAxisLabels(
            [
                { index: 0, left: 0, right: 36 },
                { index: 12, left: 410, right: 447 },
                { index: 13, left: 460, right: 495 },
            ],
            CLEARANCE,
        );

        expect([...hidden]).toEqual([]);
    });

    it('also suppresses collisions between interior labels', () => {
        const hidden = findOverlappingXAxisLabels(
            [
                { index: 0, left: 0, right: 36 },
                { index: 2, left: 20, right: 55 },
                { index: 4, left: 60, right: 95 },
                { index: 7, left: 130, right: 165 },
            ],
            CLEARANCE,
        );

        expect([...hidden]).toEqual([2]);
    });

    it('retains the final label when both endpoints cannot fit', () => {
        expect([
            ...findOverlappingXAxisLabels(
                [
                    { index: 0, left: 0, right: 70 },
                    { index: 1, left: 30, right: 100 },
                ],
                CLEARANCE,
            ),
        ]).toEqual([0]);
    });

    it('does not hide an isolated label', () => {
        expect([...findOverlappingXAxisLabels([{ index: 0, left: 0, right: 70 }], CLEARANCE)]).toEqual([]);
    });
});
