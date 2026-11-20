import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../environments/environment';
import { ImageUploadService } from '../api/image-upload.service';
import { imageSelection } from '../models/image-upload.data';
import { entityId } from '../models/semantics/entity-id';
import { ImageUploadFacade } from './image-upload.facade';

const imageApi = environment.apiUrls.images;
const assetId = entityId<'image-asset'>('fixture-image');
const imageUrl = 'https://cdn.example.test/fixture.png';
const storageUrl = 'https://storage.example.test/fixture.png?signature=fixture';

describe('image upload HTTP failure boundaries', () => {
    let facade: ImageUploadFacade;
    let http: HttpTestingController;
    let file: File;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [provideHttpClient(), provideHttpClientTesting(), ImageUploadService, ImageUploadFacade],
        });
        facade = TestBed.inject(ImageUploadFacade);
        http = TestBed.inject(HttpTestingController);
        file = new File(['fixture'], 'fixture.png', { type: 'image/png' });
    });

    afterEach(() => {
        http.verify();
    });

    it('stops before sending file bytes when the API cannot issue a signed upload', async () => {
        const unavailable = { status: 503, statusText: 'Service Unavailable' };
        const result = expect(firstValueFrom(facade.upload(file))).rejects.toMatchObject(unavailable);
        http.expectOne({ method: 'POST', url: `${imageApi}/upload-url` }).flush(null, unavailable);

        await result;
        http.expectNone({ method: 'PUT' });
    });

    it('skips confirmation after storage rejects the signature and requests a fresh signature on retry', async () => {
        const expired = { status: 403, statusText: 'Forbidden' };
        const rejected = expect(firstValueFrom(facade.upload(file))).rejects.toMatchObject({ ...expired, error: 'Signature expired' });
        issueSignature(http, storageUrl);
        http.expectOne({ method: 'PUT', url: storageUrl }).flush('Signature expired', expired);
        await rejected;
        http.expectNone({ method: 'POST', url: `${imageApi}/${assetId}/confirm` });

        const newUrl = 'https://storage.example.test/fixture.png?signature=replacement';
        const retry = expect(firstValueFrom(facade.upload(file))).resolves.toEqual(imageSelection(imageUrl, assetId));
        issueSignature(http, newUrl);
        const upload = http.expectOne({ method: 'PUT', url: newUrl });
        expect(upload.request.body).toBe(file);
        upload.flush('');
        http.expectOne({ method: 'POST', url: `${imageApi}/${assetId}/confirm` }).flush({ assetId, fileUrl: imageUrl });
        await retry;
    });

    it('waits for confirmation and preserves a rejection instead of publishing the tentative image URL', async () => {
        const missing = { status: 404, statusText: 'Not Found' };
        const result = expect(firstValueFrom(facade.upload(file))).rejects.toMatchObject(missing);
        issueSignature(http, storageUrl);
        http.expectOne({ method: 'PUT', url: storageUrl }).flush('');
        http.expectOne({ method: 'POST', url: `${imageApi}/${assetId}/confirm` }).flush(null, missing);

        await result;
    });

    it('reports failed cleanup to the owner and allows another deletion attempt', async () => {
        const url = `${imageApi}/${assetId}`;
        const unavailable = { status: 503, statusText: 'Service Unavailable' };
        const result = expect(firstValueFrom(facade.deleteAsset(assetId))).rejects.toMatchObject(unavailable);
        http.expectOne({ method: 'DELETE', url }).flush(null, unavailable);
        await result;

        const retry = expect(firstValueFrom(facade.deleteAsset(assetId))).resolves.toBeNull();
        http.expectOne({ method: 'DELETE', url }).flush(null, { status: 204, statusText: 'No Content' });
        await retry;
    });
});

function issueSignature(http: HttpTestingController, uploadUrl: string): void {
    http.expectOne({ method: 'POST', url: `${imageApi}/upload-url` }).flush({
        assetId,
        uploadUrl,
        fileUrl: imageUrl,
        expiresAtUtc: '2026-11-20T10:00:00Z',
    });
}
