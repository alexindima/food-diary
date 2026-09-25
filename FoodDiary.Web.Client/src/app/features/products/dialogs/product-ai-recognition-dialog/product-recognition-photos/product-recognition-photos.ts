import { ChangeDetectionStrategy, Component, input, model, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';

import { ImageUploadFieldComponent } from '../../../../../components/shared/image-upload-field/image-upload-field';
import type { ImageSelection } from '../../../../../shared/models/image-upload.data';

const MAX_PHOTOS = 5;
@Component({
    selector: 'fd-product-recognition-photos',
    templateUrl: './product-recognition-photos.html',
    styleUrl: './product-recognition-photos.scss',
    imports: [TranslatePipe, FdUiButtonComponent, ImageUploadFieldComponent],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductRecognitionPhotosComponent {
    public readonly photos = model.required<ImageSelection[]>();
    public readonly disabled = input(false);
    public readonly reviewing = input(false);
    public readonly cover = model<ImageSelection | null>(null);
    public readonly uploading = model(false);

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
