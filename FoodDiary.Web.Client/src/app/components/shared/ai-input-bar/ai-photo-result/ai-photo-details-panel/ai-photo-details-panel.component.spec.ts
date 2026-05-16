import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import { AiPhotoDetailsPanelComponent } from './ai-photo-details-panel.component';

async function setupAiPhotoDetailsPanelAsync(): Promise<ComponentFixture<AiPhotoDetailsPanelComponent>> {
    await TestBed.configureTestingModule({
        imports: [AiPhotoDetailsPanelComponent, FormsModule, TranslateModule.forRoot()],
    }).compileComponents();

    const fixture = TestBed.createComponent(AiPhotoDetailsPanelComponent);
    fixture.componentRef.setInput('isVisible', true);
    fixture.componentRef.setInput('showDetails', true);
    fixture.componentRef.setInput('isExpanded', false);
    fixture.componentRef.setInput('toggleView', { labelKey: 'COMMON.SHOW_MORE', icon: 'expand_more' });
    fixture.componentRef.setInput('submitLabelKey', 'COMMON.SAVE');
    fixture.componentRef.setInput('submitDisabled', false);
    fixture.componentRef.setInput('date', '2026-05-17');
    fixture.componentRef.setInput('time', '12:30');
    fixture.componentRef.setInput('comment', '');
    fixture.componentRef.setInput('preMealSatietyLevel', null);
    fixture.componentRef.setInput('postMealSatietyLevel', null);
    return fixture;
}

describe('AiPhotoDetailsPanelComponent', () => {
    it('does not render actions when hidden', async () => {
        const fixture = await setupAiPhotoDetailsPanelAsync();
        fixture.componentRef.setInput('isVisible', false);
        fixture.detectChanges();

        expect((fixture.nativeElement as HTMLElement).querySelector('.ai-photo-result__actions')).toBeNull();
    });

    it('emits toggle and submit actions', async () => {
        const fixture = await setupAiPhotoDetailsPanelAsync();
        const toggleSpy = vi.fn();
        const submitSpy = vi.fn();
        fixture.componentInstance.detailsToggle.subscribe(toggleSpy);
        fixture.componentInstance.mealSubmit.subscribe(submitSpy);
        fixture.detectChanges();

        (fixture.nativeElement as HTMLElement).querySelector<HTMLElement>('.ai-photo-result__details-toggle')?.click();
        (fixture.nativeElement as HTMLElement).querySelector<HTMLElement>('.ai-photo-result__submit-action')?.click();

        expect(toggleSpy).toHaveBeenCalledOnce();
        expect(submitSpy).toHaveBeenCalledOnce();
    });
});
