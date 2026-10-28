import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import { AuthService } from '../../../services/auth.service';
import { FrontendLoggerService } from '../../../services/frontend-logger.service';
import { NavigationService } from '../../../services/navigation.service';
import { LocalizationService } from '../../../shared/i18n/localization.service';
import { AiFoodFacade } from '../../../shared/lib/ai-food.facade';
import { ImageUploadFacade } from '../../../shared/lib/image-upload.facade';
import { UserFacade } from '../../../shared/lib/user.facade';
import { SpeechRecognitionService } from '../../../shared/platform/speech-recognition.service';
import { AiInputBarComponent } from '../ai-input-bar/ai-input-bar';
import type { AiInputBarResult } from '../ai-input-bar/ai-input-bar.types';
import { MealsPreviewComponent } from './meals-preview';

async function setupMealsPreviewAsync(): Promise<ComponentFixture<MealsPreviewComponent>> {
    await TestBed.configureTestingModule({
        imports: [MealsPreviewComponent],
        providers: [
            provideTranslateTesting(),
            { provide: AiFoodFacade, useValue: {} },
            { provide: UserFacade, useValue: { user: signal(null) } },
            { provide: AuthService, useValue: { isPremium: signal(true), getUserId: (): string => 'meal-preview-test' } },
            { provide: LocalizationService, useValue: { getCurrentLanguage: (): string => 'en' } },
            { provide: NavigationService, useValue: {} },
            { provide: FdUiDialogService, useValue: { open: vi.fn() } },
            { provide: ImageUploadFacade, useValue: { deleteAsset: vi.fn().mockReturnValue(of(undefined)) } },
            { provide: FrontendLoggerService, useValue: { warn: vi.fn() } },
            {
                provide: SpeechRecognitionService,
                useValue: { isSupported: false, isListening: signal(false), isOwnedBy: (): boolean => false, stop: vi.fn() },
            },
        ],
    }).compileComponents();

    const fixture = TestBed.createComponent(MealsPreviewComponent);
    fixture.componentRef.setInput('entries', []);
    return fixture;
}

describe('MealsPreviewComponent header', () => {
    it('opens the list from the visible all action', async () => {
        const fixture = await setupMealsPreviewAsync();
        const viewAll = vi.fn();
        fixture.componentInstance.viewAll.subscribe(viewAll);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;

        host.querySelector<HTMLButtonElement>('.meals-preview__header button')?.click();

        expect(viewAll).toHaveBeenCalledExactlyOnceWith(undefined);
    });

    it('supports previews without a list action', async () => {
        const fixture = await setupMealsPreviewAsync();
        fixture.componentRef.setInput('showViewAll', false);
        fixture.detectChanges();

        expect((fixture.nativeElement as HTMLElement).querySelector('.meals-preview__header button')).toBeNull();
    });
});

describe('MealsPreviewComponent contextual composer', () => {
    it('opens one contextual composer from the row and preserves drafts when switching and collapsing', async () => {
        const fixture = await setupMealsPreviewAsync();
        fixture.componentRef.setInput('showAiButtons', true);
        fixture.componentRef.setInput('entries', [{ slot: 'lunch' }, { slot: 'dinner' }]);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const rows = host.querySelectorAll<HTMLButtonElement>('.meals-preview__placeholder-main');
        const add = vi.fn();
        fixture.componentInstance.add.subscribe(add);

        rows[0].focus();
        rows[0].click();
        fixture.detectChanges();
        const lunch = host.querySelector<HTMLInputElement>('fd-ai-input-bar input[type="text"]');
        expect(lunch).not.toBeNull();
        if (lunch === null) {
            return;
        }
        expect(rows[0].getAttribute('aria-expanded')).toBe('true');
        expect(rows[0].ownerDocument.activeElement).toBe(rows[0]);
        const panelId = rows[0].getAttribute('aria-controls');
        expect(lunch.closest('.meals-preview__ai-panel')?.id).toBe(panelId);
        lunch.value = 'Rice and chicken';
        lunch.dispatchEvent(new Event('input'));

        rows[1].click();
        fixture.detectChanges();
        const panels = host.querySelectorAll<HTMLElement>('.meals-preview__ai-panel');
        expect(Array.from(panels).filter(panel => panel.hidden === false)).toHaveLength(1);
        expect(rows[0].getAttribute('aria-expanded')).toBe('false');
        const dinnerComposer = fixture.debugElement.queryAll(By.directive(AiInputBarComponent))[1].componentInstance as AiInputBarComponent;
        expect(dinnerComposer.mealType()).toBe('dinner');

        rows[1].click();
        fixture.detectChanges();
        expect(Array.from(panels).every(panel => panel.hidden === true)).toBe(true);
        rows[0].click();
        fixture.detectChanges();
        expect(host.querySelector('fd-ai-input-bar input[type="text"]')).toBe(lunch);
        expect(lunch.value).toBe('Rice and chicken');
        expect(add).not.toHaveBeenCalled();
    });
});

describe('MealsPreviewComponent saved drafts', () => {
    it('keeps other drafts when a meal is saved and disables switching during persistence', async () => {
        const fixture = await setupMealsPreviewAsync();
        fixture.componentRef.setInput('showAiButtons', true);
        fixture.componentRef.setInput('entries', [{ slot: 'lunch' }, { slot: 'dinner' }]);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const rows = host.querySelectorAll<HTMLButtonElement>('.meals-preview__placeholder-main');
        for (const [index, row] of Array.from(rows).entries()) {
            row.click();
            fixture.detectChanges();
            const input = host.querySelectorAll<HTMLInputElement>('fd-ai-input-bar input[type="text"]')[index];
            input.value = index === 0 ? 'Lunch draft' : 'Dinner draft';
            input.dispatchEvent(new Event('input'));
        }

        const dinnerComposer = fixture.debugElement.queryAll(By.directive(AiInputBarComponent))[1].componentInstance as AiInputBarComponent;
        dinnerComposer.mealCreateRequested.emit({ source: 'Text', recognizedAtUtc: '2026-05-17T00:00:00Z', items: [] });
        fixture.componentRef.setInput('isAiMealSaving', true);
        fixture.detectChanges();
        expect(rows[0].disabled).toBe(true);
        rows[0].click();
        expect(rows[1].getAttribute('aria-expanded')).toBe('true');

        fixture.componentRef.setInput('isAiMealSaving', false);
        fixture.componentRef.setInput('aiMealClearToken', 1);
        fixture.detectChanges();
        expect(rows[1].getAttribute('aria-expanded')).toBe('false');
        const inputs = host.querySelectorAll<HTMLInputElement>('fd-ai-input-bar input[type="text"]');
        expect(inputs[1].value).toBe('');
        rows[0].click();
        fixture.detectChanges();
        expect(inputs[0].value).toBe('Lunch draft');
    });
});

describe('MealsPreviewComponent AI panel', () => {
    it('retains the manual action and selected slot inside the expanded card', async () => {
        const fixture = await setupMealsPreviewAsync();
        fixture.componentRef.setInput('showAiButtons', true);
        fixture.componentRef.setInput('entries', [{ slot: 'dinner' }]);
        const add = vi.fn();
        fixture.componentInstance.add.subscribe(add);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        host.querySelector<HTMLButtonElement>('.meals-preview__placeholder-main')?.click();
        fixture.detectChanges();

        host.querySelector<HTMLButtonElement>('[inputActions] button')?.click();

        expect(add).toHaveBeenCalledExactlyOnceWith('dinner');
        expect(host.querySelector('.meals-preview__placeholder fd-ai-input-bar')).not.toBeNull();
    });

    it('toggles expanded AI slot', async () => {
        const fixture = await setupMealsPreviewAsync();
        const component = fixture.componentInstance;
        fixture.detectChanges();

        component['toggleAi']('breakfast');
        expect(component['expandedAiSlot']()).toBe('breakfast');

        component['toggleAi']('breakfast');
        expect(component['expandedAiSlot']()).toBeNull();
    });

    it('normalizes missing slot to null', async () => {
        const fixture = await setupMealsPreviewAsync();
        const component = fixture.componentInstance;
        fixture.detectChanges();

        component['toggleAi']();

        expect(component['expandedAiSlot']()).toBeNull();
    });

    it('keeps AI slot open while meal create is pending and emits created meal result', async () => {
        const fixture = await setupMealsPreviewAsync();
        const component = fixture.componentInstance;
        const emitSpy = vi.fn<(result: AiInputBarResult) => void>();
        const result: AiInputBarResult = {
            source: 'Text',
            recognizedAtUtc: '2026-05-17T00:00:00Z',
            items: [],
        };
        component['aiMealCreateRequested'].subscribe(resultValue => {
            emitSpy(resultValue);
        });
        component['toggleAi']('lunch');
        fixture.detectChanges();

        component['createMealFromAiResult'](result);

        expect(component['expandedAiSlot']()).toBe('lunch');
        expect(emitSpy).toHaveBeenCalledWith(result);
    });
});

describe('MealsPreviewComponent save completion', () => {
    it('collapses AI slot when clear token changes after successful create', async () => {
        const fixture = await setupMealsPreviewAsync();
        const component = fixture.componentInstance;
        fixture.detectChanges();

        component['toggleAi']('lunch');
        component['createMealFromAiResult']({ source: 'Text', recognizedAtUtc: '2026-05-17T00:00:00Z', items: [] });
        fixture.componentRef.setInput('aiMealClearToken', 1);
        fixture.detectChanges();

        expect(component['expandedAiSlot']()).toBeNull();
    });

    it('keeps the expanded draft when the global quick input saves or a slot save fails', async () => {
        const fixture = await setupMealsPreviewAsync();
        const component = fixture.componentInstance;
        fixture.detectChanges();
        component['toggleAi']('lunch');
        fixture.componentRef.setInput('aiMealClearToken', 1);
        fixture.detectChanges();
        expect(component['expandedAiSlot']()).toBe('lunch');

        component['createMealFromAiResult']({ source: 'Text', recognizedAtUtc: '2026-05-17T00:00:00Z', items: [] });
        fixture.componentRef.setInput('isAiMealSaving', true);
        fixture.detectChanges();
        fixture.componentRef.setInput('isAiMealSaving', false);
        fixture.detectChanges();
        expect(component['expandedAiSlot']()).toBe('lunch');

        fixture.componentRef.setInput('aiMealClearToken', 2);
        fixture.detectChanges();
        expect(component['expandedAiSlot']()).toBe('lunch');
    });
});
