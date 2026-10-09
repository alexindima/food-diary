import { HttpClient, HttpContext, HttpHeaders } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { catchError, map, type Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { SKIP_AUTH } from '../../constants/http-context.tokens';
import { rethrowApiError } from '../lib/api-error.utils';
import type { ConfirmImageUploadResponse, ImageUploadUrlResponse } from '../models/image-upload.data';
import { utcInstant } from '../models/semantics/date-value';
import { entityId, type ImageAssetId } from '../models/semantics/entity-id';
import { publicImageUrl, type SignedImageUploadUrl, signedImageUploadUrl } from '../models/semantics/image-location';
import { ImagesSdk } from './sdk/generated/api/images.service';
import { createSdkConnection } from './sdk/sdk-connection';
import { requireSdkFields } from './sdk/sdk-response';

@Service()
export class ImageUploadService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = environment.apiUrls.images;
    private readonly sdk = createSdkConnection(ImagesSdk, this.baseUrl, this.http);

    public requestUploadUrl(file: File): Observable<ImageUploadUrlResponse> {
        const body = {
            fileName: file.name,
            contentType: file.type,
            fileSizeBytes: file.size,
        };

        return this.sdk.client.postImagesUploadUrl({ version: this.sdk.version, getImageUploadUrlHttpRequest: body }).pipe(
            map(response => {
                const value = requireSdkFields(response, ['assetId', 'uploadUrl', 'fileUrl', 'expiresAtUtc']);
                return {
                    ...value,
                    assetId: entityId<'image-asset'>(value.assetId),
                    uploadUrl: signedImageUploadUrl(value.uploadUrl),
                    fileUrl: publicImageUrl(value.fileUrl),
                    expiresAtUtc: utcInstant(value.expiresAtUtc),
                };
            }),
            catchError((error: unknown) => rethrowApiError('Failed to request image upload URL', error)),
        );
    }

    public uploadToPresignedUrl(uploadUrl: SignedImageUploadUrl, file: File): Observable<void> {
        const headers = new HttpHeaders({
            'Content-Type': file.type,
        });

        const context = new HttpContext().set(SKIP_AUTH, true);

        return this.http.put(uploadUrl, file, { headers, responseType: 'text', context }).pipe(
            map(() => void 0),
            catchError((error: unknown) => rethrowApiError('Failed to upload image to S3', error)),
        );
    }

    public confirmUpload(assetId: ImageAssetId): Observable<ConfirmImageUploadResponse> {
        return this.sdk.client.postImagesByAssetIdConfirm({ version: this.sdk.version, assetId }).pipe(
            map(response => {
                const value = requireSdkFields(response, ['assetId', 'fileUrl']);
                return { ...value, assetId: entityId<'image-asset'>(value.assetId), fileUrl: publicImageUrl(value.fileUrl) };
            }),
            catchError((error: unknown) => rethrowApiError('Failed to confirm image upload', error)),
        );
    }

    public deleteAsset(assetId: ImageAssetId): Observable<void> {
        return this.sdk.client
            .deleteImagesByAssetId({ version: this.sdk.version, assetId })
            .pipe(catchError((error: unknown) => rethrowApiError('Failed to delete image asset', error)));
    }
}
