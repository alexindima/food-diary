interface BarcodeDetectorOptions {
    formats?: string[];
}

interface DetectedBarcode {
    rawValue: string;
    format: string;
    boundingBox: DOMRectReadOnly;
    cornerPoints: { x: number; y: number }[];
}

declare class BarcodeDetector {
    constructor(options?: BarcodeDetectorOptions);
    detect(source: ImageBitmapSource): Promise<DetectedBarcode[]>;
    static getSupportedFormats(): Promise<string[]>;
}
