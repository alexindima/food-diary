import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { ImageUploadFieldComponent } from '../../image-upload-field/image-upload-field.component';
import { ImageSelection } from '../../../../shared/models/image-upload.data';

@Component({
    selector: 'fd-photo-upload-dialog',
    standalone: true,
    imports: [TranslatePipe, FdUiDialogComponent, FdUiDialogFooterDirective, FdUiButtonComponent, ImageUploadFieldComponent],
    templateUrl: './photo-upload-dialog.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PhotoUploadDialogComponent {
    private readonly dialogRef = inject(FdUiDialogRef<PhotoUploadDialogComponent, ImageSelection | null>, { optional: true });

    public onImageChanged(selection: ImageSelection | null): void {
        if (selection?.assetId) {
            this.dialogRef?.close(selection);
        }
    }

    public close(): void {
        this.dialogRef?.close(null);
    }
}
