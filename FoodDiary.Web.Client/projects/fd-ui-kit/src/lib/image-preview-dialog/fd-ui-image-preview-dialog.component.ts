import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FD_UI_DIALOG_DATA, FdUiDialogRef } from '../material';
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
