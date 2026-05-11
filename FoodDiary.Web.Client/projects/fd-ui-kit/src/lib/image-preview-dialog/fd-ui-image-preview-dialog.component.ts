import { CommonModule, NgOptimizedImage } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { FdUiDialogComponent } from '../dialog/fd-ui-dialog.component';
import { FD_UI_DIALOG_DATA } from '../dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from '../dialog/fd-ui-dialog-ref';

export interface FdUiImagePreviewDialogData {
    imageUrl?: string;
    collageImages?: ReadonlyArray<FdUiImagePreviewDialogCollageImage>;
    alt?: string;
    title?: string;
}

export interface FdUiImagePreviewDialogCollageImage {
    url: string;
    alt?: string;
}

const MAX_COLLAGE_IMAGES = 4;

@Component({
    selector: 'fd-ui-image-preview-dialog',
    standalone: true,
    imports: [NgOptimizedImage, CommonModule, TranslatePipe, FdUiDialogComponent],
    templateUrl: './fd-ui-image-preview-dialog.component.html',
    styleUrls: ['./fd-ui-image-preview-dialog.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiImagePreviewDialogComponent {
    private readonly dialogRef = inject(FdUiDialogRef<FdUiImagePreviewDialogComponent, void>);
    private readonly dialogData = inject<FdUiImagePreviewDialogData>(FD_UI_DIALOG_DATA);

    public readonly imageUrl = this.dialogData.imageUrl?.trim() ?? '';
    public readonly collageImages = (this.dialogData.collageImages ?? []).slice(0, MAX_COLLAGE_IMAGES);
    public readonly alt = this.dialogData.alt ?? '';
    public readonly title = this.dialogData.title?.trim() ?? '';

    public close(): void {
        this.dialogRef.close();
    }
}
