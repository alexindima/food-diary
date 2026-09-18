import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { FdUiDialogService, FdUiSelectComponent } from 'fd-ui-kit';
import { type Observable, of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { AdminAiPromptWorkbenchComponent } from '../components/admin-ai-prompt-workbench';
import { AdminAiPromptsFacade } from '../lib/admin-ai-prompts.facade';
import type { AdminAiPromptKey, AdminAiPromptScenario } from '../models/admin-ai-prompt-scenario';
import { AdminAiPromptsPageComponent } from './admin-ai-prompts';

describe('AdminAiPromptsPageComponent', () => {
    const scenarios: AdminAiPromptScenario[] = (['vision', 'text-parse', 'nutrition'] as AdminAiPromptKey[]).flatMap(key =>
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
