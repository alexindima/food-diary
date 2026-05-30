import { CommonModule, NgOptimizedImage } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { FdUiDialogComponent } from '../dialog/fd-ui-dialog';
import { FD_UI_DIALOG_DATA } from '../dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from '../dialog/fd-ui-dialog-ref';

export type FdUiImagePreviewDialogData = {
    imageUrl?: string;
    collageImages?: readonly FdUiImagePreviewDialogCollageImage[];
    alt?: string;
    title?: string;
};

export type FdUiImagePreviewDialogCollageImage = {
    url: string;
    alt?: string;
};

const MAX_COLLAGE_IMAGES = 4;

@Component({
    selector: 'fd-ui-image-preview-dialog',
    imports: [NgOptimizedImage, CommonModule, TranslatePipe, FdUiDialogComponent],
    templateUrl: './fd-ui-image-preview-dialog.html',
    styleUrls: ['./fd-ui-image-preview-dialog.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiImagePreviewDialogComponent {
    private readonly dialogRef = inject(FdUiDialogRef<FdUiImagePreviewDialogComponent, void>);
    private readonly dialogData = inject<FdUiImagePreviewDialogData>(FD_UI_DIALOG_DATA);

    protected readonly imageUrl = this.dialogData.imageUrl?.trim() ?? '';
    protected readonly collageImages = (this.dialogData.collageImages ?? []).slice(0, MAX_COLLAGE_IMAGES);
    protected readonly alt = this.dialogData.alt ?? '';
    protected readonly title = this.dialogData.title?.trim() ?? '';

    protected close(): void {
        this.dialogRef.close();
    }
}
