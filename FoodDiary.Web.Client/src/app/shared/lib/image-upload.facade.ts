import { inject, Service } from '@angular/core';
import { map, type Observable, switchMap } from 'rxjs';

import { ImageUploadService } from '../api/image-upload.service';
import type { ImageSelection } from '../models/image-upload.data';
import { imageSelection } from '../models/image-upload.data';
import type { ImageAssetId } from '../models/semantics/entity-id';

@Service()
export class ImageUploadFacade {
    private readonly imageUploadService = inject(ImageUploadService);

    public upload(file: File, keepPrivate = false): Observable<ImageSelection> {
        const request = keepPrivate ? this.imageUploadService.requestUploadUrl(file, true) : this.imageUploadService.requestUploadUrl(file);
        return request.pipe(
            switchMap(presign =>
                this.imageUploadService.uploadToPresignedUrl(presign.uploadUrl, file).pipe(
                    switchMap(() => this.imageUploadService.confirmUpload(presign.assetId)),
                    map(confirmed => imageSelection(confirmed.fileUrl, confirmed.assetId)),
                ),
            ),
        );
    }

    public getOwnerPreview(assetId: ImageAssetId): Observable<string> {
        return this.imageUploadService.getOwnerPreview(assetId);
    }

    public deleteAsset(assetId: ImageAssetId): Observable<void> {
        return this.imageUploadService.deleteAsset(assetId);
    }
}
