import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { FdUiDialogService } from 'fd-ui-kit';
import { type Observable, of } from 'rxjs';
import { expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../src/testing/translate-testing.module';
import { adminId, adminUtcInstant } from '../../shared/models/semantics/admin-meaning';
import { AdminAuthService } from '../admin-auth/contracts/admin-session';
import type { AdminTemplateRevision } from '../admin-template-history/contracts/history';
import { AdminTemplateHistoryFacade } from '../admin-template-history/lib/admin-template-history.facade';
import { adminAiPromptsRoutes } from './admin-ai-prompts.routes';
import { AdminAiPromptsFacade } from './lib/admin-ai-prompts.facade';
import type { AdminAiPrompt } from './models/admin-ai-prompt';
import type { AdminAiPromptScenario } from './models/admin-ai-prompt-scenario';

@Component({ selector: 'fd-test-prompts-destination', template: '<p>Review destination</p>' })
class PromptReviewDestinationComponent {}

const savedOnUtc = adminUtcInstant('2026-01-01T00:00:00Z');
const saved: AdminAiPrompt = {
    id: adminId<'ai-prompt'>('nutrition-prompt'),
    key: 'nutrition',
    locale: 'en',
    promptText: 'Current custom prompt',
    version: 1,
    isActive: true,
    createdOnUtc: savedOnUtc,
    updatedOnUtc: null,
};
const baseScenario: AdminAiPromptScenario = {
    key: 'vision',
    locale: 'en',
    promptText: 'Built-in {{languageHint}}',
    source: 'built-in',
    sourceLocale: 'en',
    inheritedPromptText: 'Built-in {{languageHint}}',
    inheritedSource: 'built-in',
    variables: ['languageHint'],
    responseFormatJson: '{}',
    template: null,
};
const available: AdminAiPromptScenario[] = [
    baseScenario,
    { ...baseScenario, key: 'nutrition', source: 'custom', promptText: saved.promptText, template: saved },
];

function button(root: HTMLElement, text: string): HTMLButtonElement {
    const control = [...root.querySelectorAll('button')].find(candidate => candidate.textContent.trim() === text);
    if (control === undefined) {
        throw new Error(`Expected the ${text} button.`);
    }
    return control;
}

it('keeps an archived draft after a cancelled route change and discards it only on confirmation', async () => {
    const revision: AdminTemplateRevision = {
        id: adminId<'template-revision'>('archived-prompt'),
        subject: null,
        htmlBody: null,
        textBody: 'Archived prompt {{languageHint}}',
        isActive: true,
        version: 1,
        savedOnUtc,
        archivedOnUtc: adminUtcInstant('2026-01-02T00:00:00Z'),
    };
    const getRevisions = vi.fn<AdminTemplateHistoryFacade['getRevisions']>().mockReturnValue(of([revision]));
    const close = vi.fn<() => Observable<boolean>>().mockReturnValueOnce(of(false)).mockReturnValueOnce(of(true));
    const preview = vi.fn<AdminAiPromptsFacade['preview']>().mockReturnValue(of({ text: 'Checked archived draft' }));
    const save = vi.fn<AdminAiPromptsFacade['save']>();
    const getScenarios = vi.fn<AdminAiPromptsFacade['getScenarios']>().mockReturnValue(of(available));
    TestBed.configureTestingModule({
        providers: [
            provideRouter([
                { path: 'ai-prompts', children: adminAiPromptsRoutes },
                { path: 'destination', component: PromptReviewDestinationComponent },
            ]),
            provideTranslateTesting(),
            { provide: AdminTemplateHistoryFacade, useValue: { getRevisions } },
            { provide: AdminAiPromptsFacade, useValue: { getScenarios, preview, save } },
            { provide: FdUiDialogService, useValue: { open: (): { afterClosed: typeof close } => ({ afterClosed: close }) } },
            {
                provide: AdminAuthService,
                useValue: {
                    applySsoFromQueryAsync: vi.fn<AdminAuthService['applySsoFromQueryAsync']>().mockResolvedValue(undefined),
                    sessionUnavailable: (): boolean => false,
                    isAuthenticated: (): boolean => true,
                    isAdmin: (): boolean => true,
                },
            },
        ],
    });
    const harness = await RouterTestingHarness.create('/ai-prompts');
    const router = TestBed.inject(Router);
    const root = harness.routeNativeElement;
    if (root === null) {
        throw new Error('Expected the prompt editor route.');
    }
    button(root, 'ADMIN_PROMPTS.SCENARIOS_nutrition').click();
    await vi.waitFor(() => {
        harness.detectChanges();
        expect(getRevisions).toHaveBeenLastCalledWith('ai-prompts', 'nutrition', 'en');
    });
    root.querySelector<HTMLElement>('fd-admin-template-history details > summary')?.click();
    button(root, 'ADMIN_TEMPLATE_HISTORY.RESTORE').click();
    harness.detectChanges();
    expect(root.querySelector<HTMLTextAreaElement>('form textarea')?.value).toBe(revision.textBody);
    expect(button(root, 'ADMIN_PROMPTS.APPLY').disabled).toBe(true);
    expect(save).not.toHaveBeenCalled();

    button(root, 'ADMIN_PROMPTS.PREVIEW').click();
    await vi.waitFor(() => {
        harness.detectChanges();
        expect(button(root, 'ADMIN_PROMPTS.APPLY').disabled).toBe(false);
    });
    expect(preview).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ key: 'nutrition', promptText: revision.textBody }));
    expect(await router.navigateByUrl('/destination')).toBe(false);
    expect(router.url).toBe('/ai-prompts');
    expect(root.querySelector<HTMLTextAreaElement>('form textarea')?.value).toBe(revision.textBody);
    expect(await router.navigateByUrl('/destination')).toBe(true);
    harness.detectChanges();
    expect(harness.routeNativeElement?.textContent).toContain('Review destination');
    expect(close).toHaveBeenCalledTimes(2);
    expect(save).not.toHaveBeenCalled();
});
