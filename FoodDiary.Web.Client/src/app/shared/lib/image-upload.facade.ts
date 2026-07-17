import { inject, Service } from '@angular/core';
import { map, type Observable, switchMap } from 'rxjs';

import { ImageUploadService } from '../api/image-upload.service';
import type { ImageSelection } from '../models/image-upload.data';

@Service()
export class ImageUploadFacade {
    private readonly imageUploadService = inject(ImageUploadService);

    public upload(file: File): Observable<ImageSelection> {
        return this.imageUploadService
            .requestUploadUrl(file)
            .pipe(
                switchMap(presign =>
                    this.imageUploadService
                        .uploadToPresignedUrl(presign.uploadUrl, file)
                        .pipe(map(() => ({ url: presign.fileUrl, assetId: presign.assetId }))),
                ),
            );
    }

    public deleteAsset(assetId: string): Observable<void> {
        return this.imageUploadService.deleteAsset(assetId);
    }
}
