import { isPlatformBrowser } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, type ElementRef, inject, PLATFORM_ID, signal, viewChild } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader';

@Component({
    selector: 'fd-barcode-scanner',
    imports: [TranslatePipe, FdUiDialogComponent, FdUiButtonComponent, FdUiLoaderComponent],
    templateUrl: './barcode-scanner.html',
    styleUrl: './barcode-scanner.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BarcodeScannerComponent {
    private readonly dialogRef = inject(FdUiDialogRef<BarcodeScannerComponent, string | null>);
    private readonly destroyRef = inject(DestroyRef);
    private readonly platformId = inject(PLATFORM_ID);
    private readonly isBrowser = isPlatformBrowser(this.platformId);
    private readonly videoRef = viewChild<ElementRef<HTMLVideoElement>>('video');

    protected readonly isCameraReady = signal(false);
    protected readonly isCameraError = signal(false);
    protected readonly isUnsupported = signal(false);

    private stream: MediaStream | null = null;
    private animationFrameId = 0;
    private readonly detector: BarcodeDetector | null = null;

    public constructor() {
        if (!this.isBrowser || !('BarcodeDetector' in window)) {
            this.isUnsupported.set(true);
            return;
        }
        this.detector = new BarcodeDetector({
            formats: [
                'ean_13',
                'ean_8',
                'upc_a',
                'upc_e',
                'code_128',
                'code_39',
                'code_93',
                'codabar',
                'itf',
                'qr_code',
                'data_matrix',
                'pdf417',
                'aztec',
            ],
        });
        void this.startCameraAsync();
        this.destroyRef.onDestroy(() => {
            this.stopCamera();
        });
    }

    protected close(): void {
        this.stopCamera();
        this.dialogRef.close(null);
    }

    private async startCameraAsync(): Promise<void> {
        try {
            this.stream = await navigator.mediaDevices.getUserMedia({
                video: { facingMode: 'environment' },
            });
            // Wait for next tick so viewChild is available
            setTimeout(() => {
                const video = this.videoRef()?.nativeElement;
                if (video !== undefined) {
                    video.srcObject = this.stream;
                    void video.play();
                    this.isCameraReady.set(true);
                    this.scanLoop();
                }
            });
        } catch {
            this.isCameraError.set(true);
        }
    }

    private scanLoop(): void {
        const video = this.videoRef()?.nativeElement;
        if (video === undefined || this.detector === null) {
            return;
        }

        if (!this.isBrowser) {
            return;
        }

        this.animationFrameId = window.requestAnimationFrame(() => {
            void this.scanFrameAsync(video);
        });
    }

    private async scanFrameAsync(video: HTMLVideoElement): Promise<void> {
        if (video.readyState === video.HAVE_ENOUGH_DATA) {
            try {
                const detector = this.detector;
                if (detector === null) {
                    return;
                }
                const barcodes = await detector.detect(video);
                if (barcodes.length > 0) {
                    this.stopCamera();
                    this.dialogRef.close(barcodes[0]?.rawValue ?? null);
                    return;
                }
            } catch {
                /* ignore detection errors */
            }
        }
        this.scanLoop();
    }

    private stopCamera(): void {
        if (this.isBrowser) {
            window.cancelAnimationFrame(this.animationFrameId);
        }
        this.stream?.getTracks().forEach(track => {
            track.stop();
        });
        this.stream = null;
    }
}
