import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { BarcodeFormat } from '@zxing/library';
import { ZXingScannerModule } from '@zxing/ngx-scanner';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiDialogRef } from 'fd-ui-kit/material';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader.component';

@Component({
    selector: 'fd-barcode-scanner',
    standalone: true,
    imports: [
        ZXingScannerModule,
        TranslatePipe,
        FdUiDialogComponent,
        FdUiButtonComponent,
        FdUiLoaderComponent,
    ],
    templateUrl: './barcode-scanner.component.html',
    styleUrl: './barcode-scanner.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class BarcodeScannerComponent {
    private readonly dialogRef = inject(FdUiDialogRef<BarcodeScannerComponent, string | null>);

    public allowedScannerFormats = [
        BarcodeFormat.AZTEC,
        BarcodeFormat.CODABAR,
        BarcodeFormat.CODE_39,
        BarcodeFormat.CODE_93,
        BarcodeFormat.CODE_128,
        BarcodeFormat.DATA_MATRIX,
        BarcodeFormat.EAN_8,
        BarcodeFormat.EAN_13,
        BarcodeFormat.ITF,
        BarcodeFormat.MAXICODE,
        BarcodeFormat.PDF_417,
        BarcodeFormat.QR_CODE,
        BarcodeFormat.RSS_14,
        BarcodeFormat.RSS_EXPANDED,
        BarcodeFormat.UPC_A,
        BarcodeFormat.UPC_E,
        BarcodeFormat.UPC_EAN_EXTENSION
    ];

    public isCameraReady = signal<boolean>(false);
    public isCameraError = signal<boolean>(false);

    public onCamerasFound(devices: MediaDeviceInfo[]): void {
        if (devices && devices.length > 0) {
            this.isCameraReady.set(true);
        } else {
            this.isCameraError.set(true);
        }
    }

    public onScanSuccess(barcode: string): void {
        this.dialogRef.close(barcode);
    }

    public close(): void {
        this.dialogRef.close(null);
    }
}
