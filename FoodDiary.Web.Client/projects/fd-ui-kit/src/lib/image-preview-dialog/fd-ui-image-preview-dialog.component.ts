import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, ViewEncapsulation } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FD_UI_DIALOG_DATA } from '../dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from '../dialog/fd-ui-dialog-ref';
import { FdUiDialogComponent } from '../dialog/fd-ui-dialog.component';

export interface FdUiImagePreviewDialogData {
    imageUrl: string;
    alt?: string;
    title?: string;
}

@Component({
    selector: 'fd-ui-image-preview-dialog',
    standalone: true,
    imports: [CommonModule, TranslatePipe, FdUiDialogComponent],
    templateUrl: './fd-ui-image-preview-dialog.component.html',
    styleUrls: ['./fd-ui-image-preview-dialog.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    encapsulation: ViewEncapsulation.None,
})
export class FdUiImagePreviewDialogComponent {
    private readonly dialogRef = inject(FdUiDialogRef<FdUiImagePreviewDialogComponent, void>);
    private readonly dialogData = inject<FdUiImagePreviewDialogData>(FD_UI_DIALOG_DATA);

    public readonly imageUrl = this.dialogData.imageUrl;
    public readonly alt = this.dialogData.alt ?? '';
    public readonly title = this.dialogData.title?.trim() ?? '';

    public close(): void {
        this.dialogRef.close();
    }
}
