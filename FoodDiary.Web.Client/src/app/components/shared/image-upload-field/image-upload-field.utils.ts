import { createClientId } from '../../../shared/lib/client-id.utils';

const BYTES_PER_KB = 1024;
const MIN_DIMENSION = 1;
const RESIZABLE_IMAGE_TYPES = new Set(['image/jpeg', 'image/png', 'image/webp']);

export type ImageResizeDimensions = {
    width: number;
    height: number;
};

export function createImageUploadId(prefix: string): string {
    return createClientId(prefix);
}

export function getMaxImageUploadBytes(maxSizeMb: number): number {
    return maxSizeMb * BYTES_PER_KB * BYTES_PER_KB;
}

export function canResizeImageType(type: string): boolean {
    return RESIZABLE_IMAGE_TYPES.has(type);
}

export function calculateImageResizeDimensions(width: number, height: number, maxDimension: number): ImageResizeDimensions | null {
    if (maxDimension <= 0) {
        return null;
    }

    const largestSide = Math.max(width, height);
    if (largestSide <= maxDimension) {
        return null;
    }

    const scale = maxDimension / largestSide;
    return {
        width: Math.max(MIN_DIMENSION, Math.round(width * scale)),
        height: Math.max(MIN_DIMENSION, Math.round(height * scale)),
    };
}
