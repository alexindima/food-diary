import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { QRCodeComponent } from 'angularx-qrcode';
import { FdUiDialogComponent } from 'fd-ui-kit';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';

@Component({
    selector: 'fd-public-recipe-qr-dialog',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [FdUiDialogComponent, QRCodeComponent, TranslatePipe],
    templateUrl: './public-recipe-qr-dialog.html',
    styleUrl: './public-recipe-qr-dialog.scss',
})
export class PublicRecipeQrDialogComponent {
    protected readonly data = inject<{ url: string }>(FD_UI_DIALOG_DATA);
}
