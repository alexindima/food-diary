import { TestBed } from '@angular/core/testing';
import { firstValueFrom, of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { ImageUploadService } from '../api/image-upload.service';
import { ImageUploadFacade } from './image-upload.facade';

describe('ImageUploadFacade', () => {
    it('completes presign, binary upload, and server confirmation as one operation', async () => {
        const imageUploadService = {
            requestUploadUrl: vi.fn(() =>
                of({ uploadUrl: 'https://upload.example.com', fileUrl: 'https://cdn.example.com/image.jpg', assetId: 'asset-1' }),
            ),
            uploadToPresignedUrl: vi.fn(() => of(void 0)),
            confirmUpload: vi.fn(() => of({ fileUrl: 'https://cdn.example.com/image.jpg', assetId: 'asset-1' })),
        };
        TestBed.configureTestingModule({
            providers: [ImageUploadFacade, { provide: ImageUploadService, useValue: imageUploadService }],
        });
        const facade = TestBed.inject(ImageUploadFacade);
        const file = new File(['image'], 'photo.png', { type: 'image/png' });

        await expect(firstValueFrom(facade.upload(file))).resolves.toEqual({
            url: 'https://cdn.example.com/image.jpg',
            assetId: 'asset-1',
        });
        expect(imageUploadService.requestUploadUrl).toHaveBeenCalledWith(file);
        expect(imageUploadService.uploadToPresignedUrl).toHaveBeenCalledWith('https://upload.example.com', file);
        expect(imageUploadService.confirmUpload).toHaveBeenCalledWith('asset-1');
    });
});
