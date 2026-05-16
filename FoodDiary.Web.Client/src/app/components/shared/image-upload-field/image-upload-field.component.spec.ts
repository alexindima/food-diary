import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { FrontendLoggerService } from '../../../services/frontend-logger.service';
import { ImageUploadService } from '../../../shared/api/image-upload.service';
import { ImageUploadFieldComponent } from './image-upload-field.component';

type ImageUploadFieldTestContext = {
    component: ImageUploadFieldComponent;
    fixture: ComponentFixture<ImageUploadFieldComponent>;
    imageUploadService: {
        deleteAsset: ReturnType<typeof vi.fn>;
        requestUploadUrl: ReturnType<typeof vi.fn>;
        uploadToPresignedUrl: ReturnType<typeof vi.fn>;
    };
    translateService: TranslateService;
};

async function setupImageUploadFieldAsync(): Promise<ImageUploadFieldTestContext> {
    const imageUploadService = {
        requestUploadUrl: vi
            .fn()
            .mockReturnValue(
                of({ uploadUrl: 'https://upload.example.com', fileUrl: 'https://cdn.example.com/image.jpg', assetId: 'asset-1' }),
            ),
        uploadToPresignedUrl: vi.fn().mockReturnValue(of(undefined)),
        deleteAsset: vi.fn().mockReturnValue(of(undefined)),
    };

    await TestBed.configureTestingModule({
        imports: [ImageUploadFieldComponent, TranslateModule.forRoot()],
        providers: [
            { provide: ImageUploadService, useValue: imageUploadService },
            { provide: FrontendLoggerService, useValue: { warn: vi.fn() } },
        ],
    }).compileComponents();

    const fixture = TestBed.createComponent(ImageUploadFieldComponent);
    const component = fixture.componentInstance;
    const translateService = TestBed.inject(TranslateService);
    return { component, fixture, imageUploadService, translateService };
}

describe('ImageUploadFieldComponent control value', () => {
    it('writes value and disabled state', async () => {
        const { component, fixture } = await setupImageUploadFieldAsync();
        fixture.detectChanges();

        component.writeValue({ url: 'https://example.com/image.jpg', assetId: 'asset-1' });
        component.setDisabledState(true);

        expect(component.selection).toEqual({ url: 'https://example.com/image.jpg', assetId: 'asset-1' });
        expect(component.disabled).toBe(true);
    });
});

describe('ImageUploadFieldComponent upload', () => {
    it('rejects non-image files before upload', async () => {
        const { component, fixture, imageUploadService, translateService } = await setupImageUploadFieldAsync();
        vi.spyOn(translateService, 'instant').mockReturnValue('Only images');
        fixture.detectChanges();

        component.handleIncomingFile(new File(['text'], 'notes.txt', { type: 'text/plain' }));
        await fixture.whenStable();

        expect(component.error).toBe('Only images');
        expect(imageUploadService.requestUploadUrl).not.toHaveBeenCalled();
    });

    it('uploads valid image files and emits selection', async () => {
        const { component, fixture, imageUploadService } = await setupImageUploadFieldAsync();
        const changeSpy = vi.fn();
        component.imageChanged.subscribe(changeSpy);
        fixture.detectChanges();

        component.handleIncomingFile(new File(['image'], 'photo.png', { type: 'image/png' }));
        await fixture.whenStable();

        expect(imageUploadService.requestUploadUrl).toHaveBeenCalledOnce();
        expect(component.selection).toEqual({ url: 'https://cdn.example.com/image.jpg', assetId: 'asset-1' });
        expect(changeSpy).toHaveBeenCalledWith({ url: 'https://cdn.example.com/image.jpg', assetId: 'asset-1' });
    });
});

describe('ImageUploadFieldComponent clearing', () => {
    it('clears selection and deletes asset when configured', async () => {
        const { component, fixture, imageUploadService } = await setupImageUploadFieldAsync();
        fixture.componentRef.setInput('deleteOnClear', true);
        const onChange = vi.fn();
        const onTouched = vi.fn();
        component.registerOnChange(onChange);
        component.registerOnTouched(onTouched);
        component.writeValue({ url: 'https://example.com/image.jpg', assetId: 'asset-1' });
        fixture.detectChanges();

        component.clearImage();

        expect(component.selection).toEqual({ url: null, assetId: null });
        expect(onChange).toHaveBeenCalledWith({ url: null, assetId: null });
        expect(onTouched).toHaveBeenCalledOnce();
        expect(imageUploadService.deleteAsset).toHaveBeenCalledWith('asset-1');
    });
});

describe('ImageUploadFieldComponent interactions', () => {
    it('does not open file picker when disabled or already selected', async () => {
        const { component, fixture } = await setupImageUploadFieldAsync();
        const clickSpy = vi.fn();
        const fileInput = { click: clickSpy } as unknown as HTMLInputElement;
        fixture.detectChanges();

        component.setDisabledState(true);
        component.onZoneClick(fileInput);
        component.setDisabledState(false);
        component.writeValue({ url: 'https://example.com/image.jpg', assetId: 'asset-1' });
        component.onZoneClick(fileInput);

        expect(clickSpy).not.toHaveBeenCalled();
    });

    it('tracks drag state when enabled', async () => {
        const { component, fixture } = await setupImageUploadFieldAsync();
        const preventDefault = vi.fn();
        fixture.detectChanges();

        component.onDragOver({ preventDefault } as unknown as DragEvent);
        expect(component.isDragging).toBe(true);

        component.onDragLeave({ preventDefault } as unknown as DragEvent);
        expect(component.isDragging).toBe(false);
    });
});
