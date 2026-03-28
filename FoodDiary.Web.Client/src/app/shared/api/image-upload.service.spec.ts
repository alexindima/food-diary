import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ImageUploadService } from './image-upload.service';
import { SKIP_AUTH } from '../../constants/http-context.tokens';
import { environment } from '../../../environments/environment';

describe('ImageUploadService', () => {
    let service: ImageUploadService;
    let httpMock: HttpTestingController;

    const baseUrl = environment.apiUrls.images;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [ImageUploadService, provideHttpClient(), provideHttpClientTesting()],
        });

        service = TestBed.inject(ImageUploadService);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should request upload URL with file info', () => {
        const file = new File(['image-data'], 'photo.jpg', { type: 'image/jpeg' });

        const mockResponse = {
            uploadUrl: 'https://s3.example.com/upload?signed=abc',
            fileUrl: 'https://cdn.example.com/photo.jpg',
            objectKey: 'uploads/photo.jpg',
            expiresAtUtc: '2026-03-28T12:00:00Z',
            assetId: 'asset-001',
        };

        service.requestUploadUrl(file).subscribe(response => {
            expect(response.uploadUrl).toBe('https://s3.example.com/upload?signed=abc');
            expect(response.assetId).toBe('asset-001');
        });

        const req = httpMock.expectOne(`${baseUrl}/upload-url`);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual({
            fileName: 'photo.jpg',
            contentType: 'image/jpeg',
            fileSizeBytes: file.size,
        });
        req.flush(mockResponse);
    });

    it('should upload to presigned URL with correct headers', () => {
        const file = new File(['image-data'], 'photo.png', { type: 'image/png' });
        const presignedUrl = 'https://s3.example.com/upload?signed=xyz';

        service.uploadToPresignedUrl(presignedUrl, file).subscribe();

        const req = httpMock.expectOne(presignedUrl);
        expect(req.request.method).toBe('PUT');
        expect(req.request.headers.get('Content-Type')).toBe('image/png');
        req.flush('', { status: 200, statusText: 'OK' });
    });

    it('should set SKIP_AUTH context on presigned upload', () => {
        const file = new File(['image-data'], 'photo.png', { type: 'image/png' });
        const presignedUrl = 'https://s3.example.com/upload?signed=xyz';

        service.uploadToPresignedUrl(presignedUrl, file).subscribe();

        const req = httpMock.expectOne(presignedUrl);
        expect(req.request.context.get(SKIP_AUTH)).toBeTrue();
        req.flush('', { status: 200, statusText: 'OK' });
    });

    it('should delete asset', () => {
        const assetId = 'asset-to-delete';

        service.deleteAsset(assetId).subscribe();

        const req = httpMock.expectOne(`${baseUrl}/${assetId}`);
        expect(req.request.method).toBe('DELETE');
        req.flush(null);
    });
});
