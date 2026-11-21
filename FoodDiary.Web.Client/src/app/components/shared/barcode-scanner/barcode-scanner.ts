import { ChangeDetectionStrategy, Component, DestroyRef, type ElementRef, inject, signal, viewChild } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader';

import { BrowserWindowService } from '../../../shared/platform/browser-window.service';

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
    private readonly browserWindow = inject(BrowserWindowService);
    private readonly videoRef = viewChild<ElementRef<HTMLVideoElement>>('video');

    protected readonly isCameraReady = signal(false);
    protected readonly isCameraError = signal(false);
    protected readonly isUnsupported = signal(false);

    private stream: MediaStream | null = null;
    private animationFrameId: number | null = null;
    private isClosed = false;

    public constructor() {
        const detector = this.browserWindow.createBarcodeDetector([
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
        ]);
        if (detector === null) {
            this.isUnsupported.set(true);
            return;
        }
        void this.startCameraAsync(detector);
        this.destroyRef.onDestroy(() => {
            this.stopCamera();
        });
    }

    protected close(): void {
        this.stopCamera();
        this.dialogRef.close(null);
    }

    private async startCameraAsync(detector: BarcodeDetector): Promise<void> {
        try {
            const stream = await this.browserWindow.getUserMediaAsync({
                video: { facingMode: 'environment' },
            });
            if (this.isInactive()) {
                stream.getTracks().forEach(track => {
                    track.stop();
                });
                return;
            }
            this.stream = stream;
            // Wait for next tick so viewChild is available
            await new Promise<void>(resolve => {
                setTimeout(resolve);
            });
            if (this.isInactive()) {
                return;
            }
            const video = this.videoRef()?.nativeElement;
            if (video === undefined) {
                return;
            }
            video.srcObject = stream;
            await video.play();
            if (this.isInactive()) {
                return;
            }
            this.isCameraReady.set(true);
            this.scanLoop(video, detector);
        } catch {
            if (!this.isInactive()) {
                this.stopCamera();
                this.isCameraError.set(true);
            }
        }
    }

    private isInactive(): boolean {
        return this.isClosed || this.destroyRef.destroyed;
    }

    private scanLoop(video: HTMLVideoElement, detector: BarcodeDetector): void {
        if (this.isClosed) {
            return;
        }

        this.animationFrameId = this.browserWindow.requestAnimationFrame(() => {
            void this.scanFrameAsync(video, detector);
        });
    }

    private async scanFrameAsync(video: HTMLVideoElement, detector: BarcodeDetector): Promise<void> {
        if (video.readyState === video.HAVE_ENOUGH_DATA) {
            try {
                const barcodes = await detector.detect(video);
                if (this.isClosed) {
                    return;
                }
                const barcode = barcodes.at(0);
                if (barcode !== undefined) {
                    this.stopCamera();
                    this.dialogRef.close(barcode.rawValue);
                    return;
                }
            } catch {
                /* ignore detection errors */
            }
        }
        this.scanLoop(video, detector);
    }

    private stopCamera(): void {
        this.isClosed = true;
        this.browserWindow.cancelAnimationFrame(this.animationFrameId);
        this.animationFrameId = null;
        this.stream?.getTracks().forEach(track => {
            track.stop();
        });
        this.stream = null;
    }
}
