import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

import { ImageUploadService } from '../api/image-upload.service';
import type { ImageUploadUrlResponse } from '../models/image-upload.data';

@Injectable({ providedIn: 'root' })
export class ImageUploadFacade {
    private readonly imageUploadService = inject(ImageUploadService);

    public requestUploadUrl(file: File): Observable<ImageUploadUrlResponse> {
        return this.imageUploadService.requestUploadUrl(file);
    }

    public uploadToPresignedUrl(uploadUrl: string, file: File): Observable<void> {
        return this.imageUploadService.uploadToPresignedUrl(uploadUrl, file);
    }

    public deleteAsset(assetId: string): Observable<void> {
        return this.imageUploadService.deleteAsset(assetId);
    }
}
