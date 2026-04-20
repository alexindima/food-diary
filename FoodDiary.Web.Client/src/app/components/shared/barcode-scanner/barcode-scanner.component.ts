import { ChangeDetectionStrategy, Component, DestroyRef, ElementRef, inject, signal, viewChild } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader.component';

@Component({
    selector: 'fd-barcode-scanner',
    standalone: true,
    imports: [TranslatePipe, FdUiDialogComponent, FdUiButtonComponent, FdUiLoaderComponent],
    templateUrl: './barcode-scanner.component.html',
    styleUrl: './barcode-scanner.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BarcodeScannerComponent {
    private readonly dialogRef = inject(FdUiDialogRef<BarcodeScannerComponent, string | null>);
    private readonly destroyRef = inject(DestroyRef);
    private readonly videoRef = viewChild<ElementRef<HTMLVideoElement>>('video');

    public readonly isCameraReady = signal(false);
    public readonly isCameraError = signal(false);
    public readonly isUnsupported = signal(false);

    private stream: MediaStream | null = null;
    private animationFrameId = 0;
    private detector: BarcodeDetector | null = null;

    public constructor() {
        if (!('BarcodeDetector' in window)) {
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
        this.startCamera();
        this.destroyRef.onDestroy(() => this.stopCamera());
    }

    public close(): void {
        this.stopCamera();
        this.dialogRef.close(null);
    }

    private async startCamera(): Promise<void> {
        try {
            this.stream = await navigator.mediaDevices.getUserMedia({
                video: { facingMode: 'environment' },
            });
            // Wait for next tick so viewChild is available
            setTimeout(() => {
                const video = this.videoRef()?.nativeElement;
                if (video) {
                    video.srcObject = this.stream;
                    video.play();
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
        if (!video || !this.detector) {
            return;
        }

        this.animationFrameId = requestAnimationFrame(async () => {
            if (video.readyState === video.HAVE_ENOUGH_DATA) {
                try {
                    const barcodes = await this.detector!.detect(video);
                    if (barcodes.length > 0) {
                        this.stopCamera();
                        this.dialogRef.close(barcodes[0].rawValue);
                        return;
                    }
                } catch {
                    /* ignore detection errors */
                }
            }
            this.scanLoop();
        });
    }

    private stopCamera(): void {
        cancelAnimationFrame(this.animationFrameId);
        this.stream?.getTracks().forEach(track => track.stop());
        this.stream = null;
    }
}
