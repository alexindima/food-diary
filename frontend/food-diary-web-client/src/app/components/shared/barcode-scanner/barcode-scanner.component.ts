import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { BarcodeFormat } from '@zxing/library';
import { ZXingScannerModule } from '@zxing/ngx-scanner';
import { injectContext } from '@taiga-ui/polymorpheus';
import { TuiDialogContext, TuiLoader } from '@taiga-ui/core';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
    selector: 'app-barcode-scanner',
    imports: [
        ZXingScannerModule,
        TranslatePipe,
        TuiLoader
    ],
    templateUrl: './barcode-scanner.component.html',
    styleUrl: './barcode-scanner.component.less',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class BarcodeScannerComponent {
    public readonly context = injectContext<TuiDialogContext<string, null>>();

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
        this.context.completeWith(barcode);
    }
}
