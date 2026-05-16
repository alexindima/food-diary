import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import { AiPhotoResultActionsComponent } from './ai-photo-result-actions.component';

async function setupAiPhotoResultActionsAsync(isEditing: boolean): Promise<ComponentFixture<AiPhotoResultActionsComponent>> {
    await TestBed.configureTestingModule({
        imports: [AiPhotoResultActionsComponent, TranslateModule.forRoot()],
    }).compileComponents();

    const fixture = TestBed.createComponent(AiPhotoResultActionsComponent);
    fixture.componentRef.setInput('isAnalyzing', false);
    fixture.componentRef.setInput('isEditing', isEditing);
    fixture.componentRef.setInput('editActionView', { variant: 'secondary', fill: 'outline', labelKey: 'EDIT' });
    return fixture;
}

describe('AiPhotoResultActionsComponent', () => {
    it('emits action button events', async () => {
        const fixture = await setupAiPhotoResultActionsAsync(true);
        const component = fixture.componentInstance;
        const reanalyzeSpy = vi.fn();
        const editActionSpy = vi.fn();
        const cancelSpy = vi.fn();
        component.reanalyze.subscribe(reanalyzeSpy);
        component.editAction.subscribe(editActionSpy);
        component.editCancel.subscribe(cancelSpy);
        fixture.detectChanges();

        component.reanalyze.emit();
        component.editAction.emit();
        component.editCancel.emit();

        expect(reanalyzeSpy).toHaveBeenCalledOnce();
        expect(editActionSpy).toHaveBeenCalledOnce();
        expect(cancelSpy).toHaveBeenCalledOnce();
    });
});
