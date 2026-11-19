import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FormRoot } from '@angular/forms/signals';
import { By } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { FdUiDialogService, FdUiSelectComponent } from 'fd-ui-kit';
import { type Observable, of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { adminId, adminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
import { AdminTemplateHistoryService } from '../../admin-template-history/api/admin-template-history.service';
import { AdminAiPromptWorkbenchComponent } from '../components/admin-ai-prompt-workbench';
import { AdminAiPromptsFacade } from '../lib/admin-ai-prompts.facade';
import type { AdminAiPrompt } from '../models/admin-ai-prompt';
import type { AdminAiPromptKey, AdminAiPromptScenario } from '../models/admin-ai-prompt-scenario';
import { AdminAiPromptsPageComponent } from './admin-ai-prompts';

const scenarios: AdminAiPromptScenario[] = (['vision', 'text-parse', 'nutrition', 'product-label'] as AdminAiPromptKey[]).flatMap(key =>
    ['en', 'ru'].map(locale => ({
        key,
        locale,
        promptText: 'Built-in {{languageHint}}',
        source: 'built-in',
        sourceLocale: 'en',
        inheritedPromptText: 'Built-in {{languageHint}}',
        inheritedSource: 'built-in',
        template: null,
        variables: ['languageHint'],
        responseFormatJson: JSON.stringify({ name: key === 'nutrition' ? 'food_nutrition' : 'food_vision', strict: true }),
    })),
);

describe('AdminAiPromptsPageComponent', () => {
    const api = { getScenarios: vi.fn(), preview: vi.fn(), test: vi.fn(), save: vi.fn() };
    beforeEach(async () => {
        vi.clearAllMocks();
        api.getScenarios.mockReturnValue(of(scenarios));
        api.preview.mockReturnValue(of({ text: 'Resolved text' }));
        await TestBed.configureTestingModule({
            imports: [AdminAiPromptsPageComponent],
            providers: [
                provideRouter([]),
                provideHttpClient(),
                provideHttpClientTesting(),
                ...provideTranslateTesting(),
                { provide: AdminAiPromptsFacade, useValue: api },
                {
                    provide: FdUiDialogService,
                    useValue: {
                        open: (): { afterClosed: () => Observable<boolean> } => ({ afterClosed: (): Observable<boolean> => of(false) }),
                    },
                },
            ],
        }).compileComponents();
    });

    it('shows supported scenarios even without saved templates and requires a preview before applying', async () => {
        const fixture = TestBed.createComponent(AdminAiPromptsPageComponent);
        fixture.detectChanges();
        const page = fixture.componentInstance;
        expect((fixture.nativeElement as HTMLElement).querySelectorAll('nav fd-ui-button')).toHaveLength(scenarios.length / 2);
        expect((fixture.nativeElement as HTMLElement).textContent).toContain('ADMIN_PROMPTS.SOURCE_built-in');
        page['setCustom'](true);
        fixture.detectChanges();
        expect(page['canApply']()).toBe(false);
        const workbench: AdminAiPromptWorkbenchComponent = fixture.debugElement
            .query(By.directive(AdminAiPromptWorkbenchComponent))
            .injector.get(AdminAiPromptWorkbenchComponent);
        await workbench['inspectAsync'](false);
        fixture.detectChanges();
        expect(page['canApply']()).toBe(true);
        expect(api.save).not.toHaveBeenCalled();
        expect(api.test).not.toHaveBeenCalled();
        page['formModel'].update(value => ({ ...value, promptText: 'A different draft' }));
        fixture.detectChanges();
        expect(page['canApply']()).toBe(false);
    });

    it('keeps unsaved text when switching scenario is cancelled', () => {
        const fixture = TestBed.createComponent(AdminAiPromptsPageComponent);
        fixture.detectChanges();
        fixture.componentInstance['setCustom'](true);
        const select = fixture.debugElement.query(By.directive(FdUiSelectComponent)).injector.get(FdUiSelectComponent);
        select.value.set('ru');
        fixture.componentInstance['selectScenario']('nutrition', 'ru');
        expect(select.value()).toBe('en');
        expect(fixture.componentInstance['key']()).toBe('vision');
        expect(fixture.componentInstance['locale']()).toBe('en');
        expect(fixture.componentInstance['formModel']().isActive).toBe(true);
    });

    it('shows a load error and supports retry', () => {
        api.getScenarios.mockReturnValueOnce(throwError(() => new Error('offline')));
        const fixture = TestBed.createComponent(AdminAiPromptsPageComponent);
        fixture.detectChanges();
        expect((fixture.nativeElement as HTMLElement).querySelector('fd-admin-load-error')).not.toBeNull();
        fixture.componentInstance['load']();
        fixture.detectChanges();
        expect(fixture.componentInstance['selected']()?.key).toBe('vision');
    });
});

function button(root: HTMLElement, text: string): HTMLButtonElement {
    const result = Array.from(root.querySelectorAll('button')).find(control => control.textContent.trim() === text);
    if (result === undefined) {
        throw new Error(`Missing prompt editor button ${text}`);
    }
    return result;
}

function savedPrompt(promptText: string): AdminAiPrompt {
    return {
        id: adminId<'ai-prompt'>('vision-prompt'),
        key: 'vision',
        locale: 'en',
        promptText,
        version: 1,
        isActive: true,
        createdOnUtc: adminUtcInstant('2026-10-01T00:00:00Z'),
        updatedOnUtc: null,
    };
}

describe('AdminAiPromptsPageComponent saves through the prompt editor', () => {
    const api = {
        getScenarios: vi.fn<AdminAiPromptsFacade['getScenarios']>(),
        preview: vi.fn<AdminAiPromptsFacade['preview']>(),
        save: vi.fn<AdminAiPromptsFacade['save']>(),
    };
    let fixture: ComponentFixture<AdminAiPromptsPageComponent>;
    let root: HTMLElement;

    beforeEach(async () => {
        vi.resetAllMocks();
        api.getScenarios.mockReturnValue(of(scenarios));
        api.preview.mockReturnValue(of({ text: 'Verified draft' }));
        await TestBed.configureTestingModule({
            imports: [AdminAiPromptsPageComponent],
            providers: [
                provideRouter([]),
                ...provideTranslateTesting(),
                { provide: AdminAiPromptsFacade, useValue: api },
                { provide: AdminTemplateHistoryService, useValue: { getRevisions: vi.fn().mockReturnValue(of([])) } },
            ],
        }).compileComponents();
        fixture = TestBed.createComponent(AdminAiPromptsPageComponent);
        fixture.detectChanges();
        root = fixture.nativeElement as HTMLElement;
    });

    it('keeps the checked draft after a failed save and refreshes the saved scenario on retry', async () => {
        const promptText = 'Identify only the foods mentioned by the user';
        const saved = savedPrompt(promptText);
        const saving = new Subject<AdminAiPrompt>();
        api.save.mockReturnValueOnce(saving).mockReturnValueOnce(of(saved));
        root.querySelector<HTMLInputElement>('form input[type="checkbox"]')?.click();
        fixture.detectChanges();
        const draft = root.querySelector<HTMLTextAreaElement>('form textarea');
        if (draft === null) {
            throw new Error('Missing editable prompt');
        }
        draft.value = promptText;
        draft.dispatchEvent(new Event('input', { bubbles: true }));
        fixture.detectChanges();
        expect(button(root, 'ADMIN_PROMPTS.APPLY').disabled).toBe(true);
        button(root, 'ADMIN_PROMPTS.PREVIEW').click();
        await fixture.whenStable();
        fixture.detectChanges();

        button(root, 'ADMIN_PROMPTS.APPLY').click();
        fixture.detectChanges();
        expect(api.save).toHaveBeenCalledExactlyOnceWith('vision', 'en', promptText, true);
        expect(draft.readOnly).toBe(true);
        button(root, 'ADMIN_PROMPTS.SCENARIOS_nutrition').click();
        fixture.detectChanges();
        expect(root.querySelector('.prompt-heading h2')?.textContent).toContain('ADMIN_PROMPTS.SCENARIOS_vision');
        saving.error(new Error('Save temporarily failed'));
        await fixture.whenStable();
        fixture.detectChanges();

        expect(root.querySelector('form [role="alert"]:not([hidden])')?.textContent).toContain('ADMIN_PROMPTS.SAVE_ERROR');
        expect(draft.value).toBe(promptText);
        expect(draft.readOnly).toBe(false);
        expect(button(root, 'ADMIN_PROMPTS.APPLY').disabled).toBe(false);
        api.getScenarios.mockReturnValueOnce(
            of(
                scenarios.map<AdminAiPromptScenario>(item =>
                    item.key === 'vision' && item.locale === 'en' ? { ...item, promptText, source: 'custom', template: saved } : item,
                ),
            ),
        );
        const form = fixture.debugElement.query(By.directive(FormRoot)).injector.get(FormRoot);
        await vi.waitFor(() => {
            expect(form.fieldTree()().submitting()).toBe(false);
        });
        button(root, 'ADMIN_PROMPTS.APPLY').click();
        await fixture.whenStable();
        fixture.detectChanges();

        expect(api.save).toHaveBeenLastCalledWith('vision', 'en', promptText, true);
        expect(api.save).toHaveBeenCalledTimes(2);
        expect(root.querySelector('form [role="alert"]:not([hidden])')).toBeNull();
        expect(root.querySelector('form [role="status"]:not([hidden])')?.textContent).toContain('ADMIN_PROMPTS.SAVED');
        expect(root.querySelector('.prompt-current pre')?.textContent).toBe(promptText);
        expect(button(root, 'ADMIN_PROMPTS.APPLY').disabled).toBe(true);
    });
});
