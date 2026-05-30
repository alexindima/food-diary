import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';

import type { ImageSelection } from '../../../../shared/models/image-upload.data';
import { ImageUploadFieldComponent } from '../../image-upload-field/image-upload-field';

@Component({
    selector: 'fd-photo-upload-dialog',
    imports: [TranslatePipe, FdUiDialogComponent, FdUiDialogFooterDirective, FdUiButtonComponent, ImageUploadFieldComponent],
    templateUrl: './photo-upload-dialog.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PhotoUploadDialogComponent {
    private readonly dialogRef = inject(FdUiDialogRef<PhotoUploadDialogComponent, ImageSelection | null>, { optional: true });

    protected onImageChanged(selection: ImageSelection | null): void {
        if ((selection?.assetId ?? '').trim().length > 0) {
            this.dialogRef?.close(selection);
        }
    }

    protected close(): void {
        this.dialogRef?.close(null);
    }
}
