import { describe, expect, it, vi } from 'vitest';

import {
    calculateImageResizeDimensions,
    canResizeImageType,
    createImageUploadId,
    getMaxImageUploadBytes,
} from './image-upload-field.utils';

const MAX_SIZE_MB = 20;
const BYTES_PER_KB = 1024;
const BYTES_IN_MB = BYTES_PER_KB * BYTES_PER_KB;
const WIDE_WIDTH = 4000;
const WIDE_HEIGHT = 2000;
const MAX_DIMENSION = 1000;
const EXPECTED_WIDE_WIDTH = 1000;
const EXPECTED_WIDE_HEIGHT = 500;
const SMALL_WIDTH = 800;
const SMALL_HEIGHT = 600;

describe('image upload field utils', () => {
    it('creates stable-prefixed ids using crypto when available', () => {
        const originalCrypto = globalThis.crypto;
        Object.defineProperty(globalThis, 'crypto', {
            configurable: true,
            value: { randomUUID: vi.fn(() => 'uuid-1') },
        });

        expect(createImageUploadId('image-upload-error')).toBe('image-upload-error-uuid-1');

        Object.defineProperty(globalThis, 'crypto', {
            configurable: true,
            value: originalCrypto,
        });
    });

    it('converts megabytes to bytes', () => {
        expect(getMaxImageUploadBytes(MAX_SIZE_MB)).toBe(MAX_SIZE_MB * BYTES_IN_MB);
    });

    it('allows resizing only for canvas-friendly image formats', () => {
        expect(canResizeImageType('image/jpeg')).toBe(true);
        expect(canResizeImageType('image/png')).toBe(true);
        expect(canResizeImageType('image/webp')).toBe(true);
        expect(canResizeImageType('image/gif')).toBe(false);
    });

    it('calculates proportional resize dimensions when image is too large', () => {
        expect(calculateImageResizeDimensions(WIDE_WIDTH, WIDE_HEIGHT, MAX_DIMENSION)).toEqual({
            width: EXPECTED_WIDE_WIDTH,
            height: EXPECTED_WIDE_HEIGHT,
        });
    });

    it('returns null when resize is not needed or max dimension is invalid', () => {
        expect(calculateImageResizeDimensions(SMALL_WIDTH, SMALL_HEIGHT, MAX_DIMENSION)).toBeNull();
        expect(calculateImageResizeDimensions(WIDE_WIDTH, WIDE_HEIGHT, 0)).toBeNull();
    });
});
