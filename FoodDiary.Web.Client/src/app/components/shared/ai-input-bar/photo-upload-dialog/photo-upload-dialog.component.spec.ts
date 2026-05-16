import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { FrontendLoggerService } from '../../../../services/frontend-logger.service';
import { ImageUploadService } from '../../../../shared/api/image-upload.service';
import { PhotoUploadDialogComponent } from './photo-upload-dialog.component';

type PhotoUploadDialogTestContext = {
    dialogRef: { close: ReturnType<typeof vi.fn> };
    fixture: ComponentFixture<PhotoUploadDialogComponent>;
};

async function setupPhotoUploadDialogAsync(): Promise<PhotoUploadDialogTestContext> {
    const dialogRef = { close: vi.fn() };
    await TestBed.configureTestingModule({
        imports: [PhotoUploadDialogComponent, TranslateModule.forRoot()],
        providers: [
            { provide: FdUiDialogRef, useValue: dialogRef },
            {
                provide: ImageUploadService,
                useValue: {
                    requestUploadUrl: vi.fn(),
                    uploadToPresignedUrl: vi.fn(),
                    deleteAsset: vi.fn().mockReturnValue(of(undefined)),
                },
            },
            { provide: FrontendLoggerService, useValue: { warn: vi.fn() } },
        ],
    }).compileComponents();

    return { dialogRef, fixture: TestBed.createComponent(PhotoUploadDialogComponent) };
}

describe('PhotoUploadDialogComponent', () => {
    it('closes with image selection only when asset id is present', async () => {
        const { dialogRef, fixture } = await setupPhotoUploadDialogAsync();
        const component = fixture.componentInstance;
        fixture.detectChanges();

        component.onImageChanged({ url: 'https://example.com/image.jpg', assetId: null });
        component.onImageChanged({ url: 'https://example.com/image.jpg', assetId: 'asset-1' });

        expect(dialogRef.close).toHaveBeenCalledOnce();
        expect(dialogRef.close).toHaveBeenCalledWith({ url: 'https://example.com/image.jpg', assetId: 'asset-1' });
    });

    it('closes with null on cancel', async () => {
        const { dialogRef, fixture } = await setupPhotoUploadDialogAsync();
        fixture.detectChanges();

        fixture.componentInstance.close();

        expect(dialogRef.close).toHaveBeenCalledWith(null);
    });
});
