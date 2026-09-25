import { ChangeDetectionStrategy, Component, computed, inject, input, model, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiImagePreviewDialogComponent } from 'fd-ui-kit/image-preview-dialog/fd-ui-image-preview-dialog';
import { FdUiMenuComponent, FdUiMenuItemComponent, FdUiMenuTriggerDirective } from 'fd-ui-kit/menu';

import { ImageUploadFieldComponent } from '../../../../../components/shared/image-upload-field/image-upload-field';
import type { ImageSelection } from '../../../../../shared/models/image-upload.data';

const MAX_PHOTOS = 5;
@Component({
    selector: 'fd-product-recognition-photos',
    templateUrl: './product-recognition-photos.html',
    styleUrl: './product-recognition-photos.scss',
    imports: [
        TranslatePipe,
        FdUiButtonComponent,
        ImageUploadFieldComponent,
        FdUiMenuComponent,
        FdUiMenuItemComponent,
        FdUiMenuTriggerDirective,
    ],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductRecognitionPhotosComponent {
    private readonly dialogService = inject(FdUiDialogService);
    public readonly editor = input(false);
    public readonly photos = model.required<ImageSelection[]>();
    public readonly disabled = input(false);
    public readonly scanning = input(false);
    public readonly reviewing = input(false);
    public readonly cover = model<ImageSelection | null>(null);
    public readonly uploading = model(false);

    protected preview(index: number): void {
        this.dialogService.open(FdUiImagePreviewDialogComponent, {
            size: 'lg',
            width: 'var(--fd-size-dialog-media-width)',
            maxWidth: 'var(--fd-size-dialog-media-max-width)',
            data: { collageImages: this.photos().map(photo => ({ url: photo.url ?? '' })), initialIndex: index },
        });
    }

    protected readonly emptySlots = computed(() =>
        this.editor() && this.photos().length > 0
            ? Array.from({ length: Math.min(MAX_PHOTOS - 1, MAX_PHOTOS - this.photos().length) }, (_, index) => index)
            : [],
    );

    protected readonly maxPhotos = MAX_PHOTOS;
    protected readonly clearRequest = signal(0);
    protected addBatch(photos: ImageSelection[]): void {
        this.photos.set([...this.photos(), ...photos].slice(0, MAX_PHOTOS));
    }
    protected add(photo: ImageSelection | null): void {
        if (photo === null || (photo.assetId ?? '').length === 0) {
            return;
        }
        this.uploading.set(false);
        if (!this.disabled() && this.photos().length < MAX_PHOTOS && !this.photos().some(item => item.assetId === photo.assetId)) {
            this.photos.set([...this.photos(), photo]);
        }
        this.clearRequest.update(value => value + 1);
    }
    protected remove(photo: ImageSelection): void {
        if (this.disabled() || this.uploading()) {
            return;
        }
        if (this.cover()?.assetId === photo.assetId) {
            this.cover.set(null);
        }
        this.photos.set(this.photos().filter(item => item.assetId !== photo.assetId));
    }
}
