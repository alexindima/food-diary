import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import { AiPhotoPreviewComponent } from './ai-photo-preview.component';

async function setupAiPhotoPreviewAsync(): Promise<ComponentFixture<AiPhotoPreviewComponent>> {
    await TestBed.configureTestingModule({
        imports: [AiPhotoPreviewComponent, TranslateModule.forRoot()],
    }).compileComponents();

    const fixture = TestBed.createComponent(AiPhotoPreviewComponent);
    fixture.componentRef.setInput('imageUrl', null);
    fixture.componentRef.setInput('sourceText', 'eggs');
    fixture.componentRef.setInput('sourceTextLabelKey', 'AI_INPUT_BAR.TEXT_PREVIEW_LABEL');
    fixture.componentRef.setInput('isAnalyzing', false);
    fixture.componentRef.setInput('isNutritionLoading', false);
    return fixture;
}

describe('AiPhotoPreviewComponent', () => {
    it('renders source text preview when image is absent', async () => {
        const fixture = await setupAiPhotoPreviewAsync();
        fixture.detectChanges();

        expect((fixture.nativeElement as HTMLElement).textContent).toContain('eggs');
    });
});
