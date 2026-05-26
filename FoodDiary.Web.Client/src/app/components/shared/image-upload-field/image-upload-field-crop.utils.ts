import { calculateImageResizeDimensions } from './image-upload-field.utils';

const MIN_DIMENSION = 1;

export type CropRect = {
    x: number;
    y: number;
    width: number;
    height: number;
};

export type CropInteractionMode = 'move' | 'resize-nw' | 'resize-ne' | 'resize-sw' | 'resize-se';

export type CropResizeOptions = {
    mode: CropInteractionMode;
    rect: CropRect;
    bounds: CropRect;
    dx: number;
    dy: number;
    aspectRatio: number | null;
    minSize: number;
};

export type ContainedImageBoundsOptions = {
    surfaceWidth: number;
    surfaceHeight: number;
    imageWidth: number;
    imageHeight: number;
};

export type CreateCroppedCanvasOptions = {
    ownerDocument: Document;
    image: HTMLImageElement;
    selection: CropRect;
    bounds: CropRect;
    fixedSize: number | null;
    fillBackground: boolean;
};

export type ResizeCanvasOptions = {
    ownerDocument: Document;
    canvas: HTMLCanvasElement;
    maxSize: number;
};

export function calculateContainedImageBounds(options: ContainedImageBoundsOptions): CropRect | null {
    const { surfaceWidth, surfaceHeight, imageWidth, imageHeight } = options;
    if (surfaceWidth <= 0 || surfaceHeight <= 0 || imageWidth <= 0 || imageHeight <= 0) {
        return null;
    }

    const imageAspectRatio = imageWidth / imageHeight;
    const surfaceAspectRatio = surfaceWidth / surfaceHeight;
    const width = imageAspectRatio > surfaceAspectRatio ? surfaceWidth : surfaceHeight * imageAspectRatio;
    const height = imageAspectRatio > surfaceAspectRatio ? surfaceWidth / imageAspectRatio : surfaceHeight;

    return {
        x: (surfaceWidth - width) / 2,
        y: (surfaceHeight - height) / 2,
        width,
        height,
    };
}

export function createInitialCropSelection(bounds: CropRect, aspectRatio: number | null): CropRect {
    if (aspectRatio !== null && aspectRatio > 0) {
        let width = bounds.width;
        let height = width / aspectRatio;
        if (height > bounds.height) {
            height = bounds.height;
            width = height * aspectRatio;
        }

        return centerCropRect(bounds, width, height);
    }

    const size = Math.min(bounds.width, bounds.height);
    return centerCropRect(bounds, size, size);
}

export function moveCropSelection(rect: CropRect, bounds: CropRect, dx: number, dy: number): CropRect {
    return {
        ...rect,
        x: clamp(rect.x + dx, bounds.x, bounds.x + bounds.width - rect.width),
        y: clamp(rect.y + dy, bounds.y, bounds.y + bounds.height - rect.height),
    };
}

export function resizeCropSelection(options: CropResizeOptions): CropRect {
    const fixedAspectRatio = options.aspectRatio !== null && options.aspectRatio > 0 ? options.aspectRatio : null;
    const isWest = options.mode === 'resize-nw' || options.mode === 'resize-sw';
    const isNorth = options.mode === 'resize-nw' || options.mode === 'resize-ne';

    if (fixedAspectRatio !== null) {
        return resizeFixedAspectCropSelection(options, fixedAspectRatio, isWest, isNorth);
    }

    return resizeFreeCropSelection(options, isWest, isNorth);
}

export function createCroppedCanvas(options: CreateCroppedCanvasOptions): HTMLCanvasElement | null {
    const { ownerDocument, image, selection, bounds, fixedSize, fillBackground } = options;
    const scaleX = image.naturalWidth / bounds.width;
    const scaleY = image.naturalHeight / bounds.height;
    const sourceX = Math.round((selection.x - bounds.x) * scaleX);
    const sourceY = Math.round((selection.y - bounds.y) * scaleY);
    const sourceWidth = Math.round(selection.width * scaleX);
    const sourceHeight = Math.round(selection.height * scaleY);
    const canvas = ownerDocument.createElement('canvas');
    canvas.width = fixedSize !== null && fixedSize > 0 ? fixedSize : sourceWidth;
    canvas.height = fixedSize !== null && fixedSize > 0 ? fixedSize : sourceHeight;
    const ctx = canvas.getContext('2d');
    if (ctx === null || sourceWidth <= 0 || sourceHeight <= 0) {
        return null;
    }

    if (fillBackground) {
        ctx.fillStyle = '#fff';
        ctx.fillRect(0, 0, canvas.width, canvas.height);
    }

    ctx.drawImage(image, sourceX, sourceY, sourceWidth, sourceHeight, 0, 0, canvas.width, canvas.height);
    return canvas;
}

export function resizeCanvasToMax(options: ResizeCanvasOptions): HTMLCanvasElement | null {
    const { ownerDocument, canvas, maxSize } = options;
    const dimensions = calculateImageResizeDimensions(canvas.width, canvas.height, maxSize);
    if (dimensions === null) {
        return canvas;
    }

    const resized = ownerDocument.createElement('canvas');
    resized.width = Math.max(MIN_DIMENSION, dimensions.width);
    resized.height = Math.max(MIN_DIMENSION, dimensions.height);
    const ctx = resized.getContext('2d');
    if (ctx === null) {
        return null;
    }

    ctx.fillStyle = '#fff';
    ctx.fillRect(0, 0, resized.width, resized.height);
    ctx.drawImage(canvas, 0, 0, resized.width, resized.height);
    return resized;
}

function centerCropRect(bounds: CropRect, width: number, height: number): CropRect {
    return {
        x: bounds.x + (bounds.width - width) / 2,
        y: bounds.y + (bounds.height - height) / 2,
        width,
        height,
    };
}

function resizeFixedAspectCropSelection(options: CropResizeOptions, aspectRatio: number, isWest: boolean, isNorth: boolean): CropRect {
    const { rect, dx, dy, bounds, minSize } = options;
    const rawWidth = isWest ? rect.width - dx : rect.width + dx;
    const rawHeight = isNorth ? rect.height - dy : rect.height + dy;
    const sizeFromWidth = Math.max(minSize, rawWidth);
    const sizeFromHeight = Math.max(minSize / aspectRatio, rawHeight) * aspectRatio;
    const width = Math.max(sizeFromWidth, sizeFromHeight);
    const height = width / aspectRatio;
    const anchorX = isWest ? rect.x + rect.width : rect.x;
    const anchorY = isNorth ? rect.y + rect.height : rect.y;

    return clampCropRectToBounds(
        {
            x: isWest ? anchorX - width : anchorX,
            y: isNorth ? anchorY - height : anchorY,
            width,
            height,
        },
        bounds,
        aspectRatio,
    );
}

function resizeFreeCropSelection(options: CropResizeOptions, isWest: boolean, isNorth: boolean): CropRect {
    const { rect, dx, dy, bounds, minSize } = options;
    const width = Math.max(minSize, isWest ? rect.width - dx : rect.width + dx);
    const height = Math.max(minSize, isNorth ? rect.height - dy : rect.height + dy);
    const anchorX = isWest ? rect.x + rect.width : rect.x;
    const anchorY = isNorth ? rect.y + rect.height : rect.y;

    return clampCropRectToBounds(
        {
            x: isWest ? anchorX - width : rect.x,
            y: isNorth ? anchorY - height : rect.y,
            width,
            height,
        },
        bounds,
        null,
    );
}

function clampCropRectToBounds(rect: CropRect, bounds: CropRect, aspectRatio: number | null): CropRect {
    let width = Math.min(rect.width, bounds.width);
    let height = Math.min(rect.height, bounds.height);
    if (aspectRatio !== null) {
        if (width / height > aspectRatio) {
            width = height * aspectRatio;
        } else {
            height = width / aspectRatio;
        }
    }

    const x = clamp(rect.x, bounds.x, bounds.x + bounds.width - width);
    const y = clamp(rect.y, bounds.y, bounds.y + bounds.height - height);
    return { x, y, width, height };
}

function clamp(value: number, min: number, max: number): number {
    return Math.min(Math.max(value, min), max);
}
