import { describe, expect, it } from 'vitest';

import {
    calculateContainedImageBounds,
    createInitialCropSelection,
    type CropRect,
    moveCropSelection,
    resizeCropSelection,
} from './image-upload-field-crop.utils';

const SURFACE_WIDTH = 600;
const SURFACE_HEIGHT = 400;
const WIDE_IMAGE_WIDTH = 1200;
const WIDE_IMAGE_HEIGHT = 600;
const SQUARE_ASPECT_RATIO = 1;
const MIN_SIZE = 48;
const MOVE_RIGHT_BEYOND_BOUNDS = 500;
const MOVE_UP_BEYOND_BOUNDS = -100;

const BOUNDS: CropRect = {
    x: 0,
    y: 0,
    width: 400,
    height: 300,
};

describe('image upload crop utils', () => {
    it('calculates contained image bounds for object-fit contain layout', () => {
        expect(
            calculateContainedImageBounds({
                surfaceWidth: SURFACE_WIDTH,
                surfaceHeight: SURFACE_HEIGHT,
                imageWidth: WIDE_IMAGE_WIDTH,
                imageHeight: WIDE_IMAGE_HEIGHT,
            }),
        ).toEqual({
            x: 0,
            y: 50,
            width: 600,
            height: 300,
        });
    });

    it('centers initial crop selection with fixed aspect ratio', () => {
        expect(createInitialCropSelection(BOUNDS, SQUARE_ASPECT_RATIO)).toEqual({
            x: 50,
            y: 0,
            width: 300,
            height: 300,
        });
    });

    it('moves crop selection without leaving image bounds', () => {
        expect(
            moveCropSelection({ x: 50, y: 60, width: 120, height: 80 }, BOUNDS, MOVE_RIGHT_BEYOND_BOUNDS, MOVE_UP_BEYOND_BOUNDS),
        ).toEqual({
            x: 280,
            y: 0,
            width: 120,
            height: 80,
        });
    });

    it('resizes fixed-ratio selection and keeps it within bounds', () => {
        expect(
            resizeCropSelection({
                mode: 'resize-se',
                rect: { x: 100, y: 50, width: 120, height: 120 },
                bounds: BOUNDS,
                dx: 300,
                dy: 300,
                aspectRatio: SQUARE_ASPECT_RATIO,
                minSize: MIN_SIZE,
            }),
        ).toEqual({
            x: 100,
            y: 0,
            width: 300,
            height: 300,
        });
    });

    it('resizes free selection with minimum size', () => {
        expect(
            resizeCropSelection({
                mode: 'resize-nw',
                rect: { x: 100, y: 80, width: 120, height: 100 },
                bounds: BOUNDS,
                dx: 200,
                dy: 200,
                aspectRatio: null,
                minSize: MIN_SIZE,
            }),
        ).toEqual({
            x: 172,
            y: 132,
            width: 48,
            height: 48,
        });
    });
});
