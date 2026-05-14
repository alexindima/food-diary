import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { TranslateModule } from '@ngx-translate/core';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { ImageUploadFieldComponent } from '../../../../../components/shared/image-upload-field/image-upload-field.component';
import { FrontendLoggerService } from '../../../../../services/frontend-logger.service';
import { ImageUploadService } from '../../../../../shared/api/image-upload.service';
import type { ImageSelection } from '../../../../../shared/models/image-upload.data';
import { MealPhotoUploadPanelComponent } from './meal-photo-upload-panel.component';

const RESIZE_MAX_DIMENSION = 1536;

describe('MealPhotoUploadPanelComponent', () => {
    it('should pass image upload inputs and emit image changes', async () => {
        const initialSelection: ImageSelection = { url: 'https://example.com/photo.jpg', assetId: 'asset-1' };
        const nextSelection: ImageSelection = { url: 'https://example.com/next.jpg', assetId: 'asset-2' };
        const { component, fixture } = await setupComponentAsync({ initialSelection });
        const imageChangedSpy = vi.fn();
        component.imageChanged.subscribe(imageChangedSpy);

        fixture.detectChanges();
        const uploadField = fixture.debugElement.query(By.directive(ImageUploadFieldComponent))
            .componentInstance as ImageUploadFieldComponent;
        uploadField.imageChanged.emit(nextSelection);

        expect(uploadField.appearance()).toBe('preview');
        expect(uploadField.cropEnabled()).toBe(false);
        expect(uploadField.resizeMaxDimension()).toBe(RESIZE_MAX_DIMENSION);
        expect(uploadField.deleteOnClear()).toBe(true);
        expect(uploadField.initialSelection()).toEqual(initialSelection);
        expect(imageChangedSpy).toHaveBeenCalledWith(nextSelection);
    });

    it('should render status and loading overlay states', async () => {
        const { fixture } = await setupComponentAsync({
            statusKey: 'CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.ANALYZING',
            isLoading: true,
            isNutritionLoading: false,
        });

        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;

        expect(host.textContent).toContain('CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.ANALYZING');
        expect(host.querySelector('.photo-ai-dialog__status--loading')).not.toBeNull();
        expect(host.querySelector('.photo-ai-dialog__scan-overlay')).not.toBeNull();
        expect(host.querySelector('.photo-ai-dialog__scan-line--nutrition')).toBeNull();
    });

    it('should mark scan line as nutrition loading', async () => {
        const { fixture } = await setupComponentAsync({ isNutritionLoading: true });

        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;

        expect(host.querySelector('.photo-ai-dialog__scan-line--nutrition')).not.toBeNull();
    });
});

async function setupComponentAsync(
    overrides: Partial<{
        initialSelection: ImageSelection | null;
        isLoading: boolean;
        isNutritionLoading: boolean;
        statusKey: string | null;
    }> = {},
): Promise<{
    component: MealPhotoUploadPanelComponent;
    fixture: ComponentFixture<MealPhotoUploadPanelComponent>;
}> {
    await TestBed.resetTestingModule()
        .configureTestingModule({
            imports: [MealPhotoUploadPanelComponent, TranslateModule.forRoot()],
            providers: [
                {
                    provide: ImageUploadService,
                    useValue: {
                        deleteAsset: vi.fn().mockReturnValue(of(void 0)),
                        requestUploadUrl: vi.fn(),
                        uploadToPresignedUrl: vi.fn(),
                    },
                },
                { provide: FrontendLoggerService, useValue: { warn: vi.fn() } },
            ],
        })
        .compileComponents();

    const fixture = TestBed.createComponent(MealPhotoUploadPanelComponent);
    fixture.componentRef.setInput('initialSelection', overrides.initialSelection ?? null);
    fixture.componentRef.setInput('statusKey', overrides.statusKey ?? null);
    fixture.componentRef.setInput('isLoading', overrides.isLoading ?? false);
    fixture.componentRef.setInput('isNutritionLoading', overrides.isNutritionLoading ?? false);

    return {
        component: fixture.componentInstance,
        fixture,
    };
}
