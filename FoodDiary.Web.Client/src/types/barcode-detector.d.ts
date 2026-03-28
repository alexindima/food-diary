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
    public constructor(options?: BarcodeDetectorOptions);
    public detect(source: ImageBitmapSource): Promise<DetectedBarcode[]>;
    public static getSupportedFormats(): Promise<string[]>;
}
