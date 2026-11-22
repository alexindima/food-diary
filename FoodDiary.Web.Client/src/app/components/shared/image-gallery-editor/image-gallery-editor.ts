import { ChangeDetectionStrategy, Component, computed, ElementRef, inject, input, model, signal, viewChildren } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';
import { FdUiImagePreviewDialogComponent } from 'fd-ui-kit/image-preview-dialog/fd-ui-image-preview-dialog';
import { FdUiMenuComponent, FdUiMenuItemComponent, FdUiMenuTriggerDirective } from 'fd-ui-kit/menu';

import type { ImageSelection } from '../../../shared/models/image-upload.data';
import { ImageUploadFieldComponent } from '../image-upload-field/image-upload-field';

const MAX_PHOTOS = 5;
@Component({
    selector: 'fd-image-gallery-editor',
    templateUrl: './image-gallery-editor.html',
    styleUrl: './image-gallery-editor.scss',
    imports: [
        TranslatePipe,
        FdUiButtonComponent,
        FdUiIconComponent,
        ImageUploadFieldComponent,
        FdUiMenuComponent,
        FdUiMenuItemComponent,
        FdUiMenuTriggerDirective,
    ],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ImageGalleryEditorComponent {
    private readonly dialogService = inject(FdUiDialogService);
    private readonly photoTriggers = viewChildren<FdUiButtonComponent, ElementRef<HTMLElement>>('photoTrigger', { read: ElementRef });
    public readonly compact = input(false);
    public readonly photos = model.required<ImageSelection[]>();
    public readonly disabled = input(false);
    public readonly scanning = input(false);
    public readonly reviewing = input(false);
    public readonly cover = model<ImageSelection | null>(null);
    public readonly uploading = model(false);
    protected readonly photoPreviews = computed(() => this.photos().map(({ url }) => (url !== null && url.trim().length > 0 ? url : null)));
    protected readonly uploadAppearance = computed(() => {
        if (this.photos().length > 0) {
            return 'hidden';
        }
        return this.compact() ? 'compact' : 'preview';
    });

    protected preview(index: number): void {
        const previews = this.photoPreviews().flatMap((url, sourceIndex) => (url === null ? [] : [{ url, sourceIndex }]));
        const initialIndex = previews.findIndex(preview => preview.sourceIndex === index);
        if (initialIndex === -1) {
            return;
        }

        this.dialogService.open(FdUiImagePreviewDialogComponent, {
            size: 'lg',
            width: 'var(--fd-size-dialog-media-width)',
            maxWidth: 'var(--fd-size-dialog-media-max-width)',
            restoreFocus: this.photoTriggers()[index]?.nativeElement.querySelector('button') ?? true,
            data: { collageImages: previews.map(({ url }) => ({ url })), initialIndex },
        });
    }

    protected readonly emptySlots = computed(() =>
        this.photos().length > 0
            ? Array.from({ length: Math.min(MAX_PHOTOS - 1, MAX_PHOTOS - this.photos().length) }, (_, index) => index)
            : [],
    );

    protected readonly maxPhotos = MAX_PHOTOS;
    protected readonly hasUploadError = signal(false);
    protected addBatch(photos: ImageSelection[]): void {
        this.photos.set([...this.photos(), ...photos].slice(0, MAX_PHOTOS));
    }
    protected remove(photo: ImageSelection): void {
        if (this.disabled() || this.uploading()) {
            return;
        }
        const matchesPhoto = (selection: ImageSelection): boolean =>
            selection.assetId === photo.assetId && (photo.assetId !== null || selection.url === photo.url);
        const cover = this.cover();
        if (cover !== null && matchesPhoto(cover)) {
            this.cover.set(null);
        }
        this.photos.set(this.photos().filter(item => !matchesPhoto(item)));
    }
}
