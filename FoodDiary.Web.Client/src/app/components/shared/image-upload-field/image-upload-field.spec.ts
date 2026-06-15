import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { of, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import { FrontendLoggerService } from '../../../services/frontend-logger.service';
import { ImageUploadFacade } from '../../../shared/lib/image-upload.facade';
import { ImageUploadFieldComponent } from './image-upload-field';

type ImageUploadFieldTestContext = {
    component: ImageUploadFieldComponent;
    fixture: ComponentFixture<ImageUploadFieldComponent>;
    imageUploadService: {
        deleteAsset: ReturnType<typeof vi.fn>;
        requestUploadUrl: ReturnType<typeof vi.fn>;
        uploadToPresignedUrl: ReturnType<typeof vi.fn>;
    };
    logger: {
        warn: ReturnType<typeof vi.fn>;
    };
    translateService: TranslateService;
};

const TINY_MAX_SIZE_MB = 0.000001;
const CROP_SHIFTED_X = 20;

async function setupImageUploadFieldAsync(): Promise<ImageUploadFieldTestContext> {
    const imageUploadService = {
        requestUploadUrl: vi
            .fn()
            .mockReturnValue(
                of({ uploadUrl: 'https://upload.example.com', fileUrl: 'https://cdn.example.com/image.jpg', assetId: 'asset-1' }),
            ),
        uploadToPresignedUrl: vi.fn().mockReturnValue(of(void 0)),
        deleteAsset: vi.fn().mockReturnValue(of(void 0)),
    };
    const logger = { warn: vi.fn() };

    await TestBed.configureTestingModule({
        imports: [ImageUploadFieldComponent],
        providers: [
            provideTranslateTesting(),
            { provide: ImageUploadFacade, useValue: imageUploadService },
            { provide: FrontendLoggerService, useValue: logger },
        ],
    }).compileComponents();

    const fixture = TestBed.createComponent(ImageUploadFieldComponent);
    const component = fixture.componentInstance;
    const translateService = TestBed.inject(TranslateService);
    return { component, fixture, imageUploadService, logger, translateService };
}

describe('ImageUploadFieldComponent control value', () => {
    it('writes value and disabled state', async () => {
        const { component, fixture } = await setupImageUploadFieldAsync();
        fixture.detectChanges();

        component.value.set({ url: 'https://example.com/image.jpg', assetId: 'asset-1' });
        fixture.componentRef.setInput('disabled', true);
        fixture.detectChanges();

        expect(component['selection']()).toEqual({ url: 'https://example.com/image.jpg', assetId: 'asset-1' });
        expect(component.disabled()).toBe(true);
    });
});

describe('ImageUploadFieldComponent upload', () => {
    it('rejects non-image files before upload', async () => {
        const { component, fixture, imageUploadService, translateService } = await setupImageUploadFieldAsync();
        vi.spyOn(translateService, 'instant').mockReturnValue('Only images');
        fixture.detectChanges();

        component['handleIncomingFile'](new File(['text'], 'notes.txt', { type: 'text/plain' }));
        await fixture.whenStable();

        expect(component['error']()).toBe('Only images');
        expect(imageUploadService.requestUploadUrl).not.toHaveBeenCalled();
    });

    it('uploads valid image files and emits selection', async () => {
        const { component, fixture, imageUploadService } = await setupImageUploadFieldAsync();
        const changeSpy = vi.fn();
        component['imageChanged'].subscribe(changeSpy);
        fixture.detectChanges();

        component['handleIncomingFile'](new File(['image'], 'photo.png', { type: 'image/png' }));
        await fixture.whenStable();

        expect(imageUploadService.requestUploadUrl).toHaveBeenCalledOnce();
        expect(component['selection']()).toEqual({ url: 'https://cdn.example.com/image.jpg', assetId: 'asset-1' });
        expect(changeSpy).toHaveBeenCalledWith({ url: 'https://cdn.example.com/image.jpg', assetId: 'asset-1' });
    });

    it('rejects oversized files before requesting upload URL', async () => {
        const { component, fixture, imageUploadService, translateService } = await setupImageUploadFieldAsync();
        vi.spyOn(translateService, 'instant').mockReturnValue('Too large');
        fixture.componentRef.setInput('maxSizeMb', TINY_MAX_SIZE_MB);
        fixture.detectChanges();

        component['handleIncomingFile'](new File(['image-data'], 'photo.png', { type: 'image/png' }));
        await fixture.whenStable();

        expect(component['error']()).toBe('Too large');
        expect(imageUploadService.requestUploadUrl).not.toHaveBeenCalled();
    });

    it('shows upload error when presigned upload fails', async () => {
        const { component, fixture, imageUploadService, translateService } = await setupImageUploadFieldAsync();
        vi.spyOn(translateService, 'instant').mockReturnValue('Upload failed');
        imageUploadService.uploadToPresignedUrl.mockReturnValueOnce(throwError(() => new Error('upload failed')));
        fixture.detectChanges();

        component['handleIncomingFile'](new File(['image'], 'photo.png', { type: 'image/png' }));
        await fixture.whenStable();

        expect(component['error']()).toBe('Upload failed');
        expect(component['isUploading']()).toBe(false);
    });
});

describe('ImageUploadFieldComponent clearing', () => {
    it('clears selection and deletes asset when configured', async () => {
        const { component, fixture, imageUploadService } = await setupImageUploadFieldAsync();
        fixture.componentRef.setInput('deleteOnClear', true);
        component.value.set({ url: 'https://example.com/image.jpg', assetId: 'asset-1' });
        fixture.detectChanges();

        component['clearImage']();

        expect(component['selection']()).toEqual({ url: null, assetId: null });
        expect(component.value()).toEqual({ url: null, assetId: null });
        expect(component.touched()).toBe(true);
        expect(imageUploadService.deleteAsset).toHaveBeenCalledWith('asset-1');
    });

    it('logs delete failures without restoring cleared selection', async () => {
        const { component, fixture, imageUploadService, logger } = await setupImageUploadFieldAsync();
        imageUploadService.deleteAsset.mockReturnValueOnce(throwError(() => new Error('delete failed')));
        fixture.componentRef.setInput('deleteOnClear', true);
        component.value.set({ url: 'https://example.com/image.jpg', assetId: 'asset-1' });
        fixture.detectChanges();

        component['clearImage']();

        expect(component['selection']()).toEqual({ url: null, assetId: null });
        expect(logger.warn).toHaveBeenCalledWith('Failed to delete orphan image asset', expect.any(Error));
    });
});

describe('ImageUploadFieldComponent interactions', () => {
    it('does not open file picker when disabled or already selected', async () => {
        const { component, fixture } = await setupImageUploadFieldAsync();
        const clickSpy = vi.fn();
        const fileInput = { click: clickSpy } as unknown as HTMLInputElement;
        fixture.detectChanges();

        fixture.componentRef.setInput('disabled', true);
        fixture.detectChanges();
        component['onZoneClick'](fileInput);
        fixture.componentRef.setInput('disabled', false);
        component.value.set({ url: 'https://example.com/image.jpg', assetId: 'asset-1' });
        fixture.detectChanges();
        component['onZoneClick'](fileInput);

        expect(clickSpy).not.toHaveBeenCalled();
    });

    it('tracks drag state when enabled', async () => {
        const { component, fixture } = await setupImageUploadFieldAsync();
        const preventDefault = vi.fn();
        fixture.detectChanges();

        component['onDragOver']({ preventDefault } as unknown as DragEvent);
        expect(component['isDragging']()).toBe(true);

        component['onDragLeave']({ preventDefault } as unknown as DragEvent);
        expect(component['isDragging']()).toBe(false);
    });

    it('ignores drop and dragover while disabled', async () => {
        const { component, fixture, imageUploadService } = await setupImageUploadFieldAsync();
        const preventDefault = vi.fn();
        const stopPropagation = vi.fn();
        fixture.detectChanges();

        fixture.componentRef.setInput('disabled', true);
        fixture.detectChanges();
        component['onDragOver']({ preventDefault } as unknown as DragEvent);
        component['onDrop']({
            preventDefault,
            stopPropagation,
            dataTransfer: { files: [new File(['image'], 'photo.png', { type: 'image/png' })] },
        } as unknown as DragEvent);

        expect(component['isDragging']()).toBe(false);
        expect(imageUploadService.requestUploadUrl).not.toHaveBeenCalled();
        expect(preventDefault).toHaveBeenCalled();
        expect(stopPropagation).toHaveBeenCalled();
    });

    it('moves crop selection with keyboard arrows and ignores unrelated keys', async () => {
        const { component, fixture } = await setupImageUploadFieldAsync();
        const preventDefault = vi.fn();
        component['cropImageBounds'].set({ x: 0, y: 0, width: 100, height: 100 });
        component['cropSelection'].set({ x: 10, y: 10, width: 40, height: 40 });
        fixture.detectChanges();

        component['onCropKeydown']({ key: 'ArrowRight', shiftKey: true, preventDefault } as unknown as KeyboardEvent);
        expect(component['cropSelection']()?.x).toBe(CROP_SHIFTED_X);

        component['onCropKeydown']({ key: 'Enter', shiftKey: false, preventDefault } as unknown as KeyboardEvent);
        expect(component['cropSelection']()?.x).toBe(CROP_SHIFTED_X);
    });
});
